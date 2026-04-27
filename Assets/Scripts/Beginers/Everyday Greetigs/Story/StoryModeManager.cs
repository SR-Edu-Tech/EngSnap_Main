using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// STORY MODE - Single Script (Manual Button Placement)
///
/// HIERARCHY SETUP:
/// Canvas
/// └── StoryModeManager              ← attach this script + CanvasGroup
///     ├── Scene_0                   ← assign to scenes[0].sceneRoot
///     │   ├── BackgroundImage       ← your scene image (full rect)
///     │   ├── Button_A              ← manually placed anywhere over the image
///     │   └── Button_B              ← manually placed anywhere over the image
///     ├── Scene_1                   ← assign to scenes[1].sceneRoot
///     │   ├── BackgroundImage
///     │   ├── Button_A
///     │   └── Button_B
///     ├── Scene_2 ... Scene_4       ← same pattern
///     ├── NextButton                ← shared, always visible on top
///     ├── ProgressContainer         ← HorizontalLayoutGroup (for dots)
///     └── CompletedPanel            ← disabled by default
///         └── RestartButton
///
/// HOW TO ASSIGN IN INSPECTOR:
/// - scenes[i].sceneRoot   → drag the Scene_X GameObject
/// - scenes[i].buttons[j].buttonObject → drag each Button from that scene
/// - scenes[i].buttons[j].audioClip    → assign the AudioClip
/// </summary>
public class StoryModeManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneButton
    {
        [Tooltip("Drag the manually-placed Button GameObject here")]
        public GameObject   buttonObject;

        [Tooltip("Audio that plays when this button is tapped")]
        public AudioClip    audioClip;

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

        [Tooltip("Root GameObject for this scene — contains the background image and all buttons")]
        public GameObject    sceneRoot;

        [Tooltip("All tappable buttons in this scene")]
        public SceneButton[] buttons;
    }

    // ── Inspector Fields ───────────────────────────────────────────────────────

    [Header("── Scenes ──────────────────────")]
    public StoryScene[] scenes;

    [Header("── Shared UI ──────────────────")]
    [Tooltip("CanvasGroup on the root (or a content container) — used for fade")]
    public CanvasGroup      sceneCanvasGroup;
    public TextMeshProUGUI  titleText;
    public TextMeshProUGUI  subtitleText;
    public Button           nextButton;
    public TextMeshProUGUI  nextButtonLabel;

   // [Header("── Progress Dots ──────────────")]
    //public Transform        progressContainer;
    //public GameObject       dotPrefab;
    /// <summary>
    /// public Color            dotDefault  = new Color(0.75f, 0.75f, 0.75f);
    /// </summary>
    //public Color            dotActive   = new Color(0.22f, 0.54f, 0.87f);
    //public Color            dotDone     = new Color(0.23f, 0.63f, 0.18f);

    [Header("── Completion ─────────────────")]
    public GameObject       completedPanel;
    public Button           restartButton;
    [Tooltip("'Next/Done' button on CompletedPanel — closes story, returns to unit panel")]
    public Button           finishButton;

    [Header("── Settings ───────────────────")]
    public AudioSource      audioSource;
    public float            fadeDuration = 0.6f;

    [Header("── Unit Integration ───────────")]
    [Tooltip("The UnitButton_BB1 that launched this story")]
    public UnitButton_BB1           ownerUnitButton;
    [Tooltip("The UnitPanelController_BB1 to return to after story finishes")]
    public UnitPanelController_BB1  ownerUnitPanel;

    // ── Runtime ────────────────────────────────────────────────────────────────

    private int             currentScene = 0;
    private HashSet<int>    playedSet    = new();
    private List<Image>     dots         = new();
    private bool            isPlaying    = false;

    // ── Unity Lifecycle ────────────────────────────────────────────────────────

    void Start()
    {
        // Hide every scene root at start
        foreach (var s in scenes)
            if (s.sceneRoot) s.sceneRoot.SetActive(false);

        completedPanel.SetActive(false);

        nextButton.onClick.AddListener(OnNextClicked);
        restartButton.onClick.AddListener(Restart);

        if (finishButton)
            finishButton.onClick.AddListener(OnStoryFinished);

       // BuildDots();
        LoadScene(0);
    }
    void OnEnable()
    {
        LoadScene(0);
    }
    // ── Progress Dots ──────────────────────────────────────────────────────────

    // void BuildDots()
    // {
    //  foreach (Transform t in progressContainer) Destroy(t.gameObject);
    // dots.Clear();
    //
    //  for (int i = 0; i < scenes.Length; i++)
    // {
    //    var go = Instantiate(dotPrefab, progressContainer);
    //  dots.Add(go.GetComponent<Image>());
    //  }

    //  RefreshDots();
    // }

    // void RefreshDots()
    // {
    //    for (int i = 0; i < dots.Count; i++)
    //  {
    //     if      (i < currentScene)  dots[i].color = dotDone;
    //    else if (i == currentScene) dots[i].color = dotActive;
    //    else                        dots[i].color = dotDefault;
    // }
    // }

    // ── Load Scene ─────────────────────────────────────────────────────────────


    void LoadScene(int index)
    {
        currentScene = index;
        playedSet.Clear();
        isPlaying = false;

        var scene = scenes[index];

        // Show only this scene's GameObject
        foreach (var s in scenes)
            if (s.sceneRoot) s.sceneRoot.SetActive(false);
        if (scene.sceneRoot) scene.sceneRoot.SetActive(true);

        // Title & subtitle
        if (titleText)    titleText.text    = scene.title;
        if (subtitleText) subtitleText.text = scene.subtitle;

        // Next button
        nextButton.interactable = false;
        if (nextButtonLabel)
            nextButtonLabel.text = (index == scenes.Length - 1) ? "Finish ✓" : "Next →";

        // Wire buttons
        for (int i = 0; i < scene.buttons.Length; i++)
        {
            var sb = scene.buttons[i];
            sb.played = false;

            if (sb.buttonObject == null)
            {
                Debug.LogWarning($"[StoryMode] Scene '{scene.title}' button[{i}] has no buttonObject assigned.");
                continue;
            }

            // Reset color & interactable
            SetButtonColor(sb, sb.defaultColor);
            sb.buttonObject.GetComponent<Button>().interactable = true;

            // Re-wire click (fresh)
            var btn = sb.buttonObject.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();

            int captured = i;
            btn.onClick.AddListener(() => OnButtonTapped(captured));
        }

       // RefreshDots();
        StartCoroutine(FadeIn());
    }

    // ── Button Tap ─────────────────────────────────────────────────────────────

    void OnButtonTapped(int idx)
    {
        if (playedSet.Contains(idx) || isPlaying) return;
        StartCoroutine(PlayAudio(idx));
    }

    IEnumerator PlayAudio(int idx)
    {
        isPlaying = true;

        var sb   = scenes[currentScene].buttons[idx];
        var clip = sb.audioClip;

        // Show playing state
        SetButtonColor(sb, sb.playingColor);

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            Debug.LogWarning($"[StoryMode] No AudioClip on button {idx} — scene '{scenes[currentScene].title}'");
            yield return new WaitForSeconds(0.5f);
        }

        // Show done state
        sb.played = true;
        playedSet.Add(idx);
        SetButtonColor(sb, sb.doneColor);
        sb.buttonObject.GetComponent<Button>().interactable = false;

        isPlaying = false;

        // All buttons played → unlock Next
        if (playedSet.Count >= scenes[currentScene].buttons.Length)
            nextButton.interactable = true;
    }

    // ── Next Button ────────────────────────────────────────────────────────────

    void OnNextClicked()
    {
        nextButton.interactable = false;
        StartCoroutine(TransitionToNext());
    }

    IEnumerator TransitionToNext()
    {
        yield return StartCoroutine(FadeOut());

       // dots[currentScene].color = dotDone;
        int next = currentScene + 1;

        if (next >= scenes.Length)
        {
            // All scenes done
            if (scenes[currentScene].sceneRoot)
                scenes[currentScene].sceneRoot.SetActive(false);

            completedPanel.SetActive(true);
            yield break;
        }

        LoadScene(next);
       
        
    }
  
    // ── Restart ────────────────────────────────────────────────────────────────

    void Restart()
    {
        // Restart is only reachable from CompletedPanel — no coroutines are
        // running at this point so StopAllCoroutines is safe here.
        StopAllCoroutines();
        if (audioSource.isPlaying) audioSource.Stop();

        completedPanel.SetActive(false);
        ResetAllScenes();
        LoadScene(0);
    }

    // ── Story Finish → Return to Unit Panel ────────────────────────────────────

    void OnStoryFinished()
    {
        completedPanel.SetActive(false);
        gameObject.SetActive(false);

        if (ownerUnitButton != null && ownerUnitPanel != null)
            ownerUnitPanel.UnitFinished(ownerUnitButton);
        else
            Debug.LogWarning("[StoryMode] ownerUnitButton or ownerUnitPanel not assigned.");
    }

    // ── Open Story (called from UnitButton) ────────────────────────────────────

    /// <summary>
    /// Call this from the UnitButton that launches the story:
    ///   storyManager.OpenStory(this, panel);
    /// </summary>
    public void OpenStory(UnitButton_BB1 unitButton, UnitPanelController_BB1 unitPanel)
    {
        ownerUnitButton = unitButton;
        ownerUnitPanel  = unitPanel;

        gameObject.SetActive(true);

        // Stop audio from previous run.
        // Do NOT call StopAllCoroutines() here — LoadScene immediately
        // starts FadeIn and StopAllCoroutines would kill it on the same frame.
        if (audioSource.isPlaying) audioSource.Stop();

        completedPanel.SetActive(false);

        ResetAllScenes();   // hides all scenes, resets all buttons, clears runtime state
        LoadScene(0);       // shows scene 0, wires buttons, starts FadeIn
    }

    // ── Shared Reset Helper ────────────────────────────────────────────────────

    void ResetAllScenes()
    {
        currentScene = 0;
        playedSet.Clear();
        isPlaying = false;

        foreach (var s in scenes)
        {
            // Hide root
            if (s.sceneRoot) s.sceneRoot.SetActive(false);

            // Reset every button back to default
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