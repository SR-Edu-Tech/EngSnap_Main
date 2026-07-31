using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Reading 3 controller for Unit 1: Boost Someone Up!
/// Based on Book 2A PolishedCommunication_Reading_LessonOne (Find the Partner), adapted for audio playback on correct selection.
/// </summary>
public class Masters_ConditionalClause_Reading_LessonThree : Masters_Lesson {

    [System.Serializable]
    public class PartnerRoundData {
        public string shownExpression;
        public string correctPartner;
        public string[] wrongDistractors;
        public AudioClip correctAudio; // Added to play ARIA VO on correct
    }

    [Header("Reading L3 Setup")]
    [SerializeField] private PartnerRoundData[] rounds;
    [SerializeField] private TextMeshProUGUI shownExpressionTMP;
    [SerializeField] private Button[] optionButtons; // 4 chip buttons
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.35f;
    [SerializeField] private int passThreshold = 8;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentRoundIndex = 0;
    private int correctScore = 0;
    private bool canClick = false;

#if UNITY_EDITOR
    private void Reset() {
        AutoFindOptionButtons();
        AutoFindExpressionTMP();
    }
#endif

    protected override void Awake() {
        base.Awake();

        if (optionButtons == null || optionButtons.Length == 0) {
            AutoFindOptionButtons();
        }
        if (shownExpressionTMP == null) {
            AutoFindExpressionTMP();
        }
    }

    protected override void Start() {
        base.Start();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        currentRoundIndex = 0;
        correctScore = 0;

        EnsureLessonTitleAndExpressionBox();

        // Immediately hide and scale down UI chips and expression box on frame 0 during initial audio
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) {
                    btn.transform.DOKill();
                    btn.transform.localScale = Vector3.zero;
                    btn.gameObject.SetActive(false);
                }
            }
        }
        if (shownExpressionTMP != null) {
            shownExpressionTMP.transform.DOKill();
            shownExpressionTMP.transform.localScale = Vector3.zero;
        }

        StartCoroutine(InitializeLessonRoutine());
    }

    private void EnsureLessonTitleAndExpressionBox() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;

            // Ensure LessonTitle is properly styled and active
            if (tmp.name.ToLower().Contains("lessontitle") || tmp.name.ToLower().Contains("title") || tmp.text.Contains("Find the Partner") || tmp.text.Contains("R01") || tmp.text.Contains("R03")) {
                tmp.gameObject.SetActive(true);
                if (tmp.text.Contains("Hear") || tmp.text.Contains("Listen") || string.IsNullOrWhiteSpace(tmp.text)) {
                    tmp.text = "R03 Finish the Kind Line";
                }
                continue;
            }

            // Identify and setup shownExpressionTMP if null
            if (shownExpressionTMP == null && tmp != progressTMP && (tmp.name == "TMP" || tmp.text.Contains("Hear the Phrase") || tmp.text.Contains("FORMAL") || tmp.text.Contains("INFORMAL"))) {
                shownExpressionTMP = tmp as TextMeshProUGUI;
            }
        }

        if (shownExpressionTMP == null) {
            AutoFindExpressionTMP();
        }

        if (shownExpressionTMP != null) {
            shownExpressionTMP.gameObject.SetActive(true);
            RectTransform rect = shownExpressionTMP.rectTransform;
            if (rect != null) {
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 1100f), Mathf.Max(rect.sizeDelta.y, 160f));
            }
            shownExpressionTMP.enableWordWrapping = true;
            shownExpressionTMP.alignment = TextAlignmentOptions.Center;
            shownExpressionTMP.fontSize = Mathf.Min(shownExpressionTMP.fontSize, 42f);
        }

        // Disable any leftover audio/instruction lines from old base prefab without disabling title
        foreach (var tmp in allTMPs) {
            if (tmp != null && tmp != shownExpressionTMP && tmp != progressTMP && !tmp.name.ToLower().Contains("title") && !tmp.name.ToLower().Contains("lessontitle")) {
                bool isOptionText = tmp.transform.parent != null && (tmp.transform.parent.name.ToLower().Contains("option") || tmp.transform.parent.name.ToLower().Contains("button"));
                if (!isOptionText && (tmp.text.Contains("Hear the Phrase") || tmp.text.Contains("Pick the Words") || tmp.text.Contains("Sort the words"))) {
                    tmp.gameObject.SetActive(false);
                }
            }
        }
    }

    private void AutoFindOptionButtons() {
        List<Button> foundBtns = new List<Button>();
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in allButtons) {
            if (btn != null && btn != nextButton && btn.name.ToLower().Contains("option")) {
                foundBtns.Add(btn);
            }
        }
        if (foundBtns.Count == 0 && allButtons.Length >= 4) {
            for (int i = 0; i < Mathf.Min(4, allButtons.Length); i++) {
                if (allButtons[i] != nextButton) {
                    foundBtns.Add(allButtons[i]);
                }
            }
        }
        optionButtons = foundBtns.ToArray();
    }

    private void AutoFindExpressionTMP() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp != null && tmp != progressTMP && !tmp.name.ToLower().Contains("title") && !tmp.name.ToLower().Contains("lessontitle")) {
                bool isOption = tmp.transform.parent != null && (tmp.transform.parent.name.ToLower().Contains("option") || tmp.transform.parent.name.ToLower().Contains("button"));
                if (!isOption && (tmp.name == "TMP" || tmp.text.Contains("FORMAL") || tmp.text.Contains("Hear the Phrase"))) {
                    shownExpressionTMP = tmp as TextMeshProUGUI;
                    break;
                }
            }
        }
    }

    private IEnumerator InitializeLessonRoutine() {
        if (Masters_AudioManager.Instance != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        } else {
            yield return new WaitForSeconds(1.0f);
        }
        LoadRound(0);
    }

    private void LoadRound(int roundIdx) {
        if (rounds == null || roundIdx >= rounds.Length) {
            OnAllRoundsCompleted();
            return;
        }

        currentRoundIndex = roundIdx;
        canClick = false;

        if (progressTMP != null) {
            progressTMP.text = $"{currentRoundIndex + 1}/{rounds.Length}";
        }

        PartnerRoundData round = rounds[currentRoundIndex];
        if (round == null) return;

        if (shownExpressionTMP != null) {
            shownExpressionTMP.text = round.shownExpression;
            shownExpressionTMP.transform.DOKill();
            shownExpressionTMP.transform.localScale = Vector3.zero;
            shownExpressionTMP.gameObject.SetActive(true);
            shownExpressionTMP.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }

        List<string> options = new List<string>();
        options.Add(round.correctPartner);
        if (round.wrongDistractors != null) {
            foreach (string dist in round.wrongDistractors) {
                if (options.Count < (optionButtons != null ? optionButtons.Length : 4)) {
                    options.Add(dist);
                }
            }
        }

        // Shuffle options evenly across buttons
        for (int i = 0; i < options.Count; i++) {
            string temp = options[i];
            int rand = Random.Range(i, options.Count);
            options[i] = options[rand];
            options[rand] = temp;
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    Button currentBtn = optionButtons[i];
                    currentBtn.onClick.RemoveAllListeners();

                    if (i < options.Count) {
                        string optionTextStr = options[i];
                        SetButtonText(currentBtn, optionTextStr);

                        currentBtn.onClick.AddListener(() => OnOptionSelected(currentBtn, optionTextStr, round.correctPartner));

                        currentBtn.transform.DOKill();
                        currentBtn.transform.localScale = Vector3.zero;
                        currentBtn.gameObject.SetActive(true);
                        currentBtn.transform.DOScale(Vector3.one, animationSpeed).SetDelay(i * 0.08f).SetEase(Ease.OutBack);
                    } else {
                        currentBtn.gameObject.SetActive(false);
                    }
                }
            }
        }

        StartCoroutine(EnableClickAfterDelay(animationSpeed + 0.35f));
    }

    private IEnumerator EnableClickAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        canClick = true;
    }

    private void SetButtonText(Button btn, string text) {
        if (btn == null) return;
        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) {
            tmp.text = text;
            tmp.enableWordWrapping = true;
            tmp.alignment = TextAlignmentOptions.Center;
        } else {
            Text legacy = btn.GetComponentInChildren<Text>(true);
            if (legacy != null) {
                legacy.text = text;
                legacy.alignment = TextAnchor.MiddleCenter;
            }
        }
    }

    private void OnOptionSelected(Button selectedBtn, string chosenText, string correctPartner) {
        if (!canClick || selectedBtn == null) return;

        if (chosenText == correctPartner) {
            // Correct
            canClick = false;
            correctScore++;
            if (shownExpressionTMP != null) {
                string fillText = chosenText;
                if (fillText.ToLower() == "nothing") {
                    string text = rounds[currentRoundIndex].shownExpression;
                    text = text.Replace("<color=red>______</color> ", "");
                    text = text.Replace(" <color=red>______</color>", "");
                    text = text.Replace("______ ", "");
                    text = text.Replace(" ______", "");
                    shownExpressionTMP.text = text;
                } else {
                    string text = rounds[currentRoundIndex].shownExpression;
                    string greenText = $"<color=green>{fillText}</color>";
                    text = text.Replace("<color=red>______</color>", greenText);
                    text = text.Replace("<color=red>_____</color>", greenText);
                    text = text.Replace("______", greenText);
                    text = text.Replace("_____", greenText);
                    shownExpressionTMP.text = text;
                }
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (rounds[currentRoundIndex].correctAudio != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(rounds[currentRoundIndex].correctAudio);
                }
            }

            if (selectedBtn.TryGetComponent(out Masters_ButtonPunchAnimator punch)) {
                punch.Punch();
            } else {
                selectedBtn.transform.DOKill();
                selectedBtn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            StartCoroutine(NextRoundRoutine());
        } else {
            // Wrong
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            selectedBtn.transform.DOKill(true);
            selectedBtn.transform.DOShakePosition(0.4f, new Vector3(12f, 0, 0));
        }
    }

    private IEnumerator NextRoundRoutine() {
        if (rounds[currentRoundIndex].correctAudio != null && Masters_AudioManager.Instance != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        } else {
            yield return new WaitForSeconds(0.6f);
        }
        LoadRound(currentRoundIndex + 1);
    }

    private void OnAllRoundsCompleted() {
        if (correctScore >= passThreshold) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                NextButtonAnimation();
            }
        } else {
            currentRoundIndex = 0;
            correctScore = 0;
            LoadRound(0);
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
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}

