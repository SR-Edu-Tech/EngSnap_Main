using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WishCard_BB2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text wishText;
    [SerializeField] private Image background;
    [SerializeField] private Button tapButton;

    [Header("Colours")]
    [SerializeField] private Color idleColor = new Color(1f, 0.97f, 0.85f, 1f);
    [SerializeField] private Color usedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.35f, 1f);

    [Header("Wobble")]
    [SerializeField] private float wobbleAmount = 8f;
    [SerializeField] private float wobbleDuration = 0.3f;

    public int WishIndex { get; private set; }
    public bool IsUsed { get; private set; }
    public WishData_BB2 Data { get; private set; }

    private System.Action<WishCard_BB2> _onTapped;
    private JuicyButton _juicy;
    private Coroutine _wobbleCo;
    private bool _isHighlighted;

    private void Awake()
    {
        _juicy = GetComponent<JuicyButton>();

        if (tapButton != null)
        {
            tapButton.onClick.RemoveAllListeners();
            tapButton.onClick.AddListener(HandleTap);
        }
    }

    public void Initialise(int wishIndex, WishData_BB2 data, System.Action<WishCard_BB2> onTapped)
    {
        WishIndex = wishIndex;
        Data = data;
        IsUsed = false;
        _isHighlighted = false;
        _onTapped = onTapped;

        if (wishText != null) wishText.text = data.wishText;
        if (background != null) background.color = idleColor;

        if (tapButton != null) tapButton.interactable = true;
    }

    private void HandleTap() => _onTapped?.Invoke(this);

    public void SetUsed()
    {
        IsUsed = true;
        if (background != null) background.color = usedColor;
    }

    public void SetHighlighted(bool on)
    {
        if (IsUsed || _isHighlighted == on) return;
        _isHighlighted = on;
        if (background != null) background.color = on ? highlightColor : idleColor;
    }

    public void PlayWobble()
    {
        if (_juicy != null)
        {
            _juicy.PlayWrongAnim();
            return;
        }

        if (_wobbleCo != null) StopCoroutine(_wobbleCo);
        _wobbleCo = StartCoroutine(WobbleCoroutine());
    }

    private IEnumerator WobbleCoroutine()
    {
        Vector3 origin = transform.localPosition;
        float e = 0f;
        while (e < wobbleDuration)
        {
            float x = Mathf.Sin(e * Mathf.PI * 10f) * wobbleAmount * (1f - e / wobbleDuration);
            transform.localPosition = origin + new Vector3(x, 0f, 0f);
            e += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = origin;
    }
}

