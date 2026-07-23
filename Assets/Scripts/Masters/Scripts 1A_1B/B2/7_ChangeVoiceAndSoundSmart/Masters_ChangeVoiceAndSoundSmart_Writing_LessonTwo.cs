using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ChangeVoiceAndSoundSmart_Writing_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class TabData {
        [Tooltip("The text prompt that will be shown in the panel for this tab")]
        [TextArea(3, 5)]
        public string promptText;

        [Tooltip("Acceptable passive sentences for this tab. Punctuation and case are ignored.")]
        public string[] acceptableCorrectSentences;
        
        [HideInInspector]
        public bool isCompleted = false;
    }

    [Header("Tab Data (3 Required)")]
    [SerializeField]
    private TabData[] tabDatas;

    [Header("UI Elements")]
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private Transform singleTextPanel;
    [SerializeField] private TextMeshProUGUI singlePanelText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Animation & Feedback")]
    [SerializeField] private float timeBetweenQuestions = 1.5f;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultInputFieldColor = Color.white;
    [SerializeField] private Image inputFieldBackground;

    private int activeTabIndex = -1;
    private bool canCheck = false;

    protected override void Awake() {
        base.Awake();
        checkButton.onClick.AddListener(OnCheckButtonClicked);
        
        for (int i = 0; i < tabButtons.Length; i++) {
            int index = i; // capture index for closure
            tabButtons[i].onClick.AddListener(() => OnTabClicked(index));
        }
    }

    protected override void Start() {
        base.Start();
        
        // Hide input field and check button initially
        inputField.gameObject.SetActive(false);
        checkButton.gameObject.SetActive(false);
        
        // Set scale to 0 so the panel is hidden until a tab is clicked
        if (singleTextPanel != null) {
            singleTextPanel.localScale = Vector3.zero;
        }

        // Prompt the player to click the first tab instead of opening it automatically
        if (tabButtons != null && tabButtons.Length > 0 && tabButtons[0] != null) {
            AnimateTabButton(0);
        }
    }

    private void AnimateTabButton(int index) {
        if (index >= tabButtons.Length || tabButtons[index] == null) return;
        
        Transform btnTransform = tabButtons[index].transform;
        btnTransform.DOKill();
        btnTransform.localScale = Vector3.one;
        // Infinite slow pulse to draw attention
        btnTransform.DOScale(1.05f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }

    private void OnTabClicked(int index) {
        if (index >= tabDatas.Length || activeTabIndex == index) return;

        // Kill the pulse animation and reset scale when clicked
        if (index < tabButtons.Length && tabButtons[index] != null) {
            tabButtons[index].transform.DOKill();
            tabButtons[index].transform.localScale = Vector3.one;
        }

        activeTabIndex = index;
        
        // Kill existing animations
        if (singleTextPanel != null) singleTextPanel.DOKill();

        Sequence seq = DOTween.Sequence();
        
        // If it's already visible, shrink it first
        if (singleTextPanel.localScale.y > 0.1f) {
            seq.Append(singleTextPanel.DOScale(new Vector3(1f, 0f, 1f), 0.15f).SetEase(Ease.InQuad));
        }

        // Mid-point swap
        seq.AppendCallback(() => {
            if (singlePanelText != null) {
                singlePanelText.text = tabDatas[index].promptText;
            }

            // Reset the shared input field text
            inputField.text = "";
            
            if (tabDatas[index].isCompleted) {
                inputField.gameObject.SetActive(false);
                checkButton.gameObject.SetActive(false);
            } else {
                inputField.gameObject.SetActive(true);
                checkButton.gameObject.SetActive(true);
                canCheck = true;
                checkButton.interactable = true;
                if (inputFieldBackground != null) inputFieldBackground.color = defaultInputFieldColor;
            }
        });

        // Pop open
        seq.Append(singleTextPanel.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack));
    }

    private void OnCheckButtonClicked() {
        if (!canCheck || activeTabIndex < 0) return;

        string input = inputField.text.Trim();
        if (string.IsNullOrEmpty(input)) {
            // Give a little shake if empty
            inputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            return;
        }

        TabData currentTab = tabDatas[activeTabIndex];
        bool isCorrect = false;
        string normalizedInput = string.Join(" ", ExtractWords(input));

        if (currentTab.acceptableCorrectSentences != null && currentTab.acceptableCorrectSentences.Length > 0) {
            foreach (string correctVariation in currentTab.acceptableCorrectSentences) {
                if (string.IsNullOrEmpty(correctVariation)) continue;
                
                string normalizedCorrect = string.Join(" ", ExtractWords(correctVariation));
                if (normalizedInput == normalizedCorrect) {
                    isCorrect = true;
                    break;
                }
            }
        }

        if (!isCorrect) {
            WrongAnswer();
            return;
        }

        CorrectAnswer();
    }

    private List<string> ExtractWords(string sentence) {
        // Remove common terminal/separating punctuation
        string clean = sentence.Replace(",", "").Replace(".", "").Replace("?", "").Replace("!", "").Replace(";", "").Replace(":", "").ToLower();
        return clean.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private void CorrectAnswer() {
        canCheck = false;
        checkButton.interactable = false;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

        if (inputFieldBackground != null) {
            inputFieldBackground.color = correctColor;
        }

        tabDatas[activeTabIndex].isCompleted = true;

        // Check if all tabs have been successfully completed
        bool allCompleted = true;
        foreach (var tab in tabDatas) {
            if (!tab.isCompleted) {
                allCompleted = false;
                break;
            }
        }

        if (allCompleted) {
            Invoke(nameof(OnLessonCompleteSequence), timeBetweenQuestions);
        } else {
            // Wait a moment then animate the next unfinished tab button to draw attention
            Invoke(nameof(AnimateNextUncompletedTab), timeBetweenQuestions);
        }
    }
    
    private void AnimateNextUncompletedTab() {
        for (int i = 0; i < tabDatas.Length; i++) {
            if (!tabDatas[i].isCompleted && i < tabButtons.Length) {
                AnimateTabButton(i);
                return;
            }
        }
    }

    private void OnLessonCompleteSequence() {
        inputField.gameObject.SetActive(false);
        checkButton.gameObject.SetActive(false);
        
        nextButton.interactable = true;
        NextButtonAnimation();
    }

    private void WrongAnswer() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        
        if (inputFieldBackground != null) {
            inputFieldBackground.DOKill();
            inputFieldBackground.DOColor(incorrectColor, 0.2f).OnComplete(() => {
                inputFieldBackground.DOColor(defaultInputFieldColor, 0.3f);
            });
        }
        
        inputField.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);
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
