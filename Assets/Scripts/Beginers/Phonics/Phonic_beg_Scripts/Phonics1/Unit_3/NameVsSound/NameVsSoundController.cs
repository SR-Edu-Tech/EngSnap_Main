using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameVsSoundController : MonoBehaviour
{
    [Header("Mascot & Subtitles")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("UI Letter Cards")]
    [SerializeField] private NameVsSoundCard mainLetterCard;

    [Header("Data Sets")]
    [SerializeField] private NameVsSoundData[] letterPairsData;

    [Header("Voice Script Clips")]
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip pairCompletionSfx;
    [SerializeField] private AudioClip soundTwinCheckIntroClip;
    [SerializeField] private AudioClip genericPraiseClip;
    [SerializeField] private AudioClip tryAgainClip;
    [SerializeField] private AudioClip stopCompletionClip;

    [Header("Rewards & Navigation")]
    [SerializeField] private GameObject confettiParticles;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject nextPanel;
    [SerializeField] private GameObject currentPanel;
    [SerializeField] private GameObject unitContentPanel;

    private int currentIndex = 0;
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
        currentIndex = 0;
        isTransitioning = false;
        isActivityCompleted = false;

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (confettiParticles != null) confettiParticles.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);

        if (letterPairsData != null && letterPairsData.Length > 0 && mainLetterCard != null)
        {
            mainLetterCard.Setup(letterPairsData[0], OnCardButtonTapped);
        }
    }

    private IEnumerator IntroSequence()
    {
        isTransitioning = true;
        SetSubtitle("Every letter has a name and a sound. Listen to both!");

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

        LoadCurrentPair();
        isTransitioning = false;
    }

    private void LoadCurrentPair()
    {
        if (letterPairsData == null || letterPairsData.Length == 0) return;
        if (currentIndex >= letterPairsData.Length)
        {
            StartCoroutine(CompleteStopSequence());
            return;
        }

        NameVsSoundData data = letterPairsData[currentIndex];
        if (mainLetterCard != null)
        {
            mainLetterCard.Setup(data, OnCardButtonTapped);
        }
        SetSubtitle($"Tap the 'Name' or 'Sound' button for letter {data.letter}!");
    }

    private void OnCardButtonTapped(NameVsSoundCard card, bool isName)
    {
        if (isTransitioning) return;
        StartCoroutine(HandleTapSequence(card, isName));
    }

    private IEnumerator HandleTapSequence(NameVsSoundCard card, bool isName)
    {
        isTransitioning = true;
        NameVsSoundData data = card.CurrentData;

        AudioClip clipToPlay = isName ? data.letterNameClip : data.letterSoundClip;
        string clipText = isName ? data.letterNameText : data.letterSoundText;
        SetSubtitle($"Letter {data.letter} {(isName ? "Name" : "Sound")}: '{clipText}'");

        if (clipToPlay != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clipToPlay;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clipToPlay.length + 0.2f);
        }

        // Require child to listen to BOTH Name and Sound before mascot reinforcement
        if (card.HasTappedName && card.HasTappedSound)
        {
            // Response sound SFX
            if (pairCompletionSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(pairCompletionSfx);
            }
            else if (genericPraiseClip != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(genericPraiseClip);
            }

            yield return new WaitForSeconds(0.2f);

            // Mascot reinforcement commentary: "The name is 'bee', but the sound is 'buh'!"
            if (data.mascotReinforcementClip != null)
            {
                SetSubtitle(data.reinforcementSubtitles);
                voiceAudioSource.Stop();
                voiceAudioSource.clip = data.mascotReinforcementClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(data.mascotReinforcementClip.length + 0.4f);
            }
            else
            {
                SetSubtitle(data.reinforcementSubtitles);
                yield return new WaitForSeconds(2.5f);
            }

            // Move to next pair
            currentIndex++;
            LoadCurrentPair();
        }
        else
        {
            // Prompt child to tap the other button
            if (isName)
            {
                SetSubtitle($"Great! Now tap the 'Sound' button to hear the sound of letter {data.letter}!");
            }
            else
            {
                SetSubtitle($"Great! Now tap the 'Name' button to hear the name of letter {data.letter}!");
            }
        }

        isTransitioning = false;
    }

    private IEnumerator CompleteStopSequence()
    {
        isActivityCompleted = true;
        SetSubtitle("Great listening! You learned names and sounds!");

        // Mark topic complete immediately in TopicProgressUI
        TopicProgressUI.MarkTopicComplete(gameObject);

        if (confettiParticles != null) confettiParticles.SetActive(true);
        if (rewardPopup != null) rewardPopup.SetActive(true);
        if (continueButton != null) continueButton.SetActive(true);

        if (stopCompletionClip != null && voiceAudioSource != null && voiceAudioSource.gameObject.activeInHierarchy)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = stopCompletionClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(stopCompletionClip.length + 0.2f);
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
