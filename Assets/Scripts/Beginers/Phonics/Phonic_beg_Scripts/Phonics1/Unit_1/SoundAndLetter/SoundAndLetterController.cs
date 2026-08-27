using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundAndLetterController : MonoBehaviour
{
    [Header("Mascot & Subtitles")]
    [Tooltip("Animator of the mascot character.")]
    [SerializeField] private Animator mascotAnimator;

    [Tooltip("Text component to display mascot subtitles.")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Audio Sources")]
    [Tooltip("Audio source for mascot narration voice-overs.")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Tooltip("Audio source for letter sounds and interface sound effects.")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Speaker / Ear Replay Icon")]
    [Tooltip("Button to replay the current target sound.")]
    [SerializeField] private Button speakerButton;

    [Tooltip("Animator or GameObject for speaker pulse effect when sound plays.")]
    [SerializeField] private Animator speakerAnimator;

    [Header("Star Meter")]
    [Tooltip("UI Image (type Filled) acting as the Star Meter progress bar.")]
    [SerializeField] private Image starMeterFillImage;

    [Tooltip("Text to show star count e.g. '3 / 4 Stars'.")]
    [SerializeField] private TMP_Text starCountText;

    [Tooltip("Optional Animator on Star Meter to trigger 'Wiggle' animation when filled.")]
    [SerializeField] private Animator starMeterAnimator;

    [Tooltip("Optional RectTransform on Star Meter for code-driven wiggle bounce.")]
    [SerializeField] private RectTransform starMeterTransform;

    [Tooltip("SFX played when a star is earned.")]
    [SerializeField] private AudioClip starEarnedSfx;

    [Header("Letter Choice Cards")]
    [Tooltip("The 3 big choice letter cards.")]
    [SerializeField] private SoundAndLetterCard[] letterCards;

    [Header("Voice Script Audio Clips")]
    [Tooltip("Intro: 'When we hear a sound, we write a letter for it. Listen, then tap the right letter!'")]
    [SerializeField] private AudioClip introClip;

    [Tooltip("Praise: 'Perfect! That letter makes the sound...' or 'Wonderful! You are a Phonics Star!'")]
    [SerializeField] private AudioClip genericPraiseClip;

    [Tooltip("Try again: 'Oops, try again! Listen carefully.'")]
    [SerializeField] private AudioClip tryAgainClip;

    [Tooltip("Completion: 'Fantastic! You know your sounds and letters!'")]
    [SerializeField] private AudioClip completionClip;

    [Tooltip("Unlock Unit 2: 'You finished Unit 1! A new world is open. Let's keep going!'")]
    [SerializeField] private AudioClip unlockUnit2Clip;

    [Header("Activity Rounds Config")]
    [Tooltip("The rounds configuration.")]
    [SerializeField] private SoundAndLetterData[] roundsData;

    [Header("Badge, Trophy & Map Unlock UI")]
    [Tooltip("Popup displaying the 'Phonics Star!' badge & trophy.")]
    [SerializeField] private GameObject starBadgePopup;

    [Tooltip("Animator for the trophy celebration.")]
    [SerializeField] private Animator trophyAnimator;

    [Tooltip("Map FX / Shine animation showing Unit 2 unlocking.")]
    [SerializeField] private GameObject unit2ShineEffect;

    [Header("Rewards & Progression")]
    [SerializeField] private GameObject confettiParticles;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject nextPanel;
    [SerializeField] private GameObject currentPanel;
    [SerializeField] private GameObject unitContentPanel;

    // Internal State
    private int currentRoundIndex = 0;
    private int starsEarned = 0;
    private bool isTransitioning = false;
    private bool isActivityCompleted = false;

    public AudioSource SfxAudioSource => sfxAudioSource;
    public bool IsTransitioning => isTransitioning;

    private bool isStarted = false;

    private void Awake()
    {
        EnsureAudioSources();
    }

    private void EnsureAudioSources()
    {
        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null)
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        if (sfxAudioSource != null)
        {
            sfxAudioSource.spatialBlend = 0f;
            sfxAudioSource.volume = 1f;
            sfxAudioSource.loop = false;
        }

        if (voiceAudioSource == null)
        {
            voiceAudioSource = sfxAudioSource;
        }
        else
        {
            voiceAudioSource.spatialBlend = 0f;
            voiceAudioSource.volume = 1f;
            voiceAudioSource.loop = false;
        }
    }

    private void Start()
    {
        EnsureAudioSources();

        // Setup Speaker Replay Button
        if (speakerButton != null)
        {
            speakerButton.onClick.RemoveAllListeners();
            speakerButton.onClick.AddListener(ReplayTargetSound);
        }

        // Setup Continue Button
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

    /// <summary>
    /// Resets the Sound and Letter activity completely.
    /// </summary>
    public void ResetLevel()
    {
        EnsureAudioSources();
        StopAllCoroutines();

        if (voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
        }
        if (sfxAudioSource != null)
        {
            sfxAudioSource.Stop();
        }

        currentRoundIndex = 0;
        starsEarned = 0;
        isTransitioning = false;
        isActivityCompleted = false;

        if (starBadgePopup != null) starBadgePopup.SetActive(false);
        if (unit2ShineEffect != null) unit2ShineEffect.SetActive(false);
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
                    card.ResetVisualState();
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

        SoundAndLetterData round = roundsData[roundIdx];
        if (round == null) return;

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
                    letterCards[i].ResetVisualState();
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

        SetDialogue("When we hear a sound, we write a letter for it. Listen, then tap the right letter!");

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

            SetDialogue("When we hear a sound, we write a letter for it. Listen, then tap the right letter!");
        }
    }

    private void LoadCurrentRound()
    {
        if (roundsData == null || currentRoundIndex >= roundsData.Length)
        {
            StartCoroutine(CompleteSequence());
            return;
        }

        SoundAndLetterData round = roundsData[currentRoundIndex];
        if (round == null) return;

        // Show Mascot Prompt Dialogue & Subtitles
        if (!string.IsNullOrEmpty(round.promptText))
        {
            SetDialogue(round.promptText);
        }

        // Play Prompt Voice Clip
        if (round.promptAudioClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = round.promptAudioClip;
            voiceAudioSource.Play();
        }

        // Setup Choice Letter Cards
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

        // Play Target Sound if prompt clip is null
        if (round.promptAudioClip == null && round.targetSoundClip != null)
        {
            ReplayTargetSound();
        }
    }

    private IEnumerator PlayTargetSoundRoutine(SoundAndLetterData round)
    {
        if (round == null || round.targetSoundClip == null) yield break;

        TriggerSpeakerPulseAnimation();

        if (sfxAudioSource != null)
        {
            sfxAudioSource.Stop();
            sfxAudioSource.clip = round.targetSoundClip;
            sfxAudioSource.Play();
        }

        yield return null;
    }

    public void ReplayTargetSound()
    {
        if (isTransitioning || roundsData == null || currentRoundIndex >= roundsData.Length) return;

        SoundAndLetterData round = roundsData[currentRoundIndex];
        if (round != null)
        {
            AudioClip clipToPlay = round.promptAudioClip != null ? round.promptAudioClip : round.targetSoundClip;
            if (clipToPlay != null && sfxAudioSource != null)
            {
                TriggerSpeakerPulseAnimation();
                sfxAudioSource.Stop();
                sfxAudioSource.clip = clipToPlay;
                sfxAudioSource.Play();
            }
        }
    }

    public void OnCardSelected(SoundAndLetterCard card)
    {
        if (isTransitioning || isActivityCompleted || card == null || roundsData == null || currentRoundIndex >= roundsData.Length)
            return;

        SoundAndLetterData currentRound = roundsData[currentRoundIndex];
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

    private IEnumerator HandleCorrectSelection(SoundAndLetterCard card, SoundAndLetterData round)
    {
        isTransitioning = true;
        SetCardsInteractable(false);

        // Highlight selected correct option in GREEN & gray out the rest
        if (letterCards != null)
        {
            foreach (var c in letterCards)
            {
                if (c == null || !c.gameObject.activeSelf) continue;
                if (c == card)
                {
                    c.SetGreenHighlight();
                }
                else
                {
                    c.SetGrayedOut(true);
                }
            }
        }

        // Letter card happy dance & confetti burst
        card.PlayDanceAnimation();

        // Earn Star & Wiggle Star Meter UI
        starsEarned++;
        UpdateStarMeterUI(triggerWiggle: true);

        if (starEarnedSfx != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(starEarnedSfx);
        }

        // Play letter sound clip
        if (card.ChoiceData != null && card.ChoiceData.soundClip != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(card.ChoiceData.soundClip);
        }

        // Show Praise Mascot Subtitles
        SetDialogue($"Perfect! That letter makes the sound... {round.targetLetter}!");

        // Play Voice Praise Clip
        AudioClip praiseToPlay = round.roundPraiseClip != null ? round.roundPraiseClip : genericPraiseClip;
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

    private IEnumerator HandleWrongSelection(SoundAndLetterCard card)
    {
        isTransitioning = true;
        SetCardsInteractable(false);

        // Highlight only the clicked wrong card in RED (do not grayout others)
        if (card != null)
        {
            card.SetRedHighlight();
            card.PlayWiggleAnimation();
        }

        // Soft try again voice and dialogue
        SetDialogue("Oops, try again! Listen carefully.");

        if (tryAgainClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = tryAgainClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(tryAgainClip.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(1.2f);
        }

        SetDialogue("");

        // Reset visual state of the wrong card for retry
        if (card != null)
        {
            card.ResetVisualState();
        }

        // Replay target sound to help child
        ReplayTargetSound();

        isTransitioning = false;
        SetCardsInteractable(true);
    }

    private IEnumerator CompleteSequence()
    {
        isActivityCompleted = true;
        isTransitioning = true;
        SetCardsInteractable(false);

        // Mark topic complete immediately in TopicProgressUI
        TopicProgressUI.MarkTopicComplete(gameObject);

        // Save Unit 1 Completion
        PlayerPrefs.SetInt("Unit1_Completed", 1);
        PlayerPrefs.Save();

        // Confetti burst
        if (confettiParticles != null)
        {
            confettiParticles.SetActive(true);
        }

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

        // Show Badge / Trophy Popup
        if (starBadgePopup != null)
        {
            starBadgePopup.SetActive(true);
        }

        if (completionClip != null && voiceAudioSource != null)
        {
            SetDialogue("Fantastic! You know your sounds and letters!");
            voiceAudioSource.Stop();
            voiceAudioSource.clip = completionClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(completionClip.length + 0.5f);
        }

        // Map Unlock FX: Unit 2 Shines & Unlocks
        if (unit2ShineEffect != null)
        {
            unit2ShineEffect.SetActive(true);
        }

        if (unlockUnit2Clip != null && voiceAudioSource != null)
        {
            SetDialogue("You finished Unit 1! A new world is open. Let's keep going!");
            voiceAudioSource.Stop();
            voiceAudioSource.clip = unlockUnit2Clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(unlockUnit2Clip.length + 0.4f);
        }

        if (rewardPopup != null) rewardPopup.SetActive(true);
        if (continueButton != null) continueButton.SetActive(true);
    }

    private void UpdateStarMeterUI(bool triggerWiggle = false)
    {
        int totalRounds = roundsData != null ? roundsData.Length : 4;
        float fillAmount = Mathf.Clamp01((float)starsEarned / totalRounds);

        if (starMeterFillImage != null)
        {
            starMeterFillImage.fillAmount = fillAmount;
        }

        if (starCountText != null)
        {
            starCountText.text = $"{starsEarned} / {totalRounds} Stars";
        }
    }

    private void TriggerSpeakerPulseAnimation()
    {
        if (speakerAnimator != null)
        {
            speakerAnimator.SetTrigger("Pulse");
        }
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
