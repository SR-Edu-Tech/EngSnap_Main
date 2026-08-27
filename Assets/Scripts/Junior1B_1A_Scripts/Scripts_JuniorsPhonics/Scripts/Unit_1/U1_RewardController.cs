using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U1_RewardController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI rewardTitleLabel;
    [SerializeField] private TextMeshProUGUI rewardDescriptionLabel;
    [SerializeField] private Image badgeIcon;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button nextUnitButton;

    [Header("Reward Sprite")]
    [SerializeField] private Sprite unit1StickerSprite;

    [Header("Particles")]
    [SerializeField] private ParticleSystem starParticles;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip unit1VictoryClip; // "ui_unit_done"

    private void Awake()
    {
        EnsureInit();
    }

    private void OnEnable()
    {
        EnsureInit();
        ShowReward();
    }

    private void EnsureInit()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (nextUnitButton != null)
        {
            nextUnitButton.onClick.RemoveAllListeners();
            nextUnitButton.onClick.AddListener(OnNextUnitClicked);
        }
    }

    public void ShowReward()
    {
        EnsureInit();

        if (rewardTitleLabel != null)
            rewardTitleLabel.text = "UNIT 1 COMPLETE!";

        if (rewardDescriptionLabel != null)
            rewardDescriptionLabel.text = "Wow! You finished Unit One!\nYou're an Alphabet Star!";

        if (badgeIcon != null && unit1StickerSprite != null)
            badgeIcon.sprite = unit1StickerSprite;

        if (starParticles != null)
            starParticles.Play();

        if (unit1VictoryClip != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(unit1VictoryClip);
        }

        if (badgeIcon != null)
        {
            StartCoroutine(AnimateBadge(badgeIcon.gameObject));
        }

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.PlayCelebrationAnimation();
        }
    }

    private IEnumerator AnimateBadge(GameObject badgeObj)
    {
        Vector3 orig = badgeObj.transform.localScale;
        badgeObj.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            badgeObj.transform.localScale = Vector3.Lerp(Vector3.zero, orig * 1.15f, t);
            yield return null;
        }

        elapsed = 0f;
        duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            badgeObj.transform.localScale = Vector3.Lerp(orig * 1.15f, orig, t);
            yield return null;
        }

        badgeObj.transform.localScale = orig;
    }

    private void OnContinueClicked()
    {
        gameObject.SetActive(false);

        GameObject sectionSelection = GameObject.Find("Unit_1_Section_Selection_Panels");
        if (sectionSelection != null)
        {
            sectionSelection.SetActive(true);
        }

        Unit_Selection_Panel_Phonics_Junior unitSel = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
        if (unitSel != null)
        {
            unitSel.Open_Unit_1_Lessons();
        }
    }

    private void OnNextUnitClicked()
    {
        gameObject.SetActive(false);

        Unit_Selection_Panel_Phonics_Junior unitSel = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
        if (unitSel != null)
        {
            unitSel.Open_Unit_2_Lessons();
        }
    }
}
