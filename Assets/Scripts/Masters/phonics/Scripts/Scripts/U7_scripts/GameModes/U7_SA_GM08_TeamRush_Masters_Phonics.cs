using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U7_SA_GM08_TeamRush_Masters_Phonics : MonoBehaviour
    {
        [Header("Rush Timer & HUD")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Slider timerBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private TextMeshProUGUI rushAnnouncementText;

        [Header("Chute Buttons (4 Chutes)")]
        [SerializeField] private Button chute1Btn_LongA;      // /eɪ/ (ai, ay, ei, ey)
        [SerializeField] private Button chute2Btn_LongE;      // /iː/ (ee, ea, ey, ie)
        [SerializeField] private Button chute3Btn_LongO;      // /əʊ/ (oa, ow, oe)
        [SerializeField] private Button chute4Btn_Diphthong;  // /ɔɪ/, /aʊ/ (oi, oy, ou, ow)

        [Header("Word Card Spawner & Drag")]
        [SerializeField] private RectTransform currentCardRect;
        [SerializeField] private TextMeshProUGUI currentWordText;
        [SerializeField] private CanvasGroup currentCardCanvasGroup;

        [Header("Result Summary Modal")]
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TextMeshProUGUI summaryTitleText;
        [SerializeField] private TextMeshProUGUI summaryStatsText;
        [SerializeField] private Button summaryContinueBtn;

        [Header("Audio SFX")]
        public AudioClip rushBGM;
        public AudioClip correctSFX;
        public AudioClip wrongSFX;
        public AudioClip timerTickSFX;
        public AudioClip rushEndSFX;

        [Header("Card & Feedback Colors")]
        [Tooltip("Default card background color")]
        public Color defaultCardColor = Color.white;
        [Tooltip("Card color on correct answer")]
        public Color correctColor = new Color(0.2f, 0.82f, 0.45f, 1f);
        [Tooltip("Card color on wrong answer")]
        public Color wrongColor = new Color(0.92f, 0.3f, 0.3f, 1f);

        [Header("Word Pool")]
        public List<TeamRushItem> wordPool = new List<TeamRushItem>();

        private float remainingTime = 60f;
        private int lives = 3;
        private bool isGameActive = false;
        private int currentWordIndex = 0;
        private int totalScore = 0;
        private int comboCount = 0;
        private int correctCount = 0;
        private int totalAttempts = 0;
        private bool isProcessingInput = false;

        private Vector2 dragStartPos;
        private Canvas rootCanvas;
        private RectTransform canvasRect;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            StartGame();
        }

        private void OnDisable()
        {
            isGameActive = false;
            StopAllCoroutines();
        }

        public void AutoBindHierarchyElements()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
                canvasRect = rootCanvas.GetComponent<RectTransform>();

            if (wordPool == null || wordPool.Count == 0)
            {
                wordPool = new List<TeamRushItem>
                {
                    // Long A (/eɪ/)
                    new TeamRushItem("rain", SoundCategoryType.LongA_AY),
                    new TeamRushItem("train", SoundCategoryType.LongA_AY),
                    new TeamRushItem("paint", SoundCategoryType.LongA_AY),
                    new TeamRushItem("day", SoundCategoryType.LongA_AY),
                    new TeamRushItem("stay", SoundCategoryType.LongA_AY),
                    new TeamRushItem("play", SoundCategoryType.LongA_AY),
                    new TeamRushItem("eight", SoundCategoryType.LongA_AY),
                    new TeamRushItem("weigh", SoundCategoryType.LongA_AY),
                    new TeamRushItem("they", SoundCategoryType.LongA_AY),

                    // Long E (/iː/)
                    new TeamRushItem("see", SoundCategoryType.LongE_EE),
                    new TeamRushItem("tree", SoundCategoryType.LongE_EE),
                    new TeamRushItem("green", SoundCategoryType.LongE_EE),
                    new TeamRushItem("meet", SoundCategoryType.LongE_EE),
                    new TeamRushItem("team", SoundCategoryType.LongE_EE),
                    new TeamRushItem("clean", SoundCategoryType.LongE_EE),
                    new TeamRushItem("leaf", SoundCategoryType.LongE_EE),
                    new TeamRushItem("key", SoundCategoryType.LongE_EE),
                    new TeamRushItem("money", SoundCategoryType.LongE_EE),
                    new TeamRushItem("queen", SoundCategoryType.LongE_EE),

                    // Long O (/əʊ/)
                    new TeamRushItem("boat", SoundCategoryType.LongO_OH),
                    new TeamRushItem("coat", SoundCategoryType.LongO_OH),
                    new TeamRushItem("road", SoundCategoryType.LongO_OH),
                    new TeamRushItem("soap", SoundCategoryType.LongO_OH),
                    new TeamRushItem("snow", SoundCategoryType.LongO_OH),
                    new TeamRushItem("grow", SoundCategoryType.LongO_OH),
                    new TeamRushItem("slow", SoundCategoryType.LongO_OH),
                    new TeamRushItem("toe", SoundCategoryType.LongO_OH),
                    new TeamRushItem("goes", SoundCategoryType.LongO_OH),

                    // Diphthongs (/ɔɪ/ & /aʊ/)
                    new TeamRushItem("coin", SoundCategoryType.Diphthong_OY),
                    new TeamRushItem("boil", SoundCategoryType.Diphthong_OY),
                    new TeamRushItem("soil", SoundCategoryType.Diphthong_OY),
                    new TeamRushItem("join", SoundCategoryType.Diphthong_OY),
                    new TeamRushItem("oil", SoundCategoryType.Diphthong_OY),
                    new TeamRushItem("boy", SoundCategoryType.Diphthong_OY),
                    new TeamRushItem("toy", SoundCategoryType.Diphthong_OY),
                    new TeamRushItem("joy", SoundCategoryType.Diphthong_OY),
                    new TeamRushItem("couch", SoundCategoryType.Diphthong_OW),
                    new TeamRushItem("cloud", SoundCategoryType.Diphthong_OW),
                    new TeamRushItem("found", SoundCategoryType.Diphthong_OW),
                    new TeamRushItem("house", SoundCategoryType.Diphthong_OW),
                    new TeamRushItem("mouse", SoundCategoryType.Diphthong_OW),
                    new TeamRushItem("owl", SoundCategoryType.Diphthong_OW),
                    new TeamRushItem("town", SoundCategoryType.Diphthong_OW),
                    new TeamRushItem("clown", SoundCategoryType.Diphthong_OW),
                    new TeamRushItem("brown", SoundCategoryType.Diphthong_OW)
                };
            }

            // Bind HUD elements (flexible search across template variations)
            if (timerText == null)
            {
                Transform tt = transform.Find("ProgressHUD/ProgressText") 
                            ?? transform.Find("ProgressHUD/Progress_Text")
                            ?? transform.Find("ProgressHUD/ItemCounterText")
                            ?? transform.Find("HUD/ProgressText")
                            ?? transform.Find("HUD/Progress_Text")
                            ?? transform.Find("HUD/TimerText") 
                            ?? transform.Find("TimerText") 
                            ?? transform.Find("ProgressText")
                            ?? transform.Find("Progress_Text");
                if (tt != null) timerText = tt.GetComponent<TextMeshProUGUI>();
            }

            if (timerBar == null)
            {
                Transform tb = transform.Find("ProgressHUD/ProgressBar") 
                            ?? transform.Find("ProgressHUD/Progress_Bar")
                            ?? transform.Find("HUD/ProgressBar")
                            ?? transform.Find("HUD/TimerBar") 
                            ?? transform.Find("TimerBar") 
                            ?? transform.Find("ProgressBar")
                            ?? transform.Find("Progress_Bar");
                if (tb != null) timerBar = tb.GetComponent<Slider>();
                if (timerBar == null) timerBar = GetComponentInChildren<Slider>(true);
            }

            if (scoreText == null)
            {
                Transform st = transform.Find("ScoreHUD/ScoreText")
                            ?? transform.Find("ScoreHUD/Score_Text")
                            ?? transform.Find("Score_HUD/Score_Text") 
                            ?? transform.Find("Score_HUD/ScoreText")
                            ?? transform.Find("HUD_Container/Score_Text")
                            ?? transform.Find("HUD_Container/ScoreText")
                            ?? transform.Find("ProgressHUD/Score_Text")
                            ?? transform.Find("HUD/ScoreText") 
                            ?? transform.Find("HUD/Score_Text")
                            ?? transform.Find("ScoreText") 
                            ?? transform.Find("Score_Text");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }

            if (comboText == null)
            {
                Transform ct = transform.Find("ScoreHUD/StreakText")
                            ?? transform.Find("ScoreHUD/Streak_Text")
                            ?? transform.Find("Score_HUD/Streak_Text") 
                            ?? transform.Find("Score_HUD/StreakText")
                            ?? transform.Find("HUD_Container/Streak_Text")
                            ?? transform.Find("HUD/ComboText") 
                            ?? transform.Find("ComboText") 
                            ?? transform.Find("StreakText")
                            ?? transform.Find("Streak_Text");
                if (ct != null) comboText = ct.GetComponent<TextMeshProUGUI>();
            }

            // Find 4 chutes
            Transform chutesGroup = transform.Find("Chutes") ?? transform.Find("ChutesContainer") ?? transform;
            if (chute1Btn_LongA == null)
            {
                Transform c1 = chutesGroup.Find("Chute1_LongA") ?? chutesGroup.Find("Chute1");
                if (c1 != null) chute1Btn_LongA = c1.GetComponentInChildren<Button>(true);
            }
            if (chute2Btn_LongE == null)
            {
                Transform c2 = chutesGroup.Find("Chute2_LongE") ?? chutesGroup.Find("Chute2");
                if (c2 != null) chute2Btn_LongE = c2.GetComponentInChildren<Button>(true);
            }
            if (chute3Btn_LongO == null)
            {
                Transform c3 = chutesGroup.Find("Chute3_LongO") ?? chutesGroup.Find("Chute3");
                if (c3 != null) chute3Btn_LongO = c3.GetComponentInChildren<Button>(true);
            }
            if (chute4Btn_Diphthong == null)
            {
                Transform c4 = chutesGroup.Find("Chute4_Diphthong") ?? chutesGroup.Find("Chute4");
                if (c4 != null) chute4Btn_Diphthong = c4.GetComponentInChildren<Button>(true);
            }

            // Find Word Card and wire Drag Handler
            if (currentCardRect == null)
            {
                Transform cc = transform.Find("CurrentCard") ?? transform.Find("CurrentWordCard") ?? transform.Find("Card");
                if (cc != null) currentCardRect = cc.GetComponent<RectTransform>();
            }
            if (currentCardRect != null)
            {
                if (currentCardCanvasGroup == null)
                    currentCardCanvasGroup = currentCardRect.GetComponent<CanvasGroup>();
                if (currentWordText == null)
                    currentWordText = currentCardRect.GetComponentInChildren<TextMeshProUGUI>(true);

                var dragHelper = currentCardRect.GetComponent<U7_TeamRushCardDragHandler>() 
                              ?? currentCardRect.gameObject.AddComponent<U7_TeamRushCardDragHandler>();
                dragHelper.manager = this;
            }

            if (summaryPanel == null)
            {
                Transform sp = transform.Find("SummaryPanel") ?? transform.Find("VictoryPanel");
                if (sp != null) summaryPanel = sp.gameObject;
            }
            if (summaryPanel != null)
            {
                summaryTitleText = summaryPanel.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
                summaryStatsText = summaryPanel.transform.Find("StatsText")?.GetComponent<TextMeshProUGUI>();
                summaryContinueBtn = summaryPanel.GetComponentInChildren<Button>(true);
            }

            // Hook Chute Tap Listeners
            if (chute1Btn_LongA != null)
            {
                chute1Btn_LongA.onClick.RemoveAllListeners();
                chute1Btn_LongA.onClick.AddListener(() => OnChuteSelected(SoundCategoryType.LongA_AY));
            }
            if (chute2Btn_LongE != null)
            {
                chute2Btn_LongE.onClick.RemoveAllListeners();
                chute2Btn_LongE.onClick.AddListener(() => OnChuteSelected(SoundCategoryType.LongE_EE));
            }
            if (chute3Btn_LongO != null)
            {
                chute3Btn_LongO.onClick.RemoveAllListeners();
                chute3Btn_LongO.onClick.AddListener(() => OnChuteSelected(SoundCategoryType.LongO_OH));
            }
            if (chute4Btn_Diphthong != null)
            {
                chute4Btn_Diphthong.onClick.RemoveAllListeners();
                chute4Btn_Diphthong.onClick.AddListener(() => OnChuteSelected(SoundCategoryType.Diphthong_OY));
            }

            if (summaryContinueBtn != null)
            {
                summaryContinueBtn.onClick.RemoveAllListeners();
                summaryContinueBtn.onClick.AddListener(OnSummaryContinueClicked);
            }
        }

        public void StartGame()
        {
            remainingTime = 60f;
            lives = 3;
            totalScore = 0;
            comboCount = 0;
            correctCount = 0;
            totalAttempts = 0;
            currentWordIndex = 0;
            isGameActive = true;
            isProcessingInput = false;

            if (summaryPanel != null) summaryPanel.SetActive(false);

            ShuffleWordPool();
            UpdateHUD();
            SpawnNextWordCard();
            StartCoroutine(TimerRoutine());

            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip intro = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("U07_VO_act6_intro") ??
                                  U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Team Rush! Sort as many vowel sounds as you can in 60 seconds!");
                if (intro != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(intro);
            }
        }

        private void ShuffleWordPool()
        {
            for (int i = 0; i < wordPool.Count; i++)
            {
                int r = Random.Range(i, wordPool.Count);
                var temp = wordPool[i];
                wordPool[i] = wordPool[r];
                wordPool[r] = temp;
            }
        }

        private IEnumerator TimerRoutine()
        {
            float lastTickTime = remainingTime;
            while (remainingTime > 0f && isGameActive && lives > 0)
            {
                remainingTime -= Time.deltaTime;
                if (remainingTime < 0f) remainingTime = 0f;

                UpdateTimerVisuals();

                // Urgent ticking when < 10 seconds
                if (remainingTime <= 10f && Mathf.FloorToInt(remainingTime) < Mathf.FloorToInt(lastTickTime))
                {
                    if (timerTickSFX != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                        U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(timerTickSFX);
                }
                lastTickTime = remainingTime;

                yield return null;
            }

            if (isGameActive)
            {
                EndGame();
            }
        }

        private void UpdateTimerVisuals()
        {
            if (timerText != null)
            {
                int seconds = Mathf.CeilToInt(remainingTime);
                string livesDisplay = new string('I', lives).PadRight(3, '-');
                timerText.text = $"Time: <b>{seconds}s</b>   Lives: <b>{lives}/3</b>";
                timerText.color = seconds <= 10 ? new Color(0.95f, 0.25f, 0.25f, 1f) : new Color(0.95f, 0.95f, 0.95f, 1f);
            }

            if (timerBar != null)
            {
                timerBar.maxValue = 60f;
                timerBar.value = remainingTime;
                U7_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(timerBar);
            }
        }

        private void SpawnNextWordCard()
        {
            if (!isGameActive || lives <= 0) return;

            if (currentWordIndex >= wordPool.Count)
            {
                ShuffleWordPool();
                currentWordIndex = 0;
            }

            var item = wordPool[currentWordIndex];

            if (currentCardRect != null)
            {
                currentCardRect.localScale = Vector3.one;
                currentCardRect.localPosition = new Vector3(0f, 100f, 0f);
                currentCardRect.localRotation = Quaternion.identity;
                var img = currentCardRect.GetComponent<Image>();
                if (img != null) img.color = defaultCardColor;
            }
            if (currentCardCanvasGroup != null)
                currentCardCanvasGroup.alpha = 1f;

            if (currentWordText != null)
            {
                currentWordText.text = $"<b>{item.word}</b>";
                currentWordText.color = new Color(0.1f, 0.15f, 0.2f, 1f);
            }

            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip clip = item.wordAudio ??
                                 U7_SA_AudioManager_Masters_Phonics.ResolveAudio(item.word) ??
                                 U7_SA_AudioManager_Masters_Phonics.ResolveAudio($"U07_WB_{item.word}");
                if (clip != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            }

            isProcessingInput = false;
        }

        // =========================================================================
        // Drag & Flick Handling on Card
        // =========================================================================

        public void OnCardBeginDrag(PointerEventData eventData)
        {
            if (!isGameActive || isProcessingInput || currentCardRect == null) return;
            dragStartPos = eventData.position;
        }

        public void OnCardDrag(PointerEventData eventData)
        {
            if (!isGameActive || isProcessingInput || currentCardRect == null) return;

            Camera eventCam = eventData.pressEventCamera;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(currentCardRect, eventData.position, eventCam, out Vector3 worldPos))
            {
                currentCardRect.position = worldPos;
                float tilt = Mathf.Clamp(-(eventData.position.x - dragStartPos.x) * 0.06f, -15f, 15f);
                currentCardRect.localRotation = Quaternion.Euler(0f, 0f, tilt);
            }
        }

        public void OnCardEndDrag(PointerEventData eventData)
        {
            if (!isGameActive || isProcessingInput || currentCardRect == null) return;

            Button[] chutes = { chute1Btn_LongA, chute2Btn_LongE, chute3Btn_LongO, chute4Btn_Diphthong };
            SoundCategoryType[] chuteTypes = { SoundCategoryType.LongA_AY, SoundCategoryType.LongE_EE, SoundCategoryType.LongO_OH, SoundCategoryType.Diphthong_OY };

            SoundCategoryType bestType = SoundCategoryType.Other;
            float minDistance = float.MaxValue;
            Camera cam = eventData.pressEventCamera;

            for (int i = 0; i < chutes.Length; i++)
            {
                if (chutes[i] == null) continue;
                RectTransform chuteRt = chutes[i].GetComponent<RectTransform>();
                if (chuteRt == null) continue;

                if (RectTransformUtility.RectangleContainsScreenPoint(chuteRt, eventData.position, cam))
                {
                    bestType = chuteTypes[i];
                    minDistance = 0f;
                    break;
                }

                Vector2 chuteScreen = RectTransformUtility.WorldToScreenPoint(cam, chuteRt.position);
                float dist = Vector2.Distance(eventData.position, chuteScreen);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestType = chuteTypes[i];
                }
            }

            float deltaY = eventData.position.y - dragStartPos.y;
            // Evaluates choice if dropped within range of a chute or flicked downward towards chutes
            if (minDistance <= 220f || (deltaY < -60f && minDistance < 420f))
            {
                OnChuteSelected(bestType);
            }
            else
            {
                StartCoroutine(SnapBackCard());
            }
        }

        private IEnumerator SnapBackCard()
        {
            if (currentCardRect == null) yield break;

            float duration = 0.22f;
            float elapsed = 0f;
            Vector3 startPos = currentCardRect.localPosition;
            Quaternion startRot = currentCardRect.localRotation;
            Vector3 targetPos = new Vector3(0f, 100f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                currentCardRect.localPosition = Vector3.Lerp(startPos, targetPos, ease);
                currentCardRect.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, ease);
                yield return null;
            }

            currentCardRect.localPosition = targetPos;
            currentCardRect.localRotation = Quaternion.identity;
        }

        // =========================================================================
        // Chute Evaluation & Scoring
        // =========================================================================

        public void OnChuteSelected(SoundCategoryType chosenType)
        {
            if (!isGameActive || isProcessingInput || currentWordIndex >= wordPool.Count) return;

            isProcessingInput = true;
            totalAttempts++;
            var item = wordPool[currentWordIndex];

            // Normalize diphthong categories (oi/oy and ou/ow both map to 4th chute)
            bool isCorrect = false;
            if (chosenType == SoundCategoryType.Diphthong_OY || chosenType == SoundCategoryType.Diphthong_OW)
            {
                isCorrect = (item.targetChute == SoundCategoryType.Diphthong_OY || item.targetChute == SoundCategoryType.Diphthong_OW);
            }
            else
            {
                isCorrect = (chosenType == item.targetChute);
            }

            if (isCorrect)
            {
                correctCount++;
                comboCount++;

                // +10 per correct + 5 streak bonus every 5 in a row
                int points = 10;
                if (comboCount % 5 == 0) points += 5;
                totalScore += points;

                PlaySFX(correctSFX, true);

                var img = currentCardRect != null ? currentCardRect.GetComponent<Image>() : null;
                if (img != null) img.color = correctColor;

                StartCoroutine(AnimateCardSuccess(() =>
                {
                    currentWordIndex++;
                    SpawnNextWordCard();
                }));
            }
            else
            {
                comboCount = 0;
                lives--;
                PlaySFX(wrongSFX, false);

                var img = currentCardRect != null ? currentCardRect.GetComponent<Image>() : null;
                if (img != null) img.color = wrongColor;

                StartCoroutine(AnimateCardWrong(() =>
                {
                    if (img != null) img.color = defaultCardColor;

                    if (lives <= 0)
                    {
                        EndGame();
                    }
                    else
                    {
                        isProcessingInput = false;
                        StartCoroutine(SnapBackCard());
                    }
                }));
            }

            UpdateHUD();
        }

        private IEnumerator AnimateCardSuccess(System.Action onComplete)
        {
            if (currentCardRect == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float duration = 0.18f;
            float elapsed = 0f;
            Vector3 startPos = currentCardRect.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                currentCardRect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.25f, t);
                currentCardRect.localPosition = startPos + new Vector3(0f, 35f * t, 0f);
                if (currentCardCanvasGroup != null)
                    currentCardCanvasGroup.alpha = 1f - t;
                yield return null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator AnimateCardWrong(System.Action onComplete)
        {
            if (currentCardRect == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float duration = 0.26f;
            float elapsed = 0f;
            Vector3 centerPos = new Vector3(0f, 100f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float xOffset = Mathf.Sin(elapsed * 50f) * 18f * (1f - (elapsed / duration));
                currentCardRect.localPosition = new Vector3(centerPos.x + xOffset, centerPos.y, 0f);
                yield return null;
            }

            currentCardRect.localPosition = centerPos;
            onComplete?.Invoke();
        }

        private void EndGame()
        {
            isGameActive = false;

            if (rushEndSFX != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(rushEndSFX);

            int earnedStars = (correctCount >= 18) ? 3 : (correctCount >= 10) ? 2 : 1;

            if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(6, earnedStars);
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(totalScore);
            }

            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip victoryClip = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("U07_VO_act6_complete") ??
                                        U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Incredible speed! You scored high in Team Rush!");
                if (victoryClip != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(victoryClip);
            }

            U7_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform, 
                "Activity 6 — Team Rush", 
                earnedStars, 
                totalScore, 
                () => {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenSevenTypesMap();
                }
            );
        }

        private void OnSummaryContinueClicked()
        {
            if (summaryPanel != null) summaryPanel.SetActive(false);

            if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.OpenSevenTypesMap();
            }
        }

        private void PlaySFX(AudioClip clip, bool isCorrect)
        {
            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                if (clip != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(clip);
                else
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayAnswerFeedbackSFX(isCorrect);
            }
        }

        private void UpdateHUD()
        {
            if (scoreText != null)
                scoreText.text = $"Score: <b>{totalScore}</b>";

            if (comboText != null)
                comboText.text = comboCount > 1 ? $"<b>{comboCount}x STREAK!</b>" : "";

            UpdateTimerVisuals();
        }
    }

    /// <summary>
    /// Helper component attached to the word card to relay drag events to the Team Rush manager.
    /// </summary>
    public class U7_TeamRushCardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public U7_SA_GM08_TeamRush_Masters_Phonics manager;

        public void OnBeginDrag(PointerEventData eventData)
        {
            manager?.OnCardBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            manager?.OnCardDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            manager?.OnCardEndDrag(eventData);
        }
    }
}
