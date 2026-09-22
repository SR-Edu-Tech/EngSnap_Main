using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controller for Unit 7 (Collocations) Writing Branch - Stage W01: Type the Missing Half.
/// Features 10 Cloze fill-in items on verbatim collocations with meaning clues,
/// case-insensitive whitespace-trimmed answer validation, multi-answer support (Item 8),
/// first-letter retry hint, 8/10 success condition, ARIA readback audio, and safe UI bindings.
/// </summary>
public class Masters_Collocations_Writing_LessonOne : Masters_PolishedCommunication_Writing_LessonOne {

    [System.Serializable]
    public class W01CollocationItem {
        public string promptText;           // e.g. "______ ready (to prepare yourself)"
        public string[] acceptedAnswers;    // e.g. ["get"] or ["electricity", "energy"]
        public string firstLetterHint;      // e.g. "g..."
        public string completedCollocation; // e.g. "get ready"
        public AudioClip readbackAudio;     // VO_W01_READBACK clip
    }

    [Header("W01 Collocation Data (10 Items)")]
    [SerializeField] private W01CollocationItem[] collocationItems;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI w01TitleTMP;
    [SerializeField] private TextMeshProUGUI w01HeaderTMP;
    [SerializeField] private TextMeshProUGUI w01InstructionTMP;
    [SerializeField] private TextMeshProUGUI w01PromptTMP;
    [SerializeField] private TextMeshProUGUI w01ProgressTMP;
    [SerializeField] private TextMeshProUGUI w01FeedbackTMP;
    [SerializeField] private TextMeshProUGUI w01HintTMP;
    [SerializeField] private TMP_InputField w01InputField;
    [SerializeField] private Button w01SubmitButton;

    [Header("Result & Navigation UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTMP;
    [SerializeField] private Button retryButton;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip ariaIntroAudio;  // VO_W01_ARIA clip
    [SerializeField] private AudioClip sfxSnap;         // SFX_MAGNET_SNAP clip

    [Header("Pass Threshold")]
    [SerializeField] private int passScore = 8;         // At least 8 of 10 items required to pass

    // Runtime state variables
    private int currentItemIndex = 0;
    private int correctCount = 0;
    private int attemptsOnCurrentItem = 0;
    private bool isCheckingAnswer = false;
#pragma warning disable 0414
    private bool itemHadErrors = false;
#pragma warning restore 0414

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;
        narratorSpeech = null;
        AutoFindUIReferences();
        Initialize10CollocationItems();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Writing;
        AutoFindUIReferences();
        Initialize10CollocationItems();
        UpdateTitleAndUIComponents();
        SetupUIBindings();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        currentItemIndex = 0;
        correctCount = 0;

        // Play intro ARIA voiceover
        PlayIntroVoiceover();

        LoadItem(0);
    }

    public void Initialize10CollocationItems() {
        if (collocationItems != null && collocationItems.Length >= 10) return;

        string audioDir = "Assets/Audio/2A/7_Collocations/Writing/";

        collocationItems = new W01CollocationItem[] {
            new W01CollocationItem {
                promptText = "______ ready (to prepare yourself)",
                acceptedAnswers = new string[] { "get" },
                firstLetterHint = "g...",
                completedCollocation = "get ready",
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get ready.mp3")
                #endif
            },
            new W01CollocationItem {
                promptText = "get ______ (to put on your clothes)",
                acceptedAnswers = new string[] { "dressed" },
                firstLetterHint = "d...",
                completedCollocation = "get dressed",
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get dressed.mp3")
                #endif
            },
            new W01CollocationItem {
                promptText = "get ______ soon (what you say to someone ill)",
                acceptedAnswers = new string[] { "well" },
                firstLetterHint = "w...",
                completedCollocation = "get well soon",
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get well soon.mp3")
                #endif
            },
            new W01CollocationItem {
                promptText = "______ a thief (what the police do)",
                acceptedAnswers = new string[] { "catch" },
                firstLetterHint = "c...",
                completedCollocation = "catch a thief",
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a thief.mp3")
                #endif
            },
            new W01CollocationItem {
                promptText = "catch a ______ (an illness with sneezing)",
                acceptedAnswers = new string[] { "cold" },
                firstLetterHint = "c...",
                completedCollocation = "catch a cold",
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a cold.mp3")
                #endif
            },
            new W01CollocationItem {
                promptText = "catch your ______ (to rest after running)",
                acceptedAnswers = new string[] { "breath" },
                firstLetterHint = "b...",
                completedCollocation = "catch your breath",
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch your breath.mp3")
                #endif
            },
            new W01CollocationItem {
                promptText = "______ water (to not waste it)",
                acceptedAnswers = new string[] { "save" },
                firstLetterHint = "s...",
                completedCollocation = "save water",
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "save water.mp3")
                #endif
            },
            new W01CollocationItem {
                promptText = "save ______ (to not waste power)",
                acceptedAnswers = new string[] { "electricity", "energy" },
                firstLetterHint = "e...",
                completedCollocation = "save electricity",
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "save electricity.mp3")
                #endif
            },
            new W01CollocationItem {
                promptText = "save someone's ______ (to rescue them)",
                acceptedAnswers = new string[] { "life" },
                firstLetterHint = "l...",
                completedCollocation = "save someone's life",
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "save someone's life.mp3")
                #endif
            },
            new W01CollocationItem {
                promptText = "an ______ idea (a very good one)",
                acceptedAnswers = new string[] { "excellent" },
                firstLetterHint = "e...",
                completedCollocation = "an excellent idea",
                #if UNITY_EDITOR
                readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "an excellent idea.mp3")
                #endif
            }
        };
    }

    private void PlayIntroVoiceover() {
        if (ariaIntroAudio == null) {
            #if UNITY_EDITOR
            ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Writing/Type the missing half - make the pair complete.mp3");
            #endif
        }
        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
        }
    }

    private void AutoFindUIReferences() {
        if (w01InputField == null) {
            w01InputField = GetComponentInChildren<TMP_InputField>(true);
            if (w01InputField == null) w01InputField = inputField;
        }

        if (w01SubmitButton == null) {
            if (checkButton != null) {
                w01SubmitButton = checkButton;
            } else {
                Button[] btns = GetComponentsInChildren<Button>(true);
                foreach (var b in btns) {
                    if (b == null) continue;
                    string bName = b.name.ToLower();
                    if (bName.Contains("check") || bName.Contains("submit") || bName.Contains("btn")) {
                        w01SubmitButton = b;
                        break;
                    }
                }
            }
        }

        if (w01PromptTMP == null) {
            if (promptTMP != null) w01PromptTMP = promptTMP;
            else {
                Transform t = transform.Find("PromptText") ?? transform.Find("QuestionText") ?? transform.Find("Card/Text");
                if (t != null) w01PromptTMP = t.GetComponent<TextMeshProUGUI>();
            }
        }

        if (w01ProgressTMP == null) {
            if (progressCountTMP != null) w01ProgressTMP = progressCountTMP;
            else {
                Transform t = transform.Find("ProgressIndicator") ?? transform.Find("ProgressText");
                if (t != null) w01ProgressTMP = t.GetComponent<TextMeshProUGUI>();
            }
        }

        if (w01HintTMP == null) {
            if (hintTMP != null) w01HintTMP = hintTMP;
            else {
                Transform t = transform.Find("HintText") ?? transform.Find("HintPanel/Text");
                if (t != null) w01HintTMP = t.GetComponent<TextMeshProUGUI>();
            }
        }

        if (w01FeedbackTMP == null) {
            Transform t = transform.Find("FeedbackText");
            if (t != null) w01FeedbackTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (resultPanel == null) {
            Transform res = transform.Find("ResultPanel");
            if (res != null) resultPanel = res.gameObject;
        }

        if (retryButton == null && resultPanel != null) {
            retryButton = resultPanel.GetComponentInChildren<Button>(true);
        }

        if (sfxSnap == null) {
            #if UNITY_EDITOR
            sfxSnap = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
            #endif
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";

            if (lowerName.Contains("title") || textVal.Contains("Polished") || textVal.Contains("W01") || textVal.Contains("Rewrite") || textVal.Contains("Greeting") || textVal.Contains("Complete")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "W01 Type the Missing Half";
            }
            if (lowerName.Contains("heading") || textVal.Contains("WRITING")) {
                tmp.text = "WRITING BRANCH (Writing Bench)";
            }
            if (lowerName.Contains("instruction") || textVal.Contains("type") || textVal.Contains("missing")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "Type the missing half of the collocation into the input field.";
            }
        }
    }

    private void SetupUIBindings() {
        if (w01SubmitButton != null) {
            w01SubmitButton.onClick.RemoveAllListeners();
            w01SubmitButton.onClick.AddListener(OnCheckSubmitted);
        }

        if (w01InputField != null) {
            w01InputField.onSubmit.RemoveAllListeners();
            w01InputField.onSubmit.AddListener(text => OnCheckSubmitted());
        }
    }

    private void LoadItem(int index) {
        if (collocationItems == null || index < 0 || index >= collocationItems.Length) {
            EvaluateLessonCompletion();
            return;
        }

        currentItemIndex = index;
        attemptsOnCurrentItem = 0;
        isCheckingAnswer = false;
        itemHadErrors = false;

        W01CollocationItem item = collocationItems[index];

        // Update Prompt Text
        if (w01PromptTMP != null) {
            w01PromptTMP.gameObject.SetActive(true);
            w01PromptTMP.text = item.promptText;
        }

        // Update Progress Indicator
        if (w01ProgressTMP != null) {
            w01ProgressTMP.gameObject.SetActive(true);
            w01ProgressTMP.text = $"Item {index + 1}/{collocationItems.Length}";
        }

        // Reset Feedback and Hint UI
        if (w01FeedbackTMP != null) {
            w01FeedbackTMP.text = "";
            w01FeedbackTMP.gameObject.SetActive(false);
        }
        if (w01HintTMP != null) {
            w01HintTMP.text = "";
            w01HintTMP.gameObject.SetActive(false);
        }
        if (hintPanel != null) {
            hintPanel.SetActive(false);
        }

        // Reset Input Field
        if (w01InputField != null) {
            w01InputField.gameObject.SetActive(true);
            w01InputField.text = "";
            w01InputField.interactable = true;

            TMP_Text placeholder = w01InputField.placeholder as TMP_Text;
            if (placeholder != null) {
                placeholder.text = "Type missing word...";
            }

            Image bg = w01InputField.GetComponent<Image>();
            if (bg != null) bg.color = Color.white;

            w01InputField.Select();
            w01InputField.ActivateInputField();
        }

        if (w01SubmitButton != null) {
            w01SubmitButton.gameObject.SetActive(true);
            w01SubmitButton.interactable = true;
        }
    }

    public void OnCheckSubmitted() {
        if (isCheckingAnswer || collocationItems == null || currentItemIndex >= collocationItems.Length) return;
        if (w01InputField == null) return;

        string userInput = w01InputField.text.Trim();
        if (string.IsNullOrEmpty(userInput)) return;

        W01CollocationItem currentItem = collocationItems[currentItemIndex];

        // Soft Key Sound on Submit
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        // Case-insensitive, whitespace-trimmed comparison against all accepted answers
        bool isCorrect = CheckAnswerMatch(userInput, currentItem.acceptedAnswers);

        if (isCorrect) {
            StartCoroutine(HandleCorrectAnswer(currentItem, userInput));
        } else {
            HandleWrongAnswer(currentItem);
        }
    }

    private bool CheckAnswerMatch(string input, string[] acceptedList) {
        if (string.IsNullOrEmpty(input) || acceptedList == null || acceptedList.Length == 0) return false;

        string cleanInput = input.Trim().ToLowerInvariant();
        cleanInput = System.Text.RegularExpressions.Regex.Replace(cleanInput, @"[^\w\s]", "");

        foreach (var target in acceptedList) {
            if (string.IsNullOrEmpty(target)) continue;

            string cleanTarget = target.Trim().ToLowerInvariant();
            cleanTarget = System.Text.RegularExpressions.Regex.Replace(cleanTarget, @"[^\w\s]", "");

            if (cleanInput.Equals(cleanTarget)) {
                return true;
            }
        }

        return false;
    }

    private IEnumerator HandleCorrectAnswer(W01CollocationItem item, string matchedWord) {
        isCheckingAnswer = true;
        correctCount++;

        if (w01SubmitButton != null) w01SubmitButton.interactable = false;
        if (w01InputField != null) w01InputField.interactable = false;

        // Play SFX Correct & Snap effect
        PlaySnapSFX();

        // Visually complete the collocation with green highlighted word
        if (w01PromptTMP != null) {
            string completedText = FormatCompletedCollocation(item.promptText, matchedWord);
            w01PromptTMP.text = completedText;
            w01PromptTMP.transform.DOKill();
            w01PromptTMP.transform.DOPunchScale(Vector3.one * 0.15f, 0.35f);
        }

        // Highlight input field green
        Image bg = w01InputField != null ? w01InputField.GetComponent<Image>() : null;
        if (bg != null) {
            bg.DOColor(new Color(0.4f, 0.9f, 0.4f, 1f), 0.3f);
        }

        // Play ARIA readback audio
        PlayReadbackAudio(item);

        float waitTime = (item.readbackAudio != null) ? item.readbackAudio.length + 0.3f : 1.8f;
        yield return new WaitForSeconds(Mathf.Max(1.5f, waitTime));

        // Reset input bg color
        if (bg != null) {
            bg.DOColor(Color.white, 0.2f);
        }

        currentItemIndex++;
        if (currentItemIndex < collocationItems.Length) {
            LoadItem(currentItemIndex);
        } else {
            EvaluateLessonCompletion();
        }
    }

    private string FormatCompletedCollocation(string prompt, string typedAnswer) {
        if (string.IsNullOrEmpty(prompt)) return typedAnswer;
        string greenAnswer = $"<color=#22C55E><b>{typedAnswer}</b></color>";
        if (prompt.Contains("______")) return prompt.Replace("______", greenAnswer);
        if (prompt.Contains("________")) return prompt.Replace("________", greenAnswer);
        return $"{greenAnswer} {prompt}";
    }

    private void PlayReadbackAudio(W01CollocationItem item) {
        if (item == null || item.readbackAudio == null) return;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
        }
    }

    private void PlaySnapSFX() {
        if (sfxSnap != null) {
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(sfxSnap, pos);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }
    }

    private void HandleWrongAnswer(W01CollocationItem item) {
        itemHadErrors = true;
        attemptsOnCurrentItem++;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        // Shake input field
        if (w01InputField != null) {
            w01InputField.transform.DOKill();
            w01InputField.transform.DOShakePosition(0.35f, new Vector3(12f, 0, 0));
        }

        if (attemptsOnCurrentItem == 1) {
            // First retry: Display subtle first-letter hint
            string hintText = $"Hint: Starts with '{item.firstLetterHint}'";
            if (hintPanel != null) hintPanel.SetActive(true);

            if (w01HintTMP != null) {
                w01HintTMP.gameObject.SetActive(true);
                w01HintTMP.text = hintText;
            }
            if (w01FeedbackTMP != null) {
                w01FeedbackTMP.gameObject.SetActive(true);
                w01FeedbackTMP.text = $"Incorrect. {hintText}";
            }

            // Clear input for retry
            if (w01InputField != null) {
                w01InputField.text = "";
                w01InputField.Select();
                w01InputField.ActivateInputField();
            }
        } else {
            // Retry exhausted: Auto-reveal answer and move forward
            if (w01FeedbackTMP != null) {
                w01FeedbackTMP.gameObject.SetActive(true);
                w01FeedbackTMP.text = $"Answer: {item.completedCollocation}";
            }

            StartCoroutine(AutoAdvanceExhaustedRetry(item));
        }
    }

    private IEnumerator AutoAdvanceExhaustedRetry(W01CollocationItem item) {
        isCheckingAnswer = true;
        if (w01SubmitButton != null) w01SubmitButton.interactable = false;
        if (w01InputField != null) w01InputField.interactable = false;

        if (w01PromptTMP != null) {
            w01PromptTMP.text = item.completedCollocation;
        }

        PlayReadbackAudio(item);

        float waitTime = (item.readbackAudio != null) ? item.readbackAudio.length + 0.3f : 1.8f;
        yield return new WaitForSeconds(Mathf.Max(1.5f, waitTime));

        currentItemIndex++;
        if (currentItemIndex < collocationItems.Length) {
            LoadItem(currentItemIndex);
        } else {
            EvaluateLessonCompletion();
        }
    }

    private void EvaluateLessonCompletion() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        bool passed = (correctCount >= passScore);

        if (resultTMP != null) {
            if (passed) {
                resultTMP.text = $"GREAT JOB! Score: {correctCount}/{collocationItems.Length}\nYou completed the Writing Bench!";
            } else {
                resultTMP.text = $"TRY AGAIN! Score: {correctCount}/{collocationItems.Length}\nYou need at least {passScore}/{collocationItems.Length} to pass.";
            }
        }

        if (passed) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
                NextButtonAnimation();
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (retryButton != null) {
                retryButton.gameObject.SetActive(true);
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(RestartActivity);
            }
        }
    }

    public void RestartActivity() {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        currentItemIndex = 0;
        correctCount = 0;
        attemptsOnCurrentItem = 0;
        isCheckingAnswer = false;
        itemHadErrors = false;
        LoadItem(0);
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Writing;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}