using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controls the Unit 1 Intro: "YOU, ME & WE".
/// Implements IUnitCompletable to signal back to SharedUnitPanelController.
/// Built with advanced DOTween animations for a highly responsive, engaging, and smooth experience for kids.
/// </summary>
public class YouMeAndWeIntroController : MonoBehaviour, IUnitCompletable
{
    [Header("Callbacks (Auto-set at runtime)")]
    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton unitButton;

    [Header("Background & Scene Transition")]
    [Tooltip("The main canvas group used to fade the entire intro screen in/out")]
    public CanvasGroup mainCanvasGroup;
    public float fadeInDuration = 1.5f;

    [Header("Wooden Sign")]
    public RectTransform woodenSign;
    public float signDropDuration = 0.8f;
    [Tooltip("Offset above the target position where the sign drops from")]
    public float signOffscreenOffsetY = 800f;
    [Tooltip("Initial rotation angle when dropping in to simulate swinging")]
    public float signInitialSwingAngle = 15f;

    [Header("Robin Mascot")]
    public RectTransform robinMascot;
    public Animator robinAnimator;
    [Tooltip("Animation state name for Robin waving")]
    public string waveAnimationState = "wave";
    [Tooltip("Animation state name for Robin idle/talking")]
    public string talkAnimationState = "talk";

    [Header("Pronoun Bubbles")]
    [Tooltip("The 6 friendly pronoun bubbles (I, He, She, It, We, You)")]
    public RectTransform[] pronounBubbles;
    [Tooltip("How high bubbles float before wrapping around (if using rising loop)")]
    public float bubbleFloatHeight = 600f;
    public float bubbleDriftSpeed = 50f; // pixels per second
    [Tooltip("Wobble angle for the bubbles to make them feel organic")]
    public float bubbleWobbleAngle = 8f;
    public float bubbleWobbleSpeed = 2f;

    [Header("Start Button")]
    public Button startButton;
    public RectTransform startButtonRect;
    [Tooltip("How long one pulse cycle takes")]
    public float buttonPulseDuration = 0.8f;

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource speechSource;

    [Header("Audio Clips")]
    public AudioClip bgmJungleLoop;
    public AudioClip sfxSignDrop;
    public AudioClip sfxButtonPress;
    public AudioClip speechWelcome;
    public AudioClip speechTapStart;

    // Track active tweens to cleanly kill them on disable
    private Tween _bgmFadeTween;
    private Tween _signDropTween;
    private Tween _signRotateTween;
    private Tween _buttonPulseTween;
    private Tween[] _bubblePositionTweens;
    private Tween[] _bubbleRotationTweens;
    
    private Coroutine _introSequenceCoroutine;
    private Coroutine _promptCoroutine;

    private bool _hasClickedStart = false;

    // ── IUnitCompletable Implementation ──────────────────────────────────
    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        panel = sharedPanel;
        unitButton = sharedButton;
    }

    // ── Unity Lifecycle ──────────────────────────────────────────────────
    private void OnEnable()
    {
        ResetSceneState();
        StartIntro();
    }

    private void OnDisable()
    {
        CleanUpTweens();
        if (_introSequenceCoroutine != null) StopCoroutine(_introSequenceCoroutine);
        if (_promptCoroutine != null) StopCoroutine(_promptCoroutine);
    }

    // ── Intro Setup & Sequence ──────────────────────────────────────────
    private void ResetSceneState()
    {
        _hasClickedStart = false;
        
        // Hide/Reset Main Canvas Group
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.alpha = 0f;
            mainCanvasGroup.blocksRaycasts = false;
        }

        // Position wooden sign offscreen top
        if (woodenSign != null)
        {
            woodenSign.anchoredPosition = new Vector2(woodenSign.anchoredPosition.x, woodenSign.anchoredPosition.y + signOffscreenOffsetY);
            woodenSign.localRotation = Quaternion.identity;
        }

        // Initialize Robin Mascot scale and position
        if (robinMascot != null)
        {
            robinMascot.localScale = Vector3.zero;
        }

        // Set start button to be inactive at first
        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (startButtonRect != null)
        {
            startButtonRect.localScale = Vector3.zero;
        }

        // Reset and hide bubbles initially, we will fade them or pop them in
        if (pronounBubbles != null)
        {
            foreach (var bubble in pronounBubbles)
            {
                if (bubble != null)
                {
                    bubble.localScale = Vector3.zero;
                    bubble.localRotation = Quaternion.identity;
                }
            }
        }

        CleanUpTweens();
    }

    private void StartIntro()
    {
        _introSequenceCoroutine = StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // 1. Background and jungle ambience fade in (1.5s)
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad);
        }

        if (bgmSource != null && bgmJungleLoop != null)
        {
            bgmSource.clip = bgmJungleLoop;
            bgmSource.loop = true;
            bgmSource.volume = 0f;
            bgmSource.Play();
            _bgmFadeTween = bgmSource.DOFade(0.25f, fadeInDuration); // Shared low volume BGM
        }

        yield return new WaitForSeconds(fadeInDuration);

        // 2. Wooden sign drops in with a soft "thock" and a small bounce
        if (woodenSign != null)
        {
            float targetY = woodenSign.anchoredPosition.y - signOffscreenOffsetY;
            
            // Drop down with a nice bounce
            _signDropTween = woodenSign.DOAnchorPosY(targetY, signDropDuration).SetEase(Ease.OutBounce);
            
            // Subtle swing animation for a playful feel
            woodenSign.localRotation = Quaternion.Euler(0f, 0f, signInitialSwingAngle);
            _signRotateTween = woodenSign.DOLocalRotate(new Vector3(0f, 0f, 0f), signDropDuration * 1.5f, RotateMode.Fast)
                                         .SetEase(Ease.OutBack);

            // Play sign drop sound
            if (sfxSource != null && sfxSignDrop != null)
            {
                sfxSource.PlayOneShot(sfxSignDrop);
            }

            yield return new WaitForSeconds(signDropDuration);
        }

        // 3. Robin waves and says his welcome line
        if (robinMascot != null)
        {
            // Pop Robin up with an elastic ease (extra cute/engaging)
            robinMascot.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack);

            // Play wave animation if animator exists
            if (robinAnimator != null)
            {
                robinAnimator.Play(waveAnimationState);
            }

            // Play welcome voiceover
            if (speechSource != null && speechWelcome != null)
            {
                speechSource.clip = speechWelcome;
                speechSource.Play();
                
                // Let Robin wave for a bit, then switch to a talking state if we have a speech clip running
                float speechLength = speechWelcome.length;
                float waveDuration = Mathf.Min(1.5f, speechLength);
                yield return new WaitForSeconds(waveDuration);
                
                if (robinAnimator != null && speechLength > waveDuration)
                {
                    robinAnimator.Play(talkAnimationState);
                }
                
                yield return new WaitForSeconds(speechLength - waveDuration);
                
                // Return to idle
                if (robinAnimator != null)
                {
                    robinAnimator.Play("idle");
                }
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
        }

        // 4. Six pronoun bubbles pop in and drift upward slowly on a loop
        StartPronounBubbles();

        // Allow child to tap start now
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.blocksRaycasts = true;
        }

        // 5. Activate START leaf-button
        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            if (startButtonRect != null)
            {
                // Playful pop-in animation
                startButtonRect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).OnComplete(StartButtonIdlePulse);
            }
        }

        // 6. Start prompt reminder timer (plays u1_intro_tap_start.mp3 if inactive for 5 seconds)
        _promptCoroutine = StartCoroutine(StartPromptReminderTimer(5f));
    }

    // ── Pronoun Bubbles Floating Loop ───────────────────────────────────
    private void StartPronounBubbles()
    {
        if (pronounBubbles == null || pronounBubbles.Length == 0) return;

        _bubblePositionTweens = new Tween[pronounBubbles.Length];
        _bubbleRotationTweens = new Tween[pronounBubbles.Length];

        for (int i = 0; i < pronounBubbles.Length; i++)
        {
            var bubble = pronounBubbles[i];
            if (bubble == null) continue;

            // Staggered pop-in animation
            float delay = i * 0.15f;
            bubble.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack).SetDelay(delay);

            // Establish rising loop
            // Calculate a random height drift or duration for natural feel
            float startY = bubble.anchoredPosition.y;
            float targetY = startY + bubbleFloatHeight;
            float duration = bubbleFloatHeight / (bubbleDriftSpeed * Random.Range(0.8f, 1.2f));

            // Start vertical movement loop
            // When it reaches target Y, it wraps around to start Y
            _bubblePositionTweens[i] = bubble.DOAnchorPosY(targetY, duration)
                .SetEase(Ease.Linear)
                .SetDelay(delay)
                .OnStepComplete(() => {
                    // Wrap around to start Y instantly on complete
                    bubble.anchoredPosition = new Vector2(bubble.anchoredPosition.x, startY);
                })
                .SetLoops(-1, LoopType.Restart);

            // Add organic side-to-side wobble rotation
            float wobbleAngle = bubbleWobbleAngle * Random.Range(0.7f, 1.3f);
            float wobbleDur = bubbleWobbleSpeed * Random.Range(0.8f, 1.2f);
            
            // Set initial rotation
            bubble.localRotation = Quaternion.Euler(0f, 0f, -wobbleAngle);
            _bubbleRotationTweens[i] = bubble.DOLocalRotate(new Vector3(0f, 0f, wobbleAngle), wobbleDur)
                .SetEase(Ease.InOutQuad)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    // ── Button Idle Pulse (Engagement Helper) ──────────────────────────
    private void StartButtonIdlePulse()
    {
        if (startButtonRect == null) return;

        // Soft engaging breathing pulse
        _buttonPulseTween = startButtonRect.DOScale(new Vector3(1.08f, 0.95f, 1f), buttonPulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    // ── Start Prompt Reminder ──────────────────────────────────────────
    private IEnumerator StartPromptReminderTimer(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Keep reminding every 8 seconds if welcome speech has ended and they haven't clicked
        while (!_hasClickedStart)
        {
            if (speechSource != null && speechTapStart != null && !speechSource.isPlaying)
            {
                speechSource.clip = speechTapStart;
                speechSource.Play();
            }
            yield return new WaitForSeconds(8f);
        }
    }

    // ── Handle Start Button Click ─────────────────────────────────────
    private void OnStartButtonClicked()
    {
        if (_hasClickedStart) return;
        _hasClickedStart = true;

        // Stop reminder speech immediately
        if (speechSource != null && speechSource.isPlaying)
        {
            speechSource.Stop();
        }

        // Kill pulsing tween
        if (_buttonPulseTween != null)
        {
            _buttonPulseTween.Kill();
        }

        // Play press sound
        if (sfxSource != null && sfxButtonPress != null)
        {
            sfxSource.PlayOneShot(sfxButtonPress);
        }

        // Juicy Squish Animation Sequence (scale squash & stretch)
        if (startButtonRect != null)
        {
            Sequence squishSeq = DOTween.Sequence();
            
            // 1. Squash down (width goes wide, height goes short)
            squishSeq.Append(startButtonRect.DOScale(new Vector3(1.25f, 0.7f, 1f), 0.15f).SetEase(Ease.OutQuad));
            
            // 2. Stretch up (width goes narrow, height goes tall)
            squishSeq.Append(startButtonRect.DOScale(new Vector3(0.85f, 1.2f, 1f), 0.15f).SetEase(Ease.InOutQuad));
            
            // 3. Return to normal with a cute springy overshoot
            squishSeq.Append(startButtonRect.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutElastic));

            // Complete the unit once squish finishes
            squishSeq.OnComplete(FinishUnit);
        }
        else
        {
            FinishUnit();
        }
    }

    private void FinishUnit()
    {
        // Smoothly fade out screen before returning
        if (mainCanvasGroup != null)
        {
            mainCanvasGroup.blocksRaycasts = false;
            mainCanvasGroup.DOFade(0f, 0.4f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                if (panel != null && unitButton != null)
                {
                    panel.UnitFinished(unitButton);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            });
        }
        else
        {
            if (panel != null && unitButton != null)
            {
                panel.UnitFinished(unitButton);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    // ── Helper to Clean Up Tweens ──────────────────────────────────────
    private void CleanUpTweens()
    {
        _bgmFadeTween?.Kill();
        _signDropTween?.Kill();
        _signRotateTween?.Kill();
        _buttonPulseTween?.Kill();

        if (_bubblePositionTweens != null)
        {
            foreach (var t in _bubblePositionTweens) t?.Kill();
            _bubblePositionTweens = null;
        }

        if (_bubbleRotationTweens != null)
        {
            foreach (var t in _bubbleRotationTweens) t?.Kill();
            _bubbleRotationTweens = null;
        }
    }
}
