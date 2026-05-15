using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryModeManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneButton
    {
        [Tooltip("Drag the manually-placed Button GameObject here")]
        public GameObject buttonObject;

        [Tooltip("Audio that plays when this button is tapped")]
        public AudioClip audioClip;

        [Header("Button Colors")]
        public Color defaultColor = new Color(1f,    1f,    1f,    0.85f);
        public Color playingColor = new Color(0.55f, 0.78f, 1f,    1f);
        public Color doneColor    = new Color(0.60f, 0.92f, 0.60f, 1f);

        [HideInInspector] public bool played;
    }

    [System.Serializable]
    public class StoryScene
    {
        public string title;
        public string subtitle;

        [Tooltip("Root GameObject for this scene — contains the background image and all buttons")]
        public GameObject sceneRoot;

        [Tooltip("★ Narration clip that auto-plays when this scene fades in.\n" +
                 "Buttons are locked until narration finishes (or immediately if null).")]
        public AudioClip narrationClip;

        [Tooltip("All tappable buttons in this scene")]
        public SceneButton[] buttons;
    }

    // ── Inspector Fields ───────────────────────────────────────────────────────


    [Header("── Kid Friendly Animation ─────────")]
public float pulseScale = 1.15f;
public float pulseSpeed = 0.5f;

private Coroutine pulseRoutine;
private Coroutine currentButtonAudioRoutine;
private AudioSource currentPlayingSource;
    [Header("── Scenes ──────────────────────")]
    public StoryScene[] scenes;

    [Header("── Shared UI ──────────────────")]
    [Tooltip("CanvasGroup on the root — used for fade")]
    public CanvasGroup     sceneCanvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public Button          nextButton;
    public TextMeshProUGUI nextButtonLabel;

    [Header("── Completion ─────────────────")]
    public GameObject completedPanel;
   
   // public Button     restartButton;
    [Tooltip("'Finish' button on CompletedPanel — closes story, returns to unit panel")]
    public Button     finishButton;

    [Header("★ Intro Panel ──────────────────")]
    [Tooltip("Panel shown before any scene. Hidden once the player taps Start.")]
    public GameObject introPanel;

    [Tooltip("Audio that plays automatically when the story opens.\n" +
             "Start button appears only after this finishes.")]
    public AudioClip introNarrationClip;

    [Tooltip("The Start button inside IntroPanel — hidden until intro audio finishes.")]
    public Button startButton;

    [Header("★ Completion Narration ────────")]
    [Tooltip("Audio clip that plays when the Completed panel appears.")]
    public AudioClip completionNarrationClip;

    [Header("── Audio Sources ──────────────")]
    [Tooltip("AudioSource used for button tap SFX / button audio clips")]
    public AudioSource audioSource;

    [Tooltip("★ Dedicated AudioSource for narration (scene + completion).\n" +
             "If left empty, audioSource is used as fallback.")]
    public AudioSource narrationAudioSource;

    [Header("── Settings ───────────────────")]
    public float fadeDuration = 0.6f;

    [Header("── Unit Integration ───────────")]
    [Tooltip("The UnitButton_BB1 that launched this story")]
    public UnitButton_BB1 ownerUnitButton;
    [Tooltip("The UnitPanelController_BB1 to return to after story finishes")]
    public UnitPanelController_BB1 ownerUnitPanel;

    // ── Runtime ────────────────────────────────────────────────────────────────

    private int          currentScene = 0;
    private HashSet<int> playedSet    = new();
    private List<Image>  dots         = new();
    private bool         isPlaying    = false;

    // ── Convenience: which AudioSource to use for narration ───────────────────

    AudioSource NarrationSource => narrationAudioSource != null ? narrationAudioSource : audioSource;

    // ── Unity Lifecycle ────────────────────────────────────────────────────────

    // tracks whether Start() has already run
    private bool _initialised = false;

    void Start()
    {
        _initialised = true;

        foreach (var s in scenes)
            if (s.sceneRoot) s.sceneRoot.SetActive(false);

        completedPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);
        nextButton.onClick.AddListener(OnNextClicked);
        //restartButton.onClick.AddListener(Restart);

        if (finishButton)
            finishButton.onClick.AddListener(OnStoryFinished);

        // Wire Start button — hidden until intro audio ends
        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
        }

        ShowIntro();
    }

    void OnEnable()
    {
        // OnEnable fires before Start on the very first activation,
        // so guard to avoid running before _initialised.
        if (!_initialised) return;

        StopAllCoroutines();
        NarrationSource.Stop();
        audioSource.Stop();

        ResetAllScenes();
        completedPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        ShowIntro();
    }

    // ── ★ Intro Panel ──────────────────────────────────────────────────────────

    void ShowIntro()
    {
        // Hide all scenes while intro plays
        foreach (var s in scenes)
            if (s.sceneRoot) s.sceneRoot.SetActive(false);

        if (introPanel != null)
        {
            introPanel.SetActive(true);

            // Hide Start button until audio finishes
            if (startButton != null)
                startButton.gameObject.SetActive(false);

            StartCoroutine(PlayIntroAudio());
        }
        else
        {
            // No intro panel configured — go straight to Scene 0
            LoadScene(0);
        }
    }

    IEnumerator PlayIntroAudio()
    {
        NarrationSource.Stop();

        if (introNarrationClip != null)
        {
            NarrationSource.PlayOneShot(introNarrationClip);
            yield return new WaitForSeconds(introNarrationClip.length);
        }

        // Reveal Start button after audio completes (or immediately if no clip)
        if (startButton != null)
            startButton.gameObject.SetActive(true);
    }

    void OnStartClicked()
    {
        if (introPanel != null) introPanel.SetActive(false);
        LoadScene(0);
    }

    // ── Load Scene ─────────────────────────────────────────────────────────────

    void LoadScene(int index)
    {
        currentScene = index;
        playedSet.Clear();
        isPlaying = false;

        var scene = scenes[index];

        // Show only this scene root
        foreach (var s in scenes)
            if (s.sceneRoot) s.sceneRoot.SetActive(false);
        if (scene.sceneRoot) scene.sceneRoot.SetActive(true);

        // Title & subtitle
        if (titleText)    titleText.text    = scene.title;
        if (subtitleText) subtitleText.text = scene.subtitle;

        // Next button
        nextButton.gameObject.SetActive(false);
        if (nextButtonLabel)
            nextButtonLabel.text = (index == scenes.Length - 1) ? "Finish ✓" : "Next →";

        // Wire buttons — lock them initially; narration coroutine unlocks them
        for (int i = 0; i < scene.buttons.Length; i++)
        {
            var sb = scene.buttons[i];
            sb.played = false;

            if (sb.buttonObject == null)
            {
                Debug.LogWarning($"[StoryMode] Scene '{scene.title}' button[{i}] has no buttonObject assigned.");
                continue;
            }

            SetButtonColor(sb, sb.defaultColor);

            var btn = sb.buttonObject.GetComponent<Button>();
            btn.interactable = false;       // locked until narration ends
            btn.onClick.RemoveAllListeners();

            int captured = i;
            btn.onClick.AddListener(() => OnButtonTapped(captured));
        }

        StartCoroutine(FadeInThenNarrate(scene));
    }

    // ── ★ Fade In → Play Narration → Unlock Buttons ────────────────────────────

    IEnumerator FadeInThenNarrate(StoryScene scene)
    {
        // Fade the scene in first
        yield return StartCoroutine(FadeIn());

        // Stop any leftover narration from the previous scene
        NarrationSource.Stop();

        if (scene.narrationClip != null)
        {
            NarrationSource.PlayOneShot(scene.narrationClip);
            yield return new WaitForSeconds(scene.narrationClip.length);
        }

        // Unlock all buttons after narration (or immediately if no clip)
     // Unlock buttons
foreach (var sb in scene.buttons)
{
    if (sb.buttonObject == null) continue;
    sb.buttonObject.GetComponent<Button>().interactable = true;
}

// Start pulsing first button
PulseNextButton();
    }

    // ── Button Tap ─────────────────────────────────────────────────────────────

 void OnButtonTapped(int idx)
{
    if (playedSet.Contains(idx)) return;

    // Stop narration instantly if still playing
    if (NarrationSource.isPlaying)
        NarrationSource.Stop();

    // Stop previous button audio
    if (currentButtonAudioRoutine != null)
        StopCoroutine(currentButtonAudioRoutine);

    if (audioSource.isPlaying)
        audioSource.Stop();

    // Stop button pulse animation
    StopButtonPulse();

    currentButtonAudioRoutine = StartCoroutine(PlayAudio(idx));
}

IEnumerator PlayAudio(int idx)
{
    isPlaying = true;

    var sb = scenes[currentScene].buttons[idx];
    var clip = sb.audioClip;

    SetButtonColor(sb, sb.playingColor);

    // Disable all buttons while one audio is playing
    foreach (var b in scenes[currentScene].buttons)
    {
        if (b.buttonObject != null)
            b.buttonObject.GetComponent<Button>().interactable = false;
    }

    if (clip != null)
    {
        audioSource.clip = clip;
        audioSource.Play();

        yield return new WaitWhile(() => audioSource.isPlaying);
    }

    sb.played = true;
    playedSet.Add(idx);

    SetButtonColor(sb, sb.doneColor);

    // Enable remaining buttons
    foreach (var b in scenes[currentScene].buttons)
    {
        if (b.buttonObject == null) continue;

        int buttonIndex = System.Array.IndexOf(scenes[currentScene].buttons, b);

        if (!playedSet.Contains(buttonIndex))
            b.buttonObject.GetComponent<Button>().interactable = true;
    }

    isPlaying = false;

    // ALL BUTTONS COMPLETED
    if (playedSet.Count >= scenes[currentScene].buttons.Length)
    {
        nextButton.gameObject.SetActive(true);
    }
    else
    {
        // Pulse next unplayed button
        PulseNextButton();
    }
}
void PulseNextButton()
{
    StopButtonPulse();

    for (int i = 0; i < scenes[currentScene].buttons.Length; i++)
    {
        if (!playedSet.Contains(i))
        {
            pulseRoutine = StartCoroutine(PulseButton(
                scenes[currentScene].buttons[i].buttonObject.transform
            ));
            break;
        }
    }
}

void StopButtonPulse()
{
    if (pulseRoutine != null)
        StopCoroutine(pulseRoutine);
}

IEnumerator PulseButton(Transform target)
{
    Vector3 originalScale = Vector3.one;

    while (true)
    {
        // Scale Up
        float t = 0f;
        while (t < pulseSpeed)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(1f, pulseScale, t / pulseSpeed);
            target.localScale = originalScale * scale;
            yield return null;
        }

        // Scale Down
        t = 0f;
        while (t < pulseSpeed)
        {
            t += Time.deltaTime;
            float scale = Mathf.Lerp(pulseScale, 1f, t / pulseSpeed);
            target.localScale = originalScale * scale;
            yield return null;
        }
    }
}
    // ── Next Button ────────────────────────────────────────────────────────────

    void OnNextClicked()
    {
        // Stop narration if the player skips ahead before it finishes
        NarrationSource.Stop();

        nextButton.gameObject.SetActive(false);
        StartCoroutine(TransitionToNext());
    }

    IEnumerator TransitionToNext()
    {
        yield return StartCoroutine(FadeOut());

        int next = currentScene + 1;

        if (next >= scenes.Length)
        {
            if (scenes[currentScene].sceneRoot)
                scenes[currentScene].sceneRoot.SetActive(false);

            ShowCompleted();
            yield break;
        }

        LoadScene(next);
    }

    // ── ★ Completed Panel + Narration ──────────────────────────────────────────

    void ShowCompleted()
    {
        completedPanel.SetActive(true);
        StartCoroutine(PlayCompletionNarration());
    }

    IEnumerator PlayCompletionNarration()
    {
        NarrationSource.Stop();

        if (completionNarrationClip != null)
        {
            NarrationSource.PlayOneShot(completionNarrationClip);
            yield return new WaitForSeconds(completionNarrationClip.length);
        }
        else
        {
            yield break;
        }
    }

    // ── Restart ────────────────────────────────────────────────────────────────

    void Restart()
    {
        StopAllCoroutines();
        if (audioSource.isPlaying)     audioSource.Stop();
        if (NarrationSource.isPlaying) NarrationSource.Stop();

        completedPanel.SetActive(false);
        ResetAllScenes();

        // Restart from the intro, not from Scene 0 directly
        ShowIntro();
    }

    // ── Story Finish → Return to Unit Panel ────────────────────────────────────

    void OnStoryFinished()
    {
        NarrationSource.Stop();
        completedPanel.SetActive(false);
        gameObject.SetActive(false);

        if (ownerUnitButton != null && ownerUnitPanel != null)
            ownerUnitPanel.UnitFinished(ownerUnitButton);
        else
            Debug.LogWarning("[StoryMode] ownerUnitButton or ownerUnitPanel not assigned.");
    }

    // ── Open Story (called from UnitButton) ────────────────────────────────────

    public void OpenStory(UnitButton_BB1 unitButton, UnitPanelController_BB1 unitPanel)
    {
        ownerUnitButton = unitButton;
        ownerUnitPanel  = unitPanel;

        gameObject.SetActive(true);

        if (audioSource.isPlaying)     audioSource.Stop();
        if (NarrationSource.isPlaying) NarrationSource.Stop();

        completedPanel.SetActive(false);
        ResetAllScenes();
        ShowIntro();   // ★ always start from the intro
    }

    // ── Shared Reset Helper ────────────────────────────────────────────────────

    void ResetAllScenes()
    {
        currentScene = 0;
        playedSet.Clear();
        isPlaying = false;

        foreach (var s in scenes)
        {
            if (s.sceneRoot) s.sceneRoot.SetActive(false);

            foreach (var sb in s.buttons)
            {
                sb.played = false;
                if (sb.buttonObject == null) continue;

                var btn = sb.buttonObject.GetComponent<Button>();
                if (btn)
                {
                    btn.interactable = true;
                    btn.onClick.RemoveAllListeners();
                }

                SetButtonColor(sb, sb.defaultColor);
            }
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    void SetButtonColor(SceneButton sb, Color color)
    {
        if (sb.buttonObject == null) return;
        var img = sb.buttonObject.GetComponent<Image>();
        if (img) img.color = color;
    }

    // ── Fade ───────────────────────────────────────────────────────────────────

    IEnumerator FadeIn()
    {
        sceneCanvasGroup.alpha          = 0f;
        sceneCanvasGroup.interactable   = false;
        sceneCanvasGroup.blocksRaycasts = false;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            sceneCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        sceneCanvasGroup.alpha          = 1f;
        sceneCanvasGroup.interactable   = true;
        sceneCanvasGroup.blocksRaycasts = true;
    }

    IEnumerator FadeOut()
    {
        sceneCanvasGroup.interactable   = false;
        sceneCanvasGroup.blocksRaycasts = false;

        float t = fadeDuration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            sceneCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        sceneCanvasGroup.alpha = 0f;
    }
}