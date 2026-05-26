using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Android;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

/// <summary>
/// ONE shared speech manager for the entire app.
/// All gameplay scripts (BB1, S1A, Junior, Masters) subscribe to the static events.
/// Only one instance ever exists — DontDestroyOnLoad keeps it alive across all scenes.
/// The Java SpeechPlugin singleton only ever gets one callback target: this GameObject.
/// </summary>
public class CrossPlatformSpeechManager : MonoBehaviour
{
    // ── Inspector Events ──────────────────────────────────────────────────────
    [Header("Speech Events")]
    public UnityEvent<string> onResult;
    public UnityEvent<string> onPartial;
    public UnityEvent         onReady;
    public UnityEvent         onBegin;
    public UnityEvent         onEnd;
    public UnityEvent<string> onError;

    // ── Static C# Events — subscribe from any gameplay script ────────────────
    public static event Action<string> OnResultStatic;
    public static event Action<string> OnPartialStatic;
    public static event Action         OnReadyStatic;
    public static event Action         OnEndStatic;
    public static event Action         OnRecordingReadyStatic;

    // ── Singleton ─────────────────────────────────────────────────────────────
    public static CrossPlatformSpeechManager Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _isListening    = false;
    public  bool IsListening     => _isListening;
    private bool _resultReceived = false;

    // ── Android permission / plugin state ─────────────────────────────────────
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _androidPlugin;
    private bool _pluginReady        = false;
    private bool _permissionRequested = false;
    private bool _pendingStart       = false;
#endif

    // ── Recording Playback ────────────────────────────────────────────────────
    [Header("Recording Playback")]
    public AudioSource playbackAudioSource;

    private AudioClip _lastRecordingClip = null;
    public  bool HasRecording => _lastRecordingClip != null;

    // ── Unity Microphone (Windows / iOS only) ─────────────────────────────────
    private string    _micDevice        = null;
    private AudioClip _activeRecording  = null;
    private int       _sampleRate       = 16000;
    private int       _maxRecordSeconds = 10;

    private const int AndroidSampleRate = 16000;
    private const int AndroidChannels   = 1;

    // ── Windows ───────────────────────────────────────────────────────────────
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private DictationRecognizer _dictation;
    private bool _windowsSTTAvailable = false;
#endif

    // ── iOS ───────────────────────────────────────────────────────────────────
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

#if !UNITY_ANDROID || UNITY_EDITOR
        _micDevice = Microphone.devices.Length > 0 ? Microphone.devices[0] : null;
#endif

        if (playbackAudioSource == null)
            playbackAudioSource = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        InitPlatform();
    }

    void OnDestroy() => DestroyPlatform();

    // ── Re-init when app regains focus (handles returning from another lesson) ─
    void OnApplicationFocus(bool hasFocus)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (hasFocus && _pluginReady)
        {
            Debug.Log("[STT] App regained focus — reinitializing Android recognizer.");
            InitAndroidPlugin();
        }
#endif
    }

    // ── Platform Init ─────────────────────────────────────────────────────────

    void InitPlatform()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        InitWindows();
#elif UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(InitAndroidWithPermission());
#elif UNITY_IOS && !UNITY_EDITOR
        InitIOS();
#else
        Debug.Log("[STT] Editor (non-Windows) — STT simulated. Build to device to test.");
#endif
    }

    // ── Android ───────────────────────────────────────────────────────────────

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator InitAndroidWithPermission()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            _permissionRequested = true;
            Debug.Log("[STT] Requesting RECORD_AUDIO permission...");
            var cb = new PermissionCallbacks();
            cb.PermissionGranted += p => Debug.Log("[STT] Permission granted: " + p);
            cb.PermissionDenied  += p => Debug.LogWarning("[STT] Permission denied: " + p);
            Permission.RequestUserPermission(Permission.Microphone, cb);

            float waited = 0f;
            while (!Permission.HasUserAuthorizedPermission(Permission.Microphone) && waited < 30f)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Debug.LogWarning("[STT] RECORD_AUDIO denied. Speech will not work.");
            OnSpeechError("Microphone permission denied. Allow it in device Settings → Apps → Permissions.");
            yield break;
        }

        // Check recognition service availability
        bool serviceAvailable = false;
        try
        {
            using var ctx = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                                .GetStatic<AndroidJavaObject>("currentActivity");
            using var cls = new AndroidJavaClass("com.yourgame.speech.SpeechPlugin");
            serviceAvailable = cls.CallStatic<bool>("isRecognitionAvailable", ctx);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[STT] isRecognitionAvailable check failed: " + e.Message);
            serviceAvailable = true; // assume available if check fails
        }

        if (!serviceAvailable)
        {
            Debug.LogWarning("[STT] No SpeechRecognizer service on this device.");
            OnSpeechError("Speech recognition unavailable. Please install Google app.");
            yield break;
        }

        InitAndroidPlugin();
    }

    void InitAndroidPlugin()
    {
        try
        {
            // Destroy old recognizer first — critical for switching between lessons
            if (_androidPlugin != null)
            {
                try { _androidPlugin.Call("destroy"); } catch { }
                _androidPlugin = null;
            }

            using var cls = new AndroidJavaClass("com.yourgame.speech.SpeechPlugin");
            _androidPlugin = cls.CallStatic<AndroidJavaObject>("getInstance");

            // This is the ONE place init() is called — this GameObject's name
            // is permanently registered as the callback target in the Java singleton.
            _androidPlugin.Call("init", gameObject.name, "OnSpeechResult");
            _pluginReady = true;
            Debug.Log("[STT] Android plugin ready. Callback target: " + gameObject.name);

            if (_pendingStart)
            {
                _pendingStart = false;
                StartListening();
            }
        }
        catch (Exception e)
        {
            _pluginReady = false;
            Debug.LogError("[STT] Android plugin init failed: " + e.Message);
            OnSpeechError("Speech plugin failed to initialize: " + e.Message);
        }
    }
#endif

    // ── Windows ───────────────────────────────────────────────────────────────

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    void InitWindows()
    {
        if (PhraseRecognitionSystem.Status == SpeechSystemStatus.Failed)
        {
            Debug.LogWarning("[STT] Windows Speech Recognition unavailable.");
            return;
        }
        try
        {
            _dictation = new DictationRecognizer();
            _dictation.DictationResult     += (text, _) => OnSpeechResult(text);
            _dictation.DictationHypothesis += (text)    => OnSpeechPartial(text);
            _dictation.DictationComplete   += (_)        => { _isListening = false; onEnd?.Invoke(); OnEndStatic?.Invoke(); };
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

    // ── iOS ───────────────────────────────────────────────────────────────────

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

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!_pluginReady)
        {
            Debug.Log("[STT] Plugin not ready — queuing StartListening.");
            _pendingStart = true;
            if (!_permissionRequested)
                StartCoroutine(InitAndroidWithPermission());
            return;
        }
        _isListening    = true;
        _resultReceived = false;
        _androidPlugin?.Call("startListening");

#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        StartMicCapture();
        if (!_windowsSTTAvailable || _dictation == null)
        {
            _isListening = false;
            OnSpeechError("Windows Speech Recognition is disabled.\nEnable it in Settings → Privacy & Security → Speech.");
            return;
        }
        _isListening    = true;
        _resultReceived = false;
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
            OnSpeechError("Could not start speech recognition: " + e.Message);
        }

#elif UNITY_IOS && !UNITY_EDITOR
        _isListening    = true;
        _resultReceived = false;
        StartMicCapture();
        STT_StartListening();

#else
        Debug.Log("[STT] StartListening() — build to device to test.");
#endif
    }

    public void StopListening()
    {
        _isListening = false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        StopMicCapture();
        if (_dictation != null && _dictation.Status == SpeechSystemStatus.Running)
            _dictation.Stop();
#elif UNITY_ANDROID && !UNITY_EDITOR
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

    // ── Recording Playback ────────────────────────────────────────────────────

    public void PlayLastRecording()
    {
        if (_lastRecordingClip == null) { Debug.LogWarning("[STT] No recording to play."); return; }
        playbackAudioSource.Stop();
        playbackAudioSource.clip = _lastRecordingClip;
        playbackAudioSource.loop = false;
        playbackAudioSource.Play();
    }

    public void ClearLastRecording()
    {
        _lastRecordingClip = null;
#if !UNITY_ANDROID || UNITY_EDITOR
        if (_activeRecording != null) { Microphone.End(_micDevice); _activeRecording = null; }
#endif
    }

    // ── Unity Microphone (Windows / iOS) ──────────────────────────────────────

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
            OnRecordingReadyStatic?.Invoke();
        }
        _activeRecording = null;
    }

    // ── Native Callbacks (UnitySendMessage target) ────────────────────────────

    [UnityEngine.Scripting.Preserve]
    void OnSpeechAudioReady(string payload)
    {
        if (string.IsNullOrEmpty(payload)) { Debug.LogWarning("[STT] No audio buffer from device."); return; }
        try
        {
            byte[]  pcmBytes    = Convert.FromBase64String(payload);
            int     sampleCount = pcmBytes.Length / 2;
            float[] samples     = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short raw = (short)(pcmBytes[i * 2] | (pcmBytes[i * 2 + 1] << 8));
                samples[i] = raw / 32768f;
            }
            _lastRecordingClip = AudioClip.Create("lastRecording", sampleCount,
                AndroidChannels, AndroidSampleRate, false);
            _lastRecordingClip.SetData(samples, 0);
            OnRecordingReadyStatic?.Invoke();
        }
        catch (Exception e) { Debug.LogError("[STT] Audio decode failed: " + e.Message); }
    }

    [UnityEngine.Scripting.Preserve]
    void OnSpeechResult(string result)
    {
        Debug.Log("[STT] Result: " + result);
        _isListening    = false;
        _resultReceived = true;
        onResult?.Invoke(result);
        OnResultStatic?.Invoke(result);
    }

    [UnityEngine.Scripting.Preserve]
    void OnSpeechPartial(string partial)
    {
        if (_resultReceived) return;
        onPartial?.Invoke(partial);
        OnPartialStatic?.Invoke(partial);
    }

    [UnityEngine.Scripting.Preserve]
    void OnSpeechReady(string _)
    {
        onReady?.Invoke();
        OnReadyStatic?.Invoke();
    }

    [UnityEngine.Scripting.Preserve]
    void OnSpeechBegin(string _) => onBegin?.Invoke();

    [UnityEngine.Scripting.Preserve]
    void OnSpeechEnd(string _)
    {
        _isListening = false;
        onEnd?.Invoke();
        OnEndStatic?.Invoke();
    }

    [UnityEngine.Scripting.Preserve]
    void OnSpeechError(string error)
    {
        _isListening = false;
        Debug.LogWarning("[STT] Error: " + error);
        onError?.Invoke(error);
    }

    [UnityEngine.Scripting.Preserve]
    void OnPermissionGranted(string _) => Debug.Log("[STT] Permission granted.");
}