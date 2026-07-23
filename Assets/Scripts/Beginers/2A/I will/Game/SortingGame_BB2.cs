using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────
//  DATA
// ─────────────────────────────────────────────────────────────────────────

public enum BinType_BB2 { IWill, IWillNot }

/// <summary>
/// One picture chit, e.g. "help a friend" → IWill.
/// </summary>
[System.Serializable]
public class ChitData_BB2
{
    [Tooltip("Illustration shown on the chit.")]
    public Sprite chitSprite;

    [Tooltip("Optional label under the illustration.")]
    public string chitLabel;

    [Tooltip("Which bin this chit correctly belongs in.")]
    public BinType_BB2 correctBin;

    [Tooltip("VO played when sorted correctly, e.g. 'I will help!'")]
    public AudioClip lockedInAudioClip;
}

// ─────────────────────────────────────────────────────────────────────────
//  GAMEPLAY — Screen 2 (BB2 Sorting)
// ─────────────────────────────────────────────────────────────────────────

/// <summary>
/// I Will / I Will Not sorting game — BB2.
/// CENTRE: a tray of 8 shuffled picture chits (chitsData), spawned at
///         runtime into trayParent.
/// BOTTOM: two bins — iWillBin and iWillNotBin — pre-placed in the scene.
///
/// Student drags each chit into a bin. Correct bin → chit locks into
/// place (shrinks, greys, VO plays, counter increments). Wrong bin, or
/// dropped outside both bins → chit bounces back to the tray, no penalty.
///
/// Call RestartGame() every time this screen is (re)entered.
/// Fires OnFinished when Next is pressed — GameManager decides what
/// happens next.
/// </summary>
public class SortingGame_BB2 : MonoBehaviour
{
    [Header("Data — 8 chits")]
    public ChitData_BB2[] chitsData = new ChitData_BB2[8];

    [Header("Prefab")]
    public Chit_BB2 chitPrefab;

    [Header("Tray")]
    public Transform trayParent;

    [Header("Bins — pre-placed in the scene")]
    public BinDropZone_BB2 iWillBin;
    public BinDropZone_BB2 iWillNotBin;

    [Header("UI")]
    public CanvasGroup mainCanvasGroup;
    public TMP_Text counterText;
    public Button nextButton;
    [Tooltip("Plays each chit's locked-in VO clip")]
    public AudioSource dialogueAudioSource;

    [Header("Narrator VO (optional)")]
    public AudioClip introVO;
    public AudioClip wrongBinVO;
    public AudioClip allSortedVO;

    [Header("Timing")]
    [SerializeField] private float delayBeforeOutro = 0.6f;

    /// Fired when Next is pressed. GameManager wires this externally.
    [HideInInspector] public System.Action OnFinished;

    private readonly List<Chit_BB2> _chits = new();
    private int _sortedCount;

    // ════════════════════════════════════════════════════════════════════
    //  Reset / entry point — call every time this screen is shown
    // ════════════════════════════════════════════════════════════════════

    public void RestartGame()
    {
        StopAllCoroutines();
        ClearTray();

        _sortedCount = 0;
        UpdateCounter();

        nextButton?.gameObject.SetActive(false);
        if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

        iWillBin?.Initialise(OnChitDropped);
        iWillNotBin?.Initialise(OnChitDropped);

        SpawnTray();

        if (introVO != null)
            AudioManager.Instance?.PlayVO(introVO);

        Debug.Log("[SortingGame_BB2] RestartGame — fresh tray");
    }

    private void SpawnTray()
    {
        var order = ShuffleIndices(chitsData.Length);
        foreach (int idx in order)
        {
            var chit = Instantiate(chitPrefab, trayParent);
            chit.Initialise(idx, chitsData[idx]);
            _chits.Add(chit);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Chit dropped on a bin
    // ════════════════════════════════════════════════════════════════════

    private void OnChitDropped(Chit_BB2 chit, BinType_BB2 binType)
    {
        if (chit.IsLocked) return;

        if (chit.Data.correctBin == binType)
            StartCoroutine(CorrectSort(chit, binType));
        else
            WrongSort(chit);
    }

    private IEnumerator CorrectSort(Chit_BB2 chit, BinType_BB2 binType)
    {
        var bin = binType == BinType_BB2.IWill ? iWillBin : iWillNotBin;

        chit.LockIn(bin != null ? bin.DockAnchor : null);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxCorrect);
        VFXManager.Instance?.SpawnCorrectBurst(chit.GetComponent<RectTransform>());

        yield return StartCoroutine(PlayChitAudio(chit.Data));

        _sortedCount++;
        UpdateCounter();

        if (_sortedCount >= chitsData.Length)
            StartCoroutine(CompleteSequence());
    }

    private void WrongSort(Chit_BB2 chit)
    {
        chit.PlayWrongBounce();
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxWrong);

        if (wrongBinVO != null)
            AudioManager.Instance?.PlayVO(wrongBinVO);
    }

    private IEnumerator PlayChitAudio(ChitData_BB2 data)
    {
        if (dialogueAudioSource != null && data.lockedInAudioClip != null)
        {
            dialogueAudioSource.clip = data.lockedInAudioClip;
            dialogueAudioSource.Play();
            yield return new WaitForSeconds(data.lockedInAudioClip.length);
        }
        else
        {
            // No audio assigned yet — fall back to a short readable pause.
            yield return new WaitForSeconds(0.6f);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  All 8 chits sorted
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator CompleteSequence()
    {
        yield return new WaitForSeconds(delayBeforeOutro);

        AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxRoundComplete);
        VFXManager.Instance?.SpawnConfetti();

        if (allSortedVO != null)
        {
            AudioManager.Instance?.PlayVO(allSortedVO);
            yield return new WaitForSeconds(allSortedVO.length);
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

    private void UpdateCounter()
    {
        if (counterText != null)
            counterText.text = $"{_sortedCount} / {chitsData.Length}";
    }

    private void ClearTray()
    {
        foreach (var c in _chits)
            if (c != null) Destroy(c.gameObject);
        _chits.Clear();
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
