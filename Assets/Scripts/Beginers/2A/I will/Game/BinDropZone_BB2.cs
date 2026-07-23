using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// ─────────────────────────────────────────────────────────────────────────
//  BinDropZone_BB2 — one of the two bins (I WILL / I WILL NOT).
//  Pre-place two of these in the scene — NOT instantiated at runtime.
//
//  IMPORTANT: needs an Image with "Raycast Target" enabled, or OnDrop
//  will never fire.
// ─────────────────────────────────────────────────────────────────────────

public class BinDropZone_BB2 : MonoBehaviour, IDropHandler
{
    [SerializeField] private BinType_BB2 binType;
    [Tooltip("Where correctly-sorted chits animate to. Defaults to this object's own RectTransform if left empty.")]
    [SerializeField] private RectTransform dockAnchor;

    public BinType_BB2 BinType => binType;
    public RectTransform DockAnchor => dockAnchor != null ? dockAnchor : (RectTransform)transform;

    private System.Action<Chit_BB2, BinType_BB2> _onChitDropped;

    public void Initialise(System.Action<Chit_BB2, BinType_BB2> onChitDropped)
    {
        _onChitDropped = onChitDropped;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;

        var chit = eventData.pointerDrag.GetComponent<Chit_BB2>();
        if (chit == null || chit.IsLocked) return;

        chit.MarkResolved();
        _onChitDropped?.Invoke(chit, binType);
    }
}