using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;
using UnityEngine.Tilemaps;
using WhisperInput;

namespace AITransformer
{
    public class TaskCreator : MonoBehaviour
    {
        private Tilemap _tilemap;
        private HttpClient _httpClient;
        private WhisperTranscriber _transcriber;
        private OpenAIChatClient _chatClient;
        private List<TaskSystem.BaseTask> _tasks;
        private String _apikey = "YOUR_API_KEY_HERE";
        private IEnumerator _enumerator;
        public TilemapPrinter tilemapPrinter;
        public TaskExecutor taskExecutor;

        // var AudioInputHandler -> TODO

        void Start()
        {
            _httpClient = new HttpClient();
            _transcriber = new WhisperTranscriber(_apikey);
            _chatClient = new OpenAIChatClient(_apikey);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Start a new instance of the coroutine
                //StartCoroutine(CreateTaskCoroutine());
            }
        }

        public IEnumerator ProcessAudioToTask(byte[] audioBytes)
        {
            Debug.Log("Processing audio to task...");
            var transcribeTask = _transcriber.TranscribeAudioAsync(audioBytes);
            yield return new WaitUntil(() => transcribeTask.IsCompleted);

            if (transcribeTask.Exception != null)
            {
                Debug.LogError(transcribeTask.Exception);
                yield break;
            }

            var text = transcribeTask.Result;
            var conversionTask = _chatClient.ConvertInputToInGameTask(text, tilemapPrinter.GetTilemap(), taskExecutor.Store.GetCurrentResources());
            yield return new WaitUntil(() => conversionTask.IsCompleted);

            if (conversionTask.Exception != null)
            {
                //Debug.LogError(conversionTask.Exception);
                yield break;
            }

            var tasks = conversionTask.Result;
            Debug.Log($"Received {tasks.Count} tasks");

            foreach (var task in tasks)
            {
                if (task != null)
                {
                    Debug.Log($"Successfully processed audio to task of type: {task.GetType().Name}");
                    taskExecutor.AddNewTask(task);
                }
                else
                {
                    Debug.LogError("Failed to extract a recognized task type from the result.");
                }
            }
        }

        public IEnumerator CreateTaskCoroutineByFilepath(string audioFilePath)
        {
            Debug.Log("Starting coroutine with file path...");
            byte[] audioBytes = System.IO.File.ReadAllBytes(audioFilePath);
            yield return StartCoroutine(ProcessAudioToTask(audioBytes));
            Debug.Log("Finished coroutine with file path.");
        }

        private IEnumerator CreateTaskCoroutine()
        {
            Debug.Log("Starting coroutine with default path...");
            byte[] audioBytes = System.IO.File.ReadAllBytes("Assets/AITransformer/test_place_hut.mp3");
            yield return StartCoroutine(ProcessAudioToTask(audioBytes));
            Debug.Log("Finished coroutine with default path.");
        }

        public IEnumerator ProcessTextToTask(string textInput)
        {
            Debug.Log($"Processing text to task: {textInput}");

            var conversionTask = _chatClient.ConvertInputToInGameTask(textInput, tilemapPrinter.GetTilemap(), taskExecutor.Store.GetCurrentResources());
            yield return new WaitUntil(() => conversionTask.IsCompleted);

            if (conversionTask.Exception != null)
            {
                Debug.LogError($"Error converting text to task: {conversionTask.Exception.Message}");
                yield break;
            }

            var tasks = conversionTask.Result;
            Debug.Log($"Received {tasks.Count} tasks from text input");

            foreach (var task in tasks)
            {
                if (task != null)
                {
                    Debug.Log($"Successfully processed text to task of type: {task.GetType().Name}");
                    taskExecutor.AddNewTask(task);
                }
                else
                {
                    Debug.LogError("Failed to extract a recognized task type from the result.");
                }
            }
        }

    }
}