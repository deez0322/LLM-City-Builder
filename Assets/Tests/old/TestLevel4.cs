using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    public class TestLevel4
    {
        /*private GameObject tilemap;
        private GameObject aiTaskConverter;
        private GameObject aiTaskExecutor;
        private BuildingRegister _buildingRegister;
        private ResourceStore _resourceStore;

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

            _resourceStore = aiTaskExecutor.GetComponent<TaskExecutor>().Store;
            SetupInitialResources();
        }

        private void SetupInitialResources()
        {
            var initialResources = new ResourceStore.Resources(
                wood: 10000,
                salt: 8000,
                stone: 12000,
                iron: 6000,
                money: 50000,
                food: 15000
            );
            _resourceStore.SetResources(initialResources);
        }

        [UnityTest]
        public IEnumerator TestSustainableVillageBuilding()
        {
            Debug.Log("Starting test: Build sustainable village");

            int initialBuildingCount = _buildingRegister.getAllGameObjects().Count();
            var initialResources = _resourceStore.GetCurrentResources();
            var taskCreator = aiTaskConverter.GetComponent<TaskCreator>();

            var retval = taskCreator.CreateTaskCoroutineByFilepath(
                "Assets/TestAudioFiles/test_14_build_sustainable_village.mp3");
            yield return retval;

            float waitTime = 30f;
            yield return new WaitForSeconds(waitTime);

            int finalBuildingCount = _buildingRegister.getAllGameObjects().Count();
            var finalResources = _resourceStore.GetCurrentResources();

            Assert.IsTrue(finalBuildingCount > initialBuildingCount,
                $"No new buildings were constructed. Initial: {initialBuildingCount}, Final: {finalBuildingCount}");

            var allBuildings = _buildingRegister.getAllGameObjects();
            int farmCount = allBuildings.Count(b => b.Item2 == Enums.BuildingType.Farm);
            int millCount = allBuildings.Count(b => b.Item2 == Enums.BuildingType.Mill);
            int houseCount = allBuildings.Count(b => b.Item2 == Enums.BuildingType.House);

            Assert.IsTrue(farmCount > 0, "No farms were built for food production");
            Assert.IsTrue(millCount > 0, "No mills were built for wood production");
            Assert.IsTrue(houseCount > 0, "No houses were built for population growth");

            float resourceRatio = CalculateResourceRatio(finalResources);
            float initialResourceRatio = CalculateResourceRatio(initialResources);

            Assert.IsTrue(resourceRatio > initialResourceRatio,
                $"Resource balance not improved. Initial ratio: {initialResourceRatio}, Final ratio: {resourceRatio}");

            LogSustainabilityResults( initialBuildingCount, finalBuildingCount, initialResources, finalResources);

            Debug.Log("Test completed: Build sustainable village");
        }

        private float CalculateResourceRatio(ResourceStore.Resources resources)
        {
            return (float)(resources.Food + resources.Wood) / (resources.Stone + resources.Iron + 1);
        }

        private void LogSustainabilityResults(int initialBuildings, int finalBuildings,
            ResourceStore.Resources initialResources, ResourceStore.Resources finalResources)
        {
            string filePath = "Assets/TestLogs/sustainability_results.csv";
            bool fileExists = File.Exists(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                if (!fileExists)
                {
                    writer.WriteLine(
                        "Timestamp,Initial Buildings,Final Buildings,Initial Food,Final Food,Initial Wood,Final Wood,Initial Stone,Final Stone,Initial Iron,Final Iron,Initial Money,Final Money");
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                writer.WriteLine(
                    $"{timestamp},{initialBuildings},{finalBuildings},{initialResources.Food},{finalResources.Food},{initialResources.Wood},{finalResources.Wood},{initialResources.Stone},{finalResources.Stone},{initialResources.Iron},{finalResources.Iron},{initialResources.Money},{finalResources.Money}");
            }
        }

        [UnityTest]
        public IEnumerator TestStoneMineAndHouseConstruction()
        {
            Debug.Log("Starting test: Build stone mine and houses");

            int initialHouseCount =
                _buildingRegister.getAllGameObjects().Count(b => b.Item2 == Enums.BuildingType.House);
            var initialResources = _resourceStore.GetCurrentResources();
            var taskCreator = aiTaskConverter.GetComponent<TaskCreator>();

            var retval = taskCreator.CreateTaskCoroutineByFilepath(
                "Assets/TestAudioFiles/test_15_build_stone_mine_and_houses.mp3");
            yield return retval;

            float waitTime = 20f;
            yield return new WaitForSeconds(waitTime);

            int finalStoneMinaCount = _buildingRegister.getAllGameObjects()
                .Count(b => b.Item2 == Enums.BuildingType.StoneMine);
            int finalHouseCount = _buildingRegister.getAllGameObjects().Count(b => b.Item2 == Enums.BuildingType.House);
            var finalResources = _resourceStore.GetCurrentResources();

            Assert.IsTrue(finalStoneMinaCount > initialStoneMinaCount,
                $"No new stone mine was built. Initial: {initialStoneMinaCount}, Final: {finalStoneMinaCount}");

            bool shouldBuildHouses = HasSufficientResourcesForHouses(initialResources);
            int expectedNewHouses = shouldBuildHouses ? 2 : 0;

            Assert.AreEqual(expectedNewHouses, finalHouseCount - initialHouseCount,
                $"Incorrect number of new houses built. Expected: {expectedNewHouses}, Actual: {finalHouseCount - initialHouseCount}");

            LogConstructionResults(initialStoneMinaCount, finalStoneMinaCount, initialHouseCount, finalHouseCount,
                initialResources, finalResources, shouldBuildHouses);

            Debug.Log("Test completed: Build stone mine and houses");
        }

        private bool HasSufficientResourcesForHouses(ResourceStore.Resources resources)
        {
            var houseCost = Enums.BuildingCosts[Enums.BuildingType.House];
            return resources.Wood >= houseCost["Wood"] * 2 &&
                   resources.Stone >= houseCost["Stone"] * 2;
        }

        private void LogConstructionResults(int initialStoneMines, int finalStoneMines, int initialHouses,
            int finalHouses,
            ResourceStore.Resources initialResources, ResourceStore.Resources finalResources, bool shouldBuildHouses)
        {
            string filePath = "Assets/TestLogs/construction_results.csv";
            bool fileExists = File.Exists(filePath);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                if (!fileExists)
                {
                    writer.WriteLine(
                        "Timestamp,Initial Stone Mines,Final Stone Mines,Initial Houses,Final Houses,Should Build Houses,Initial Wood,Final Wood,Initial Stone,Final Stone");
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                writer.WriteLine(
                    $"{timestamp},{initialStoneMines},{finalStoneMines},{initialHouses},{finalHouses},{shouldBuildHouses},{initialResources.Wood},{finalResources.Wood},{initialResources.Stone},{finalResources.Stone}");
            }
        }*/
    }
}