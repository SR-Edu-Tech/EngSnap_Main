using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen 2 — Bob (player) greets the teacher.
/// Two turns:
///   Turn 1 → Bob initiates greeting.
///   Turn 2 → Bob responds to teacher's question.
/// Context-specific wrong-answer feedback:
///   Card B Turn 1 → "We say Hello to friends. Teachers need a special greeting!"
///   Card C Turn 1 → Clock animation + "It is morning time!"
///   Card B Turn 2 → "Be polite! Try a better answer."
/// </summary>
public class Screen2Controller : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────────

    //[Header("── Characters ──")]
   // public Animator bobAnimator;
    //public Animator teacherAnimator;

    [Header("── Speech Bubbles ──")]
    public GameObject bobSpeechBubble;
    public TMP_Text   bobSpeechText;
    public GameObject teacherSpeechBubble;
    public TMP_Text   teacherSpeechText;

    [Header("── Instruction ──")]
    public TMP_Text instructionText;

    [Header("── Choice Cards ──")]
    public GameObject choiceCardPanel;
    public Button cardA;
    public Button cardB;
    public Button cardC;
    public TMP_Text cardAText;
    public TMP_Text cardBText;
    public TMP_Text cardCText;

    [Header("── Clock Animation ──")]
    public GameObject clockAnimationObject;   // Drag your clock anim GO here

    [Header("── Audio ──")]
    public AudioSource audioSource;
    // Turn 1
    public AudioClip   bobGreetingClip;          // 'Good morning, teacher!'
    public AudioClip   teacherGreetingClip;       // 'Good morning, Bob! How are you doing?'
    public AudioClip   wrongFriendGreetingClip;   // "We say Hello to friends…"
    public AudioClip   wrongEveningClip;          // "It is morning time!"
    // Turn 2
    public AudioClip   bobResponseClip;           // 'I am doing great, ma'am! Thank you.'
    public AudioClip   teacherFinalClip;          // 'I am great too, thanks.'
    public AudioClip   wrongImpoliteClip;         // "Be polite! Try a better answer."
    public AudioClip   wrongGoodbyeClip;          // generic try-again
    [Tooltip("Short pop sound played for each card as it appears.")]
    public AudioClip   cardPopSound;

    [Header("── Feedback ──")]
    public float shakeDuration  = 0.4f;
    public float shakeMagnitude = 10f;

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE
    // ─────────────────────────────────────────────────────────────
    private Action  onCompleteCallback;
    private int     turn         = 1;     // 1 or 2
    private int     wrongAttempts = 0;
    private bool    inputLocked   = false;

    private static readonly int TalkingParam   = Animator.StringToHash("isTalking");
    private static readonly int ShakeHeadParam = Animator.StringToHash("shakeHead");

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────
    public void StartScreen(Action onComplete)
    {
        onCompleteCallback = onComplete;
        turn          = 1;
        wrongAttempts = 0;
        inputLocked   = false;

        bobSpeechBubble.SetActive(false);
        teacherSpeechBubble.SetActive(false);
        choiceCardPanel.SetActive(false);
        if (clockAnimationObject != null) clockAnimationObject.SetActive(false);

        SetCardGlow(cardA, false);
        SetCardGlow(cardB, false);
        SetCardGlow(cardC, false);

        LoadTurn1();
    }

    // ─────────────────────────────────────────────────────────────
    //  TURN 1
    // ─────────────────────────────────────────────────────────────
    private void LoadTurn1()
    {
        turn          = 1;
        wrongAttempts = 0;

        instructionText.text = "Bob walks into class. What does he say to the teacher?";

        cardAText.text = "Good morning, teacher!";
        cardBText.text = "Hello, friend!";
        cardCText.text = "Good evening.";

        SetCardGlow(cardA, false);
        SetCardGlow(cardB, false);
        SetCardGlow(cardC, false);

        cardA.onClick.RemoveAllListeners();
        cardB.onClick.RemoveAllListeners();
        cardC.onClick.RemoveAllListeners();
        cardA.onClick.AddListener(() => OnCardTapped(true,  cardA, "A"));
        cardB.onClick.AddListener(() => OnCardTapped(false, cardB, "B"));
        cardC.onClick.AddListener(() => OnCardTapped(false, cardC, "C"));

        choiceCardPanel.SetActive(false); // PopCardsIn will activate it
        StartCoroutine(PopCardsIn(new Button[] { cardA, cardB, cardC }));
        inputLocked = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  TURN 2
    // ─────────────────────────────────────────────────────────────
    private void LoadTurn2()
    {
        turn          = 2;
        wrongAttempts = 0;

        instructionText.text = "Teacher asks Bob how he is. What does Bob say?";

        cardAText.text = "I am doing great, Thank you ma'am.How about you ma'am?";
        cardBText.text = "I am sleepy.";
        cardCText.text = "Goodbye, teacher.";

        SetCardGlow(cardA, false);
        SetCardGlow(cardB, false);
        SetCardGlow(cardC, false);

        cardA.onClick.RemoveAllListeners();
        cardB.onClick.RemoveAllListeners();
        cardC.onClick.RemoveAllListeners();
        cardA.onClick.AddListener(() => OnCardTapped(true,  cardA, "A"));
        cardB.onClick.AddListener(() => OnCardTapped(false, cardB, "B"));
        cardC.onClick.AddListener(() => OnCardTapped(false, cardC, "C"));

        choiceCardPanel.SetActive(false); // PopCardsIn will activate it
        StartCoroutine(PopCardsIn(new Button[] { cardA, cardB, cardC }));
        inputLocked = false;
    }

    // ─────────────────────────────────────────────────────────────
    //  CARD TAP HANDLER
    // ─────────────────────────────────────────────────────────────
    private void OnCardTapped(bool isCorrect, Button tappedCard, string cardLabel)
    {
        if (inputLocked) return;
        inputLocked = true;

        if (isCorrect)
        {
            StartCoroutine(CorrectAnswer());
        }
        else
        {
            wrongAttempts++;
            StartCoroutine(WrongAnswer(tappedCard, cardLabel));
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  CORRECT PATHS
    // ─────────────────────────────────────────────────────────────
    private IEnumerator CorrectAnswer()
    {
        choiceCardPanel.SetActive(false);

        if (turn == 1)
        {
            // Bob says good morning
            bobSpeechBubble.SetActive(true);
            bobSpeechText.text = "Good morning, teacher!";
           // SetTalking(bobAnimator, true);
            PlayClip(bobGreetingClip);
            yield return new WaitForSeconds(GetClipLength(bobGreetingClip, 2.0f));
           // SetTalking(bobAnimator, false);
            bobSpeechBubble.SetActive(false);

            // Teacher responds
            yield return new WaitForSeconds(0.4f);
            teacherSpeechBubble.SetActive(true);
            teacherSpeechText.text = "Good morning, Bob! How are you doing?";
            //SetTalking(teacherAnimator, true);
            PlayClip(teacherGreetingClip);
            yield return new WaitForSeconds(GetClipLength(teacherGreetingClip, 2.5f));
            //SetTalking(teacherAnimator, false);
            teacherSpeechBubble.SetActive(false);

            // Move to turn 2
            yield return new WaitForSeconds(0.3f);
            LoadTurn2();
        }
        else // turn == 2
        {
            // Bob responds
            bobSpeechBubble.SetActive(true);
            bobSpeechText.text = "I am doing great, ma'am! Thank you.";
            //SetTalking(bobAnimator, true);
            PlayClip(bobResponseClip);
            yield return new WaitForSeconds(GetClipLength(bobResponseClip, 2.5f));
            //SetTalking(bobAnimator, false);
            bobSpeechBubble.SetActive(false);

            // Teacher final
            yield return new WaitForSeconds(0.4f);
            teacherSpeechBubble.SetActive(true);
            teacherSpeechText.text = "I am great too, thanks.";
           // SetTalking(teacherAnimator, true);
            PlayClip(teacherFinalClip);
            yield return new WaitForSeconds(GetClipLength(teacherFinalClip, 2.0f));
            //SetTalking(teacherAnimator, false);
            teacherSpeechBubble.SetActive(false);

            yield return new WaitForSeconds(0.3f);
            onCompleteCallback?.Invoke();
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  WRONG PATHS  (context-specific feedback)
    // ─────────────────────────────────────────────────────────────
    private IEnumerator WrongAnswer(Button tappedCard, string cardLabel)
    {
        yield return StartCoroutine(ShakeCard(tappedCard));
        //bobAnimator.SetTrigger(ShakeHeadParam);

        if (turn == 1)
        {
            if (cardLabel == "B")
            {
                // "Hello, friend!" — wrong register
                PlayClip(wrongFriendGreetingClip);
                yield return new WaitForSeconds(GetClipLength(wrongFriendGreetingClip, 3.5f));
            }
            else if (cardLabel == "C")
            {
                // "Good evening." — wrong time → show clock
                if (clockAnimationObject != null)
                {
                    clockAnimationObject.SetActive(true);
                    yield return new WaitForSeconds(0.5f);   // let anim play a beat
                }
                PlayClip(wrongEveningClip);
                yield return new WaitForSeconds(GetClipLength(wrongEveningClip, 3.0f));
                if (clockAnimationObject != null) clockAnimationObject.SetActive(false);
            }
        }
        else // turn == 2
        {
            if (cardLabel == "B")
            {
                // "I am sleepy." — impolite
                PlayClip(wrongImpoliteClip);
                yield return new WaitForSeconds(GetClipLength(wrongImpoliteClip, 3.0f));
            }
            else
            {
                // "Goodbye, teacher." — generic
                PlayClip(wrongGoodbyeClip);
                yield return new WaitForSeconds(GetClipLength(wrongGoodbyeClip, 2.5f));
            }
        }

        // Hint glow after 2 wrong attempts
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

    // ─────────────────────────────────────────────────────────────
    //  CARD POP-IN
    // ─────────────────────────────────────────────────────────────
    private IEnumerator PopCardsIn(Button[] cards)
    {
        choiceCardPanel.SetActive(true);

        foreach (var card in cards)
        {
            card.transform.localScale = Vector3.zero;
            card.gameObject.SetActive(true);
        }

        foreach (var card in cards)
        {
            if (cardPopSound != null) audioSource.PlayOneShot(cardPopSound);
            yield return StartCoroutine(PopCard(card.transform));
            yield return new WaitForSeconds(0.08f);
        }
    }

    private IEnumerator PopCard(Transform t)
    {
        float duration = 0.25f, elapsed = 0f;
        while (elapsed < duration)
        {
            t.localScale = Vector3.one * Mathf.Lerp(0f, 1.2f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        elapsed = 0f; float settle = 0.1f;
        while (elapsed < settle)
        {
            t.localScale = Vector3.one * Mathf.Lerp(1.2f, 1f, elapsed / settle);
            elapsed += Time.deltaTime;
            yield return null;
        }
        t.localScale = Vector3.one;
    }
}