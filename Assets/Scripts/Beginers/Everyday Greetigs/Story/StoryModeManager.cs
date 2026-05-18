using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryModeManager : MonoBehaviour, IUnitCompletable
{
    [System.Serializable]
    public class SceneButton
    {
        public GameObject buttonObject;
        public AudioClip  audioClip;

        [Header("Button Colors")]
        public Color defaultColor = new Color(1f,    1f,    1f,    0.85f);
        public Color playingColor = new Color(0.55f, 0.78f, 1f,    1f);
        public Color doneColor    = new Color(0.60f, 0.92f, 0.60f, 1f);

        [HideInInspector] public bool played;
    }

    [System.Serializable]
    public class StoryScene
    {
        public string        title;
        public string        subtitle;
        public GameObject    sceneRoot;
        public AudioClip     narrationClip;
        public SceneButton[] buttons;
    }

    [Header("── Kid Friendly Animation ─────────")]
    public float pulseScale = 1.15f;
    public float pulseSpeed = 0.5f;

    private Coroutine   pulseRoutine;
    private Coroutine   currentButtonAudioRoutine;

    [Header("── Scenes ──────────────────────")]
    public StoryScene[] scenes;

    [Header("── Shared UI ──────────────────")]
    public CanvasGroup     sceneCanvasGroup;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public Button          nextButton;
    public TextMeshProUGUI nextButtonLabel;

    [Header("── Completion ─────────────────")]
    public GameObject completedPanel;
    public Button     finishButton;

    [Header("★ Intro Panel ──────────────────")]
    public GameObject introPanel;
    public AudioClip  introNarrationClip;
    public Button     startButton;

    [Header("★ Completion Narration ────────")]
    public AudioClip completionNarrationClip;

    [Header("── Audio Sources ──────────────")]
    public AudioSource audioSource;
    public AudioSource narrationAudioSource;

    [Header("── Settings ───────────────────")]
    public float fadeDuration = 0.6f;

    // ── IUnitCompletable ──────────────────────────────────────────────────
    [HideInInspector] public SharedUnitButton          ownerUnitButton;
    [HideInInspector] public SharedUnitPanelController ownerUnitPanel;

    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        ownerUnitPanel  = sharedPanel;
        ownerUnitButton = sharedButton;
    }

    // ── Runtime ───────────────────────────────────────────────────────────
    private int          currentScene = 0;
    private HashSet<int> playedSet    = new HashSet<int>();
    private bool         isPlaying    = false;
    private bool         _initialised = false;

    AudioSource NarrationSource => narrationAudioSource != null ? narrationAudioSource : audioSource;

    // ── Unity ─────────────────────────────────────────────────────────────
    void Start()
    {
        _initialised = true;

        // Wire buttons once
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnNextClicked);

        if (finishButton != null)
        {
            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(OnStoryFinished);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
        }

        FullReset();
        ShowIntro();
    }

    void OnEnable()
    {
        // Guard: Start() hasn't run yet on first activation
        if (!_initialised) return;

        StopAllCoroutines();
        StopAllAudio();

        // Always do a full reset so replaying starts fresh from intro
        FullReset();
        ShowIntro();
    }

    // ── Full Reset — call this every time the story is (re)opened ─────────
    void FullReset()
    {
        currentScene = 0;
        playedSet.Clear();
        isPlaying = false;

        // Hide everything
        completedPanel.SetActive(false);
        nextButton.gameObject.SetActive(false);

        if (introPanel  != null) introPanel.SetActive(false);
        if (startButton != null) startButton.gameObject.SetActive(false);

        // Reset all scenes and their buttons
        foreach (var s in scenes)
        {
            if (s.sceneRoot != null) s.sceneRoot.SetActive(false);

            if (s.buttons == null) continue;
            foreach (var sb in s.buttons)
            {
                sb.played = false;
                if (sb.buttonObject == null) continue;
                var btn = sb.buttonObject.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = true;
                    btn.onClick.RemoveAllListeners();
                }
                SetButtonColor(sb, sb.defaultColor);
            }
        }

        // Reset canvas group so fade works correctly
        if (sceneCanvasGroup != null)
        {
            sceneCanvasGroup.alpha          = 1f;
            sceneCanvasGroup.interactable   = true;
            sceneCanvasGroup.blocksRaycasts = true;
        }
    }

    void StopAllAudio()
    {
        if (audioSource       != null && audioSource.isPlaying)       audioSource.Stop();
        if (narrationAudioSource != null && narrationAudioSource.isPlaying) narrationAudioSource.Stop();
    }

    // ── Intro ─────────────────────────────────────────────────────────────
    void ShowIntro()
    {
        if (introPanel != null)
        {
            introPanel.SetActive(true);
            if (startButton != null) startButton.gameObject.SetActive(false);
            StartCoroutine(PlayIntroAudio());
        }
        else
        {
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
        if (startButton != null) startButton.gameObject.SetActive(true);
    }

    void OnStartClicked()
    {
        if (introPanel != null) introPanel.SetActive(false);
        LoadScene(0);
    }

    // ── Load Scene ────────────────────────────────────────────────────────
    void LoadScene(int index)
    {
        currentScene = index;
        playedSet.Clear();
        isPlaying = false;

        var scene = scenes[index];

        foreach (var s in scenes)
            if (s.sceneRoot) s.sceneRoot.SetActive(false);

        if (scene.sceneRoot) scene.sceneRoot.SetActive(true);

        if (titleText)    titleText.text    = scene.title;
        if (subtitleText) subtitleText.text = scene.subtitle;

        nextButton.gameObject.SetActive(false);
        if (nextButtonLabel != null)
            nextButtonLabel.text = (index == scenes.Length - 1) ? "Finish ✓" : "Next →";

        for (int i = 0; i < scene.buttons.Length; i++)
        {
            var sb = scene.buttons[i];
            sb.played = false;
            if (sb.buttonObject == null) continue;

            SetButtonColor(sb, sb.defaultColor);
            var btn = sb.buttonObject.GetComponent<Button>();
            btn.interactable = false;
            btn.onClick.RemoveAllListeners();
            int captured = i;
            btn.onClick.AddListener(() => OnButtonTapped(captured));
        }

        StartCoroutine(FadeInThenNarrate(scene));
    }

    IEnumerator FadeInThenNarrate(StoryScene scene)
    {
        yield return StartCoroutine(FadeIn());

        NarrationSource.Stop();
        if (scene.narrationClip != null)
        {
            NarrationSource.PlayOneShot(scene.narrationClip);
            yield return new WaitForSeconds(scene.narrationClip.length);
        }

        foreach (var sb in scene.buttons)
        {
            if (sb.buttonObject == null) continue;
            sb.buttonObject.GetComponent<Button>().interactable = true;
        }

        PulseNextButton();
    }

    // ── Button Tap ────────────────────────────────────────────────────────
    void OnButtonTapped(int idx)
    {
        if (playedSet.Contains(idx)) return;
        if (NarrationSource.isPlaying) NarrationSource.Stop();
        if (currentButtonAudioRoutine != null) StopCoroutine(currentButtonAudioRoutine);
        if (audioSource.isPlaying)    audioSource.Stop();
        StopButtonPulse();
        currentButtonAudioRoutine = StartCoroutine(PlayAudio(idx));
    }

    IEnumerator PlayAudio(int idx)
    {
        isPlaying = true;
        var sb   = scenes[currentScene].buttons[idx];

        SetButtonColor(sb, sb.playingColor);

        foreach (var b in scenes[currentScene].buttons)
            if (b.buttonObject != null)
                b.buttonObject.GetComponent<Button>().interactable = false;

        if (sb.audioClip != null)
        {
            audioSource.clip = sb.audioClip;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        sb.played = true;
        playedSet.Add(idx);
        SetButtonColor(sb, sb.doneColor);

        foreach (var b in scenes[currentScene].buttons)
        {
            if (b.buttonObject == null) continue;
            int buttonIndex = System.Array.IndexOf(scenes[currentScene].buttons, b);
            if (!playedSet.Contains(buttonIndex))
                b.buttonObject.GetComponent<Button>().interactable = true;
        }

        isPlaying = false;

        if (playedSet.Count >= scenes[currentScene].buttons.Length)
            nextButton.gameObject.SetActive(true);
        else
            PulseNextButton();
    }

    // ── Pulse ─────────────────────────────────────────────────────────────
    void PulseNextButton()
    {
        StopButtonPulse();
        for (int i = 0; i < scenes[currentScene].buttons.Length; i++)
        {
            if (!playedSet.Contains(i) && scenes[currentScene].buttons[i].buttonObject != null)
            {
                pulseRoutine = StartCoroutine(PulseButton(scenes[currentScene].buttons[i].buttonObject.transform));
                break;
            }
        }
    }

    void StopButtonPulse() { if (pulseRoutine != null) { StopCoroutine(pulseRoutine); pulseRoutine = null; } }

    IEnumerator PulseButton(Transform target)
    {
        Vector3 originalScale = Vector3.one;
        while (true)
        {
            float t = 0f;
            while (t < pulseSpeed) { t += Time.deltaTime; target.localScale = originalScale * Mathf.Lerp(1f, pulseScale, t / pulseSpeed); yield return null; }
            t = 0f;
            while (t < pulseSpeed) { t += Time.deltaTime; target.localScale = originalScale * Mathf.Lerp(pulseScale, 1f, t / pulseSpeed); yield return null; }
        }
    }

    // ── Next ──────────────────────────────────────────────────────────────
    void OnNextClicked()
    {
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
            if (scenes[currentScene].sceneRoot) scenes[currentScene].sceneRoot.SetActive(false);
            ShowCompleted();
            yield break;
        }
        LoadScene(next);
    }

    // ── Completed ─────────────────────────────────────────────────────────
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
    }

    // ── Story Finish → Return to Unit Panel ───────────────────────────────
    void OnStoryFinished()
    {
        StopAllCoroutines();
        StopAllAudio();

        // Cache before deactivating
        var cachedPanel  = ownerUnitPanel;
        var cachedButton = ownerUnitButton;

        // Full reset so next time it opens it starts fresh
        FullReset();

        gameObject.SetActive(false);

        if (cachedPanel != null && cachedButton != null)
            cachedPanel.UnitFinished(cachedButton);
        else
            Debug.LogWarning("[StoryMode] ownerUnitPanel or ownerUnitButton is null on finish.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    void SetButtonColor(SceneButton sb, Color color)
    {
        if (sb.buttonObject == null) return;
        var img = sb.buttonObject.GetComponent<Image>();
        if (img) img.color = color;
    }

    IEnumerator FadeIn()
    {
        sceneCanvasGroup.alpha = 0f; sceneCanvasGroup.interactable = false; sceneCanvasGroup.blocksRaycasts = false;
        float t = 0f;
        while (t < fadeDuration) { t += Time.deltaTime; sceneCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration); yield return null; }
        sceneCanvasGroup.alpha = 1f; sceneCanvasGroup.interactable = true; sceneCanvasGroup.blocksRaycasts = true;
    }

    IEnumerator FadeOut()
    {
        sceneCanvasGroup.interactable = false; sceneCanvasGroup.blocksRaycasts = false;
        float t = fadeDuration;
        while (t > 0f) { t -= Time.deltaTime; sceneCanvasGroup.alpha = Mathf.Clamp01(t / fadeDuration); yield return null; }
        sceneCanvasGroup.alpha = 0f;
    }
}