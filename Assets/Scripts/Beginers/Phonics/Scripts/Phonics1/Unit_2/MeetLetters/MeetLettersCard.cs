using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MeetLettersCard : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private GameObject glowEffect;

    [Header("Animation Settings")]
    [SerializeField] private float wiggleDuration = 0.5f;

    [Header("Confetti Burst")]
    [SerializeField] private GameObject confettiPrefab;
    [SerializeField] private float confettiLifetime = 2.5f;

    private MeetLettersData data;
    private MeetLettersController controller;
    private Button button;

    private bool isTapped = false;
    private Coroutine wiggleCoroutine;
    private Vector3 initialScale;
    private Quaternion initialRotation;

    public MeetLettersData Data => data;
    public bool IsTapped => isTapped;

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

    public void Setup(MeetLettersData letterData, MeetLettersController mainController)
    {
        data = letterData;
        controller = mainController;
        isTapped = false;

        if (wiggleCoroutine != null)
        {
            StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = null;
        }

        if (initialScale != Vector3.zero) transform.localScale = initialScale;
        transform.localRotation = initialRotation;

        SetInteractable(true);
        SetGlow(false);

        if (letterText != null && data != null)
        {
            letterText.text = data.letterPair ?? "";
        }
    }

    public void SetInteractable(bool state)
    {
        if (button != null)
        {
            button.interactable = state;
        }
    }

    public void SetGlow(bool active)
    {
        if (glowEffect != null)
        {
            glowEffect.SetActive(active);
        }
    }

    private void OnCardClicked()
    {
        if (data == null) return;
        if (controller != null && controller.IsTransitioning) return;

        isTapped = true;

        if (wiggleCoroutine != null) StopCoroutine(wiggleCoroutine);
        wiggleCoroutine = StartCoroutine(WiggleCoroutine());

        SpawnConfetti();

        if (controller != null)
        {
            controller.OnCardTapped(this);
        }
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

        int myIndex = transform.GetSiblingIndex();
        confetti.transform.SetSiblingIndex(Mathf.Max(0, myIndex - 1));

        Destroy(confetti, confettiLifetime);
    }

    private IEnumerator WiggleCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < wiggleDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / wiggleDuration;

            float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.25f;
            transform.localScale = initialScale * scaleFactor;

            float rotZ = Mathf.Sin(percent * Mathf.PI * 2f) * 10f;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            yield return null;
        }

        transform.localScale = initialScale;
        transform.localRotation = initialRotation;
        wiggleCoroutine = null;
    }
}
