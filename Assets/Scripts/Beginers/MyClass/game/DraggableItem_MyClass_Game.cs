using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// DRAGGABLE ITEM — attach to every item card in Screen 2.
///
/// PREFAB STRUCTURE:
///   DraggableItem (this script + CanvasGroup + Image)
///     └── NameLabel  (TextMeshProUGUI — optional)
///
/// HOW IT WORKS:
///   - Screen2 calls Setup() right after Instantiate, then SpawnCards calls
///     RecordHome() after the layout settles (one frame after spawn).
///   - While dragging the card is reparented to the root Canvas so it draws
///     on top of everything.
///   - On release, if the card is over the bag drop zone → OnDroppedInBag fires.
///     Otherwise → OnDroppedOutsideBag fires and the card snaps back.
/// </summary>
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem_MyClass_Game : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    // ─────────────────────────────────────────────────────────────
    //  CALLBACKS — wired by Screen2_PackYourBagGame after Instantiate
    // ─────────────────────────────────────────────────────────────

    [HideInInspector] public Action OnDroppedInBag;
    [HideInInspector] public Action OnDroppedOutsideBag;

    // ─────────────────────────────────────────────────────────────
    //  VISUAL SETTINGS (tweak in Inspector on the prefab)
    // ─────────────────────────────────────────────────────────────

    [Header("Card Feel")]
    public float dragLiftScale     = 1.12f;
    public float dragRotateDeg     = 8f;        // max tilt while dragging
    public float snapBackDuration  = 0.35f;
    public float slideIntoDuration = 0.4f;

    [Header("Colours")]
    public Color cardDefaultColor = Color.white;
    public Color cardHoverColor   = new Color(0.95f, 1f,   0.85f);
    public Color cardCorrectColor = new Color(0.7f,  1f,   0.7f);
    public Color cardWrongColor   = new Color(1f,    0.7f, 0.7f);

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────────────────────

    private Image         img;
    private CanvasGroup   cg;
    private RectTransform rt;
    private Canvas        rootCanvas;
    private RectTransform bagDropZone;

    // "Home" — recorded once after spawn so snap-back always returns here
    private Vector3    originalLocalPos;
    private Vector3    originalLocalScale;
    private Quaternion originalRotation;
    private Transform  originalParent;
    private int        originalSiblingIndex;
    private bool       homeRecorded = false;     // BUG FIX: guard so we only record once

    private bool isDragging = false;
    private bool namingMode = false;

    // BUG FIX: cache the event camera so IsOverBag works on Camera-space canvases
    private Camera _eventCamera;

    // naming-mode audio
    private AudioClip   namingVoice;
    private AudioSource sfxSrc, voiceSrc;
    private AudioClip   popSfx;

    // ─────────────────────────────────────────────────────────────
    //  SETUP — called by Screen2 after Instantiate
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        img = GetComponent<Image>();
        cg  = GetComponent<CanvasGroup>();
        rt  = GetComponent<RectTransform>();
        // NOTE: rootCanvas is resolved lazily on first drag (OnPointerDown)
        // because at Awake() time the card may not yet be parented to the Canvas
        // (Screen2 instantiates without a parent, then calls SetParent after Setup).
    }

    // Resolves rootCanvas on first use — safe to call any time after SetParent.
    void EnsureRootCanvas()
    {
        if (rootCanvas != null) return;
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null) rootCanvas = c.rootCanvas;
    }

    /// <summary>
    /// Called by Screen2_PackYourBagGame right after Instantiate.
    /// Sets the visual sprite, item name label, and the bag drop zone reference.
    /// </summary>
    public void Setup(string itemName, Sprite sprite, bool isCorrect, RectTransform bagZone)
    {
        // BUG FIX: guard against unassigned sprite in Inspector — prevents the
        // NullReferenceException thrown by img.sprite = null on some Unity versions.
        if (sprite != null)
            img.sprite = sprite;
        else
            Debug.LogWarning($"[DraggableItem] Setup called with null sprite for item '{itemName}'. " +
                             "Assign a sprite to every ItemData entry in the Screen2 Inspector.");

        bagDropZone = bagZone;

        TextMeshProUGUI label = GetComponentInChildren<TextMeshProUGUI>();
        if (label) label.text = itemName;
    }

    /// <summary>
    /// Switches to naming mode — card is no longer draggable.
    /// Tapping it plays the voice clip for that item's name.
    /// Called by Screen2 for Round 3.
    /// </summary>
    public void SetNamingMode(AudioClip voice, AudioSource sfx, AudioSource voiceSource, AudioClip pop)
    {
        namingMode  = true;
        namingVoice = voice;
        sfxSrc      = sfx;
        voiceSrc    = voiceSource;
        popSfx      = pop;
    }

    /// <summary>
    /// Records the card's current local position as "home" so animations can
    /// return to it. Call this ONCE, one frame after spawn so the layout group
    /// has finished placing the card. Screen2.SpawnCards handles this timing.
    /// BUG FIX: guard prevents re-recording mid-drag.
    /// </summary>
    public void RecordHome()
    {
        if (homeRecorded) return;           // BUG FIX: only record once
        homeRecorded         = true;
        originalLocalPos     = rt.localPosition;
        originalLocalScale   = rt.localScale;
        originalRotation     = rt.localRotation;
        originalParent       = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
    }

    // ─────────────────────────────────────────────────────────────
    //  POINTER EVENTS
    // ─────────────────────────────────────────────────────────────

    public void OnPointerDown(PointerEventData e)
    {
        if (namingMode)
        {
            // Round 3 — tap to hear item name, no dragging
            StartCoroutine(TapPop());
            if (sfxSrc  && popSfx)      sfxSrc.PlayOneShot(popSfx);
            if (voiceSrc && namingVoice) { voiceSrc.Stop(); voiceSrc.clip = namingVoice; voiceSrc.Play(); }
            return;
        }

        if (!homeRecorded) return;

        EnsureRootCanvas();
        if (rootCanvas == null) return;  // safety: not in a canvas yet

        isDragging   = true;
        _eventCamera = e.pressEventCamera;

        transform.SetParent(rootCanvas.transform, true);
        transform.SetAsLastSibling();

        StartCoroutine(LiftAnimation());
    }

    public void OnDrag(PointerEventData e)
    {
        if (!isDragging) return;

        _eventCamera = e.pressEventCamera; // keep camera up to date

        // Follow the pointer in canvas-local space
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.GetComponent<RectTransform>(),
            e.position, _eventCamera, out localPoint);

        rt.localPosition = localPoint;

        // Tilt card based on horizontal drag velocity — feels natural
        float tilt = Mathf.Clamp(e.delta.x * 0.5f, -dragRotateDeg, dragRotateDeg);
        rt.localRotation = Quaternion.Euler(0, 0, -tilt);

        // Highlight green when hovering over the bag drop zone
        img.color = IsOverBag(e.position) ? cardHoverColor : cardDefaultColor;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (!isDragging) return;
        isDragging = false;

        rt.localRotation = originalRotation;
        img.color        = cardDefaultColor;

        if (bagDropZone != null && IsOverBag(e.position))
        {
            OnDroppedInBag?.Invoke();
        }
        else
        {
            OnDroppedOutsideBag?.Invoke();
        }
    }

    // BUG FIX: was passing null as camera — now uses cached _eventCamera.
    // Passing null only works on Screen Space – Overlay. With Screen Space – Camera
    // the hit detection would silently fail. Using the cached camera is always correct.
    bool IsOverBag(Vector2 screenPos)
    {
        if (bagDropZone == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(
            bagDropZone, screenPos, _eventCamera);
    }

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC ANIMATIONS — called by Screen2_PackYourBagGame
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Pop-in animation played when a card is first spawned.
    /// Screen2 waits one frame after this before calling RecordHome()
    /// so the layout group finishes placing the card first.
    /// </summary>
    public IEnumerator SpawnAnimation()
    {
        // BUG FIX: originalLocalScale is only set by RecordHome(), which runs AFTER
        // SpawnAnimation returns. So we must snapshot the scale here — BEFORE zeroing
        // it — otherwise every Lerp uses (0→0) and the card stays invisible.
        Vector3 targetScale = rt.localScale;   // the prefab's authored scale
        rt.localScale = Vector3.zero;

        // BUG FIX: if Screen2 zeroed our alpha before Instantiate to prevent the
        // default prefab flash, we fade it back in here as the card pops in.
        CanvasGroup spawnCG = GetComponent<CanvasGroup>();

        float dur = 0.42f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float p  = Mathf.Clamp01(elapsed / dur);
            float s  = SpringOvershoot(p);
            rt.localScale = targetScale * s;
            if (spawnCG) spawnCG.alpha = Mathf.Clamp01(p / 0.25f); // fully opaque by 25% of anim
            yield return null;
        }
        rt.localScale = targetScale;
        if (spawnCG) spawnCG.alpha = 1f;

        // Wait one extra frame so the HorizontalLayoutGroup finishes repositioning
        // the card before we record its home position.
        yield return null;
    }

    /// <summary>
    /// Smoothly slides the card back to its original position.
    /// Called when the card is dropped outside the bag.
    /// </summary>
    public IEnumerator SnapBackAnimation()
    {
        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);

        float   dur        = snapBackDuration;
        float   elapsed    = 0f;
        Vector3 startPos   = rt.localPosition;
        Vector3 startScale = rt.localScale;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t  = EaseOut(elapsed / dur);
            rt.localPosition = Vector3.Lerp(startPos,   originalLocalPos,   t);
            rt.localScale    = Vector3.Lerp(startScale, originalLocalScale, t);
            yield return null;
        }

        rt.localPosition = originalLocalPos;
        rt.localScale    = originalLocalScale;
        rt.localRotation = originalRotation;
    }

    /// <summary>
    /// Red flash + bounce back — played when a wrong item is dropped into the bag.
    /// </summary>
    public IEnumerator BounceBackAnimation()
    {
        img.color = cardWrongColor;

        transform.SetParent(originalParent, true);
        transform.SetSiblingIndex(originalSiblingIndex);

        Vector3 start   = rt.localPosition;
        float   dur     = 0.5f;
        float   elapsed = 0f;

        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float p  = elapsed / dur;
            float sp = SpringOvershoot(p);
            rt.localPosition = Vector3.Lerp(start, originalLocalPos, EaseOut(p));
            rt.localScale    = originalLocalScale * (0.9f + 0.1f * sp);
            yield return null;
        }

        rt.localPosition = originalLocalPos;
        rt.localScale    = originalLocalScale;

        // Hold red briefly, then fade back to white
        yield return new WaitForSeconds(0.25f);
        float fade = 0f;
        while (fade < 0.35f)
        {
            fade      += Time.deltaTime;
            img.color  = Color.Lerp(cardWrongColor, cardDefaultColor, fade / 0.35f);
            yield return null;
        }
        img.color = cardDefaultColor;
    }

    /// <summary>
    /// Slides the card into the bag and shrinks it to nothing.
    /// onComplete fires at the end so Screen2 can destroy the card and
    /// check whether the round is now complete.
    /// </summary>
    public IEnumerator SlideIntoBagAnimation(RectTransform bag, Action onComplete)
    {
        img.color = cardCorrectColor;

        Vector3 startPos   = rt.position;
        Vector3 targetPos  = bag.position;
        Vector3 startScale = rt.localScale;
        float   dur        = slideIntoDuration;
        float   elapsed    = 0f;

        while (elapsed < dur)
        {
            elapsed    += Time.deltaTime;
            float t     = EaseIn(elapsed / dur);
            rt.position   = Vector3.Lerp(startPos,   targetPos,   t);
            rt.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────
    //  PRIVATE ANIMATIONS
    // ─────────────────────────────────────────────────────────────

    IEnumerator LiftAnimation()
    {
        float dur     = 0.12f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float t  = EaseOut(elapsed / dur);
            rt.localScale = Vector3.Lerp(originalLocalScale, originalLocalScale * dragLiftScale, t);
            yield return null;
        }
        rt.localScale = originalLocalScale * dragLiftScale;
    }

    IEnumerator TapPop()
    {
        Vector3 orig    = rt.localScale;
        float   dur     = 0.2f;
        float   elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s  = 1f + 0.3f * Mathf.Sin(elapsed / dur * Mathf.PI);
            rt.localScale = orig * s;
            yield return null;
        }
        rt.localScale = orig;
    }

    // ─────────────────────────────────────────────────────────────
    //  EASING HELPERS
    // ─────────────────────────────────────────────────────────────

    float EaseOut(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
    float EaseIn(float t)  => Mathf.Pow(Mathf.Clamp01(t), 2f);

    // Goes 0 → 1.15 at 70% then settles to 1.0 — gives the pop-in overshoot feel
    float SpringOvershoot(float p)
    {
        if (p < 0.7f) return Mathf.Lerp(0f,    1.15f, p / 0.7f);
        return              Mathf.Lerp(1.15f, 1f,    (p - 0.7f) / 0.3f);
    }
}