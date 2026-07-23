using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Toggle-to-talk button for BB1 Speaking unit.
/// • Idle      → mic icon + "Tap to speak" label, overlay hidden
/// • Listening → overlay pops in (scale + fade), Animator plays on parent, bars animate
/// </summary>
public class ToggleToTalkButton_BB1 : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Icon Sprites")]
    public Sprite idleIcon;
    public Sprite listeningIcon;

    [Header("References")]
    public Image           buttonImage;
    public TextMeshProUGUI statusLabel;

    [Header("Labels")]
    public string idleLabel      = "Tap to speak";
    public string listeningLabel = "Listening...";

    [Header("Listening Overlay")]
    [Tooltip("Child GameObject shown while listening (your bars/wave panel + stop button).")]
    public GameObject listeningOverlay;

    [Header("Listening Animator")]
    [Tooltip("Animator sits on THIS GameObject (parent). We enable it via enabled flag, not SetActive.")]
    public Animator listeningAnim;
    [Tooltip("Must match the state name in your Animator controller exactly (default: listening)")]
    public string   listeningStateName = "listening";

    [Header("Equalizer Bars (inside the mic button)")]
    public RectTransform[] bars;

    [Header("Bar Animation Settings")]
    public float barMinHeight = 8f;
    public float barMaxHeight = 40f;
    public float barAnimSpeed = 0.25f;

    [Header("SFX")]
    public AudioSource sfxSource;
    public AudioClip   sfxMicOn;       // played when mic button tapped to start
    public AudioClip   sfxMicOff;      // played when listening stops

    // ── Runtime ────────────────────────────────────────────────────────────────
    private bool        _isListening = false;
    private float[]     _barBaseHeights;
    private CanvasGroup _overlayCanvasGroup;

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    void Awake()
    {
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        if (listeningOverlay != null)
        {
            _overlayCanvasGroup = listeningOverlay.GetComponent<CanvasGroup>();
            if (_overlayCanvasGroup == null)
                _overlayCanvasGroup = listeningOverlay.AddComponent<CanvasGroup>();
        }
    }

    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnButtonClicked);
        }

        if (bars != null)
        {
            _barBaseHeights = new float[bars.Length];
            for (int i = 0; i < bars.Length; i++)
                if (bars[i] != null)
                    _barBaseHeights[i] = bars[i].sizeDelta.y;
        }

        ApplyState(false);
    }

    void OnEnable()
    {
        _isListening = false;
        ApplyState(false);
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
    }

    void OnDisable()
    {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
        StopBarAnimation();
    }

    // ── Toggle ─────────────────────────────────────────────────────────────────
    void OnButtonClicked()
    {
        _isListening = !_isListening;

        if (_isListening)
        {
            PlaySFX(sfxMicOn);
            CrossPlatformSpeechManager.Instance?.StartListening();
        }
        else
        {
            PlaySFX(sfxMicOff);
            CrossPlatformSpeechManager.Instance?.StopListening();
        }

        ApplyState(_isListening);
    }

    // ── Public API ─────────────────────────────────────────────────────────────
    public void ForceIdle()
    {
        if (!_isListening) return;
        _isListening = false;
        CrossPlatformSpeechManager.Instance?.StopListening();
        PlaySFX(sfxMicOff);
        ApplyState(false);
    }

    /// <summary>Wire to the Stop button inside listeningOverlay.</summary>
    public void OnStopButtonClicked()
    {
        _isListening = false;
        CrossPlatformSpeechManager.Instance?.StopListening();
        PlaySFX(sfxMicOff);
        ApplyState(false);
    }

    // ── State ──────────────────────────────────────────────────────────────────
    void ApplyState(bool listening)
    {
        if (buttonImage != null)
            buttonImage.sprite = listening ? listeningIcon : idleIcon;

        if (statusLabel != null)
            statusLabel.text = listening ? listeningLabel : idleLabel;

        // Play/stop animation directly by state name — no parameters needed
        if (listeningAnim != null)
        {
            if (listening)
            {
                listeningAnim.enabled = true;
                listeningAnim.Play(listeningStateName, 0, 0f);
            }
            else
            {
                listeningAnim.enabled = false;
            }
        }

        if (listening)
        {
            StartCoroutine(ShowOverlay());
            StartBarAnimation();
        }
        else
        {
            HideOverlay();
            StopBarAnimation();
        }
    }

    // ── Overlay ────────────────────────────────────────────────────────────────
    IEnumerator ShowOverlay()
    {
        if (listeningOverlay == null) yield break;

        var rt = listeningOverlay.GetComponent<RectTransform>();

        // Start state: active, invisible, scaled down
        listeningOverlay.SetActive(true);
        if (rt != null) rt.localScale = new Vector3(0.5f, 0.5f, 1f);
        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.alpha          = 0f;
            _overlayCanvasGroup.interactable   = false;
            _overlayCanvasGroup.blocksRaycasts = false;
        }

        yield return null; // one frame gap

        // Animate in
        if (rt != null)
            rt.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.DOFade(1f, 0.25f).OnComplete(() =>
            {
                _overlayCanvasGroup.interactable   = true;
                _overlayCanvasGroup.blocksRaycasts = true;
            });
        }
    }

    void HideOverlay()
    {
        if (listeningOverlay == null) return;

        var rt = listeningOverlay.GetComponent<RectTransform>();

        if (_overlayCanvasGroup != null)
        {
            _overlayCanvasGroup.interactable   = false;
            _overlayCanvasGroup.blocksRaycasts = false;
            _overlayCanvasGroup.DOFade(0f, 0.2f);
        }

        if (rt != null)
            rt.DOScale(new Vector3(0.5f, 0.5f, 1f), 0.2f).SetEase(Ease.InBack)
              .OnComplete(() => listeningOverlay.SetActive(false));
        else
            listeningOverlay.SetActive(false);
    }

    // ── Bar Animation ──────────────────────────────────────────────────────────
    void StartBarAnimation()
    {
        if (bars == null || bars.Length == 0) return;
        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] == null) continue;
            bars[i].DOKill();
            AnimateBarLoop(bars[i], i * (barAnimSpeed * 0.35f));
        }
    }

    void AnimateBarLoop(RectTransform bar, float initialDelay)
    {
        if (bar == null) return;
        DOTween.Sequence().SetId(bar)
            .AppendInterval(initialDelay)
            .AppendCallback(() => PumpBar(bar));
    }

    void PumpBar(RectTransform bar)
    {
        if (bar == null || !_isListening) return;
        float targetH = Random.Range(barMinHeight, barMaxHeight);
        bar.DOSizeDelta(new Vector2(bar.sizeDelta.x, targetH), barAnimSpeed)
           .SetEase(Ease.InOutSine)
           .OnComplete(() => { if (_isListening) PumpBar(bar); else ResetBar(bar); });
    }

    void StopBarAnimation()
    {
        if (bars == null) return;
        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] == null) continue;
            bars[i].DOKill();
            ResetBar(bars[i]);
        }
    }

    void ResetBar(RectTransform bar)
    {
        if (bar == null) return;
        int   idx   = System.Array.IndexOf(bars, bar);
        float baseH = (idx >= 0 && _barBaseHeights != null && idx < _barBaseHeights.Length)
                    ? _barBaseHeights[idx] : barMinHeight;
        bar.DOSizeDelta(new Vector2(bar.sizeDelta.x, baseH), 0.15f).SetEase(Ease.OutSine);
    }

    // ── SFX ───────────────────────────────────────────────────────────────────
    void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // ── Speech Callback ────────────────────────────────────────────────────────
    void OnSpeechResult(string _)
    {
        _isListening = false;
        ApplyState(false);
    }
}