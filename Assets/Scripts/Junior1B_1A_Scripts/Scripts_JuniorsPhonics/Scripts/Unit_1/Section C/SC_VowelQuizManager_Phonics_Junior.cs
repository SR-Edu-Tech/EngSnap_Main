using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SC_VowelQuizManager_Phonics_Junior : MonoBehaviour
{
    [System.Serializable]
    public class QuizLetter
    {
        public string letter;
        public bool isVowel;
        public AudioClip sound;
    }
    private bool canAnswer = true;

    [Header("Quiz Data")]
    [SerializeField] private QuizLetter[] quizLetters;
    [SerializeField] private MascotController_Phonics_Junior mascotController;

    [Header("UI & Controls")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Replay Audio Button")]
    [SerializeField] private Button replayButton;

    [Header("Panels")]
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private GameObject completionPanel;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;

    private int currentQuestion;
    private int score;

    private void Awake()
    {
        EnsureInit();
    }

    private void OnEnable()
    {
        EnsureInit();
    }

    private void Start()
    {
        EnsureInit();
    }

    private void EnsureInit()
    {
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
        if (audioSource == null) audioSource = FindFirstObjectByType<AudioSource>();

        if (mascotController == null) mascotController = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);

        if (quizPanel == null) quizPanel = gameObject;

        if (completionPanel == null && transform.parent != null)
        {
            Transform t = transform.parent.Find("Completion Panel");
            if (t == null) t = transform.parent.Find("CompletionPanel");
            if (t != null) completionPanel = t.gameObject;
        }

        if (replayButton == null)
        {
            Transform searchRoot = transform;
            Transform t = searchRoot.Find("ReplayButton");
            if (t == null) t = searchRoot.Find("Replay Button");
            if (t == null) t = searchRoot.Find("SpeakerButton");
            if (t == null) t = searchRoot.Find("Speaker Button");
            if (t == null) t = searchRoot.Find("AudioButton");
            if (t != null) replayButton = t.GetComponent<Button>();

            if (replayButton == null)
            {
                Button[] allButtons = GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    if (b != null && (b.name.ToLower().Contains("replay") || b.name.ToLower().Contains("audio") || b.name.ToLower().Contains("speaker") || b.name.ToLower().Contains("sound")))
                    {
                        replayButton = b;
                        break;
                    }
                }
            }
        }

        if (replayButton != null)
        {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(() => ReplayLetter());
        }
    }

    public void StartQuiz()
    {
        EnsureInit();
        currentQuestion = 0;
        score = 0;
        canAnswer = true;

        ShowQuestion();
    }

    private void ShowQuestion()
    {
        EnsureInit();

        if (quizLetters == null || currentQuestion >= quizLetters.Length)
        {
            if (quizPanel != null) quizPanel.SetActive(false);
            StartCoroutine(ShowCompletion());
            return;
        }

        if (letterText != null) letterText.text = quizLetters[currentQuestion].letter;

        // Reset any draggable letter tiles back to start position for the new question!
        SC_DraggableLetter_Phonics_Juniors[] draggables = FindObjectsByType<SC_DraggableLetter_Phonics_Juniors>(FindObjectsSortMode.None);
        foreach (var d in draggables)
        {
            if (d != null) d.ResetPosition();
        }

        if (quizLetters[currentQuestion].sound != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(quizLetters[currentQuestion].sound);
        }

        if (progressText != null) progressText.text = $"{currentQuestion + 1}/{quizLetters.Length}";
        if (scoreText != null) scoreText.text = $"Score : {score}";
    }

    public void SelectVowel()
    {
        CheckAnswer(true);
    }

    public void SelectConsonant()
    {
        CheckAnswer(false);
    }

    private void CheckAnswer(bool selectedVowel)
    {
        if (!canAnswer)
            return;

        if (quizLetters == null || currentQuestion >= quizLetters.Length)
            return;

        bool correct = quizLetters[currentQuestion].isVowel == selectedVowel;

        if (correct)
        {
            canAnswer = false;
            StartCoroutine(CorrectRoutine());
        }
        else
        {
            if (mascotController != null)
            {
                mascotController.ShowMascot();
                mascotController.PlayHiAnimation();
            }

            if (audioSource != null && wrongClip != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(wrongClip);
            }

            // Reset draggable card to start position on wrong drop so student can try again!
            SC_DraggableLetter_Phonics_Juniors[] draggables = FindObjectsByType<SC_DraggableLetter_Phonics_Juniors>(FindObjectsSortMode.None);
            foreach (var d in draggables)
            {
                if (d != null) d.ResetPosition();
            }

            StopCoroutine(nameof(HideMascotAfterAudio));
            StartCoroutine(HideMascotAfterAudio(wrongClip != null ? wrongClip.length : 1f));
        }
    }

    private IEnumerator CorrectRoutine()
    {
        if (audioSource != null) audioSource.Stop();

        if (mascotController != null)
        {
            mascotController.ShowMascot();
            mascotController.PlayHiAnimation();
        }

        if (correctClip != null && audioSource != null)
        {
            audioSource.PlayOneShot(correctClip);
        }
        score++;

        float waitDuration = (correctClip != null) ? correctClip.length : 1f;
        yield return new WaitForSeconds(waitDuration);

        if (mascotController != null) mascotController.HideMascot();

        currentQuestion++;

        ShowQuestion();

        canAnswer = true;
    }

    public void ReplayLetter()
    {
        EnsureInit();

        if (quizLetters == null || quizLetters.Length == 0)
            return;

        if (currentQuestion < 0 || currentQuestion >= quizLetters.Length)
            return;

        QuizLetter q = quizLetters[currentQuestion];
        if (q == null || q.sound == null)
            return;

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(q.sound);
        }
    }

    private IEnumerator ShowCompletion()
    {
        if (mascotController != null) mascotController.HideMascot();

        if (quizPanel != null) quizPanel.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(true);

        yield return null;
    }

    private IEnumerator HideMascotAfterAudio(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (mascotController != null) mascotController.HideMascot();
    }
}