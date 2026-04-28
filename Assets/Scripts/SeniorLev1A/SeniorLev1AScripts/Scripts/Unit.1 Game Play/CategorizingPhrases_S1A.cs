using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategorizingPhrases_S1A : MonoBehaviour
{
    [System.Serializable]
    public class PhraseData
    {
        public string text;
        public int correctCategory;
    }

    [System.Serializable]
    public class Pot
    {
        public int id;
        public RectTransform dropArea;

        public Image bg;
        public Color normalColor = Color.white;
        public Color highlightColor = Color.yellow;
    }

    [System.Serializable]
    public class DraggableItem
    {
        public RectTransform rect;
        public TMP_Text text;
        public DraggableItems_S1A dragHandler;

        [HideInInspector] public PhraseData data;
        [HideInInspector] public Vector3 startPos;
        [HideInInspector] public bool active;
    }

    [Header("Data")]
    public PhraseData[] allPhrases;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text instructionText;
    public DraggableItem[] draggableItems;
    public Pot[] pots;
    public Button nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    [Header("Animation")]
    public float titleSpeed = 3f;
    public float popSpeed = 5f;
    public float shakeAmount = 10f;
    public float returnSpeed = 6f;

    private Queue<PhraseData> phraseQueue;
    private bool canPlay = false;
    private Canvas canvas;

    public bool CanPlay()
    {
        return canPlay;
    }

    // -----------------------
    // FIX: PRE-HIDE EVERYTHING
    // -----------------------
    void Awake()
    {
        titleText.transform.localScale = Vector3.zero;
        instructionText.transform.localScale = Vector3.zero;

        foreach (var pot in pots)
            pot.dropArea.localScale = Vector3.zero;

        foreach (var item in draggableItems)
            item.rect.localScale = Vector3.zero;
    }

    void OnEnable()
    {
        SetupGame();
        StartCoroutine(IntroSequence());
    }

    void SetupGame()
    {
        canvas = GetComponentInParent<Canvas>();

        nextButton.gameObject.SetActive(false);
        canPlay = false;

        phraseQueue = new Queue<PhraseData>(allPhrases);

        titleText.transform.localScale = Vector3.zero;

        foreach (var item in draggableItems)
        {
            item.startPos = item.rect.localPosition;
            item.dragHandler.Setup(this, item);
            LoadIntoItem(item);

            item.rect.localScale = Vector3.zero;
        }

        foreach (var pot in pots)
        {
            pot.dropArea.localScale = Vector3.zero;
        }

        instructionText.transform.localScale = Vector3.zero;

        ResetAllPotHighlights();
    }

    void LoadIntoItem(DraggableItem item)
    {
        if (phraseQueue.Count > 0)
        {
            item.data = phraseQueue.Dequeue();
            item.text.text = item.data.text;
            item.rect.localPosition = item.startPos;
            item.rect.gameObject.SetActive(true);
            item.active = true;
        }
        else
        {
            item.rect.gameObject.SetActive(false);
            item.active = false;
            CheckCompletion();
        }
    }

    void CheckCompletion()
    {
        foreach (var item in draggableItems)
        {
            if (item.active) return;
        }

        nextButton.gameObject.SetActive(true);
    }

    // -----------------------
    // INTRO
    // -----------------------
    IEnumerator IntroSequence()
    {
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        yield return StartCoroutine(TitleDrop());
        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(PopIn(instructionText.transform));

        // Pots
        foreach (var pot in pots)
            yield return StartCoroutine(PopIn(pot.dropArea));

        // Options
        foreach (var item in draggableItems)
            yield return StartCoroutine(PopIn(item.rect));

        if (introClip)
            yield return new WaitForSeconds(introClip.length - 0.2f);

        canPlay = true;
    }

    // -----------------------
    // HOVER
    // -----------------------
    public void HandleDragHover(Vector2 screenPos)
    {
        Camera cam = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        foreach (var pot in pots)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(pot.dropArea, screenPos, cam))
            {
                HighlightPot(pot);
                return;
            }
        }

        ResetAllPotHighlights();
    }

    void HighlightPot(Pot target)
    {
        foreach (var pot in pots)
        {
            if (pot.bg != null)
            {
                pot.bg.color = (pot == target) ? pot.highlightColor : pot.normalColor;

                if (pot.dropArea.localScale != Vector3.zero)
                    pot.dropArea.localScale = (pot == target) ? Vector3.one * 1.1f : Vector3.one;
            }
        }
    }

    public void ResetAllPotHighlights()
    {
        foreach (var pot in pots)
        {
            if (pot.bg != null)
            {
                pot.bg.color = pot.normalColor;

                if (pot.dropArea.localScale != Vector3.zero)
                    pot.dropArea.localScale = Vector3.one;
            }
        }
    }

    // -----------------------
    // DROP
    // -----------------------
    public void HandleDrop(DraggableItem item, Vector2 screenPos)
    {
        if (!canPlay || !item.active) return;

        Camera cam = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;

        foreach (var pot in pots)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(pot.dropArea, screenPos, cam))
            {
                if (pot.id == item.data.correctCategory)
                {
                    StartCoroutine(HandleCorrect(item));
                    return;
                }
                else
                {
                    StartCoroutine(HandleWrong(item));
                    return;
                }
            }
        }

        StartCoroutine(ReturnToStart(item));
    }

    IEnumerator HandleCorrect(DraggableItem item)
    {
        if (audioSource && correctSFX)
            audioSource.PlayOneShot(correctSFX);

        yield return StartCoroutine(Pulse(item.rect, 1.2f));
        yield return new WaitForSeconds(0.2f);

        LoadIntoItem(item);
    }

    IEnumerator HandleWrong(DraggableItem item)
    {
        if (audioSource && wrongSFX)
            audioSource.PlayOneShot(wrongSFX);

        yield return StartCoroutine(Shake(item.rect));
        yield return StartCoroutine(ReturnToStart(item));
    }

    IEnumerator ReturnToStart(DraggableItem item)
    {
        Vector3 start = item.rect.localPosition;
        Vector3 end = item.startPos;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * returnSpeed;
            item.rect.localPosition = Vector3.Lerp(start, end, t);
            yield return null;
        }
    }

    // -----------------------
    // ANIMATION
    // -----------------------
    IEnumerator TitleDrop()
    {
        Vector3 start = titleText.transform.localPosition + Vector3.up * 300f;
        Vector3 end = titleText.transform.localPosition;

        titleText.transform.localPosition = start;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * titleSpeed;
            titleText.transform.localPosition = Vector3.Lerp(start, end, t);
            yield return null;
        }

        yield return StartCoroutine(Pulse(titleText.transform, 1.1f));
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

    IEnumerator Pulse(Transform target, float scale)
    {
        Vector3 original = Vector3.one;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(original, Vector3.one * scale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            target.localScale = Vector3.Lerp(Vector3.one * scale, original, t);
            yield return null;
        }
    }

    IEnumerator Shake(Transform target)
    {
        Vector3 original = target.localPosition;

        for (int i = 0; i < 10; i++)
        {
            target.localPosition = original + new Vector3(Random.Range(-shakeAmount, shakeAmount), 0, 0);
            yield return new WaitForSeconds(0.02f);
        }

        target.localPosition = original;
    }
}