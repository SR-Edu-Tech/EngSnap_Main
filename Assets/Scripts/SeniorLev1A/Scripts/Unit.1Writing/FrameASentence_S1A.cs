using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FrameASentence_S1A : MonoBehaviour
{
    [Header("UI References")]
    public Transform titleContainer;
    public Transform descriptionContainer;
    public Transform sentenceContainer;
    public Transform wordsContainer;
    public Transform resetContainer;

    public TMP_Text sentenceText;
    public Button[] wordButtons;

    public GameObject nextButton;
    public GameObject resetButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    [Header("Words")]
    public string[] words;
    public string[] correctOrder;

    [Header("Animation Settings")]
    public float titleDropHeight = 300f;
    public float titleSpeed = 3f;
    public float popSpeed = 5f;
    public float bounceScale = 1.15f;
    public float staggerDelay = 0.08f;

    private List<string> selectedWords = new List<string>();
    private bool isCompleted = false;
    private bool canPlay = false;

    // -----------------------------
    void Awake()
    {
        titleContainer.localScale = Vector3.zero;
        descriptionContainer.localScale = Vector3.zero;
        sentenceContainer.localScale = Vector3.zero;
        wordsContainer.localScale = Vector3.zero;
        resetContainer.localScale = Vector3.zero;
    }

    void OnEnable()
    {
        ResetGame();
        StartCoroutine(IntroFlow());
    }

    // -----------------------------
    void ResetGame()
    {
        selectedWords.Clear();
        isCompleted = false;
        canPlay = false;

        nextButton.SetActive(false);
        resetButton.SetActive(true);

        SetupButtons();
        UpdateSentence();
    }

    // -----------------------------
    IEnumerator IntroFlow()
    {
        // Play intro
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        // TITLE
        yield return StartCoroutine(TitleDrop());

        yield return new WaitForSeconds(0.2f);

        // DESCRIPTION
        yield return StartCoroutine(PopIn(descriptionContainer));

        // SENTENCE BOARD
        yield return StartCoroutine(PopIn(sentenceContainer));

        // WORDS (staggered bounce)
        yield return StartCoroutine(AnimateWords());

        // RESET BUTTON
        yield return StartCoroutine(PopIn(resetContainer));

        // Wait audio end
        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        canPlay = true;
    }

    // -----------------------------
    void SetupButtons()
    {
        for (int i = 0; i < wordButtons.Length; i++)
        {
            int index = i;

            wordButtons[i].GetComponentInChildren<TMP_Text>().text = words[i];

            wordButtons[i].onClick.RemoveAllListeners();
            wordButtons[i].onClick.AddListener(() =>
            {
                if (!canPlay) return;
                OnWordClicked(words[index], wordButtons[index]);
            });

            wordButtons[i].interactable = true;
        }
    }

    // -----------------------------
    void OnWordClicked(string word, Button btn)
    {
        if (!canPlay || isCompleted) return;

        selectedWords.Add(word);
        btn.interactable = false;

        UpdateSentence();

        if (selectedWords.Count >= correctOrder.Length)
        {
            isCompleted = true;

            resetButton.SetActive(false);

            ShowResults();
        }
    }

    // -----------------------------
    public void OnResetClicked()
    {
        if (!canPlay || isCompleted) return;

        selectedWords.Clear();

        foreach (var btn in wordButtons)
            btn.interactable = true;

        UpdateSentence();
    }

    // -----------------------------
    void UpdateSentence()
    {
        string display = "";

        for (int i = 0; i < selectedWords.Count; i++)
        {
            string word = selectedWords[i];

            if (!isCompleted)
            {
                display += $"<color=#000000>{word}</color> ";
            }
            else
            {
                bool isCorrect = word == correctOrder[i];
                string color = isCorrect ? "#2E7D32" : "#FF0000";

                display += $"<color={color}>{word}</color> ";
            }
        }

        int remaining = correctOrder.Length - selectedWords.Count;
        for (int i = 0; i < remaining; i++)
        {
            display += "<color=#888888>_____</color> ";
        }

        sentenceText.text = display.Trim();
    }

    // -----------------------------
    void ShowResults()
    {
        UpdateSentence();
        nextButton.SetActive(true);
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

    IEnumerator AnimateWords()
    {
        wordsContainer.localScale = Vector3.one;

        foreach (Transform child in wordsContainer)
        {
            child.localScale = Vector3.zero;
        }

        foreach (Transform child in wordsContainer)
        {
            yield return StartCoroutine(BounceIn(child));
            yield return new WaitForSeconds(staggerDelay);
        }
    }

    IEnumerator BounceIn(Transform target)
    {
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