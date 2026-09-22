using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.ConfusedWords
{
    /// <summary>
    /// Foundation controller for Unit 7 Intro (Word Detective Agency).
    /// Inherits from Masters_Lesson and handles the same-scene transition to Hub.
    /// </summary>
    public class M3A_U7_IntroController : Masters_Lesson
    {
        [Header("Intro UI Controls")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button ariaReplayButton;
        [SerializeField] private Button backButton;

        [Header("Intro Audio")]
        [SerializeField] private AudioClip voIntroAria;
        [SerializeField] private AudioClip voIntroLeo;

        [Header("Intro Visual Elements")]
        [SerializeField] private GameObject corkboardObject;
        [SerializeField] private GameObject ariaMascot;
        [SerializeField] private GameObject leoMascot;

        private bool transitionStarted;

        protected override void Awake()
        {
            topic = Masters_Topic.Intro;

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
        }

        protected override void Start()
        {
            base.Start();
            if (voIntroAria != null && Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlayVoiceOver(voIntroAria);
            }
        }

        public void ReplayAriaVoiceOver()
        {
            if (voIntroAria != null && Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlayVoiceOver(voIntroAria);
            }
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

            M3A_U7_HubProgress.MarkIntroComplete();

            if (M3A_U7_ScreenController.Instance != null)
            {
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
            if (transitionStarted)
            {
                return;
            }

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
}
