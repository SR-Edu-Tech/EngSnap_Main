using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  PronounDragScreen_YouAndWeGame  —  Screen 1
///  Picture cards fall from the top. Child drags each card into the
///  correct pronoun tree-house.
/// ════════════════════════════════════════════════════════════════════
///
///  SCENE HIERARCHY  (Screen1_PronounDrag GO — add this script here)
///  ─────────────────────────────────────────────────────────────────
///  Screen1_PronounDrag       [this script]
///    ├─ CardSpawnArea         RectTransform  (top of screen, cards spawned here)
///    ├─ HousesRow             HorizontalLayoutGroup
///    │    ├─ House_I          [PronounHouse_YouAndWeGame]   pronoun=I
///    │    ├─ House_He         [PronounHouse_YouAndWeGame]   pronoun=He
///    │    ├─ House_She        [PronounHouse_YouAndWeGame]   pronoun=She
///    │    ├─ House_It         [PronounHouse_YouAndWeGame]   pronoun=It
///    │    └─ House_We         [PronounHouse_YouAndWeGame]   pronoun=We
///    ├─ RobinSpeechBubble     TMP_Text  (hint / feedback text)
///    ├─ NextButton            Button    (disabled until all cards placed)
///    └─ SfxSource             AudioSource
///
///  CARD PREFAB  (PronounCard_Prefab):
///    PronounCard_Prefab   [RectTransform] [CanvasGroup] [PronounCard_YouAndWeGame]
///      └─ CardImage         Image   (set sprite per card in PronounCardData)
///
///  Inspector wiring:
///    cardPrefab          → PronounCard_Prefab
///    cardSpawnArea       → CardSpawnArea RectTransform
///    houses              → all 5 PronounHouse_YouAndWeGame components
///    robinSpeech         → RobinSpeechBubble TMP_Text
///    nextButton          → NextButton
///    sfxSource           → SfxSource
///    correctClip         → chime sound
///    wrongClip           → buzz sound
///    allDoneClip         → fanfare sound
///    cardData            → fill array (6-8 entries)
/// </summary>
public class PronounDragScreen_YouAndWeGame : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────
    [Header("Prefab & Areas")]
    public GameObject          cardPrefab;
    public RectTransform       cardSpawnArea;   // top strip, cards born here

    [Header("Houses")]
    public PronounHouse_YouAndWeGame[] houses;  // I, He, She, It, We

    [Header("UI")]
    public TMP_Text robinSpeech;
    public Button   nextButton;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip   correctClip;
    public AudioClip   wrongClip;
    public AudioClip   allDoneClip;

    [Header("Card Data")]
    public PronounCardData_YouAndWeGame[] cardData;  // 6-8 entries

    [Header("Timing")]
    public float fallSpeed      = 80f;   // pixels per second
    public float cardSpacing    = 3.5f;  // seconds between card drops

    // ── Private ──────────────────────────────────────────────────────
    private YouAndWeGameController_YouAndWeGame _controller;
    private List<PronounCard_YouAndWeGame>      _activeCards = new List<PronounCard_YouAndWeGame>();
    private int _totalCards;
    private int _placedCards;
    private int _currentCardIndex = 0;
    private Coroutine _spawnRoutine;

    // ── Public API ───────────────────────────────────────────────────
    public void Initialise(YouAndWeGameController_YouAndWeGame controller)
    {
        _controller = controller;
        ResetScreen();
        StartSpawning();
    }

    void ResetScreen()
    {
        // Destroy any leftover cards
        foreach (var c in _activeCards)
            if (c != null) Destroy(c.gameObject);
        _activeCards.Clear();

        // Reset houses
        foreach (var h in houses)
            h.Reset();

        _placedCards      = 0;
        _currentCardIndex = 0;
        _totalCards       = cardData != null ? cardData.Length : 0;

        if (nextButton != null)
        {
            nextButton.interactable = false;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        SetRobinSpeech("Help each picture find its home!");
    }

    void StartSpawning()
    {
        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        _spawnRoutine = StartCoroutine(SpawnCards());
    }

    // ── Spawn loop ───────────────────────────────────────────────────
    IEnumerator SpawnCards()
    {
        yield return new WaitForSeconds(0.8f);   // brief opening pause

        for (int i = 0; i < _totalCards; i++)
        {
            SpawnCard(cardData[i]);
            yield return new WaitForSeconds(cardSpacing);
        }
    }

    void SpawnCard(PronounCardData_YouAndWeGame data)
    {
        GameObject go = Instantiate(cardPrefab, cardSpawnArea);
        var card      = go.GetComponent<PronounCard_YouAndWeGame>();
        if (card == null) card = go.AddComponent<PronounCard_YouAndWeGame>();

        // Random X within spawn area
        float halfW = cardSpawnArea.rect.width * 0.5f - 60f;
        float randomX = Random.Range(-halfW, halfW);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(randomX, 80f);   // just above visible area

        card.Initialise(data, this, fallSpeed, cardSpawnArea);
        _activeCards.Add(card);
    }

    // ── Called by PronounCard when dropped ───────────────────────────
    public void OnCardDropped(PronounCard_YouAndWeGame card, PronounHouse_YouAndWeGame house)
    {
        if (house == null)
        {
            // Dropped in empty space — float back to falling
            card.ReturnToFall();
            return;
        }

        if (house.pronoun == card.Data.correctPronoun)
        {
            // ✅ Correct
            PlayClip(correctClip);
            house.PlayCorrectAnim();
            card.SnapToHouse(house);
            _placedCards++;
            SetRobinSpeech($"{house.pronoun}!");
            StartCoroutine(PunchRobinSpeech());

            if (_placedCards >= _totalCards)
                StartCoroutine(AllPlacedDelay());
        }
        else
        {
            // ❌ Wrong
            PlayClip(wrongClip);
            house.PlayHintGlow();
            card.ReturnToFall();
            SetRobinSpeech("Oops! Try the other house 😊");
        }
    }

    IEnumerator AllPlacedDelay()
    {
        yield return new WaitForSeconds(0.5f);
        PlayClip(allDoneClip);
        SetRobinSpeech("Amazing! All done! 🎉");
        if (nextButton != null) nextButton.interactable = true;
    }

    // ── Robin speech helpers ─────────────────────────────────────────
    void SetRobinSpeech(string text)
    {
        if (robinSpeech != null) robinSpeech.text = text;
    }

    IEnumerator PunchRobinSpeech()
    {
        if (robinSpeech == null) yield break;
        Vector3 orig = robinSpeech.transform.localScale;
        robinSpeech.transform.localScale = orig * 1.3f;
        yield return new WaitForSeconds(0.12f);
        robinSpeech.transform.localScale = orig;
    }

    // ── Next ─────────────────────────────────────────────────────────
    void OnNextClicked()
    {
        if (_spawnRoutine != null) StopCoroutine(_spawnRoutine);
        _controller?.ShowScreen2();
    }

    // ── Helpers ──────────────────────────────────────────────────────
    void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    public Canvas GetCanvas() => GetComponentInParent<Canvas>();
}

// ── Serializable data per card ────────────────────────────────────────
[System.Serializable]
public class PronounCardData_YouAndWeGame
{
    public Sprite cardSprite;          // picture to show on the card
    [Tooltip("I / He / She / It / We")]
    public string correctPronoun;      // must match PronounHouse.pronoun exactly
    public string cardLabel;           // optional small label under image
}
