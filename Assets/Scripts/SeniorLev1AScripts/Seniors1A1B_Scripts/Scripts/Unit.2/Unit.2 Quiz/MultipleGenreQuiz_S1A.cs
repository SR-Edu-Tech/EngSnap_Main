using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultipleGenreQuiz_S1A : MonoBehaviour
{
    public enum QuestionType
    {
        ListenAndIdentify,
        CompleteTheSentence,
        ImageQuestion,
        DialogueComplete,
        IntroductionMatch
    }

    [System.Serializable]
    public class QuizLayoutUI
    {
        public GameObject root;

        [Header("Options")]
        public Button[] optionButtons;
        public TMP_Text[] optionTexts;
        public Image[] optionBackgrounds;
        public RectTransform[] optionCards;

        [Header("Confirm")]
        public Button confirmButton;

        [Header("Type-Specific References")]
        public TMP_Text questionTextField;
        public Image questionImageDisplay;
        
        [Header("Audio Replay Button")]
        public Button audioReplayButton;
        public Image audioReplayImage;
        public TMP_Text audioReplayText;
    }

    [System.Serializable]
    public class QuizQuestion
    {
        [Header("Question Type")]
        public QuestionType questionType;

        [Header("Question")]
        [TextArea]
        public string questionText;

        [Header("Image (for ImageQuestion type)")]
        public Sprite questionImage;

        [Header("Options")]
        public string[] options;

        public int correctIndex;

        [Header("Audio")]
        public AudioClip introAudio;
        public AudioClip questionAudio;
    }

    [Header("Questions")]
    public QuizQuestion[] questions;

    [Header("UI")]
    public GameObject nextButton;

    [Header("Layouts")]
    public QuizLayoutUI listenAndIdentifyLayout;
    public QuizLayoutUI completeTheSentenceLayout;
    public QuizLayoutUI imageQuestionLayout;
    public QuizLayoutUI dialogueCompleteLayout;
    public QuizLayoutUI introductionMatchLayout;

    [Header("Audio Button Styling")]
    public Color audioPlayingColor = new Color(0.3f, 0.8f, 1f);
    public Color audioDefaultColor = Color.white;
    public string audioPlayingTextStr = "Playing";
    public string audioDefaultTextStr = "Listen";

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.8f, 0.8f, 1f);
    public Color correctColor = new Color(0.3f, 1f, 0.4f);
    public Color wrongColor = new Color(1f, 0.3f, 0.3f);

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSfx;
    public AudioClip wrongSfx;
    public AudioClip optionPopSfx;
    public AudioClip finishSfx;

    [Header("Animation")]
    public float typeSpeed = 0.03f;
    public float optionPopSpeed = 5f;
    public float questionPopSpeed = 3f;

    private int currentQuestionIndex;
    private int currentSelectedIndex = -1;
    private bool canInteract;
    private QuizLayoutUI activeLayout;
    private Coroutine audioVisualRoutine;
    private AudioSource sfxSource;

    void Awake()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
    }

    void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    void OnEnable()
    {
        ResetUI();
        StartCoroutine(StartQuiz());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource) audioSource.Stop();
    }

    // HELPERS

    QuizLayoutUI GetLayout(QuestionType type)
    {
        switch (type)
        {
            case QuestionType.ListenAndIdentify: return listenAndIdentifyLayout;
            case QuestionType.CompleteTheSentence: return completeTheSentenceLayout;
            case QuestionType.ImageQuestion: return imageQuestionLayout;
            case QuestionType.DialogueComplete: return dialogueCompleteLayout;
            case QuestionType.IntroductionMatch: return introductionMatchLayout;
            default: return listenAndIdentifyLayout;
        }
    }

    QuizLayoutUI[] AllLayouts()
    {
        return new QuizLayoutUI[]
        {
            listenAndIdentifyLayout,
            completeTheSentenceLayout,
            imageQuestionLayout,
            dialogueCompleteLayout,
            introductionMatchLayout
        };
    }

    void HideAllLayouts()
    {
        foreach (var layout in AllLayouts())
        {
            if (layout != null && layout.root != null)
                layout.root.SetActive(false);
        }
    }

    void ResetLayoutUI(QuizLayoutUI layout)
    {
        if (layout == null) return;

        for (int i = 0; i < layout.optionCards.Length; i++)
        {
            layout.optionCards[i].localScale = Vector3.zero;

            if (i < layout.optionBackgrounds.Length)
                layout.optionBackgrounds[i].color = normalColor;

            if (i < layout.optionTexts.Length)
                layout.optionTexts[i].text = "";
        }

        if (layout.confirmButton)
        {
            layout.confirmButton.gameObject.SetActive(true);
            layout.confirmButton.transform.localScale = Vector3.zero;
            layout.confirmButton.interactable = false;
        }

        if (layout.audioReplayButton)
            layout.audioReplayButton.transform.localScale = Vector3.zero;
    }

    void ResetUI()
    {
        currentQuestionIndex = 0;
        canInteract = false;
        activeLayout = null;
        currentSelectedIndex = -1;

        if (nextButton) nextButton.SetActive(false);

        HideAllLayouts();

        foreach (var layout in AllLayouts())
            ResetLayoutUI(layout);
    }

    void SetupButtons(QuizLayoutUI layout)
    {
        if (layout == null) return;

        for (int i = 0; i < layout.optionButtons.Length; i++)
        {
            int index = i;
            layout.optionButtons[i].onClick.RemoveAllListeners();
            layout.optionButtons[i].onClick.AddListener(() =>
            {
                if (!canInteract) return;

                // Remove highlight from old selection
                if (currentSelectedIndex >= 0 && currentSelectedIndex < layout.optionBackgrounds.Length)
                    layout.optionBackgrounds[currentSelectedIndex].color = normalColor;

                currentSelectedIndex = index;

                // Highlight new selection
                if (currentSelectedIndex < layout.optionBackgrounds.Length)
                    layout.optionBackgrounds[currentSelectedIndex].color = selectedColor;

                if (layout.confirmButton != null)
                    layout.confirmButton.interactable = true;
            });
        }

        if (layout.confirmButton != null)
        {
            layout.confirmButton.onClick.RemoveAllListeners();
            layout.confirmButton.onClick.AddListener(() =>
            {
                if (!canInteract || currentSelectedIndex < 0) return;
                StartCoroutine(CheckAnswer(currentSelectedIndex));
            });
        }
    }

    void SetupAudioReplayButton(QuizLayoutUI layout, QuizQuestion q)
    {
        if (layout == null || layout.audioReplayButton == null) return;

        layout.audioReplayButton.onClick.RemoveAllListeners();

        if (q.questionAudio != null)
        {
            layout.audioReplayButton.gameObject.SetActive(true);
            layout.audioReplayButton.onClick.AddListener(() =>
            {
                if (audioSource.clip == q.questionAudio && audioSource.isPlaying)
                {
                    audioSource.Stop();
                }
                else
                {
                    audioSource.clip = q.questionAudio;
                    audioSource.Play();
                }
            });
        }
        else
        {
            layout.audioReplayButton.gameObject.SetActive(false);
        }
    }

    void StartAudioVisualMonitor(QuizLayoutUI layout, QuizQuestion q)
    {
        if (audioVisualRoutine != null) StopCoroutine(audioVisualRoutine);
        if (layout != null && layout.audioReplayButton != null && q.questionAudio != null)
        {
            audioVisualRoutine = StartCoroutine(AudioVisualUpdate(layout, q));
        }
    }

    IEnumerator AudioVisualUpdate(QuizLayoutUI layout, QuizQuestion q)
    {
        while (true)
        {
            bool isPlayingQuestion = (audioSource.isPlaying && audioSource.clip == q.questionAudio);
            
            if (isPlayingQuestion)
            {
                if (layout.audioReplayImage) layout.audioReplayImage.color = audioPlayingColor;
                if (layout.audioReplayText) layout.audioReplayText.text = audioPlayingTextStr;
            }
            else
            {
                if (layout.audioReplayImage) layout.audioReplayImage.color = audioDefaultColor;
                if (layout.audioReplayText) layout.audioReplayText.text = audioDefaultTextStr;
            }
            
            yield return null;
        }
    }

    // LAYOUT ACTIVATION

    void ActivateLayout(QuizQuestion q)
    {
        HideAllLayouts();

        activeLayout = GetLayout(q.questionType);

        if (activeLayout == null || activeLayout.root == null) return;

        activeLayout.root.SetActive(true);
        ResetLayoutUI(activeLayout);

        // Type-specific setup
        switch (q.questionType)
        {
            case QuestionType.ListenAndIdentify:
                if (activeLayout.questionTextField)
                    activeLayout.questionTextField.text = "";
                break;

            case QuestionType.CompleteTheSentence:
                if (activeLayout.questionTextField)
                    activeLayout.questionTextField.text = q.questionText;
                break;

            case QuestionType.ImageQuestion:
                if (activeLayout.questionImageDisplay && q.questionImage)
                    activeLayout.questionImageDisplay.sprite = q.questionImage;

                if (activeLayout.questionTextField)
                {
                    bool hasText = !string.IsNullOrEmpty(q.questionText);
                    activeLayout.questionTextField.gameObject.SetActive(hasText);
                    
                    if (hasText)
                    {
                        activeLayout.questionTextField.text = q.questionText;
                    }
                }
                break;

            case QuestionType.DialogueComplete:
            case QuestionType.IntroductionMatch:
                break;
        }
    }

    // QUIZ FLOW

    IEnumerator StartQuiz()
    {
        yield return LoadQuestion(currentQuestionIndex);
    }

    IEnumerator LoadQuestion(int index)
    {
        canInteract = false;
        currentSelectedIndex = -1;

        QuizQuestion q = questions[index];

        ActivateLayout(q);
        SetupButtons(activeLayout);
        SetupAudioReplayButton(activeLayout, q);
        StartAudioVisualMonitor(activeLayout, q);

        if (activeLayout != null && activeLayout.root != null)
        {
            activeLayout.root.transform.localScale = Vector3.zero;
            yield return BounceIn(activeLayout.root.transform);
        }

        // Typewriter for ListenAndIdentify
        if (q.questionType == QuestionType.ListenAndIdentify && activeLayout.questionTextField)
        {
            if (!string.IsNullOrEmpty(q.questionText))
                yield return TypeQuestion(activeLayout.questionTextField, q.questionText);
        }

        SetupOptions(activeLayout, q);

        yield return ShowOptions(activeLayout);

        yield return PlayQuestionAudio(q);

        canInteract = true;
    }

    void SetupOptions(QuizLayoutUI layout, QuizQuestion q)
    {
        if (layout == null) return;

        for (int i = 0; i < layout.optionTexts.Length; i++)
        {
            if (i < q.options.Length)
            {
                layout.optionTexts[i].text = q.options[i];
                layout.optionButtons[i].gameObject.SetActive(true);
            }
            else
            {
                layout.optionButtons[i].gameObject.SetActive(false);
            }

            if (i < layout.optionBackgrounds.Length)
                layout.optionBackgrounds[i].color = normalColor;
        }
    }

    IEnumerator PlayQuestionAudio(QuizQuestion q)
    {
        if (q.introAudio)
        {
            audioSource.clip = q.introAudio;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }

        if (q.questionAudio)
        {
            audioSource.clip = q.questionAudio;
            audioSource.Play();
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
    }

    string FillBlank(string text, string word, Color color)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        string coloredWord = $"<color=#{colorHex}>{word}</color>";
        return Regex.Replace(text, @"_+", coloredWord);
    }

    IEnumerator CheckAnswer(int selectedIndex)
    {
        canInteract = false;

        QuizQuestion q = questions[currentQuestionIndex];
        QuizLayoutUI layout = activeLayout;

        if (layout == null) yield break;

        bool correct = selectedIndex == q.correctIndex;

        if (correct)
        {
            if (q.questionType == QuestionType.CompleteTheSentence && layout.questionTextField != null)
                layout.questionTextField.text = FillBlank(q.questionText, q.options[selectedIndex], correctColor);

            if (selectedIndex < layout.optionBackgrounds.Length)
                layout.optionBackgrounds[selectedIndex].color = correctColor;

            if (correctSfx)
                PlaySFX(correctSfx);

            if (selectedIndex < layout.optionCards.Length)
                yield return BounceIn(layout.optionCards[selectedIndex]);

            yield return new WaitForSeconds(1f);

            currentQuestionIndex++;

            if (currentQuestionIndex >= questions.Length)
            {
                if (finishSfx)
                    PlaySFX(finishSfx);

                if (nextButton)
                {
                    nextButton.SetActive(true);
                    StartCoroutine(PopButton(nextButton.transform));
                }
            }
            else
            {
                yield return LoadQuestion(currentQuestionIndex);
            }
        }
        else
        {
            if (q.questionType == QuestionType.CompleteTheSentence && layout.questionTextField != null)
                layout.questionTextField.text = FillBlank(q.questionText, q.options[selectedIndex], wrongColor);

            if (selectedIndex < layout.optionBackgrounds.Length)
                layout.optionBackgrounds[selectedIndex].color = wrongColor;

            if (wrongSfx)
                PlaySFX(wrongSfx);

            if (selectedIndex < layout.optionCards.Length)
                yield return Shake(layout.optionCards[selectedIndex]);

            yield return new WaitForSeconds(0.5f);

            // Reset back to allow try again
            if (q.questionType == QuestionType.CompleteTheSentence && layout.questionTextField != null)
                layout.questionTextField.text = q.questionText; // restore blank

            if (selectedIndex < layout.optionBackgrounds.Length)
                layout.optionBackgrounds[selectedIndex].color = normalColor;

            currentSelectedIndex = -1;
            if (layout.confirmButton != null)
                layout.confirmButton.interactable = false;

            canInteract = true;
        }
    }

    // ANIMATIONS

    IEnumerator BounceIn(RectTransform target)
    {
        target.localScale = Vector3.zero;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * optionPopSpeed;
            float clamped = Mathf.Clamp01(t);

            float overshoot = 1.70158f;
            float c1 = overshoot + 1f;
            float ease = 1f + c1 * Mathf.Pow(clamped - 1f, 3f) + overshoot * Mathf.Pow(clamped - 1f, 2f);

            target.localScale = Vector3.one * ease;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator BounceIn(Transform target)
    {
        target.localScale = Vector3.zero;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * questionPopSpeed;
            float clamped = Mathf.Clamp01(t);

            float overshoot = 1.70158f;
            float c1 = overshoot + 1f;
            float ease = 1f + c1 * Mathf.Pow(clamped - 1f, 3f) + overshoot * Mathf.Pow(clamped - 1f, 2f);

            target.localScale = Vector3.one * ease;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator TypeQuestion(TMP_Text textField, string text)
    {
        textField.text = text;
        textField.maxVisibleCharacters = 0;

        for (int i = 1; i <= text.Length; i++)
        {
            textField.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typeSpeed);
        }

        textField.maxVisibleCharacters = 99999;
    }

    IEnumerator ShowOptions(QuizLayoutUI layout)
    {
        if (layout == null) yield break;
        
        // Pop audio button
        if (layout.audioReplayButton != null && layout.audioReplayButton.gameObject.activeSelf)
        {
            if (optionPopSfx) PlaySFX(optionPopSfx);
            StartCoroutine(BounceIn(layout.audioReplayButton.transform));
            yield return new WaitForSeconds(0.1f);
        }

        for (int i = 0; i < layout.optionCards.Length; i++)
        {
            if (i >= layout.optionButtons.Length || !layout.optionButtons[i].gameObject.activeSelf)
                continue;

            if (optionPopSfx)
                PlaySFX(optionPopSfx);

            StartCoroutine(BounceIn(layout.optionCards[i]));

            yield return new WaitForSeconds(0.1f);
        }
        
        // Pop confirm button
        if (layout.confirmButton != null && layout.confirmButton.gameObject.activeSelf)
        {
            if (optionPopSfx) PlaySFX(optionPopSfx);
            StartCoroutine(BounceIn(layout.confirmButton.transform));
        }
    }

    IEnumerator PopButton(Transform btn)
    {
        btn.localScale = Vector3.zero;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            btn.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.15f, Mathf.Clamp01(t));
            yield return null;
        }
        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 10f;
            float smooth = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f);
            btn.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one, smooth);
            yield return null;
        }
        btn.localScale = Vector3.one;
    }

    IEnumerator Shake(RectTransform t)
    {
        Vector2 originalPos = t.anchoredPosition;

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.anchoredPosition = originalPos + new Vector2(Random.Range(-15f, 15f), 0);
            yield return null;
        }

        t.anchoredPosition = originalPos;
    }
}