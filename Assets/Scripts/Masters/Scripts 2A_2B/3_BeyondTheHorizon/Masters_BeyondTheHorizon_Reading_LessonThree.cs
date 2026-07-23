using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Reading 3 controller for Unit 3: Beyond the Horizon (Book 2A).
/// R03 — Read the Map — Choose the Correct Direction (across 6 map rounds).
/// Displays a map situation prompt, illustrated map graphic, and 4 direction phrase chip buttons.
/// Pure reading focus: No phrase voiceovers played on selection.
/// </summary>
public class Masters_BeyondTheHorizon_Reading_LessonThree : Masters_Lesson {

    [System.Serializable]
    public class MapSituationRoundData {
        [TextArea(2, 3)] public string mapSituationText;
        public string correctPhrase;
        public string[] wrongDistractors;
    }

    [Header("Reading L03 Setup")]
    [SerializeField] private MapSituationRoundData[] rounds;
    [SerializeField] private TextMeshProUGUI shownSituationTMP;
    [SerializeField] private Button[] optionButtons; // 4 chip buttons
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.35f;
#pragma warning disable 0414
    [SerializeField] private int passThreshold = 5;
#pragma warning restore 0414

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentRoundIndex = 0;
    private int correctScore = 0;
    private bool canClick = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;

        if (optionButtons == null || optionButtons.Length == 0) {
            AutoFindOptionButtons();
        }
        if (shownSituationTMP == null) {
            AutoFindSituationTMP();
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int index = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }
        }
    }

    protected override void Start() {
        base.Start();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        currentRoundIndex = 0;
        correctScore = 0;
        StartCoroutine(InitializeLessonRoutine());
    }

    private void AutoFindOptionButtons() {
        Transform container = transform.Find("Options");
        if (container == null) container = transform.Find("OptionsContainer");
        if (container == null) container = FindChildRecursive(transform, "Options");
        if (container == null) container = FindChildRecursive(transform, "OptionsContainer");

        List<Button> foundButtons = new List<Button>();
        if (container != null) {
            Button[] btns = container.GetComponentsInChildren<Button>(true);
            foreach (Button b in btns) {
                if (b != nextButton && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("back")) {
                    foundButtons.Add(b);
                }
            }
        } else {
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (Button b in allBtns) {
                if (b != nextButton && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("back")) {
                    foundButtons.Add(b);
                }
            }
        }

        if (foundButtons.Count > 0) {
            optionButtons = foundButtons.ToArray();
        }
    }

    private Transform FindChildRecursive(Transform parent, string name) {
        foreach (Transform child in parent) {
            if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void AutoFindSituationTMP() {
        Transform exprTransform = transform.Find("Situation");
        if (exprTransform == null) exprTransform = transform.Find("Expression");
        if (exprTransform == null) exprTransform = transform.Find("ShownExpression");
        if (exprTransform == null) exprTransform = FindChildRecursive(transform, "Situation");
        if (exprTransform == null) exprTransform = FindChildRecursive(transform, "Expression");

        if (exprTransform != null) {
            shownSituationTMP = exprTransform.GetComponent<TextMeshProUGUI>();
            if (shownSituationTMP == null) shownSituationTMP = exprTransform.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (shownSituationTMP == null) {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps) {
                if (t.name.ToLower().Contains("situation") || t.name.ToLower().Contains("expression") || t.name.ToLower().Contains("prompt")) {
                    shownSituationTMP = t;
                    break;
                }
            }
        }
    }

    private IEnumerator InitializeLessonRoutine() {
        if (Masters_AudioManager.Instance != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(1.5f);
        }

        LoadRound(0);
    }

    private void LoadRound(int index) {
        if (rounds == null || index >= rounds.Length) {
            OnAllRoundsCompleted();
            return;
        }

        currentRoundIndex = index;
        canClick = true;

        if (progressTMP != null) {
            progressTMP.text = $"{currentRoundIndex + 1}/{rounds.Length}";
        }

        MapSituationRoundData round = rounds[currentRoundIndex];
        if (round == null) return;

        if (shownSituationTMP != null) {
            shownSituationTMP.text = round.mapSituationText;
        }

        List<string> chipTexts = new List<string>();
        chipTexts.Add(round.correctPhrase);
        if (round.wrongDistractors != null) {
            foreach (string d in round.wrongDistractors) {
                if (!string.IsNullOrEmpty(d)) chipTexts.Add(d);
            }
        }

        // Shuffle chips cleanly
        for (int i = 0; i < chipTexts.Count; i++) {
            string temp = chipTexts[i];
            int rand = Random.Range(i, chipTexts.Count);
            chipTexts[i] = chipTexts[rand];
            chipTexts[rand] = temp;
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (i < chipTexts.Count) {
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;
                        optionButtons[i].transform.DOKill();
                        optionButtons[i].transform.localScale = Vector3.one;
                        SetButtonText(optionButtons[i], chipTexts[i]);
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void SetButtonText(Button btn, string text) {
        if (btn == null) return;
        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) {
            tmp.text = text;
        } else {
            Text legacy = btn.GetComponentInChildren<Text>(true);
            if (legacy != null) legacy.text = text;
        }
    }

    private string GetButtonText(Button btn) {
        if (btn == null) return "";
        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) return tmp.text;
        Text legacy = btn.GetComponentInChildren<Text>(true);
        if (legacy != null) return legacy.text;
        return "";
    }

    private void OnOptionSelected(int buttonIndex) {
        if (!canClick || rounds == null || currentRoundIndex >= rounds.Length) return;
        MapSituationRoundData round = rounds[currentRoundIndex];
        if (round == null) return;

        Button selectedBtn = (optionButtons != null && buttonIndex < optionButtons.Length) ? optionButtons[buttonIndex] : null;
        if (selectedBtn == null) return;

        string chosenText = GetButtonText(selectedBtn);
        bool isCorrect = string.Equals(chosenText, round.correctPhrase, System.StringComparison.OrdinalIgnoreCase);

        canClick = false;
        if (optionButtons != null) {
            foreach (Button b in optionButtons) {
                if (b != null) b.interactable = false;
            }
        }

        if (isCorrect) {
            correctScore++;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(ButtonFeedbackCoroutine(selectedBtn, true));
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            StartCoroutine(ButtonFeedbackCoroutine(selectedBtn, false));
        }
    }

    private IEnumerator ButtonFeedbackCoroutine(Button btn, bool isCorrect) {
        Image img = btn.GetComponent<Image>();
        Color originalColor = img != null ? img.color : Color.white;

        if (img != null) {
            img.color = isCorrect ? Color.green : new Color(1f, 0.4f, 0.4f);
        }

        if (isCorrect) {
            btn.transform.DOScale(Vector3.one * 1.1f, animationSpeed).SetLoops(2, LoopType.Yoyo);
            yield return new WaitForSeconds(1.2f);
            if (img != null) img.color = originalColor;
            LoadRound(currentRoundIndex + 1);
        } else {
            btn.transform.DOShakePosition(animationSpeed, new Vector3(15f, 0, 0), 20);
            yield return new WaitForSeconds(animationSpeed);
            if (img != null) img.color = originalColor;
            canClick = true;
            if (optionButtons != null) {
                foreach (Button b in optionButtons) {
                    if (b != null) b.interactable = true;
                }
            }
        }
    }

    private void OnAllRoundsCompleted() {
        if (correctScore >= passThreshold || correctScore >= 0) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.LogWarning($"Topic not set for {this.name}!");
            return;
        }
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

    public void SetReadingData(MapSituationRoundData[] data) {
        rounds = data;
    }
}
