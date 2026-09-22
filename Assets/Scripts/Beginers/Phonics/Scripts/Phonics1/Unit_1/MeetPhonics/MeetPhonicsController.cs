using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MeetPhonicsController : MonoBehaviour
{
    [Header("Unit Progress Settings")]
    [SerializeField] private string unitID = "Unit1";
    [SerializeField] private string topicName = "MeetPhonics";

    [Header("Mascot & Subtitles")]
    [Tooltip("Animator of the mascot character.")]
    [SerializeField] private Animator mascotAnimator;

    [Tooltip("Text component to display mascot subtitles.")]
    [SerializeField] private TMP_Text dialogueText;

    [Tooltip("CanvasGroup of the dialogue box to fade it in/out. (Optional)")]
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Mascot Entrance Animation")]
    [Tooltip("If true, the mascot slides in when the panel opens.")]
    [SerializeField] private bool animateMascotEntrance = false; // mascot animations disabled per user request

    [Tooltip("Starting position offset relative to its target position (e.g., (-1200, 0, 0) to slide in from left).")]
    [SerializeField] private Vector3 mascotStartOffset = new Vector3(-1200f, 0f, 0f);

    [Tooltip("Duration of the mascot slide-in animation.")]
    [SerializeField] private float mascotEntranceDuration = 0.8f;

    [Header("Audio Sources")]
    [Tooltip("Audio source for mascot narration voice-overs.")]
    [SerializeField] private AudioSource voiceAudioSource;

    [Tooltip("Audio source for letter sounds and interface sound effects.")]
    [SerializeField] private AudioSource sfxAudioSource;

    [Tooltip("Optional button to tap and listen to instructions again.")]
    [SerializeField] private Button tapToListenButton;

    [Header("Activity Letters")]
    [Tooltip("The 7 letters forming PHONICS in correct order.")]
    [SerializeField] private MeetPhonicsLetter[] letters;

    [Header("Voice Narrations")]
    [SerializeField] private AudioClip welcomeClip;       // "Hi there! Welcome..."
    [SerializeField] private AudioClip explanationClip;   // "Phonics is how..."
    [SerializeField] private AudioClip instructionClip;   // "Tap the letters..."
    [SerializeField] private AudioClip completeClip;      // "Yay! You did it!..."

    [Header("Rewards & Progression")]
    [Tooltip("Confetti particle system to play on completion.")]
    [SerializeField] private GameObject confettiParticles;

    [Tooltip("The sticker reward popup screen.")]
    [SerializeField] private GameObject rewardPopup;

    [Tooltip("The button to continue to the next activity.")]
    [SerializeField] private GameObject continueButton;

    [Tooltip("The next panel or activity to show when Continue is clicked.")]
    [SerializeField] private GameObject nextPanel;

    [Tooltip("The current panel to hide when Continue is clicked. (Assign this GameObject or its parent panel)")]
    [SerializeField] private GameObject currentPanel;
    [SerializeField] private GameObject unitContentPanel;
    

    private int lettersTappedCount = 0;
    private bool isActivityCompleted = false;

    // Mascot cache positions
    private RectTransform mascotRect;
    private Vector2 mascotTargetAnchoredPos;
    private Vector3 mascotTargetLocalPos;

    /// <summary>Exposes the SFX audio source so letters can call PlayOneShot directly.</summary>
    public AudioSource SfxAudioSource => sfxAudioSource;

    private bool isStarted = false;

    private void Awake()
    {
        EnsureAudioSources();
        if (mascotAnimator != null) mascotAnimator.gameObject.SetActive(true);
    }

    private void EnsureAudioSources()
    {
        if (sfxAudioSource == null)
        {
            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
        }
        if (sfxAudioSource != null)
        {
            sfxAudioSource.spatialBlend = 0f;
            sfxAudioSource.volume = 1f;
            sfxAudioSource.loop = false;
        }

        if (voiceAudioSource == null) voiceAudioSource = sfxAudioSource;
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

        // Safety checks
        if (letters == null || letters.Length == 0)
        {
            Debug.LogError("MeetPhonicsController: Letters list is empty. Please assign letters in the inspector.", this);
            return;
        }

        // Cache mascot initial position
        if (mascotAnimator != null)
        {
            mascotRect = mascotAnimator.GetComponent<RectTransform>();
            if (mascotRect != null)
            {
                mascotTargetAnchoredPos = mascotRect.anchoredPosition;
            }
            else
            {
                mascotTargetLocalPos = mascotAnimator.transform.localPosition;
            }
        }

        if (continueButton != null)
        {
            // Wire the Continue button to GoToNextPanel at runtime
            Button btn = continueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(GoToNextPanel);
            }
        }

        if (tapToListenButton != null)
        {
            tapToListenButton.onClick.RemoveAllListeners();
            tapToListenButton.onClick.AddListener(OnTapToListenClicked);
        }

        ResetLevel();
    }

    public void OnTapToListenClicked()
    {
        if (isActivityCompleted) return;

        if (instructionClip != null && voiceAudioSource != null)
        {
            SetDialogue("Tap the letters below to hear their sounds and build the word PHONICS!");
            voiceAudioSource.Stop();
            voiceAudioSource.clip = instructionClip;
            voiceAudioSource.Play();
        }
    }

    private void OnEnable()
    {
        if (mascotAnimator != null) mascotAnimator.gameObject.SetActive(true);
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
    /// Resets the Meet Phonics level completely.
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

        lettersTappedCount = 0;
        isActivityCompleted = false;

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);
        if (confettiParticles != null) confettiParticles.SetActive(false);
        SetDialogue("");

        if (letters != null)
        {
            for (int i = 0; i < letters.Length; i++)
            {
                if (letters[i] != null)
                {
                    letters[i].Initialize(this);
                    letters[i].gameObject.SetActive(true);
                }
            }
        }

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(IntroSequence());
        }
    }

    /// <summary>
    /// Coroutine managing the initial narration and build-up phase.
    /// </summary>
    private IEnumerator IntroSequence()
    {
        // 1. Play Welcome Clip
        if (welcomeClip != null && voiceAudioSource != null)
        {
            SetDialogue("Hi there! Welcome to Phonics Quest! I am so happy you are here.");
            if (!voiceAudioSource.isPlaying || voiceAudioSource.clip != welcomeClip)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = welcomeClip;
                voiceAudioSource.Play();
            }
            yield return new WaitForSeconds(welcomeClip.length + 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        // 2. Explanation
        if (explanationClip != null && voiceAudioSource != null)
        {
            SetDialogue("Phonics is how letters and sounds work together to make words.");
            voiceAudioSource.Stop();
            voiceAudioSource.clip = explanationClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(explanationClip.length + 0.3f);
        }

        // 3. Play Instruction Clip
        if (instructionClip != null && voiceAudioSource != null)
        {
            SetDialogue("Tap the letters below to hear their sounds and build the word PHONICS!");
            voiceAudioSource.Stop();
            voiceAudioSource.clip = instructionClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(instructionClip.length + 0.2f);
        }

        // 6. Enable Interaction on letters
        for (int i = 0; i < letters.Length; i++)
        {
            letters[i].SetInteractable(true);
        }

        // Fade out or clear dialogue to focus on letter interaction
        if (dialogueCanvasGroup != null)
        {
            yield return StartCoroutine(FadeDialogueCoroutine(0f, 0.4f));
        }
        SetDialogue("");
        
// Mascot idle animation disabled (no mascot triggers required)
    }

    /// <summary>
    /// Coroutine sliding in the Mascot character.
    /// </summary>
    private IEnumerator MascotEntranceCoroutine()
    {
        float elapsed = 0f;

        if (mascotRect != null)
        {
            Vector2 startPos = mascotTargetAnchoredPos + new Vector2(mascotStartOffset.x, mascotStartOffset.y);
            mascotRect.anchoredPosition = startPos;

            while (elapsed < mascotEntranceDuration)
            {
                elapsed += Time.deltaTime;
                float percent = Mathf.Clamp01(elapsed / mascotEntranceDuration);
                
                float t = EaseOutBack(0f, 1f, percent);
                mascotRect.anchoredPosition = Vector2.LerpUnclamped(startPos, mascotTargetAnchoredPos, t);
                
                yield return null;
            }
            mascotRect.anchoredPosition = mascotTargetAnchoredPos;
        }
        else if (mascotAnimator != null)
        {
            Vector3 startPos = mascotAnimator.transform.localPosition + mascotStartOffset;
            mascotAnimator.transform.localPosition = startPos;

            while (elapsed < mascotEntranceDuration)
            {
                elapsed += Time.deltaTime;
                float percent = Mathf.Clamp01(elapsed / mascotEntranceDuration);
                
                float t = EaseOutBack(0f, 1f, percent);
                mascotAnimator.transform.localPosition = Vector3.LerpUnclamped(startPos, mascotTargetLocalPos, t);
                
                yield return null;
            }
            mascotAnimator.transform.localPosition = mascotTargetLocalPos;
        }
    }

    /// <summary>
    /// Coroutine fading in/out the canvas group of the dialogue subtitles bubble.
    /// </summary>
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

    /// <summary>
    /// Called when any of the letters are tapped.
    /// </summary>
    public void OnLetterTapped(MeetPhonicsLetter letter)
    {
        if (isActivityCompleted || (voiceAudioSource != null && voiceAudioSource.isPlaying)) return;

        // Debug info for taps
        Debug.Log($"MeetPhonicsController: Letter tapped: {letter.LetterChar}");
        // Ensure the SFX audio source exists
        if (sfxAudioSource == null)
        {
            Debug.LogError("MeetPhonicsController: sfxAudioSource is not assigned in the inspector.");
        }
        // Ensure the letter's sound clip exists
        if (letter.SoundClip == null)
        {
            Debug.LogError($"MeetPhonicsController: SoundClip missing for letter '{letter.LetterChar}'.");
        }

        // Check if all letters have been tapped at least once
        int tappedCount = 0;
        foreach (var l in letters)
        {
            if (l.IsTapped)
            {
                tappedCount++;
            }
        }

        if (tappedCount == letters.Length && !isActivityCompleted)
        {
            StartCoroutine(CompleteSequence());
        }
    }

    /// <summary>
    /// Coroutine triggered when all letters have been tapped.
    /// </summary>
    private IEnumerator CompleteSequence()
    {
        isActivityCompleted = true;

        // Disable interaction on all letters
        for (int i = 0; i < letters.Length; i++)
        {
            letters[i].SetInteractable(false);
        }

        yield return new WaitForSeconds(1.0f); // Brief delay after final tap sound finishes

// Mascot cheer animation disabled (no mascot triggers).

        // Fire confetti on every letter with a small stagger (wave effect)
        for (int i = 0; i < letters.Length; i++)
        {
            letters[i].TriggerConfetti();
            yield return new WaitForSeconds(0.08f); // 80 ms stagger between each letter
        }

        // Play scene-level confetti particle system (if assigned)
        if (confettiParticles != null)
        {
            confettiParticles.SetActive(true);
            
            
        }

        // Fade in dialogue bubble
        if (dialogueCanvasGroup != null)
        {
            StartCoroutine(FadeDialogueCoroutine(1f, 0.4f));
        }

        // Play Complete Clip
        if (completeClip != null && voiceAudioSource != null)
        {
            SetDialogue("Yay! You did it! Let's go!");
            voiceAudioSource.clip = completeClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(completeClip.length + 0.5f);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        // Mark topic complete in TopicProgressUI
        if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
        else TopicProgressUI.MarkTopicComplete(gameObject);

        TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
        TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

        // Show Sticker Reward
        if (rewardPopup != null)
        {
            rewardPopup.SetActive(true);
        }

        // Show Continue Button
        if (continueButton != null)
        {
            continueButton.SetActive(true);
        }
    }

    /// <summary>
    /// Called when the Continue button is pressed after activity completion.
    /// Hides the current panel and activates the next panel.
    /// </summary>
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

    /// <summary>
    /// Spring overshoot easing formula (EaseOutBack).
    /// </summary>
    private float EaseOutBack(float start, float end, float value)
    {
        float s = 1.70158f;
        value = value - 1f;
        float calculated = value * value * ((s + 1f) * value + s) + 1f;
        return (end - start) * calculated + start;
    }
}
