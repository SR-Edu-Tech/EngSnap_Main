using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U7_RewardController : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI rewardTitleLabel;
    public TextMeshProUGUI rewardDescriptionLabel;
    public Image badgeIcon;
    public Button continueButton;

    [Header("Badge & Trophy Sprites")]
    public Sprite longIBadgeSprite;
    public Sprite longOBadgeSprite;
    public Sprite longUBadgeSprite;
    public Sprite championTrophySprite;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip victoryClip;

    public U7_Manager manager;

    private static bool longICompleted = false;
    private static bool longOCompleted = false;
    private static bool longUCompleted = false;

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

    public void SetupReward(U7_LevelData levelData)
    {
        if (levelData != null)
        {
            string title = levelData.levelTitle != null ? levelData.levelTitle.ToLower() : "";

            // Use specific phrases to avoid false matches (e.g. "i" matching "Phonics")
            if (title.Contains("long i") || title.Contains("long_i") || title.Contains("i teams"))
                longICompleted = true;
            else if (title.Contains("long o") || title.Contains("long_o") || title.Contains("o teams"))
                longOCompleted = true;
            else if (title.Contains("long u") || title.Contains("long_u") || title.Contains("u teams"))
                longUCompleted = true;

            // Determine Reward Badge or Champion Trophy
            if (longICompleted && longOCompleted && longUCompleted)
            {
                if (rewardTitleLabel != null) rewardTitleLabel.text = "LONG VOWEL CHAMPION!";
                if (rewardDescriptionLabel != null) rewardDescriptionLabel.text = "You know all long vowels! You earned the Champion Trophy!";
                if (badgeIcon != null && championTrophySprite != null) badgeIcon.sprite = championTrophySprite;
            }
            else if (title.Contains("long u") || title.Contains("long_u") || title.Contains("u teams"))
            {
                if (rewardTitleLabel != null) rewardTitleLabel.text = "LONG U MASTER!";
                if (rewardDescriptionLabel != null) rewardDescriptionLabel.text = "You earned the Long-U Badge!";
                if (badgeIcon != null && longUBadgeSprite != null) badgeIcon.sprite = longUBadgeSprite;
            }
            else if (title.Contains("long o") || title.Contains("long_o") || title.Contains("o teams"))
            {
                if (rewardTitleLabel != null) rewardTitleLabel.text = "LONG O MASTER!";
                if (rewardDescriptionLabel != null) rewardDescriptionLabel.text = "You earned the Long-O Badge!";
                if (badgeIcon != null && longOBadgeSprite != null) badgeIcon.sprite = longOBadgeSprite;
            }
            else
            {
                if (rewardTitleLabel != null) rewardTitleLabel.text = "LONG I MASTER!";
                if (rewardDescriptionLabel != null) rewardDescriptionLabel.text = "You earned the Long-I Badge!";
                if (badgeIcon != null && longIBadgeSprite != null) badgeIcon.sprite = longIBadgeSprite;
            }
        }

        // Play Victory Audio
        PlayVictoryAudio();

        // Animate Badge / Trophy Icon
        if (badgeIcon != null)
        {
            StartCoroutine(AnimateBadge(badgeIcon.gameObject));
        }

        // Mascot Cheer
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.PlayHiAnimation();
    }

    public void PlayVictoryAudio()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        AudioClip clipToPlay = victoryClip;
        if (clipToPlay == null)
        {
            clipToPlay = Resources.Load<AudioClip>("Audio/Great job");
#if UNITY_EDITOR
            if (clipToPlay == null) clipToPlay = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio Clips/General/Great job.mp3");
#endif
        }

        if (clipToPlay != null)
        {
            if (audioSource != null)
            {
                audioSource.spatialBlend = 0f;
                audioSource.volume = 1f;
                audioSource.mute = false;
                audioSource.PlayOneShot(clipToPlay);
            }
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clipToPlay, camPos);
        }
    }

    private IEnumerator AnimateBadge(GameObject badgeObj)
    {
        Vector3 initialScale = Vector3.one;
        float elapsed = 0f;
        float duration = 0.6f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = 1f + 0.3f * Mathf.Sin((elapsed / duration) * Mathf.PI);
            badgeObj.transform.localScale = initialScale * scale;
            yield return null;
        }

        badgeObj.transform.localScale = initialScale;
    }

    private void OnContinueClicked()
    {
        if (manager != null)
        {
            manager.ShowLevelSelection();
        }
    }
}
