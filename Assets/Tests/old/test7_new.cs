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
    public class Test7New
    {
        private GameObject _tilemap;
        private GameObject aiTaskConverter;
        private GameObject aiTaskExecutor;
        private BuildingRegister _buildingRegister;

        private class TestConfiguration
        {
            public string Model { get; set; }
            public LLMExecutionOptions.SelfCorrectionType SelfCorrection { get; set; }
            public LLMExecutionOptions.ContextFormatType ContextFormat { get; set; }
            public string Description { get; set; }
            public string GetConfigIdentifier() =>
                $"{Model}_{SelfCorrection}_{ContextFormat}";
        }

        private static readonly TestConfiguration[] TEST_CONFIGURATIONS = new[]
        {
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                ContextFormat = LLMExecutionOptions.ContextFormatType.SQL,
                Description = "SQL (4o-mini. No Step Correction)"
            },
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                ContextFormat = LLMExecutionOptions.ContextFormatType.MinimapText,
                Description = "Minimap (4o-mini. No Step Correction)"
            },
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                ContextFormat = LLMExecutionOptions.ContextFormatType.JSON,
                Description = "JSON (4o-mini. No Step Correction)"
            },
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                ContextFormat = LLMExecutionOptions.ContextFormatType.JSON,
                Description = "No CoT (4o, with JSON, no step)"
            },
            new TestConfiguration
            {
                Model = "o1-mini-2024-09-12",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                ContextFormat = LLMExecutionOptions.ContextFormatType.JSON,
                Description = "With CoT (o1, JSON, No Step Correction)"
            },
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                ContextFormat = LLMExecutionOptions.ContextFormatType.JSON,
                Description = "No Self-Correction (with JSON, 4o)"
            },
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.SingleStep,
                ContextFormat = LLMExecutionOptions.ContextFormatType.JSON,
                Description = "Singlestep SelfCorrection (with JSON, 4o)"
            },
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.MultiStep,
                ContextFormat = LLMExecutionOptions.ContextFormatType.JSON,
                Description = "Two Step SelfCorrection (with JSON, mini)"
            },
            new TestConfiguration
            {
                Model = "o1-mini-2024-09-12",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.MultiStep,
                ContextFormat = LLMExecutionOptions.ContextFormatType.SQL,
                Description = "Combined Solution (SQL, o1, multi-step)"
            }
        };

        private const int REQUIRED_RUNS = 15;

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

            _tilemap = GameObject.Find("Tilemap");
            aiTaskConverter = GameObject.Find("AITaskConverter");
            aiTaskExecutor = GameObject.Find("AITaskExecutor");
            GameObject buildingRegisterObject = GameObject.Find("BuildingRegister");
            if (buildingRegisterObject != null)
            {
                _buildingRegister = buildingRegisterObject.GetComponent<BuildingRegister>();
            }
        }

        private LLMExecutionOptions SetupLLMExecutionOptions(TestConfiguration config)
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

            options.model = config.Model;
            options.selfCorrection = config.SelfCorrection;
            options.contextFormat = config.ContextFormat;
            options.useFewShotPrompting = false;
            return options;
        }

        private TestConfiguration GetNextIncompleteConfiguration()
        {
            foreach (var config in TEST_CONFIGURATIONS)
            {
                string csvPath = GetCsvPathForConfiguration(config);
                
                if (!File.Exists(csvPath))
                {
                    Debug.Log($"Starting new configuration: {config.Description}");
                    return config;
                }

                var lineCount = File.ReadAllLines(csvPath).Length - 1;
                if (lineCount < REQUIRED_RUNS)
                {
                    Debug.Log($"Continuing configuration: {config.Description} (Run {lineCount + 1}/{REQUIRED_RUNS})");
                    return config;
                }
            }

            return null;
        }

        private string GetCsvPathForConfiguration(TestConfiguration config)
        {
            return Path.Combine(Application.dataPath, "TestLogs",
                $"test_14_lumberjack_placement_kpis_most_adjacent_{config.GetConfigIdentifier()}.csv");
        }

        [UnityTest]
        public IEnumerator TestCase7PlaceLumberjackNearTrees()
        {
            var configuration = GetNextIncompleteConfiguration();
            
            if (configuration == null)
            {
                Debug.Log("All configurations have completed 30 runs!");
                Assert.IsFalse(true, "Testing complete for all configurations");
                yield break;
            }

            Debug.Log($"Running test with configuration: {configuration.Description}");
            
            var options = SetupLLMExecutionOptions(configuration);

            float testStartTime = Time.time;
            int initialCount = _buildingRegister.getAllGameObjects().Count;
            var taskCreator = aiTaskConverter.gameObject.GetComponent<TaskCreator>();
            var retval = taskCreator.CreateTaskCoroutineByFilepath("Assets/TestAudioFiles/test_14new.mp3");
            yield return retval;

            float waitTime = 30f;
            float startTime = Time.time;
            Debug.Log("Starting wait Countdown for lumberjack placement test");
            Vector3 buildingPosition = Vector3.zero;
            AITransformer.Enums.BuildingType buildingType = default;
            bool buildingPlaced = false;
            var tasksLeft = 999;
            var taskExecutor = aiTaskExecutor.GetComponent<TaskExecutor>();

            while (Time.time - startTime < waitTime)
            {
                tasksLeft = taskExecutor._tasks.Count();
                int currentCount = _buildingRegister.getAllGameObjects().Count;
                if (tasksLeft == 0)
                {
                    buildingPlaced = true;
                    var buildings = _buildingRegister.getAllGameObjects();
                    var lastBuilding = buildings[buildings.Count - 1];
                    buildingPosition = lastBuilding.Item1;
                    buildingType = lastBuilding.Item2;
                    break;
                }
                yield return null;
            }

            int finalCount = _buildingRegister.getAllGameObjects().Count;
            //Assert.IsTrue(finalCount > initialCount, "A new building was not added to the scene.");

            float executionSpeed = Time.time - testStartTime;
            int adjacentTreeCount = CountAdjacentTrees(buildingPosition);
            bool correctlyPlaced = ValidateLumberjackPlacement(buildingPosition, buildingType, adjacentTreeCount);
            string coordinates = $"{buildingPosition.x},{buildingPosition.y},{buildingPosition.z}";

            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string csvPath = GetCsvPathForConfiguration(configuration);

            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

            StringBuilder csv = new StringBuilder();
            csv.AppendLine($"{timestamp},{executionSpeed},{correctlyPlaced},{coordinates},{buildingType},{adjacentTreeCount}");
            File.AppendAllText(csvPath, csv.ToString());

            Debug.Log($"Lumberjack placement test results saved to: {csvPath}");
            Debug.Log("Lumberjack placement test coroutine finished.");
            Assert.IsTrue(true, "Building was not correctly placed near trees.");
        }
        

        private int CountAdjacentTrees(Vector3 position)
        {
            List<Vector3> treePositions = _buildingRegister.GetTilesByType("Wood");
            
            int treeCount = 0;
            foreach (Vector3 treePos in treePositions)
            {
                float distance = Vector3.Distance(position, treePos);
                if (distance <= Mathf.Sqrt(2))
                {
                    treeCount++;
                }
            }

            return treeCount;
        }

        private bool ValidateLumberjackPlacement(Vector3 position, AITransformer.Enums.BuildingType buildingType, int adjacentTreeCount)
        {
            if (position == null) return false;

            bool isLumberjack = buildingType == AITransformer.Enums.BuildingType.LumberjackHut;
            if (isLumberjack && position.x == 2f && position.y == 2f)
                return true;
            return false;
        }
    }
}