using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Rewards Lesson One (RWD — Word Detective Agency Badge & Unit Completion).
/// Certified Word Detective Award screen presented upon solving all 6 cases and passing Q01.
/// </summary>
public class Masters_ConfusedEnglishWords_Rewards_LessonOne : Masters_Lesson
{
    [Header("UI References")]
    [SerializeField] private TMP_Text headerTMP;
    [SerializeField] private TMP_Text titleTMP;
    [SerializeField] private TMP_Text badgeTitleTMP;
    [SerializeField] private TMP_Text messageTMP;
    [SerializeField] private TMP_Text casesSolvedTMP;
    [SerializeField] private TMP_Text quizScoreTMP;
    [SerializeField] private TMP_Text congratsTMP;
    [SerializeField] private Image wordDetectiveBadge;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button backButton;

    protected override void Awake()
    {
        topic = Masters_Topic.Rewards;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnContinueClicked);
            nextButton.gameObject.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
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

        // Safe gating guard: if accessed while locked, return to Hub immediately
        if (!M3A_U7_HubProgress.IsIntroComplete() || !M3A_U7_HubProgress.RewardUnlocked || M3A_U7_HubProgress.CompletedCount < 6 || !M3A_U7_HubProgress.IsQ01Complete)
        {
            Debug.LogWarning("[U7 AUDIO BLOCKED] Rewards_LessonOne access denied: Unit 7 Intro, all 6 branches and Q01 must be completed first.");
            if (M3A_U7_ScreenController.Instance != null)
            {
                M3A_U7_ScreenController.Instance.ReturnToHub();
            }
            return;
        }


        PopulateRewardDisplay();

        // Mark Reward complete, award badge and complete unit canonically
        M3A_U7_HubProgress.AwardWordDetectiveBadge();
        M3A_U7_HubProgress.MarkRewardComplete();
        M3A_U7_HubProgress.MarkUnitComplete();
    }

    private void PopulateRewardDisplay()
    {
        if (headerTMP != null) headerTMP.text = "WORD DETECTIVE AGENCY";
        if (titleTMP != null) titleTMP.text = "CASE CLOSED!";
        if (badgeTitleTMP != null) badgeTitleTMP.text = "CERTIFIED WORD DETECTIVE";
        if (messageTMP != null) messageTMP.text = "You solved every case file and passed the final Word Detective Assessment!";
        if (casesSolvedTMP != null) casesSolvedTMP.text = "6/6 CASE FILES SOLVED";
        if (quizScoreTMP != null) quizScoreTMP.text = "FINAL QUIZ: PASSED (>= 9/12)";
        if (congratsTMP != null) congratsTMP.text = "Chief Detective Aria & Junior Detective Leo commend your outstanding service to the English language!";
    }

    private void OnContinueClicked()
    {
        M3A_U7_HubProgress.AwardWordDetectiveBadge();
        M3A_U7_HubProgress.MarkRewardComplete();
        M3A_U7_HubProgress.MarkUnitComplete();

        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Rewards);
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
    }



    protected override void OnNextButtonClicked()
    {
        OnContinueClicked();
    }

    private void OnBackClicked()
    {
        if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
    }
}

