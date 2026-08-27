using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class CorKQuestion
{
    [Tooltip("The full correct word, e.g. 'car'")]
    public string wordText;

    [Tooltip("The gapped word text, e.g. '__ar'")]
    public string gappedWordText;

    [Tooltip("True if correct tile is C, false if correct tile is K")]
    public bool isC;

    [Tooltip("The picture sprite for this word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for this word")]
    public AudioClip wordAudio;
}

public class CorK_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    public List<CorKQuestion> questions = new List<CorKQuestion>();

    [Header("UI Choice Tiles")]
    [Tooltip("Button for choice tile 'c'")]
    public Button cButton;
    public TextMeshProUGUI cButtonText;
    public Image cButtonBg;

    [Tooltip("Button for choice tile 'k'")]
    public Button kButton;
    public TextMeshProUGUI kButtonText;
    public Image kButtonBg;

    [Header("UI Components - General")]
    public Image wordImage;
    public TextMeshProUGUI wordTextLabel;
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI instructionLabel;
    public Button replayWordButton;
    public RectTransform mascotCharacter;
    public GameObject starEffectObject;
    public GameObject globalNextButton;

    [Header("Progress & Indicators")]
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

    [Header("Tile Styling")]
    public Color tileNormalColor = Color.white;
    public Color tileCorrectColor = Color.green;
    public Color tileWrongColor = Color.red;

    [Header("Completion Events")]
    public UnityEvent onLevelComplete;

    // Runtime state
    private int _currentIndex = 0;
    private int _score = 0;
    private bool _canTap = true;
    private bool _started = false;
    private List<GameObject> _dotInstances = new List<GameObject>();
    private Vector3 _originalMascotScale = Vector3.one;
    private Vector3 _origCScale = Vector3.one;
    private Vector3 _origKScale = Vector3.one;
    private GameFlowManager_Senior_Phonics _flowManager;

    private void Reset()
    {
#if UNITY_EDITOR
        AutoAssignAndPopulate();
#else
        PopulateDefaultQuestions();
#endif
    }

    [ContextMenu("Populate Questions")]
    public void PopulateDefaultQuestions()
    {
        questions = new List<CorKQuestion>();

        // Book p. 44 grid of 26 words (deduplicating 'keen'):
        // car, cool, cake, keys, cubs, kilo, coke, cave, calm, cape, king, keen, cup, cold, kept, kiwi, cube, kind, corn, keep, coat, kite, cow, kitten, cane, kettle
        string[] wordsList = {
            "car", "cool", "cake", "keys", "cubs", "kilo", "coke", "cave", "calm", "cape",
            "king", "keen", "cup", "cold", "kept", "kiwi", "cube", "kind", "corn", "keep",
            "coat", "kite", "cow", "kitten", "cane", "kettle"
        };

        foreach (string w in wordsList)
        {
            CorKQuestion q = new CorKQuestion();
            q.wordText = w;
            
            // Build gapped word, e.g. "_ar" for "car"
            if (w.Length > 1)
            {
                q.gappedWordText = "_" + w.Substring(1);
            }
            else
            {
                q.gappedWordText = "_";
            }

            q.isC = w.StartsWith("c", StringComparison.OrdinalIgnoreCase);
#if UNITY_EDITOR
            q.wordSprite = ResolveSprite(w);
            q.wordAudio = ResolveAudio(w);
#endif

            questions.Add(q);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Assign and Populate")]
    public void AutoAssignAndPopulate()
    {
        PopulateDefaultQuestions();

        // 1. Populate assets in Editor
        foreach (var q in questions)
        {
            q.wordSprite = ResolveSprite(q.wordText);
            q.wordAudio = ResolveAudio(q.wordText);
        }

        // Preload sound clips
        if (correctSFX == null) correctSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Correct Answer.mp3");
        if (wrongSFX == null) wrongSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/That is incorrect, Try again.mp3");
        if (cheerSFX == null) cheerSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (levelCompleteSFX == null) levelCompleteSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (introClip == null) introClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/PopUpSound.mp3");

        // 2. Scan recursively in children for UI Elements to tolerate trailing spaces (e.g. "WordImage " / "WordTextLabel ")
        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            string cleanName = img.gameObject.name.Trim().ToLower();
            if (cleanName == "wordimage" || cleanName == "word image" || cleanName == "cardimage" || cleanName == "card image")
            {
                wordImage = img;
            }
        }

        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in allTexts)
        {
            string cleanName = txt.gameObject.name.Trim().ToLower();
            if (cleanName == "wordtextlabel" || cleanName == "wordtext" || cleanName == "word text label" || cleanName == "wordlabel" || cleanName == "word label")
            {
                wordTextLabel = txt;
            }
            else if (cleanName == "scorelabel" || cleanName == "scoretext" || cleanName == "score panel")
            {
                scoreLabel = txt;
            }
            else if (cleanName == "instructionlabel" || cleanName == "instruction label" || cleanName == "prompt")
            {
                instructionLabel = txt;
            }
            else if (cleanName == "progresstextlabel" || cleanName == "progresslabel" || cleanName == "progress label" || cleanName == "progresstext")
            {
                progressLabel = txt;
            }
        }

        // Scan for AudioSources
        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        foreach (var src in sources)
        {
            string cleanName = src.gameObject.name.Trim().ToLower();
            if (cleanName.Contains("mascot"))
            {
                mascotAudioSource = src;
            }
            else if (cleanName.Contains("sfx"))
            {
                sfxAudioSource = src;
            }
        }
        if (mascotAudioSource == null && sources.Length > 0) mascotAudioSource = sources[0];
        if (sfxAudioSource == null && sources.Length > 1) sfxAudioSource = sources[1];

        // Replay Button
        if (replayWordButton == null)
        {
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                string cleanName = btn.gameObject.name.Trim().ToLower();
                if (cleanName.Contains("replay") || cleanName.Contains("speaker") || cleanName.Contains("sound") || cleanName.Contains("listen"))
                {
                    replayWordButton = btn;
                    break;
                }
            }
        }

        // Progress dots and stars
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            string cleanName = t.gameObject.name.Trim().ToLower();
            if (cleanName == "progressdotscontainer" || cleanName == "progress dots container" || cleanName == "dotscontainer")
            {
                progressDotsContainer = t.GetComponent<RectTransform>();
                Transform dotTemp = t.Find("ProgressDotTemplate");
                if (dotTemp == null) dotTemp = t.Find("DotTemplate");
                if (dotTemp != null) progressDotPrefab = dotTemp.gameObject;
            }
            else if (cleanName == "stareffectplaceholder" || cleanName == "star effect" || cleanName == "stareffect")
            {
                starEffectObject = t.gameObject;
            }
        }

        // Find Option Tiles
        Transform optionsTray = null;
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            string cleanName = t.gameObject.name.Trim().ToLower();
            if (cleanName == "options_tray" || cleanName == "optioncardstray" || cleanName == "optioncards_tray" || cleanName == "optionstray" || cleanName == "options tray" || cleanName == "option cards tray" || cleanName == "optionspanel" || cleanName == "options panel")
            {
                optionsTray = t;
                break;
            }
        }

        if (optionsTray != null && optionsTray.childCount >= 2)
        {
            // Map OptionCard_0 to C Button
            Transform opt0 = optionsTray.GetChild(0);
            cButton = opt0.GetComponent<Button>();
            if (cButton == null) cButton = opt0.GetComponentInChildren<Button>();
            cButtonText = opt0.GetComponentInChildren<TextMeshProUGUI>();
            cButtonBg = opt0.GetComponent<Image>();
            if (cButtonBg == null && cButton != null) cButtonBg = cButton.GetComponent<Image>();
            if (cButtonText != null) cButtonText.text = "c";

            // Map OptionCard_1 to K Button
            Transform opt1 = optionsTray.GetChild(1);
            kButton = opt1.GetComponent<Button>();
            if (kButton == null) kButton = opt1.GetComponentInChildren<Button>();
            kButtonText = opt1.GetComponentInChildren<TextMeshProUGUI>();
            kButtonBg = opt1.GetComponent<Image>();
            if (kButtonBg == null && kButton != null) kButtonBg = kButton.GetComponent<Image>();
            if (kButtonText != null) kButtonText.text = "k";

            // Hide the unused cards (2 and 3)
            for (int i = 2; i < optionsTray.childCount; i++)
            {
                optionsTray.GetChild(i).gameObject.SetActive(false);
            }
        }

        // Global objects
        GameObject nextBtnObj = GameObject.Find("GlobalNextButton");
        if (nextBtnObj == null) nextBtnObj = GameObject.Find("NextButton");
        if (nextBtnObj != null) globalNextButton = nextBtnObj;

        GameObject characterObj = GameObject.Find("Character");
        if (characterObj == null) characterObj = GameObject.Find("MascotCharacter");
        if (characterObj != null) mascotCharacter = characterObj.GetComponent<RectTransform>();

        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log("[CorK_P_Senior] AutoAssignAndPopulate complete! Wired all references recursively.");
    }

    private Sprite ResolveSprite(string word)
    {
        Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phonics/Assets/Phonics_Assets/Phonics_Unit 8/" + word + ".png");
        if (sprite == null)
            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phonics/Assets/Phonics_Assets/Phonics_Unit 8/" + word + ".jpg");

        if (sprite == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(word + " t:Sprite");
            if (guids.Length > 0)
            {
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
        return sprite;
    }

    private AudioClip ResolveAudio(string word)
    {
        AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/Unit 8 Phonics/" + word + ".mp3");
        if (clip == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets(word + " t:AudioClip");
            if (guids.Length > 0)
            {
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
        return clip;
    }
#endif

    private void Awake()
    {
        if (questions == null || questions.Count == 0)
        {
            PopulateDefaultQuestions();
        }

        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (cButton != null)
        {
            _origCScale = cButton.transform.localScale;
            cButton.onClick.RemoveAllListeners();
            cButton.onClick.AddListener(() => OnOptionSelected(true));
        }

        if (kButton != null)
        {
            _origKScale = kButton.transform.localScale;
            kButton.onClick.RemoveAllListeners();
            kButton.onClick.AddListener(() => OnOptionSelected(false));
        }

        if (replayWordButton != null)
        {
            replayWordButton.onClick.RemoveAllListeners();
            replayWordButton.onClick.AddListener(ReplayCurrentWordAudio);
        }

        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        // Hide unused cards from tray at runtime in case the user kept 4 objects in the hierarchy
        Transform optionsTray = transform.Find("OptionsTray");
        if (optionsTray == null) optionsTray = transform.Find("OptionCardsPanel");
        if (optionsTray == null) optionsTray = transform.Find("GridOptions");
        if (optionsTray != null)
        {
            for (int i = 2; i < optionsTray.childCount; i++)
            {
                optionsTray.GetChild(i).gameObject.SetActive(false);
            }
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

    public void ResetToStart()
    {
        _currentIndex = 0;
        _score = 0;
        _canTap = false;

        if (starEffectObject != null) starEffectObject.SetActive(false);
        if (globalNextButton != null) globalNextButton.SetActive(false);

        if (cButtonText != null) cButtonText.text = "c";
        if (kButtonText != null) kButtonText.text = "k";

        ResetButtonsScaleAndColor();
        InitializeProgressDots();
        LoadQuestion();

        if (introClip != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = introClip;
            mascotAudioSource.Play();
            StartCoroutine(WaitForAudioAndEnableTaps(introClip.length));
        }
        else
        {
            _canTap = true;
        }
    }

    private IEnumerator WaitForAudioAndEnableTaps(float delay)
    {
        yield return new WaitForSeconds(delay);
        _canTap = true;
        ReplayCurrentWordAudio();
    }

    private void LoadQuestion()
    {
        ResetButtonsScaleAndColor();

        if (_currentIndex < 0 || _currentIndex >= questions.Count)
        {
            OnCompletedAllQuestions();
            return;
        }

        CorKQuestion q = questions[_currentIndex];

        if (wordTextLabel != null)
        {
            wordTextLabel.text = q.gappedWordText;
        }

        if (wordImage != null)
        {
            if (q.wordSprite != null)
            {
                wordImage.sprite = q.wordSprite;
                wordImage.gameObject.SetActive(true);
            }
            else
            {
                wordImage.gameObject.SetActive(false);
            }
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Tap c or k to start the word!";
        }

        UpdateProgressUI();
        _canTap = true;
    }

    private void OnOptionSelected(bool tappedC)
    {
        if (!_canTap || _currentIndex < 0 || _currentIndex >= questions.Count) return;

        CorKQuestion q = questions[_currentIndex];
        bool isCorrect = (tappedC == q.isC);

        if (isCorrect)
        {
            StartCoroutine(HandleCorrectChoice(tappedC));
        }
        else
        {
            StartCoroutine(HandleIncorrectChoice(tappedC));
        }
    }

    private IEnumerator HandleCorrectChoice(bool tappedC)
    {
        _canTap = false;
        _score += 10;
        UpdateScoreUI();

        if (sfxAudioSource != null && correctSFX != null)
        {
            sfxAudioSource.PlayOneShot(correctSFX);
        }

        // Highlight chosen tile
        Image correctBg = tappedC ? cButtonBg : kButtonBg;
        if (correctBg != null)
        {
            correctBg.color = tileCorrectColor;
        }

        // Reveal full word
        if (wordTextLabel != null)
        {
            CorKQuestion q = questions[_currentIndex];
            // Format full word with initial letter highlighted
            string fullWord = q.wordText;
            string highlighted = tappedC
                ? "<b><color=#4CAF50>c</color></b>" + fullWord.Substring(1)
                : "<b><color=#4CAF50>k</color></b>" + fullWord.Substring(1);
            wordTextLabel.text = highlighted;
        }

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(true);
            StartCoroutine(HideStarAfterDelay(1.2f));
        }

        if (mascotCharacter != null)
        {
            StartCoroutine(AnimateMascotBounce());
        }

        yield return new WaitForSeconds(0.4f);

        // Play word audio
        CorKQuestion currentQ = questions[_currentIndex];
        if (currentQ.wordAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(currentQ.wordAudio);
            yield return new WaitForSeconds(currentQ.wordAudio.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.6f);
        }

        _currentIndex++;
        LoadQuestion();
    }

    private IEnumerator HandleIncorrectChoice(bool tappedC)
    {
        _canTap = false;

        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        Button wrongButton = tappedC ? cButton : kButton;
        Image wrongBg = tappedC ? cButtonBg : kButtonBg;

        if (wrongBg != null)
        {
            wrongBg.color = tileWrongColor;
        }

        // Replay gapped word audio
        ReplayCurrentWordAudio();

        // Shake incorrect button
        if (wrongButton != null)
        {
            Transform t = wrongButton.transform;
            Vector3 originalPos = t.localPosition;
            float elapsed = 0f;
            float duration = 0.4f;
            float speed = 30f;
            float amount = 10f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float xOffset = Mathf.Sin(elapsed * speed) * amount;
                t.localPosition = new Vector3(originalPos.x + xOffset, originalPos.y, originalPos.z);
                yield return null;
            }

            t.localPosition = originalPos;
        }

        if (wrongBg != null)
        {
            wrongBg.color = tileNormalColor;
        }

        _canTap = true;
    }

    private void ReplayCurrentWordAudio()
    {
        if (_currentIndex < 0 || _currentIndex >= questions.Count) return;
        CorKQuestion q = questions[_currentIndex];

        if (mascotAudioSource != null && q.wordAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(q.wordAudio);
        }
    }

    private void ResetButtonsScaleAndColor()
    {
        if (cButton != null) cButton.transform.localScale = _origCScale;
        if (kButton != null) kButton.transform.localScale = _origKScale;

        if (cButtonBg != null) cButtonBg.color = tileNormalColor;
        if (kButtonBg != null) kButtonBg.color = tileNormalColor;
    }

    private IEnumerator HideStarAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
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

    private void InitializeProgressDots()
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

        for (int i = 0; i < questions.Count; i++)
        {
            GameObject dotObj = null;
            if (progressDotPrefab != null)
            {
                dotObj = Instantiate(progressDotPrefab, progressDotsContainer);
                dotObj.SetActive(true);
            }
            else
            {
                dotObj = new GameObject($"Dot_{i + 1}", typeof(RectTransform), typeof(Image));
                dotObj.transform.SetParent(progressDotsContainer, false);
                RectTransform rt = dotObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(18f, 18f);
                Image img = dotObj.GetComponent<Image>();
                img.color = dotEmptyColor;
            }
            _dotInstances.Add(dotObj);
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
                if (i < _currentIndex)
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

    private void UpdateProgressUI()
    {
        UpdateProgressDots();

        if (scoreLabel != null)
        {
            scoreLabel.text = _score.ToString();
        }

        if (progressLabel != null)
        {
            progressLabel.text = $"{_currentIndex + 1} / {questions.Count}";
        }
    }

    private void UpdateScoreUI()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = _score.ToString();
        }
    }

    private void OnCompletedAllQuestions()
    {
        _canTap = false;

        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
        }

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(true);
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Activity Complete!";
        }

        onLevelComplete?.Invoke();

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
}
