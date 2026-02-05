using AITransformer;
using UnityEngine;
using UnityEngine.UI;

namespace AITransformer
{
    public class GameLoop : MonoBehaviour
    {
        private int population;
        private BuildingRegister buildingRegister;
        TaskExecutor taskExecutor;
        public Text resourcesText;
        

        void Start()
        {
            Debug.Log("GameLoop started!");
            buildingRegister = FindObjectOfType<BuildingRegister>();
            resourcesText.text = "Initializing ... ";
            // Find task executor and get the resource store
            taskExecutor = FindObjectOfType<TaskExecutor>();
            if (buildingRegister == null)
            {
                Debug.LogError("BuildingRegister not found!");
                return;
            }
            UpdatePopulation();
        }
        
        void Update()
        {
            if (Time.time % 1f < Time.deltaTime)
            {
                DecreaseResources();
            }
            //every second update text
            if (Time.time % 1f < Time.deltaTime)
            {
                UpdateResourcesText();
            }
        }
        
        private void UpdateResourcesText()
        {
            if (taskExecutor.Store != null && resourcesText != null)
            {
                var resources = taskExecutor.Store.GetCurrentResources();
                resourcesText.text = $"Wood: {resources.Wood} " +
                                     $"Salt: {resources.Salt} " +
                                     $"Stone: {resources.Stone} " +
                                     $"Iron: {resources.Iron} " +
                                     $"Money: {resources.Money} " +
                                     $"Food: {resources.Food} " +
                                     $"Population: {population}";
            } 
        }
        
        
        private void DecreaseResources()
        {
            int resourceDecrease = population / 10; // Adjust this ratio as needed
            // Assuming you have a ResourceManager class to handle resources
            var currentResources = taskExecutor.Store.GetCurrentResources();
            var newResources = new ResourceStore.Resources(
                wood: currentResources.Wood,
                salt: currentResources.Salt,
                stone: currentResources.Stone,
                iron: currentResources.Iron,
                money: currentResources.Money,
                food: currentResources.Food - resourceDecrease
            );
            taskExecutor.Store.SetResources(newResources);
            //Debug.Log($"Resources decreased by {resourceDecrease} due to population of {population}");
        }
        

        public void UpdatePopulation()
        {
            if (buildingRegister != null)
            {
                var houses = buildingRegister.GetTilesByType("House");
                population = 10 * houses.Count;
                Debug.Log($"Population updated to {population}");
            }
        }

        public int GetPopulation()
        {
            return population;
        }
    }
}