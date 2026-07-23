using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Reading Lesson 3 for Unit 11 Is There a Difference?
/// Implements Error Correction MCQ with Learner-Friendly Rule display.
/// </summary>
public class Masters_IsThereADifference_Reading_LessonThree : Masters_Lesson {

    [System.Serializable]
    public class QuestionData {
        public string sentenceText;
        public string targetWrongWord;
        public string correctWord;
        public string[] wrongWords;
        public string ruleText;
        public AudioClip sentenceAudioClip;
        public AudioClip ruleAudioClip;
    }

    [Header("Reading MCQ Settings")]
    [SerializeField] private QuestionData[] questions;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI sentenceTMP;
    [SerializeField] private TextMeshProUGUI progressionTMP;
    [SerializeField] private GameObject ruleTMP; // Extra UI textfield for learner-friendly grammar rule
    [SerializeField] private Masters_LessonSO nextLessonSO;
    [SerializeField] private float timeBetweenEachAnimation = 0.1f;
    [SerializeField] private float animationSpeed = 0.4f;
    [SerializeField] private float delayAfterCorrectAnswer = 3.0f;

    [Header("Sentence Move-Up Animation Settings")]
    [SerializeField] private RectTransform sentenceContainerRect;
    [SerializeField] private float targetTopAnchoredPosY = 250f;
    private Vector2 sentenceOriginalAnchoredPos;
    private bool optionsRevealed = false;

    private int currentQuestionIndex = 0;
    private List<string> currentOptions = new List<string>();

    private RectTransform mistakeOverlayRect;

    protected override void Awake() {
        base.Awake();

        if (optionButtons != null) {
            foreach (Button optionButton in optionButtons) {
                if (optionButton != null) {
                    optionButton.onClick.AddListener(() => {
                        OnOptionButtonClicked(optionButton);
                    });
                }
            }
        }

        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }

        if (sentenceContainerRect == null && sentenceTMP != null) {
            sentenceContainerRect = sentenceTMP.rectTransform.parent as RectTransform ?? sentenceTMP.rectTransform;
        }
        if (sentenceContainerRect != null) {
            sentenceOriginalAnchoredPos = sentenceContainerRect.anchoredPosition;
        }

        if (sentenceTMP != null) {
            sentenceTMP.raycastTarget = true;
            var linkHandler = sentenceTMP.gameObject.GetComponent<Masters_LinkClickHandler>();
            if (linkHandler == null) linkHandler = sentenceTMP.gameObject.AddComponent<Masters_LinkClickHandler>();
            linkHandler.onClickAction = OnSentenceBackgroundClick;
        }
    }

    private void OnSentenceBackgroundClick(PointerEventData eventData) {
        if (optionsRevealed || sentenceTMP == null) return;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        if (sentenceContainerRect != null) {
            sentenceContainerRect.DOShakePosition(0.3f, new Vector3(10f, 0f, 0f));
        }
    }

    private Coroutine firstQuestionRoutine;

    protected override void Start() {
        base.Start();
        if (firstQuestionRoutine != null) StopCoroutine(firstQuestionRoutine);
        firstQuestionRoutine = StartCoroutine(DelayFirstQuestionLoad());
    }

    private IEnumerator DelayFirstQuestionLoad() {
        if (sentenceTMP != null) sentenceTMP.text = "";
        if (ruleTMP != null) ruleTMP.SetActive(false);
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.gameObject.SetActive(false);
            }
        }

        yield return new WaitForSeconds(4.5f);

        currentQuestionIndex = 0;
        if (questions != null && questions.Length > 0) {
            LoadQuestion(0);
        }
    }

    private void LoadQuestion(int index) {
        currentQuestionIndex = index;
        optionsRevealed = false;

        if (sentenceContainerRect != null) {
            sentenceContainerRect.DOKill();
            sentenceContainerRect.anchoredPosition = sentenceOriginalAnchoredPos;
        }

        if (progressionTMP != null && questions != null) {
            progressionTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        }

        if (questions == null || currentQuestionIndex >= questions.Length) return;

        QuestionData question = questions[currentQuestionIndex];

        if (sentenceTMP != null) {
            sentenceTMP.text = question.sentenceText;
        }

        if (question.sentenceAudioClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(question.sentenceAudioClip);
        }

        if (ruleTMP != null) {
            ruleTMP.SetActive(false); // Hide rule initially until player taps to reveal options
            var tmp = ruleTMP.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = question.ruleText;
        }

        currentOptions.Clear();
        currentOptions.Add(question.correctWord);
        if (question.wrongWords != null) {
            currentOptions.AddRange(question.wrongWords);
        }

        // Rule 4: Shuffling options evenly across buttons A, B, C, D
        currentOptions = currentOptions.OrderBy(x => Guid.NewGuid()).ToList();

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].transform.localScale = Vector3.zero;
                    optionButtons[i].gameObject.SetActive(false);

                    if (i < currentOptions.Count) {
                        TextMeshProUGUI optionText = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (optionText != null) {
                            optionText.text = currentOptions[i];
                        }
                    }
                }
            }
        }

        if (sentenceTMP != null) {
            StartCoroutine(PositionOverlayButton());
        }
    }

    private IEnumerator PositionOverlayButton() {
        if (mistakeOverlayRect != null) mistakeOverlayRect.gameObject.SetActive(false);
        yield return null; // Wait 1 frame for UI layout and font mesh rebuild
        if (sentenceTMP == null || questions == null || currentQuestionIndex >= questions.Length) yield break;

        QuestionData question = questions[currentQuestionIndex];
        sentenceTMP.ForceMeshUpdate();

        int targetFirstCharIdx = -1;
        int targetLastCharIdx = -1;

        if (sentenceTMP.textInfo != null && sentenceTMP.textInfo.wordInfo != null && !string.IsNullOrEmpty(question.targetWrongWord)) {
            var targetTokens = question.targetWrongWord.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            char[] punct = new char[] { '.', ',', '!', '?', ';', ':', ' ' };
            for (int i = 0; i <= sentenceTMP.textInfo.wordCount - targetTokens.Length; i++) {
                bool match = true;
                for (int t = 0; t < targetTokens.Length; t++) {
                    string wText = sentenceTMP.textInfo.wordInfo[i + t].GetWord();
                    string cleanWord = wText.Trim(punct);
                    string cleanToken = targetTokens[t].Trim(punct);
                    if (!cleanWord.Equals(cleanToken, StringComparison.OrdinalIgnoreCase)) {
                        match = false;
                        break;
                    }
                }
                if (match && targetTokens.Length > 0) {
                    targetFirstCharIdx = sentenceTMP.textInfo.wordInfo[i].firstCharacterIndex;
                    targetLastCharIdx = sentenceTMP.textInfo.wordInfo[i + targetTokens.Length - 1].lastCharacterIndex;
                    break;
                }
            }
        }

        if (targetFirstCharIdx < 0 && !string.IsNullOrEmpty(question.targetWrongWord)) {
            int charIdx = sentenceTMP.text.IndexOf(question.targetWrongWord, StringComparison.OrdinalIgnoreCase);
            if (charIdx >= 0) {
                targetFirstCharIdx = charIdx;
                targetLastCharIdx = charIdx + question.targetWrongWord.Length - 1;
            }
        }

        if (targetFirstCharIdx >= 0 && targetLastCharIdx < sentenceTMP.textInfo.characterInfo.Length) {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int c = targetFirstCharIdx; c <= targetLastCharIdx && c < sentenceTMP.textInfo.characterInfo.Length; c++) {
                if (!sentenceTMP.textInfo.characterInfo[c].isVisible) continue;
                minX = Mathf.Min(minX, sentenceTMP.textInfo.characterInfo[c].bottomLeft.x);
                minY = Mathf.Min(minY, sentenceTMP.textInfo.characterInfo[c].bottomLeft.y);
                maxX = Mathf.Max(maxX, sentenceTMP.textInfo.characterInfo[c].topRight.x);
                maxY = Mathf.Max(maxY, sentenceTMP.textInfo.characterInfo[c].topRight.y);
            }

            Vector3 wordCenter;
            Vector2 wordSize;
            if (minX < maxX && minY < maxY) {
                wordCenter = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);
                wordSize = new Vector2(Mathf.Max(maxX - minX + 60f, 220f), Mathf.Max(maxY - minY + 40f, 100f));
            } else {
                Vector3 bottomLeft = sentenceTMP.textInfo.characterInfo[targetFirstCharIdx].bottomLeft;
                Vector3 topRight = sentenceTMP.textInfo.characterInfo[targetLastCharIdx].topRight;
                wordCenter = (bottomLeft + topRight) / 2f;
                wordSize = new Vector2(Mathf.Max(topRight.x - bottomLeft.x + 120f, 220f), Mathf.Max(topRight.y - bottomLeft.y + 100f, 150f));
            }

            if (mistakeOverlayRect == null) {
                GameObject overlayGo = new GameObject("MistakeTapOverlay");
                overlayGo.transform.SetParent(sentenceTMP.transform, false);
                mistakeOverlayRect = overlayGo.AddComponent<RectTransform>();
                
                Image img = overlayGo.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.002f); // Non-zero alpha guarantees 100% reliable graphic raycast hits across all Unity UI modes
                img.raycastTarget = true;

                Button btn = overlayGo.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(RevealOptions);
            }

            mistakeOverlayRect.gameObject.SetActive(true);
            mistakeOverlayRect.localPosition = wordCenter;
            mistakeOverlayRect.sizeDelta = wordSize;
        }
    }

    public void RevealOptions() {
        if (optionsRevealed) return;
        optionsRevealed = true;

        if (mistakeOverlayRect != null) {
            mistakeOverlayRect.gameObject.SetActive(false);
        }

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);

        if (sentenceContainerRect != null) {
            sentenceContainerRect.DOAnchorPosY(targetTopAnchoredPosY, animationSpeed).SetEase(Ease.OutQuad);
        }

        StartCoroutine(AnimateOptionsIn());
    }

    private IEnumerator AnimateOptionsIn() {
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null && i < currentOptions.Count) {
                    optionButtons[i].gameObject.SetActive(true);
                    yield return new WaitForSeconds(timeBetweenEachAnimation);
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
                    optionButtons[i].transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
                }
            }
        }
    }

    private void OnOptionButtonClicked(Button clickedButton) {
        TextMeshProUGUI optionText = clickedButton.GetComponentInChildren<TextMeshProUGUI>();
        if (optionText == null || questions == null || currentQuestionIndex >= questions.Length) return;

        QuestionData question = questions[currentQuestionIndex];

        if (optionText.text == question.correctWord) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            if (question.ruleAudioClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(question.ruleAudioClip);
            }

            if (optionButtons != null) {
                foreach (var btn in optionButtons) {
                    if (btn != null) btn.interactable = false;
                }
            }

            if (ruleTMP != null) {
                ruleTMP.SetActive(true);
                ruleTMP.transform.localScale = Vector3.zero;
                ruleTMP.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }

            StartCoroutine(WaitAndLoadNextQuestion());
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            clickedButton.transform.DOShakePosition(0.3f, 5f);
        }
    }

    private IEnumerator WaitAndLoadNextQuestion() {
        yield return new WaitForSeconds(delayAfterCorrectAnswer);

        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.interactable = true;
            }
        }

        if (currentQuestionIndex + 1 < questions.Length) {
            LoadQuestion(currentQuestionIndex + 1);
        } else {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            NextButtonAnimation();
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

public class Masters_LinkClickHandler : MonoBehaviour, IPointerClickHandler {
    public Action<PointerEventData> onClickAction;
    public void OnPointerClick(PointerEventData eventData) {
        onClickAction?.Invoke(eventData);
    }
}
