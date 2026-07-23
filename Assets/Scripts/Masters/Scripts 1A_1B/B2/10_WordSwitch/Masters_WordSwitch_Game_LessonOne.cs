using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Masters_WordSwitch_Game_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class SparkItem {
        public string synonymText;
        public string homeBankKey;
    }

    [Header("Game Data Pool")]
    [SerializeField] private SparkItem[] sparkPoolArray;

    [Header("UI Binding")]
    [SerializeField] private Masters_UniversalSortBin[] sortBinArray;
    [SerializeField] private Masters_UniversalSortPhraseCard cardTemplatePrefab;
    [SerializeField] private RectTransform spawnAreaRectTransform;
    [SerializeField] private GameObject[] livesImageArray;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private GameObject completedPanel;
    [SerializeField] private TextMeshProUGUI completedTitleTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Game Guardrails & Timers")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float sparkLifetime = 7.0f;
    [SerializeField] private int maxActiveSparksOnScreen = 6;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int winTargetScore = 15;

    private float timeRemaining;
    private float spawnTimer;
    private int score;
    private int currentLives;
    private bool isGameActive;

    private List<Masters_WordSwitch_FloatingSpark> activeSparks = new List<Masters_WordSwitch_FloatingSpark>();
    private readonly string[] binLabels = { "SAW", "SAID", "NICE", "LITTLE", "FUNNY", "GOOD", "PRETTY", "HAPPY", "SAD", "SMART", "LAUGHED" };

    protected override void Awake() {
        base.Awake();
        if (retryButton != null) retryButton.onClick.AddListener(RestartGame);
        if (completedPanel != null) completedPanel.SetActive(false);
        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
            nextButton.interactable = false;
        }
    }

    protected override void Start() {
        base.Start();
        RestartGame();
    }

    public void RestartGame() {
        if (completedPanel != null) completedPanel.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        foreach (var spk in activeSparks) {
            if (spk != null) Destroy(spk.gameObject);
        }
        activeSparks.Clear();

        score = 0;
        currentLives = startingLives;
        timeRemaining = gameDuration;
        spawnTimer = 0.5f; // First spark spawns almost instantly
        isGameActive = true;

        if (livesImageArray != null) {
            foreach (var heart in livesImageArray) {
                if (heart != null) heart.SetActive(true);
            }
        }

        UpdateScoreUI();
        UpdateTimerUI();

        if (cardTemplatePrefab != null) cardTemplatePrefab.gameObject.SetActive(false);
    }

    private void Update() {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0) {
            timeRemaining = 0;
            UpdateTimerUI();
            EndGame(score >= winTargetScore || score > 0);
            return;
        }
        UpdateTimerUI();

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0) {
            spawnTimer = spawnInterval;
            TrySpawnSpark();
        }
    }

    private void TrySpawnSpark() {
        if (!isGameActive || sparkPoolArray == null || sparkPoolArray.Length == 0 || spawnAreaRectTransform == null || cardTemplatePrefab == null) return;
        
        // Guardrail: Pause spawning if clutter limit reached
        if (activeSparks.Count >= maxActiveSparksOnScreen) return;

        SparkItem item = sparkPoolArray[UnityEngine.Random.Range(0, sparkPoolArray.Length)];
        if (item == null || string.IsNullOrEmpty(item.synonymText)) return;

        Masters_UniversalSortPhraseCard clone = Instantiate(cardTemplatePrefab, spawnAreaRectTransform);
        clone.SetSortIdAndExpression(0, item.synonymText);
        if (clone.GetButton() != null) clone.GetButton().enabled = false; // Disable button click so drag works cleanly

        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        cloneRect.anchoredPosition = new Vector2(
            UnityEngine.Random.Range(-180f, 180f),
            UnityEngine.Random.Range(-120f, 120f)
        );
        clone.gameObject.SetActive(true);

        var floatSpark = clone.gameObject.AddComponent<Masters_WordSwitch_FloatingSpark>();
        floatSpark.Init(item.synonymText, item.homeBankKey, sparkLifetime, UnityEngine.Random.Range(130f, 180f), this);
        activeSparks.Add(floatSpark);
    }

    public bool IsGameActive() => isGameActive;

    public void OnSparkExpired(Masters_WordSwitch_FloatingSpark spark) {
        if (spark == null) return;
        activeSparks.Remove(spark);
        Destroy(spark.gameObject);
    }

    public void OnSparkDropped(Masters_WordSwitch_FloatingSpark spark, PointerEventData eventData) {
        if (!isGameActive || spark == null) return;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        Masters_UniversalSortBin targetBin = null;
        foreach (var r in results) {
            targetBin = r.gameObject.GetComponentInParent<Masters_UniversalSortBin>();
            if (targetBin != null) break;
        }

        if (targetBin == null) {
            // Dropped in empty air -> resumes bouncing mid-air
            return;
        }

        int binId = targetBin.GetSortId();
        string binLabel = (binId >= 0 && binId < binLabels.Length) ? binLabels[binId] : "";

        bool isCorrect = (spark.homeBankKey == binLabel);
        
        // Special Witty Dual-Membership Rule
        if (spark.synonymText.Equals("witty", StringComparison.OrdinalIgnoreCase) && (binLabel == "SMART" || binLabel == "FUNNY")) {
            isCorrect = true;
        }

        if (isCorrect) {
            score++;
            UpdateScoreUI();
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            targetBin.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.3f);

            activeSparks.Remove(spark);
            spark.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => Destroy(spark.gameObject));

            if (score >= winTargetScore) {
                EndGame(true);
            }
        } else {
            currentLives--;
            if (currentLives >= 0 && currentLives < livesImageArray.Length && livesImageArray[currentLives] != null) {
                livesImageArray[currentLives].SetActive(false);
            }
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            targetBin.transform.DOShakePosition(0.3f, 8f);

            if (currentLives <= 0) {
                activeSparks.Remove(spark);
                Destroy(spark.gameObject);
                EndGame(false);
            }
        }
    }

    private void EndGame(bool won) {
        isGameActive = false;
        foreach (var spk in activeSparks) {
            if (spk != null) Destroy(spk.gameObject);
        }
        activeSparks.Clear();

        if (completedPanel != null) {
            completedPanel.SetActive(true);
            if (completedTitleTMP != null) {
                completedTitleTMP.text = won ? "LEVEL COMPLETE!" : "GAME OVER!";
                completedTitleTMP.color = won ? new Color(0.18f, 0.6f, 0.2f) : Color.red;
            }
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = won;
        }

        if (won) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            NextButtonAnimation();
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private void UpdateScoreUI() {
        if (scoreTMP != null) scoreTMP.text = $"{score}/{winTargetScore}";
    }

    private void UpdateTimerUI() {
        if (timerTMP != null) {
            int mins = Mathf.FloorToInt(timeRemaining / 60f);
            int secs = Mathf.FloorToInt(timeRemaining % 60f);
            timerTMP.text = $"{mins:00}:{secs:00}";
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
