using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class WordHighlightTiming
{
    [Tooltip("The word text to display and highlight")]
    public string word;

    [Tooltip("Start time of this word relative to the line start (seconds)")]
    public float relativeTime;

    [Tooltip("Duration of this word's highlight/note (seconds)")]
    public float duration;

    [Tooltip("Syllable tone frequency (Hz) for procedural synth fallback (C Major: C4=261.6, D4=293.7, E4=329.6, F4=349.2, G4=392.0, A4=440.0)")]
    public float frequency;
}

[System.Serializable]
public class LyricLineData
{
    [Tooltip("The full line text as a reference")]
    public string lineText;

    [Tooltip("Individual word configurations and timings")]
    public List<WordHighlightTiming> words = new List<WordHighlightTiming>();

    [Tooltip("Total duration of the entire line (seconds)")]
    public float lineDuration;

    [Tooltip("Optional audio clip for just this line (e.g. generated from TTS)")]
    public AudioClip lineAudio;
}

public class MagicESong_Unit9_Senior : MonoBehaviour
{
    [Header("Unit Complete Mascot Audio")]
    public AudioClip unitCompleteAudio;
    [Header("UI References")]
    [Tooltip("Text component displaying the song title")]
    public TextMeshProUGUI titleTextLabel;

    [Tooltip("Text component displaying the highlighted lyrics")]
    public TextMeshProUGUI lyricsTextLabel;

    [Tooltip("The mascot character to animate ( Cindy or Car )")]
    public RectTransform mascotCharacter;

    [Tooltip("Button to progress to the next gameplay screen")]
    public GameObject nextButton;

    [Tooltip("Button to play/replay the song")]
    public Button playReplayButton;

    [Header("Audio Settings")]
    [Tooltip("Main AudioSource used for the vocal song track")]
    public AudioSource voiceAudioSource;

    [Tooltip("AudioSource used for sound effects and procedural tones")]
    public AudioSource sfxAudioSource;

    [Tooltip("AudioSource used for the background instrumental/music track")]
    public AudioSource bgmAudioSource;

    [Tooltip("Optional vocal/instrumental song track. If null, a procedural music box melody plays.")]
    public AudioClip songAudio;

    [Tooltip("Optional intro prompt clip (e.g. Mascot saying 'Let's sing the Magic e song!')")]
    public AudioClip introPromptAudio;

    [Header("UI Colors & Styling")]
    [Tooltip("Color of normal/inactive lyrics lines")]
    public Color inactiveLineColor = new Color32(176, 190, 197, 255); // #B0BEC5

    [Tooltip("Color of the currently active lyrics line")]
    public Color activeLineColor = new Color32(255, 255, 255, 255); // #FFFFFF

    [Tooltip("Color used to highlight the active word")]
    public Color activeWordColor = new Color32(255, 51, 102, 255); // #FF3366

    [Header("Configuration")]
    [Tooltip("List of song lines with detailed word timings. Pre-populated automatically if empty.")]
    public List<LyricLineData> lyricLines = new List<LyricLineData>();

    [Tooltip("Delay in seconds before the song starts")]
    public float startDelay = 1.0f;

    [Tooltip("Delay in seconds before activating the Next button after the song ends")]
    public float completionDelay = 1.5f;

    // Runtime state
    private Coroutine _songCoroutine;
    private bool _isPlaying = false;
    private Vector3 _originalMascotScale = Vector3.one;
    private GameFlowManager_Senior_Phonics _flowManager;
    private bool _started = false;

    private void Awake()
    {
        _flowManager = FindFirstObjectByType<GameFlowManager_Senior_Phonics>();

        if (mascotCharacter != null)
        {
            _originalMascotScale = mascotCharacter.localScale;
        }

        if (playReplayButton != null)
        {
            playReplayButton.onClick.RemoveAllListeners();
            playReplayButton.onClick.AddListener(OnPlayReplayClicked);
        }

        if (nextButton != null)
        {
            Button btn = nextButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextButtonClicked);
            }
        }

        // Dynamically find AudioSources if not set
        if (voiceAudioSource == null) voiceAudioSource = GetComponent<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = GetComponent<AudioSource>();
        if (voiceAudioSource == null) voiceAudioSource = FindFirstObjectByType<AudioSource>();
        if (sfxAudioSource == null) sfxAudioSource = FindFirstObjectByType<AudioSource>();
    }

    private void Start()
    {
        _started = true;
        ResetActivity();
        StartPlayback();
    }

    private void OnEnable()
    {
        if (_started)
        {
            ResetActivity();
            StartPlayback();
        }
    }

    private void OnDisable()
    {
        StopPlayback();
    }

    public void ResetActivity()
    {
        StopPlayback();

        if (lyricLines == null || lyricLines.Count == 0)
        {
            InitializeDefaultLyrics();
        }

        if (nextButton != null)
        {
            nextButton.SetActive(false);
        }

        if (mascotCharacter != null)
        {
            mascotCharacter.localScale = _originalMascotScale;
            LeanTween.cancel(mascotCharacter.gameObject);
        }

        UpdateLyricsDisplay(-1, -1);
    }

    public void StartPlayback()
    {
        StopPlayback();
        _songCoroutine = StartCoroutine(PlaySongSequence());
    }

    public void StopPlayback()
    {
        _isPlaying = false;
        if (_songCoroutine != null)
        {
            StopCoroutine(_songCoroutine);
            _songCoroutine = null;
        }

        if (voiceAudioSource != null) voiceAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
        if (bgmAudioSource != null) bgmAudioSource.Stop();

        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            mascotCharacter.localScale = _originalMascotScale;
        }
    }

    private void OnPlayReplayClicked()
    {
        StartPlayback();
    }

    private IEnumerator PlaySongSequence()
    {
        _isPlaying = true;

        if (nextButton != null) nextButton.SetActive(false);

        // 1. Play intro voice prompt (if any)
        if (introPromptAudio != null && voiceAudioSource != null)
        {
            voiceAudioSource.PlayOneShot(introPromptAudio);
            yield return new WaitForSeconds(introPromptAudio.length + 0.5f);
        }
        else
        {
            yield return new WaitForSeconds(startDelay);
        }

        // 2. Play backing track / song vocal if provided
        if (songAudio != null)
        {
            AudioSource targetBgmSource = bgmAudioSource != null ? bgmAudioSource : sfxAudioSource;
            if (targetBgmSource != null)
            {
                targetBgmSource.clip = songAudio;
                targetBgmSource.loop = true;
                targetBgmSource.volume = 0.25f; // Play backing track softly
                targetBgmSource.Play();
            }
        }

        float lineStartTime;

        // 3. Play lyric sequence
        for (int lineIndex = 0; lineIndex < lyricLines.Count; lineIndex++)
        {
            LyricLineData line = lyricLines[lineIndex];
            lineStartTime = Time.time;

            // If this line has its own audio clip (e.g. from TTS generator), play it!
            if (line.lineAudio != null && voiceAudioSource != null)
            {
                voiceAudioSource.clip = line.lineAudio;
                voiceAudioSource.Play();
            }

            // Highlight words sequentially inside this line
            for (int wordIndex = 0; wordIndex < line.words.Count; wordIndex++)
            {
                WordHighlightTiming wordTiming = line.words[wordIndex];

                // Wait until it's time to highlight this word
                float targetWordTime = lineStartTime + wordTiming.relativeTime;
                while (Time.time < targetWordTime && _isPlaying)
                {
                    yield return null;
                }

                if (!_isPlaying) yield break;

                // Highlight active word
                UpdateLyricsDisplay(lineIndex, wordIndex);

                // Play synth bell tone as a music box fallback if no song audio or line audio track is playing
                if (songAudio == null && line.lineAudio == null && sfxAudioSource != null && wordTiming.frequency > 0)
                {
                    PlayMusicBoxTone(wordTiming.frequency, wordTiming.duration);
                }

                // Bounce/animate mascot character on each highlighted word/note
                BounceMascot();

                // Keep highlighted for the specified word duration
                float targetHighlightEndTime = targetWordTime + wordTiming.duration;
                while (Time.time < targetHighlightEndTime && _isPlaying)
                {
                    yield return null;
                }

                if (!_isPlaying) yield break;
            }

            // Wait for remaining line duration if any
            float lineEndTime = lineStartTime + line.lineDuration;
            while (Time.time < lineEndTime && _isPlaying)
            {
                yield return null;
            }

            if (!_isPlaying) yield break;
        }

        // Clear highlights at the end of the song
        UpdateLyricsDisplay(-1, -1);
        _isPlaying = false;

        // 4. Activate Next button with LeanTween bounce
        yield return new WaitForSeconds(completionDelay);

        if (nextButton != null)
        {
            if (unitCompleteAudio != null && voiceAudioSource != null) voiceAudioSource.PlayOneShot(unitCompleteAudio);
            nextButton.SetActive(true);
            nextButton.transform.localScale = Vector3.zero;
            LeanTween.scale(nextButton, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack);
        }
    }

    private void BounceMascot()
    {
        if (mascotCharacter != null)
        {
            LeanTween.cancel(mascotCharacter.gameObject);
            mascotCharacter.localScale = _originalMascotScale;
            LeanTween.scale(mascotCharacter.gameObject, _originalMascotScale * 1.15f, 0.15f)
                .setLoopPingPong(1)
                .setEase(LeanTweenType.easeInOutQuad);
        }
    }

    private void UpdateLyricsDisplay(int activeLineIndex, int activeWordIndex)
    {
        if (lyricsTextLabel == null) return;

        string inactiveHex = "#" + ColorUtility.ToHtmlStringRGB(inactiveLineColor);
        string activeLineHex = "#" + ColorUtility.ToHtmlStringRGB(activeLineColor);
        string activeWordHex = "#" + ColorUtility.ToHtmlStringRGB(activeWordColor);

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < lyricLines.Count; i++)
        {
            LyricLineData line = lyricLines[i];
            if (i == activeLineIndex)
            {
                // Highlight the active line
                sb.Append($"<b><size=115%><color={activeLineHex}>");
                for (int w = 0; w < line.words.Count; w++)
                {
                    if (w == activeWordIndex)
                    {
                        // Active word is highlighted and underlined
                        sb.Append($"<color={activeWordHex}><u>{line.words[w].word}</u></color>");
                    }
                    else
                    {
                        sb.Append(line.words[w].word);
                    }
                    if (w < line.words.Count - 1) sb.Append(" ");
                }
                sb.Append("</color></size></b>");
            }
            else
            {
                // Muted/grayed out non-active lines
                sb.Append($"<color={inactiveHex}>");
                for (int w = 0; w < line.words.Count; w++)
                {
                    sb.Append(line.words[w].word);
                    if (w < line.words.Count - 1) sb.Append(" ");
                }
                sb.Append("</color>");
            }
            sb.Append("\n\n");
        }
        lyricsTextLabel.text = sb.ToString();
    }

    private void PlayMusicBoxTone(float frequency, float duration)
    {
        if (sfxAudioSource == null) return;

        AudioClip clip = CreateMusicBoxToneClip(frequency, duration);
        sfxAudioSource.PlayOneShot(clip);
    }

    private AudioClip CreateMusicBoxToneClip(float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float attackTime = 0.03f;
        float decayTime = duration - attackTime;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float amplitude = 1.0f;

            if (t < attackTime)
            {
                // Smooth attack fade-in
                amplitude = t / attackTime;
            }
            else
            {
                // Exponential decay fade-out
                float decayProgress = (t - attackTime) / decayTime;
                amplitude = Mathf.Exp(-4.0f * decayProgress);
            }

            // Sine wave tone with volume scaling
            samples[i] = Mathf.Sin(2.0f * Mathf.PI * frequency * t) * amplitude * 0.2f;
        }

        AudioClip clip = AudioClip.Create("MusicBoxTone_" + frequency, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnNextButtonClicked()
    {
        if (_flowManager != null)
        {
            _flowManager.NextGameplay();
        }
        else
        {
            Debug.LogWarning("[MagicESong_Unit9_Senior] GameFlowManager not found. Restarting.");
            ResetActivity();
            StartPlayback();
        }
    }

    private void InitializeDefaultLyrics()
    {
        lyricLines = new List<LyricLineData>();

        // Note Frequencies:
        float C4 = 261.63f;
        float D4 = 293.66f;
        float E4 = 329.63f;
        float F4 = 349.23f;
        float G4 = 392.00f;
        float A4 = 440.00f;

        // Line 1: Magic 'e' Magic 'e'
        LyricLineData l1 = new LyricLineData { lineText = "Magic 'e' Magic 'e'", lineDuration = 4.2f };
        l1.words.Add(new WordHighlightTiming { word = "Magic", relativeTime = 0.0f, duration = 0.8f, frequency = C4 });
        l1.words.Add(new WordHighlightTiming { word = "'e'", relativeTime = 0.8f, duration = 1.0f, frequency = G4 });
        l1.words.Add(new WordHighlightTiming { word = "Magic", relativeTime = 1.8f, duration = 0.8f, frequency = G4 });
        l1.words.Add(new WordHighlightTiming { word = "'e'", relativeTime = 2.6f, duration = 1.2f, frequency = A4 });
        lyricLines.Add(l1);

        // Line 2: The end of a word is where I'll be!
        LyricLineData l2 = new LyricLineData { lineText = "The end of a word is where I'll be!", lineDuration = 4.5f };
        l2.words.Add(new WordHighlightTiming { word = "The", relativeTime = 0.0f, duration = 0.4f, frequency = F4 });
        l2.words.Add(new WordHighlightTiming { word = "end", relativeTime = 0.4f, duration = 0.4f, frequency = F4 });
        l2.words.Add(new WordHighlightTiming { word = "of", relativeTime = 0.8f, duration = 0.4f, frequency = E4 });
        l2.words.Add(new WordHighlightTiming { word = "a", relativeTime = 1.2f, duration = 0.4f, frequency = E4 });
        l2.words.Add(new WordHighlightTiming { word = "word", relativeTime = 1.6f, duration = 0.4f, frequency = D4 });
        l2.words.Add(new WordHighlightTiming { word = "is", relativeTime = 2.0f, duration = 0.4f, frequency = D4 });
        l2.words.Add(new WordHighlightTiming { word = "where", relativeTime = 2.4f, duration = 0.4f, frequency = C4 });
        l2.words.Add(new WordHighlightTiming { word = "I'll", relativeTime = 2.8f, duration = 0.4f, frequency = C4 });
        l2.words.Add(new WordHighlightTiming { word = "be!", relativeTime = 3.2f, duration = 1.0f, frequency = C4 });
        lyricLines.Add(l2);

        // Line 3: I don't don't say e or e
        LyricLineData l3 = new LyricLineData { lineText = "I don't don't say e or e", lineDuration = 4.2f };
        l3.words.Add(new WordHighlightTiming { word = "I", relativeTime = 0.0f, duration = 0.4f, frequency = G4 });
        l3.words.Add(new WordHighlightTiming { word = "don't", relativeTime = 0.4f, duration = 0.4f, frequency = G4 });
        l3.words.Add(new WordHighlightTiming { word = "don't", relativeTime = 0.8f, duration = 0.4f, frequency = F4 });
        l3.words.Add(new WordHighlightTiming { word = "say", relativeTime = 1.2f, duration = 0.5f, frequency = F4 });
        l3.words.Add(new WordHighlightTiming { word = "e", relativeTime = 1.7f, duration = 0.4f, frequency = E4 });
        l3.words.Add(new WordHighlightTiming { word = "or", relativeTime = 2.1f, duration = 0.4f, frequency = E4 });
        l3.words.Add(new WordHighlightTiming { word = "e", relativeTime = 2.5f, duration = 1.2f, frequency = D4 });
        lyricLines.Add(l3);

        // Line 4: I just sit there silently.
        LyricLineData l4 = new LyricLineData { lineText = "I just sit there silently.", lineDuration = 4.5f };
        l4.words.Add(new WordHighlightTiming { word = "I", relativeTime = 0.0f, duration = 0.4f, frequency = G4 });
        l4.words.Add(new WordHighlightTiming { word = "just", relativeTime = 0.4f, duration = 0.4f, frequency = G4 });
        l4.words.Add(new WordHighlightTiming { word = "sit", relativeTime = 0.8f, duration = 0.4f, frequency = F4 });
        l4.words.Add(new WordHighlightTiming { word = "there", relativeTime = 1.2f, duration = 0.5f, frequency = F4 });
        l4.words.Add(new WordHighlightTiming { word = "si-", relativeTime = 1.7f, duration = 0.4f, frequency = E4 });
        l4.words.Add(new WordHighlightTiming { word = "lent-", relativeTime = 2.1f, duration = 0.4f, frequency = E4 });
        l4.words.Add(new WordHighlightTiming { word = "ly.", relativeTime = 2.5f, duration = 1.5f, frequency = D4 });
        lyricLines.Add(l4);

        // Line 5: But the vowel that I'm around,
        LyricLineData l5 = new LyricLineData { lineText = "But the vowel that I'm around,", lineDuration = 4.5f };
        l5.words.Add(new WordHighlightTiming { word = "But", relativeTime = 0.0f, duration = 0.4f, frequency = G4 });
        l5.words.Add(new WordHighlightTiming { word = "the", relativeTime = 0.4f, duration = 0.4f, frequency = G4 });
        l5.words.Add(new WordHighlightTiming { word = "vowel", relativeTime = 0.8f, duration = 0.8f, frequency = F4 });
        l5.words.Add(new WordHighlightTiming { word = "that", relativeTime = 1.6f, duration = 0.4f, frequency = E4 });
        l5.words.Add(new WordHighlightTiming { word = "I'm", relativeTime = 2.0f, duration = 0.4f, frequency = E4 });
        l5.words.Add(new WordHighlightTiming { word = "a-", relativeTime = 2.4f, duration = 0.4f, frequency = D4 });
        l5.words.Add(new WordHighlightTiming { word = "round,", relativeTime = 2.8f, duration = 1.2f, frequency = D4 });
        lyricLines.Add(l5);

        // Line 6: Gets to make their long, long sound!
        LyricLineData l6 = new LyricLineData { lineText = "Gets to make their long, long sound!", lineDuration = 4.5f };
        l6.words.Add(new WordHighlightTiming { word = "Gets", relativeTime = 0.0f, duration = 0.4f, frequency = G4 });
        l6.words.Add(new WordHighlightTiming { word = "to", relativeTime = 0.4f, duration = 0.4f, frequency = G4 });
        l6.words.Add(new WordHighlightTiming { word = "make", relativeTime = 0.8f, duration = 0.4f, frequency = F4 });
        l6.words.Add(new WordHighlightTiming { word = "their", relativeTime = 1.2f, duration = 0.4f, frequency = F4 });
        l6.words.Add(new WordHighlightTiming { word = "long,", relativeTime = 1.6f, duration = 0.4f, frequency = E4 });
        l6.words.Add(new WordHighlightTiming { word = "long", relativeTime = 2.0f, duration = 0.4f, frequency = E4 });
        l6.words.Add(new WordHighlightTiming { word = "sound!", relativeTime = 2.4f, duration = 1.2f, frequency = D4 });
        lyricLines.Add(l6);

        // Line 7: Magic 'e' Magic 'e'
        LyricLineData l7 = new LyricLineData { lineText = "Magic 'e' Magic 'e'", lineDuration = 4.2f };
        l7.words.Add(new WordHighlightTiming { word = "Magic", relativeTime = 0.0f, duration = 0.8f, frequency = C4 });
        l7.words.Add(new WordHighlightTiming { word = "'e'", relativeTime = 0.8f, duration = 1.0f, frequency = G4 });
        l7.words.Add(new WordHighlightTiming { word = "Magic", relativeTime = 1.8f, duration = 0.8f, frequency = G4 });
        l7.words.Add(new WordHighlightTiming { word = "'e'", relativeTime = 2.6f, duration = 1.2f, frequency = A4 });
        lyricLines.Add(l7);

        // Line 8: 'Star' is now 'stare' I say!
        LyricLineData l8 = new LyricLineData { lineText = "'Star' is now 'stare' I say!", lineDuration = 5.0f };
        l8.words.Add(new WordHighlightTiming { word = "'Star'", relativeTime = 0.0f, duration = 0.5f, frequency = F4 });
        l8.words.Add(new WordHighlightTiming { word = "is", relativeTime = 0.5f, duration = 0.4f, frequency = F4 });
        l8.words.Add(new WordHighlightTiming { word = "now", relativeTime = 0.9f, duration = 0.4f, frequency = E4 });
        l8.words.Add(new WordHighlightTiming { word = "'stare'", relativeTime = 1.3f, duration = 0.6f, frequency = E4 });
        l8.words.Add(new WordHighlightTiming { word = "I", relativeTime = 1.9f, duration = 0.4f, frequency = D4 });
        l8.words.Add(new WordHighlightTiming { word = "say!", relativeTime = 2.3f, duration = 1.8f, frequency = C4 });
        lyricLines.Add(l8);
    }

    public void AutoAssignAssetsAndWireUI()
    {
        // 1. Locate the Title
        Transform titleTrans = transform.Find("Title");
        if (titleTrans != null)
        {
            titleTextLabel = titleTrans.GetComponentInChildren<TextMeshProUGUI>();
            if (titleTextLabel != null)
            {
                titleTextLabel.text = "Magic 'e' Song";
            }
        }

        // 2. Locate Mascot Characters
        Transform mascotCindy = transform.Find("CindyMascot");
        Transform mascotCar = transform.Find("CarMascot");
        if (mascotCindy != null) mascotCharacter = mascotCindy.GetComponent<RectTransform>();
        else if (mascotCar != null) mascotCharacter = mascotCar.GetComponent<RectTransform>();

        // 3. Locate NextButton
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Transform nextBtnTrans = canvas.transform.Find("NextButton");
            if (nextBtnTrans != null)
            {
                nextButton = nextBtnTrans.gameObject;
            }
        }

        // 4. Locate or Create Lyrics Text Label
        Transform lyricsTrans = transform.Find("LyricsDisplay");
        if (lyricsTrans == null)
        {
            // Try to find CentralWordCard to duplicate its layout, or build a new one
            Transform parentCard = transform.Find("CentralWordCard");
            if (parentCard != null)
            {
                parentCard.gameObject.SetActive(false); // Hide Soft G cards
            }

            GameObject lyricsGo = new GameObject("LyricsDisplay", typeof(RectTransform));
            lyricsGo.transform.SetParent(transform, false);
            RectTransform lyricsRt = lyricsGo.GetComponent<RectTransform>();
            lyricsRt.anchorMin = new Vector2(0.1f, 0.15f);
            lyricsRt.anchorMax = new Vector2(0.9f, 0.75f);
            lyricsRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI lyricsText = lyricsGo.AddComponent<TextMeshProUGUI>();
            lyricsText.alignment = TextAlignmentOptions.Center;
            lyricsText.fontSize = 44f;
            lyricsText.lineSpacing = 15f;
            lyricsText.enableWordWrapping = true;
            lyricsTextLabel = lyricsText;

            // Copy font settings from title if possible
            if (titleTextLabel != null)
            {
                lyricsText.font = titleTextLabel.font;
                lyricsText.fontSharedMaterial = titleTextLabel.fontSharedMaterial;
            }
        }
        else
        {
            lyricsTextLabel = lyricsTrans.GetComponent<TextMeshProUGUI>();
        }

        // 5. Setup Play Button: If there's an options/speaker button, assign it
        Transform speakerBtnTrans = transform.Find("PlayButton");
        if (speakerBtnTrans == null) speakerBtnTrans = transform.Find("CentralReplayButton");
        if (speakerBtnTrans == null) speakerBtnTrans = transform.Find("CentralWordCard/CentralReplayButton");
        if (speakerBtnTrans != null)
        {
            playReplayButton = speakerBtnTrans.GetComponent<Button>();
        }

        // 6. Disable placeholders for other activities
        Transform softGCol = transform.Find("SoftGColumn");
        if (softGCol != null) softGCol.gameObject.SetActive(false);

        Transform hardGCol = transform.Find("HardGColumn");
        if (hardGCol != null) hardGCol.gameObject.SetActive(false);

        Transform wordBtnTemp = transform.Find("WordButtonTemplate");
        if (wordBtnTemp != null) wordBtnTemp.gameObject.SetActive(false);

        Transform instrBg = transform.Find("Instruction Bg");
        if (instrBg != null) instrBg.gameObject.SetActive(false);

        // 7. Pre-populate lyrics list so it is editable in Editor Mode
        if (lyricLines == null || lyricLines.Count == 0)
        {
            InitializeDefaultLyrics();
        }
    }

    private void OnValidate()
    {
        if (lyricLines == null || lyricLines.Count == 0)
        {
            InitializeDefaultLyrics();
        }
    }
}
