using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using UnityEngine;
using WhisperInput;

namespace AITransformer
{
    public class TaskExecutor : MonoBehaviour
    {
        private HttpClient _httpClient;
        private WhisperTranscriber _transcriber;
        private OpenAIChatClient _chatClient;
        public Stack<TaskSystem.BaseTask> _tasks = new Stack<TaskSystem.BaseTask>();
        //save old tasks
        public Stack<TaskSystem.BaseTask> oldTasks = new Stack<TaskSystem.BaseTask>();
        public ResourceStore Store; 
        private BuildingRegister _buildingRegister;
        private string _apiKey = "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";
        private float _perspectiveFix = 0.42f;
        private int total_executed_tasks = 0;

        void Start()
        {
            GameObject buildingRegisterObject = GameObject.Find("BuildingRegister");
            if (buildingRegisterObject != null)
            {
                _buildingRegister = buildingRegisterObject.GetComponent<BuildingRegister>();
            }
            else
            {
                Debug.LogError("BuildingRegister GameObject not found in the scene.");
            }
            InitializeResourceStore();
        }

        private void InitializeResourceStore()
        {
            Store = new ResourceStore();
            
            // Set initial resources
            ResourceStore.Resources initialResources = new ResourceStore.Resources(
                wood: 100,
                salt: 50,
                stone: 75,
                iron: 25,
                money: 100000,
                food: 200
            );

            Store.SetResources(initialResources);

            Debug.Log("ResourceStore initialized with initial resources:");
            Store.DisplayResources();
        }

        void Update()
        {
            if (_tasks.Count > 0)
            {
                // get last task without removing it
                var task = _tasks.Peek();
                //save old tasks
                oldTasks.Push(task);
                Debug.Log($"Popped new task: {task.GetType().Name} - {task.Type}");
                try
                {
                    ExecuteTask(task);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error executing task: {ex.Message}");
                }
                _tasks.Pop();
            }
        }

        public void AddNewTask(TaskSystem.BaseTask task)
        {
            Debug.Log("Received new Task ... ");
            if (task != null)
            {
                _tasks.Push(task);
            }
            else
            {
                Debug.LogWarning("Attempted to add a null task.");
            }
        }

        private void ExecuteTask(TaskSystem.BaseTask task)
        {
            
            Debug.Log("Executing task number " + (total_executed_tasks +1));
            Debug.Log("Task Type: " + task.GetType().Name);
            switch (task)
            {
                case TaskSystem.MapInteractionTask mapTask:
                    if (mapTask.Location != null)
                    {
                        ExecuteMapTask(mapTask);
                    }
                    break;
                case TaskSystem.StoreInteractionTask storeTask:
                    ExecuteStoreTask(storeTask);
                    break;
                default:
                    Debug.LogWarning($"Task type '{task.GetType().Name}' not recognized.");
                    break;
            }
            total_executed_tasks++;
        }

        private void ExecuteMapTask(TaskSystem.MapInteractionTask task)
        {
            switch (task.Type)
            {
                case Enums.TaskType.Add:
                    ExecuteAddTask(task);
                    break;
                case Enums.TaskType.Delete:
                    ExecuteDeleteTask(task);
                    break;
                case Enums.TaskType.Move:
                    ExecuteMoveTask(task);
                    break;
                default:
                    Debug.LogWarning($"Map task type '{task.Type}' not recognized.");
                    break;
            }
        }

        private void ExecuteStoreTask(TaskSystem.StoreInteractionTask task)
        {
            // Implement store task execution logic here
            Debug.Log($"Executing store task with resources: {task.Resources}");
            switch (task.Type)
            {
                case Enums.TaskType.Sell:
                    Debug.Log("Executing Sell Task");
                    ExecuteSellTask(task);
                    break;
                case Enums.TaskType.Buy:
                    Debug.Log("Executing Buy Task");
                    ExecuteBuyTask(task);
                    break;
                default:
                    Debug.LogWarning($"Map task type '{task.Type}' not recognized.");
                    break;
            }
        }

        private void ExecuteBuyTask(TaskSystem.StoreInteractionTask task)
        {
            Store.Buy(task.Resources);
        }

        private void ExecuteSellTask(TaskSystem.StoreInteractionTask task)
        {
            Store.Sell(task.Resources);
        }

        private void ExecuteMoveTask(TaskSystem.MapInteractionTask task)
        {
            Vector3 oldLocation = new Vector3(task.Location.X, task.Location.Y, 0);
            Vector3 newLocation = new Vector3(task.NewLocation.X, task.NewLocation.Y, 0);

            Enums.BuildingType buildingType = _buildingRegister.GetBuildingAtLocation(oldLocation);
            if (buildingType != Enums.BuildingType.NoBuilding)
            {
                _buildingRegister.UpdateBuildingAtLocation(oldLocation, newLocation, buildingType);
                Debug.Log($"Moved {buildingType} from ({task.Location.X}, {task.Location.Y}) to ({task.NewLocation.X}, {task.NewLocation.Y})");
            }
            else
            {
                Debug.LogWarning($"No building found at location ({task.Location.X}, {task.Location.Y})");
            }
        }

        private void ExecuteDeleteTask(TaskSystem.MapInteractionTask task)
        {
            Vector3 location = new Vector3(task.Location.X, task.Location.Y, 0);
            Enums.BuildingType buildingType = _buildingRegister.GetBuildingAtLocation(location);

            if (buildingType != Enums.BuildingType.NoBuilding)
            {
                _buildingRegister.RemoveBuildingAtLocation(location);
                Debug.Log($"Removed {buildingType} at location ({task.Location.X}, {task.Location.Y})");
            }
            else
            {
                Debug.LogWarning($"No building found at location ({task.Location.X}, {task.Location.Y})");
            }
        }

        
        private bool IsTileOccupied(TaskSystem.Location location)
        {
            Vector3 position = new Vector3(location.X, location.Y, 0);
            return _buildingRegister.GetBuildingAtLocation(position) != Enums.BuildingType.NoBuilding;
        }



        private void ExecuteAddTask(TaskSystem.MapInteractionTask task)
        {
            if (IsTileOccupied(task.Location))
            {
                Debug.LogWarning($"Cannot build at location ({task.Location.X}, {task.Location.Y}). Tile is already occupied.");
                return;
            }

            Vector3 location = new Vector3(task.Location.X, task.Location.Y, 0);
            _buildingRegister.RegisterBuilding(location, task.Building);

            Debug.Log($"Added {task.Building} at location ({task.Location.X}, {task.Location.Y})");
        }

    }
}
