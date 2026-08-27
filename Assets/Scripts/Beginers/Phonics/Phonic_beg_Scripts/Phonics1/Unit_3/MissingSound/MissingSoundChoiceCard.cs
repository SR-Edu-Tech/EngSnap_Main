using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissingSoundChoiceCard : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text choiceLetterText;
    [SerializeField] private Image cardBackgroundImage;
    [SerializeField] private Button cardButton;
    [SerializeField] private Image glowHighlight;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color correctColor = new Color(0.4f, 0.9f, 0.4f, 1f);
    [SerializeField] private Color incorrectColor = new Color(0.95f, 0.4f, 0.4f, 1f);

    [Header("Wiggle Animation Settings")]
    [SerializeField] private float wiggleDuration = 0.4f;
    [SerializeField] private float wiggleAngle = 10f;
    [SerializeField] private float scaleBoost = 0.2f;

    private string letter;
    private System.Action<MissingSoundChoiceCard> onChoiceSelected;

    private Vector3 initialScale = Vector3.one;
    private Quaternion initialRotation = Quaternion.identity;
    private bool hasCachedTransform = false;
    private Coroutine wiggleCoroutine;

    public string Letter => letter;

    private void Awake()
    {
        CacheInitialTransform();
    }

    private void CacheInitialTransform()
    {
        if (!hasCachedTransform)
        {
            initialScale = transform.localScale;
            initialRotation = transform.localRotation;
            hasCachedTransform = true;
        }
    }

    public void Setup(string choiceLetter, System.Action<MissingSoundChoiceCard> callback)
    {
        CacheInitialTransform();
        letter = choiceLetter;
        onChoiceSelected = callback;

        if (choiceLetterText != null) choiceLetterText.text = choiceLetter;
        if (cardBackgroundImage != null) cardBackgroundImage.color = normalColor;
        if (glowHighlight != null) glowHighlight.enabled = false;

        ResetTransform();

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() =>
            {
                PlayWiggle();
                onChoiceSelected?.Invoke(this);
            });
        }
    }

    public void SetState(bool isCorrect)
    {
        if (cardBackgroundImage != null)
        {
            cardBackgroundImage.color = isCorrect ? correctColor : incorrectColor;
        }
        PlayWiggle();
    }

    public void ResetState()
    {
        if (cardBackgroundImage != null) cardBackgroundImage.color = normalColor;
        if (glowHighlight != null) glowHighlight.enabled = false;
        ResetTransform();
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

            // Scale bounce
            float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * scaleBoost;
            transform.localScale = initialScale * scaleFactor;

            // Wiggle tilt left and right
            float rotZ = Mathf.Sin(percent * Mathf.PI * 2f) * wiggleAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            yield return null;
        }

        ResetTransform();
        wiggleCoroutine = null;
    }

    private void ResetTransform()
    {
        if (wiggleCoroutine != null)
        {
            StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = null;
        }
        if (hasCachedTransform)
        {
            transform.localScale = initialScale;
            transform.localRotation = initialRotation;
        }
    }
}
