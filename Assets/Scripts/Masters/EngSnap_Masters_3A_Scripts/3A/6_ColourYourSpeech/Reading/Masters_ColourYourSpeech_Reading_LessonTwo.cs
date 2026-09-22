using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum IdiomFamilyType
{
    Easy = 0,
    CrazyStrange = 1
}

/// <summary>
/// Unit 6 Colour Your Speech — Reading L02 Controller.
/// Activity: "R02 Same Meaning! — Group the Idiom Families"
/// 
/// Mechanics:
/// - Two-bucket idiom family grouping game (EASY vs CRAZY/STRANGE).
/// - 6 floating idiom cards dragged into the 2 paint buckets.
/// - Correct drop: plays SFX_DROP, card dissolves into bucket, counts as correct.
/// - Wrong drop: plays SFX_WRONG, card bounces back to origin, retry allowed.
/// - Success condition: at least 5 of 6 cards correctly grouped.
/// - Complete static hierarchy editable in Inspector.
/// </summary>
public class Masters_ColourYourSpeech_Reading_LessonTwo : Masters_Lesson
{
    [System.Serializable]
    public class IdiomFamilyCardData
    {
        public string idiomText;
        public IdiomFamilyType family;
    }

    [System.Serializable]
    public class IdiomDragCardUI
    {
        [Header("Hierarchy References")]
        public GameObject root;
        public RectTransform rectTransform;
        public Image background;
        public Image border;
        public Image idiomImage;
        public TextMeshProUGUI cardText;
        public CanvasGroup canvasGroup;

        [Header("Identity")]
        public int cardId;
        public IdiomFamilyType family;

        [System.NonSerialized] public bool isSolved;
        [System.NonSerialized] public bool isDragging;
        [System.NonSerialized] public Vector3 originalLocalPosition;
        [System.NonSerialized] public Vector3 originalScale;
        [System.NonSerialized] public int originalSiblingIndex;

        public void Initialize(int id, IdiomFamilyType fam, string text)
        {
            cardId = id;
            family = fam;
            isSolved = false;
            isDragging = false;

            if (root != null)
            {
                if (rectTransform == null) rectTransform = root.GetComponent<RectTransform>();
                if (canvasGroup == null) canvasGroup = root.GetComponent<CanvasGroup>();
                originalLocalPosition = rectTransform.localPosition;
                originalScale = root.transform.localScale;
                originalSiblingIndex = root.transform.GetSiblingIndex();
            }

            if (cardText != null && !string.IsNullOrEmpty(text))
            {
                cardText.text = text;
            }
        }

        public void SetText(string text)
        {
            if (cardText != null && !string.IsNullOrEmpty(text))
            {
                cardText.text = text;
            }
        }

        public void DissolveIntoBucket(Vector3 targetWorldPos, System.Action onComplete)
        {
            isSolved = true;
            if (root == null) return;

            root.transform.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(root.transform.DOMove(targetWorldPos, 0.35f).SetEase(Ease.InQuad));
            seq.Join(root.transform.DOScale(Vector3.zero, 0.35f).SetEase(Ease.InBack));
            if (canvasGroup != null)
            {
                seq.Join(canvasGroup.DOFade(0f, 0.35f));
            }
            seq.OnComplete(() =>
            {
                root.SetActive(false);
                onComplete?.Invoke();
            });
        }

        public void BounceBackToOrigin()
        {
            if (root == null) return;

            root.transform.DOKill();
            root.transform.SetSiblingIndex(originalSiblingIndex);
            if (border != null)
            {
                border.color = new Color32(231, 76, 60, 255); // Red flash
                border.DOColor(new Color32(70, 95, 160, 255), 0.5f);
            }
            root.transform.DOLocalMove(originalLocalPosition, 0.4f).SetEase(Ease.OutBack);
            root.transform.DOScale(originalScale, 0.3f);
        }
    }

    [System.Serializable]
    public class IdiomBucketUI
    {
        [Header("Hierarchy References")]
        public GameObject root;
        public RectTransform rectTransform;
        public Image bucketImage;
        public Image bucketDecoration;
        public TextMeshProUGUI labelTMP;
        public IdiomFamilyType acceptedFamily;

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera cam)
        {
            if (rectTransform == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, cam);
        }

        public void PlayDropEffect()
        {
            if (bucketImage != null)
            {
                bucketImage.transform.DOKill();
                bucketImage.transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.3f, 10, 1f);
            }
            else if (root != null)
            {
                root.transform.DOKill();
                root.transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.3f, 10, 1f);
            }
        }
    }

    [Header("Activity Data (6 Cards)")]
    [SerializeField] private IdiomFamilyCardData[] defaultCards = new IdiomFamilyCardData[]
    {
        new IdiomFamilyCardData { idiomText = "A piece of cake", family = IdiomFamilyType.Easy },
        new IdiomFamilyCardData { idiomText = "Easy as pie", family = IdiomFamilyType.Easy },
        new IdiomFamilyCardData { idiomText = "Giving candy to a baby", family = IdiomFamilyType.Easy },
        new IdiomFamilyCardData { idiomText = "Fruitcake", family = IdiomFamilyType.CrazyStrange },
        new IdiomFamilyCardData { idiomText = "Nut / Nutty", family = IdiomFamilyType.CrazyStrange },
        new IdiomFamilyCardData { idiomText = "Going bananas", family = IdiomFamilyType.CrazyStrange }
    };

    [Header("Independent Card Hierarchy (6 Cards)")]
    [SerializeField] private IdiomDragCardUI[] cards = new IdiomDragCardUI[6];

    [Header("Screen State References")]
    [SerializeField] private GameObject introScreen;
    [SerializeField] private GameObject gameplayScreen;
    [SerializeField] private Button startButton;

    [Header("Independent Buckets (2 Buckets)")]
    [SerializeField] private IdiomBucketUI easyBucket = new IdiomBucketUI { acceptedFamily = IdiomFamilyType.Easy };
    [SerializeField] private IdiomBucketUI crazyBucket = new IdiomBucketUI { acceptedFamily = IdiomFamilyType.CrazyStrange };

    [Header("Settings & Audio")]
    [SerializeField] private int passThreshold = 5;
    [SerializeField] private AudioClip voAriaIntro;
    [SerializeField] private AudioClip sfxDrop;
    [SerializeField] private AudioClip sfxWrong;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private Button backButton;
    [SerializeField] private RectTransform gameArea;

    // Runtime state
    private int correctCount = 0;
    private int processedCount = 0;
    private int totalCards = 6;
    private bool isCompleted = false;
    private bool isGameplayStarted = false;
    private IdiomDragCardUI currentDraggingCard = null;

    protected override void Awake()
    {
        base.Awake();
        topic = Masters_Topic.Reading;

        FindAndAutoAssignUI();

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
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

        ShowIntroScreen();
    }

    public void ShowIntroScreen()
    {
        isGameplayStarted = false;
        isCompleted = false;
        correctCount = 0;
        processedCount = 0;
        currentDraggingCard = null;
        totalCards = defaultCards != null ? defaultCards.Length : 6;

        if (introScreen != null) introScreen.SetActive(true);
        if (gameplayScreen != null) gameplayScreen.SetActive(false);

        if (startButton != null)
        {
            startButton.interactable = true;
        }

        if (progressTMP != null)
        {
            progressTMP.text = $"0/{totalCards}";
        }

        // Play intro voiceover on intro screen
        AudioClip clipToPlay = voAriaIntro != null ? voAriaIntro : narratorSpeech;
        if (clipToPlay != null && Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
        }
    }

    public void OnStartClicked()
    {
        if (isGameplayStarted) return;

        if (startButton != null)
        {
            startButton.interactable = false;
        }

        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (introScreen != null) introScreen.SetActive(false);
        if (gameplayScreen != null) gameplayScreen.SetActive(true);

        BeginGameplay();
    }

    public void BeginGameplay()
    {
        isGameplayStarted = true;
        correctCount = 0;
        processedCount = 0;
        isCompleted = false;
        totalCards = defaultCards != null ? defaultCards.Length : 6;

        if (progressTMP != null)
        {
            progressTMP.text = $"0/{totalCards}";
        }

        InitializeCardsAndBuckets();
    }

    private void FindAndAutoAssignUI()
    {
        if (introScreen == null)
        {
            Transform introTrans = transform.Find("IntroScreen");
            if (introTrans != null) introScreen = introTrans.gameObject;
        }

        if (gameplayScreen == null)
        {
            Transform gameTrans = transform.Find("GameplayScreen");
            if (gameTrans != null) gameplayScreen = gameTrans.gameObject;
        }

        if (startButton == null && introScreen != null)
        {
            Transform startTrans = introScreen.transform.Find("StartButton") ?? FindChildRecursive(introScreen.transform, "StartButton");
            if (startTrans != null) startButton = startTrans.GetComponent<Button>();
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

        if (gameArea == null)
        {
            Transform gameTrans = transform.Find("GameArea") ?? FindChildRecursive(transform, "GameArea");
            if (gameTrans != null) gameArea = gameTrans as RectTransform;
        }
    }

    public void InitializeCardsAndBuckets()
    {
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null || cards[i].root == null) continue;

                string text = (defaultCards != null && i < defaultCards.Length) ? defaultCards[i].idiomText : "";
                IdiomFamilyType fam = (defaultCards != null && i < defaultCards.Length) ? defaultCards[i].family : IdiomFamilyType.Easy;

                cards[i].Initialize(i, fam, text);

                // Add or setup EventTrigger / DragHandler proxy
                SetupCardDragHandlers(cards[i]);
            }
        }

        if (easyBucket != null && easyBucket.root != null)
        {
            if (easyBucket.rectTransform == null) easyBucket.rectTransform = easyBucket.root.GetComponent<RectTransform>();
            easyBucket.acceptedFamily = IdiomFamilyType.Easy;
            if (easyBucket.labelTMP != null) easyBucket.labelTMP.text = "VERY EASY";
        }

        if (crazyBucket != null && crazyBucket.root != null)
        {
            if (crazyBucket.rectTransform == null) crazyBucket.rectTransform = crazyBucket.root.GetComponent<RectTransform>();
            crazyBucket.acceptedFamily = IdiomFamilyType.CrazyStrange;
            if (crazyBucket.labelTMP != null) crazyBucket.labelTMP.text = "CRAZY / STRANGE";
        }
    }

    private void SetupCardDragHandlers(IdiomDragCardUI card)
    {
        if (card.root == null) return;

        EventTrigger trigger = card.root.GetComponent<EventTrigger>();
        if (trigger == null) trigger = card.root.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        // BeginDrag
        EventTrigger.Entry beginEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginEntry.callback.AddListener((data) => OnCardBeginDrag(card, (PointerEventData)data));
        trigger.triggers.Add(beginEntry);

        // Drag
        EventTrigger.Entry dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        dragEntry.callback.AddListener((data) => OnCardDrag(card, (PointerEventData)data));
        trigger.triggers.Add(dragEntry);

        // EndDrag
        EventTrigger.Entry endEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endEntry.callback.AddListener((data) => OnCardEndDrag(card, (PointerEventData)data));
        trigger.triggers.Add(endEntry);
    }

    private Canvas cachedCanvas;

    private Canvas GetCanvas()
    {
        if (cachedCanvas == null)
        {
            cachedCanvas = GetComponentInParent<Canvas>();
        }
        return cachedCanvas;
    }

    private Camera GetCanvasCamera()
    {
        Canvas c = GetCanvas();
        if (c != null && c.renderMode == RenderMode.ScreenSpaceCamera)
        {
            return c.worldCamera != null ? c.worldCamera : Camera.main;
        }
        return null;
    }

    public void OnCardBeginDrag(IdiomDragCardUI card, PointerEventData eventData)
    {
        if (card == null || card.isSolved || isCompleted) return;

        currentDraggingCard = card;
        card.isDragging = true;
        card.root.transform.DOKill();
        card.root.transform.SetAsLastSibling();
        card.root.transform.DOScale(card.originalScale * 1.05f, 0.15f);

        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    public void OnCardDrag(IdiomDragCardUI card, PointerEventData eventData)
    {
        if (card == null || card.isSolved || !card.isDragging) return;

        RectTransform parentRT = card.root.transform.parent as RectTransform;
        if (parentRT != null)
        {
            Camera cam = GetCanvasCamera();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRT, eventData.position, cam, out Vector2 localPoint))
            {
                card.rectTransform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            }
        }
    }

    public void OnCardEndDrag(IdiomDragCardUI card, PointerEventData eventData)
    {
        if (card == null || card.isSolved || !card.isDragging) return;

        card.isDragging = false;
        Camera cam = GetCanvasCamera();

        // Check if dropped on Easy Bucket
        if (easyBucket != null && easyBucket.ContainsScreenPoint(eventData.position, cam))
        {
            EvaluateDrop(card, easyBucket);
            return;
        }

        // Check if dropped on Crazy Bucket
        if (crazyBucket != null && crazyBucket.ContainsScreenPoint(eventData.position, cam))
        {
            EvaluateDrop(card, crazyBucket);
            return;
        }

        // Dropped nowhere valid -> bounce back
        card.BounceBackToOrigin();
    }

    private void EvaluateDrop(IdiomDragCardUI card, IdiomBucketUI targetBucket)
    {
        if (card.family == targetBucket.acceptedFamily)
        {
            // ================== CORRECT DROP ==================
            // Play SFX Drop
            if (sfxDrop != null && Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(sfxDrop);
            }
            else if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            targetBucket.PlayDropEffect();

            Vector3 bucketWorldPos = targetBucket.rectTransform != null ? targetBucket.rectTransform.position : targetBucket.root.transform.position;
            card.DissolveIntoBucket(bucketWorldPos, () =>
            {
                correctCount++;
                processedCount++;
                if (progressTMP != null)
                {
                    progressTMP.text = $"{correctCount}/{totalCards}";
                }
                CheckActivityCompletion();
            });
        }
        else
        {
            // ================== WRONG DROP ==================
            if (sfxWrong != null && Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(sfxWrong);
            }
            else if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            card.BounceBackToOrigin();
        }
    }

    private void CheckActivityCompletion()
    {
        if (processedCount >= totalCards || correctCount >= totalCards)
        {
            if (!isCompleted)
            {
                isCompleted = true;
                OnActivityCompleted();
            }
        }
    }

    private void OnActivityCompleted()
    {
        // Mark Unit 6 Reading progress complete if pass condition met
        if (correctCount >= passThreshold)
        {
            M3A_U6_HubProgress.MarkComplete(M3A_U6_Branch.Reading);
        }

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
