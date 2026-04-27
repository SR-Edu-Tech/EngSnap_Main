using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizSystem_S1A : MonoBehaviour
{
    [System.Serializable]
    public class QuestionData
    {
        public string question;
        public string[] options;
        public int correctIndex;
    }

    [Header("Questions")]
    public QuestionData[] questions;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text questionText;
    public TMP_Text optionsTitleText; //  NEW
    public Button[] optionButtons;
    public TMP_Text[] optionTexts;
    public Button nextButton;

    [Header("Containers")]
    public RectTransform questionPanel;
    public RectTransform optionsParent;

    [Header("Colors")]
    public Color normalColor = Color.black;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    private int currentQuestion = 0;
    private bool canPlay = false;
    private bool isProcessing = false;

    // ---------------------------
    void OnEnable()
    {
        ResetQuiz();
        StartCoroutine(IntroSequence());
    }

    void ResetQuiz()
    {
        currentQuestion = 0;
        canPlay = false;
        isProcessing = false;

        nextButton.gameObject.SetActive(false);

        titleText.transform.localScale = Vector3.one;
        questionPanel.localScale = Vector3.zero;
        optionsParent.localScale = Vector3.one;

        optionsTitleText.transform.localScale = Vector3.zero; //  NEW

        LoadQuestion();
    }

    // ---------------------------
    IEnumerator IntroSequence()
    {
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        yield return StartCoroutine(TitleBounce(titleText.transform));
        yield return StartCoroutine(PopIn(questionPanel));
        yield return StartCoroutine(PopIn(optionsTitleText.transform)); //  NEW
        yield return StartCoroutine(AnimateOptions());

        if (introClip)
            yield return new WaitForSeconds(Mathf.Max(0, introClip.length - 0.5f));

        canPlay = true;
    }

    // ---------------------------
    void LoadQuestion()
    {
        var q = questions[currentQuestion];

        questionText.text = q.question;

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].interactable = true;
            optionTexts[i].text = q.options[i];
            optionTexts[i].color = normalColor;

            int index = i;

            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));

            optionButtons[i].transform.localScale = Vector3.zero;
        }
    }

    // ---------------------------
    void OnOptionSelected(int index)
    {
        if (!canPlay || isProcessing) return;

        StartCoroutine(HandleAnswer(index));
    }

    // ---------------------------
    IEnumerator HandleAnswer(int index)
    {
        isProcessing = true;

        var q = questions[currentQuestion];

        if (index == q.correctIndex)
        {
            optionTexts[index].color = correctColor;

            if (audioSource && correctSFX)
                audioSource.PlayOneShot(correctSFX);

            yield return StartCoroutine(Pulse(optionButtons[index].transform, 1.2f, 0.1f));

            currentQuestion++;

            if (currentQuestion >= questions.Length)
            {
                nextButton.gameObject.SetActive(true);
            }
            else
            {
                yield return StartCoroutine(PopOut(questionPanel));

                LoadQuestion();

                yield return StartCoroutine(PopIn(questionPanel));
                yield return StartCoroutine(PopIn(optionsTitleText.transform)); //  NEW
                yield return StartCoroutine(AnimateOptions());
            }
        }
        else
        {
            optionTexts[index].color = wrongColor;

            if (audioSource && wrongSFX)
                audioSource.PlayOneShot(wrongSFX);

            yield return StartCoroutine(Shake(optionButtons[index].transform));

            optionTexts[index].color = normalColor;
        }

        isProcessing = false;
    }

    // ---------------------------
    // ANIMATIONS
    // ---------------------------

    IEnumerator TitleBounce(Transform target)
    {
        Vector3 start = target.localPosition + Vector3.up * 300f;
        Vector3 end = target.localPosition;

        target.localPosition = start;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 3f;
            target.localPosition = Vector3.Lerp(start, end, t);
            yield return null;
        }

        yield return StartCoroutine(Pulse(target, 1.1f, 0.15f));
    }

    IEnumerator PopIn(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            float scale = Mathf.Lerp(0, 1.1f, t);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    IEnumerator PopOut(Transform target)
    {
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            target.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            yield return null;
        }
    }

    IEnumerator AnimateOptions()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            yield return StartCoroutine(SlidePop(optionButtons[i].transform));
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator SlidePop(Transform target)
    {
        Vector3 start = target.localPosition + Vector3.down * 50f;
        Vector3 end = target.localPosition;

        target.localPosition = start;
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            target.localPosition = Vector3.Lerp(start, end, t);
            target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }
    }

    IEnumerator Pulse(Transform target, float scale, float time)
    {
        Vector3 original = Vector3.one;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / time;
            target.localScale = Vector3.Lerp(original, Vector3.one * scale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / time;
            target.localScale = Vector3.Lerp(Vector3.one * scale, original, t);
            yield return null;
        }
    }

    IEnumerator Shake(Transform target)
    {
        Vector3 original = target.localPosition;

        for (int i = 0; i < 10; i++)
        {
            target.localPosition = original + new Vector3(Random.Range(-10, 10), 0, 0);
            yield return new WaitForSeconds(0.02f);
        }

        target.localPosition = original;
    }
}