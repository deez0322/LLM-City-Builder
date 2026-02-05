using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using AITransformer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests
{
    public class Test1
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
                Description = "Multi Step SelfCorrection (with JSON, o1)"
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
                $"test_1_house_placement_kpis_{config.GetConfigIdentifier()}.csv");
        }

        [UnityTest]
        public IEnumerator TestCase1PlaceHouseAnywhere()
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
            _buildingRegister.InitializeTilemap();

            float testStartTime = Time.time;
            int initialCount = _buildingRegister.getAllGameObjects().Count;
            
            var taskCreator = aiTaskConverter.gameObject.GetComponent<TaskCreator>();
            var retval = taskCreator.CreateTaskCoroutineByFilepath("Assets/TestAudioFiles/test_1.mp3");
            yield return retval;

            float waitTime = 30f;
            float startTime = Time.time;
            Debug.Log("Starting wait Countdown");
            Vector3 housePosition = Vector3.zero;
            AITransformer.Enums.BuildingType buildingType = default;
            bool buildingPlaced = false;

            while (Time.time - startTime < waitTime)
            {
                int currentCount = _buildingRegister.getAllGameObjects().Count;
                if (currentCount > initialCount)
                {
                    buildingPlaced = true;
                    var buildings = _buildingRegister.getAllGameObjects();
                    var lastBuilding = buildings[buildings.Count - 1];
                    housePosition = lastBuilding.Item1;
                    buildingType = lastBuilding.Item2;
                    break;
                }
                yield return null;
            }

            float executionSpeed = Time.time - testStartTime;
            bool correctlyPlaced = ValidateHousePlacement(housePosition, buildingType);
            string coordinates = $"{housePosition.x},{housePosition.y},{housePosition.z}";

            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string csvPath = GetCsvPathForConfiguration(configuration);

            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

            StringBuilder csv = new StringBuilder();
            if (!File.Exists(csvPath))
            {
                csv.AppendLine("Timestamp,ExecutionSpeed,CorrectlyPlaced,Coordinates,BuildingType");
            }
            csv.AppendLine($"{timestamp},{executionSpeed},{correctlyPlaced},{coordinates},{buildingType}");
            File.AppendAllText(csvPath, csv.ToString());

            Debug.Log($"Test results saved to: {csvPath}");
            Debug.Log("Test coroutine finished.");
            Assert.IsTrue(true);
        }

        private bool ValidateHousePlacement(Vector3 position, AITransformer.Enums.BuildingType buildingType)
        {
            if (position == Vector3.zero) return false;
            
            var tile = _buildingRegister.GetTileAtLocation(position);
            if (tile == "ironTile" || tile == "riverTile" || tile == "treeTile")
                return false;

            if (buildingType != AITransformer.Enums.BuildingType.House)
                return false;

            return true;
        }
    }
}