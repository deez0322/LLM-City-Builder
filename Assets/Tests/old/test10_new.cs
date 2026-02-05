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
    public class Test10new
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
                Description = "Two Step SelfCorrection (with JSON, o1)"
            },
            new TestConfiguration
            {
                Model = "o1-mini-2024-09-12",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.MultiStep,
                ContextFormat = LLMExecutionOptions.ContextFormatType.SQL,
                Description = "Combined Solution (SQL, o1, multi-step)"
            }
        };

        private const int REQUIRED_RUNS = 30;

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
            options.useFewShotPrompting = false;  // No fewshot for map interaction tasks
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
                $"test_10_two_houses_placement_kpis_{config.GetConfigIdentifier()}.csv");
        }

        [UnityTest]
        public IEnumerator TestCase10PlaceTwoHousesEastRiver()
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
            var retval = taskCreator.CreateTaskCoroutineByFilepath("Assets/TestAudioFiles/test10_new.mp3");
            yield return retval;

            float waitTime = 45f; // Increased wait time for two buildings
            float startTime = Time.time;
            Debug.Log("Starting wait Countdown for two houses placement test");
            List<(Vector3, AITransformer.Enums.BuildingType)> placedBuildings = new List<(Vector3, AITransformer.Enums.BuildingType)>();

            while (Time.time - startTime < waitTime)
            {
                int currentCount = _buildingRegister.getAllGameObjects().Count;
                if (currentCount > initialCount + 1)
                {
                    var buildings = _buildingRegister.getAllGameObjects();
                    placedBuildings = buildings.Skip(currentCount - 2)
                                    .Select(b => (b.Item1, b.Item2))
                                    .ToList();
                    break;
                }
                yield return null;
            }

            int finalCount = _buildingRegister.getAllGameObjects().Count;
            //Assert.IsTrue(finalCount > initialCount + 1, "Two new buildings were not added to the scene.");

            float executionSpeed = Time.time - testStartTime;
            int buildingsPlaced = placedBuildings.Count;
            bool correctlyPlaced = ValidateHousePlacements(placedBuildings);
            string coordinates = string.Join(";", placedBuildings.Select(b => 
                $"({b.Item1.x},{b.Item1.y},{b.Item1.z})"));

            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string csvPath = GetCsvPathForConfiguration(configuration);

            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

            StringBuilder csv = new StringBuilder();
            csv.AppendLine($"{timestamp},{executionSpeed},{correctlyPlaced},{buildingsPlaced},{coordinates}");
            File.AppendAllText(csvPath, csv.ToString());

            Debug.Log($"Two houses placement test results saved to: {csvPath}");
            Debug.Log("Two houses placement test coroutine finished.");
            Assert.IsTrue(true, "Houses were not correctly placed east of the river.");
        }

        private bool ValidateHousePlacements(List<(Vector3, AITransformer.Enums.BuildingType)> buildings)
        {
            if (buildings.Count != 2) return false;

            foreach (var (position, buildingType) in buildings)
            {
                if (buildingType != AITransformer.Enums.BuildingType.House)
                    return false;

                var tile = _buildingRegister.GetTileAtLocation(position);
                if (tile == "ironTile" || tile == "riverTile" || tile == "treeTile")
                    return false;

                if (position.x <= 5)
                    return false;

                var otherBuilding = buildings.FirstOrDefault(b => b.Item1 != position);
                if (otherBuilding.Item1 == position)
                    return false;
            }

            return true;
        }
    }
}