using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

[System.Serializable]
public class QuietLettersWordData
{
    [Tooltip("The word text, e.g., 'knee'")]
    public string word;

    [Tooltip("0-based index of silent letter(s) (e.g. [0] for 'k' in 'knee')")]
    public List<int> silentIndices = new List<int>();

    [Tooltip("The letter pair category, e.g., 'kn'")]
    public string letterPair;

    [Tooltip("Audio clip for this word pronunciation")]
    public AudioClip wordAudio;

    [Tooltip("Custom explanation sentence, e.g., 'In knee, the k is silent.' If left empty, it auto-generates.")]
    public string explanationOverride;
}

public class TeachQuietLetters_Unit10_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Teach Level Configuration")]
    public List<QuietLettersWordData> words = new List<QuietLettersWordData>();

    [Header("UI References")]
    public TextMeshProUGUI titleTextLabel;
    public TextMeshProUGUI instructionLabel;
    public RectTransform wordContainer;
    public GameObject letterTileTemplate;
    public RectTransform mascotCharacter;

    [Header("Friendly Letter-Pair Character Card")]
    public GameObject characterCardPanel;
    public TextMeshProUGUI characterCardText;
    public Image characterCardSpriteImage; // Optional image reference if customized
    public TextMeshProUGUI explanationLabel;

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
    public AudioClip cheerSFX;
    public AudioClip popSFX;
    public AudioClip transitionSFX;

    [Header("UI Colors")]
    public Color letterNormalColor = new Color32(93, 64, 55, 255); // Dark Brown
    public Color letterSilentColor = new Color32(93, 64, 55, 60);  // Faded Grey/Brown
    public Color tileNormalBorderColor = new Color32(93, 64, 55, 255); // Dark Brown
    public Color tileSilentBorderColor = new Color32(180, 180, 180, 100); // Faded Border
    public Color tileNormalBgColor = new Color32(251, 245, 233, 255); // Cream
    public Color tileSilentBgColor = new Color32(251, 245, 233, 100); // Faded Cream

    [Header("Animation Settings")]
    public float startDelay = 0.3f;
    public float stepDelay = 0.5f;

    [Header("Completion Events")]
    public UnityEvent onTeachComplete;

    // Runtime state
    private int _currentIndex = 0;
    private bool _canTap = true;
    private Vector3 _originalMascotScale = Vector3.one;
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

        // Cache default references if unassigned
        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        if (mascotAudioSource == null && sources.Length > 0) mascotAudioSource = sources[0];
        if (sfxAudioSource == null && sources.Length > 1) sfxAudioSource = sources[1];
        if (sfxAudioSource == null) sfxAudioSource = mascotAudioSource;

        if (words == null || words.Count == 0)
        {
            InitializeDefaultWords();
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

        if (characterCardPanel != null)
        {
            Button btn = characterCardPanel.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(PlayCurrentWordAudio);
            }
        }
    }

    private void Start()
    {
        SetupProgressDots();
        LoadWord(0);
    }

    public void InitializeDefaultWords()
    {
        words = new List<QuietLettersWordData>();

        // gn
        AddWord("gnome", new List<int> { 0 }, "gn");
        AddWord("sign", new List<int> { 2 }, "gn");
        AddWord("gnaw", new List<int> { 0 }, "gn");

        // kn
        AddWord("knee", new List<int> { 0 }, "kn");
        AddWord("knife", new List<int> { 0 }, "kn");
        AddWord("knock", new List<int> { 0 }, "kn");

        // lk
        AddWord("talk", new List<int> { 2 }, "lk");
        AddWord("walk", new List<int> { 2 }, "lk");
        AddWord("chalk", new List<int> { 3 }, "lk");

        // mb
        AddWord("comb", new List<int> { 3 }, "mb");
        AddWord("climb", new List<int> { 4 }, "mb");
        AddWord("thumb", new List<int> { 4 }, "mb");
    }

    private void AddWord(string w, List<int> indices, string pair)
    {
        words.Add(new QuietLettersWordData
        {
            word = w,
            silentIndices = indices,
            letterPair = pair
        });
    }

    private void LoadWord(int index)
    {
        if (words == null || index < 0 || index >= words.Count) return;

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
        InitializeVisualsForWord(words[index]);

        _teachFlowCoroutine = StartCoroutine(TeachFlowRoutine(index));
    }

    private void InitializeVisualsForWord(QuietLettersWordData wordData)
    {
        // Clear word container children except template
        foreach (Transform child in wordContainer)
        {
            if (child.gameObject != letterTileTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        // Instantiate word letter tiles
        for (int i = 0; i < wordData.word.Length; i++)
        {
            GameObject tile = Instantiate(letterTileTemplate, wordContainer);
            tile.SetActive(true);
            tile.name = "Tile_" + wordData.word[i] + "_" + i;

            bool isSilent = wordData.silentIndices.Contains(i);

            // Configure text
            TextMeshProUGUI txt = null;
            Transform faceTrans = tile.transform.Find("TileFace");
            if (faceTrans != null)
            {
                // Background color setup
                Image faceImg = faceTrans.GetComponent<Image>();
                if (faceImg != null)
                {
                    faceImg.color = isSilent ? tileSilentBgColor : tileNormalBgColor;
                }

                Transform letterTrans = faceTrans.Find("LetterText");
                if (letterTrans != null)
                {
                    txt = letterTrans.GetComponent<TextMeshProUGUI>();
                    if (txt != null)
                    {
                        txt.text = wordData.word[i].ToString();
                        txt.color = isSilent ? letterSilentColor : letterNormalColor;
                    }
                }
            }

            // Outline border color setup
            Image borderImg = tile.GetComponent<Image>();
            if (borderImg != null)
            {
                borderImg.color = isSilent ? tileSilentBorderColor : tileNormalBorderColor;
            }
        }

        // Set up Letter-Pair Character Card
        if (characterCardPanel != null)
        {
            characterCardPanel.SetActive(true);
        }

        if (characterCardText != null)
        {
            characterCardText.text = wordData.letterPair.ToLower();
        }

        if (explanationLabel != null)
        {
            if (!string.IsNullOrEmpty(wordData.explanationOverride))
            {
                explanationLabel.text = wordData.explanationOverride;
            }
            else
            {
                // Auto-generate clean explanation, e.g. "In knee, the k is silent."
                string silentLetters = "";
                foreach (int idx in wordData.silentIndices)
                {
                    if (idx >= 0 && idx < wordData.word.Length)
                    {
                        silentLetters += wordData.word[idx];
                    }
                }
                if (!string.IsNullOrEmpty(silentLetters))
                {
                    explanationLabel.text = $"In <color=#E91E63>{wordData.word}</color>, the <color=#FF5722>{silentLetters}</color> is silent.";
                }
                else
                {
                    explanationLabel.text = "";
                }
            }
        }
    }

    private IEnumerator TeachFlowRoutine(int index)
    {
        QuietLettersWordData wordData = words[index];
        yield return new WaitForSeconds(startDelay);

        // 1. Play intro audio (only on first card)
        if (index == 0 && introAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(introAudio);
            yield return StartCoroutine(MascotTalkAnimation(introAudio.length));
            yield return new WaitForSeconds(0.4f);
        }

        // 2. Play word audio
        if (wordData.wordAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(wordData.wordAudio);
            yield return StartCoroutine(MascotTalkAnimation(wordData.wordAudio.length));
            yield return new WaitForSeconds(stepDelay);
        }

        // Enable next navigation
        _canTap = true;
        ShowNextNavigation();
    }

    private IEnumerator MascotTalkAnimation(float duration)
    {
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            // Slight squash/stretch pingpong to mimic talking
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
        if (_currentIndex < words.Count - 1)
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
                
                if (sfxAudioSource != null && cheerSFX != null)
                {
                    sfxAudioSource.PlayOneShot(cheerSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
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

        LoadWord(_currentIndex + 1);
    }

    public void OnPrevCardClicked()
    {
        if (!_canTap) return;

        if (sfxAudioSource != null && transitionSFX != null)
        {
            sfxAudioSource.PlayOneShot(transitionSFX);
        }

        LoadWord(_currentIndex - 1);
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

    public void PlayCurrentWordAudio()
    {
        if (words == null || _currentIndex < 0 || _currentIndex >= words.Count) return;
        QuietLettersWordData wordData = words[_currentIndex];

        if (wordData.wordAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(wordData.wordAudio);
            StartCoroutine(MascotTalkAnimation(wordData.wordAudio.length));
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
        for (int i = 0; i < words.Count; i++)
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
            else if (n.Contains("explanation") || n.Contains("caption")) explanationLabel = txt;
            else if (n.Contains("cardtext") || n.Contains("pairtext")) characterCardText = txt;
        }

        // Layout Containers
        Transform container = transform.Find("GameArea/WordContainer");
        if (container != null) wordContainer = container.GetComponent<RectTransform>();

        Transform cardPanel = transform.Find("GameArea/CharacterCardPanel");
        if (cardPanel != null)
        {
            characterCardPanel = cardPanel.gameObject;
            characterCardSpriteImage = cardPanel.Find("SpriteImage")?.GetComponent<Image>();
        }

        Transform template = transform.Find("LetterTileTemplate");
        if (template != null) letterTileTemplate = template.gameObject;

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

        // Resolve SFX Clips
        popSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/PopUpSound.mp3");
        transitionSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Correct Answer.mp3");
        cheerSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");

        // Wire assets for all words
        if (words == null || words.Count == 0)
        {
            InitializeDefaultWords();
        }

        foreach (var wordData in words)
        {
            // Search for word audio clip in Assets/Phonics/Audio
            string[] guids = UnityEditor.AssetDatabase.FindAssets(wordData.word + " t:AudioClip");
            if (guids != null && guids.Length > 0)
            {
                foreach (var guid in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("Unit 10 Phonics") || path.Contains("Silent") || path.Contains("Words"))
                    {
                        wordData.wordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        break;
                    }
                }
                if (wordData.wordAudio == null)
                {
                    wordData.wordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
        }

        // Search for intro audio
        string[] introGuids = UnityEditor.AssetDatabase.FindAssets("QuietLettersIntro t:AudioClip");
        if (introGuids == null || introGuids.Length == 0)
        {
            introGuids = UnityEditor.AssetDatabase.FindAssets("silent_intro t:AudioClip");
        }
        if (introGuids != null && introGuids.Length > 0)
        {
            introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(introGuids[0]));
        }

        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
