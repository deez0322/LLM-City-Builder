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
    public class Test12
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
                $"test_12_city_center_kpis_{config.GetConfigIdentifier()}.csv");
        }

        [UnityTest]
        public IEnumerator TestCase12BuildCityCenter()
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
            float testStartTime = Time.time;
            int initialCount = initialBuildings.Count;

            var retval = taskCreator.CreateTaskCoroutineByFilepath("Assets/TestAudioFiles/test12_new.mp3");
            yield return retval;

            float waitTime = 45f;
            float startTime = Time.time;
            Debug.Log("Starting wait Countdown for city center building test");
            
            bool success = false;
            List<Tuple<Vector3, AITransformer.Enums.BuildingType>> finalBuildings = new List<Tuple<Vector3, AITransformer.Enums.BuildingType>>();
            Vector3 centerPosition = Vector3.zero;
            int occupiedTilesCount = 0;
            var tasksLeft = 999;
            var taskExecutor = aiTaskExecutor.GetComponent<TaskExecutor>();
            while (Time.time - startTime < waitTime)
            {
                tasksLeft = taskExecutor._tasks.Count();
                var currentBuildings = _buildingRegister.getAllGameObjects();
                var newBuildings = currentBuildings.Skip(initialCount).ToList();
                if (tasksLeft == 0)
                {
                    if (newBuildings.Count >= 2)
                    {
                        Debug.Log("Tasks left: " + tasksLeft);
                        centerPosition = CalculateCenterPosition(newBuildings);
                        occupiedTilesCount = CountOccupiedTilesInRadius(centerPosition, newBuildings);

                        finalBuildings = currentBuildings;
                        success = true;
                        break;
                    }
                    else
                    {
                        break;
                    }
                }
                
                yield return null;
            }
            
            finalBuildings = _buildingRegister.getAllGameObjects();

            float executionSpeed = Time.time - testStartTime;
            int buildingsPlaced = finalBuildings.Count - initialCount;
            bool correctlyPlaced = success;
            
            string houses = string.Join(";", finalBuildings
                .Skip(initialCount)
                .Select(b => $"\\{{\\\"position\\\":\\{{\\\"x\\\":{b.Item1.x},\\\"y\\\":{b.Item1.y},\\\"z\\\":{b.Item1.z}\\}},\\\"type\\\":\\\"{b.Item2}\\\"\\}}"));

            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string csvPath = GetCsvPathForConfiguration(configuration);

            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

            StringBuilder csv = new StringBuilder();
            csv.AppendLine($"{timestamp},{executionSpeed},{success},{buildingsPlaced}," +
                          $"\"{houses}\",{occupiedTilesCount}");
            File.AppendAllText(csvPath, csv.ToString());

            Debug.Log($"City center building test results saved to: {csvPath}");
            Debug.Log("City center building test coroutine finished.");
            Assert.IsTrue(true, "City center was not correctly built.");
        }

        private Vector3 CalculateCenterPosition(List<Tuple<Vector3, AITransformer.Enums.BuildingType>> buildings)
        {
            if (!buildings.Any()) return Vector3.zero;

            float sumX = buildings.Sum(b => b.Item1.x);
            float sumY = buildings.Sum(b => b.Item1.y);
            float count = buildings.Count;

            return new Vector3(sumX / count, sumY / count, 0);
        }

        private int CountOccupiedTilesInRadius(Vector3 center, List<Tuple<Vector3, AITransformer.Enums.BuildingType>> buildings)
        {
            HashSet<Vector3Int> occupiedTiles = new HashSet<Vector3Int>();
            int radius = 1;
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector3Int checkPos = new Vector3Int(
                        Mathf.RoundToInt(center.x + x),
                        Mathf.RoundToInt(center.y + y),
                        0
                    );

                    foreach (var building in buildings)
                    {
                        Vector3Int buildingPos = new Vector3Int(
                            Mathf.RoundToInt(building.Item1.x),
                            Mathf.RoundToInt(building.Item1.y),
                            0
                        );

                        if (buildingPos == checkPos)
                        {
                            occupiedTiles.Add(checkPos);
                            break;
                        }
                    }
                }
            }
            return occupiedTiles.Count;
        }

        private bool ValidateCityCenterPlacement(List<Tuple<Vector3, AITransformer.Enums.BuildingType>> buildings, Vector3 center)
        {
            if (buildings.Count < 3) return false;

            foreach (var building in buildings)
            {
                var tile = _buildingRegister.GetTileAtLocation(building.Item1);
                if (tile == "ironTile" || tile == "riverTile" || tile == "treeTile")
                    return false;
            }

            foreach (var building in buildings)
            {
                float distanceToCenter = Vector3.Distance(building.Item1, center);
                if (distanceToCenter > 2)
                    return false;
            }

            for (int i = 0; i < buildings.Count; i++)
            {
                for (int j = i + 1; j < buildings.Count; j++)
                {
                    if (Vector3.Distance(buildings[i].Item1, buildings[j].Item1) < 0.1f)
                        return false;
                }
            }
            return true;
        }
    }
}