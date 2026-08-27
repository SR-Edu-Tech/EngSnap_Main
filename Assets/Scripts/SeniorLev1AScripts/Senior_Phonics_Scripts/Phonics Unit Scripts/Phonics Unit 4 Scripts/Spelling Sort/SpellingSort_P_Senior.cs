using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SpellingSortWord
{
    [Tooltip("The word text, e.g. 'boil' or 'boy'")]
    public string wordText;

    [Tooltip("Optional image sprite representing the word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for the word audio")]
    public AudioClip wordAudio;

    [Tooltip("The index of the correct column (0-based) in the round's spelling list")]
    public int correctColumnIndex;
}

[System.Serializable]
public class SpellingSortRound
{
    [Tooltip("The spelling representations for the columns, e.g. ['oi', 'oy']")]
    public string[] columnSpellings;

    [Tooltip("The list of words to be sorted in this round")]
    public List<SpellingSortWord> words = new List<SpellingSortWord>();
}

[System.Serializable]
public class SpellingSortColumnUI
{
    [Tooltip("The parent GameObject containing the column visual elements")]
    public GameObject container;

    [Tooltip("Drop area RectTransform where drag detection occurs")]
    public RectTransform dropArea;

    [Tooltip("The TextMeshPro label for the spelling heading")]
    public TextMeshProUGUI spellingLabel;

    [Tooltip("The container/Layout where correct cards badges are stacked")]
    public RectTransform cardStackContainer;

    [Tooltip("Image component of background for hover highlight feedback")]
    public Image highlightBg;

    [HideInInspector] public Color originalColor;
}

public class SpellingSort_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    public List<SpellingSortRound> rounds = new List<SpellingSortRound>();

    [Header("UI Columns UI Elements")]
    [Tooltip("Columns for sorting (usually max of 3 slots supported visually)")]
    public SpellingSortColumnUI[] columns;

    [Header("UI Draggable Staging Card")]
    public RectTransform draggableCard;
    public TextMeshProUGUI draggableCardText;
    public Image draggableCardImage;
    public Image draggableCardBg;
    public DraggableSpellingCard_P_Senior draggableCardHandler;
    public RectTransform stagingArea;
    public GameObject correctCardBadgePrefab;

    [Header("UI Controls & Labels")]
    public Button replayWordButton;
    public Button listenAgainButton;
    public GameObject continueButton;
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI instructionLabel;
    public RectTransform mascotCharacter;
    public GameObject starEffectObject;

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
    public bool autoAdvanceRounds = false;
    public float roundCompleteDelay = 1.5f;
    public float dropInHeight = 600f;
    public Color columnNormalColor = Color.white;
    public Color columnHighlightColor = Color.yellow;
    public Color cardNormalColor = Color.white;
    public Color cardCorrectColor = Color.green;
    public Color cardWrongColor = Color.red;

    // Runtime state
    private int _currentRoundIndex = 0;
    private int _currentWordIndex = 0;
    private int _overallWordIndex = 0;
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
    private Coroutine _audioCoroutine;
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

        ValidateAndFixBadgePrefab();

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

        // Cache initial colors for columns
        foreach (var col in columns)
        {
            if (col != null && col.highlightBg != null)
            {
                col.originalColor = col.highlightBg.color;
            }
            else if (col != null)
            {
                col.originalColor = columnNormalColor;
            }
        }

        if (draggableCard != null)
        {
            draggableCard.gameObject.SetActive(false);
        }

        foreach (var col in columns)
        {
            if (col != null && col.cardStackContainer != null)
            {
                GridLayoutGroup glg = col.cardStackContainer.GetComponent<GridLayoutGroup>();
                if (glg != null)
                {
                    glg.cellSize = collectedCardSize;
                }
            }
        }
    }



    private void Start()
    {
        _started = true;

        if (replayWordButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string name = b.gameObject.name.ToLower();
                if (name.Contains("replay") || name.Contains("speaker") || name.Contains("sound") || name.Contains("listen"))
                {
                    replayWordButton = b;
                    break;
                }
            }
        }

        if (replayWordButton != null)
        {
            replayWordButton.onClick.RemoveAllListeners();
            replayWordButton.onClick.AddListener(OnReplayClicked);
        }

        if (listenAgainButton != null)
        {
            listenAgainButton.onClick.RemoveAllListeners();
            listenAgainButton.onClick.AddListener(OnReplayClicked);
        }

        if (continueButton != null)
        {
            var btn = continueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnContinueClicked);
            }
        }

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
        _overallWordIndex = 0;
        _score = 0;

        UpdateScoreUI();

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.SetActive(false);
        }

        ClearAllBadges();
        SetupProgressDots();
        LoadRound(_currentRoundIndex);
    }

    private void SetupProgressDots()
    {
        if (progressDotsContainer == null) return;

        List<GameObject> keptDots;
        GameObject activeDotTemplate = PrepareContainer(progressDotsContainer, progressDotPrefab, out keptDots);
        _dotInstances.Clear();

        if (activeDotTemplate == null)
        {
            Debug.LogError("[SpellingSort] No progress dot prefab or template found!");
            return;
        }

        int totalWords = GetTotalWordsCount();
        Debug.Log($"[SpellingSort] SetupProgressDots: totalWords = {totalWords}, template = {(activeDotTemplate != null ? activeDotTemplate.name : "null")}");
        for (int i = 0; i < totalWords; i++)
        {
            GameObject dotObj = Instantiate(activeDotTemplate, progressDotsContainer);
            dotObj.SetActive(true);
            _dotInstances.Add(dotObj);
        }

        UpdateProgressDots();
    }

    private int GetTotalWordsCount()
    {
        int count = 0;
        foreach (var r in rounds)
        {
            if (r != null && r.words != null)
            {
                count += r.words.Count;
            }
        }
        return count;
    }

    private void UpdateProgressDots()
    {
        for (int i = 0; i < _dotInstances.Count; i++)
        {
            Image img = _dotInstances[i].GetComponent<Image>();
            if (img == null) img = _dotInstances[i].GetComponentInChildren<Image>();

            if (img != null)
            {
                bool isCompleted = i < _overallWordIndex;
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
            progressLabel.text = $"Round {_currentRoundIndex + 1} / {rounds.Count}";
        }
    }

    private void LoadRound(int roundIdx)
    {
        _currentRoundIndex = roundIdx;
        _currentWordIndex = 0;

        if (rounds == null || rounds.Count == 0)
        {
            Debug.LogWarning("[SpellingSort] No rounds configured!");
            return;
        }

        if (roundIdx < 0 || roundIdx >= rounds.Count)
        {
            OnCompletedAll();
            return;
        }

        UpdateProgressLabel();
        ClearAllBadges();
        SetupColumnsForRound(roundIdx);

        // Play intro or level start audio
        if (roundIdx == 0 && introClip != null && mascotAudioSource != null)
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

    private void SetupColumnsForRound(int roundIdx)
    {
        var round = rounds[roundIdx];
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (col == null) continue;

            if (i < round.columnSpellings.Length)
            {
                col.container.SetActive(true);
                col.spellingLabel.text = round.columnSpellings[i];
                if (col.highlightBg != null)
                {
                    col.highlightBg.color = col.originalColor;
                }
            }
            else
            {
                col.container.SetActive(false);
            }
        }
    }

    private void LoadWord()
    {
        var round = rounds[_currentRoundIndex];
        if (round.words == null || _currentWordIndex >= round.words.Count)
        {
            OnRoundComplete();
            return;
        }        UpdateProgressLabel();

        var word = round.words[_currentWordIndex];

        if (_activeCardInstance != null)
        {
            Destroy(_activeCardInstance);
        }

        if (draggableCard == null)
        {
            Debug.LogError("[SpellingSort] Draggable card template is null!");
            return;
        }

        _activeCardInstance = Instantiate(draggableCard.gameObject, stagingArea);
        _activeCardInstance.SetActive(true);

        RectTransform instanceRt = _activeCardInstance.GetComponent<RectTransform>();
        TextMeshProUGUI instanceText = _activeCardInstance.GetComponentInChildren<TextMeshProUGUI>();
        Image instanceBg = _activeCardInstance.GetComponent<Image>();
        DraggableSpellingCard_P_Senior instanceHandler = _activeCardInstance.GetComponent<DraggableSpellingCard_P_Senior>();

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
            instanceText.text = word.wordText;
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
            instructionLabel.text = "Drag the word to its spelling.";
        }

        if (continueButton != null)
        {
            continueButton.SetActive(false);
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

    public void OnCardDragStart(DraggableSpellingCard_P_Senior card)
    {
        if (card != null)
        {
            LeanTween.cancel(card.gameObject);
            LeanTween.scale(card.gameObject, Vector3.one * 1.05f, 0.15f);
        }
    }

    public void OnCardDragHover(DraggableSpellingCard_P_Senior card, Vector2 screenPos)
    {
        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        var round = rounds[_currentRoundIndex];

        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (col == null || !col.container.activeSelf) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(col.dropArea, screenPos, cam))
            {
                if (col.highlightBg != null)
                {
                    col.highlightBg.color = columnHighlightColor;
                }
            }
            else
            {
                if (col.highlightBg != null)
                {
                    col.highlightBg.color = col.originalColor;
                }
            }
        }
    }

    public void OnCardDragEnd(DraggableSpellingCard_P_Senior card, Vector2 screenPos)
    {
        ResetAllColumnHighlights();

        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        var round = rounds[_currentRoundIndex];
        var word = round.words[_currentWordIndex];

        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (col == null || !col.container.activeSelf) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(col.dropArea, screenPos, cam))
            {
                if (i == word.correctColumnIndex)
                {
                    StartCoroutine(HandleCorrectChoice(card, i));
                    return;
                }
                else
                {
                    StartCoroutine(HandleIncorrectChoice(card));
                    return;
                }
            }
        }

        // Dropped outside columns, return to staging area
        ReturnToStaging(card);
    }

    private void ResetAllColumnHighlights()
    {
        foreach (var col in columns)
        {
            if (col != null && col.highlightBg != null)
            {
                col.highlightBg.color = col.originalColor;
            }
        }
    }

    private void ReturnToStaging(DraggableSpellingCard_P_Senior card)
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

    private IEnumerator HandleCorrectChoice(DraggableSpellingCard_P_Senior card, int colIndex)
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

        if (card != null && columns[colIndex].cardStackContainer != null)
        {
            card.transform.SetParent(columns[colIndex].cardStackContainer, false);
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
        _overallWordIndex++;
        UpdateProgressDots();

        yield return new WaitForSeconds(0.3f);

        var currentRound = rounds[_currentRoundIndex];
        if (_currentWordIndex >= currentRound.words.Count)
        {
            OnRoundComplete();
        }
        else
        {
            LoadWord();
        }
    }

    private IEnumerator HandleIncorrectChoice(DraggableSpellingCard_P_Senior card)
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

        // Shake animation
        if (card != null && stagingArea != null)
        {
            LeanTween.cancel(card.gameObject);
            Vector3 centerPos = Vector3.zero;
            float shakeAmt = 15f;
            
            LeanTween.moveLocalX(card.gameObject, centerPos.x + shakeAmt, 0.05f)
                .setLoopPingPong(3)
                .setOnComplete(() => {
                    card.transform.localPosition = centerPos;
                    if (cardBg != null)
                    {
                        cardBg.color = Color.white;
                    }
                    ReturnToStaging(card);
                });
        }
        else
        {
            if (cardBg != null)
            {
                cardBg.color = Color.white;
            }
            _canDrag = true;
        }

        yield return null;
    }

    private void OnRoundComplete()
    {
        _canDrag = false;

        if (sfxAudioSource != null && cheerSFX != null)
        {
            sfxAudioSource.PlayOneShot(cheerSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(true);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Round Complete!";
        }

        if (autoAdvanceRounds)
        {
            StartCoroutine(AutoAdvanceRoundSequence());
        }
        else
        {
            if (continueButton != null)
            {
                continueButton.SetActive(true);
                continueButton.transform.localScale = Vector3.zero;
                LeanTween.cancel(continueButton);
                LeanTween.scale(continueButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
            }
        }
    }

    private IEnumerator AutoAdvanceRoundSequence()
    {
        yield return new WaitForSeconds(roundCompleteDelay);
        OnContinueClicked();
    }

    private void OnContinueClicked()
    {
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        int nextRoundIndex = _currentRoundIndex + 1;
        if (nextRoundIndex < rounds.Count)
        {
            LoadRound(nextRoundIndex);
        }
        else
        {
            OnCompletedAll();
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

        if (instructionLabel != null)
        {
            instructionLabel.text = "Well Done!";
        }

        if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnReplayClicked()
    {
        if (!_canDrag || rounds == null || _currentRoundIndex >= rounds.Count) return;
        var round = rounds[_currentRoundIndex];
        if (round.words == null || _currentWordIndex >= round.words.Count) return;

        var word = round.words[_currentWordIndex];
        if (word.wordAudio != null)
        {
            _canDrag = false;
            StartCoroutine(PlayAudioSequence(word.wordAudio, () => {
                _canDrag = true;
            }));
        }
    }

    private IEnumerator PlayAudioSequence(AudioClip clip, System.Action onComplete)
    {
        if (clip != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = clip;
            mascotAudioSource.Play();

            yield return StartCoroutine(MascotTalkAnimation(clip.length));
        }
        else
        {
            yield return null;
        }

        onComplete?.Invoke();
    }

    private IEnumerator MascotTalkAnimation(float duration)
    {
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.05f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt(duration / 0.5f));
        }

        yield return new WaitForSeconds(duration);

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = _score.ToString();
            
            LeanTween.cancel(scoreLabel.gameObject);
            scoreLabel.transform.localScale = Vector3.one * 1.3f;
            LeanTween.scale(scoreLabel.gameObject, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutQuad);
        }
    }

    private void ClearAllBadges()
    {
        foreach (var badge in _instantiatedBadges)
        {
            if (badge != null)
            {
                Destroy(badge);
            }
        }
        _instantiatedBadges.Clear();

        if (_activeCardInstance != null)
        {
            Destroy(_activeCardInstance);
            _activeCardInstance = null;
        }
    }

    private GameObject PrepareContainer(RectTransform container, GameObject prefab, out List<GameObject> keptObjects)
    {
        keptObjects = new List<GameObject>();
        if (container == null) return null;

        GameObject template = prefab;

        if (template != null)
        {
            foreach (Transform child in container)
            {
                if (child.gameObject != template)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                else
                {
                    child.gameObject.SetActive(false); // Force the scene template to be inactive
                    keptObjects.Add(child.gameObject);
                }
            }
        }
        else
        {
            foreach (Transform child in container)
            {
                string nameLower = child.name.ToLower();
                if (nameLower != "bg" && nameLower != "background" && template == null)
                {
                    template = child.gameObject;
                    template.SetActive(false);
                    keptObjects.Add(template);
                }
                else if (nameLower == "bg" || nameLower == "background")
                {
                    keptObjects.Add(child.gameObject);
                }
                else
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
            }

            if (template == null && container.parent != null)
            {
                foreach (Transform sibling in container.parent)
                {
                    string nameLower = sibling.name.ToLower();
                    if (sibling.gameObject != container.gameObject && 
                        nameLower != "bg" && 
                        nameLower != "background" && 
                        template == null)
                    {
                        template = sibling.gameObject;
                        template.SetActive(false);
                        break;
                    }
                }
            }
        }

        return template;
    }

    private Transform FindChildRecursive(Transform parent, string nameContains)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(nameContains.ToLower()))
            {
                return child;
            }
            Transform found = FindChildRecursive(child, nameContains);
            if (found != null) return found;
        }
        return null;
    }

    private void ValidateAndFixBadgePrefab()
    {
        // 1. Print all children of the script's game object for diagnostics
        Debug.Log($"[SpellingSort] ({gameObject.name}) Listing all children of activity:");
        foreach (Transform child in transform)
        {
            Debug.Log($"[SpellingSort] - Child: '{child.name}', Active={child.gameObject.activeSelf}");
        }

        // 2. Search scene-wide with less restrictive checks (e.g. without isLoaded if it's early in load)
        if (correctCardBadgePrefab == null)
        {
            GameObject[] allGo = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGo)
            {
                if (go.name.Contains("SortedBadgeTemplate") || go.name.Contains("BadgeTemplate"))
                {
                    // Check if it belongs to a loaded scene or looks like a scene object
                    if (!string.IsNullOrEmpty(go.scene.name) || go.transform.parent != null)
                    {
                        correctCardBadgePrefab = go;
                        Debug.Log($"[SpellingSort] ({gameObject.name}) Auto-assigned correctCardBadgePrefab via scene search: '{go.name}' in scene '{go.scene.name}'");
                        break;
                    }
                }
            }
        }

        if (correctCardBadgePrefab == null)
        {
            Transform foundTemplate = FindChildRecursive(transform, "SortedBadgeTemplate");
            if (foundTemplate == null)
            {
                foundTemplate = FindChildRecursive(transform, "BadgeTemplate");
            }
            if (foundTemplate == null)
            {
                foundTemplate = FindChildRecursive(transform, "Badge");
            }

            if (foundTemplate != null)
            {
                correctCardBadgePrefab = foundTemplate.gameObject;
                Debug.Log($"[SpellingSort] ({gameObject.name}) Auto-assigned missing correctCardBadgePrefab from scene children: {correctCardBadgePrefab.name}");
            }
        }

        // Fallback: search the entire loaded scene (including inactive GameObjects)
        if (correctCardBadgePrefab == null)
        {
            GameObject[] allGo = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGo)
            {
                if (go.scene.isLoaded && (go.name.Equals("SortedBadgeTemplate", System.StringComparison.OrdinalIgnoreCase) || 
                                           go.name.Equals("BadgeTemplate", System.StringComparison.OrdinalIgnoreCase)))
                {
                    correctCardBadgePrefab = go;
                    Debug.Log($"[SpellingSort] ({gameObject.name}) Auto-assigned correctCardBadgePrefab via scene search: {go.name}");
                    break;
                }
            }
        }

        if (correctCardBadgePrefab == null)
        {
            Debug.LogWarning($"[SpellingSort] ({gameObject.name}) SortedBadgeTemplate is completely missing from scene! Creating a new one programmatically.");
            
            // Create the template container
            GameObject badgeTemplate = new GameObject("SortedBadgeTemplate", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            badgeTemplate.transform.SetParent(transform, false);
            
            RectTransform badgeRt = badgeTemplate.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(220f, 70f);
            badgeTemplate.GetComponent<Image>().color = new Color32(245, 245, 245, 255); // soft gray badge background

            HorizontalLayoutGroup badgeHlg = badgeTemplate.GetComponent<HorizontalLayoutGroup>();
            badgeHlg.padding = new RectOffset(10, 10, 8, 8);
            badgeHlg.spacing = 10f;
            badgeHlg.childAlignment = TextAnchor.MiddleLeft;
            badgeHlg.childControlWidth = false;
            badgeHlg.childControlHeight = false;
            badgeHlg.childForceExpandWidth = false;
            badgeHlg.childForceExpandHeight = false;

            // Badge Image component
            GameObject badgeImgObj = new GameObject("BadgeImage", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            badgeImgObj.transform.SetParent(badgeTemplate.transform, false);
            RectTransform badgeImgRt = badgeImgObj.GetComponent<RectTransform>();
            badgeImgRt.sizeDelta = new Vector2(50f, 50f);
            LayoutElement badgeImgLe = badgeImgObj.GetComponent<LayoutElement>();
            badgeImgLe.preferredWidth = 50f;
            badgeImgLe.preferredHeight = 50f;
            badgeImgObj.GetComponent<Image>().color = Color.white; // default white base

            // Badge Text component
            GameObject badgeTextObj = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            badgeTextObj.transform.SetParent(badgeTemplate.transform, false);
            RectTransform btRt = badgeTextObj.GetComponent<RectTransform>();
            btRt.sizeDelta = new Vector2(130f, 50f);
            LayoutElement badgeTextLe = badgeTextObj.GetComponent<LayoutElement>();
            badgeTextLe.preferredWidth = 130f;
            badgeTextLe.preferredHeight = 50f;

            TextMeshProUGUI badgeText = badgeTextObj.GetComponent<TextMeshProUGUI>();
            badgeText.text = "Word";
            badgeText.fontSize = 24f;
            badgeText.alignment = TextAlignmentOptions.Left; // Align left next to the image
            badgeText.color = Color.black;
            badgeText.fontStyle = FontStyles.Bold;

            badgeTemplate.SetActive(false); // template
            correctCardBadgePrefab = badgeTemplate;
        }

        if (correctCardBadgePrefab == null)
        {
            Debug.LogError($"[SpellingSort] ({gameObject.name}) correctCardBadgePrefab is null, cannot instantiate sorted badges!");
            return;
        }

        // Also fix columns stack container if null or set to parent column container instead of CardStackContainer child
        if (columns != null)
        {
            foreach (var col in columns)
            {
                if (col != null && col.container != null)
                {
                    if (col.cardStackContainer == null || col.cardStackContainer == col.container)
                    {
                        Transform stack = col.container.transform.Find("CardStackContainer");
                        if (stack != null)
                        {
                            col.cardStackContainer = stack.GetComponent<RectTransform>();
                            Debug.Log($"[SpellingSort] Auto-assigned correct cardStackContainer child for {col.container.name}.");
                        }
                    }
                }
            }
        }

        // If it doesn't have a HorizontalLayoutGroup, add one
        HorizontalLayoutGroup hlg = correctCardBadgePrefab.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
        {
            hlg = correctCardBadgePrefab.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 8, 8);
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
        }

        // Adjust template size if needed (only if it is completely zero/uninitialized)
        RectTransform rt = correctCardBadgePrefab.GetComponent<RectTransform>();
        if (rt != null && rt.sizeDelta == Vector2.zero)
        {
            rt.sizeDelta = new Vector2(220f, 70f);
        }

        // Check if "BadgeImage" child exists, if not create it
        Transform imgTrans = correctCardBadgePrefab.transform.Find("BadgeImage");
        if (imgTrans == null)
        {
            GameObject imgObj = new GameObject("BadgeImage", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            imgObj.transform.SetParent(correctCardBadgePrefab.transform, false);
            
            RectTransform imgRt = imgObj.GetComponent<RectTransform>();
            imgRt.sizeDelta = new Vector2(50f, 50f);
            
            LayoutElement le = imgObj.GetComponent<LayoutElement>();
            le.preferredWidth = 50f;
            le.preferredHeight = 50f;
            
            imgObj.GetComponent<Image>().color = Color.white;
            imgTrans = imgObj.transform;
        }

        // Check if "BadgeText" child exists, if not create it
        Transform textTrans = correctCardBadgePrefab.transform.Find("BadgeText");
        if (textTrans == null)
        {
            GameObject txtObj = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            txtObj.transform.SetParent(correctCardBadgePrefab.transform, false);
            
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.sizeDelta = new Vector2(130f, 50f);
            
            LayoutElement le = txtObj.GetComponent<LayoutElement>();
            le.preferredWidth = 130f;
            le.preferredHeight = 50f;
            
            TextMeshProUGUI tm = txtObj.GetComponent<TextMeshProUGUI>();
            tm.text = "Word";
            tm.fontSize = 24f;
            tm.alignment = TextAlignmentOptions.Left;
            tm.color = Color.black;
            tm.fontStyle = FontStyles.Bold;
        }
    }
}
