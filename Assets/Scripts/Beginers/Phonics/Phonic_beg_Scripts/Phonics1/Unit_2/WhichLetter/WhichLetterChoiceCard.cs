using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class WhichLetterChoiceCard : MonoBehaviour
{
    [Header("UI Visuals")]
    [SerializeField] private TMP_Text letterText;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Confetti Burst")]
    [SerializeField] private GameObject confettiPrefab;
    [SerializeField] private float confettiLifetime = 2.5f;

    private WhichLetterChoice choiceData;
    private WhichLetterController controller;
    private Button button;

    private Coroutine animCoroutine;
    private Vector3 initialScale;
    private Quaternion initialRotation;

    public WhichLetterChoice ChoiceData => choiceData;
    public string Letter => choiceData != null ? choiceData.letterChoice : "";

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnCardClicked);
        }

        initialScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        initialRotation = transform.localRotation;
    }

    public void Setup(WhichLetterChoice choice, WhichLetterController mainController)
    {
        choiceData = choice;
        controller = mainController;

        gameObject.SetActive(true);

        if (choiceData != null && letterText != null)
        {
            letterText.gameObject.SetActive(true);
            letterText.text = choiceData.letterChoice ?? "";
        }

        SetInteractable(true);
        ResetTransform();
    }

    public void SetInteractable(bool state)
    {
        if (button != null) button.interactable = state;
    }

    public void SetGlow(bool active)
    {
        // Optional glow effect removed
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
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(DanceCoroutine());
        SpawnConfetti();
    }

    public void PlayWiggleAnimation()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(WiggleCoroutine());
    }

    public void TriggerConfetti() => SpawnConfetti();

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

    private IEnumerator DanceCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / animationDuration;

            float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.35f;
            transform.localScale = initialScale * scaleFactor;

            float rotZ = Mathf.Sin(percent * Mathf.PI * 4f) * 12f;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            yield return null;
        }

        ResetTransform();
    }

    private IEnumerator WiggleCoroutine()
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
