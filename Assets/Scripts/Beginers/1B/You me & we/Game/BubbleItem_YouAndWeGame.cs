using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  BubbleItem_YouAndWeGame
///  One rising bubble. Self-contained movement + tap detection.
/// ════════════════════════════════════════════════════════════════════
///
///  PREFAB STRUCTURE:
///  Bubble_Prefab    [RectTransform] [CanvasGroup] [Image] [Button]
///                   [this script]
///    └─ WordText    TMP_Text   (centered, displays the affirmation word)
///
///  No extra wiring needed — BubblePopScreen passes all dependencies
///  through Initialise().
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Button))]
public class BubbleItem_YouAndWeGame : MonoBehaviour, IPointerClickHandler
{
    [Header("Visuals")]
    public Image    bubbleImage;
    public TMP_Text wordText;

    // ── Runtime ──────────────────────────────────────────────────────
    private bool   _isTarget;
    private float  _riseSpeed;
    private bool   _alive = true;

    private RectTransform              _rt;
    private CanvasGroup                _cg;
    private RectTransform              _spawnArea;
    private BubblePopScreen_YouAndWeGame _screen;

    // Drift
    private float _driftTimer;
    private float _driftFreq;
    private float _driftAmp;
    private float _startX;

    // Idle bob
    private float _bobTimer;

    // ── Initialise ───────────────────────────────────────────────────
    public void Initialise(string word, bool isTarget,
                           float startX, float startY,
                           float riseSpeed,
                           BubblePopScreen_YouAndWeGame screen,
                           RectTransform spawnArea)
    {
        _isTarget  = isTarget;
        _riseSpeed = riseSpeed;
        _screen    = screen;
        _spawnArea = spawnArea;
        _startX    = startX;

        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();

        _rt.anchoredPosition = new Vector2(startX, startY);

        if (wordText    != null) wordText.text = word;
        if (bubbleImage != null)
        {
            // Tint targets slightly warmer — subtle hint
            bubbleImage.color = isTarget
                ? new Color(1f, 0.95f, 0.75f)
                : new Color(0.75f, 0.90f, 1f);
        }

        _driftFreq  = Random.Range(0.3f, 0.6f);
        // Keep drift small — each bubble owns a lane, large drift causes overlap
        _driftAmp   = Random.Range(8f, 16f);
        _driftTimer = Random.Range(0f, Mathf.PI * 2f);
        _bobTimer   = Random.Range(0f, Mathf.PI * 2f);

        // Entry pop-in
        transform.localScale = Vector3.zero;
        StartCoroutine(PopIn());
    }

    // ── Update — rise and drift ───────────────────────────────────────
    void Update()
    {
        if (!_alive) return;

        _driftTimer += Time.deltaTime * _driftFreq;
        _bobTimer   += Time.deltaTime * 1.2f;

        float drift = Mathf.Sin(_driftTimer * Mathf.PI * 2f) * _driftAmp;
        float bob   = Mathf.Sin(_bobTimer   * Mathf.PI * 2f) * 2f;  // tiny vertical oscillation

        Vector2 pos = _rt.anchoredPosition;
        pos.x = _startX + drift;
        pos.y += (_riseSpeed + bob) * Time.deltaTime;
        _rt.anchoredPosition = pos;

        // Wrap off top — return to bottom of same lane (keep _startX, no random re-lane)
        if (pos.y > _spawnArea.rect.height * 0.5f + 80f)
        {
            pos.y = -(_spawnArea.rect.height * 0.5f + 60f);
            // _startX intentionally unchanged — bubble stays in its assigned lane
            _rt.anchoredPosition = pos;
        }
    }

    // ── Tap ──────────────────────────────────────────────────────────
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_alive) return;
        _screen?.OnBubbleTapped(this, _isTarget);
    }

    // ── Reactions ────────────────────────────────────────────────────
    public void Wobble()
    {
        StopCoroutine("WobbleAnim");
        StartCoroutine(WobbleAnim());
    }

    public void PopAndDestroy()
    {
        _alive = false;
        StopAllCoroutines();
        StartCoroutine(PopAnim());
    }

    public void FadeOutAndDestroy()
    {
        _alive = false;
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    // ── Coroutines ────────────────────────────────────────────────────
    IEnumerator PopIn()
    {
        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float s = EaseOutBack(p);
            transform.localScale = Vector3.one * s;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    IEnumerator WobbleAnim()
    {
        float t = 0f, dur = 0.45f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float shake = Mathf.Sin(t * Mathf.PI * 14f) * Mathf.Lerp(8f, 0f, t / dur);
            _rt.anchoredPosition += new Vector2(shake, 0f);
            yield return null;
        }
    }

    IEnumerator PopAnim()
    {
        // Quick scale-up burst then vanish
        float t = 0f, dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float s = Mathf.Lerp(1f, 1.6f, p);
            transform.localScale = Vector3.one * s;
            if (_cg != null) _cg.alpha = 1f - p;
            yield return null;
        }
        Destroy(gameObject);
    }

    IEnumerator FadeOut()
    {
        float t = 0f, dur = 0.3f;
        float startAlpha = _cg != null ? _cg.alpha : 1f;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (_cg != null) _cg.alpha = Mathf.Lerp(startAlpha, 0f, t / dur);
            yield return null;
        }
        Destroy(gameObject);
    }

    static float EaseOutBack(float t, float o = 1.70158f)
    { t -= 1f; return t * t * ((o + 1f) * t + o) + 1f; }
}