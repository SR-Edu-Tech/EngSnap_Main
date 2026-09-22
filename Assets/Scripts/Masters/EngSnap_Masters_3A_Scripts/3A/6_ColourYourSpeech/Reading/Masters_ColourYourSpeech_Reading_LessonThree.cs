using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unit 6 Colour Your Speech — Reading L03 Controller.
/// Activity: "R03 — Say It Another Way — the Swap Box"
/// 
/// Mechanics:
/// - 6-card idiom-to-alternative matching activity.
/// - Presentation: Idiom displayed on an easel presentation card, with an empty "Say instead" speech bubble.
/// - Swap Chips: 4 interactive option chips containing correct book alternatives and distractors.
/// - Flexible Acceptance: Any valid alternative listed in acceptedSwaps evaluates as correct.
/// - Retry Policy: Exactly one retry on wrong selection before correct reveal and advancing.
/// - Audio: VO_R03_ARIA intro, full phrase audio playback on correct selection, SFX_CORRECT, SFX_WRONG.
/// - Progress & Flags: Evaluates pass condition (>= 5 of 6 correct), sets R03 sub-flag, and evaluates Reading = R01 && R02 && R03.
/// - Navigation: Final screen of Reading branch returns cleanly to Hub via Masters_LevelManager (nextLessonSO = null).
/// </summary>
public class Masters_ColourYourSpeech_Reading_LessonThree : Masters_Lesson
{
    [Serializable]
    public class SwapCardData
    {
        public string idiomText;
        public string[] acceptedSwaps;
        public string[] allOptions;
        public AudioClip correctVoiceClip;
        public AudioClip[] acceptedVoiceClips;
        public Sprite idiomArt;
    }

    [Serializable]
    public class SwapChipUI
    {
        [Header("Hierarchy References")]
        public GameObject root;
        public RectTransform rectTransform;
        public Button button;
        public Image background;
        public Image border;
        public TextMeshProUGUI labelTMP;
        public CanvasGroup canvasGroup;

        [NonSerialized] public string currentText;

        public void Initialize(Action<SwapChipUI> onClickCallback)
        {
            if (root != null)
            {
                if (rectTransform == null) rectTransform = root.GetComponent<RectTransform>();
                if (canvasGroup == null) canvasGroup = root.GetComponent<CanvasGroup>();
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClickCallback?.Invoke(this));
            }
        }

        public void SetText(string text)
        {
            currentText = text;
            if (labelTMP != null)
            {
                labelTMP.text = text;
            }
        }

        public void FullReset(Transform expectedParent, int expectedSiblingIndex)
        {
            if (root != null)
            {
                Transform t = root.transform;
                t.DOKill();

                if (expectedParent != null && t.parent != expectedParent)
                {
                    t.SetParent(expectedParent, false);
                }

                if (t.GetSiblingIndex() != expectedSiblingIndex)
                {
                    t.SetSiblingIndex(expectedSiblingIndex);
                }

                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;

                if (rectTransform != null)
                {
                    rectTransform.DOKill();
                    rectTransform.localRotation = Quaternion.identity;
                    rectTransform.localScale = Vector3.one;
                }

                root.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (button != null)
            {
                button.interactable = true;
            }

            if (background != null)
            {
                background.DOKill();
                background.color = new Color32(245, 247, 250, 255);
            }

            if (border != null)
            {
                border.DOKill();
                border.color = new Color32(70, 95, 160, 255);
            }

            if (labelTMP != null)
            {
                labelTMP.DOKill();
                labelTMP.color = new Color32(30, 40, 60, 255);
            }
        }

        public void ResetVisualState()
        {
            if (root != null)
            {
                root.transform.DOKill();
                root.transform.localRotation = Quaternion.identity;
                root.transform.localScale = Vector3.one;
            }

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (background != null)
            {
                background.DOKill();
                background.color = new Color32(245, 247, 250, 255);
            }

            if (border != null)
            {
                border.DOKill();
                border.color = new Color32(70, 95, 160, 255);
            }

            if (button != null)
            {
                button.interactable = true;
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
            if (canvasGroup != null)
            {
                canvasGroup.interactable = interactable;
                canvasGroup.blocksRaycasts = interactable;
            }
        }

        public void HighlightCorrect()
        {
            if (background != null)
            {
                background.DOKill();
                background.color = new Color32(220, 255, 230, 255);
            }

            if (border != null)
            {
                border.DOKill();
                border.color = new Color32(46, 204, 113, 255);
            }

            if (root != null)
            {
                root.transform.DOKill();
                root.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.3f, 8, 1f);
            }
        }

        public void FlashWrong()
        {
            if (background != null)
            {
                background.DOKill();
                background.color = new Color32(255, 230, 230, 255);
            }

            if (border != null)
            {
                border.DOKill();
                border.color = new Color32(231, 76, 60, 255);
            }

            if (root != null)
            {
                root.transform.DOKill();
                root.transform.DOPunchPosition(new Vector3(8f, 0f, 0f), 0.35f, 10, 1f);
            }
        }

        public void DimAndDisable()
        {
            if (button != null) button.interactable = false;
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.DOFade(0.4f, 0.25f);
            }
        }
    }

    [Header("Activity Data (6 Cards)")]
    [SerializeField] private SwapCardData[] defaultCards = new SwapCardData[]
    {
        // Card 1: "My way or the highway" -> "take it or leave it", "end of story"
        new SwapCardData
        {
            idiomText = "My way or the highway",
            acceptedSwaps = new[] { "take it or leave it", "end of story" },
            allOptions = new[] { "take it or leave it", "seventh heaven", "playful", "end of story" }
        },
        // Card 2: "On cloud nine" -> "seventh heaven", "head in the clouds", "over the moon"
        new SwapCardData
        {
            idiomText = "On cloud nine",
            acceptedSwaps = new[] { "seventh heaven", "head in the clouds", "over the moon" },
            allOptions = new[] { "seventh heaven", "take it or leave it", "head in the clouds", "over the moon" }
        },
        // Card 3: "Tongue-in-cheek" -> "cheeky", "playful"
        new SwapCardData
        {
            idiomText = "Tongue-in-cheek",
            acceptedSwaps = new[] { "cheeky", "playful" },
            allOptions = new[] { "cheeky", "end of story", "seventh heaven", "playful" }
        },
        // Card 4: "On cloud nine" -> "over the moon"
        new SwapCardData
        {
            idiomText = "On cloud nine",
            acceptedSwaps = new[] { "over the moon" },
            allOptions = new[] { "take it or leave it", "over the moon", "cheeky", "end of story" }
        },
        // Card 5: "My way or the highway" -> "end of story"
        new SwapCardData
        {
            idiomText = "My way or the highway",
            acceptedSwaps = new[] { "end of story" },
            allOptions = new[] { "seventh heaven", "playful", "end of story", "head in the clouds" }
        },
        // Card 6: "Tongue-in-cheek" -> "playful"
        new SwapCardData
        {
            idiomText = "Tongue-in-cheek",
            acceptedSwaps = new[] { "playful" },
            allOptions = new[] { "over the moon", "playful", "take it or leave it", "seventh heaven" }
        }
    };

    [Header("Screen State References")]
    [SerializeField] private GameObject introScreen;
    [SerializeField] private GameObject gameplayScreen;
    [SerializeField] private Button startButton;

    [Header("Header & Navigation")]
    [SerializeField] private TextMeshProUGUI lessonTitleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private Button backButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Easel / Presentation Box")]
    [SerializeField] private RectTransform easelCardRoot;
    [SerializeField] private TextMeshProUGUI idiomTextTMP;
    [SerializeField] private Image idiomImage;
    [SerializeField] private Image easelBg;
    [SerializeField] private Image easelBorder;

    [Header("Say Instead Speech Bubble")]
    [SerializeField] private RectTransform sayInsteadBubbleRoot;
    [SerializeField] private TextMeshProUGUI sayInsteadLabelTMP;
    [SerializeField] private TextMeshProUGUI sayInsteadValueTMP;
    [SerializeField] private Image sayInsteadBubbleBg;

    [Header("Swap Chip Options (4 Chips)")]
    [SerializeField] private RectTransform optionsContainer;
    [SerializeField] private SwapChipUI[] swapChips = new SwapChipUI[4];

    [Header("Settings & Audio")]
    [SerializeField] private int passThreshold = 5;
    [SerializeField] private AudioClip voAriaIntro;
    [SerializeField] private AudioClip sfxCorrect;
    [SerializeField] private AudioClip sfxWrong;

    [Header("Sub-Lesson Status")]
    [SerializeField] private bool isR01Completed = true;
    [SerializeField] private bool isR02Completed = true;
    [SerializeField] private bool isR03Completed = false;

    // Runtime state
    private int currentCardIndex = 0;
    private int correctScore = 0;
    private int attemptsOnCurrentCard = 0;
    private bool canClick = false;
    private bool isEvaluating = false;
    private bool isCompleted = false;
    private Coroutine activeCardRoutine = null;

    protected override void Awake()
    {
        base.Awake();
        topic = Masters_Topic.Reading;

        FindAndAutoAssignUI();

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (swapChips != null)
        {
            for (int i = 0; i < swapChips.Length; i++)
            {
                if (swapChips[i] != null)
                {
                    swapChips[i].Initialize(OnSwapChipClicked);
                }
            }
        }
    }

    protected override void Start()
    {
        base.Start();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.interactable = false;
        }

        ShowIntroScreen();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
    }

    public void ShowIntroScreen()
    {
        isCompleted = false;
        correctScore = 0;
        currentCardIndex = 0;
        attemptsOnCurrentCard = 0;
        canClick = false;
        isEvaluating = false;

        if (introScreen != null) introScreen.SetActive(true);
        if (gameplayScreen != null) gameplayScreen.SetActive(false);

        if (startButton != null)
        {
            startButton.interactable = true;
        }

        if (lessonTitleTMP != null) lessonTitleTMP.text = "Say It Another Way — the Swap Box";
        if (subtitleTMP != null) subtitleTMP.text = "Pick the alternative expression you can say instead!";
        if (progressTMP != null) progressTMP.text = $"0/{TotalCards}";

        PlayAriaIntroAudio();
    }

    public void OnStartClicked()
    {
        if (introScreen != null) introScreen.SetActive(false);
        if (gameplayScreen != null) gameplayScreen.SetActive(true);

        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        StartGameplay();
    }

    public void StartGameplay()
    {
        currentCardIndex = 0;
        correctScore = 0;
        isCompleted = false;
        LoadCard(currentCardIndex);
    }

    public void LoadCard(int index)
    {
        if (index < 0 || index >= TotalCards)
        {
            OnActivityCompleted();
            return;
        }

        attemptsOnCurrentCard = 0;
        isEvaluating = false;
        canClick = false;

        if (activeCardRoutine != null)
        {
            StopCoroutine(activeCardRoutine);
            activeCardRoutine = null;
        }

        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        SwapCardData card = GetCardData(index);
        if (card == null) return;

        // Update Text
        if (idiomTextTMP != null) idiomTextTMP.text = $"\"{card.idiomText}\"";
        if (sayInsteadLabelTMP != null) sayInsteadLabelTMP.text = "Say instead:";
        if (sayInsteadValueTMP != null) sayInsteadValueTMP.text = "...";
        if (progressTMP != null) progressTMP.text = $"{index + 1}/{TotalCards}";

        // Configure & Full Reset Chips
        string[] options = card.allOptions ?? Array.Empty<string>();
        Transform parentTransform = optionsContainer != null ? optionsContainer.transform : null;

        for (int i = 0; i < swapChips.Length; i++)
        {
            if (swapChips[i] != null && swapChips[i].root != null)
            {
                swapChips[i].FullReset(parentTransform, i);

                if (i < options.Length)
                {
                    swapChips[i].root.SetActive(true);
                    swapChips[i].SetText(options[i]);
                }
                else
                {
                    swapChips[i].root.SetActive(false);
                }
            }
        }

        // Force GridLayoutGroup layout rebuild immediately
        if (optionsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContainer);
        }

        // Animate Entry
        activeCardRoutine = StartCoroutine(AnimateCardEntryRoutine());
    }

    private IEnumerator AnimateCardEntryRoutine()
    {
        if (easelCardRoot != null)
        {
            easelCardRoot.DOKill();
            easelCardRoot.localScale = Vector3.zero;
            easelCardRoot.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (sayInsteadBubbleRoot != null)
        {
            sayInsteadBubbleRoot.DOKill();
            sayInsteadBubbleRoot.localScale = Vector3.zero;
            sayInsteadBubbleRoot.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetDelay(0.1f);
        }

        for (int i = 0; i < swapChips.Length; i++)
        {
            if (swapChips[i] != null && swapChips[i].root != null && swapChips[i].root.activeSelf)
            {
                Transform t = swapChips[i].root.transform;
                t.DOKill();
                t.localScale = Vector3.zero;
                t.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetDelay(0.15f + i * 0.06f);
            }
        }

        yield return new WaitForSeconds(0.45f);
        canClick = true;
    }

    public void OnSwapChipClicked(SwapChipUI chip)
    {
        if (!canClick || isEvaluating || isCompleted || chip == null) return;

        SwapCardData currentCard = GetCardData(currentCardIndex);
        if (currentCard == null) return;

        isEvaluating = true;
        canClick = false;

        bool isCorrect = IsAcceptedSwap(chip.currentText, currentCard.acceptedSwaps);

        if (isCorrect)
        {
            if (activeCardRoutine != null) StopCoroutine(activeCardRoutine);
            activeCardRoutine = StartCoroutine(HandleCorrectSelectionRoutine(chip, currentCard));
        }
        else
        {
            if (activeCardRoutine != null) StopCoroutine(activeCardRoutine);
            activeCardRoutine = StartCoroutine(HandleWrongSelectionRoutine(chip, currentCard));
        }
    }

    private IEnumerator HandleCorrectSelectionRoutine(SwapChipUI chip, SwapCardData currentCard)
    {
        // First-try bonus
        if (attemptsOnCurrentCard == 0)
        {
            correctScore++;
        }

        // Disable all chips during feedback
        SetChipsInteractable(false);

        // Highlight chip
        chip.HighlightCorrect();

        // Update Speech Bubble
        if (sayInsteadValueTMP != null)
        {
            sayInsteadValueTMP.text = $"\"{chip.currentText}\"";
        }

        if (sayInsteadBubbleRoot != null)
        {
            sayInsteadBubbleRoot.DOKill();
            sayInsteadBubbleRoot.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.35f, 10, 1f);
        }

        // Play SFX Correct through dedicated SFX AudioSource
        PlayCorrectAudio();

        // Resolve matching ARIA phrase voice clip
        AudioClip clipToPlay = ResolveVoiceClipForSelectedOption(chip.currentText, currentCard);
        float voiceDuration = 1.2f;
        if (clipToPlay != null)
        {
            voiceDuration = clipToPlay.length;
            if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            }
        }

        // Wait until audio clip has fully finished playing before advancing
        yield return new WaitForSeconds(voiceDuration + 0.35f);

        // Advance to next card
        currentCardIndex++;
        if (currentCardIndex < TotalCards)
        {
            LoadCard(currentCardIndex);
        }
        else
        {
            OnActivityCompleted();
        }
    }

    private AudioClip ResolveVoiceClipForSelectedOption(string selectedText, SwapCardData card)
    {
        if (card == null || string.IsNullOrEmpty(selectedText)) return null;

        if (card.acceptedSwaps != null && card.acceptedVoiceClips != null)
        {
            string selTrimmed = selectedText.Trim().ToLowerInvariant();
            for (int i = 0; i < card.acceptedSwaps.Length; i++)
            {
                if (i < card.acceptedVoiceClips.Length && card.acceptedVoiceClips[i] != null)
                {
                    if (card.acceptedSwaps[i].Trim().Equals(selTrimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        return card.acceptedVoiceClips[i];
                    }
                }
            }
        }

        return card.correctVoiceClip;
    }

    private IEnumerator HandleWrongSelectionRoutine(SwapChipUI chip, SwapCardData currentCard)
    {
        attemptsOnCurrentCard++;

        // Play SFX Wrong
        PlayWrongAudio();

        // Flash and shake wrong chip
        chip.FlashWrong();

        yield return new WaitForSeconds(0.4f);

        if (attemptsOnCurrentCard == 1)
        {
            // Allow exactly one retry: disable the chosen wrong chip, keep others interactable
            chip.DimAndDisable();
            SetChipsInteractable(true);
            chip.SetInteractable(false);
            canClick = true;
            isEvaluating = false;
        }
        else
        {
            // 2nd attempt failed: Reveal correct answer
            SetChipsInteractable(false);

            string firstAccepted = (currentCard.acceptedSwaps != null && currentCard.acceptedSwaps.Length > 0)
                ? currentCard.acceptedSwaps[0]
                : "";

            if (sayInsteadValueTMP != null && !string.IsNullOrEmpty(firstAccepted))
            {
                sayInsteadValueTMP.text = $"\"{firstAccepted}\"";
            }

            if (sayInsteadBubbleRoot != null)
            {
                sayInsteadBubbleRoot.DOKill();
                sayInsteadBubbleRoot.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.3f, 8, 1f);
            }

            // Highlight corresponding chip if present
            for (int i = 0; i < swapChips.Length; i++)
            {
                if (swapChips[i] != null && IsAcceptedSwap(swapChips[i].currentText, currentCard.acceptedSwaps))
                {
                    swapChips[i].HighlightCorrect();
                }
            }

            // Play correct voice clip for reveal
            AudioClip revealClip = ResolveVoiceClipForSelectedOption(firstAccepted, currentCard);
            float revealDuration = 1.2f;
            if (revealClip != null)
            {
                revealDuration = revealClip.length;
                if (Masters_AudioManager.Instance != null)
                {
                    Masters_AudioManager.Instance.PlayVoiceOver(revealClip);
                }
            }

            // Wait until the reveal audio finishes playing completely
            yield return new WaitForSeconds(revealDuration + 0.35f);

            // Advance to next card
            currentCardIndex++;
            if (currentCardIndex < TotalCards)
            {
                LoadCard(currentCardIndex);
            }
            else
            {
                OnActivityCompleted();
            }
        }
    }

    private void SetChipsInteractable(bool interactable)
    {
        for (int i = 0; i < swapChips.Length; i++)
        {
            if (swapChips[i] != null)
            {
                swapChips[i].SetInteractable(interactable);
            }
        }
    }

    private bool IsAcceptedSwap(string selectedText, string[] acceptedSwaps)
    {
        if (string.IsNullOrEmpty(selectedText) || acceptedSwaps == null) return false;
        string selTrimmed = selectedText.Trim().ToLowerInvariant();

        for (int i = 0; i < acceptedSwaps.Length; i++)
        {
            if (string.IsNullOrEmpty(acceptedSwaps[i])) continue;
            if (selTrimmed.Equals(acceptedSwaps[i].Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void OnActivityCompleted()
    {
        if (isCompleted) return;
        isCompleted = true;

        if (progressTMP != null)
        {
            progressTMP.text = $"{TotalCards}/{TotalCards}";
        }

        bool passed = correctScore >= passThreshold;

        if (passed)
        {
            isR03Completed = true;
            M3A_U6_HubProgress.MarkR03Complete();

            if (isR01Completed && isR02Completed && isR03Completed)
            {
                M3A_U6_HubProgress.MarkComplete(M3A_U6_Branch.Reading);
            }
            else if (M3A_U6_HubProgress.IsR01Complete && M3A_U6_HubProgress.IsR02Complete && M3A_U6_HubProgress.IsR03Complete)
            {
                M3A_U6_HubProgress.MarkComplete(M3A_U6_Branch.Reading);
            }
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    protected override void OnNextButtonClicked()
    {
        if (correctScore >= passThreshold)
        {
            M3A_U6_HubProgress.MarkR03Complete();
            M3A_U6_HubProgress.MarkComplete(M3A_U6_Branch.Reading);
        }

        if (nextLessonSO != null)
        {
            if (Masters_LevelManager.Instance != null)
            {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        }
        else
        {
            if (Masters_LevelManager.Instance != null)
            {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    private void OnBackClicked()
    {
        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
    }

    private void PlayAriaIntroAudio()
    {
        if (voAriaIntro != null && Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlayVoiceOver(voAriaIntro);
        }
    }

    private void PlayCorrectAudio()
    {
        if (Masters_AudioManager.Instance != null)
        {
            if (sfxCorrect != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(sfxCorrect);
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        }
    }

    private void PlayWrongAudio()
    {
        if (Masters_AudioManager.Instance != null)
        {
            if (sfxWrong != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(sfxWrong);
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    private SwapCardData GetCardData(int index)
    {
        if (defaultCards != null && index >= 0 && index < defaultCards.Length)
        {
            return defaultCards[index];
        }
        return null;
    }

    public int TotalCards => defaultCards != null ? defaultCards.Length : 6;
    public int CurrentCardIndex => currentCardIndex;
    public int CorrectScore => correctScore;
    public int PassThreshold => passThreshold;

    private void FindAndAutoAssignUI()
    {
        if (introScreen == null)
        {
            Transform t = transform.Find("IntroScreen");
            if (t != null) introScreen = t.gameObject;
        }

        if (gameplayScreen == null)
        {
            Transform t = transform.Find("GameplayScreen");
            if (t != null) gameplayScreen = t.gameObject;
        }

        if (startButton == null && introScreen != null)
        {
            startButton = introScreen.GetComponentInChildren<Button>(true);
        }

        if (lessonTitleTMP == null)
        {
            Transform t = transform.Find("GameplayScreen/LessonTitle") ?? transform.Find("LessonTitle");
            if (t != null) lessonTitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (subtitleTMP == null)
        {
            Transform t = transform.Find("GameplayScreen/Subtitle") ?? transform.Find("Subtitle");
            if (t != null) subtitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (progressTMP == null)
        {
            Transform t = transform.Find("GameplayScreen/ProgressText") ?? transform.Find("ProgressText");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (backButton == null)
        {
            Transform t = transform.Find("BackButton") ?? transform.Find("TopBar/BackButton");
            if (t != null) backButton = t.GetComponent<Button>();
        }

        if (nextButton == null)
        {
            Transform t = transform.Find("GameplayScreen/NextButton") ?? transform.Find("NextButton");
            if (t != null) nextButton = t.GetComponent<Button>();
        }

        if (easelCardRoot == null)
        {
            Transform t = transform.Find("GameplayScreen/EaselCard") ?? transform.Find("EaselCard");
            if (t != null)
            {
                easelCardRoot = t.GetComponent<RectTransform>();
                if (idiomTextTMP == null) idiomTextTMP = t.Find("IdiomText")?.GetComponent<TextMeshProUGUI>();
            }
        }

        if (sayInsteadBubbleRoot == null)
        {
            Transform t = transform.Find("GameplayScreen/SayInsteadBubble") ?? transform.Find("SayInsteadBubble");
            if (t != null)
            {
                sayInsteadBubbleRoot = t.GetComponent<RectTransform>();
                if (sayInsteadLabelTMP == null) sayInsteadLabelTMP = t.Find("LabelText")?.GetComponent<TextMeshProUGUI>();
                if (sayInsteadValueTMP == null) sayInsteadValueTMP = t.Find("ValueText")?.GetComponent<TextMeshProUGUI>();
            }
        }

        if (optionsContainer == null)
        {
            Transform t = transform.Find("GameplayScreen/GameArea/OptionsContainer") ?? transform.Find("GameArea/OptionsContainer") ?? transform.Find("GameplayScreen/OptionsContainer");
            if (t != null)
            {
                optionsContainer = t.GetComponent<RectTransform>();
            }
        }
    }
}
