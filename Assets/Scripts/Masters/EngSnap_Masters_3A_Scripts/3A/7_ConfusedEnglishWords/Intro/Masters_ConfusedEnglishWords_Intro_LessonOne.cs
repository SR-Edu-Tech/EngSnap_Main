using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Intro Lesson One (Intro — Word Detective Agency).
/// Sets the theme, introduces the three case file twin pairs, and launches the Hub.
/// </summary>
public class Masters_ConfusedEnglishWords_Intro_LessonOne : Masters_Lesson
{
    [Header("Intro UI References")]
    [SerializeField] private TMP_Text headerTMP;
    [SerializeField] private TMP_Text titleTMP;
    [SerializeField] private TMP_Text introDescriptionTMP;
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    [Header("Case File Visuals")]
    [SerializeField] private GameObject casePanel;
    [SerializeField] private GameObject quietQuiteCard;
    [SerializeField] private GameObject diaryDairyCard;
    [SerializeField] private GameObject whoeverWhomeverCard;
    [SerializeField] private GameObject ariaMascot;
    [SerializeField] private GameObject leoMascot;
    [SerializeField] private Animator characterAnimator;

    [Header("Intro Audio")]
    [SerializeField] private AudioClip voIntroAria;

    private bool transitionStarted = false;

    protected override void Awake()
    {
        topic = Masters_Topic.Intro;

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
            nextButton = startButton;
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    protected override void Start()
    {
        base.Start();
        transitionStarted = false;
        Debug.Log("[U7 FLOW] Intro opened");
        PopulateIntroContent();

        if (characterAnimator != null)
        {
            characterAnimator.enabled = true;
            characterAnimator.Play("Hello", 0, 0f);
        }

        if (voIntroAria != null && Masters_AudioManager.Instance != null)
        {
            if (startButton != null) startButton.interactable = false;
            Debug.Log("[U7 FLOW] Intro VO started");
            Masters_AudioManager.Instance.PlayVoiceOver(voIntroAria);
            StartCoroutine(WaitForIntroVOSequence());
        }
        else
        {
            if (startButton != null) startButton.interactable = true;
        }
    }

    private System.Collections.IEnumerator WaitForIntroVOSequence()
    {
        yield return null;
        if (Masters_AudioManager.Instance != null)
        {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {});
        }
        Debug.Log("[U7 FLOW] Intro VO completed");
        if (startButton != null)
        {
            startButton.interactable = true;
        }
    }

    private void PopulateIntroContent()
    {
        if (headerTMP != null) headerTMP.text = "WORD DETECTIVE AGENCY";
        if (titleTMP != null) titleTMP.text = "CONFUSED ENGLISH WORDS";
        if (introDescriptionTMP != null)
        {
            introDescriptionTMP.text = "Some English words look almost identical, but their meanings are completely different.\n\nYour mission is to investigate the clues, find the correct word, and solve every case!";
        }
    }

    private void OnStartClicked()
    {
        if (transitionStarted) return;
        transitionStarted = true;

        if (startButton != null) startButton.interactable = false;

        Debug.Log("[U7 FLOW] Intro START pressed");

        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_HubProgress.MarkIntroComplete();
            Debug.Log("[U7 FLOW] Intro marked complete");
            M3A_U7_ScreenController.Instance.ShowHub();
        }
        else if (Masters_LevelManager.Instance != null)
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
        if (transitionStarted) return;
        transitionStarted = true;

        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    protected override void OnNextButtonClicked()
    {
        OnStartClicked();
    }
}
