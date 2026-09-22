using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Speaking Lesson One (SP01 — Word Pronunciation & Speech Studio).
/// Focuses on speaking the 6 confused words clearly:
/// QUIET, QUITE, DIARY, DAIRY, WHOEVER, WHOMEVER.
/// Completing SP01 marks Speaking branch complete (4/6).
/// </summary>
public class Masters_ConfusedEnglishWords_Speaking_LessonOne : Masters_Lesson
{
    [Serializable]
    public class SpeakingWordItem
    {
        public string wordText;
        public string phoneticSpelling;
        public string definitionHint;
        public AudioClip modelAudio;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text wordCountTMP;
    [SerializeField] private TMP_Text targetWordTMP;
    [SerializeField] private TMP_Text phoneticTMP;
    [SerializeField] private TMP_Text definitionHintTMP;
    [SerializeField] private TMP_Text feedbackTMP;
    [SerializeField] private Button listenModelButton;
    [SerializeField] private Button recordSpeakButton;
    [SerializeField] private Slider accuracySlider;
    [SerializeField] private TMP_Text accuracyScoreTMP;
    [SerializeField] private Button backButton;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Content")]
    [SerializeField] private List<SpeakingWordItem> items = new List<SpeakingWordItem>();

    private int currentIndex = 0;
    private bool isRecording = false;

    protected override void Awake()
    {
        topic = Masters_Topic.Speaking;

        if (items == null || items.Count == 0)
        {
            InitializeDefaultItems();
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
            nextButton.gameObject.SetActive(false);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (listenModelButton != null)
        {
            listenModelButton.onClick.RemoveAllListeners();
            listenModelButton.onClick.AddListener(PlayModelAudio);
        }

        if (recordSpeakButton != null)
        {
            recordSpeakButton.onClick.RemoveAllListeners();
            recordSpeakButton.onClick.AddListener(OnRecordButtonClicked);
        }
    }

    protected override void Start()
    {
        base.Start();
        currentIndex = 0;
        LoadCurrentItem();
    }

    private void InitializeDefaultItems()
    {
        items = new List<SpeakingWordItem>
        {
            new SpeakingWordItem { wordText = "QUIET", phoneticSpelling = "/ˈkwaɪ.ət/", definitionHint = "Meaning: no noise, silent" },
            new SpeakingWordItem { wordText = "QUITE", phoneticSpelling = "/kwaɪt/", definitionHint = "Meaning: not exactly, not perfectly" },
            new SpeakingWordItem { wordText = "DIARY", phoneticSpelling = "/ˈdaɪə.ri/", definitionHint = "Meaning: personal daily record book" },
            new SpeakingWordItem { wordText = "DAIRY", phoneticSpelling = "/ˈdeə.ri/", definitionHint = "Meaning: animal milk product" },
            new SpeakingWordItem { wordText = "WHOEVER", phoneticSpelling = "/huːˈev.ər/", definitionHint = "Meaning: subject position pronoun" },
            new SpeakingWordItem { wordText = "WHOMEVER", phoneticSpelling = "/huːmˈev.ər/", definitionHint = "Meaning: object position pronoun" }
        };
    }

    private void LoadCurrentItem()
    {
        if (currentIndex >= items.Count)
        {
            OnAllItemsComplete();
            return;
        }

        isRecording = false;
        var item = items[currentIndex];

        if (wordCountTMP != null) wordCountTMP.text = $"Speech Studio: {currentIndex + 1}/{items.Count}";
        if (targetWordTMP != null) targetWordTMP.text = item.wordText;
        if (phoneticTMP != null) phoneticTMP.text = item.phoneticSpelling;
        if (definitionHintTMP != null) definitionHintTMP.text = item.definitionHint;
        if (feedbackTMP != null) feedbackTMP.text = "Tap 'Listen' to hear model pronunciation, then tap 'Speak'.";

        if (accuracySlider != null) accuracySlider.value = 0f;
        if (accuracyScoreTMP != null) accuracyScoreTMP.text = "Accuracy: --%";

        if (recordSpeakButton != null) recordSpeakButton.interactable = true;
        if (listenModelButton != null) listenModelButton.interactable = true;
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        PlayModelAudio();
    }

    private void PlayModelAudio()
    {
        if (!M3A_U7_HubProgress.IsIntroComplete())
        {
            Debug.LogWarning("[U7 AUDIO BLOCKED] Speaking_LessonOne attempted audio before Intro completion");
            return;
        }

        if (currentIndex < items.Count && items[currentIndex].modelAudio != null)
        {
            if (Masters_AudioManager.Instance != null)
            {
                Debug.Log("[U7 FLOW] SP01 model audio started");
                Masters_AudioManager.Instance.PlayVoiceOver(items[currentIndex].modelAudio);
            }
        }
    }


    private void OnRecordButtonClicked()
    {
        if (isRecording) return;
        isRecording = true;

        if (recordSpeakButton != null) recordSpeakButton.interactable = false;
        if (feedbackTMP != null) feedbackTMP.text = "Listening to your pronunciation...";

        StartCoroutine(SimulateSpeechRecognition());
    }

    private IEnumerator SimulateSpeechRecognition()
    {
        yield return new WaitForSeconds(1.2f);

        float simulatedAccuracy = UnityEngine.Random.Range(0.88f, 0.98f);
        int percentage = Mathf.RoundToInt(simulatedAccuracy * 100f);

        if (accuracySlider != null) accuracySlider.value = simulatedAccuracy;
        if (accuracyScoreTMP != null) accuracyScoreTMP.text = $"Accuracy: {percentage}%";

        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        if (feedbackTMP != null) feedbackTMP.text = $"Great pronunciation of {items[currentIndex].wordText}!";

        yield return new WaitForSeconds(1.5f);

        currentIndex++;
        if (currentIndex < items.Count)
        {
            LoadCurrentItem();
        }
        else
        {
            OnAllItemsComplete();
        }
    }

    private void OnAllItemsComplete()
    {
        if (feedbackTMP != null) feedbackTMP.text = "SP01 Pronunciation Studio Complete! Speaking Branch Solved (4/6).";
        M3A_U7_HubProgress.MarkSP01Complete();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkSP01Complete();

        if (nextLessonSO != null && Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
        else if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Speaking);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnBackClicked()
    {
        if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
        else if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
