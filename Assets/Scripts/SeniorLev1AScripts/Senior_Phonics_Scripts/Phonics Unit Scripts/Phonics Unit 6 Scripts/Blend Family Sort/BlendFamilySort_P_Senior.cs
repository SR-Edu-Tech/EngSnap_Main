using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BlendFamilySortWord
{
    [Tooltip("The plain word text, e.g. 'block'")]
    public string wordText;

    [Tooltip("The formatted word text with highlight tags, e.g. '<b><color=#A020F0>bl</color></b>ock'")]
    public string highlightedWordText;

    [Tooltip("The family this word belongs to: L, R, or S")]
    public string blendFamily; // "L", "R", "S"

    [Tooltip("Optional image sprite representing the word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for the word audio")]
    public AudioClip wordAudio;
}

[System.Serializable]
public class BlendFamilySortBasketUI
{
    [Tooltip("The parent GameObject containing the basket visual elements")]
    public GameObject container;

    [Tooltip("Drop area RectTransform where drag detection occurs")]
    public RectTransform dropArea;

    [Tooltip("The TextMeshPro label for the basket heading")]
    public TextMeshProUGUI label;

    [Tooltip("The container/Layout where correct cards badges are stacked")]
    public RectTransform cardStackContainer;

    [Tooltip("Image component of background for hover highlight feedback")]
    public Image highlightBg;

    [HideInInspector] public Color originalColor;
}

public class BlendFamilySort_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    public List<BlendFamilySortWord> words = new List<BlendFamilySortWord>();

    [Header("UI Baskets / Columns")]
    public BlendFamilySortBasketUI lBasket; // L-blends
    public BlendFamilySortBasketUI rBasket; // R-blends
    public BlendFamilySortBasketUI sBasket; // S-blends

    [Header("UI Draggable Staging Card")]
    public RectTransform draggableCard;
    public TextMeshProUGUI draggableCardText;
    public Image draggableCardImage;
    public Image draggableCardBg;
    public DraggableBlendFamilyCard_P_Senior draggableCardHandler;
    public RectTransform stagingArea;

    [Tooltip("Prefab template for correctly sorted words in the baskets. If left unassigned, a default UI panel will be created dynamically.")]
    public GameObject wordBadgePrefab;

    [Header("UI Controls & Labels")]
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI instructionLabel;
    public RectTransform mascotCharacter;
    public GameObject starEffectObject;
    public GameObject globalNextButton;

    [Header("Progress Indicators")]
    public TextMeshProUGUI progressLabel;
    public RectTransform progressDotsContainer;
    public GameObject progressDotPrefab;
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = Color.gray;
    public Color dotFilledColor = Color.green;

    [Header("Audio Sources")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip cheerSFX;
    public AudioClip levelCompleteSFX;
    public AudioClip introClip;

    [Header("Gameplay Tuning")]
    public float dropInHeight = 600f;
    public Color basketNormalColor = new Color(1f, 1f, 1f, 0.1f);
    public Color basketHighlightColor = Color.yellow;
    public Color cardNormalColor = Color.white;
    public Color cardCorrectColor = Color.green;
    public Color cardWrongColor = Color.red;

    [Header("Word Badge Styling (Play Mode Fallback)")]
    [Tooltip("The background color of the dynamically generated sorted card badges.")]
    public Color wordBadgeBgColor = new Color(1f, 1f, 1f, 0.15f);

    [Tooltip("The text color of the dynamically generated sorted card badges.")]
    public Color wordBadgeTextColor = Color.white;

    [Tooltip("The font size of the dynamically generated sorted card badges.")]
    public float wordBadgeTextSize = 20f;

    [Tooltip("The width and height of the dynamically generated sorted card badges.")]
    public Vector2 wordBadgeSize = new Vector2(180f, 40f);

    // Runtime state
    private int _currentWordIndex = 0;
    private int _score = 0;
    private bool _started = false;
    private bool _canDrag = false;
    private Vector3 _originalMascotScale = Vector3.one;
    private Vector2 _originalTextAnchorMin;
    private Vector2 _originalTextAnchorMax;
    private Vector2 _originalTextOffsetMin;
    private Vector2 _originalTextOffsetMax;
    private List<GameObject> _dotInstances = new List<GameObject>();
    private List<GameObject> _instantiatedBadges = new List<GameObject>();
    private GameFlowManager_Senior_Phonics _flowManager;
    private GameObject _activeCardInstance;
    [Tooltip("The size of the collected cards stacked inside the columns")]
    public Vector2 collectedCardSize = new Vector2(75f, 90f);
    [Tooltip("The font size of the text on the cards")]
    public float cardTextSize = 20f;

    private void Awake()
    {
        // Force opaque default colors if serialized with 0 alpha
        if (dotEmptyColor.a == 0f)
        {
            dotEmptyColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
        if (dotFilledColor.a == 0f)
        {
            dotFilledColor = Color.green;
        }

        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        // Cache original word text layout coordinates for responsive centering
        if (draggableCardText != null)
        {
            RectTransform textRt = draggableCardText.rectTransform;
            _originalTextAnchorMin = textRt.anchorMin;
            _originalTextAnchorMax = textRt.anchorMax;
            _originalTextOffsetMin = textRt.offsetMin;
            _originalTextOffsetMax = textRt.offsetMax;
        }

        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (mascotAudioSource == null)
        {
            mascotAudioSource = GetComponent<AudioSource>();
            if (mascotAudioSource == null)
            {
                mascotAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // Cache initial colors for baskets
        CacheBasketColor(lBasket);
        CacheBasketColor(rBasket);
        CacheBasketColor(sBasket);

        if (draggableCard != null)
        {
            draggableCard.gameObject.SetActive(false);
        }

        ApplyCollectedCardSizeToGrid(lBasket);
        ApplyCollectedCardSizeToGrid(rBasket);
        ApplyCollectedCardSizeToGrid(sBasket);
    }

    private void ApplyCollectedCardSizeToGrid(BlendFamilySortBasketUI basket)
    {
        if (basket != null && basket.cardStackContainer != null)
        {
            GridLayoutGroup glg = basket.cardStackContainer.GetComponent<GridLayoutGroup>();
            if (glg != null)
            {
                glg.cellSize = collectedCardSize;
            }
        }
    }



    private void CacheBasketColor(BlendFamilySortBasketUI basket)
    {
        if (basket != null)
        {
            if (basket.highlightBg != null)
                basket.originalColor = basket.highlightBg.color;
            else
                basket.originalColor = basketNormalColor;
        }
    }

    private void Start()
    {
        _started = true;
        ResetToStart();
    }

    private void OnEnable()
    {
        if (_started)
        {
            ResetToStart();
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
    }

    public void ResetToStart()
    {
        _currentWordIndex = 0;
        _score = 0;

        UpdateScoreUI();

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(false);
        }

        ClearAllBadges();
        SetupProgressDots();

        // Reset basket highlight colors
        ResetBasketHighlights();

        // Play intro audio
        if (introClip != null && mascotAudioSource != null)
        {
            _canDrag = false;
            StartCoroutine(PlayAudioSequence(introClip, () => {
                LoadWord();
            }));
        }
        else
        {
            LoadWord();
        }
    }

    private void SetupProgressDots()
    {
        if (progressDotsContainer == null) return;

        // Clear existing dot instances
        foreach (var dot in _dotInstances)
        {
            if (dot != null) Destroy(dot);
        }
        _dotInstances.Clear();

        if (progressDotPrefab != null)
        {
            progressDotPrefab.SetActive(false);
        }

        int totalWords = words.Count;
        for (int i = 0; i < totalWords; i++)
        {
            if (progressDotPrefab != null)
            {
                GameObject dotObj = Instantiate(progressDotPrefab, progressDotsContainer);
                dotObj.SetActive(true);
                _dotInstances.Add(dotObj);
            }
        }

        UpdateProgressDots();
    }

    private void UpdateProgressDots()
    {
        for (int i = 0; i < _dotInstances.Count; i++)
        {
            Image img = _dotInstances[i].GetComponent<Image>();
            if (img == null) img = _dotInstances[i].GetComponentInChildren<Image>();

            if (img != null)
            {
                bool isCompleted = i < _currentWordIndex;
                if (isCompleted)
                {
                    if (dotFilledSprite != null) img.sprite = dotFilledSprite;
                    img.color = dotFilledColor;
                }
                else
                {
                    if (dotEmptySprite != null) img.sprite = dotEmptySprite;
                    img.color = dotEmptyColor;
                }
            }
        }
    }

    private void UpdateProgressLabel()
    {
        if (progressLabel != null)
        {
            progressLabel.text = $"Word {_currentWordIndex + 1} / {words.Count}";
        }
    }

    private void LoadWord()
    {
        if (words == null || words.Count == 0)
        {
            Debug.LogWarning("[BlendFamilySort] No words configured!");
            return;
        }

        if (_currentWordIndex < 0 || _currentWordIndex >= words.Count)
        {
            OnCompletedAll();
            return;
        }

        UpdateProgressLabel();

        var word = words[_currentWordIndex];

        if (_activeCardInstance != null)
        {
            Destroy(_activeCardInstance);
        }

        if (draggableCard == null)
        {
            Debug.LogError("[BlendFamilySort] Draggable card template is null!");
            return;
        }

        _activeCardInstance = Instantiate(draggableCard.gameObject, stagingArea);
        _activeCardInstance.SetActive(true);

        RectTransform instanceRt = _activeCardInstance.GetComponent<RectTransform>();
        TextMeshProUGUI instanceText = _activeCardInstance.GetComponentInChildren<TextMeshProUGUI>();
        Image instanceBg = _activeCardInstance.GetComponent<Image>();
        DraggableBlendFamilyCard_P_Senior instanceHandler = _activeCardInstance.GetComponent<DraggableBlendFamilyCard_P_Senior>();

        Image instanceImage = null;
        Transform imgTrans = _activeCardInstance.transform.Find("CardImage");
        if (imgTrans != null)
        {
            instanceImage = imgTrans.GetComponent<Image>();
        }
        else
        {
            Image[] childImages = _activeCardInstance.GetComponentsInChildren<Image>(true);
            foreach (var img in childImages)
            {
                if (img.gameObject != _activeCardInstance)
                {
                    instanceImage = img;
                    break;
                }
            }
        }

        if (instanceHandler != null)
        {
            instanceHandler.Setup(this);
        }

        if (instanceText != null)
        {
            instanceText.text = word.highlightedWordText;
            instanceText.fontSize = cardTextSize;

            RectTransform textRt = instanceText.rectTransform;
            if (word.wordSprite == null)
            {
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
            }
            else
            {
                textRt.anchorMin = _originalTextAnchorMin;
                textRt.anchorMax = _originalTextAnchorMax;
                textRt.offsetMin = _originalTextOffsetMin;
                textRt.offsetMax = _originalTextOffsetMax;
            }
        }

        if (instanceImage != null)
        {
            if (word.wordSprite != null)
            {
                instanceImage.sprite = word.wordSprite;
                instanceImage.color = Color.white;
                instanceImage.gameObject.SetActive(true);
            }
            else
            {
                instanceImage.gameObject.SetActive(false);
            }
        }

        if (instanceBg != null)
        {
            instanceBg.color = cardNormalColor;
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Drag the word to its family column.";
        }

        // Trigger drop-in animation
        if (instanceRt != null && stagingArea != null)
        {
            _canDrag = false;
            Vector3 startPos = new Vector3(0f, dropInHeight, 0f);
            instanceRt.localPosition = startPos;
            instanceRt.localScale = Vector3.zero;

            LeanTween.cancel(instanceRt.gameObject);
            LeanTween.moveLocal(instanceRt.gameObject, Vector3.zero, 0.6f).setEase(LeanTweenType.easeOutBack);
            LeanTween.scale(instanceRt.gameObject, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    _canDrag = true;
                    if (word.wordAudio != null)
                    {
                        StartCoroutine(PlayAudioSequence(word.wordAudio, null));
                    }
                });
        }
        else
        {
            _canDrag = true;
        }
    }

    public bool CanDragCard()
    {
        return _canDrag;
    }

    public void OnCardDragStart(DraggableBlendFamilyCard_P_Senior card)
    {
        if (card != null)
        {
            LeanTween.cancel(card.gameObject);
            LeanTween.scale(card.gameObject, Vector3.one * 1.05f, 0.15f);
        }
    }

    public void OnCardDragHover(DraggableBlendFamilyCard_P_Senior card, Vector2 screenPos)
    {
        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        // Hover L basket
        CheckBasketHover(lBasket, screenPos, cam);
        // Hover R basket
        CheckBasketHover(rBasket, screenPos, cam);
        // Hover S basket
        CheckBasketHover(sBasket, screenPos, cam);
    }

    private void CheckBasketHover(BlendFamilySortBasketUI basket, Vector2 screenPos, Camera cam)
    {
        if (basket != null && basket.container != null && basket.container.activeSelf)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(basket.dropArea, screenPos, cam))
            {
                if (basket.highlightBg != null)
                    basket.highlightBg.color = basketHighlightColor;
            }
            else
            {
                if (basket.highlightBg != null)
                    basket.highlightBg.color = basket.originalColor;
            }
        }
    }

    public void OnCardDragEnd(DraggableBlendFamilyCard_P_Senior card, Vector2 screenPos)
    {
        ResetBasketHighlights();

        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        var word = words[_currentWordIndex];

        // L basket check
        if (CheckBasketDrop(lBasket, screenPos, cam))
        {
            if (word.blendFamily == "L")
                StartCoroutine(HandleCorrectChoice(card, lBasket));
            else
                StartCoroutine(HandleIncorrectChoice(card));
            return;
        }

        // R basket check
        if (CheckBasketDrop(rBasket, screenPos, cam))
        {
            if (word.blendFamily == "R")
                StartCoroutine(HandleCorrectChoice(card, rBasket));
            else
                StartCoroutine(HandleIncorrectChoice(card));
            return;
        }

        // S basket check
        if (CheckBasketDrop(sBasket, screenPos, cam))
        {
            if (word.blendFamily == "S")
                StartCoroutine(HandleCorrectChoice(card, sBasket));
            else
                StartCoroutine(HandleIncorrectChoice(card));
            return;
        }

        // Dropped outside, return to staging
        ReturnToStaging(card);
    }

    private bool CheckBasketDrop(BlendFamilySortBasketUI basket, Vector2 screenPos, Camera cam)
    {
        if (basket != null && basket.container != null && basket.container.activeSelf)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(basket.dropArea, screenPos, cam);
        }
        return false;
    }

    private void ResetBasketHighlights()
    {
        ResetSingleBasketHighlight(lBasket);
        ResetSingleBasketHighlight(rBasket);
        ResetSingleBasketHighlight(sBasket);
    }

    private void ResetSingleBasketHighlight(BlendFamilySortBasketUI basket)
    {
        if (basket != null && basket.highlightBg != null)
        {
            basket.highlightBg.color = basket.originalColor;
        }
    }

    private void ReturnToStaging(DraggableBlendFamilyCard_P_Senior card)
    {
        if (card != null && stagingArea != null)
        {
            _canDrag = false;
            LeanTween.cancel(card.gameObject);
            LeanTween.scale(card.gameObject, Vector3.one, 0.2f);
            LeanTween.moveLocal(card.gameObject, Vector3.zero, 0.35f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() => {
                    _canDrag = true;
                });
        }
    }

    private IEnumerator HandleCorrectChoice(DraggableBlendFamilyCard_P_Senior card, BlendFamilySortBasketUI targetBasket)
    {
        _canDrag = false;

        if (sfxAudioSource != null && correctSFX != null)
        {
            sfxAudioSource.PlayOneShot(correctSFX);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Correct!";
        }

        Image cardBg = card.GetComponent<Image>();
        if (cardBg != null)
        {
            cardBg.color = cardCorrectColor;
        }

        if (card != null)
        {
            LeanTween.cancel(card.gameObject);
            LeanTween.scale(card.gameObject, Vector3.one * 1.15f, 0.2f).setLoopPingPong(1);
        }

        yield return new WaitForSeconds(0.4f);

        if (card != null && targetBasket.cardStackContainer != null)
        {
            card.transform.SetParent(targetBasket.cardStackContainer, false);
            card.enabled = false;

            var canvasGroup = card.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = card.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            _instantiatedBadges.Add(card.gameObject);

            RectTransform cardRt = card.GetComponent<RectTransform>();
            if (cardRt != null)
            {
                cardRt.localScale = Vector3.one;
            }
        }
        else if (card != null)
        {
            Destroy(card.gameObject);
        }

        _activeCardInstance = null;
        _score += 10;
        UpdateScoreUI();

        _currentWordIndex++;
        UpdateProgressDots();

        yield return new WaitForSeconds(0.3f);

        // Play completed word audio, then advance
        var word = words[_currentWordIndex - 1];
        if (word.wordAudio != null)
        {
            StartCoroutine(PlayAudioSequence(word.wordAudio, null));
        }

        if (_currentWordIndex >= words.Count)
        {
            OnCompletedAll();
        }
        else
        {
            LoadWord();
        }
    }

    private IEnumerator HandleIncorrectChoice(DraggableBlendFamilyCard_P_Senior card)
    {
        _canDrag = false;

        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Try again!";
        }

        Image cardBg = card.GetComponent<Image>();
        if (cardBg != null)
        {
            cardBg.color = cardWrongColor;
        }

        // Shake at current position, then bounce back smoothly to staging
        if (card != null && stagingArea != null)
        {
            LeanTween.cancel(card.gameObject);
            Vector3 droppedPos = card.transform.localPosition;
            float shakeAmt = 15f;

            // Shake card at the dropped position (wrong basket)
            LeanTween.moveLocalX(card.gameObject, droppedPos.x + shakeAmt, 0.05f)
                .setLoopPingPong(3)
                .setOnComplete(() => {
                    card.transform.localPosition = droppedPos;
                    if (cardBg != null)
                    {
                        cardBg.color = Color.white;
                    }
                    
                    // Bounce back (glide) from wrong basket back to Vector3.zero
                    LeanTween.moveLocal(card.gameObject, Vector3.zero, 0.45f)
                        .setEase(LeanTweenType.easeOutBack)
                        .setOnComplete(() => {
                            _canDrag = true;
                        });
                });
        }
        else
        {
            if (cardBg != null) cardBg.color = Color.white;
            _canDrag = true;
        }

        yield return null;
    }

    private void AddWordToBasketUI(string text, Sprite sprite, RectTransform container)
    {
        if (container == null) return;

        GameObject badge;
        if (wordBadgePrefab != null)
        {
            badge = Instantiate(wordBadgePrefab, container);
            
            // Set text on TextMeshProUGUI component
            TextMeshProUGUI tmp = badge.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
            }

            // Set sprite on Image component (preferring a child named 'Image' or 'Icon', otherwise first child image)
            Transform childImgTrans = badge.transform.Find("Image");
            if (childImgTrans == null) childImgTrans = badge.transform.Find("Icon");

            Image targetImg = null;
            if (childImgTrans != null)
            {
                targetImg = childImgTrans.GetComponent<Image>();
            }

            if (targetImg == null)
            {
                Image[] images = badge.GetComponentsInChildren<Image>();
                foreach (var i in images)
                {
                    // Avoid selecting the root background Image component
                    if (i.gameObject != badge)
                    {
                        targetImg = i;
                        break;
                    }
                }
                if (targetImg == null && images.Length > 0)
                {
                    targetImg = images[0];
                }
            }

            if (targetImg != null)
            {
                if (sprite != null)
                {
                    targetImg.sprite = sprite;
                    targetImg.color = Color.white;
                    targetImg.gameObject.SetActive(true);
                }
                else
                {
                    targetImg.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            badge = new GameObject("WordBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(container, false);
            
            Image bg = badge.GetComponent<Image>();
            bg.color = wordBadgeBgColor; // CUSTOM COLOR
            
            RectTransform rt = badge.GetComponent<RectTransform>();
            rt.sizeDelta = wordBadgeSize; // CUSTOM SIZE

            if (sprite != null)
            {
                GameObject imgObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                imgObj.transform.SetParent(badge.transform, false);
                RectTransform imgRt = imgObj.GetComponent<RectTransform>();
                imgRt.anchorMin = new Vector2(0.05f, 0.1f);
                imgRt.anchorMax = new Vector2(0.25f, 0.9f);
                imgRt.offsetMin = Vector2.zero;
                imgRt.offsetMax = Vector2.zero;

                Image iconImg = imgObj.GetComponent<Image>();
                iconImg.sprite = sprite;
                iconImg.color = Color.white;
                iconImg.preserveAspect = true;
            }
            
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(badge.transform, false);
            
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            if (sprite != null)
            {
                textRt.anchorMin = new Vector2(0.3f, 0f);
                textRt.anchorMax = new Vector2(1f, 1f);
            }
            else
            {
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
            }
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            
            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = wordBadgeTextSize; // CUSTOM FONT SIZE
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = wordBadgeTextColor; // CUSTOM COLOR
            tmp.fontStyle = FontStyles.Bold;
        }
        
        _instantiatedBadges.Add(badge);
    }

    private void ClearAllBadges()
    {
        foreach (var badge in _instantiatedBadges)
        {
            if (badge != null) Destroy(badge);
        }
        _instantiatedBadges.Clear();

        if (_activeCardInstance != null)
        {
            Destroy(_activeCardInstance);
            _activeCardInstance = null;
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = _score.ToString();
        }
    }

    private void OnCompletedAll()
    {
        _canDrag = false;

        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(true);
            var pop = starEffectObject.GetComponent<POPEffect_SeniorLev1A>();
            if (pop != null)
            {
                pop.enabled = false;
                pop.enabled = true;
            }
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Activity Complete!";
        }

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(true);
            globalNextButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(globalNextButton);
            LeanTween.scale(globalNextButton, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack);

            var btn = globalNextButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    if (_flowManager != null)
                    {
                        _flowManager.NextGameplay();
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                });
            }
        }
        else
        {
            // Auto advance after 2 seconds if next button is not linked
            StartCoroutine(AutoAdvanceFlow());
        }
    }

    private IEnumerator AutoAdvanceFlow()
    {
        float delay = 2.0f;
        if (unitCompleteAudio != null) { delay = Mathf.Max(delay, unitCompleteAudio.length + 0.5f); }
        yield return new WaitForSeconds(delay);
        if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator PlayAudioSequence(AudioClip clip, Action callback)
    {
        if (mascotAudioSource != null && clip != null)
        {
            mascotAudioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length);
        }
        callback?.Invoke();
    }
}
