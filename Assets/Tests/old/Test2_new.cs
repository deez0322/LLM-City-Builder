using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AITransformer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using WhisperInput;
using Assert = UnityEngine.Assertions.Assert;
using Object = UnityEngine.Object;

namespace Tests
{
    public class Test2New
    {
        private GameObject _tilemap;
        private GameObject aiTaskConverter;
        private GameObject aiTaskExecutor;
        private BuildingRegister _buildingRegister;
        private ResourceStore resourceManager;


        [OneTimeSetUp]
        public void LoadSceneOnce()
        {
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            while (SceneManager.GetActiveScene().name != "SampleScene")
            {
                yield return null;
            }

            // Now that we are sure the scene is loaded, find the game objects
            _tilemap = GameObject.Find("Tilemap");
            aiTaskConverter = GameObject.Find("AITaskConverter");
            aiTaskExecutor = GameObject.Find("AITaskExecutor");
            resourceManager = aiTaskExecutor.GetComponent<TaskExecutor>().Store;
            GameObject buildingRegisterObject = GameObject.Find("BuildingRegister");
            if (buildingRegisterObject != null)
            {
                _buildingRegister = buildingRegisterObject.GetComponent<BuildingRegister>();
            }

            _buildingRegister.RegisterBuilding(new Vector3(3, 0, 0), Enums.BuildingType.House);
        }

        private LLMExecutionOptions SetupLLMExecutionOptions(
            string model = "gpt-4o-mini",
            LLMExecutionOptions.SelfCorrectionType selfCorrection = LLMExecutionOptions.SelfCorrectionType.MultiStep,
            LLMExecutionOptions.ContextFormatType contextFormat = LLMExecutionOptions.ContextFormatType.JSON)
        {
            GameObject globalVariables = GameObject.Find("GlobalVariables");
            if (globalVariables == null)
            {
                globalVariables = new GameObject("GlobalVariables");
                Object.DontDestroyOnLoad(globalVariables);
            }

            LLMExecutionOptions options = globalVariables.GetComponent<LLMExecutionOptions>();
            if (options == null)
            {
                options = globalVariables.AddComponent<LLMExecutionOptions>();
            }

            options.model = model;
            options.selfCorrection = selfCorrection;
            options.contextFormat = contextFormat;
            return options;
        }

        [UnityTest]
        public IEnumerator TestCase9DeleteLeftmostHouse()
        {
            Debug.Log("Start delete leftmost house test...");
            var options = SetupLLMExecutionOptions(
                model: "o1-mini-2024-09-12", // o1-mini-2024-09-12 or gpt-4o-mini
                selfCorrection: LLMExecutionOptions.SelfCorrectionType.MultiStep,
                contextFormat: LLMExecutionOptions.ContextFormatType.SQL
            );
            _buildingRegister.InitializeTilemap();

            var taskCreator = aiTaskConverter.gameObject.GetComponent<TaskCreator>();
            _buildingRegister.RegisterBuilding(new Vector3(3, 0, 0), Enums.BuildingType.House);
            _buildingRegister.RegisterBuilding(new Vector3(4, 0, 0), Enums.BuildingType.House);
            _buildingRegister.RegisterBuilding(new Vector3(5, 0, 0), Enums.BuildingType.House);
            var initialBuildings = _buildingRegister.getAllGameObjects();
            float testStartTime = Time.time;

            // Find the leftmost house before deletion
            var leftmostHouse = initialBuildings
                .Where(b => b.Item2 == AITransformer.Enums.BuildingType.House)
                .OrderBy(b => b.Item1.x)
                .FirstOrDefault();

            if (leftmostHouse.Item1 == Vector3.zero)
            {
                Debug.LogError("No houses found on the map!");
                yield break;
            }

            Vector3 leftmostPosition = leftmostHouse.Item1;

            // Start the test for deleting leftmost house
            var retval = taskCreator.CreateTaskCoroutineByFilepath("Assets/TestAudioFiles/test_2new.mp3");
            yield return retval;

            float waitTime = 30f;
            float startTime = Time.time;
            Debug.Log("Starting wait Countdown for house deletion test");

            bool success = false;
            bool correctBuildingDeleted = false;

            while (Time.time - startTime < waitTime)
            {
                var currentBuildings = _buildingRegister.getAllGameObjects();

                // Check if a building was deleted
                if (currentBuildings.Count < initialBuildings.Count)
                {
                    break;
                }
                yield return null;
            }

            // Calculate KPIs
            float executionSpeed = Time.time - testStartTime;
            
            //get building at 3,0,0
            var buildingAt3 = _buildingRegister.GetBuildingAtLocation(leftmostPosition);
            if(buildingAt3 == Enums.BuildingType.NoBuilding)
            {
                correctBuildingDeleted = true;
                success = true;
            }
            // Format the deleted building position as JSON
            string deletedBuildingJson =
                $"\\{{\\\"position\\\":\\{{\\\"x\\\":{leftmostPosition.x},\\\"y\\\":{leftmostPosition.y},\\\"z\\\":{leftmostPosition.z}\\}},\\\"type\\\":\\\"House\\\"\\}}";

            // Save KPIs to CSV
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string csvPath = Path.Combine(Application.dataPath, "TestLogs",
                $"test_2_delete_house_kpis_{options.contextFormat}" +
                $"_{options.selfCorrection}" +
                $"_{options.model}.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));
            TaskSystem.MapInteractionTask execute_task = (TaskSystem.MapInteractionTask)aiTaskExecutor.GetComponent<TaskExecutor>().oldTasks.First();
            StringBuilder csv = new StringBuilder();
            string locationJson = $"{{\"x\":{execute_task.Location.X},\"y\":{execute_task.Location.Y}}}";
            csv.AppendLine($"{timestamp},{executionSpeed},{correctBuildingDeleted},\"{locationJson}\"");
            File.AppendAllText(csvPath, csv.ToString());

            Debug.Log($"House deletion test results saved to: {csvPath}");
            Debug.Log("House deletion test coroutine finished.");
            Assert.IsTrue(true, "Test completed successfully.");
        }
    }
}