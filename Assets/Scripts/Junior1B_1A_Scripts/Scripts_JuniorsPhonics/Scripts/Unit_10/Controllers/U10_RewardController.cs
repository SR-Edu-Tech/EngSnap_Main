using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reward Controller for Unit 10 — awards Section Badges and the Grand Finale
/// 'Reading Star' Trophy 🏆 along with the All-10-Units Badge Recap Wall!
/// - Section-wise Completion (Stage 1, 2, 3, 4 Badges): Hides TrophyIcon & Recap Wall, shows ONLY BadgeIcon!
/// - Grand Finale (Unit 10 Book Completion): Hides BadgeIcon, shows TrophyIcon AND BadgeRecapWallContainer!
/// </summary>
public class U10_RewardController : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI rewardTitleLabel;
    public TextMeshProUGUI rewardDescriptionLabel;
    public Image            trophyIcon;               // Grand Finale Trophy image on left
    public Image            badgeIcon;                // Single section completion badge image
    public Button           continueButton;
    public GameObject       badgeRecapWallContainer; // Recap wall showing badges from Units 1 to 10

    [Header("Reward Sprites")]
    public Sprite beginningBlendsBadgeSprite;     // Stage 1 Badge (U10_SA_badge1)
    public Sprite startItRightBadgeSprite;         // Stage 2 Badge (U10_SB_badge_2)
    public Sprite endingBlendsBadgeSprite;        // Stage 3 Badge (U10_SC_badge_3)
    public Sprite finishItRightBadgeSprite;       // Stage 4 Badge (U10_SD_badge_4)
    public Sprite readingStarGrandTrophySprite;   // Grand Finale Reading Star Trophy 🏆 (U10_trophy_0)

    [Header("Particles")]
    public ParticleSystem starParticles;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip   victoryClip;               // u10_book_done: "You finished the whole book! You're a Reading Star!"
    public AudioClip   badgeClip;                 // u10_section_done: "Nicely done! Here's your sticker!"

    [Header("References")]
    public U10_Manager manager;

    // 1 = Beginning, 2 = StartItRight, 3 = Ending, 4 = FinishItRight, 5 = Grand Finale Trophy
    public int currentRewardType = 5;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        AutoFindUIElements();
    }

    private void AutoFindUIElements()
    {
        if (continueButton == null)
        {
            Transform t = transform.Find("ContinueButton");
            if (t == null) t = transform.Find("Continue_Button");
            if (t != null) continueButton = t.GetComponent<Button>();
        }

        if (badgeIcon == null)
        {
            Transform t = transform.Find("BadgeIcon");
            if (t == null) t = transform.Find("Badge_Icon");
            if (t == null) t = transform.Find("Badge");
            if (t != null) badgeIcon = t.GetComponent<Image>();
        }

        if (trophyIcon == null)
        {
            Transform t = transform.Find("TrophyIcon");
            if (t == null) t = transform.Find("Trophy_Icon");
            if (t != null) trophyIcon = t.GetComponent<Image>();
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

#if UNITY_EDITOR
        if (badgeClip == null)
            badgeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Unit 10/Nicely done Here's your sticker.mp3");
        if (victoryClip == null)
            victoryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/Unit 10/You finished the whole book You're a Reading Star.mp3");
#endif
        if (badgeClip   == null) badgeClip   = Resources.Load<AudioClip>("u10_section_done");
        if (victoryClip == null) victoryClip = Resources.Load<AudioClip>("u10_book_done");
    }

    public void ShowBeginningBlendsBadge()
    {
        currentRewardType = 1;
        SetupRewardView("BEGINNING BLENDS CLEARED!", "You mastered beginning consonant blends!\nYou earned the Beginning Blends Badge!", beginningBlendsBadgeSprite, badgeClip);
    }

    public void ShowStartItRightBadge()
    {
        currentRewardType = 2;
        SetupRewardView("START IT RIGHT CLEARED!", "You completed the beginning blend word game!\nYou earned the Start it Right Badge!", startItRightBadgeSprite, badgeClip);
    }

    public void ShowEndingBlendsBadge()
    {
        currentRewardType = 3;
        SetupRewardView("ENDING BLENDS CLEARED!", "You mastered ending consonant blends!\nYou earned the Ending Blends Badge!", endingBlendsBadgeSprite, badgeClip);
    }

    public void ShowFinishItRightBadge()
    {
        currentRewardType = 4;
        SetupRewardView("FINISH IT RIGHT CLEARED!", "You completed the ending blend word game!\nYou earned the Finish it Right Badge!", finishItRightBadgeSprite, badgeClip);
    }

    public void ShowGrandFinaleTrophy()
    {
        currentRewardType = 5;
        SetupRewardView("READING STAR GRAND TROPHY!", "CONGRATULATIONS!\nYou completed the ENTIRE Phonics Book!\nYou are an Official Reading Star!", readingStarGrandTrophySprite, victoryClip);
    }

    public void ShowTrophy() => ShowGrandFinaleTrophy();
    public void ShowReward() => ShowGrandFinaleTrophy();

    private void SetupRewardView(string title, string desc, Sprite iconSprite, AudioClip audio)
    {
        AutoFindUIElements();

        if (rewardTitleLabel       != null) rewardTitleLabel.text       = title;
        if (rewardDescriptionLabel != null) rewardDescriptionLabel.text  = desc;

        if (currentRewardType == 5)
        {
            // Grand Finale: Show TrophyIcon on left & BadgeRecapWallContainer on right! Hide single BadgeIcon.
            if (badgeIcon != null) badgeIcon.gameObject.SetActive(false);

            if (trophyIcon != null)
            {
                if (iconSprite != null) trophyIcon.sprite = iconSprite;
                trophyIcon.gameObject.SetActive(true);
                StartCoroutine(AnimateTrophy(trophyIcon.gameObject));
            }

            if (badgeRecapWallContainer != null) badgeRecapWallContainer.SetActive(true);
        }
        else
        {
            // Section-wise Completion: Hide TrophyIcon & Recap Wall; show ONLY single BadgeIcon!
            if (trophyIcon != null) trophyIcon.gameObject.SetActive(false);
            if (badgeRecapWallContainer != null) badgeRecapWallContainer.SetActive(false);

            if (badgeIcon != null)
            {
                if (iconSprite != null) badgeIcon.sprite = iconSprite;
                badgeIcon.gameObject.SetActive(true);
                StartCoroutine(AnimateTrophy(badgeIcon.gameObject));
            }
        }

        if (starParticles != null) starParticles.Play();

        PlayRewardAudio(audio);

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.PlayCelebrationAnimation();

        // Reveal Continue Button (Redirects to Section Selection Signboard)
        if (continueButton != null) continueButton.gameObject.SetActive(true);

        // Reveal Next Button (Advances directly to Next Section Activity)
        if (manager != null) manager.ShowNextButton();
    }

    private IEnumerator AnimateTrophy(GameObject targetObj)
    {
        if (targetObj == null) yield break;
        Vector3 orig = targetObj.transform.localScale;
        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1.2f, elapsed / 0.4f);
            if (elapsed > 0.4f) scale = Mathf.Lerp(1.2f, 1.0f, (elapsed - 0.4f) / 0.2f);
            targetObj.transform.localScale = orig * scale;
            yield return null;
        }
        targetObj.transform.localScale = orig;
    }

    private void PlayRewardAudio(AudioClip targetClip)
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        AudioClip clip = targetClip;
        if (clip == null) clip = victoryClip;

        if (clip != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.spatialBlend = 0f;
            audioSource.volume       = 1f;
            audioSource.PlayOneShot(clip);
        }
    }

    public void OnContinueClicked()
    {
        // Continue Button -> Redirects to Section Selection Signboard!
        if (manager != null)
        {
            manager.ShowLevelSelection();
        }
    }
}
