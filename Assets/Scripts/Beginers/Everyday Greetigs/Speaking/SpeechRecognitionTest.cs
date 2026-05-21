// SpeechRecognitionTest.cs
// Updated: includes public static events OnRecognitionStart and OnRecognitionFinished
// Paste into Assets (overwrite existing file) and wire Inspector fields.

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
//using Whisper;
public class SpeechRecognitionTest : MonoBehaviour{}
/*{
    // Events to notify other scripts (WordMatchEvaluator subscribes to these)
  /*  public static event Action OnRecognitionStart;
    public static event Action<string> OnRecognitionFinished;

    [Header("UI")]
    public Text statusText;             // small UI text to show status/transcription
    public Button startButton;
    public Button stopButton;
    public Button playButton;

    [Header("Question Display")]
    [Tooltip("AudioSource used to play the question audio clip (separate from recording AudioSource)")]
    public AudioSource questionAudioSource;
    [Tooltip("Button that replays the current question audio")]
    public Button replayAudioButton;

    [Header("Audio / Recording")]
    public AudioSource audioSource;     // assign in Inspector (or created automatically)
    public int sampleRate = 16000;      // sample rate
    public int maxRecordSeconds = 10;   // max length of recording

    [Header("Hugging Face Router")]
    [Tooltip("Set your HF token here (do NOT commit token to repo).")]
    public string apiKey = "";
    [Tooltip("Router endpoint — default uses openai/whisper-large-v3. You can change model owner/name.")]
    public string apiUrl = "https://router.huggingface.co/hf-inference/models/openai/whisper-large-v3";

    // internal storage
    private string micDevice = null;
    private AudioClip recordingClip = null;  // the clip returned by Microphone.Start
    private AudioClip lastClip = null;       // trimmed clip of the last recording
    private byte[] lastWav = null;           // WAV bytes for the last recording
    private bool isRecording = false;

    [Header("Local Whisper")]
    public string modelFileName = "ggml-base.bin";

   // [SerializeField] private WhisperManager whisper;
    private bool whisperReady = false;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Wire buttons if not wired in Inspector (safe default)
        if (startButton != null) startButton.onClick.AddListener(StartRecording);
        if (stopButton != null) stopButton.onClick.AddListener(StopRecordingAndSend);
        if (playButton != null) playButton.onClick.AddListener(PlayLastRecording);
        if (replayAudioButton != null) replayAudioButton.onClick.AddListener(ReplayQuestionAudio);

        // choose default mic
        if (Microphone.devices.Length > 0)
            micDevice = Microphone.devices[0];
        else
            micDevice = null;

        UpdateStatus("Ready");
        if (stopButton != null) stopButton.interactable = false;

        string modelPath = Path.Combine(Application.streamingAssetsPath, modelFileName);

       // whisper = new WhisperManager(modelPath);
        whisperReady = true;

        Debug.Log("[Whisper] Loaded model: " + modelPath);
    }

    void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log("[SRT] " + msg);
    }

    // ── Question Loading ──────────────────────────────────────────────────────

    /// <summary>
    /// Called by SpeakingGameController to set the current question text and audio.
    /// Plays the question audio automatically when loaded.
    /// </summary>
    public void LoadQuestion(string questionText, AudioClip questionClip)
    {
        // Show text in statusText (or you can use a dedicated TMP label wired in SpeakingGameController)
        UpdateStatus(questionText);

        // Play question audio automatically
        if (questionAudioSource != null && questionClip != null)
        {
            questionAudioSource.Stop();
            questionAudioSource.clip = questionClip;
            questionAudioSource.Play();
        }

        // Enable replay button
        if (replayAudioButton != null)
            replayAudioButton.interactable = questionClip != null;

        // Reset recording state for fresh attempt
        if (isRecording)
        {
            Microphone.End(micDevice);
            isRecording = false;
        }

        if (startButton != null) startButton.interactable = true;
        if (stopButton != null)  stopButton.interactable  = false;
    }

    /// <summary>Replays the current question audio clip.</summary>
    public void ReplayQuestionAudio()
    {
        if (questionAudioSource != null && questionAudioSource.clip != null)
        {
            questionAudioSource.Stop();
            questionAudioSource.Play();
        }
    }

    #region Recording API

 public void StartRecording()
{
    if (isRecording)
    {
        Debug.LogWarning("[SRT] Already recording");
        return;
    }

    if (micDevice == null)
    {
        UpdateStatus("No microphone found");
        Debug.LogError("[SRT] Microphone.devices.Length == 0");
        return;
    }

    recordingClip = Microphone.Start(micDevice, false, maxRecordSeconds, sampleRate);
    isRecording = true;
    UpdateStatus("Recognizing."); // immediate user feedback
    Debug.Log("[SRT] Recording started on device: " + micDevice);

    if (startButton != null) startButton.interactable = false;
    if (stopButton != null) stopButton.interactable = true;

    // Notify listeners to reset their UI
    OnRecognitionStart?.Invoke();
}

public void StopRecordingAndSend()
{
    if (!isRecording)
    {
        Debug.LogWarning("[SRT] Stop called but not recording");
        return;
    }

    int pos = Microphone.GetPosition(micDevice);
    isRecording = false;
    Debug.Log("[SRT] Recording stopped. Samples recorded: " + pos);

    if (pos <= 0 || recordingClip == null)
    {
        UpdateStatus("No audio recorded");
        if (startButton != null) startButton.interactable = true;
        return;
    }

    // Trim to actual length
    lastClip = TrimClip(recordingClip, pos);
    lastWav = WavUtility.FromAudioClip(lastClip);

    UpdateStatus("Recorded " + (pos / (float)sampleRate).ToString("F2") + "s");

    // Start upload coroutine
    StartCoroutine(RunLocalWhisper(lastClip));

    if (startButton != null) startButton.interactable = true;
}

    private AudioClip TrimClip(AudioClip clip, int samples)
    {
        if (clip == null) return null;
        float[] data = new float[samples * clip.channels];
        clip.GetData(data, 0);
        AudioClip newClip = AudioClip.Create(clip.name + "_trimmed", samples, clip.channels, clip.frequency, false);
        newClip.SetData(data, 0);
        return newClip;
    }

    #endregion

    #region Playback / Save
private IEnumerator RunLocalWhisper(AudioClip clip)
    {
        //if (whisper == null || clip == null)
        {
            UpdateStatus("Whisper not ready");
            yield break;
        }

        UpdateStatus("Processing...");

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

      //  var task = whisper.GetTextAsync(samples, clip.frequency, clip.channels);

       // while (!task.IsCompleted)
            yield return null;

        if (task.Exception != null)
        {
            Debug.LogError("[Whisper] " + task.Exception);
            UpdateStatus("Recognition failed");
            yield break;
        }

        var result = task.Result;
        string transcript = result?.Result ?? "";

        if (string.IsNullOrWhiteSpace(transcript))
            transcript = "(no text)";

        UpdateStatus(transcript);

        OnRecognitionFinished?.Invoke(transcript);
    }
    public void PlayLastRecording()
    {
        if (lastClip == null)
        {
            UpdateStatus("No recording to play");
            Debug.LogWarning("[SRT] No lastClip to play");
            return;
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.Stop();
        audioSource.clip = lastClip;
        audioSource.loop = false;
        audioSource.Play();
        UpdateStatus("Playing last recording");
        Debug.Log("[SRT] Playing last recording");
    }

    public string SaveLastRecordingToDisk(string filename = "last_recording.wav")
    {
        if (lastWav == null || lastWav.Length == 0)
        {
            Debug.LogWarning("[SRT] No wav to save");
            UpdateStatus("No recording to save");
            return null;
        }

        try
        {
            string path = Path.Combine(Application.persistentDataPath, filename);
            File.WriteAllBytes(path, lastWav);
            Debug.Log("[SRT] Saved WAV to: " + path);
            UpdateStatus("Saved: " + filename);
            return path;
        }
        catch (Exception e)
        {
            Debug.LogError("[SRT] Failed to save WAV: " + e.Message);
            UpdateStatus("Save failed");
            return null;
        }
    }

    #endregion

    #region Hugging Face Upload

    private IEnumerator SendToHuggingFace(byte[] wavData)
    {
        if (wavData == null || wavData.Length == 0)
        {
            UpdateStatus("No audio to send");
            yield break;
        }

        // Force router pattern if inspector contains old api-inference
        if (string.IsNullOrEmpty(apiUrl))
            apiUrl = "https://router.huggingface.co/hf-inference/models/openai/whisper-large-v3";

        //if (apiUrl.Contains("api-inference.huggingface.co"))
        //{
        //    Debug.LogWarning("[SRT] Detected old api-inference URL; switching to router pattern");
        //    apiUrl = apiUrl.Replace("https://api-inference.huggingface.co/models/", "https://router.huggingface.co/hf-inference/models/");
        //}

        Debug.Log("[SRT] Using apiUrl = " + apiUrl);
        Debug.Log("[SRT] Sending " + wavData.Length + " bytes to " + apiUrl);

        UnityWebRequest www = new UnityWebRequest(apiUrl, "POST");
        www.uploadHandler = new UploadHandlerRaw(wavData);
        www.downloadHandler = new DownloadHandlerBuffer();

        if (string.IsNullOrEmpty(apiKey))
        {
            UpdateStatus("API key missing (set in Inspector)");
            Debug.LogError("[SRT] HF token missing. Set apiKey in Inspector.");
            yield break;
        }

        www.SetRequestHeader("Authorization", "Bearer " + apiKey);
        www.SetRequestHeader("Content-Type", "audio/wav");

        yield return www.SendWebRequest();

        long code = www.responseCode;
        string body = www.downloadHandler?.text ?? "";

        Debug.Log("[SRT] Router response code: " + code);
        Debug.Log("[SRT] Router body (first 1000 chars): " + (body.Length > 0 ? body.Substring(0, Math.Min(1000, body.Length)) : "<empty>"));

        if (www.result != UnityWebRequest.Result.Success)
        {
            if (code == 410)
            {
                UpdateStatus("Model unavailable (410). Try another model/provider.");
                Debug.LogError("[SRT] HTTP 410 from HF: model not available via this endpoint.");
            }
            else if (code == 401 || code == 403)
            {
                UpdateStatus("Auth error: check HF token");
                Debug.LogError("[SRT] Auth failure " + code + ". Check token and permissions.");
            }
            else
            {
                UpdateStatus("Network error: " + code);
                Debug.LogError("[SRT] Network error " + code + " | " + www.error);
            }
            yield break;
        }

        // success: parse transcript
        string transcript = TryParseTranscript(body);
        if (string.IsNullOrEmpty(transcript))
        {
            UpdateStatus("No transcription returned");
            Debug.LogWarning("[SRT] Transcription parsing failed; raw body: " + body);
            transcript = "(no text)";
        }
        else
        {
            UpdateStatus(transcript);
            Debug.Log("[SRT] Transcription: " + transcript);
        }

        // Notify listeners before updating UI (so they can react first)
        OnRecognitionFinished?.Invoke(transcript);
    }

    private string TryParseTranscript(string body)
    {
        if (string.IsNullOrEmpty(body)) return null;

        int idx = body.IndexOf("\"text\":", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            int start = body.IndexOf('"', idx + 7);
            if (start >= 0)
            {
                start += 1;
                int end = body.IndexOf('"', start);
                if (end > start)
                {
                    string raw = body.Substring(start, end - start);
                    return raw.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\"", "\"");
                }
            }
        }

        return body.Length > 1000 ? body.Substring(0, 1000) : body;
    }

    #endregion
}

/// <summary>
/// Minimal WAV utility: converts AudioClip (float samples) to 16-bit PCM WAV bytes.
/// </summary>
public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null) return null;
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        return ConvertAndWrite(samples, clip.channels, clip.frequency);
    }

    private static byte[] ConvertAndWrite(float[] samples, int channels, int sampleRate)
    {
        short[] intData = new short[samples.Length];
        byte[] bytesData = new byte[samples.Length * 2];
        const float rescaleFactor = 32767f;

        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * rescaleFactor);
            byte[] b = BitConverter.GetBytes(intData[i]);
            b.CopyTo(bytesData, i * 2);
        }

        int headerSize = 44;
        byte[] wav = new byte[headerSize + bytesData.Length];

        System.Text.Encoding.ASCII.GetBytes("RIFF").CopyTo(wav, 0);
        BitConverter.GetBytes(wav.Length - 8).CopyTo(wav, 4);
        System.Text.Encoding.ASCII.GetBytes("WAVE").CopyTo(wav, 8);
        System.Text.Encoding.ASCII.GetBytes("fmt ").CopyTo(wav, 12);
        BitConverter.GetBytes(16).CopyTo(wav, 16);
        BitConverter.GetBytes((short)1).CopyTo(wav, 20);
        BitConverter.GetBytes((short)channels).CopyTo(wav, 22);
        BitConverter.GetBytes(sampleRate).CopyTo(wav, 24);
        BitConverter.GetBytes(sampleRate * channels * 2).CopyTo(wav, 28);
        BitConverter.GetBytes((short)(channels * 2)).CopyTo(wav, 32);
        BitConverter.GetBytes((short)16).CopyTo(wav, 34);
        System.Text.Encoding.ASCII.GetBytes("data").CopyTo(wav, 36);
        BitConverter.GetBytes(bytesData.Length).CopyTo(wav, 40);
        bytesData.CopyTo(wav, 44);

        return wav;
    }
}*/