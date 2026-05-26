using UnityEngine;

/// <summary>
/// GameManager_MagicWords_Reading
/// Central state machine for the Magic Words reading unit.
/// Manages transitions between Panel 1 (Word Bubbles) and Panel 2 (Situation Cards).
/// Attach to a persistent GameObject named "GameManager" in the scene.
/// </summary>
public class GameManager_MagicWords_Reading : MonoBehaviour, IUnitCompletable
{
    public static GameManager_MagicWords_Reading Instance { get; private set; }

    // ── Panel references ──────────────────────────────────────────────────────
    [Header("Panels")]
    [Tooltip("Root GameObject for Panel 1 – Word Bubble intro screen")]
    public GameObject panel1WordBubbles;

    [Tooltip("Root GameObject for Panel 2 – Situation Card reader")]
    public GameObject panel2SituationCards;

    // ── SFX references ────────────────────────────────────────────────────────
    [Header("Global SFX")]
    [Tooltip("Played once on game start / scene load")]
    public AudioClip sfxIntroJingle;

    [Tooltip("Played on every panel transition")]
    public AudioClip sfxPanelTransition;

    [Tooltip("Celebration sound when student completes all interactions")]
    public AudioClip sfxUnitComplete;

    private AudioSource _globalAudio;

      [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton          unitButton;

    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        panel      = sharedPanel;
        unitButton = sharedButton;
    }


    // ── State ─────────────────────────────────────────────────────────────────
    public enum GamePanel { Panel1_WordBubbles, Panel2_SituationCards }
    public GamePanel CurrentPanel { get; private set; } = GamePanel.Panel1_WordBubbles;

    // BUG FIX: tracks whether the very first ShowPanel call has happened.
    // Start() calls ShowPanel(Panel1) to set the initial state — we skip the
    // transition SFX on that first call so no sound plays on scene load.
    private bool _firstShowDone = false;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        // Singleton – persist across scene loads if needed
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _globalAudio = GetComponent<AudioSource>();
        if (_globalAudio == null) _globalAudio = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        ShowPanel(GamePanel.Panel1_WordBubbles);

        if (sfxIntroJingle != null)
            _globalAudio.PlayOneShot(sfxIntroJingle);
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Activate the requested panel and deactivate the other.</summary>
    public void ShowPanel(GamePanel targetPanel)
    {
        // BUG FIX: parameter was named 'panel', shadowing the
        // 'SharedUnitPanelController panel' field. Renamed to 'targetPanel'
        // to eliminate the ambiguity and prevent future accidental field access.

        CurrentPanel = targetPanel;

        bool showP1 = (targetPanel == GamePanel.Panel1_WordBubbles);
        panel1WordBubbles.SetActive(showP1);
        panel2SituationCards.SetActive(!showP1);

        // BUG FIX: skip transition SFX on the first call (scene startup).
        // Previously the jingle AND the transition sound both fired on Start().
        if (_firstShowDone && sfxPanelTransition != null)
            _globalAudio.PlayOneShot(sfxPanelTransition);

        _firstShowDone = true;
    }

    /// <summary>Called by Panel 1 NEXT button → load Panel 2.</summary>
    public void GoToPanel2()
    {
        ShowPanel(GamePanel.Panel2_SituationCards);
    }

    /// <summary>Called by Panel 2 NEXT button → unit complete, load next scene or celebrate.</summary>
    public void GoToNextUnit()
    {
        if (sfxUnitComplete != null)
            _globalAudio.PlayOneShot(sfxUnitComplete);

        // TODO: Replace with your scene management / curriculum flow
        Debug.Log("[MagicWords] Unit complete! Load next unit here.");

        panel.UnitFinished(unitButton);


    }

    /// <summary>Replay from beginning (Panel 1).</summary>
    public void ReplayFromStart()
    {
        ShowPanel(GamePanel.Panel1_WordBubbles);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void PlayGlobalSFX(AudioClip clip)
    {
        if (clip != null) _globalAudio.PlayOneShot(clip);
    }
}