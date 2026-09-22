using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Subclass Game Manager for Unit 8 (Source of Inspiration / Tongue Twisters) Intro Lesson One.
/// Interactive Recording Studio Cinematic Sequence ("Step Up to the Mic").
/// Features:
/// - 5-Step Cinematic Sequence: Studio fade-in, ON AIR sign ignition, LEO slow & fast tongue twister attempts with dancing waveform, ARIA goal preview.
/// - Dynamic audio waveform visualization driven during voice line playback.
/// - Optional ARIA replay button replaying VO_INTRO_ARIA.
/// - Gated START button enabling upon cinematic sequence completion.
/// - Progression integration marking Unit 8 Intro complete upon START tap.
/// </summary>
public class Masters_SourceOfInspiration_Intro_LessonOne : Masters_Lesson {

    [Header("UI Header & Titles")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI mainTitleTMP;

    [Header("Recording Studio Environment")]
    [SerializeField] private GameObject onAirSignPanel;
    [SerializeField] private TextMeshProUGUI onAirTextTMP;
    [SerializeField] private Image onAirGlowImage;

    [Header("Audio Waveform Display")]
    [SerializeField] private GameObject waveformContainer;
    [SerializeField] private RectTransform[] waveformBars;

    [Header("Character Avatars")]
    [SerializeField] private RectTransform leoAvatarRect;
    [SerializeField] private RectTransform ariaAvatarRect;

    [Header("Subtitles & Controls")]
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private Button ariaReplayButton;
    [SerializeField] private Button startButton;

    [Header("Audio References")]
    [SerializeField] private AudioClip voIntroAria;
    [SerializeField] private AudioClip voIntroLeoSlow;
    [SerializeField] private AudioClip voIntroLeoFast;
    [SerializeField] private AudioClip sfxStudioTone;
    [SerializeField] private AudioClip sfxOnAir;
    [SerializeField] private AudioClip musUnitTheme;

    private bool isWaveformAnimating = false;
#pragma warning disable 0414
    private bool isCinematicFinished = false;
#pragma warning restore 0414
    private Coroutine waveformCoroutine;

    protected virtual void OnEnable() {
        // Prevent STT subscriptions
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Intro;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeAudioReferences();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Intro;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeAudioReferences();
        UpdateTitleAndUIComponents();
        SetupButtonListeners();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (startButton != null) startButton.gameObject.SetActive(false);

        StartCoroutine(IntroCinematicRoutine());
    }

    private void DeactivateObsoleteBaseUI() {
        string[] obsoleteNames = new string[] {
            "UnitHeading", "Sign Board left", "Sign Board Right", "Formal", "Informal",
            "MagnetStations", "WordTiles", "CompletedPhraseGlow", "ARIA", "SkipButton", "Continue", "Heading", "Header"
        };

        foreach (Transform child in transform) {
            if (child == null) continue;
            string cName = child.name;
            bool isObsolete = false;
            foreach (var obs in obsoleteNames) {
                if (cName.Equals(obs, System.StringComparison.OrdinalIgnoreCase)) {
                    isObsolete = true;
                    break;
                }
            }

            if (isObsolete) {
                child.gameObject.SetActive(false);
            } else {
                TMP_Text tmp = child.GetComponent<TMP_Text>();
                if (tmp != null) {
                    string txt = tmp.text ?? "";
                    if (txt.Contains("POLISHED") || txt.Contains("COMMUNICATION") || txt.Contains("GROOVE") || txt.Contains("COLLOCATIONS") || txt.Contains("New Text")) {
                        tmp.gameObject.SetActive(false);
                    }
                }
            }
        }

        // Ensure Character is active and rendered IN FRONT of all background panels & UI (Sorting Order 1000)
        Transform charTrans = transform.Find("Character");
        if (charTrans != null) {
            charTrans.gameObject.SetActive(true);
            charTrans.SetAsLastSibling();

            Canvas charCanvas = charTrans.GetComponent<Canvas>();
            if (charCanvas == null) charCanvas = charTrans.gameObject.AddComponent<Canvas>();
            charCanvas.overrideSorting = true;
            charCanvas.sortingOrder = 1000;

            SpriteRenderer[] renderers = charTrans.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers) {
                if (sr != null) {
                    sr.sortingOrder = 1000;
                }
            }

            RectTransform cRect = charTrans.GetComponent<RectTransform>();
            if (cRect != null) {
                cRect.anchoredPosition = new Vector2(-220f, -50f);
            }
        }

        // Deactivate leftover station images from hub base prefabs if present
        foreach (Transform child in transform) {
            if (child == null) continue;
            if (child.name.StartsWith("Image") && child != charTrans) {
                if (child.Find("Text (TMP)") != null || child.Find("Text") != null) {
                    child.gameObject.SetActive(false);
                }
            }
        }
    }

    private void InitializeAudioReferences() {
#if UNITY_EDITOR
        string audioDir = "Assets/Audio/2A/8_SourceOfInspiration/Intro/";
        if (voIntroAria == null) {
            voIntroAria = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Welcome to Unit 8 Source of Inspiration.mp3");
            if (voIntroAria == null) {
                voIntroAria = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Tongue twisters train your mouth and your memory Slow first fast later - let's practise.mp3");
            }
        }
        if (voIntroLeoSlow == null) voIntroLeoSlow = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "I wish to wish the wish you wish to wish.mp3");
        if (voIntroLeoFast == null) voIntroLeoFast = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "I wish to wish the fish you wish to twist oops Haha.mp3");

        if (sfxOnAir == null) sfxOnAir = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
        if (sfxStudioTone == null) sfxStudioTone = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
        if (musUnitTheme == null) musUnitTheme = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
#endif
    }

    private IEnumerator IntroCinematicRoutine() {
        isCinematicFinished = false;

        // Reset ON AIR sign to dim
        SetOnAirSignState(false);
        SetSubtitle("");

        yield return new WaitForSeconds(0.4f);

        // STEP 1 — STUDIO FADE-IN & ON AIR SIGN IGNITION
        if (sfxOnAir != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxOnAir);
        }
        SetOnAirSignState(true);

        yield return new WaitForSeconds(0.6f);

        // STEP 2 — WELCOME INTRO AUDIO & ARIA INTRODUCTION
        SetSubtitle("ARIA: \"Welcome to Unit 8 Source of Inspiration! Every story starts with a spark. Today we discover the words that inspire us.\"");
        yield return StartCoroutine(PlayVoiceWithWaveformRoutine(voIntroAria));

        yield return new WaitForSeconds(0.5f);

        // STEP 3 — LEO TRIES THE LINE SLOWLY
        if (leoAvatarRect != null) {
            leoAvatarRect.DOKill();
            leoAvatarRect.DOPunchScale(Vector3.one * 0.15f, 0.4f);
        }
        SetSubtitle("LEO (Slow): \"I wish to wish the wish you wish to wish\"");
        yield return StartCoroutine(PlayVoiceWithWaveformRoutine(voIntroLeoSlow));

        yield return new WaitForSeconds(0.5f);

        // STEP 4 — LEO TRIES FASTER (Playful Word Tangle)
        if (leoAvatarRect != null) {
            leoAvatarRect.DOKill();
            leoAvatarRect.DOShakePosition(0.5f, 15f, 10, 90f);
        }
        SetSubtitle("LEO (Fast): \"I wish to wish the fish you wish to twist... oops! Haha!\"");
        yield return StartCoroutine(PlayVoiceWithWaveformRoutine(voIntroLeoFast));

        yield return new WaitForSeconds(0.5f);

        // STEP 5 — ARIA PREVIEWS THE GOAL & REASSURES LEO
        SetSubtitle("ARIA: \"Tongue twisters train your mouth and your memory. Slow first, fast later — let's practise!\"");
        yield return StartCoroutine(PlayVoiceWithWaveformRoutine(voIntroAria));

        yield return new WaitForSeconds(0.4f);

        // STEP 6 — REVEAL & ENABLE PROMINENT START BUTTON
        isCinematicFinished = true;
        SetSubtitle("Press START to begin practice!");

        if (startButton != null) {
            startButton.gameObject.SetActive(true);
            startButton.interactable = true;
            startButton.transform.DOKill();
            startButton.transform.localScale = Vector3.zero;
            startButton.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        if (ariaReplayButton != null) {
            ariaReplayButton.gameObject.SetActive(true);
            ariaReplayButton.interactable = true;
        }
    }

    private IEnumerator PlayVoiceWithWaveformRoutine(AudioClip clip) {
        if (clip != null && Masters_AudioManager.Instance != null) {
            StartWaveformAnimation();
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
            StopWaveformAnimation();
        } else {
            yield return new WaitForSeconds(1.8f);
        }
    }

    private void StartWaveformAnimation() {
        if (isWaveformAnimating) return;
        isWaveformAnimating = true;
        waveformCoroutine = StartCoroutine(AnimateWaveformBarsRoutine());
    }

    private void StopWaveformAnimation() {
        isWaveformAnimating = false;
        if (waveformCoroutine != null) {
            StopCoroutine(waveformCoroutine);
            waveformCoroutine = null;
        }

        // Reset waveform bar heights
        if (waveformBars != null) {
            foreach (var bar in waveformBars) {
                if (bar != null) bar.sizeDelta = new Vector2(bar.sizeDelta.x, 15f);
            }
        }
    }

    private IEnumerator AnimateWaveformBarsRoutine() {
        while (isWaveformAnimating) {
            if (waveformBars != null) {
                for (int i = 0; i < waveformBars.Length; i++) {
                    if (waveformBars[i] != null) {
                        float randomHeight = Random.Range(15f, 65f);
                        waveformBars[i].DOSizeDelta(new Vector2(waveformBars[i].sizeDelta.x, randomHeight), 0.1f);
                    }
                }
            }
            yield return new WaitForSeconds(0.12f);
        }
    }

    private void SetOnAirSignState(bool isOn) {
        if (onAirGlowImage != null) {
            onAirGlowImage.color = isOn ? new Color(1f, 0.2f, 0.2f, 1f) : new Color(0.3f, 0.1f, 0.1f, 0.5f);
        }
        if (onAirTextTMP != null) {
            onAirTextTMP.color = isOn ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.5f);
        }
    }

    private void SetSubtitle(string text) {
        if (subtitleTMP != null) {
            subtitleTMP.text = text;
        }
    }

    public void OnAriaReplayClicked() {
        if (voIntroAria != null && Masters_AudioManager.Instance != null) {
            StartCoroutine(PlayVoiceWithWaveformRoutine(voIntroAria));
        }
    }

    public void OnStartButtonClicked() {
        if (startButton != null) startButton.interactable = false;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        ReturnToHub();
    }

    private void UpdateTitleAndUIComponents() {
        if (headerTMP != null) {
            headerTMP.text = "INTRO";
        }

        if (mainTitleTMP != null) {
            mainTitleTMP.gameObject.SetActive(true);
            mainTitleTMP.text = "Step Up to the Mic";
            mainTitleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            RectTransform rt = mainTitleTMP.GetComponent<RectTransform>();
            if (rt != null) {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(1000f, 50f);
                rt.anchoredPosition = new Vector2(0f, -40f);
            }
        }
    }

    private void SetupButtonListeners() {
        if (ariaReplayButton != null) {
            ariaReplayButton.onClick.RemoveAllListeners();
            ariaReplayButton.onClick.AddListener(OnAriaReplayClicked);
        }

        if (startButton != null) {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
    }

    protected override void OnNextButtonClicked() {
        ReturnToHub();
    }

    public void ReturnToHub() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Intro);
        }
    }

    private void AutoFindUIReferences() {
        if (mainTitleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title");
            if (t != null) mainTitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (onAirSignPanel == null) {
            Transform t = transform.Find("OnAirSignPanel");
            if (t != null) {
                onAirSignPanel = t.gameObject;
                onAirGlowImage = t.GetComponent<Image>();
                onAirTextTMP = t.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (waveformContainer == null) {
            Transform t = transform.Find("WaveformContainer");
            if (t != null) {
                waveformContainer = t.gameObject;
                waveformBars = t.GetComponentsInChildren<RectTransform>(true);
            }
        }

        if (leoAvatarRect == null) {
            Transform t = transform.Find("LeoAvatar");
            if (t != null) leoAvatarRect = t.GetComponent<RectTransform>();
        }

        if (ariaAvatarRect == null) {
            Transform t = transform.Find("AriaAvatar");
            if (t != null) ariaAvatarRect = t.GetComponent<RectTransform>();
        }

        if (subtitleTMP == null) {
            Transform t = transform.Find("SubtitleText");
            if (t != null) subtitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (ariaReplayButton == null) {
            Transform t = transform.Find("AriaReplayButton");
            if (t != null) ariaReplayButton = t.GetComponent<Button>();
        }

        if (startButton == null) {
            Transform t = transform.Find("StartButton");
            if (t != null) startButton = t.GetComponent<Button>();
        }
    }
}