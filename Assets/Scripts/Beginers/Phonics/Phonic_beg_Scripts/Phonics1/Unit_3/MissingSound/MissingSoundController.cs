using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissingSoundController : MonoBehaviour
{
    [Header("Mascot & Subtitles")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Keyword & Partial Word UI")]
    [SerializeField] private Image pictureDisplayImage;
    [SerializeField] private TMP_Text partialWordText;
    [SerializeField] private Button speakerButton;

    [Header("Star Meter UI")]
    [SerializeField] private Image starMeterFillImage;
    [SerializeField] private TMP_Text starCountText;
    [SerializeField] private AudioClip starEarnedSfx;

    [Header("Letter Choice Cards")]
    [SerializeField] private MissingSoundChoiceCard[] choiceCards;

    [Header("Rounds Config")]
    [SerializeField] private MissingSoundData[] roundsData;

    [Header("Voice Script Clips")]
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip genericPraiseClip;
    [SerializeField] private AudioClip tryAgainClip;
    [SerializeField] private AudioClip completionClip;
    [SerializeField] private AudioClip unlockUnit4Clip;

    [Header("Badge, Trophy & Map Unlock UI")]
    [SerializeField] private GameObject soundStarBadgePopup;
    [SerializeField] private GameObject unit4ShineEffect;

    [Header("Rewards & Navigation")]
    [SerializeField] private GameObject confettiParticles;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject nextPanel;
    [SerializeField] private GameObject currentPanel;
    [SerializeField] private GameObject unitContentPanel;

    private int currentRoundIndex = 0;
    private int starsEarned = 0;
    private bool isTransitioning = false;
    private bool isActivityCompleted = false;

    public AudioSource SfxAudioSource => sfxAudioSource;
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        EnsureAudioSources();
    }

    private void EnsureAudioSources()
    {
        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }
        sfxAudioSource.spatialBlend = 0f;
        sfxAudioSource.volume = 1f;

        if (voiceAudioSource == null) voiceAudioSource = sfxAudioSource;
        else voiceAudioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        EnsureAudioSources();

        if (speakerButton != null)
        {
            speakerButton.onClick.RemoveAllListeners();
            speakerButton.onClick.AddListener(ReplayPromptSound);
        }

        if (continueButton != null)
        {
            Button btn = continueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(GoToNextPanel);
            }
        }
    }

    private void OnEnable()
    {
        ResetLevel();
        StartCoroutine(StartIntroOnNextFrame());
    }

    private IEnumerator StartIntroOnNextFrame()
    {
        yield return null;
        if (gameObject.activeInHierarchy && !isActivityCompleted)
        {
            StartCoroutine(IntroSequence());
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void ResetLevel()
    {
        StopAllCoroutines();
        currentRoundIndex = 0;
        starsEarned = 0;
        isTransitioning = false;
        isActivityCompleted = false;

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (soundStarBadgePopup != null) soundStarBadgePopup.SetActive(false);
        if (unit4ShineEffect != null) unit4ShineEffect.SetActive(false);
        if (confettiParticles != null) confettiParticles.SetActive(false);

        UpdateStarMeterUI();
        LoadRound(currentRoundIndex);
    }

    private IEnumerator IntroSequence()
    {
        isTransitioning = true;
        SetSubtitle("Say the picture. What is the LAST sound you hear? Tap the letter!");

        if (introClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = introClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(introClip.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        PlayCurrentRoundPrompt();
        isTransitioning = false;
    }

    private void LoadRound(int index)
    {
        if (roundsData == null || roundsData.Length == 0) return;
        if (index >= roundsData.Length)
        {
            StartCoroutine(CompleteActivitySequence());
            return;
        }

        MissingSoundData data = roundsData[index];

        if (pictureDisplayImage != null && data.keywordSprite != null)
        {
            pictureDisplayImage.sprite = data.keywordSprite;
            pictureDisplayImage.enabled = true;
        }

        if (partialWordText != null) partialWordText.text = data.partialWord;

        // Setup choices
        for (int i = 0; i < choiceCards.Length; i++)
        {
            if (choiceCards[i] != null)
            {
                choiceCards[i].ResetState();
                if (data.choiceLetters != null && i < data.choiceLetters.Length)
                {
                    choiceCards[i].Setup(data.choiceLetters[i], OnChoiceCardSelected);
                    choiceCards[i].gameObject.SetActive(true);
                }
                else
                {
                    choiceCards[i].gameObject.SetActive(false);
                }
            }
        }

        SetSubtitle($"What is the last sound in '{data.completedWord}'?");
    }

    private void ReplayPromptSound()
    {
        if (isTransitioning) return;
        PlayCurrentRoundPrompt();
    }

    private void PlayCurrentRoundPrompt()
    {
        if (roundsData != null && currentRoundIndex < roundsData.Length)
        {
            MissingSoundData data = roundsData[currentRoundIndex];
            if (data.roundPromptClip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = data.roundPromptClip;
                voiceAudioSource.Play();
            }
        }
    }

    private void OnChoiceCardSelected(MissingSoundChoiceCard card)
    {
        if (isTransitioning || card == null) return;
        StartCoroutine(ProcessChoiceSequence(card));
    }

    private IEnumerator ProcessChoiceSequence(MissingSoundChoiceCard card)
    {
        isTransitioning = true;
        MissingSoundData data = roundsData[currentRoundIndex];

        bool isCorrect = (card.Letter.ToLower().Trim() == data.correctLetter.ToLower().Trim());
        card.SetState(isCorrect);

        if (isCorrect)
        {
            // Drop letter into blank
            if (partialWordText != null) partialWordText.text = data.completedWord;

            starsEarned++;
            UpdateStarMeterUI();

            if (starEarnedSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(starEarnedSfx);
            }

            // Play completed word clip
            if (data.completedWordClip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = data.completedWordClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(data.completedWordClip.length + 0.3f);
            }

            if (genericPraiseClip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = genericPraiseClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(genericPraiseClip.length + 0.3f);
            }

            currentRoundIndex++;
            if (currentRoundIndex < roundsData.Length)
            {
                LoadRound(currentRoundIndex);
                PlayCurrentRoundPrompt();
                isTransitioning = false;
            }
            else
            {
                StartCoroutine(CompleteActivitySequence());
            }
        }
        else
        {
            SetSubtitle("Try again! Listen for the last sound.");
            if (tryAgainClip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = tryAgainClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(tryAgainClip.length + 0.3f);
            }

            card.ResetState();
            isTransitioning = false;
        }
    }

    private void UpdateStarMeterUI()
    {
        int totalRounds = roundsData != null ? roundsData.Length : 5;
        if (starMeterFillImage != null) starMeterFillImage.fillAmount = (float)starsEarned / totalRounds;
        if (starCountText != null) starCountText.text = $"{starsEarned}/{totalRounds}";
    }

    private IEnumerator CompleteActivitySequence()
    {
        isActivityCompleted = true;
        SetSubtitle("Yes! You are a Sound Star! Unit 4 is open!");

        // Mark topic complete immediately in TopicProgressUI
        TopicProgressUI.MarkTopicComplete(gameObject);

        if (soundStarBadgePopup != null) soundStarBadgePopup.SetActive(true);
        if (unit4ShineEffect != null) unit4ShineEffect.SetActive(true);
        if (confettiParticles != null) confettiParticles.SetActive(true);
        if (rewardPopup != null) rewardPopup.SetActive(true);
        if (continueButton != null) continueButton.SetActive(true);

        if (completionClip != null && voiceAudioSource != null && voiceAudioSource.gameObject.activeInHierarchy)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = completionClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(completionClip.length + 0.2f);
        }

        if (unlockUnit4Clip != null && voiceAudioSource != null && voiceAudioSource.gameObject.activeInHierarchy)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = unlockUnit4Clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(unlockUnit4Clip.length + 0.3f);
        }
    }

    public void GoToNextPanel()
    {
        if (isActivityCompleted)
        {
            TopicProgressUI.MarkTopicComplete(gameObject);
        }

        ResetLevel();

        if (nextPanel != null)
        {
            nextPanel.SetActive(true);
            if (unitContentPanel != null && nextPanel != unitContentPanel && !nextPanel.transform.IsChildOf(unitContentPanel.transform))
            {
                unitContentPanel.SetActive(false);
            }
        }
        else if (unitContentPanel != null)
        {
            unitContentPanel.SetActive(true);
        }

        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        TopicProgressUI.RefreshAllTicks();
    }

    private void SetSubtitle(string text)
    {
        EngSnap.Common.DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
    }
}
