using System.Collections;
using System.Linq;
using AITransformer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class MiscTests
    {
        private BuildingRegister _buildingRegister;
        
        private GameObject globalVariables;

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

            GameObject buildingRegisterObject = GameObject.Find("BuildingRegister");
            if (buildingRegisterObject != null)
            {
                _buildingRegister = buildingRegisterObject.GetComponent<BuildingRegister>();
            }
            else
            {
                Assert.Fail("BuildingRegister not found in the scene.");
            }
            
            globalVariables = GameObject.Find("GlobalVariables");
            if (globalVariables == null)
            {
                globalVariables = new GameObject("GlobalVariables");
                Object.DontDestroyOnLoad(globalVariables);
            }

            // Add LLMExecutionOptions component
            var options = globalVariables.GetComponent<LLMExecutionOptions>();
            if (options == null)
            {
                options = globalVariables.AddComponent<LLMExecutionOptions>();
            }

            // Set default values
            options.model = "gpt-4o-mini";
            options.selfCorrection = LLMExecutionOptions.SelfCorrectionType.None;
            options.contextFormat = LLMExecutionOptions.ContextFormatType.JSON;

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestBuildingRegisterSQLite()
        {
            // Clear existing buildings
            var existingBuildings = _buildingRegister.getAllGameObjects();
            foreach (var building in existingBuildings)
            {
                _buildingRegister.RemoveBuildingAtLocation(building.Item1);
            }

            // Test RegisterBuilding
            Vector3 location1 = new Vector3(1, 1, 0);
            Vector3 location2 = new Vector3(2, 2, 0);
            _buildingRegister.RegisterBuilding(location1, Enums.BuildingType.House);
            _buildingRegister.RegisterBuilding(location2, Enums.BuildingType.LumberjackHut);

            // Test GetBuildingAtLocation
            Assert.AreEqual(Enums.BuildingType.House, _buildingRegister.GetBuildingAtLocation(location1));
            Assert.AreEqual(Enums.BuildingType.LumberjackHut, _buildingRegister.GetBuildingAtLocation(location2));

            // Test UpdateBuildingAtLocation
            Vector3 newLocation = new Vector3(3, 3, 0);
            _buildingRegister.UpdateBuildingAtLocation(location1, newLocation, Enums.BuildingType.Store);
            Assert.AreEqual(Enums.BuildingType.Store, _buildingRegister.GetBuildingAtLocation(newLocation));
            Assert.AreEqual(Enums.BuildingType.NoBuilding, _buildingRegister.GetBuildingAtLocation(location1));

            // Test RemoveBuildingAtLocation
            _buildingRegister.RemoveBuildingAtLocation(location2);
            Assert.AreEqual(Enums.BuildingType.NoBuilding, _buildingRegister.GetBuildingAtLocation(location2));

            // Test getAllGameObjects
            var allBuildings = _buildingRegister.getAllGameObjects();
            Assert.AreEqual(1, allBuildings.Count);
            Assert.AreEqual(newLocation, allBuildings[0].Item1);
            Assert.AreEqual(Enums.BuildingType.Store, allBuildings[0].Item2);

            // Test GetBuildingsAsJson
            string json = _buildingRegister.GetBuildingsAsJson();
            Assert.IsTrue(json.Contains("\"type\": \"Store\""));
            Assert.IsTrue(json.Contains("\"X\": 3"));
            Assert.IsTrue(json.Contains("\"Y\": 3"));

            // Test ExecuteQuery
            string queryResult = _buildingRegister.ExecuteQuery("SELECT * FROM tiles");
            Assert.IsTrue(queryResult.Contains("x: 3"));
            Assert.IsTrue(queryResult.Contains("y: 3"));
            Assert.IsTrue(queryResult.Contains("type: Store"));

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestBuildingRegisterPersistence()
        {
            // Register a building
            Vector3 location = new Vector3(4, 4, 0);
            _buildingRegister.RegisterBuilding(location, Enums.BuildingType.Farm);

            // Simulate a scene reload
            yield return SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

            // Wait for the scene to load
            while (SceneManager.GetActiveScene().name != "SampleScene")
            {
                yield return null;
            }

            // Get the BuildingRegister again
            GameObject buildingRegisterObject = GameObject.Find("BuildingRegister");
            _buildingRegister = buildingRegisterObject.GetComponent<BuildingRegister>();

            // Check if the building is still registered
            Assert.AreEqual(Enums.BuildingType.Farm, _buildingRegister.GetBuildingAtLocation(location));
        }

        [UnityTest]
        public IEnumerator TestBuildingRegisterConcurrency()
        {
            // Register multiple buildings concurrently
            for (int i = 0; i < 100; i++)
            {
                Vector3 location = new Vector3(i, i, 0);
                _buildingRegister.RegisterBuilding(location, Enums.BuildingType.House);
            }

            // Allow some time for all operations to complete
            yield return new WaitForSeconds(1);

            // Verify all buildings were registered
            var allBuildings = _buildingRegister.getAllGameObjects();
            Assert.AreEqual(100, allBuildings.Count);

            // Verify each building is in the correct location
            for (int i = 0; i < 100; i++)
            {
                Vector3 location = new Vector3(i, i, 0);
                Assert.AreEqual(Enums.BuildingType.House, _buildingRegister.GetBuildingAtLocation(location));
            }
        }
    }
}
