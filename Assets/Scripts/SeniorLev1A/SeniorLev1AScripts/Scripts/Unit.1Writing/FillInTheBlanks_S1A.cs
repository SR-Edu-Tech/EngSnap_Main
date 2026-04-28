using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FillInTheBlanks_S1A : MonoBehaviour
{
    [System.Serializable]
    public class BlankSlot
    {
        public Button button;
        public TMP_Text text;
        public string correctAnswer;

        [HideInInspector] public bool filled;
        [HideInInspector] public string currentValue;
    }

    [System.Serializable]
    public class OptionItem
    {
        public Button button;
        public TMP_Text text;
        public string value;

        [HideInInspector] public bool used;
    }

    [Header("UI")]
    public BlankSlot[] slots;
    public OptionItem[] options;
    public GameObject nextButton;
    public Button resetButton;

    [Header("Containers")]
    public RectTransform title;
    public RectTransform board;
    public RectTransform optionsContainer;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    [Header("Colors")]
    public Color normalColor = Color.black;
    public Color correctColor = new Color(0.18f, 0.55f, 0.34f);
    public Color wrongColor = Color.red;
    public Color selectedSlotColor = new Color(1f, 0.7f, 0.2f);
    public Color usedOptionColor = Color.gray;

    private BlankSlot currentSlot;
    private bool canInteract = false;
    private bool hasCompletedOnce = false;

    void OnEnable()
    {
        ResetUI();
        Setup();
        StartCoroutine(IntroFlow());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (audioSource) audioSource.Stop();
    }

    void ResetUI()
    {
        currentSlot = null;
        canInteract = false;

        if (!hasCompletedOnce)
            nextButton.SetActive(false);

        foreach (var s in slots)
        {
            s.filled = false;
            s.currentValue = "";
            s.text.text = "[Tap To Select]";
            s.text.color = normalColor;
            s.button.interactable = true;
        }

        foreach (var o in options)
        {
            o.used = false;
            o.button.interactable = true;
            o.text.color = Color.black;
        }

        title.localScale = Vector3.zero;
        board.localScale = Vector3.zero;
        optionsContainer.localScale = Vector3.zero;

        foreach (Transform t in optionsContainer)
            t.localScale = Vector3.zero;
    }

    void Setup()
    {
        foreach (var s in slots)
        {
            BlankSlot captured = s;
            s.button.onClick.RemoveAllListeners();
            s.button.onClick.AddListener(() => OnSlotSelected(captured));
        }

        foreach (var o in options)
        {
            OptionItem captured = o;
            o.button.onClick.RemoveAllListeners();
            o.button.onClick.AddListener(() => OnOptionSelected(captured));
        }

        resetButton.onClick.RemoveAllListeners();
        resetButton.onClick.AddListener(OnReset);
    }

    IEnumerator IntroFlow()
    {
        if (introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        StartCoroutine(AnimateTitle());
        yield return new WaitForSeconds(0.3f);

        StartCoroutine(AnimateBoard());
        yield return new WaitForSeconds(0.2f);

        StartCoroutine(AnimateOptions());

        if (introClip)
            yield return new WaitWhile(() => audioSource.isPlaying);

        canInteract = true;
    }

    void OnSlotSelected(BlankSlot slot)
    {
        if (!canInteract) return;

        currentSlot = slot;

        foreach (var s in slots)
        {
            if (!s.filled)
            {
                s.text.color = normalColor;

                if (s != slot)
                    s.text.text = "[Tap To Select]";
            }
        }

        slot.text.color = selectedSlotColor;

        if (!slot.filled)
            slot.text.text = "[Select an option]";
    }

    void OnOptionSelected(OptionItem option)
    {
        if (!canInteract || currentSlot == null) return;
        if (currentSlot.filled) return;
        if (option.used) return;

        currentSlot.currentValue = option.value;
        currentSlot.text.text = option.value;
        currentSlot.filled = true;

        bool correct = option.value == currentSlot.correctAnswer;
        currentSlot.text.color = correct ? correctColor : wrongColor;

        option.used = true;
        option.button.interactable = false;
        option.text.color = usedOptionColor;

        currentSlot = null;

        CheckCompletion();
    }

    void CheckCompletion()
    {
        foreach (var s in slots)
        {
            if (!s.filled)
                return;
        }

        nextButton.SetActive(true);
        hasCompletedOnce = true;
    }

    void OnReset()
    {
        currentSlot = null;

        foreach (var s in slots)
        {
            s.filled = false;
            s.currentValue = "";
            s.text.text = "[Tap To Select]";
            s.text.color = normalColor;
        }

        foreach (var o in options)
        {
            o.used = false;
            o.button.interactable = true;
            o.text.color = normalColor;
        }
    }

    IEnumerator AnimateTitle()
    {
        yield return ScaleIn(title, 4f);
    }

    IEnumerator AnimateBoard()
    {
        yield return ScaleIn(board, 3f);
    }

    IEnumerator AnimateOptions()
    {
        optionsContainer.localScale = Vector3.one;

        foreach (Transform t in optionsContainer)
        {
            yield return ScaleIn(t, 5f);
            yield return new WaitForSeconds(0.08f);
        }
    }

    IEnumerator ScaleIn(Transform t, float speed)
    {
        t.localScale = Vector3.zero;

        float time = 0;
        while (time < 1)
        {
            time += Time.deltaTime * speed;
            t.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time);
            yield return null;
        }

        t.localScale = Vector3.one;
    }
}