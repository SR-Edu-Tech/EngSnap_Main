using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundSafariTile : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Image keywordImage;
    [SerializeField] private TMP_Text keywordText;
    [SerializeField] private Image checkmarkIcon;
    [SerializeField] private Image glowHighlight;
    [SerializeField] private Button tileButton;

    [Header("Wiggle Animation")]
    [SerializeField] private float wiggleDuration = 0.45f;

    private SoundSafariData currentData;
    private System.Action<SoundSafariTile> onTileTapped;

    private CanvasGroup canvasGroup;
    private Vector3 initialScale;
    private Quaternion initialRotation;
    private Coroutine wiggleCoroutine;

    public SoundSafariData CurrentData => currentData;
    public bool IsExplored { get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        initialScale = transform.localScale;
        initialRotation = transform.localRotation;
    }

    public void Setup(SoundSafariData data, System.Action<SoundSafariTile> callback)
    {
        currentData = data;
        onTileTapped = callback;
        IsExplored = false;

        if (letterText != null) letterText.text = data.letter;
        if (keywordText != null) keywordText.text = data.keyword;
        if (keywordImage != null) keywordImage.sprite = data.keywordSprite;
        if (checkmarkIcon != null) checkmarkIcon.enabled = false;
        if (glowHighlight != null) glowHighlight.enabled = false;
        if (canvasGroup != null) canvasGroup.alpha = 1.0f;

        if (tileButton != null)
        {
            ColorBlock cb = tileButton.colors;
            cb.disabledColor = Color.white;
            tileButton.colors = cb;

            tileButton.onClick.RemoveAllListeners();
            tileButton.onClick.AddListener(() =>
            {
                MarkExplored();
                PlayWiggle();
                onTileTapped?.Invoke(this);
            });
        }
    }

    public void MarkExplored()
    {
        IsExplored = true;
        if (checkmarkIcon != null) checkmarkIcon.enabled = true;
        if (canvasGroup != null) canvasGroup.alpha = 0.5f;
    }

    public void SetHighlight(bool active)
    {
        if (glowHighlight != null) glowHighlight.enabled = active;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = active ? 1.0f : (IsExplored ? 0.5f : 1.0f);
        }
        if (active) PlayWiggle();
    }

    public void PlayWiggle()
    {
        if (wiggleCoroutine != null) StopCoroutine(wiggleCoroutine);
        wiggleCoroutine = StartCoroutine(WiggleCoroutine());
    }

    private IEnumerator WiggleCoroutine()
    {
        if (initialScale == Vector3.zero) initialScale = transform.localScale;

        float elapsed = 0f;

        while (elapsed < wiggleDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / wiggleDuration;

            // Scale: pop up then return (MeetPhonics math)
            float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.25f;
            transform.localScale = initialScale * scaleFactor;

            // Rotation: tilt left then right then back (MeetPhonics math)
            float rotZ = Mathf.Sin(percent * Mathf.PI * 2f) * 10f;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            yield return null;
        }

        transform.localScale = initialScale;
        transform.localRotation = initialRotation;
        wiggleCoroutine = null;
    }
}
