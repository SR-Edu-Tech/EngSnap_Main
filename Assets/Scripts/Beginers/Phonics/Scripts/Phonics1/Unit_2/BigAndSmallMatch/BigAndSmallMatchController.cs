using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BigAndSmallMatchController : MonoBehaviour
{
    [Header("Mascot & Subtitles")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Match Data Set")]
    [SerializeField] private BigAndSmallMatchData[] pairsData;

    [Header("UI Letter Cards")]
    [Tooltip("Capital letter card slots (left column).")]
    [SerializeField] private BigAndSmallMatchCard[] capitalCards;

    [Tooltip("Small letter card slots (right column).")]
    [SerializeField] private BigAndSmallMatchCard[] smallCards;

    [Header("Voice Clips")]
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip genericPraiseClip;
    [SerializeField] private AudioClip tryAgainClip;
    [SerializeField] private AudioClip completionClip;
    [SerializeField] private AudioClip matchSfx;

    [Header("Rewards & Progression")]
    [SerializeField] private GameObject confettiParticles;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject nextPanel;
    [SerializeField] private GameObject currentPanel;
    [SerializeField]private GameObject unitContentPanel;

    private int currentSetIndex = 0;
    private int matchedPairsInSet = 0;
    private bool isTransitioning = false;
    private bool isActivityCompleted = false;

    private BigAndSmallMatchCard selectedCapitalCard;
    private BigAndSmallMatchCard selectedSmallCard;

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

        currentSetIndex = 0;
        matchedPairsInSet = 0;
        isTransitioning = false;
        isActivityCompleted = false;
        selectedCapitalCard = null;
        selectedSmallCard = null;

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);
        if (confettiParticles != null) confettiParticles.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        if (dialogueCanvasGroup != null) dialogueCanvasGroup.alpha = 0f;

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(IntroSequence());
        }
    }

    private IEnumerator IntroSequence()
    {
        isTransitioning = true;
        SetCardsInteractable(false);

        if (dialogueText != null) dialogueText.text = "Every letter has a big shape and a small shape. Match the big letter to its small letter!";
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.gameObject.SetActive(true);
            StartCoroutine(FadeDialogueCoroutine(1f, 0.4f));
        }

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

        if (dialogueCanvasGroup != null) yield return StartCoroutine(FadeDialogueCoroutine(0f, 0.4f));

        LoadCurrentSet();
    }

    private void LoadCurrentSet()
    {
        selectedCapitalCard = null;
        selectedSmallCard = null;
        matchedPairsInSet = 0;

        if (pairsData == null || pairsData.Length == 0 || capitalCards == null || smallCards == null) return;

        int setSize = Mathf.Min(capitalCards.Length, smallCards.Length);
        int startIndex = currentSetIndex * setSize;

        if (startIndex >= pairsData.Length)
        {
            StartCoroutine(CompleteSequence());
            return;
        }

        List<BigAndSmallMatchData> currentDataList = new List<BigAndSmallMatchData>();
        for (int i = 0; i < setSize; i++)
        {
            int idx = startIndex + i;
            if (idx < pairsData.Length && pairsData[idx] != null)
            {
                currentDataList.Add(pairsData[idx]);
            }
        }

        if (currentDataList.Count == 0)
        {
            StartCoroutine(CompleteSequence());
            return;
        }

        // Setup Capital Cards (Left Column)
        for (int i = 0; i < setSize; i++)
        {
            if (i < currentDataList.Count)
            {
                capitalCards[i].gameObject.SetActive(true);
                capitalCards[i].Setup(currentDataList[i].capitalLetter, BigAndSmallMatchCard.LetterType.Capital, this);
            }
            else
            {
                capitalCards[i].gameObject.SetActive(false);
            }
        }

        // Setup Small Cards (Right Column - Shuffled)
        List<BigAndSmallMatchData> shuffledList = new List<BigAndSmallMatchData>(currentDataList);
        ShuffleList(shuffledList);

        for (int i = 0; i < setSize; i++)
        {
            if (i < shuffledList.Count)
            {
                smallCards[i].gameObject.SetActive(true);
                smallCards[i].Setup(shuffledList[i].smallLetter, BigAndSmallMatchCard.LetterType.Small, this);
            }
            else
            {
                smallCards[i].gameObject.SetActive(false);
            }
        }

        SetCardsInteractable(true);
        isTransitioning = false;
    }

    public void OnCardSelected(BigAndSmallMatchCard card)
    {
        if (isTransitioning || isActivityCompleted || card == null) return;

        if (card.Type == BigAndSmallMatchCard.LetterType.Capital)
        {
            if (selectedCapitalCard != null && selectedCapitalCard != card)
            {
                selectedCapitalCard.SetGlow(false);
            }
            selectedCapitalCard = card;
            selectedCapitalCard.SetGlow(true);
        }
        else if (card.Type == BigAndSmallMatchCard.LetterType.Small)
        {
            if (selectedSmallCard != null && selectedSmallCard != card)
            {
                selectedSmallCard.SetGlow(false);
            }
            selectedSmallCard = card;
            selectedSmallCard.SetGlow(true);
        }

        if (selectedCapitalCard != null && selectedSmallCard != null)
        {
            selectedCapitalCard.SetGlow(true);
            selectedSmallCard.SetGlow(true);
            CheckMatch();
        }
    }

    private void CheckMatch()
    {
        if (selectedCapitalCard == null || selectedSmallCard == null) return;

        bool isCorrect = string.Equals(selectedCapitalCard.LetterValue, selectedSmallCard.LetterValue, System.StringComparison.OrdinalIgnoreCase);

        if (isCorrect)
        {
            StartCoroutine(HandleMatchSuccess(selectedCapitalCard, selectedSmallCard));
        }
        else
        {
            StartCoroutine(HandleMatchFailed(selectedCapitalCard, selectedSmallCard));
        }
    }

    private IEnumerator HandleMatchSuccess(BigAndSmallMatchCard capCard, BigAndSmallMatchCard smCard)
    {
        isTransitioning = true;

        capCard.PlayMatchAnimation();
        smCard.PlayMatchAnimation();

        if (matchSfx != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(matchSfx);
        }

        if (dialogueCanvasGroup != null) StartCoroutine(FadeDialogueCoroutine(1f, 0.3f));
        if (dialogueText != null) dialogueText.text = $"Yes! Big {capCard.LetterValue} and small {smCard.LetterValue} are partners!";

        if (genericPraiseClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = genericPraiseClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(genericPraiseClip.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        if (dialogueCanvasGroup != null) yield return StartCoroutine(FadeDialogueCoroutine(0f, 0.3f));

        capCard.gameObject.SetActive(false);
        smCard.gameObject.SetActive(false);

        selectedCapitalCard = null;
        selectedSmallCard = null;
        matchedPairsInSet++;

        int activeSetSize = 0;
        foreach (var c in capitalCards)
        {
            if (c != null && c.gameObject.activeSelf) activeSetSize++;
        }

        if (activeSetSize == 0)
        {
            currentSetIndex++;
            int setSize = Mathf.Min(capitalCards.Length, smallCards.Length);
            if (currentSetIndex * setSize < pairsData.Length)
            {
                LoadCurrentSet();
            }
            else
            {
                yield return StartCoroutine(CompleteSequence());
            }
        }
        else
        {
            isTransitioning = false;
        }
    }

    private IEnumerator HandleMatchFailed(BigAndSmallMatchCard capCard, BigAndSmallMatchCard smCard)
    {
        isTransitioning = true;

        capCard.PlayMismatchAnimation();
        smCard.PlayMismatchAnimation();

        if (dialogueCanvasGroup != null) StartCoroutine(FadeDialogueCoroutine(1f, 0.3f));
        if (dialogueText != null) dialogueText.text = "Not quite — try again!";

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

        if (dialogueCanvasGroup != null) yield return StartCoroutine(FadeDialogueCoroutine(0f, 0.3f));

        capCard.SetGlow(false);
        smCard.SetGlow(false);

        selectedCapitalCard = null;
        selectedSmallCard = null;

        isTransitioning = false;
    }

    private IEnumerator CompleteSequence()
    {
        isActivityCompleted = true;
        isTransitioning = true;

        // Mark topic complete immediately in TopicProgressUI
        TopicProgressUI.MarkTopicComplete(gameObject);

        if (confettiParticles != null) confettiParticles.SetActive(true);

        if (dialogueCanvasGroup != null) StartCoroutine(FadeDialogueCoroutine(1f, 0.4f));

        if (completionClip != null && voiceAudioSource != null)
        {
            if (dialogueText != null) dialogueText.text = "Fantastic matching! You know your big and small letters!";
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

    private void SetCardsInteractable(bool state)
    {
        if (capitalCards != null)
        {
            foreach (var c in capitalCards) if (c != null) c.SetInteractable(state);
        }
        if (smallCards != null)
        {
            foreach (var c in smallCards) if (c != null) c.SetInteractable(state);
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
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
