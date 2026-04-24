using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen 1 — Ben (player) meets Mary.
/// Jungle background + wooden sign.
/// Mary speaks first (auto), then player picks Ben's reply.
/// </summary>
public class Screen1Controller : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────────

  

    [Header("── Speech Bubbles ──")]
    public GameObject marySpeechBubble;
    public TMP_Text   marySpeechText;
    public GameObject benSpeechBubble;
    public TMP_Text   benSpeechText;

    [Header("── Labels ──")]
    public GameObject youAreBenLabel;       // 'YOU ARE BEN' above boy

    [Header("── Choice Cards ──")]
    public GameObject choiceCardPanel;
    public Button cardA;                    // Correct
    public Button cardB;                    // Wrong
    public Button cardC;                    // Wrong
    public TMP_Text cardAText;
    public TMP_Text cardBText;
    public TMP_Text cardCText;

    [Header("── Audio ──")]
    public AudioSource audioSource;
    public AudioClip maryGreetingClip;      // 'Hi, Ben! How are you?'
    public AudioClip benReplyClip;          // Correct answer audio
    public AudioClip maryResponseClip;      // 'I am also good, thanks.'
    public AudioClip tryAgainClip;          // 'Try again! Think…'

    [Header("── Feedback ──")]
    public float shakeDuration  = 0.4f;
    public float shakeMagnitude = 10f;      // degrees for shake

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────
    private Action   onCompleteCallback;
    private int      wrongAttempts = 0;
    private bool     inputLocked   = false;

    // Animator parameter names (set these on your Animator)
    private static readonly int TalkingParam   = Animator.StringToHash("isTalking");
    private static readonly int ShakeHeadParam = Animator.StringToHash("shakeHead");

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────
    public void StartScreen(Action onComplete)
    {
        onCompleteCallback = onComplete;
        wrongAttempts      = 0;
        inputLocked        = false;

        // Reset UI
        marySpeechBubble.SetActive(false);
        benSpeechBubble.SetActive(false);
        choiceCardPanel.SetActive(false);
        youAreBenLabel.SetActive(true);

        SetCardGlow(cardA, false);
        SetCardGlow(cardB, false);
        SetCardGlow(cardC, false);

        // Wire cards
        cardA.onClick.RemoveAllListeners();
        cardB.onClick.RemoveAllListeners();
        cardC.onClick.RemoveAllListeners();
        cardA.onClick.AddListener(() => OnCardTapped(true,  cardA));
        cardB.onClick.AddListener(() => OnCardTapped(false, cardB));
        cardC.onClick.AddListener(() => OnCardTapped(false, cardC));

        cardAText.text = "Hi, Mary! I am fine, thanks. What about you?";
        cardBText.text = "Good night, Mary!";
        cardCText.text = "I am a student.";

        StartCoroutine(Step1_MarySpeak());
    }

    // ─────────────────────────────────────────────────────────────
    //  STEPS
    // ─────────────────────────────────────────────────────────────

    /// STEP 1 — Mary speaks automatically
    private IEnumerator Step1_MarySpeak()
    {
        yield return new WaitForSeconds(0.5f);

        marySpeechBubble.SetActive(true);
        marySpeechText.text = "Hi, Ben! How are you?";
        //SetTalking(maryAnimator, true);
        PlayClip(maryGreetingClip);

        // Wait for clip length (or a fallback time)
        yield return new WaitForSeconds(GetClipLength(maryGreetingClip, 2.5f));
        //SetTalking(maryAnimator, false);

        // STEP 2 — Show choice cards
        marySpeechBubble.SetActive(false);
        choiceCardPanel.SetActive(true);
    }

    /// STEP 2 — Player taps a card
    private void OnCardTapped(bool isCorrect, Button tappedCard)
    {
        if (inputLocked) return;
        inputLocked = true;

        if (isCorrect)
        {
            StartCoroutine(Step3_CorrectAnswer());
        }
        else
        {
            wrongAttempts++;
            StartCoroutine(Step3_WrongAnswer(tappedCard));
        }
    }

    /// STEP 3 — Correct path
    private IEnumerator Step3_CorrectAnswer()
    {
        choiceCardPanel.SetActive(false);

        // Ben speaks
        benSpeechBubble.SetActive(true);
        benSpeechText.text = "Hi, Mary! I am fine, thanks. What about you?";
        //SetTalking(benAnimator, true);
        PlayClip(benReplyClip);

        yield return new WaitForSeconds(GetClipLength(benReplyClip, 3.0f));
        //SetTalking(benAnimator, false);
        benSpeechBubble.SetActive(false);

        // Mary responds
        yield return new WaitForSeconds(0.4f);
        marySpeechBubble.SetActive(true);
        marySpeechText.text = "I am also good, thanks.";
       // SetTalking(maryAnimator, true);
        PlayClip(maryResponseClip);

        yield return new WaitForSeconds(GetClipLength(maryResponseClip, 2.5f));
        //SetTalking(maryAnimator, false);
        marySpeechBubble.SetActive(false);

        yield return new WaitForSeconds(0.3f);
        onCompleteCallback?.Invoke();
    }

    /// STEP 3 — Wrong path
    private IEnumerator Step3_WrongAnswer(Button tappedCard)
    {
        // Shake the card
        yield return StartCoroutine(ShakeCard(tappedCard));

        // Ben shakes head
       // benAnimator.SetTrigger(ShakeHeadParam);

        // Play try-again audio
        PlayClip(tryAgainClip);
        yield return new WaitForSeconds(GetClipLength(tryAgainClip, 3.0f));

        // Hint glow after 2 wrong attempts
        if (wrongAttempts >= 2)
        {
            SetCardGlow(cardA, true);   // cardA is always correct
        }

        inputLocked = false;    // re-enable input
    }

    // ─────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────
    private void SetTalking(Animator anim, bool state)
    {
        if (anim != null) anim.SetBool(TalkingParam, state);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private float GetClipLength(AudioClip clip, float fallback)
        => (clip != null) ? clip.length : fallback;

    private void SetCardGlow(Button card, bool glow)
    {
        // Assumes the card has an Outline or a child "GlowImage" you toggle
        // Adjust to match your UI setup
        var outline = card.GetComponent<Outline>();
        if (outline != null) outline.enabled = glow;
    }

    private IEnumerator ShakeCard(Button card)
    {
        RectTransform rt     = card.GetComponent<RectTransform>();
        Quaternion    origin = rt.localRotation;
        float         elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float angle = Mathf.Sin(elapsed / shakeDuration * Mathf.PI * 8f) * shakeMagnitude;
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rt.localRotation = origin;
    }
}
