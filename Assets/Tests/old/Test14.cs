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
using Assert = UnityEngine.Assertions.Assert;
using Object = UnityEngine.Object;

namespace Tests
{
    public class Test14
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

        private LLMExecutionOptions SetupLLMExecutionOptionsStore(
            string model = "gpt-4o-mini",
            LLMExecutionOptions.SelfCorrectionType selfCorrection = LLMExecutionOptions.SelfCorrectionType.MultiStep,
            bool useFewShotPrompting = true
            )
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
            options.useFewShotPrompting = useFewShotPrompting;
            return options;
        }

        [UnityTest]
        public IEnumerator TestCase14SellAllResourcesNeedNotForHousesAndBuyNewOnes()
        {
            Debug.Log("Start wood purchase test...");
            var options = SetupLLMExecutionOptionsStore(
                model: "o1-mini-2024-09-12",
                selfCorrection: LLMExecutionOptions.SelfCorrectionType.SingleStep,
                useFewShotPrompting: true
            );

            var taskCreator = aiTaskConverter.gameObject.GetComponent<TaskCreator>();

            // Record initial state
            resourceManager.SetResources(new ResourceStore.Resources(
                1000, 1000, 1000, 1000, 1000, 1000));
            var initialResources = resourceManager.GetCurrentResources();
            int initialMoney = resourceManager.GetCurrentResources().Money;
           
            float testStartTime = Time.time;

            // Start the test for buying wood
            var retval = taskCreator.CreateTaskCoroutineByFilepath("Assets/TestAudioFiles/test_14.mp3");
            yield return retval;

            float waitTime = 30f;
            float startTime = Time.time;
            Debug.Log("Starting wait Countdown for wood purchase test");

            bool success = false;
            ResourceStore.Resources finalResources = new ResourceStore.Resources();
            int finalMoney = 0;

            while (Time.time - startTime < waitTime)
            {
                var currentResources = resourceManager.GetCurrentResources();
                int currentMoney = currentResources.Money;

                if (
                    currentResources.Iron == 0 && currentResources.Food == 0 && currentResources.Salt == 0
                    && currentResources.Stone > 1000 && currentResources.Wood > 1000)
                {
                    finalResources = currentResources;
                    finalMoney = currentMoney;
                    success = true;
                    break;
                }

                yield return null;
            }

            // Calculate KPIs
            float executionSpeed = Time.time - testStartTime;
            float moneyGained = Math.Abs(initialMoney - finalMoney);

            // Format resources as JSON
            string resourcesJson =
                $"\\{{\\\"wood\\\":{finalResources.Wood},\\\"stone\\\":{finalResources.Stone},\\\"iron\\\":{finalResources.Iron}\\" +
                $"\\\"food\\\":{finalResources.Food},\\\"salt\\\":{finalResources.Salt}\\}}";

            // Format money as JSON
            string moneyJson =
                $"\\{{\\\"initial\\\":{initialMoney},\\\"final\\\":{finalMoney},\\\"gained\\\":{moneyGained}\\}}";

            // Save KPIs to CSV
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string csvPath = Path.Combine(Application.dataPath, "TestLogs",
                $"bought_resources_and_everything_not_for_houses_sold_kpis_{options.contextFormat}" +
                $"_{options.selfCorrection}" +
                $"_{options.model}.csv");

            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

            StringBuilder csv = new StringBuilder();
            csv.AppendLine($"{timestamp},{executionSpeed},{success},\"{resourcesJson}\",\"{moneyJson}\"");
            File.AppendAllText(csvPath, csv.ToString());

            Debug.Log($"Wood purchase test results saved to: {csvPath}");
            Debug.Log("Wood purchase test coroutine finished.");
            Assert.IsTrue(success, "Test completed successfully.");
        }
    }
}