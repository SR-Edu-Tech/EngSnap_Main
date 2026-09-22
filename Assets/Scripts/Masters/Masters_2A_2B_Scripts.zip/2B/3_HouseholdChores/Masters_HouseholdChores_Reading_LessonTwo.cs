using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// Unit 3: Household Chores — Reading Lesson Two (R02 Match — The Verb and the Job).
/// Pairs 10 verbs with their chore objects (2 pages of 5 pairs):
/// Left: Sweep, Iron, Make, Set, Dry, Do, Take, Feed, Wash, Clean.
/// Right: the floor, the clothes, the bed, the table, the dishes, the shopping, the trash out, the dog, the car, the window.
/// Features:
/// 1. Introduction voiceover plays first with matching locked.
/// 2. Once introduction completes, gameplay automatically unlocks so the student can play.
/// 3. Correct pair triggers glowing connection and reads the full chore sentence aloud.
/// 4. Pass threshold: >= 8 / 10 pairs.
/// </summary>
public class Masters_HouseholdChores_Reading_LessonTwo : Masters_PolishedCommunication_Reading_LessonTwo {

    [Header("Sentence Readout Audios (10 Verbatim Clips)")]
    [SerializeField] public AudioClip[] sentenceReadoutAudios = new AudioClip[10];

    [Header("UI Feedback & Header Overrides")]
    [SerializeField] private TextMeshProUGUI headerTitleTMP;
    [SerializeField] private TextMeshProUGUI choreSubtitleTMP;

    private bool isGameUnlocked = false;
    private Coroutine introRoutine;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
        useUniqueRightCards = false; // 1-to-1 matching: 5 distinct verb cards to 5 distinct chore objects per set
        pairsPerSet = 5; // 5 pairs per page (2 pages total = 10 pairs)
        InitializeHouseholdChoresPuzzles();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Reading;

        EnsureHeadersAndLabels();

        // Lock drag interactions until introduction finishes
        isGameUnlocked = false;

        introRoutine = StartCoroutine(IntroductionRoutine());
    }

    private void EnsureHeadersAndLabels() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            Transform parent = tmp.transform.parent;
            string pn = parent != null ? parent.name.ToLower() : "";

            if (n.Contains("branch") || n.Contains("header") || pn.Contains("branch") || pn.Contains("header")) {
                tmp.text = "HOUSEHOLD CHORES 🧹";
            } else if (n.Contains("lessontitle") || pn.Contains("lessontitle") || (n == "tmp" && pn.Contains("title"))) {
                tmp.text = "R02 Match — The Verb and the Job";
            } else if (n.Contains("instruction") || n.Contains("subtitle") || pn.Contains("instruction") || pn.Contains("subtitle")) {
                tmp.text = "Pair each verb with the chore object it belongs to!";
            }
        }
    }

    private IEnumerator IntroductionRoutine() {
        if (progressCountTMP != null) {
            progressCountTMP.text = "0/10";
        }

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(narratorSpeech.length + 0.3f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        UnlockGameplay();
    }

    public void UnlockGameplay() {
        isGameUnlocked = true;
    }

    public override void OnBeginDrag(PointerEventData eventData) {
        if (!isGameUnlocked) return;
        base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData) {
        if (!isGameUnlocked) return;
        base.OnDrag(eventData);
    }

    public override void OnEndDrag(PointerEventData eventData) {
        if (!isGameUnlocked) return;

        int prevCorrectCount = correctCount;
        base.OnEndDrag(eventData);

        // Check if a new match was made
        if (correctCount > prevCorrectCount) {
            int matchIndex = correctCount - 1;
            PlaySentenceReadout(matchIndex);
        }
    }

    private void PlaySentenceReadout(int index) {
        if (sentenceReadoutAudios != null && index >= 0 && index < sentenceReadoutAudios.Length) {
            AudioClip clip = sentenceReadoutAudios[index];
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }
    }

    protected override void OnNextButtonClicked() {
        if (!isGameUnlocked) {
            // Skip intro on click
            if (introRoutine != null) StopCoroutine(introRoutine);
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.StopVoiceOver();
            UnlockGameplay();
            return;
        }

        base.OnNextButtonClicked();
    }

    public void InitializeHouseholdChoresPuzzles() {
        useUniqueRightCards = false;
        pairsPerSet = 5;

        puzzles = new MatchPuzzle[] {
            // Level / Set 1 (5 Pairs)
            new MatchPuzzle { leftPhrase = "Sweep", rightPhrase = "the floor" },
            new MatchPuzzle { leftPhrase = "Iron", rightPhrase = "the clothes" },
            new MatchPuzzle { leftPhrase = "Make", rightPhrase = "the bed" },
            new MatchPuzzle { leftPhrase = "Set", rightPhrase = "the table" },
            new MatchPuzzle { leftPhrase = "Dry", rightPhrase = "the dishes" },

            // Level / Set 2 (5 Pairs)
            new MatchPuzzle { leftPhrase = "Do", rightPhrase = "the shopping" },
            new MatchPuzzle { leftPhrase = "Take", rightPhrase = "the trash out" },
            new MatchPuzzle { leftPhrase = "Feed", rightPhrase = "the dog" },
            new MatchPuzzle { leftPhrase = "Wash", rightPhrase = "the car" },
            new MatchPuzzle { leftPhrase = "Clean", rightPhrase = "the window" }
        };
    }
}
