using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_AbbreviationsAndAcronyms_Writing_LessonTwo : Masters_Lesson {

    public class MasterListItem {
        public string token;
        public GameObject uiElement; 
        public TextMeshProUGUI uiText; 
        public bool isUsed = false;
    }

    [Header("Game Settings")]
    [SerializeField] private int requiredSentencesToPass = 3;

    [Header("Dynamic Tokens Generation")]
    [SerializeField] private List<string> tokensToSpawn;
    [SerializeField] private GameObject tokenPrefab;
    [SerializeField] private Transform tokensContainer;

    private List<MasterListItem> masterList = new List<MasterListItem>();

    [Header("UI Elements")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private Image inputFieldBackground;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Animation & Feedback")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultInputFieldColor = Color.white;
    [SerializeField] private float timeBetweenQuestions = 1.5f;

    private int sentencesCompleted = 0;
    private bool isAnimating = false;

    protected override void Awake() {
        base.Awake();
        if (submitButton != null) {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }
    }

    protected override void Start() {
        base.Start();

        // Clear existing children
        if (tokensContainer != null) {
            foreach (Transform child in tokensContainer) {
                Destroy(child.gameObject);
            }
        }

        if (tokenPrefab != null && tokensContainer != null) {
            foreach (string token in tokensToSpawn) {
                GameObject spawnedObj = Instantiate(tokenPrefab, tokensContainer);
                TextMeshProUGUI spawnedText = spawnedObj.GetComponentInChildren<TextMeshProUGUI>();
                
                if (spawnedText != null) {
                    spawnedText.text = token;
                }

                MasterListItem newItem = new MasterListItem {
                    token = token,
                    uiElement = spawnedObj,
                    uiText = spawnedText,
                    isUsed = false
                };
                masterList.Add(newItem);
            }
        }

        UpdateProgressUI();
        if (inputField != null) {
            inputField.text = "";
        }
    }

    private void OnSubmitClicked() {
        if (isAnimating) return;

        string input = inputField.text.Trim();
        if (string.IsNullOrEmpty(input)) {
            inputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            return;
        }

        bool matchFound = false;
        MasterListItem matchedItem = null;

        foreach (var item in masterList) {
            if (item.isUsed) continue;

            // This regex ensures we match the token exactly, case-insensitive, 
            // and ensures it's not buried inside a longer word (e.g., checking that no letters exist immediately before or after).
            string pattern = $@"(?i)(?<![a-z]){Regex.Escape(item.token)}(?![a-z])";
            
            if (Regex.IsMatch(input, pattern)) {
                // Ensure the user typed something else besides just the token
                string withoutToken = Regex.Replace(input, pattern, "");
                if (Regex.IsMatch(withoutToken, @"[a-zA-Z0-9]")) {
                    matchFound = true;
                    matchedItem = item;
                    break;
                }
            }
        }

        if (matchFound) {
            string feedback;
            if (Masters_SentenceValidator.Validate(input, new string[] { matchedItem.token }, out feedback)) {
                CorrectAnswer(matchedItem);
            } else {
                WrongAnswer();
            }
        } else {
            WrongAnswer();
        }
    }

    private void CorrectAnswer(MasterListItem matchedItem) {
        isAnimating = true;
        submitButton.interactable = false;
        matchedItem.isUsed = true;
        
        // Visual feedback for the matched token
        if (matchedItem.uiText != null) {
            matchedItem.uiText.fontStyle = FontStyles.Strikethrough;
            matchedItem.uiText.color = Color.gray;
        } else if (matchedItem.uiElement != null) {
            matchedItem.uiElement.SetActive(false);
        }

        sentencesCompleted++;
        UpdateProgressUI();

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

        if (inputFieldBackground != null) {
            inputFieldBackground.color = correctColor;
        }

        if (sentencesCompleted >= requiredSentencesToPass) {
            Invoke(nameof(GameWon), timeBetweenQuestions);
        } else {
            Invoke(nameof(ResetForNextSentence), timeBetweenQuestions);
        }
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

    private void ResetForNextSentence() {
        inputField.text = "";
        if (inputFieldBackground != null) {
            inputFieldBackground.color = defaultInputFieldColor;
        }
        submitButton.interactable = true;
        isAnimating = false;
    }

    private void UpdateProgressUI() {
        if (progressTMP != null) {
            progressTMP.text = $"{sentencesCompleted}/{requiredSentencesToPass}";
        }
    }

    private void GameWon() {
        if (inputField != null) inputField.gameObject.SetActive(false);
        if (submitButton != null) submitButton.gameObject.SetActive(false);
        
        nextButton.interactable = true;
        NextButtonAnimation();
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
