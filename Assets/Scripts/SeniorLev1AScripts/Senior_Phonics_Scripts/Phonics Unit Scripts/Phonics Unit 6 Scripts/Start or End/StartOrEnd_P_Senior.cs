using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StartOrEndWord
{
    [Tooltip("The plain word text, e.g. 'block'")]
    public string wordText;

    [Tooltip("The formatted word text with highlight tags, e.g. '<b><color=#A020F0>bl</color></b>ock'")]
    public string highlightedWordText;

    [Tooltip("Is this word a Beginning Blend (true) or Ending Blend (false)?")]
    public bool isBeginningBlend;

    [Tooltip("Optional image sprite representing the word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for the word audio")]
    public AudioClip wordAudio;
}

[System.Serializable]
public class StartOrEndRound
{
    [Tooltip("The list of words to be sorted in this round")]
    public List<StartOrEndWord> words = new List<StartOrEndWord>();
}

[System.Serializable]
public class StartOrEndBasketUI
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

public class StartOrEnd_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    public List<StartOrEndRound> rounds = new List<StartOrEndRound>();

    [Header("UI Baskets")]
    public StartOrEndBasketUI beginningBasket;
    public StartOrEndBasketUI endingBasket;

    [Header("UI Draggable Staging Card")]
    public RectTransform draggableCard;
    public TextMeshProUGUI draggableCardText;
    public Image draggableCardImage;
    public Image draggableCardBg;
    public DraggableStartOrEndCard_P_Senior draggableCardHandler;
    public RectTransform stagingArea;
    public GameObject correctCardBadgePrefab;

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
    public bool autoAdvanceRounds = true;
    public float roundCompleteDelay = 1.5f;
    public float dropInHeight = 600f;
    public Color basketNormalColor = new Color(1f, 1f, 1f, 0.1f);
    public Color basketHighlightColor = Color.yellow;
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
        if (beginningBasket != null)
        {
            if (beginningBasket.highlightBg != null)
                beginningBasket.originalColor = beginningBasket.highlightBg.color;
            else
                beginningBasket.originalColor = basketNormalColor;
        }

        if (endingBasket != null)
        {
            if (endingBasket.highlightBg != null)
                endingBasket.originalColor = endingBasket.highlightBg.color;
            else
                endingBasket.originalColor = basketNormalColor;
        }

        if (draggableCardHandler != null)
        {
            draggableCardHandler.Setup(this);
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
        _overallWordIndex = 0;
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
        LoadRound(_currentRoundIndex);
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

        // Deactivate template if it exists
        if (progressDotPrefab != null)
        {
            progressDotPrefab.SetActive(false);
        }

        int totalWords = GetTotalWordsCount();
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
            Debug.LogWarning("[StartOrEnd] No rounds configured!");
            return;
        }

        if (roundIdx < 0 || roundIdx >= rounds.Count)
        {
            OnCompletedAll();
            return;
        }

        UpdateProgressLabel();
        ClearAllBadges();

        // Reset highlight colors
        ResetBasketHighlights();

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

    private void LoadWord()
    {
        var round = rounds[_currentRoundIndex];
        if (round.words == null || _currentWordIndex >= round.words.Count)
        {
            OnRoundComplete();
            return;
        }

        var word = round.words[_currentWordIndex];

        if (draggableCardText != null)
        {
            draggableCardText.text = word.highlightedWordText;
        }

        if (draggableCardText != null)
        {
            RectTransform textRt = draggableCardText.rectTransform;
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

        if (draggableCardImage != null)
        {
            if (word.wordSprite != null)
            {
                draggableCardImage.sprite = word.wordSprite;
                draggableCardImage.color = Color.white;
                draggableCardImage.gameObject.SetActive(true);
            }
            else
            {
                draggableCardImage.gameObject.SetActive(false);
            }
        }

        if (draggableCardBg != null)
        {
            draggableCardBg.color = cardNormalColor;
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Drag the word to the right basket.";
        }



        // Trigger drop-in animation
        if (draggableCard != null && stagingArea != null)
        {
            _canDrag = false;
            draggableCard.gameObject.SetActive(true);
            Vector3 startPos = new Vector3(0f, dropInHeight, 0f);
            draggableCard.localPosition = startPos;
            draggableCard.localScale = Vector3.zero;

            LeanTween.cancel(draggableCard.gameObject);
            LeanTween.moveLocal(draggableCard.gameObject, Vector3.zero, 0.6f).setEase(LeanTweenType.easeOutBack);
            LeanTween.scale(draggableCard.gameObject, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack)
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

    public void OnCardDragStart(DraggableStartOrEndCard_P_Senior card)
    {
        if (draggableCard != null)
        {
            LeanTween.cancel(draggableCard.gameObject);
            LeanTween.scale(draggableCard.gameObject, Vector3.one * 1.05f, 0.15f);
        }
    }

    public void OnCardDragHover(Vector2 screenPos)
    {
        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        // Check Beginning basket
        if (beginningBasket != null && beginningBasket.container != null && beginningBasket.container.activeSelf)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(beginningBasket.dropArea, screenPos, cam))
            {
                if (beginningBasket.highlightBg != null)
                    beginningBasket.highlightBg.color = basketHighlightColor;
            }
            else
            {
                if (beginningBasket.highlightBg != null)
                    beginningBasket.highlightBg.color = beginningBasket.originalColor;
            }
        }

        // Check Ending basket
        if (endingBasket != null && endingBasket.container != null && endingBasket.container.activeSelf)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(endingBasket.dropArea, screenPos, cam))
            {
                if (endingBasket.highlightBg != null)
                    endingBasket.highlightBg.color = basketHighlightColor;
            }
            else
            {
                if (endingBasket.highlightBg != null)
                    endingBasket.highlightBg.color = endingBasket.originalColor;
            }
        }
    }

    public void OnCardDragEnd(Vector2 screenPos)
    {
        ResetBasketHighlights();

        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        var round = rounds[_currentRoundIndex];
        var word = round.words[_currentWordIndex];

        // Check Beginning Basket drop
        if (beginningBasket != null && beginningBasket.container != null && beginningBasket.container.activeSelf)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(beginningBasket.dropArea, screenPos, cam))
            {
                if (word.isBeginningBlend)
                {
                    StartCoroutine(HandleCorrectChoice(beginningBasket));
                }
                else
                {
                    StartCoroutine(HandleIncorrectChoice());
                }
                return;
            }
        }

        // Check Ending Basket drop
        if (endingBasket != null && endingBasket.container != null && endingBasket.container.activeSelf)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(endingBasket.dropArea, screenPos, cam))
            {
                if (!word.isBeginningBlend)
                {
                    StartCoroutine(HandleCorrectChoice(endingBasket));
                }
                else
                {
                    StartCoroutine(HandleIncorrectChoice());
                }
                return;
            }
        }

        // Dropped outside, return to staging
        ReturnToStaging();
    }

    private void ResetBasketHighlights()
    {
        if (beginningBasket != null && beginningBasket.highlightBg != null)
        {
            beginningBasket.highlightBg.color = beginningBasket.originalColor;
        }
        if (endingBasket != null && endingBasket.highlightBg != null)
        {
            endingBasket.highlightBg.color = endingBasket.originalColor;
        }
    }

    private void ReturnToStaging()
    {
        if (draggableCard != null && stagingArea != null)
        {
            _canDrag = false;
            LeanTween.cancel(draggableCard.gameObject);
            LeanTween.scale(draggableCard.gameObject, Vector3.one, 0.2f);
            LeanTween.moveLocal(draggableCard.gameObject, Vector3.zero, 0.35f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() => {
                    _canDrag = true;
                });
        }
    }

    private IEnumerator HandleCorrectChoice(StartOrEndBasketUI targetBasket)
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

        // Flash bg green
        if (draggableCardBg != null)
        {
            draggableCardBg.color = cardCorrectColor;
        }

        // Pulse scale
        if (draggableCard != null)
        {
            LeanTween.cancel(draggableCard.gameObject);
            LeanTween.scale(draggableCard.gameObject, Vector3.one * 1.15f, 0.2f).setLoopPingPong(1);
        }

        yield return new WaitForSeconds(0.4f);

        // Badge instantiation removed as per request (no collected word display)

        // Hide draggable card
        if (draggableCard != null)
        {
            LeanTween.cancel(draggableCard.gameObject);
            LeanTween.scale(draggableCard.gameObject, Vector3.zero, 0.2f);
            yield return new WaitForSeconds(0.2f);
            draggableCard.gameObject.SetActive(false);
        }

        _score += 10;
        UpdateScoreUI();

        _currentWordIndex++;
        _overallWordIndex++;
        UpdateProgressDots();

        yield return new WaitForSeconds(0.3f);

        // Play completed word audio, then advance or show continue button
        var currentRound = rounds[_currentRoundIndex];
        var wordData = currentRound.words[_currentWordIndex - 1];

        if (wordData.wordAudio != null)
        {
            StartCoroutine(PlayAudioSequence(wordData.wordAudio, null));
        }

        if (_currentWordIndex >= currentRound.words.Count)
        {
            OnRoundComplete();
        }
        else
        {
            LoadWord();
        }
    }

    private IEnumerator HandleIncorrectChoice()
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

        // Flash bg red
        if (draggableCardBg != null)
        {
            draggableCardBg.color = cardWrongColor;
        }

        // Shake at current position, then bounce back smoothly to staging
        if (draggableCard != null && stagingArea != null)
        {
            LeanTween.cancel(draggableCard.gameObject);
            Vector3 droppedPos = draggableCard.localPosition;
            float shakeAmt = 15f;

            // Shake card at the dropped position (wrong basket)
            LeanTween.moveLocalX(draggableCard.gameObject, droppedPos.x + shakeAmt, 0.05f)
                .setLoopPingPong(3)
                .setOnComplete(() => {
                    draggableCard.localPosition = droppedPos;
                    if (draggableCardBg != null)
                    {
                        draggableCardBg.color = Color.white;
                    }
                    
                    // Bounce back (glide) from wrong basket back to Vector3.zero
                    LeanTween.moveLocal(draggableCard.gameObject, Vector3.zero, 0.45f)
                        .setEase(LeanTweenType.easeOutBack)
                        .setOnComplete(() => {
                            _canDrag = true;
                        });
                });
        }
        else
        {
            if (draggableCardBg != null)
            {
                draggableCardBg.color = Color.white;
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
            var pop = starEffectObject.GetComponent<POPEffect_SeniorLev1A>();
            if (pop != null)
            {
                pop.enabled = false;
                pop.enabled = true;
            }
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Round Complete!";
        }

        StartCoroutine(AutoAdvanceRoundSequence());
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

        var currentRound = rounds[_currentRoundIndex];
        if (_currentWordIndex < currentRound.words.Count)
        {
            LoadWord();
        }
        else
        {
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
    }

    private void OnCompletedAll()
    {
        Debug.Log("[StartOrEnd] Completed all rounds!");
        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
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
            if (_flowManager != null)
            {
                _flowManager.NextGameplay();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = _score.ToString();
        }
    }

    private void ClearAllBadges()
    {
        foreach (var badge in _instantiatedBadges)
        {
            if (badge != null) Destroy(badge);
        }
        _instantiatedBadges.Clear();
    }



    private IEnumerator PlayAudioSequence(AudioClip clip, System.Action onComplete)
    {
        if (clip != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = clip;
            mascotAudioSource.Play();

            // Speak animation
            if (mascotCharacter != null)
            {
                LeanTween.cancel(mascotCharacter.gameObject);
                LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.05f, 0.25f)
                    .setLoopPingPong(Mathf.CeilToInt(clip.length / 0.5f));
            }

            yield return new WaitForSeconds(clip.length);

            if (mascotCharacter != null)
            {
                LeanTween.cancel(mascotCharacter.gameObject);
                LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
            }
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        onComplete?.Invoke();
    }
}
