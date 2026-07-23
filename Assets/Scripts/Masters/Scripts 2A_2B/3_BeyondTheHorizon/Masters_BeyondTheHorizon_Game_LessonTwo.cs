using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Unit 3: Beyond The Horizon - Game Lesson Two (G02: Route Runner — Grid Path Puzzle)
/// Standalone Controller inheriting directly from Masters_Lesson.
/// 
/// Mechanics exactly per dev-team spec:
/// - 4x4 Grid Board stored as a clean 16-element string array of labels (`gridLabels`).
/// - Only Landmarks and Start ('Start (S)') are labeled.
/// - The Goal tile is completely unlabeled (`""`) so the player must deduce its position from the instructions!
/// - Direction hint (`routeDirectionsTMP`) ONLY displays if the player clicks wrong tiles more than 2 times (`consecutiveWrongClicks >= 2`).
/// - All strings use strict ASCII characters (no special unicode symbols or arrows) to prevent Unity font warnings.
/// - Retry and Close buttons cleanly wired in both game state and end quiz screen.
/// </summary>
public class Masters_BeyondTheHorizon_Game_LessonTwo : Masters_Lesson {

    [Header("Route Runner UI Configuration")]
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI routeProgressTMP;
    [SerializeField] private TextMeshProUGUI routeTitleTMP;
    [SerializeField] private TextMeshProUGUI routeDirectionsTMP;
    [SerializeField] private Transform phraseRailContainer; // Renders the 4x4 Grid Board (SpawnArea)
    [SerializeField] private RectTransform leoAvatar;       // LEO's piece that hops across grid buttons

    [Header("Game State UI")]
    [SerializeField] private GameObject gamePlayGameObject;
    [SerializeField] private GameObject quizCompleteGameObject;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Route Target Definitions")]
    [SerializeField] private List<TargetRouteData> targetRoutes = new List<TargetRouteData>();

    [System.Serializable]
    public class RouteNavigationStep {
        public string stepInstruction; // e.g. "Step 1/2: 'Go straight...' - Tap the Junction tile!"
        public int targetTileIndex;    // Grid index the player must move LEO onto to clear this step
        public List<int> intermediateTileIndices = new List<int>(); // Tiles leading up to and including target
    }

    [System.Serializable]
    public class TargetRouteData {
        public string routeTitle;      // e.g. "Route 1: To the School"
        public string fullSummaryText; // e.g. "'Go straight...' -> 'Turn right...' -> 'It is beside...'"
        public int startTileIndex;     // Where LEO starts ('S')
        public int goalTileIndex;      // Final unlabeled goal tile ('')
        public string[] gridLabels = new string[16]; // 16-element array: "" for empty/goal, label text for landmarks
        public List<RouteNavigationStep> navigationSteps = new List<RouteNavigationStep>();
    }

    private int currentRouteIndex = 0;
    private int currentStepIndex = 0;
    private int score = 0;
    private bool isRouteActive = false;
    private List<Button> activeGridButtons = new List<Button>();
    private int currentLeoTileIndex = -1;
    private int consecutiveWrongClicks = 0;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    protected override void Start() {
        base.Start();
        AutoFindReferences();
        InitializeDefaultRoutes();

        if (retryButton != null) {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryGame);
        }
        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseGame);
        }

        StartGame();
    }

    private void AutoFindReferences() {
        if (scoreTMP == null) {
            Transform t = FindTransformRecursive(transform, "ScoreTMP") ?? FindTransformRecursive(transform, "ProgressionCountTMP");
            if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (routeTitleTMP == null) {
            Transform t = FindTransformRecursive(transform, "RouteTitleTMP") ?? FindTransformRecursive(transform, "LessonTitle");
            if (t != null) routeTitleTMP = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (routeProgressTMP == null) {
            Transform t = FindTransformRecursive(transform, "RouteProgressTMP");
            if (t != null) routeProgressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (phraseRailContainer == null) {
            phraseRailContainer = FindTransformRecursive(transform, "PhraseRailContainer") ?? FindTransformRecursive(transform, "SpawnArea");
        }
        if (routeDirectionsTMP == null) {
            Transform t = FindTransformRecursive(transform, "RouteDirectionsTMP");
            if (t != null) routeDirectionsTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (leoAvatar == null) {
            Transform t = FindTransformRecursive(transform, "LeoAvatar") ?? FindTransformRecursive(transform, "CharacterAvatar");
            if (t != null) leoAvatar = t.GetComponent<RectTransform>();
        }

        // Wire Retry and Close buttons automatically if present in hierarchy
        if (retryButton == null) {
            Transform t = FindTransformRecursive(transform, "RetryButton");
            if (t != null) retryButton = t.GetComponent<Button>();
        }
        if (retryButton != null) {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryGame);
        }

        if (closeButton == null) {
            Transform t = FindTransformRecursive(transform, "CloseButton") ?? FindTransformRecursive(transform, "BackButton") ?? FindTransformRecursive(transform, "ExitButton");
            if (t != null) closeButton = t.GetComponent<Button>();
        }
        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseGame);
        }
    }

    private Transform FindTransformRecursive(Transform parent, string nameContains) {
        Transform[] transforms = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in transforms) {
            if (t.name.ToLower().Contains(nameContains.ToLower())) return t;
        }
        return null;
    }

    private void InitializeDefaultRoutes() {
        if (targetRoutes != null && targetRoutes.Count > 0) return;

        targetRoutes = new List<TargetRouteData>() {
            // Route 1: School at 11, Goal at 7 (Beside School). Start at 13 -> straight to Junction at 5 -> turn right to Goal at 7.
            new TargetRouteData {
                routeTitle = "Route 1: To the School",
                fullSummaryText = "'Go straight...' -> 'Turn right from the junction.' -> 'It is beside the School.'",
                startTileIndex = 13,
                goalTileIndex = 7,
                gridLabels = new string[16] {
                    "", "", "Clinic", "",
                    "", "Junction", "", "", // [7] is Goal (unlabeled)
                    "Bakery", "", "", "School", // [11] is School
                    "", "Start (S)", "", ""  // [13] is Start
                },
                navigationSteps = new List<RouteNavigationStep>() {
                    new RouteNavigationStep {
                        stepInstruction = "Step 1/2: 'Go straight...' - Move LEO straight up from Start to the Junction!",
                        targetTileIndex = 5,
                        intermediateTileIndices = new List<int>() { 9, 5 }
                    },
                    new RouteNavigationStep {
                        stepInstruction = "Step 2/2: 'Turn right from the junction -> It is beside...' - Move right to the tile beside the School!",
                        targetTileIndex = 7,
                        intermediateTileIndices = new List<int>() { 6, 7 }
                    }
                }
            },
            // Route 2: Park at 0, Market at 2, Goal at 4 (Opposite Park). Start at 14 -> straight past Library (10) to 6 -> walk along road left to Goal at 4.
            new TargetRouteData {
                routeTitle = "Route 2: To the Park",
                fullSummaryText = "'Go past the Library...' -> 'Walk along the road.' -> 'It's opposite to the Park.'",
                startTileIndex = 14,
                goalTileIndex = 4,
                gridLabels = new string[16] {
                    "Park", "", "Market", "", // [0] is Park, [2] is Market
                    "", "", "", "",            // [4] is Goal (unlabeled), [6] is turn corner
                    "", "", "Library", "",     // [10] is Library
                    "", "", "Start (S)", ""    // [14] is Start
                },
                navigationSteps = new List<RouteNavigationStep>() {
                    new RouteNavigationStep {
                        stepInstruction = "Step 1/2: 'Go past the Library...' - Move straight up past the Library!",
                        targetTileIndex = 6,
                        intermediateTileIndices = new List<int>() { 10, 6 }
                    },
                    new RouteNavigationStep {
                        stepInstruction = "Step 2/2: 'Walk along the road -> It's opposite...' - Walk left along the road to the tile opposite the Park!",
                        targetTileIndex = 4,
                        intermediateTileIndices = new List<int>() { 5, 4 }
                    }
                }
            },
            // Route 3: Junction at 4, Station at 1, Goal at 5 (Station on your left when facing right). Start at 12 -> straight to Junction 4 -> turn right to Goal at 5.
            new TargetRouteData {
                routeTitle = "Route 3: To the Station",
                fullSummaryText = "'Go straight...' -> 'Take a right.' -> 'The Station is on your left.'",
                startTileIndex = 12,
                goalTileIndex = 5,
                gridLabels = new string[16] {
                    "Clinic", "Station", "", "Hotel", // [1] is Station
                    "Junction", "", "", "",           // [4] is Junction, [5] is Goal (unlabeled)
                    "", "", "", "",
                    "Start (S)", "", "", ""           // [12] is Start
                },
                navigationSteps = new List<RouteNavigationStep>() {
                    new RouteNavigationStep {
                        stepInstruction = "Step 1/2: 'Go straight...' - Move straight up from Start to the Junction!",
                        targetTileIndex = 4,
                        intermediateTileIndices = new List<int>() { 8, 4 }
                    },
                    new RouteNavigationStep {
                        stepInstruction = "Step 2/2: 'Take a right -> The Station is on your left.' - Move right onto the tile where the Station is on your left!",
                        targetTileIndex = 5,
                        intermediateTileIndices = new List<int>() { 5 }
                    }
                }
            }
        };
    }

    private void StartGame() {
        currentRouteIndex = 0;
        score = 0;
        consecutiveWrongClicks = 0;
        UpdateScoreUI();
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        if (narratorSpeech != null) {
            PlayAudioClip(narratorSpeech);
        }

        SetupCurrentRoute();
    }

    private void SetupCurrentRoute() {
        if (currentRouteIndex >= targetRoutes.Count) {
            CompleteGame();
            return;
        }

        currentStepIndex = 0;
        consecutiveWrongClicks = 0;
        isRouteActive = true;

        TargetRouteData route = targetRoutes[currentRouteIndex];
        currentLeoTileIndex = route.startTileIndex;
        UpdateRouteUI();

        // Ensure phraseRailContainer has a clean 4x4 GridLayoutGroup
        if (phraseRailContainer != null) {
            GridLayoutGroup grid = phraseRailContainer.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = phraseRailContainer.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.cellSize = new Vector2(180, 100);
            grid.spacing = new Vector2(15, 15);
            grid.childAlignment = TextAnchor.MiddleCenter;

            foreach (Transform child in phraseRailContainer) {
                Destroy(child.gameObject);
            }
        }

        activeGridButtons.Clear();

        // Generate exactly 16 Grid Tiles directly from the gridLabels array
        for (int i = 0; i < 16; i++) {
            string label = (route.gridLabels != null && i < route.gridLabels.Length) ? route.gridLabels[i] : "";
            Button btn = CreateGridTileButton(i, label, route);
            activeGridButtons.Add(btn);
        }

        StartCoroutine(SnapAvatarToTileRoutine(route.startTileIndex));
    }

    private IEnumerator SnapAvatarToTileRoutine(int tileIndex) {
        yield return new WaitForEndOfFrame(); // Wait for GridLayoutGroup positioning

        if (leoAvatar != null && tileIndex >= 0 && tileIndex < activeGridButtons.Count) {
            leoAvatar.gameObject.SetActive(true);
            leoAvatar.position = activeGridButtons[tileIndex].transform.position;
            leoAvatar.SetAsLastSibling();
        }
    }

    private Button CreateGridTileButton(int index, string labelText, TargetRouteData route) {
        GameObject btnObj = new GameObject($"Tile_{index}");
        if (phraseRailContainer != null) btnObj.transform.SetParent(phraseRailContainer, false);

        Image img = btnObj.AddComponent<Image>();
        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(4, 4);
        txtRect.offsetMax = new Vector2(-4, -4);

        if (!string.IsNullOrEmpty(labelText)) {
            // Landmark / Start Square
            if (index == route.startTileIndex) {
                img.color = new Color(0.2f, 0.65f, 0.3f); // Green Start Square
                tmp.color = Color.white;
            } else {
                img.color = new Color(0.28f, 0.48f, 0.78f); // Blue Landmark Tile
                tmp.color = Color.white;
            }
            tmp.text = labelText;
        } else {
            // Clean empty street / goal tile (NO label text or symbols to avoid font warnings and keep goal unspoilered!)
            img.color = new Color(0.18f, 0.22f, 0.3f); // Dark city tile
            tmp.text = "";
            tmp.color = new Color(0.4f, 0.45f, 0.55f);
        }

        btn.onClick.AddListener(() => OnGridTileClicked(btn, index));
        return btn;
    }

    private void OnGridTileClicked(Button clickedBtn, int clickedTileIndex) {
        if (!isRouteActive) return;

        TargetRouteData route = targetRoutes[currentRouteIndex];
        RouteNavigationStep currentStep = route.navigationSteps[currentStepIndex];

        // Check if player clicked the target tile for this step OR an intermediate step along the path
        if (clickedTileIndex == currentStep.targetTileIndex || currentStep.intermediateTileIndices.Contains(clickedTileIndex)) {
            if (clickedTileIndex == currentLeoTileIndex) return; // already on this tile

            PlaySFX(SFXType.Correct);
            consecutiveWrongClicks = 0;
            currentLeoTileIndex = clickedTileIndex;

            Image btnImg = clickedBtn.GetComponent<Image>();
            if (btnImg != null) btnImg.color = new Color(0.2f, 0.8f, 0.3f); // Highlight green when stepped on

            if (leoAvatar != null) {
                leoAvatar.DOMove(clickedBtn.transform.position, 0.35f).SetEase(Ease.OutQuad);
                leoAvatar.DOPunchScale(Vector3.one * 0.2f, 0.35f);
            }

            // If player landed on the target tile of this step
            if (clickedTileIndex == currentStep.targetTileIndex) {
                score += 15;
                UpdateScoreUI();
                currentStepIndex++;

                if (currentStepIndex >= route.navigationSteps.Count) {
                    // All navigation steps completed! LEO reached the unlabeled goal square!
                    isRouteActive = false;
                    if (routeDirectionsTMP != null) {
                        routeDirectionsTMP.gameObject.SetActive(true);
                        routeDirectionsTMP.text = "GOAL REACHED! LEO completed the path!";
                    }
                    StartCoroutine(AdvanceToNextRouteRoutine());
                } else {
                    UpdateRouteUI();
                }
            } else {
                UpdateRouteUI();
            }
        } else {
            // Wrong tile clicked — give error sfx and increment wrong attempts counter
            PlaySFX(SFXType.Incorrect);
            consecutiveWrongClicks++;
            clickedBtn.transform.DOShakePosition(0.4f, new Vector3(10, 0, 0), 20, 90, false, true);
            UpdateRouteUI();
        }
    }

    private void UpdateRouteUI() {
        TargetRouteData route = targetRoutes[currentRouteIndex];
        if (routeTitleTMP != null) routeTitleTMP.text = $"{route.routeTitle} - {route.fullSummaryText}";
        if (routeProgressTMP != null) routeProgressTMP.text = $"Route: {currentRouteIndex + 1} / {targetRoutes.Count}";

        if (routeDirectionsTMP != null && isRouteActive && currentStepIndex < route.navigationSteps.Count) {
            // Only display route direction hint if player pressed wrong tiles more than 2 times (consecutiveWrongClicks >= 2)
            if (consecutiveWrongClicks >= 2) {
                routeDirectionsTMP.gameObject.SetActive(true);
                RouteNavigationStep currentStep = route.navigationSteps[currentStepIndex];
                routeDirectionsTMP.text = currentStep.stepInstruction;
            } else {
                routeDirectionsTMP.text = "";
                routeDirectionsTMP.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateScoreUI() {
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
    }

    private IEnumerator AdvanceToNextRouteRoutine() {
        yield return new WaitForSeconds(2.0f);
        currentRouteIndex++;
        SetupCurrentRoute();
    }

    private void CompleteGame() {
        isRouteActive = false;
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(false);
        if (quizCompleteGameObject != null) {
            quizCompleteGameObject.SetActive(true);
            Transform titleT = FindTransformRecursive(quizCompleteGameObject.transform, "Title");
            if (titleT != null) {
                TextMeshProUGUI tmp = titleT.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = $"Great Job! Final Score: {score}";
            }
        }
        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    public void RetryGame() {
        StartGame();
    }

    public void CloseGame() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) topic = Masters_Topic.Game;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    public enum SFXType { Correct, Incorrect }

    private void PlaySFX(SFXType type) {
        if (Masters_AudioManager.Instance != null) {
            if (type == SFXType.Correct) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            else if (type == SFXType.Incorrect) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private void PlayAudioClip(AudioClip clip) {
        if (Masters_AudioManager.Instance != null && clip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }
    }
}
