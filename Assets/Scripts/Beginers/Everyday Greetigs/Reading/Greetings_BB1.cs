using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Greetings_BB1.cs
/// Attach to an empty GameObject named "Greetings_BB1" in your scene.
/// Manages row reveal, audio playback, pair highlighting, and Play All sequence.
/// </summary>
public class Greetings_BB1 : MonoBehaviour
{
    [Header("Data Arrays")]
    public string[] greetingTexts;
    public AudioClip[] greetingAudios;
    public string[] responseTexts;
    public AudioClip[] responseAudios;

    [Header("Prefabs and Containers")]
    public GameObject greetingPrefab;
    public GameObject responsePrefab;
    public Transform greetingContainer;
    public Transform responseContainer;
    public float verticalSpacing = 100f;

    [Header("Highlight Colors")]
    public Color defaultColor = Color.white;
    public Color highlightColor = new Color(1f, 0.85f, 0.9f); // soft pink
    public Color playAllColor = new Color(1f, 0.75f, 0.3f);   // warm amber for Play All

    [Header("Button Scale Settings")]
    [Tooltip("Scale multiplier applied to buttons when a row is highlighted")]
    public float highlightScale = 1.08f;
    [Tooltip("Duration of the scale tween in seconds")]
    public float scaleDuration = 0.15f;

    [Header("Play All Settings")]
    [Tooltip("How long to highlight a pair during Play All before moving to next")]
    public float playAllPauseDuration = 1.8f;

    [Header("Animate-In Sound")]
    [Tooltip("Short whoosh/pop played each time a greeting or response slides in. One AudioSource is created automatically.")]
    public AudioClip animateInSound;
    private AudioSource _animateSFX;

    [Header("Buttons")]
    public Button nextButton;
    public Button replayButton;

    private List<GreetingRow_BB1> rows = new List<GreetingRow_BB1>();
    private bool isPlayingAll = false;
    private Coroutine playAllCoroutine;

    // ─────────────────────────────────────────────
    void OnEnable()
    {
        // FIRST TIME INIT ONLY
        if (rows.Count == 0)
        {
            InitializeRows();
        }

        // Ensure a dedicated AudioSource exists for the animate-in SFX
        if (_animateSFX == null)
        {
            _animateSFX = gameObject.AddComponent<AudioSource>();
            _animateSFX.playOnAwake = false;
        }

        StopAllCoroutines();
        StopAllAudios();
        ResetVisuals();

        StartCoroutine(StartSequence());
    }

    void InitializeRows()
    {
        for (int i = 0; i < greetingTexts.Length; i++)
        {
            var g = Instantiate(greetingPrefab, greetingContainer);
            var r = Instantiate(responsePrefab, responseContainer);

            var pairObj = new GameObject("Pair_" + i);
            pairObj.transform.SetParent(transform, false);

            var row = pairObj.AddComponent<GreetingRow_BB1>();
            row.Initialize(g, r, greetingTexts[i], greetingAudios[i], responseTexts[i], responseAudios[i], this);

            g.transform.localPosition = new Vector3(0, -i * verticalSpacing, 0);
            r.transform.localPosition = new Vector3(0, -i * verticalSpacing, 0);

            rows.Add(row);
        }
    }

    void ResetVisuals()
    {
        foreach (var row in rows)
        {
            if (row.greetingRect != null)
                row.greetingRect.gameObject.SetActive(false);

            if (row.responseRect != null)
                row.responseRect.gameObject.SetActive(false);

            row.SetHighlight(defaultColor);
            row.SetButtonScale(Vector3.one); // reset scale
        }

        // Hide both buttons until the full sequence completes
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);
        if (replayButton != null)
            replayButton.gameObject.SetActive(false);
    }

    // Stop all audios in all rows
    void StopAllAudios()
    {
        foreach (var row in rows)
        {
            if (row.greetingAudio != null) row.greetingAudio.Stop();
            if (row.responseAudio != null) row.responseAudio.Stop();
        }
    }

    // ─────────────────────────────────────────────
    // START SEQUENCE
    // ─────────────────────────────────────────────
    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(0.3f); // small pause

        // Play all audios sequentially with animation
        yield return StartCoroutine(PlayAllSequence(true));

        // Show buttons only after the full sequence finishes
        if (nextButton != null) nextButton.gameObject.SetActive(true);
        if (replayButton != null) replayButton.gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────
    // CALLED BY GreetingRow_BB1 when a cell is tapped
    // ─────────────────────────────────────────────
    public void OnRowTapped(GreetingRow_BB1 tappedRow)
    {
        if (isPlayingAll) return; // Don't allow taps during Play All

        // Clear all highlights and scales
        ClearAllHighlights();

        // Highlight + scale up the tapped pair
        tappedRow.SetHighlight(highlightColor);
        tappedRow.SetButtonScale(Vector3.one * highlightScale, scaleDuration);

        // Stop any currently playing audio before starting new ones
        StopAllAudios();

        // Play greeting then response
        tappedRow.PlayGreetingAudio();
        StartCoroutine(PlayResponseAfterGreeting(tappedRow));
    }

    IEnumerator PlayResponseAfterGreeting(GreetingRow_BB1 row)
    {
        float len = (row.greetingAudio != null && row.greetingAudio.clip != null)
            ? row.greetingAudio.clip.length + 0.15f : 0.5f;
        yield return new WaitForSeconds(len);

        row.PlayResponseAudio();
    }

    // ─────────────────────────────────────────────
    // REPLAY BUTTON
    // ─────────────────────────────────────────────
    public void OnReplayClicked()
    {
        // Stop everything first so audio restarts cleanly from the beginning
        StopAllCoroutines();
        StopAllAudios();
        ClearAllHighlights();

        isPlayingAll = false;
        playAllCoroutine = null;

        // Hide buttons while replaying; they re-appear after sequence finishes
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);

        playAllCoroutine = StartCoroutine(PlayAllSequenceAndShowButtons());
    }

    IEnumerator PlayAllSequenceAndShowButtons()
    {
        yield return StartCoroutine(PlayAllSequence(false));

        // Re-show buttons once replay finishes
        if (nextButton != null) nextButton.gameObject.SetActive(true);
        if (replayButton != null) replayButton.gameObject.SetActive(true);
    }

    // ─────────────────────────────────────────────
    // NEXT BUTTON
    // ─────────────────────────────────────────────
    public void OnNextClicked()
    {
        // Stop all coroutines and audio immediately so nothing bleeds into the next screen
        StopAllCoroutines();
        StopAllAudios();
        isPlayingAll = false;
        playAllCoroutine = null;

        // Disable both buttons
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);

        ClearAllHighlights();

        PlayerPrefs.SetInt("reading_state", 2); // move to next panel
        PlayerPrefs.Save();

        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // PLAY ALL SEQUENCE
    // ─────────────────────────────────────────────
    IEnumerator PlayAllSequence(bool animate = false)
    {
        isPlayingAll = true;

        for (int i = 0; i < rows.Count; i++)
        {
            ClearAllHighlights();
            rows[i].SetHighlight(playAllColor);
            rows[i].SetButtonScale(Vector3.one * highlightScale, scaleDuration); // scale up active row

            // ── GREETING ──────────────────────────────────────
            StopAllAudios();

            if (animate)
            {
                PlayAnimateInSound();
                yield return StartCoroutine(rows[i].AnimateIn(0.5f, 800f, onlyGreeting: true));
            }
            else
            {
                if (rows[i].greetingRect != null) rows[i].greetingRect.gameObject.SetActive(true);
            }

            rows[i].PlayGreetingAudio();
            float greetingLen = (rows[i].greetingAudio != null && rows[i].greetingAudio.clip != null)
                ? rows[i].greetingAudio.clip.length + 0.15f : 0.6f;
            yield return new WaitForSeconds(greetingLen);

            // ── RESPONSE ──────────────────────────────────────
            StopAllAudios();

            if (animate)
            {
                PlayAnimateInSound();
                yield return StartCoroutine(rows[i].AnimateIn(0.5f, 800f, onlyGreeting: false, onlyResponse: true));
            }
            else
            {
                if (rows[i].responseRect != null) rows[i].responseRect.gameObject.SetActive(true);
            }

            rows[i].PlayResponseAudio();
            float responseLen = (rows[i].responseAudio != null && rows[i].responseAudio.clip != null)
                ? rows[i].responseAudio.clip.length + 0.2f : 0.6f;
            yield return new WaitForSeconds(responseLen + playAllPauseDuration);

            // Reset scale after pair finishes
            rows[i].SetButtonScale(Vector3.one, scaleDuration);
        }

        ClearAllHighlights();
        isPlayingAll = false;
        playAllCoroutine = null;
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────
    void ClearAllHighlights()
    {
        foreach (var row in rows)
        {
            row.SetHighlight(defaultColor);
            row.SetButtonScale(Vector3.one, scaleDuration);
        }
    }

    // ─────────────────────────────────────────────
    // ANIMATE-IN SOUND HELPER
    // ─────────────────────────────────────────────
    void PlayAnimateInSound()
    {
        if (_animateSFX != null && animateInSound != null)
            _animateSFX.PlayOneShot(animateInSound);
    }

    public void OnBackClicked()
    {
        PlayerPrefs.SetInt("reading_state", 1);
        PlayerPrefs.Save();

        StopAllCoroutines();
        StopAllAudios();

        gameObject.SetActive(false);

        if (transform.parent != null)
            transform.parent.gameObject.SetActive(false);
    }
}