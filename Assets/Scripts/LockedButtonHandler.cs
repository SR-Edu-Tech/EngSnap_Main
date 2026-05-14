using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Attach this to any locked course/level button.
///
/// When the button is tapped it:
///   1. Plays a "locked" sound effect
///   2. Shows a popup panel with a customisable hint message
///   3. Auto-dismisses the popup after <see cref="autoDismissSeconds"/> seconds
///      (or immediately when the player taps the close button)
///
/// SETUP
/// ─────
/// 1. Create a UI Panel for the alert (see structure below) and assign it to
///    <see cref="lockedAlertPanel"/>.  Start it INACTIVE in the hierarchy.
/// 2. Inside that panel add:
///      • A TextMeshProUGUI for the message  → assign to <see cref="alertMessageText"/>
///      • A Button to close the popup        → assign to <see cref="closeButton"/>
///      • (Optional) an Animator on the panel for a bounce/shake entrance
/// 3. Add an AudioSource to the same GameObject as this script (or any object)
///    and assign it to <see cref="audioSource"/>.
/// 4. Assign a locked-sound AudioClip to <see cref="lockedSfx"/>.
/// 5. Customise <see cref="lockedMessage"/> per button in the Inspector.
///
/// Recommended panel hierarchy
/// ───────────────────────────
///  LockedAlertPanel  (Panel, Image darkened overlay, starts inactive)
///  └─ AlertCard      (child panel, Image with rounded sprite)
///     ├─ LockIcon    (Image – padlock sprite, optional)
///     ├─ MessageText (TextMeshProUGUI)
///     └─ CloseButton (Button + TextMeshProUGUI "OK" or "×")
/// </summary>
[RequireComponent(typeof(Button))]
public class LockedButtonHandler : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Alert Panel")]
    [Tooltip("Root panel GameObject that covers the screen. Starts inactive.")]
    [SerializeField] private GameObject lockedAlertPanel;

    [Tooltip("TextMeshProUGUI inside the panel that shows the hint.")]
    [SerializeField] private TextMeshProUGUI alertMessageText;

    [Tooltip("Button inside the panel that closes it (optional).")]
    [SerializeField] private Button closeButton;

    [Tooltip("How long (seconds) before the popup auto-dismisses. 0 = never auto-dismiss.")]
    [SerializeField] private float autoDismissSeconds = 3f;

    [Header("Message")]
    [Tooltip("Hint shown when this locked button is tapped.\n" +
             "You can customise per button, e.g.:\n" +
             "\"Complete the previous level to unlock this!\"")]
    [SerializeField]
    [TextArea(2, 4)]
    private string lockedMessage = "🔒 This content is locked!\nFinish earlier levels to unlock it.";

    [Header("SFX")]
    [Tooltip("AudioSource used to play the locked clip. " +
             "Can be a shared AudioSource anywhere in the scene.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Sound played when the locked button is tapped.")]
    [SerializeField] private AudioClip lockedSfx;

    [Header("Animation (optional)")]
    [Tooltip("Animator on the AlertCard child (not the root panel). " +
             "Triggers the 'Show' trigger when the popup opens.")]
    [SerializeField] private Animator alertAnimator;

    // ── Private ───────────────────────────────────────────────────────────────

    private Button      _button;
    private Coroutine   _dismissCoroutine;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnLockedButtonClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(DismissAlert);
    }

    private void OnLockedButtonClicked()
    {
        PlayLockedSfx();
        ShowAlert();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  SFX
    // ─────────────────────────────────────────────────────────────────────────

    private void PlayLockedSfx()
    {
        if (lockedSfx == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(lockedSfx);
        }
        else
        {
            // Fallback: play at world origin (still audible)
            AudioSource.PlayClipAtPoint(lockedSfx, Vector3.zero);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Alert panel
    // ─────────────────────────────────────────────────────────────────────────

    private void ShowAlert()
    {
        if (lockedAlertPanel == null)
        {
            Debug.LogWarning("[LockedButtonHandler] lockedAlertPanel is not assigned.");
            return;
        }

        // Set message text
        if (alertMessageText != null)
            alertMessageText.text = lockedMessage;

        // Show the panel
        lockedAlertPanel.SetActive(true);

        // Trigger entrance animation if wired
        if (alertAnimator != null)
            alertAnimator.SetTrigger("Show");

        // Cancel any running auto-dismiss, then restart it
        if (_dismissCoroutine != null)
            StopCoroutine(_dismissCoroutine);

        if (autoDismissSeconds > 0f)
            _dismissCoroutine = StartCoroutine(AutoDismiss());
    }

    private void DismissAlert()
    {
        if (_dismissCoroutine != null)
        {
            StopCoroutine(_dismissCoroutine);
            _dismissCoroutine = null;
        }

        if (lockedAlertPanel != null)
            lockedAlertPanel.SetActive(false);
    }

    private IEnumerator AutoDismiss()
    {
        yield return new WaitForSeconds(autoDismissSeconds);
        DismissAlert();
    }
}
