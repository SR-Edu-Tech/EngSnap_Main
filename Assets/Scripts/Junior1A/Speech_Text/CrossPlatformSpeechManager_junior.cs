using System;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
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
        if (PhraseRecognitionSystem.Status == SpeechSystemStatus.Failed) return;

        try
        {
            _dictation = new DictationRecognizer();
            _dictation.DictationResult += (text, _) => OnSpeechResult(text);
            _dictation.DictationHypothesis += (text) => OnSpeechPartial(text);
            _dictation.DictationComplete += (_) => { IsListening = false; onEnd?.Invoke(); };
            _dictation.DictationError += (err, _) => OnSpeechError(err);
            _windowsSTTAvailable = true;
        }
        catch
        {
            _windowsSTTAvailable = false;
        }
    }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    void InitAndroid()
    {
        using var cls = new AndroidJavaClass("com.yourgame.speech.SpeechPlugin");
        _androidPlugin = cls.CallStatic<AndroidJavaObject>("getInstance");
        _androidPlugin.Call("init", gameObject.name, "OnSpeechResult");
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
            OnSpeechError("Windows Speech Recognition disabled");
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
            IsListening = false;
            OnSpeechError(e.Message);
        }

#elif UNITY_ANDROID && !UNITY_EDITOR
        _androidPlugin?.Call("startListening");

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

    public void OnSpeechResult(string result)
    {
        IsListening = false;
        onResult?.Invoke(result);
        OnResultStatic?.Invoke(result);
    }

    public void OnSpeechPartial(string partial)
    {
        onPartial?.Invoke(partial);
        OnPartialStatic?.Invoke(partial);
    }

    public void OnSpeechReady(string _)
    {
        onReady?.Invoke();
        OnReadyStatic?.Invoke();
    }

    public void OnSpeechBegin(string _) => onBegin?.Invoke();

    public void OnSpeechEnd(string _)
    {
        IsListening = false;
        onEnd?.Invoke();
    }

    public void OnSpeechError(string error)
    {
        IsListening = false;
        onError?.Invoke(error);
    }

    public void OnPermissionGranted(string _) { }
}