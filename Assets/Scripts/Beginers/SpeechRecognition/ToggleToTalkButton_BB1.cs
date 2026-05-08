using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggle-to-talk button.
/// • First click  → StartListening  (icon switches, animation plays from the start)
/// • Second click → StopListening   (icon reverts, animation stops and snaps to frame 0)
///
/// Wire up in Inspector:
///   idleIcon          — sprite shown when NOT listening
///   listeningIcon     — sprite shown WHILE listening
///   listeningAnim     — (optional) Animator on the button
///   listeningAnimName — exact Animator state name to play  (default: "ListeningAnim")
///   buttonImage       — Image to swap sprite on (auto-found if blank)
/// </summary>
public class ToggleToTalkButton_BB1 : MonoBehaviour
{
    [Header("Icon Sprites")]
    [Tooltip("Sprite shown when microphone is idle")]
    public Sprite idleIcon;

    [Tooltip("Sprite shown while the microphone is active / listening")]
    public Sprite listeningIcon;

    [Header("References")]
    [Tooltip("Image whose sprite is swapped. Leave blank to auto-find on this GameObject.")]
    public Image buttonImage;

    [Tooltip("(Optional) Animator on the button.")]
    public Animator listeningAnim;

    [Tooltip("Exact name of the Animator state to play while listening.")]
    public string listeningAnimName = "ListeningAnim";

    [Tooltip("Optional label that shows 'Tap to speak' / 'Listening...'")]
    public TextMeshProUGUI statusLabel;

    [Header("Labels (optional)")]
    public string idleLabel      = "Tap to speak";
    public string listeningLabel = "Listening...";

    // ── Runtime ────────────────────────────────────────────────────────────────

    private bool _isListening = false;

    void Awake()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
    }

    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnButtonClicked);
        }

        ApplyState(false);
    }

    // ── Toggle ─────────────────────────────────────────────────────────────────

    void OnButtonClicked()
    {
        _isListening = !_isListening;

        if (_isListening)
            CrossPlatformSpeechManager_BB1.Instance?.StartListening();
        else
            CrossPlatformSpeechManager_BB1.Instance?.StopListening();

        ApplyState(_isListening);
    }

    /// <summary>
    /// Call externally to force the button back to idle (e.g. on scene change).
    /// </summary>
    public void ForceIdle()
    {
        if (!_isListening) return;
        _isListening = false;
        CrossPlatformSpeechManager_BB1.Instance?.StopListening();
        ApplyState(false);
    }

    // ── Visual State ───────────────────────────────────────────────────────────

    void ApplyState(bool listening)
    {
        // Swap icon sprite
        if (buttonImage != null)
            buttonImage.sprite = listening ? listeningIcon : idleIcon;

        // Animation: play by state name while listening;
        // stop and snap back to the very first frame when done.
        if (listeningAnim != null)
        {
            if (listening)
            {
                // Resume speed and play the state from normalised time 0 (first frame)
                listeningAnim.speed = 1f;
                listeningAnim.Play(listeningAnimName, 0, 0f);
            }
            else
            {
                // Seek to normalised time 0 and freeze — button returns to its rest pose
                listeningAnim.speed = 0f;
                listeningAnim.Play(listeningAnimName, 0, 0f);
                listeningAnim.Update(0f); // force the pose to apply immediately this frame
            }
        }

        // Update status label
        if (statusLabel != null)
            statusLabel.text = listening ? listeningLabel : idleLabel;
    }

    // ── Auto-reset when STT returns a final result ────────────────────────────

    void OnEnable()
    {
        CrossPlatformSpeechManager_BB1.OnResultStatic += OnSpeechResult;
    }

    void OnDisable()
    {
        CrossPlatformSpeechManager_BB1.OnResultStatic -= OnSpeechResult;
    }

    /// <summary>
    /// Automatically flips back to idle the moment the speech engine
    /// returns a final result — the player never needs to click again after speaking.
    /// </summary>
    void OnSpeechResult(string _)
    {
        _isListening = false;
        ApplyState(false);
    }
}