using System;
using UnityEngine;

namespace EngSnap.ConfusedWords
{
    public enum U7_ScreenState
    {
        Intro,
        Hub,
        L01,
        L02,
        R01,
        R02,
        R03,
        W01,
        W02,
        SP01,
        G01,
        G02,
        RP01,
        RP02,
        Q01,
        RWD
    }

    /// <summary>
    /// Master same-scene screen controller for Unit 7 (Confused English Words).
    /// Manages state switching without scene loading, fully inside Masters_3A.unity.
    /// </summary>
    public class M3A_U7_ScreenController : MonoBehaviour
    {
        public static M3A_U7_ScreenController Instance { get; private set; }

        [Header("Screen State")]
        [SerializeField] private U7_ScreenState currentState = U7_ScreenState.Intro;

        [Header("Screen Roots / Containers")]
        [SerializeField] private GameObject introScreenContainer;
        [SerializeField] private GameObject hubScreenContainer;
        [SerializeField] private GameObject listeningContainer;
        [SerializeField] private GameObject readingContainer;
        [SerializeField] private GameObject writingContainer;
        [SerializeField] private GameObject speakingContainer;
        [SerializeField] private GameObject gameContainer;
        [SerializeField] private GameObject roleplayContainer;
        [SerializeField] private GameObject quizContainer;
        [SerializeField] private GameObject rewardContainer;

        public event Action<U7_ScreenState> ScreenChanged;

        public U7_ScreenState CurrentState => currentState;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (M3A_U7_HubProgress.IsIntroComplete())
            {
                ShowHub();
            }
            else
            {
                ShowIntro();
            }
        }

        public void ShowIntro()
        {
            TransitionToState(U7_ScreenState.Intro);
        }

        public void ShowHub()
        {
            TransitionToState(U7_ScreenState.Hub);
        }

        public void ShowListening(int subIndex = 1)
        {
            TransitionToState(subIndex <= 1 ? U7_ScreenState.L01 : U7_ScreenState.L02);
        }

        public void ShowReading(int subIndex = 1)
        {
            if (subIndex <= 1) TransitionToState(U7_ScreenState.R01);
            else if (subIndex == 2) TransitionToState(U7_ScreenState.R02);
            else TransitionToState(U7_ScreenState.R03);
        }

        public void ShowWriting(int subIndex = 1)
        {
            TransitionToState(subIndex <= 1 ? U7_ScreenState.W01 : U7_ScreenState.W02);
        }

        public void ShowSpeaking()
        {
            TransitionToState(U7_ScreenState.SP01);
        }

        public void ShowGame(int subIndex = 1)
        {
            TransitionToState(subIndex <= 1 ? U7_ScreenState.G01 : U7_ScreenState.G02);
        }

        public void ShowGame02()
        {
            ShowGame(2);
        }

        public void ShowRolePlay(int subIndex = 1)
        {
            TransitionToState(subIndex <= 1 ? U7_ScreenState.RP01 : U7_ScreenState.RP02);
        }

        public void ShowRolePlay02()
        {
            ShowRolePlay(2);
        }


        public void ShowQuiz()
        {
            if (M3A_U7_HubProgress.QuizUnlocked)
            {
                TransitionToState(U7_ScreenState.Q01);
            }
            else
            {
                Debug.LogWarning("[Unit 7 ScreenController] Cannot open Quiz: All 6 branches are not yet complete.");
            }
        }

        public void ShowReward()
        {
            if (M3A_U7_HubProgress.RewardUnlocked)
            {
                TransitionToState(U7_ScreenState.RWD);
            }
            else
            {
                Debug.LogWarning("[Unit 7 ScreenController] Cannot open Reward: Quiz is not yet passed.");
            }
        }

        public void ReturnToHub()
        {
            ShowHub();
        }

        public void TransitionToState(U7_ScreenState targetState)
        {
            currentState = targetState;

            // Activate target container and deactivate others if wired
            SetContainerActive(introScreenContainer, targetState == U7_ScreenState.Intro);
            SetContainerActive(hubScreenContainer, targetState == U7_ScreenState.Hub);
            SetContainerActive(listeningContainer, targetState == U7_ScreenState.L01 || targetState == U7_ScreenState.L02);
            SetContainerActive(readingContainer, targetState == U7_ScreenState.R01 || targetState == U7_ScreenState.R02 || targetState == U7_ScreenState.R03);
            SetContainerActive(writingContainer, targetState == U7_ScreenState.W01 || targetState == U7_ScreenState.W02);
            SetContainerActive(speakingContainer, targetState == U7_ScreenState.SP01);
            SetContainerActive(gameContainer, targetState == U7_ScreenState.G01 || targetState == U7_ScreenState.G02);
            SetContainerActive(roleplayContainer, targetState == U7_ScreenState.RP01 || targetState == U7_ScreenState.RP02);
            SetContainerActive(quizContainer, targetState == U7_ScreenState.Q01);
            SetContainerActive(rewardContainer, targetState == U7_ScreenState.RWD);

            ScreenChanged?.Invoke(currentState);
        }

        private void SetContainerActive(GameObject container, bool active)
        {
            if (container != null && container.activeSelf != active)
            {
                container.SetActive(active);
            }
        }
    }
}
