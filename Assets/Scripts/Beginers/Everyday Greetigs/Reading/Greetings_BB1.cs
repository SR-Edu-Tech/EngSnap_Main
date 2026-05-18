using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Greetings_BB1 — first half of the Reading unit.
/// Implements IUnitCompletable so SharedUnitPanelController wires panel + button.
/// When the player clicks Next, it passes panel + button to PostReadingFlow_BB1
/// and activates it. PostReadingFlow_BB1 calls UnitFinished at the very end.
/// </summary>
public class Greetings_BB1 : MonoBehaviour, IUnitCompletable
{
    // ── IUnitCompletable — filled automatically by SharedUnitPanelController ──
    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton          unitButton;

    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        panel      = sharedPanel;
        unitButton = sharedButton;
    }

    // ── Link to second half of this Reading unit ──────────────────────────
    [Header("── Next Screen (PostReadingFlow) ──")]
    [Tooltip("Drag the PostReadingFlow_BB1 GameObject here")]
    public PostReadingFlow_BB1 postReadingFlow;

    // ── Data ──────────────────────────────────────────────────────────────
    [Header("Data Arrays")]
    public string[]   greetingTexts;
    public AudioClip[] greetingAudios;
    public string[]   responseTexts;
    public AudioClip[] responseAudios;

    [Header("Prefabs and Containers")]
    public GameObject greetingPrefab;
    public GameObject responsePrefab;
    public Transform  greetingContainer;
    public Transform  responseContainer;
    public float      verticalSpacing = 100f;

    [Header("Highlight Colors")]
    public Color defaultColor   = Color.white;
    public Color highlightColor = new Color(1f, 0.85f, 0.9f);
    public Color playAllColor   = new Color(1f, 0.75f, 0.3f);

    [Header("Button Scale Settings")]
    public float highlightScale = 1.08f;
    public float scaleDuration  = 0.15f;

    [Header("Play All Settings")]
    public float playAllPauseDuration = 1.8f;

    [Header("Animate-In Sound")]
    public AudioClip animateInSound;
    private AudioSource _animateSFX;

    [Header("Buttons")]
    public Button nextButton;
    public Button replayButton;

    private List<GreetingRow_BB1> rows             = new List<GreetingRow_BB1>();
    private bool                  isPlayingAll      = false;
    private Coroutine             playAllCoroutine;
    private Coroutine             _rowPlayCoroutine;

    // ── Unity ─────────────────────────────────────────────────────────────
    void OnEnable()
    {
        if (rows.Count == 0)
            InitializeRows();

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
            if (row.greetingRect != null) row.greetingRect.gameObject.SetActive(false);
            if (row.responseRect != null) row.responseRect.gameObject.SetActive(false);
            row.SetHighlight(defaultColor);
            row.SetButtonScale(Vector3.one);
        }

        if (nextButton   != null) nextButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);
    }

    void StopAllAudios()
    {
        foreach (var row in rows)
        {
            if (row.greetingAudio != null) row.greetingAudio.Stop();
            if (row.responseAudio != null) row.responseAudio.Stop();
        }
    }

    // ── Start Sequence ────────────────────────────────────────────────────
    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(PlayAllSequence(true));

        if (nextButton   != null) nextButton.gameObject.SetActive(true);
        if (replayButton != null) replayButton.gameObject.SetActive(true);
    }

    // ── Row Tap ───────────────────────────────────────────────────────────
    public void OnRowTapped(GreetingRow_BB1 tappedRow)
    {
        if (isPlayingAll) return;

        if (_rowPlayCoroutine != null) { StopCoroutine(_rowPlayCoroutine); _rowPlayCoroutine = null; }

        StopAllAudios();
        ClearAllHighlights();

        tappedRow.SetHighlight(highlightColor);
        tappedRow.SetButtonScale(Vector3.one * highlightScale, scaleDuration);
        tappedRow.PlayGreetingAudio();
        _rowPlayCoroutine = StartCoroutine(PlayResponseAfterGreeting(tappedRow));
    }

    IEnumerator PlayResponseAfterGreeting(GreetingRow_BB1 row)
    {
        float len = (row.greetingAudio != null && row.greetingAudio.clip != null)
            ? row.greetingAudio.clip.length + 0.15f : 0.5f;
        yield return new WaitForSeconds(len);
        row.PlayResponseAudio();
        _rowPlayCoroutine = null;
    }

    // ── Replay ────────────────────────────────────────────────────────────
    public void OnReplayClicked()
    {
        StopAllCoroutines();
        StopAllAudios();
        ClearAllHighlights();

        isPlayingAll     = false;
        playAllCoroutine = null;

        if (nextButton   != null) nextButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);

        playAllCoroutine = StartCoroutine(PlayAllSequenceAndShowButtons());
    }

    IEnumerator PlayAllSequenceAndShowButtons()
    {
        yield return StartCoroutine(PlayAllSequence(false));
        if (nextButton   != null) nextButton.gameObject.SetActive(true);
        if (replayButton != null) replayButton.gameObject.SetActive(true);
    }

    // ── Next Button → hand off to PostReadingFlow ─────────────────────────
    public void OnNextClicked()
    {
        StopAllCoroutines();
        StopAllAudios();
        isPlayingAll     = false;
        playAllCoroutine = null;

        if (nextButton   != null) nextButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);

        ClearAllHighlights();

        // Deactivate self
        gameObject.SetActive(false);

        // Hand panel + button to PostReadingFlow then activate it
        if (postReadingFlow == null)
        {
            Debug.LogError("[Greetings_BB1] postReadingFlow field is not assigned in Inspector!");
            return;
        }

        if (panel == null || unitButton == null)
        {
            Debug.LogError("[Greetings_BB1] panel or unitButton is null! " +
                           "OnUnitStart was never called — check that the Reading entry in TopicData_BB2 " +
                           "points to THIS Greetings_BB1 GameObject, not PostReadingFlow_BB1.");
            return;
        }

        postReadingFlow.OpenFromGreetings(panel, unitButton);
    }

    // ── Back ──────────────────────────────────────────────────────────────
    public void OnBackClicked()
    {
        StopAllCoroutines();
        StopAllAudios();
        gameObject.SetActive(false);
        if (panel != null) panel.gameObject.SetActive(true);
    }

    // ── Play All Sequence ─────────────────────────────────────────────────
    IEnumerator PlayAllSequence(bool animate = false)
    {
        isPlayingAll = true;

        for (int i = 0; i < rows.Count; i++)
        {
            ClearAllHighlights();
            rows[i].SetHighlight(playAllColor);
            rows[i].SetButtonScale(Vector3.one * highlightScale, scaleDuration);

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

            rows[i].SetButtonScale(Vector3.one, scaleDuration);
        }

        ClearAllHighlights();
        isPlayingAll     = false;
        playAllCoroutine = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    void ClearAllHighlights()
    {
        foreach (var row in rows)
        {
            row.SetHighlight(defaultColor);
            row.SetButtonScale(Vector3.one, scaleDuration);
        }
    }

    void PlayAnimateInSound()
    {
        if (_animateSFX != null && animateInSound != null)
            _animateSFX.PlayOneShot(animateInSound);
    }
}