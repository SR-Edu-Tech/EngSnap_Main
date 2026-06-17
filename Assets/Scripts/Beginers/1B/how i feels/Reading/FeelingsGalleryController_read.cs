using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// FeelingsGalleryController_read  —  Screen 1
/// ─────────────────────────────────────────────────────────────────────────
/// Spawns 14 tiles at runtime from a single prefab. No manual tile creation.
///
/// HIERARCHY (you build this, tiles are spawned by script):
///   Screen1_Gallery
///     ├─ Grid                      ← GridLayoutGroup — assign to 'tilesGrid'
///     ├─ SentenceFrame
///     │    ├─ SentencePrefix       ← TMP_Text "I am feeling"
///     │    └─ SentenceWord         ← TMP_Text (updates on tap)
///     ├─ NextButton                ← Button
///     └─ ReplayButton              ← Button
///
/// TILE PREFAB (one prefab, assign to 'tilePrefab'):
///   TilePrefab                     ← FeelingTile_read + Button + Image (bg)
///     ├─ KidImage                  ← Image
///     ├─ WordBubble                ← Image
///     │    └─ WordText             ← TMP_Text
///     └─ StarOverlay               ← Image
///
/// Fill tileData[0..13] in Inspector — script spawns + configures each tile.
/// </summary>
public class FeelingsGalleryController_read : MonoBehaviour
{
    [System.Serializable]
    public class TileData
    {
        public string    feelingWord;
        public Sprite    tileSprite;
        public AudioClip audioClip;
    }

    [Header("Prefab — ONE tile prefab used for all 14")]
    [SerializeField] private FeelingTile_read tilePrefab;

    [Header("Tile Data (14 entries in order)")]
    [SerializeField] private TileData[] tileData = new TileData[14];

    [Header("Scene Refs")]
    [SerializeField] private Transform  tilesGrid;
    [SerializeField] private TMP_Text   sentenceWord;
    [SerializeField] private TMP_Text   sentencePrefix;
    [SerializeField] private GameObject sentenceFrame;
    [SerializeField] private Button     nextButton;
    [SerializeField] private Button     replayButton;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   introClip;
    [SerializeField] private AudioClip   tapAnyClip;

    [Header("Timing")]
    [SerializeField] private float autoPlayInterval  = 1.2f;
    [SerializeField] private int   tapsToUnlockNext  = 4;

    // ── Runtime ──────────────────────────────────────────────────────────
    private GameManager_Reading_read  _manager;
    private List<FeelingTile_read>    _tiles = new();
    private int  _tapCount;
    private bool _interactive;

    // ════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════

    public void StartScreen(GameManager_Reading_read manager)
    {
        _manager     = manager;
        _tapCount    = 0;
        _interactive = false;

        StopAllCoroutines();
        SpawnTiles();       // ← destroys old tiles + instantiates fresh ones
        InitUI();
        StartCoroutine(AutoPlaySequence());
    }

    public void ResetScreen()
    {
        StopAllCoroutines();
        _tapCount    = 0;
        _interactive = false;
        if (audioSource != null) audioSource.Stop();
        // Destroy spawned tiles so next StartScreen is clean
        DestroyTiles();
    }

    // ════════════════════════════════════════════════════════════════════
    //  Spawn / destroy tiles
    // ════════════════════════════════════════════════════════════════════

    private void SpawnTiles()
    {
        DestroyTiles();

        if (tilePrefab == null)
        {
            Debug.LogError("[FeelingsGallery] tilePrefab not assigned!");
            return;
        }

        for (int i = 0; i < tileData.Length; i++)
        {
            var tile = Instantiate(tilePrefab, tilesGrid);
            tile.name = $"Tile_{tileData[i].feelingWord}";
            _tiles.Add(tile);
        }
    }

    private void DestroyTiles()
    {
        foreach (var t in _tiles)
            if (t != null) Destroy(t.gameObject);
        _tiles.Clear();
    }

    // ════════════════════════════════════════════════════════════════════
    //  InitUI
    // ════════════════════════════════════════════════════════════════════

    private void InitUI()
    {
        if (sentenceFrame  != null) sentenceFrame.SetActive(true);
        if (sentenceWord   != null) sentenceWord.text   = "____";
        if (sentencePrefix != null) sentencePrefix.text = "I am feeling";

        if (nextButton   != null) { nextButton.gameObject.SetActive(false);  nextButton.onClick.RemoveAllListeners();  nextButton.onClick.AddListener(OnNextPressed); }
        if (replayButton != null) { replayButton.gameObject.SetActive(false); replayButton.onClick.RemoveAllListeners(); replayButton.onClick.AddListener(OnReplayPressed); }

        // Configure each spawned tile
        for (int i = 0; i < _tiles.Count; i++)
        {
            var data = tileData[i];
            _tiles[i].Initialise(
                word:     data.feelingWord,
                sprite:   data.tileSprite,
                index:    i,
                locked:   true,
                onTapped: OnTileTapped
            );
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  Auto-play
    // ════════════════════════════════════════════════════════════════════

    private IEnumerator AutoPlaySequence()
    {
        // Intro VO
        PlayVO(introClip);
        if (introClip != null) yield return new WaitForSeconds(introClip.length + 0.3f);

        // Stagger tile pop-in
        for (int i = 0; i < _tiles.Count; i++)
            StartCoroutine(_tiles[i].PopIn(i * 0.07f));
        yield return new WaitForSeconds(_tiles.Count * 0.07f + 0.4f);

        // Play each tile in order
        for (int i = 0; i < _tiles.Count; i++)
        {
            var data = tileData[i];
            _tiles[i].SetHighlight(true);
            if (sentenceWord != null) sentenceWord.text = data.feelingWord;

            PlayVO(data.audioClip);
            float clipLen = data.audioClip != null ? data.audioClip.length : 0.6f;
            yield return new WaitForSeconds(Mathf.Max(clipLen, 0.6f));

            _tiles[i].SetHighlight(false);
            yield return new WaitForSeconds(0.15f);
        }

        if (sentenceWord != null) sentenceWord.text = "____";

        // Unlock for tap-to-explore
        PlayVO(tapAnyClip);
        _interactive = true;
        foreach (var t in _tiles) t.SetLocked(false);

        if (tapAnyClip != null) yield return new WaitForSeconds(tapAnyClip.length);
        if (replayButton != null) replayButton.gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Tile tapped
    // ════════════════════════════════════════════════════════════════════

    private void OnTileTapped(FeelingTile_read tile, int index)
    {
        if (!_interactive) return;

        var data = tileData[index];
        if (sentenceWord != null) sentenceWord.text = data.feelingWord;
        PlayVO(data.audioClip);
        tile.PlayTapAnim();

        _tapCount++;
        if (_tapCount >= tapsToUnlockNext && nextButton != null)
            nextButton.gameObject.SetActive(true);
    }

    // ════════════════════════════════════════════════════════════════════
    //  Buttons
    // ════════════════════════════════════════════════════════════════════

    private void OnNextPressed() => _manager?.OnScreen1Complete();

    private void OnReplayPressed()
    {
        _tapCount    = 0;
        _interactive = false;
        if (nextButton   != null) nextButton.gameObject.SetActive(false);
        if (replayButton != null) replayButton.gameObject.SetActive(false);
        StartCoroutine(AutoPlaySequence());
    }

    private void PlayVO(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }
}