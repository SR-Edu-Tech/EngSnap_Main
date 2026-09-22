using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// G01 Echo Sort — Two Arches, Fast Controller for Book 2B Unit 7 (Question Tags).
/// Timed sorting reaction game with 2 arches: POSITIVE TAG and NEGATIVE TAG.
/// Sentences slide down the arcade cave; student sorts each sentence into the matching arch.
/// 60-second timer, 3 lives, score target >= 12 items.
/// Completing this lesson unlocks the next game/branch.
/// </summary>
public class Masters_QuestionTags_Game_LessonOne : Masters_Lesson {

public enum QuestionTagType {
    PositiveTag,
    NegativeTag
}

[System.Serializable]
public class QuestionTags_GameItem {
    public string sentenceText;          // e.g. "You are a student, aren't you?"
    public QuestionTagType tagType;      // PositiveTag or NegativeTag
    public string tagExplanation;        // e.g. "Tag 'aren't you?' is negative."
    public AudioClip sentenceAudio;
}

    [Header("Question Tags Echo Sort Items")]
    [SerializeField]
    private QuestionTags_GameItem[] itemsPool;

    [Header("UI Headers & Counters")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI subtitleTMP;
    [SerializeField]
    private TextMeshProUGUI timerTMP;
    [SerializeField]
    private TextMeshProUGUI scoreTMP;
    [SerializeField]
    private TextMeshProUGUI livesTMP;
    [SerializeField]
    private TextMeshProUGUI hintTMP;

    [Header("Falling Sentence Card")]
    [SerializeField]
    private GameObject sentenceCardObject;
    [SerializeField]
    private Image sentenceCardBg;
    [SerializeField]
    private TextMeshProUGUI sentenceCardTextTMP;
    [SerializeField]
    private RectTransform topSpawnPoint;
    [SerializeField]
    private RectTransform bottomTargetPoint;
    [SerializeField]
    private Button listenEchoBtn;

    [Header("Two Arches (Sort Targets)")]
    [SerializeField]
    private Button positiveArchBtn;
    [SerializeField]
    private Button negativeArchBtn;
    [SerializeField]
    private Image positiveArchImg;
    [SerializeField]
    private Image negativeArchImg;
    [SerializeField]
    private TextMeshProUGUI positiveArchLabelTMP;
    [SerializeField]
    private TextMeshProUGUI negativeArchLabelTMP;

    [Header("Results Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultTitleTMP;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;
    [SerializeField]
    private Button returnHubBtn;

    [Header("Audio References")]
    [SerializeField]
    private AudioClip introAudio;

    [Header("Navigation")]
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    [Header("Game Configuration")]
    [SerializeField]
    private float roundDuration = 60f;
    [SerializeField]
    private int maxLives = 3;
    [SerializeField]
    private int passTargetScore = 12;
    [SerializeField]
    private float cardSlideDuration = 4.5f;

    private int score = 0;
    private int remainingLives = 3;
    private float remainingTime = 60f;
    private bool isGameRunning = false;
    private bool isProcessingCard = false;
    private int currentItemIndex = 0;
    private List<QuestionTags_GameItem> shuffledDeck = new List<QuestionTags_GameItem>();
    private Tween currentSlideTween = null;

    private readonly Color positiveArchColor = new Color(0.12f, 0.55f, 0.85f, 0.95f);
    private readonly Color negativeArchColor = new Color(0.85f, 0.40f, 0.15f, 0.95f);
    private readonly Color correctColor = new Color(0.15f, 0.78f, 0.35f, 1f);
    private readonly Color wrongColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Game;
        base.Awake();

        AutoBindReferences();

        if (positiveArchBtn != null) {
            positiveArchBtn.onClick.RemoveAllListeners();
            positiveArchBtn.onClick.AddListener(() => OnArchSelected(QuestionTagType.PositiveTag));
        }

        if (negativeArchBtn != null) {
            negativeArchBtn.onClick.RemoveAllListeners();
            negativeArchBtn.onClick.AddListener(() => OnArchSelected(QuestionTagType.NegativeTag));
        }

        if (listenEchoBtn != null) {
            listenEchoBtn.onClick.RemoveAllListeners();
            listenEchoBtn.onClick.AddListener(PlayCurrentSentenceAudio);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartLesson);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(OnReturnHubClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    protected override void Start() {
        base.Start();

        EnsureHeaderAndTitle();

        if (itemsPool == null || itemsPool.Length == 0) {
            PopulateDefaultItems();
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        if (sentenceCardObject != null) sentenceCardObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        StartCoroutine(InitializeLessonRoutine());
    }

    private IEnumerator InitializeLessonRoutine() {
        SetInteractiveState(false);

        if (Masters_AudioManager.Instance != null && introAudio != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(0.3f);
        StartGameSequence();
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP != null) headerTMP.text = "GAME BRANCH (Echo Sort)";
        if (titleTMP != null) titleTMP.text = "G01 Echo Sort — Two Arches, Fast";
        if (subtitleTMP != null) subtitleTMP.text = "Sort each sentence into the POSITIVE TAG or NEGATIVE TAG arch before time runs out!";
        if (hintTMP != null) hintTMP.text = "Left Arch: [+] POSITIVE TAG  |  Right Arch: [-] NEGATIVE TAG";
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (timerTMP == null && (n.Contains("timer") || n.Contains("time"))) timerTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (livesTMP == null && (n.Contains("lives") || n.Contains("heart"))) livesTMP = tmp;
            else if (hintTMP == null && (n.Contains("hint") || n.Contains("rule"))) hintTMP = tmp;
            else if (sentenceCardTextTMP == null && (n.Contains("sentence") || n.Contains("cardtext"))) sentenceCardTextTMP = tmp;
            else if (positiveArchLabelTMP == null && (n.Contains("poslabel") || n.Contains("positivearch"))) positiveArchLabelTMP = tmp;
            else if (negativeArchLabelTMP == null && (n.Contains("neglabel") || n.Contains("negativearch"))) negativeArchLabelTMP = tmp;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("positive") || n.Contains("leftarch") || n == "btn_positive") {
                positiveArchBtn = btn;
                positiveArchImg = btn.GetComponent<Image>();
            } else if (n.Contains("negative") || n.Contains("rightarch") || n == "btn_negative") {
                negativeArchBtn = btn;
                negativeArchImg = btn.GetComponent<Image>();
            } else if (listenEchoBtn == null && (n.Contains("listen") || n.Contains("replay") || n.Contains("audio"))) {
                listenEchoBtn = btn;
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (returnHubBtn == null && (n.Contains("hub") || n.Contains("complete"))) {
                returnHubBtn = btn;
            } else if (nextButton == null && n == "nextbutton") {
                nextButton = btn;
            }
        }
    }

    public void PopulateDefaultItems() {
        itemsPool = new QuestionTags_GameItem[] {
            // Positive Tags
            new QuestionTags_GameItem { sentenceText = "You aren't a teacher, are you?", tagType = QuestionTagType.PositiveTag, tagExplanation = "'are you?' is a POSITIVE tag" },
            new QuestionTags_GameItem { sentenceText = "They weren't late, were they?", tagType = QuestionTagType.PositiveTag, tagExplanation = "'were they?' is a POSITIVE tag" },
            new QuestionTags_GameItem { sentenceText = "He isn't crazy, is he?", tagType = QuestionTagType.PositiveTag, tagExplanation = "'is he?' is a POSITIVE tag" },
            new QuestionTags_GameItem { sentenceText = "You don't speak French, do you?", tagType = QuestionTagType.PositiveTag, tagExplanation = "'do you?' is a POSITIVE tag" },
            new QuestionTags_GameItem { sentenceText = "You didn't study for the test, did you?", tagType = QuestionTagType.PositiveTag, tagExplanation = "'did you?' is a POSITIVE tag" },
            new QuestionTags_GameItem { sentenceText = "You wouldn't stop me, would you?", tagType = QuestionTagType.PositiveTag, tagExplanation = "'would you?' is a POSITIVE tag" },
            new QuestionTags_GameItem { sentenceText = "You couldn't do it for me, could you?", tagType = QuestionTagType.PositiveTag, tagExplanation = "'could you?' is a POSITIVE tag" },
            new QuestionTags_GameItem { sentenceText = "We mustn't say anything, must we?", tagType = QuestionTagType.PositiveTag, tagExplanation = "'must we?' is a POSITIVE tag" },

            // Negative Tags
            new QuestionTags_GameItem { sentenceText = "You are a student, aren't you?", tagType = QuestionTagType.NegativeTag, tagExplanation = "'aren't you?' is a NEGATIVE tag" },
            new QuestionTags_GameItem { sentenceText = "He is very busy, isn't he?", tagType = QuestionTagType.NegativeTag, tagExplanation = "'isn't he?' is a NEGATIVE tag" },
            new QuestionTags_GameItem { sentenceText = "He was happy, wasn't he?", tagType = QuestionTagType.NegativeTag, tagExplanation = "'wasn't he?' is a NEGATIVE tag" },
            new QuestionTags_GameItem { sentenceText = "You speak English, don't you?", tagType = QuestionTagType.NegativeTag, tagExplanation = "'don't you?' is a NEGATIVE tag" },
            new QuestionTags_GameItem { sentenceText = "He studies French, doesn't he?", tagType = QuestionTagType.NegativeTag, tagExplanation = "'doesn't he?' is a NEGATIVE tag" },
            new QuestionTags_GameItem { sentenceText = "You will pass the exam, won't you?", tagType = QuestionTagType.NegativeTag, tagExplanation = "'won't you?' is a NEGATIVE tag" },
            new QuestionTags_GameItem { sentenceText = "You must be patient, mustn't you?", tagType = QuestionTagType.NegativeTag, tagExplanation = "'mustn't you?' is a NEGATIVE tag" },
            new QuestionTags_GameItem { sentenceText = "You have studied all week, haven't you?", tagType = QuestionTagType.NegativeTag, tagExplanation = "'haven't you?' is a NEGATIVE tag" }
        };
    }

    public void StartGameSequence() {
        score = 0;
        remainingLives = maxLives;
        remainingTime = roundDuration;
        isGameRunning = true;
        isProcessingCard = false;

        shuffledDeck.Clear();
        shuffledDeck.AddRange(itemsPool);
        ShuffleList(shuffledDeck);
        currentItemIndex = 0;

        if (resultPanel != null) resultPanel.SetActive(false);
        UpdateHUD();
        SetInteractiveState(true);

        StartCoroutine(GameTimerCoroutine());
        SpawnNextCard();
    }

    private IEnumerator GameTimerCoroutine() {
        while (isGameRunning && remainingTime > 0) {
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
            UpdateHUD();

            if (remainingTime <= 0) {
                EndGame(false, "Time's up!");
            }
        }
    }

    private void SpawnNextCard() {
        if (!isGameRunning) return;

        if (shuffledDeck.Count == 0 || currentItemIndex >= shuffledDeck.Count) {
            ShuffleList(shuffledDeck);
            currentItemIndex = 0;
        }

        var currentItem = shuffledDeck[currentItemIndex];
        isProcessingCard = false;

        if (sentenceCardObject != null) {
            sentenceCardObject.SetActive(true);
            RectTransform cardRt = sentenceCardObject.GetComponent<RectTransform>();

            if (topSpawnPoint != null && bottomTargetPoint != null) {
                cardRt.anchoredPosition = topSpawnPoint.anchoredPosition;
            } else {
                cardRt.anchoredPosition = new Vector2(0f, 150f);
            }

            sentenceCardObject.transform.localScale = Vector3.one;

            if (sentenceCardTextTMP != null) {
                sentenceCardTextTMP.text = $"\"{currentItem.sentenceText}\"";
                sentenceCardTextTMP.color = Color.white;
            }

            if (sentenceCardBg != null) {
                sentenceCardBg.color = new Color(0.08f, 0.12f, 0.22f, 0.96f);
            }

            Vector2 targetPos = (bottomTargetPoint != null) ? bottomTargetPoint.anchoredPosition : new Vector2(0f, -40f);

            currentSlideTween?.Kill();
            currentSlideTween = cardRt.DOAnchorPos(targetPos, cardSlideDuration)
                .SetEase(Ease.Linear)
                .OnComplete(OnCardReachedBottomWithoutSort);
        }

        PlayCurrentSentenceAudio();
    }

    private void OnCardReachedBottomWithoutSort() {
        if (!isGameRunning || isProcessingCard) return;
        isProcessingCard = true;

        remainingLives--;
        UpdateHUD();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        if (sentenceCardBg != null) sentenceCardBg.color = wrongColor;

        if (remainingLives <= 0) {
            EndGame(false, "No lives remaining!");
        } else {
            currentItemIndex++;
            StartCoroutine(DelayNextSpawn(0.6f));
        }
    }

    public void OnArchSelected(QuestionTagType selectedType) {
        if (!isGameRunning || isProcessingCard || currentItemIndex >= shuffledDeck.Count) return;
        isProcessingCard = true;

        currentSlideTween?.Kill();
        var item = shuffledDeck[currentItemIndex];
        bool isCorrect = (selectedType == item.tagType);

        Button targetBtn = (selectedType == QuestionTagType.PositiveTag) ? positiveArchBtn : negativeArchBtn;
        Image targetImg = (selectedType == QuestionTagType.PositiveTag) ? positiveArchImg : negativeArchImg;

        if (targetBtn != null) {
            targetBtn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
        }

        RectTransform cardRt = sentenceCardObject.GetComponent<RectTransform>();
        Vector2 archPos = (selectedType == QuestionTagType.PositiveTag) ? new Vector2(-280f, -140f) : new Vector2(280f, -140f);

        if (isCorrect) {
            score++;
            UpdateHUD();

            if (sentenceCardBg != null) sentenceCardBg.color = correctColor;
            if (targetImg != null) targetImg.color = correctColor;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            cardRt.DOAnchorPos(archPos, 0.35f).SetEase(Ease.InBack);
            sentenceCardObject.transform.DOScale(Vector3.zero, 0.35f).OnComplete(() => {
                ResetArchColors();
                currentItemIndex++;
                SpawnNextCard();
            });
        } else {
            remainingLives--;
            UpdateHUD();

            if (sentenceCardBg != null) sentenceCardBg.color = wrongColor;
            if (targetImg != null) targetImg.color = wrongColor;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            sentenceCardObject.transform.DOShakePosition(0.4f, 15f, 20).OnComplete(() => {
                ResetArchColors();
                if (remainingLives <= 0) {
                    EndGame(false, "No lives remaining!");
                } else {
                    currentItemIndex++;
                    SpawnNextCard();
                }
            });
        }
    }

    private void ResetArchColors() {
        if (positiveArchImg != null) positiveArchImg.color = positiveArchColor;
        if (negativeArchImg != null) negativeArchImg.color = negativeArchColor;
    }

    private IEnumerator DelayNextSpawn(float delay) {
        yield return new WaitForSeconds(delay);
        SpawnNextCard();
    }

    public void PlayCurrentSentenceAudio() {
        if (shuffledDeck == null || currentItemIndex >= shuffledDeck.Count) return;
        var item = shuffledDeck[currentItemIndex];

        if (item.sentenceAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(item.sentenceAudio);
        }
    }

    private void UpdateHUD() {
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
        if (timerTMP != null) timerTMP.text = $"Time: {(int)remainingTime}s";
        if (livesTMP != null) {
            string hearts = "";
            for (int i = 0; i < remainingLives; i++) hearts += "❤️ ";
            livesTMP.text = $"Lives: {hearts.Trim()}";
        }
    }

    private void EndGame(bool timeOut, string reason) {
        isGameRunning = false;
        currentSlideTween?.Kill();

        if (sentenceCardObject != null) sentenceCardObject.SetActive(false);

        ShowResults();
    }

    private void ShowResults() {
        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        bool passed = (score >= passTargetScore);

        if (resultTitleTMP != null) {
            resultTitleTMP.text = passed ? "ECHO SORT MASTERED!" : "KEEP PRACTICING!";
            resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f) : new Color(1f, 0.65f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Final Score: {score} Sentences Sorted";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed
                ? $"Awesome reaction speed! You sorted {score} question tag sentences accurately!"
                : $"You sorted {score} sentences. Target is at least {passTargetScore} to pass. Tap Retry to play again!";
        }

        if (retryBtn != null) retryBtn.gameObject.SetActive(!passed);
        if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(true);

        if (nextButton != null) {
            nextButton.gameObject.SetActive(passed);
            if (passed) NextButtonAnimation();
        }

        if (passed) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }
    }

    private void SetInteractiveState(bool state) {
        if (positiveArchBtn != null) positiveArchBtn.interactable = state;
        if (negativeArchBtn != null) negativeArchBtn.interactable = state;
    }

    public void RestartLesson() {
        StartGameSequence();
    }

    public void OnReturnHubClicked() {
        OnNextButtonClicked();
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    private void ShuffleList<T>(List<T> list) {
        for (int i = 0; i < list.Count; i++) {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
