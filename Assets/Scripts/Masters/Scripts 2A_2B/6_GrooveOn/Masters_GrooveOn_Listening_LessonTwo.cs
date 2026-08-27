using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
//using EngSnap.Masters.Unit3;

public enum Masters_GrooveOn_Festival8 {
    DIWALI = 0,
    CHRISTMAS = 1,
    EID = 2,
    NEW_YEAR = 3,
    INDEPENDENCE_DAY = 4,
    GURU_NANAK_JAYANTI = 5,
    EASTER = 6,
    SANKRANTI = 7,
    GANDHI_JAYANTI = 8
}

/// <summary>
/// Subclass for Unit 6 (Groove On) Listening Lesson Two: L02 Name the Festival — Hear the Greeting.
/// Audio-cued festival identification game built according to exact GDD spec (8 rounds).
/// </summary>
[ExecuteAlways]
public class Masters_GrooveOn_Listening_LessonTwo : Masters_PolishedCommunication_Listening_LessonTwo {

    [System.Serializable]
    public class GrooveOnFestival8TileData {
        public string expressionText;
        public AudioClip expressionAudio;
        public AudioClip slowAudio;
        public Masters_GrooveOn_Festival8 correctFestival;
    }

    [Header("Unit 6 Listening L2 Data (GDD 8 Festival Options)")]
    [SerializeField] private GrooveOnFestival8TileData[] festivalTiles;

    [Header("Unit 6 8-Festival Card Labels (GDD L02 Spec)")]
    [SerializeField] private string[] festivalLabels = new string[] {
        "Diwali",
        "Christmas",
        "Eid",
        "New Year",
        "Independence Day",
        "Gandhi Jayanti",
        "Guru Nanak Jayanti",
        "Easter"
    };

    private List<Color> savedInspectorColors = new List<Color>();
    private AudioSource localAudioSource;
    private bool isSlowed = false;
    private int currentRoundRetries = 0;

    private void OnEnable() {
        if (!Application.isPlaying) {
            AutoFindUIReferences();
            EnsureFestivalTilesInitialized();
            UpdateTitleText();
            ConfigureSortBins();
            SetupOtherControls();
        }
    }

    protected override void Awake() {
        // Do not call base.Awake() to prevent base class from auto-configuring 2-bin FORMAL/INFORMAL layout
        topic = Masters_Topic.Listening;

        localAudioSource = GetComponent<AudioSource>();
        if (localAudioSource == null) {
            localAudioSource = gameObject.AddComponent<AudioSource>();
            localAudioSource.playOnAwake = false;
        }

        AutoFindUIReferences();
        SaveOriginalButtonColors();
        FixCanvasAndEventSystem();
        RemoveChildCamera();
        CleanOrphanedSubMeshes();
        EnsureFestivalTilesInitialized();
        UpdateTitleText();
        ConfigureSortBins();
        SetupOtherControls();
    }

    protected override void Start() {
        // Do not call base.Start() to prevent base class InitializeLessonRoutine() from locking clicks (canClick = false)
        StopAllCoroutines();
        topic = Masters_Topic.Listening;

        AutoFindUIReferences();
        SaveOriginalButtonColors();
        FixCanvasAndEventSystem();
        RemoveChildCamera();
        CleanOrphanedSubMeshes();
        EnsureFestivalTilesInitialized();
        UpdateTitleText();
        ConfigureSortBins();
        SetupOtherControls();

        currentTileIndex = 0;
        correctSorts = 0;
        currentRoundRetries = 0;
        canClick = true;

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        UpdateProgressDisplay();
        EnsureScoreBoardHUDCreated();
        StartCoroutine(StartFirstRoundAudioRoutine());
    }

    private void AutoFindUIReferences() {
        if (nextButton == null) {
            Transform t = transform.Find("NextButton") ?? transform.Find("Next");
            if (t != null) nextButton = t.GetComponent<Button>();
        }

        if (progressTMP == null) {
            Transform t = transform.Find("ExpressionCountTMP") ?? transform.Find("ProgressTMP") ?? transform.Find("ProgressText");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
    }

    private void SaveOriginalButtonColors() {
        if (savedInspectorColors != null && savedInspectorColors.Count > 0) return;

        if (savedInspectorColors == null) savedInspectorColors = new List<Color>();
        savedInspectorColors.Clear();

        Button[] btns = GetComponentsInChildren<Button>(true);
        foreach (var b in btns) {
            if (b != null && b != nextButton && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("speaker") && !b.name.ToLower().Contains("back")) {
                Image img = b.GetComponent<Image>();
                if (img != null) {
                    savedInspectorColors.Add(img.color);
                } else {
                    savedInspectorColors.Add(Color.white);
                }
            }
        }
    }

    private IEnumerator StartFirstRoundAudioRoutine() {
        if (narratorSpeech == null) {
#if UNITY_EDITOR
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Listening/Listen carefully to each greeting and select the matching festival card.mp3");
#endif
        }

        if (narratorSpeech != null) {
            Debug.Log($"[L02 Narrator Audio] Playing intro: {narratorSpeech.name}");
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            }
            if (localAudioSource != null) {
                localAudioSource.Stop();
                localAudioSource.clip = narratorSpeech;
                localAudioSource.volume = 1.0f;
                localAudioSource.spatialBlend = 0f;
                localAudioSource.Play();
            }
            yield return new WaitForSeconds(narratorSpeech.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.2f);
        }

        PlayCurrentRoundAudio();
    }

    private void FixCanvasAndEventSystem() {
        if (UnityEngine.EventSystems.EventSystem.current == null) {
            GameObject es = GameObject.Find("EventSystem");
            if (es == null) {
                es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        Canvas c = GetComponentInParent<Canvas>();
        if (c != null && c.GetComponent<GraphicRaycaster>() == null) {
            c.gameObject.AddComponent<GraphicRaycaster>();
        }

        Transform cardTrans = transform.Find("SortPhraseCard");
        if (cardTrans != null) {
            Image cardImg = cardTrans.GetComponent<Image>();
            if (cardImg != null && !cardTrans.gameObject.activeInHierarchy) {
                cardImg.raycastTarget = false;
            }
        }
    }

    private void EnsureFestivalTilesInitialized() {
        string[] texts = new string[] {
            "Wish you a Happy Diwali!",
            "Merry Christmas to you!",
            "Eid Mubarak!",
            "Happy New Year!",
            "Happy Independence Day!",
            "Happy Gurupurab! Guru Nanak Jayanti!",
            "Happy Easter to you!",
            "Wish you a Happy Sankranti!"
        };

        Masters_GrooveOn_Festival8[] enums = new Masters_GrooveOn_Festival8[] {
            Masters_GrooveOn_Festival8.DIWALI,
            Masters_GrooveOn_Festival8.CHRISTMAS,
            Masters_GrooveOn_Festival8.EID,
            Masters_GrooveOn_Festival8.NEW_YEAR,
            Masters_GrooveOn_Festival8.INDEPENDENCE_DAY,
            Masters_GrooveOn_Festival8.GURU_NANAK_JAYANTI,
            Masters_GrooveOn_Festival8.EASTER,
            Masters_GrooveOn_Festival8.SANKRANTI
        };

        if (festivalTiles == null || festivalTiles.Length < 8) {
            festivalTiles = new GrooveOnFestival8TileData[8];
            for (int i = 0; i < 8; i++) {
                festivalTiles[i] = new GrooveOnFestival8TileData();
            }
        }

        for (int i = 0; i < 8 && i < festivalTiles.Length; i++) {
            if (festivalTiles[i] == null) festivalTiles[i] = new GrooveOnFestival8TileData();
            festivalTiles[i].expressionText = texts[i];
            festivalTiles[i].correctFestival = enums[i];
            GetAudioClipForRound(i);
        }
    }

    private AudioClip GetAudioClipForRound(int index) {
        if (festivalTiles != null && index >= 0 && index < festivalTiles.Length && festivalTiles[index] != null && festivalTiles[index].expressionAudio != null) {
            return festivalTiles[index].expressionAudio;
        }

#if UNITY_EDITOR
        string audioDir = "Assets/Audio/2A/6_GrooveOn/Listening/";
        string[][] candidateNames = new string[][] {
            new string[] { "Wish you a Happy Diwali.mp3", "Joy and prosperity this Diwali.mp3" },
            new string[] { "Merry Christmas to you.mp3", "Merry Christmas.mp3" },
            new string[] { "Eid Mubarak.mp3" },
            new string[] { "Happy New Year.mp3" },
            new string[] { "Happy Independence Day.mp3" },
            new string[] { "Happy Gurpurab Guru Nanak Jayanti.mp3", "Happy Gurupurab Guru Nanak Jayanti.mp3" },
            new string[] { "Happy Easter to you.mp3", "Happy Easter to you all.mp3" },
            new string[] { "Wish you a Happy Sankranti.mp3" }
        };

        if (index >= 0 && index < candidateNames.Length) {
            foreach (string fileName in candidateNames[index]) {
                AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + fileName);
                if (clip != null) {
                    if (festivalTiles != null && index < festivalTiles.Length && festivalTiles[index] != null) {
                        festivalTiles[index].expressionAudio = clip;
                    }
                    return clip;
                }
            }
        }
#endif
        return null;
    }

    private void RemoveChildCamera() {
        Transform childCam = transform.Find("Camera");
        if (childCam != null) {
            childCam.gameObject.SetActive(false);
            Destroy(childCam.gameObject);
        }
    }

    private void CleanOrphanedSubMeshes() {
        TMP_SubMeshUI[] subMeshes = GetComponentsInChildren<TMP_SubMeshUI>(true);
        foreach (var subMesh in subMeshes) {
            if (subMesh != null) {
                if (subMesh.sharedMaterial == null || subMesh.canvasRenderer == null || subMesh.fontAsset == null) {
                    try {
                        GameObject go = subMesh.gameObject;
                        if (Application.isPlaying) {
                            Destroy(subMesh);
                            if (go != null && go != gameObject && go.transform.childCount == 0) Destroy(go);
                        } else {
                            DestroyImmediate(subMesh);
                            if (go != null && go != gameObject && go.transform.childCount == 0) DestroyImmediate(go);
                        }
                    } catch { }
                }
            }
        }
    }

    private void UpdateTitleText() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;

            if (tmp.GetComponentInParent<Button>() != null) continue;
            if (tmp.GetComponentInParent<Masters_UniversalSortBin>() != null) continue;
            if (tmp.name.Contains("BackButton")) continue;

            string textVal = tmp.text ?? "";
            string lowerName = tmp.name.ToLower();

            if (lowerName.Equals("lessontitletext") || lowerName.Equals("lessontitle") || lowerName.Equals("titletext")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "L02 Name the Festival — Hear the Greeting";
            }
            else if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("LISTENING") || textVal.Contains("SAY IT") || textVal.Contains("PHRASAL") || textVal.Contains("ACTION")) {
                tmp.text = "L02 Name the Festival — Hear the Greeting";
            }
        }
    }

    private void SetupOtherControls() {
        // 1. Back Button
        GameObject backBtnObj = GameObject.Find("BackButton");
        if (backBtnObj == null) {
            Transform t = FindChildRecursive(transform, "BackButton");
            if (t != null) backBtnObj = t.gameObject;
        }

        if (backBtnObj != null) {
            backBtnObj.SetActive(true);

            Image[] backImgs = backBtnObj.GetComponentsInChildren<Image>(true);
            foreach (var img in backImgs) {
                if (img != null) {
                    img.enabled = true;
                    img.raycastTarget = true;
                }
            }

            Button b = backBtnObj.GetComponent<Button>();
            if (b == null) b = backBtnObj.AddComponent<Button>();
            if (b != null) {
                b.interactable = true;
                b.transition = Selectable.Transition.None;

                Navigation nav = new Navigation { mode = Navigation.Mode.None };
                b.navigation = nav;

                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => {
                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                        Masters_AudioManager.Instance.StopVoiceOver();
                    }
                    if (Masters_LevelManager.Instance != null) {
                        Masters_LevelManager.Instance.OnBackButtonClicked();
                    }
                });
            }
        }

        // 2. Slow Button
        Transform slowTrans = FindChildRecursive(transform, "Slow") ?? FindChildRecursive(transform, "SlowButton") ?? FindChildRecursive(transform, "SlowToggle") ?? FindChildRecursive(transform, "Toggle_Slow");
        if (slowTrans != null) {
            Button slowBtn = slowTrans.GetComponent<Button>();
            if (slowBtn == null) slowBtn = slowTrans.gameObject.AddComponent<Button>();
            if (slowBtn != null) {
                slowBtn.interactable = true;
                slowBtn.transition = Selectable.Transition.None;

                Navigation nav = new Navigation { mode = Navigation.Mode.None };
                slowBtn.navigation = nav;

                Image slowImg = slowTrans.GetComponent<Image>();
                if (slowImg != null) slowImg.raycastTarget = true;

                slowBtn.onClick.RemoveAllListeners();
                slowBtn.onClick.AddListener(() => {
                    isSlowed = !isSlowed;
                    PlayCurrentRoundAudio();
                });
            }
        }

        // 3. Repeat This Button
        Transform repeatTrans = FindChildRecursive(transform, "Repeat") ?? FindChildRecursive(transform, "RepeatThis") ?? FindChildRecursive(transform, "Repeat this") ?? FindChildRecursive(transform, "RepeatButton");
        if (repeatTrans != null) {
            Button repeatBtn = repeatTrans.GetComponent<Button>();
            if (repeatBtn == null) repeatBtn = repeatTrans.gameObject.AddComponent<Button>();
            if (repeatBtn != null) {
                repeatBtn.interactable = true;
                repeatBtn.transition = Selectable.Transition.None;

                Navigation nav = new Navigation { mode = Navigation.Mode.None };
                repeatBtn.navigation = nav;

                Image repeatImg = repeatTrans.GetComponent<Image>();
                if (repeatImg != null) repeatImg.raycastTarget = true;

                repeatBtn.onClick.RemoveAllListeners();
                repeatBtn.onClick.AddListener(() => {
                    PlayCurrentRoundAudio();
                });
            }
        }

        // 4. Next Button
        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => {
                OnNextButtonClicked();
            });
        }
    }

    protected virtual new void ConfigureSortBins() {
        List<GameObject> optionObjs = new List<GameObject>();

        // 1. Search for options container: PhrasalVerbOptionsContainer, OptionsContainer, or Options
        Transform containerTrans = transform.Find("PhrasalVerbOptionsContainer") 
                                ?? transform.Find("OptionsContainer") 
                                ?? transform.Find("Options");

        if (containerTrans != null && containerTrans.childCount > 0) {
            foreach (Transform child in containerTrans) {
                if (child != null) {
                    Button b = child.GetComponent<Button>();
                    if (b != null && !optionObjs.Contains(child.gameObject)) {
                        optionObjs.Add(child.gameObject);
                    }
                }
            }
        }

        // 2. Fallback: collect all non-control option buttons under transform
        if (optionObjs.Count < 8) {
            optionObjs.Clear();
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (var b in allBtns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (b.gameObject == nextButton?.gameObject) continue;
                if (bName.Contains("next") || bName.Contains("back") || bName.Contains("slow") || bName.Contains("repeat") || bName.Contains("speaker") || bName.Contains("card")) continue;

                if (!optionObjs.Contains(b.gameObject)) {
                    optionObjs.Add(b.gameObject);
                }
            }
        }

        List<Masters_UniversalSortBin> binList = new List<Masters_UniversalSortBin>();

        for (int i = 0; i < optionObjs.Count && i < 8; i++) {
            GameObject binObj = optionObjs[i];
            if (binObj == null) continue;

            binObj.SetActive(true);

            Masters_UniversalSortBin binComp = binObj.GetComponent<Masters_UniversalSortBin>();
            if (binComp == null) {
                binComp = binObj.AddComponent<Masters_UniversalSortBin>();
            }

            binComp.SetSortId(i);
            binList.Add(binComp);

            // Enable raycastTarget on images so clicks register properly without overriding scene visual colors
            Image[] imgs = binObj.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs) {
                if (img != null) {
                    img.enabled = true;
                    img.raycastTarget = true;
                }
            }

            Button binBtn = binObj.GetComponent<Button>();
            if (binBtn == null) binBtn = binObj.GetComponentInChildren<Button>(true);
            if (binBtn == null) binBtn = binObj.AddComponent<Button>();

            if (binBtn != null) {
                binBtn.interactable = true;
                binBtn.transition = Selectable.Transition.None;

                Navigation nav = new Navigation { mode = Navigation.Mode.None };
                binBtn.navigation = nav;

                Masters_UniversalSortBin currentBin = binComp;
                binBtn.onClick.RemoveAllListeners();
                binBtn.onClick.AddListener(() => OnSortBinClicked(currentBin));
            }
        }

        sortBinArray = binList.ToArray();
    }

    protected virtual new void OnSortBinClicked(Masters_UniversalSortBin sortBin) {
        if (!canClick || sortBin == null) return;

        string btnText = "";
        TMP_Text tmp = sortBin.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) btnText = tmp.text;
        else {
            Text legacy = sortBin.GetComponentInChildren<Text>(true);
            if (legacy != null) btnText = legacy.text;
        }

        bool isCorrect = CheckAnswer(btnText, sortBin.GetSortId());
        Debug.Log($"[L02] Round {currentTileIndex} | Spoken Greeting: '{festivalTiles[currentTileIndex]?.expressionText}' | Clicked: '{btnText}' (sortId {sortBin.GetSortId()}) | Correct: {isCorrect}");

        if (isCorrect) {
            canClick = false;
            correctSorts++;
            currentRoundRetries = 0;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            Image binImg = sortBin.GetComponent<Image>() ?? sortBin.GetComponentInChildren<Image>(true);
            if (binImg != null) {
                binImg.DOColor(new Color(0.2f, 0.8f, 0.35f, 1f), 0.2f); // Green for correct
            }

            sortBin.transform.DOPunchScale(Vector3.one * 0.18f, 0.35f);

            currentTileIndex++;
            UpdateProgressDisplay();

            int totalRounds = (festivalTiles != null && festivalTiles.Length > 0) ? festivalTiles.Length : 8;
            if (currentTileIndex >= totalRounds) {
                EvaluateL02Completion();
            } else {
                StartCoroutine(NextRoundRoutine());
            }
        } else {
            canClick = false;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            Image binImg = sortBin.GetComponent<Image>() ?? sortBin.GetComponentInChildren<Image>(true);
            if (binImg != null) {
                binImg.DOColor(new Color(0.85f, 0.25f, 0.2f, 1f), 0.2f); // Red for incorrect
            }

            sortBin.transform.DOKill(true);
            sortBin.transform.DOShakePosition(0.4f, new Vector3(15f, 0, 0));

         DOVirtual.DelayedCall(1.0f, () => {
    ResetSortBinColors();
    canClick = true;
});
        }
    }

    private bool CheckAnswer(string btnText, int sortId) {
        if (festivalTiles == null || currentTileIndex < 0 || currentTileIndex >= festivalTiles.Length) {
            return false;
        }

        string targetGreeting = festivalTiles[currentTileIndex]?.expressionText?.ToLower() ?? "";
        string btnLower = (btnText ?? "").ToLower().Trim();

        if (targetGreeting.Contains("diwali") && btnLower.Contains("diwali")) return true;
        if (targetGreeting.Contains("christmas") && btnLower.Contains("christmas")) return true;
        if (targetGreeting.Contains("easter") && btnLower.Contains("easter")) return true;
        if (targetGreeting.Contains("eid") && btnLower.Contains("eid")) return true;
        if (targetGreeting.Contains("new year") && (btnLower.Contains("new year") || btnLower.Contains("newyear"))) return true;
        if (targetGreeting.Contains("independence") && (btnLower.Contains("independence") || btnLower.Contains("independance"))) return true;
        if (targetGreeting.Contains("gandhi") && btnLower.Contains("gandhi")) return true;
        if (targetGreeting.Contains("gurpurab") || targetGreeting.Contains("nanak") || targetGreeting.Contains("jayanti") || targetGreeting.Contains("jayanthi")) {
            if (btnLower.Contains("nanak") || btnLower.Contains("gurpurab") || btnLower.Contains("jayanti") || btnLower.Contains("jayanthi") || btnLower.Contains("guru")) return true;
        }
        if (targetGreeting.Contains("sankranti") || targetGreeting.Contains("sankranthi")) {
            if (btnLower.Contains("sankranti") || btnLower.Contains("sankranthi")) return true;
        }

        if (sortId == (int)festivalTiles[currentTileIndex].correctFestival) return true;
        if (sortId == currentTileIndex) return true;

        return false;
    }

    private IEnumerator NextRoundRoutine() {
        canClick = false;
        yield return new WaitForSeconds(0.7f);
        try {
            PlayCurrentRoundAudio();
        } catch { }
        finally {
            canClick = true;
        }
    }

    private void PlayCurrentRoundAudio() {
        if (currentTileIndex < 0) return;

        AudioClip clip = GetAudioClipForRound(currentTileIndex);

        if (clip != null) {
            Debug.Log($"[L02 Spoken Audio] Playing round {currentTileIndex + 1}/8: {clip.name}");
            
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }

            if (localAudioSource != null) {
                localAudioSource.Stop();
                localAudioSource.clip = clip;
                localAudioSource.volume = 1.0f;
                localAudioSource.spatialBlend = 0f;
                localAudioSource.Play();
            }
        } else {
            Debug.LogWarning($"[L02] Could not find spoken audio clip for round {currentTileIndex + 1}!");
        }
    }

    [Header("Score Board UI (Editable in Inspector)")]
    [SerializeField] public TextMeshProUGUI scoreBoardTMP;

    private void EnsureScoreBoardHUDCreated() {
        if (scoreBoardTMP != null) {
            scoreBoardTMP.gameObject.SetActive(true);
            UpdateProgressDisplay();
            return;
        }

        Transform countT = transform.Find("ExpressionCountTMP") ?? transform.Find("ProgressTMP") ?? transform.Find("PuzzleCountTMP");
        if (countT != null) {
            scoreBoardTMP = countT.GetComponent<TextMeshProUGUI>();
            if (scoreBoardTMP != null) {
                scoreBoardTMP.gameObject.SetActive(true);
                UpdateProgressDisplay();
                return;
            }
        }
        Transform boardTrans = transform.Find("ScoreBoardHUD");
        GameObject boardGo;
        if (boardTrans == null) {
            boardGo = new GameObject("ScoreBoardHUD");
            boardGo.transform.SetParent(transform, false);
        } else {
            boardGo = boardTrans.gameObject;
        }

        boardGo.SetActive(true);
        boardGo.transform.SetAsLastSibling(); // Ensure Score Board renders on top of all canvas layers

        RectTransform rect = boardGo.GetComponent<RectTransform>();
        if (rect == null) rect = boardGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-40f, -40f);
        rect.sizeDelta = new Vector2(340f, 60f);

        Image bgImg = boardGo.GetComponent<Image>();
        if (bgImg == null) bgImg = boardGo.AddComponent<Image>();
        bgImg.enabled = true;
        bgImg.color = new Color(0.08f, 0.14f, 0.28f, 0.92f); // Dark blue HUD pill background
        bgImg.raycastTarget = false;

        Transform textTrans = boardGo.transform.Find("ScoreText");
        GameObject scoreGo;
        if (textTrans == null) {
            scoreGo = new GameObject("ScoreText");
            scoreGo.transform.SetParent(boardGo.transform, false);
        } else {
            scoreGo = textTrans.gameObject;
        }

        scoreGo.SetActive(true);
        RectTransform scoreRect = scoreGo.GetComponent<RectTransform>();
        if (scoreRect == null) scoreRect = scoreGo.AddComponent<RectTransform>();
        scoreRect.anchorMin = Vector2.zero;
        scoreRect.anchorMax = Vector2.one;
        scoreRect.offsetMin = new Vector2(10f, 4f);
        scoreRect.offsetMax = new Vector2(-10f, -4f);

        TextMeshProUGUI tmp = scoreGo.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = scoreGo.AddComponent<TextMeshProUGUI>();
        tmp.enabled = true;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 14;
        tmp.fontSizeMax = 24;
        tmp.alignment = TextAlignmentOptions.Center;

        scoreBoardTMP = tmp;
        UpdateProgressDisplay();
    }

    private void UpdateProgressDisplay() {
        int qNum = Mathf.Min(currentTileIndex + 1, 8);
        if (progressTMP != null) {
            progressTMP.text = $"{qNum}/8";
        }
        if (scoreBoardTMP != null) {
            scoreBoardTMP.text = $"SCORE: {correctSorts * 100}   |   PROGRESS: {qNum}/8";
        }
    }

    private void EvaluateL02Completion() {
        canClick = false;
        if (progressTMP != null) {
            progressTMP.text = "8/8";
        }

        Debug.Log($"[L02 Name the Festival] Activity Finished! Score: {correctSorts}/8");

        if (correctSorts >= 6) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            ShowAllCompletedBanner();

            if (nextButton == null) {
                Transform nbTrans = FindChildRecursive(transform, "NextButton") ?? FindChildRecursive(transform, "Next Button") ?? FindChildRecursive(transform, "Next");
                if (nbTrans != null) nextButton = nbTrans.GetComponent<Button>();
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
                NextButtonAnimation();
            } else {
                Debug.LogWarning("[L02] NextButton not found in hierarchy!");
            }
        } else {
            Debug.LogWarning($"[L02] Score {correctSorts}/8 is below 6/8 requirement. Restarting L02 activity.");
            RestartActivity();
        }
    }

    public void RestartActivity() {
        currentTileIndex = 0;
        correctSorts = 0;
        currentRoundRetries = 0;
        canClick = true;

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        UpdateProgressDisplay();
        StartCoroutine(StartFirstRoundAudioRoutine());
    }

    private void ShowAllCompletedBanner() {
        TMP_FontAsset fontAsset = null;
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) {
            if (t != null && t.font != null) {
                fontAsset = t.font;
                break;
            }
        }

        Transform bannerTrans = transform.Find("AllCompletedBanner");
        GameObject bannerObj;
        if (bannerTrans == null) {
            bannerObj = new GameObject("AllCompletedBanner");
            bannerObj.transform.SetParent(transform, false);
        } else {
            bannerObj = bannerTrans.gameObject;
        }

        bannerObj.SetActive(true);
        bannerObj.transform.SetAsLastSibling();

        RectTransform rect = bannerObj.GetComponent<RectTransform>();
        if (rect == null) rect = bannerObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -145f); // Safely positioned below title header
        rect.sizeDelta = new Vector2(800f, 50f);

        TextMeshProUGUI bannerText = bannerObj.GetComponent<TextMeshProUGUI>();
        if (bannerText == null) bannerText = bannerObj.AddComponent<TextMeshProUGUI>();
        bannerText.enabled = true;
        if (fontAsset != null) bannerText.font = fontAsset;
        bannerText.text = "ALL COMPLETED!";
        bannerText.fontStyle = FontStyles.Bold;
        bannerText.fontSize = 38;
        bannerText.color = new Color(1f, 0.92f, 0.23f, 1f); // Vibrant Gold
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.enableWordWrapping = false;

        bannerObj.transform.localScale = Vector3.zero;
        bannerObj.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
    }

    private void ResetSortBinColors() {
        if (sortBinArray == null) return;
        Color[] festColors = new Color[] {
            new Color(0.85f, 0.47f, 0.02f, 1f), // 0: Diwali (Amber Gold)
            new Color(0.86f, 0.15f, 0.15f, 1f), // 1: Christmas (Crimson Red)
            new Color(0.02f, 0.59f, 0.41f, 1f), // 2: Eid (Emerald Green)
            new Color(0.15f, 0.39f, 0.92f, 1f), // 3: New Year (Royal Blue)
            new Color(0.31f, 0.27f, 0.90f, 1f), // 4: Independence Day (Deep Navy)
            new Color(0.79f, 0.54f, 0.02f, 1f), // 5: Guru Nanak Jayanti (Warm Ochre)
            new Color(0.49f, 0.23f, 0.93f, 1f), // 6: Easter (Vibrant Purple)
            new Color(0.05f, 0.58f, 0.53f, 1f)  // 7: Sankranti (Teal / Cyan)
        };

        for (int i = 0; i < sortBinArray.Length; i++) {
            if (sortBinArray[i] == null) continue;
            sortBinArray[i].transform.DOKill(true);
            sortBinArray[i].transform.localScale = Vector3.one;

            Image img = sortBinArray[i].GetComponent<Image>() ?? sortBinArray[i].GetComponentInChildren<Image>(true);
            if (img != null) {
                img.color = festColors[i % festColors.Length];
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName) {
        if (parent == null) return null;
        foreach (Transform child in parent) {
            if (child == null) continue;
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform result = FindChildRecursive(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}