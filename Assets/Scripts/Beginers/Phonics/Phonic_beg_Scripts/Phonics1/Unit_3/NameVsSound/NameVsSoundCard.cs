using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameVsSoundCard : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text displayLetterText;
    [SerializeField] private Button nameButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private TMP_Text nameLabelText;
    [SerializeField] private TMP_Text soundLabelText;
    [SerializeField] private Image nameGlowHighlight;
    [SerializeField] private Image soundGlowHighlight;

    [Header("Visual Feedback Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color activeColor = new Color(1f, 0.92f, 0.4f, 1f);

    private NameVsSoundData currentData;
    private System.Action<NameVsSoundCard, bool> onButtonTapped; // bool: true = Name, false = Sound

    private Vector3 nameInitialScale = Vector3.one;
    private Quaternion nameInitialRotation = Quaternion.identity;
    private Vector3 soundInitialScale = Vector3.one;
    private Quaternion soundInitialRotation = Quaternion.identity;
    private bool hasCachedTransforms = false;

    public NameVsSoundData CurrentData => currentData;
    public bool HasTappedName { get; private set; }
    public bool HasTappedSound { get; private set; }

    private void Awake()
    {
        CacheInitialTransforms();
    }

    private void CacheInitialTransforms()
    {
        if (hasCachedTransforms) return;

        if (nameButton != null)
        {
            nameInitialScale = nameButton.transform.localScale;
            nameInitialRotation = nameButton.transform.localRotation;
        }

        if (soundButton != null)
        {
            soundInitialScale = soundButton.transform.localScale;
            soundInitialRotation = soundButton.transform.localRotation;
        }

        hasCachedTransforms = true;
    }

    public void Setup(NameVsSoundData data, System.Action<NameVsSoundCard, bool> callback)
    {
        CacheInitialTransforms();
        currentData = data;
        onButtonTapped = callback;
        HasTappedName = false;
        HasTappedSound = false;

        ResetTransforms();

        if (displayLetterText != null) displayLetterText.text = data.letter;
        if (nameLabelText != null) nameLabelText.text = data.letterNameText;
        if (soundLabelText != null) soundLabelText.text = data.letterSoundText;

        if (nameGlowHighlight != null) nameGlowHighlight.enabled = false;
        if (soundGlowHighlight != null) soundGlowHighlight.enabled = false;

        if (nameButton != null)
        {
            nameButton.onClick.RemoveAllListeners();
            nameButton.onClick.AddListener(() =>
            {
                HasTappedName = true;
                HighlightButton(true);
                onButtonTapped?.Invoke(this, true);
            });
        }

        if (soundButton != null)
        {
            soundButton.onClick.RemoveAllListeners();
            soundButton.onClick.AddListener(() =>
            {
                HasTappedSound = true;
                HighlightButton(false);
                onButtonTapped?.Invoke(this, false);
            });
        }
    }

    public void HighlightButton(bool isName)
    {
        CacheInitialTransforms();

        if (nameGlowHighlight != null) nameGlowHighlight.enabled = isName;
        if (soundGlowHighlight != null) soundGlowHighlight.enabled = !isName;

        StopAllCoroutines();
        ResetTransforms();

        Transform targetTransform = isName ? (nameButton != null ? nameButton.transform : null) : (soundButton != null ? soundButton.transform : null);
        Quaternion baseRotation = isName ? nameInitialRotation : soundInitialRotation;

        if (targetTransform != null)
        {
            StartCoroutine(AnimateWiggle(targetTransform, baseRotation));
        }
    }

    public void ResetHighlights()
    {
        if (nameGlowHighlight != null) nameGlowHighlight.enabled = false;
        if (soundGlowHighlight != null) soundGlowHighlight.enabled = false;
        ResetTransforms();
    }

    public void ResetTransforms()
    {
        if (!hasCachedTransforms) return;

        if (nameButton != null)
        {
            nameButton.transform.localScale = nameInitialScale;
            nameButton.transform.localRotation = nameInitialRotation;
        }

        if (soundButton != null)
        {
            soundButton.transform.localScale = soundInitialScale;
            soundButton.transform.localRotation = soundInitialRotation;
        }
    }

    private IEnumerator AnimateWiggle(Transform targetTransform, Quaternion baseRotation)
    {
        if (targetTransform == null) yield break;

        float duration = 0.35f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            // Rotational wiggle side to side without size/scale increase (matches other buttons wiggle)
            float angle = Mathf.Sin(progress * Mathf.PI * 4f) * 12f * (1f - progress);
            targetTransform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        targetTransform.localRotation = baseRotation;
    }
}
