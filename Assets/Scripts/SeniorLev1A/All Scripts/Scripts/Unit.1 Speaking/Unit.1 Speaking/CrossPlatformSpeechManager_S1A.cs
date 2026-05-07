// ── CrossPlatformSpeechManager_S1A.cs ────────────────────────────────────────────
// Single unified speech-to-text manager for Windows, Android, and iOS.
//
// SETUP:
//  1. Create an empty GameObject named exactly "SpeechManager" in your scene.
//  2. Attach this script to it.
//  3. Wire up the UnityEvents in the Inspector, OR subscribe to the static
//     events from any other script.
//  4. Call StartListening() / StopListening() / ToggleListening() from a button.
// ─────────────────────────────────────────────────────────────────────────────

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

public class CrossPlatformSpeechManager_S1A : MonoBehaviour
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
    public static event System.Action<string> OnResultStatic;

    /// <summary>Subscribe here to receive partial live results.</summary>
    public static event System.Action<string> OnPartialStatic;

    /// <summary>Fires when mic is open and ready.</summary>
    public static event System.Action OnReadyStatic;

    // ── Singleton ─────────────────────────────────────────────────────────────
    public static CrossPlatformSpeechManager_S1A Instance { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _isListening = false;
    public bool IsListening => _isListening;

    // ── Platform-specific fields ──────────────────────────────────────────────

    // Windows
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private DictationRecognizer _dictation;
#endif

    // Android
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _androidPlugin;
#endif

    // iOS — P/Invoke to our SpeechPlugin.mm
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
        Debug.Log("[STT] Running in Editor (non-Windows). STT is simulated — build to device to test.");
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private bool _windowsSTTAvailable = false;

    void InitWindows()
    {
        // Check if Windows online speech recognition is available before creating
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
        catch (System.Exception e)
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

    /// <summary>Start listening for speech input.</summary>
    public void StartListening()
    {
        if (_isListening) return;
        _isListening = true;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
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
        catch (System.Exception e)
        {
            _isListening = false;
            string msg = "Could not start speech recognition: " + e.Message +
                         "\nEnable 'Online speech recognition' in Windows Settings → Privacy & Security → Speech.";
            Debug.LogWarning("[STT] " + msg);
            OnSpeechError(msg);
        }

#elif UNITY_ANDROID && !UNITY_EDITOR
        _androidPlugin?.Call("startListening");

#elif UNITY_IOS && !UNITY_EDITOR
        STT_StartListening();

#else
        // Editor simulation
        Debug.Log("[STT] StartListening() called — build to Android/iOS/Windows to test.");
        _isListening = false;
#endif
    }

    /// <summary>Stop listening (force-stop before the user finishes).</summary>
    public void StopListening()
    {
        _isListening = false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (_dictation.Status == SpeechSystemStatus.Running)
            _dictation.Stop();

#elif UNITY_ANDROID && !UNITY_EDITOR
        _androidPlugin?.Call("stopListening");

#elif UNITY_IOS && !UNITY_EDITOR
        STT_StopListening();
#endif
    }

    /// <summary>Toggle start/stop — ideal for a single push-to-talk button.</summary>
    public void ToggleListening()
    {
        if (_isListening) StopListening();
        else              StartListening();
    }

    // ── Callbacks (from native plugins via UnitySendMessage + Windows events) ─
    // These method names must stay public / match what's passed to init().

    void OnSpeechResult(string result)
    {
        _isListening = false;
        //Debug.Log("[STT] Result: " + result);
        onResult?.Invoke(result);
        OnResultStatic?.Invoke(result);
    }

    void OnSpeechPartial(string partial)
    {
        onPartial?.Invoke(partial);
        OnPartialStatic?.Invoke(partial);
    }

    void OnSpeechReady(string _)
    {
        onReady?.Invoke();
        OnReadyStatic?.Invoke();
    }

    void OnSpeechBegin(string _) => onBegin?.Invoke();

    void OnSpeechEnd(string _)
    {
        _isListening = false;
        onEnd?.Invoke();
    }

    void OnSpeechError(string error)
    {
        _isListening = false;
        Debug.LogWarning("[STT] Error: " + error);
        onError?.Invoke(error);
    }

    void OnPermissionGranted(string _) => Debug.Log("[STT] Permission granted.");
}
