using System;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class CrossPlatformSpeechManager_junior : MonoBehaviour
{
    public UnityEvent<string> onResult;
    public UnityEvent<string> onPartial;
    public UnityEvent onReady;
    public UnityEvent onBegin;
    public UnityEvent onEnd;
    public UnityEvent<string> onError;

    public static event Action<string> OnResultStatic;
    public static event Action<string> OnPartialStatic;
    public static event Action OnReadyStatic;

    public static CrossPlatformSpeechManager_junior Instance { get; private set; }

    public bool IsListening { get; private set; } = false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private DictationRecognizer _dictation;
    private bool _windowsSTTAvailable = false;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _androidPlugin;
#endif

#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void STT_Init(string gameObjectName);
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void STT_RequestPermission();
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void STT_StartListening();
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void STT_StopListening();
    [System.Runtime.InteropServices.DllImport("__Internal")] static extern void STT_Destroy();
#endif

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitPlatform();
    }

    void OnDestroy() => DestroyPlatform();

    void InitPlatform()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        InitWindows();
#elif UNITY_ANDROID && !UNITY_EDITOR
        InitAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
        InitIOS();
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    void InitWindows()
    {
        if (PhraseRecognitionSystem.Status == SpeechSystemStatus.Failed)
        {
            Debug.LogError("Windows Speech Recognition API failed to initialize (Status: Failed).");
            return;
        }

        try
        {
            _dictation = new DictationRecognizer();
            _dictation.DictationResult += (text, _) => OnSpeechResult(text);
            _dictation.DictationHypothesis += (text) => OnSpeechPartial(text);
            _dictation.DictationComplete += (_) => { IsListening = false; onEnd?.Invoke(); };
            _dictation.DictationError += (err, _) => OnSpeechError(err);
            _windowsSTTAvailable = true;
            Debug.Log("Windows Speech Recognition API connected successfully.");
        }
        catch (Exception e)
        {
            _windowsSTTAvailable = false;
            Debug.LogError($"Windows Speech Recognition API failed to connect: {e.Message}");
        }
    }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    void InitAndroid()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }

        try
        {
            using var cls = new AndroidJavaClass("com.yourgame.speech.SpeechPlugin");
            _androidPlugin = cls.CallStatic<AndroidJavaObject>("getInstance");
            _androidPlugin.Call("init", gameObject.name, "OnSpeechResult");
            Debug.Log("Android Speech API connected successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Android Speech API failed to connect: {e.Message}");
        }
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    void InitIOS()
    {
        STT_Init(gameObject.name);
        STT_RequestPermission();
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

    public void StartListening()
    {
        if (IsListening) return;
        IsListening = true;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!_windowsSTTAvailable || _dictation == null)
        {
            IsListening = false;
            Debug.LogError("Windows Speech API is NOT connected when trying to start listening.");
            OnSpeechError("Windows Speech Recognition disabled");
            return;
        }

        try
        {
            if (_dictation.Status != SpeechSystemStatus.Running)
                _dictation.Start();
            Debug.Log("Windows Speech API is connected and started listening.");
            onReady?.Invoke();
            OnReadyStatic?.Invoke();
        }
        catch (Exception e)
        {
            IsListening = false;
            Debug.LogError($"Windows Speech API encountered an error starting: {e.Message}");
            OnSpeechError(e.Message);
        }

#elif UNITY_ANDROID && !UNITY_EDITOR
        if (_androidPlugin != null)
        {
            Debug.Log("Android Speech API is connected and started listening.");
            _androidPlugin.Call("startListening");
        }
        else
        {
            Debug.LogError("Android Speech API is NOT connected when trying to start listening.");
        }

#elif UNITY_IOS && !UNITY_EDITOR
        STT_StartListening();
#else
        IsListening = false;
#endif
    }

    public void StopListening()
    {
        IsListening = false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (_dictation != null && _dictation.Status == SpeechSystemStatus.Running)
            _dictation.Stop();
#elif UNITY_ANDROID && !UNITY_EDITOR
        _androidPlugin?.Call("stopListening");
#elif UNITY_IOS && !UNITY_EDITOR
        STT_StopListening();
#endif
    }

    public void ToggleListening()
    {
        if (IsListening) StopListening();
        else StartListening();
    }

    [UnityEngine.Scripting.Preserve]
    public void OnSpeechResult(string result)
    {
        Debug.Log($"OnSpeechResult called with: {result}");
        IsListening = false;
        onResult?.Invoke(result);
        OnResultStatic?.Invoke(result);
    }

    [UnityEngine.Scripting.Preserve]
    public void OnSpeechPartial(string partial)
    {
        onPartial?.Invoke(partial);
        OnPartialStatic?.Invoke(partial);
    }

    [UnityEngine.Scripting.Preserve]
    public void OnSpeechReady(string _)
    {
        onReady?.Invoke();
        OnReadyStatic?.Invoke();
    }

    [UnityEngine.Scripting.Preserve]
    public void OnSpeechBegin(string _)
    {
        onBegin?.Invoke();
    }

    [UnityEngine.Scripting.Preserve]
    public void OnSpeechEnd(string _)
    {
        IsListening = false;
        onEnd?.Invoke();
    }

    [UnityEngine.Scripting.Preserve]
    public void OnSpeechError(string error)
    {
        Debug.LogError($"OnSpeechError called with: {error}");
        IsListening = false;
        onError?.Invoke(error);
    }

    [UnityEngine.Scripting.Preserve]
    public void OnPermissionGranted(string _)
    {
    }
}