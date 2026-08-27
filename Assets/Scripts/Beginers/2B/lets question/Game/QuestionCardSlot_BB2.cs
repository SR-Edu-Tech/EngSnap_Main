using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One of the 6 FIXED question cards in the left column (not spawned —
/// place 6 of these directly in the scene, one per question).
/// (Add Component → search "QuestionCardSlot_BB2")
/// </summary>
public class QuestionCardSlot_BB2 : MonoBehaviour, IDropHandler
{
    [Header("UI Refs")]
    public TMP_Text label;
    public Image    background;

    public int PairIndex { get; private set; }

    private System.Action<AnswerCard_BB2, QuestionCardSlot_BB2> _onCardDropped;
    private Color _originalColor;

    public void Initialise(int pairIndex, string questionText, Color tintColor, System.Action<AnswerCard_BB2, QuestionCardSlot_BB2> onCardDropped)
    {
        PairIndex      = pairIndex;
        _onCardDropped = onCardDropped;

        if (label != null)      label.text = questionText;
        if (background != null) background.color = tintColor;
        _originalColor = tintColor;
    }

    public Color TintColor => _originalColor;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null) return;
        var card = eventData.pointerDrag.GetComponent<AnswerCard_BB2>();
        if (card == null || card.IsPlaced) return;
        _onCardDropped?.Invoke(card, this);
    }
}
