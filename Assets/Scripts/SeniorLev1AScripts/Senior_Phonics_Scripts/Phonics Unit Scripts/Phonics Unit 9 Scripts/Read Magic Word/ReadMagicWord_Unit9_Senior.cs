using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum MagicWordGameMode
{
    ReadAndPlay, // Mascot shows word + picture, asks child to read, plays audio on click/tap
    HearAndTap   // Mascot plays target word audio, child taps from 3 choices
}

[System.Serializable]
public class MagicWordQuestion
{
    [Tooltip("The Magic-E word, e.g. 'cave'")]
    public string word;

    [Tooltip("The 0-based index of the vowel letter (e.g., 1 for 'cave')")]
    public int vowelIndex;

    [Tooltip("The 0-based index of the silent-e letter (e.g., 3 for 'cave')")]
    public int silentEIndex;

    [Tooltip("The picture sprite representing the word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for normal pronunciation")]
    public AudioClip wordAudio;

    [Tooltip("Audio clip for slow pronunciation (played on incorrect choice)")]
    public AudioClip slowWordAudio;

    [Tooltip("Optional custom distractor words for the 3 choices. If empty, distractors are chosen randomly from the word list.")]
    public string[] customDistractors;
}

public class ReadMagicWord_Unit9_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    [Tooltip("The game mode for this level instance")]
    public MagicWordGameMode gameMode = MagicWordGameMode.ReadAndPlay;

    [Tooltip("List of magic-e word questions. Configurable in the Inspector or auto-populated.")]
    public List<MagicWordQuestion> questions = new List<MagicWordQuestion>();

    [Header("UI Components - General")]
    public TextMeshProUGUI titleTextLabel;
    public TextMeshProUGUI instructionLabel;
    public Image wordImage;
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI progressLabel;
    public GameObject globalNextButton;
    public RectTransform mascotCharacter;
    public GameObject starEffectObject;
    [Tooltip("Text label displaying the word next to/under the image")]
    public TextMeshProUGUI pictureWordTextLabel;

    [Header("UI Components - Read & Play Mode")]
    [Tooltip("The panel containing the central word card")]
    public GameObject readPlayPanel;
    [Tooltip("The central text label to display the magic word")]
    public TextMeshProUGUI wordTextLabel;
    [Tooltip("Speaker button to play the word audio again")]
    public Button centralSpeakerButton;
    [Tooltip("Continue button to advance in Read & Play mode")]
    public GameObject readPlayContinueButton;

    [Header("UI Components - Hear & Tap Mode")]
    [Tooltip("The panel containing the 3 choices")]
    public GameObject hearTapPanel;
    [Tooltip("The 3 choice buttons")]
    public Button[] choiceButtons = new Button[3];
    [Tooltip("The 3 text labels inside the choice buttons")]
    public TextMeshProUGUI[] choiceTextLabels = new TextMeshProUGUI[3];
    [Tooltip("The 3 image backgrounds of the choice buttons")]
    public Image[] choiceBgImages = new Image[3];
    [Tooltip("Speaker button to hear the target word again")]
    public Button hearTapSpeakerButton;

    [Header("Progress & Dots")]
    public RectTransform progressDotsContainer;
    public GameObject progressDotPrefab;
    public Sprite dotEmptySprite;
    public Sprite dotFilledSprite;
    public Color dotEmptyColor = Color.gray;
    public Color dotFilledColor = Color.green;

    [Header("UI Colors & Styling")]
    public Color cardNormalColor = Color.white;
    public Color cardCorrectColor = new Color32(76, 175, 80, 255); // Solid green
    public Color cardWrongColor = new Color32(244, 67, 54, 255);   // Solid red
    [Tooltip("Color used to highlight the vowel and silent-e (usually a pink/red glow)")]
    public Color highlightColor = new Color32(255, 51, 102, 255); // Vibrant Pink

    [Header("Audio Sources")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;

    [Header("Audio Clips")]
    public AudioClip introPromptAudio; // Mascot saying: "Read the magic words!" or similar
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip cheerSFX;
    public AudioClip levelCompleteSFX;

    [Header("Completion Event")]
    public UnityEvent onLevelComplete;

    // Runtime state
    private int _currentIndex = 0;
    private int _score = 0;
    private bool _canTap = false;
    private bool _started = false;
    private List<GameObject> _dotInstances = new List<GameObject>();
    private Coroutine _gameplayCoroutine;
    private Vector3 _originalMascotScale = Vector3.one;
    private Vector3[] _origChoiceScales = new Vector3[3];
    private Vector3 _origCentralCardScale = Vector3.one;
    private GameFlowManager_Senior_Phonics _flowManager;
    private List<int> _currentChoiceIndices = new List<int>(); // Maps button index to question index

    private void Awake()
    {
        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        if (readPlayPanel != null)
        {
            _origCentralCardScale = readPlayPanel.transform.localScale;
        }

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null)
            {
                _origChoiceScales[i] = choiceButtons[i].transform.localScale;
                int index = i; // local copy for closure
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(index));
            }
        }

        if (centralSpeakerButton != null)
        {
            centralSpeakerButton.onClick.RemoveAllListeners();
            centralSpeakerButton.onClick.AddListener(PlayCurrentWordAudio);
        }

        if (hearTapSpeakerButton != null)
        {
            hearTapSpeakerButton.onClick.RemoveAllListeners();
            hearTapSpeakerButton.onClick.AddListener(PlayCurrentWordAudio);
        }

        if (readPlayContinueButton != null)
        {
            Button btn = readPlayContinueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnReadPlayContinueClicked);
            }
        }

        if (mascotAudioSource == null) mascotAudioSource = GetComponent<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        _started = true;
        ResetActivity();
    }

    private void OnEnable()
    {
        if (_started)
        {
            ResetActivity();
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (mascotAudioSource != null) mascotAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Spacebar bypass: Auto-progress in editor
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[ReadMagicWord Bypass] Space pressed. Simulating success.");
            if (gameMode == MagicWordGameMode.ReadAndPlay)
            {
                OnReadPlayContinueClicked();
            }
            else
            {
                // Find correct choice index
                int correctBtnIdx = -1;
                for (int i = 0; i < _currentChoiceIndices.Count; i++)
                {
                    if (_currentChoiceIndices[i] == _currentIndex)
                    {
                        correctBtnIdx = i;
                        break;
                    }
                }
                if (correctBtnIdx >= 0)
                {
                    OnChoiceSelected(correctBtnIdx);
                }
            }
        }
    }
#endif

    public void ResetActivity()
    {
        _currentIndex = 0;
        _score = 0;
        _canTap = false;

        if (starEffectObject != null) starEffectObject.SetActive(false);
        if (globalNextButton != null) globalNextButton.SetActive(false);
        if (readPlayContinueButton != null) readPlayContinueButton.SetActive(false);

        ResetCardVisuals();
        InitializeProgressDots();

        if (questions == null || questions.Count == 0)
        {
            Debug.LogWarning("[ReadMagicWord] Questions list is empty. Pre-populating defaults.");
            PopulateDefaultQuestions();
        }

        if (_gameplayCoroutine != null) StopCoroutine(_gameplayCoroutine);
        _gameplayCoroutine = StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // Animate title/mascot entry if desired
        if (titleTextLabel != null)
        {
            titleTextLabel.transform.localScale = Vector3.zero;
            LeanTween.scale(titleTextLabel.gameObject, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
        }
        if (mascotCharacter != null)
        {
            mascotCharacter.localScale = Vector3.zero;
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.5f).setEase(LeanTweenType.easeOutBack);
        }

        yield return new WaitForSeconds(0.5f);

        // Play intro audio
        if (mascotAudioSource != null && introPromptAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = introPromptAudio;
            mascotAudioSource.Play();
            yield return new WaitForSeconds(introPromptAudio.length + 0.2f);
        }

        LoadQuestion();
    }

    private void LoadQuestion()
    {
        ResetCardVisuals();

        if (_currentIndex < 0 || _currentIndex >= questions.Count)
        {
            OnCompletedAllQuestions();
            return;
        }

        MagicWordQuestion q = questions[_currentIndex];

        // Update UI Info
        if (titleTextLabel != null)
        {
            titleTextLabel.text = gameMode == MagicWordGameMode.ReadAndPlay ? "Read the Word" : "Hear & Tap";
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = gameMode == MagicWordGameMode.ReadAndPlay 
                ? "Look at the picture and read the magic-e word aloud!"
                : "Listen to the word and tap the correct spelling!";
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

        if (pictureWordTextLabel != null)
        {
            pictureWordTextLabel.gameObject.SetActive(true);
            if (gameMode == MagicWordGameMode.ReadAndPlay)
            {
                pictureWordTextLabel.text = q.word;
            }
            else
            {
                pictureWordTextLabel.text = GetWordWithUnderscores(q.word, q.vowelIndex, q.silentEIndex);
            }
            Debug.Log($"[LoadQuestion] pictureWordTextLabel text set to: '{pictureWordTextLabel.text}', activeInHierarchy: {pictureWordTextLabel.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.Log("[LoadQuestion] pictureWordTextLabel is NULL in the inspector!");
        }

        UpdateProgressUI();

        // Switch panels based on Game Mode
        if (gameMode == MagicWordGameMode.ReadAndPlay)
        {
            if (readPlayPanel != null) readPlayPanel.SetActive(true);
            if (hearTapPanel != null) hearTapPanel.SetActive(false);

            if (wordTextLabel != null)
            {
                // Word is displayed normally
                wordTextLabel.text = q.word;
            }

            if (readPlayContinueButton != null) readPlayContinueButton.SetActive(true);

            _canTap = true;
            PlayCurrentWordAudio();
        }
        else
        {
            if (readPlayPanel != null) readPlayPanel.SetActive(false);
            if (hearTapPanel != null) hearTapPanel.SetActive(true);

            SetupChoices();
            _canTap = true;
            PlayCurrentWordAudio();
        }
    }

    private void SetupChoices()
    {
        _currentChoiceIndices.Clear();
        MagicWordQuestion q = questions[_currentIndex];

        // Generate 3 choices: index 0 is correct, 1 and 2 are distractors
        List<int> optionsIndices = new List<int>();
        List<string> optionsStrings = new List<string>();

        // 1. Add correct answer
        optionsIndices.Add(_currentIndex);
        optionsStrings.Add(q.word);

        // 2. Add distractors
        if (q.customDistractors != null && q.customDistractors.Length >= 2)
        {
            optionsIndices.Add(-1);
            optionsStrings.Add(q.customDistractors[0]);

            optionsIndices.Add(-1);
            optionsStrings.Add(q.customDistractors[1]);
        }
        else
        {
            // Pick two random words from list that are different
            List<int> pool = new List<int>();
            for (int i = 0; i < questions.Count; i++)
            {
                if (i != _currentIndex) pool.Add(i);
            }

            // Shuffle pool
            for (int i = 0; i < pool.Count; i++)
            {
                int temp = pool[i];
                int randomIndex = UnityEngine.Random.Range(i, pool.Count);
                pool[i] = pool[randomIndex];
                pool[randomIndex] = temp;
            }

            int count = 0;
            for (int i = 0; i < pool.Count && count < 2; i++)
            {
                optionsIndices.Add(pool[i]);
                optionsStrings.Add(questions[pool[i]].word);
                count++;
            }
        }

        // 3. Shuffle the positions (0, 1, 2)
        List<int> buttonPlacementOrder = new List<int> { 0, 1, 2 };
        for (int i = 0; i < buttonPlacementOrder.Count; i++)
        {
            int temp = buttonPlacementOrder[i];
            int randomIndex = UnityEngine.Random.Range(i, buttonPlacementOrder.Count);
            buttonPlacementOrder[i] = buttonPlacementOrder[randomIndex];
            buttonPlacementOrder[randomIndex] = temp;
        }

        // 4. Map to choice arrays and set text
        for (int i = 0; i < 3; i++)
        {
            int choiceIndex = buttonPlacementOrder[i];
            _currentChoiceIndices.Add(optionsIndices[choiceIndex]);

            if (choiceTextLabels[i] != null && choiceIndex < optionsStrings.Count)
            {
                choiceTextLabels[i].text = optionsStrings[choiceIndex];
            }
        }
    }

    private void OnChoiceSelected(int buttonIndex)
    {
        Debug.Log($"[OnChoiceSelected] Clicked buttonIndex: {buttonIndex}, _canTap: {_canTap}, gameMode: {gameMode}, currentIndex: {_currentIndex}");
        if (_currentChoiceIndices != null)
        {
            string logIndices = "";
            for (int i = 0; i < _currentChoiceIndices.Count; i++)
            {
                logIndices += $"[{i}] -> {_currentChoiceIndices[i]} | ";
            }
            Debug.Log($"[OnChoiceSelected] currentChoiceIndices map: {logIndices}");
        }
        else
        {
            Debug.Log("[OnChoiceSelected] _currentChoiceIndices list is null!");
        }

        if (!_canTap)
        {
            Debug.Log("[OnChoiceSelected] Click ignored: _canTap is false.");
            return;
        }
        if (_currentIndex < 0 || _currentIndex >= questions.Count)
        {
            Debug.Log($"[OnChoiceSelected] Click ignored: _currentIndex {_currentIndex} is out of bounds (questions count: {questions.Count}).");
            return;
        }
        if (gameMode != MagicWordGameMode.HearAndTap)
        {
            Debug.Log($"[OnChoiceSelected] Click ignored: gameMode is {gameMode} (needs to be HearAndTap).");
            return;
        }
        if (_currentChoiceIndices == null || buttonIndex < 0 || buttonIndex >= _currentChoiceIndices.Count)
        {
            Debug.Log($"[OnChoiceSelected] Click ignored: choice indices list is null or buttonIndex {buttonIndex} is out of bounds.");
            return;
        }

        int targetIndex = _currentChoiceIndices[buttonIndex];
        bool isCorrect = (targetIndex == _currentIndex);
        Debug.Log($"[OnChoiceSelected] targetIndex: {targetIndex}, isCorrect: {isCorrect}");

        if (_gameplayCoroutine != null) StopCoroutine(_gameplayCoroutine);

        if (isCorrect)
        {
            _gameplayCoroutine = StartCoroutine(HandleCorrectChoice(buttonIndex));
        }
        else
        {
            _gameplayCoroutine = StartCoroutine(HandleIncorrectChoice(buttonIndex));
        }
    }

    private IEnumerator HandleCorrectChoice(int buttonIndex)
    {
        _canTap = false;
        _score += 10;
        UpdateScoreUI();

        MagicWordQuestion q = questions[_currentIndex];

        // Play correct SFX
        if (sfxAudioSource != null && correctSFX != null)
        {
            sfxAudioSource.PlayOneShot(correctSFX);
        }

        // Green highlight
        if (choiceBgImages[buttonIndex] != null)
        {
            choiceBgImages[buttonIndex].color = cardCorrectColor;
        }

        // Zoom button
        if (choiceButtons[buttonIndex] != null)
        {
            GameObject btnObj = choiceButtons[buttonIndex].gameObject;
            Vector3 originalScale = _origChoiceScales[buttonIndex];
            LeanTween.cancel(btnObj);
            LeanTween.scale(btnObj, originalScale * 1.25f, 0.35f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => {
                    LeanTween.scale(btnObj, originalScale, 0.2f)
                        .setEase(LeanTweenType.easeInQuad)
                        .setDelay(0.2f);
                });
        }

        // Bounce Mascot
        if (mascotCharacter != null)
        {
            StartCoroutine(AnimateMascotBounce());
        }

        // Star VFX
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(true);
            StartCoroutine(HideStarAfterDelay(1.2f));
        }

        // Reveal full word on picture text label
        if (pictureWordTextLabel != null)
        {
            pictureWordTextLabel.text = FormatWordWithHighlights(q.word, q.vowelIndex, q.silentEIndex, highlightColor);
        }

        yield return new WaitForSeconds(0.4f);

        // Play word audio
        if (mascotAudioSource != null && q.wordAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(q.wordAudio);
            yield return new WaitForSeconds(q.wordAudio.length + 0.2f);
        }

        // Cheer audio
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

    private IEnumerator HandleIncorrectChoice(int buttonIndex)
    {
        _canTap = false;

        // Play wrong SFX
        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        // Red highlight
        if (choiceBgImages[buttonIndex] != null)
        {
            choiceBgImages[buttonIndex].color = cardWrongColor;
        }

        // Shake button
        if (choiceButtons[buttonIndex] != null)
        {
            Transform t = choiceButtons[buttonIndex].transform;
            Vector3 originalPos = t.localPosition;
            float elapsed = 0f;
            float duration = 0.4f;
            float speed = 30f;
            float amount = 12f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float xOffset = Mathf.Sin(elapsed * speed) * amount;
                t.localPosition = new Vector3(originalPos.x + xOffset, originalPos.y, originalPos.z);
                yield return null;
            }
            t.localPosition = originalPos;
        }

        // Reset the red highlight to normal
        if (choiceBgImages[buttonIndex] != null)
        {
            choiceBgImages[buttonIndex].color = cardNormalColor;
        }

        // Highlight VOWEL and SILENT-E on correct button, repeat word slowly
        int correctBtnIndex = -1;
        for (int i = 0; i < 3; i++)
        {
            if (_currentChoiceIndices[i] == _currentIndex)
            {
                correctBtnIndex = i;
                break;
            }
        }

        MagicWordQuestion q = questions[_currentIndex];

        if (correctBtnIndex >= 0)
        {
            // Highlight the correct spelling's card green temporarily, and format the text with highlighting
            if (choiceBgImages[correctBtnIndex] != null)
            {
                choiceBgImages[correctBtnIndex].color = cardCorrectColor;
            }

            if (choiceTextLabels[correctBtnIndex] != null)
            {
                choiceTextLabels[correctBtnIndex].text = FormatWordWithHighlights(q.word, q.vowelIndex, q.silentEIndex, highlightColor);
            }
        }

        // Repeat word slowly
        AudioClip slowClip = q.slowWordAudio != null ? q.slowWordAudio : q.wordAudio;
        if (mascotAudioSource != null && slowClip != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(slowClip);
            yield return new WaitForSeconds(slowClip.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        // Revert correct card formatting to normal text and normal color so child can tap again
        if (correctBtnIndex >= 0)
        {
            if (choiceBgImages[correctBtnIndex] != null)
            {
                choiceBgImages[correctBtnIndex].color = cardNormalColor;
            }
            if (choiceTextLabels[correctBtnIndex] != null)
            {
                choiceTextLabels[correctBtnIndex].text = q.word;
            }
        }

        _canTap = true;
    }

    private void OnReadPlayContinueClicked()
    {
        if (!_canTap || _currentIndex < 0 || _currentIndex >= questions.Count) return;

        // Perform Mascot bounce on correct completion of reading flashcard
        if (mascotCharacter != null)
        {
            StartCoroutine(AnimateMascotBounce());
        }

        // Play optional cheer/correct sound quickly
        if (sfxAudioSource != null && correctSFX != null)
        {
            sfxAudioSource.PlayOneShot(correctSFX);
        }

        _currentIndex++;
        LoadQuestion();
    }

    private void PlayCurrentWordAudio()
    {
        if (_currentIndex < 0 || _currentIndex >= questions.Count) return;
        MagicWordQuestion q = questions[_currentIndex];

        if (mascotAudioSource != null && q.wordAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(q.wordAudio);
        }
    }

    private void ResetCardVisuals()
    {
        // Revert choice card dimensions & colors
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (choiceButtons[i] != null) choiceButtons[i].transform.localScale = _origChoiceScales[i];
            if (choiceBgImages[i] != null) choiceBgImages[i].color = cardNormalColor;
        }

        if (readPlayPanel != null)
        {
            readPlayPanel.transform.localScale = _origCentralCardScale;
        }
    }

    private string FormatWordWithHighlights(string word, int vowelIdx, int silentEIdx, Color color)
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(color);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < word.Length; i++)
        {
            if (i == vowelIdx || i == silentEIdx)
            {
                sb.Append($"<color=#{hexColor}><b><u>{word[i]}</u></b></color>");
            }
            else
            {
                sb.Append(word[i]);
            }
        }
        return sb.ToString();
    }

    private string GetWordWithUnderscores(string word, int vowelIdx, int silentEIdx)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < word.Length; i++)
        {
            if (i == vowelIdx || i == silentEIdx)
            {
                sb.Append("_");
            }
            else
            {
                sb.Append(word[i]);
            }
            if (i < word.Length - 1) sb.Append(" ");
        }
        return sb.ToString();
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

    private IEnumerator HideStarAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }
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

        if (readPlayContinueButton != null)
        {
            readPlayContinueButton.SetActive(false);
        }

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(true);
            globalNextButton.transform.localScale = Vector3.zero;
            LeanTween.cancel(globalNextButton);
            LeanTween.scale(globalNextButton, Vector3.one, 0.45f).setEase(LeanTweenType.easeOutBack);

            Button btn = globalNextButton.GetComponent<Button>();
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

    [ContextMenu("Populate Questions")]
    public void PopulateDefaultQuestions()
    {
        questions = new List<MagicWordQuestion>();

        // Mixed list of exactly 10 questions (2 of each magic-e pattern)
        // a_e
        questions.Add(new MagicWordQuestion { word = "cave", vowelIndex = 1, silentEIndex = 3 });
        questions.Add(new MagicWordQuestion { word = "cake", vowelIndex = 1, silentEIndex = 3 });

        // e_e
        questions.Add(new MagicWordQuestion { word = "here", vowelIndex = 1, silentEIndex = 3 });
        questions.Add(new MagicWordQuestion { word = "Pete", vowelIndex = 1, silentEIndex = 3 });

        // i_e
        questions.Add(new MagicWordQuestion { word = "time", vowelIndex = 1, silentEIndex = 3 });
        questions.Add(new MagicWordQuestion { word = "bike", vowelIndex = 1, silentEIndex = 3 });

        // o_e
        questions.Add(new MagicWordQuestion { word = "bone", vowelIndex = 1, silentEIndex = 3 });
        questions.Add(new MagicWordQuestion { word = "home", vowelIndex = 1, silentEIndex = 3 });

        // u_e
        questions.Add(new MagicWordQuestion { word = "cute", vowelIndex = 1, silentEIndex = 3 });
        questions.Add(new MagicWordQuestion { word = "mule", vowelIndex = 1, silentEIndex = 3 });
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Assign Assets & Wire UI")]
    public void AutoAssignAssetsAndWireUI()
    {
        // 1. Populate default questions if empty
        if (questions == null || questions.Count == 0)
        {
            PopulateDefaultQuestions();
        }

        // 2. Resolve Asset References (sprites & audios)
        foreach (var q in questions)
        {
            q.wordSprite = ResolveSpriteAsset(q.word);
            q.wordAudio = ResolveAudioAsset(q.word, false);
            q.slowWordAudio = ResolveAudioAsset(q.word, true);
        }

        // Resolve sound effect clip preloads
        if (correctSFX == null) correctSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Correct Answer.mp3");
        if (wrongSFX == null) wrongSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/That is incorrect, Try again.mp3");
        if (cheerSFX == null) cheerSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");
        if (levelCompleteSFX == null) levelCompleteSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Phonics/Audio/SFX/Finish.mp3");

        // 3. Auto-Wire UI components hierarchically in children
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in allTexts)
        {
            string nameLower = t.gameObject.name.ToLower().Trim();
            if (nameLower.Contains("title")) titleTextLabel = t;
            else if (nameLower.Contains("instruction") || nameLower.Contains("prompt")) instructionLabel = t;
            else if (nameLower.Contains("score")) scoreLabel = t;
            else if (nameLower.Contains("progress") && !nameLower.Contains("dots")) progressLabel = t;
            else if (nameLower.Contains("pictureword") || nameLower.Contains("imageword") || nameLower.Contains("picture word") || nameLower.Contains("image word")) pictureWordTextLabel = t;
            else if (nameLower.Contains("wordtext") || nameLower.Contains("word text") || nameLower.Contains("wordlabel") || nameLower.Contains("word label") || nameLower == "word") wordTextLabel = t;
        }

        Image[] allImages = GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            string nameLower = img.gameObject.name.ToLower().Trim();
            if (nameLower.Contains("wordimage") || nameLower.Contains("pictureimage") || nameLower.Contains("word image") || nameLower.Contains("picture image")) wordImage = img;
        }

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            string nameLower = t.gameObject.name.ToLower().Trim();
            if (nameLower.Contains("readplay") || nameLower.Contains("readandplay") || nameLower.Contains("reading") || nameLower.Contains("flashcard")) 
                readPlayPanel = t.gameObject;
            else if (nameLower.Contains("heartap") || nameLower.Contains("hearandtap") || nameLower.Contains("quiz") || nameLower.Contains("choice")) 
                hearTapPanel = t.gameObject;
            else if (nameLower.Contains("progressdots") || nameLower.Contains("dotscontainer") || nameLower.Contains("dots")) 
                progressDotsContainer = t.GetComponent<RectTransform>();
            else if (nameLower.Contains("stareffect") || nameLower.Contains("star effect") || (nameLower.Contains("star") && !nameLower.Contains("button") && !nameLower.Contains("text") && !nameLower.Contains("card") && !nameLower.Contains("dot"))) 
                starEffectObject = t.gameObject;
        }

        // Wire dots prefab
        if (progressDotsContainer != null && progressDotPrefab == null)
        {
            Transform template = progressDotsContainer.Find("ProgressDotTemplate");
            if (template == null) template = progressDotsContainer.Find("DotTemplate");
            if (template == null) template = progressDotsContainer.Find("Dot");
            if (template != null) progressDotPrefab = template.gameObject;
        }

        // Wire Buttons
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        List<Button> choiceBtns = new List<Button>();
        foreach (var btn in allButtons)
        {
            string nameLower = btn.gameObject.name.ToLower().Trim();
            if (nameLower.Contains("next") || nameLower.Contains("continue") || nameLower.Contains("arrow"))
            {
                if (btn.transform.parent != null && (btn.transform.parent.gameObject == readPlayPanel || btn.transform.parent.gameObject.name.ToLower().Contains("read")))
                {
                    readPlayContinueButton = btn.gameObject;
                }
                else
                {
                    globalNextButton = btn.gameObject;
                }
            }
            else if (nameLower.Contains("speaker") || nameLower.Contains("sound") || nameLower.Contains("replay") || nameLower.Contains("listen"))
            {
                if (btn.transform.parent != null && (btn.transform.parent.gameObject == readPlayPanel || btn.transform.parent.gameObject.name.ToLower().Contains("read")))
                {
                    centralSpeakerButton = btn;
                }
                else
                {
                    hearTapSpeakerButton = btn;
                }
            }
            else
            {
                // Fallback: any other button in children is treated as a choice option (e.g. LeftWordButton, MiddleWordButton)
                choiceBtns.Add(btn);
            }
        }

        // Fallbacks for buttons using global searches if not found in children
        if (globalNextButton == null)
        {
            GameObject nextBtnObj = GameObject.Find("GlobalNextButton");
            if (nextBtnObj == null) nextBtnObj = GameObject.Find("NextButton");
            if (nextBtnObj == null) nextBtnObj = GameObject.Find("ContinueButton");
            if (nextBtnObj != null) globalNextButton = nextBtnObj;
        }

        // Sort choices left-to-right based on local X position
        if (choiceBtns.Count >= 3)
        {
            choiceBtns.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
            for (int i = 0; i < 3; i++)
            {
                choiceButtons[i] = choiceBtns[i];
                choiceTextLabels[i] = choiceBtns[i].GetComponentInChildren<TextMeshProUGUI>();
                choiceBgImages[i] = choiceBtns[i].GetComponent<Image>();
                if (choiceBgImages[i] == null) choiceBgImages[i] = choiceBtns[i].GetComponentInChildren<Image>();
            }
        }

        // Wire mascot
        if (mascotCharacter == null)
        {
            GameObject mascot = GameObject.Find("Character");
            if (mascot == null) mascot = GameObject.Find("MascotCharacter");
            if (mascot != null) mascotCharacter = mascot.GetComponent<RectTransform>();
        }

        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log("[ReadMagicWord_Unit9_Senior] Assets auto-assigned & hierarchy wired successfully.");
    }

    private Sprite ResolveSpriteAsset(string word)
    {
        Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Phonics/Assets/Phonics_Assets/Phonics_Unit 9/Add ‘E’ Magic/{word}.png");
        if (sprite == null) sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Phonics/Assets/Phonics_Assets/Phonics_Unit 9/Add ‘E’ Magic/{word}.jpg");

        if (sprite == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{word} t:Sprite");
            if (guids.Length > 0)
            {
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
        return sprite;
    }

    private AudioClip ResolveAudioAsset(string word, bool slowVersion)
    {
        string suffix = slowVersion ? "_slow" : "";
        AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Phonics/Audio/Unit 2 Phonics/Add'e' Magic/Words/{word}{suffix}.mp3");

        if (clip == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{word}{suffix} t:AudioClip");
            if (guids.Length > 0)
            {
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }

        // Fallback for slow versions: if not found, use normal clip
        if (clip == null && slowVersion)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"{word} t:AudioClip");
            if (guids.Length > 0)
            {
                clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }
        }
        return clip;
    }
#endif
}
