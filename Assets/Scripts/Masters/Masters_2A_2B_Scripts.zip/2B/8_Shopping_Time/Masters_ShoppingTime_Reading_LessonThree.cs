using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// R03 At the Counter — Put the Conversation in Order
/// Book 2B Unit 8: Shopping Time (Reading Lesson Three).
/// Orders the 9-line shopping conversation between Joy and the Shopkeeper (verbatim p.35).
/// Success condition: Student places at least 7 of 9 lines in the correct order.
/// </summary>
public class Masters_ShoppingTime_Reading_LessonThree : Masters_Lesson {

[System.Serializable]
public class ShoppingTime_ReadingR03Step {
    public int stepIndex;
    public string speakerName;
    public string lineText;
    public string[] optionChoices; // 3 speech bubble candidate choices
    public int correctOptionIndex; // 0..2
    public AudioClip lineAudio;
}

    [Header("9 Conversation Steps (Verbatim p.35)")]
    [SerializeField] private ShoppingTime_ReadingR03Step[] steps;

    [Header("UI Headers & Progress")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;

    [Header("Conversation Window")]
    [SerializeField] private GameObject conversationCardObject;
    [SerializeField] private TextMeshProUGUI conversationTranscriptTMP;
    [SerializeField] private ScrollRect transcriptScrollRect;
    [SerializeField] private Button replayAudioBtn;

    [Header("3 Speech Bubble Option Chips")]
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionTexts;
    [SerializeField] private Image[] optionImages;

    [Header("Results Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button returnHubBtn;

    [Header("Audio References")]
    [SerializeField] private AudioClip introAudio;
    [SerializeField] private AudioClip recapAudio;

    [Header("Navigation")]
    [SerializeField] private Button backButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Pass Threshold")]
    [SerializeField] private int passThreshold = 7;

    private int currentStepIndex = 0;
    private int score = 0;
    private bool isHandlingAnswer = false;
    private List<string> completedTranscript = new List<string>();

    private Color defaultChipColor = new Color(0.14f, 0.42f, 0.70f, 0.95f);
    private Color correctChipColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private Color wrongChipColor = new Color(0.85f, 0.25f, 0.25f, 1f);

#if UNITY_EDITOR
    private void Reset() {
        topic = Masters_Topic.Reading;
        AutoBindReferences();
        InitStepsIfEmpty();
    }

    private void OnValidate() {
        topic = Masters_Topic.Reading;
        AutoBindReferences();
        InitStepsIfEmpty();
    }
#endif

    protected override void Awake() {
        topic = Masters_Topic.Reading;
        base.Awake();

        AutoBindReferences();
        InitStepsIfEmpty();
        WireEventListeners();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Reading;
        AutoBindReferences();
        WireEventListeners();
        RestartLesson();
    }

    public void AutoBindReferences() {
        // 1. Text headers
        TextMeshProUGUI[] allTMPs = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in allTMPs) {
            string n = t.gameObject.name.ToLower();
            Transform p = t.transform.parent;
            string pn = p != null ? p.name.ToLower() : "";

            if ((n.Contains("header") || pn.Contains("header")) && headerTMP == null) headerTMP = t;
            else if ((n.Contains("title") || pn.Contains("title")) && !n.Contains("result") && !pn.Contains("result") && titleTMP == null) titleTMP = t;
            else if ((n.Contains("subtitle") || pn.Contains("subtitle") || n.Contains("instruction")) && subtitleTMP == null) subtitleTMP = t;
            else if (n.Contains("progress") && progressTMP == null) progressTMP = t;
            else if (n.Contains("score") && !n.Contains("result") && scoreTMP == null) scoreTMP = t;
            else if ((n.Contains("windowsign") || n.Contains("transcript") || n.Contains("notice")) && conversationTranscriptTMP == null) {
                conversationTranscriptTMP = t;
            }
        }

        // 2. Conversation card & transcript
        if (conversationCardObject == null) {
            Transform card = transform.Find("WindowSignContainer") ?? transform.Find("NoticeboardCard") ?? transform.Find("PromptCard") ?? transform.Find("ConversationCard");
            if (card != null) conversationCardObject = card.gameObject;
        }

        if (conversationTranscriptTMP == null && conversationCardObject != null) {
            conversationTranscriptTMP = conversationCardObject.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (transcriptScrollRect == null) {
            transcriptScrollRect = GetComponentInChildren<ScrollRect>(true);
        }

        // 3. Option buttons, images, and texts
        Transform container = transform.Find("OptionButtonsContainer") ?? transform.Find("OptionsGrid") ?? transform.Find("OptionsContainer");
        if (container != null) {
            List<Button> bList = new List<Button>();
            List<Image> iList = new List<Image>();
            List<TextMeshProUGUI> tList = new List<TextMeshProUGUI>();

            for (int i = 0; i < container.childCount; i++) {
                Transform child = container.GetChild(i);
                Button b = child.GetComponent<Button>();
                if (b != null) {
                    if (bList.Count < 3) {
                        bList.Add(b);
                        iList.Add(child.GetComponent<Image>());
                        TextMeshProUGUI txt = child.GetComponentInChildren<TextMeshProUGUI>(true);
                        tList.Add(txt);
                    } else {
                        child.gameObject.SetActive(false); // Disable 4th button
                    }
                }
            }

            optionButtons = bList.ToArray();
            optionImages = iList.ToArray();
            optionTexts = tList.ToArray();
        }

        // 4. Result Panel
        if (resultPanel == null) {
            Transform rp = transform.Find("ResultPanel");
            if (rp != null) resultPanel = rp.gameObject;
        }

        if (resultPanel != null) {
            if (resultTitleTMP == null) {
                Transform rt = resultPanel.transform.Find("ResultTitle") ?? resultPanel.transform.Find("ResultTitleTMP");
                if (rt != null) resultTitleTMP = rt.GetComponent<TextMeshProUGUI>();
            }
            if (resultScoreTMP == null) {
                Transform rs = resultPanel.transform.Find("ResultScore") ?? resultPanel.transform.Find("ResultScoreTMP");
                if (rs != null) resultScoreTMP = rs.GetComponent<TextMeshProUGUI>();
            }
            if (resultStatusTMP == null) {
                Transform rst = resultPanel.transform.Find("ResultStatus") ?? resultPanel.transform.Find("ResultStatusTMP");
                if (rst != null) resultStatusTMP = rst.GetComponent<TextMeshProUGUI>();
            }
            if (retryBtn == null) {
                Transform rb = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
                if (rb != null) retryBtn = rb.GetComponent<Button>();
            }
            if (returnHubBtn == null) {
                Transform hb = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("ReturnHubBtn") ?? resultPanel.transform.Find("NextButton");
                if (hb != null) returnHubBtn = hb.GetComponent<Button>();
            }
        }

        // 5. Back Button
        if (backButton == null) {
            Transform bb = transform.Find("BackButton");
            if (bb != null) backButton = bb.GetComponent<Button>();
        }

        // Set correct titles
        if (headerTMP != null) headerTMP.text = "SHOP COUNTER 🛍️";
        if (titleTMP != null) {
            titleTMP.text = "At the Counter — Put the Conversation in Order";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f);
        }
        if (subtitleTMP != null) subtitleTMP.text = "Choose each line in order so the purchase runs from greeting to thank you.";
    }

    private void WireEventListeners() {
        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartLesson);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(OnNextButtonClicked);
        }

        if (backButton != null) {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int idx = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionClicked(idx));
                }
            }
        }
    }

    public void RestartLesson() {
        currentStepIndex = 0;
        score = 0;
        isHandlingAnswer = false;
        completedTranscript.Clear();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
            nextButton.interactable = false;
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        UpdateTranscriptDisplay();
        LoadStep(currentStepIndex);

        if (introAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
        }
    }

    private void LoadStep(int stepIdx) {
        if (steps == null || steps.Length == 0) return;

        if (stepIdx >= steps.Length) {
            OnLessonCompleted();
            return;
        }

        var step = steps[stepIdx];

        if (progressTMP != null) {
            progressTMP.text = $"Step {stepIdx + 1}/{steps.Length}";
        }
        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {score}/{steps.Length}";
        }

        // Configure Option Chips
        if (optionButtons != null && step.optionChoices != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] == null) continue;

                if (i < step.optionChoices.Length) {
                    optionButtons[i].gameObject.SetActive(true);
                    optionButtons[i].interactable = true;

                    if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                        optionTexts[i].text = step.optionChoices[i];
                    }

                    if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                        optionImages[i].color = defaultChipColor;
                    }

                    optionButtons[i].transform.DOKill();
                    optionButtons[i].transform.localScale = Vector3.one;
                } else {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        isHandlingAnswer = false;
    }

    private void OnOptionClicked(int optionIdx) {
        if (isHandlingAnswer || currentStepIndex >= steps.Length) return;

        var step = steps[currentStepIndex];
        bool isCorrect = (optionIdx == step.correctOptionIndex);

        if (isCorrect) {
            StartCoroutine(HandleCorrectAnswer(optionIdx, step));
        } else {
            StartCoroutine(HandleWrongAnswer(optionIdx));
        }
    }

    private IEnumerator HandleCorrectAnswer(int optionIdx, ShoppingTime_ReadingR03Step step) {
        isHandlingAnswer = true;
        score++;

        if (optionImages != null && optionIdx < optionImages.Length && optionImages[optionIdx] != null) {
            optionImages[optionIdx].color = correctChipColor;
        }

        if (optionButtons != null && optionIdx < optionButtons.Length && optionButtons[optionIdx] != null) {
            optionButtons[optionIdx].transform.DOPunchScale(Vector3.one * 0.15f, 0.35f, 10, 1);
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        // Add formatted line to transcript
        string speakerColor = step.speakerName.ToLower().Contains("joy") ? "#66D9FF" : "#FFE066";
        completedTranscript.Add($"<b><color={speakerColor}>{step.speakerName}:</color></b> \"{step.lineText}\"");
        UpdateTranscriptDisplay();

        // Play line audio
        if (step.lineAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(step.lineAudio);
            yield return new WaitForSeconds(Mathf.Max(step.lineAudio.length + 0.3f, 1.2f));
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        currentStepIndex++;
        LoadStep(currentStepIndex);
    }

    private IEnumerator HandleWrongAnswer(int optionIdx) {
        isHandlingAnswer = true;

        if (optionImages != null && optionIdx < optionImages.Length && optionImages[optionIdx] != null) {
            optionImages[optionIdx].color = wrongChipColor;
        }

        if (optionButtons != null && optionIdx < optionButtons.Length && optionButtons[optionIdx] != null) {
            optionButtons[optionIdx].transform.DOShakePosition(0.4f, 10f, 20, 90, false, true);
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        yield return new WaitForSeconds(0.6f);

        if (optionImages != null && optionIdx < optionImages.Length && optionImages[optionIdx] != null) {
            optionImages[optionIdx].color = defaultChipColor;
        }

        isHandlingAnswer = false;
    }

    private void UpdateTranscriptDisplay() {
        if (conversationTranscriptTMP == null) return;

        if (completedTranscript.Count == 0) {
            conversationTranscriptTMP.text = "<color=#88A0C0><i>Tap the speech bubble below to place the first line of the conversation...</i></color>";
        } else {
            // Show ONLY the current/latest line (hide past lines as requested)
            conversationTranscriptTMP.text = completedTranscript[completedTranscript.Count - 1];
        }

        // Scroll to bottom
        if (transcriptScrollRect != null) {
            Canvas.ForceUpdateCanvases();
            transcriptScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void ReplayCurrentAudio() {
        if (currentStepIndex < steps.Length && steps[currentStepIndex].lineAudio != null) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(steps[currentStepIndex].lineAudio);
            }
        }
    }

    private void OnLessonCompleted() {
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.gameObject.SetActive(false);
            }
        }

        bool passed = score >= passThreshold;

        if (resultPanel != null) {
            resultPanel.SetActive(true);

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "Purchase Complete! 🎉" : "Good Effort!";
            }
            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Ordered {score}/{steps.Length} Lines Correctly\n<size=80%><color=#66FF66>Price Tag: ₹400 ➔ ₹360 (10% Off)</color></size>";
            }
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "You put the shopping dialogue in perfect order!" : "Keep practicing! Try putting all lines in order.";
            }
        }

        if (passed) {
            if (recapAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            NextButtonAnimation();
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Writing);
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    private void OnBackButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.gameObject.SetActive(true);
        }
        gameObject.SetActive(false);
    }

    public void InitStepsIfEmpty() {
        string audioDir = "Assets/Audio/2B/8_Shopping_Time/Reading/";

        steps = new ShoppingTime_ReadingR03Step[] {
            new ShoppingTime_ReadingR03Step {
                stepIndex = 1,
                speakerName = "Shopkeeper",
                lineText = "Excuse me! May I help you?",
                optionChoices = new string[] {
                    "Excuse me! May I help you?",
                    "Alright! I will buy that.",
                    "You are welcome."
                },
                correctOptionIndex = 0,
#if UNITY_EDITOR
                lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r03_line1_shopkeeper.mp3")
#endif
            },
            new ShoppingTime_ReadingR03Step {
                stepIndex = 2,
                speakerName = "Joy",
                lineText = "Yes, I want a T-Shirt. Can you show me one?",
                optionChoices = new string[] {
                    "Three hundred and sixty rupees please.",
                    "Yes, I want a T-Shirt. Can you show me one?",
                    "Well, it costs four hundred rupees."
                },
                correctOptionIndex = 1,
#if UNITY_EDITOR
                lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r03_line2_joy.mp3")
#endif
            },
            new ShoppingTime_ReadingR03Step {
                stepIndex = 3,
                speakerName = "Shopkeeper",
                lineText = "Here are a few. Which one would you like to take?",
                optionChoices = new string[] {
                    "The blue one, just hanging at the side of the red one.",
                    "Here are a few. Which one would you like to take?",
                    "Here it is. Thank you."
                },
                correctOptionIndex = 1,
#if UNITY_EDITOR
                lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r03_line3_shopkeeper.mp3")
#endif
            },
            new ShoppingTime_ReadingR03Step {
                stepIndex = 4,
                speakerName = "Joy",
                lineText = "The blue one, just hanging at the side of the red one. How much does it cost?",
                optionChoices = new string[] {
                    "You will get 10 % discount.",
                    "Excuse me! May I help you?",
                    "The blue one, just hanging at the side of the red one. How much does it cost?"
                },
                correctOptionIndex = 2,
#if UNITY_EDITOR
                lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r03_line4_joy.mp3")
#endif
            },
            new ShoppingTime_ReadingR03Step {
                stepIndex = 5,
                speakerName = "Shopkeeper",
                lineText = "Well, it costs four hundred rupees. You will get 10 % discount. That means forty rupees off.",
                optionChoices = new string[] {
                    "Well, it costs four hundred rupees. You will get 10 % discount. That means forty rupees off.",
                    "Alright! I will buy that.",
                    "Here are a few."
                },
                correctOptionIndex = 0,
#if UNITY_EDITOR
                lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r03_line5_shopkeeper.mp3")
#endif
            },
            new ShoppingTime_ReadingR03Step {
                stepIndex = 6,
                speakerName = "Joy",
                lineText = "Alright! I will buy that.",
                optionChoices = new string[] {
                    "Three hundred and sixty rupees please.",
                    "Alright! I will buy that.",
                    "You are welcome."
                },
                correctOptionIndex = 1,
#if UNITY_EDITOR
                lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r03_line6_joy.mp3")
#endif
            },
            new ShoppingTime_ReadingR03Step {
                stepIndex = 7,
                speakerName = "Shopkeeper",
                lineText = "Three hundred and sixty rupees please.",
                optionChoices = new string[] {
                    "Yes, I want a T-Shirt.",
                    "How much does it cost?",
                    "Three hundred and sixty rupees please."
                },
                correctOptionIndex = 2,
#if UNITY_EDITOR
                lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r03_line7_shopkeeper.mp3")
#endif
            },
            new ShoppingTime_ReadingR03Step {
                stepIndex = 8,
                speakerName = "Joy",
                lineText = "Here it is. Thank you.",
                optionChoices = new string[] {
                    "Here it is. Thank you.",
                    "Well, it costs four hundred rupees.",
                    "Which one would you like to take?"
                },
                correctOptionIndex = 0,
#if UNITY_EDITOR
                lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r03_line8_joy.mp3")
#endif
            },
            new ShoppingTime_ReadingR03Step {
                stepIndex = 9,
                speakerName = "Shopkeeper",
                lineText = "You are welcome.",
                optionChoices = new string[] {
                    "May I help you?",
                    "That means forty rupees off.",
                    "You are welcome."
                },
                correctOptionIndex = 2,
#if UNITY_EDITOR
                lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "st_r03_line9_shopkeeper.mp3")
#endif
            }
        };
    }
}
