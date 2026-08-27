using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class GSortWord
{
    [Tooltip("The plain word text, e.g. 'gate'")]
    public string wordText;

    [Tooltip("The formatted word text with highlight tags, e.g. '<b><color=#A020F0>g</color></b>ate'")]
    public string highlightedWordText;

    [Tooltip("Is it Soft G (true/j/) or Hard G (false/g/)?")]
    public bool isSoftG;

    [Tooltip("Optional image sprite representing the word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for the word audio")]
    public AudioClip wordAudio;
}

[System.Serializable]
public class GSortRound
{
    [Tooltip("The name of the round (e.g. 'Round 1: Pictures' or 'Round 2: Written')")]
    public string roundName;

    [Tooltip("The list of words to be sorted in this round")]
    public List<GSortWord> words = new List<GSortWord>();
}

[System.Serializable]
public class GSortBasketUI
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

public class GSort_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    public List<GSortRound> rounds = new List<GSortRound>();

    [Header("UI Baskets / Columns")]
    public GSortBasketUI softGBasket; // Soft G /j/
    public GSortBasketUI hardGBasket; // Hard G /g/

    [Header("UI Draggable Staging Card")]
    public RectTransform draggableCard;
    public TextMeshProUGUI draggableCardText;
    public Image draggableCardImage;
    public Image draggableCardBg;
    public DraggableGSortCard_P_Senior draggableCardHandler;
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
    [Tooltip("Audio clip played when child makes a wrong choice, saying 'Look at the letter after the g.'")]
    public AudioClip wrongInstructionClip;

    [Header("Gameplay Tuning")]
    public float dropInHeight = 600f;
    public Color basketNormalColor = new Color(1f, 1f, 1f, 0.1f);
    public Color basketHighlightColor = Color.yellow;
    public Color cardNormalColor = Color.white;
    public Color cardCorrectColor = Color.green;
    public Color cardWrongColor = Color.red;

    [Header("Word Badge Styling (Play Mode Fallback)")]
    public Color wordBadgeBgColor = new Color(1f, 1f, 1f, 0.15f);
    public Color wordBadgeTextColor = Color.white;
    public float wordBadgeTextSize = 20f;
    public Vector2 wordBadgeSize = new Vector2(180f, 40f);

    // Runtime state
    private int _currentRoundIndex = 0;
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

    private void Reset()
    {
#if UNITY_EDITOR
        AutoAssignAndPopulate();
#else
        PopulateDefaultWords();
#endif
    }

    [ContextMenu("Populate Words")]
    public void PopulateDefaultWords()
    {
        rounds = new List<GSortRound>();

        // --- ROUND 1: Picture Set ---
        GSortRound r1 = new GSortRound();
        r1.roundName = "Round 1: Picture-Word Cards";
        
        string[] r1Words = { "gate", "goat", "frog", "giraffe", "page", "orange", "bridge", "guitar", "gold", "gum", "ginger", "cage" };
        string[] r1Highlights = {
            "<b><color=#A020F0>g</color></b>ate",
            "<b><color=#A020F0>g</color></b>oat",
            "fro<b><color=#A020F0>g</color></b>",
            "<b><color=#A020F0>g</color></b>iraffe",
            "pa<b><color=#A020F0>g</color></b>e",
            "oran<b><color=#A020F0>g</color></b>e",
            "brid<b><color=#A020F0>g</color></b>e",
            "<b><color=#A020F0>g</color></b>uitar",
            "<b><color=#A020F0>g</color></b>old",
            "<b><color=#A020F0>g</color></b>um",
            "<b><color=#A020F0>g</color></b>inger",
            "ca<b><color=#A020F0>g</color></b>e"
        };
        bool[] r1SoftG = { false, false, false, true, true, true, true, false, false, false, true, true };

        for (int i = 0; i < r1Words.Length; i++)
        {
            GSortWord w = new GSortWord();
            w.wordText = r1Words[i];
            w.highlightedWordText = r1Highlights[i];
            w.isSoftG = r1SoftG[i];
            r1.words.Add(w);
        }
        rounds.Add(r1);

        // --- ROUND 2: Written Set ---
        GSortRound r2 = new GSortRound();
        r2.roundName = "Round 2: Written Word Cards";

        string[] r2Words = { "fragile", "guest", "knowledge", "genius", "goat", "guitar", "energy", "great", "gold", "geyser" };
        string[] r2Highlights = {
            "fra<b><color=#A020F0>g</color></b>ile",
            "<b><color=#A020F0>g</color></b>uest",
            "knowled<b><color=#A020F0>g</color></b>e",
            "<b><color=#A020F0>g</color></b>enius",
            "<b><color=#A020F0>g</color></b>oat",
            "<b><color=#A020F0>g</color></b>uitar",
            "ener<b><color=#A020F0>g</color></b>y",
            "<b><color=#A020F0>g</color></b>reat",
            "<b><color=#A020F0>g</color></b>old",
            "<b><color=#A020F0>g</color></b>eyser"
        };
        bool[] r2SoftG = { true, false, true, true, false, false, true, false, false, false };

        for (int i = 0; i < r2Words.Length; i++)
        {
            GSortWord w = new GSortWord();
            w.wordText = r2Words[i];
            w.highlightedWordText = r2Highlights[i];
            w.isSoftG = r2SoftG[i];
            r2.words.Add(w);
        }
        rounds.Add(r2);
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Assign and Populate")]
    public void AutoAssignAndPopulate()
    {
        // 1. Populate default words first
        PopulateDefaultWords();

        // 2. Automatically find and assign audio and sprite assets
        foreach (var round in rounds)
        {
            foreach (var word in round.words)
            {
                // Load Sprite (Round 1 only)
                if (round == rounds[0])
                {
                    string spritePath = FindAssetPathInEditor(word.wordText, "t:Sprite");
                    if (!string.IsNullOrEmpty(spritePath))
                    {
                        word.wordSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    }
                }

                // Load Audio
                string audioPath = FindAssetPathInEditor(word.wordText, "t:AudioClip");
                if (!string.IsNullOrEmpty(audioPath))
                {
                    word.wordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
                }
            }
        }

        // 3. Populate default clips
        if (correctSFX == null) correctSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Correct Answer.mp3");
        if (wrongSFX == null) wrongSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/That is incorrect, Try again.mp3");
        if (cheerSFX == null) cheerSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (levelCompleteSFX == null) levelCompleteSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (introClip == null) introClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/PopUpSound.mp3");

        string mascotWrongPath = FindAssetPathInEditor("Look at the letter after the g", "t:AudioClip");
        if (string.IsNullOrEmpty(mascotWrongPath)) mascotWrongPath = FindAssetPathInEditor("Look at the letter after", "t:AudioClip");
        if (!string.IsNullOrEmpty(mascotWrongPath))
        {
            wrongInstructionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(mascotWrongPath);
        }

        // 4. Assign UI child references
        Transform mascotAudioTrans = transform.Find("MascotAudioSource");
        if (mascotAudioTrans != null) mascotAudioSource = mascotAudioTrans.GetComponent<AudioSource>();

        Transform sfxAudioTrans = transform.Find("SFXAudioSource");
        if (sfxAudioTrans != null) sfxAudioSource = sfxAudioTrans.GetComponent<AudioSource>();

        Transform stagingTrans = transform.Find("StagingArea");
        if (stagingTrans != null)
        {
            stagingArea = stagingTrans.GetComponent<RectTransform>();
            Transform cardTrans = stagingTrans.Find("ActiveDraggableCard");
            if (cardTrans != null)
            {
                draggableCard = cardTrans.GetComponent<RectTransform>();
                draggableCardBg = cardTrans.GetComponent<Image>();
                draggableCardHandler = cardTrans.GetComponent<DraggableGSortCard_P_Senior>();
                if (draggableCardHandler == null)
                {
                    draggableCardHandler = cardTrans.gameObject.AddComponent<DraggableGSortCard_P_Senior>();
                }

                Transform cardImgTrans = cardTrans.Find("CardImage");
                if (cardImgTrans != null) draggableCardImage = cardImgTrans.GetComponent<Image>();

                Transform cardTxtTrans = cardTrans.Find("CardText");
                if (cardTxtTrans != null) draggableCardText = cardTxtTrans.GetComponent<TextMeshProUGUI>();
            }
        }

        Transform basketsContainerTrans = transform.Find("BasketsContainer");
        if (basketsContainerTrans != null)
        {
            if (basketsContainerTrans.childCount > 0)
            {
                softGBasket = SetupBasketUIReference(basketsContainerTrans.GetChild(0), "Soft G /j/");
            }
            if (basketsContainerTrans.childCount > 1)
            {
                hardGBasket = SetupBasketUIReference(basketsContainerTrans.GetChild(1), "Hard G /g/");
            }
        }

        Transform scoreTrans = transform.Find("ScorePanel");
        if (scoreTrans != null) scoreLabel = scoreTrans.GetComponent<TextMeshProUGUI>();

        Transform progressLabelTrans = transform.Find("ProgressTextLabel");
        if (progressLabelTrans != null) progressLabel = progressLabelTrans.GetComponent<TextMeshProUGUI>();

        Transform progressDotsTrans = transform.Find("ProgressDotsContainer");
        if (progressDotsTrans != null)
        {
            progressDotsContainer = progressDotsTrans.GetComponent<RectTransform>();
            Transform dotTemplate = progressDotsTrans.Find("ProgressDotTemplate");
            if (dotTemplate != null) progressDotPrefab = dotTemplate.gameObject;
        }

        Transform instructionBgTrans = transform.Find("Instruction Bg");
        if (instructionBgTrans != null)
        {
            Transform instTrans = instructionBgTrans.Find("InstructionLabel");
            if (instTrans == null) instTrans = instructionBgTrans.Find("Instruction Label");
            if (instTrans != null) instructionLabel = instTrans.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Transform instTrans = transform.Find("InstructionLabel");
            if (instTrans != null) instructionLabel = instTrans.GetComponent<TextMeshProUGUI>();
        }

        Transform starTrans = transform.Find("StarEffectPlaceholder");
        if (starTrans != null) starEffectObject = starTrans.gameObject;

        GameObject nextBtnObj = GameObject.Find("GlobalNextButton");
        if (nextBtnObj == null) nextBtnObj = GameObject.Find("NextButton");
        if (nextBtnObj != null) globalNextButton = nextBtnObj;

        GameObject characterObj = GameObject.Find("Character");
        if (characterObj == null) characterObj = GameObject.Find("MascotCharacter");
        if (characterObj != null) mascotCharacter = characterObj.GetComponent<RectTransform>();

        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log("[GSort] Script configured and references assigned automatically!");
    }

    private GSortBasketUI SetupBasketUIReference(Transform basketTrans, string defaultLabel)
    {
        GSortBasketUI basket = new GSortBasketUI();
        basket.container = basketTrans.gameObject;
        basket.dropArea = basketTrans.GetComponent<RectTransform>();
        basket.highlightBg = basketTrans.GetComponent<Image>();

        Transform headerLabelTrans = basketTrans.Find("BasketHeaderLabel");
        if (headerLabelTrans != null)
        {
            basket.label = headerLabelTrans.GetComponent<TextMeshProUGUI>();
            if (basket.label != null && (string.IsNullOrEmpty(basket.label.text) || basket.label.text.StartsWith("L-") || basket.label.text.StartsWith("R-") || basket.label.text.StartsWith("S-")))
            {
                basket.label.text = defaultLabel;
            }
        }

        Transform stackTrans = basketTrans.Find("CardStackContainer");
        if (stackTrans != null) basket.cardStackContainer = stackTrans.GetComponent<RectTransform>();

        return basket;
    }

    private string FindAssetPathInEditor(string name, string filterType)
    {
        string filter = name + " " + filterType;
        string[] guids = UnityEditor.AssetDatabase.FindAssets(filter);
        if (guids != null && guids.Length > 0)
        {
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (filename == name.ToLower())
                {
                    return path;
                }
            }
        }
        return null;
    }
#endif

    private void Awake()
    {
        if (rounds == null || rounds.Count == 0)
        {
            PopulateDefaultWords();
        }

        if (dotEmptyColor.a == 0f) dotEmptyColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        if (dotFilledColor.a == 0f) dotFilledColor = Color.green;

        if (mascotCharacter != null) _originalMascotScale = mascotCharacter.localScale;

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
            if (mascotAudioSource == null) mascotAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (sfxAudioSource == null)
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }

        CacheBasketColor(softGBasket);
        CacheBasketColor(hardGBasket);

        if (draggableCard != null)
        {
            draggableCard.gameObject.SetActive(false);
        }

        ApplyCollectedCardSizeToGrid(softGBasket);
        ApplyCollectedCardSizeToGrid(hardGBasket);
    }

    private void ApplyCollectedCardSizeToGrid(GSortBasketUI basket)
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


    private void CacheBasketColor(GSortBasketUI basket)
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
        _currentRoundIndex = 0;
        _currentWordIndex = 0;
        _score = 0;

        UpdateScoreUI();

        if (starEffectObject != null) starEffectObject.SetActive(false);
        if (globalNextButton != null) globalNextButton.SetActive(false);

        ClearAllBadges();
        SetupRound();

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

    private void SetupRound()
    {
        _currentWordIndex = 0;
        ClearAllBadges();
        SetupProgressDots();
        ResetBasketHighlights();
    }

    private void SetupProgressDots()
    {
        if (progressDotsContainer == null) return;

        foreach (var dot in _dotInstances)
        {
            if (dot != null) Destroy(dot);
        }
        _dotInstances.Clear();

        if (progressDotPrefab != null)
        {
            progressDotPrefab.SetActive(false);
        }

        if (rounds == null || _currentRoundIndex >= rounds.Count) return;
        var round = rounds[_currentRoundIndex];
        int totalWords = round.words.Count;

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
        if (progressLabel != null && rounds != null && _currentRoundIndex < rounds.Count)
        {
            var round = rounds[_currentRoundIndex];
            progressLabel.text = $"Word {_currentWordIndex + 1} / {round.words.Count} (Round {_currentRoundIndex + 1}/{rounds.Count})";
        }
    }

    private void LoadWord()
    {
        if (rounds == null || rounds.Count == 0 || _currentRoundIndex >= rounds.Count)
        {
            Debug.LogWarning("[GSort] No rounds configured!");
            return;
        }

        var round = rounds[_currentRoundIndex];
        if (round.words == null || round.words.Count == 0)
        {
            Debug.LogWarning($"[GSort] No words configured in round {_currentRoundIndex}!");
            return;
        }

        if (_currentWordIndex < 0 || _currentWordIndex >= round.words.Count)
        {
            AdvanceRound();
            return;
        }

        UpdateProgressLabel();

        var word = round.words[_currentWordIndex];

        if (_activeCardInstance != null)
        {
            Destroy(_activeCardInstance);
        }

        if (draggableCard == null)
        {
            Debug.LogError("[GSort] Draggable card template is null!");
            return;
        }

        _activeCardInstance = Instantiate(draggableCard.gameObject, stagingArea);
        _activeCardInstance.SetActive(true);

        RectTransform instanceRt = _activeCardInstance.GetComponent<RectTransform>();
        TextMeshProUGUI instanceText = _activeCardInstance.GetComponentInChildren<TextMeshProUGUI>();
        Image instanceBg = _activeCardInstance.GetComponent<Image>();
        DraggableGSortCard_P_Senior instanceHandler = _activeCardInstance.GetComponent<DraggableGSortCard_P_Senior>();

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
            instructionLabel.text = "Drag the card to Soft G /j/ or Hard G /g/.";
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
                    PlayCurrentWordAudio();
                });
        }
        else
        {
            _canDrag = true;
        }
    }


    public void PlayCurrentWordAudio()
    {
        if (rounds == null || _currentRoundIndex >= rounds.Count) return;
        var round = rounds[_currentRoundIndex];
        if (round.words == null || _currentWordIndex >= round.words.Count) return;

        var word = round.words[_currentWordIndex];
        if (word.wordAudio != null)
        {
            StartCoroutine(PlayAudioSequence(word.wordAudio, null));
        }
    }

    public bool CanDragCard()
    {
        return _canDrag;
    }

    public void OnCardDragStart(DraggableGSortCard_P_Senior card)
    {
        if (card != null)
        {
            LeanTween.cancel(card.gameObject);
            LeanTween.scale(card.gameObject, Vector3.one * 1.05f, 0.15f);
        }
    }

    public void OnCardDragHover(DraggableGSortCard_P_Senior card, Vector2 screenPos)
    {
        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        CheckBasketHover(softGBasket, screenPos, cam);
        CheckBasketHover(hardGBasket, screenPos, cam);
    }

    private void CheckBasketHover(GSortBasketUI basket, Vector2 screenPos, Camera cam)
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

    public void OnCardDragEnd(DraggableGSortCard_P_Senior card, Vector2 screenPos)
    {
        ResetBasketHighlights();

        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        if (rounds == null || _currentRoundIndex >= rounds.Count) return;
        var round = rounds[_currentRoundIndex];
        var word = round.words[_currentWordIndex];

        // Soft G drop check
        if (CheckBasketDrop(softGBasket, screenPos, cam))
        {
            if (word.isSoftG)
                StartCoroutine(HandleCorrectChoice(card, softGBasket));
            else
                StartCoroutine(HandleIncorrectChoice(card));
            return;
        }

        // Hard G drop check
        if (CheckBasketDrop(hardGBasket, screenPos, cam))
        {
            if (!word.isSoftG)
                StartCoroutine(HandleCorrectChoice(card, hardGBasket));
            else
                StartCoroutine(HandleIncorrectChoice(card));
            return;
        }

        // Return to staging
        ReturnToStaging(card);
    }

    private bool CheckBasketDrop(GSortBasketUI basket, Vector2 screenPos, Camera cam)
    {
        if (basket != null && basket.container != null && basket.container.activeSelf)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(basket.dropArea, screenPos, cam);
        }
        return false;
    }

    private void ResetBasketHighlights()
    {
        ResetSingleBasketHighlight(softGBasket);
        ResetSingleBasketHighlight(hardGBasket);
    }

    private void ResetSingleBasketHighlight(GSortBasketUI basket)
    {
        if (basket != null && basket.highlightBg != null)
        {
            basket.highlightBg.color = basket.originalColor;
        }
    }

    private void ReturnToStaging(DraggableGSortCard_P_Senior card)
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

    private IEnumerator HandleCorrectChoice(DraggableGSortCard_P_Senior card, GSortBasketUI targetBasket)
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

        var round = rounds[_currentRoundIndex];
        var word = round.words[_currentWordIndex - 1];
        if (word.wordAudio != null)
        {
            yield return StartCoroutine(PlayAudioSequence(word.wordAudio, null));
        }

        if (_currentWordIndex >= round.words.Count)
        {
            AdvanceRound();
        }
        else
        {
            LoadWord();
        }
    }

    private IEnumerator HandleIncorrectChoice(DraggableGSortCard_P_Senior card)
    {
        _canDrag = false;

        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Look at the letter after the g.";
        }

        Image cardBg = card.GetComponent<Image>();
        if (cardBg != null)
        {
            cardBg.color = cardWrongColor;
        }

        // Play custom incorrect voice line
        if (wrongInstructionClip != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(wrongInstructionClip);
        }

        if (card != null && stagingArea != null)
        {
            LeanTween.cancel(card.gameObject);
            Vector3 droppedPos = card.transform.localPosition;
            float shakeAmt = 15f;

            LeanTween.moveLocalX(card.gameObject, droppedPos.x + shakeAmt, 0.05f)
                .setLoopPingPong(3)
                .setOnComplete(() => {
                    card.transform.localPosition = droppedPos;
                    if (cardBg != null)
                    {
                        cardBg.color = Color.white;
                    }
                    
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


    private void AdvanceRound()
    {
        _currentRoundIndex++;
        if (_currentRoundIndex >= rounds.Count)
        {
            OnCompletedAll();
        }
        else
        {
            SetupRound();
            LoadWord();
        }
    }

    private void AddWordToBasketUI(string text, Sprite sprite, RectTransform container)
    {
        if (container == null) return;

        GameObject badge;
        if (wordBadgePrefab != null)
        {
            badge = Instantiate(wordBadgePrefab, container);
            TextMeshProUGUI tmp = badge.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;

            Transform childImgTrans = badge.transform.Find("Image");
            if (childImgTrans == null) childImgTrans = badge.transform.Find("Icon");

            Image targetImg = null;
            if (childImgTrans != null) targetImg = childImgTrans.GetComponent<Image>();

            if (targetImg == null)
            {
                Image[] images = badge.GetComponentsInChildren<Image>();
                foreach (var i in images)
                {
                    if (i.gameObject != badge)
                    {
                        targetImg = i;
                        break;
                    }
                }
                if (targetImg == null && images.Length > 0) targetImg = images[0];
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
            bg.color = wordBadgeBgColor;
            
            RectTransform rt = badge.GetComponent<RectTransform>();
            rt.sizeDelta = wordBadgeSize;

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
            tmp.fontSize = wordBadgeTextSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = wordBadgeTextColor;
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
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length);
        }
        callback?.Invoke();
    }
}
