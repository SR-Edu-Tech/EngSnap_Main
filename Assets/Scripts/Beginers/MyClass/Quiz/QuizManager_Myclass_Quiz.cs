using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// QuizManager_Myclass_Quiz
///
/// SUPER ENERGETIC KINDERGARTEN QUIZ SYSTEM
///
/// FEATURES
/// • Dynamic question modes
/// • Cartoon style transitions
/// • Floating images
/// • Teacher voice replay pulse
/// • Typing text animation
/// • Wobbly toy-like buttons
/// • Confetti celebration
/// • Soft wrong feedback
/// • Character reactions
/// • Background transitions
/// • Completion celebration
///
/// QUESTION TYPES
/// • ImageQuestion
/// • Vocabulary
/// • ClassroomLanguage
/// • PackBag
/// • PoemLine
///
/// PERFECT FOR:
/// Preschool / Kindergarten / Nursery learning games
/// </summary>

public class QuizManager_Myclass_Quiz : MonoBehaviour
{
    //═══════════════════════════════════════
    // QUESTION TYPES
    //═══════════════════════════════════════

    public enum QuestionType
    {
        ImageQuestion,
        Vocabulary,
        ClassroomLanguage,
        PackBag,
        PoemLine
    }

    [System.Serializable]
    public class QuizQuestion
    {
        public QuestionType type;

        [TextArea]
        public string questionText;

        public AudioClip questionAudio;

        [Header("Visuals")]
        public Sprite questionImage;
        public Sprite backgroundSprite;

        [Header("Options")]
        public string[] optionLabels = new string[3];
        public int correctIndex;

        [Header("Audio")]
        public AudioClip successVoice;
        public AudioClip retryVoice;

        [Header("Effects")]
        public bool useTypingEffect = true;
        public bool useFloatingImage = true;
    }

    //═══════════════════════════════════════
    // INSPECTOR
    //═══════════════════════════════════════

    [Header("Questions")]
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    [Header("Main UI")]
    public GameObject quizPanel;

    public TextMeshProUGUI questionText;

    public Image backgroundImage;

    [Header("Question Image")]
    public GameObject imageContainer;
    public Image questionImage;

    [Header("Option Buttons")]
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    public Image[] optionBGs;

    [Header("Replay Audio")]
    public Button replayButton;

    [Header("Completion")]
    public GameObject completionPanel;
    public TextMeshProUGUI resultText;

    [Header("Character")]
    public Animator mascotAnimator;

    [Header("Particles")]
    public ParticleSystem confettiParticles;
    public ParticleSystem sparkleParticles;

    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("SFX")]
    public AudioClip popSFX;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    public AudioClip buttonTapSFX;
    public AudioClip completionSFX;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color correctColor = new Color(0.3f, 1f, 0.5f);
    public Color wrongColor = new Color(1f, 0.6f, 0.6f);

    //═══════════════════════════════════════
    // PRIVATE
    //═══════════════════════════════════════

    private int currentQuestion;
    private bool canAnswer;

    private int correctAnswers;

    //═══════════════════════════════════════
    // START
    //═══════════════════════════════════════

    void Start()
    {
        completionPanel.SetActive(false);

        LoadQuestion(0);
    }

    //═══════════════════════════════════════
    // LOAD QUESTION
    //═══════════════════════════════════════
    void OnEnable()
{
    RestartQuiz();
}
    void LoadQuestion(int index)
    {
        StopAllCoroutines();

        currentQuestion = index;

        QuizQuestion q = questions[index];

        canAnswer = false;

        ResetButtons();

        // Background
        if (q.backgroundSprite != null)
            backgroundImage.sprite = q.backgroundSprite;

        // Image
        bool showImage = q.questionImage != null;

        imageContainer.SetActive(showImage);

        if (showImage)
        {
            questionImage.sprite = q.questionImage;

            if (q.useFloatingImage)
                StartCoroutine(FloatImage(questionImage.transform));
        }

        // Question text
        questionText.text = "";

        if (q.useTypingEffect)
            StartCoroutine(TypeQuestion(q));
        else
            questionText.text = q.questionText;

        // Setup options
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(false);

            optionTexts[i].text = q.optionLabels[i];

            int captured = i;

            optionButtons[i].onClick.RemoveAllListeners();

            optionButtons[i].onClick.AddListener(() =>
            {
                OnOptionSelected(captured);
            });
        }

        StartCoroutine(QuestionFlow(q));
    }

    //═══════════════════════════════════════
    // QUESTION FLOW
    //═══════════════════════════════════════

    IEnumerator QuestionFlow(QuizQuestion q)
    {
        yield return new WaitForSeconds(0.3f);

        // Teacher reaction
        if (mascotAnimator != null)
            mascotAnimator.Play("TeacherTalk");

        // Play audio
        if (q.questionAudio != null)
        {
            voiceSource.Stop();
            voiceSource.clip = q.questionAudio;
            voiceSource.Play();

            StartCoroutine(PulseReplayButton());
        }

        yield return new WaitForSeconds(0.5f);

        // Pop buttons one by one
        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].gameObject.SetActive(true);

            optionButtons[i].transform.localScale = Vector3.zero;

            if (popSFX != null)
                sfxSource.PlayOneShot(popSFX);

            StartCoroutine(PopButton(optionButtons[i].transform));
            StartCoroutine(IdleButtonAnimation(optionButtons[i].transform));

            yield return new WaitForSeconds(0.18f);
        }

        canAnswer = true;
    }

    //═══════════════════════════════════════
    // TYPEWRITER
    //═══════════════════════════════════════

    IEnumerator TypeQuestion(QuizQuestion q)
    {
        questionText.text = "";

        foreach (char c in q.questionText)
        {
            questionText.text += c;

            yield return new WaitForSeconds(0.025f);
        }
    }

    //═══════════════════════════════════════
    // OPTION SELECTED
    //═══════════════════════════════════════

    void OnOptionSelected(int selectedIndex)
    {
        if (!canAnswer)
            return;

        canAnswer = false;

        QuizQuestion q = questions[currentQuestion];

        foreach (Button btn in optionButtons)
            btn.interactable = false;

        StartCoroutine(ButtonTapAnimation(
            optionButtons[selectedIndex].transform));

        if (buttonTapSFX != null)
            sfxSource.PlayOneShot(buttonTapSFX);

        bool correct = selectedIndex == q.correctIndex;

        if (correct)
        {
            StartCoroutine(CorrectSequence(selectedIndex));
        }
        else
        {
            StartCoroutine(WrongSequence(selectedIndex, q.correctIndex));
        }
    }

    //═══════════════════════════════════════
    // CORRECT
    //═══════════════════════════════════════

    IEnumerator CorrectSequence(int index)
    {
        correctAnswers++;

        if (mascotAnimator != null)
            mascotAnimator.Play("Happy");

        optionBGs[index].color = correctColor;

        if (correctSFX != null)
            sfxSource.PlayOneShot(correctSFX);

        if (confettiParticles != null)
            confettiParticles.Play();

        if (sparkleParticles != null)
            sparkleParticles.Play();

        yield return StartCoroutine(BounceButton(
            optionButtons[index].transform));

        yield return new WaitForSeconds(1f);

        NextQuestion();
    }

    //═══════════════════════════════════════
    // WRONG
    //═══════════════════════════════════════

    IEnumerator WrongSequence(int selected, int correct)
    {
        if (mascotAnimator != null)
            mascotAnimator.Play("Oops");

        optionBGs[selected].color = wrongColor;

        if (wrongSFX != null)
            sfxSource.PlayOneShot(wrongSFX);

        yield return StartCoroutine(ShakeButton(
            optionButtons[selected].transform));

        yield return new WaitForSeconds(0.5f);

        optionBGs[correct].color = correctColor;

        yield return new WaitForSeconds(1.2f);

        NextQuestion();
    }

    //═══════════════════════════════════════
    // NEXT QUESTION
    //═══════════════════════════════════════

    void NextQuestion()
    {
        if (currentQuestion + 1 >= questions.Count)
        {
            ShowCompletion();
        }
        else
        {
            LoadQuestion(currentQuestion + 1);
        }
    }

    //═══════════════════════════════════════
    // COMPLETION
    //═══════════════════════════════════════

    void ShowCompletion()
    {
        quizPanel.SetActive(false);

        completionPanel.SetActive(true);

        if (completionSFX != null)
            sfxSource.PlayOneShot(completionSFX);

        if (confettiParticles != null)
            confettiParticles.Play();

        if (correctAnswers == questions.Count)
        {
            resultText.text = "SUPER STAR!";
        }
        else if (correctAnswers >= 3)
        {
            resultText.text = "GREAT JOB!";
        }
        else
        {
            resultText.text = "GOOD TRY!";
        }
    }

    //═══════════════════════════════════════
    // REPLAY AUDIO
    //═══════════════════════════════════════

    public void ReplayAudio()
    {
        QuizQuestion q = questions[currentQuestion];

        if (q.questionAudio == null)
            return;

        voiceSource.Stop();
        voiceSource.clip = q.questionAudio;
        voiceSource.Play();

        StartCoroutine(PulseReplayButton());
    }

    //═══════════════════════════════════════
    // BUTTON ANIMATIONS
    //═══════════════════════════════════════

    IEnumerator PopButton(Transform t)
    {
        float elapsed = 0f;

        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;

            float p = elapsed / 0.35f;

            float scale =
                Mathf.LerpUnclamped(
                    0f,
                    1.15f,
                    Mathf.Sin(p * Mathf.PI * 0.5f));

            t.localScale = Vector3.one * scale;

            yield return null;
        }

        t.localScale = Vector3.one;
    }

    IEnumerator BounceButton(Transform t)
    {
        float elapsed = 0f;

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;

            float scale =
                1f + Mathf.Sin(elapsed * 10f) * 0.2f;

            t.localScale = Vector3.one * scale;

            yield return null;
        }

        t.localScale = Vector3.one;
    }

    IEnumerator ShakeButton(Transform t)
    {
        Vector3 original = t.localPosition;

        float elapsed = 0f;

        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;

            float x =
                Mathf.Sin(elapsed * 50f) * 15f;

            t.localPosition =
                original + new Vector3(x, 0f, 0f);

            yield return null;
        }

        t.localPosition = original;
    }

    IEnumerator ButtonTapAnimation(Transform t)
    {
        Vector3 original = t.localScale;

        t.localScale = original * 0.85f;

        yield return new WaitForSeconds(0.08f);

        t.localScale = original;
    }

    IEnumerator IdleButtonAnimation(Transform t)
    {
        Vector3 start = Vector3.one;

        while (true)
        {
            float scale =
                1f + Mathf.Sin(Time.time * 3f) * 0.03f;

            t.localScale = start * scale;

            yield return null;
        }
    }

    IEnumerator FloatImage(Transform t)
    {
        Vector3 start = t.localPosition;

        while (true)
        {
            float y =
                Mathf.Sin(Time.time * 2f) * 15f;

            t.localPosition =
                start + new Vector3(0f, y, 0f);

            yield return null;
        }
    }

    IEnumerator PulseReplayButton()
    {
        while (voiceSource.isPlaying)
        {
            float scale =
                1f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.25f;

            replayButton.transform.localScale =
                Vector3.one * scale;

            yield return null;
        }

        replayButton.transform.localScale = Vector3.one;
    }

    //═══════════════════════════════════════
    // HELPERS
    //═══════════════════════════════════════

    void ResetButtons()
    {
        foreach (Image img in optionBGs)
        {
            img.color = normalColor;
        }

        foreach (Button btn in optionButtons)
        {
            btn.interactable = true;
        }
    }


    public void RestartQuiz()
{
    StopAllCoroutines();

    currentQuestion = 0;
    correctAnswers = 0;
    canAnswer = false;

    // Stop audio
    if (voiceSource != null)
        voiceSource.Stop();

    if (sfxSource != null)
        sfxSource.Stop();

    // Reset replay button scale
    if (replayButton != null)
        replayButton.transform.localScale = Vector3.one;

    // Hide completion panel
    completionPanel.SetActive(false);

    // Show quiz panel
    quizPanel.SetActive(true);

    // Reset button visuals
    ResetButtons();

    // Load first question
    LoadQuestion(0);
}
}