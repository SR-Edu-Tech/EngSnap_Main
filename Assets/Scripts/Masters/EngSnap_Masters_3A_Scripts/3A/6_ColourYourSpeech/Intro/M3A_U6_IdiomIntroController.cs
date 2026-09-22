using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presents the Unit 6 Idiom Paint Studio introduction inside the existing Masters_3A lesson canvas.
/// Inherits from Masters_Lesson following the standard Book 3 Intro architectural pattern (Unit 5 reference).
/// </summary>
public sealed class M3A_U6_IdiomIntroController : Masters_Lesson
{
    [Header("Sequence")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private CanvasGroup potsGroup;
    [SerializeField] private CanvasGroup leftEaselGroup;
    [SerializeField] private CanvasGroup rightEaselGroup;
    [SerializeField] private CanvasGroup leftArtGroup;
    [SerializeField] private CanvasGroup rightArtGroup;
    [SerializeField] private CanvasGroup splatGroup;
    [SerializeField] private CanvasGroup meaningCardGroup;
    [SerializeField] private CanvasGroup startGroup;
    [SerializeField] private RectTransform splatTransform;
    [SerializeField] private RectTransform brushTransform;
    [SerializeField] private RectTransform startButtonTransform;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float stepPause = 0.8f;

    [Header("Dialogue")]
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text meaningText;
    [SerializeField] private TMP_Text finalText;
    [SerializeField] private TMP_Text startLabel;

    [Header("Controls")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button ariaReplayButton;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject unitSelectionCanvas;
    [SerializeField] private GameObject topicSelectionCanvas;

    [Header("Audio")]
    [SerializeField] private AudioClip voIntroAria;
    [SerializeField] private AudioClip voIntroLeo;
    [SerializeField] private AudioClip sfxSqueeze;
    [SerializeField] private AudioClip sfxBrush;
    [SerializeField] private AudioClip musUnitTheme;

    [Header("Character")]
    [SerializeField] private GameObject characterObject;
    [SerializeField] private CanvasGroup characterGroup;
    [SerializeField] private Animator characterAnimator;

    private Coroutine sequenceCoroutine;
    private bool sequenceComplete;
    private bool transitionStarted;

    protected override void Awake()
    {
        topic = Masters_Topic.Intro;

        if (backButton == null)
        {
            Transform backButtonTransform = transform.Find("BackButton");
            if (backButtonTransform != null)
            {
                backButton = backButtonTransform.GetComponent<Button>();
            }
        }

        if (startButton == null)
        {
            Transform startBtnTrans = transform.Find("StartButton");
            if (startBtnTrans == null) startBtnTrans = FindChildRecursive(transform, "StartButton");
            if (startBtnTrans != null) startButton = startBtnTrans.GetComponent<Button>();
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
            startButton.interactable = false;
            nextButton = startButton;
        }

        if (ariaReplayButton != null)
        {
            ariaReplayButton.onClick.RemoveAllListeners();
            ariaReplayButton.onClick.AddListener(ReplayAriaVoiceOver);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (characterObject == null)
        {
            Transform charTrans = transform.Find("Character");
            if (charTrans != null)
            {
                characterObject = charTrans.gameObject;
                if (characterGroup == null) characterGroup = characterObject.GetComponent<CanvasGroup>();
                if (characterAnimator == null) characterAnimator = characterObject.GetComponentInChildren<Animator>();
            }
        }
    }

    protected override void Start()
    {
        HideExistingSelectionCanvases();
        PlayUnitTheme();

        if (sequenceCoroutine == null)
        {
            sequenceCoroutine = StartCoroutine(PlayIntroSequence());
        }
    }

    protected override void OnNextButtonClicked()
    {
        OnStartClicked();
    }

    /// <summary>
    /// Activates and restarts the canonical scene-based intro without creating another instance.
    /// </summary>
    public void BeginIntro()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        sequenceComplete = false;
        transitionStarted = false;
        HideExistingSelectionCanvases();
        PlayUnitTheme();
        sequenceCoroutine = StartCoroutine(PlayIntroSequence());
    }

    private void OnDisable()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }
    }

    private IEnumerator PlayIntroSequence()
    {
        PrepareInitialState();

        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutSine);
        }

        yield return new WaitForSeconds(fadeDuration);
        yield return RevealGroup(potsGroup, 0.55f);
        yield return new WaitForSeconds(stepPause);

        SetDialogue("LEO", "If I'm late, my dad will go bananas!", "GOING BANANAS");
        PlaySqueezeSound();
        yield return RevealGroup(splatGroup, 0.35f);
        yield return new WaitForSeconds(stepPause);

        yield return RevealGroup(leftEaselGroup, 0.65f);
        yield return RevealGroup(leftArtGroup, 0.45f);
        PlayBrushSound();
        PlayBrushAnimation();
        if (characterAnimator != null)
        {
            characterAnimator.enabled = true;
            characterAnimator.Play("Hello", 0, 0f);
        }
        PlayVoiceOver(voIntroLeo);
        yield return WaitForVoiceOverToFinish(voIntroLeo);

        SetDialogue("ARIA", "Idioms paint pictures with words — but never the picture you expect!", "LITERAL OR FIGURATIVE?");
        PlayVoiceOver(voIntroAria);
        yield return new WaitForSeconds(1.4f);

        yield return RevealGroup(rightEaselGroup, 0.65f);
        yield return RevealGroup(rightArtGroup, 0.45f);
        DimLiteralAndHighlightFigurative();
        yield return RevealGroup(meaningCardGroup, 0.55f);
        yield return new WaitForSeconds(stepPause);

        if (finalText != null)
        {
            finalText.text = "Today we learn ten colourful idioms and what they really mean.";
            finalText.gameObject.SetActive(true);
            finalText.alpha = 0f;
            finalText.DOFade(1f, 0.45f);
        }

        yield return new WaitForSeconds(0.65f);
        ShowStartButton();
        sequenceComplete = true;
        sequenceCoroutine = null;
    }

    private void PrepareInitialState()
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
        }

        SetGroupAlpha(potsGroup, 0f);
        SetGroupAlpha(leftEaselGroup, 0f);
        SetGroupAlpha(rightEaselGroup, 0f);
        SetGroupAlpha(leftArtGroup, 0f);
        SetGroupAlpha(rightArtGroup, 0f);
        SetGroupAlpha(splatGroup, 0f);
        SetGroupAlpha(meaningCardGroup, 0f);
        SetGroupAlpha(startGroup, 0f);

        if (characterObject != null)
        {
            characterObject.SetActive(true);
        }

        if (characterGroup != null)
        {
            characterGroup.alpha = 1f;
            characterGroup.interactable = true;
            characterGroup.blocksRaycasts = false;
        }

        if (characterAnimator != null)
        {
            characterAnimator.enabled = true;
            characterAnimator.Play("Hello", 0, 0f);
        }

        if (splatTransform != null)
        {
            splatTransform.DOKill();
            splatTransform.localScale = Vector3.zero;
        }

        if (brushTransform != null)
        {
            brushTransform.DOKill();
            brushTransform.localScale = Vector3.zero;
        }

        if (startButtonTransform != null)
        {
            startButtonTransform.DOKill();
        }

        if (finalText != null)
        {
            finalText.gameObject.SetActive(false);
        }

        if (meaningText != null)
        {
            meaningText.text = "GOING BANANAS\nFeeling extremely excited, angry, or overwhelmed";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "Literal or figurative? Let’s paint the meaning.";
        }

        if (startLabel != null)
        {
            startLabel.text = "START";
        }
    }

    private IEnumerator RevealGroup(CanvasGroup group, float duration)
    {
        if (group == null)
        {
            yield break;
        }

        group.gameObject.SetActive(true);
        group.interactable = true;
        group.blocksRaycasts = true;
        group.DOFade(1f, duration).SetEase(Ease.OutSine);
        if (group == splatGroup && splatTransform != null)
        {
            splatTransform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(duration);
    }

    private void DimLiteralAndHighlightFigurative()
    {
        if (leftEaselGroup != null)
        {
            leftEaselGroup.DOFade(0.35f, 0.4f);
        }

        if (rightEaselGroup != null)
        {
            rightEaselGroup.DOFade(1f, 0.4f);
        }
    }

    private void ShowStartButton()
    {
        if (startButton == null)
        {
            return;
        }

        startButton.interactable = true;
        if (startGroup != null)
        {
            startGroup.gameObject.SetActive(true);
            startGroup.interactable = true;
            startGroup.blocksRaycasts = true;
            startGroup.DOFade(1f, 0.4f);
        }

        if (startButtonTransform != null)
        {
            startButtonTransform.localScale = Vector3.one;
            startButtonTransform.DOScale(Vector3.one * 0.92f, 0.55f).SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void SetDialogue(string speaker, string dialogue, string subtitle)
    {
        if (speakerText != null)
        {
            speakerText.text = speaker;
        }

        if (dialogueText != null)
        {
            dialogueText.text = dialogue;
        }

        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
        }
    }

    private IEnumerator WaitForVoiceOverToFinish(AudioClip clip)
    {
        if (clip == null)
        {
            yield break;
        }

        if (Masters_AudioManager.Instance != null)
        {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
            yield break;
        }

        yield return new WaitForSeconds(clip.length);
    }

    private void PlayVoiceOver(AudioClip clip)
    {
        if (clip == null || Masters_AudioManager.Instance == null)
        {
            return;
        }

        Masters_AudioManager.Instance.PlayVoiceOver(clip);
    }

    private void PlaySqueezeSound()
    {
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(sfxSqueeze);
        }
    }

    private void PlayBrushAnimation()
    {
        if (brushTransform == null)
        {
            return;
        }

        brushTransform.localScale = Vector3.zero;
        brushTransform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
    }

    private void PlayBrushSound()
    {
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(sfxBrush);
        }
    }

    private void PlayUnitTheme()
    {
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlayMusic(musUnitTheme);
        }
    }

    /// <summary>
    /// Replays ARIA's explanation without restarting the introduction sequence.
    /// </summary>
    public void ReplayAriaVoiceOver()
    {
        PlayVoiceOver(voIntroAria);
    }

    private void OnStartClicked()
    {
        if (transitionStarted)
        {
            return;
        }

        transitionStarted = true;
        if (startButton != null)
        {
            startButton.interactable = false;
        }

        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        M3A_U6_HubProgress.MarkIntroComplete();
        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Intro);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnBackClicked()
    {
        if (transitionStarted)
        {
            return;
        }

        transitionStarted = true;
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }

        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void HideExistingSelectionCanvases()
    {
        if (unitSelectionCanvas != null)
        {
            unitSelectionCanvas.SetActive(false);
        }

        if (topicSelectionCanvas != null)
        {
            topicSelectionCanvas.SetActive(false);
        }
    }

    private static void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
