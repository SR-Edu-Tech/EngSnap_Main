using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoundBox : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Image boxBackgroundImage;
    [SerializeField] private Image glowHighlight;
    [SerializeField] private Button boxButton;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeGlowColor = new Color(0.4f, 0.8f, 1f, 1f);

    [Header("Wiggle Animation")]
    [SerializeField] private float wiggleDuration = 0.45f;

    private RectTransform rectTransform;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalWorldPosition;
    private AudioClip soundClip;
    private System.Action<SoundBox> onBoxTapped;

    private Vector3 initialScale;
    private Quaternion initialRotation;
    private Coroutine wiggleCoroutine;
    private bool isInitialized = false;

    public bool HasBeenTapped { get; private set; }
    public AudioClip SoundClip => soundClip;

    private void CacheInitialTransform()
    {
        if (isInitialized) return;

        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalAnchoredPosition = rectTransform.anchoredPosition;
            originalWorldPosition = transform.position;
        }

        initialScale = transform.localScale;
        initialRotation = transform.localRotation;

        isInitialized = true;
    }

    private void Awake()
    {
        CacheInitialTransform();
    }

    public void Setup(string letter, AudioClip clip, System.Action<SoundBox> callback)
    {
        CacheInitialTransform();
        soundClip = clip;
        onBoxTapped = callback;
        HasBeenTapped = false;

        if (letterText != null) letterText.text = letter;
        if (glowHighlight != null) glowHighlight.enabled = false;

        ResetPosition();

        if (boxButton != null)
        {
            boxButton.onClick.RemoveAllListeners();
            boxButton.onClick.AddListener(() =>
            {
                HasBeenTapped = true;
                PlayWiggle();
                onBoxTapped?.Invoke(this);
            });
        }
    }

    public void SetHighlight(bool active)
    {
        if (glowHighlight != null) glowHighlight.enabled = active;
        if (active) PlayWiggle();
    }

    public void PlayWiggle()
    {
        CacheInitialTransform();
        if (wiggleCoroutine != null) StopCoroutine(wiggleCoroutine);
        wiggleCoroutine = StartCoroutine(WiggleCoroutine());
    }

    private IEnumerator WiggleCoroutine()
    {
        CacheInitialTransform();

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

    public void ResetPosition()
    {
        CacheInitialTransform();

        // Restore layout group participation if modified
        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null) layoutElement.ignoreLayout = false;

        if (rectTransform != null) rectTransform.anchoredPosition = originalAnchoredPosition;
        if (glowHighlight != null) glowHighlight.enabled = false;
        HasBeenTapped = false;

        if (initialScale != Vector3.zero) transform.localScale = initialScale;
        transform.localRotation = initialRotation;
    }

    public IEnumerator SlideToPosition(Vector3 targetWorldPos, float duration = 0.55f)
    {
        CacheInitialTransform();

        // Prevent LayoutGroup from forcing position back to layout slot during sliding animation
        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null) layoutElement = gameObject.AddComponent<LayoutElement>();
        if (layoutElement != null) layoutElement.ignoreLayout = true;

        Vector3 startWorldPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
            yield return null;
        }

        transform.position = targetWorldPos;
    }
}
