using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel 1 - starts ACTIVE inside unitGameObject.
/// OnEnable resets to Q0 every time it becomes active (first play + replays).
/// Next button (shown after all questions done) disables this panel
/// and enables the DragDrop sibling panel.
/// </summary>
public class ImageOptionGameController : MonoBehaviour
{
    [Header("Questions")]
    public ImageOptionQuestion[] questions;

    [Header("UI")]
    public Image questionImage;
    public Button[] optionButtons;
    public TMP_Text[] optionTexts;

    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Navigation")]
    [Tooltip("Shown after all questions are answered.")]
    public Button nextButton;
    [Tooltip("The DragDrop sibling panel — inactive by default in the hierarchy.")]
    public GameObject nextGamePanel;

    [Header("Colors")]
    public Color correctColor = Color.green;
    public Color wrongColor   = Color.red;
    public Color normalColor  = Color.white;

    [Header("Animation Settings")]
    public float popDuration   = 0.25f;
    public float popScale      = 1.2f;
    public float shakeDuration = 0.3f;
    public float shakeStrength = 10f;

    private int  currentIndex  = 0;
    private bool canAnswer     = false;
    private bool listenersWired = false;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Wire Next button in Awake so it is ready before the first OnEnable
        if (!listenersWired)
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextPressed);
            listenersWired = true;
        }
    }

    void OnEnable()
    {
        // Resets and starts fresh every time this panel is shown
        ResetAndStart();
    }

    // ─────────────────────────────────────────────────────────────────────

    void ResetAndStart()
    {
        StopAllCoroutines();

        canAnswer    = false;
        currentIndex = 0;

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        SetOptionsVisible(false);

        if (questions != null && questions.Length > 0)
            LoadQuestion(0);
    }

    // ─────────────────────────────────────────────────────────────────────

    void LoadQuestion(int index)
    {
        currentIndex = index;
        var q = questions[index];

        questionImage.sprite = q.image;
        questionImage.transform.localScale = Vector3.zero;
        StartCoroutine(PopIn(questionImage.transform));

        SetOptionsVisible(false);

        for (int i = 0; i < optionTexts.Length; i++)
        {
            optionTexts[i].text  = q.options[i];
            optionTexts[i].color = normalColor;
        }

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int captured = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(captured));
        }

        StartCoroutine(PlayQuestionAndEnable(q.questionAudio));
    }

    IEnumerator PlayQuestionAndEnable(AudioClip clip)
    {
        canAnswer = false;

        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
            yield return new WaitForSeconds(clip.length);
        }

        ShowButtonsWithPop();
        SetOptionsInteractable(true);
        canAnswer = true;
    }

    void ShowButtonsWithPop()
    {
        foreach (var btn in optionButtons)
        {
            btn.gameObject.SetActive(true);
            btn.transform.localScale = Vector3.zero;
            StartCoroutine(PopIn(btn.transform));
        }
    }

    void SetOptionsVisible(bool value)
    {
        foreach (var btn in optionButtons)
            btn.gameObject.SetActive(value);
    }

    void SetOptionsInteractable(bool value)
    {
        foreach (var btn in optionButtons)
            btn.interactable = value;
    }

    // ─────────────────────────────────────────────────────────────────────

    void OnOptionSelected(int index)
    {
        if (!canAnswer) return;

        canAnswer = false;
        SetOptionsInteractable(false);

        var q = questions[currentIndex];

        if (index == q.correctIndex)
            StartCoroutine(HandleCorrect(index, q));
        else
            StartCoroutine(HandleWrong(index, q));
    }

    IEnumerator HandleCorrect(int index, ImageOptionQuestion q)
    {
        optionTexts[index].color = correctColor;
        yield return StartCoroutine(PunchScale(optionButtons[index].transform));

        if (q.correctAudio != null)
        {
            audioSource.clip = q.correctAudio;
            audioSource.Play();
            yield return new WaitForSeconds(q.correctAudio.length);
        }

        yield return new WaitForSeconds(0.3f);
        GoNext();
    }

    IEnumerator HandleWrong(int index, ImageOptionQuestion q)
    {
        optionTexts[index].color = wrongColor;
        yield return StartCoroutine(Shake(optionButtons[index].transform));

        if (q.wrongAudio != null)
        {
            audioSource.clip = q.wrongAudio;
            audioSource.Play();
            yield return new WaitForSeconds(q.wrongAudio.length);
        }

        yield return new WaitForSeconds(0.2f);
        optionTexts[index].color = normalColor;

        canAnswer = true;
        SetOptionsInteractable(true);
    }

    void GoNext()
    {
        int next = currentIndex + 1;

        if (next < questions.Length)
        {
            LoadQuestion(next);
        }
        else
        {
            // All questions done — hide answer buttons, show Next button
            SetOptionsVisible(false);
            if (nextButton != null)
                nextButton.gameObject.SetActive(true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────

    void OnNextPressed()
    {
        nextButton.gameObject.SetActive(false);

        // Disable this panel, enable DragDrop sibling
        // DragDropPanel.OnEnable will fire and reset it automatically
        gameObject.SetActive(false);

        if (nextGamePanel != null)
            nextGamePanel.SetActive(true);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Animations
    // ─────────────────────────────────────────────────────────────────────

    IEnumerator PopIn(Transform target)
    {
        float time = 0f;
        while (time < popDuration)
        {
            target.localScale = Vector3.one * Mathf.Lerp(0f, popScale, time / popDuration);
            time += Time.deltaTime;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator PunchScale(Transform target)
    {
        float time = 0f;
        while (time < popDuration)
        {
            target.localScale = Vector3.one * Mathf.Lerp(1f, popScale, time / popDuration);
            time += Time.deltaTime;
            yield return null;
        }
        time = 0f;
        while (time < popDuration)
        {
            target.localScale = Vector3.one * Mathf.Lerp(popScale, 1f, time / popDuration);
            time += Time.deltaTime;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    IEnumerator Shake(Transform target)
    {
        Vector3 origin = target.localPosition;
        float time = 0f;
        while (time < shakeDuration)
        {
            target.localPosition = origin + new Vector3(Random.Range(-1f, 1f) * shakeStrength, 0, 0);
            time += Time.deltaTime;
            yield return null;
        }
        target.localPosition = origin;
    }
}