using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum Masters_GrooveOn_OccasionCategory {
    BIRTHDAY_WISH = 0,
    PARTY_QUESTION = 1,
    FESTIVAL_GREETING = 2,
    PREPARATION = 3
}

/// <summary>
/// Subclass for Unit 6 (Groove On) Listening Lesson One: Hear It - Which Occasion?
/// Displays the 4 GDD occasion categories in the Unity Inspector dropdown:
/// 0: BIRTHDAY_WISH
/// 1: PARTY_QUESTION
/// 2: FESTIVAL_GREETING
/// 3: PREPARATION
/// Preserves custom Inspector button colors during Play mode!
/// </summary>
public class Masters_GrooveOn_Listening_LessonOne : Masters_PolishedCommunication_Listening_LessonOne {

    [System.Serializable]
    public class GrooveOnListeningQuestionData {
        public AudioClip expressionAudio;
        public AudioClip slowAudio;
        public string expressionText;
        public Masters_GrooveOn_OccasionCategory correctCategory;
    }

    [Header("Unit 6 Listening L1 Data (GDD Occasion Chips)")]
    [SerializeField] private GrooveOnListeningQuestionData[] listeningQuestions;

    [Header("Unit 6 4-Chip Category Labels (GDD L01)")]
    [SerializeField] private string[] categoryLabels = new string[] {
        "BIRTHDAY WISH",
        "PARTY QUESTION",
        "FESTIVAL GREETING",
        "PREPARATION"
    };

    private List<Color> savedInspectorColors = new List<Color>();

    protected override void Awake() {
        SaveOriginalButtonColors();
        base.Awake();
        CleanNonIntroCharacters();
    }

    private void CleanNonIntroCharacters() {
        string[] charNames = new string[] {
            "Character", "LEO", "Leo", "NPCCharacter", "StudentCharacter",
            "NpcAndStudent", "CharacterImage", "Boy", "BoyCharacter",
            "Avatar", "NpcCloud", "StudentCloud"
        };

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren) {
            if (child == null || child == transform) continue;
            foreach (string name in charNames) {
                if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) {
                    child.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    protected override void Start() {
        SaveOriginalButtonColors();
        base.Start();
        topic = Masters_Topic.Listening;
        UpdateTitleAndUIComponents();
        EnsureScoreBoardHUDCreated();

        // Play VO_L01_ARIA intro voiceover when L01 starts
        if (Masters_AudioManager.Instance != null) {
            AudioClip introClip = null;
#if UNITY_EDITOR
            introClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Listening/Listen carefully to each greeting and select the matching celebration category.mp3");
#endif
            if (introClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(introClip);
            }
        }
    }

    private void SaveOriginalButtonColors() {
        if (savedInspectorColors != null && savedInspectorColors.Count > 0) return;

        if (savedInspectorColors == null) savedInspectorColors = new List<Color>();
        savedInspectorColors.Clear();

        Button[] btns = GetComponentsInChildren<Button>(true);
        foreach (var b in btns) {
            if (b != null && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("speaker") && !b.name.ToLower().Contains("back")) {
                Image img = b.GetComponent<Image>();
                if (img != null) {
                    savedInspectorColors.Add(img.color);
                } else {
                    savedInspectorColors.Add(Color.white);
                }
            }
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text;
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Occasion") || textVal.Contains("Polished") || textVal.Contains("L01")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "L01 Pick the Category — Heard Greeting";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("LISTENING")) {
                tmp.text = "LISTENING BRANCH (Audio Board)";
            }
        }
    }

    protected virtual new void ConfigureOptionButtons() {
        if (optionButtons == null || optionButtons.Length == 0) {
            Button[] btns = GetComponentsInChildren<Button>(true);
            List<Button> opts = new List<Button>();
            foreach (var b in btns) {
                if (b != null && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("speaker") && !b.name.ToLower().Contains("back")) {
                    opts.Add(b);
                }
            }
            if (opts.Count > 0) optionButtons = opts.ToArray();
        }

        if (optionButtons != null && optionButtons.Length > 0) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] == null) continue;

                bool isActive = (categoryLabels != null && i < categoryLabels.Length);
                optionButtons[i].gameObject.SetActive(isActive);
                if (isActive) {
                    optionButtons[i].transition = Selectable.Transition.None;
                    Image img = optionButtons[i].GetComponent<Image>();
                    if (img != null) {
                        img.raycastTarget = true;
                        // PRESERVE ORIGINAL INSPECTOR COLOR!
                        if (savedInspectorColors != null && i < savedInspectorColors.Count) {
                            img.color = savedInspectorColors[i];
                        }
                    }

                    TMP_Text tmp = optionButtons[i].GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null) {
                        tmp.raycastTarget = false;
                        tmp.color = Color.white;
                        tmp.text = categoryLabels[i];
                    }

                    int btnIdx = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(btnIdx));
                }
            }
        } else {
            base.ConfigureOptionButtons();
        }
    }

    protected virtual void ResetOptionButtons() {
        if (optionButtons != null && optionButtons.Length > 0) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].interactable = true;
                    Image img = optionButtons[i].GetComponent<Image>();
                    if (img != null) {
                        if (savedInspectorColors != null && i < savedInspectorColors.Count) {
                            img.color = savedInspectorColors[i];
                        }
                    }
                    TMP_Text tmp = optionButtons[i].GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null) {
                        tmp.color = Color.white;
                    }
                }
            }
        }
    }

    protected virtual new void OnOptionSelected(int buttonIndex) {
        if (isAnswering) return;

        if (listeningQuestions != null && listeningQuestions.Length > 0) {
            if (currentQuestionIndex >= listeningQuestions.Length) return;

            GrooveOnListeningQuestionData q = listeningQuestions[currentQuestionIndex];
            if (q == null) return;

            int correctIndex = (int)q.correctCategory;

            if (buttonIndex < optionButtons.Length && optionButtons[buttonIndex] != null) {
                Button selBtn = optionButtons[buttonIndex];
                Image btnImg = selBtn.GetComponent<Image>();
                TMP_Text btnTMP = selBtn.GetComponentInChildren<TMP_Text>(true);

                if (buttonIndex == correctIndex) {
                    isAnswering = true;
                    correctScore++;

                    // CORRECT CHOICE -> TURN GREEN (#22C55E)
                    if (btnImg != null) btnImg.color = new Color(0.13f, 0.77f, 0.36f, 1f);
                    if (btnTMP != null) btnTMP.color = Color.white;

                    selBtn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);

                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.StopVoiceOver();
                        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    }

                    StartCoroutine(ProceedToNextQuestionRoutine(1.2f));
                } else {
                    // WRONG CHOICE -> TURN RED (#EF4444) AND RESTORE COLOR AFTER FLASH
                    if (btnImg != null) btnImg.color = new Color(0.9f, 0.2f, 0.2f, 1f);
                    if (btnTMP != null) btnTMP.color = Color.white;

                    selBtn.transform.DOShakePosition(0.35f, 10f, 15, 90f);

                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.StopVoiceOver();
                        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
                    }

                    StartCoroutine(RestoreButtonColorRoutine(buttonIndex, 0.8f));
                }
            }
        }
    }

    private IEnumerator RestoreButtonColorRoutine(int index, float delay) {
        yield return new WaitForSeconds(delay);
        if (optionButtons != null && index < optionButtons.Length && optionButtons[index] != null) {
            Image img = optionButtons[index].GetComponent<Image>();
            if (img != null && savedInspectorColors != null && index < savedInspectorColors.Count) {
                img.color = savedInspectorColors[index];
            }
        }
    }

    private IEnumerator ProceedToNextQuestionRoutine(float delay) {
        yield return new WaitForSeconds(delay);
        currentQuestionIndex++;
        if (listeningQuestions != null && currentQuestionIndex < listeningQuestions.Length) {
            ResetOptionButtons();
            isAnswering = false;
            PlayCurrentQuestionAudio();
        } else {
            OnAllQuestionsCompleted();
        }
    }

    private void OnAllQuestionsCompleted() {
        isAnswering = true;

        ShowAllCompletedBanner();

        if (nextButton == null) {
            Transform nbTrans = transform.Find("NextButton") ?? transform.Find("Next Button") ?? transform.Find("Next");
            if (nbTrans != null) {
                nextButton = nbTrans.GetComponent<Button>();
            }
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
            NextButtonAnimation();
        }
    }

    private void ShowAllCompletedBanner() {
        TMP_FontAsset fontAsset = null;
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) {
            if (t != null && t.font != null) {
                fontAsset = t.font;
                break;
            }
        }

        Transform bannerTrans = transform.Find("AllCompletedBanner");
        GameObject bannerObj;
        if (bannerTrans == null) {
            bannerObj = new GameObject("AllCompletedBanner");
            bannerObj.transform.SetParent(transform, false);
        } else {
            bannerObj = bannerTrans.gameObject;
        }

        bannerObj.SetActive(true);
        bannerObj.transform.SetAsLastSibling();

        RectTransform rect = bannerObj.GetComponent<RectTransform>();
        if (rect == null) rect = bannerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -145f); // Safely positioned below title header
        rect.sizeDelta = new Vector2(800f, 50f);

        TextMeshProUGUI bannerText = bannerObj.GetComponent<TextMeshProUGUI>();
        if (bannerText == null) bannerText = bannerObj.AddComponent<TextMeshProUGUI>();
        bannerText.enabled = true;
        if (fontAsset != null) bannerText.font = fontAsset;
        bannerText.text = "ALL COMPLETED!";
        bannerText.fontStyle = FontStyles.Bold;
        bannerText.fontSize = 38;
        bannerText.color = new Color(1f, 0.92f, 0.23f, 1f); // Vibrant Gold
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.enableWordWrapping = false;

        bannerObj.transform.localScale = Vector3.zero;
        bannerObj.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
    }

    [Header("Score Board UI (Editable in Inspector)")]
    [SerializeField] public TextMeshProUGUI scoreBoardTMP;

    private void EnsureScoreBoardHUDCreated() {
        if (scoreBoardTMP != null) {
            scoreBoardTMP.gameObject.SetActive(true);
            UpdateScoreHUD();
            return;
        }

        Transform countT = transform.Find("PuzzleCountTMP") ?? transform.Find("ExpressionCountTMP") ?? transform.Find("ProgressTMP");
        if (countT != null) {
            scoreBoardTMP = countT.GetComponent<TextMeshProUGUI>();
            if (scoreBoardTMP != null) {
                scoreBoardTMP.gameObject.SetActive(true);
                UpdateScoreHUD();
                return;
            }
        }
        Transform boardTrans = transform.Find("ScoreBoardHUD");
        GameObject boardGo;
        if (boardTrans == null) {
            boardGo = new GameObject("ScoreBoardHUD");
            boardGo.transform.SetParent(transform, false);
        } else {
            boardGo = boardTrans.gameObject;
        }

        boardGo.SetActive(true);
        boardGo.transform.SetAsLastSibling(); // Ensure Score Board renders on top of all canvas layers

        RectTransform rect = boardGo.GetComponent<RectTransform>();
        if (rect == null) rect = boardGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-40f, -40f);
        rect.sizeDelta = new Vector2(340f, 60f);

        Image bgImg = boardGo.GetComponent<Image>();
        if (bgImg == null) bgImg = boardGo.AddComponent<Image>();
        bgImg.enabled = true;
        bgImg.color = new Color(0.08f, 0.14f, 0.28f, 0.92f); // Dark blue HUD pill background
        bgImg.raycastTarget = false;

        Transform textTrans = boardGo.transform.Find("ScoreText");
        GameObject scoreGo;
        if (textTrans == null) {
            scoreGo = new GameObject("ScoreText");
            scoreGo.transform.SetParent(boardGo.transform, false);
        } else {
            scoreGo = textTrans.gameObject;
        }

        scoreGo.SetActive(true);
        RectTransform scoreRect = scoreGo.GetComponent<RectTransform>();
        if (scoreRect == null) scoreRect = scoreGo.AddComponent<RectTransform>();
        scoreRect.anchorMin = Vector2.zero;
        scoreRect.anchorMax = Vector2.one;
        scoreRect.offsetMin = new Vector2(10f, 4f);
        scoreRect.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI tmp = scoreGo.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = scoreGo.AddComponent<TextMeshProUGUI>();
        tmp.enabled = true;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14;
        tmp.fontSizeMax = 24;
        tmp.alignment = TextAlignmentOptions.Center;

        scoreBoardTMP = tmp;
        UpdateScoreHUD();
    }

    private void UpdateScoreHUD() {
        if (scoreBoardTMP == null) return;
        int total = (listeningQuestions != null && listeningQuestions.Length > 0) ? listeningQuestions.Length : 6;
        int qNum = Mathf.Min(currentQuestionIndex + 1, total);
        scoreBoardTMP.text = $"SCORE: {correctScore * 100}   |   PROGRESS: {qNum}/{total}";
    }

    private void PlayCurrentQuestionAudio() {
        UpdateScoreHUD();
        if (listeningQuestions != null && currentQuestionIndex < listeningQuestions.Length) {
            GrooveOnListeningQuestionData q = listeningQuestions[currentQuestionIndex];
            if (q != null && q.expressionAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(q.expressionAudio);
            }
        }
    }
}