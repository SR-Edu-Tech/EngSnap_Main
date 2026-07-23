using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  FriendSlot_BB2 — one friend position in the circle, drop target for
//  GiftBox_BB2. Pre-place 7 of these directly in the scene, arranged in
//  the circle layout matching the book's page-12 illustration — they are
//  NOT instantiated at runtime, only Initialise()'d with data.
//
//  IMPORTANT: the Image used for raycasting (friendImage or backgroundGlow)
//  must have "Raycast Target" enabled, or OnDrop will never fire.
// ─────────────────────────────────────────────────────────────────────────

public class FriendSlot_BB2 : MonoBehaviour, IDropHandler
{
    [Header("References")]
    [SerializeField] private Image friendImage;
    [SerializeField] private Image backgroundGlow;
    [Tooltip("Where the gift box docks when dropped here. Defaults to this object's own RectTransform if left empty.")]
    [SerializeField] private RectTransform dockAnchor;

    [Header("Colours")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private Color activeColor = new Color(1f, 0.9f, 0.4f, 0.9f);
    [SerializeField] private Color wishedColor = new Color(0.6f, 1f, 0.65f, 0.9f);

    [Header("Timing")]
    [SerializeField] private float tintDuration = 0.15f;
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private float popDuration = 0.18f;

    public int SlotIndex { get; private set; }
    public bool IsWished { get; private set; }
    public RectTransform DockAnchor => dockAnchor != null ? dockAnchor : (RectTransform)transform;

    private System.Action<FriendSlot_BB2> _onGiftDropped;
    private Coroutine _tintCo;
    private Coroutine _popCo;
    private Vector3 _originalScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
        if (backgroundGlow != null) backgroundGlow.color = idleColor;
    }

    public void Initialise(int slotIndex, FriendData_BB2 data, System.Action<FriendSlot_BB2> onGiftDropped)
    {
        SlotIndex = slotIndex;
        IsWished = false;
        _onGiftDropped = onGiftDropped;

        if (friendImage != null && data != null)
            friendImage.sprite = data.friendSprite;

        transform.localScale = _originalScale;
        if (backgroundGlow != null) backgroundGlow.color = idleColor;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (IsWished) return;
        if (eventData.pointerDrag == null) return;
        if (eventData.pointerDrag.GetComponent<GiftBox_BB2>() == null) return;

        _onGiftDropped?.Invoke(this);
    }

    public void SetActiveHighlight(bool active)
    {
        if (IsWished) return;
        TintTo(active ? activeColor : idleColor);
        PopTo(active ? popScale : 1f);
    }

    public void SetWished()
    {
        IsWished = true;
        TintTo(wishedColor);
        PopTo(1f);
    }

    private void TintTo(Color target)
    {
        if (backgroundGlow == null) return;
        if (_tintCo != null) StopCoroutine(_tintCo);
        _tintCo = StartCoroutine(TintCoroutine(target));
    }

    private IEnumerator TintCoroutine(Color target)
    {
        Color start = backgroundGlow.color;
        float t = 0f;
        while (t < tintDuration)
        {
            t += Time.deltaTime;
            backgroundGlow.color = Color.Lerp(start, target, t / tintDuration);
            yield return null;
        }
        backgroundGlow.color = target;
    }

    private void PopTo(float targetMult)
    {
        if (_popCo != null) StopCoroutine(_popCo);
        _popCo = StartCoroutine(PopCoroutine(targetMult));
    }

    private IEnumerator PopCoroutine(float targetMult)
    {
        Vector3 target = _originalScale * targetMult;
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, target, t / popDuration);
            yield return null;
        }
        transform.localScale = target;
    }
}



