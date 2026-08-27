using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Subclass for Unit 7 (Collocations) Reading Lesson Two: R02 Build the Word-Web.
/// Core gameplay: Rebuild 4 word-webs (GET, CATCH, IDEA, SAVE).
/// Center hub word surrounded by 6 radial slots + bottom tray with mixed partner tiles & cross-hub decoys.
/// Drag & drop: correct tile snaps into slot; wrong tile springs back to tray.
/// Web completes when all slots are filled.
/// Pass threshold: Complete at least 3 out of 4 word-webs.
/// </summary>
[ExecuteAlways]
public class Masters_Collocations_Reading_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class WordWebData {
        public CollocationHub hubId;
        public string hubDisplayName;
        public string[] correctPartners;
    }

    [Header("Unit 7 Collocations Reading R02 Data")]
    [SerializeField] private WordWebData[] webs;
    [SerializeField] private TextMeshProUGUI centerHubTMP;
    [SerializeField] private RectTransform[] slotRects;
    [SerializeField] private RectTransform trayContainer;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private TextMeshProUGUI collocationR02ProgressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTMP;
    [SerializeField] private Button retryButton;

    [Header("Audio & SFX")]
    [SerializeField] private AudioClip ariaIntroAudio;
    [SerializeField] private AudioClip webDoneAudio;

    [Header("Rules")]
    [SerializeField] private int passScore = 3;

    private int currentWebIndex = 0;
    private int completedWebsCount = 0;
    private int slotsFilledInCurrentWeb = 0;
    private bool isTransitioning = false;
    private List<Masters_ReadingR02DraggableTile> currentTiles = new List<Masters_ReadingR02DraggableTile>();
    private bool[] slotOccupied;

    protected override void Awake() {
        base.Awake();
        AutoFindUIReferences();
    }

    private void OnEnable() {
        AutoFindUIReferences();
        if (!Application.isPlaying) {
            UpdateEditModePreview();
        }
    }

    protected void OnValidate() {
        AutoFindUIReferences();
        if (!Application.isPlaying) {
            UpdateEditModePreview();
        }
    }

    private void EnsureTrayTilesExistInHierarchy() {
        if (!Application.isPlaying) return;
        if (trayContainer == null) return;

        string[] defaultNames = new string[] { "Tile_0", "Tile_1", "Tile_2", "Tile_3", "Tile_4", "Tile_5" };
        string[] defaultSampleText = new string[] { "ready", "along with", "a job", "a thief", "save water", "excellent idea" };
        Color[] chipColors = new Color[] {
            new Color(0.2f, 0.55f, 0.9f, 1f),
            new Color(0.2f, 0.72f, 0.45f, 1f),
            new Color(0.9f, 0.32f, 0.32f, 1f),
            new Color(0.95f, 0.65f, 0.2f, 1f),
            new Color(0.6f, 0.4f, 0.8f, 1f),
            new Color(0.2f, 0.75f, 0.75f, 1f)
        };

        Sprite emptyBtnSprite = null;
#if UNITY_EDITOR
        emptyBtnSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/EmptyButton.png");
#endif

        for (int i = 0; i < 6; i++) {
            Transform tTrans = trayContainer.Find(defaultNames[i]) ?? (i < trayContainer.childCount ? trayContainer.GetChild(i) : null);
            if (tTrans == null) {
                GameObject tObj = new GameObject(defaultNames[i], typeof(RectTransform), typeof(Image), typeof(Button), typeof(Masters_ReadingR02DraggableTile));
                tObj.transform.SetParent(trayContainer, false);
                tTrans = tObj.transform;
            } else {
                tTrans.name = defaultNames[i];
            }

            tTrans.gameObject.SetActive(true);

            RectTransform r = tTrans.GetComponent<RectTransform>();
            if (r != null && (r.sizeDelta.x < 10f || r.sizeDelta.y < 10f)) {
                r.sizeDelta = new Vector2(160f, 65f);
            }

            Image img = tTrans.GetComponent<Image>();
            if (img != null) {
                img.color = chipColors[i % chipColors.Length];
                if (img.sprite == null && emptyBtnSprite != null) {
                    img.sprite = emptyBtnSprite;
                    img.type = Image.Type.Sliced;
                }
            }

            Button btn = tTrans.GetComponent<Button>();
            if (btn == null) tTrans.gameObject.AddComponent<Button>();

            Masters_ReadingR02DraggableTile tileComp = tTrans.GetComponent<Masters_ReadingR02DraggableTile>();
            if (tileComp == null) tTrans.gameObject.AddComponent<Masters_ReadingR02DraggableTile>();

            TextMeshProUGUI tmp = tTrans.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp == null) {
                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(tTrans, false);
                tmp = txtObj.GetComponent<TextMeshProUGUI>();
            }

            tmp.gameObject.SetActive(true);
            if (string.IsNullOrEmpty(tmp.text) || tmp.text.Contains("Sample") || tmp.text.Contains("Text")) {
                tmp.text = defaultSampleText[i];
            }
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform tr = tmp.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
        }
    }

    private void UpdateEditModePreview() {
        EnsureDefaultWebsData();
        AutoFindUIReferences();
        EnsureTrayTilesExistInHierarchy();
        UpdateTitleAndUIComponents();

        if (webs != null && webs.Length > 0 && webs[0] != null) {
            WordWebData activeWeb = webs[0];
            if (centerHubTMP != null) {
                centerHubTMP.text = activeWeb.hubDisplayName;
            }
            if (collocationR02ProgressTMP != null) {
                collocationR02ProgressTMP.text = "Web 1/4";
            }
            if (scoreTMP != null) {
                scoreTMP.text = "Completed: 0/4";
            }
            SpawnTilesForCurrentWeb(activeWeb);
        }

        if (slotRects != null) {
            foreach (var slot in slotRects) {
                if (slot != null) slot.gameObject.SetActive(true);
            }
        }
    }

    protected override void Start() {
        topic = Masters_Topic.Reading;
        UpdateTitleAndUIComponents();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        currentWebIndex = 0;
        completedWebsCount = 0;
        StartCoroutine(InitializeReadingR02Routine());
    }

    private void EnsureDefaultWebsData() {
        if (webs == null || webs.Length < 4 || webs[0] == null || webs[0].correctPartners == null || webs[0].correctPartners.Length == 0) {
            webs = new WordWebData[] {
                new WordWebData {
                    hubId = CollocationHub.GET,
                    hubDisplayName = "get",
                    correctPartners = new string[] { "ready", "along with", "a job", "angry", "dressed", "permission" }
                },
                new WordWebData {
                    hubId = CollocationHub.CATCH,
                    hubDisplayName = "catch",
                    correctPartners = new string[] { "a bus", "a cold", "a train", "a thief", "someone's eye", "fire" }
                },
                new WordWebData {
                    hubId = CollocationHub.SAVE,
                    hubDisplayName = "save",
                    correctPartners = new string[] { "water", "time", "a life", "electricity", "money", "a seat" }
                },
                new WordWebData {
                    hubId = CollocationHub.IDEA,
                    hubDisplayName = "idea",
                    correctPartners = new string[] { "excellent", "clever", "bright", "good", "great", "ridiculous" }
                }
            };
        }
    }

    private void AutoFindUIReferences() {
        EnsureDefaultWebsData();

        if (centerHubTMP == null) {
            Transform hubNode = transform.Find("CenterHubNode") ?? transform.Find("ReadingBench/CenterHubNode");
            if (hubNode != null) centerHubTMP = hubNode.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (collocationR02ProgressTMP == null) {
            Transform prog = transform.Find("ProgressIndicator") ?? transform.Find("ProgressText");
            if (prog != null) collocationR02ProgressTMP = prog.GetComponent<TextMeshProUGUI>();
        }

        if (scoreTMP == null) {
            Transform sc = transform.Find("ScoreIndicator") ?? transform.Find("ScoreText");
            if (sc != null) scoreTMP = sc.GetComponent<TextMeshProUGUI>();
        }

        if (trayContainer == null) {
            Transform tray = transform.Find("TileTray") ?? transform.Find("Tray") ?? transform.Find("OptionChips") ?? transform.Find("Chips");
            if (tray != null) trayContainer = tray.GetComponent<RectTransform>();
        }

        if (trayContainer == null) {
            GameObject trayObj = new GameObject("TileTray", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            trayObj.transform.SetParent(transform, false);
            trayContainer = trayObj.GetComponent<RectTransform>();
            trayContainer.sizeDelta = new Vector2(1000f, 130f);
            trayContainer.anchoredPosition = new Vector2(0f, -220f);

            Image tImg = trayObj.GetComponent<Image>();
            tImg.color = new Color(1.0f, 1.0f, 1.0f, 0.88f);

            HorizontalLayoutGroup layout = trayObj.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 15f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        if (slotRects == null || slotRects.Length < 6) {
            Transform slotsParent = transform.Find("RadialSlots") ?? transform.Find("Slots");
            if (slotsParent != null) {
                RectTransform[] foundSlots = slotsParent.GetComponentsInChildren<RectTransform>(true);
                List<RectTransform> slots = new List<RectTransform>();
                foreach (var r in foundSlots) {
                    if (r != slotsParent) slots.Add(r);
                }
                if (slots.Count >= 6) {
                    slotRects = new RectTransform[6];
                    for (int i = 0; i < 6 && i < slots.Count; i++) {
                        slotRects[i] = slots[i];
                    }
                }
            }
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text;
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Occasion") || textVal.Contains("Polished") || textVal.Contains("R02") || textVal.Contains("Build")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "R02 Build the Word-Web";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("LISTENING") || textVal.Contains("READING")) {
                tmp.text = "READING BRANCH (Reading Bench)";
            }
        }
    }

    private IEnumerator InitializeReadingR02Routine() {
        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        LoadWeb(0);
    }

    private void ClearOldTiles() {
        if (currentTiles != null) {
            foreach (var t in currentTiles) {
                if (t != null) {
                    t.ResetToStartPosition();
                }
            }
            currentTiles.Clear();
        }
    }

    private void LoadWeb(int index) {
        if (webs == null || index >= webs.Length) {
            EvaluateFinalScore();
            return;
        }

        currentWebIndex = index;
        slotsFilledInCurrentWeb = 0;
        isTransitioning = false;

        WordWebData activeWeb = webs[currentWebIndex];
        if (activeWeb == null) return;

        if (centerHubTMP != null) {
            centerHubTMP.text = activeWeb.hubDisplayName;
            centerHubTMP.transform.DOKill();
            centerHubTMP.transform.localScale = Vector3.one;
            centerHubTMP.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
        }

        if (collocationR02ProgressTMP != null) {
            collocationR02ProgressTMP.text = $"Web {currentWebIndex + 1}/{webs.Length}";
        }

        if (scoreTMP != null) {
            scoreTMP.text = $"Completed: {completedWebsCount}/4";
        }

        int slotCount = (slotRects != null && slotRects.Length > 0) ? slotRects.Length : 6;
        slotOccupied = new bool[slotCount];

        ClearOldTiles();
        SpawnTilesForCurrentWeb(activeWeb);
    }

    private void SpawnTilesForCurrentWeb(WordWebData activeWeb) {
        if (trayContainer == null || activeWeb.correctPartners == null || activeWeb.correctPartners.Length == 0) return;

        // Pick 3 correct partners for this hub
        List<(string text, CollocationHub hub)> tileList = new List<(string text, CollocationHub hub)>();
        int countCorrect = Mathf.Min(3, activeWeb.correctPartners.Length);
        for (int i = 0; i < countCorrect; i++) {
            string partnerText = activeWeb.correctPartners[i];
            if (activeWeb.hubId == CollocationHub.IDEA) {
                partnerText = partnerText + " idea";
            }
            tileList.Add((partnerText, activeWeb.hubId));
        }

        // Pick 3 decoy partners from other hubs
        for (int w = 0; w < webs.Length; w++) {
            if (webs[w].hubId != activeWeb.hubId && webs[w].correctPartners != null && webs[w].correctPartners.Length > 0) {
                string decoyText = webs[w].correctPartners[0];
                if (webs[w].hubId == CollocationHub.IDEA) decoyText = decoyText + " idea";
                tileList.Add((decoyText, webs[w].hubId));
                if (tileList.Count >= 6) break;
            }
        }

        // Shuffle tile list
        System.Random rng = new System.Random(currentWebIndex + 99);
        int n = tileList.Count;
        while (n > 1) {
            n--;
            int k = rng.Next(n + 1);
            var val = tileList[k];
            tileList[k] = tileList[n];
            tileList[n] = val;
        }

        // Use pre-existing prefab buttons inside trayContainer (NO runtime instantiation of new GameObjects!)
        List<Transform> existingTileTransforms = new List<Transform>();
        foreach (Transform child in trayContainer) {
            if (child != null) {
                existingTileTransforms.Add(child);
            }
        }

        for (int i = 0; i < existingTileTransforms.Count; i++) {
            Transform tTrans = existingTileTransforms[i];
            if (tTrans == null) continue;

            bool active = i < tileList.Count;
            tTrans.gameObject.SetActive(active);

            if (active) {
                TextMeshProUGUI tmp = tTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null) {
                    tmp.gameObject.SetActive(true);
                    tmp.text = tileList[i].text;
                }

                Masters_ReadingR02DraggableTile tileComp = tTrans.GetComponent<Masters_ReadingR02DraggableTile>();
                if (tileComp == null) {
                    tileComp = tTrans.gameObject.AddComponent<Masters_ReadingR02DraggableTile>();
                }

                tileComp.Initialize(tileList[i].text, tileList[i].hub, this);
                tileComp.SetStartPosition(tTrans.position);
                currentTiles.Add(tileComp);
            }
        }
    }

    public void OnTileDropped(Masters_ReadingR02DraggableTile tile) {
        if (isTransitioning || tile == null || webs == null || currentWebIndex >= webs.Length) return;

        WordWebData currentWeb = webs[currentWebIndex];
        if (currentWeb == null) return;

        // Check distance to slot targets
        int targetSlotIndex = -1;
        float minDistance = 140f; // Drop threshold radius

        if (slotRects != null) {
            for (int i = 0; i < slotRects.Length; i++) {
                if (slotRects[i] == null || (slotOccupied != null && slotOccupied[i])) continue;
                float dist = Vector3.Distance(tile.transform.position, slotRects[i].position);
                if (dist < minDistance) {
                    minDistance = dist;
                    targetSlotIndex = i;
                }
            }
        }

        bool isCorrectHub = (tile.correctHub == currentWeb.hubId);

        if (targetSlotIndex >= 0 && isCorrectHub) {
            // Correct Snap!
            if (slotOccupied != null) slotOccupied[targetSlotIndex] = true;
            tile.LockInSlot(slotRects[targetSlotIndex].position);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            }

            slotsFilledInCurrentWeb++;

            int targetGoal = 3; // 3 correct partners required to complete web
            if (slotsFilledInCurrentWeb >= targetGoal) {
                isTransitioning = true;
                completedWebsCount++;
                if (scoreTMP != null) {
                    scoreTMP.text = $"Completed: {completedWebsCount}/4";
                }
                StartCoroutine(OnWebCompletedRoutine());
            }
        } else {
            // Incorrect Repel / Springback!
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            tile.ReturnToStart();
        }
    }

    private IEnumerator OnWebCompletedRoutine() {
        yield return new WaitForSeconds(0.4f);

        if (webDoneAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(webDoneAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(1.5f);
        }

        LoadWeb(currentWebIndex + 1);
    }

    private void EvaluateFinalScore() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        bool passed = (completedWebsCount >= passScore);

        if (resultTMP != null) {
            if (passed) {
                resultTMP.text = $"EXCELLENT! Completed Webs: {completedWebsCount}/4\nReading Branch Mastered!";
            } else {
                resultTMP.text = $"KEEP TRYING! Completed Webs: {completedWebsCount}/4\nYou need at least {passScore}/4 to pass.";
            }
        }

        if (passed) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
                NextButtonAnimation();
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (retryButton != null) {
                retryButton.gameObject.SetActive(true);
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(RestartLesson);
            }
        }
    }

    public void RestartLesson() {
        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }
        currentWebIndex = 0;
        completedWebsCount = 0;
        LoadWeb(0);
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Reading;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

}