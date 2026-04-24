using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroManager_BB1 : MonoBehaviour
{
    [Header("Callback")]
    public UnitPanelController_BB1 panel;   // drag the parent Topic panel here
    public UnitButton_BB1 unitButton;       // drag the Intro UnitButton_BB1 here

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
    public TextMeshProUGUI[] introLines;    // 0: Listen, 1: Speak, 2: Play
    public Image[] introIcons;              // 0: Ear,    1: Mic,   2: Controller
    public RectTransform flashcardIcon;
    public Button startButton;

    [Header("Config")]
    public string badgeTextValue = "Unit I";
   // public Color badgeTextColor  = new Color(0.5f, 0f, 1f);
    //public Color badgeBgColor    = new Color(1f, 0.5f, 0f);
    public string mainTitleText  = "Everyday Greetings";
   // public Color mainTitleColor  = new Color(0.1f, 0.1f, 0.5f);
    public float letterAnimDelay = 0.05f;
    public float lineAnimDelay   = 0.4f;
    public float lineFadeDuration = 0.35f;

    // ── Unity Lifecycle ───────────────────────────────────────────────────

    void OnEnable()
    {
        // Re-run the intro every time the GameObject is activated
        StartIntro();
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Call this (or wire the Start button) to finish the intro and return to unit panel.
    /// </summary>
    public void OnIntroFinished()
    {
        if (panel != null && unitButton != null)
            panel.UnitFinished(unitButton);
        else
            gameObject.SetActive(false); // fallback
    }

    // ── Private ───────────────────────────────────────────────────────────

    void StartIntro()
    {
        StopAllCoroutines();

        // Play BGM
        if (bgmSource != null && bgmClip != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // Reset UI
        if (unitBadge  != null) unitBadge.gameObject.SetActive(false);
        if (mainTitle  != null) mainTitle.gameObject.SetActive(false);
        if (introBox   != null) introBox.SetActive(false);
        if (flashcardIcon != null) flashcardIcon.gameObject.SetActive(false);
        if (startButton != null) startButton.gameObject.SetActive(false);

        foreach (var line in introLines)
        {
            SetAlpha(line, 0f);
            line.gameObject.SetActive(false);
        }
        foreach (var icon in introIcons)
        {
            SetAlpha(icon, 0f);
            icon.gameObject.SetActive(false);
        }

        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        // 1. Badge drop-in
        if (unitBadge != null)
        {
            unitBadge.gameObject.SetActive(true);
          //  if (badgeText != null) { badgeText.text = badgeTextValue; badgeText.color = badgeTextColor; }
            yield return StartCoroutine(AnimateBadgeDropIn());
        }

        // 2. Main title
        if (mainTitle != null)
        {
            mainTitle.gameObject.SetActive(true);
            mainTitle.text  = "";
           // mainTitle.color = mainTitleColor;
            yield return StartCoroutine(AnimateTitle(mainTitleText));
        }

        // 3. Intro box lines & icons
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

        // 4. Character speech
        if (characterAudioSource != null && characterSpeechClip != null)
        {
            characterAudioSource.clip = characterSpeechClip;
            characterAudioSource.Play();
            yield return new WaitForSeconds(characterSpeechClip.length);
        }

        // 5. Flashcard flip
        if (flashcardIcon != null)
        {
            flashcardIcon.gameObject.SetActive(true);
            yield return StartCoroutine(AnimateFlashcardFlip());
        }

        // 6. Start button — wired here to OnIntroFinished
        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnIntroFinished);
            StartCoroutine(AnimateStartButtonPulse());
        }
    }

    // ── Animations (unchanged from original) ─────────────────────────────

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

    IEnumerator AnimateStartButtonPulse()
    {
        float pulseScale = 1.05f, duration = 0.7f;
        Vector3 orig = startButton.transform.localScale;
        while (startButton.gameObject.activeSelf)
        {
            float t = 0f;
            while (t < duration / 2f) { t += Time.deltaTime; startButton.transform.localScale = orig * Mathf.Lerp(1f, pulseScale, t / duration); yield return null; }
            t = 0f;
            while (t < duration / 2f) { t += Time.deltaTime; startButton.transform.localScale = orig * Mathf.Lerp(pulseScale, 1f, t / duration); yield return null; }
        }
        startButton.transform.localScale = orig;
    }

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
