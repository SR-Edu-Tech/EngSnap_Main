using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reward Panel for Unit 9 — awards individual Stage Badges after Stage 1, 2, and 3,
/// and the grand 'Digraph Detective' Trophy after completing the Final Game!
/// </summary>
public class U9_RewardController : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI rewardTitleLabel;
    public TextMeshProUGUI rewardDescriptionLabel;
    public Image            trophyIcon;
    public Button           continueButton;

    [Header("Reward Sprites")]
    public Sprite stage1BadgeSprite;              // Stage 1 Badge (ch, sh)
    public Sprite stage2BadgeSprite;              // Stage 2 Badge (th, wh)
    public Sprite stage3BadgeSprite;              // Stage 3 Badge (ck, nk, ng)
    public Sprite digraphDetectiveTrophySprite;   // Final Game Trophy 🏆

    [Header("Particles")]
    public ParticleSystem starParticles;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   victoryClip;               // u9_unit_done: "You're a Digraph Detective!"
    public AudioClip   badgeClip;                 // u9_badge_done: "Stage cleared! Here's your badge!"

    [Header("References")]
    public U9_Manager manager;

    // 1 = Stage1, 2 = Stage2, 3 = Stage3, 4 = Final Trophy
    private int currentRewardType = 4;

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

    public void ShowStage1Badge()
    {
        currentRewardType = 1;
        SetupRewardView("STAGE 1 CLEARED!", "You mastered ch and sh digraph words!\nYou earned the Stage 1 Explorer Badge! ", stage1BadgeSprite, badgeClip);
    }

    public void ShowStage2Badge()
    {
        currentRewardType = 2;
        SetupRewardView("STAGE 2 CLEARED!", "You mastered th and wh digraph words!\nYou earned the Stage 2 Explorer Badge! ", stage2BadgeSprite, badgeClip);
    }

    public void ShowStage3Badge()
    {
        currentRewardType = 3;
        SetupRewardView("STAGE 3 CLEARED!", "You mastered ck, nk, and ng digraph words!\nYou earned the Stage 3 Explorer Badge! ", stage3BadgeSprite, badgeClip);
    }

    public void ShowTrophy()
    {
        currentRewardType = 4;
        SetupRewardView("DIGRAPH DETECTIVE!", "You masterfully solved all digraph words!\nYou earned the Digraph Detective trophy! ", digraphDetectiveTrophySprite, victoryClip);
    }

    public void ShowReward() => ShowTrophy();
    public void SetupReward() => ShowTrophy();

    private void SetupRewardView(string title, string desc, Sprite iconSprite, AudioClip audio)
    {
        if (rewardTitleLabel       != null) rewardTitleLabel.text       = title;
        if (rewardDescriptionLabel != null) rewardDescriptionLabel.text  = desc;

        if (trophyIcon != null && iconSprite != null)
        {
            trophyIcon.sprite = iconSprite;
        }

        if (starParticles != null) starParticles.Play();

        PlayRewardAudio(audio);

        if (trophyIcon != null)
            StartCoroutine(AnimateTrophy(trophyIcon.gameObject));

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayCelebrationAnimation();
    }

    private IEnumerator AnimateTrophy(GameObject trophyObj)
    {
        Vector3 orig = trophyObj.transform.localScale;
        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, elapsed / 0.4f);
            if (elapsed > 0.4f) scale = Mathf.Lerp(1.2f, 1.0f, (elapsed - 0.4f) / 0.2f);
            trophyObj.transform.localScale = orig * scale;
            yield return null;
        }
        trophyObj.transform.localScale = orig;
    }

    private void PlayRewardAudio(AudioClip targetClip)
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        AudioClip clip = targetClip;
        if (clip == null) clip = Resources.Load<AudioClip>("u9_unit_done");
        if (clip == null) clip = Resources.Load<AudioClip>("u8_done");

        if (clip != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.spatialBlend = 0f;
            audioSource.volume       = 1f;
            audioSource.PlayOneShot(clip);
        }
    }

    private void OnContinueClicked()
    {
        if (manager != null)
        {
            manager.OnRewardContinueClicked(currentRewardType);
        }
    }
}
