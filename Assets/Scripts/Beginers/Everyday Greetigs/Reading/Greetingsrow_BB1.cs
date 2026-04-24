using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// GreetingRow_BB1.cs
/// Manages one greeting/response pair even when those items are instantiated into separate containers.
/// </summary>
public class GreetingRow_BB1 : MonoBehaviour
{
    [Header("Greeting Item")]
    public TextMeshProUGUI greetingText;
    public AudioSource greetingAudio;
    public Image greetingBackground;
    public Button greetingButton;

    [Header("Response Item")]
    public TextMeshProUGUI responseText;
    public AudioSource responseAudio;
    public Image responseBackground;
    public Button responseButton;

    // For animation
    [HideInInspector] public RectTransform greetingRect;
    [HideInInspector] public RectTransform responseRect;

    private Greetings_BB1 manager;

    // Track active scale tweens so they don't pile up
    private Tweener greetingScaleTween;
    private Tweener responseScaleTween;

    public void Initialize(GameObject greetingObject, GameObject responseObject, string gText, AudioClip gAudio, string rText, AudioClip rAudio, Greetings_BB1 mgr)
    {
        manager = mgr;

        if (greetingObject != null)
        {
            greetingText = greetingObject.GetComponentInChildren<TextMeshProUGUI>();
            greetingAudio = greetingObject.GetComponent<AudioSource>();
            greetingBackground = greetingObject.GetComponent<Image>();
            greetingButton = greetingObject.GetComponent<Button>();
            greetingRect = greetingObject.GetComponent<RectTransform>();
        }

        if (responseObject != null)
        {
            responseText = responseObject.GetComponentInChildren<TextMeshProUGUI>();
            responseAudio = responseObject.GetComponent<AudioSource>();
            responseBackground = responseObject.GetComponent<Image>();
            responseButton = responseObject.GetComponent<Button>();
            responseRect = responseObject.GetComponent<RectTransform>();
        }

        if (greetingText != null) greetingText.text = gText;
        if (greetingAudio != null) greetingAudio.clip = gAudio;
        if (greetingBackground != null) greetingBackground.color = manager.defaultColor;

        if (responseText != null) responseText.text = rText;
        if (responseAudio != null) responseAudio.clip = rAudio;
        if (responseBackground != null) responseBackground.color = manager.defaultColor;

        if (greetingButton != null)
            greetingButton.onClick.AddListener(() => manager.OnRowTapped(this));

        if (responseButton != null)
            responseButton.onClick.AddListener(() => manager.OnRowTapped(this));
    }

    // ─────────────────────────────────────────────
    // SCALE
    // Scales both button GameObjects. Pass duration = 0 for instant snap.
    // ─────────────────────────────────────────────
    public void SetButtonScale(Vector3 targetScale, float duration = 0f)
    {
        if (greetingRect != null)
        {
            greetingScaleTween?.Kill();
            if (duration > 0f)
                greetingScaleTween = greetingRect.DOScale(targetScale, duration).SetEase(Ease.OutBack);
            else
                greetingRect.localScale = targetScale;
        }

        if (responseRect != null)
        {
            responseScaleTween?.Kill();
            if (duration > 0f)
                responseScaleTween = responseRect.DOScale(targetScale, duration).SetEase(Ease.OutBack);
            else
                responseRect.localScale = targetScale;
        }
    }

    // ─────────────────────────────────────────────
    // ANIMATE IN
    // Animate greeting from left, response from right.
    // onlyGreeting: if true, only animate greeting; onlyResponse: if true, only animate response
    // ─────────────────────────────────────────────
    public IEnumerator AnimateIn(float duration = 0.5f, float offset = 800f, bool onlyGreeting = false, bool onlyResponse = false)
    {
        if (!onlyResponse && greetingRect != null)
        {
            var startPos = greetingRect.anchoredPosition;
            greetingRect.anchoredPosition = new Vector2(-offset, startPos.y);
            greetingRect.gameObject.SetActive(true);
            var tween = greetingRect.DOAnchorPos(startPos, duration).SetEase(Ease.OutBack);
            yield return tween.WaitForCompletion();
        }
        if (!onlyGreeting && responseRect != null)
        {
            var startPos = responseRect.anchoredPosition;
            responseRect.anchoredPosition = new Vector2(offset, startPos.y);
            responseRect.gameObject.SetActive(true);
            var tween = responseRect.DOAnchorPos(startPos, duration).SetEase(Ease.OutBack);
            yield return tween.WaitForCompletion();
        }
    }

    // ─────────────────────────────────────────────
    // HIGHLIGHT & AUDIO
    // ─────────────────────────────────────────────
    public void SetHighlight(Color color)
    {
        if (greetingBackground != null)
            greetingBackground.color = color;
        if (responseBackground != null)
            responseBackground.color = color;
    }

    public void PlayGreetingAudio()
    {
        if (greetingAudio != null && greetingAudio.clip != null)
        {
            greetingAudio.Stop();   // ensure it starts from the beginning
            greetingAudio.Play();
        }
    }

    public void PlayResponseAudio()
    {
        if (responseAudio != null && responseAudio.clip != null)
        {
            responseAudio.Stop();   // ensure it starts from the beginning
            responseAudio.Play();
        }
    }
}