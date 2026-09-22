using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Masters_CourtesyCall_Writing_LessonOne : Masters_Lesson {

[System.Serializable]
public class WritingW01ClozeItem {
    public string sentencePromptWithBlank; // e.g. "Thanks a _____ . [thank]"
    public string acceptedAnswer;           // e.g. "lot"
    public string fullCompletedSentence;    // e.g. "Thanks a lot."
    public AudioClip itemAudio;
}

    [Header("W01 12 Verbatim Cloze Items (p.11-12)")]
    [SerializeField]
    private WritingW01ClozeItem[] items;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI sentencePromptTMP;
    [SerializeField]
    private TextMeshProUGUI hintTextTMP;

    [Header("Input & Submission")]
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private Button submitBtn;

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Editor Preview")]
    [Range(0, 11)]
    public int editorPreviewRound = 0;

    private int currentItemIndex = 0;
    private int score = 0;
    private int attemptCountOnCurrentItem = 0;
    private bool isProcessingInput = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (submitBtn != null) {
            submitBtn.onClick.RemoveAllListeners();
            submitBtn.onClick.AddListener(OnSubmitButtonClicked);
        }

        if (inputField != null) {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener((val) => OnSubmitButtonClicked());
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (items == null || items.Length == 0) {
            PopulateFailsafeItems();
        }

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Writing/VO_W01_ARIA.mp3");
        }
#endif

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;
        if (inputField != null) inputField.interactable = false;
        if (submitBtn != null) submitBtn.interactable = false;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(Mathf.Max(2.5f, narratorSpeech.length));
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        currentItemIndex = 0;
        score = 0;
        isProcessingInput = false;
        if (inputField != null) inputField.interactable = true;
        if (submitBtn != null) submitBtn.interactable = true;

        LoadItem(currentItemIndex);
    }

    private void PopulateFailsafeItems() {
        items = new WritingW01ClozeItem[] {
            new WritingW01ClozeItem { sentencePromptWithBlank = "Thanks a _____ . [thank]", acceptedAnswer = "lot", fullCompletedSentence = "Thanks a lot." },
            new WritingW01ClozeItem { sentencePromptWithBlank = "Thank you so much for the _____ gift. [thank]", acceptedAnswer = "birthday", fullCompletedSentence = "Thank you so much for the birthday gift." },
            new WritingW01ClozeItem { sentencePromptWithBlank = "Thank you so much for _____ me home. [thank]", acceptedAnswer = "driving", fullCompletedSentence = "Thank you so much for driving me home." },
            new WritingW01ClozeItem { sentencePromptWithBlank = "I really _____ you helping me out. [thank]", acceptedAnswer = "appreciate", fullCompletedSentence = "I really appreciate you helping me out." },
            new WritingW01ClozeItem { sentencePromptWithBlank = "I'm sorry for the _____ . [apologize]", acceptedAnswer = "mess", fullCompletedSentence = "I'm sorry for the mess." },
            new WritingW01ClozeItem { sentencePromptWithBlank = "I'm so sorry I'm _____ . [apologize]", acceptedAnswer = "late", fullCompletedSentence = "I'm so sorry I'm late." },
            new WritingW01ClozeItem { sentencePromptWithBlank = "I'm sorry, I don't _____ . [apologize]", acceptedAnswer = "understand", fullCompletedSentence = "I'm sorry, I don't understand." },
            new WritingW01ClozeItem { sentencePromptWithBlank = "Excuse me, do you know what _____ it is? [courtesy]", acceptedAnswer = "time", fullCompletedSentence = "Excuse me, do you know what time it is?" },
            new WritingW01ClozeItem { sentencePromptWithBlank = "Excuse me sir, you dropped your _____ . [courtesy]", acceptedAnswer = "wallet", fullCompletedSentence = "Excuse me sir, you dropped your wallet." },
            new WritingW01ClozeItem { sentencePromptWithBlank = "Excuse me, is this _____ taken? [courtesy]", acceptedAnswer = "seat", fullCompletedSentence = "Excuse me, is this seat taken?" },
            new WritingW01ClozeItem { sentencePromptWithBlank = "Thank you from the bottom of my _____ . [thank]", acceptedAnswer = "heart", fullCompletedSentence = "Thank you from the bottom of my heart." },
            new WritingW01ClozeItem { sentencePromptWithBlank = "Excuse me, could you tell me the _____ ? [courtesy]", acceptedAnswer = "way", fullCompletedSentence = "Excuse me, could you tell me the way?" }
        };

#if UNITY_EDITOR
        string audioDir = "Assets/Audio/2B/2_CourtesyCall/Writing/";
        for (int i = 0; i < items.Length; i++) {
            items[i].itemAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + $"VO_W01_I{i + 1}.mp3");
        }
#endif
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (items == null || items.Length == 0) {
            PopulateFailsafeItems();
        }

        if (items == null || items.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, items.Length - 1);
        WritingW01ClozeItem item = items[idx];

        if (headerTMP != null) headerTMP.text = "WRITING BRANCH (Post-Office Desk)";
        if (titleTMP != null) titleTMP.text = "W01 Complete the Courtesy";
        if (progressTMP != null) progressTMP.text = $"{idx + 1}/{items.Length}";
        if (sentencePromptTMP != null) sentencePromptTMP.text = item.sentencePromptWithBlank;
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "SlateRectTransform", "ButtonsParentTransform"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "WRITING BRANCH (Post-Office Desk)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "W01 Complete the Courtesy";
        }
    }

    private void AutoBindReferences() {
        if (inputField == null) {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (submitBtn == null && (n.Contains("submit") || n.Contains("check") || n.Contains("confirm"))) submitBtn = btn;
            else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (sentencePromptTMP == null && (n.Contains("sentence") || n.Contains("prompt") || n.Contains("question") || n.Contains("dialogue"))) sentencePromptTMP = tmp;
            else if (hintTextTMP == null && n.Contains("hint")) hintTextTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    private void LoadItem(int itemIndex) {
        if (items == null || itemIndex < 0 || itemIndex >= items.Length) return;

        isProcessingInput = false;
        attemptCountOnCurrentItem = 0;
        WritingW01ClozeItem item = items[itemIndex];

        if (progressTMP != null) progressTMP.text = $"{itemIndex + 1}/{items.Length}";
        if (sentencePromptTMP != null) sentencePromptTMP.text = item.sentencePromptWithBlank;

        if (hintTextTMP != null) {
            hintTextTMP.text = "";
            hintTextTMP.gameObject.SetActive(false);
        }

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            inputField.ActivateInputField();
        }

        PlayCurrentItemAudio();
    }

    private void PlayCurrentItemAudio() {
        if (items != null && currentItemIndex < items.Length && items[currentItemIndex].itemAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(items[currentItemIndex].itemAudio);
        } else if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
    }

    private void OnSubmitButtonClicked() {
        if (isProcessingInput || items == null || currentItemIndex >= items.Length) return;

        string userText = (inputField != null) ? inputField.text.Trim() : "";
        if (string.IsNullOrEmpty(userText)) return;

        isProcessingInput = true;
        WritingW01ClozeItem item = items[currentItemIndex];
        bool isCorrect = userText.Equals(item.acceptedAnswer.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (isCorrect) {
            score++;
            if (sentencePromptTMP != null) sentencePromptTMP.text = item.fullCompletedSentence;
            if (inputField != null) inputField.interactable = false;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            StartCoroutine(AdvanceItemRoutine());
        } else {
            attemptCountOnCurrentItem++;
            if (attemptCountOnCurrentItem == 1 && !string.IsNullOrEmpty(item.acceptedAnswer)) {
                // First retry with first-letter hint
                char firstLetter = char.ToUpper(item.acceptedAnswer[0]);
                if (hintTextTMP != null) {
                    hintTextTMP.gameObject.SetActive(true);
                    hintTextTMP.text = $"Hint: Missing word starts with '{firstLetter}'";
                    hintTextTMP.color = Color.yellow;
                }
                if (inputField != null) {
                    inputField.text = "";
                    inputField.transform.DOShakePosition(0.4f, 8f);
                    inputField.ActivateInputField();
                }
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
                isProcessingInput = false;
            } else {
                // Failed after 2 attempts -> reveal answer and move on
                if (sentencePromptTMP != null) sentencePromptTMP.text = $"{item.sentencePromptWithBlank} (Answer: {item.acceptedAnswer})";
                if (inputField != null) inputField.interactable = false;

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
                StartCoroutine(AdvanceItemRoutine());
            }
        }
    }

    private IEnumerator AdvanceItemRoutine() {
        yield return new WaitForSeconds(1.5f);

        currentItemIndex++;
        if (currentItemIndex < items.Length) {
            LoadItem(currentItemIndex);
        } else {
            CompleteLesson();
        }
    }

    private void CompleteLesson() {
        bool passed = (score >= 8); // Pass condition: 8 of 12

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultScoreTMP != null) resultScoreTMP.text = $"Score: {score}/{items.Length}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "GREAT JOB! LESSON COMPLETE!" : "TRY AGAIN TO PASS!";
                resultStatusTMP.color = passed ? Color.green : Color.red;
            }
        }

        if (passed && nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(passed ? Masters_SFX.Correct : Masters_SFX.Incorrect);
        }
    }

    private void OnReplayAudioClicked() {
        PlayCurrentItemAudio();
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) resultPanel.SetActive(false);
        currentItemIndex = 0;
        score = 0;
        LoadItem(currentItemIndex);
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
