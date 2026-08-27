using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SD_SoundTile_Phonics_Junior : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text graphemeText;
    [SerializeField] private TMP_Text keywordText;
    [SerializeField] private Image objectImage;
    [SerializeField] private Image backgroundImage;

    [Header("Audio")]
    [SerializeField] private float gapBetweenSounds = 0.05f;

    private SD_SoundTileData_Phonics_Junior data;
    private AudioSource audioSource;
    private Vector3 originalScale;
    [SerializeField] private Color visitedColor = new Color(1f, 0.85f, 0.3f, 1f); // Golden yellow
    private bool visited;
    private Color originalColor = Color.white;

    private IEnumerator Start()
    {
        yield return null;
        originalScale = transform.localScale;
    }

    public void SetUIElements(TMP_Text grapheme, TMP_Text keyword, Image bgImage, Image objImage = null)
    {
        graphemeText = grapheme;
        keywordText = keyword;
        backgroundImage = bgImage;
        objectImage = objImage;

        if (backgroundImage != null)
            originalColor = backgroundImage.color;
    }

    private void AutoResolveUIReferences()
    {
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();

        if (graphemeText == null || keywordText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && graphemeText == null) graphemeText = texts[0];
            if (texts.Length > 1 && keywordText == null) keywordText = texts[1];
        }
    }

    public bool MarkVisited()
    {
        if (visited)
            return false;

        visited = true;

        if (backgroundImage != null)
            backgroundImage.color = visitedColor;

        Button button = GetComponent<Button>();

        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = visitedColor;
            colors.highlightedColor = visitedColor;
            colors.selectedColor = visitedColor;
            colors.pressedColor = visitedColor;
            button.colors = colors;
        }

        return true;
    }

    public void Initialize(SD_SoundTileData_Phonics_Junior tileData, AudioSource source)
    {
        AutoResolveUIReferences();

        data = tileData;
        audioSource = source;

        if (data != null && data.image != null)
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = data.image;
                backgroundImage.color = Color.white;
                backgroundImage.type = Image.Type.Simple;
                backgroundImage.preserveAspect = false;
            }

            if (objectImage != null) objectImage.gameObject.SetActive(false);
            if (graphemeText != null) graphemeText.gameObject.SetActive(false);
            if (keywordText != null) keywordText.gameObject.SetActive(false);
        }
        else
        {
            if (graphemeText != null)
            {
                graphemeText.gameObject.SetActive(true);
                if (data != null && !string.IsNullOrEmpty(data.grapheme)) graphemeText.text = data.grapheme;
                graphemeText.fontSize = 28f;
                graphemeText.enableAutoSizing = false;
                graphemeText.overflowMode = TextOverflowModes.Overflow;
                graphemeText.raycastTarget = false;
            }

            if (keywordText != null)
            {
                keywordText.gameObject.SetActive(true);
                if (data != null && !string.IsNullOrEmpty(data.keyword)) keywordText.text = data.keyword;
                keywordText.fontSize = 16f;
                keywordText.enableAutoSizing = false;
                keywordText.overflowMode = TextOverflowModes.Overflow;
                keywordText.raycastTarget = false;
            }

            if (objectImage != null)
            {
                objectImage.gameObject.SetActive(false);
            }
        }

        if (backgroundImage != null)
            originalColor = backgroundImage.color;

        Button button = GetComponent<Button>();
        if (button != null)
        {
            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
        }
    }

    public void ResetTile()
    {
        visited = false;

        if (backgroundImage != null)
            backgroundImage.color = originalColor;

        Button button = GetComponent<Button>();

        if (button != null)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = originalColor;
            colors.highlightedColor = originalColor;
            colors.selectedColor = originalColor;
            colors.pressedColor = originalColor;
            button.colors = colors;

            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.None;
            button.navigation = nav;
        }

        if (originalScale != Vector3.zero)
            transform.localScale = originalScale;
    }

    public void PlaySound()
    {
        if (audioSource != null) audioSource.Stop();
        StopAllCoroutines();

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        StartCoroutine(PopAnimation());

        if (data != null && data.soundClip != null && audioSource != null)
        {
            audioSource.clip = data.soundClip;
            audioSource.Play();

            yield return new WaitWhile(() => audioSource != null && audioSource.isPlaying);
        }

        if (gapBetweenSounds > 0f)
        {
            yield return new WaitForSeconds(gapBetweenSounds);
        }

        if (data != null && data.keywordClip != null && audioSource != null)
        {
            audioSource.clip = data.keywordClip;
            audioSource.Play();

            yield return new WaitWhile(() => audioSource != null && audioSource.isPlaying);
        }
    }

    private IEnumerator PopAnimation()
    {
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 1.08f;

        float timer = 0f;
        float duration = 0.08f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, targetScale, timer / duration);
            yield return null;
        }

        timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, startScale, timer / duration);
            yield return null;
        }

        transform.localScale = startScale;
    }

    public float GetTotalDuration()
    {
        float duration = 0f;

        if (data != null)
        {
            if (data.soundClip != null)
                duration += data.soundClip.length;

            duration += gapBetweenSounds;

            if (data.keywordClip != null)
                duration += data.keywordClip.length;
        }

        return duration;
    }
}