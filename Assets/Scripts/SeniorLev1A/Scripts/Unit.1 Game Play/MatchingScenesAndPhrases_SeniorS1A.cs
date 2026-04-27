using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchingScenesAndPhrases_SeniorS1A : MonoBehaviour
{
    [System.Serializable]
    public class Option
    {
        public int id;
        public string text;
        public Button button;
        public Image bg;
    }

    [System.Serializable]
    public class Slot
    {
        public int correctID;
        public Button button;
        public TMP_Text textField;
        public Image bg;
    }

    [Header("Data")]
    public Option[] options;
    public Slot[] slots;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text instructionText;
    public Button nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    [Header("Animation Settings")]
    public float titleSpeed = 3f;
    public float popSpeed = 5f;
    public float slideSpeed = 5f;
    public float fadeSpeed = 4f;

    public float delayAfterTitle = 0.2f;
    public float delayBetweenOptions = 0.1f;
    public float delayBetweenSlots = 0.1f;

    public float slideDistance = 80f;
    public float titleDropDistance = 300f;

    [Header("Advanced Animation")]
    public float bounceStrength = 1.15f;
    public float elasticStrength = 1.25f;
    public float shakeAmount = 10f;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color usedColor = Color.gray;

    private Option selectedOption;
    private int correctCount = 0;
    private bool canPlay = false;

    void OnEnable()
    {
        ResetGame();
        StartCoroutine(IntroSequence());
    }

    void ResetGame()
    {
        correctCount = 0;
        selectedOption = null;
        canPlay = false;

        nextButton.gameObject.SetActive(false);

        titleText.transform.localScale = Vector3.one;
        instructionText.transform.localScale = Vector3.zero;

        foreach (var opt in options)
        {
            opt.bg.color = normalColor;
            opt.button.interactable = true;

            opt.button.onClick.RemoveAllListeners();
            opt.button.onClick.AddListener(() => SelectOption(opt));

            opt.button.transform.localScale = Vector3.zero;
        }

        foreach (var slot in slots)
        {
            slot.textField.text = "";
            slot.bg.color = normalColor;
            slot.button.interactable = true;

            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(() => OnSlotClicked(slot));

            slot.button.transform.localScale = Vector3.zero;
        }
    }

    IEnumerator IntroSequence()
    {
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        yield return StartCoroutine(TitleDropBounce(titleText.transform));
        yield return new WaitForSeconds(delayAfterTitle);

        yield return StartCoroutine(FadeScaleIn(instructionText.transform));
        yield return StartCoroutine(AnimateOptions());
        yield return StartCoroutine(AnimateSlots());

        if (introClip)
            yield return new WaitForSeconds(Mathf.Max(0, introClip.length - delayAfterTitle));

        canPlay = true;
    }

    void SelectOption(Option opt)
    {
        if (!canPlay) return;
        if (!opt.button.interactable) return;

        selectedOption = opt;

        foreach (var o in options)
            o.bg.color = (o == opt) ? selectedColor : normalColor;
    }

    void OnSlotClicked(Slot slot)
    {
        if (!canPlay) return;
        if (selectedOption == null) return;
        if (!slot.button.interactable) return;

        slot.textField.text = selectedOption.text;

        if (selectedOption.id == slot.correctID)
        {
            slot.bg.color = correctColor;
            slot.button.interactable = false;

            selectedOption.bg.color = usedColor;
            selectedOption.button.interactable = false;

            if (audioSource && correctSFX)
                audioSource.PlayOneShot(correctSFX);

            StartCoroutine(Pulse(slot.button.transform, bounceStrength));

            selectedOption = null;
            correctCount++;

            if (correctCount == slots.Length)
                nextButton.gameObject.SetActive(true);
        }
        else
        {
            slot.bg.color = wrongColor;

            if (audioSource && wrongSFX)
                audioSource.PlayOneShot(wrongSFX);

            StartCoroutine(Shake(slot.button.transform));
            StartCoroutine(ClearWrong(slot));
        }
    }

    IEnumerator ClearWrong(Slot slot)
    {
        yield return new WaitForSeconds(0.5f);

        slot.textField.text = "";
        slot.bg.color = normalColor;
    }

    // ANIMATIONS

    IEnumerator TitleDropBounce(Transform target)
    {
        Vector3 start = target.localPosition + Vector3.up * titleDropDistance;
        Vector3 end = target.localPosition;

        target.localPosition = start;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * titleSpeed;
            target.localPosition = Vector3.Lerp(start, end, t);
            yield return null;
        }

        yield return StartCoroutine(Pulse(target, bounceStrength));
    }

    IEnumerator FadeScaleIn(Transform target)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0;
        target.localScale = Vector3.one * 0.8f;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * fadeSpeed;
            cg.alpha = t;
            target.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
            yield return null;
        }
    }

    IEnumerator AnimateOptions()
    {
        for (int i = 0; i < options.Length; i++)
        {
            yield return StartCoroutine(SlideElastic(options[i].button.transform));
            yield return new WaitForSeconds(delayBetweenOptions);
        }
    }

    IEnumerator AnimateSlots()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            yield return StartCoroutine(ElasticPop(slots[i].button.transform));
            yield return new WaitForSeconds(delayBetweenSlots);
        }
    }

    IEnumerator SlideElastic(Transform target)
    {
        Vector3 start = target.localPosition + Vector3.left * slideDistance;
        Vector3 end = target.localPosition;

        target.localPosition = start;
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * slideSpeed;
            float scale = Mathf.Lerp(0, elasticStrength, t);

            target.localPosition = Vector3.Lerp(start, end, t);
            target.localScale = Vector3.one * scale;

            yield return null;
        }

        target.localScale = Vector3.one;
    }

    IEnumerator ElasticPop(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            float scale = Mathf.Lerp(0, elasticStrength, t);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
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