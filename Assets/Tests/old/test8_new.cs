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
    public class Test8new
    {
        private GameObject _tilemap;
        private GameObject aiTaskConverter;
        private GameObject aiTaskExecutor;
        private BuildingRegister _buildingRegister;
        private ResourceStore resourceManager;

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
            resourceManager = aiTaskExecutor.GetComponent<TaskExecutor>().Store;
            GameObject buildingRegisterObject = GameObject.Find("BuildingRegister");
            if (buildingRegisterObject != null)
            {
                _buildingRegister = buildingRegisterObject.GetComponent<BuildingRegister>();
            }
            _buildingRegister.RegisterBuilding(new Vector3(3, 0, 0), Enums.BuildingType.House);
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
                $"test_8_fishing_huts_kpis_{config.GetConfigIdentifier()}.csv");
        }

        [UnityTest]
        public IEnumerator TestCase8BuildMaxFishingHuts()
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

            var taskCreator = aiTaskConverter.gameObject.GetComponent<TaskCreator>();
            var initialBuildings = _buildingRegister.getAllGameObjects();
            var initialResources = resourceManager.GetCurrentResources();
            var maxNrOfFishingHuts = Math.Max(initialResources.Wood / Enums.BuildingCosts[Enums.BuildingType.FishingHut]["Wood"], 
                initialResources.Stone / Enums.BuildingCosts[Enums.BuildingType.FishingHut]["Stone"]);
            float testStartTime = Time.time;
            int initialCount = initialBuildings.Count;
            var retval = taskCreator.CreateTaskCoroutineByFilepath("Assets/TestAudioFiles/test_8.mp3");
            yield return retval;

            float waitTime = 60f;
            float startTime = Time.time;
            Debug.Log("Starting wait Countdown for fishing huts building test");
            bool success = false;
            List<Tuple<Vector3, AITransformer.Enums.BuildingType>> finalBuildings = new List<Tuple<Vector3, AITransformer.Enums.BuildingType>>();
            ResourceStore.Resources unusedResources = new ResourceStore.Resources();

            while (Time.time - startTime < waitTime)
            {
                var currentBuildings = _buildingRegister.getAllGameObjects();
                var currentResources = resourceManager.GetCurrentResources();
                var fishingHuts = currentBuildings.Where(b => b.Item2 == AITransformer.Enums.BuildingType.FishingHut).ToList();
                
                if (fishingHuts.Count == maxNrOfFishingHuts && ValidateFishingHutsPlacement(fishingHuts))
                {
                    finalBuildings = currentBuildings;
                    unusedResources = currentResources;
                    success = true;
                    break;
                }
                yield return null;
            }

            float executionSpeed = Time.time - testStartTime;
            int buildingsPlaced = finalBuildings.Count - initialCount;
            bool correctlyPlaced = buildingsPlaced == maxNrOfFishingHuts;
            unusedResources = new ResourceStore.Resources();

            string buildingsJson = string.Join(";", finalBuildings
                .Skip(initialCount)
                .Select(b => $"\\{{\\\"position\\\":\\{{\\\"x\\\":{b.Item1.x},\\\"y\\\":{b.Item1.y},\\\"z\\\":{b.Item1.z}\\}},\\\"type\\\":\\\"{b.Item2}\\\"\\}}"));
            string resourcesJson = $"\\{{\\\"wood\\\":{unusedResources.Wood},\\\"stone\\\":{unusedResources.Stone},\\\"iron\\\":{unusedResources.Iron}\\}}";
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string csvPath = GetCsvPathForConfiguration(configuration);

            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

            StringBuilder csv = new StringBuilder();
            csv.AppendLine($"{timestamp},{executionSpeed},{correctlyPlaced},{buildingsPlaced}," +
                          $"\"{buildingsJson}\",\"{resourcesJson}\"");
            File.AppendAllText(csvPath, csv.ToString());

            Debug.Log($"Fishing huts building test results saved to: {csvPath}");
            Debug.Log("Fishing huts building test coroutine finished.");
            Assert.IsTrue(true, "Test completed successfully.");
        }

        private bool ValidateFishingHutsPlacement(List<Tuple<Vector3, AITransformer.Enums.BuildingType>> fishingHuts)
        {
            if (!fishingHuts.Any()) return false;

            foreach (var building in fishingHuts)
            {
                if (building.Item2 != AITransformer.Enums.BuildingType.FishingHut)
                    return false;

                var position = building.Item1;
                
                var tile = _buildingRegister.GetTileAtLocation(position);
                if (tile == "ironTile" || tile == "riverTile" || tile == "treeTile")
                    return false;

                if (fishingHuts.Count(h => Vector3.Distance(h.Item1, position) < 0.1f) > 1)
                    return false;
            }
            return true;
        }
    }
}