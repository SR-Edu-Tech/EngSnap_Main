using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// W01 Complete the Shopping Sentence
/// Stationers' counter receipt pad cloze fill-in writing controller for Book 2B Unit 8 (Shopping Time).
/// Types the missing word to complete verbatim price phrases, signs, and dialogue lines.
/// Success condition: Student types the correct word in at least 8 of 10 items.
/// </summary>
public class Masters_ShoppingTime_Writing_LessonOne : Masters_Lesson {

[System.Serializable]
public class ShoppingTime_WritingW01Item {
    public int itemId;
    public string promptText;           // e.g. "Excuse me! How _____ is this? [asking the price]"
    public string[] acceptedAnswers;    // e.g. ["much"]
    public string firstLetterHint;      // e.g. "Hint: Starts with 'm...'"
    public string completedSentence;    // e.g. "Excuse me! How much is this?"
    public AudioClip readbackAudio;     // Audio voiceover
}

    [Header("W01 12 Cloze Fill-in Items")]
    [SerializeField] private ShoppingTime_WritingW01Item[] items;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI promptTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI feedbackTMP;
    [SerializeField] private TextMeshProUGUI hintTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private Image inputFieldBg;

    [Header("Receipt Card / Desk Object")]
    [SerializeField] private GameObject receiptCardObject;
    [SerializeField] private Button replayAudioBtn;

    [Header("Results & Retry Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button returnHubBtn;

    [Header("Audio References")]
    [SerializeField] private AudioClip introAudio;
    [SerializeField] private AudioClip recapAudio;

    [Header("Pass Threshold")]
    [SerializeField] private int passScore = 8;
    [SerializeField] private int totalQuestionsToPlay = 10;

    private int currentItemIndex = 0;
    private int correctCount = 0;
    private int attemptsOnCurrentItem = 0;
    private bool isCheckingAnswer = false;

    private readonly Color defaultInputBgColor = Color.white;
    private readonly Color correctColor = new Color(0.2f, 0.85f, 0.35f, 1f);
    private readonly Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Writing;
        base.Awake();

        AutoBindReferences();
        InitItemsIfEmpty();
        WireEventListeners();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Writing;
        AutoBindReferences();
        WireEventListeners();
        EnsureNextAndBackButtonWired();
        RestartLesson();

        if (introAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
        }
    }

    private void WireEventListeners() {
        if (submitButton != null) {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        if (inputField != null) {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener((val) => OnSubmitClicked());
        }

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
            returnHubBtn.onClick.AddListener(OnReturnHubClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    public void InitItemsIfEmpty() {
        if (items != null && items.Length > 0) return;

        items = new ShoppingTime_WritingW01Item[] {
            new ShoppingTime_WritingW01Item {
                itemId = 1,
                promptText = "Excuse me! How _____ is this? [asking the price]",
                acceptedAnswers = new string[] { "much" },
                firstLetterHint = "Hint: Starts with 'm...'",
                completedSentence = "Excuse me! How much is this?"
            },
            new ShoppingTime_WritingW01Item {
                itemId = 2,
                promptText = "That's a little outside my _____. [too dear]",
                acceptedAnswers = new string[] { "budget" },
                firstLetterHint = "Hint: Starts with 'b...'",
                completedSentence = "That's a little outside my budget."
            },
            new ShoppingTime_WritingW01Item {
                itemId = 3,
                promptText = "I can't _____ it. [too dear]",
                acceptedAnswers = new string[] { "afford" },
                firstLetterHint = "Hint: Starts with 'a...'",
                completedSentence = "I can't afford it."
            },
            new ShoppingTime_WritingW01Item {
                itemId = 4,
                promptText = "It cost an _____ and a leg. [too dear — idiom]",
                acceptedAnswers = new string[] { "arm" },
                firstLetterHint = "Hint: Starts with 'a...'",
                completedSentence = "It cost an arm and a leg."
            },
            new ShoppingTime_WritingW01Item {
                itemId = 5,
                promptText = "That's quite _____. [a good price]",
                acceptedAnswers = new string[] { "reasonable" },
                firstLetterHint = "Hint: Starts with 'r...'",
                completedSentence = "That's quite reasonable."
            },
            new ShoppingTime_WritingW01Item {
                itemId = 6,
                promptText = "What a _____ ! [a good price]",
                acceptedAnswers = new string[] { "bargain" },
                firstLetterHint = "Hint: Starts with 'b...'",
                completedSentence = "What a bargain!"
            },
            new ShoppingTime_WritingW01Item {
                itemId = 7,
                promptText = "That's good _____. [a good price]",
                acceptedAnswers = new string[] { "value" },
                firstLetterHint = "Hint: Starts with 'v...'",
                completedSentence = "That's good value."
            },
            new ShoppingTime_WritingW01Item {
                itemId = 8,
                promptText = "It was buy one get one _____. [an offer]",
                acceptedAnswers = new string[] { "free" },
                firstLetterHint = "Hint: Starts with 'f...'",
                completedSentence = "It was buy one get one free."
            },
            new ShoppingTime_WritingW01Item {
                itemId = 9,
                promptText = "There's a 20% _____. [an offer]",
                acceptedAnswers = new string[] { "discount" },
                firstLetterHint = "Hint: Starts with 'd...'",
                completedSentence = "There's a 20% discount."
            },
            new ShoppingTime_WritingW01Item {
                itemId = 10,
                promptText = "Cash on _____. [sign]",
                acceptedAnswers = new string[] { "delivery" },
                firstLetterHint = "Hint: Starts with 'd...'",
                completedSentence = "Cash on delivery."
            },
            new ShoppingTime_WritingW01Item {
                itemId = 11,
                promptText = "Credit cards are _____ here. [sign]",
                acceptedAnswers = new string[] { "accepted" },
                firstLetterHint = "Hint: Starts with 'a...'",
                completedSentence = "Credit cards are accepted here."
            },
            new ShoppingTime_WritingW01Item {
                itemId = 12,
                promptText = "Well, it costs four hundred rupees. You will get 10 % _____. [dialogue]",
                acceptedAnswers = new string[] { "discount" },
                firstLetterHint = "Hint: Starts with 'd...'",
                completedSentence = "Well, it costs four hundred rupees. You will get 10 % discount."
            }
        };
    }

    public void RestartLesson() {
        currentItemIndex = 0;
        correctCount = 0;
        attemptsOnCurrentItem = 0;
        isCheckingAnswer = false;

        if (headerTMP != null) headerTMP.gameObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (receiptCardObject != null) receiptCardObject.SetActive(true);

        UpdateScoreUI();
        ShowItem(0);
    }

    public void ShowItem(int index) {
        if (items == null || items.Length == 0) return;
        currentItemIndex = Mathf.Clamp(index, 0, items.Length - 1);
        attemptsOnCurrentItem = 0;
        isCheckingAnswer = false;

        ShoppingTime_WritingW01Item item = items[currentItemIndex];

        UpdateScoreUI();

        if (headerTMP != null) headerTMP.text = "STATIONERS' COUNTER 🧾";
        if (titleTMP != null) {
            titleTMP.text = "Complete the Shopping Sentence";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
        }
        if (subtitleTMP != null) subtitleTMP.text = "Type the missing word to complete each price phrase or shopping line.";

        if (promptTMP != null) promptTMP.text = item.promptText;

        if (feedbackTMP != null) feedbackTMP.text = "";
        if (hintTMP != null) hintTMP.gameObject.SetActive(false);

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            inputField.ActivateInputField();
        }

        if (inputFieldBg != null) {
            inputFieldBg.color = defaultInputBgColor;
        }

        if (submitButton != null) {
            submitButton.interactable = true;
        }
    }

    private void UpdateScoreUI() {
        int targetTotal = (totalQuestionsToPlay > 0 && totalQuestionsToPlay <= items.Length) ? totalQuestionsToPlay : items.Length;
        if (progressTMP != null) {
            progressTMP.text = $"{Mathf.Min(currentItemIndex + 1, targetTotal)}/{targetTotal}";
        }
        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {correctCount}";
        }
    }

    public void OnSubmitClicked() {
        if (isCheckingAnswer) return;
        if (items == null || currentItemIndex >= items.Length) return;

        string typed = inputField != null ? inputField.text.Trim().ToLower() : "";
        if (string.IsNullOrEmpty(typed)) return;

        ShoppingTime_WritingW01Item item = items[currentItemIndex];
        bool isCorrect = false;

        foreach (var accepted in item.acceptedAnswers) {
            if (string.Equals(typed, accepted.Trim().ToLower())) {
                isCorrect = true;
                break;
            }
        }

        StartCoroutine(HandleAnswerEvaluation(isCorrect));
    }

    private IEnumerator HandleAnswerEvaluation(bool isCorrect) {
        isCheckingAnswer = true;
        attemptsOnCurrentItem++;

        ShoppingTime_WritingW01Item item = items[currentItemIndex];

        if (isCorrect) {
            if (attemptsOnCurrentItem == 1) {
                correctCount++;
                UpdateScoreUI();
            }

            if (inputFieldBg != null) inputFieldBg.color = correctColor;
            if (feedbackTMP != null) {
                feedbackTMP.text = "<color=#33D866><b>✓ Correct!</b></color>";
            }

            if (promptTMP != null) {
                promptTMP.text = $"<color=#FFD80D><b>{item.completedSentence}</b></color>";
            }

            if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            yield return new WaitForSeconds(2.0f);

            int targetTotal = (totalQuestionsToPlay > 0 && totalQuestionsToPlay <= items.Length) ? totalQuestionsToPlay : items.Length;
            if (currentItemIndex < targetTotal - 1) {
                ShowItem(currentItemIndex + 1);
            } else {
                ShowResults();
            }
        } else {
            if (inputFieldBg != null) inputFieldBg.color = wrongColor;

            if (inputField != null) {
                inputField.transform.DOShakePosition(0.4f, 10f, 14, 90, false, true);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (attemptsOnCurrentItem == 1) {
                // Show retry hint
                if (feedbackTMP != null) feedbackTMP.text = "<color=#FF6666>Not quite, try again!</color>";
                if (hintTMP != null) {
                    hintTMP.gameObject.SetActive(true);
                    hintTMP.text = item.firstLetterHint;
                }
                if (inputField != null) {
                    inputField.text = "";
                    inputField.ActivateInputField();
                }
                isCheckingAnswer = false;
            } else {
                // Second failure: show answer and advance
                if (feedbackTMP != null) {
                    feedbackTMP.text = $"<color=#FFAA33>Answer: {item.acceptedAnswers[0]}</color>";
                }

                if (promptTMP != null) {
                    promptTMP.text = $"<color=#FFD80D><b>{item.completedSentence}</b></color>";
                }

                if (item.readbackAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                }

                yield return new WaitForSeconds(2.2f);

                int targetTotal = (totalQuestionsToPlay > 0 && totalQuestionsToPlay <= items.Length) ? totalQuestionsToPlay : items.Length;
                if (currentItemIndex < targetTotal - 1) {
                    ShowItem(currentItemIndex + 1);
                } else {
                    ShowResults();
                }
            }
        }
    }

    public void ReplayCurrentAudio() {
        if (items == null || currentItemIndex >= items.Length) return;
        AudioClip clip = items[currentItemIndex].readbackAudio;
        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }
    }

    private void ShowResults() {
        if (receiptCardObject != null) receiptCardObject.SetActive(false);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

        if (resultPanel == null) {
            Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
            if (t != null) resultPanel = t.gameObject;
        }

        int targetTotal = (totalQuestionsToPlay > 0 && totalQuestionsToPlay <= items.Length) ? totalQuestionsToPlay : items.Length;
        bool passed = (correctCount >= passScore);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "SHOPPING COMPLETE! 🛒" : "KEEP PRACTICING!";
                resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"You typed {correctCount} of {targetTotal} shopping words correctly!";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Success! All shopping sentences have been properly completed." : $"You need at least {passScore}/{targetTotal} correct to complete this writing topic.";
            }

            if (returnHubBtn != null) {
                returnHubBtn.gameObject.SetActive(passed);
            }

            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(!passed || correctCount < targetTotal);
            }
        }

        if (recapAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(passed);
        }

        if (passed && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    public void OnReturnHubClicked() {
        topic = Masters_Topic.Writing;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Writing;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    private void AutoBindReferences() {
        if (titleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("HeaderContainer/LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP == null) {
            Transform t = transform.Find("progression count") ?? transform.Find("ProgressTMP") ?? transform.Find("Progress") ?? transform.Find("HeaderContainer/ProgressTMP");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (promptTMP == null) {
            Transform t = transform.Find("questions text") ?? transform.Find("DeskCard/PromptTMP") ?? transform.Find("ReceiptCard/PromptTMP");
            if (t != null) promptTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (hintTMP == null) {
            Transform t = transform.Find("Hint text") ?? transform.Find("hint/Hint text");
            if (t != null) hintTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (submitButton == null) {
            Transform t = transform.Find("Check") ?? transform.Find("CheckButton") ?? transform.Find("SubmitButton");
            if (t != null) submitButton = t.GetComponent<Button>();
        }
        if (inputField == null) {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }
        if (inputField != null && inputFieldBg == null) {
            inputFieldBg = inputField.GetComponent<Image>();
        }
        if (receiptCardObject == null) {
            Transform t = transform.Find("DeskCard") ?? transform.Find("ReceiptCard") ?? transform.Find("QuestionContainer");
            if (t != null) receiptCardObject = t.gameObject;
        }
        if (resultPanel == null) {
            Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
            if (t != null) resultPanel = t.gameObject;
        }
        if (resultTitleTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultTitle") ?? resultPanel.transform.Find("Title");
            if (t != null) resultTitleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (resultScoreTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultScore") ?? resultPanel.transform.Find("Score");
            if (t != null) resultScoreTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (resultStatusTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultStatus") ?? resultPanel.transform.Find("Status");
            if (t != null) resultStatusTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (retryBtn == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
            if (t != null) retryBtn = t.GetComponent<Button>();
        }
        if (returnHubBtn == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("ReturnHubBtn") ?? resultPanel.transform.Find("NextButton");
            if (t != null) returnHubBtn = t.GetComponent<Button>();
        }
    }
}
