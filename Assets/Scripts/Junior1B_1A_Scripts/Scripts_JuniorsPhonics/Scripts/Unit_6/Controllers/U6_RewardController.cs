using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U6_RewardController : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI rewardTitleLabel;
    public TextMeshProUGUI rewardDescriptionLabel;
    public Image badgeIcon;
    public Button continueButton;

    [Header("Badge & Trophy Sprites")]
    public Sprite longABadgeSprite;
    public Sprite longEBadgeSprite;
    public Sprite unitTrophySprite;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip victoryClip;

    public U6_Manager manager;

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

    public void SetupReward(U6_LevelData levelData)
    {
        if (manager == null) manager = FindFirstObjectByType<U6_Manager>(FindObjectsInactive.Include);

        if (levelData != null)
        {
            string title = levelData.levelTitle != null ? levelData.levelTitle.ToLower() : "";
            bool isLongE = (manager != null && levelData == manager.levelLongE) || title.Contains("long e") || title.Contains("long_e") || title.Contains("section e") || title.EndsWith(" e");

            if (isLongE)
            {
                if (rewardTitleLabel != null) rewardTitleLabel.text = "LONG E MASTER!";
                if (rewardDescriptionLabel != null) rewardDescriptionLabel.text = "You earned the Long-E Badge!";
                if (badgeIcon != null && longEBadgeSprite != null) badgeIcon.sprite = longEBadgeSprite;
            }
            else
            {
                if (rewardTitleLabel != null) rewardTitleLabel.text = "LONG A MASTER!";
                if (rewardDescriptionLabel != null) rewardDescriptionLabel.text = "You earned the Long-A Badge!";
                if (badgeIcon != null && longABadgeSprite != null) badgeIcon.sprite = longABadgeSprite;
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
            manager.OnRewardFinished();
        }
        else
        {
            U6_Manager mgr = FindFirstObjectByType<U6_Manager>(FindObjectsInactive.Include);
            if (mgr != null) mgr.OnRewardFinished();
        }
    }
}
