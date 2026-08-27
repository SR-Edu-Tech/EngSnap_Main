using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EchoSentenceData
{
    [Tooltip("The vowel category (e.g., Short a, Long e)")]
    public string vowelCategory;

    [Tooltip("The sentence text to read")]
    public string sentenceText;

    [Tooltip("Words in the sentence that represent the target vowel sound and should glow in color")]
    public string[] targetVowelWords;

    [Tooltip("The illustration for the sentence")]
    public Sprite sentencePicture;

    [Tooltip("The audio clip of the mascot reading the sentence")]
    public AudioClip readingAudio;

    [Tooltip("Start times (in seconds) for each word in the audio clip. Optional.")]
    public float[] wordStartTimes;

    [Tooltip("Durations (in seconds) for each word in the audio clip. Optional.")]
    public float[] wordDurations;
}

public class EchoRead_P_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;

    [Header("Mascot Intro Audio")]
    public AudioClip introAudio;
    [Header("Gameplay Config")]
    public List<EchoSentenceData> sentences = new List<EchoSentenceData>();
    
    [Range(0f, 1f)]
    public float passThreshold = 0.5f;

    [Header("UI Components")]
    public TextMeshProUGUI sentenceTextLabel;
    public Image sentenceImage;
    public TextMeshProUGUI vowelCategoryLabel;
    public TextMeshProUGUI progressLabel;
    public Slider progressSlider;
    public GameObject nextButton;
    public Button replayButton;
    public ToggleToAddButton_S1A micButton;
    public RectTransform mascotCharacter;
    
    [Header("Accuracy UI (Optional)")]
    public TextMeshProUGUI recognizedTextLabel;
    public TextMeshProUGUI accuracyPercentLabel;
    public Slider accuracySlider;
    public CanvasGroup accuracyGroup;

    [Header("Audio")]
    public AudioSource mascotAudioSource;
    public AudioSource sfxAudioSource;
    public AudioClip correctSFX;
    public AudioClip popSFX;

    [Header("Typography / Colors")]
    public string defaultColorHex = "#FFFFFF";       // White
    public string targetGlowColorHex = "#FF5722";     // Vibrant Orange
    public string highlightColorHex = "#FFD700";      // Gold

    [Header("Vowel Indicator UI")]
    public TextMeshProUGUI indicatorLetterLabel;
    public Image indicatorLetterImage;
    public Sprite[] indicatorVowelSprites;
    public TextMeshProUGUI indicatorNoteLabel;
    [Header("Vowel Indicator Colors")]
    public string vowelIndicatorRedColorHex = "#A03020";

    // Runtime state
    private int _currentIndex = 0;
    private bool _started = false;
    private Coroutine _karaokeCoroutine;
    private Vector3 _originalMascotScale = Vector3.one;
    private GameFlowManager_Senior_Phonics _flowManager;

    private void Awake()
    {
        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
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
    }

    private void Start()
    {
        _started = true;

        if (nextButton != null)
        {
            var btn = nextButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextClicked);
            }
            nextButton.SetActive(false);
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(OnReplayClicked);
        }

        ResetAccuracyUI();
        ResetToStart();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Spacebar bypass: Pressing Space in Editor simulates a successful speech result.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[EchoRead Bypass] Spacebar pressed. Simulating 100% correct speech transcript.");
            EvaluateSpeechAccuracy(sentences[_currentIndex].sentenceText);
        }
    }
#endif

    private void OnEnable()
    {
        CrossPlatformSpeechManager.OnResultStatic += HandleSpeechResult;
        CrossPlatformSpeechManager.OnPartialStatic += HandleSpeechPartial;
        CrossPlatformSpeechManager.OnRecordingReadyStatic += HandleRecordingReady;

        if (_started)
        {
            ResetToStart();
        }
    }

    private void OnDisable()
    {
        CrossPlatformSpeechManager.OnResultStatic -= HandleSpeechResult;
        CrossPlatformSpeechManager.OnPartialStatic -= HandleSpeechPartial;
        CrossPlatformSpeechManager.OnRecordingReadyStatic -= HandleRecordingReady;

        StopAllCoroutines();
        if (mascotAudioSource != null) mascotAudioSource.Stop();
    }

    public void ResetToStart()
    {
        _currentIndex = 0;
        LoadSentence(_currentIndex);
    }

    private void LoadSentence(int index)
    {
        _currentIndex = index;

        if (sentences == null || sentences.Count == 0)
        {
            Debug.LogWarning("[EchoRead] No sentences configured!");
            return;
        }

        if (index < 0 || index >= sentences.Count)
        {
            OnCompletedAll();
            return;
        }

        var data = sentences[index];

        // Update Vowel Indicator Panel
        string vowelLetter = GetVowelLetter(data.vowelCategory);
        if (indicatorLetterLabel != null)
        {
            indicatorLetterLabel.text = vowelLetter;
        }
        if (indicatorLetterImage != null && indicatorVowelSprites != null && indicatorVowelSprites.Length > 0)
        {
            int spriteIndex = GetVowelSpriteIndex(vowelLetter);
            if (spriteIndex >= 0 && spriteIndex < indicatorVowelSprites.Length)
            {
                indicatorLetterImage.sprite = indicatorVowelSprites[spriteIndex];
            }
        }
        if (indicatorNoteLabel != null)
        {
            string formattedCategory = FormatCategoryForNote(data.vowelCategory);
            indicatorNoteLabel.text = $"We are learning <color={vowelIndicatorRedColorHex}>{formattedCategory}</color> sound.";
        }

        // 1. Update text and images
        if (vowelCategoryLabel != null) vowelCategoryLabel.text = data.vowelCategory;
        if (sentenceImage != null && data.sentencePicture != null)
        {
            sentenceImage.sprite = data.sentencePicture;
            sentenceImage.gameObject.SetActive(true);
            
            // Fade in picture nicely using LeanTween
            sentenceImage.transform.localScale = Vector3.zero;
            LeanTween.cancel(sentenceImage.gameObject);
            LeanTween.scale(sentenceImage.gameObject, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
        }
        else if (sentenceImage != null)
        {
            sentenceImage.gameObject.SetActive(false);
        }

        // 2. Format sentence default text (glow words in color)
        UpdateSentenceText(-1);

        // 3. Update progress UI
        if (progressLabel != null)
        {
            progressLabel.text = $"Sentence {index + 1} / {sentences.Count}";
        }
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = sentences.Count;
            progressSlider.value = index + 1;
        }

        // 4. Reset status UIs
        ResetAccuracyUI();
        if (nextButton != null) nextButton.SetActive(false);
        if (micButton != null) micButton.ForceIdle();

        // 5. Play popup sound
        if (sfxAudioSource != null && popSFX != null)
        {
            sfxAudioSource.PlayOneShot(popSFX);
        }

        // 6. Mascot scales in and starts reading
        if (mascotCharacter != null)
        {
            mascotCharacter.localScale = Vector3.zero;
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.45f)
                .setEase(LeanTweenType.easeOutBack)
                .setOnComplete(() => StartCoroutine(IntroAndStartFlow(data)));
        }
        else
        {
            StartCoroutine(IntroAndStartFlow(data));
        }
    }

    private void PlayMascotReading(EchoSentenceData data)
    {
        if (_karaokeCoroutine != null) StopCoroutine(_karaokeCoroutine);

        if (data.readingAudio != null)
        {
            mascotAudioSource.Stop();
            mascotAudioSource.clip = data.readingAudio;
            mascotAudioSource.Play();
            
            _karaokeCoroutine = StartCoroutine(KaraokeSyncFlow(data));
        }
        else
        {
            // Fallback if no audio clip: just highlight word-by-word with basic timer
            _karaokeCoroutine = StartCoroutine(KaraokeFallbackFlow(data.sentenceText));
        }
    }

    private IEnumerator KaraokeSyncFlow(EchoSentenceData data)
    {
        string[] words = data.sentenceText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        int wordCount = words.Length;

        // Bounce mascot slightly while talking
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.06f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt(data.readingAudio.length / 0.5f));
        }

        // Determine timings
        float[] starts = data.wordStartTimes;
        float[] durs = data.wordDurations;

        // If timing lists are unassigned or sized incorrectly, fallback to automatic split
        if (starts == null || starts.Length < wordCount || durs == null || durs.Length < wordCount)
        {
            float totalLen = data.readingAudio.length;
            float perWord = totalLen / wordCount;
            starts = new float[wordCount];
            durs = new float[wordCount];
            for (int i = 0; i < wordCount; i++)
            {
                starts[i] = i * perWord;
                durs[i] = perWord;
            }
        }

        int lastWordIndex = -1;
        while (mascotAudioSource.isPlaying)
        {
            float time = mascotAudioSource.time;
            int activeIndex = -1;

            for (int i = 0; i < wordCount; i++)
            {
                if (time >= starts[i] && time <= (starts[i] + durs[i]))
                {
                    activeIndex = i;
                    break;
                }
            }

            // If time is between words, keep the last word highlighted until a new one starts
            if (activeIndex == -1 && lastWordIndex != -1)
            {
                activeIndex = lastWordIndex;
            }

            if (activeIndex != lastWordIndex)
            {
                lastWordIndex = activeIndex;
                UpdateSentenceText(activeIndex);
            }

            yield return null;
        }

        // Done reading
        UpdateSentenceText(-1);
        
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }
    }

    private IEnumerator KaraokeFallbackFlow(string sentenceText)
    {
        string[] words = sentenceText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        int wordCount = words.Length;
        float delayPerWord = 0.4f;

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.06f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt((wordCount * delayPerWord) / 0.5f));
        }

        for (int i = 0; i < wordCount; i++)
        {
            UpdateSentenceText(i);
            yield return new WaitForSeconds(delayPerWord);
        }

        UpdateSentenceText(-1);

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }
    }

    private void UpdateSentenceText(int highlightedIndex)
    {
        if (sentenceTextLabel == null) return;

        var data = sentences[_currentIndex];
        string[] words = data.sentenceText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            string clean = CleanWord(word);

            bool isTarget = false;
            if (data.targetVowelWords != null)
            {
                foreach (string tw in data.targetVowelWords)
                {
                    if (string.Equals(clean, CleanWord(tw), StringComparison.OrdinalIgnoreCase))
                    {
                        isTarget = true;
                        break;
                    }
                }
            }

            string formatted;
            if (i == highlightedIndex)
            {
                // Active karaoke highlight
                formatted = $"<color={highlightColorHex}><b>{word}</b></color>";
            }
            else if (isTarget)
            {
                // Target word glowing in color
                formatted = $"<color={targetGlowColorHex}><u>{word}</u></color>";
            }
            else
            {
                // Normal text
                formatted = $"<color={defaultColorHex}>{word}</color>";
            }

            sb.Append(formatted);
            if (i < words.Length - 1) sb.Append(" ");
        }

        sentenceTextLabel.text = sb.ToString();
    }

    private string CleanWord(string word)
    {
        if (string.IsNullOrEmpty(word)) return "";
        var sb = new StringBuilder();
        foreach (char c in word)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }
        return sb.ToString().ToLowerInvariant();
    }

    // ── Speech Events ─────────────────────────────────────────────────────────

    private void HandleSpeechResult(string transcript)
    {
        if (recognizedTextLabel != null)
        {
            recognizedTextLabel.color = new Color32(32, 63, 10, 255);
            recognizedTextLabel.text = transcript;
        }

        EvaluateSpeechAccuracy(transcript);
    }

    private void HandleSpeechPartial(string partial)
    {
        if (recognizedTextLabel != null)
        {
            recognizedTextLabel.color = Color.yellow;
            recognizedTextLabel.text = partial;
        }
    }

    private void HandleRecordingReady()
    {
        // Handled in ToggleToAddButton
    }

    private void EvaluateSpeechAccuracy(string hypothesis)
    {
        if (sentences == null || _currentIndex >= sentences.Count) return;

        string reference = sentences[_currentIndex].sentenceText;
        float score = SimilarityPercent(reference, hypothesis);

        if (accuracySlider != null) accuracySlider.value = score;
        if (accuracyPercentLabel != null) accuracyPercentLabel.text = Mathf.RoundToInt(score * 100f) + "%";

        ShowAccuracyGroup();

        if (score >= passThreshold)
        {
            // Celebrate success!
            if (sfxAudioSource != null && correctSFX != null)
            {
                sfxAudioSource.PlayOneShot(correctSFX);
            }

            if (mascotCharacter != null)
            {
                LeanTween.cancel(mascotCharacter.gameObject);
                LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.15f, 0.2f)
                    .setEase(LeanTweenType.easeOutQuad)
                    .setLoopPingPong(1);
            }

            // Stop listening before moving on
            CrossPlatformSpeechManager.Instance?.StopListening();
            if (micButton != null) micButton.ForceIdle();

            // Show and scale in Next button
            if (nextButton != null)
            {
                nextButton.SetActive(true);
                nextButton.transform.localScale = Vector3.zero;
                LeanTween.cancel(nextButton);
                LeanTween.scale(nextButton, Vector3.one, 0.35f).setEase(LeanTweenType.easeOutBack);
            }
        }
    }

    // ── UI Button Handlers ─────────────────────────────────────────────────────

    private void OnReplayClicked()
    {
        if (sentences == null || _currentIndex >= sentences.Count) return;
        
        CrossPlatformSpeechManager.Instance?.StopListening();
        if (micButton != null) micButton.ForceIdle();

        PlayMascotReading(sentences[_currentIndex]);
    }

    private void OnNextClicked()
    {
        CrossPlatformSpeechManager.Instance?.StopListening();
        if (micButton != null) micButton.ForceIdle();

        int nextIndex = _currentIndex + 1;
        if (nextIndex < sentences.Count)
        {
            LoadSentence(nextIndex);
        }
        else
        {
            OnCompletedAll();
        }
    }

    private void OnCompletedAll()
    {
        Debug.Log("[EchoRead] Completed all sentences!");
        if (unitCompleteAudio != null)
        {
            if (mascotAudioSource != null)
            {
                mascotAudioSource.PlayOneShot(unitCompleteAudio);
            }
            StartCoroutine(DelayNextGameplay(unitCompleteAudio.length + 0.5f));
        }
        else
        {
            TriggerNextGameplay();
        }
    }

    private IEnumerator DelayNextGameplay(float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerNextGameplay();
    }

    private void TriggerNextGameplay()
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

    // ── Similarity Calculation (Levenshtein) ───────────────────────────────────

    private float SimilarityPercent(string reference, string hypothesis)
    {
        string a = Normalize(reference);
        string b = Normalize(hypothesis);
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
        if (a == b) return 1f;
        int dist = Levenshtein(a, b);
        int maxLen = Mathf.Max(a.Length, b.Length);
        return 1f - (float)dist / maxLen;
    }

    private string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                sb.Append(c);
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    private int Levenshtein(string s, string t)
    {
        int n = s.Length, m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
        {
            char si = s[i - 1];
            for (int j = 1; j <= m; j++)
            {
                int cost = (si == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    private void ResetAccuracyUI()
    {
        if (accuracySlider != null) accuracySlider.value = 0f;
        if (accuracyPercentLabel != null) accuracyPercentLabel.text = "";
        if (recognizedTextLabel != null) recognizedTextLabel.text = "";
        HideAccuracyGroup();
    }

    private void HideAccuracyGroup()
    {
        if (accuracyGroup == null) return;
        accuracyGroup.alpha = 0f; 
        accuracyGroup.interactable = false; 
        accuracyGroup.blocksRaycasts = false;
    }

    private void ShowAccuracyGroup()
    {
        if (accuracyGroup == null) return;
        accuracyGroup.alpha = 1f; 
        accuracyGroup.interactable = true; 
        accuracyGroup.blocksRaycasts = true;
    }

    private string GetVowelLetter(string category)
    {
        if (string.IsNullOrEmpty(category)) return "A";
        string lower = category.ToLowerInvariant();
        if (lower.Contains("a")) return "A";
        if (lower.Contains("e")) return "E";
        if (lower.Contains("i")) return "I";
        if (lower.Contains("o")) return "O";
        if (lower.Contains("u")) return "U";
        return "A";
    }

    private int GetVowelSpriteIndex(string vowelLetter)
    {
        switch (vowelLetter.ToUpperInvariant())
        {
            case "A": return 0;
            case "E": return 1;
            case "I": return 2;
            case "O": return 3;
            case "U": return 4;
            default: return 0;
        }
    }

    private string FormatCategoryForNote(string category)
    {
        if (string.IsNullOrEmpty(category)) return "";
        string[] parts = category.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length > 0)
            {
                if (parts[i].Length == 1)
                {
                    parts[i] = parts[i].ToUpperInvariant();
                }
                else
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1).ToLowerInvariant();
                    if (i == parts.Length - 1)
                    {
                        parts[i] = parts[i].ToUpperInvariant();
                    }
                }
            }
        }
        return string.Join(" ", parts);
    }

    private IEnumerator IntroAndStartFlow(EchoSentenceData data)
    {
        if (_currentIndex == 0 && introAudio != null && mascotAudioSource != null)
        {
            mascotAudioSource.clip = introAudio;
            mascotAudioSource.Play();
            yield return StartCoroutine(MascotTalkAnimation(introAudio.length));
        }
        PlayMascotReading(data);
    }

    private IEnumerator MascotTalkAnimation(float duration)
    {
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.06f, 0.25f)
                .setLoopPingPong(Mathf.CeilToInt(duration / 0.5f));
        }

        yield return new WaitForSeconds(duration);

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale, 0.2f);
        }
    }

}