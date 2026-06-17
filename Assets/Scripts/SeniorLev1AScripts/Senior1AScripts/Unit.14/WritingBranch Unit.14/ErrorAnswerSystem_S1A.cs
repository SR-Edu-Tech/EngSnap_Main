using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ErrorAnswerSystem_S1A : MonoBehaviour
{
    [System.Serializable]
    public class FrameData
    {
        [Header("FRAME ROOT")]
        public GameObject frame;

        [Header("SLOTS")]
        public Button[] slots;

        public TMP_Text[] slotTexts;

        [Header("DEFAULT WORDS")]
        public string[] defaultWords;

        [Header("OPTIONS")]
        public Button[] options;

        [Header("CORRECT ANSWERS")]
        public string[] correctAnswers;

        [Header("CONFIRM")]
        public Button confirmButton;
    }

    [Header("UI")]
    public TMP_Text titleText;

    public FrameData[] frames;

    public GameObject questionBG;

    public Button nextButton;

    [Header("Result")]
    public GameObject resultPanel;

    public TMP_Text resultText;

    public Button retryButton;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip introClip;

    public AudioClip popClip;

    public AudioClip placeClip;

    public AudioClip finishClip;

    [Header("Colors")]
    public Color correctColor;

    public Color wrongColor;

    [Header("Animation")]
    public float popSpeed = 5f;

    private int currentFrame = 0;

    private int selectedSlot = -1;

    private string[] currentAnswers;

    void Start()
    {
        nextButton.gameObject.SetActive(false);

        resultPanel.SetActive(false);

        SetupFrames();

        StartCoroutine(IntroSequence());
    }

    void SetupFrames()
    {
        for (int i = 0; i < frames.Length; i++)
        {
            int frameIndex = i;

            frames[i].frame.SetActive(i == 0);

            // SLOT BUTTONS
            for (int j = 0; j < frames[i].slots.Length; j++)
            {
                int slotIndex = j;

                frames[i].slots[j].onClick.RemoveAllListeners();

                frames[i].slots[j].onClick.AddListener(() =>
                {
                    SelectSlot(slotIndex);
                });
            }

            // OPTION BUTTONS
            for (int j = 0; j < frames[i].options.Length; j++)
            {
                int optionIndex = j;

                frames[i].options[j].onClick.RemoveAllListeners();

                frames[i].options[j].onClick.AddListener(() =>
                {
                    SelectOption(optionIndex);
                });
            }

            // CONFIRM
            frames[i].confirmButton.onClick.RemoveAllListeners();

            frames[i].confirmButton.onClick.AddListener(() =>
            {
                CheckAnswers(frameIndex);
            });
        }
    }

    IEnumerator IntroSequence()
    {
        titleText.transform.localScale = Vector3.zero;

        LeanTween.scale(titleText.gameObject, Vector3.one, 0.4f)
            .setEaseOutBack();

        if (introClip != null)
        {
            audioSource.PlayOneShot(introClip);

            yield return new WaitForSeconds(introClip.length);

            questionBG.gameObject.SetActive(true);

            questionBG.transform.localScale = Vector3.zero;

            LeanTween.scale(questionBG.gameObject, Vector3.one, 0.35f)
                .setEaseOutBack();
        }

        ShowFrame(currentFrame);
    }

    void ShowFrame(int frameIndex)
    {
        FrameData frame = frames[frameIndex];

        currentAnswers = new string[frame.slots.Length];

        selectedSlot = -1;

        frame.frame.transform.localScale = Vector3.zero;

        LeanTween.scale(frame.frame, Vector3.one, 0.35f)
            .setEaseOutBack();

        if (popClip != null)
        {
            audioSource.PlayOneShot(popClip);
        }

        // RESET TO ORIGINAL WORDS
        for (int i = 0; i < frame.slotTexts.Length; i++)
        {
            frame.slotTexts[i].text =
                frame.defaultWords[i];

            frame.slotTexts[i].color = Color.black;
        }
    }

    void SelectSlot(int slotIndex)
    {
        FrameData frame = frames[currentFrame];

        if (slotIndex >= frame.slotTexts.Length)
            return;

        selectedSlot = slotIndex;

        // SLOT POP
        LeanTween.cancel(frame.slots[slotIndex].gameObject);

        frame.slots[slotIndex].transform.localScale =
            Vector3.one;

        LeanTween.scale(
            frame.slots[slotIndex].gameObject,
            Vector3.one * 1.08f,
            0.12f
        ).setEaseOutBack()
        .setOnComplete(() =>
        {
            LeanTween.scale(
                frame.slots[slotIndex].gameObject,
                Vector3.one,
                0.12f
            );
        });
    }

    void SelectOption(int optionIndex)
    {
        FrameData frame = frames[currentFrame];

        if (selectedSlot < 0)
            return;

        if (selectedSlot >= currentAnswers.Length)
            return;

        if (optionIndex >= frame.options.Length)
            return;

        TMP_Text txt =
            frame.options[optionIndex]
            .GetComponentInChildren<TMP_Text>();

        if (txt == null)
            return;

        string value = txt.text;

        // PLACE TEXT
        frame.slotTexts[selectedSlot].text = value;

        currentAnswers[selectedSlot] = value;

        // AUDIO
        if (placeClip != null)
        {
            audioSource.PlayOneShot(placeClip);
        }

        frame.options[optionIndex].interactable = false;

        // POP ANIM
        int slotToAnimate = selectedSlot;

        LeanTween.scale(
            frame.slots[slotToAnimate].gameObject,
            Vector3.one * 1.08f,
            0.12f
        ).setEaseOutBack()
        .setOnComplete(() =>
        {
            LeanTween.scale(
                frame.slots[slotToAnimate].gameObject,
                Vector3.one,
                0.12f
            );
        });

        selectedSlot = -1;
    }

    void CheckAnswers(int frameIndex)
    {
        FrameData frame = frames[frameIndex];

        bool allCorrect = true;

        for (int i = 0; i < frame.correctAnswers.Length; i++)
        {
            if (i >= currentAnswers.Length)
                continue;

            // CORRECT
            if (currentAnswers[i] ==
                frame.correctAnswers[i])
            {
                frame.slotTexts[i].color =
                    correctColor;
            }
            // WRONG
            else
            {
                frame.slotTexts[i].color =
                    wrongColor;

                allCorrect = false;
            }
        }

        if (allCorrect)
        {
            StartCoroutine(NextFrameSequence());
        }
        else
        {
            StartCoroutine(ResetWrongAnswers());
        }
    }

    IEnumerator ResetWrongAnswers()
    {
        yield return new WaitForSeconds(1f);

        FrameData frame = frames[currentFrame];

        // RESET SLOT TEXTS
        for (int i = 0; i < frame.slotTexts.Length; i++)
        {
            frame.slotTexts[i].text =
                frame.defaultWords[i];

            frame.slotTexts[i].color =
                Color.black;
        }

        // RESET OPTIONS
        for (int i = 0; i < frame.options.Length; i++)
        {
            frame.options[i].interactable = true;
        }

        currentAnswers =
            new string[frame.slots.Length];

        selectedSlot = -1;
    }

    IEnumerator NextFrameSequence()
    {
        if (finishClip != null)
        {
            audioSource.PlayOneShot(finishClip);
        }

        yield return new WaitForSeconds(0.7f);

        frames[currentFrame].frame.SetActive(false);

        currentFrame++;

        // ALL DONE
        if (currentFrame >= frames.Length)
        {
            foreach (FrameData frame in frames)
            {
                frame.frame.SetActive(false);
            }

            questionBG.SetActive(false);

            resultPanel.SetActive(true);

            resultText.text = "Perfect!";

            nextButton.gameObject.SetActive(true);

            nextButton.transform.localScale = Vector3.zero;

            LeanTween.scale(
                nextButton.gameObject,
                Vector3.one,
                0.3f
            ).setEaseOutBack();

            yield break;
        }

        frames[currentFrame].frame.SetActive(true);

        ShowFrame(currentFrame);
    }
}
