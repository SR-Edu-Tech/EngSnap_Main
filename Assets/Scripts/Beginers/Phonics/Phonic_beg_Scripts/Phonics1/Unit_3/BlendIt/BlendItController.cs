using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlendItController : MonoBehaviour
{
    [Header("Mascot & Subtitles")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CanvasGroup dialogueCanvasGroup;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource voiceAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;

    [Header("Sound Boxes (3 Slots for CVC)")]
    [SerializeField] private SoundBox[] soundBoxes;
    [SerializeField] private RectTransform[] joinedTargetPositions; // Target positions when sliding together

    [Header("Blend Controls & Display")]
    [SerializeField] private Button blendButton;
    [SerializeField] private Image blendButtonGlow;
    [SerializeField] private Image wordPictureDisplayImage;
    [SerializeField] private TMP_Text blendedWordText;

    [Header("Word Data Sets (cat, pin, dog, sun)")]
    [SerializeField] private BlendItData[] wordDataSets;

    [Header("Voice Script Clips")]
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip tapBlendPromptClip;
    [SerializeField] private AudioClip blendSfx;
    [SerializeField] private AudioClip stopCompletionClip;

    [Header("Rewards & Navigation")]
    [SerializeField] private GameObject confettiParticles;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject nextPanel;
    [SerializeField] private GameObject currentPanel;
    [SerializeField] private GameObject unitContentPanel;


    private int currentWordIndex = 0;
    private bool isTransitioning = false;
    private bool isActivityCompleted = false;

    private Vector3 blendButtonInitialScale = Vector3.one;
    private Coroutine blendButtonPopCoroutine;
    private Coroutine tapBlendPromptCoroutine;

    public AudioSource SfxAudioSource => sfxAudioSource;
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        EnsureAudioSources();
        CacheBlendButtonScale();
    }

    private void CacheBlendButtonScale()
    {
        if (blendButton != null)
        {
            blendButtonInitialScale = blendButton.transform.localScale;
            if (blendButtonInitialScale == Vector3.zero) blendButtonInitialScale = Vector3.one;
        }
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
        CacheBlendButtonScale();

        if (blendButton != null)
        {
            blendButton.onClick.RemoveAllListeners();
            blendButton.onClick.AddListener(OnBlendButtonClicked);
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
        blendButtonPopCoroutine = null;
        tapBlendPromptCoroutine = null;
        if (blendButton != null) blendButton.transform.localScale = blendButtonInitialScale;
    }

    public void ResetLevel()
    {
        StopAllCoroutines();
        blendButtonPopCoroutine = null;
        StopBlendButtonPop();
        currentWordIndex = 0;
        isTransitioning = false;
        isActivityCompleted = false;

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (confettiParticles != null) confettiParticles.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);

        LoadWord(0);
    }

    private void LoadWord(int index)
    {
        if (wordDataSets == null || index < 0 || index >= wordDataSets.Length) return;

        StopBlendButtonPop();

        BlendItData data = wordDataSets[index];

        if (wordPictureDisplayImage != null)
        {
            if (data.wordSprite != null) wordPictureDisplayImage.sprite = data.wordSprite;
            wordPictureDisplayImage.enabled = false;
        }
        if (blendedWordText != null) blendedWordText.text = "";
        if (blendButtonGlow != null) blendButtonGlow.enabled = false;

        // Reset sound boxes positions and setup
        for (int i = 0; i < soundBoxes.Length; i++)
        {
            if (soundBoxes[i] != null)
            {
                soundBoxes[i].ResetPosition();
                if (data.phonemeLetters != null && i < data.phonemeLetters.Length && data.phonemeClips != null && i < data.phonemeClips.Length)
                {
                    soundBoxes[i].Setup(data.phonemeLetters[i], data.phonemeClips[i], OnSoundBoxTapped);
                    soundBoxes[i].gameObject.SetActive(true);
                }
                else
                {
                    soundBoxes[i].gameObject.SetActive(false);
                }
            }
        }

        SetSubtitle($"Tap each sound, then tap BLEND!");
    }

    private IEnumerator IntroSequence()
    {
        isTransitioning = true;
        SetSubtitle("Let's join sounds to make a word! Tap each sound, then blend!");

        if (introClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = introClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(introClip.length + 0.3f);
        }

        isTransitioning = false;
    }

    private void OnSoundBoxTapped(SoundBox box)
    {
        if (isTransitioning || box == null) return;

        // Play wiggle animation on tapped sound box
        box.PlayWiggle();

        // Play phoneme sound clip instantly without blocking future taps
        if (box.SoundClip != null && voiceAudioSource != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = box.SoundClip;
            voiceAudioSource.Play();
        }

        // Check if all active sound boxes for current word have been tapped
        bool allTapped = true;
        foreach (var b in soundBoxes)
        {
            if (b != null && b.gameObject.activeSelf && !b.HasBeenTapped)
            {
                allTapped = false;
                break;
            }
        }

        if (allTapped)
        {
            if (blendButtonGlow != null) blendButtonGlow.enabled = true;
            PlayBlendButtonPop();
            SetSubtitle("Tap the BLEND button!");

            float audioDelay = (box.SoundClip != null) ? Mathf.Max(0.35f, box.SoundClip.length * 0.85f) : 0.35f;
            if (tapBlendPromptCoroutine != null) StopCoroutine(tapBlendPromptCoroutine);
            tapBlendPromptCoroutine = StartCoroutine(PlayTapBlendPromptAudio(audioDelay));
        }
    }

    private IEnumerator PlayTapBlendPromptAudio(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (tapBlendPromptClip != null && voiceAudioSource != null && !isTransitioning)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = tapBlendPromptClip;
            voiceAudioSource.Play();
        }
        tapBlendPromptCoroutine = null;
    }

    private void PlayBlendButtonPop()
    {
        if (blendButton == null) return;
        if (blendButtonPopCoroutine != null) StopCoroutine(blendButtonPopCoroutine);
        blendButtonPopCoroutine = StartCoroutine(BlendButtonPopCoroutine());
    }

    private void StopBlendButtonPop()
    {
        if (blendButtonPopCoroutine != null)
        {
            StopCoroutine(blendButtonPopCoroutine);
            blendButtonPopCoroutine = null;
        }
        if (tapBlendPromptCoroutine != null)
        {
            StopCoroutine(tapBlendPromptCoroutine);
            tapBlendPromptCoroutine = null;
        }
        if (blendButton != null)
        {
            blendButton.transform.localScale = blendButtonInitialScale;
        }
    }

    private IEnumerator BlendButtonPopCoroutine()
    {
        if (blendButton == null) yield break;

        // 1. Energetic pop animation: scale up to 1.35x and spring back
        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / duration;
            float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.35f;
            blendButton.transform.localScale = blendButtonInitialScale * scaleFactor;
            yield return null;
        }

        blendButton.transform.localScale = blendButtonInitialScale;

        // 2. Continuous pulse loop to strongly indicate to player to press BLEND
        while (true)
        {
            float pulseElapsed = 0f;
            float pulseDuration = 0.8f;
            while (pulseElapsed < pulseDuration)
            {
                pulseElapsed += Time.deltaTime;
                float p = pulseElapsed / pulseDuration;
                float scaleFactor = 1f + (Mathf.Sin(p * Mathf.PI * 2f) * 0.08f + 0.08f);
                blendButton.transform.localScale = blendButtonInitialScale * scaleFactor;
                yield return null;
            }
        }
    }

    private void OnBlendButtonClicked()
    {
        if (isTransitioning) return;
        StopBlendButtonPop();
        StartCoroutine(BlendSequence());
    }

    private IEnumerator BlendSequence()
    {
        isTransitioning = true;
        StopBlendButtonPop();
        if (blendButtonGlow != null) blendButtonGlow.enabled = false;

        BlendItData data = wordDataSets[currentWordIndex];

        // 1. Play individual sounds sequentially left-to-right with a balanced blending rhythm
        for (int i = 0; i < soundBoxes.Length; i++)
        {
            if (soundBoxes[i] != null && soundBoxes[i].gameObject.activeSelf)
            {
                soundBoxes[i].SetHighlight(true);
                if (soundBoxes[i].SoundClip != null)
                {
                    voiceAudioSource.Stop();
                    voiceAudioSource.clip = soundBoxes[i].SoundClip;
                    voiceAudioSource.Play();

                    float clipLen = soundBoxes[i].SoundClip.length;
                    float waitTime = Mathf.Clamp(clipLen * 0.90f, 0.40f, 0.50f);
                    yield return new WaitForSeconds(waitTime);
                }
                soundBoxes[i].SetHighlight(false);
            }
        }

        // 2. Play slide sound effect & animate boxes sliding together
        if (blendSfx != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(blendSfx);
        }

        for (int i = 0; i < soundBoxes.Length; i++)
        {
            if (soundBoxes[i] != null && joinedTargetPositions != null && i < joinedTargetPositions.Length && joinedTargetPositions[i] != null)
            {
                StartCoroutine(soundBoxes[i].SlideToPosition(joinedTargetPositions[i].position, 0.55f));
            }
        }

        yield return new WaitForSeconds(0.4f);

        // 3. Play blended word audio & show picture
        if (blendedWordText != null) blendedWordText.text = data.targetWord;
        if (wordPictureDisplayImage != null)
        {
            if (data.wordSprite != null) wordPictureDisplayImage.sprite = data.wordSprite;
            wordPictureDisplayImage.enabled = true;
        }

        if (data.blendedWordClip != null)
        {
            voiceAudioSource.Stop();
            voiceAudioSource.clip = data.blendedWordClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(data.blendedWordClip.length + 0.3f);
        }

        // 4. Mascot celebration line
        if (data.mascotCelebrationClip != null)
        {
            SetSubtitle($"{data.targetWord}! You made a word!");
            voiceAudioSource.Stop();
            voiceAudioSource.clip = data.mascotCelebrationClip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(data.mascotCelebrationClip.length + 0.3f);
        }

        // Move to next word
        currentWordIndex++;
        if (currentWordIndex < wordDataSets.Length)
        {
            LoadWord(currentWordIndex);
            isTransitioning = false;
        }
        else
        {
            StartCoroutine(CompleteStopSequence());
        }
    }

    private IEnumerator CompleteStopSequence()
    {
        isActivityCompleted = true;
        SetSubtitle("Awesome blending! You can read words now!");

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
            yield return new WaitForSeconds(stopCompletionClip.length + 0.3f);
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
