using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SilentLetterSortWord
{
    [Tooltip("The plain word text, e.g. 'comb'")]
    public string word;

    [Tooltip("The 0-based indices of the silent letters in the word")]
    public List<int> silentIndices = new List<int>();

    [Tooltip("The index of the column this word belongs to (0, 1, or 2) in its round")]
    public int targetColumnIndex;

    [Tooltip("The sprite image representing this word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for the word audio")]
    public AudioClip wordAudio;
}

[System.Serializable]
public class SilentLetterSortRound
{
    [Tooltip("The name of the round (e.g. 'Round 1: b/k/w')")]
    public string roundName;

    [Tooltip("Header label for Column 0")]
    public string column0Header;

    [Tooltip("Header label for Column 1")]
    public string column1Header;

    [Tooltip("Header label for Column 2")]
    public string column2Header;

    [Tooltip("The total library of words for this round")]
    public List<SilentLetterSortWord> words = new List<SilentLetterSortWord>();
}

[System.Serializable]
public class SilentLetterSortColumnUI
{
    [Tooltip("The parent GameObject containing the column visual elements")]
    public GameObject container;

    [Tooltip("Drop area RectTransform where drag detection occurs")]
    public RectTransform dropArea;

    [Tooltip("The TextMeshPro label for the column heading")]
    public TextMeshProUGUI label;

    [Tooltip("The container/Layout where correct cards badges are stacked")]
    public RectTransform cardStackContainer;

    [Tooltip("Image component of background for hover highlight feedback")]
    public Image highlightBg;

    [HideInInspector] public Color originalColor;
}

public class SilentLetterSort_Unit10_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    public List<SilentLetterSortRound> rounds = new List<SilentLetterSortRound>();
    [Tooltip("Number of words to randomly select from each category/column per round (e.g. 3 words from b, 3 from k, 3 from w)")]
    public int wordsPerCategoryPerRound = 3;

    [Header("UI Columns")]
    public List<SilentLetterSortColumnUI> columns = new List<SilentLetterSortColumnUI>();

    [Header("UI Draggable Staging Card")]
    public RectTransform draggableCard;
    public TextMeshProUGUI draggableCardText;
    public Image draggableCardBg;
    public Image draggableCardImage;
    public DraggableSilentLetterSortCard_Unit10_Senior draggableCardHandler;
    public RectTransform stagingArea;

    [Tooltip("Prefab template for correctly sorted words in the columns.")]
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
    [Tooltip("Audio clip played when child makes a wrong choice, saying 'Which letter do you NOT hear?'")]
    public AudioClip mascotWrongInstructionClip;

    [Header("Gameplay Tuning")]
    public float dropInHeight = 600f;
    public Color columnNormalColor = new Color(1f, 1f, 1f, 0.1f);
    public Color columnHighlightColor = Color.yellow;
    public Color cardNormalColor = Color.white;
    public Color cardCorrectColor = Color.green;
    public Color cardWrongColor = Color.red;

    [Header("Word Badge Styling (Play Mode Fallback)")]
    public Color wordBadgeBgColor = new Color(1f, 1f, 1f, 0.15f);
    public Color wordBadgeTextColor = Color.white;
    public float wordBadgeTextSize = 24f;
    public Vector2 wordBadgeSize = new Vector2(200f, 50f);
    [Tooltip("The size of the collected cards stacked inside the columns")]
    public Vector2 collectedCardSize = new Vector2(75f, 90f);
    [Tooltip("The font size of the text on the cards")]
    public float cardTextSize = 20f;

    // Runtime state
    private int _currentRoundIndex = 0;
    private int _currentWordIndex = 0;
    private int _score = 0;
    private bool _started = false;
    private bool _canDrag = false;
    private Vector3 _originalMascotScale = Vector3.one;

    private List<SilentLetterSortWord> _activeWordsInRound = new List<SilentLetterSortWord>();
    private List<GameObject> _dotInstances = new List<GameObject>();
    private List<GameObject> _instantiatedBadges = new List<GameObject>();
    private GameFlowManager_Senior_Phonics _flowManager;
    private GameObject _activeCardInstance;

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
        rounds = new List<SilentLetterSortRound>();

        // --- ROUND 1: b/k/w (Exactly 10 words) ---
        SilentLetterSortRound r1 = new SilentLetterSortRound();
        r1.roundName = "Round 1: b / k / w";
        r1.column0Header = "silent b";
        r1.column1Header = "silent k";
        r1.column2Header = "silent w";

        // b words (Column 0 - 4 words)
        AddWordToRound(r1, "comb", new List<int> { 3 }, 0);
        AddWordToRound(r1, "climb", new List<int> { 4 }, 0);
        AddWordToRound(r1, "thumb", new List<int> { 4 }, 0);
        AddWordToRound(r1, "lamb", new List<int> { 3 }, 0);

        // k words (Column 1 - 3 words)
        AddWordToRound(r1, "know", new List<int> { 0 }, 1);
        AddWordToRound(r1, "knee", new List<int> { 0 }, 1);
        AddWordToRound(r1, "knife", new List<int> { 0 }, 1);

        // w words (Column 2 - 3 words)
        AddWordToRound(r1, "write", new List<int> { 0 }, 2);
        AddWordToRound(r1, "wrong", new List<int> { 0 }, 2);
        AddWordToRound(r1, "wrap", new List<int> { 0 }, 2);

        rounds.Add(r1);

        // --- ROUND 2: h/g/t (Exactly 10 words) ---
        SilentLetterSortRound r2 = new SilentLetterSortRound();
        r2.roundName = "Round 2: h / g / t";
        r2.column0Header = "silent h";
        r2.column1Header = "silent g / gh";
        r2.column2Header = "silent t";

        // h words (Column 0 - 4 words)
        AddWordToRound(r2, "ghost", new List<int> { 1 }, 0);
        AddWordToRound(r2, "hour", new List<int> { 0 }, 0);
        AddWordToRound(r2, "honest", new List<int> { 0 }, 0);
        AddWordToRound(r2, "school", new List<int> { 2 }, 0);

        // g / gh words (Column 1 - 3 words)
        AddWordToRound(r2, "sign", new List<int> { 2 }, 1);
        AddWordToRound(r2, "gnome", new List<int> { 0 }, 1);
        AddWordToRound(r2, "light", new List<int> { 2, 3 }, 1);

        // t words (Column 2 - 3 words)
        AddWordToRound(r2, "castle", new List<int> { 3 }, 2);
        AddWordToRound(r2, "listen", new List<int> { 3 }, 2);
        AddWordToRound(r2, "whistle", new List<int> { 4 }, 2);

        rounds.Add(r2);

        // --- ROUND 3: l/u/c (Exactly 10 words) ---
        SilentLetterSortRound r3 = new SilentLetterSortRound();
        r3.roundName = "Round 3: l / u / c";
        r3.column0Header = "silent l";
        r3.column1Header = "silent u";
        r3.column2Header = "silent c/p/d";

        // l words (Column 0 - 4 words)
        AddWordToRound(r3, "talk", new List<int> { 2 }, 0);
        AddWordToRound(r3, "walk", new List<int> { 2 }, 0);
        AddWordToRound(r3, "half", new List<int> { 2 }, 0);
        AddWordToRound(r3, "calm", new List<int> { 2 }, 0);

        // u words (Column 1 - 3 words)
        AddWordToRound(r3, "guitar", new List<int> { 1 }, 1);
        AddWordToRound(r3, "guest", new List<int> { 1 }, 1);
        AddWordToRound(r3, "guard", new List<int> { 1 }, 1);

        // c/p/d words (Column 2 - 3 words)
        AddWordToRound(r3, "scissors", new List<int> { 1 }, 2);
        AddWordToRound(r3, "cupboard", new List<int> { 2 }, 2);
        AddWordToRound(r3, "badge", new List<int> { 2 }, 2);

        rounds.Add(r3);
    }

    private void AddWordToRound(SilentLetterSortRound round, string word, List<int> indices, int targetCol)
    {
        SilentLetterSortWord w = new SilentLetterSortWord();
        w.word = word;
        w.silentIndices = indices;
        w.targetColumnIndex = targetCol;
        round.words.Add(w);
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Assign and Populate")]
    public void AutoAssignAndPopulate()
    {
        PopulateDefaultWords();

        // 2. Automatically find and assign audio and sprite assets
        foreach (var round in rounds)
        {
            foreach (var word in round.words)
            {
                string audioPath = FindAssetPathInEditor(word.word, "t:AudioClip");
                if (!string.IsNullOrEmpty(audioPath))
                {
                    word.wordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
                }

                string spritePath = FindAssetPathInEditor(word.word, "t:Sprite");
                if (!string.IsNullOrEmpty(spritePath))
                {
                    word.wordSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                }
            }
        }

        // 3. Populate default clips
        if (correctSFX == null) correctSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Correct Answer.mp3");
        if (wrongSFX == null) wrongSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/That is incorrect, Try again.mp3");
        if (cheerSFX == null) cheerSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (levelCompleteSFX == null) levelCompleteSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (introClip == null) introClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/PopUpSound.mp3");

        // Try to search for "Which letter do you NOT hear" or similar clip
        string mascotWrongPath = FindAssetPathInEditor("Which letter do you NOT hear", "t:AudioClip");
        if (string.IsNullOrEmpty(mascotWrongPath)) mascotWrongPath = FindAssetPathInEditor("Which letter do you", "t:AudioClip");
        if (!string.IsNullOrEmpty(mascotWrongPath))
        {
            mascotWrongInstructionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(mascotWrongPath);
        }

        // 4. Assign UI child references
        Transform mascotAudioTrans = transform.Find("MascotAudioSource");
        if (mascotAudioTrans != null) mascotAudioSource = mascotAudioTrans.GetComponent<AudioSource>();

        Transform sfxAudioTrans = transform.Find("SFXAudioSource");
        if (sfxAudioTrans != null) sfxAudioSource = sfxAudioTrans.GetComponent<AudioSource>();

        Transform stagingTrans = transform.Find("GameArea/StagingArea");
        if (stagingTrans == null) stagingTrans = transform.Find("StagingArea");
        if (stagingTrans != null)
        {
            stagingArea = stagingTrans.GetComponent<RectTransform>();
            Transform cardTrans = stagingTrans.Find("ActiveDraggableCard");
            if (cardTrans != null)
            {
                draggableCard = cardTrans.GetComponent<RectTransform>();
                draggableCardBg = cardTrans.GetComponent<Image>();
                draggableCardHandler = cardTrans.GetComponent<DraggableSilentLetterSortCard_Unit10_Senior>();
                if (draggableCardHandler == null)
                {
                    draggableCardHandler = cardTrans.gameObject.AddComponent<DraggableSilentLetterSortCard_Unit10_Senior>();
                }

                Transform cardTxtTrans = cardTrans.Find("CardText");
                if (cardTxtTrans != null) draggableCardText = cardTxtTrans.GetComponent<TextMeshProUGUI>();

                Transform cardImgTrans = cardTrans.Find("CardImage");
                if (cardImgTrans != null) draggableCardImage = cardImgTrans.GetComponent<Image>();
            }
        }

        Transform columnsContainerTrans = transform.Find("GameArea/ColumnsContainer");
        if (columnsContainerTrans == null) columnsContainerTrans = transform.Find("ColumnsContainer");
        if (columnsContainerTrans != null)
        {
            columns.Clear();
            for (int i = 0; i < Mathf.Min(3, columnsContainerTrans.childCount); i++)
            {
                columns.Add(SetupColumnUIReference(columnsContainerTrans.GetChild(i), $"silent category {i}"));
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

        Transform instructionTrans = transform.Find("InstructionLabel");
        if (instructionTrans != null) instructionLabel = instructionTrans.GetComponent<TextMeshProUGUI>();

        Transform starTrans = transform.Find("StarEffectPlaceholder");
        if (starTrans != null) starEffectObject = starTrans.gameObject;

        GameObject nextBtnObj = GameObject.Find("GlobalNextButton");
        if (nextBtnObj == null) nextBtnObj = GameObject.Find("NextButton");
        if (nextBtnObj != null) globalNextButton = nextBtnObj;

        GameObject characterObj = GameObject.Find("Character");
        if (characterObj == null) characterObj = GameObject.Find("MascotCharacter");
        if (characterObj != null) mascotCharacter = characterObj.GetComponent<RectTransform>();

        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log("[SilentLetterSort] Script configured and references assigned automatically!");
    }

    private SilentLetterSortColumnUI SetupColumnUIReference(Transform colTrans, string defaultLabel)
    {
        SilentLetterSortColumnUI col = new SilentLetterSortColumnUI();
        col.container = colTrans.gameObject;
        col.dropArea = colTrans.GetComponent<RectTransform>();
        col.highlightBg = colTrans.GetComponent<Image>();

        Transform headerLabelTrans = colTrans.Find("ColumnHeaderLabel");
        if (headerLabelTrans != null)
        {
            col.label = headerLabelTrans.GetComponent<TextMeshProUGUI>();
            if (col.label != null)
            {
                col.label.text = defaultLabel;
            }
        }

        Transform stackTrans = colTrans.Find("CardStackContainer");
        if (stackTrans != null) col.cardStackContainer = stackTrans.GetComponent<RectTransform>();

        return col;
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

        foreach (var col in columns)
        {
            CacheColumnColor(col);
            if (col != null && col.cardStackContainer != null)
            {
                GridLayoutGroup glg = col.cardStackContainer.GetComponent<GridLayoutGroup>();
                if (glg != null)
                {
                    glg.cellSize = collectedCardSize;
                }
            }
        }

        if (draggableCard != null)
        {
            draggableCard.gameObject.SetActive(false);
        }
    }

    private void CacheColumnColor(SilentLetterSortColumnUI col)
    {
        if (col != null)
        {
            if (col.highlightBg != null)
                col.originalColor = col.highlightBg.color;
            else
                col.originalColor = columnNormalColor;
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
        ResetColumnHighlights();

        if (rounds == null || _currentRoundIndex >= rounds.Count) return;
        var round = rounds[_currentRoundIndex];

        // Update column headers
        if (columns.Count > 0 && columns[0].label != null) columns[0].label.text = round.column0Header;
        if (columns.Count > 1 && columns[1].label != null) columns[1].label.text = round.column1Header;
        if (columns.Count > 2 && columns[2].label != null) columns[2].label.text = round.column2Header;

        // Select subset of words for the active round
        SelectActiveWordsForRound(round);

        Debug.Log($"[SilentLetterSort] SetupRound: Selected {_activeWordsInRound.Count} words for Round {_currentRoundIndex + 1}");

        SetupProgressDots();
    }

    private void SelectActiveWordsForRound(SilentLetterSortRound round)
    {
        _activeWordsInRound.Clear();
        
        // Since each round now has exactly 10 words defined in the library,
        // we can simply use all of them directly.
        _activeWordsInRound.AddRange(round.words);

        // Shuffle active list so columns are mixed
        ShuffleList(_activeWordsInRound);
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rIndex = UnityEngine.Random.Range(i, list.Count);
            T temp = list[i];
            list[i] = list[rIndex];
            list[rIndex] = temp;
        }
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

        int totalWords = _activeWordsInRound.Count;

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
            progressLabel.text = $"Word {_currentWordIndex + 1} / {_activeWordsInRound.Count} (Round {_currentRoundIndex + 1}/{rounds.Count})";
        }
    }

    private void LoadWord()
    {
        if (_activeWordsInRound == null || _activeWordsInRound.Count == 0)
        {
            Debug.LogWarning("[SilentLetterSort] No active words in round!");
            return;
        }

        if (_currentWordIndex < 0 || _currentWordIndex >= _activeWordsInRound.Count)
        {
            AdvanceRound();
            return;
        }

        UpdateProgressLabel();

        var wordData = _activeWordsInRound[_currentWordIndex];

        if (_activeCardInstance != null)
        {
            Destroy(_activeCardInstance);
        }

        if (draggableCard == null)
        {
            Debug.LogError("[SilentLetterSort] Draggable card template is null!");
            return;
        }

        _activeCardInstance = Instantiate(draggableCard.gameObject, stagingArea);
        _activeCardInstance.SetActive(true);

        RectTransform instanceRt = _activeCardInstance.GetComponent<RectTransform>();
        TextMeshProUGUI instanceText = _activeCardInstance.GetComponentInChildren<TextMeshProUGUI>();
        Image instanceBg = _activeCardInstance.GetComponent<Image>();
        DraggableSilentLetterSortCard_Unit10_Senior instanceHandler = _activeCardInstance.GetComponent<DraggableSilentLetterSortCard_Unit10_Senior>();

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
            instanceText.text = FormatWordWithHighlight(wordData.word, wordData.silentIndices);
            instanceText.fontSize = cardTextSize;
        }

        if (instanceBg != null)
        {
            instanceBg.color = cardNormalColor;
        }

        if (instanceImage != null)
        {
            if (wordData.wordSprite != null)
            {
                instanceImage.sprite = wordData.wordSprite;
                instanceImage.gameObject.SetActive(true);
            }
            else
            {
                instanceImage.gameObject.SetActive(false);
            }
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Drag the card under its silent letter.";
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

    private string FormatWordWithHighlight(string wordText, List<int> indices)
    {
        if (string.IsNullOrEmpty(wordText) || indices == null || indices.Count == 0) return wordText;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < wordText.Length; i++)
        {
            if (indices.Contains(i))
            {
                sb.Append($"<b><color=#FF3366><u>{wordText[i]}</u></color></b>");
            }
            else
            {
                sb.Append(wordText[i]);
            }
        }
        return sb.ToString();
    }

    public void PlayCurrentWordAudio()
    {
        if (_activeWordsInRound == null || _currentWordIndex >= _activeWordsInRound.Count) return;

        var wordData = _activeWordsInRound[_currentWordIndex];
        if (wordData.wordAudio != null)
        {
            StartCoroutine(PlayAudioSequence(wordData.wordAudio, null));
        }
    }

    public bool CanDragCard()
    {
        return _canDrag;
    }

    public void OnCardDragStart(DraggableSilentLetterSortCard_Unit10_Senior card)
    {
        if (card != null)
        {
            LeanTween.cancel(card.gameObject);
            LeanTween.scale(card.gameObject, Vector3.one * 1.05f, 0.15f);
        }
    }

    public void OnCardDragHover(DraggableSilentLetterSortCard_Unit10_Senior card, Vector2 screenPos)
    {
        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        for (int i = 0; i < columns.Count; i++)
        {
            CheckColumnHover(columns[i], screenPos, cam);
        }
    }

    private void CheckColumnHover(SilentLetterSortColumnUI col, Vector2 screenPos, Camera cam)
    {
        if (col != null && col.container != null && col.container.activeSelf)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(col.dropArea, screenPos, cam))
            {
                if (col.highlightBg != null)
                    col.highlightBg.color = columnHighlightColor;
            }
            else
            {
                if (col.highlightBg != null)
                    col.highlightBg.color = col.originalColor;
            }
        }
    }

    public void OnCardDragEnd(DraggableSilentLetterSortCard_Unit10_Senior card, Vector2 screenPos)
    {
        ResetColumnHighlights();

        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        if (_activeWordsInRound == null || _currentWordIndex >= _activeWordsInRound.Count) return;
        var wordData = _activeWordsInRound[_currentWordIndex];

        // Check columns
        for (int i = 0; i < columns.Count; i++)
        {
            if (CheckColumnDrop(columns[i], screenPos, cam))
            {
                if (wordData.targetColumnIndex == i)
                {
                    StartCoroutine(HandleCorrectChoice(card, columns[i]));
                }
                else
                {
                    StartCoroutine(HandleIncorrectChoice(card));
                }
                return;
            }
        }

        // Return to staging if dropped outside
        ReturnToStaging(card);
    }

    private bool CheckColumnDrop(SilentLetterSortColumnUI col, Vector2 screenPos, Camera cam)
    {
        if (col != null && col.container != null && col.container.activeSelf)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(col.dropArea, screenPos, cam);
        }
        return false;
    }

    private void ResetColumnHighlights()
    {
        foreach (var col in columns)
        {
            if (col != null && col.highlightBg != null)
            {
                col.highlightBg.color = col.originalColor;
            }
        }
    }

    private void ReturnToStaging(DraggableSilentLetterSortCard_Unit10_Senior card)
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

    private IEnumerator HandleCorrectChoice(DraggableSilentLetterSortCard_Unit10_Senior card, SilentLetterSortColumnUI targetCol)
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

        if (card != null && targetCol.cardStackContainer != null)
        {
            card.transform.SetParent(targetCol.cardStackContainer, false);
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

        var wordData = _activeWordsInRound[_currentWordIndex - 1];
        if (wordData.wordAudio != null)
        {
            yield return StartCoroutine(PlayAudioSequence(wordData.wordAudio, null));
        }

        if (_currentWordIndex >= _activeWordsInRound.Count)
        {
            AdvanceRound();
        }
        else
        {
            LoadWord();
        }
    }

    private IEnumerator HandleIncorrectChoice(DraggableSilentLetterSortCard_Unit10_Senior card)
    {
        _canDrag = false;

        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Which letter do you NOT hear?";
        }

        Image cardBg = card.GetComponent<Image>();
        if (cardBg != null)
        {
            cardBg.color = cardWrongColor;
        }

        // Play mascot wrong voice prompt
        if (mascotWrongInstructionClip != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(mascotWrongInstructionClip);
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

    private void AddWordToColumnUI(string text, RectTransform container)
    {
        if (container == null) return;

        GameObject badge;
        if (wordBadgePrefab != null)
        {
            badge = Instantiate(wordBadgePrefab, container);
            TextMeshProUGUI tmp = badge.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }
        else
        {
            badge = new GameObject("WordBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(container, false);
            
            Image bg = badge.GetComponent<Image>();
            bg.color = wordBadgeBgColor;
            
            RectTransform rt = badge.GetComponent<RectTransform>();
            rt.sizeDelta = wordBadgeSize;
            
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(badge.transform, false);
            
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
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
