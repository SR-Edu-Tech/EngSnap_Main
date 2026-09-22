using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MeetLettersController : MonoBehaviour
{
    [Header("Mascot & Subtitles")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Letters Config")]
    [Tooltip("List of letter data objects for this stop (A-M or N-Z).")]
    [SerializeField] private MeetLettersData[] lettersData;

    [Tooltip("Active UI letter cards on screen.")]
    [SerializeField] private MeetLettersCard[] letterCards;

    [Header("Keyword Card Display")]
    [SerializeField] private GameObject keywordCard;
    [SerializeField] private Image keywordImage;
    [SerializeField] private TMP_Text keywordText;

    [Header("Progress Ring")]
    [SerializeField] private Image progressRingImage;
    [SerializeField] private TMP_Text progressText;

    [Header("Voice Narrations")]
    [SerializeField] private AudioClip welcomeClip;
    [SerializeField] private AudioClip completionClip;

    [Header("Rewards & Navigation")]
    [SerializeField] private GameObject confettiParticles;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject nextPanel;
    [SerializeField] private GameObject currentPanel;
    [SerializeField]private GameObject unitContentPanel;

    private int currentLetterIndex = 0;
    private int exploredCount = 0;
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

        exploredCount = 0;
        currentLetterIndex = 0;
        isTransitioning = false;
        isActivityCompleted = false;

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (keywordCard != null) keywordCard.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);
        if (confettiParticles != null) confettiParticles.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0f;

        ClearKeywordDisplay();
        UpdateProgressUI();
        LoadCurrentGroupLetters();

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(IntroSequence());
        }
    }

    private IEnumerator IntroSequence()
    {
        isTransitioning = true;
        SetCardsInteractable(false);

        if (dialogueText != null) dialogueText.text = "Let's meet the letters! Tap a letter to hear its name.";
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.gameObject.SetActive(true);
            StartCoroutine(FadeDialogueCoroutine(1f, 0.4f));
        }

        if (welcomeClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = welcomeClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(welcomeClip.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(2.5f);
        }

        if (dialogueCanvasGroup != null) yield return StartCoroutine(FadeDialogueCoroutine(0f, 0.4f));

        SetCardsInteractable(true);
        isTransitioning = false;
    }

    private void LoadCurrentGroupLetters()
    {
        ClearKeywordDisplay();

        if (letterCards == null || lettersData == null || lettersData.Length == 0) return;

        int groupSize = letterCards.Length;

        for (int i = 0; i < groupSize; i++)
        {
            if (letterCards[i] == null) continue;

            int index = currentLetterIndex + i;

            if (index < lettersData.Length && lettersData[index] != null)
            {
                letterCards[i].gameObject.SetActive(true);
                letterCards[i].Setup(lettersData[index], this);
            }
            else
            {
                letterCards[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnCardTapped(MeetLettersCard card)
    {
        if (isTransitioning || isActivityCompleted || card == null || card.Data == null) return;

        ShowKeyword(card.Data.keywordSprite, card.Data.keywordWord);

        // Play letter name audio
        AudioClip clipToPlay = card.Data.letterNameAndWordAudio != null ? card.Data.letterNameAndWordAudio : card.Data.letterNameAudio;
        if (clipToPlay != null && sfxAudioSource != null)
        {
            sfxAudioSource.Stop();
            sfxAudioSource.PlayOneShot(clipToPlay);
        }

        if (!card.IsTapped)
        {
            exploredCount++;
            UpdateProgressUI();
        }

        int total = lettersData != null ? lettersData.Length : 0;
        int groupSize = letterCards != null ? letterCards.Length : 4;
        int currentGroupTotal = Mathf.Min(groupSize, total - currentLetterIndex);

        int tappedInGroup = 0;
        for (int i = 0; i < currentGroupTotal; i++)
        {
            if (letterCards[i] != null && letterCards[i].gameObject.activeSelf && letterCards[i].IsTapped)
            {
                tappedInGroup++;
            }
        }

        if (tappedInGroup >= currentGroupTotal)
        {
            StartCoroutine(NextGroupOrCompleteRoutine());
        }
    }

    private IEnumerator NextGroupOrCompleteRoutine()
    {
        isTransitioning = true;
        SetCardsInteractable(false);

        yield return new WaitForSeconds(0.6f);

        int groupSize = letterCards != null ? letterCards.Length : 4;
        int nextIndex = currentLetterIndex + groupSize;

        if (lettersData != null && nextIndex < lettersData.Length)
        {
            currentLetterIndex = nextIndex;
            LoadCurrentGroupLetters();
            SetCardsInteractable(true);
            isTransitioning = false;
        }
        else
        {
            yield return StartCoroutine(CompleteSequence());
        }
    }

    private IEnumerator CompleteSequence()
    {
        isActivityCompleted = true;
        isTransitioning = true;
        SetCardsInteractable(false);

        // Mark topic complete immediately in TopicProgressUI
        TopicProgressUI.MarkTopicComplete(gameObject);

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

        if (confettiParticles != null) confettiParticles.SetActive(true);

        if (dialogueCanvasGroup != null) StartCoroutine(FadeDialogueCoroutine(1f, 0.4f));

        if (completionClip != null && voiceAudioSource != null)
        {
            if (dialogueText != null) dialogueText.text = "You met all the letters! Great job!";
            voiceAudioSource.Stop();
            voiceAudioSource.clip = completionClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(completionClip.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        if (rewardPopup != null) rewardPopup.SetActive(true);
        if (continueButton != null) continueButton.SetActive(true);
    }

    private void ShowKeyword(Sprite sprite, string word)
    {
        if (keywordCard != null) keywordCard.SetActive(true);

        if (keywordImage != null)
        {
            keywordImage.sprite = sprite;
            keywordImage.enabled = sprite != null;
        }

        if (keywordText != null)
        {
            keywordText.text = word ?? "";
        }
    }

    private void ClearKeywordDisplay()
    {
        if (keywordCard != null) keywordCard.SetActive(false);
        if (keywordImage != null)
        {
            keywordImage.sprite = null;
            keywordImage.enabled = false;
        }
        if (keywordText != null) keywordText.text = "";
    }

    private void UpdateProgressUI()
    {
        int total = lettersData != null ? lettersData.Length : 0;
        float fillAmount = total > 0 ? Mathf.Clamp01((float)exploredCount / total) : 0f;

        if (progressRingImage != null) progressRingImage.fillAmount = fillAmount;
        if (progressText != null) progressText.text = $"{exploredCount} / {total}";
    }

    private void SetCardsInteractable(bool state)
    {
        if (letterCards == null) return;
        foreach (var card in letterCards)
        {
            if (card != null) card.SetInteractable(state);
        }
    }

    private IEnumerator FadeDialogueCoroutine(float targetAlpha, float duration)
    {
        if (dialogueCanvasGroup == null) yield break;
        float elapsed = 0f;
        float startAlpha = dialogueCanvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            dialogueCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        dialogueCanvasGroup.alpha = targetAlpha;
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
}
