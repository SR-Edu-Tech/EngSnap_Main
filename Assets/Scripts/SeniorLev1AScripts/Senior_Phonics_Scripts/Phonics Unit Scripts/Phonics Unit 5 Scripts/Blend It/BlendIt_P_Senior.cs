using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.IO;

[System.Serializable]
public class BlendWordData
{
    [Tooltip("The whole word (e.g., 'chill')")]
    public string wholeWord;

    [Tooltip("The segment parts/letters (e.g., ['ch', 'i', 'll'])")]
    public List<string> wordParts = new List<string>();

    [Tooltip("The consonant digraph group this word belongs to (e.g. 'ch', 'sh', 'th')")]
    public string digraphGroup;

    [Tooltip("Audio clip for the whole word blend sound (e.g., saying 'chill')")]
    public AudioClip wholeWordAudio;

    [Tooltip("Audio clips for each segment part (must match wordParts count)")]
    public List<AudioClip> partAudios = new List<AudioClip>();

    [Tooltip("Optional image/sprite for the word (if available)")]
    public Sprite wordSprite;
}

public class BlendIt_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Word Configurations")]
    public List<BlendWordData> wordsList = new List<BlendWordData>();

    [Header("UI Component References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI groupTitleText; // e.g., "Consonant digraphs with 'ch'"
    public TextMeshProUGUI instructionLabel;
    public RectTransform mascotCharacter;
    
    [Tooltip("Word boxes container where letter cards are spawned")]
    public RectTransform wordBoxesContainer;
    
    [Tooltip("Prefab/Template for spawning letter boxes")]
    public GameObject letterBoxPrefab;
    
    [Tooltip("Prefab/Template for spawning arrows between boxes")]
    public GameObject arrowPrefab;

    [Tooltip("Prefab/Template for spawning the final complete word button")]
    public GameObject completeWordButtonPrefab;

    [Header("Digraph Badge Indicator")]
    public RectTransform activeDigraphIndicatorCard;
    public TextMeshProUGUI activeDigraphIndicatorText;

    [Header("Navigation Buttons")]
    public GameObject nextButton;
    public GameObject replayButton;
    public GameObject globalNextButton;

    [Header("Group Progress Setup (e.g. 7 Digraph Groups)")]
    public RectTransform groupProgressDotsContainer;
    public GameObject groupProgressDotPrefab;
    public Sprite groupDotEmptySprite;
    public Sprite groupDotFilledSprite;
    public Color groupDotEmptyColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    public Color groupDotFilledColor = new Color32(76, 175, 80, 255); // green

    [Header("Word Progress Setup (Words in current active group)")]
    public RectTransform wordProgressDotsContainer;
    public GameObject wordProgressDotPrefab;
    public Sprite wordDotEmptySprite;
    public Sprite wordDotFilledSprite;
    public Color wordDotEmptyColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    public Color wordDotFilledColor = new Color32(33, 150, 243, 255); // blue
    public TextMeshProUGUI progressLabel;

    [Header("Card Colors")]
    public Color boxNormalColor = Color.white;
    public Color boxTappedColor = new Color32(200, 230, 201, 255); // light green
    public Color digraphHighlightColor = new Color32(236, 64, 120, 255); // Pink/Red for digraphs

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip introScreenAudio;
    public AudioClip popSFX;
    public AudioClip transitionSFX;
    public AudioClip cheerSFX;

    [Header("Animation Settings")]
    public float scaleHighlightMult = 1.15f;
    public float animationSpeed = 4f;

    [Header("Events & Transitions")]
    public UnityEvent onActivityComplete;

    // Runtime state
    private int _currentIndex = 0;
    private int _currentPartIndex = 0;
    private bool _canTap = false;
    private Coroutine _audioSeqCoroutine;
    private Vector3 _originalMascotScale = Vector3.one;

    private List<string> _uniqueGroups = new List<string>();
    private List<GameObject> _groupDotInstances = new List<GameObject>();
    private List<GameObject> _wordDotInstances = new List<GameObject>();
    private List<GameObject> _activeBoxes = new List<GameObject>();
    private List<GameObject> _activeArrows = new List<GameObject>();
    
    private GameObject _activeCompleteWordArrow;
    private GameObject _activeCompleteWordButton;
    private string _lastDigraphGroup = "";

    private GameFlowManager_Senior_Phonics _flowManager;

    private void Awake()
    {
        // 1. Auto-bind UI references if they are null in the Inspector
        AutoBindUI();

        // 2. Cache mascot scale
        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Dynamically find global next button under the unit panel if not set
        if (globalNextButton == null)
        {
            Transform unitParent = transform.parent != null ? transform.parent.parent : null;
            if (unitParent != null)
            {
                Transform nextBtnTrans = unitParent.Find("NextButton");
                if (nextBtnTrans != null)
                {
                    globalNextButton = nextBtnTrans.gameObject;
                }
            }
        }

        // 3. Populate default 38 words if wordsList is empty
        if (wordsList == null || wordsList.Count == 0)
        {
            PopulateDefaultWords();
        }

        InitializeGroups();
    }

    private void Start()
    {
        // Hook button listeners
        if (nextButton != null)
        {
            Button btn = nextButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextClicked);
            }
        }

        if (replayButton != null)
        {
            Button btn = replayButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnReplayClicked);
            }
        }

        ResetUI();
    }

    private void OnEnable()
    {
        ResetUI();
        StartCoroutine(IntroFlow());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        _audioSeqCoroutine = null;
    }

    private void AutoBindUI()
    {
        if (titleText == null) titleText = GetChildComponent<TextMeshProUGUI>("TitleText");
        if (groupTitleText == null) groupTitleText = GetChildComponent<TextMeshProUGUI>("GroupTitleText");
        if (instructionLabel == null) instructionLabel = GetChildComponent<TextMeshProUGUI>("InstructionLabel");
        
        if (wordBoxesContainer == null)
        {
            Transform t = transform.Find("WordBoxesContainer");
            if (t != null) wordBoxesContainer = t.GetComponent<RectTransform>();
        }

        if (wordBoxesContainer != null)
        {
            if (letterBoxPrefab == null)
            {
                Transform t = wordBoxesContainer.Find("LetterBoxTemplate");
                if (t != null) letterBoxPrefab = t.gameObject;
            }
            if (arrowPrefab == null)
            {
                Transform t = wordBoxesContainer.Find("ArrowTemplate");
                if (t != null) arrowPrefab = t.gameObject;
            }
        }

        if (completeWordButtonPrefab == null)
        {
            Transform t = transform.Find("CompleteWordButtonTemplate");
            if (t != null) completeWordButtonPrefab = t.gameObject;
        }

        if (nextButton == null)
        {
            Transform t = transform.Find("NextWordButton");
            if (t != null) nextButton = t.gameObject;
        }

        if (replayButton == null)
        {
            Transform t = transform.Find("ReplayButton");
            if (t != null) replayButton = t.gameObject;
        }

        if (groupProgressDotsContainer == null)
        {
            Transform t = transform.Find("GroupProgressDotsContainer");
            if (t != null) groupProgressDotsContainer = t.GetComponent<RectTransform>();
        }

        if (groupProgressDotsContainer != null && groupProgressDotPrefab == null)
        {
            Transform t = groupProgressDotsContainer.Find("GroupProgressDotTemplate");
            if (t != null) groupProgressDotPrefab = t.gameObject;
        }

        if (wordProgressDotsContainer == null)
        {
            Transform t = transform.Find("WordProgressDotsContainer");
            if (t != null) wordProgressDotsContainer = t.GetComponent<RectTransform>();
        }

        if (wordProgressDotsContainer != null && wordProgressDotPrefab == null)
        {
            Transform t = wordProgressDotsContainer.Find("WordProgressDotTemplate");
            if (t != null) wordProgressDotPrefab = t.gameObject;
        }

        if (progressLabel == null) progressLabel = GetChildComponent<TextMeshProUGUI>("ProgressLabel");

        if (activeDigraphIndicatorCard == null)
        {
            Transform t = transform.Find("ActiveDigraphIndicatorCard");
            if (t != null) activeDigraphIndicatorCard = t.GetComponent<RectTransform>();
        }

        if (activeDigraphIndicatorCard != null && activeDigraphIndicatorText == null)
        {
            Transform t = activeDigraphIndicatorCard.Find("Text");
            if (t != null) activeDigraphIndicatorText = t.GetComponent<TextMeshProUGUI>();
        }
    }

    private T GetChildComponent<T>(string childName) where T : Component
    {
        Transform t = transform.Find(childName);
        return t != null ? t.GetComponent<T>() : null;
    }

    private void PopulateDefaultWords()
    {
        wordsList = new List<BlendWordData>();

        var configs = new[] {
            // ch
            new { word = "chill", parts = new[] { "ch", "i", "ll" }, grp = "ch" },
            new { word = "chimp", parts = new[] { "ch", "i", "m", "p" }, grp = "ch" },
            new { word = "chips", parts = new[] { "ch", "i", "p", "s" }, grp = "ch" },
            new { word = "chess", parts = new[] { "ch", "e", "ss" }, grp = "ch" },
            new { word = "check", parts = new[] { "ch", "e", "ck" }, grp = "ch" },
            // sh
            new { word = "shop", parts = new[] { "sh", "o", "p" }, grp = "sh" },
            new { word = "shot", parts = new[] { "sh", "o", "t" }, grp = "sh" },
            new { word = "shell", parts = new[] { "sh", "e", "ll" }, grp = "sh" },
            new { word = "cash", parts = new[] { "c", "a", "sh" }, grp = "sh" },
            new { word = "mash", parts = new[] { "m", "a", "sh" }, grp = "sh" },
            // th
            new { word = "thumb", parts = new[] { "th", "u", "mb" }, grp = "th" },
            new { word = "bath", parts = new[] { "b", "a", "th" }, grp = "th" },
            new { word = "think", parts = new[] { "th", "i", "n", "k" }, grp = "th" },
            new { word = "thorn", parts = new[] { "th", "o", "r", "n" }, grp = "th" },
            new { word = "this", parts = new[] { "th", "i", "s" }, grp = "th" },
            new { word = "that", parts = new[] { "th", "a", "t" }, grp = "th" },
            new { word = "them", parts = new[] { "th", "e", "m" }, grp = "th" },
            new { word = "then", parts = new[] { "th", "e", "n" }, grp = "th" },
            // wh
            new { word = "which", parts = new[] { "wh", "i", "ch" }, grp = "wh" },
            new { word = "while", parts = new[] { "wh", "i", "l", "e" }, grp = "wh" },
            new { word = "where", parts = new[] { "wh", "e", "r", "e" }, grp = "wh" },
            new { word = "when", parts = new[] { "wh", "e", "n" }, grp = "wh" },
            new { word = "what", parts = new[] { "wh", "a", "t" }, grp = "wh" },
            // ck
            new { word = "back", parts = new[] { "b", "a", "ck" }, grp = "ck" },
            new { word = "sack", parts = new[] { "s", "a", "ck" }, grp = "ck" },
            new { word = "neck", parts = new[] { "n", "e", "ck" }, grp = "ck" },
            new { word = "duck", parts = new[] { "d", "u", "ck" }, grp = "ck" },
            new { word = "luck", parts = new[] { "l", "u", "ck" }, grp = "ck" },
            // nk
            new { word = "wink", parts = new[] { "w", "i", "nk" }, grp = "nk" },
            new { word = "think", parts = new[] { "th", "i", "nk" }, grp = "nk" },
            new { word = "drink", parts = new[] { "d", "r", "i", "nk" }, grp = "nk" },
            new { word = "bank", parts = new[] { "b", "a", "nk" }, grp = "nk" },
            new { word = "thank", parts = new[] { "th", "a", "nk" }, grp = "nk" },
            // ng
            new { word = "wing", parts = new[] { "w", "i", "ng" }, grp = "ng" },
            new { word = "bring", parts = new[] { "b", "r", "i", "ng" }, grp = "ng" },
            new { word = "long", parts = new[] { "l", "o", "ng" }, grp = "ng" },
            new { word = "hung", parts = new[] { "h", "u", "ng" }, grp = "ng" },
            new { word = "king", parts = new[] { "k", "i", "ng" }, grp = "ng" }
        };

        foreach (var c in configs)
        {
            BlendWordData data = new BlendWordData();
            data.wholeWord = c.word;
            data.digraphGroup = c.grp;
            foreach (var p in c.parts)
            {
                data.wordParts.Add(p);
            }
            wordsList.Add(data);
        }
    }

    private void InitializeGroups()
    {
        _uniqueGroups.Clear();
        foreach (var word in wordsList)
        {
            if (!string.IsNullOrEmpty(word.digraphGroup) && !_uniqueGroups.Contains(word.digraphGroup))
            {
                _uniqueGroups.Add(word.digraphGroup);
            }
        }
    }

    private void GetWordGroupInfo(int wordIndex, out int groupIndex, out int wordInGroupIndex, out int totalWordsInGroup)
    {
        groupIndex = -1;
        wordInGroupIndex = -1;
        totalWordsInGroup = 0;
        
        if (wordIndex < 0 || wordIndex >= wordsList.Count) return;
        
        BlendWordData targetWord = wordsList[wordIndex];
        string grp = targetWord.digraphGroup;
        
        groupIndex = _uniqueGroups.IndexOf(grp);
        
        int count = 0;
        for (int i = 0; i < wordsList.Count; i++)
        {
            if (wordsList[i].digraphGroup == grp)
            {
                if (i == wordIndex)
                {
                    wordInGroupIndex = count;
                }
                count++;
            }
        }
        totalWordsInGroup = count;
    }

    private void ResetUI()
    {
        if (mascotCharacter != null) mascotCharacter.localScale = Vector3.zero;
        if (nextButton != null) nextButton.SetActive(false);
        if (globalNextButton != null) globalNextButton.SetActive(false);

        _currentIndex = 0;
        _canTap = false;
        _lastDigraphGroup = "";

        InitializeGroups();
        SetupGroupProgressDots();
    }

    private IEnumerator IntroFlow()
    {
        // 1. Play general screen intro audio if configured
        if (audioSource != null && introScreenAudio != null)
        {
            audioSource.clip = introScreenAudio;
            audioSource.Play();
        }

        // 2. Pop in the Mascot character
        if (mascotCharacter != null)
        {
            yield return StartCoroutine(PopUI(mascotCharacter));
        }

        if (audioSource != null && introScreenAudio != null)
        {
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Load the first word
        LoadWord(_currentIndex);
    }

    private void LoadWord(int index)
    {
        if (_audioSeqCoroutine != null) StopCoroutine(_audioSeqCoroutine);
        
        _currentIndex = index;
        _currentPartIndex = 0;
        _canTap = false;

        if (wordsList == null || wordsList.Count == 0)
        {
            Debug.LogWarning("[BlendIt] No words configured!");
            return;
        }

        if (index < 0 || index >= wordsList.Count)
        {
            OnActivityCompleted();
            return;
        }

        BlendWordData wordData = wordsList[index];
        StartCoroutine(LoadWordSequence(wordData));
    }

    private IEnumerator LoadWordSequence(BlendWordData wordData)
    {
        // Dynamic loading of audio clips and sprites at runtime if they are null
        yield return StartCoroutine(LoadWordAssetsIfNeeded(wordData));

        // 1. Check if digraph group changed to trigger indicator badge scale pulse
        bool groupChanged = (wordData.digraphGroup != _lastDigraphGroup);
        _lastDigraphGroup = wordData.digraphGroup;

        // Update active digraph badge indicator
        if (activeDigraphIndicatorText != null)
        {
            activeDigraphIndicatorText.text = wordData.digraphGroup;
        }

        if (activeDigraphIndicatorCard != null && groupChanged)
        {
            activeDigraphIndicatorCard.localScale = Vector3.zero;
            LeanTween.cancel(activeDigraphIndicatorCard.gameObject);
            LeanTween.scale(activeDigraphIndicatorCard.gameObject, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack);
        }

        // 2. Setup progress dot containers
        int groupIdx, wordInGroupIdx, totalWordsInGrp;
        GetWordGroupInfo(_currentIndex, out groupIdx, out wordInGroupIdx, out totalWordsInGrp);

        UpdateGroupProgressDots(groupIdx);
        SetupWordProgressDots(totalWordsInGrp);
        UpdateWordProgressDots(wordInGroupIdx);
        UpdateProgressLabel(wordInGroupIdx, totalWordsInGrp, wordData.digraphGroup);

        // Update group header title text
        if (groupTitleText != null)
        {
            if (!string.IsNullOrEmpty(wordData.digraphGroup))
            {
                groupTitleText.text = $"Consonant digraphs with '{wordData.digraphGroup}'";
            }
            else
            {
                groupTitleText.text = "Consonant Digraphs";
            }
        }

        if (nextButton != null) nextButton.SetActive(false);

        // Clear previous word items
        foreach (var box in _activeBoxes)
        {
            if (box != null) Destroy(box);
        }
        _activeBoxes.Clear();

        foreach (var arrow in _activeArrows)
        {
            if (arrow != null) Destroy(arrow);
        }
        _activeArrows.Clear();

        if (_activeCompleteWordArrow != null)
        {
            Destroy(_activeCompleteWordArrow);
            _activeCompleteWordArrow = null;
        }

        if (_activeCompleteWordButton != null)
        {
            Destroy(_activeCompleteWordButton);
            _activeCompleteWordButton = null;
        }

        // Spawning letter boxes and arrows
        if (wordBoxesContainer != null && letterBoxPrefab != null)
        {
            int numParts = wordData.wordParts.Count;
            for (int i = 0; i < numParts; i++)
            {
                GameObject boxObj = Instantiate(letterBoxPrefab, wordBoxesContainer);
                boxObj.name = $"LetterBox_{i}";
                boxObj.SetActive(true);

                Image img = boxObj.GetComponent<Image>();
                if (img != null) img.color = boxNormalColor;
                
                TextMeshProUGUI txt = boxObj.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null)
                {
                    string partText = wordData.wordParts[i];
                    if (partText.ToLower() == wordData.digraphGroup.ToLower())
                    {
                        txt.text = partText;
                        txt.color = digraphHighlightColor;
                    }
                    else
                    {
                        txt.text = partText;
                        txt.color = new Color32(40, 44, 76, 255);
                    }
                }

                Button btn = boxObj.GetComponent<Button>();
                if (btn == null) btn = boxObj.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    int partIndex = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnPartClicked(partIndex));
                }

                boxObj.transform.localScale = Vector3.zero;
                StartCoroutine(PopUI(boxObj.GetComponent<RectTransform>()));

                _activeBoxes.Add(boxObj);

                if (i < numParts - 1 && arrowPrefab != null)
                {
                    GameObject arrowObj = Instantiate(arrowPrefab, wordBoxesContainer);
                    arrowObj.name = $"Arrow_{i}";
                    arrowObj.SetActive(true);
                    
                    arrowObj.transform.localScale = Vector3.zero;
                    StartCoroutine(PopUI(arrowObj.GetComponent<RectTransform>()));
                    
                    _activeArrows.Add(arrowObj);
                }
            }

            if (arrowPrefab != null && completeWordButtonPrefab != null)
            {
                _activeCompleteWordArrow = Instantiate(arrowPrefab, wordBoxesContainer);
                _activeCompleteWordArrow.name = "FinalCompleteArrow";
                _activeCompleteWordArrow.SetActive(false);

                _activeCompleteWordButton = Instantiate(completeWordButtonPrefab, wordBoxesContainer);
                _activeCompleteWordButton.name = "CompleteWordButton";
                
                TextMeshProUGUI btnTxt = _activeCompleteWordButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnTxt != null)
                {
                    btnTxt.text = GetColoredWord(wordData.wholeWord, wordData.digraphGroup);
                }

                Button btn = _activeCompleteWordButton.GetComponent<Button>();
                if (btn == null) btn = _activeCompleteWordButton.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(OnCompleteWordButtonClicked);
                }

                _activeCompleteWordButton.SetActive(false);
            }
        }

        if (audioSource != null && popSFX != null)
        {
            audioSource.PlayOneShot(popSFX);
        }

        _canTap = true;
    }

    private IEnumerator LoadWordAssetsIfNeeded(BlendWordData wordData)
    {
        // Try to load parts audio and whole word audio from disk at runtime if null
        string basePath = Application.dataPath + "/Phonics/Audio/Unit 5 Phonics/Blend it/";
        
        if (wordData.partAudios == null || wordData.partAudios.Count == 0)
        {
            wordData.partAudios = new List<AudioClip>();
            foreach (var p in wordData.wordParts)
            {
                AudioClip clip = null;
                // Try wav first
                string wavPath = "file://" + basePath + p + ".wav";
                yield return StartCoroutine(LoadAudioClipFromUrl(wavPath, AudioType.WAV, (loadedClip) => clip = loadedClip));
                
                if (clip == null)
                {
                    // Try mp3 fallback
                    string mp3Path = "file://" + basePath + p + ".mp3";
                    yield return StartCoroutine(LoadAudioClipFromUrl(mp3Path, AudioType.MPEG, (loadedClip) => clip = loadedClip));
                }
                
                wordData.partAudios.Add(clip);
            }
        }

        if (wordData.wholeWordAudio == null)
        {
            AudioClip wholeClip = null;
            string wMp3Path = "file://" + basePath + wordData.wholeWord + ".mp3";
            yield return StartCoroutine(LoadAudioClipFromUrl(wMp3Path, AudioType.MPEG, (loadedClip) => wholeClip = loadedClip));
            wordData.wholeWordAudio = wholeClip;
        }

        // Try load sprite dynamically
        if (wordData.wordSprite == null)
        {
            string spritePath = Application.dataPath + "/Phonics/Sprites/Unit 5/" + wordData.wholeWord + ".png";
            if (File.Exists(spritePath))
            {
                byte[] bytes = File.ReadAllBytes(spritePath);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    wordData.wordSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
        }
    }

    private IEnumerator LoadAudioClipFromUrl(string url, AudioType type, System.Action<AudioClip> callback)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, type))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                callback?.Invoke(clip);
            }
            else
            {
                callback?.Invoke(null);
            }
        }
    }

    private void OnPartClicked(int partIndex)
    {
        if (!_canTap) return;

        BlendWordData wordData = wordsList[_currentIndex];

        if (partIndex == _currentPartIndex)
        {
            _canTap = false;
            
            AudioClip clip = null;
            if (wordData.partAudios != null && partIndex < wordData.partAudios.Count)
            {
                clip = wordData.partAudios[partIndex];
            }

            float wiggleDuration = 0.5f;
            if (clip != null && audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                wiggleDuration = clip.length;
            }

            GameObject currentBox = _activeBoxes[partIndex];
            
            Image img = currentBox.GetComponent<Image>();
            if (img != null) img.color = boxTappedColor;

            StartCoroutine(WiggleAnimation(currentBox.transform, Vector3.one, Quaternion.identity, wiggleDuration));
            StartCoroutine(MascotTalkAnimation(wiggleDuration));

            StartCoroutine(AdvanceAfterDelay(wiggleDuration));
        }
        else if (partIndex < _currentPartIndex)
        {
            AudioClip clip = null;
            if (wordData.partAudios != null && partIndex < wordData.partAudios.Count)
            {
                clip = wordData.partAudios[partIndex];
            }

            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }

            GameObject currentBox = _activeBoxes[partIndex];
            StartCoroutine(WiggleAnimation(currentBox.transform, Vector3.one, Quaternion.identity, 0.4f));
        }
        else
        {
            if (_currentPartIndex < _activeBoxes.Count)
            {
                GameObject nextBox = _activeBoxes[_currentPartIndex];
                StartCoroutine(WiggleAnimation(nextBox.transform, Vector3.one, Quaternion.identity, 0.3f));
            }
        }
    }

    private IEnumerator AdvanceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        BlendWordData wordData = wordsList[_currentIndex];
        _currentPartIndex++;

        if (_currentPartIndex < wordData.wordParts.Count)
        {
            _canTap = true;
        }
        else
        {
            yield return StartCoroutine(BlendWordSequence(wordData));
        }
    }

    private IEnumerator BlendWordSequence(BlendWordData wordData)
    {
        _canTap = false;

        foreach (var box in _activeBoxes)
        {
            if (box != null)
            {
                StartCoroutine(WiggleAnimation(box.transform, Vector3.one, Quaternion.identity, 1.0f));
            }
        }

        yield return new WaitForSeconds(0.2f);

        if (_activeCompleteWordArrow != null)
        {
            _activeCompleteWordArrow.SetActive(true);
            _activeCompleteWordArrow.transform.localScale = Vector3.zero;
            LeanTween.scale(_activeCompleteWordArrow, Vector3.one, 0.3f).setEase(LeanTweenType.easeOutBack);
        }

        if (_activeCompleteWordButton != null)
        {
            _activeCompleteWordButton.SetActive(true);
            _activeCompleteWordButton.transform.localScale = Vector3.zero;
            yield return StartCoroutine(PopUI(_activeCompleteWordButton.GetComponent<RectTransform>()));
        }

        if (wordData.wholeWordAudio != null && audioSource != null)
        {
            audioSource.clip = wordData.wholeWordAudio;
            audioSource.Play();
            yield return StartCoroutine(MascotTalkAnimation(wordData.wholeWordAudio.length));
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.8f);
        }

        if (audioSource != null && cheerSFX != null)
        {
            audioSource.PlayOneShot(cheerSFX);
        if (unitCompleteAudio != null && audioSource != null) audioSource.PlayOneShot(unitCompleteAudio);
        }

        // Fill progress dot for this word immediately
        int groupIdx, wordInGroupIdx, totalWordsInGrp;
        GetWordGroupInfo(_currentIndex, out groupIdx, out wordInGroupIdx, out totalWordsInGrp);
        UpdateWordProgressDots(wordInGroupIdx + 1);

        if (nextButton != null)
        {
            nextButton.SetActive(true);
            nextButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(nextButton);
            LeanTween.scale(nextButton, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack);
        }
    }

    private void OnCompleteWordButtonClicked()
    {
        BlendWordData wordData = wordsList[_currentIndex];
        
        if (wordData.wholeWordAudio != null && audioSource != null)
        {
            audioSource.clip = wordData.wholeWordAudio;
            audioSource.Play();
            if (_activeCompleteWordButton != null)
            {
                StartCoroutine(WiggleAnimation(_activeCompleteWordButton.transform, Vector3.one, Quaternion.identity, wordData.wholeWordAudio.length));
            }
            StartCoroutine(MascotTalkAnimation(wordData.wholeWordAudio.length));
        }
    }

    private string GetColoredWord(string word, string digraphGroup)
    {
        string wordLower = word.ToLower();
        string d = string.IsNullOrEmpty(digraphGroup) ? "" : digraphGroup.ToLower();
        
        if (!string.IsNullOrEmpty(d) && wordLower.Contains(d))
        {
            int index = wordLower.IndexOf(d);
            if (index != -1)
            {
                string originalDigraph = word.Substring(index, d.Length);
                return word.Substring(0, index) + "<color=#EC407A>" + originalDigraph + "</color>" + word.Substring(index + d.Length);
            }
        }

        string[] digraphs = { "ch", "sh", "th", "wh", "ck", "nk", "ng", "ph", "gh" };
        foreach (var fallbackDigraph in digraphs)
        {
            int index = wordLower.IndexOf(fallbackDigraph);
            if (index != -1)
            {
                string originalDigraph = word.Substring(index, fallbackDigraph.Length);
                return word.Substring(0, index) + "<color=#EC407A>" + originalDigraph + "</color>" + word.Substring(index + fallbackDigraph.Length);
            }
        }
        return word;
    }

    private void OnNextClicked()
    {
        if (transitionSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSFX);
        }

        int nextIndex = _currentIndex + 1;
        if (nextIndex < wordsList.Count)
        {
            LoadWord(nextIndex);
        }
        else
        {
            OnActivityCompleted();
        }
    }

    private void OnReplayClicked()
    {
        if (_currentIndex < 0 || _currentIndex >= wordsList.Count) return;
        
        BlendWordData wordData = wordsList[_currentIndex];
        
        if (_currentPartIndex >= wordData.wordParts.Count)
        {
            OnCompleteWordButtonClicked();
        }
        else
        {
            int idx = Mathf.Clamp(_currentPartIndex - 1, 0, wordData.wordParts.Count - 1);
            AudioClip clip = null;
            if (wordData.partAudios != null && idx < wordData.partAudios.Count)
            {
                clip = wordData.partAudios[idx];
            }
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    private void OnActivityCompleted()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        if (nextButton != null) nextButton.SetActive(false);

        // Fill all group dots on absolute complete
        UpdateGroupProgressDots(_uniqueGroups.Count);

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(true);
            globalNextButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(globalNextButton);
            LeanTween.scale(globalNextButton, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack);
        }

        if (onActivityComplete != null)
        {
            onActivityComplete.Invoke();
        }
        else if (_flowManager == null)
        {
            Debug.LogWarning("No completion action set up and GameFlowManager not found in scene.");
        }
    }

    private void UpdateProgressLabel(int wordInGroupIdx, int totalWordsInGrp, string activeGroup)
    {
        if (progressLabel != null)
        {
            progressLabel.text = $"'{activeGroup}' Group: Word {wordInGroupIdx + 1} / {totalWordsInGrp}";
        }
    }

    private void SetupGroupProgressDots()
    {
        if (groupProgressDotsContainer == null) return;

        foreach (Transform child in groupProgressDotsContainer)
        {
            if (child.gameObject != groupProgressDotPrefab)
            {
                Destroy(child.gameObject);
            }
        }

        _groupDotInstances.Clear();
        if (groupProgressDotPrefab == null) return;

        groupProgressDotPrefab.SetActive(false);

        int count = _uniqueGroups.Count;
        for (int i = 0; i < count; i++)
        {
            GameObject dotObj = Instantiate(groupProgressDotPrefab, groupProgressDotsContainer);
            dotObj.SetActive(true);
            
            TextMeshProUGUI label = dotObj.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null && i < _uniqueGroups.Count)
            {
                label.text = _uniqueGroups[i];
            }

            _groupDotInstances.Add(dotObj);
        }

        UpdateGroupProgressDots(0);
    }

    private void UpdateGroupProgressDots(int activeGroupIndex)
    {
        for (int i = 0; i < _groupDotInstances.Count; i++)
        {
            Image img = _groupDotInstances[i].GetComponent<Image>();
            if (img == null) img = _groupDotInstances[i].GetComponentInChildren<Image>();

            if (img != null)
            {
                bool isCompleted = i < activeGroupIndex;
                if (isCompleted)
                {
                    img.sprite = groupDotFilledSprite;
                    img.color = groupDotFilledColor;
                }
                else
                {
                    img.sprite = groupDotEmptySprite;
                    img.color = groupDotEmptyColor;
                }
            }
        }
    }

    private void SetupWordProgressDots(int totalWordsInGroup)
    {
        if (wordProgressDotsContainer == null) return;

        foreach (Transform child in wordProgressDotsContainer)
        {
            if (child.gameObject != wordProgressDotPrefab)
            {
                Destroy(child.gameObject);
            }
        }

        _wordDotInstances.Clear();
        if (wordProgressDotPrefab == null) return;

        wordProgressDotPrefab.SetActive(false);

        for (int i = 0; i < totalWordsInGroup; i++)
        {
            GameObject dotObj = Instantiate(wordProgressDotPrefab, wordProgressDotsContainer);
            dotObj.SetActive(true);
            _wordDotInstances.Add(dotObj);
        }
    }

    private void UpdateWordProgressDots(int currentWordIndexInGroup)
    {
        for (int i = 0; i < _wordDotInstances.Count; i++)
        {
            Image img = _wordDotInstances[i].GetComponent<Image>();
            if (img == null) img = _wordDotInstances[i].GetComponentInChildren<Image>();

            if (img != null)
            {
                bool isCompleted = i < currentWordIndexInGroup;
                if (isCompleted)
                {
                    img.sprite = wordDotFilledSprite;
                    img.color = wordDotFilledColor;
                }
                else
                {
                    img.sprite = wordDotEmptySprite;
                    img.color = wordDotEmptyColor;
                }
            }
        }
    }

    // Animation Helpers
    private IEnumerator WiggleAnimation(Transform target, Vector3 origScale, Quaternion origRot, float duration)
    {
        float elapsed = 0f;
        float wiggleSpeed = 24f;
        float wiggleAngle = 10f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (target == null) yield break;

            float angle = Mathf.Sin(elapsed * wiggleSpeed) * wiggleAngle;
            target.localRotation = origRot * Quaternion.Euler(0f, 0f, angle);

            float scaleProgress = Mathf.Min(elapsed / 0.15f, 1f);
            float baseScaleMult = Mathf.Lerp(1.0f, scaleHighlightMult, scaleProgress);

            float scalePulseX = 1f + Mathf.Sin(elapsed * wiggleSpeed) * 0.06f;
            float scalePulseY = 1f - Mathf.Sin(elapsed * wiggleSpeed) * 0.06f;

            target.localScale = new Vector3(
                origScale.x * baseScaleMult * scalePulseX,
                origScale.y * baseScaleMult * scalePulseY,
                origScale.z
            );

            yield return null;
        }

        if (target != null)
        {
            float t = 0f;
            Vector3 currentScale = target.localScale;
            Quaternion currentRotation = target.localRotation;
            while (t < 1f)
            {
                t += Time.deltaTime * animationSpeed;
                if (target == null) yield break;

                target.localScale = Vector3.Lerp(currentScale, origScale, t);
                target.localRotation = Quaternion.Lerp(currentRotation, origRot, t);
                yield return null;
            }

            target.localScale = origScale;
            target.localRotation = origRot;
        }
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

    private IEnumerator PopUI(RectTransform target)
    {
        if (target == null) yield break;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed;
            float scale = Mathf.Lerp(0f, 1.15f, 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f));
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * animationSpeed * 2f;
            float scale = Mathf.Lerp(1.15f, 1f, Mathf.Clamp01(t));
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }
}
