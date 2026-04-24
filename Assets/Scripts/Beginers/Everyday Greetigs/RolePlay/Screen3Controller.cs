using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen 3 — Ted (player) talks with Dad in the morning.
/// Dad speaks first (auto), Ted replies, then Take It Home card appears.
/// </summary>
public class Screen3Controller : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────────

  //  [Header("── Characters ──")]
   // public Animator tedAnimator;
   // public Animator dadAnimator;

    [Header("── Speech Bubbles ──")]
    public GameObject tedSpeechBubble;
    public TMP_Text   tedSpeechText;
    public GameObject dadSpeechBubble;
    public TMP_Text   dadSpeechText;

    [Header("── Choice Cards ──")]
    public GameObject choiceCardPanel;
    public Button cardA;
    public Button cardB;
    public Button cardC;
    public TMP_Text cardAText;
    public TMP_Text cardBText;
    public TMP_Text cardCText;

    [Header("── Take It Home Card ──")]
    public GameObject takeItHomeCard;       // shown after screen completes

    [Header("── Audio ──")]
    public AudioSource audioSource;
    public AudioClip   dadGreetingClip;     // 'Good morning, Ted! How are you today?'
    public AudioClip   tedReplyClip;        // Correct answer audio
    public AudioClip   dadResponseClip;     // 'Oh! I am great too, thanks.'
    public AudioClip   tryAgainClip;        // Generic try-again prompt

    [Header("── Feedback ──")]
    public float shakeDuration  = 0.4f;
    public float shakeMagnitude = 10f;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────
    private Action onCompleteCallback;
    private int    wrongAttempts = 0;
    private bool   inputLocked   = false;

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

        tedSpeechBubble.SetActive(false);
        dadSpeechBubble.SetActive(false);
        choiceCardPanel.SetActive(false);
        takeItHomeCard.SetActive(false);

        SetCardGlow(cardA, false);
        SetCardGlow(cardB, false);
        SetCardGlow(cardC, false);

        cardAText.text = "Good morning, dad! I am doing great, thanks. What about you, dad?";
        cardBText.text = "Hi.";
        cardCText.text = "Good evening, dad.";

        cardA.onClick.RemoveAllListeners();
        cardB.onClick.RemoveAllListeners();
        cardC.onClick.RemoveAllListeners();
        cardA.onClick.AddListener(() => OnCardTapped(true,  cardA));
        cardB.onClick.AddListener(() => OnCardTapped(false, cardB));
        cardC.onClick.AddListener(() => OnCardTapped(false, cardC));

        StartCoroutine(Step1_DadSpeak());
    }

    // ─────────────────────────────────────────────────────────────
    //  STEPS
    // ─────────────────────────────────────────────────────────────

    /// STEP 1 — Dad speaks automatically
    private IEnumerator Step1_DadSpeak()
    {
        yield return new WaitForSeconds(0.5f);

        dadSpeechBubble.SetActive(true);
        dadSpeechText.text = "Good morning, Ted! How are you today?";
        //SetTalking(dadAnimator, true);
        PlayClip(dadGreetingClip);

        yield return new WaitForSeconds(GetClipLength(dadGreetingClip, 2.5f));
       // SetTalking(dadAnimator, false);
        dadSpeechBubble.SetActive(false);

        // STEP 2 — Show cards
        choiceCardPanel.SetActive(true);
        inputLocked = false;
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

        // Ted speaks
        tedSpeechBubble.SetActive(true);
        tedSpeechText.text = "Good morning, dad! I am doing great, thanks. What about you, dad?";
        //SetTalking(tedAnimator, true);
        PlayClip(tedReplyClip);
        yield return new WaitForSeconds(GetClipLength(tedReplyClip, 3.5f));
        //SetTalking(tedAnimator, false);
        tedSpeechBubble.SetActive(false);

        // Dad responds
        yield return new WaitForSeconds(0.4f);
        dadSpeechBubble.SetActive(true);
        dadSpeechText.text = "Oh! I am great too, thanks.";
        //SetTalking(dadAnimator, true);
        PlayClip(dadResponseClip);
        yield return new WaitForSeconds(GetClipLength(dadResponseClip, 2.0f));
        //SetTalking(dadAnimator, false);
        dadSpeechBubble.SetActive(false);

        yield return new WaitForSeconds(0.3f);

        // Show Take It Home card BEFORE Well Done banner
        takeItHomeCard.SetActive(true);

        yield return new WaitForSeconds(1.0f);   // let player read it briefly

        // Signal completion → manager shows WELL DONE + NEXT
        onCompleteCallback?.Invoke();
    }

    /// STEP 3 — Wrong path
    private IEnumerator Step3_WrongAnswer(Button tappedCard)
    {
        yield return StartCoroutine(ShakeCard(tappedCard));

        //tedAnimator.SetTrigger(ShakeHeadParam);
        PlayClip(tryAgainClip);
        yield return new WaitForSeconds(GetClipLength(tryAgainClip, 2.5f));

        // Hint after 2 wrong attempts
        if (wrongAttempts >= 2) SetCardGlow(cardA, true);

        inputLocked = false;
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
        var outline = card.GetComponent<Outline>();
        if (outline != null) outline.enabled = glow;
    }

    private IEnumerator ShakeCard(Button card)
    {
        RectTransform rt      = card.GetComponent<RectTransform>();
        Quaternion    origin  = rt.localRotation;
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
