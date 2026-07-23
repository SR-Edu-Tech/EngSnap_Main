using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Reading 1 controller for Unit 2: Clear Confusion (Book 2A).
/// R01 Pick the Right Question (in-context): Displays a classroom situation + bracketed job hint on the noticeboard.
/// Student selects the correct verbatim phrase from 4 chip options across 12 rounds.
/// </summary>
public class Masters_ClearConfusion_Reading_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class SituationRoundData {
        [TextArea(2, 3)] public string situationText;
        public string jobHint;
        public string correctPhrase;
        public AudioClip correctPhraseAudio;
        public string[] wrongDistractors;
    }

    [Header("Reading R01 Setup")]
    [SerializeField] private SituationRoundData[] rounds;
    [SerializeField] private TextMeshProUGUI shownExpressionTMP;
    [SerializeField] private Button[] optionButtons; // 4 chip buttons
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.35f;
    [SerializeField] private int passThreshold = 10;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentRoundIndex = 0;
    private int correctScore = 0;
    private bool canClick = false;
    private Dictionary<Button, Color> defaultButtonColors = new Dictionary<Button, Color>();

    private void CacheButtonColor(Button btn) {
        if (btn == null) return;
        if (!defaultButtonColors.ContainsKey(btn)) {
            Image img = btn.GetComponent<Image>();
            if (img != null) defaultButtonColors[btn] = img.color;
        }
    }

    private void RestoreButtonColor(Button btn) {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null && defaultButtonColors.ContainsKey(btn)) {
            img.color = defaultButtonColors[btn];
        }
    }

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

    private void AutoFindOptionButtons() {
        Transform container = transform.Find("Options");
        if (container == null) container = transform.Find("OptionsContainer");
        if (container == null) container = transform.Find("Chips");
        if (container == null) container = FindChildRecursive(transform, "Options");
        if (container == null) container = FindChildRecursive(transform, "OptionsContainer");

        if (container != null) {
            List<Button> btns = new List<Button>();
            foreach (Button b in container.GetComponentsInChildren<Button>(true)) {
                if (b != nextButton && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("back")) {
                    btns.Add(b);
                }
            }
            if (btns.Count > 0) optionButtons = btns.ToArray();
        }
    }

    private void AutoFindExpressionTMP() {
        Transform exprTrans = FindChildRecursive(transform, "ShownExpression");
        if (exprTrans == null) exprTrans = FindChildRecursive(transform, "ExpressionBox");
        if (exprTrans == null) exprTrans = FindChildRecursive(transform, "NoticeboardText");
        if (exprTrans != null) {
            shownExpressionTMP = exprTrans.GetComponent<TextMeshProUGUI>();
            if (shownExpressionTMP == null) shownExpressionTMP = exprTrans.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    private Transform FindChildRecursive(Transform parent, string targetName) {
        foreach (Transform child in parent) {
            if (child.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase)) {
                return child;
            }
            Transform found = FindChildRecursive(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private IEnumerator InitializeLessonRoutine() {
        if (Masters_AudioManager.Instance != null && narratorSpeech != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(narratorSpeech != null ? narratorSpeech.length + 0.5f : 2.5f);
        }

        if (rounds != null && rounds.Length > 0) {
            LoadRound(0);
        } else {
            EndLesson();
        }
    }

    private void LoadRound(int index) {
        if (rounds == null || index >= rounds.Length) {
            OnAllRoundsCompleted();
            return;
        }

        currentRoundIndex = index;
        canClick = false;

        if (progressTMP != null) {
            progressTMP.text = $"{currentRoundIndex + 1}/{rounds.Length}";
        }

        SituationRoundData round = rounds[currentRoundIndex];
        if (round == null) {
            LoadRound(currentRoundIndex + 1);
            return;
        }

        if (shownExpressionTMP != null) {
            string formattedText = $"{round.situationText}\n\n<color=#FFD700><b>[{round.jobHint}]</b></color>";
            shownExpressionTMP.text = formattedText;
            shownExpressionTMP.transform.DOKill();
            shownExpressionTMP.transform.localScale = Vector3.zero;
            shownExpressionTMP.gameObject.SetActive(true);
            shownExpressionTMP.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }

        List<string> options = new List<string>();
        options.Add(round.correctPhrase);
        if (round.wrongDistractors != null) {
            foreach (string dist in round.wrongDistractors) {
                if (!string.IsNullOrEmpty(dist) && !options.Contains(dist)) {
                    options.Add(dist);
                }
            }
        }

        // Shuffle options
        for (int i = 0; i < options.Count; i++) {
            string temp = options[i];
            int randIndex = Random.Range(i, options.Count);
            options[i] = options[randIndex];
            options[randIndex] = temp;
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (i < options.Count) {
                        string optionText = options[i];
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;
                        SetButtonText(optionButtons[i], optionText);

                        CacheButtonColor(optionButtons[i]);
                        RestoreButtonColor(optionButtons[i]);

                        optionButtons[i].transform.DOKill();
                        optionButtons[i].transform.localScale = Vector3.zero;

                        int btnIndex = i;
                        optionButtons[i].onClick.RemoveAllListeners();
                        optionButtons[i].onClick.AddListener(() => OnOptionClicked(btnIndex, optionText, round.correctPhrase, round.correctPhraseAudio));

                        optionButtons[i].transform.DOScale(Vector3.one, animationSpeed)
                            .SetDelay(i * 0.08f)
                            .SetEase(Ease.OutBack)
                            .OnComplete(() => {
                                if (btnIndex == Mathf.Min(options.Count - 1, optionButtons.Length - 1)) {
                                    canClick = true;
                                }
                            });
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        } else {
            canClick = true;
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

    private void OnOptionClicked(int buttonIndex, string selectedText, string correctText, AudioClip revealAudio) {
        if (!canClick || optionButtons == null || buttonIndex >= optionButtons.Length) return;

        Button clickedBtn = optionButtons[buttonIndex];
        if (clickedBtn == null) return;

        bool isCorrect = string.Equals(selectedText.Trim(), correctText.Trim(), System.StringComparison.OrdinalIgnoreCase);

        if (isCorrect) {
            canClick = false;
            correctScore++;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (revealAudio != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(revealAudio);
                }
            }

            Image img = clickedBtn.GetComponent<Image>();
            if (img != null) img.color = Color.green;

            clickedBtn.transform.DOScale(Vector3.one * 1.08f, 0.2f).SetLoops(2, LoopType.Yoyo);

            for (int i = 0; i < optionButtons.Length; i++) {
                if (i != buttonIndex && optionButtons[i] != null && optionButtons[i].gameObject.activeSelf) {
                    optionButtons[i].interactable = false;
                    optionButtons[i].transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
                }
            }

            StartCoroutine(WaitAndLoadNextRound(revealAudio != null ? revealAudio.length + 0.6f : 1.5f));
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            Image img = clickedBtn.GetComponent<Image>();
            if (img != null) {
                img.color = new Color(1f, 0.4f, 0.4f);
                clickedBtn.transform.DOShakePosition(0.4f, new Vector3(12f, 0, 0), 20).OnComplete(() => {
                    RestoreButtonColor(clickedBtn);
                });
            }
        }
    }

    private IEnumerator WaitAndLoadNextRound(float delay) {
        yield return new WaitForSeconds(delay);
        LoadRound(currentRoundIndex + 1);
    }

    private void OnAllRoundsCompleted() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (correctScore >= passThreshold) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                NextButtonAnimation();
            } else {
                OnNextButtonClicked();
            }
        } else {
            currentRoundIndex = 0;
            correctScore = 0;
            LoadRound(0);
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

    private void EndLesson() {
        OnAllRoundsCompleted();
    }
}
