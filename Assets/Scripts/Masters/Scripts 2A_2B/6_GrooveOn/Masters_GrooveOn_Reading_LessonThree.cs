using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Data structure for Stage R03: Order the Preparations rounds.
/// </summary>
[System.Serializable]
public class R03RoundData {
    public string roundTitle;
    public string[] preparationCards; // 4 verbatim cards
    public AudioClip ariaPromptAudio;
}

/// <summary>
/// Controller for Unit 6 (Groove On) Reading Branch - Stage R03: Get Ready for the Festival — Order the Preparations.
/// Manages 3 rounds of sequence ordering for 4 preparation cards.
/// Validation Rule: INDEX(CLEAN) < INDEX(DECORATE) (Clean must happen before Decorate).
/// Pass Threshold: >= 2 / 3 valid orders.
/// </summary>
public class Masters_GrooveOn_Reading_LessonThree : Masters_PolishedCommunication_Reading_LessonThree {

    [Header("R03 Round Data Bank (3 Rounds)")]
    [SerializeField] private R03RoundData[] r03Rounds;

    [Header("R03 UI References")]
    [SerializeField] private TextMeshProUGUI r03TitleTMP;
    [SerializeField] private TextMeshProUGUI r03ProgressTMP;
    [SerializeField] private TextMeshProUGUI ariaHintTMP;
    [SerializeField] private Button checkButton;
    [SerializeField] private Transform[] orderSlots; // 4 order slot containers
    [SerializeField] private Button[] cardButtons; // 4 preparation card buttons

    [Header("R03 House Celebration Visuals")]
    [SerializeField] private GameObject houseVisualReadyEffect;

    private int r03CurrentRoundIndex = 0;
    private int validOrdersCount = 0;
    private const int TOTAL_ROUNDS = 3;
    private const int PASS_THRESHOLD = 2;
    private int[] slotToCardMapping = new int[] { -1, -1, -1, -1 };
    private int selectedCardIndex = -1;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
        AutoWireUIReferences();
    }

    protected override void Start() {
        // Do NOT call base.Start() to prevent launching base Unit 1 KEEP/FIX coroutines
        topic = Masters_Topic.Reading;
        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        r03CurrentRoundIndex = 0;
        validOrdersCount = 0;

        AutoWireUIReferences();
        HideObsoleteUnit1UI();
        UpdateTitleAndUIComponents();

        if (checkButton != null) {
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        }

        if (narratorSpeech == null) {
#if UNITY_EDITOR
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Reading/Before we decorate what must we do first.mp3");
#endif
        }

        AudioClip introClip = narratorSpeech;
        narratorSpeech = null;

        // Load titles, cards, and UI IMMEDIATELY on frame 1 so titles and cards display at the exact same time as audio
        LoadR03Round(r03CurrentRoundIndex);

        if (introClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introClip);
        }
    }

    private void HideObsoleteUnit1UI() {
        Transform sentenceTmt = transform.Find("Sentence tmt");
        if (sentenceTmt != null) sentenceTmt.gameObject.SetActive(false);

        Transform ruleObj = transform.Find("Rule");
        if (ruleObj != null) ruleObj.gameObject.SetActive(false);

        Button[] btns = GetComponentsInChildren<Button>(true);
        foreach (var b in btns) {
            if (b == null) continue;
            string bName = b.name.ToLower();
            if (bName.Contains("keep") || bName.Contains("fix")) {
                b.gameObject.SetActive(false);
            }
        }

        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) {
            if (t == null) continue;
            string txt = t.text;
            if (txt.Contains("PRINCIPAL") || txt.Contains("CASUAL") || txt.Contains("HEY")) {
                t.gameObject.SetActive(false);
            }
        }

        Transform gridObj = transform.Find("PhraseCardsGrid");
        if (gridObj != null) {
            gridObj.gameObject.SetActive(true);
        }
    }

    private void AutoWireUIReferences() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string tName = tmp.name.ToLower();
            if (r03TitleTMP == null && (tName.Contains("lessontitle") || tName.Contains("title"))) {
                r03TitleTMP = tmp as TextMeshProUGUI;
            }
            if (r03ProgressTMP == null && (tName.Contains("expressioncount") || tName.Contains("progress"))) {
                r03ProgressTMP = tmp as TextMeshProUGUI;
            }
            if (ariaHintTMP == null && (tName.Contains("sentence") || tName.Contains("tmp") || tName.Contains("aria"))) {
                ariaHintTMP = tmp as TextMeshProUGUI;
            }
        }

        if (checkButton == null) {
            Button[] btns = GetComponentsInChildren<Button>(true);
            foreach (var b in btns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (bName.Contains("fix") || bName.Contains("check") || bName.Contains("submit") || bName.Contains("keep")) {
                    checkButton = b;
                    break;
                }
            }
        }

        if (nextButton == null) {
            Button[] btns = GetComponentsInChildren<Button>(true);
            foreach (var b in btns) {
                if (b != null && b.name.ToLower().Contains("next")) {
                    nextButton = b;
                    break;
                }
            }
        }

        if (cardButtons == null || cardButtons.Length == 0) {
            Button[] btns = GetComponentsInChildren<Button>(true);
            List<Button> cards = new List<Button>();
            foreach (var b in btns) {
                if (b == null) continue;
                if (b.name.Contains("OptionButton") || b.name.Contains("word")) {
                    cards.Add(b);
                }
            }
            if (cards.Count > 0) cardButtons = cards.ToArray();
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text;
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Spot") || textVal.Contains("Polished") || textVal.Contains("R03")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "R03 Get Ready for the Festival — Order the Preparations";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("READING")) {
                tmp.text = "READING BRANCH (Book Stall)";
            }
            if (textVal.Contains("PRINCIPAL") || textVal.Contains("CASUAL") || textVal.Contains("HEY")) {
                tmp.gameObject.SetActive(false);
            }
        }

        // Hide obsolete Keep and Fix buttons from base Unit 1 prefab
        Button[] allBtns = GetComponentsInChildren<Button>(true);
        foreach (var b in allBtns) {
            if (b == null) continue;
            string bName = b.name.ToLower();
            if (bName.Contains("keep") || bName.Contains("fix")) {
                b.gameObject.SetActive(false);
            }
        }
    }

    public void LoadR03Round(int roundIdx) {
        EnsureR03RoundsInitialized();

        if (r03Rounds == null || roundIdx < 0 || roundIdx >= r03Rounds.Length) {
            EvaluateFinalScoreAndComplete();
            return;
        }

        r03CurrentRoundIndex = roundIdx;
        selectedCardIndex = -1;

        for (int i = 0; i < slotToCardMapping.Length; i++) {
            slotToCardMapping[i] = -1;
        }

        UpdateTitleAndUIComponents();
        R03RoundData data = r03Rounds[r03CurrentRoundIndex];

        if (r03TitleTMP != null) r03TitleTMP.text = string.IsNullOrEmpty(data.roundTitle) ? $"Round {roundIdx + 1}: Celebration Prep" : data.roundTitle;
        if (r03ProgressTMP != null) r03ProgressTMP.text = $"{r03CurrentRoundIndex + 1} / {TOTAL_ROUNDS}";
        if (ariaHintTMP != null) {
            ariaHintTMP.gameObject.SetActive(true);
            ariaHintTMP.text = "Before we decorate, what must we do first?";
        }

        // Prevent double audio overlap on Round 0 (intro voiceover already played in StartReadingLessonThreeRoutine)
        if (roundIdx > 0 && data.ariaPromptAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(data.ariaPromptAudio);
        }

        if (houseVisualReadyEffect != null) {
            houseVisualReadyEffect.SetActive(false);
        }

        SetupAndShuffleCards(data);
    }

    private void EnsureR03RoundsInitialized() {
        if (r03Rounds != null && r03Rounds.Length >= 3 && r03Rounds[0] != null && r03Rounds[0].preparationCards != null && r03Rounds[0].preparationCards.Length == 4 && r03Rounds[1] != null && r03Rounds[1].roundTitle != r03Rounds[0].roundTitle) return;

        r03Rounds = new R03RoundData[3];

        // Round 1: Festival House Prep
        r03Rounds[0] = new R03RoundData {
            roundTitle = "Round 1: Festival House Prep",
            preparationCards = new string[] {
                "Clean the house.",
                "Decorate with lights and diyas.",
                "Make delicious festival sweets.",
                "Wear new clothes."
            }
        };

        // Round 2: Birthday Party Prep
        r03Rounds[1] = new R03RoundData {
            roundTitle = "Round 2: Birthday Party Prep",
            preparationCards = new string[] {
                "Send birthday invitations.",
                "Clean the living room.",
                "Bake the birthday cake.",
                "Decorate with balloons."
            }
        };

        // Round 3: Celebration Feast Prep
        r03Rounds[2] = new R03RoundData {
            roundTitle = "Round 3: Holiday Feast Prep",
            preparationCards = new string[] {
                "Do shopping for food.",
                "Clean the dining table.",
                "Prepare the delicious feast.",
                "Decorate the house."
            }
        };
    }

    private void SetupAndShuffleCards(R03RoundData data) {
        if (cardButtons == null || cardButtons.Length == 0 || data.preparationCards == null) return;

        List<string> cards = new List<string>(data.preparationCards);

        // Fisher-Yates Shuffle initial card positions
        for (int i = cards.Count - 1; i > 0; i--) {
            int rand = Random.Range(0, i + 1);
            string temp = cards[i];
            cards[i] = cards[rand];
            cards[rand] = temp;
        }

        for (int i = 0; i < cardButtons.Length && i < cards.Count; i++) {
            Button btn = cardButtons[i];
            if (btn == null) continue;

            btn.gameObject.SetActive(true);
            btn.interactable = true;
            btn.transition = Selectable.Transition.None;

            Image img = btn.GetComponent<Image>();
            if (img != null) {
                img.raycastTarget = true;
                img.color = new Color(0.12f, 0.25f, 0.48f, 1f); // Royal Blue (#1E40AF)
            }

            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) {
                tmp.raycastTarget = false;
                tmp.text = cards[i];
                tmp.color = Color.white;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 18;
                tmp.fontSizeMax = 32;
            }

            int cardIdx = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnCardClicked(cardIdx));
        }
    }

    public void OnCardClicked(int cardIdx) {
        if (cardButtons == null || cardIdx < 0 || cardIdx >= cardButtons.Length) return;

        Button tappedBtn = cardButtons[cardIdx];
        if (tappedBtn == null) return;

        TextMeshProUGUI tmp = tappedBtn.GetComponentInChildren<TextMeshProUGUI>();
        string cardText = tmp != null ? tmp.text.ToLower() : "";

        // Check if card contains clean vs decorate rule
        bool isCleanCard = cardText.Contains("clean");
        bool isDecorateCard = cardText.Contains("decorate");

        Image img = tappedBtn.GetComponent<Image>();

        if (isCleanCard) {
            // CORRECT TAP: Clean must happen first -> GREEN
            tappedBtn.transition = Selectable.Transition.None;
            if (img != null) img.color = new Color(0.13f, 0.77f, 0.36f, 1f); // Emerald Green
            if (tmp != null) tmp.color = Color.white;
            tappedBtn.transform.DOPunchScale(Vector3.one * 0.12f, 0.3f);

            if (ariaHintTMP != null) ariaHintTMP.text = "Great! Clean the house first!";

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Auto-advance to next round after 1.5s
            Invoke(nameof(AdvanceToNextRound), 1.5f);
        } else if (isDecorateCard) {
            // WRONG SEQUENCE TAP: Decorate before Clean -> RED
            tappedBtn.transition = Selectable.Transition.None;
            if (img != null) img.color = new Color(0.93f, 0.26f, 0.26f, 1f); // Crimson Red
            if (tmp != null) tmp.color = Color.white;

            if (ariaHintTMP != null) ariaHintTMP.text = "Try again. Before we decorate, what must we do first?";

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            Invoke(nameof(ResetCardColors), 1.2f);
        } else {
            // NEUTRAL PREPARATION CARD -> Highlight Royal Blue / Green
            tappedBtn.transition = Selectable.Transition.None;
            if (img != null) img.color = new Color(0.13f, 0.77f, 0.36f, 1f); // Emerald Green
            if (tmp != null) tmp.color = Color.white;
            tappedBtn.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }

        // Auto place in first empty slot
        for (int slot = 0; slot < slotToCardMapping.Length; slot++) {
            if (slotToCardMapping[slot] == -1) {
                PlaceCardInSlot(cardIdx, slot);
                break;
            }
        }
    }

    public void OnSlotClicked(int slotIdx) {
        if (selectedCardIndex >= 0) {
            PlaceCardInSlot(selectedCardIndex, slotIdx);
            selectedCardIndex = -1;
        } else if (slotToCardMapping[slotIdx] >= 0) {
            // Remove card from slot
            int cardIdx = slotToCardMapping[slotIdx];
            slotToCardMapping[slotIdx] = -1;
            if (cardIdx < cardButtons.Length && cardButtons[cardIdx] != null) {
                cardButtons[cardIdx].gameObject.SetActive(true);
            }
        }
    }

    private void PlaceCardInSlot(int cardIdx, int slotIdx) {
        if (slotIdx < 0 || slotIdx >= slotToCardMapping.Length || cardIdx < 0 || cardIdx >= cardButtons.Length) return;

        slotToCardMapping[slotIdx] = cardIdx;

        if (cardButtons[cardIdx] != null && orderSlots != null && slotIdx < orderSlots.Length && orderSlots[slotIdx] != null) {
            cardButtons[cardIdx].transform.SetParent(orderSlots[slotIdx]);
            cardButtons[cardIdx].transform.localPosition = Vector3.zero;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    public void OnCheckButtonClicked() {
        // Collect current card order text
        List<string> orderedTexts = new List<string>();
        for (int i = 0; i < slotToCardMapping.Length; i++) {
            int cIdx = slotToCardMapping[i];
            if (cIdx >= 0 && cIdx < cardButtons.Length && cardButtons[cIdx] != null) {
                TextMeshProUGUI tmp = cardButtons[cIdx].GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) orderedTexts.Add(tmp.text);
            }
        }

        int cleanIndex = -1;
        int decorateIndex = -1;

        for (int i = 0; i < orderedTexts.Count; i++) {
            string txt = orderedTexts[i].ToLower();
            if (txt.Contains("clean")) cleanIndex = i;
            if (txt.Contains("decorate")) decorateIndex = i;
        }

        // VALIDATION MANDATE: CLEAN MUST HAPPEN BEFORE DECORATE
        bool isValid = (cleanIndex >= 0 && decorateIndex >= 0 && cleanIndex < decorateIndex);

        if (isValid) {
            // VALID ORDER -> TURN CARDS EMERALD GREEN (#22C55E)
            validOrdersCount++;

            foreach (var btn in cardButtons) {
                if (btn == null) continue;
                btn.transition = Selectable.Transition.None;
                Image img = btn.GetComponent<Image>();
                if (img != null) img.color = new Color(0.13f, 0.77f, 0.36f, 1f); // Emerald Green
                TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.color = Color.white;
                btn.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
            }

            if (ariaHintTMP != null) ariaHintTMP.text = "Great order! The house is ready!";

            if (houseVisualReadyEffect != null) {
                houseVisualReadyEffect.SetActive(true);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            Invoke(nameof(AdvanceToNextRound), 2.0f);
        } else {
            // INVALID ORDER -> TURN CARDS CRIMSON RED (#EF4444)
            foreach (var btn in cardButtons) {
                if (btn == null) continue;
                btn.transition = Selectable.Transition.None;
                Image img = btn.GetComponent<Image>();
                if (img != null) img.color = new Color(0.93f, 0.26f, 0.26f, 1f); // Crimson Red
                TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.color = Color.white;
            }

            if (ariaHintTMP != null) ariaHintTMP.text = "Try again. Before we decorate, what must we do first?";

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            // Reset card colors after 1.5 seconds so player can try again
            Invoke(nameof(ResetCardColors), 1.5f);
        }
    }

    private void ResetCardColors() {
        if (cardButtons == null) return;
        foreach (var btn in cardButtons) {
            if (btn == null) continue;
            btn.transition = Selectable.Transition.None;
            Image img = btn.GetComponent<Image>();
            if (img != null) img.color = new Color(0.12f, 0.25f, 0.48f, 1f); // Royal Blue
            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.color = Color.white;
        }
    }

    private void AdvanceToNextRound() {
        r03CurrentRoundIndex++;
        if (r03Rounds != null && r03CurrentRoundIndex < r03Rounds.Length) {
            LoadR03Round(r03CurrentRoundIndex);
        } else {
            EvaluateFinalScoreAndComplete();
        }
    }

    private void EvaluateFinalScoreAndComplete() {
        bool isPass = validOrdersCount >= PASS_THRESHOLD;
        string resultMsg = isPass ? "Great order! The house is ready for the festival!" : "Good try! Let's practice ordering preparations again.";

        if (ariaHintTMP != null) ariaHintTMP.text = resultMsg;

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
                if (promptTrans == null && (lowerName.Contains("questions text") || lowerName.Contains("prompt") || lowerName.Contains("card") || txt.Contains("BEFORE WE DECORATE") || txt.Contains("DECORATE") || txt.Contains("LESSON"))) {
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