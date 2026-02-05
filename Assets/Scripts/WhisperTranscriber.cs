using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine;

namespace AITransformer
{
    public class WhisperTranscriber
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public WhisperTranscriber(string apiKey)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _httpClient = new HttpClient();
        }
    
        private AudioClip _audioClip;
        private bool _isRecording = false;
        private const int SAMPLE_RATE = 44100; // Standard sample rate

        // Call this method to start recording
        public void StartRecording()
        {
            _audioClip = Microphone.Start(null, false, 10, SAMPLE_RATE);
            _isRecording = true;
        }

        // Call this method to stop recording and start transcription
        public async void StopRecordingAndTranscribe()
        {
            if (_isRecording)
            {
                Microphone.End(null);
                _isRecording = false;
                var data = ConvertAudioClipToByteArray(_audioClip);

                // Assuming TranscribeAudioAsync is available and properly implemented
                var transcription = await TranscribeAudioAsync(data);
                Debug.Log(transcription);
            }
        }

        private byte[] ConvertAudioClipToByteArray(AudioClip clip)
        {
            var samples = new float[clip.samples];
            clip.GetData(samples, 0);

            byte[] bytes = new byte[samples.Length * 4];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public async Task<string> TranscribeAudioAsync(byte[] audioData)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(audioData);
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                content.Add(fileContent, "file", "audio.wav");
                content.Add(new StringContent("whisper-1"), "model");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/audio/transcriptions", content);

                if (!response.IsSuccessStatusCode)
                {
                    // Log or handle error
                    Console.WriteLine($"Error: {response.StatusCode}");
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent;
            }
            catch (Exception ex)
            {
                // Log or handle exception
                Console.WriteLine($"Exception: {ex.Message}");
                return null;
            }
        }
    }
}