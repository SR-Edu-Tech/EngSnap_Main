using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Animations;

public class IntroManager_BB1 : MonoBehaviour, IUnitCompletable
{
    [Header("Callback — auto-set at runtime by SharedUnitPanelController")]
    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton unitButton;

    [Header("Audio")]
    public AudioSource bgmSource;
    public AudioClip bgmClip;
    public AudioSource characterAudioSource;
    public AudioClip characterSpeechClip;

    [Header("UI References")]
    public RectTransform unitBadge;
    public TextMeshProUGUI badgeText;
    public TextMeshProUGUI mainTitle;
    public GameObject introBox;
    public TextMeshProUGUI[] introLines;
    public Image[] introIcons;
    public RectTransform flashcardIcon;
    public Button startButton;

    public Animator animator;

    [Header("Config")]
    public string badgeTextValue = "Unit I";
    public string mainTitleText  = "Everyday Greetings";
    public float letterAnimDelay = 0.05f;
    public float lineAnimDelay   = 0.4f;
    public float lineFadeDuration = 0.35f;

    // ── IUnitCompletable ──────────────────────────────────────────────────
    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        panel      = sharedPanel;
        unitButton = sharedButton;
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────
    void OnEnable()
    {
        StartIntro();

        if (animator != null)
        {
            animator.Play("monkey", 0, 0f);
            animator.speed = 0f;
        }

        Invoke("monkeyanimation", 2f);
    }

    void monkeyanimation()
    {
        if (animator != null)
        {
            animator.speed = 1f;
            animator.Play("monkey", 0, 0f);
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    // ── Public API ────────────────────────────────────────────────────────
    public void OnIntroFinished()
    {
        if (panel != null && unitButton != null)
            panel.UnitFinished(unitButton);
        else
            gameObject.SetActive(false);
    }

    // ── Private ───────────────────────────────────────────────────────────
    void StartIntro()
    {
        StopAllCoroutines();

        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        if (unitBadge     != null) unitBadge.gameObject.SetActive(false);
        if (mainTitle     != null) mainTitle.gameObject.SetActive(false);
        if (introBox      != null) introBox.SetActive(false);
        if (flashcardIcon != null) flashcardIcon.gameObject.SetActive(false);
        if (startButton   != null) startButton.gameObject.SetActive(false);

        foreach (var line in introLines) { SetAlpha(line, 0f); line.gameObject.SetActive(false); }
        foreach (var icon in introIcons) { SetAlpha(icon, 0f); icon.gameObject.SetActive(false); }

        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        if (unitBadge != null)
        {
            unitBadge.gameObject.SetActive(true);
            yield return StartCoroutine(AnimateBadgeDropIn());
        }

        if (mainTitle != null)
        {
            mainTitle.gameObject.SetActive(true);
            mainTitle.text = "";
            yield return StartCoroutine(AnimateTitle(mainTitleText));
        }

        if (introBox != null) introBox.SetActive(true);

        int itemCount = Mathf.Min(introLines.Length, introIcons.Length);
        for (int i = 0; i < itemCount; i++)
        {
            introLines[i].gameObject.SetActive(true);
            yield return StartCoroutine(FadeText(introLines[i], lineFadeDuration));
            yield return new WaitForSeconds(lineAnimDelay);

            introIcons[i].gameObject.SetActive(true);
            yield return StartCoroutine(FadeImage(introIcons[i], lineFadeDuration));
            yield return new WaitForSeconds(lineAnimDelay);
        }

        if (characterAudioSource != null && characterSpeechClip != null)
        {
            characterAudioSource.clip = characterSpeechClip;
            characterAudioSource.Play();
            yield return new WaitForSeconds(characterSpeechClip.length);
        }

        if (flashcardIcon != null)
        {
            flashcardIcon.gameObject.SetActive(true);
            yield return StartCoroutine(AnimateFlashcardFlip());
        }

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnIntroFinished);
            AnimateStartButtonPulse();
        }
    }

    IEnumerator AnimateBadgeDropIn()
    {
        Vector2 startPos = unitBadge.anchoredPosition;
        float targetY    = startPos.y;
        float offscreenY = targetY + 300f;
        unitBadge.anchoredPosition = new Vector2(startPos.x, offscreenY);

        float t = 0f, duration = 0.6f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float eased = Mathf.Sin((t / duration) * Mathf.PI * 0.5f);
            unitBadge.anchoredPosition = new Vector2(startPos.x, Mathf.Lerp(offscreenY, targetY, eased));
            yield return null;
        }
        unitBadge.anchoredPosition = new Vector2(startPos.x, targetY);
    }

    IEnumerator AnimateFlashcardFlip()
    {
        float t = 0f, duration = 0.5f;
        flashcardIcon.localScale = new Vector3(0f, 1f, 1f);
        while (t < duration)
        {
            t += Time.deltaTime;
            float scaleX = Mathf.Sin((t / duration) * Mathf.PI * 0.5f);
            flashcardIcon.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }
        flashcardIcon.localScale = Vector3.one;
    }

    public void AnimateStartButtonPulse() { }

    IEnumerator AnimateTitle(string title)
    {
        mainTitle.text = "";
        for (int i = 0; i < title.Length; i++)
        {
            mainTitle.text += title[i];
            yield return new WaitForSeconds(letterAnimDelay);
        }
    }

    IEnumerator FadeText(TextMeshProUGUI text, float duration)
    {
        SetAlpha(text, 0f);
        float elapsed = 0f;
        while (elapsed < duration) { elapsed += Time.deltaTime; SetAlpha(text, Mathf.Clamp01(elapsed / duration)); yield return null; }
        SetAlpha(text, 1f);
    }

    IEnumerator FadeImage(Image image, float duration)
    {
        SetAlpha(image, 0f);
        float elapsed = 0f;
        while (elapsed < duration) { elapsed += Time.deltaTime; SetAlpha(image, Mathf.Clamp01(elapsed / duration)); yield return null; }
        SetAlpha(image, 1f);
    }

    void SetAlpha(Graphic graphic, float alpha)
    {
        Color c = graphic.color;
        c.a = alpha;
        graphic.color = c;
    }
}