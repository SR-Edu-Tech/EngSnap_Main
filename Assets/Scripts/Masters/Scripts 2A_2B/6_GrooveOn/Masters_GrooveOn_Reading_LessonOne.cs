using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Data structure for R01 Pick the Right Wish (in-context) rounds.
/// </summary>
[System.Serializable]
public class R01RoundData {
    [Tooltip("Situation prompt displayed on the board.")]
    public string situationText;
    [Tooltip("Bracketed kind hint (e.g. [birthday wish], [party question], [festival greeting], [preparation])")]
    public string kindHint;
    [Tooltip("Primary correct phrase chip text.")]
    public string correctPhrase;
    [Tooltip("All accepted verbatim correct phrases (e.g. Wish you a very happy birthday / Happy birthday to you).")]
    public string[] acceptedPhrases;
    [Tooltip("3 distractor phrase chips.")]
    public string[] distractors;
    [Tooltip("Optional ARIA situation audio clip.")]
    public AudioClip situationAudio;
    [Tooltip("Optional ARIA correct phrase audio clip.")]
    public AudioClip correctPhraseAudio;
}

/// <summary>
/// Controller for Unit 6 (Groove On) Reading Branch - Stage R01: Pick the Right Wish (in-context).
/// Manages 12 rounds across 4 kinds:
/// 1-3. Birthday wish
/// 4-6. Party question
/// 7-10. Festival greeting
/// 11-12. Preparation
/// Features 1-retry hint mechanism, option shuffling, ARIA feedback, and 10/12 pass threshold.
/// </summary>
public class Masters_GrooveOn_Reading_LessonOne : Masters_PolishedCommunication_Reading_LessonOne {

    [Header("R01 Round Data Bank (12 Rounds)")]
    [SerializeField] private R01RoundData[] r01Rounds;

    [Header("R01 UI References")]
    [SerializeField] private TextMeshProUGUI situationTMP;
    [SerializeField] private TextMeshProUGUI kindHintTMP;
    [SerializeField] private TextMeshProUGUI r01ProgressTMP;
    [SerializeField] private TextMeshProUGUI ariaHintTMP;
    [SerializeField] private TextMeshProUGUI scoreCountTMP;
    [SerializeField] private Button[] phraseChipButtons; // 3-4 option chips

    [Header("R01 Color Tokens")]
    [SerializeField] private Color chipDefaultColor = new Color(0.12f, 0.25f, 0.48f, 1f); // Royal Blue (#1E40AF)
    [SerializeField] private Color chipCorrectColor = new Color(0.13f, 0.77f, 0.36f, 1f); // Emerald Green (#22C55E)
    [SerializeField] private Color chipWrongColor = new Color(0.93f, 0.26f, 0.26f, 1f);   // Crimson Red (#EF4444)

    private int r01CurrentRoundIndex = 0;
    private int r01CorrectScore = 0;
    private int attemptCountCurrentRound = 0;
    private bool isInputActive = false;
    private int currentCorrectOptionIndex = 0;
    private const int TOTAL_ROUNDS = 12;
    private const int PASS_THRESHOLD = 10;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
        CleanNonIntroCharacters();
        EnsureR01RoundsInitialized();
        AutoWireUIReferences();
        if (r01Rounds != null && r01Rounds.Length > 0) {
            SetupAndShuffleAnswerChips(r01Rounds[0]);
        }
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
        base.Start();
        r01CurrentRoundIndex = 0;
        r01CorrectScore = 0;
        attemptCountCurrentRound = 0;

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        EnsureR01RoundsInitialized();
        AutoWireUIReferences();
        UpdateTitleAndUIComponents();

        if (narratorSpeech == null) {
#if UNITY_EDITOR
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Reading/What does this moment call for a wish a question a greeting or a preparation.mp3");
#endif
        }

        AudioClip introClip = narratorSpeech;
        narratorSpeech = null; // Clear so base.Start() doesn't replay it on retry

        // Load titles, situation text, and cards IMMEDIATELY on frame 1 so titles and cards display at the exact same time as audio
        LoadRound(r01CurrentRoundIndex);

        if (introClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introClip);
        }
    }

    private void EnsureR01RoundsInitialized() {
        if (r01Rounds != null && r01Rounds.Length >= 12) return;

        List<R01RoundData> list = new List<R01RoundData>();

        // 1. Birthday Wish
        list.Add(CreateR01Round("It's your friend's birthday today.", "Birthday Wish", "Wish you a very happy birthday!", new string[] { "Where's the party?", "Wish you a Happy Diwali!", "Clean the house." }));
        // 2. Birthday Wish
        list.Add(CreateR01Round("You forgot — the birthday was yesterday.", "Birthday Wish", "Belated birthday wishes!", new string[] { "When's the party?", "Merry Christmas to you!", "Do shopping for new clothes." }));
        // 3. Birthday Wish
        list.Add(CreateR01Round("You wish your friend many more birthdays.", "Birthday Wish", "Many more happy returns of the day!", new string[] { "What about the theme?", "Eid Mubarak!", "Clean the house." }));
        // 4. Party Question
        list.Add(CreateR01Round("You want to know the party's location.", "Party Question", "Where's the party?", new string[] { "Wish you a very happy birthday!", "Happy New Year!", "Do shopping for new clothes." }));
        // 5. Party Question
        list.Add(CreateR01Round("You want to know the party's timing.", "Party Question", "When's the party?", new string[] { "Belated birthday wishes!", "Wish you a Happy Diwali!", "Clean the house." }));
        // 6. Party Question
        list.Add(CreateR01Round("You want to know how the party is themed.", "Party Question", "What about the theme?", new string[] { "Many more happy returns of the day!", "Merry Christmas to you!", "Do shopping for new clothes." }));
        // 7. Festival Greeting
        list.Add(CreateR01Round("It's Diwali at your neighbour's house.", "Festival Greeting", "Wish you a Happy Diwali!", new string[] { "Where's the party?", "Wish you a very happy birthday!", "Clean the house." }));
        // 8. Festival Greeting
        list.Add(CreateR01Round("Your Christian friend celebrates in December.", "Festival Greeting", "Merry Christmas to you!", new string[] { "When's the party?", "Belated birthday wishes!", "Do shopping for new clothes." }));
        // 9. Festival Greeting
        list.Add(CreateR01Round("Your Muslim friend celebrates Eid.", "Festival Greeting", "Eid Mubarak!", new string[] { "What about the theme?", "Many more happy returns of the day!", "Clean the house." }));
        // 10. Festival Greeting
        list.Add(CreateR01Round("It's the 1st of January.", "Festival Greeting", "Happy New Year!", new string[] { "Where's the party?", "Wish you a very happy birthday!", "Do shopping for new clothes." }));
        // 11. Preparation
        list.Add(CreateR01Round("The family tidies up before the festival.", "Preparation", "Clean the house.", new string[] { "Wish you a Happy Diwali!", "When's the party?", "Belated birthday wishes!" }));
        // 12. Preparation
        list.Add(CreateR01Round("The family buys festive outfits.", "Preparation", "Do shopping for new clothes.", new string[] { "Merry Christmas to you!", "What about the theme?", "Many more happy returns of the day!" }));

        r01Rounds = list.ToArray();
    }

    private R01RoundData CreateR01Round(string sit, string hint, string correct, string[] distractors) {
        return new R01RoundData {
            situationText = sit,
            kindHint = hint,
            correctPhrase = correct,
            acceptedPhrases = new string[] { correct },
            distractors = distractors
        };
    }

    private void AutoWireUIReferences() {
        Transform gridTrans = transform.Find("PhraseCardsGrid") ?? transform.Find("OptionsGrid") ?? transform.Find("SelectionPanel");
        if (gridTrans != null) {
            Button[] gridBtns = gridTrans.GetComponentsInChildren<Button>(true);
            List<Button> optionBtns = new List<Button>();
            foreach (var btn in gridBtns) {
                if (btn == null) continue;
                string bName = btn.name.ToLower();
                if (!bName.Contains("next") && !bName.Contains("speaker") && !bName.Contains("back")) {
                    optionBtns.Add(btn);
                }
            }
            if (optionBtns.Count >= 4) {
                phraseChipButtons = optionBtns.ToArray();
            }
        }

        if (phraseChipButtons == null || phraseChipButtons.Length == 0) {
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            List<Button> optionBtns = new List<Button>();
            foreach (var btn in allBtns) {
                if (btn == null) continue;
                string bName = btn.name.ToLower();
                if (!bName.Contains("next") && !bName.Contains("speaker") && !bName.Contains("back")) {
                    optionBtns.Add(btn);
                }
            }
            if (optionBtns.Count > 0) phraseChipButtons = optionBtns.ToArray();
        }

        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string tName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";

            if (situationTMP == null && (tName == "sentence" || tName == "tmp" || textVal.Contains("birthday") || textVal.Contains("friend"))) {
                situationTMP = tmp as TextMeshProUGUI;
            }
            if (r01ProgressTMP == null && (tName.Contains("expressioncount") || tName.Contains("progress") || textVal.Contains("1/"))) {
                r01ProgressTMP = tmp as TextMeshProUGUI;
            }
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            if (tmp.GetComponentInParent<Button>() != null) continue;

            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";

            if (lowerName.Equals("lessontitletext") || lowerName.Equals("lessontitle") || lowerName.Equals("titletext") || textVal.Contains("Find the Partner") || textVal.Contains("Polished") || textVal.Contains("R01")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "R01 Pick the Right Wish (in-context)";
            }
            else if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("READING")) {
                tmp.text = "READING BRANCH (Book Stall)";
            }
        }
    }

    /// <summary>
    /// Loads the specified round index into the UI.
    /// </summary>
    public void LoadRound(int index) {
        if (r01Rounds == null || index < 0 || index >= r01Rounds.Length) {
            EvaluateFinalScoreAndComplete();
            return;
        }

        r01CurrentRoundIndex = index;
        attemptCountCurrentRound = 0;
        isInputActive = true;

        R01RoundData data = r01Rounds[r01CurrentRoundIndex];

        // Update Title dynamically to guarantee clean display
        UpdateTitleAndUIComponents();

        // Update Text Display
        if (situationTMP != null) situationTMP.text = data.situationText;
        if (kindHintTMP != null) {
            string rawKind = string.IsNullOrEmpty(data.kindHint) ? "" : data.kindHint.Trim('[', ']');
            kindHintTMP.text = $"[{rawKind}]";
        }
        if (r01ProgressTMP != null) r01ProgressTMP.text = $"Round {r01CurrentRoundIndex + 1} / {TOTAL_ROUNDS}";
        if (scoreCountTMP != null) scoreCountTMP.text = $"Score: {r01CorrectScore} / {TOTAL_ROUNDS}";
        if (ariaHintTMP != null) ariaHintTMP.text = "";

        // Play ARIA situation voiceover
        if (data.situationAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(data.situationAudio);
        }

        SetupAndShuffleAnswerChips(data);
    }

    private void SetupAndShuffleAnswerChips(R01RoundData data) {
        // Build list of options: 1 correct + up to 3 distractors
        List<(string text, bool isCorrect)> options = new List<(string, bool)>();
        if (!string.IsNullOrEmpty(data.correctPhrase)) {
            options.Add((data.correctPhrase, true));
        }

        if (data.distractors != null) {
            foreach (var distractor in data.distractors) {
                if (!string.IsNullOrEmpty(distractor)) {
                    options.Add((distractor, false));
                }
            }
        }

        // Fisher-Yates Shuffle option positions
        for (int i = options.Count - 1; i > 0; i--) {
            int rand = Random.Range(0, i + 1);
            var temp = options[i];
            options[i] = options[rand];
            options[rand] = temp;
        }

        RebuildFourOptionButtons(options);
    }

    private void RebuildFourOptionButtons(List<(string text, bool isCorrect)> options) {
        Transform gridTrans = transform.Find("PhraseCardsGrid") ?? transform.Find("OptionsGrid") ?? transform.Find("SelectionPanel");
        if (gridTrans == null) {
            GameObject gridGo = new GameObject("PhraseCardsGrid");
            gridGo.transform.SetParent(transform, false);
            gridTrans = gridGo.transform;
        }

        RectTransform gridRect = gridTrans.GetComponent<RectTransform>();
        if (gridRect != null) {
            gridRect.anchorMin = new Vector2(0.5f, 0.4f);
            gridRect.anchorMax = new Vector2(0.5f, 0.4f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = new Vector2(0f, -50f);
            gridRect.sizeDelta = new Vector2(980f, 320f);
        }

        GridLayoutGroup glg = gridTrans.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = gridTrans.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(460f, 130f);
        glg.spacing = new Vector2(30f, 20f);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 2;
        glg.childAlignment = TextAnchor.MiddleCenter;

        // Clear or prepare 4 option button children
        List<Button> builtBtns = new List<Button>();
        for (int i = 0; i < 4; i++) {
            Transform childT = gridTrans.Find($"OptionButton_{i}");
            GameObject btnGo;
            if (childT == null) {
                if (i < gridTrans.childCount) {
                    btnGo = gridTrans.GetChild(i).gameObject;
                    btnGo.name = $"OptionButton_{i}";
                } else {
                    btnGo = new GameObject($"OptionButton_{i}");
                    btnGo.transform.SetParent(gridTrans, false);
                }
            } else {
                btnGo = childT.gameObject;
            }

            btnGo.SetActive(true);

            // 1. Outer Border Card (Light cyan-gray frame matching reference image)
            Image outerBorderImg = btnGo.GetComponent<Image>();
            if (outerBorderImg == null) outerBorderImg = btnGo.AddComponent<Image>();
            outerBorderImg.enabled = true;
            outerBorderImg.raycastTarget = true;
            outerBorderImg.color = new Color(0.745f, 0.831f, 0.835f, 1.0f); // Light cyan-gray border stroke

            Button btn = btnGo.GetComponent<Button>();
            if (btn == null) btn = btnGo.AddComponent<Button>();
            btn.interactable = true;
            btn.transition = Selectable.Transition.None;

            // 2. Inner Fill Card (Deep royal navy blue body inset by 7px)
            Transform fillT = btnGo.transform.Find("InnerFill");
            GameObject fillGo;
            if (fillT == null) {
                fillGo = new GameObject("InnerFill");
                fillGo.transform.SetParent(btnGo.transform, false);
            } else {
                fillGo = fillT.gameObject;
            }
            fillGo.SetActive(true);

            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            if (fillRect == null) fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = new Vector2(-14f, -14f); // 7px thick border stroke
            fillRect.anchoredPosition = Vector2.zero;

            Image fillImg = fillGo.GetComponent<Image>();
            if (fillImg == null) fillImg = fillGo.AddComponent<Image>();
            fillImg.enabled = true;
            fillImg.raycastTarget = false;
            fillImg.color = chipDefaultColor; // Deep Royal Navy Blue

            // 3. Centered Bold White Text
            Transform textT = fillGo.transform.Find("Text") ?? fillGo.transform.Find("TMP") ?? btnGo.transform.Find("Text");
            GameObject textGo;
            if (textT == null) {
                textGo = new GameObject("Text");
                textGo.transform.SetParent(fillGo.transform, false);
            } else {
                textGo = textT.gameObject;
                textGo.transform.SetParent(fillGo.transform, false);
            }
            textGo.SetActive(true);

            RectTransform textRect = textGo.GetComponent<RectTransform>();
            if (textRect == null) textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            TMP_FontAsset howdybunFont = null;
#if UNITY_EDITOR
            howdybunFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Howdybun SDF 1.asset");
#endif
            if (howdybunFont == null) {
                howdybunFont = Resources.Load<TMP_FontAsset>("Fonts/Howdybun SDF 1");
            }

            TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.enabled = true;
            tmp.raycastTarget = false;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (howdybunFont != null) {
                tmp.font = howdybunFont;
            }
            tmp.enableAutoSizing = false;
            tmp.fontSize = 24f;
            tmp.enableWordWrapping = true;
            tmp.margin = new Vector4(12f, 8f, 12f, 8f);

            if (i < options.Count) {
                tmp.text = options[i].text;
                if (options[i].isCorrect) {
                    currentCorrectOptionIndex = i;
                }

                int buttonIdx = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnPhraseChipTapped(buttonIdx));
                builtBtns.Add(btn);

                // Staggered pop animation for all 4 buttons
                btnGo.transform.DOKill();
                btnGo.transform.localScale = Vector3.zero;
                btnGo.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(i * 0.08f);
            } else {
                btnGo.SetActive(false);
            }
        }

        // Hide any extra children in gridTrans
        for (int i = 4; i < gridTrans.childCount; i++) {
            gridTrans.GetChild(i).gameObject.SetActive(false);
        }

        phraseChipButtons = builtBtns.ToArray();
    }

    public void OnPhraseChipTapped(int selectedIndex) {
        if (!isInputActive) return;

        attemptCountCurrentRound++;
        R01RoundData currentData = r01Rounds[r01CurrentRoundIndex];

        // Check if selected option matches correctPhrase or any entry in acceptedPhrases
        bool isCorrectSelection = (selectedIndex == currentCorrectOptionIndex);
        if (phraseChipButtons != null && selectedIndex < phraseChipButtons.Length && phraseChipButtons[selectedIndex] != null) {
            TextMeshProUGUI btnTMP = phraseChipButtons[selectedIndex].GetComponentInChildren<TextMeshProUGUI>();
            if (btnTMP != null && currentData.acceptedPhrases != null) {
                foreach (string acc in currentData.acceptedPhrases) {
                    if (!string.IsNullOrEmpty(acc) && btnTMP.text.Trim().Equals(acc.Trim(), System.StringComparison.OrdinalIgnoreCase)) {
                        isCorrectSelection = true;
                        break;
                    }
                }
            }
        }

        if (isCorrectSelection) {
            // CORRECT ANSWER
            isInputActive = false;

            if (attemptCountCurrentRound == 1) {
                r01CorrectScore++;
            }

            if (scoreCountTMP != null) scoreCountTMP.text = $"Score: {r01CorrectScore} / {TOTAL_ROUNDS}";
            if (ariaHintTMP != null) ariaHintTMP.text = $"Great choice! [{currentData.kindHint.Trim('[', ']')}]";

            // Highlight chip Green
            if (phraseChipButtons != null && selectedIndex < phraseChipButtons.Length && phraseChipButtons[selectedIndex] != null) {
                phraseChipButtons[selectedIndex].transition = Selectable.Transition.None;
                Image fillImg = phraseChipButtons[selectedIndex].transform.Find("InnerFill")?.GetComponent<Image>() ?? phraseChipButtons[selectedIndex].GetComponent<Image>();
                if (fillImg != null) fillImg.color = chipCorrectColor;
                TextMeshProUGUI btnTMP = phraseChipButtons[selectedIndex].GetComponentInChildren<TextMeshProUGUI>();
                if (btnTMP != null) btnTMP.color = Color.white;
                phraseChipButtons[selectedIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (currentData.correctPhraseAudio != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(currentData.correctPhraseAudio);
                }
            }

            Invoke(nameof(AdvanceToNextRound), 1.5f);
        } else {
            // WRONG ANSWER
            if (phraseChipButtons != null && selectedIndex < phraseChipButtons.Length && phraseChipButtons[selectedIndex] != null) {
                phraseChipButtons[selectedIndex].transition = Selectable.Transition.None;
                Image fillImg = phraseChipButtons[selectedIndex].transform.Find("InnerFill")?.GetComponent<Image>() ?? phraseChipButtons[selectedIndex].GetComponent<Image>();
                if (fillImg != null) fillImg.color = chipWrongColor;
                TextMeshProUGUI btnTMP = phraseChipButtons[selectedIndex].GetComponentInChildren<TextMeshProUGUI>();
                if (btnTMP != null) btnTMP.color = Color.white;
                phraseChipButtons[selectedIndex].interactable = false;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (attemptCountCurrentRound == 1) {
                // Allow 1 retry for this round
                if (ariaHintTMP != null) ariaHintTMP.text = "Try again! Think about what the situation needs.";
                isInputActive = true;
            } else {
                // 2nd attempt failed -> Reveal correct answer & advance
                isInputActive = false;
                if (ariaHintTMP != null) ariaHintTMP.text = $"Correct answer: \"{currentData.correctPhrase}\"";

                if (phraseChipButtons != null && currentCorrectOptionIndex < phraseChipButtons.Length && phraseChipButtons[currentCorrectOptionIndex] != null) {
                    Image correctImg = phraseChipButtons[currentCorrectOptionIndex].GetComponent<Image>();
                    if (correctImg != null) correctImg.color = chipCorrectColor;
                }

                Invoke(nameof(AdvanceToNextRound), 2.0f);
            }
        }
    }

    private void AdvanceToNextRound() {
        r01CurrentRoundIndex++;
        if (r01Rounds != null && r01CurrentRoundIndex < r01Rounds.Length) {
            LoadRound(r01CurrentRoundIndex);
        } else {
            EvaluateFinalScoreAndComplete();
        }
    }

    private void EvaluateFinalScoreAndComplete() {
        isInputActive = false;

        bool isPass = r01CorrectScore >= PASS_THRESHOLD;
        string resultMsg = isPass ? "Great job! You picked the right wishes!" : "Good try! Let's practice these wishes again.";

        if (ariaHintTMP != null) ariaHintTMP.text = resultMsg;
        if (scoreCountTMP != null) scoreCountTMP.text = $"Final Score: {r01CorrectScore} / {TOTAL_ROUNDS}";

        ShowAllCompletedBanner();

        if (nextButton == null) {
            Transform nbTrans = transform.Find("NextButton") ?? transform.Find("Next Button") ?? transform.Find("Next");
            if (nbTrans != null) nextButton = nbTrans.GetComponent<Button>();
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
        Transform promptTrans = null;
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) {
            if (t != null) {
                if (fontAsset == null && t.font != null) fontAsset = t.font;
                string lowerName = t.name.ToLower();
                string txt = t.text ?? "";
                if (promptTrans == null && (lowerName.Contains("questions text") || lowerName.Contains("prompt") || lowerName.Contains("card") || txt.Contains("THE FAMILY BUYS") || txt.Contains("BIRTHDAY") || txt.Contains("LESSON"))) {
                    promptTrans = t.transform;
                }
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

        float yPos = -225f;
        if (promptTrans != null) {
            RectTransform pRect = promptTrans.GetComponent<RectTransform>();
            if (pRect != null) {
                float pBottom = pRect.anchoredPosition.y - (pRect.rect.height * (1f - pRect.pivot.y)) - 35f;
                if (pBottom < -100f && pBottom > -300f) {
                    yPos = pBottom;
                }
            }
        }

        rect.anchoredPosition = new Vector2(0f, yPos);
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

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Reading;
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Reading);
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

}