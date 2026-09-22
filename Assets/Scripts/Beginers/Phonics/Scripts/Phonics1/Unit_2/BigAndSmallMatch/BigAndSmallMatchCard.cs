using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BigAndSmallMatchCard : MonoBehaviour
{
    public enum LetterType { Capital, Small }

    [Header("UI Visuals")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private GameObject glowEffect;
    [SerializeField] private Image cardImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.84f, 0f, 1f); // Vibrant Golden highlight

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.4f;

    private LetterType type;
    private string letterValue;
    private BigAndSmallMatchController controller;
    private Button button;

    private Image rootImage;
    private Color initialRootColor = Color.white;
    private Color initialGlowColor = Color.white;
    private bool hasCachedColor = false;

    private Coroutine animCoroutine;
    private Vector3 initialScale;
    private Quaternion initialRotation;

    public LetterType Type => type;
    public string LetterValue => letterValue;

    private void Awake()
    {
        selectedColor = new Color(1f, 0.84f, 0f, 1f);
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnCardClicked);
        }

        rootImage = GetComponent<Image>();
        if (rootImage != null && !hasCachedColor)
        {
            initialRootColor = rootImage.color;
            hasCachedColor = true;
        }

        if (glowEffect != null)
        {
            Graphic glowGraphic = glowEffect.GetComponent<Graphic>();
            if (glowGraphic != null) initialGlowColor = glowGraphic.color;
        }

        initialScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        initialRotation = transform.localRotation;
    }

    public void Setup(string letter, LetterType letterType, BigAndSmallMatchController mainController)
    {
        letterValue = letter;
        type = letterType;
        controller = mainController;

        gameObject.SetActive(true);

        if (letterText != null)
        {
            letterText.text = letter ?? "";
        }

        SetInteractable(true);
        SetGlow(false);
        ResetTransform();
    }

    public void SetInteractable(bool state)
    {
        if (button != null)
        {
            ColorBlock cb = button.colors;
            cb.disabledColor = Color.white;
            button.colors = cb;
            button.interactable = state;
        }
    }

    public void SetGlow(bool active)
    {
        if (glowEffect != null)
        {
            glowEffect.SetActive(active);
            Graphic glowGraphic = glowEffect.GetComponent<Graphic>();
            if (glowGraphic != null)
            {
                glowGraphic.color = active ? new Color(1f, 0.84f, 0f, 1f) : initialGlowColor;
            }
        }

        Image targetImage = cardImage != null ? cardImage : rootImage;
        if (targetImage != null)
        {
            targetImage.color = active ? selectedColor : initialRootColor;
        }
    }

    private void OnCardClicked()
    {
        if (controller != null && controller.IsTransitioning) return;

        if (controller != null)
        {
            controller.OnCardSelected(this);
        }
    }

    public void PlayMatchAnimation()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(MatchCoroutine());
    }

    public void PlayMismatchAnimation()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(MismatchCoroutine());
    }

    private void ResetTransform()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }

        transform.localScale = initialScale;
        transform.localRotation = initialRotation;
    }

    private IEnumerator MatchCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / animationDuration;

            float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.35f;
            transform.localScale = initialScale * scaleFactor;

            float rotZ = Mathf.Sin(percent * Mathf.PI * 4f) * 10f;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            yield return null;
        }

        ResetTransform();
    }

    private IEnumerator MismatchCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / animationDuration;

            float rotZ = Mathf.Sin(percent * Mathf.PI * 6f) * 12f;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            yield return null;
        }

        ResetTransform();
    }
}
