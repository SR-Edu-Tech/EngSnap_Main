using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RolePlayWithFriend_S1A : MonoBehaviour
{
    [System.Serializable]
    public class OptionData
    {
        public int id;
        public string text;
    }

    [System.Serializable]
    public class DialogueStep
    {
        public string girlText;
        public int correctOptionID;
        public OptionData[] options;
    }

    [System.Serializable]
    public class OptionUI
    {
        public Button button;
        public TMP_Text label;
        public GameObject root;
    }

    [Header("Dialogue")]
    public DialogueStep[] steps;

    [Header("Option UI")]
    public OptionUI[] optionUIs;

    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text girlDialogueText;
    public TMP_Text boyDialogueText;
    public Button nextButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;

    [Header("Animation Settings")]
    public float titleSpeed = 3f;
    public float popSpeed = 5f;
    public float optionSpeed = 5f;
    public float pulseSpeed = 8f;

    public float delayAfterTitle = 0.2f;
    public float delayBetweenOptions = 0.1f;
    public float delayAfterCorrect = 0.4f;
    public float delayAfterWrong = 0.4f;

    public float optionSlideDistance = 50f;
    public float titleDropDistance = 300f;

    private int currentStep = 0;
    private bool canPlay = false;
    private bool isProcessing = false;

    void OnEnable()
    {
        ResetGame();
        StartCoroutine(IntroSequence());
    }

    void ResetGame()
    {
        currentStep = 0;
        canPlay = false;
        isProcessing = false;

        nextButton.gameObject.SetActive(false);
        boyDialogueText.text = "";

        titleText.transform.localScale = Vector3.one;
        girlDialogueText.transform.localScale = Vector3.zero;

        foreach (var ui in optionUIs)
        {
            ui.root.transform.localScale = Vector3.zero;
        }

        SetupStep();
    }

    IEnumerator IntroSequence()
    {
        if (audioSource && introClip)
        {
            audioSource.clip = introClip;
            audioSource.Play();
        }

        yield return StartCoroutine(TitleBounce(titleText.transform));
        yield return new WaitForSeconds(delayAfterTitle);

        yield return StartCoroutine(PopIn(girlDialogueText.transform));
        yield return StartCoroutine(AnimateOptions());

        if (introClip)
            yield return new WaitForSeconds(Mathf.Max(0, introClip.length - delayAfterTitle));

        canPlay = true;
    }

    void SetupStep()
    {
        var step = steps[currentStep];
        girlDialogueText.text = step.girlText;

        for (int i = 0; i < optionUIs.Length; i++)
        {
            if (i < step.options.Length)
            {
                var data = step.options[i];
                var ui = optionUIs[i];

                ui.root.SetActive(true);
                ui.label.text = data.text;
                ui.label.color = Color.black;
                ui.button.interactable = true;

                int id = data.id;
                string text = data.text;
                OptionUI capturedUI = ui;

                ui.button.onClick.RemoveAllListeners();
                ui.button.onClick.AddListener(() =>
                    OnOptionClicked(id, text, capturedUI)
                );

                ui.root.transform.localScale = Vector3.zero;
            }
            else
            {
                optionUIs[i].root.SetActive(false);
            }
        }
    }

    void OnOptionClicked(int id, string text, OptionUI ui)
    {
        if (!canPlay || isProcessing) return;
        StartCoroutine(HandleSelection(id, text, ui));
    }

    IEnumerator HandleSelection(int id, string text, OptionUI ui)
    {
        isProcessing = true;

        boyDialogueText.text = text;
        ui.label.color = Color.yellow;

        yield return new WaitForSeconds(0.2f);

        if (id == steps[currentStep].correctOptionID)
        {
            ui.label.color = Color.green;

            if (audioSource && correctSFX)
                audioSource.PlayOneShot(correctSFX);

            yield return StartCoroutine(Pulse(ui.root.transform, 1.2f));

            yield return new WaitForSeconds(delayAfterCorrect);

            currentStep++;

            if (currentStep >= steps.Length)
            {
                nextButton.gameObject.SetActive(true);
            }
            else
            {
                boyDialogueText.text = "";

                yield return StartCoroutine(FadeOutOptions());
                SetupStep();

                yield return StartCoroutine(PopIn(girlDialogueText.transform));
                yield return StartCoroutine(AnimateOptions());
            }
        }
        else
        {
            ui.label.color = Color.red;

            if (audioSource && wrongSFX)
                audioSource.PlayOneShot(wrongSFX);

            yield return StartCoroutine(Shake(ui.root.transform));

            yield return new WaitForSeconds(delayAfterWrong);

            boyDialogueText.text = "";
            ui.label.color = Color.black;
        }

        isProcessing = false;
    }

    // ANIMATIONS

    IEnumerator TitleBounce(Transform target)
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

        yield return StartCoroutine(Pulse(target, 1.1f));
    }

    IEnumerator PopIn(Transform target)
    {
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * popSpeed;
            float scale = Mathf.Lerp(0, 1.1f, t);
            target.localScale = Vector3.one * scale;
            yield return null;
        }

        target.localScale = Vector3.one;
    }

    IEnumerator AnimateOptions()
    {
        for (int i = 0; i < optionUIs.Length; i++)
        {
            yield return StartCoroutine(SlidePop(optionUIs[i].root.transform));
            yield return new WaitForSeconds(delayBetweenOptions);
        }
    }

    IEnumerator SlidePop(Transform target)
    {
        Vector3 start = target.localPosition + Vector3.down * optionSlideDistance;
        Vector3 end = target.localPosition;

        target.localPosition = start;
        target.localScale = Vector3.zero;

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * optionSpeed;
            target.localPosition = Vector3.Lerp(start, end, t);
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
            t += Time.deltaTime * pulseSpeed;
            target.localScale = Vector3.Lerp(original, Vector3.one * scale, t);
            yield return null;
        }

        t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * pulseSpeed;
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

    IEnumerator FadeOutOptions()
    {
        foreach (var ui in optionUIs)
        {
            ui.root.transform.localScale = Vector3.zero;
        }
        yield return new WaitForSeconds(0.2f);
    }
}