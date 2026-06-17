using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CelebrationScreen_game  —  Screen 2
/// ─────────────────────────────────────────────────────────────────────────
/// Shown after all 6 rounds are complete.
/// Plays a VO clip, then waits for the player to tap Next.
///
/// HIERARCHY EXAMPLE:
///   Screen2_Celebration              ← this script
///     ├─ MessageImage (optional)     ← static illustration / banner
///     └─ NextButton                  ← Button component
///
/// INSPECTOR WIRING:
///   nextButton          → NextButton
///   audioSource         → AudioSource on this GO or child
///   celebrationVoClip   → VO: "You are a true good student! All the rules are clear!"
/// </summary>
public class CelebrationScreen_game : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button nextButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   celebrationVoClip;

    // ── Runtime ──────────────────────────────────────────────────────────
    private GameManager_SchoolRules_game _manager;

    // ════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Called by GameManager when transitioning to Screen 2.</summary>
    public void Show(GameManager_SchoolRules_game manager)
    {
        _manager = manager;

        // Disable Next until VO finishes so player hears the full message
        SetNextInteractable(false);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextPressed);
        }

        StartCoroutine(PlayAndEnable());
    }

    /// <summary>
    /// Hides and fully resets this screen.
    /// Called by GameManager before every unit start so Screen 2 is clean on re-open.
    /// </summary>
    public void ResetPanel()
    {
        StopAllCoroutines();

        if (audioSource != null) audioSource.Stop();

        SetNextInteractable(false);

        if (nextButton != null)
            nextButton.onClick.RemoveAllListeners();

        gameObject.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Private
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator PlayAndEnable()
    {
        // Play VO
        if (audioSource != null && celebrationVoClip != null)
        {
            audioSource.clip = celebrationVoClip;
            audioSource.Play();
            yield return new WaitForSeconds(celebrationVoClip.length);
        }

        // VO done — enable Next button
        SetNextInteractable(true);
    }

    private void OnNextPressed()
    {
        if (audioSource != null) audioSource.Stop();
        _manager?.OnScreen2Complete();
    }

    private void SetNextInteractable(bool interactable)
    {
        if (nextButton != null) nextButton.interactable = interactable;
    }
}
