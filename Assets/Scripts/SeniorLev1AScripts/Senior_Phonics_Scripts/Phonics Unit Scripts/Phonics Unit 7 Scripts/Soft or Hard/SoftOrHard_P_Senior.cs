using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class SoftOrHardWord
{
    [Tooltip("The plain word text, e.g. 'giant'")]
    public string wordText;

    [Tooltip("The formatted word text with letter after 'g' highlighted, e.g. 'g<b><color=#FF3366><u>i</u></color></b>ant'")]
    public string highlightedWordText;

    [Tooltip("True if soft G sound /j/ (when followed by e, i, y), false if hard G sound /g/")]
    public bool isSoftG;

    [Tooltip("Optional image sprite for the word")]
    public Sprite wordSprite;

    [Tooltip("Audio clip for speaking the word")]
    public AudioClip wordAudio;
}

public class SoftOrHard_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("Gameplay Config")]
    [Tooltip("Word list for book p. 38 Activity 1 - Soft or Hard?")]
    public List<SoftOrHardWord> words = new List<SoftOrHardWord>();

    [Tooltip("Highlight color for the letter after 'g'")]
    public Color highlightColor = new Color(1f, 0.2f, 0.4f); // Vibrant Pink/Red

    [Header("UI Display Components")]
    public TextMeshProUGUI titleTextLabel;
    public TextMeshProUGUI instructionLabel;
    public TextMeshProUGUI wordTextLabel;
    public Image wordImage;

    [Header("UI Sound Choice Buttons")]
    [Tooltip("Button for /j/ soft sound choice")]
    public Button softGButton;
    public TextMeshProUGUI softGButtonText;

    [Tooltip("Button for /g/ hard sound choice")]
    public Button hardGButton;
    public TextMeshProUGUI hardGButtonText;

    [Tooltip("Replay audio button to hear the word again")]
    public Button replayWordButton;

    [Header("Mascot & Animations")]
    public RectTransform mascotCharacter;
    public GameObject starEffectObject;
    public GameObject globalNextButton;

    [Header("Progress & Score")]
    public TextMeshProUGUI scoreLabel;
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
    [Tooltip("Optional audio clip to play when the activity starts")]
    public AudioClip introAudio;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip levelCompleteSFX;

    [Header("Events & Completion")]
    public GameObject unitCompleteScreen;
    public UnityEvent onLevelComplete;

    // Runtime state variables
    private int _currentIndex = 0;
    private int _score = 0;
    private bool _canTap = true;
    private List<GameObject> _dotInstances = new List<GameObject>();

    private Vector3 _softBtnOriginalScale = Vector3.one;
    private Vector3 _hardBtnOriginalScale = Vector3.one;
    private Coroutine _pulseCoroutine;
    private GameFlowManager_Senior_Phonics _flowManager;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (words == null || words.Count == 0)
        {
            PopulateDefaultWords();
        }
    }
#endif

    private void Awake()
    {
        _flowManager = FindObjectOfType<GameFlowManager_Senior_Phonics>();

        if (words == null || words.Count == 0)
        {
            PopulateDefaultWords();
        }

        AutoWireUI();

        if (softGButton != null)
        {
            _softBtnOriginalScale = softGButton.transform.localScale;
            softGButton.onClick.RemoveAllListeners();
            softGButton.onClick.AddListener(() => OnChoiceSelected(true));
        }

        if (hardGButton != null)
        {
            _hardBtnOriginalScale = hardGButton.transform.localScale;
            hardGButton.onClick.RemoveAllListeners();
            hardGButton.onClick.AddListener(() => OnChoiceSelected(false));
        }

        if (replayWordButton != null)
        {
            replayWordButton.onClick.RemoveAllListeners();
            replayWordButton.onClick.AddListener(ReplayCurrentWordAudio);
        }
    }

    private void Start()
    {
        _currentIndex = 0;
        _score = 0;
        _canTap = false;

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(false);
        }

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(false);
        }

        InitializeProgressDots();
        LoadCurrentWord(playAudio: false);
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        _canTap = false;

        if (introAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.clip = introAudio;
            mascotAudioSource.Play();

            while (mascotAudioSource.isPlaying)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.2f);
            mascotAudioSource.clip = null; // Clear clip reference so PlayOneShot won't trigger intro flag
        }
        else
        {
            yield return new WaitForSeconds(0.6f);
        }

        ReplayCurrentWordAudio();
        _canTap = true;
    }

    /// <summary>
    /// Populates the exact 20 words from Book p. 38 list:
    /// game, gym, gate, gold, gist, giant, great, gentle, genius, glove, bridge, magnet, organ, engine, triangle, forgot, angle, angry, rigid, danger.
    /// </summary>
    public void PopulateDefaultWords()
    {
        words = new List<SoftOrHardWord>();

        string hexColor = ColorUtility.ToHtmlStringRGB(highlightColor);
        if (string.IsNullOrEmpty(hexColor)) hexColor = "FF3366";

        (string word, bool isSoft)[] p38List = new (string, bool)[]
        {
            ("game", false),     // g + a -> hard /g/
            ("gym", true),       // g + y -> soft /j/
            ("gate", false),     // g + a -> hard /g/
            ("gold", false),     // g + o -> hard /g/
            ("gist", true),      // g + i -> soft /j/
            ("giant", true),     // g + i -> soft /j/
            ("great", false),    // g + r -> hard /g/
            ("gentle", true),    // g + e -> soft /j/
            ("genius", true),    // g + e -> soft /j/
            ("glove", false),    // g + l -> hard /g/
            ("bridge", true),    // g + e -> soft /j/
            ("magnet", false),   // g + n -> hard /g/
            ("organ", false),    // g + a -> hard /g/
            ("engine", true),    // g + i -> soft /j/
            ("triangle", false), // g + l -> hard /g/
            ("forgot", false),   // g + o -> hard /g/
            ("angle", false),    // g + l -> hard /g/
            ("angry", false),    // g + r -> hard /g/
            ("rigid", true),     // g + i -> soft /j/
            ("danger", true)     // g + e -> soft /j/
        };

        foreach (var entry in p38List)
        {
            SoftOrHardWord item = new SoftOrHardWord();
            item.wordText = entry.word;
            item.isSoftG = entry.isSoft;
            item.highlightedWordText = FormatWordWithHighlight(entry.word, hexColor);
            words.Add(item);
        }
    }

    /// <summary>
    /// Formats text to highlight the letter directly after 'g'.
    /// </summary>
    public static string FormatWordWithHighlight(string word, string colorHex = "FF3366")
    {
        if (string.IsNullOrEmpty(word)) return word;

        int gIndex = word.IndexOf('g', StringComparison.OrdinalIgnoreCase);
        if (gIndex >= 0 && gIndex < word.Length - 1)
        {
            string before = word.Substring(0, gIndex + 1);
            char letterAfter = word[gIndex + 1];
            string after = word.Substring(gIndex + 2);
            return $"{before}<b><color=#{colorHex}><u>{letterAfter}</u></color></b>{after}";
        }

        return word;
    }

    private void LoadCurrentWord(bool playAudio = true)
    {
        StopButtonPulse();

        if (_currentIndex < 0 || _currentIndex >= words.Count)
        {
            OnAllWordsCompleted();
            return;
        }

        SoftOrHardWord currentWord = words[_currentIndex];

        if (wordTextLabel != null)
        {
            string formatted = string.IsNullOrEmpty(currentWord.highlightedWordText)
                ? FormatWordWithHighlight(currentWord.wordText, ColorUtility.ToHtmlStringRGB(highlightColor))
                : currentWord.highlightedWordText;
            wordTextLabel.text = formatted;
        }

        if (wordImage != null)
        {
            if (currentWord.wordSprite != null)
            {
                wordImage.sprite = currentWord.wordSprite;
                wordImage.gameObject.SetActive(true);
            }
            else
            {
                wordImage.gameObject.SetActive(false);
            }
        }

        if (instructionLabel != null)
        {
            instructionLabel.text = "Is 'g' soft /j/ or hard /g/? Tap the correct sound!";
        }

        if (titleTextLabel != null)
        {
            titleTextLabel.text = "Soft or Hard?";
        }

        if (softGButton != null) softGButton.transform.localScale = _softBtnOriginalScale;
        if (hardGButton != null) hardGButton.transform.localScale = _hardBtnOriginalScale;

        UpdateProgressUI();
        
        if (playAudio)
        {
            ReplayCurrentWordAudio();
        }

        _canTap = !voiceAudioSourceIsPlayingIntro(); // set canTap true only if intro is not playing
    }

    private bool voiceAudioSourceIsPlayingIntro()
    {
        if (mascotAudioSource == null || introAudio == null) return false;
        return mascotAudioSource.isPlaying && mascotAudioSource.clip == introAudio;
    }

    private void OnChoiceSelected(bool tappedSoft)
    {
        if (!_canTap || _currentIndex < 0 || _currentIndex >= words.Count) return;

        SoftOrHardWord currentWord = words[_currentIndex];
        bool isCorrect = (tappedSoft == currentWord.isSoftG);

        if (isCorrect)
        {
            HandleCorrectAnswer();
        }
        else
        {
            HandleWrongAnswer(currentWord.isSoftG);
        }
    }

    private void HandleCorrectAnswer()
    {
        _canTap = false;
        _score++;

        StopButtonPulse();

        if (sfxAudioSource != null && correctSFX != null)
        {
            sfxAudioSource.PlayOneShot(correctSFX);
        }

        if (starEffectObject != null)
        {
            starEffectObject.SetActive(true);
            StartCoroutine(HideStarEffectAfterDelay(1.2f));
        }

        if (mascotCharacter != null)
        {
            StartCoroutine(AnimateMascotBounce());
        }

        UpdateProgressUI();

        StartCoroutine(NextWordCoroutine(1.3f));
    }

    private void HandleWrongAnswer(bool correctIsSoft)
    {
        if (sfxAudioSource != null && wrongSFX != null)
        {
            sfxAudioSource.PlayOneShot(wrongSFX);
        }

        // Word repeats audio on wrong answer
        ReplayCurrentWordAudio();

        // Correct button pulses to visually guide child (No lives lost)
        Button correctButton = correctIsSoft ? softGButton : hardGButton;
        if (correctButton != null)
        {
            StartButtonPulse(correctButton);
        }

        // Re-highlight word letter after 'g'
        if (wordTextLabel != null && _currentIndex >= 0 && _currentIndex < words.Count)
        {
            SoftOrHardWord currentWord = words[_currentIndex];
            wordTextLabel.text = FormatWordWithHighlight(currentWord.wordText, ColorUtility.ToHtmlStringRGB(highlightColor));
        }
    }

    private void ReplayCurrentWordAudio()
    {
        if (_currentIndex < 0 || _currentIndex >= words.Count) return;
        SoftOrHardWord word = words[_currentIndex];

        if (mascotAudioSource != null && word.wordAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.PlayOneShot(word.wordAudio);
        }
    }

    private IEnumerator NextWordCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        _currentIndex++;
        LoadCurrentWord();
    }

    private IEnumerator HideStarEffectAfterDelay(float delay)
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

        Vector3 originalScale = mascotCharacter.localScale;
        Vector3 targetScale = originalScale * 1.15f;

        float elapsedTime = 0f;
        float duration = 0.2f;

        while (elapsedTime < duration)
        {
            mascotCharacter.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            mascotCharacter.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        mascotCharacter.localScale = originalScale;
    }

    private void StartButtonPulse(Button btn)
    {
        StopButtonPulse();
        _pulseCoroutine = StartCoroutine(PulseButtonCoroutine(btn.transform));
    }

    private void StopButtonPulse()
    {
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }

        if (softGButton != null) softGButton.transform.localScale = _softBtnOriginalScale;
        if (hardGButton != null) hardGButton.transform.localScale = _hardBtnOriginalScale;
    }

    private IEnumerator PulseButtonCoroutine(Transform btnTransform)
    {
        Vector3 baseScale = btnTransform.localScale;
        float speed = 5f;

        while (true)
        {
            float scale = 1f + Mathf.Sin(Time.time * speed) * 0.12f;
            btnTransform.localScale = baseScale * scale;
            yield return null;
        }
    }

    private void InitializeProgressDots()
    {
        if (progressDotsContainer == null) return;

        if (progressDotPrefab == null)
        {
            Transform templateTrans = progressDotsContainer.Find("ProgressDotTemplate");
            if (templateTrans == null) templateTrans = progressDotsContainer.Find("DotTemplate");
            if (templateTrans != null) progressDotPrefab = templateTrans.gameObject;
        }

        foreach (Transform child in progressDotsContainer)
        {
            if (progressDotPrefab == null || child.gameObject != progressDotPrefab)
            {
                Destroy(child.gameObject);
            }
        }
        _dotInstances.Clear();

        if (progressDotPrefab != null)
        {
            progressDotPrefab.SetActive(false);
        }

        for (int i = 0; i < words.Count; i++)
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
                RectTransform dotRt = dotObj.GetComponent<RectTransform>();
                dotRt.sizeDelta = new Vector2(22f, 22f);
                Image dotImg = dotObj.GetComponent<Image>();
                dotImg.color = dotEmptyColor;
            }
            _dotInstances.Add(dotObj);
        }

        UpdateProgressDots();
    }

    private void UpdateProgressDots()
    {
        for (int i = 0; i < _dotInstances.Count; i++)
        {
            Image dotImage = _dotInstances[i].GetComponent<Image>();
            if (dotImage != null)
            {
                if (i < _currentIndex)
                {
                    if (dotFilledSprite != null) dotImage.sprite = dotFilledSprite;
                    dotImage.color = dotFilledColor;
                }
                else
                {
                    if (dotEmptySprite != null) dotImage.sprite = dotEmptySprite;
                    dotImage.color = dotEmptyColor;
                }
            }
        }
    }

    private void UpdateProgressUI()
    {
        if (scoreLabel != null)
        {
            scoreLabel.text = $"Score: {_score}";
        }

        if (progressLabel != null)
        {
            progressLabel.text = $"{_currentIndex + 1} / {words.Count}";
        }

        UpdateProgressDots();
    }

    private void OnAllWordsCompleted()
    {
        _canTap = false;

        if (sfxAudioSource != null && levelCompleteSFX != null)
        {
            sfxAudioSource.PlayOneShot(levelCompleteSFX);
        }

        if (unitCompleteScreen != null)
        {
            unitCompleteScreen.SetActive(true);
        }

        if (globalNextButton != null)
        {
            globalNextButton.SetActive(true);
        }

        onLevelComplete?.Invoke();

        if (_flowManager != null)
        {
            StartCoroutine(DelayNextGameplay());
        }
    }

    private IEnumerator DelayNextGameplay()
    {
        float delay = 2.0f;
        if (unitCompleteAudio != null)
        {
            if (mascotAudioSource != null)
            {
                mascotAudioSource.PlayOneShot(unitCompleteAudio);
            }
            delay = Mathf.Max(delay, unitCompleteAudio.length + 0.5f);
        }
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

    /// <summary>
    /// Attempts to auto-wire unassigned UI references in the scene hierarchy.
    /// </summary>
    public void AutoWireUI()
    {
        if (softGButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string name = b.gameObject.name.ToLower();
                if (name.Contains("soft") || name.Contains("/j/"))
                {
                    softGButton = b;
                    break;
                }
            }
        }

        if (hardGButton == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                string name = b.gameObject.name.ToLower();
                if (name.Contains("hard") || name.Contains("/g/"))
                {
                    hardGButton = b;
                    break;
                }
            }
        }

        if (wordTextLabel == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                string name = t.gameObject.name.ToLower();
                if (name.Contains("word") || name.Contains("target") || name.Contains("display"))
                {
                    wordTextLabel = t;
                    break;
                }
            }
        }

        if (instructionLabel == null)
        {
            TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                string name = t.gameObject.name.ToLower();
                if (name.Contains("instruction") || name.Contains("prompt"))
                {
                    instructionLabel = t;
                    break;
                }
            }
        }

        if (wordImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                string name = img.gameObject.name.ToLower();
                if (name.Contains("wordimage") || name.Contains("picture") || name.Contains("illustration"))
                {
                    wordImage = img;
                    break;
                }
            }
        }

        if (sfxAudioSource == null)
        {
            AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
            if (sources.Length > 0) sfxAudioSource = sources[0];
            if (sources.Length > 1) mascotAudioSource = sources[1];
        }

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
    }
}
