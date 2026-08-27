using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

/// <summary>
/// Controller for Unit 7 (Collocations) Role Play Branch - Stage RP01: On Stage — Find Your Other Half.
/// Classroom stage role-play activity where LEO holds one half of a collocation and 4 classmate NPC cards step forward.
/// Features:
/// - 6 verbatim rounds demonstrating both Hub-first and Partner-first collocation matching directions
/// - 4 physical Classmate NPC cards on stage (ClassmateCard_1 to ClassmateCard_4)
/// - 1 retry allowed per round on wrong attempt
/// - Magnet Snap on correct match, full collocation flash & readback
/// - 5 of 6 correct rounds pass threshold setting Roleplay-RP01 completion sub-flag
/// </summary>
public class Masters_Collocations_Roleplay_LessonOne : Masters_Lesson {

    public enum MatchingDirection {
        HubFirst,       // LEO holds first half (e.g. "catch" + "train")
        PartnerFirst    // LEO holds second half (e.g. "innovative" + "idea")
    }

    [System.Serializable]
    public class RP01RoundData {
        public int roundId;
        public string leoHalfText;
        public string[] classmateWords; // Array of 4 classmate words
        public int correctIndex;        // 0 to 3
        public string fullCollocationText;
        public MatchingDirection direction;
        public AudioClip leoWordAudio;  // Single prompt word audio
        public AudioClip pairAudio;     // Full collocation pair audio
    }

    [Header("RP01 Round Data (6 Rounds)")]
    [SerializeField] private RP01RoundData[] rounds;

    [Header("Stage & Characters UI")]
    [SerializeField] private GameObject stageContainer;
    [SerializeField] private RectTransform leoCardRect;
    [SerializeField] private TextMeshProUGUI leoCardTMP;
    [SerializeField] private Button leoCardButton;
    [SerializeField] private RectTransform[] classmateCardRects; // 4 classmate card containers
    [SerializeField] private TextMeshProUGUI[] classmateCardTMPs;  // 4 classmate text components
    [SerializeField] private Button[] classmateButtons;          // 4 classmate click buttons

    [Header("Game State UI")]
    [SerializeField] private TextMeshProUGUI roundProgressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI feedbackBannerTMP;
    [SerializeField] private TextMeshProUGUI flashBannerTMP;

    [Header("Title & Instruction UI")]
    [SerializeField] private TextMeshProUGUI rp01TitleTMP;
    [SerializeField] private TextMeshProUGUI rp01HeaderTMP;
    [SerializeField] private TextMeshProUGUI rp01InstructionTMP;

    [Header("Result Popup")]
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnHubButton;

    [Header("Audio References")]
    [SerializeField] private AudioClip ariaTeacherAudio;
    [SerializeField] private AudioClip sfxMagnetSnap;
    [SerializeField] private AudioClip sfxCurtain;

    // Runtime state variables
    private int currentRoundIndex = 0;
    private int correctRoundsCount = 0;
    private int currentRetryCount = 0;
    private bool isRoundInputActive = false;
    private const int PASS_THRESHOLD = 5;
    private const int TOTAL_ROUNDS = 6;

    protected virtual void OnEnable() {
        // Prevent unwanted STT subscriptions
    }

    protected virtual void OnDisable() {
        // Cleanup if needed
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeRoundsData();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Roleplay;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeRoundsData();
        UpdateTitleAndUIComponents();
        SetupButtonListeners();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (resultPopup != null) resultPopup.SetActive(false);
        if (flashBannerTMP != null) flashBannerTMP.gameObject.SetActive(false);

        StartCoroutine(InitializeRoleplayLessonRoutine());
    }

    private IEnumerator InitializeRoleplayLessonRoutine() {
        currentRoundIndex = 0;
        correctRoundsCount = 0;
        currentRetryCount = 0;

        UpdateScoreUI();
        if (resultPopup != null) resultPopup.SetActive(false);

        if (ariaTeacherAudio == null) {
            #if UNITY_EDITOR
            ariaTeacherAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Roleplay/RP01/Find the friend who completes your pair.mp3");
            #endif
        }

        if (ariaTeacherAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(ariaTeacherAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(0.3f);
        }

        LoadRound(0);
    }

    public void StartNewGame() {
        StartCoroutine(InitializeRoleplayLessonRoutine());
    }

    private void DeactivateObsoleteBaseUI() {
        Transform skipTrans = transform.Find("SkipButton");
        if (skipTrans != null) skipTrans.gameObject.SetActive(false);

        Transform contTrans = transform.Find("Continue");
        if (contTrans != null) contTrans.gameObject.SetActive(false);

        Transform debugTrans = transform.Find("DebugText");
        if (debugTrans != null) debugTrans.gameObject.SetActive(false);

        // Deactivate cloned NpcAndStudent multiple-choice UI
        Transform npcStudent = transform.Find("NpcAndStudent");
        if (npcStudent != null) npcStudent.gameObject.SetActive(false);

        Transform oldProgress = transform.Find("ProgressCountTMP");
        if (oldProgress != null) oldProgress.gameObject.SetActive(false);

        // Deactivate obsolete text components
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string txt = tmp.text ?? "";
            string gName = tmp.name.ToLower();

            if (txt.Contains("0/3") || gName.Contains("puzzlecount") || gName.Contains("progresscount")) {
                tmp.gameObject.SetActive(false);
            }
        }
    }

    public void InitializeRoundsData() {
        string audioDir = "Assets/Audio/2A/7_Collocations/Roleplay/RP01/";

        rounds = new RP01RoundData[] {
            // Round 1
            new RP01RoundData {
                roundId = 1,
                leoHalfText = "catch",
                classmateWords = new string[] { "train", "money", "ready", "clever" },
                correctIndex = 0,
                fullCollocationText = "CATCH A TRAIN",
                direction = MatchingDirection.HubFirst,
                #if UNITY_EDITOR
                leoWordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch.mp3"),
                #endif
                #if UNITY_EDITOR
                pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a train.mp3")
                #endif
            },
            // Round 2
            new RP01RoundData {
                roundId = 2,
                leoHalfText = "save",
                classmateWords = new string[] { "a cold", "someone a seat", "dressed", "grand" },
                correctIndex = 1,
                fullCollocationText = "SAVE SOMEONE A SEAT",
                direction = MatchingDirection.HubFirst,
                #if UNITY_EDITOR
                leoWordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "save.mp3"),
                #endif
                #if UNITY_EDITOR
                pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "save someone a seat.mp3")
                #endif
            },
            // Round 3
            new RP01RoundData {
                roundId = 3,
                leoHalfText = "get",
                classmateWords = new string[] { "along with", "a ball", "water", "original" },
                correctIndex = 0,
                fullCollocationText = "GET ALONG WITH",
                direction = MatchingDirection.HubFirst,
                #if UNITY_EDITOR
                leoWordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get.mp3"),
                #endif
                #if UNITY_EDITOR
                pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get along with.mp3")
                #endif
            },
            // Round 4 (PartnerFirst)
            new RP01RoundData {
                roundId = 4,
                leoHalfText = "idea",
                classmateWords = new string[] { "innovative", "a fire", "time", "married" },
                correctIndex = 0,
                fullCollocationText = "INNOVATIVE IDEA",
                direction = MatchingDirection.PartnerFirst,
                #if UNITY_EDITOR
                leoWordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "idea.mp3"),
                #endif
                #if UNITY_EDITOR
                pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "innovative idea.mp3")
                #endif
            },
            // Round 5 (PartnerFirst)
            new RP01RoundData {
                roundId = 5,
                leoHalfText = "thief",
                classmateWords = new string[] { "get", "catch", "idea", "save" },
                correctIndex = 1,
                fullCollocationText = "CATCH A THIEF",
                direction = MatchingDirection.PartnerFirst,
                #if UNITY_EDITOR
                leoWordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "thief.mp3"),
                #endif
                #if UNITY_EDITOR
                pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a thief.mp3")
                #endif
            },
            // Round 6 (PartnerFirst)
            new RP01RoundData {
                roundId = 6,
                leoHalfText = "energy",
                classmateWords = new string[] { "get", "catch", "idea", "save" },
                correctIndex = 3,
                fullCollocationText = "SAVE ENERGY",
                direction = MatchingDirection.PartnerFirst,
                #if UNITY_EDITOR
                leoWordAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "energy.mp3"),
                #endif
                #if UNITY_EDITOR
                pairAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "save energy.mp3")
                #endif
            }
        };
    }

    public void PlayLeoWordAudio() {
        if (rounds != null && currentRoundIndex >= 0 && currentRoundIndex < rounds.Length) {
            AudioClip clip = rounds[currentRoundIndex].leoWordAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }
    }

    private void LoadRound(int index) {
        if (rounds == null || index < 0 || index >= rounds.Length) return;

        currentRoundIndex = index;
        currentRetryCount = 0;
        isRoundInputActive = true;

        RP01RoundData currentRound = rounds[index];

        UpdateProgressUI();
        ShowFeedback($"Round {index + 1}/6: Tap the classmate who completes LEO's card!", true);

        if (stageContainer != null) stageContainer.SetActive(true);

        // Display LEO card half
        if (leoCardTMP != null) {
            leoCardTMP.text = currentRound.leoHalfText;
        }

        // Animate LEO card entrance
        if (leoCardRect != null) {
            leoCardRect.DOKill();
            leoCardRect.anchoredPosition = new Vector2(0f, 120f);
            leoCardRect.localScale = Vector3.zero;
            leoCardRect.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        // Play LEO prompt word audio on round load
        if (currentRound.leoWordAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(currentRound.leoWordAudio);
        }

        float[] classmateXPos = new float[] { -340f, -110f, 110f, 340f };

        // Display 4 Classmate cards
        for (int i = 0; i < 4; i++) {
            if (i < currentRound.classmateWords.Length) {
                if (classmateCardTMPs != null && i < classmateCardTMPs.Length && classmateCardTMPs[i] != null) {
                    classmateCardTMPs[i].text = currentRound.classmateWords[i];
                }
                if (classmateCardRects != null && i < classmateCardRects.Length && classmateCardRects[i] != null) {
                    classmateCardRects[i].gameObject.SetActive(true);
                    classmateCardRects[i].DOKill();
                    classmateCardRects[i].anchoredPosition = new Vector2(classmateXPos[i], -110f);
                    classmateCardRects[i].localScale = Vector3.zero;
                    classmateCardRects[i].DOScale(Vector3.one, 0.35f).SetDelay(0.08f * i).SetEase(Ease.OutBack);

                    Image img = classmateCardRects[i].GetComponent<Image>();
                    if (img != null) img.color = new Color(0.12f, 0.25f, 0.48f, 0.95f);
                }
                if (classmateButtons != null && i < classmateButtons.Length && classmateButtons[i] != null) {
                    classmateButtons[i].interactable = true;
                }
            }
        }
    }

    public void OnClassmateSelected(int classmateIndex) {
        if (!isRoundInputActive) return;
        if (currentRoundIndex < 0 || currentRoundIndex >= rounds.Length) return;

        RP01RoundData round = rounds[currentRoundIndex];

        if (classmateIndex == round.correctIndex) {
            OnCorrectClassmateSelected(classmateIndex, round);
        } else {
            OnWrongClassmateSelected(classmateIndex, round);
        }
    }

    private void OnCorrectClassmateSelected(int classmateIndex, RP01RoundData round) {
        isRoundInputActive = false;
        correctRoundsCount++;
        UpdateScoreUI();

        // Highlight correct classmate card green
        if (classmateCardRects != null && classmateIndex < classmateCardRects.Length && classmateCardRects[classmateIndex] != null) {
            Image img = classmateCardRects[classmateIndex].GetComponent<Image>();
            if (img != null) img.color = new Color(0.13f, 0.77f, 0.36f, 1f);

            // Animate LEO and classmate card together
            Vector2 targetPos = new Vector2(classmateCardRects[classmateIndex].anchoredPosition.x, -20f);
            if (leoCardRect != null) {
                leoCardRect.DOKill();
                leoCardRect.DOAnchorPos(targetPos + new Vector2(-110f, 0f), 0.35f);
            }
            classmateCardRects[classmateIndex].DOKill();
            classmateCardRects[classmateIndex].DOAnchorPos(targetPos + new Vector2(110f, 0f), 0.35f);
        }

        if (sfxMagnetSnap != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxMagnetSnap);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        FlashCollocationBanner(round.fullCollocationText);
        ShowFeedback($"Correct! {round.fullCollocationText}", true);

        if (round.pairAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(round.pairAudio);
        }

        StartCoroutine(AdvanceToNextRoundWithDelay(1.8f));
    }

    private void OnWrongClassmateSelected(int classmateIndex, RP01RoundData round) {
        // Shake selected wrong classmate card
        if (classmateCardRects != null && classmateIndex < classmateCardRects.Length && classmateCardRects[classmateIndex] != null) {
            classmateCardRects[classmateIndex].DOKill();
            classmateCardRects[classmateIndex].DOShakePosition(0.35f, 18f, 10, 90f);
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        currentRetryCount++;

        if (currentRetryCount == 1) {
            // First wrong attempt -> Allow 1 retry
            ShowFeedback("Not quite! Try again.", false);
        } else {
            // Second wrong attempt -> End round without marking correct and advance
            isRoundInputActive = false;
            ShowFeedback($"The correct partner was '{round.classmateWords[round.correctIndex]}'.", false);
            StartCoroutine(AdvanceToNextRoundWithDelay(1.8f));
        }
    }

    private IEnumerator AdvanceToNextRoundWithDelay(float delay) {
        yield return new WaitForSeconds(delay);

        if (sfxCurtain != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxCurtain);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        int nextRoundIndex = currentRoundIndex + 1;
        if (nextRoundIndex < rounds.Length) {
            LoadRound(nextRoundIndex);
        } else {
            EndRoleplayActivity();
        }
    }

    private void EndRoleplayActivity() {
        isRoundInputActive = false;

        bool passed = (correctRoundsCount >= PASS_THRESHOLD);

        if (passed) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Roleplay);
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        ShowResultPopup(passed);
    }

    private void ShowResultPopup(bool passed) {
        if (resultPopup != null) {
            resultPopup.SetActive(true);
            resultPopup.transform.DOKill();
            resultPopup.transform.localScale = Vector3.zero;
            resultPopup.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = passed ? "ROLE PLAY PASSED!" : "TRY AGAIN!";
            resultTitleTMP.color = passed ? new Color(0.13f, 0.77f, 0.36f) : new Color(0.85f, 0.2f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Correct Rounds: {correctRoundsCount}/{TOTAL_ROUNDS}\n{(passed ? "Sub-flag Roleplay-RP01 Unlocked!" : "Get 5 or more correct to pass!")}";
        }
    }

    private void FlashCollocationBanner(string fullText) {
        if (flashBannerTMP != null) {
            flashBannerTMP.gameObject.SetActive(true);
            flashBannerTMP.text = fullText;
            flashBannerTMP.transform.DOKill();
            flashBannerTMP.transform.localScale = Vector3.zero;
            flashBannerTMP.transform.DOScale(Vector3.one * 1.2f, 0.25f).SetEase(Ease.OutBack).OnComplete(() => {
                flashBannerTMP.transform.DOScale(Vector3.one, 0.15f);
                DOVirtual.DelayedCall(0.8f, () => {
                    if (flashBannerTMP != null) flashBannerTMP.gameObject.SetActive(false);
                });
            });
        }
    }

    private void ShowFeedback(string msg, bool isSuccess) {
        if (feedbackBannerTMP != null) {
            feedbackBannerTMP.gameObject.SetActive(true);
            feedbackBannerTMP.text = msg;
            feedbackBannerTMP.color = isSuccess ? new Color(0.9f, 0.95f, 1f) : new Color(0.95f, 0.3f, 0.3f);
        }
    }

    private void UpdateProgressUI() {
        if (roundProgressTMP != null) {
            roundProgressTMP.text = $"Round {currentRoundIndex + 1}/{TOTAL_ROUNDS}";
        }
    }

    private void UpdateScoreUI() {
        if (scoreTMP != null) {
            scoreTMP.text = $"Correct: {correctRoundsCount}/{TOTAL_ROUNDS}";
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string txt = tmp.text ?? "";
            if (txt.Contains("Main Console") || txt.Contains("Quiz — Collocations")) {
                tmp.gameObject.SetActive(false);
            }
        }

        if (rp01HeaderTMP != null) {
            rp01HeaderTMP.gameObject.SetActive(false); // Deactivate wack dev header text
        }
        if (rp01TitleTMP != null) {
            rp01TitleTMP.gameObject.SetActive(true);
            rp01TitleTMP.text = "On Stage — Find Your Other Half";
            rp01TitleTMP.color = Color.white;
            RectTransform rt = rp01TitleTMP.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = new Vector2(0f, 460f);
        }
        if (rp01InstructionTMP != null) {
            rp01InstructionTMP.gameObject.SetActive(true);
            rp01InstructionTMP.text = "Tap the classmate holding the word that completes LEO's card!";
            rp01InstructionTMP.color = new Color(0.9f, 0.95f, 1f);
            RectTransform rt = rp01InstructionTMP.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = new Vector2(0f, 410f);
        }
    }

    private void SetupButtonListeners() {
        if (leoCardButton != null) {
            leoCardButton.onClick.RemoveAllListeners();
            leoCardButton.onClick.AddListener(() => PlayLeoWordAudio());
        }

        if (classmateButtons != null) {
            for (int i = 0; i < classmateButtons.Length; i++) {
                int index = i;
                if (classmateButtons[i] != null) {
                    classmateButtons[i].onClick.RemoveAllListeners();
                    classmateButtons[i].onClick.AddListener(() => OnClassmateSelected(index));
                }
            }
        }

        if (retryButton != null) {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(StartNewGame);
        }

        if (returnHubButton != null) {
            returnHubButton.onClick.RemoveAllListeners();
            returnHubButton.onClick.AddListener(ReturnToHub);
        }
    }

    protected override void OnNextButtonClicked() {
        ReturnToHub();
    }

    public void ReturnToHub() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Roleplay);
        }
    }

    private void AutoFindUIReferences() {
        if (stageContainer == null) {
            Transform t = transform.Find("StageContainer");
            if (t != null) stageContainer = t.gameObject;
        }

        if (leoCardRect == null || leoCardButton == null) {
            Transform t = transform.Find("StageContainer/LeoCard") ?? transform.Find("LeoCard");
            if (t != null) {
                leoCardRect = t.GetComponent<RectTransform>();
                leoCardTMP = t.GetComponentInChildren<TextMeshProUGUI>();
                leoCardButton = t.GetComponent<Button>();
                if (leoCardButton == null) leoCardButton = t.gameObject.AddComponent<Button>();
            }
        }

        if (classmateCardRects == null || classmateCardRects.Length < 4) {
            classmateCardRects = new RectTransform[4];
            classmateCardTMPs = new TextMeshProUGUI[4];
            classmateButtons = new Button[4];

            for (int i = 0; i < 4; i++) {
                Transform t = transform.Find($"StageContainer/ClassmateCard_{i + 1}") ?? transform.Find($"ClassmateCard_{i + 1}");
                if (t != null) {
                    classmateCardRects[i] = t.GetComponent<RectTransform>();
                    classmateCardTMPs[i] = t.GetComponentInChildren<TextMeshProUGUI>();
                    classmateButtons[i] = t.GetComponent<Button>();
                }
            }
        }

        if (roundProgressTMP == null) {
            Transform t = transform.Find("RoundProgressText") ?? transform.Find("ProgressIndicator");
            if (t != null) roundProgressTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (scoreTMP == null) {
            Transform t = transform.Find("ScoreText") ?? transform.Find("ScoreIndicator");
            if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (feedbackBannerTMP == null) {
            Transform t = transform.Find("FeedbackText");
            if (t != null) feedbackBannerTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (flashBannerTMP == null) {
            Transform t = transform.Find("FlashBannerText");
            if (t != null) flashBannerTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (rp01TitleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title");
            if (t != null) rp01TitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (rp01HeaderTMP == null) {
            Transform t = transform.Find("Heading") ?? transform.Find("Header");
            if (t != null) rp01HeaderTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (rp01InstructionTMP == null) {
            Transform t = transform.Find("InstructionText") ?? transform.Find("Instruction");
            if (t != null) rp01InstructionTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (resultPopup == null) {
            Transform t = transform.Find("ResultPopup") ?? transform.Find("ResultPanel");
            if (t != null) resultPopup = t.gameObject;
        }

        if (resultPopup != null) {
            Button[] resBtns = resultPopup.GetComponentsInChildren<Button>(true);
            foreach (var b in resBtns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (retryButton == null && (bName.Contains("retry") || bName.Contains("again"))) retryButton = b;
                if (returnHubButton == null && (bName.Contains("hub") || bName.Contains("home") || bName.Contains("continue"))) returnHubButton = b;
            }
        }
    }
}