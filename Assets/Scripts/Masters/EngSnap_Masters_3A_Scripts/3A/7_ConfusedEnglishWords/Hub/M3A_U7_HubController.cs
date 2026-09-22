using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.ConfusedWords
{
    /// <summary>
    /// Controller for the Word Detective Agency Hub.
    /// Manages case desks, progress counter (0/6 -> 6/6), branch completion stamps, and Case-Closed gating.
    /// </summary>
    public class M3A_U7_HubController : MonoBehaviour
    {
        [Header("Desk Buttons")]
        [SerializeField] private Button listeningDeskButton;
        [SerializeField] private Button readingDeskButton;
        [SerializeField] private Button writingDeskButton;
        [SerializeField] private Button speakingDeskButton;
        [SerializeField] private Button gameDeskButton;
        [SerializeField] private Button roleplayDeskButton;
        [SerializeField] private Button caseClosedQuizButton;

        [Header("Solved Stamps / Badges")]
        [SerializeField] private GameObject listeningSolvedStamp;
        [SerializeField] private GameObject readingSolvedStamp;
        [SerializeField] private GameObject writingSolvedStamp;
        [SerializeField] private GameObject speakingSolvedStamp;
        [SerializeField] private GameObject gameSolvedStamp;
        [SerializeField] private GameObject roleplaySolvedStamp;

        [Header("Case Closed Room / Quiz Gating")]
        [SerializeField] private GameObject caseClosedLockOverlay;
        [SerializeField] private GameObject caseClosedUnlockedFeedback;

        [Header("Progress UI")]
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Slider progressBar;

        [Header("Header & Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button ariaChiefReplayButton;
        [SerializeField] private AudioClip voHubAria;

        private void OnEnable()
        {
            Debug.Log("[U7 FLOW] Hub opened");
            M3A_U7_HubProgress.ProgressUpdated += RefreshHubUI;
            M3A_U7_HubProgress.BranchCompleted += OnBranchCompleted;
            RegisterButtonListeners();
            RefreshHubUI();
        }

        private void OnDisable()
        {
            M3A_U7_HubProgress.ProgressUpdated -= RefreshHubUI;
            M3A_U7_HubProgress.BranchCompleted -= OnBranchCompleted;
            RemoveButtonListeners();
        }

        private void RegisterButtonListeners()
        {
            RemoveButtonListeners();

            if (listeningDeskButton != null) listeningDeskButton.onClick.AddListener(OnListeningDeskClicked);
            if (readingDeskButton != null) readingDeskButton.onClick.AddListener(OnReadingDeskClicked);
            if (writingDeskButton != null) writingDeskButton.onClick.AddListener(OnWritingDeskClicked);
            if (speakingDeskButton != null) speakingDeskButton.onClick.AddListener(OnSpeakingDeskClicked);
            if (gameDeskButton != null) gameDeskButton.onClick.AddListener(OnGameDeskClicked);
            if (roleplayDeskButton != null) roleplayDeskButton.onClick.AddListener(OnRoleplayDeskClicked);
            if (caseClosedQuizButton != null) caseClosedQuizButton.onClick.AddListener(OnQuizDeskClicked);
            if (ariaChiefReplayButton != null) ariaChiefReplayButton.onClick.AddListener(OnAriaChiefClicked);
            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void RemoveButtonListeners()
        {
            if (listeningDeskButton != null) listeningDeskButton.onClick.RemoveAllListeners();
            if (readingDeskButton != null) readingDeskButton.onClick.RemoveAllListeners();
            if (writingDeskButton != null) writingDeskButton.onClick.RemoveAllListeners();
            if (speakingDeskButton != null) speakingDeskButton.onClick.RemoveAllListeners();
            if (gameDeskButton != null) gameDeskButton.onClick.RemoveAllListeners();
            if (roleplayDeskButton != null) roleplayDeskButton.onClick.RemoveAllListeners();
            if (caseClosedQuizButton != null) caseClosedQuizButton.onClick.RemoveAllListeners();
            if (ariaChiefReplayButton != null) ariaChiefReplayButton.onClick.RemoveAllListeners();
            if (backButton != null) backButton.onClick.RemoveAllListeners();
        }

        public void RefreshHubUI()
        {
            int completed = M3A_U7_HubProgress.CompletedCount;

            if (progressText != null)
            {
                progressText.text = $"{completed}/6";
            }

            if (progressBar != null)
            {
                progressBar.value = Mathf.Clamp01(completed / 6f);
            }

            // Solved stamps
            SetObjectActive(listeningSolvedStamp, M3A_U7_HubProgress.IsListeningComplete);
            SetObjectActive(readingSolvedStamp, M3A_U7_HubProgress.IsReadingComplete);
            SetObjectActive(writingSolvedStamp, M3A_U7_HubProgress.IsWritingComplete);
            SetObjectActive(speakingSolvedStamp, M3A_U7_HubProgress.IsSpeakingComplete);
            SetObjectActive(gameSolvedStamp, M3A_U7_HubProgress.IsGameComplete);
            SetObjectActive(roleplaySolvedStamp, M3A_U7_HubProgress.IsRoleplayComplete);

            // Quiz room gating
            bool isQuizUnlocked = M3A_U7_HubProgress.QuizUnlocked;
            if (caseClosedQuizButton != null)
            {
                caseClosedQuizButton.interactable = isQuizUnlocked;
            }

            SetObjectActive(caseClosedLockOverlay, !isQuizUnlocked);
            SetObjectActive(caseClosedUnlockedFeedback, isQuizUnlocked);
        }

        private void OnBranchCompleted(M3A_U7_Branch branch)
        {
            RefreshHubUI();
        }

        private void OnListeningDeskClicked()
        {
            Debug.Log("[U7 FLOW] Activity selected: L01");
            Debug.Log("[U7 FLOW] Activity audio unlocked");
            if (M3A_U7_ScreenController.Instance != null)
            {
                M3A_U7_ScreenController.Instance.ShowListening(1);
            }
        }

        private void OnReadingDeskClicked()
        {
            Debug.Log("[U7 FLOW] Activity selected: R01");
            Debug.Log("[U7 FLOW] Activity audio unlocked");
            if (M3A_U7_ScreenController.Instance != null)
            {
                M3A_U7_ScreenController.Instance.ShowReading(1);
            }
        }

        private void OnWritingDeskClicked()
        {
            Debug.Log("[U7 FLOW] Activity selected: W01");
            Debug.Log("[U7 FLOW] Activity audio unlocked");
            if (M3A_U7_ScreenController.Instance != null)
            {
                M3A_U7_ScreenController.Instance.ShowWriting(1);
            }
        }

        private void OnSpeakingDeskClicked()
        {
            Debug.Log("[U7 FLOW] Activity selected: SP01");
            Debug.Log("[U7 FLOW] Activity audio unlocked");
            if (M3A_U7_ScreenController.Instance != null)
            {
                M3A_U7_ScreenController.Instance.ShowSpeaking();
            }
        }

        private void OnGameDeskClicked()
        {
            Debug.Log("[U7 FLOW] Activity selected: G01");
            Debug.Log("[U7 FLOW] Activity audio unlocked");
            if (M3A_U7_ScreenController.Instance != null)
            {
                M3A_U7_ScreenController.Instance.ShowGame(1);
            }
        }

        private void OnRoleplayDeskClicked()
        {
            Debug.Log("[U7 FLOW] Activity selected: RP01");
            Debug.Log("[U7 FLOW] Activity audio unlocked");
            if (M3A_U7_ScreenController.Instance != null)
            {
                M3A_U7_ScreenController.Instance.ShowRolePlay(1);
            }
        }

        private void OnQuizDeskClicked()
        {
            if (M3A_U7_HubProgress.QuizUnlocked)
            {
                Debug.Log("[U7 FLOW] Activity selected: Q01");
                Debug.Log("[U7 FLOW] Activity audio unlocked");
                if (M3A_U7_ScreenController.Instance != null)
                {
                    M3A_U7_ScreenController.Instance.ShowQuiz();
                }
            }
        }


        private void OnAriaChiefClicked()
        {
            if (Masters_AudioManager.Instance != null && voHubAria != null)
            {
                Masters_AudioManager.Instance.PlayVoiceOver(voHubAria);
            }
        }

        private void OnBackClicked()
        {
            if (Masters_LevelManager.Instance != null)
            {
                Masters_LevelManager.Instance.OnBackButtonClicked();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void SetObjectActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
