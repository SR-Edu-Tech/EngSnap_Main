using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SoundAndLetterCard : MonoBehaviour
{
    [Header("UI Visuals")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private Image cardImage;
    [SerializeField] private GameObject glowEffect;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Confetti Burst")]
    [SerializeField] private GameObject confettiPrefab;
    [SerializeField] private float confettiLifetime = 2.5f;

    private SoundAndLetterChoice choiceData;
    private SoundAndLetterController controller;
    private Button button;

    private Coroutine animCoroutine;
    private Vector3 initialScale;
    private Quaternion initialRotation;

    private CanvasGroup canvasGroup;
    private Image rootImage;
    private Color initialLetterTextColor = Color.white;
    private Color initialCardImageColor = Color.white;
    private Color initialRootImageColor = Color.white;
    private bool hasCachedColors = false;

    public SoundAndLetterChoice ChoiceData => choiceData;
    public string Letter => choiceData != null ? choiceData.letter : "";

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnCardClicked);
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        rootImage = GetComponent<Image>();

        initialScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        initialRotation = transform.localRotation;

        CacheInitialColors();
    }

    private Color initialGlowColor = Color.white;

    private void CacheInitialColors()
    {
        if (hasCachedColors) return;

        if (letterText != null) initialLetterTextColor = letterText.color;
        if (cardImage != null) initialCardImageColor = cardImage.color;
        if (rootImage != null) initialRootImageColor = rootImage.color;
        if (glowEffect != null)
        {
            Graphic glowGraphic = glowEffect.GetComponent<Graphic>();
            if (glowGraphic != null) initialGlowColor = glowGraphic.color;
        }

        hasCachedColors = true;
    }

    public void Setup(SoundAndLetterChoice choice, SoundAndLetterController mainController)
    {
        choiceData = choice;
        controller = mainController;

        // Ensure the card button GameObject itself stays active
        gameObject.SetActive(true);

        if (choiceData != null)
        {
            bool hasPicture = choiceData.isPictureCard && choiceData.imageSprite != null;

            if (cardImage != null)
            {
                // Only toggle child GameObject if cardImage is not on the root button itself
                if (cardImage.gameObject != gameObject)
                {
                    cardImage.gameObject.SetActive(hasPicture);
                }
                cardImage.sprite = choiceData.imageSprite;
                cardImage.enabled = hasPicture;
            }

            if (letterText != null)
            {
                if (hasPicture)
                {
                    letterText.gameObject.SetActive(false);
                    letterText.text = "";
                }
                else
                {
                    letterText.gameObject.SetActive(true);
                    letterText.text = choiceData.letter ?? "";
                }
            }
        }

        SetInteractable(true);
        ResetVisualState();
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

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = state;
        }
    }

    public void SetGlow(bool active)
    {
        if (glowEffect != null)
        {
            glowEffect.SetActive(active);
        }
    }

    public void SetHighlighted(bool highlight)
    {
        if (highlight) SetGreenHighlight();
        else ResetVisualState();
    }

    public void SetGreenHighlight()
    {
        CacheInitialColors();
        SetGlow(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        Color greenTint = new Color(0.45f, 0.95f, 0.45f, 1f);
        if (rootImage != null) rootImage.color = initialRootImageColor * greenTint;
        if (cardImage != null) cardImage.color = initialCardImageColor * greenTint;

        if (glowEffect != null)
        {
            Graphic glowGraphic = glowEffect.GetComponent<Graphic>();
            if (glowGraphic != null) glowGraphic.color = new Color(0.3f, 0.95f, 0.3f, 1f);
        }
    }

    public void SetRedHighlight()
    {
        CacheInitialColors();
        SetGlow(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        Color redTint = new Color(0.95f, 0.45f, 0.45f, 1f);
        if (rootImage != null) rootImage.color = initialRootImageColor * redTint;
        if (cardImage != null) cardImage.color = initialCardImageColor * redTint;

        if (glowEffect != null)
        {
            Graphic glowGraphic = glowEffect.GetComponent<Graphic>();
            if (glowGraphic != null) glowGraphic.color = new Color(0.95f, 0.3f, 0.3f, 1f);
        }
    }

    public void SetGrayedOut(bool grayedOut)
    {
        CacheInitialColors();
        if (grayedOut)
        {
            SetGlow(false);
            if (canvasGroup != null) canvasGroup.alpha = 0.4f;

            Color grayMultiplier = new Color(0.65f, 0.65f, 0.65f, 1f);
            if (rootImage != null) rootImage.color = initialRootImageColor * grayMultiplier;
            if (cardImage != null) cardImage.color = initialCardImageColor * grayMultiplier;
            if (letterText != null) letterText.color = initialLetterTextColor * grayMultiplier;
        }
        else
        {
            ResetVisualState();
        }
    }

    public void ResetVisualState()
    {
        CacheInitialColors();
        SetGlow(false);
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (rootImage != null) rootImage.color = initialRootImageColor;
        if (cardImage != null) cardImage.color = initialCardImageColor;
        if (letterText != null) letterText.color = initialLetterTextColor;

        if (glowEffect != null)
        {
            Graphic glowGraphic = glowEffect.GetComponent<Graphic>();
            if (glowGraphic != null) glowGraphic.color = initialGlowColor;
        }
    }

    private void OnCardClicked()
    {
        if (choiceData == null) return;
        if (controller != null && controller.IsTransitioning) return;

        if (controller != null)
        {
            controller.OnCardSelected(this);
        }
    }

    public void PlayDanceAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Dance");
        }
        else
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(DanceCoroutine());
        }

        SpawnConfetti();
    }

    public void PlayWiggleAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Shake");
        }
        else
        {
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(WiggleCoroutine());
        }
    }

    public void TriggerConfetti()
    {
        SpawnConfetti();
    }

    private void SpawnConfetti()
    {
        if (confettiPrefab == null) return;

        Transform parent = transform.parent != null ? transform.parent : transform;
        GameObject confetti = Instantiate(confettiPrefab, parent);

        RectTransform myRect = GetComponent<RectTransform>();
        RectTransform confettiRect = confetti.GetComponent<RectTransform>();

        if (myRect != null && confettiRect != null)
        {
            confettiRect.anchoredPosition = myRect.anchoredPosition;
            confettiRect.sizeDelta = myRect.sizeDelta;
            confettiRect.localScale = Vector3.one;
        }
        else
        {
            confetti.transform.position = transform.position;
        }

        Destroy(confetti, confettiLifetime);
    }

    private IEnumerator DanceCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / animationDuration;

            // Bounce scale & happy rotation
            float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.35f;
            transform.localScale = initialScale * scaleFactor;

            float rotZ = Mathf.Sin(percent * Mathf.PI * 4f) * 12f;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            yield return null;
        }

        ResetTransform();
        animCoroutine = null;
    }

    private IEnumerator WiggleCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / animationDuration;

            // Shake side to side
            float rotZ = Mathf.Sin(percent * Mathf.PI * 6f) * 15f;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            yield return null;
        }

        ResetTransform();
        animCoroutine = null;
    }

    private void ResetTransform()
    {
        transform.localScale = initialScale;
        transform.localRotation = initialRotation;
    }
}
