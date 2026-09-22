using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Core Manager for Unit 6: Colour Your Speech — RWD: "Idiom Artist Badge".
/// Celebratory completion scene shown after Unit 6 completion.
/// Awards the M3A_U6_IdiomArtist badge, marks Unit 6 complete, and handles COLLECT -> CONTINUE transition.
/// </summary>
public class Masters_ColourYourSpeech_Rewards_LessonOne : Masters_Lesson {

    public const string BADGE_ID = "M3A_U6_IdiomArtist";

    [Header("Header UI")]
    [SerializeField] private TextMeshProUGUI unitLabelTMP;
    [SerializeField] private TextMeshProUGUI rewardTitleTMP;
    [SerializeField] private TextMeshProUGUI rewardSubtitleTMP;
    [SerializeField] private Button backButton;

    [Header("Badge & Reward Area")]
    [SerializeField] private GameObject badgeContainer;
    [SerializeField] private Image badgeGlowImage;
    [SerializeField] private Image badgeIconImage;
    [SerializeField] private TextMeshProUGUI badgeNameTMP;
    [SerializeField] private TextMeshProUGUI badgeSubtitleTMP;

    [Header("Host & Speech")]
    [SerializeField] private GameObject hostContainer;
    [SerializeField] private Image hostAvatarImage;
    [SerializeField] private TextMeshProUGUI hostSpeechBubbleTMP;

    [Header("Gallery Showcase (Idiom Cards)")]
    [SerializeField] private GameObject galleryContainer;
    [SerializeField] private GameObject[] idiomCards;
    [SerializeField] private TextMeshProUGUI[] idiomCardTexts;
    [SerializeField] private Image[] idiomCardBorders;

    [Header("Summary Takeaways")]
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private TextMeshProUGUI takeawaySwapTMP;
    [SerializeField] private TextMeshProUGUI takeawayLiteralTMP;

    [Header("Button Flow (Collect -> Continue)")]
    [SerializeField] private Button collectButton;
    [SerializeField] private TextMeshProUGUI collectButtonTMP;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonTMP;

    [Header("Audio & SFX")]
    [SerializeField] private AudioClip ariaCongratClip;
    [SerializeField] private AudioClip ariaSummaryClip;
    [SerializeField] private AudioClip sfxSplat;
    [SerializeField] private AudioClip sfxBrush;
    [SerializeField] private AudioClip sfxBadge;

    [Header("Visual Effects")]
    [SerializeField] private GameObject[] paintSplatters;

    // Runtime state
    private bool isCollected = false;
    private Coroutine celebrationCoroutine = null;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Rewards;
        narratorSpeech = null;

        if (backButton != null) {
            var mb = backButton.GetComponent<Masters_BackButton>() ?? backButton.GetComponentInParent<Masters_BackButton>();
            if (mb == null) {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        if (collectButton != null) {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(OnCollectButtonClicked);
        }

        if (continueButton != null) {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
    }

    protected override void Start() {
        narratorSpeech = null;
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        // Check if already awarded previously
        isCollected = M3A_U6_HubProgress.HasIdiomArtistBadge;

        if (isCollected) {
            SetupPostCollectionState();
        } else {
            SetupPreCollectionState();
        }

        celebrationCoroutine = StartCoroutine(CelebrationRoutine());
    }

    private void OnDisable() {
        if (celebrationCoroutine != null) {
            StopCoroutine(celebrationCoroutine);
            celebrationCoroutine = null;
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
    }

    private void OnDestroy() {
        if (celebrationCoroutine != null) {
            StopCoroutine(celebrationCoroutine);
            celebrationCoroutine = null;
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
    }

    private void SetupPreCollectionState() {
        if (collectButton != null) {
            collectButton.gameObject.SetActive(true);
            collectButton.interactable = true;
            if (collectButtonTMP != null) collectButtonTMP.text = "COLLECT BADGE";
        }
        if (continueButton != null) continueButton.gameObject.SetActive(false);
    }

    private void SetupPostCollectionState() {
        if (collectButton != null) collectButton.gameObject.SetActive(false);
        if (continueButton != null) {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
            if (continueButtonTMP != null) continueButtonTMP.text = "CONTINUE TO UNIT MAP";
            continueButton.transform.DOScale(Vector3.one * 1.05f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    private IEnumerator CelebrationRoutine() {
        // Step 1: Stop any active voice-over
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        // Pop celebratory paint splatters
        if (paintSplatters != null) {
            for (int i = 0; i < paintSplatters.Length; i++) {
                if (paintSplatters[i] != null) {
                    paintSplatters[i].SetActive(true);
                    paintSplatters[i].transform.localScale = Vector3.zero;
                    paintSplatters[i].transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
                    yield return new WaitForSeconds(0.08f);
                }
            }
        }

        // Play splat sound
        if (sfxSplat != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(sfxSplat);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        // Step 2: Animate Badge reveal
        if (badgeContainer != null) {
            badgeContainer.transform.localScale = Vector3.zero;
            badgeContainer.transform.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBounce);
        }

        if (badgeGlowImage != null) {
            badgeGlowImage.transform.DOScale(Vector3.one * 1.15f, 1.2f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        // Step 3: Reveal Gallery cards with sequential pop
        if (idiomCards != null) {
            for (int i = 0; i < idiomCards.Length; i++) {
                if (idiomCards[i] != null) {
                    idiomCards[i].SetActive(true);
                    idiomCards[i].transform.localScale = Vector3.zero;
                    idiomCards[i].transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                    yield return new WaitForSeconds(0.06f);
                }
            }
        }

        // Step 4: Play Congratulatory ARIA VoiceOver
        if (ariaCongratClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaCongratClip);
            if (hostSpeechBubbleTMP != null) {
                hostSpeechBubbleTMP.text = "\"You're a true Idiom Artist now — your speech is full of colour!\"";
            }
            yield return new WaitForSeconds(ariaCongratClip.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        // Step 5: Summary voiceover
        if (ariaSummaryClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaSummaryClip);
        }
    }

    public void OnCollectButtonClicked() {
        if (isCollected) return;
        isCollected = true;

        // 1. Award badge and persist in PlayerPrefs and HubProgress
        M3A_U6_HubProgress.AwardIdiomArtistBadge();
        M3A_U6_HubProgress.MarkUnitComplete();

        // 2. Play badge sound & sparkle
        if (sfxBadge != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(sfxBadge);
        } else if (sfxBrush != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(sfxBrush);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        // 3. Animate badge collection
        if (badgeContainer != null) {
            badgeContainer.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 10, 1f);
        }

        // 4. Transition button from COLLECT to CONTINUE
        SetupPostCollectionState();
    }

    private bool isNavigating = false;

    public void OnContinueButtonClicked() {
        if (isNavigating) return;
        isNavigating = true;

        if (continueButton != null) {
            continueButton.interactable = false;
        }

        if (celebrationCoroutine != null) {
            StopCoroutine(celebrationCoroutine);
            celebrationCoroutine = null;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        // Mark completion in HubProgress
        M3A_U6_HubProgress.AwardIdiomArtistBadge();
        M3A_U6_HubProgress.MarkUnitComplete();

        // Inform LevelManager
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        OnContinueButtonClicked();
    }

    private void OnBackButtonClicked() {
        if (celebrationCoroutine != null) {
            StopCoroutine(celebrationCoroutine);
            celebrationCoroutine = null;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
    }
}
