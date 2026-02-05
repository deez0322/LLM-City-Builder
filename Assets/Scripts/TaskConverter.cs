using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using WhisperInput;

namespace AITransformer
{
    public class LLMExecutionOptions : MonoBehaviour
    {
        public enum ContextFormatType
        {
            SQL,
            JSON,
            MinimapText
        }

        public enum SelfCorrectionType
        {
            SingleStep,
            MultiStep,
            None
        }

        public string model { get; set; }
        public SelfCorrectionType selfCorrection { get; set; }
        public ContextFormatType contextFormat { get; set; }

        public bool useFewShotPrompting { get; set; }

        public bool useChainOfThough { get; set; }

        private void Awake()
        {
            // Set default values
            model = "gpt-4o-mini";
            selfCorrection = SelfCorrectionType.None;
            contextFormat = ContextFormatType.JSON;
        }
    }

    public class ChatMessage
    {
        [JsonProperty("role")] public string Role { get; set; }

        [JsonProperty("content")] public string Content { get; set; }

        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    public class OpenAIChatClient
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private BuildingRegister _buildingRegister;

        public OpenAIChatClient(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _httpClient = new HttpClient();
            GameObject buildingRegisterObject = GameObject.Find("BuildingRegister");
            if (buildingRegisterObject != null)
            {
                _buildingRegister = buildingRegisterObject.GetComponent<BuildingRegister>();
            }
        }

        // Add this method to your OpenAIChatClient class
        public async Task<string> ProcessTextCommand(string textCommand)
        {
            // TODO: 
            return "";
        }

        public async Task<List<TaskSystem.BaseTask>> ConvertInputToInGameTask(string task, string tilemap,
            ResourceStore.Resources resources)
        {
            var options = GameObject.Find("GlobalVariables")?.GetComponent<LLMExecutionOptions>();
            if (options == null)
            {
                options = new LLMExecutionOptions()
                {
                    model = "gpt-4o-mini",
                    selfCorrection = LLMExecutionOptions.SelfCorrectionType.None,
                    contextFormat = LLMExecutionOptions.ContextFormatType.JSON
                };
            }

            if (task == null || task.Length < 3)
            {
                return new List<TaskSystem.BaseTask>();
            }

            if (task.ToLower().Contains("buy") || task.ToLower().Contains("sell") ||
                task.ToLower().Contains("purchase"))
            {
                return await ConvertToStoreInteractionTasks(task, resources, options);
            }
            else
            {
                return await ConvertToMapInteractionTasks(task, tilemap, options, resources);
            }
        }

        public async Task<string> ProcessChatMessagesAsync(String modelName, ChatMessage[] messages)
        {
            try
            {
                var responseAsString = await SendChatMessageAsync(modelName, messages);
                Debug.Log("Intermediate response: " + responseAsString);

                var response = JsonConvert.DeserializeObject<TaskSystem.ChatCompletionResponse>(responseAsString);
                var contentJson = response?.Choices?.FirstOrDefault()?.Message?.Content;

                return contentJson ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing chat messages: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<List<TaskSystem.BaseTask>> ConvertToMapInteractionTasks(string task, string tilemap,
            LLMExecutionOptions options, ResourceStore.Resources resources)
        {
            PromptBuilder promptBuilder;
            if (options.contextFormat == LLMExecutionOptions.ContextFormatType.SQL)
            {
                promptBuilder = new PromptBuilder()
                    .WithReplacement("type", Enums.AllTaskTypes())
                    .WithReplacement("building", Enums.AllBuildingTypes())
                    .WithReplacement("task", task)
                    .WithReplacement("resources", resources.GetOutputString())
                    .WithReplacement("prices", Enums.GetAllBuildingCostsAsString())
                    .WithReplacement("mapContext", PromptBuilder.getSQLMapContextPrompt());
            }
            else if (options.contextFormat == LLMExecutionOptions.ContextFormatType.JSON)
            {
                promptBuilder = new PromptBuilder()
                    .WithReplacement("type", Enums.AllTaskTypes())
                    .WithReplacement("building", Enums.AllBuildingTypes())
                    .WithReplacement("resources", resources.GetOutputString())
                    .WithReplacement("task", task)
                    .WithReplacement("prices", Enums.GetAllBuildingCostsAsString())
                    .WithReplacement("mapContext",
                        PromptBuilder.getJSONMapContextPrompt(_buildingRegister.GetTilesAsJsonArray()));
            }
            else if (options.contextFormat == LLMExecutionOptions.ContextFormatType.MinimapText)
            {
                promptBuilder = new PromptBuilder()
                    .WithReplacement("type", Enums.AllTaskTypes())
                    .WithReplacement("building", Enums.AllBuildingTypes())
                    .WithReplacement("resources", resources.GetOutputString())
                    .WithReplacement("task", task)
                    .WithReplacement("prices", Enums.GetAllBuildingCostsAsString())
                    .WithReplacement("mapContext",
                        PromptBuilder.getMiniMapContextPrompt(_buildingRegister.GetTilesAsMinimap()));
            }
            else
            {
                throw new NotImplementedException("Context format not implemented");
            }

            var prompt = promptBuilder.GetPrompt(false, 0);

            Debug.Log("Generated Prompt: " + prompt);
            var chatMessages = new List<ChatMessage>
            {
                new ChatMessage("assistant", prompt)
            };
            var requestedInfoFromTilemap = "";
            if (options.contextFormat == LLMExecutionOptions.ContextFormatType.SQL)
            {
                var contentJson = await ProcessChatMessagesAsync(options.model, chatMessages.ToArray());
                requestedInfoFromTilemap = _buildingRegister.ExecuteQuery(contentJson);
                if (!requestedInfoFromTilemap.StartsWith("Error:"))
                {
                    Debug.Log("Requested the following information: " + requestedInfoFromTilemap);
                }
                else
                {
                    Debug.LogWarning("Failed to execute query: " + requestedInfoFromTilemap);
                }
                Debug.Log(requestedInfoFromTilemap);


                var receivedContent = "You executed a SQL command to get the map data you need. " +
                                     "This is the result of the query: Query: " +
                                     string.Join(", ", _buildingRegister.ExtractSqlStatements(contentJson))
                                     + " \n Result: " + requestedInfoFromTilemap;
                promptBuilder = new PromptBuilder()
                    .WithReplacement("type", Enums.AllTaskTypes())
                    .WithReplacement("building", Enums.AllBuildingTypes())
                    .WithReplacement("task", task)
                    .WithReplacement("resources", resources.GetOutputString())
                    .WithReplacement("prices", Enums.GetAllBuildingCostsAsString())
                    .WithReplacement("mapContext", receivedContent);
                chatMessages.Clear();
                chatMessages.Add(new ChatMessage("assistant", promptBuilder.GetPrompt(false, 0)));
            }

            var prompt2 = promptBuilder.GetPrompt(false, 1);
            chatMessages.Add(new ChatMessage("assistant", prompt2));
            var responseAsString = await ProcessChatMessagesAsync(options.model, chatMessages.ToArray());
            var prompt3 = promptBuilder.GetPrompt(false, 2);
            chatMessages.Add(new ChatMessage("assistant", responseAsString));
            chatMessages.Add(new ChatMessage("assistant", prompt3));
            var responseAsString2 = await ProcessChatMessagesAsync(options.model, chatMessages.ToArray());
            Debug.Log("received final response: " + responseAsString2);
            var mapTasks = new List<TaskSystem.MapInteractionTask>();
            if (options.selfCorrection == LLMExecutionOptions.SelfCorrectionType.SingleStep)
            {
                var selfCorrectionPrompt = new PromptBuilder()
                    .WithReplacement("type", Enums.AllTaskTypes())
                    .WithReplacement("building", Enums.AllBuildingTypes())
                    .WithReplacement("task", task)
                    .WithReplacement("response", responseAsString2)
                    .GetPrompt(false, 5);
                chatMessages.Add(new ChatMessage("assistant", responseAsString2));
                chatMessages.Add(new ChatMessage("assistant", selfCorrectionPrompt));
                var correctedResponseAsString = await ProcessChatMessagesAsync(options.model, chatMessages.ToArray());
                if (correctedResponseAsString.ToLower().Contains("no errors"))
                {
                    mapTasks = TaskSystem.BaseTask.ExtractTasks<TaskSystem.MapInteractionTask>(responseAsString2);
                    return new List<TaskSystem.BaseTask>(mapTasks);
                }
                else
                {
                    mapTasks = TaskSystem.BaseTask.ExtractTasks<TaskSystem.MapInteractionTask>(correctedResponseAsString);
                    return new List<TaskSystem.BaseTask>(mapTasks);
                }
            }

            if (options.selfCorrection == LLMExecutionOptions.SelfCorrectionType.MultiStep)
            {
                // Step 1: Find errors
                var errorFindingPrompt = new PromptBuilder()
                    .WithReplacement("type", Enums.AllTaskTypes())
                    .WithReplacement("building", Enums.AllBuildingTypes())
                    .WithReplacement("task", task)
                    .WithReplacement("response", responseAsString2)
                    .GetPrompt(false, 3); // Assuming index 3 is for error finding

                chatMessages.Add(new ChatMessage("assistant", responseAsString2));
                chatMessages.Add(new ChatMessage("assistant", errorFindingPrompt));

                var errorFindingResponse = await ProcessChatMessagesAsync(options.model, chatMessages.ToArray());
                if (errorFindingResponse.ToLower().Contains("no errors"))
                {
                    mapTasks = TaskSystem.BaseTask.ExtractTasks<TaskSystem.MapInteractionTask>(responseAsString2);
                    return new List<TaskSystem.BaseTask>(mapTasks);
                }
                // Step 2: Fix errors
                var errorFixingPrompt = new PromptBuilder()
                    .WithReplacement("type", Enums.AllTaskTypes())
                    .WithReplacement("building", Enums.AllBuildingTypes())
                    .WithReplacement("task", task)
                    .WithReplacement("response", responseAsString2)
                    .WithReplacement("errors", errorFindingResponse)
                    .GetPrompt(false, 4); // Assuming index 4 is for error fixing
                chatMessages.Add(new ChatMessage("assistant", errorFindingResponse));
                chatMessages.Add(new ChatMessage("assistant", errorFixingPrompt));
                var correctedResponseAsString = await ProcessChatMessagesAsync(options.model, chatMessages.ToArray());
                Debug.Log("received final response: " + correctedResponseAsString);
                mapTasks = TaskSystem.BaseTask.ExtractTasks<TaskSystem.MapInteractionTask>(correctedResponseAsString);
                return new List<TaskSystem.BaseTask>(mapTasks);
            }
            else
            {
                mapTasks = TaskSystem.BaseTask.ExtractTasks<TaskSystem.MapInteractionTask>(responseAsString2);
            }

            return new List<TaskSystem.BaseTask>(mapTasks);
        }


        private async Task<List<TaskSystem.BaseTask>> ConvertToStoreInteractionTasks(string task,
            ResourceStore.Resources resources, LLMExecutionOptions options)
        {
            var promptBuilder = new PromptBuilder()
                .WithReplacement("type", "Buy,Sell")
                .WithReplacement("task", task)
                .WithReplacement("currentResources", resources.GetOutputString())
                .WithReplacement("buildingCosts", Enums.AllBuildingTypesWithCosts())
                .WithReplacement("resourcePrices", Enums.GetAllPricesAsString());

            var chatMessages = new List<ChatMessage>();
            var prompt = promptBuilder.GetPrompt(true);
            Debug.Log("Generated Prompt: " + prompt);

            chatMessages.Add(new ChatMessage("assistant", prompt));

            if (options.useFewShotPrompting)
            {
                //split the first message into two parts: 1. part everything until "Now," 2. part everything after "Now,"
                var firstMessage = chatMessages[0].Content;
                var firstMessageSplit = firstMessage.Split("Now,");
                chatMessages[0] = new ChatMessage("assistant", firstMessageSplit[0]);

                var fewShotExamples = new[]
                {
                    // Simple purchase
                    ("Buy 150 salt",
                        @"[{""type"": ""Buy"", ""resources"": {""Wood"": 0,  ""Salt"": 150,  ""Food"": 0,  ""Iron"": 50, ""Stone"": 50, ""Money"": 0}]"),

                    // Basic sell all
                    ("Sell all my stone",
                        @"[{""type"": ""Sell"", ""resources"": {""Wood"": 0,  ""Salt"": 0,  ""Food"": 0,  ""Iron"": 0, ""Stone"": 235, ""Money"": 0}]"),

                    // Multiple operations
                    ("Purchase 10 iron and sell 5 wood",
                        @"[{""type"": ""Buy"", ""resources"": {""Wood"": 0,  ""Salt"": 0,  ""Food"": 0,  ""Iron"": 10, ""Stone"": 0, ""Money"": 0},
                            {""type"": ""Sell"", ""resources"": {""Wood"": 5,  ""Salt"": 0,  ""Food"": 0,  ""Iron"": ß, ""Stone"": 0, ""Money"": 0}]"),

                    // Resource management for specific goal
                    ("Buy enough resources for two houses",
                        @"[{""type"": ""Buy"", {""type"": ""Buy"", ""resources"": {""Wood"": 100,  ""Salt"": 0,  ""Food"": 0,  ""Iron"": 0, ""Stone"": 60, ""Money"": 0}]"),

                    // Complex resource optimization
                    ("Sell any resources not needed for building houses, but keep enough for 3 houses",
                        @"[
                    {""type"": ""Sell"" , ""resources"": {""Wood"": 150,  ""Salt"": 125,  ""Food"": 15430,  ""Iron"": 452, ""Stone"": 324, ""Money"": 0}, {""type"": ""Buy"", ""resources"": {""Wood"": 150,  ""Salt"": 125,  ""Food"": 15430,  ""Iron"": 452, ""Stone"": 324, ""Money"": 0}},
                    ]")
                };
                chatMessages.Add(new ChatMessage("user", "Here are some examples on how to fulfill the tasks:"));
                foreach (var (example, exampleResponse) in fewShotExamples)
                {
                    chatMessages.Add(new ChatMessage("user", example));
                    chatMessages.Add(new ChatMessage("assistant", exampleResponse));
                }

                chatMessages.Add(new ChatMessage("user", "Now," + firstMessageSplit[1]));
            }

            var contentJson = await ProcessChatMessagesAsync(options.model, chatMessages.ToArray());
            var storeTasks = new List<TaskSystem.StoreInteractionTask>();
            if (options.selfCorrection == LLMExecutionOptions.SelfCorrectionType.MultiStep)
            {
                // Use existing self-correction logic
                var correctionMessages = new List<ChatMessage>(chatMessages)
                {
                    new ChatMessage("assistant", contentJson),
                    new ChatMessage("user", promptBuilder.GetPrompt(true, 2))
                };

                var contentJson2 = await ProcessChatMessagesAsync(options.model, correctionMessages.ToArray());

                Debug.Log("Found following Problems with AI Task: " + contentJson2);
                if (!contentJson2.ToLower().Contains("no errors"))
                {
                    promptBuilder = new PromptBuilder()
                        .WithReplacement("type", "Buy,Sell")
                        .WithReplacement("task", task)
                        .WithReplacement("currentResources", resources.GetOutputString())
                        .WithReplacement("buildingCosts", Enums.AllBuildingTypesWithCosts())
                        .WithReplacement("resourcePrices", Enums.GetAllPricesAsString())
                        .WithReplacement("errors", contentJson2);
                    correctionMessages.Add(new ChatMessage("user", promptBuilder.GetPrompt(true, 3)));
                    contentJson = await ProcessChatMessagesAsync(options.model, correctionMessages.ToArray());
                    Debug.Log("Received response: " + contentJson);
                }
                // Add final correction messages
                storeTasks = TaskSystem.BaseTask.ExtractTasks<TaskSystem.StoreInteractionTask>(contentJson);
            }
            else if (options.selfCorrection == LLMExecutionOptions.SelfCorrectionType.SingleStep)
            {
                // Use existing self-correction logic
                var correctionMessages = new List<ChatMessage>(chatMessages)
                {
                    new ChatMessage("assistant", contentJson),
                    new ChatMessage("user", promptBuilder.GetPrompt(true, 1))
                };
                var contentJson2 = await ProcessChatMessagesAsync(options.model, correctionMessages.ToArray());
                // if not errors found, return the original response
                if (contentJson2.ToLower().Contains("no errors"))
                {
                    storeTasks = TaskSystem.BaseTask.ExtractTasks<TaskSystem.StoreInteractionTask>(contentJson);
                    return new List<TaskSystem.BaseTask>(storeTasks);
                }
                storeTasks = TaskSystem.BaseTask.ExtractTasks<TaskSystem.StoreInteractionTask>(contentJson2);
            }
            else
            {
                storeTasks = TaskSystem.BaseTask.ExtractTasks<TaskSystem.StoreInteractionTask>(contentJson);
            }

            if (storeTasks.Count == 0)
            {
                Debug.LogWarning("Failed to extract tasks from response.");
                return new List<TaskSystem.BaseTask>();
            }
            return new List<TaskSystem.BaseTask>(storeTasks);
        }

        private async Task<string> SendChatMessageAsync(string model, ChatMessage[] messages)
        {
            var requestData = new
            {
                model = model,
                messages = messages,
                temperature = 1.0,
            }; ;
            if (model == "gpt-4o-mini")
            {
                requestData = new
                {
                    model = model,
                    messages = messages,
                    temperature = 0.2,
                };
            }

            var json = JsonConvert.SerializeObject(requestData);
            Debug.Log("Running Prompt: " + json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                // Log or handle error
                Console.WriteLine($"Error: {response.StatusCode}");
                return null;
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }
    }
}