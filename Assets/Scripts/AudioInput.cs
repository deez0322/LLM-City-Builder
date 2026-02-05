using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AITransformer;
using UnityEngine;
using Input = UnityEngine.Input;

public class RecordedAudio : MonoBehaviour
{
    private AudioClip recordedClip;
    [SerializeField] AudioSource audioSource;
    public TaskCreator taskCreator;
    private string _device;
    public GameObject recordingIndicator; // Assign this in the Inspector
    public PlaceTilemapOnPlane placeTilemapOnPlane;

    // Adjust this threshold to suit your recording levels.
    [SerializeField] private float silenceThreshold = 0.01f;

    private void Start()
    {
        taskCreator = GetComponent<TaskCreator>();
        recordingIndicator.SetActive(false);
    }

    /// <summary>
    /// Converts an AudioClip to a WAV byte array.
    /// This version trims the beginning and ending silence based on a threshold.
    /// </summary>
    /// <param name="clip">The recorded AudioClip.</param>
    /// <returns>Byte array containing the WAV file data.</returns>
    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        // Note: AudioClip.samples is the number of samples per channel.
        // To get all sample data, allocate samples * channels.
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // Trim silence from the beginning and end.
        samples = TrimSilence(samples, silenceThreshold, clip.channels);

        if (samples.Length == 0)
        {
            Debug.LogWarning("No non-silent audio detected in the recording.");
            return new byte[0];
        }

        // Convert the (trimmed) samples to a WAV byte array (PCM 16-bit)
        byte[] wavBytes = ConvertToWavBytes(samples, clip.channels, clip.frequency);
        return wavBytes;
    }

    /// <summary>
    /// Converts a float array of audio samples to WAV byte array in 16-bit PCM format.
    /// </summary>
    /// <param name="samples">Audio samples (after trimming silence, if desired).</param>
    /// <param name="channels">Number of audio channels.</param>
    /// <param name="sampleRate">Audio sample rate.</param>
    /// <returns>Byte array of the WAV file.</returns>
    private byte[] ConvertToWavBytes(float[] samples, int channels, int sampleRate)
    {
        int byteArraySize = samples.Length * 2; // 16-bit PCM = 2 bytes per sample
        byte[] bytes = new byte[44 + byteArraySize]; // 44 bytes for the WAV header

        int hz = sampleRate;
        int channelsCount = channels;
        int bitsPerSample = 16;
        int byteRate = hz * channelsCount * bitsPerSample / 8;
        int blockAlign = channelsCount * bitsPerSample / 8;

        using (var memoryStream = new MemoryStream(bytes))
        using (var writer = new BinaryWriter(memoryStream))
        {
            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));             // Chunk ID
            writer.Write(36 + byteArraySize);                            // Chunk size
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));               // Format

            // fmt subchunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));               // Subchunk1 ID
            writer.Write(16);                                            // Subchunk1 size
            writer.Write((short)1);                                      // Audio format (1 = PCM)
            writer.Write((short)channelsCount);                          // Number of channels
            writer.Write(hz);                                            // Sample rate
            writer.Write(byteRate);                                      // Byte rate
            writer.Write((short)blockAlign);                             // Block align
            writer.Write((short)bitsPerSample);                          // Bits per sample

            // data subchunk
            writer.Write(Encoding.ASCII.GetBytes("data"));               // Subchunk2 ID
            writer.Write(byteArraySize);                                 // Subchunk2 size

            // Write the audio data converted to 16-bit PCM
            foreach (float sample in samples)
            {
                short pcmValue = (short)(Mathf.Clamp(sample, -1.0f, 1.0f) * short.MaxValue);
                writer.Write(pcmValue);
            }
        }

        return bytes;
    }

    /// <summary>
    /// Trims leading and trailing silence from the audio sample array.
    /// For multi-channel audio the average amplitude of each frame is used.
    /// </summary>
    /// <param name="samples">Audio sample array (interleaved if multi-channel).</param>
    /// <param name="threshold">Amplitude threshold under which audio is considered silent.</param>
    /// <param name="channels">Number of audio channels.</param>
    /// <returns>A new float array with silence trimmed.</returns>
    private float[] TrimSilence(float[] samples, float threshold, int channels)
    {
        if (channels <= 1)
        {
            int startIndex = 0;
            int endIndex = samples.Length - 1;

            // Find first sample above the threshold.
            while (startIndex < samples.Length && Mathf.Abs(samples[startIndex]) < threshold)
            {
                startIndex++;
            }
            // Find last sample above the threshold.
            while (endIndex > startIndex && Mathf.Abs(samples[endIndex]) < threshold)
            {
                endIndex--;
            }

            int newLength = endIndex - startIndex + 1;
            if (newLength <= 0)
            {
                return new float[0];
            }

            float[] trimmedSamples = new float[newLength];
            Array.Copy(samples, startIndex, trimmedSamples, 0, newLength);
            return trimmedSamples;
        }
        else
        {
            // For multi-channel audio, process the data frame by frame.
            int totalFrames = samples.Length / channels;
            int startFrame = 0;
            int endFrame = totalFrames - 1;

            // Find the first frame where the average amplitude exceeds the threshold.
            while (startFrame < totalFrames)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                {
                    sum += Mathf.Abs(samples[startFrame * channels + c]);
                }
                if ((sum / channels) >= threshold)
                    break;
                startFrame++;
            }

            // Find the last frame where the average amplitude exceeds the threshold.
            while (endFrame > startFrame)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                {
                    sum += Mathf.Abs(samples[endFrame * channels + c]);
                }
                if ((sum / channels) >= threshold)
                    break;
                endFrame--;
            }

            int newFrameCount = endFrame - startFrame + 1;
            if (newFrameCount <= 0)
            {
                return new float[0];
            }

            float[] trimmedSamples = new float[newFrameCount * channels];
            Array.Copy(samples, startFrame * channels, trimmedSamples, 0, trimmedSamples.Length);
            return trimmedSamples;
        }
    }

    public void StartRecording()
    {
        // Use "MacBook Pro Microphone" if available; otherwise use the default device.
        _device = Microphone.devices.Contains("MacBook Pro Microphone") ? "MacBook Pro Microphone" : Microphone.devices[0];
        Debug.Log("Microphone device: " + _device);
        int sampleRate = 44100;
        int lengthSec = 40;

        recordedClip = Microphone.Start(_device, false, lengthSec, sampleRate);
        Debug.Log("Starting Recording ... ");
    }

    public void StopRecording()
    {
        Debug.Log("Stopping Recording ... ");
        Microphone.End(null);
        // Save the recorded clip to the audio source.
        audioSource.clip = recordedClip;
        StartCoroutine(ProcessRecording());
    }

    private IEnumerator ProcessRecording()
    {
        byte[] audioData = ConvertAudioClipToWav(recordedClip);
        Debug.Log("Processing Recording ... ");
        yield return StartCoroutine(taskCreator.ProcessAudioToTask(audioData));
    }

    public void PlayRecording()
    {
        if (audioSource == null || recordedClip == null)
        {
            Debug.LogError("AudioSource or RecordedClip is null!");
            return;
        }

        Debug.Log($"AudioClip length: {recordedClip.length} seconds");
        Debug.Log($"AudioClip samples: {recordedClip.samples}");
        Debug.Log($"AudioSource volume: {audioSource.volume}");
        Debug.Log($"AudioSource is playing: {audioSource.isPlaying}");

        audioSource.Play();
    }

    private bool isRecording = false;

    void Update()
    {
        // Start recording when the R key is pressed or a touch begins (and the tilemap is placed).
        if (placeTilemapOnPlane.tilemapPlaced && (Input.GetKeyDown(KeyCode.R) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
        {
            StartRecording();
            isRecording = true;
            ShowRecordingIndicator();
        }

        // Stop recording when the R key is released or the touch ends.
        if (Input.GetKeyUp(KeyCode.R) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
        {
            if (isRecording)
            {
                StopRecording();
                isRecording = false;
                HideRecordingIndicator();
            }
        }

        // Play recording when the P key is pressed.
        if (Input.GetKeyDown(KeyCode.P))
        {
            PlayRecording();
        }
    }

    private void ShowRecordingIndicator()
    {
        if (recordingIndicator != null)
        {
            recordingIndicator.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Recording indicator not assigned!");
        }
    }

    private void HideRecordingIndicator()
    {
        if (recordingIndicator != null)
        {
            recordingIndicator.SetActive(false);
        }
    }
}