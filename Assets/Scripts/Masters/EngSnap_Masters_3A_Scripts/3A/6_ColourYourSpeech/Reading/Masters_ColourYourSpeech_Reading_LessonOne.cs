using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unit 6 Colour Your Speech — Reading L01 Controller.
/// Activity: "R01 Match the Idiom to Its Meaning"
/// 
/// Refactored UI Architecture:
/// - Each card has its own independent container and child hierarchy in the Inspector.
/// - No runtime destruction or procedural instantiation of card GameObjects.
/// - The Unity Inspector is the absolute source of truth for positions, sizes, and styling.
/// - Strict interaction order: Idiom Card (Left) -> Meaning Card (Right).
/// - Pass threshold: 6/8. Completion: 8/8.
/// </summary>
public class Masters_ColourYourSpeech_Reading_LessonOne : Masters_Lesson
{
    [System.Serializable]
    public class IdiomMatchPair
    {
        public string idiomText;
        public string meaningText;
    }

    [System.Serializable]
    public class IdiomCardUI
    {
        [Header("Hierarchy References")]
        public GameObject root;
        public Image background;
        public Image border;
        public Image idiomImage;
        public TextMeshProUGUI cardText;
        public Button answerButton;

        [Header("Identity")]
        public int pairId;
        public bool isLeftColumn;

        [System.NonSerialized] public bool isSolved;
        [System.NonSerialized] public bool isSelected;
        [System.NonSerialized] public Vector3 initialScale = Vector3.one;

        public void Initialize(int id, bool isLeft, System.Action<IdiomCardUI> onClickCallback)
        {
            pairId = id;
            isLeftColumn = isLeft;
            isSolved = false;
            isSelected = false;

            if (root != null)
            {
                initialScale = root.transform.localScale;
            }

            if (answerButton != null)
            {
                answerButton.onClick.RemoveAllListeners();
                answerButton.onClick.AddListener(() => onClickCallback?.Invoke(this));
                answerButton.interactable = true;
            }
        }

        public void SetText(string text)
        {
            if (cardText != null && !string.IsNullOrEmpty(text))
            {
                cardText.text = text;
            }
        }

        public void SetVisualState(Color bgColor, Color borderColor, float scaleMultiplier = 1f)
        {
            if (background != null) background.color = bgColor;
            if (border != null) border.color = borderColor;
            if (root != null)
            {
                root.transform.DOKill();
                root.transform.DOScale(initialScale * scaleMultiplier, 0.15f);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (answerButton != null)
            {
                answerButton.interactable = interactable;
            }
        }

        public void FlashWrong(Color wrongBg, Color wrongBorder)
        {
            if (background != null) background.color = wrongBg;
            if (border != null) border.color = wrongBorder;
            if (root != null)
            {
                root.transform.DOKill();
                root.transform.DOShakePosition(0.4f, new Vector3(8f, 0f, 0f), 20);
            }
        }

        public void ResetVisuals(Color normalBg, Color normalBorder)
        {
            if (isSolved) return;
            if (background != null) background.color = normalBg;
            if (border != null) border.color = normalBorder;
            if (root != null)
            {
                root.transform.DOKill();
                root.transform.DOScale(initialScale, 0.15f);
            }
        }
    }

    [Header("Activity Data (8 Pairs)")]
    [SerializeField] private IdiomMatchPair[] defaultPairs = new IdiomMatchPair[]
    {
        new IdiomMatchPair { idiomText = "My way or the highway", meaningText = "You have to listen to me." },
        new IdiomMatchPair { idiomText = "Cloud nine", meaningText = "In a state of happiness or bliss or extreme excitement." },
        new IdiomMatchPair { idiomText = "Tongue-in-cheek", meaningText = "Saying something as a joke, not to be taken seriously." },
        new IdiomMatchPair { idiomText = "A piece of cake", meaningText = "Very easy." },
        new IdiomMatchPair { idiomText = "Fruitcake", meaningText = "Really strange or crazy." },
        new IdiomMatchPair { idiomText = "Sugar and spice", meaningText = "Behaving in a kind and friendly way; very sweet and nice." },
        new IdiomMatchPair { idiomText = "Nut / Nutty", meaningText = "Funny, kind of crazy, usually makes you laugh." },
        new IdiomMatchPair { idiomText = "Going bananas", meaningText = "Becoming crazy, especially with too much to do." }
    };

    [Header("Independent Card Containers (Inspector Assigned)")]
    [SerializeField] private IdiomCardUI[] leftCards = new IdiomCardUI[8];
    [SerializeField] private IdiomCardUI[] rightCards = new IdiomCardUI[8];

    [Header("Settings & Audio")]
    [SerializeField] private int passThreshold = 6;
    [SerializeField] private AudioClip voAriaIntro;
    [SerializeField] private AudioClip sfxLink;
    [SerializeField] private AudioClip sfxWrong;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("UI Header Elements")]
    [SerializeField] private TextMeshProUGUI lessonTitleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform connectionContainer;

    [Header("Visual Theme Palette")]
    [SerializeField] private Color normalCardBgColor = new Color32(24, 34, 76, 240);
    [SerializeField] private Color normalCardBorderColor = new Color32(70, 95, 160, 255);
    [SerializeField] private Color selectedCardBgColor = new Color32(45, 58, 120, 255);
    [SerializeField] private Color selectedCardBorderColor = new Color32(255, 204, 0, 255);
    [SerializeField] private Color solvedCardBgColor = new Color32(22, 90, 52, 240);
    [SerializeField] private Color solvedCardBorderColor = new Color32(46, 204, 113, 255);
    [SerializeField] private Color wrongCardBgColor = new Color32(110, 28, 28, 240);
    [SerializeField] private Color wrongCardBorderColor = new Color32(231, 76, 60, 255);
    [SerializeField] private Color[] brushStrokeColors = new Color[]
    {
        new Color32(255, 204, 0, 255),
        new Color32(46, 204, 113, 255),
        new Color32(52, 152, 219, 255),
        new Color32(230, 126, 34, 255),
        new Color32(155, 89, 182, 255),
        new Color32(26, 188, 156, 255),
        new Color32(241, 196, 15, 255),
        new Color32(233, 30, 99, 255)
    };

    [Header("Card Asset Sprites (Optional)")]
    [SerializeField] private Sprite brushStrokeSprite;

    // Runtime state
    private IdiomCardUI selectedLeftCard = null;
    private IdiomCardUI selectedRightCard = null;
    private int correctCount = 0;
    private int totalPairs = 8;
    private bool isBusyEvaluating = false;
    private bool isCompleted = false;

    protected override void Awake()
    {
        base.Awake();
        topic = Masters_Topic.Reading;

        FindAndAutoAssignHeaderUI();

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    protected override void Start()
    {
        base.Start();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.interactable = false;
        }

        correctCount = 0;
        isBusyEvaluating = false;
        isCompleted = false;
        selectedLeftCard = null;
        selectedRightCard = null;

        totalPairs = defaultPairs != null ? defaultPairs.Length : 8;

        InitializeCards();

        if (progressTMP != null)
        {
            progressTMP.text = $"0/{totalPairs}";
        }

        // Play intro voiceover
        AudioClip clipToPlay = voAriaIntro != null ? voAriaIntro : narratorSpeech;
        if (clipToPlay != null && Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
        }
    }

    private void FindAndAutoAssignHeaderUI()
    {
        if (lessonTitleTMP == null)
        {
            Transform titleTrans = transform.Find("LessonTitle") ?? FindChildRecursive(transform, "LessonTitle");
            if (titleTrans != null) lessonTitleTMP = titleTrans.GetComponent<TextMeshProUGUI>();
        }

        if (lessonTitleTMP != null)
        {
            lessonTitleTMP.text = "R01 Match the Idiom to Its Meaning";
        }

        if (subtitleTMP == null)
        {
            Transform subTrans = transform.Find("Subtitle") ?? FindChildRecursive(transform, "Subtitle");
            if (subTrans != null) subtitleTMP = subTrans.GetComponent<TextMeshProUGUI>();
        }

        if (subtitleTMP != null)
        {
            subtitleTMP.text = "Match each idiom with its meaning.";
        }

        if (progressTMP == null)
        {
            Transform progTrans = transform.Find("ProgressCount") ?? FindChildRecursive(transform, "ProgressCount");
            if (progTrans != null) progressTMP = progTrans.GetComponent<TextMeshProUGUI>();
        }

        if (backButton == null)
        {
            Transform backTrans = transform.Find("BackButton") ?? FindChildRecursive(transform, "BackButton");
            if (backTrans != null) backButton = backTrans.GetComponent<Button>();
        }

        if (connectionContainer == null)
        {
            Transform connTrans = transform.Find("MatchArea/ConnectionContainer") ?? FindChildRecursive(transform, "ConnectionContainer");
            if (connTrans != null) connectionContainer = connTrans as RectTransform;
        }
    }

    /// <summary>
    /// Configures the 8 Left Cards and 8 Right Cards WITHOUT modifying any RectTransform/layout values.
    /// Preserves 100% of the Inspector-configured visual positions and sizes.
    /// </summary>
    public void InitializeCards()
    {
        // 1. Clear any leftover connection lines
        if (connectionContainer != null)
        {
            for (int i = connectionContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(connectionContainer.GetChild(i).gameObject);
            }
        }

        // 2. Setup Left Cards (Idioms)
        if (leftCards != null)
        {
            for (int i = 0; i < leftCards.Length; i++)
            {
                if (leftCards[i] == null) continue;
                leftCards[i].Initialize(i, true, OnCardClicked);
                if (i < defaultPairs.Length)
                {
                    leftCards[i].SetText(defaultPairs[i].idiomText);
                }
                leftCards[i].ResetVisuals(normalCardBgColor, normalCardBorderColor);
            }
        }

        // 3. Setup Right Cards (Meanings Shuffled)
        if (rightCards != null)
        {
            List<int> shuffledIndices = new List<int>();
            for (int i = 0; i < rightCards.Length; i++) shuffledIndices.Add(i);
            ShuffleList(shuffledIndices);

            for (int i = 0; i < rightCards.Length; i++)
            {
                if (rightCards[i] == null) continue;
                int pairIndex = i < shuffledIndices.Count ? shuffledIndices[i] : i;
                rightCards[i].Initialize(pairIndex, false, OnCardClicked);
                if (pairIndex < defaultPairs.Length)
                {
                    rightCards[i].SetText(defaultPairs[pairIndex].meaningText);
                }
                rightCards[i].ResetVisuals(normalCardBgColor, normalCardBorderColor);
            }
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n - 1; i++)
        {
            int r = Random.Range(i, n);
            T temp = list[i];
            list[i] = list[r];
            list[r] = temp;
        }

        for (int i = 0; i < n; i++)
        {
            if (EqualityComparer<T>.Default.Equals(list[i], (T)(object)i))
            {
                int swapTarget = (i + 1) % n;
                T temp = list[i];
                list[i] = list[swapTarget];
                list[swapTarget] = temp;
            }
        }
    }

    /// <summary>
    /// Handles user tapping any card.
    /// Strictly enforces interaction order: IDIOM CARD (Left) -> MEANING CARD (Right).
    /// </summary>
    public void OnCardClicked(IdiomCardUI card)
    {
        if (isBusyEvaluating || card == null || card.isSolved) return;

        if (card.isLeftColumn)
        {
            // ================== Tapped a Left Card (Idiom) ==================
            if (selectedLeftCard == card)
            {
                // Deselect if tapping same card
                card.SetVisualState(normalCardBgColor, normalCardBorderColor, 1.0f);
                card.isSelected = false;
                selectedLeftCard = null;
                return;
            }

            if (selectedLeftCard != null)
            {
                selectedLeftCard.SetVisualState(normalCardBgColor, normalCardBorderColor, 1.0f);
                selectedLeftCard.isSelected = false;
            }

            selectedLeftCard = card;
            card.isSelected = true;
            card.SetVisualState(selectedCardBgColor, selectedCardBorderColor, 1.03f);
            PlaySelectSFX();
        }
        else
        {
            // ================== Tapped a Right Card (Meaning) ==================
            // STRICT RULE: An idiom card MUST be selected first!
            if (selectedLeftCard == null)
            {
                if (card.root != null)
                {
                    card.root.transform.DOKill();
                    card.root.transform.DOPunchScale(new Vector3(0.04f, 0.04f, 0f), 0.2f);
                }
                return;
            }

            selectedRightCard = card;
            card.isSelected = true;
            card.SetVisualState(selectedCardBgColor, selectedCardBorderColor, 1.03f);
            PlaySelectSFX();

            // Evaluate the pair immediately
            StartCoroutine(EvaluateMatchRoutine(selectedLeftCard, selectedRightCard));
        }
    }

    private void PlaySelectSFX()
    {
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    private IEnumerator EvaluateMatchRoutine(IdiomCardUI leftCard, IdiomCardUI rightCard)
    {
        isBusyEvaluating = true;

        if (leftCard.pairId == rightCard.pairId)
        {
            // ================== CORRECT MATCH ==================
            leftCard.isSolved = true;
            rightCard.isSolved = true;
            leftCard.SetInteractable(false);
            rightCard.SetInteractable(false);

            leftCard.SetVisualState(solvedCardBgColor, solvedCardBorderColor, 1.0f);
            rightCard.SetVisualState(solvedCardBgColor, solvedCardBorderColor, 1.0f);

            if (leftCard.root != null) leftCard.root.transform.DOPunchScale(new Vector3(0.06f, 0.06f, 0f), 0.3f);
            if (rightCard.root != null) rightCard.root.transform.DOPunchScale(new Vector3(0.06f, 0.06f, 0f), 0.3f);

            // Play SFX Link / Brush
            if (sfxLink != null && Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(sfxLink);
            }
            else if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Draw Painted Brush-Stroke Connection Line
            Color strokeColor = brushStrokeColors[leftCard.pairId % brushStrokeColors.Length];
            CreateBrushStrokeConnection(leftCard.root.GetComponent<RectTransform>(), rightCard.root.GetComponent<RectTransform>(), strokeColor);

            correctCount++;
            if (progressTMP != null)
            {
                progressTMP.text = $"{correctCount}/{totalPairs}";
            }

            selectedLeftCard = null;
            selectedRightCard = null;
            isBusyEvaluating = false;

            // Check completion condition: all 8 pairs matched
            if (correctCount >= totalPairs)
            {
                if (!isCompleted)
                {
                    isCompleted = true;
                    OnActivityCompleted();
                }
            }
        }
        else
        {
            // ================== INCORRECT MATCH ==================
            // Play SFX Wrong
            if (sfxWrong != null && Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(sfxWrong);
            }
            else if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            leftCard.FlashWrong(wrongCardBgColor, wrongCardBorderColor);
            rightCard.FlashWrong(wrongCardBgColor, wrongCardBorderColor);

            yield return new WaitForSeconds(0.45f);

            leftCard.ResetVisuals(normalCardBgColor, normalCardBorderColor);
            rightCard.ResetVisuals(normalCardBgColor, normalCardBorderColor);

            selectedLeftCard = null;
            selectedRightCard = null;
            isBusyEvaluating = false;
        }
    }

    private void CreateBrushStrokeConnection(RectTransform leftRT, RectTransform rightRT, Color strokeColor)
    {
        if (connectionContainer == null || leftRT == null || rightRT == null) return;

        GameObject lineGO = new GameObject("BrushStrokeLine", typeof(RectTransform), typeof(Image));
        lineGO.transform.SetParent(connectionContainer, false);

        RectTransform lineRT = lineGO.GetComponent<RectTransform>();
        Image lineImg = lineGO.GetComponent<Image>();

        if (brushStrokeSprite != null)
        {
            lineImg.sprite = brushStrokeSprite;
            lineImg.type = Image.Type.Sliced;
        }

        lineImg.color = strokeColor;
        lineImg.raycastTarget = false;

        Vector3 leftWorld = leftRT.TransformPoint(new Vector3(leftRT.rect.width * 0.5f, 0f, 0f));
        Vector3 rightWorld = rightRT.TransformPoint(new Vector3(-rightRT.rect.width * 0.5f, 0f, 0f));

        Vector3 leftLocal = connectionContainer.InverseTransformPoint(leftWorld);
        Vector3 rightLocal = connectionContainer.InverseTransformPoint(rightWorld);

        Vector3 midpoint = (leftLocal + rightLocal) * 0.5f;
        Vector3 diff = rightLocal - leftLocal;
        float distance = diff.magnitude;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        lineRT.localPosition = midpoint;
        lineRT.sizeDelta = new Vector2(distance, 8f);
        lineRT.localRotation = Quaternion.Euler(0f, 0f, angle);

        lineRT.localScale = new Vector3(0f, 1f, 1f);
        lineRT.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutQuad);
    }

    private void OnActivityCompleted()
    {
        M3A_U6_HubProgress.MarkComplete(M3A_U6_Branch.Reading);

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U6_HubProgress.MarkComplete(M3A_U6_Branch.Reading);

        if (nextLessonSO != null)
        {
            if (Masters_LevelManager.Instance != null)
            {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        }
        else
        {
            if (Masters_LevelManager.Instance != null)
            {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    private void OnBackClicked()
    {
        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
    }
}
