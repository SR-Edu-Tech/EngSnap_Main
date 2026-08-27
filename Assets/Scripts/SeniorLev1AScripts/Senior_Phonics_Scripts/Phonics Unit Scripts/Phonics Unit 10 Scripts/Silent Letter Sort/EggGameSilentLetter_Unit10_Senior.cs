using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EggGameWord
{
    [Tooltip("The plain word text, e.g. 'lamb'")]
    public string word;

    [Tooltip("0 for Silent B, 1 for Silent H")]
    public int targetNestIndex;

    [Tooltip("Audio clip for the word pronunciation")]
    public AudioClip wordAudio;
}

[System.Serializable]
public class EggGameNestUI
{
    [Tooltip("Parent GameObject containing the nest visual elements")]
    public GameObject container;

    [Tooltip("Drop area RectTransform where drag detection occurs")]
    public RectTransform dropArea;

    [Tooltip("TextMeshPro label for the nest heading")]
    public TextMeshProUGUI label;

    [Tooltip("The container where mini eggs are spawned when correct answers are dropped")]
    public RectTransform eggContainer;

    [Tooltip("Image component of background for hover highlight feedback")]
    public Image highlightBg;

    [Tooltip("List of 4 child transforms representing the specific positions where mini eggs will be placed")]
    public List<Transform> eggPositions = new List<Transform>();

    [HideInInspector] public Color originalColor;
    [HideInInspector] public int correctCount = 0;
}

public class EggGameSilentLetter_Unit10_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    public List<EggGameWord> words = new List<EggGameWord>();

    [Header("UI Nests")]
    [Tooltip("Exactly two nests: Index 0 is Silent B, Index 1 is Silent H")]
    public List<EggGameNestUI> nests = new List<EggGameNestUI>();

    [Header("UI Draggable Egg")]
    public RectTransform draggableEgg;
    public TextMeshProUGUI draggableEggText;
    public Image draggableEggBg;
    public DraggableEgg_Unit10_Senior draggableEggHandler;
    public RectTransform stagingArea;

    [Tooltip("Optional prefab for mini eggs added to nests when correct")]
    public GameObject miniEggPrefab;

    [Tooltip("Optional sprite image used for mini eggs (if miniEggPrefab is null)")]
    public Sprite miniEggSprite;

    [Tooltip("Custom size (width, height) of the mini eggs spawned in the nests")]
    public Vector2 miniEggSize = new Vector2(40f, 50f);

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
    [Tooltip("Plays when a nest is filled (5 eggs) - Bird Cheer sound")]
    public AudioClip birdCheerSFX;
    public AudioClip levelCompleteSFX;
    public AudioClip introClip;
    [Tooltip("Voice prompt for mistakes, e.g. 'Which letter do you NOT hear?'")]
    public AudioClip mascotWrongInstructionClip;

    [Header("Gameplay Tuning")]
    public float dropInHeight = 600f;
    public Color nestNormalColor = new Color(1f, 1f, 1f, 0.1f);
    public Color nestHighlightColor = Color.yellow;
    public Color eggNormalColor = Color.white;
    public Color eggCorrectColor = Color.green;
    public Color eggWrongColor = Color.red;

    // Runtime state
    private int _currentWordIndex = 0;
    private int _score = 0;
    private bool _started = false;
    private bool _canDrag = false;
    private Vector3 _originalMascotScale = Vector3.one;

    private List<EggGameWord> _activeWords = new List<EggGameWord>();
    private List<GameObject> _dotInstances = new List<GameObject>();
    private List<GameObject> _instantiatedMiniEggs = new List<GameObject>();
    private GameFlowManager_Senior_Phonics _flowManager;

    private void Reset()
    {
#if UNITY_EDITOR
        AutoAssignAndPopulate();
#else
        PopulateDefaultWords();
#endif
    }

    [ContextMenu("Populate Default Words")]
    public void PopulateDefaultWords()
    {
        words = new List<EggGameWord>();

        // Silent B (Nest 0): lamb, comb, subtle, numb, debt (5 words)
        AddWordToSet("lamb", 0);
        AddWordToSet("comb", 0);
        AddWordToSet("subtle", 0);
        AddWordToSet("numb", 0);
        AddWordToSet("debt", 0);

        // Silent H (Nest 1): hour, ghost, heir, echo, white (5 words)
        AddWordToSet("hour", 1);
        AddWordToSet("ghost", 1);
        AddWordToSet("heir", 1);
        AddWordToSet("echo", 1);
        AddWordToSet("white", 1);
    }

    private void AddWordToSet(string wordText, int targetNest)
    {
        EggGameWord w = new EggGameWord();
        w.word = wordText;
        w.targetNestIndex = targetNest;
        words.Add(w);
    }

#if UNITY_EDITOR
    [ContextMenu("Auto-Assign & Populate")]
    public void AutoAssignAndPopulate()
    {
        PopulateDefaultWords();

        // 1. Resolve Audio clips for words
        foreach (var w in words)
        {
            string audioPath = FindAssetPathInEditor(w.word, "t:AudioClip");
            if (!string.IsNullOrEmpty(audioPath))
            {
                w.wordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
            }
        }

        // 2. Preload SFX
        if (correctSFX == null) correctSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Correct Answer.mp3");
        if (wrongSFX == null) wrongSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/That is incorrect, Try again.mp3");
        if (birdCheerSFX == null) birdCheerSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (levelCompleteSFX == null) levelCompleteSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (introClip == null) introClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/PopUpSound.mp3");

        string mascotWrongPath = FindAssetPathInEditor("Which letter do you NOT hear", "t:AudioClip");
        if (string.IsNullOrEmpty(mascotWrongPath)) mascotWrongPath = FindAssetPathInEditor("Which letter do you", "t:AudioClip");
        if (!string.IsNullOrEmpty(mascotWrongPath))
        {
            mascotWrongInstructionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(mascotWrongPath);
        }

        // 3. Assign internal child references
        Transform mascotAudioTrans = transform.Find("MascotAudioSource");
        if (mascotAudioTrans != null) mascotAudioSource = mascotAudioTrans.GetComponent<AudioSource>();

        Transform sfxAudioTrans = transform.Find("SFXAudioSource");
        if (sfxAudioTrans != null) sfxAudioSource = sfxAudioTrans.GetComponent<AudioSource>();

        Transform stagingTrans = transform.Find("GameArea/StagingArea");
        if (stagingTrans == null) stagingTrans = transform.Find("StagingArea");
        if (stagingTrans != null)
        {
            stagingArea = stagingTrans.GetComponent<RectTransform>();
            Transform eggTrans = stagingTrans.Find("DraggableEggCard");
            if (eggTrans != null)
            {
                draggableEgg = eggTrans.GetComponent<RectTransform>();
                draggableEggBg = eggTrans.GetComponent<Image>();
                draggableEggHandler = eggTrans.GetComponent<DraggableEgg_Unit10_Senior>();
                if (draggableEggHandler == null)
                {
                    draggableEggHandler = eggTrans.gameObject.AddComponent<DraggableEgg_Unit10_Senior>();
                }

                Transform txtTrans = eggTrans.Find("WordText");
                if (txtTrans != null) draggableEggText = txtTrans.GetComponent<TextMeshProUGUI>();
            }
        }

        Transform nestsContainerTrans = transform.Find("GameArea/NestsContainer");
        if (nestsContainerTrans == null) nestsContainerTrans = transform.Find("NestsContainer");
        if (nestsContainerTrans != null)
        {
            nests.Clear();
            for (int i = 0; i < Mathf.Min(2, nestsContainerTrans.childCount); i++)
            {
                nests.Add(SetupNestUIReference(nestsContainerTrans.GetChild(i), i == 0 ? "Silent 'b'" : "Silent 'h'"));
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
        Debug.Log("[EggGameSilentLetter] Script auto-configured!");
    }

    private EggGameNestUI SetupNestUIReference(Transform nestTrans, string defaultLabel)
    {
        EggGameNestUI nest = new EggGameNestUI();
        nest.container = nestTrans.gameObject;
        nest.dropArea = nestTrans.GetComponent<RectTransform>();
        nest.highlightBg = nestTrans.GetComponent<Image>();

        Transform labelTrans = nestTrans.Find("NestHeaderLabel");
        if (labelTrans != null)
        {
            nest.label = labelTrans.GetComponent<TextMeshProUGUI>();
            if (nest.label != null) nest.label.text = defaultLabel;
        }

        Transform eggContainerTrans = nestTrans.Find("EggContainer");
        if (eggContainerTrans != null)
        {
            nest.eggContainer = eggContainerTrans.GetComponent<RectTransform>();
            
            // Scan for child layout position objects under EggContainer
            nest.eggPositions = new List<Transform>();
            for (int j = 0; j < nest.eggContainer.childCount; j++)
            {
                Transform child = nest.eggContainer.GetChild(j);
                string childName = child.gameObject.name.ToLower();
                if (childName.Contains("position") || childName.Contains("pos") || childName.Contains("place"))
                {
                    nest.eggPositions.Add(child);
                }
            }
        }

        return nest;
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
        if (words == null || words.Count == 0)
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

        foreach (var nest in nests)
        {
            CacheNestColor(nest);
        }

        if (draggableEggHandler != null)
        {
            draggableEggHandler.Setup(this);
        }
    }

    private void CacheNestColor(EggGameNestUI nest)
    {
        if (nest != null)
        {
            if (nest.highlightBg != null)
                nest.originalColor = nest.highlightBg.color;
            else
                nest.originalColor = nestNormalColor;
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

        foreach (var nest in nests)
        {
            nest.correctCount = 0;
        }

        UpdateScoreUI();

        if (starEffectObject != null) starEffectObject.SetActive(false);
        if (globalNextButton != null) globalNextButton.SetActive(false);

        ClearAllMiniEggs();
        SetupActiveWords();

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

    private void SetupActiveWords()
    {
        _activeWords.Clear();
        foreach (var w in words)
        {
            _activeWords.Add(w);
        }

        // Shuffle the 10 words
        for (int i = 0; i < _activeWords.Count; i++)
        {
            int rIndex = UnityEngine.Random.Range(i, _activeWords.Count);
            EggGameWord temp = _activeWords[i];
            _activeWords[i] = _activeWords[rIndex];
            _activeWords[rIndex] = temp;
        }

        SetupProgressDots();
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

        int totalWords = _activeWords.Count;

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
            progressLabel.text = $"Egg {_currentWordIndex + 1} / {_activeWords.Count}";
        }
    }

    private void LoadWord()
    {
        if (_activeWords == null || _activeWords.Count == 0)
        {
            Debug.LogWarning("[EggGameSilentLetter] No active words!");
            return;
        }

        if (_currentWordIndex < 0 || _currentWordIndex >= _activeWords.Count)
        {
            OnCompletedAll();
            return;
        }

        UpdateProgressLabel();
        ResetNestHighlights();

        var wordData = _activeWords[_currentWordIndex];

        if (draggableEggText != null)
        {
            draggableEggText.text = wordData.word;
        }

        if (draggableEggBg != null)
        {
            draggableEggBg.color = eggNormalColor;
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Tap the egg to hear. Drag it to the correct nest!";
        }

        // Trigger drop-in animation
        if (draggableEgg != null && stagingArea != null)
        {
            _canDrag = false;
            draggableEgg.gameObject.SetActive(true);
            Vector3 startPos = new Vector3(0f, dropInHeight, 0f);
            draggableEgg.localPosition = startPos;
            draggableEgg.localScale = Vector3.zero;

            LeanTween.cancel(draggableEgg.gameObject);
            LeanTween.moveLocal(draggableEgg.gameObject, Vector3.zero, 0.6f).setEase(LeanTweenType.easeOutBack);
            LeanTween.scale(draggableEgg.gameObject, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack)
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
        if (_activeWords == null || _currentWordIndex >= _activeWords.Count) return;

        var wordData = _activeWords[_currentWordIndex];
        if (wordData.wordAudio != null)
        {
            StartCoroutine(PlayAudioSequence(wordData.wordAudio, null));
        }
    }

    public bool CanDragEgg()
    {
        return _canDrag;
    }

    public void OnEggDragStart(DraggableEgg_Unit10_Senior egg)
    {
        if (draggableEgg != null)
        {
            LeanTween.cancel(draggableEgg.gameObject);
            LeanTween.scale(draggableEgg.gameObject, Vector3.one * 1.1f, 0.15f);
        }
    }

    public void OnEggDragHover(Vector2 screenPos)
    {
        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        for (int i = 0; i < nests.Count; i++)
        {
            CheckNestHover(nests[i], screenPos, cam);
        }
    }

    private void CheckNestHover(EggGameNestUI nest, Vector2 screenPos, Camera cam)
    {
        if (nest != null && nest.container != null && nest.container.activeSelf)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(nest.dropArea, screenPos, cam))
            {
                if (nest.highlightBg != null)
                    nest.highlightBg.color = nestHighlightColor;
            }
            else
            {
                if (nest.highlightBg != null)
                    nest.highlightBg.color = nest.originalColor;
            }
        }
    }

    public void OnEggDragEnd(Vector2 screenPos)
    {
        ResetNestHighlights();

        Camera cam = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        if (_activeWords == null || _currentWordIndex >= _activeWords.Count) return;
        var wordData = _activeWords[_currentWordIndex];

        // Check nests
        for (int i = 0; i < nests.Count; i++)
        {
            if (CheckNestDrop(nests[i], screenPos, cam))
            {
                if (wordData.targetNestIndex == i)
                {
                    StartCoroutine(HandleCorrectChoice(nests[i], i));
                }
                else
                {
                    StartCoroutine(HandleIncorrectChoice());
                }
                return;
            }
        }

        ReturnToStaging();
    }

    private bool CheckNestDrop(EggGameNestUI nest, Vector2 screenPos, Camera cam)
    {
        if (nest != null && nest.container != null && nest.container.activeSelf)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(nest.dropArea, screenPos, cam);
        }
        return false;
    }

    private void ResetNestHighlights()
    {
        foreach (var nest in nests)
        {
            if (nest != null && nest.highlightBg != null)
            {
                nest.highlightBg.color = nest.originalColor;
            }
        }
    }

    private void ReturnToStaging()
    {
        if (draggableEgg != null && stagingArea != null)
        {
            _canDrag = false;
            LeanTween.cancel(draggableEgg.gameObject);
            LeanTween.scale(draggableEgg.gameObject, Vector3.one, 0.2f);
            LeanTween.moveLocal(draggableEgg.gameObject, Vector3.zero, 0.35f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() => {
                    _canDrag = true;
                });
        }
    }

    private IEnumerator HandleCorrectChoice(EggGameNestUI targetNest, int nestIndex)
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

        if (draggableEggBg != null)
        {
            draggableEggBg.color = eggCorrectColor;
        }

        if (draggableEgg != null)
        {
            LeanTween.cancel(draggableEgg.gameObject);
            LeanTween.scale(draggableEgg.gameObject, Vector3.one * 1.15f, 0.2f).setLoopPingPong(1);
        }

        yield return new WaitForSeconds(0.4f);

        // Shrink egg to targets nest eggContainer
        if (draggableEgg != null && targetNest.eggContainer != null)
        {
            LeanTween.cancel(draggableEgg.gameObject);
            Vector3 worldPos = targetNest.eggContainer.position;
            LeanTween.move(draggableEgg.gameObject, worldPos, 0.4f).setEase(LeanTweenType.easeInQuad);
            LeanTween.scale(draggableEgg.gameObject, Vector3.zero, 0.4f).setEase(LeanTweenType.easeInQuad);
            yield return new WaitForSeconds(0.4f);
            draggableEgg.gameObject.SetActive(false);
        }

        targetNest.correctCount++;
        AddMiniEggToNest(targetNest);

        _score += 10;
        UpdateScoreUI();

        _currentWordIndex++;
        UpdateProgressDots();

        // Animate mascot
        if (mascotCharacter != null)
        {
            StartCoroutine(AnimateMascotBounce());
        }

        // Check if nest is filled (reaches 5)
        if (targetNest.correctCount == 5)
        {
            // Nest filled cheer!
            if (sfxAudioSource != null && birdCheerSFX != null)
            {
                sfxAudioSource.PlayOneShot(birdCheerSFX);
            }
            if (instructionLabel != null)
            {
                instructionLabel.text = $"The {targetNest.label.text} nest is filled!";
            }
            // Shake/Animate target nest
            if (targetNest.container != null)
            {
                LeanTween.cancel(targetNest.container);
                LeanTween.scale(targetNest.container, Vector3.one * 1.15f, 0.25f).setLoopPingPong(2);
            }
            yield return new WaitForSeconds(1.0f);
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        if (_currentWordIndex >= _activeWords.Count)
        {
            OnCompletedAll();
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

        if (draggableEggBg != null)
        {
            draggableEggBg.color = eggWrongColor;
        }

        if (mascotWrongInstructionClip != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(mascotWrongInstructionClip);
        }

        if (draggableEgg != null && stagingArea != null)
        {
            LeanTween.cancel(draggableEgg.gameObject);
            Vector3 droppedPos = draggableEgg.localPosition;
            float shakeAmt = 15f;

            LeanTween.moveLocalX(draggableEgg.gameObject, droppedPos.x + shakeAmt, 0.05f)
                .setLoopPingPong(3)
                .setOnComplete(() => {
                    draggableEgg.localPosition = droppedPos;
                    if (draggableEggBg != null)
                    {
                        draggableEggBg.color = eggNormalColor;
                    }
                    
                    LeanTween.moveLocal(draggableEgg.gameObject, Vector3.zero, 0.45f)
                        .setEase(LeanTweenType.easeOutBack)
                        .setOnComplete(() => {
                            _canDrag = true;
                        });
                });
        }
        else
        {
            if (draggableEggBg != null) draggableEggBg.color = eggNormalColor;
            _canDrag = true;
        }

        yield return null;
    }

    private void AddMiniEggToNest(EggGameNestUI nest)
    {
        if (nest == null || nest.eggContainer == null) return;

        // Programmatically disable layout component to allow precise custom positions inside nest
        var layout = nest.eggContainer.GetComponent<LayoutGroup>();
        if (layout != null) layout.enabled = false;

        GameObject miniEgg;
        if (miniEggPrefab != null)
        {
            miniEgg = Instantiate(miniEggPrefab, nest.eggContainer);
        }
        else
        {
            // Programmatically build a mini egg graphic if prefab is missing
            miniEgg = new GameObject("MiniEgg", typeof(RectTransform), typeof(Image));
            miniEgg.transform.SetParent(nest.eggContainer, false);
            
            Image img = miniEgg.GetComponent<Image>();
            
            if (miniEggSprite != null)
            {
                img.sprite = miniEggSprite;
                img.color = Color.white; // Reset color to show full sprite details
            }
            else
            {
                img.color = new Color(0.95f, 0.9f, 0.85f, 1f); // Soft eggshell color fallback
                img.sprite = Resources.Load<Sprite>("UnityPlayer"); // fallback
            }
        }

        // Apply custom size delta to the egg RectTransform
        RectTransform rt = miniEgg.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = miniEggSize;
        }

        // Add to tracking
        _instantiatedMiniEggs.Add(miniEgg);

        // Apply custom local position for the egg index (0 to 4) inside the nest container
        int eggIndex = Mathf.Clamp(nest.correctCount - 1, 0, 4);
        
        // If specific placeholder Transforms are assigned in the list, use them. Otherwise, fallback to coordinates
        if (nest.eggPositions != null && eggIndex < nest.eggPositions.Count && nest.eggPositions[eggIndex] != null)
        {
            // Set parent to the position transform to lock it, resetting local position/rotation
            miniEgg.transform.SetParent(nest.eggPositions[eggIndex], false);
            RectTransform miniEggRt = miniEgg.GetComponent<RectTransform>();
            if (miniEggRt != null)
            {
                miniEggRt.anchoredPosition = Vector2.zero;
                miniEggRt.localRotation = Quaternion.identity;
            }
            else
            {
                miniEgg.transform.localPosition = Vector3.zero;
                miniEgg.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {
            Vector2[] eggPositions = new Vector2[]
            {
                new Vector2(-35f, -15f), // Egg 0: Bottom-left
                new Vector2(35f, -15f),  // Egg 1: Bottom-right
                new Vector2(0f, -20f),   // Egg 2: Bottom-center
                new Vector2(-15f, 15f),  // Egg 3: Top-left-center
                new Vector2(15f, 15f)    // Egg 4: Top-right-center
            };

            RectTransform eggRt = miniEgg.GetComponent<RectTransform>();
            if (eggRt != null)
            {
                eggRt.anchoredPosition = eggPositions[eggIndex];
            }
        }

        // Animate mini egg popping in
        miniEgg.transform.localScale = Vector3.zero;
        LeanTween.scale(miniEgg, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
    }

    private void ClearAllMiniEggs()
    {
        foreach (var egg in _instantiatedMiniEggs)
        {
            if (egg != null) Destroy(egg);
        }
        _instantiatedMiniEggs.Clear();
    }

    private void UpdateScoreUI()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = _score.ToString();
        }
    }

    private IEnumerator AnimateMascotBounce()
    {
        if (mascotCharacter == null) yield break;

        Vector3 targetScale = _originalMascotScale * 1.15f;
        float elapsed = 0f;
        float duration = 0.2f;

        while (elapsed < duration)
        {
            mascotCharacter.localScale = Vector3.Lerp(_originalMascotScale, targetScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            mascotCharacter.localScale = Vector3.Lerp(targetScale, _originalMascotScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mascotCharacter.localScale = _originalMascotScale;
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
