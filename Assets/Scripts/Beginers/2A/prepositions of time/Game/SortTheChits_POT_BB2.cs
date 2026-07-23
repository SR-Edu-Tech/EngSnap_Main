using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

[System.Serializable]
public class ChitData_POT_BB2
{
    [Tooltip("The time phrase on the chit, e.g. 'the morning'")]
    public string phraseText;
    [Tooltip("Correct basket for this chit")]
    public PotWord_POT_BB2 correctBasket;
    [Tooltip("Small time picture shown on the chit, e.g. sunrise")]
    public Sprite chitIcon;
    [Tooltip("VO read aloud once sorted correctly, e.g. 'in the morning!'")]
    public AudioClip fullPhraseAudio;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// Sort The Chits — POT_BB2 (Preposition Of Time: IN / ON / AT).
/// Player drags each of the 9 time-phrase chits into the matching pink IN,
/// blue ON, or purple AT basket. Wrong drops bounce gently back to the
/// tray — no penalty. Fires OnFinished when Next is pressed after all 9
/// chits are correctly sorted.
/// </summary>
public class SortTheChits_POT_BB2 : MonoBehaviour
{
    [Header("Chits — 9 total")]
    public ChitData_POT_BB2[] chits = new ChitData_POT_BB2[9];

    [Header("Prefab")]
    public Chit_POT_BB2 chitPrefab;

    [Header("Layout")]
    public Transform trayParent;
    public Basket_POT_BB2 inBasket;
    public Basket_POT_BB2 onBasket;
    public Basket_POT_BB2 atBasket;

    [Header("UI")]
    public CanvasGroup mainCanvasGroup;
    public Button       nextButton;
    public AudioSource  dialogueAudioSource;
    [Tooltip("Generic hint VO for a wrong basket drop, e.g. 'Hmm, which time word?'")]
    public AudioClip    genericWrongHintClip;

    [Header("Narration — plays once each")]
    [Tooltip("Plays ONCE at the very start of the screen — e.g. 'Fill the gaps! Which basket — in, on or at?'")]
    public AudioClip introAudioClip;
    [Tooltip("Plays ONCE after all 9 chits are sorted, before the Next button appears — e.g. 'You filled every gap!'")]
    public AudioClip outroAudioClip;

    [Header("Basket Colors")]
    [SerializeField] private Color inChitColor = new Color(1f, 0.4f, 0.7f);
    [SerializeField] private Color onChitColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color atChitColor = new Color(0.6f, 0.4f, 0.9f);

    [Header("Timing")]
    [SerializeField] private float placeAnimDuration = 0.3f;
    [SerializeField] private float delayBeforeNextButton = 0.6f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private readonly List<Chit_POT_BB2> _spawnedChits = new();
    private int _sortedCount = 0;
    private int _lastRestartFrame = -1;

    void Awake()
    {
        inBasket?.Initialise(PotWord_POT_BB2.In, OnChitDroppedOnBasket);
        onBasket?.Initialise(PotWord_POT_BB2.On, OnChitDroppedOnBasket);
        atBasket?.Initialise(PotWord_POT_BB2.At, OnChitDroppedOnBasket);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        if (Time.frameCount == _lastRestartFrame)
        {
            Debug.LogWarning("[SortTheChits_POT_BB2] RestartGame called twice in the same frame — ignoring duplicate call.");
            return;
        }
        _lastRestartFrame = Time.frameCount;

        StopAllCoroutines();
        _sortedCount = 0;
        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        StartCoroutine(IntroThenSpawnTray());

        Debug.Log("[SortTheChits_POT_BB2] RestartGame — starting fresh tray");
    }

    private IEnumerator IntroThenSpawnTray()
    {
        if (dialogueAudioSource != null && introAudioClip != null)
        {
            dialogueAudioSource.clip = introAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(introAudioClip.length);
        }

        SpawnTray();
    }

    private void SpawnTray()
    {
        ClearSpawnedChits();

        var order = ShuffleIndices(chits.Length);
        foreach (int idx in order)
        {
            var data  = chits[idx];
            var color = data.correctBasket switch
            {
                PotWord_POT_BB2.In => inChitColor,
                PotWord_POT_BB2.On => onChitColor,
                _                  => atChitColor
            };

            var chit = Instantiate(chitPrefab, trayParent);
            chit.Initialise(data, color, OnChitTapped: null);
            _spawnedChits.Add(chit);
        }

        // Let the tray's layout group (if any) finish arranging the chits
        // BEFORE we read/cache their positions.
        LayoutRebuilder.ForceRebuildLayoutImmediate(trayParent as RectTransform);

        foreach (var chit in _spawnedChits)
        {
            chit.CacheTrayPosition();
            // From here on, ignore any parent Layout Group (tray or basket)
            // so our own code fully controls this chit's position.
            chit.SetIgnoreLayout(true);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Drop handling
    // ════════════════════════════════════════════════════════════════════

    private void OnChitDroppedOnBasket(Chit_POT_BB2 chit, Basket_POT_BB2 basket)
    {
        if (chit.Data.correctBasket != basket.basketWord)
        {
            HandleWrongDrop();
            return;
        }

        StartCoroutine(PlaceChitInBasket(chit, basket));
    }

    private void HandleWrongDrop()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

        if (dialogueAudioSource != null && genericWrongHintClip != null)
        {
            dialogueAudioSource.clip = genericWrongHintClip;
            dialogueAudioSource.Play();
        }
        // chit.OnEndDrag() (fires right after this) handles snapping it back to the tray.
    }

    private IEnumerator PlaceChitInBasket(Chit_POT_BB2 chit, Basket_POT_BB2 basket)
    {
        chit.MarkPlaced();
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);

        var rect = chit.GetComponent<RectTransform>();
        rect.SetParent(basket.transform, true);

        // Force centered anchors/pivot so anchoredPosition zero lands the
        // chit in the middle of the basket regardless of its original
        // tray-layout anchors.
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);

        Vector2 startPos = rect.anchoredPosition;
        float e = 0f;
        while (e < placeAnimDuration)
        {
            e += Time.deltaTime;
            rect.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, e / placeAnimDuration);
            yield return null;
        }
        rect.anchoredPosition = Vector2.zero;

        VFXManager.Instance?.SpawnCorrectBurst(rect);

        if (dialogueAudioSource != null && chit.Data.fullPhraseAudio != null)
        {
            dialogueAudioSource.clip = chit.Data.fullPhraseAudio;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(chit.Data.fullPhraseAudio.length);
        }

        _spawnedChits.Remove(chit);
        Destroy(chit.gameObject);

        _sortedCount++;
        if (_sortedCount >= chits.Length)
            StartCoroutine(AllChitsSorted());
    }

    // ════════════════════════════════════════════════════════════════════
    //  Game complete
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AllChitsSorted()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxGameComplete);
        VFXManager.Instance?.SpawnConfetti();

        if (dialogueAudioSource != null && outroAudioClip != null)
        {
            dialogueAudioSource.clip = outroAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(outroAudioClip.length);
        }
        else
        {
            yield return new WaitForSeconds(delayBeforeNextButton);
        }

        nextButton?.gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Next button — wire this to the Button's OnClick() in the Inspector
    // ════════════════════════════════════════════════════════════════════

    public void OnNextButtonPressed()
    {
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxButtonTap);
        OnFinished?.Invoke();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════

    private void ClearSpawnedChits()
    {
        foreach (var c in _spawnedChits)
            if (c != null) Destroy(c.gameObject);
        _spawnedChits.Clear();
    }

    private static List<int> ShuffleIndices(int count)
    {
        var list = new List<int>();
        for (int i = 0; i < count; i++) list.Add(i);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}