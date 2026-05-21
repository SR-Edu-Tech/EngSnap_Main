using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// WordLabel — left-side word chip. Player drags from here to an IllustrationCard.
/// </summary>
[RequireComponent(typeof(JuicyButton))]
public class WordLabel : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    [Header("Refs")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image    backgroundImage;

    [Header("Visual States")]
    [SerializeField] private Color idleColor     = new Color(1f,  0.97f, 0.85f, 1f);
    [SerializeField] private Color draggingColor = new Color(1f,  0.85f, 0.35f, 1f);
    [SerializeField] private Color matchedColor  = new Color(0.6f, 1f,  0.65f, 1f);

    public int  PairIndex  { get; private set; }
    public bool IsMatched  { get; private set; }
    public bool IsDragging { get; private set; }

    private Action<WordLabel>                    _onDragBegin;
    private Action<WordLabel, Vector2>           _onDragging;
    private Action<WordLabel, PointerEventData>  _onDragEnd;
    private JuicyButton _juicy;

    void Awake() => _juicy = GetComponent<JuicyButton>();

    public void Initialise(int pairIndex, string word, AudioClip wordClip,
                           Action<WordLabel> onDragBegin,
                           Action<WordLabel, Vector2> onDragging,
                           Action<WordLabel, PointerEventData> onDragEnd)
    {
        PairIndex      = pairIndex;
        labelText.text = word.ToUpper();
        _onDragBegin   = onDragBegin;
        _onDragging    = onDragging;
        _onDragEnd     = onDragEnd;
        SetBg(idleColor);
    }

    // IPointerDownHandler — needed so EventSystem tracks this object for drag
    public void OnPointerDown(PointerEventData eventData) { }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsMatched) return;
        IsDragging = true;
        SetBg(draggingColor);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
        _onDragBegin?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;
        _onDragging?.Invoke(this, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDragging) return;
        IsDragging = false;
        if (!IsMatched) SetBg(idleColor);
        _onDragEnd?.Invoke(this, eventData);
    }

    public void SetMatched()
    {
        IsMatched = true;
        SetBg(matchedColor);
        _juicy.PlayCorrectAnim();
        _juicy.SetDisabled(true);
    }

    public void SetWrong()
    {
        _juicy.PlayWrongAnim();
        SetBg(idleColor);
    }

    private void SetBg(Color c)
    {
        if (backgroundImage != null) backgroundImage.color = c;
    }
}