using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class EorNoEQuestion
{
    [Tooltip("The short vowel word, e.g. 'cap'")]
    public string shortWord;

    [Tooltip("The magic-e vowel word, e.g. 'cape'")]
    public string longWord;

    [Tooltip("True if long word matches the picture, false if short word matches")]
    public bool isCorrectLong;

    [Tooltip("The picture sprite representing the correct word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for speaking the short word")]
    public AudioClip shortWordAudio;

    [Tooltip("Audio clip for speaking the long word")]
    public AudioClip longWordAudio;
}

public class EorNoE_Unit9_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    public List<EorNoEQuestion> questions = new List<EorNoEQuestion>();

    [Header("UI Buttons")]
    [Tooltip("Button for the left word card")]
    public Button leftWordButton;
    [Tooltip("Button for the right word card")]
    public Button rightWordButton;
    [Tooltip("Replay audio button to hear the word again")]
    public Button replayWordButton;
    [Tooltip("Next Level navigation button")]
    public GameObject globalNextButton;

    [Header("UI Text (TMP)")]
    [Tooltip("Text label for the left word card")]
    public TextMeshProUGUI leftWordText;
    [Tooltip("Text label for the right word card")]
    public TextMeshProUGUI rightWordText;
    [Tooltip("Title label for the activity")]
    public TextMeshProUGUI titleTextLabel;
    [Tooltip("Instruction / prompt label")]
    public TextMeshProUGUI instructionLabel;
    [Tooltip("Score count label")]
    public TextMeshProUGUI scoreLabel;
    [Tooltip("Progress label (e.g. 1/8)")]
    public TextMeshProUGUI progressLabel;

    [Header("UI Images")]
    [Tooltip("Image for the left word card background")]
    public Image leftWordBg;
    [Tooltip("Image for the right word card background")]
    public Image rightWordBg;
    [Tooltip("Target word picture image component")]
    public Image wordImage;

    [Header("UI Panels & Containers")]
    [Tooltip("Container containing the progress dot indicators")]
    public RectTransform progressDotsContainer;
    [Tooltip("Prefab instantiated for each progress dot")]
    public GameObject progressDotPrefab;

    [Header("Mascot & Visual Effects")]
    [Tooltip("Transform of the Mascot Character object")]
    public RectTransform mascotCharacter;
    [Tooltip("Confetti or Star Particle effect object on correct choice")]
    public GameObject starEffectObject;

    [Header("Progress Dot Styling")]
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = Color.gray;
    public Color dotFilledColor = Color.green;

    [Header("Card Highlight Styling")]
    public Color tileNormalColor = Color.white;
    public Color tileCorrectColor = Color.green;
    public Color tileWrongColor = Color.red;

    [Header("Audio Sources")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;

    [Header("SFX & Feedback Clips")]
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip cheerSFX;
    public AudioClip levelCompleteSFX;
    public AudioClip introAudio;

    [Header("Completion Events")]
    public UnityEvent onLevelComplete;


    // Runtime state
    private int _currentIndex = 0;
    private int _score = 0;
    private bool _canTap = true;
    private bool _started = false;
    private List<GameObject> _dotInstances = new List<GameObject>();

    private Vector3 _origLeftScale = Vector3.one;
    private Vector3 _origRightScale = Vector3.one;
    private Vector3 _originalMascotScale = Vector3.one;
    private GameFlowManager_Senior_Phonics _flowManager;
    private Coroutine _audioFeedbackCoroutine;

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
        questions = new List<EorNoEQuestion>();

        // Pairs requested: cap/cape, tap/tape, kit/kite, hop/hope, cub/cube, pin/pine, man/mane, rid/ride
        // We configure exactly one question per pair (total of 8 questions) with a balanced mix of short (no E) and long (magic-E) answers.
        questions.Add(new EorNoEQuestion { shortWord = "cap", longWord = "cape", isCorrectLong = true });    // cape (E)
        questions.Add(new EorNoEQuestion { shortWord = "tap", longWord = "tape", isCorrectLong = false });   // tap (No E)
        questions.Add(new EorNoEQuestion { shortWord = "kit", longWord = "kite", isCorrectLong = true });    // kite (E)
        questions.Add(new EorNoEQuestion { shortWord = "hop", longWord = "hope", isCorrectLong = false });   // hop (No E)
        questions.Add(new EorNoEQuestion { shortWord = "cub", longWord = "cube", isCorrectLong = true });    // cube (E)
        questions.Add(new EorNoEQuestion { shortWord = "pin", longWord = "pine", isCorrectLong = false });   // pin (No E)
        questions.Add(new EorNoEQuestion { shortWord = "man", longWord = "mane", isCorrectLong = true });    // mane (E)
        questions.Add(new EorNoEQuestion { shortWord = "rid", longWord = "ride", isCorrectLong = false });   // rid (No E)
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Assign and Populate")]
    public void AutoAssignAndPopulate()
    {
        PopulateDefaultQuestions();

        // 1. Populate assets in Editor
        foreach (var q in questions)
        {
            string targetWord = q.isCorrectLong ? q.longWord : q.shortWord;
            q.wordSprite = ResolveSprite(targetWord);
            q.shortWordAudio = ResolveAudio(q.shortWord);
            q.longWordAudio = ResolveAudio(q.longWord);
        }

        // Preload sound clips
        if (correctSFX == null) correctSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Correct Answer.mp3");
        if (wrongSFX == null) wrongSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/That is incorrect, Try again.mp3");
        if (cheerSFX == null) cheerSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (levelCompleteSFX == null) levelCompleteSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (introAudio == null) introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/PopUpSound.mp3");

        // 2. Scan recursively in children for UI Elements
        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            string cleanName = img.gameObject.name.Trim().ToLower();
            if (cleanName == "wordimage" || cleanName == "word image" || cleanName == "cardimage" || cleanName == "card image" || cleanName == "pictureimage" || cleanName == "picture image")
            {
                wordImage = img;
            }
        }

        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in allTexts)
        {
            string cleanName = txt.gameObject.name.Trim().ToLower();
            if (cleanName == "titletext" || cleanName == "title text" || cleanName == "titlelabel" || cleanName == "title label")
            {
                titleTextLabel = txt;
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

        // 3. Find Choice Buttons based on horizontal position (failsafe)
        List<Button> candidateButtons = new List<Button>();
        Button[] allChildButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in allChildButtons)
        {
            string cleanName = btn.gameObject.name.Trim().ToLower();
            // Exclude replay/next/continue buttons
            if (cleanName.Contains("replay") || cleanName.Contains("speaker") || cleanName.Contains("sound") || 
                cleanName.Contains("listen") || cleanName.Contains("next") || cleanName.Contains("continue"))
            {
                continue;
            }
            candidateButtons.Add(btn);
        }

        // Sort candidate buttons by their local X position (left to right)
        if (candidateButtons.Count >= 2)
        {
            candidateButtons.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
            
            leftWordButton = candidateButtons[0];
            leftWordText = leftWordButton.GetComponentInChildren<TextMeshProUGUI>();
            leftWordBg = leftWordButton.GetComponent<Image>();
            if (leftWordBg == null) leftWordBg = leftWordButton.GetComponentInChildren<Image>();

            rightWordButton = candidateButtons[1];
            rightWordText = rightWordButton.GetComponentInChildren<TextMeshProUGUI>();
            rightWordBg = rightWordButton.GetComponent<Image>();
            if (rightWordBg == null) rightWordBg = rightWordButton.GetComponentInChildren<Image>();
        }

        // Replay Button
        if (replayWordButton == null)
        {
            foreach (var btn in allChildButtons)
            {
                string cleanName = btn.gameObject.name.Trim().ToLower();
                if (cleanName.Contains("replay") || cleanName.Contains("speaker") || cleanName.Contains("sound") || cleanName.Contains("listen"))
                {
                    replayWordButton = btn;
                    break;
                }
            }
        }

        // AudioSources
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

        // Progress dots and stars
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            string cleanName = t.gameObject.name.Trim().ToLower();
            if (cleanName.Contains("progressdots") || cleanName.Contains("progress dots") || cleanName.Contains("dotscontainer") || cleanName == "dots")
            {
                progressDotsContainer = t.GetComponent<RectTransform>();
                Transform dotTemp = t.Find("ProgressDotTemplate");
                if (dotTemp == null) dotTemp = t.Find("DotTemplate");
                if (dotTemp != null) progressDotPrefab = dotTemp.gameObject;
            }
            else if (cleanName.Contains("stareffect") || cleanName.Contains("star effect") || (cleanName.Contains("star") && !cleanName.Contains("dot") && !cleanName.Contains("stone") && !cleanName.Contains("button")))
            {
                starEffectObject = t.gameObject;
            }
        }

        // Global objects
        GameObject nextBtnObj = GameObject.Find("GlobalNextButton");
        if (nextBtnObj == null) nextBtnObj = GameObject.Find("NextButton");
        if (nextBtnObj == null) nextBtnObj = GameObject.Find("ContinueButton");
        if (nextBtnObj != null) globalNextButton = nextBtnObj;

        GameObject characterObj = GameObject.Find("Character");
        if (characterObj == null) characterObj = GameObject.Find("MascotCharacter");
        if (characterObj != null) mascotCharacter = characterObj.GetComponent<RectTransform>();

        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log("[EorNoE_Unit9_Senior] AutoAssignAndPopulate complete! Wired all references recursively.");
    }


    private Sprite ResolveSprite(string word)
    {
        Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phonics/Assets/Phonics_Assets/Phonics_Unit 9/Add ‘E’ Magic/" + word + ".png");
        if (sprite == null) sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Phonics/Assets/Phonics_Assets/Phonics_Unit 9/Add ‘E’ Magic/" + word + ".jpg");

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
        AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/Unit 2 Phonics/Add'e' Magic/Words/" + word + ".mp3");

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

        if (leftWordButton != null)
        {
            _origLeftScale = leftWordButton.transform.localScale;
            leftWordButton.onClick.RemoveAllListeners();
            leftWordButton.onClick.AddListener(() => OnOptionClicked(true));
        }

        if (rightWordButton != null)
        {
            _origRightScale = rightWordButton.transform.localScale;
            rightWordButton.onClick.RemoveAllListeners();
            rightWordButton.onClick.AddListener(() => OnOptionClicked(false));
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

        // Hide unused cards from the layout at runtime if needed
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

        ResetButtonsScaleAndColor();
        InitializeProgressDots();
        LoadQuestion();

        if (introAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = introAudio;
            mascotAudioSource.Play();
            StartCoroutine(WaitForAudioAndEnableTaps(introAudio.length));
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

        EorNoEQuestion q = questions[_currentIndex];

        // Determine button placement
        // Even indices: short word on left, long word on right
        // Odd indices: long word on left, short word on right
        bool isLeftShort = (_currentIndex % 2 == 0);

        if (leftWordText != null) leftWordText.text = isLeftShort ? q.shortWord : q.longWord;
        if (rightWordText != null) rightWordText.text = isLeftShort ? q.longWord : q.shortWord;

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

        if (titleTextLabel != null)
        {
            titleTextLabel.text = "E or No E?";
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Tap the word that matches the picture!";
        }

        UpdateProgressUI();
        _canTap = true;
    }

    private void OnOptionClicked(bool clickedLeft)
    {
        if (!_canTap || _currentIndex < 0 || _currentIndex >= questions.Count) return;

        EorNoEQuestion q = questions[_currentIndex];

        // Left word is short if index is even
        bool isLeftShort = (_currentIndex % 2 == 0);

        // Correct word is long if isCorrectLong is true, otherwise it is short
        bool isLeftCorrect;
        if (q.isCorrectLong)
        {
            // Long word is correct
            isLeftCorrect = !isLeftShort; // Left is correct if it is NOT short
        }
        else
        {
            // Short word is correct
            isLeftCorrect = isLeftShort; // Left is correct if it IS short
        }

        bool isCorrect = (clickedLeft == isLeftCorrect);

        if (_audioFeedbackCoroutine != null)
        {
            StopCoroutine(_audioFeedbackCoroutine);
        }

        if (isCorrect)
        {
            _audioFeedbackCoroutine = StartCoroutine(HandleCorrectChoice(clickedLeft));
        }
        else
        {
            _audioFeedbackCoroutine = StartCoroutine(HandleIncorrectChoice(clickedLeft, isLeftCorrect));
        }
    }

    private IEnumerator HandleCorrectChoice(bool clickedLeft)
    {
        _canTap = false;
        _score += 10;
        UpdateScoreUI();

        // 1. Play correct SFX
        if (sfxAudioSource != null && correctSFX != null)
        {
            sfxAudioSource.PlayOneShot(correctSFX);
        }

        // 2. Change correct button color to green
        Image correctBg = clickedLeft ? leftWordBg : rightWordBg;
        if (correctBg != null)
        {
            correctBg.color = tileCorrectColor;
        }

        // 3. Zoom correct button
        Button correctButton = clickedLeft ? leftWordButton : rightWordButton;
        Vector3 originalScale = clickedLeft ? _origLeftScale : _origRightScale;
        if (correctButton != null)
        {
            LeanTween.cancel(correctButton.gameObject);
            LeanTween.scale(correctButton.gameObject, originalScale * 1.25f, 0.35f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    LeanTween.scale(correctButton.gameObject, originalScale, 0.2f)
                        .setEase(LeanTweenType.easeInQuad)
                        .setDelay(0.2f);
                });
        }

        // 4. Mascot bounce animation
        if (mascotCharacter != null)
        {
            StartCoroutine(AnimateMascotBounce());
        }

        // 5. Star effect
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(true);
            StartCoroutine(HideStarAfterDelay(1.2f));
        }

        yield return new WaitForSeconds(0.4f);

        // 6. Speak matching word audio + Cheer
        EorNoEQuestion q = questions[_currentIndex];
        AudioClip correctWordClip = q.isCorrectLong ? q.longWordAudio : q.shortWordAudio;

        if (correctWordClip != null && mascotAudioSource != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(correctWordClip);
            yield return new WaitForSeconds(correctWordClip.length + 0.2f);
        }

        if (cheerSFX != null && mascotAudioSource != null)
        {
            mascotAudioSource.PlayOneShot(cheerSFX);
        if (unitCompleteAudio != null && mascotAudioSource != null) mascotAudioSource.PlayOneShot(unitCompleteAudio);
            yield return new WaitForSeconds(0.8f);
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }

        _currentIndex++;
        LoadQuestion();
    }

    private IEnumerator HandleIncorrectChoice(bool clickedLeft, bool correctIsLeft)
    {
        _canTap = false;

        // 1. Play wrong SFX
        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        // 2. Change wrong button background to red
        Image wrongBg = clickedLeft ? leftWordBg : rightWordBg;
        if (wrongBg != null)
        {
            wrongBg.color = tileWrongColor;
        }

        // 3. Shake incorrect button
        Button wrongButton = clickedLeft ? leftWordButton : rightWordButton;
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

        // Reset wrong button color to normal
        if (wrongBg != null)
        {
            wrongBg.color = tileNormalColor;
        }

        // 4. Highlight correct button green to guide the child
        Image correctBg = correctIsLeft ? leftWordBg : rightWordBg;
        if (correctBg != null)
        {
            correctBg.color = tileCorrectColor;
        }

        // 5. Play short and long word audios sequentially to compare
        EorNoEQuestion q = questions[_currentIndex];
        if (mascotAudioSource != null)
        {
            // Say short word first
            if (q.shortWordAudio != null)
            {
                mascotAudioSource.Stop();
                mascotAudioSource.clip = q.shortWordAudio;
                mascotAudioSource.Play();
                yield return new WaitForSeconds(q.shortWordAudio.length + 0.3f);
            }

            // Say long word second
            if (q.longWordAudio != null)
            {
                mascotAudioSource.Stop();
                mascotAudioSource.clip = q.longWordAudio;
                mascotAudioSource.Play();
                yield return new WaitForSeconds(q.longWordAudio.length + 0.3f);
            }
        }

        // Reset correct button color to normal
        if (correctBg != null)
        {
            correctBg.color = tileNormalColor;
        }

        _canTap = true;
    }

    private void ReplayCurrentWordAudio()
    {
        if (_currentIndex < 0 || _currentIndex >= questions.Count) return;
        EorNoEQuestion q = questions[_currentIndex];
        AudioClip targetAudio = q.isCorrectLong ? q.longWordAudio : q.shortWordAudio;

        if (mascotAudioSource != null && targetAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(targetAudio);
        }
    }

    private void ResetButtonsScaleAndColor()
    {
        if (leftWordButton != null) leftWordButton.transform.localScale = _origLeftScale;
        if (rightWordButton != null) rightWordButton.transform.localScale = _origRightScale;

        if (leftWordBg != null) leftWordBg.color = tileNormalColor;
        if (rightWordBg != null) rightWordBg.color = tileNormalColor;
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
