using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundWallController : MonoBehaviour
{
    [Header("Mascot & Subtitles")]
    [Tooltip("Animator of the mascot character.")]
    [SerializeField] private Animator mascotAnimator;

    [Tooltip("Text component to display mascot subtitles.")]
    [SerializeField] private TMP_Text dialogueText;

    [Tooltip("CanvasGroup of the dialogue box to fade it in/out.")]
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Audio Sources")]
    [Tooltip("Audio source for mascot narration voice-overs.")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Tooltip("Audio source for letter sounds and interface sound effects.")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Letters Config")]
    [Tooltip("The list of A-Z letter data ScriptableObjects.")]
    [SerializeField] private SoundWallLetterData[] lettersData;

    [Tooltip("The letter tile UI components (4 active tiles).")]
    [SerializeField] private SoundWallLetter[] letterTiles;

    [Header("Keyword Display Card")]
    [Tooltip("Card/Panel displaying the keyword image and text when a letter is tapped.")]
    [SerializeField] private GameObject keywordCard;
    [SerializeField] private Image keywordImage;
    [SerializeField] private TMP_Text keywordText;

    [Tooltip("SFX audio clip played when the keyword image pops up.")]
    [SerializeField] private AudioClip cardPopSfx;

    [Header("Progress Ring")]
    [Tooltip("UI Image set to Image.Type.Filled to act as a progress ring.")]
    [SerializeField] private Image progressRingImage;

    [Tooltip("Text to show numeric progress e.g. '4 / 26'.")]
    [SerializeField] private TMP_Text progressText;

    [Header("Voice Narrations")]
    [SerializeField] private AudioClip welcomeClip;          // "This is the Sound Wall..."
    [SerializeField] private AudioClip completeClip;         // "Fantastic! You've explored every letter!"

    [Header("Rewards & Progression")]
    [SerializeField] private GameObject confettiParticles;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject nextPanel;
    [SerializeField] private GameObject currentPanel;
    [SerializeField] private GameObject unitContentPanel;

    // Internal State
    private int currentLetterIndex = 0;
    private int exploredCount = 0;
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
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (voiceAudioSource != null) voiceAudioSource.Stop();
        if (sfxAudioSource != null) sfxAudioSource.Stop();
    }

    /// <summary>
    /// Resets the Sound Wall activity completely.
    /// </summary>
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
        SetDialogue("");

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
        SetTilesInteractable(false);

        SetDialogue("This is the Sound Wall. Every letter has its own sound. Tap a letter to hear it!");

        if (welcomeClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = welcomeClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(welcomeClip.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(2.5f);
        }

        SetDialogue("");

        SetTilesInteractable(true);
        isTransitioning = false;
    }

    private void LoadCurrentGroupLetters()
    {
        ClearKeywordDisplay();

        if (letterTiles == null || lettersData == null || lettersData.Length == 0)
            return;

        int groupSize = letterTiles.Length;

        for (int i = 0; i < groupSize; i++)
        {
            if (letterTiles[i] == null) continue;

            int index = currentLetterIndex + i;

            if (index < lettersData.Length && lettersData[index] != null)
            {
                letterTiles[i].gameObject.SetActive(true);
                letterTiles[i].Setup(lettersData[index], this);
            }
            else
            {
                letterTiles[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnLetterTapped(SoundWallLetter tile)
    {
        if (isTransitioning || isActivityCompleted || tile == null || tile.Data == null)
            return;

        // Display keyword image and text in the card
        ShowKeyword(tile.Data.keywordImage, tile.Data.keywordWord);

        // Update explored count
        if (!tile.IsTapped)
        {
            exploredCount++;
            UpdateProgressUI();
        }

        // Check if all 4 active tiles in the current group are tapped
        int total = lettersData != null ? lettersData.Length : 0;
        int groupSize = letterTiles != null ? letterTiles.Length : 4;
        int currentGroupTotal = Mathf.Min(groupSize, total - currentLetterIndex);

        int tappedInGroup = 0;
        for (int i = 0; i < currentGroupTotal; i++)
        {
            if (letterTiles[i] != null && letterTiles[i].gameObject.activeSelf && letterTiles[i].IsTapped)
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
        SetTilesInteractable(false);

        yield return new WaitForSeconds(0.5f);

        int groupSize = letterTiles != null ? letterTiles.Length : 4;
        int nextIndex = currentLetterIndex + groupSize;

        if (lettersData != null && nextIndex < lettersData.Length)
        {
            // Tile rotate animation transition
            if (letterTiles != null)
            {
                foreach (var tile in letterTiles)
                {
                    if (tile == null || !tile.gameObject.activeSelf) continue;
                    Animator anim = tile.GetComponent<Animator>();
                    if (anim != null) anim.SetTrigger("Rotate");
                }
            }
            yield return new WaitForSeconds(0.4f);

            currentLetterIndex = nextIndex;
            LoadCurrentGroupLetters();

            SetTilesInteractable(true);
            isTransitioning = false;
        }
        else
        {
            // All letters completed! Trigger completion sequence
            yield return StartCoroutine(CompleteSequence());
        }
    }

    // ── Completion Sequence ────────────────────────────────────────────────
    private IEnumerator CompleteSequence()
    {
        isActivityCompleted = true;
        isTransitioning = true;
        SetTilesInteractable(false);

        // Mark topic complete immediately in TopicProgressUI
        TopicProgressUI.MarkTopicComplete(gameObject);

        // Staggered confetti wave across active tiles
        if (letterTiles != null)
        {
            foreach (var tile in letterTiles)
            {
                if (tile != null && tile.gameObject.activeSelf)
                {
                    tile.TriggerConfetti();
                    yield return new WaitForSeconds(0.08f);
                }
            }
        }

        if (confettiParticles != null)
        {
            confettiParticles.SetActive(true);
        }

        if (dialogueCanvasGroup != null) StartCoroutine(FadeDialogueCoroutine(1f, 0.4f));

        if (completeClip != null && voiceAudioSource != null && voiceAudioSource.gameObject.activeInHierarchy)
        {
            if (dialogueText != null) dialogueText.text = "Fantastic! You've explored every letter on the Sound Wall!";
            voiceAudioSource.Stop();
            voiceAudioSource.clip = completeClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(completeClip.length + 0.5f);
        }
        else
        {
            if (dialogueText != null) dialogueText.text = "Fantastic! You've explored every letter on the Sound Wall!";
            yield return new WaitForSeconds(1.5f);
        }

        if (rewardPopup != null) rewardPopup.SetActive(true);
        if (continueButton != null) continueButton.SetActive(true);
    }

    public void ShowKeyword(Sprite sprite, string word)
    {
        bool shouldBeActive = sprite != null || !string.IsNullOrEmpty(word);

        if (keywordCard != null)
        {
            keywordCard.SetActive(shouldBeActive);
        }

        if (shouldBeActive)
        {
            if (cardPopSfx != null)
            {
                AudioSource popSource = (sfxAudioSource != null) ? sfxAudioSource : GetComponent<AudioSource>();
                if (popSource == null)
                {
                    popSource = gameObject.AddComponent<AudioSource>();
                }
                popSource.spatialBlend = 0f; // Force 2D sound
                popSource.volume = 1f;
                popSource.PlayOneShot(cardPopSfx);
            }
            else
            {
                Debug.LogWarning("[SoundWallController] Keyword image displayed, but 'Card Pop Sfx' is not assigned in the Inspector! Please drag your pop audio clip into the 'Card Pop Sfx' slot on SoundWallController.");
            }
        }

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

    public void ClearKeywordDisplay()
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
        int total = lettersData != null ? lettersData.Length : 1;
        float progress = Mathf.Clamp01((float)exploredCount / total);

        if (progressRingImage != null)
        {
            progressRingImage.fillAmount = progress;
        }

        if (progressText != null)
        {
            progressText.text = $"{exploredCount} / {total}";
        }
    }

    private void SetTilesInteractable(bool state)
    {
        if (letterTiles == null) return;
        foreach (var tile in letterTiles)
        {
            if (tile != null) tile.SetInteractable(state);
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

    private void SetDialogue(string text)
    {
        EngSnap.Common.DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
    }
}
