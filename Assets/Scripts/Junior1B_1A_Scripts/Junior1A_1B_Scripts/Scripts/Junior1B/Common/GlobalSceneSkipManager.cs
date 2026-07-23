using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class GlobalSceneSkipManager : MonoBehaviour
{
    [Header("=== UI Component ===")]
    [SerializeField] private Button _skipButton;

    [Header("=== Tracking (Read Only) ===")]
    [SerializeField] private MonoBehaviour _activeSpeakingScript;
    [SerializeField] private int _failedAttemptsCount = 0;

    private int _lastCheckedQuestionIndex = -1;

    private void Awake()
    {
        if (_skipButton == null) _skipButton = GetComponent<Button>();

        if (_skipButton != null)
        {
            _skipButton.onClick.RemoveListener(OnSkipClicked);
            _skipButton.onClick.AddListener(OnSkipClicked);
            _skipButton.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Subscribe to the exact same speech event your 20 unit scripts use!
        CrossPlatformSpeechManager.OnResultStatic += OnGlobalSpeechInputCaptured;
    }

    private void OnDisable()
    {
        CrossPlatformSpeechManager.OnResultStatic -= OnGlobalSpeechInputCaptured;
        if (_skipButton != null) _skipButton.onClick.RemoveListener(OnSkipClicked);
    }

    private void Update()
    {
        FindActiveSpeakingScript();
    }

    private void FindActiveSpeakingScript()
    {
        if (_activeSpeakingScript != null && (!_activeSpeakingScript.gameObject.activeInHierarchy || !_activeSpeakingScript.enabled))
        {
            _activeSpeakingScript = null;
        }

        if (_activeSpeakingScript == null)
        {
            // Find all behaviours including inactive objects in the scene tree structure
            var managers = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var mono in managers)
            {
                if (mono.gameObject.activeInHierarchy && mono.enabled && mono != this)
                {
                    bool isValidInterface = (mono.GetType().GetInterface("Interfaces_Junior1A") != null) ||
                                            (mono.GetType().GetInterface("Interfaces_Junior1B") != null);

                    if (isValidInterface)
                    {
                        _activeSpeakingScript = mono;
                        ResetTracker();
                        break;
                    }
                }
            }
        }
    }

    // This fires automatically whenever the mic processes a spoken sentence!
    private void OnGlobalSpeechInputCaptured(string spokenText)
    {
        if (_activeSpeakingScript == null) return;

        Type scriptType = _activeSpeakingScript.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // 1. Double check our index sync state
        FieldInfo indexField = scriptType.GetField("_currentAudioIndex", flags);
        if (indexField != null)
        {
            int currentAudioIndex = (int)indexField.GetValue(_activeSpeakingScript);
            if (currentAudioIndex != _lastCheckedQuestionIndex)
            {
                _lastCheckedQuestionIndex = currentAudioIndex;
                _failedAttemptsCount = 0; // Fresh question, clear old failures
            }
        }

        // 2. Fetch the target answers matrix array and current score thresholds from your active script
        FieldInfo answerTextField = scriptType.GetField("_answerText", flags);
        FieldInfo indexCheckField = scriptType.GetField("_currentAudioIndex", flags);
        FieldInfo thresholdField = scriptType.GetField("passThreshold", flags);

        if (answerTextField != null && indexCheckField != null && thresholdField != null)
        {
            string[] answers = (string[])answerTextField.GetValue(_activeSpeakingScript);
            int idx = (int)indexCheckField.GetValue(_activeSpeakingScript);
            float threshold = (float)thresholdField.GetValue(_activeSpeakingScript);

            if (answers != null && idx < answers.Length)
            {
                // Run the mathematical similarity analysis directly using your exact formula structure
                float score = SimilarityPercent(answers[idx], spokenText);

                // If the accuracy is below 75%, increment the failure loop!
                if (score < threshold)
                {
                    _failedAttemptsCount++;
                    Debug.Log($"<color=red>❌ Global Failure Counted!</color> Current score: {score * 100}%, Failed Attempts: {_failedAttemptsCount}");

                    if (_failedAttemptsCount >= 2)
                    {
                        RevealSkipButtonUI();
                    }
                }
                else
                {
                    // Player answered correctly! Reset failure counts for safety
                    _failedAttemptsCount = 0;
                    if (_skipButton != null) _skipButton.gameObject.SetActive(false);
                }
            }
        }
    }

    private void RevealSkipButtonUI()
    {
        if (_skipButton != null && !_skipButton.gameObject.activeSelf)
        {
            _skipButton.gameObject.SetActive(true);

            // Execute layout animations
            if (_skipButton.TryGetComponent(out Popeffect_Junior1B popB))
            {
                popB.enabled = false;
                popB.enabled = true;
            }
            else if (_skipButton.TryGetComponent(out PopEffect_Junior1A popA))
            {
                popA.enabled = false;
                popA.enabled = true;
            }
        }
    }

    private void OnSkipClicked()
    {
        if (_activeSpeakingScript == null) return;

        Type scriptType = _activeSpeakingScript.GetType();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Emulate a click on your native local next button to skip cleanly
        MethodInfo skipMethod = scriptType.GetMethod("OnLocalNextButtonPressed", flags);

        if (skipMethod != null)
        {
            skipMethod.Invoke(_activeSpeakingScript, null);
        }
        else
        {
            // Direct array loop fallback jump tracking code matrix fallback
            FieldInfo indexField = scriptType.GetField("_currentAudioIndex", flags);
            FieldInfo clipsField = scriptType.GetField("_questionClips", flags);

            if (indexField != null && clipsField != null)
            {
                int currentIdx = (int)indexField.GetValue(_activeSpeakingScript);
                AudioClip[] totalClips = (AudioClip[])clipsField.GetValue(_activeSpeakingScript);

                if (totalClips != null && currentIdx < totalClips.Length - 1)
                {
                    indexField.SetValue(_activeSpeakingScript, currentIdx + 1);
                    scriptType.GetMethod("ShowTargetWord", flags)?.Invoke(_activeSpeakingScript, null);
                }
            }
        }

        ResetTracker();
    }

    public void ResetTracker()
    {
        _failedAttemptsCount = 0;
        _lastCheckedQuestionIndex = -1;
        if (_skipButton != null) _skipButton.gameObject.SetActive(false);
    }

    // --- LEVENSHTEIN ACCURACY REPLICATION IN MANAGERS LAYER ---
    private float SimilarityPercent(string reference, string hypothesis)
    {
        string a = Normalize(reference);
        string b = Normalize(hypothesis);
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
        int dist = Levenshtein(a, b);
        return 1f - (float)dist / Mathf.Max(a.Length, b.Length);
    }

    private string Normalize(string s)
    {
        return System.Text.RegularExpressions.Regex.Replace(s.Trim().ToLowerInvariant(), @"[^a-z0-9\s]", "");
    }

    private int Levenshtein(string s, string t)
    {
        int n = s.Length, m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}