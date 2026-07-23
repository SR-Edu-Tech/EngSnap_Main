using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Draggable word label.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class WordLabel_BB2 : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerDownHandler
{
    [Header("References")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image backgroundImage;

    [Header("Colours")]
    [SerializeField] private Color idleColor = new Color(1f, 0.97f, 0.85f, 1f);
    [SerializeField] private Color draggingColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private Color matchedColor = new Color(0.6f, 1f, 0.65f, 1f);

    public int PairIndex { get; private set; }
    public bool IsMatched { get; private set; }
    public bool IsDragging { get; private set; }

    private AudioClip _audioClip;

    private Action<WordLabel_BB2> _onDragBegin;
    private Action<WordLabel_BB2, Vector2> _onDragging;
    private Action<WordLabel_BB2, PointerEventData> _onDragEnd;

    private CanvasGroup _canvasGroup;
    private JuicyButton _juicy;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _juicy = GetComponent<JuicyButton>();

        if (backgroundImage != null)
            backgroundImage.color = idleColor;
    }

    public void Initialise(
        int pairIndex,
        string word,
        AudioClip wordClip,
        Action<WordLabel_BB2> onDragBegin,
        Action<WordLabel_BB2, Vector2> onDragging,
        Action<WordLabel_BB2, PointerEventData> onDragEnd)
    {
        PairIndex = pairIndex;
        IsMatched = false;
        IsDragging = false;

        _audioClip = wordClip;

        _onDragBegin = onDragBegin;
        _onDragging = onDragging;
        _onDragEnd = onDragEnd;

        if (labelText != null)
            labelText.text = word.ToUpper();

        _canvasGroup.blocksRaycasts = true;

        SetBackground(idleColor);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsMatched)
            return;

        if (_audioClip != null)
            AudioManager.Instance?.PlaySFX(_audioClip);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsMatched)
            return;

        IsDragging = true;

        _canvasGroup.blocksRaycasts = false;

        SetBackground(draggingColor);

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);

        _onDragBegin?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        _onDragging?.Invoke(this, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        IsDragging = false;

        _canvasGroup.blocksRaycasts = true;

        if (!IsMatched)
            SetBackground(idleColor);

        _onDragEnd?.Invoke(this, eventData);
    }

    public void SetMatched()
    {
        IsMatched = true;

        _canvasGroup.blocksRaycasts = false;

        SetBackground(matchedColor);

        if (_juicy != null)
        {
            _juicy.PlayCorrectAnim();
            _juicy.SetDisabled(true);
        }
    }

    public void SetWrong()
    {
        if (_juicy != null)
            _juicy.PlayWrongAnim();

        SetBackground(idleColor);
    }

    private void SetBackground(Color colour)
    {
        if (backgroundImage != null)
            backgroundImage.color = colour;
    }
}