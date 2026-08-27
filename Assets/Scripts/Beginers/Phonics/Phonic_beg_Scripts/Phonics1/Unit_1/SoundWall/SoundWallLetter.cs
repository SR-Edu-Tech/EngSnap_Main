using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SoundWallLetter : MonoBehaviour
{
    [Header("UI Visuals")]
    [SerializeField] private TMP_Text letterText;
    [SerializeField] private GameObject glowEffect;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private float wiggleDuration = 0.5f;

    [Header("Confetti")]
    [SerializeField] private GameObject confettiPrefab;
    [SerializeField] private float confettiLifetime = 3f;

    private SoundWallLetterData data;
    private SoundWallController controller;
    private Button button;

    private bool isTapped = false;
    private Coroutine wiggleCoroutine;
    private Vector3 initialScale;
    private Quaternion initialRotation;

    public SoundWallLetterData Data => data;
    public bool IsTapped => isTapped;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnLetterTapped);
        }

        initialScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        initialRotation = transform.localRotation;
    }

    public void Setup(SoundWallLetterData letterData, SoundWallController mainController)
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
            letterText.text = data.letter ?? "";
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

    private void OnLetterTapped()
    {
        if (data == null) return;
        if (controller != null && controller.IsTransitioning) return;

        isTapped = true;

        // Button Animation
        if (animator != null)
        {
            animator.SetTrigger("Tap");
        }
        else
        {
            if (wiggleCoroutine != null) StopCoroutine(wiggleCoroutine);
            wiggleCoroutine = StartCoroutine(WiggleCoroutine());
        }

        // Play letter sound (ensures 2D spatial blend, non-zero volume, auto-attaches AudioSource if missing)
        AudioClip clipToPlay = data.soundClip != null ? data.soundClip : data.pureSoundClip;
        if (clipToPlay != null)
        {
            AudioSource sourceToUse = (controller != null && controller.SfxAudioSource != null)
                ? controller.SfxAudioSource
                : GetComponent<AudioSource>();

            if (sourceToUse == null)
            {
                sourceToUse = gameObject.AddComponent<AudioSource>();
            }

            sourceToUse.spatialBlend = 0f; // Force 2D UI sound
            sourceToUse.volume = 1f;
            sourceToUse.loop = false;
            sourceToUse.PlayOneShot(clipToPlay);
        }

        // Spawn confetti burst
        SpawnConfetti();

        // Notify Controller
        if (controller != null)
        {
            controller.OnLetterTapped(this);
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
