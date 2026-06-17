using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  PronounCard_YouAndWeGame
///  One falling picture card. Handles its own drift, drag, and snap.
/// ════════════════════════════════════════════════════════════════════
///
///  PREFAB STRUCTURE:
///  PronounCard_Prefab    [RectTransform] [CanvasGroup] [Image] [this script]
///    ├─ CardImage          Image   (the picture)
///    └─ CardLabel          TMP_Text  (optional word label)
///
///  The card auto-adds drag handlers via EventTrigger in code — no
///  extra components needed on the prefab.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class PronounCard_YouAndWeGame : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Visuals")]
    public Image    cardImage;
    public TMP_Text cardLabel;

    // ── Runtime state ────────────────────────────────────────────────
    public PronounCardData_YouAndWeGame Data { get; private set; }

    private PronounDragScreen_YouAndWeGame _screen;
    private RectTransform  _rt;
    private CanvasGroup    _cg;
    private Canvas         _canvas;
    private RectTransform  _spawnArea;

    private float _fallSpeed;
    private bool  _isDragging  = false;
    private bool  _isPlaced    = false;
    private bool  _isFalling   = true;

    private Vector2 _dragOffset;
    private Vector2 _fallPos;      // current fall position

    // Wobble idle
    private float _wobbleTimer;
    private float _wobbleFreq;
    private float _wobbleAmp;

    // ── Initialise ───────────────────────────────────────────────────
    public void Initialise(PronounCardData_YouAndWeGame data,
                           PronounDragScreen_YouAndWeGame screen,
                           float fallSpeed,
                           RectTransform spawnArea)
    {
        Data       = data;
        _screen    = screen;
        _fallSpeed = fallSpeed;
        _spawnArea = spawnArea;
        _rt        = GetComponent<RectTransform>();
        _cg        = GetComponent<CanvasGroup>();
        _canvas    = GetComponentInParent<Canvas>();

        if (cardImage != null && data.cardSprite != null)
            cardImage.sprite = data.cardSprite;

        if (cardLabel != null)
            cardLabel.text = data.cardLabel;

        _fallPos     = _rt.anchoredPosition;
        _wobbleFreq  = Random.Range(0.8f, 1.4f);
        _wobbleAmp   = Random.Range(6f, 14f);
        _wobbleTimer = Random.Range(0f, Mathf.PI * 2f);

        // Entry pop
        StartCoroutine(EntryPop());
    }

    // ── Update — falling & wobble ────────────────────────────────────
    void Update()
    {
        if (_isPlaced || _isDragging) return;

        // Fall downward
        _fallPos.y -= _fallSpeed * Time.deltaTime;

        // Gentle left-right wobble
        _wobbleTimer += Time.deltaTime * _wobbleFreq;
        float wobbleX = Mathf.Sin(_wobbleTimer * Mathf.PI * 2f) * _wobbleAmp;

        _rt.anchoredPosition = new Vector2(_fallPos.x + wobbleX, _fallPos.y);

        // Wrap: if card falls off the bottom, float it back to the top
        if (_fallPos.y < -(_spawnArea.rect.height + 100f))
        {
            _fallPos.y = 80f;
            float halfW = _spawnArea.rect.width * 0.5f - 60f;
            _fallPos.x  = Random.Range(-halfW, halfW);
        }
    }

    // ── Drag handlers ────────────────────────────────────────────────
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_isPlaced) return;
        // Lift to front
        transform.SetAsLastSibling();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_isPlaced) return;
        _isDragging = true;
        _cg.blocksRaycasts = false;

        // Use _spawnArea (same space as OnDrag) — using _rt caused a coordinate-space
        // mismatch that made the card jump away from the finger whenever it had moved.
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _spawnArea, eventData.position, eventData.pressEventCamera, out Vector2 pointerInSpawnArea);
        _dragOffset = _rt.anchoredPosition - pointerInSpawnArea;

        // Sync _fallPos so Update() doesn't fight the drag position
        _fallPos = _rt.anchoredPosition;

        // Scale up slightly for tactile feel
        StopCoroutine("ScaleTo");
        StartCoroutine(ScaleTo(1.15f, 0.08f));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || _isPlaced) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _spawnArea, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        _rt.anchoredPosition = localPoint + _dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging        = false;
        _cg.blocksRaycasts = true;

        StartCoroutine(ScaleTo(1f, 0.1f));

        // Check overlap with any house
        PronounHouse_YouAndWeGame hitHouse = FindOverlappingHouse();
        _screen.OnCardDropped(this, hitHouse);
    }

    // ── House overlap detection ───────────────────────────────────────
    PronounHouse_YouAndWeGame FindOverlappingHouse()
    {
        // Use Physics2D isn't appropriate for UI — use RectTransform overlap
        var allHouses = FindObjectsOfType<PronounHouse_YouAndWeGame>();
        foreach (var house in allHouses)
        {
            if (house.IsOccupied) continue;
            if (RectOverlaps(_rt, house.dropZone))
                return house;
        }
        return null;
    }

    bool RectOverlaps(RectTransform a, RectTransform b)
    {
        Vector3[] cornersA = new Vector3[4];
        Vector3[] cornersB = new Vector3[4];
        a.GetWorldCorners(cornersA);
        b.GetWorldCorners(cornersB);

        Rect ra = new Rect(cornersA[0].x, cornersA[0].y,
                           cornersA[2].x - cornersA[0].x,
                           cornersA[2].y - cornersA[0].y);
        Rect rb = new Rect(cornersB[0].x, cornersB[0].y,
                           cornersB[2].x - cornersB[0].x,
                           cornersB[2].y - cornersB[0].y);
        return ra.Overlaps(rb);
    }

    // ── Public: reactions ─────────────────────────────────────────────
    public void SnapToHouse(PronounHouse_YouAndWeGame house)
    {
        _isPlaced          = true;
        _isFalling         = false;
        _cg.blocksRaycasts = false;

        // Re-parent to house drop zone so it stays inside visually
        transform.SetParent(house.dropZone, true);
        StartCoroutine(AnimateSnap(Vector2.zero));
        house.IsOccupied = true;
    }

    public void ReturnToFall()
    {
        _isDragging        = false;
        _isPlaced          = false;
        _isFalling         = true;
        _cg.blocksRaycasts = true;
        StartCoroutine(ScaleTo(1f, 0.1f));
        // _fallPos keeps whatever Y it was at — card resumes falling
    }

    // ── Coroutines ────────────────────────────────────────────────────
    IEnumerator EntryPop()
    {
        transform.localScale = Vector3.zero;
        float t = 0f, dur = 0.3f;
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

    IEnumerator AnimateSnap(Vector2 targetAnchor)
    {
        RectTransform rt = GetComponent<RectTransform>();
        Vector2 start    = rt.anchoredPosition;
        float t = 0f, dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = Vector2.Lerp(start, targetAnchor, EaseOutCubic(t / dur));
            yield return null;
        }
        rt.anchoredPosition = targetAnchor;

        // Celebratory scale bounce
        yield return ScaleTo(1.2f, 0.07f);
        yield return ScaleTo(1f,   0.07f);
    }

    IEnumerator ScaleTo(float target, float dur)
    {
        Vector3 start = transform.localScale;
        Vector3 end   = Vector3.one * target;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, end, t / dur);
            yield return null;
        }
        transform.localScale = end;
    }

    static float EaseOutBack(float t, float o = 1.70158f)
    { t -= 1f; return t * t * ((o + 1f) * t + o) + 1f; }

    static float EaseOutCubic(float t)
    { t -= 1f; return t * t * t + 1f; }
}