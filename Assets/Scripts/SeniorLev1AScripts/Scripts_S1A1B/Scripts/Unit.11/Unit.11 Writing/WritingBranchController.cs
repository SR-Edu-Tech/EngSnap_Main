using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WritingBranchController : MonoBehaviour
{
    [System.Serializable]
    public class QuestionData
    {
        [Header("Question Object")]
        public GameObject questionObject;

        [Header("Input Fields")]
        public TMP_InputField[] inputFields;

        [Header("Question Audio")]
        public AudioClip questionAudio;
    }

    [Header("TOP UI")]
    public TMP_Text titleText;

    [Header("QUESTION BG")]
    public RectTransform questionBG;

    [Header("QUESTIONS")]
    public QuestionData[] questions;

    [Header("BUTTONS")]
    public Button nextButton;

    [Header("AUDIO")]
    public AudioSource audioSource;

    public AudioClip introAudio;

    public AudioClip completionAudio;

    [Header("ANIMATION")]
    public float popScale = 1.05f;

    private int currentQuestionIndex = 0;

    private bool questionCompleted = false;

    void Start()
    {
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        // INITIAL
        nextButton.gameObject.SetActive(false);

        // DISABLE ALL QUESTIONS
        for (int i = 0; i < questions.Length; i++)
        {
            questions[i].questionObject.SetActive(false);
        }

        // TITLE ANIM
        titleText.transform.localScale = Vector3.zero;

        LeanTween.scale(titleText.gameObject,
            Vector3.one,
            0.4f).setEaseOutBack();

        // INTRO AUDIO
        if (introAudio != null)
    {
        audioSource.clip = introAudio;

        audioSource.Play();

        yield return new WaitForSeconds(
            introAudio.length);
    }

        // PANEL SLIDE
        Vector2 originalPos =
            questionBG.anchoredPosition;

        questionBG.anchoredPosition =
            new Vector2(0, -1200);

        LeanTween.move(questionBG,
            originalPos,
            0.5f).setEaseOutCubic();

        yield return new WaitForSeconds(0.7f);

        // START FIRST QUESTION
        LoadQuestion(0);
    }

    void LoadQuestion(int index)
    {
        currentQuestionIndex = index;

        questionCompleted = false;

        // DISABLE ALL
        for (int i = 0; i < questions.Length; i++)
        {
            questions[i].questionObject.SetActive(false);
        }

        // CURRENT
        QuestionData current =
            questions[index];

        current.questionObject.SetActive(true);

        // POP ANIM
        current.questionObject.transform.localScale =
            Vector3.one * 0.9f;

        LeanTween.scale(current.questionObject,
            Vector3.one,
            0.3f).setEaseOutBack();

        // AUDIO
        if (current.questionAudio != null)
        {
            audioSource.PlayOneShot(
                current.questionAudio);
        }

        // INPUT SETUP
        foreach (TMP_InputField input
            in current.inputFields)
        {
            input.text = "";

            input.gameObject.SetActive(true);

            // PLACEHOLDER
            if (input.placeholder != null)
            {
                TMP_Text placeholder =
                    input.placeholder.GetComponent<TMP_Text>();

                if (placeholder != null)
                {
                    placeholder.text = "[Tap Here]";
                }
            }

            // CLICK ANIM
            input.onSelect.RemoveAllListeners();

            input.onSelect.AddListener((value) =>
            {
                LeanTween.scale(input.gameObject,
                    Vector3.one * popScale,
                    0.15f).setEasePunch();
            });

            // CHECK COMPLETE
            input.onEndEdit.RemoveAllListeners();

            input.onEndEdit.AddListener((value) =>
            {
                CheckQuestionComplete();
            });
        }
    }

    void CheckQuestionComplete()
    {
        if (questionCompleted)
            return;

        QuestionData current =
            questions[currentQuestionIndex];

        bool completed = true;

        foreach (TMP_InputField input
            in current.inputFields)
        {
            if (string.IsNullOrWhiteSpace(
                input.text))
            {
                completed = false;

                break;
            }
        }

        if (completed)
        {
            questionCompleted = true;

            StartCoroutine(NextQuestionRoutine());
        }
    }

    IEnumerator NextQuestionRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        // NEXT QUESTION
        if (currentQuestionIndex + 1 < questions.Length)
        {
            LoadQuestion(currentQuestionIndex + 1);
        }
        else
        {
            StartCoroutine(CompleteSequence());
        }
    }

    IEnumerator CompleteSequence()
    {
        // COMPLETION AUDIO
        if (completionAudio != null)
        {
            audioSource.PlayOneShot(
                completionAudio);

            yield return new WaitForSeconds(
                completionAudio.length);
        }

        // NEXT BUTTON
        nextButton.gameObject.SetActive(true);

        nextButton.transform.localScale =
            Vector3.zero;

        LeanTween.scale(nextButton.gameObject,
            Vector3.one,
            0.35f).setEaseOutBack();
    }
}