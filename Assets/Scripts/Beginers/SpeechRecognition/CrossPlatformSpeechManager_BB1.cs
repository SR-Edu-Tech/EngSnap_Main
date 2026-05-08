using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

public class CrossPlatformSpeechManager_BB1 : MonoBehaviour
{
    // ── Inspector Events ──────────────────────────────────────────────────────
    [Header("Speech Events")]
    [Tooltip("Fired with the final transcribed text.")]
    public UnityEvent<string> onResult;

    [Tooltip("Fired with partial (live) transcription while the user is speaking.")]
    public UnityEvent<string> onPartial;

    [Tooltip("Fired when the microphone opens and is ready.")]
    public UnityEvent onReady;

    [Tooltip("Fired when the user starts speaking.")]
    public UnityEvent onBegin;

    [Tooltip("Fired when the user stops speaking and STT is processing.")]
    public UnityEvent onEnd;

    [Tooltip("Fired with an error message string.")]
    public UnityEvent<string> onError;

    // ── Static C# Events (subscribe from other scripts) ──────────────────────
    /// <summary>Subscribe here to receive final transcription results.</summary>
    public static event Action<string> OnResultStatic;

    /// <summary>Subscribe here to receive partial live results.</summary>
    public static event Action<string> OnPartialStatic;

    /// <summary>Fires when mic is open and ready.</summary>
    public static event Action OnReadyStatic;

    /// <summary>
    /// Fires when a playable AudioClip has been built and stored.
    /// Logic.cs must enable the Play Recording button ONLY here — NOT in HandleResult —
    /// because on Android the clip arrives asynchronously after the text result.
    /// </summary>
    public static event Action OnRecordingReadyStatic;

    // ── Singleton ─────────────────────────────────────────────────────────────
    public static CrossPlatformSpeechManager_BB1 Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _isListening    = false;
    public  bool IsListening    => _isListening;
    private bool _resultReceived = false;

    // ── Recording Playback ────────────────────────────────────────────────────
    [Header("Recording Playback")]
    [Tooltip("AudioSource used to play back the player's last recording.")]
    public AudioSource playbackAudioSource;

    private AudioClip _lastRecordingClip = null;
    public  bool HasRecording => _lastRecordingClip != null;

    // ── Unity Microphone capture (Windows / iOS only) ─────────────────────────
    // IMPORTANT: Microphone.Start() must NEVER be called while Android's
    // SpeechRecognizer is active — it holds an exclusive hardware mic lock and
    // calling Microphone.Start() simultaneously will silently kill STT.
    // On Android, audio comes from onBufferReceived PCM via OnSpeechAudioReady.
    private string    _micDevice        = null;
    private AudioClip _activeRecording  = null;
    private int       _sampleRate       = 16000;
    private int       _maxRecordSeconds = 10;

    // Android PCM constants — must match SpeechRecognizer output (16 kHz mono 16-bit LE).
    private const int AndroidSampleRate = 16000;
    private const int AndroidChannels   = 1;

    // ── Platform-specific fields ──────────────────────────────────────────────

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private DictationRecognizer _dictation;
    private bool _windowsSTTAvailable = false;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _androidPlugin;
#endif

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void STT_Init(string gameObjectName);
    [DllImport("__Internal")] static extern void STT_RequestPermission();
    [DllImport("__Internal")] static extern void STT_StartListening();
    [DllImport("__Internal")] static extern void STT_StopListening();
    [DllImport("__Internal")] static extern void STT_Destroy();
#endif

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Only grab a mic device on platforms that use Unity's Microphone API.
        // Do NOT touch Microphone on Android — conflicts with SpeechRecognizer.
#if !UNITY_ANDROID || UNITY_EDITOR
        _micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
#endif

        if (playbackAudioSource == null)
            playbackAudioSource = gameObject.AddComponent<AudioSource>();

        InitPlatform();
    }

    void OnDestroy() => DestroyPlatform();

    // ── Platform Init ─────────────────────────────────────────────────────────

    void InitPlatform()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        InitWindows();
#elif UNITY_ANDROID && !UNITY_EDITOR
        InitAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
        InitIOS();
#else
        Debug.Log("[STT] Editor (non-Windows) — STT simulated. Build to device to test.");
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    void InitWindows()
    {
        if (PhraseRecognitionSystem.Status == SpeechSystemStatus.Failed)
        {
            Debug.LogWarning("[STT] Windows Speech Recognition system is not available.");
            return;
        }
        try
        {
            _dictation = new DictationRecognizer();
            _dictation.DictationResult     += (text, _) => OnSpeechResult(text);
            _dictation.DictationHypothesis += (text)    => OnSpeechPartial(text);
            _dictation.DictationComplete   += (_)        => { _isListening = false; onEnd?.Invoke(); };
            _dictation.DictationError      += (err, _)   => OnSpeechError(err);
            _windowsSTTAvailable = true;
            Debug.Log("[STT] Windows DictationRecognizer ready.");
        }
        catch (Exception e)
        {
            _windowsSTTAvailable = false;
            Debug.LogWarning("[STT] Windows DictationRecognizer init failed: " + e.Message);
        }
    }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    void InitAndroid()
    {
        using var cls = new AndroidJavaClass("com.yourgame.speech.SpeechPlugin");
        _androidPlugin = cls.CallStatic<AndroidJavaObject>("getInstance");
        _androidPlugin.Call("init", gameObject.name, "OnSpeechResult");
        Debug.Log("[STT] Android SpeechRecognizer ready.");
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    void InitIOS()
    {
        STT_Init(gameObject.name);
        STT_RequestPermission();
        Debug.Log("[STT] iOS SFSpeechRecognizer ready.");
    }
#endif

    void DestroyPlatform()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        _dictation?.Dispose();
#elif UNITY_ANDROID && !UNITY_EDITOR
        _androidPlugin?.Call("destroy");
#elif UNITY_IOS && !UNITY_EDITOR
        STT_Destroy();
#endif
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void StartListening()
    {
        if (_isListening) return;
        _isListening    = true;
        _resultReceived = false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        StartMicCapture();

        if (!_windowsSTTAvailable || _dictation == null)
        {
            _isListening = false;
            string msg = "Windows Speech Recognition is disabled.\n\n" +
                         "To fix: Settings → Privacy & Security → Speech\n" +
                         "Turn ON 'Online speech recognition', then restart Unity.";
            Debug.LogWarning("[STT] " + msg);
            OnSpeechError(msg);
            return;
        }
        try
        {
            if (_dictation.Status != SpeechSystemStatus.Running)
                _dictation.Start();
            onReady?.Invoke();
            OnReadyStatic?.Invoke();
        }
        catch (Exception e)
        {
            _isListening = false;
            string msg = "Could not start speech recognition: " + e.Message +
                         "\nEnable 'Online speech recognition' in Windows Settings → Privacy & Security → Speech.";
            Debug.LogWarning("[STT] " + msg);
            OnSpeechError(msg);
        }

#elif UNITY_ANDROID && !UNITY_EDITOR
        // Do NOT call StartMicCapture() here.
        // Android's SpeechRecognizer holds an exclusive mic lock — calling
        // Microphone.Start() simultaneously will silently break STT.
        // Audio arrives via onBufferReceived PCM → OnSpeechAudioReady.
        _androidPlugin?.Call("startListening");

#elif UNITY_IOS && !UNITY_EDITOR
        StartMicCapture();
        STT_StartListening();

#else
        Debug.Log("[STT] StartListening() — build to device to test.");
        _isListening = false;
#endif
    }

    public void StopListening()
    {
        _isListening = false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        StopMicCapture();
        if (_dictation.Status == SpeechSystemStatus.Running)
            _dictation.Stop();

#elif UNITY_ANDROID && !UNITY_EDITOR
        // Do NOT call StopMicCapture() — Unity's Microphone was never started on Android.
        _androidPlugin?.Call("stopListening");

#elif UNITY_IOS && !UNITY_EDITOR
        StopMicCapture();
        STT_StopListening();
#endif
    }

    public void ToggleListening()
    {
        if (_isListening) StopListening();
        else              StartListening();
    }

    // ── Recording Playback API ────────────────────────────────────────────────

    public void PlayLastRecording()
    {
        if (_lastRecordingClip == null)
        {
            Debug.LogWarning("[STT] No recording to play back yet.");
            return;
        }
        playbackAudioSource.Stop();
        playbackAudioSource.clip = _lastRecordingClip;
        playbackAudioSource.loop = false;
        playbackAudioSource.Play();
        Debug.Log("[STT] Playing last recording.");
    }

    public void ClearLastRecording()
    {
        _lastRecordingClip = null;

#if !UNITY_ANDROID || UNITY_EDITOR
        if (_activeRecording != null)
        {
            Microphone.End(_micDevice);
            _activeRecording = null;
        }
#endif
    }

    // ── Unity Microphone capture (Windows / iOS only) ─────────────────────────

    private void StartMicCapture()
    {
        if (_micDevice == null) return;
        _activeRecording = Microphone.Start(_micDevice, false, _maxRecordSeconds, _sampleRate);
    }

    private void StopMicCapture()
    {
        if (_micDevice == null || _activeRecording == null) return;

        int pos = Microphone.GetPosition(_micDevice);
        Microphone.End(_micDevice);

        if (pos > 0)
        {
            float[] data = new float[pos * _activeRecording.channels];
            _activeRecording.GetData(data, 0);
            _lastRecordingClip = AudioClip.Create("lastRecording", pos,
                _activeRecording.channels, _activeRecording.frequency, false);
            _lastRecordingClip.SetData(data, 0);

            // Clip is ready — tell Logic.cs to enable the play button
            OnRecordingReadyStatic?.Invoke();
            Debug.Log($"[STT] Recording clip ready: {pos} samples.");
        }

        _activeRecording = null;
    }

    // ── Callbacks from native plugins (UnitySendMessage) ─────────────────────

    /// <summary>
    /// Called by Java SpeechPlugin after onResults.
    /// payload = Base64 16-bit LE PCM at 16 kHz mono,
    /// or "" if the device never fired onBufferReceived.
    /// </summary>
    void OnSpeechAudioReady(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            // Device doesn't supply onBufferReceived — play button stays disabled.
            // We cannot safely record with Unity's Microphone while SpeechRecognizer is active.
            Debug.LogWarning("[STT] Device did not provide audio buffers. " +
                             "Play Recording is unavailable on this device.");
            return;
        }

        try
        {
            // 1. Base64 → raw bytes (16-bit signed LE PCM)
            byte[] pcmBytes = Convert.FromBase64String(payload);

            // 2. Byte pairs → float samples [-1, 1]
            int sampleCount = pcmBytes.Length / 2;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short raw = (short)(pcmBytes[i * 2] | (pcmBytes[i * 2 + 1] << 8));
                samples[i] = raw / 32768f;
            }

            // 3. Build AudioClip
            _lastRecordingClip = AudioClip.Create(
                "lastRecording", sampleCount, AndroidChannels, AndroidSampleRate, false);
            _lastRecordingClip.SetData(samples, 0);

            Debug.Log($"[STT] Android recording ready: {sampleCount} samples " +
                      $"({sampleCount / (float)AndroidSampleRate:F2}s)");

            // 4. Clip ready — notify Logic.cs to enable the play button NOW
            OnRecordingReadyStatic?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("[STT] Failed to decode Android audio buffer: " + e.Message);
        }
    }

    void OnSpeechResult(string result)
    {
        _isListening    = false;
        _resultReceived = true;
        onResult?.Invoke(result);
        OnResultStatic?.Invoke(result);
        // Do NOT enable play button here — clip may not exist yet on Android.
        // Logic.cs subscribes to OnRecordingReadyStatic for that.
    }

    void OnSpeechPartial(string partial)
    {
        // Guard: some Android devices send a stale partial after the final result.
        if (_resultReceived) return;
        onPartial?.Invoke(partial);
        OnPartialStatic?.Invoke(partial);
    }

    void OnSpeechReady(string _)  { onReady?.Invoke(); OnReadyStatic?.Invoke(); }
    void OnSpeechBegin(string _)  => onBegin?.Invoke();
    void OnSpeechEnd(string _)    { _isListening = false; onEnd?.Invoke(); }

    void OnSpeechError(string error)
    {
        _isListening = false;
        Debug.LogWarning("[STT] Error: " + error);
        onError?.Invoke(error);
    }

    void OnPermissionGranted(string _) => Debug.Log("[STT] Permission granted.");
}