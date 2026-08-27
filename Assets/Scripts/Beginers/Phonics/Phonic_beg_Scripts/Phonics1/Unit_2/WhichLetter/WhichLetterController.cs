using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WhichLetterController : MonoBehaviour
{
    [Header("Mascot & Subtitles")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Keyword Picture Display")]
    [SerializeField] private Image pictureDisplayImage;
    [SerializeField] private TMP_Text pictureDisplayText;
    [SerializeField] private Button speakerButton;

    [Header("Star Meter")]
    [SerializeField] private Image starMeterFillImage;
    [SerializeField] private TMP_Text starCountText;
    [SerializeField] private AudioClip starEarnedSfx;

    [Header("Letter Choice Cards")]
    [SerializeField] private WhichLetterChoiceCard[] letterCards;

    [Header("Rounds Config")]
    [SerializeField] private WhichLetterData[] roundsData;

    [Header("Voice Script Clips")]
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip genericPraiseClip;
    [SerializeField] private AudioClip tryAgainClip;
    [SerializeField] private AudioClip completionClip;
    [SerializeField] private AudioClip unlockUnit3Clip;

    [Header("Badge, Trophy & Map Unlock UI")]
    [SerializeField] private GameObject letterStarBadgePopup;
    [SerializeField] private GameObject unit3ShineEffect;

    [Header("Rewards & Navigation")]
    [SerializeField] private GameObject confettiParticles;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject nextPanel;
    [SerializeField] private GameObject currentPanel;
    [SerializeField]private GameObject unitContentPanel;

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
            speakerButton.onClick.AddListener(ReplayTargetSound);
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

        ResetLevel();
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
            StopCoroutine(IntroSequence());
            StartCoroutine(IntroSequence());
        }
    }

    private void OnDisable()
    {
        ResetLevel();
    }

    public void ResetLevel()
    {
        EnsureAudioSources();
        StopAllCoroutines();

        if (voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
        }
        if (sfxAudioSource != null) sfxAudioSource.Stop();

        currentRoundIndex = 0;
        starsEarned = 0;
        isTransitioning = false;
        isActivityCompleted = false;

        if (letterStarBadgePopup != null) letterStarBadgePopup.SetActive(false);
        if (unit3ShineEffect != null) unit3ShineEffect.SetActive(false);
        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);
        if (confettiParticles != null) confettiParticles.SetActive(false);
        SetDialogue("");

        if (letterCards != null)
        {
            foreach (var card in letterCards)
            {
                if (card != null)
                {
                    card.SetInteractable(false);
                    card.SetGlow(false);
                }
            }
        }

        UpdateStarMeterUI();
        LoadRoundCards(0);

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(IntroSequence());
        }
    }

    private void LoadRoundCards(int roundIdx)
    {
        if (roundsData == null || roundIdx >= roundsData.Length) return;

        WhichLetterData round = roundsData[roundIdx];
        if (round == null) return;

        if (pictureDisplayImage != null)
        {
            pictureDisplayImage.sprite = round.keywordSprite;
            pictureDisplayImage.enabled = round.keywordSprite != null;
        }
        if (pictureDisplayText != null)
        {
            pictureDisplayText.text = round.keywordWord ?? "";
        }

        if (letterCards != null && round.choices != null)
        {
            for (int i = 0; i < letterCards.Length; i++)
            {
                if (letterCards[i] == null) continue;

                if (i < round.choices.Length && round.choices[i] != null)
                {
                    letterCards[i].gameObject.SetActive(true);
                    letterCards[i].Setup(round.choices[i], this);
                    letterCards[i].SetInteractable(false);
                    letterCards[i].SetGlow(false);
                }
                else
                {
                    letterCards[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private IEnumerator IntroSequence()
    {
        isTransitioning = true;
        SetCardsInteractable(false);

        SetDialogue("Let's play! Look at the picture and pick the first letter.");

        if (introClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = introClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(introClip.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(2.5f);
        }

        SetDialogue("");

        LoadCurrentRound();
    }

    public void ReplayIntroAudio()
    {
        EnsureAudioSources();
        if (introClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = introClip;
            voiceAudioSource.Play();

            SetDialogue("Let's play! Look at the picture and pick the first letter.");
        }
    }

    private void LoadCurrentRound()
    {
        if (roundsData == null || currentRoundIndex >= roundsData.Length)
        {
            StartCoroutine(CompleteSequence());
            return;
        }

        WhichLetterData round = roundsData[currentRoundIndex];
        if (round == null) return;

        // Display Picture Prompt
        if (pictureDisplayImage != null)
        {
            pictureDisplayImage.sprite = round.keywordSprite;
            pictureDisplayImage.enabled = round.keywordSprite != null;
        }
        if (pictureDisplayText != null)
        {
            pictureDisplayText.text = round.keywordWord ?? "";
        }

        SetDialogue($"Which letter does '{round.keywordWord}' start with?");

        // Prompt Audio
        if (round.promptAudioClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = round.promptAudioClip;
            voiceAudioSource.Play();
        }

        // Setup Choice Buttons
        if (letterCards != null && round.choices != null)
        {
            for (int i = 0; i < letterCards.Length; i++)
            {
                if (letterCards[i] == null) continue;

                if (i < round.choices.Length && round.choices[i] != null)
                {
                    letterCards[i].gameObject.SetActive(true);
                    letterCards[i].Setup(round.choices[i], this);
                }
                else
                {
                    letterCards[i].gameObject.SetActive(false);
                }
            }
        }

        SetCardsInteractable(true);
        isTransitioning = false;
    }

    public void ReplayTargetSound()
    {
        if (isTransitioning || roundsData == null || currentRoundIndex >= roundsData.Length) return;

        WhichLetterData round = roundsData[currentRoundIndex];
        if (round != null && round.promptAudioClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = round.promptAudioClip;
            voiceAudioSource.Play();
        }
    }

    public void OnCardSelected(WhichLetterChoiceCard card)
    {
        if (isTransitioning || isActivityCompleted || card == null || roundsData == null || currentRoundIndex >= roundsData.Length)
            return;

        WhichLetterData currentRound = roundsData[currentRoundIndex];
        if (currentRound == null) return;

        bool isCorrect = string.Equals(card.Letter, currentRound.targetLetter, System.StringComparison.OrdinalIgnoreCase);

        if (isCorrect)
        {
            StartCoroutine(HandleCorrectSelection(card, currentRound));
        }
        else
        {
            StartCoroutine(HandleWrongSelection(card));
        }
    }

    private IEnumerator HandleCorrectSelection(WhichLetterChoiceCard card, WhichLetterData round)
    {
        isTransitioning = true;
        SetCardsInteractable(false);

        card.PlayDanceAnimation();

        starsEarned++;
        UpdateStarMeterUI();

        if (starEarnedSfx != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(starEarnedSfx);
        }

        SetDialogue($"Yes! {round.keywordWord} starts with {round.targetLetter.ToUpper()}!");

        AudioClip praiseToPlay = round.praiseAudioClip != null ? round.praiseAudioClip : genericPraiseClip;
        if (praiseToPlay != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = praiseToPlay;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(praiseToPlay.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(1.2f);
        }

        SetDialogue("");

        currentRoundIndex++;

        if (currentRoundIndex < roundsData.Length)
        {
            LoadCurrentRound();
        }
        else
        {
            yield return StartCoroutine(CompleteSequence());
        }
    }

    private IEnumerator HandleWrongSelection(WhichLetterChoiceCard card)
    {
        isTransitioning = true;

        card.PlayWiggleAnimation();

        SetDialogue("Oops, try again! Listen carefully.");

        if (tryAgainClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = tryAgainClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(tryAgainClip.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.8f);
        }

        SetDialogue("");

        ReplayTargetSound();

        isTransitioning = false;
        SetCardsInteractable(true);
    }

    private IEnumerator CompleteSequence()
    {
        isActivityCompleted = true;
        isTransitioning = true;
        SetCardsInteractable(false);

        if (confettiParticles != null)
        {
            confettiParticles.SetActive(true);
        }

        // Mark topic complete immediately in TopicProgressUI
        TopicProgressUI.MarkTopicComplete(gameObject);

        // Save Unit 2 Completion Progress
        PlayerPrefs.SetInt("Unit2_Completed", 1);
        PlayerPrefs.Save();

        if (confettiParticles != null) confettiParticles.SetActive(true);

        if (letterCards != null)
        {
            foreach (var card in letterCards)
            {
                if (card != null && card.gameObject.activeSelf)
                {
                    card.TriggerConfetti();
                    yield return new WaitForSeconds(0.08f);
                }
            }
        }

        // Show Letter Star Badge
        if (letterStarBadgePopup != null) letterStarBadgePopup.SetActive(true);

        if (completionClip != null && voiceAudioSource != null)
        {
            if (dialogueText != null) dialogueText.text = "You are a Letter Star! You finished Unit 2. A new world is open!";
            voiceAudioSource.Stop();
            voiceAudioSource.clip = completionClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(completionClip.length + 0.4f);
        }

        // Unit 3 Shine & Unlock
        if (unit3ShineEffect != null) unit3ShineEffect.SetActive(true);

        if (unlockUnit3Clip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = unlockUnit3Clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(unlockUnit3Clip.length + 0.4f);
        }

        if (rewardPopup != null) rewardPopup.SetActive(true);
        if (continueButton != null) continueButton.SetActive(true);
    }

    private void UpdateStarMeterUI()
    {
        int totalRounds = roundsData != null ? roundsData.Length : 4;
        float fillAmount = totalRounds > 0 ? Mathf.Clamp01((float)starsEarned / totalRounds) : 0f;

        if (starMeterFillImage != null) starMeterFillImage.fillAmount = fillAmount;
        if (starCountText != null) starCountText.text = $"{starsEarned} / {totalRounds} Stars";
    }

    private void SetCardsInteractable(bool state)
    {
        if (letterCards == null) return;
        foreach (var card in letterCards)
        {
            if (card != null) card.SetInteractable(state);
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

    private void SetDialogue(string text)
    {
        EngSnap.Common.DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
    }
}
