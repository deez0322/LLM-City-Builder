using System;
using System.Collections;
using System.Linq;
using AITransformer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;
using Object = UnityEngine.Object;

namespace Tests
{
    public class LoadSceneTest
    {
        private GameObject tilemap;
        private GameObject aiTaskConverter;
        private GameObject aiTaskExecutor;
        private BuildingRegister _buildingRegister;


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
            tilemap = GameObject.Find("Tilemap");
            aiTaskConverter = GameObject.Find("AITaskConverter");
            aiTaskExecutor = GameObject.Find("AITaskExecutor");
            GameObject buildingRegisterObject = GameObject.Find("BuildingRegister");
            if (buildingRegisterObject != null)
            {
                _buildingRegister = buildingRegisterObject.GetComponent<BuildingRegister>();
            }
        }

        [UnityTest]
        public IEnumerator TestBuildSingleBuilding()
        {
            Debug.Log("Start test ...");
            int initialCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
            var taskCreator = aiTaskConverter.gameObject.GetComponent<TaskCreator>();
            var retval =
                taskCreator.CreateTaskCoroutineByFilepath("Assets/TestAudioFiles/test_1_build_a_single_building.mp3");
            yield return retval;
            float waitTime = 30f;
            float startTime = Time.time;
            Debug.Log("Starting wait Countdown");
            while (Time.time - startTime < waitTime)
            {
                int currentCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                if (currentCount > initialCount)
                {
                    break;
                }

                yield return null;
            }

            int finalCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
            Assert.IsTrue(finalCount > initialCount, "A new building was not added to the scene.");
            Debug.Log("Test coroutine finished.");
        }

        [UnityTest]
        public IEnumerator TestDeleteSingleBuilding()
        {
            Debug.Log("Start test ...");
            GameObject prefab = Resources.Load<GameObject>("FishingHutPrefab");
            if (prefab != null)
            {
                GameObject newObject = Object.Instantiate(prefab);
                _buildingRegister.RegisterBuilding(newObject.transform.position, Enums.BuildingType.FishingHut);
                newObject.name = "NewFishingHut";
            }
            else
            {
                Debug.LogError("Prefab 'FishingHutPrefab' not found in Resources folder");
            }

            var initialCount = Object.FindObjectsOfType<GameObject>().Length;
            var taskCreator = aiTaskConverter.gameObject.GetComponent<TaskCreator>();
            var retval =
                taskCreator.CreateTaskCoroutineByFilepath(
                    "Assets/TestAudioFiles/test_2_delete_a_specific_building.mp3");
            yield return retval;
            float waitTime = 30f;
            float startTime = Time.time;
            Debug.Log("Starting wait Countdown");
            while (Time.time - startTime < waitTime)
            {
                int currentCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
                if (currentCount < initialCount)
                {
                    break;
                }

                yield return null;
            }

            int finalCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length;
            Assert.IsTrue(finalCount < initialCount, "A building was removed from the scene.");
            Debug.Log("Test coroutine finished.");
        }

        [UnityTest]
        public IEnumerator TestMoveSingleBuilding()
        {
            Debug.Log("Start test ...");
            GameObject prefab = Resources.Load<GameObject>("FishingHutPrefab");
            Vector3 initialCoordinates = new Vector3(12, 6, 0);
            if (prefab != null)
            {
                GameObject newObject = Object.Instantiate(prefab);
                newObject.transform.position = initialCoordinates;
                _buildingRegister.RegisterBuilding(newObject.transform.position, Enums.BuildingType.FishingHut);
                newObject.name = "NewFishingHut";
                initialCoordinates = newObject.transform.position;
            }
            else
            {
                Debug.LogError("Prefab 'FishingHutPrefab' not found in Resources folder");
                yield break;
            }

            var taskCreator = aiTaskConverter.GetComponent<TaskCreator>();
            var retval =
                taskCreator.CreateTaskCoroutineByFilepath("Assets/TestAudioFiles/test_3_move_a_specific_building.mp3");
            yield return retval;

            float waitTime = 3f;
            float startTime = Time.time;
            Debug.Log("Starting wait Countdown");

            Tuple<Vector3, Enums.BuildingType> movedBuilding = null;
            while (Time.time - startTime < waitTime)
            {
                movedBuilding = _buildingRegister.getAllGameObjects().First();
                if (movedBuilding != null && movedBuilding.Item1 != initialCoordinates)
                {
                    break;
                }

                yield return null;
            }

            Assert.IsNotNull(movedBuilding, "The building was not found after moving.");
            Assert.AreNotEqual(initialCoordinates, movedBuilding.Item1, "The building did not move.");
            Debug.Log("Test coroutine finished.");
        }

        [UnityTest]
        public IEnumerator TestBuyWoodFromStore()
        {
            Debug.Log("Starting test: Buy 100 units of wood from the store");
            ResourceStore resourceManager = aiTaskExecutor.GetComponent<TaskExecutor>().Store;
            Assert.IsNotNull(resourceManager, "ResourceManager component not found on ResourceManager object.");

            // Get initial wood count
            int initialWoodCount = resourceManager.GetCurrentResources().Wood;
            Debug.Log($"Initial wood count: {initialWoodCount}");

            // Get initial money count (assuming there's a money/currency system)
            int initialMoneyCount = resourceManager.GetCurrentResources().Money;
            Debug.Log($"Initial money count: {initialMoneyCount}");

            // Create and execute the task to buy wood
            var taskCreator = aiTaskConverter.GetComponent<TaskCreator>();
            var retval =
                taskCreator.CreateTaskCoroutineByFilepath(
                    "Assets/TestAudioFiles/test_4_buy_a_specific_amount_of_a_resource.mp3");
            yield return retval;

            // Wait for the purchase to be processed
            float waitTime = 5f;
            float startTime = Time.time;
            while (Time.time - startTime < waitTime)
            {
                if (resourceManager.GetCurrentResources().Wood > initialWoodCount)
                {
                    break;
                }

                yield return null;
            }

            // Check if wood count has increased by 100
            int finalWoodCount = resourceManager.GetCurrentResources().Wood;
            Assert.AreEqual(initialWoodCount + 100, finalWoodCount,
                $"Wood count did not increase by 100. Initial: {initialWoodCount}, Final: {finalWoodCount}");

            // Check if money has decreased (assuming there's a cost for wood)
            int finalMoneyCount = resourceManager.GetCurrentResources().Money;
            Assert.IsTrue(finalMoneyCount < initialMoneyCount,
                $"Money did not decrease after purchase. Initial: {initialMoneyCount}, Final: {finalMoneyCount}");

            Debug.Log("Test completed: Buy 100 units of wood from the store");
        }
        
        [UnityTest]
        public IEnumerator TestSellStoneFromStore()
        {
            Debug.Log("Starting test: Sell 50 Units of Stone from Store");
            ResourceStore resourceManager = aiTaskExecutor.GetComponent<TaskExecutor>().Store;
            Assert.IsNotNull(resourceManager, "ResourceManager component not found on ResourceManager object.");

            // Get initial wood count
            int initialStoneCount = resourceManager.GetCurrentResources().Stone;
            Debug.Log($"Initial stone count: {initialStoneCount}");

            // Get initial money count (assuming there's a money/currency system)
            int initialMoneyCount = resourceManager.GetCurrentResources().Money;
            Debug.Log($"Initial money count: {initialMoneyCount}");

            // Create and execute the task to buy wood
            var taskCreator = aiTaskConverter.GetComponent<TaskCreator>();
            var retval =
                taskCreator.CreateTaskCoroutineByFilepath(
                    "Assets/TestAudioFiles/test_5_sell_a_specific_amount_of_a_resource.mp3");
            yield return retval;

            // Wait for the purchase to be processed
            float waitTime = 5f;
            float startTime = Time.time;
            while (Time.time - startTime < waitTime)
            {
                if (resourceManager.GetCurrentResources().Stone < initialStoneCount)
                {
                    break;
                }

                yield return null;
            }

            // Check if wood count has increased by 100
            int finalStoneCount = resourceManager.GetCurrentResources().Stone;
            Assert.AreEqual(initialStoneCount - 50, finalStoneCount,
                $"Stone count did not decrease by 50. Initial: {initialStoneCount}, Final: {finalStoneCount}");

            // Check if money has decreased (assuming there's a cost for wood)
            int finalMoneyCount = resourceManager.GetCurrentResources().Money;
            Assert.IsTrue(finalMoneyCount > initialMoneyCount,
                $"Money did not decrease after purchase. Initial: {initialMoneyCount}, Final: {finalMoneyCount}");

            Debug.Log("Test completed: Sell 50 units of stone from the store");
        }
    }
}