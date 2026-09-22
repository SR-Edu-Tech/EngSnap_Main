using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Masters_ColourYourSpeech_Game_LessonOne : Masters_Lesson {
    [System.Serializable]
    public class IdiomItem {
        public string idiom;
        public int correctBucketIndex; // 0: EASY, 1: CRAZY, 2: HAPPY, 3: JOKING, 4: KIND, 5: BOSSY
        public string meaning;
    }

    [System.Serializable]
    public class MeaningBucket {
        public string meaningName;
        public RectTransform bucketRect;
        public TextMeshProUGUI labelTMP;
        public Image bucketImage;
        public Image splatVFX;
        public Button selectButton;
    }

    [Header("Idiom Data (9 Official Items)")]
    [SerializeField] private IdiomItem[] idiomDataArray;

    [Header("Buckets (6 Required Meanings)")]
    [SerializeField] private MeaningBucket[] buckets = new MeaningBucket[6];

    [Header("Game Settings")]
    [SerializeField] private int requiredProcessedCount = 6;
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float initialSpawnDelay = 1.5f;
    [SerializeField] private float initialSpawnInterval = 3.5f;
    [SerializeField] private float minSpawnInterval = 1.2f;
    [SerializeField] private float spawnIntervalDecreaseRate = 0.04f;
    [SerializeField] private float initialFallSpeed = 180f;
    [SerializeField] private float maxFallSpeed = 480f;
    [SerializeField] private float fallSpeedIncreaseRate = 5f;
    [SerializeField] private float missThresholdY = -450f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI livesTMP;
    [SerializeField] private Image[] heartIcons;
    [SerializeField] private RectTransform topSpawnPoint;
    [SerializeField] private RectTransform laneContainer;
    [SerializeField] private ColourYourSpeech_PaintPot potPrefab;

    [Header("Completion & Modal UI")]
    [SerializeField] private GameObject completedPanel;
    [SerializeField] private TextMeshProUGUI completedTitleTMP;
    [SerializeField] private TextMeshProUGUI completedScoreTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Audio")]
    [SerializeField] private AudioClip voAria;
    [SerializeField] private AudioClip sfxSplatGood;
    [SerializeField] private AudioClip sfxSplatBad;
    [SerializeField] private AudioClip musArcade;

    // Runtime state
    private float timeRemaining;
    private int score;
    private int processedCount;
    private int currentLives;
    private bool isGameActive;
    private bool isRoundFinished;
    private float currentSpawnInterval;
    private float spawnTimer;
    private float currentFallSpeed;

    private List<ColourYourSpeech_PaintPot> activePots = new List<ColourYourSpeech_PaintPot>();
    private ColourYourSpeech_PaintPot selectedPot = null;
    private List<int> idiomHistory = new List<int>();

    // Pot Colors for vibrant arcade visuals
    private static readonly Color[] PotPalette = new Color[] {
        new Color(0.20f, 0.60f, 1.00f), // Easy - Sky Blue
        new Color(0.95f, 0.30f, 0.45f), // Crazy - Crimson / Pink
        new Color(1.00f, 0.75f, 0.15f), // Happy - Bright Amber
        new Color(0.60f, 0.35f, 0.90f), // Joking - Purple
        new Color(0.20f, 0.80f, 0.45f), // Kind - Emerald Green
        new Color(0.95f, 0.50f, 0.15f)  // Bossy - Orange
    };

    public int Score => score;
    public int ProcessedCount => processedCount;
    public int RequiredProcessedCount => requiredProcessedCount;
    public int Lives => currentLives;
    public float TimeRemaining => timeRemaining;
    public bool IsGameActive => isGameActive;
    public IdiomItem[] IdiomDataArray => idiomDataArray;
    public MeaningBucket[] Buckets => buckets;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;

        if (potPrefab != null) {
            potPrefab.gameObject.SetActive(false);
        }

        if (retryButton != null) {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }

        InitializeBucketButtons();
    }

    /// <summary>
    /// Retry handler distinguishing in-game retry with remaining lives from full game restart after game over.
    /// </summary>
    public void OnRetryButtonClicked() {
        if (currentLives > 0 && !isRoundFinished) {
            // Resume current attempt: preserve lives, score, and completed idioms
            if (completedPanel != null) completedPanel.SetActive(false);
            if (retryButton != null) retryButton.gameObject.SetActive(false);
            isGameActive = true;
            UpdateUI();
        } else {
            // Full restart after game over (zero lives) or completed run
            RestartGame();
        }
    }

    protected override void Start() {
        base.Start();

        if (voAria != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(voAria);
        } else if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }

        if (musArcade != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayMusic(musArcade);
        }

        RestartGame();
    }

    private void InitializeBucketButtons() {
        if (buckets == null) return;
        for (int i = 0; i < buckets.Length; i++) {
            int bucketIdx = i;
            var bucket = buckets[i];
            if (bucket != null && bucket.selectButton != null) {
                bucket.selectButton.onClick.RemoveAllListeners();
                bucket.selectButton.onClick.AddListener(() => OnBucketClicked(bucketIdx));
            }
        }
    }

    public void RestartGame() {
        StopAllCoroutines();

        // Clear all active pots safely
        foreach (var pot in activePots) {
            if (pot != null && pot.gameObject != null) {
                Destroy(pot.gameObject);
            }
        }
        activePots.Clear();
        selectedPot = null;
        idiomHistory.Clear();

        score = 0;
        processedCount = 0;
        currentLives = startingLives;
        timeRemaining = gameDuration;
        currentSpawnInterval = initialSpawnInterval;
        currentFallSpeed = initialFallSpeed;
        spawnTimer = 0f;
        isRoundFinished = false;

        if (completedPanel != null) completedPanel.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (nextButton != null) nextButton.interactable = false;

        UpdateUI();
        StartCoroutine(InitialStartSequence());
    }

    private IEnumerator InitialStartSequence() {
        isGameActive = false;
        yield return new WaitForSeconds(initialSpawnDelay);
        isGameActive = true;
        SpawnPot();
    }

    private void Update() {
        if (!isGameActive || isRoundFinished) return;

        // Timer countdown
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f) {
            timeRemaining = 0f;
            UpdateUI();
            FinishRound(success: currentLives > 0);
            return;
        }

        // Speed ramp
        currentSpawnInterval -= spawnIntervalDecreaseRate * Time.deltaTime;
        if (currentSpawnInterval < minSpawnInterval) currentSpawnInterval = minSpawnInterval;

        currentFallSpeed += fallSpeedIncreaseRate * Time.deltaTime;
        if (currentFallSpeed > maxFallSpeed) currentFallSpeed = maxFallSpeed;

        // Spawning timer
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentSpawnInterval) {
            spawnTimer = 0f;
            SpawnPot();
        }

        UpdateUI();
        UpdatePots();
    }

    private void UpdateUI() {
        if (timerTMP != null) {
            timerTMP.text = $"{Mathf.CeilToInt(timeRemaining)}s";
        }
        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {score}";
        }
        if (livesTMP != null) {
            if (currentLives > 0) {
                string hearts = "";
                for (int i = 0; i < currentLives; i++) {
                    hearts += (i == 0 ? "♥" : " ♥");
                }
                livesTMP.text = $"LIVES: {hearts}";
            } else {
                livesTMP.text = "LIVES: 0";
            }
        }

        if (heartIcons != null) {
            for (int i = 0; i < heartIcons.Length; i++) {
                if (heartIcons[i] != null) {
                    heartIcons[i].color = (i < currentLives) ? Color.white : new Color(1f, 1f, 1f, 0.2f);
                }
            }
        }
    }

    public void SpawnPot() {
        if (idiomDataArray == null || idiomDataArray.Length == 0) return;
        if (potPrefab == null || laneContainer == null) return;
        if (!isGameActive || isRoundFinished || processedCount >= requiredProcessedCount) return;

        // Choose idiom with anti-repetition filter
        int selectedIndex = 0;
        int attempts = 0;
        do {
            selectedIndex = UnityEngine.Random.Range(0, idiomDataArray.Length);
            attempts++;
        } while (idiomHistory.Contains(selectedIndex) && attempts < 20);

        idiomHistory.Add(selectedIndex);
        if (idiomHistory.Count > Mathf.Max(3, idiomDataArray.Length / 2)) {
            idiomHistory.RemoveAt(0);
        }

        IdiomItem item = idiomDataArray[selectedIndex];
        if (item == null) return;

        ColourYourSpeech_PaintPot newPot = Instantiate(potPrefab, laneContainer);
        Color potColor = PotPalette[item.correctBucketIndex % PotPalette.Length];
        newPot.Initialize(item.idiom, item.correctBucketIndex, potColor);

        RectTransform potRect = newPot.GetComponent<RectTransform>();
        if (potRect != null) {
            potRect.localScale = Vector3.one;
            if (topSpawnPoint != null) {
                potRect.anchoredPosition = topSpawnPoint.anchoredPosition;
            } else {
                potRect.anchoredPosition = new Vector2(0f, 360f);
            }
        }

        newPot.gameObject.SetActive(true);

        // Bind drag & click events
        newPot.OnPotClicked += HandlePotClicked;
        newPot.OnPotDragStarted += HandlePotDragStarted;
        newPot.OnPotDragEnded += HandlePotDragEnded;

        activePots.Add(newPot);
    }

    private void HandlePotClicked(ColourYourSpeech_PaintPot pot) {
        if (pot == null || pot.IsResolved || !isGameActive) return;

        if (selectedPot != null && selectedPot != pot) {
            selectedPot.transform.DOScale(Vector3.one, 0.1f);
        }
        selectedPot = pot;
        selectedPot.transform.DOScale(Vector3.one * 1.15f, 0.1f);
    }

    private void HandlePotDragStarted(ColourYourSpeech_PaintPot pot) {
        if (pot == null || pot.IsResolved || !isGameActive) return;
        selectedPot = pot;
    }

    private void HandlePotDragEnded(ColourYourSpeech_PaintPot pot) {
        if (pot == null || pot.IsResolved || !isGameActive) return;

        // Check if pot was dropped over or near any bucket
        MeaningBucket targetBucket = FindClosestBucket(pot.transform.position, 180f);
        if (targetBucket != null) {
            int bucketIndex = Array.IndexOf(buckets, targetBucket);
            ResolvePotSubmission(pot, bucketIndex, targetBucket);
        } else {
            // Return towards lane center
            RectTransform potRect = pot.GetComponent<RectTransform>();
            if (potRect != null) {
                potRect.DOAnchorPosX(0f, 0.2f).SetEase(Ease.OutQuad);
            }
        }
    }

    public void OnBucketClicked(int bucketIndex) {
        if (!isGameActive || isRoundFinished) return;
        if (selectedPot == null || selectedPot.IsResolved) {
            // If no pot explicitly clicked, pick the lowest active pot in the lane
            selectedPot = GetLowestActivePot();
        }

        if (selectedPot != null && !selectedPot.IsResolved && bucketIndex >= 0 && bucketIndex < buckets.Length) {
            ResolvePotSubmission(selectedPot, bucketIndex, buckets[bucketIndex]);
            selectedPot = null;
        }
    }

    private ColourYourSpeech_PaintPot GetLowestActivePot() {
        ColourYourSpeech_PaintPot lowest = null;
        float lowestY = float.MaxValue;
        foreach (var pot in activePots) {
            if (pot != null && !pot.IsResolved) {
                if (pot.transform.position.y < lowestY) {
                    lowestY = pot.transform.position.y;
                    lowest = pot;
                }
            }
        }
        return lowest;
    }

    private MeaningBucket FindClosestBucket(Vector3 worldPos, float maxDistanceThreshold) {
        if (buckets == null) return null;
        MeaningBucket closest = null;
        float minDist = float.MaxValue;

        foreach (var b in buckets) {
            if (b == null || b.bucketRect == null) continue;
            float dist = Vector2.Distance(b.bucketRect.position, worldPos);
            if (dist < minDist) {
                minDist = dist;
                closest = b;
            }
        }

        return (minDist <= maxDistanceThreshold) ? closest : null;
    }

    private void UpdatePots() {
        float deltaY = currentFallSpeed * Time.deltaTime;

        for (int i = activePots.Count - 1; i >= 0; i--) {
            var pot = activePots[i];
            if (pot == null) {
                activePots.RemoveAt(i);
                continue;
            }

            if (pot.IsResolved) continue;

            // Move pot down only if not actively dragged
            if (!pot.IsDragging) {
                pot.MoveDown(deltaY);

                RectTransform potRect = pot.GetComponent<RectTransform>();
                if (potRect != null) {
                    // Miss check when reaching the bottom of the lane
                    if (potRect.anchoredPosition.y <= missThresholdY) {
                        HandleMissedPot(pot);
                    }
                }
            }
        }
    }

    private void HandleMissedPot(ColourYourSpeech_PaintPot pot) {
        if (pot == null || !pot.MarkResolved()) return;

        activePots.Remove(pot);
        if (selectedPot == pot) selectedPot = null;
        pot.DisappearImmediate();

        processedCount++;

        // Missed pot counts as a bad resolution / life loss
        LoseLife();

        if (Masters_AudioManager.Instance != null) {
            if (sfxSplatBad != null) Masters_AudioManager.Instance.PlaySoundEffect(sfxSplatBad);
            else Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        if (currentLives > 0 && processedCount >= requiredProcessedCount) {
            FinishRound(success: true);
        }
    }

    public void ResolvePotSubmission(ColourYourSpeech_PaintPot pot, int submittedBucketIndex, MeaningBucket bucket) {
        if (pot == null || pot.IsResolved || !isGameActive || isRoundFinished) return;
        if (!pot.MarkResolved()) return;

        activePots.Remove(pot);
        if (selectedPot == pot) selectedPot = null;
        pot.DisappearImmediate();

        processedCount++;

        bool isCorrect = (submittedBucketIndex == pot.CorrectBucketIndex);

        if (isCorrect) {
            score++;
            UpdateUI();

            if (Masters_AudioManager.Instance != null) {
                if (sfxSplatGood != null) Masters_AudioManager.Instance.PlaySoundEffect(sfxSplatGood);
                else Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Animate bucket punch & splat VFX
            if (bucket != null && bucket.bucketRect != null) {
                bucket.bucketRect.DOPunchScale(Vector3.one * 0.25f, 0.3f);
                if (bucket.splatVFX != null) {
                    bucket.splatVFX.gameObject.SetActive(true);
                    bucket.splatVFX.transform.localScale = Vector3.zero;
                    bucket.splatVFX.color = PotPalette[submittedBucketIndex % PotPalette.Length];
                    bucket.splatVFX.transform.DOScale(Vector3.one * 1.3f, 0.25f).SetEase(Ease.OutBack)
                        .OnComplete(() => bucket.splatVFX.DOFade(0f, 0.4f).OnComplete(() => bucket.splatVFX.gameObject.SetActive(false)));
                }
            }
        } else {
            LoseLife();

            if (Masters_AudioManager.Instance != null) {
                if (sfxSplatBad != null) Masters_AudioManager.Instance.PlaySoundEffect(sfxSplatBad);
                else Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (bucket != null && bucket.bucketRect != null) {
                bucket.bucketRect.DOShakePosition(0.3f, 15f, 15);
            }
        }

        // Pass condition check: 6 idioms processed
        if (currentLives > 0 && processedCount >= requiredProcessedCount) {
            FinishRound(success: true);
        }
    }

    private void LoseLife() {
        if (currentLives > 0) {
            currentLives--;
            UpdateUI();

            if (currentLives <= 0) {
                currentLives = 0;
                FinishRound(success: false);
            }
        }
    }

    private void FinishRound(bool success) {
        if (isRoundFinished) return;
        isRoundFinished = true;
        isGameActive = false;

        // Clear remaining active pots immediately
        foreach (var pot in activePots) {
            if (pot != null && pot.gameObject != null) {
                pot.gameObject.SetActive(false);
                Destroy(pot.gameObject);
            }
        }
        activePots.Clear();
        selectedPot = null;

        if (success) {
            // Save progression in M3A_U6_HubProgress
            M3A_U6_HubProgress.MarkG01Complete();

            if (completedPanel != null) {
                completedPanel.SetActive(true);
                if (completedTitleTMP != null) completedTitleTMP.text = "<color=#2ECC71>Great Job!</color>\nIdiom Splat Master!";
                if (completedScoreTMP != null) completedScoreTMP.text = $"Final Score: {score}/{requiredProcessedCount}";
                completedPanel.transform.localScale = Vector3.zero;
                completedPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }

            if (retryButton != null) retryButton.gameObject.SetActive(true);
            if (nextButton != null) {
                nextButton.interactable = true;
                nextButton.gameObject.SetActive(true);
                NextButtonAnimation();
            }
        } else {
            // Failed (lives = 0)
            if (completedPanel != null) {
                completedPanel.SetActive(true);
                if (completedTitleTMP != null) completedTitleTMP.text = "<color=#E74C3C>Out of Lives!</color>";
                if (completedScoreTMP != null) completedScoreTMP.text = $"Score: {score}\nTap Retry to try again!";
                completedPanel.transform.localScale = Vector3.zero;
                completedPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }

            if (retryButton != null) retryButton.gameObject.SetActive(true);
            if (nextButton != null) nextButton.interactable = false;
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        // Progression sub-flag guarantee
        M3A_U6_HubProgress.MarkG01Complete();

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            // Topic Final Screen Rule: return directly to Topic Selection Hub
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    private void OnDestroy() {
        StopAllCoroutines();
        DOTween.Kill(transform);
        if (completedPanel != null) DOTween.Kill(completedPanel.transform);
        if (nextButton != null) DOTween.Kill(nextButton.transform);
        if (buckets != null) {
            foreach (var bucket in buckets) {
                if (bucket != null && bucket.bucketRect != null) DOTween.Kill(bucket.bucketRect);
                if (bucket != null && bucket.splatVFX != null) DOTween.Kill(bucket.splatVFX.transform);
            }
        }
    }
}
