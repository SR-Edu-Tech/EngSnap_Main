using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reward Panel for Unit 8 — awards the 'Consonant Explorer' badge,
/// plays a star celebration, triggers mascot cheer, and returns to level selection.
/// Mirrors the U7_RewardController pattern.
/// </summary>
public class U8_RewardController : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────
    //  Inspector References
    // ──────────────────────────────────────────────────────────

    [Header("UI Elements")]
    public TextMeshProUGUI rewardTitleLabel;
    public TextMeshProUGUI rewardDescriptionLabel;
    public Image            badgeIcon;
    public Button           continueButton;

    [Header("Badge Sprite")]
    public Sprite consonantExplorerBadge; // Assign the Unit 8 badge sprite in Inspector

    [Header("Particles")]
    public ParticleSystem starParticles;  // Celebration star burst particle system

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   victoryClip;       // u8_done audio clip

    [Header("References")]
    public U8_Manager manager;

    // ──────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────

    public void SetupReward()
    {
        // Title and description
        if (rewardTitleLabel      != null) rewardTitleLabel.text      = "CONSONANT EXPLORER!";
        if (rewardDescriptionLabel != null) rewardDescriptionLabel.text = "You know all the consonant sounds!\nYou earned the Consonant Explorer badge! ";

        // Badge sprite
        if (badgeIcon != null && consonantExplorerBadge != null)
            badgeIcon.sprite = consonantExplorerBadge;

        // Star particles
        if (starParticles != null) starParticles.Play();

        // Victory audio
        PlayVictoryAudio();

        // Badge pop animation
        if (badgeIcon != null)
            StartCoroutine(AnimateBadge(badgeIcon.gameObject));

        // Mascot celebration
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.PlayCelebrationAnimation();
    }

    // ──────────────────────────────────────────────────────────
    //  Audio
    // ──────────────────────────────────────────────────────────

    private void PlayVictoryAudio()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        AudioClip clip = victoryClip;

        // Fallback: try Resources / AssetDatabase
        if (clip == null) clip = Resources.Load<AudioClip>("Audio/u8_done");
#if UNITY_EDITOR
        if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Unit8/u8_done.mp3");
        if (clip == null) clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/General/Great job.mp3");
#endif

        if (clip != null && audioSource != null)
        {
            audioSource.spatialBlend = 0f;
            audioSource.volume       = 1f;
            audioSource.mute         = false;
            audioSource.PlayOneShot(clip);
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Badge Animation (scale pop — same as U7)
    // ──────────────────────────────────────────────────────────

    private IEnumerator AnimateBadge(GameObject badgeObj)
    {
        if (badgeObj == null) yield break;

        Vector3 startScale = Vector3.zero;
        Vector3 endScale   = Vector3.one;
        float   elapsed    = 0f;
        float   duration   = 0.5f;

        // Pop in from zero
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease out back — slight overshoot for juiciness
            float s = 1f + 0.3f * Mathf.Sin(t * Mathf.PI);
            badgeObj.transform.localScale = endScale * s;
            yield return null;
        }

        badgeObj.transform.localScale = endScale;

        // Gentle idle pulse
        while (true)
        {
            elapsed = 0f;
            while (elapsed < 1.5f)
            {
                elapsed += Time.deltaTime;
                float pulse = 1f + 0.04f * Mathf.Sin(elapsed * Mathf.PI * 2f / 1.5f);
                badgeObj.transform.localScale = endScale * pulse;
                yield return null;
            }
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Navigation
    // ──────────────────────────────────────────────────────────

    private void OnContinueClicked()
    {
        // Stop idle pulse
        StopAllCoroutines();

        if (manager != null)
            manager.ShowLevelSelection();
    }

    public void OnNextButtonClicked() => OnContinueClicked();
}
