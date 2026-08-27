using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class MagicETeachPair
{
    [Tooltip("The short vowel word (e.g., 'cap')")]
    public string shortWord;

    [Tooltip("The long vowel word (e.g., 'cape')")]
    public string longWord;

    [Tooltip("0-based index of the vowel letter in the short word (e.g. 1 for 'cap')")]
    public int vowelIndex = 1;

    [Tooltip("Audio clip for the short word (e.g., 'cap')")]
    public AudioClip shortWordAudio;

    [Tooltip("Audio clip for the long word (e.g., 'cape')")]
    public AudioClip longWordAudio;
}

public class TeachSilentMagicE_Unit9_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Word Pairs Configuration")]
    public List<MagicETeachPair> wordPairs = new List<MagicETeachPair>();

    [Header("UI References")]
    public TextMeshProUGUI titleTextLabel;
    public TextMeshProUGUI instructionLabel;
    public RectTransform wordContainer;
    public GameObject letterTileTemplate;
    public RectTransform targetSlot;
    public RectTransform flyingETile;
    public RectTransform mascotCharacter;
    
    [Header("Navigation Buttons")]
    public GameObject nextCardButton;
    public GameObject prevButton;
    public GameObject globalNextButton;

    [Header("Progress Bar Dotted")]
    public RectTransform progressDotsContainer;
    public GameObject progressDotPrefab;
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = new Color32(255, 255, 255, 60);
    public Color dotFilledColor = new Color32(76, 175, 80, 255);

    [Header("Audio Settings")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    public AudioClip introAudio;
    public AudioClip sfxFly;
    public AudioClip sfxFlip;
    public AudioClip sfxCheer;
    public AudioClip popSFX;
    public AudioClip transitionSFX;

    [Header("UI Colors")]
    public Color letterNormalColor = new Color32(93, 64, 55, 255); // Dark Brown
    public Color letterHighlightColor = new Color32(236, 64, 122, 255); // Pink/Red glow

    [Header("Animation Settings")]
    public float flyDuration = 1.0f;
    public float scaleDuration = 0.3f;
    public float startDelay = 0.3f;
    public float stepDelay = 0.5f;

    [Header("Completion Events")]
    public UnityEvent onTeachComplete;

    // Runtime state
    private int _currentIndex = 0;
    private bool _canTap = true;
    private Vector3 _originalMascotScale = Vector3.one;
    private TextMeshProUGUI _vowelMarkText;
    private TextMeshProUGUI _vowelLetterText;
    private Vector3 _flyingEStartPos;
    private Vector2 _flyingEStartAnchorMin;
    private Vector2 _flyingEStartAnchorMax;
    private Vector2 _flyingEStartPivot;
    private GameFlowManager_Senior_Phonics _flowManager;
    private Coroutine _teachFlowCoroutine;
    private List<GameObject> _dotInstances = new List<GameObject>();

    private void Awake()
    {
        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        // Setup button listeners
        if (nextCardButton != null)
        {
            Button btn = nextCardButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextCardClicked);
            }
        }

        if (prevButton != null)
        {
            Button btn = prevButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnPrevCardClicked);
            }
        }

        if (globalNextButton != null)
        {
            Button btn = globalNextButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnGlobalNextButtonClicked);
            }
        }

        if (wordPairs == null || wordPairs.Count == 0)
        {
            InitializeDefaultWordPairs();
        }
    }

    private void Start()
    {
        SetupProgressDots();

        if (flyingETile != null)
        {
            _flyingEStartPos = flyingETile.anchoredPosition;
            _flyingEStartAnchorMin = flyingETile.anchorMin;
            _flyingEStartAnchorMax = flyingETile.anchorMax;
            _flyingEStartPivot = flyingETile.pivot;
        }

        LoadPair(0);
    }

    private void InitializeDefaultWordPairs()
    {
        wordPairs = new List<MagicETeachPair>
        {
            new MagicETeachPair { shortWord = "cap", longWord = "cape", vowelIndex = 1 },
            new MagicETeachPair { shortWord = "tap", longWord = "tape", vowelIndex = 1 },
            new MagicETeachPair { shortWord = "pin", longWord = "pine", vowelIndex = 1 },
            new MagicETeachPair { shortWord = "kit", longWord = "kite", vowelIndex = 1 },
            new MagicETeachPair { shortWord = "hop", longWord = "hope", vowelIndex = 1 },
            new MagicETeachPair { shortWord = "cub", longWord = "cube", vowelIndex = 1 },
            new MagicETeachPair { shortWord = "man", longWord = "mane", vowelIndex = 1 },
            new MagicETeachPair { shortWord = "rid", longWord = "ride", vowelIndex = 1 },
            new MagicETeachPair { shortWord = "bit", longWord = "bite", vowelIndex = 1 },
            new MagicETeachPair { shortWord = "cut", longWord = "cute", vowelIndex = 1 }
        };
    }

    private void LoadPair(int index)
    {
        if (wordPairs == null || index < 0 || index >= wordPairs.Count) return;

        _currentIndex = index;
        _canTap = false;

        // Reset navigation buttons
        if (nextCardButton != null) nextCardButton.SetActive(false);
        if (prevButton != null) prevButton.SetActive(index > 0);
        if (globalNextButton != null) globalNextButton.SetActive(false);

        // Stop any running animations and audios
        if (_teachFlowCoroutine != null)
        {
            StopCoroutine(_teachFlowCoroutine);
        }
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            mascotCharacter.localScale = _originalMascotScale;
        }

        UpdateProgressDots();

        // Load visual layout
        InitializeVisualsForPair(wordPairs[index]);

        _teachFlowCoroutine = StartCoroutine(TeachFlowRoutine(index));
    }

    private void InitializeVisualsForPair(MagicETeachPair pair)
    {
        // Reset flying E first to remove it from wordContainer before clearing
        if (flyingETile != null)
        {
            LeanTween.cancel(flyingETile.gameObject);
            flyingETile.SetParent(wordContainer.parent); // Put back to GameArea
            
            // Restore anchors/pivot overridden by LayoutGroup
            flyingETile.anchorMin = _flyingEStartAnchorMin;
            flyingETile.anchorMax = _flyingEStartAnchorMax;
            flyingETile.pivot = _flyingEStartPivot;

            flyingETile.anchoredPosition = _flyingEStartPos;
            flyingETile.sizeDelta = new Vector2(140f, 160f); // Reset sizeDelta to design size
            flyingETile.localRotation = Quaternion.identity;
            flyingETile.localScale = Vector3.one;
            flyingETile.gameObject.SetActive(false);

            Image borderImg = flyingETile.GetComponent<Image>();
            if (borderImg != null)
            {
                borderImg.color = new Color32(236, 64, 122, 255); // pink outline
            }
            Transform feFace = flyingETile.Find("TileFace");
            if (feFace != null)
            {
                Image faceImg = feFace.GetComponent<Image>();
                if (faceImg != null)
                {
                    faceImg.color = new Color32(251, 245, 233, 255); // cream
                }
            }
        }

        // Clear word container children except template and slot
        foreach (Transform child in wordContainer)
        {
            if (child.gameObject != letterTileTemplate && child.gameObject != targetSlot.gameObject)
            {
                Destroy(child.gameObject);
            }
        }

        // Instantiate short word letters
        for (int i = 0; i < pair.shortWord.Length; i++)
        {
            GameObject tile = Instantiate(letterTileTemplate, wordContainer);
            tile.SetActive(true);
            tile.name = "Tile_" + pair.shortWord[i];

            TextMeshProUGUI txt = null;
            Transform faceTrans = tile.transform.Find("TileFace");
            if (faceTrans != null)
            {
                Transform letterTrans = faceTrans.Find("LetterText");
                if (letterTrans != null)
                {
                    txt = letterTrans.GetComponent<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.text = pair.shortWord[i].ToString();
                        txt.color = letterNormalColor;
                    }
                }

                // Setup Vowel Mark
                Transform markTrans = faceTrans.Find("VowelMarkText");
                if (markTrans != null)
                {
                    TextMeshProUGUI markText = markTrans.GetComponent<TextMeshProUGUI>();
                    if (markText != null)
                    {
                        if (i == pair.vowelIndex)
                        {
                            _vowelMarkText = markText;
                            _vowelLetterText = txt;
                            _vowelMarkText.text = "\u02D8"; // Breve ˘
                            _vowelMarkText.gameObject.SetActive(true);
                        }
                        else
                        {
                            markText.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        // Position targetSlot at the end
        if (targetSlot != null)
        {
            targetSlot.SetAsLastSibling();
            targetSlot.gameObject.SetActive(true);

            Image slotOutline = targetSlot.GetComponent<Image>();
            if (slotOutline != null)
            {
                slotOutline.color = new Color32(236, 64, 122, 100); // transparent pink
            }
            Transform slotFace = targetSlot.Find("TileFace");
            if (slotFace != null)
            {
                Image faceImg = slotFace.GetComponent<Image>();
                if (faceImg != null)
                {
                    faceImg.color = new Color32(251, 245, 233, 50); // transparent cream
                }
            }
        }
    }

    private IEnumerator TeachFlowRoutine(int index)
    {
        MagicETeachPair pair = wordPairs[index];
        yield return new WaitForSeconds(startDelay);

        // 1. Play intro audio (only on first word)
        if (index == 0 && introAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(introAudio);
            yield return StartCoroutine(MascotTalkAnimation(introAudio.length));
            yield return new WaitForSeconds(0.4f);
        }

        // 2. Play short word audio
        if (pair.shortWordAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(pair.shortWordAudio);
            yield return StartCoroutine(MascotTalkAnimation(pair.shortWordAudio.length));
            yield return new WaitForSeconds(stepDelay);
        }

        // 3. Sparkly E Flies!
        if (flyingETile != null && targetSlot != null)
        {
            flyingETile.gameObject.SetActive(true);
            flyingETile.anchoredPosition = _flyingEStartPos;
            flyingETile.localScale = Vector3.zero;

            LeanTween.scale(flyingETile.gameObject, Vector3.one, scaleDuration).setEase(LeanTweenType.easeOutBack);
            yield return new WaitForSeconds(scaleDuration);

            if (sfxAudioSource != null && sfxFly != null)
            {
                sfxAudioSource.PlayOneShot(sfxFly);
            }

            Vector3 targetWorldPos = targetSlot.position;
            Vector3 targetLocalPos = flyingETile.parent.InverseTransformPoint(targetWorldPos);

            LeanTween.moveLocal(flyingETile.gameObject, targetLocalPos, flyDuration).setEase(LeanTweenType.easeInOutQuad);
            LeanTween.rotateAround(flyingETile.gameObject, Vector3.forward, -360f, flyDuration);

            yield return new WaitForSeconds(flyDuration);

            // Landed!
            if (sfxAudioSource != null && sfxFlip != null)
            {
                sfxAudioSource.PlayOneShot(sfxFlip);
            }

            targetSlot.gameObject.SetActive(false);
            flyingETile.SetParent(wordContainer);
            flyingETile.SetAsLastSibling();

            Image borderImg = flyingETile.GetComponent<Image>();
            if (borderImg != null)
            {
                borderImg.color = letterNormalColor;
            }
            Transform faceTrans = flyingETile.Find("TileFace");
            if (faceTrans != null)
            {
                Image faceImg = faceTrans.GetComponent<Image>();
                if (faceImg != null)
                {
                    faceImg.color = new Color32(251, 245, 233, 255);
                }
            }

            // Flip vowel markbreve -> macron
            if (_vowelMarkText != null)
            {
                yield return StartCoroutine(AnimateVowelMarkFlipRoutine());
            }

            yield return new WaitForSeconds(0.4f);
        }

        // 4. Play long word audio
        if (pair.longWordAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(pair.longWordAudio);

            if (_vowelLetterText != null)
            {
                _vowelLetterText.color = letterHighlightColor;
            }

            yield return StartCoroutine(MascotTalkAnimation(pair.longWordAudio.length));
            yield return new WaitForSeconds(0.4f);
        }

        // Enable next navigation
        _canTap = true;
        ShowNextNavigation();
    }

    private IEnumerator AnimateVowelMarkFlipRoutine()
    {
        LeanTween.scaleY(_vowelMarkText.gameObject, 0f, 0.2f).setEase(LeanTweenType.easeInQuad);
        yield return new WaitForSeconds(0.2f);

        _vowelMarkText.text = "\u00AF"; // Macron ¯

        LeanTween.scaleY(_vowelMarkText.gameObject, 1.25f, 0.25f).setEase(LeanTweenType.easeOutBack);
        yield return new WaitForSeconds(0.25f);

        LeanTween.scaleY(_vowelMarkText.gameObject, 1f, 0.12f);
        yield return new WaitForSeconds(0.12f);
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

    private void ShowNextNavigation()
    {
        if (_currentIndex < wordPairs.Count - 1)
        {
            if (nextCardButton != null && !nextCardButton.activeSelf)
            {
                nextCardButton.SetActive(true);
                nextCardButton.transform.localScale = Vector3.zero;
                LeanTween.scale(nextCardButton, Vector3.one, 0.3f).setEase(LeanTweenType.easeOutBack);
                
                if (sfxAudioSource != null && popSFX != null)
                {
                    sfxAudioSource.PlayOneShot(popSFX);
                }
            }
        }
        else
        {
            if (globalNextButton != null && !globalNextButton.activeSelf)
            {
                globalNextButton.SetActive(true);
                globalNextButton.transform.localScale = Vector3.zero;
                LeanTween.scale(globalNextButton, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
                
                if (sfxAudioSource != null && sfxCheer != null)
                {
                    sfxAudioSource.PlayOneShot(sfxCheer);
                }
                if (unitCompleteAudio != null && mascotAudioSource != null)
                {
                    mascotAudioSource.PlayOneShot(unitCompleteAudio);
                }
            }

            if (nextCardButton != null)
            {
                nextCardButton.SetActive(false);
            }
        }
    }

    public void OnNextCardClicked()
    {
        if (!_canTap) return;

        if (sfxAudioSource != null && transitionSFX != null)
        {
            sfxAudioSource.PlayOneShot(transitionSFX);
        }

        LoadPair(_currentIndex + 1);
    }

    public void OnPrevCardClicked()
    {
        if (!_canTap) return;

        if (sfxAudioSource != null && transitionSFX != null)
        {
            sfxAudioSource.PlayOneShot(transitionSFX);
        }

        LoadPair(_currentIndex - 1);
    }

    public void OnGlobalNextButtonClicked()
    {
        if (onTeachComplete != null)
        {
            onTeachComplete.Invoke();
        }
        else if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
        else
        {
            Debug.LogWarning("No completion action set up and GameFlowManager not found.");
        }
    }

    // Progress Dots setup & rendering
    private void SetupProgressDots()
    {
        if (progressDotsContainer == null) return;

        // Clear existing
        foreach (Transform child in progressDotsContainer)
        {
            if (progressDotPrefab == null || child.gameObject != progressDotPrefab)
            {
                Destroy(child.gameObject);
            }
        }
        _dotInstances.Clear();

        // Create
        for (int i = 0; i < wordPairs.Count; i++)
        {
            GameObject dotObj;
            if (progressDotPrefab != null)
            {
                dotObj = Instantiate(progressDotPrefab, progressDotsContainer);
            }
            else
            {
                dotObj = new GameObject($"Dot_{i}", typeof(RectTransform), typeof(Image));
                dotObj.transform.SetParent(progressDotsContainer, false);
                dotObj.GetComponent<RectTransform>().sizeDelta = new Vector2(20f, 20f);
            }

            dotObj.SetActive(true);
            _dotInstances.Add(dotObj);
        }
    }

    private void UpdateProgressDots()
    {
        for (int i = 0; i < _dotInstances.Count; i++)
        {
            Image img = _dotInstances[i].GetComponent<Image>();
            if (img == null) img = _dotInstances[i].GetComponentInChildren<Image>();

            if (img != null)
            {
                bool isActive = (i <= _currentIndex);
                if (isActive)
                {
                    if (dotFilledSprite != null) img.sprite = dotFilledSprite;
                    img.color = dotFilledColor;
                    _dotInstances[i].transform.localScale = (i == _currentIndex) ? (Vector3.one * 1.3f) : Vector3.one;
                }
                else
                {
                    if (dotEmptySprite != null) img.sprite = dotEmptySprite;
                    img.color = dotEmptyColor;
                    _dotInstances[i].transform.localScale = Vector3.one;
                }
            }
        }
    }

    [ContextMenu("Auto Assign Refs")]
    public void AutoAssignAssetsAndWireUI()
    {
#if UNITY_EDITOR
        // Find Text Elements
        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in texts)
        {
            string n = txt.gameObject.name.ToLower();
            if (n.Contains("title")) titleTextLabel = txt;
            else if (n.Contains("instruction") || n.Contains("prompt")) instructionLabel = txt;
        }

        Transform container = transform.Find("GameArea/WordContainer");
        if (container != null) wordContainer = container.GetComponent<RectTransform>();

        Transform template = transform.Find("LetterTileTemplate");
        if (template != null) letterTileTemplate = template.gameObject;

        Transform slot = transform.Find("GameArea/WordContainer/TargetSlot");
        if (slot != null) targetSlot = slot.GetComponent<RectTransform>();

        Transform flying = transform.Find("GameArea/Flying_E");
        if (flying != null) flyingETile = flying.GetComponent<RectTransform>();

        // Navigation Buttons
        Transform nextC = transform.Find("NextCardButton");
        if (nextC != null) nextCardButton = nextC.gameObject;

        Transform prev = transform.Find("PrevButton");
        if (prev != null) prevButton = prev.gameObject;

        GameObject nextBtnObj = GameObject.Find("GlobalNextButton");
        if (nextBtnObj == null) nextBtnObj = GameObject.Find("NextButton");
        if (nextBtnObj != null) globalNextButton = nextBtnObj;

        // Mascot
        GameObject charObj = GameObject.Find("Character");
        if (charObj == null) charObj = GameObject.Find("MascotCharacter");
        if (charObj != null) mascotCharacter = charObj.GetComponent<RectTransform>();

        // Progress Dots
        Transform dots = transform.Find("ProgressDotsContainer");
        if (dots != null)
        {
            progressDotsContainer = dots.GetComponent<RectTransform>();
            Transform dotTemp = dots.Find("ProgressDotTemplate");
            if (dotTemp != null) progressDotPrefab = dotTemp.gameObject;
        }

        Sprite circleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phonics/Assets/Circle.png");
        if (circleSprite != null)
        {
            dotEmptySprite = circleSprite;
            dotFilledSprite = circleSprite;
        }

        // Audios
        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        if (sources.Length > 0) mascotAudioSource = sources[0];
        if (sources.Length > 1) sfxAudioSource = sources[1];

        // Resolve Clips
        sfxFly = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/PopUpSound.mp3");
        sfxFlip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Correct Answer.mp3");
        sfxCheer = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        popSFX = sfxFly;
        transitionSFX = sfxFlip;

        // Wire assets for all pairs
        if (wordPairs == null || wordPairs.Count == 0)
        {
            InitializeDefaultWordPairs();
        }

        foreach (var pair in wordPairs)
        {
            pair.shortWordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/Unit 2 Phonics/Add'e' Magic/Words/" + pair.shortWord + ".mp3");
            if (pair.shortWordAudio == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets(pair.shortWord + " t:AudioClip");
                if (guids.Length > 0) pair.shortWordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            pair.longWordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/Unit 2 Phonics/Add'e' Magic/Words/" + pair.longWord + ".mp3");
            if (pair.longWordAudio == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets(pair.longWord + " t:AudioClip");
                if (guids.Length > 0) pair.longWordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
