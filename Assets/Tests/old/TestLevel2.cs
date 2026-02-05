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
    public class LoadSceneTestLevel2
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
        public IEnumerator TestConstructMultipleBuildingsOfSameType()
        {
            Debug.Log("Starting test: Construct multiple buildings of the same type");

            int initialBuildingCount = _buildingRegister.getAllGameObjects().Count();

            var taskCreator = aiTaskConverter.GetComponent<TaskCreator>();
            var retval = taskCreator.CreateTaskCoroutineByFilepath(
                "Assets/TestAudioFiles/test_6_construct_multiple_buildings_of_the_same_type.mp3");
            yield return retval;

            float waitTime = 30f;
            float startTime = Time.time;
            while (Time.time - startTime < waitTime)
            {
                int currentCount = _buildingRegister.getAllGameObjects().Count();
                if (currentCount >= initialBuildingCount + 3)
                {
                    break;
                }
                yield return null;
            }

            int finalBuildingCount = _buildingRegister.getAllGameObjects().Count();
            Assert.IsTrue(finalBuildingCount >= initialBuildingCount + 3, 
                $"Three new buildings were not added. Initial: {initialBuildingCount}, Final: {finalBuildingCount}");

            Debug.Log("Test completed: Construct multiple buildings of the same type");
        }

        [UnityTest]
        public IEnumerator TestSequentialBuildingConstruction()
        {
            Debug.Log("Starting test: Sequential building construction");

            int initialBuildingCount = _buildingRegister.getAllGameObjects().Count();

            var taskCreator = aiTaskConverter.GetComponent<TaskCreator>();
            var retval = taskCreator.CreateTaskCoroutineByFilepath(
                "Assets/TestAudioFiles/test_7_sequential_building_construction.mp3");
            yield return retval;

            float waitTime = 30f;
            float startTime = Time.time;
            while (Time.time - startTime < waitTime)
            {
                int currentCount = _buildingRegister.getAllGameObjects().Count();
                if (currentCount >= initialBuildingCount + 2)
                {
                    break;
                }
                yield return null;
            }

            int finalBuildingCount = _buildingRegister.getAllGameObjects().Count();
            Assert.IsTrue(finalBuildingCount >= initialBuildingCount + 2, 
                $"Two new buildings were not added sequentially. Initial: {initialBuildingCount}, Final: {finalBuildingCount}");

            Debug.Log("Test completed: Sequential building construction");
        }

        [UnityTest]
        public IEnumerator TestComplexTradeCommands()
        {
            Debug.Log("Starting test: Complex trade commands");

            ResourceStore resourceManager = aiTaskExecutor.GetComponent<TaskExecutor>().Store;
            resourceManager.SetResources(new ResourceStore.Resources(
                wood: 1000,
                salt: 1000,
                stone: 1000,
                iron: 1000,
                money: 10000,
                food: 1000
            ));
            int initialSaltCount = resourceManager.GetCurrentResources().Salt;
            int initialIronCount = resourceManager.GetCurrentResources().Iron;
            int initialMoneyCount = resourceManager.GetCurrentResources().Money;

            var taskCreator = aiTaskConverter.GetComponent<TaskCreator>();
            var retval = taskCreator.CreateTaskCoroutineByFilepath(
                "Assets/TestAudioFiles/test_8_complex_trade_commands.mp3");
            yield return retval;

            float waitTime = 10f;
            yield return new WaitForSeconds(waitTime);

            int finalSaltCount = resourceManager.GetCurrentResources().Salt;
            int finalIronCount = resourceManager.GetCurrentResources().Iron;
            int finalMoneyCount = resourceManager.GetCurrentResources().Money;

            Assert.AreEqual(initialSaltCount - 200, finalSaltCount, "Salt was not sold correctly");
            Assert.AreEqual(initialIronCount + 100, finalIronCount, "Iron was not bought correctly");
            Assert.IsTrue(finalMoneyCount == initialMoneyCount, "Money has to stay the same because the trades nullify each other out");

            Debug.Log("Test completed: Complex trade commands");
        }

        [UnityTest]
        public IEnumerator TestDeleteMultipleBuildings()
        {
            Debug.Log("Starting test: Delete multiple buildings");

            // First, add some fishing huts to the west side
            for (int i = 0; i < 3; i++)
            {
                GameObject prefab = Resources.Load<GameObject>("FishingHutPrefab");
                if (prefab != null)
                {
                    GameObject newObject = Object.Instantiate(prefab, new Vector3(10 - i, 0, 0), Quaternion.identity);
                    _buildingRegister.RegisterBuilding(newObject.transform.position, Enums.BuildingType.FishingHut);
                }
            }

            int initialBuildingCount = _buildingRegister.getAllGameObjects().Count();

            var taskCreator = aiTaskConverter.GetComponent<TaskCreator>();
            var retval = taskCreator.CreateTaskCoroutineByFilepath(
                "Assets/TestAudioFiles/test_9_delete_multiple_buildings.mp3");
            yield return retval;

            float waitTime = 10f;
            yield return new WaitForSeconds(waitTime);

            int finalBuildingCount = _buildingRegister.getAllGameObjects().Count();
            Assert.IsTrue(finalBuildingCount < initialBuildingCount, 
                $"Buildings were not deleted. Initial: {initialBuildingCount}, Final: {finalBuildingCount}");

            Debug.Log("Test completed: Delete multiple buildings");
        }

        [UnityTest]
        public IEnumerator TestMoveMultipleBuildings()
        {
            Debug.Log("Starting test: Move multiple buildings");

            // First, add some mines
            for (int i = 0; i < 3; i++)
            {
                GameObject prefab = Resources.Load<GameObject>("MinePrefab");
                if (prefab != null)
                {
                    GameObject newObject = Object.Instantiate(prefab, new Vector3(i * 2, i * 2, 0), Quaternion.identity);
                    _buildingRegister.RegisterBuilding(newObject.transform.position, Enums.BuildingType.IronMine);
                }
            }

            var initialPositions = _buildingRegister.getAllGameObjects()
                .Where(b => b.Item2 == Enums.BuildingType.IronMine)
                .Select(b => b.Item1)
                .ToList();

            var taskCreator = aiTaskConverter.GetComponent<TaskCreator>();
            var retval = taskCreator.CreateTaskCoroutineByFilepath(
                "Assets/TestAudioFiles/test_10_move_multiple_buildings.mp3");
            yield return retval;

            float waitTime = 10f;
            yield return new WaitForSeconds(waitTime);

            var finalPositions = _buildingRegister.getAllGameObjects()
                .Where(b => b.Item2 == Enums.BuildingType.IronMine)
                .Select(b => b.Item1)
                .ToList();

            bool allMoved = initialPositions.All(pos => !finalPositions.Contains(pos));
            Assert.IsTrue(allMoved, "Not all mines were moved");

            Debug.Log("Test completed: Move multiple buildings");
        }
    }
}
