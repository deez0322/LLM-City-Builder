using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AITransformer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace WhisperInput
{
    public class TaskSystem
    {
        public abstract class BaseTask
        {
            public Enums.TaskType Type { get; set; }

            public static List<T> ExtractTasks<T>(string jsonResponse) where T : BaseTask, new()
            {
                try
                {
                    var contentJson = jsonResponse;

                    if (!string.IsNullOrEmpty(contentJson))
                    {
                        // Try to find a JSON array in the content
                        var match = Regex.Match(contentJson, @"\[([\s\S]*?)\]");
                        if (match.Success)
                        {
                            contentJson = "[" + match.Groups[1].Value + "]";
                        }
                        else
                        {
                            Debug.Log("No JSON array found in the content.");
                            return new List<T>();
                        }

                        var jsonArray = JArray.Parse(contentJson);
                        List<T> tasks = new List<T>();
                        if (jsonArray.Count == 0) return tasks;
                        foreach (var jsonObject in jsonArray)
                        {
                            if (typeof(T) == typeof(StoreInteractionTask))
                            {
                                tasks.Add(DeserializeStoreInteractionTask(jsonObject as JObject) as T);
                            }
                            else if (typeof(T) == typeof(MapInteractionTask))
                            {
                                tasks.Add(DeserializeMapInteractionTask(jsonObject as JObject) as T);
                            }
                        }

                        return tasks;
                    }
                    else
                    {
                        //Debug.LogError($"Failed to extract tasks from response. Content is empty or null.");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    //Debug.LogError($"Error in ExtractTasks: {ex.Message}");
                    return null;
                }
            }

            public static T ExtractTask<T>(string jsonResponse) where T : BaseTask, new()
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<ChatCompletionResponse>(jsonResponse);
                    var contentJson = response?.Choices?.FirstOrDefault()?.Message?.Content;

                    if (!string.IsNullOrEmpty(contentJson))
                    {
                        contentJson = contentJson.Replace("```json\n", "").Replace("\n```", "").Trim();
                        var jsonObject = JObject.Parse(contentJson);

                        Debug.Log($"Parsed JSON: {jsonObject}");

                        bool hasResources = jsonObject["resources"] != null;
                        bool hasBuilding = jsonObject["building"] != null;
                        bool hasLocation = jsonObject["location"] != null;

                        Debug.Log(
                            $"JSON properties: Resources: {hasResources}, Building: {hasBuilding}, Location: {hasLocation}");

                        if (hasResources)
                        {
                            return DeserializeStoreInteractionTask(jsonObject) as T;
                        }
                        else if ((hasBuilding || hasLocation))
                        {
                            return DeserializeMapInteractionTask(jsonObject) as T;
                        }
                        else
                        {
                            Debug.LogError($"Mismatch between JSON content and requested type {typeof(T).Name}. " +
                                           $"JSON has: Resources: {hasResources}, Building: {hasBuilding}, Location: {hasLocation}");
                            return null;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Failed to extract task from response. Content is empty or null.");
                        return null;
                    }
                }
                catch (JsonException ex)
                {
                    Debug.LogError($"JSON conversion error: {ex.Message}");
                    return null;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unexpected error in ExtractTask: {ex.Message}");
                    return null;
                }
            }

            private static StoreInteractionTask DeserializeStoreInteractionTask(JObject jsonObject)
            {
                var task = new StoreInteractionTask
                {
                    Type = ParseEnum<Enums.TaskType>(jsonObject["type"]?.ToString()),
                    Resources = jsonObject["resources"].ToObject<AITransformer.ResourceStore.Resources>()
                };
                return task;
            }

            private static MapInteractionTask DeserializeMapInteractionTask(JObject jsonObject)
            {
                var task = new MapInteractionTask
                {
                    Type = ParseEnum<Enums.TaskType>(jsonObject["type"]?.ToString()),
                    Location = jsonObject["location"]?.ToObject<Location>(),
                    NewLocation = jsonObject["newLocation"]?.ToObject<Location>(),
                    Building = ParseEnum<Enums.BuildingType>(jsonObject["building"]?.ToString())
                };
                return task;
            }

            private static T ParseEnum<T>(string value) where T : struct
            {
                if (string.IsNullOrEmpty(value))
                    return default;

                if (Enum.TryParse<T>(value, true, out T result))
                    return result;

                Debug.LogWarning($"Failed to parse enum value: {value} for type {typeof(T).Name}");
                return default;
            }
        }

        public class MapInteractionTask : BaseTask
        {
            public Location Location { get; set; }
            public Location NewLocation { get; set; }
            public Enums.BuildingType Building { get; set; }

            public static MapInteractionTask ExtractConstructionOrder(string jsonResponse)
            {
                return ExtractTask<MapInteractionTask>(jsonResponse);
            }
        }

        public class StoreInteractionTask : BaseTask
        {
            public AITransformer.ResourceStore.Resources Resources { get; set; }

            public static StoreInteractionTask ExtractStoreOrder(string jsonResponse)
            {
                return ExtractTask<StoreInteractionTask>(jsonResponse);
            }
        }


        public class Location
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        public class ChatCompletionResponse
        {
            public string Id { get; set; }
            public string Object { get; set; }
            public long Created { get; set; }
            public string Model { get; set; }
            public List<Choice> Choices { get; set; }
            public Usage Usage { get; set; }
            public string SystemFingerprint { get; set; }
        }

        public class Choice
        {
            public int Index { get; set; }
            public Message Message { get; set; }
            public string FinishReason { get; set; }
        }

        public class Message
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }

        public class Usage
        {
            public int PromptTokens { get; set; }
            public int CompletionTokens { get; set; }
            public int TotalTokens { get; set; }
        }
    }
}