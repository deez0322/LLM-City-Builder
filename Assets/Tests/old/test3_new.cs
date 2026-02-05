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
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Tests
{
    public class Test3New
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
            public bool UseFewShot { get; set; }
            public string Description { get; set; }
            public string GetConfigIdentifier() =>
                $"{Model}_{SelfCorrection}_{(UseFewShot ? "fewshot" : "nofewshot")}";
        }

        private static readonly TestConfiguration[] TEST_CONFIGURATIONS = new[]
        {
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                UseFewShot = false,
                Description = "No CoT (with JSON, no fewshot)"
            },
            new TestConfiguration
            {
                Model = "o1-mini-2024-09-12",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                UseFewShot = false,
                Description = "With CoT"
            },
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                UseFewShot = false,
                Description = "No Fewshot"
            },
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                UseFewShot = true,
                Description = "With Fewshot"
            },
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                UseFewShot = false,
                Description = "No Self-Correction (with JSON, 4o)"
            },
            new TestConfiguration
            {
                Model = "gpt-4o-mini",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.SingleStep,
                UseFewShot = false,
                Description = "Singlestep SelfCorrection"
            },
            new TestConfiguration
            {
                Model = "o1-mini-2024-09-12",
                SelfCorrection = LLMExecutionOptions.SelfCorrectionType.MultiStep,
                UseFewShot = true,
                Description = "Combined Solution (SQL, o1, multi-step)"
            },
            new TestConfiguration
            {
            Model = "gpt-4o-mini",
            SelfCorrection = LLMExecutionOptions.SelfCorrectionType.MultiStep,
            UseFewShot = false,
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

                var lineCount = File.ReadAllLines(csvPath).Length - 1; // Subtract 1 for header
                if (lineCount < REQUIRED_RUNS)
                {
                    Debug.Log($"Continuing configuration: {config.Description} (Run {lineCount + 1}/{REQUIRED_RUNS})");
                    return config;
                }
            }

            return null; // All configurations are complete
        }

        private string GetCsvPathForConfiguration(TestConfiguration config)
        {
            return Path.Combine(Application.dataPath, "TestLogs",
                $"test_3_wood_purchase_kpis_{config.GetConfigIdentifier()}.csv");
        }

        [UnityTest]
        public IEnumerator TestCase10BuyWoodFromStore()
        {
            var configuration = GetNextIncompleteConfiguration();
            
            if (configuration == null)
            {
                Debug.Log("All configurations have completed 30 runs!");
                Assert.IsFalse(true,"Testing complete for all configurations");
                yield break;
            }

            Debug.Log($"Running test with configuration: {configuration.Description}");
            
            var options = SetupLLMExecutionOptionsStore(
                model: configuration.Model,
                selfCorrection: configuration.SelfCorrection,
                useFewShotPrompting: configuration.UseFewShot
            );

            var taskCreator = aiTaskConverter.gameObject.GetComponent<TaskCreator>();

            // Record initial state
            var initialResources = resourceManager.GetCurrentResources();
            int initialMoney = resourceManager.GetCurrentResources().Money;
            float testStartTime = Time.time;

            // Start the test for buying wood
            var retval = taskCreator.CreateTaskCoroutineByFilepath("Assets/TestAudioFiles/test_10.mp3");
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

                // Check if wood amount increased by 100
                if (currentResources.Wood == initialResources.Wood + 100)
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
            float moneySpent = initialMoney - finalMoney;

            // Format resources as JSON
            string resourcesJson =
                $"\\{{\\\"wood\\\":{finalResources.Wood},\\\"stone\\\":{finalResources.Stone},\\\"iron\\\":{finalResources.Iron}\\}}";

            // Format money as JSON
            string moneyJson =
                $"\\{{\\\"initial\\\":{initialMoney},\\\"final\\\":{finalMoney},\\\"spent\\\":{moneySpent}\\}}";

            // Save KPIs to CSV
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string csvPath = GetCsvPathForConfiguration(configuration);

            Directory.CreateDirectory(Path.GetDirectoryName(csvPath));

            StringBuilder csv = new StringBuilder();
            csv.AppendLine($"{timestamp},{executionSpeed},\"{resourcesJson}\",\"{moneyJson}\"");
            File.AppendAllText(csvPath, csv.ToString());

            Debug.Log($"Wood purchase test results saved to: {csvPath}");
            Debug.Log("Wood purchase test coroutine finished.");
            Assert.IsTrue(success, "Test completed successfully.");
        }
    }
}