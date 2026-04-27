using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FillInTheBlanks_S1A : MonoBehaviour
{
    [Header("UI References")]
    public Transform titleContainer;
    public Transform dialogueContainer;
    public Transform optionsContainer;
    public Transform retryContainer;

    public TMP_Text dialogueText;
    public Button[] optionButtons;
    public GameObject nextButton;
    public GameObject retryButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    [Header("Dialogue Settings")]
    [TextArea(5, 10)]
    public string templateText =
        "Teacher: '<b><color=#888888><u>[1]</u></color></b>! How are you?'\n" +
        "Student: 'I'm good Sir. How are you?'\n" +
        "Teacher: '<b><color=#888888><u>[2]</u></color></b>, Thank you.'\n" +
        "Student: 'Wish you a <b><color=#888888><u>[3]</u></color></b>!'";

    [Header("Options")]
    public string[] options;

    [Header("Correct Answers")]
    public string[] correctAnswers;

    [Header("Animation Settings")]
    public float titleDropHeight = 300f;
    public float titleSpeed = 3f;
    public float popSpeed = 5f;
    public float bounceScale = 1.15f;

    private string[] answers;
    private int currentIndex = 0;
    private bool isCompleted = false;
    private bool canPlay = false;

    // -----------------------------
    void Awake()
    {
        // Hide all UI first
        titleContainer.localScale = Vector3.zero;
        dialogueContainer.localScale = Vector3.zero;
        optionsContainer.localScale = Vector3.zero;
        retryContainer.localScale = Vector3.zero;
    }

    void OnEnable()
    {
        ResetGame();
        StartCoroutine(IntroFlow());
    }

    // -----------------------------
    void ResetGame()
    {
        answers = new string[3];
        currentIndex = 0;
        isCompleted = false;
        canPlay = false;

        nextButton.SetActive(false);
        retryButton.SetActive(true);

        UpdateText();
        SetupButtons();
    }

    // -----------------------------
    IEnumerator IntroFlow()
    {
        // Play intro audio
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        // 1. TITLE
        yield return StartCoroutine(TitleDrop());

        yield return new WaitForSeconds(0.2f);

        // 2. DIALOGUE BOARD
        yield return StartCoroutine(PopIn(dialogueContainer));

        // 3. OPTIONS (bounce)
        yield return StartCoroutine(BounceIn(optionsContainer));

        // 4. RETRY BUTTON
        yield return StartCoroutine(PopIn(retryContainer));

        // Wait for audio finish
        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        canPlay = true;
    }

    // -----------------------------
    void SetupButtons()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;

            optionButtons[i].GetComponentInChildren<TMP_Text>().text = options[i];

            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() =>
            {
                if (!canPlay) return;
                OnOptionSelected(options[index], optionButtons[index]);
            });

            optionButtons[i].interactable = true;
        }
    }

    // -----------------------------
    public void OnOptionSelected(string value, Button btn)
    {
        if (!canPlay || currentIndex >= answers.Length || isCompleted) return;

        answers[currentIndex] = value;
        currentIndex++;

        btn.interactable = false;

        UpdateText();

        if (currentIndex >= answers.Length)
        {
            isCompleted = true;

            retryButton.SetActive(false);

            ShowResults();
        }
    }

    // -----------------------------
    public void OnRetryClicked()
    {
        if (!canPlay || isCompleted) return;

        for (int i = 0; i < answers.Length; i++)
            answers[i] = "";

        currentIndex = 0;

        foreach (var btn in optionButtons)
            btn.interactable = true;

        UpdateText();
    }

    // -----------------------------
    void ShowResults()
    {
        UpdateText();
        nextButton.SetActive(true);
    }

    // -----------------------------
    void UpdateText()
    {
        string updated = templateText;

        for (int i = 0; i < answers.Length; i++)
        {
            string placeholder = $"<b><color=#888888><u>[{i + 1}]</u></color></b>";

            if (!isCompleted && i == currentIndex && string.IsNullOrEmpty(answers[i]))
            {
                updated = updated.Replace(
                    placeholder,
                    $"<b><color=#00BFFF><u>________</u></color></b>"
                );
            }
            else if (isCompleted && !string.IsNullOrEmpty(answers[i]))
            {
                bool isCorrect = answers[i] == correctAnswers[i];
                string color = isCorrect ? "#2E7D32" : "#FF0000";

                updated = updated.Replace(
                    placeholder,
                    $"<b><color={color}>{answers[i]}</color></b>"
                );
            }
            else if (!string.IsNullOrEmpty(answers[i]))
            {
                updated = updated.Replace(
                    placeholder,
                    $"<b><color=#000000>{answers[i]}</color></b>"
                );
            }
            else
            {
                updated = updated.Replace(
                    placeholder,
                    $"<b><color=#888888><u>________</u></color></b>"
                );
            }
        }

        dialogueText.text = updated;
    }

    // -----------------------------
    // ANIMATIONS
    // -----------------------------

    IEnumerator TitleDrop()
    {
        Vector3 start = titleContainer.localPosition + Vector3.up * titleDropHeight;
        Vector3 end = titleContainer.localPosition;

        titleContainer.localPosition = start;
        titleContainer.localScale = Vector3.one;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * titleSpeed;
            titleContainer.localPosition = Vector3.Lerp(start, end, t);
            yield return null;
        }

        yield return StartCoroutine(Pulse(titleContainer));
    }

    IEnumerator PopIn(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }
    }

    IEnumerator BounceIn(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * bounceScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(Vector3.one * bounceScale, Vector3.one, t);
            yield return null;
        }
    }

    IEnumerator Pulse(Transform target)
    {
        Vector3 original = Vector3.one;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(original, Vector3.one * bounceScale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(Vector3.one * bounceScale, original, t);
            yield return null;
        }
    }
}