using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardScreen_S3A : MonoBehaviour
{
    [System.Serializable]
    public class RewardStar
    {
        public RectTransform starRect;


        [TextArea]
        public string rewardMessage;

        [HideInInspector]
        public Vector3 originalScale;
    }

    [Header("Title")]
    public TMP_Text titleText;

    [Header("Board")]
    public RectTransform rewardBoard;

    [Header("Stars")]
    public TMP_Text dynamicStarText; // The single text object used for all stars
    public RewardStar[] stars;

    [Header("Completion")]
    public TMP_Text completedText;

    public Button continueButton;
    public GameObject backButton;
    [SerializeField] GameObject nextButton;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip popSfx;
    public AudioClip starPopSfx; // Different sound for stars

    [Header("Animation")]
    public float titleTypeSpeed = 0.08f;

    public float boardPopSpeed = 2f;

    public float starPopSpeed = 2f;

    public float textTypeSpeed = 0.06f;

    public float delayBetweenStars = 0.5f;

    public AnimationCurve popCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.7f, 1.15f), new Keyframe(1f, 1f));

    void OnEnable()
    {
        if (nextButton != null)
        nextButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        ResetUI();

        StartCoroutine(MainFlow());
    }

    void OnDisable()
    {
        StopAllCoroutines();

        if (audioSource)
            audioSource.Stop();

        if (backButton != null)
            backButton.SetActive(true);

        if (nextButton != null)
        nextButton.SetActive(true);
    }

    void ResetUI()
    {
        titleText.maxVisibleCharacters = 0;
        titleText.ForceMeshUpdate();

        rewardBoard.localScale = Vector3.zero;

        completedText.gameObject.SetActive(false);
        completedText.maxVisibleCharacters = 0;

        continueButton.transform.localScale = Vector3.zero;

        dynamicStarText.text = "";

        foreach (var star in stars)
        {
            star.originalScale = star.starRect.localScale;
            star.starRect.localScale = Vector3.zero;
        }
    }

    IEnumerator MainFlow()
    {
        // Title and Board animate simultaneously
        Coroutine titleRoutine = StartCoroutine(TypeWriterTMP(titleText, titleText.text, titleTypeSpeed));
        Coroutine boardRoutine = StartCoroutine(PopUI(rewardBoard, boardPopSpeed, Vector3.one, popSfx));
        
        yield return titleRoutine;
        yield return boardRoutine;

        // Stars pop up one by one with their text simultaneously
        for (int i = 0; i < stars.Length; i++)
        {
            dynamicStarText.gameObject.SetActive(true);

            Coroutine starPopRoutine = StartCoroutine(PopUI(stars[i].starRect, starPopSpeed, stars[i].originalScale, starPopSfx));
            Coroutine textRoutine = StartCoroutine(TypeWriterTMP(dynamicStarText, stars[i].rewardMessage, textTypeSpeed));

            yield return starPopRoutine;
            yield return textRoutine;

            yield return new WaitForSeconds(delayBetweenStars);
        }

        // The text disappears
        dynamicStarText.text = "";
        dynamicStarText.gameObject.SetActive(false);

        // Completed text pops in with typewriter
        completedText.gameObject.SetActive(true);
        yield return StartCoroutine(TypeWriterTMP(
            completedText,
            completedText.text,
            titleTypeSpeed
        ));

        // Continue button pops
        continueButton.gameObject.SetActive(true); // Ensures it is active before popping
        yield return StartCoroutine(
            PopUI(
                continueButton.transform as RectTransform,
                starPopSpeed,
                Vector3.one,
                popSfx
            )
        );
    }

    IEnumerator PopUI(RectTransform t, float speed, Vector3 targetScale, AudioClip soundToPlay)
    {
        if (soundToPlay != null)
            audioSource.PlayOneShot(soundToPlay);

        float time = 0f;

        while (time < 1f)
        {
            time += Time.deltaTime * speed;

            float curveValue = popCurve.Evaluate(time);
            t.localScale = targetScale * curveValue;

            yield return null;
        }

        t.localScale = targetScale;
    }

    IEnumerator TypeWriterTMP(
        TMP_Text tmp,
        string fullText,
        float speed
    )
    {
        if (tmp == null) yield break;

        tmp.text = fullText;
        tmp.ForceMeshUpdate();

        int totalCharacters = tmp.textInfo.characterCount;
        tmp.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalCharacters; i++)
        {
            tmp.maxVisibleCharacters = i;
            yield return new WaitForSeconds(speed);
        }
    }
}
