using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FrameAnswerSystem_S3A : MonoBehaviour
{
    [System.Serializable]
    public class AnswerPair
    {
        [Header("ONE COMPLETE CORRECT ANSWER")]
        public string[] answers;
    }

    [System.Serializable]
    public class FrameData
    {
        [Header("FRAME ROOT")]
        public GameObject frame;

        [Header("SLOTS")]
        public Button[] slots;
        public TMP_Text[] slotTexts;

        [Header("OPTIONS")]
        public Button[] options;

        [Header("CORRECT ANSWERS - OLD MODE")]
        public string[] correctAnswers;

        [Header("ORDERED ANSWERS - OLD MODE")]
        public bool useOrderedAnswers;
        public string[] orderedAnswers;

        [Header("MULTIPLE CORRECT PAIRS")]
        public bool useMultipleAnswerPairs;
        public AnswerPair[] correctAnswerPairs;

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
    public Color taphereColor;
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

            // -------------------------
            // SLOT BUTTONS
            // -------------------------

            for (int j = 0; j < frames[i].slots.Length; j++)
            {
                int slotIndex = j;

                frames[i].slots[j].onClick.RemoveAllListeners();

                frames[i].slots[j].onClick.AddListener(() =>
                {
                    SelectSlot(slotIndex);
                });
            }

            // -------------------------
            // OPTION BUTTONS
            // -------------------------

            for (int j = 0; j < frames[i].options.Length; j++)
            {
                int optionIndex = j;

                frames[i].options[j].onClick.RemoveAllListeners();

                frames[i].options[j].onClick.AddListener(() =>
                {
                    SelectOption(optionIndex);
                });
            }

            // -------------------------
            // CONFIRM BUTTON
            // -------------------------

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

        LeanTween.scale(
            titleText.gameObject,
            Vector3.one,
            0.4f
        ).setEaseOutBack();

        if (introClip != null)
        {
            audioSource.PlayOneShot(introClip);

            yield return new WaitForSeconds(introClip.length);

            questionBG.gameObject.SetActive(true);

            questionBG.transform.localScale = Vector3.zero;

            LeanTween.scale(
                questionBG.gameObject,
                Vector3.one,
                0.35f
            ).setEaseOutBack();
        }

        ShowFrame(currentFrame);
    }

    void ShowFrame(int frameIndex)
    {
        FrameData frame = frames[frameIndex];

        // Create answer array according to number of slots
        currentAnswers = new string[frame.slots.Length];

        selectedSlot = -1;

        frame.frame.transform.localScale = Vector3.zero;

        LeanTween.scale(
            frame.frame,
            Vector3.one,
            0.35f
        ).setEaseOutBack();

        if (popClip != null)
        {
            audioSource.PlayOneShot(popClip);
        }

        // -------------------------
        // RESET SLOT TEXTS
        // -------------------------

        for (int i = 0; i < frame.slotTexts.Length; i++)
        {
            frame.slotTexts[i].text = "[Tap Here]";
            frame.slotTexts[i].color = taphereColor;
        }

        // -------------------------
        // RESET OPTIONS
        // -------------------------

        for (int i = 0; i < frame.options.Length; i++)
        {
            frame.options[i].interactable = true;
        }
    }

    void SelectSlot(int slotIndex)
    {
        FrameData frame = frames[currentFrame];

        if (slotIndex < 0 ||
            slotIndex >= frame.slotTexts.Length)
        {
            return;
        }

        selectedSlot = slotIndex;

        // Reset previous selection indicator
        for (int i = 0; i < frame.slots.Length; i++)
        {
            if (i < frame.slotTexts.Length &&
                frame.slotTexts[i].text == "[Select]")
            {
                frame.slotTexts[i].text = "[Tap Here]";
            }
        }

        frame.slotTexts[slotIndex].text = "[Select]";
    }

    void SelectOption(int optionIndex)
    {
        FrameData frame = frames[currentFrame];

        // No slot selected
        if (selectedSlot < 0)
            return;

        // Safety check
        if (selectedSlot >= currentAnswers.Length)
            return;

        if (optionIndex < 0 ||
            optionIndex >= frame.options.Length)
        {
            return;
        }

        if (optionIndex >= frame.options.Length)
            return;

        TMP_Text txt =
            frame.options[optionIndex]
            .GetComponentInChildren<TMP_Text>();

        if (txt == null)
            return;

        string value = txt.text;

        // -------------------------
        // PLACE ANSWER
        // -------------------------

        frame.slotTexts[selectedSlot].text = value;

        frame.slotTexts[selectedSlot].color = taphereColor;

        currentAnswers[selectedSlot] = value;

        // -------------------------
        // AUDIO
        // -------------------------

        if (placeClip != null)
        {
            audioSource.PlayOneShot(placeClip);
        }

        // -------------------------
        // DISABLE USED OPTION
        // -------------------------

        frame.options[optionIndex].interactable = false;

        // -------------------------
        // POP ANIMATION
        // -------------------------

        int slotToAnimate = selectedSlot;

        LeanTween.scale(
            frame.slots[slotToAnimate].gameObject,
            Vector3.one * 1.08f,
            0.12f
        )
        .setEaseOutBack()
        .setOnComplete(() =>
        {
            LeanTween.scale(
                frame.slots[slotToAnimate].gameObject,
                Vector3.one,
                0.12f
            );
        });

        // Deselect slot
        selectedSlot = -1;
    }

    // ============================================================
    // CHECK ANSWERS
    // ============================================================

    void CheckAnswers(int frameIndex)
    {
        FrameData frame = frames[frameIndex];

        bool allCorrect = false;

        // ========================================================
        // NEW MULTIPLE ANSWER PAIR MODE
        // ========================================================

        if (frame.useMultipleAnswerPairs)
        {
            allCorrect = CheckMultipleAnswerPairs(frame);
        }

        // ========================================================
        // OLD ORDERED MODE
        // ========================================================

        else if (frame.useOrderedAnswers)
        {
            allCorrect = CheckOrderedAnswers(frame);
        }

        // ========================================================
        // OLD NORMAL MODE
        // ========================================================

        else
        {
            allCorrect = CheckNormalAnswers(frame);
        }

        // ========================================================
        // RESULT
        // ========================================================

        if (allCorrect)
        {
            StartCoroutine(NextFrameSequence());
        }
        else
        {
            StartCoroutine(ResetWrongAnswers());
        }
    }

    // ============================================================
    // MULTIPLE ANSWER PAIRS
    // ============================================================

    bool CheckMultipleAnswerPairs(FrameData frame)
    {
        // Make sure pairs exist
        if (frame.correctAnswerPairs == null ||
            frame.correctAnswerPairs.Length == 0)
        {
            Debug.LogWarning(
                "No Correct Answer Pairs configured for frame " +
                currentFrame
            );

            return false;
        }

        // --------------------------------------------------------
        // Check every possible correct pair
        // --------------------------------------------------------

        for (int pairIndex = 0;
             pairIndex < frame.correctAnswerPairs.Length;
             pairIndex++)
        {
            AnswerPair pair =
                frame.correctAnswerPairs[pairIndex];

            if (pair == null ||
                pair.answers == null)
            {
                continue;
            }

            // Number of answers must match number of slots
            if (pair.answers.Length != frame.slotTexts.Length)
            {
                continue;
            }

            bool thisPairIsCorrect = true;

            // Compare every slot
            for (int i = 0; i < frame.slotTexts.Length; i++)
            {
                if (currentAnswers[i] != pair.answers[i])
                {
                    thisPairIsCorrect = false;
                    break;
                }
            }

            // ----------------------------------------------------
            // One complete pair matched!
            // ----------------------------------------------------

            if (thisPairIsCorrect)
            {
                for (int i = 0; i < frame.slotTexts.Length; i++)
                {
                    frame.slotTexts[i].color = correctColor;
                }

                return true;
            }
        }

        // --------------------------------------------------------
        // No pair matched
        // --------------------------------------------------------

        for (int i = 0; i < frame.slotTexts.Length; i++)
        {
            frame.slotTexts[i].color = wrongColor;
        }

        return false;
    }

    // ============================================================
    // OLD ORDERED ANSWER CHECK
    // ============================================================

    bool CheckOrderedAnswers(FrameData frame)
    {
        bool allCorrect = true;

        if (frame.orderedAnswers == null)
        {
            return false;
        }

        for (int i = 0; i < frame.slotTexts.Length; i++)
        {
            bool isCorrect = false;

            if (i < frame.orderedAnswers.Length &&
                i < currentAnswers.Length &&
                currentAnswers[i] == frame.orderedAnswers[i])
            {
                isCorrect = true;
            }

            if (isCorrect)
            {
                frame.slotTexts[i].color = correctColor;
            }
            else
            {
                frame.slotTexts[i].color = wrongColor;
                allCorrect = false;
            }
        }

        return allCorrect;
    }

    // ============================================================
    // OLD NORMAL ANSWER CHECK
    // ============================================================

    bool CheckNormalAnswers(FrameData frame)
    {
        bool allCorrect = true;

        if (frame.correctAnswers == null)
        {
            return false;
        }

        for (int i = 0; i < frame.slotTexts.Length; i++)
        {
            bool isCorrect = false;

            foreach (string correct in frame.correctAnswers)
            {
                if (currentAnswers[i] == correct)
                {
                    isCorrect = true;
                    break;
                }
            }

            if (isCorrect)
            {
                frame.slotTexts[i].color = correctColor;
            }
            else
            {
                frame.slotTexts[i].color = wrongColor;
                allCorrect = false;
            }
        }

        return allCorrect;
    }

    // ============================================================
    // RESET WRONG ANSWERS
    // ============================================================

    IEnumerator ResetWrongAnswers()
    {
        yield return new WaitForSeconds(1f);

        FrameData frame = frames[currentFrame];

        // Reset slots
        for (int i = 0; i < frame.slotTexts.Length; i++)
        {
            frame.slotTexts[i].text = "[Tap Here]";
            frame.slotTexts[i].color = taphereColor;
        }

        // Reset options
        for (int i = 0; i < frame.options.Length; i++)
        {
            frame.options[i].interactable = true;
        }

        // Reset answers
        currentAnswers =
            new string[frame.slots.Length];

        selectedSlot = -1;
    }

    // ============================================================
    // NEXT FRAME
    // ============================================================

    IEnumerator NextFrameSequence()
    {
        if (finishClip != null)
        {
            audioSource.PlayOneShot(finishClip);
        }

        yield return new WaitForSeconds(0.7f);

        frames[currentFrame].frame.SetActive(false);

        currentFrame++;

        // ========================================================
        // ALL FRAMES FINISHED
        // ========================================================

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
            )
            .setEaseOutBack();

            yield break;
        }

        // ========================================================
        // SHOW NEXT FRAME
        // ========================================================

        frames[currentFrame].frame.SetActive(true);

        ShowFrame(currentFrame);
    }
}