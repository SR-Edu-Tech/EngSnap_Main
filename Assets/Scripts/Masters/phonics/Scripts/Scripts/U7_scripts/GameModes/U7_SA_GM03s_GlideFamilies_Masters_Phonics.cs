using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U7_SA_GM03s_GlideFamilies_Masters_Phonics : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Card & Swipe UI")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private TextMeshProUGUI cardWordText;
        [SerializeField] private TextMeshProUGUI cardPhoneticHintText;
        [SerializeField] private CanvasGroup cardCanvasGroup;

        [Header("Target Bins / Direction Indicators")]
        [SerializeField] private RectTransform middleLeftBin;
        [SerializeField] private RectTransform endRightBin;
        [SerializeField] private Button tapMiddleLeftBtn;
        [SerializeField] private Button tapEndRightBtn;
        [SerializeField] private TextMeshProUGUI middleLeftBinTitle;
        [SerializeField] private TextMeshProUGUI endRightBinTitle;

        [Header("Rule Banner")]
        [SerializeField] private TextMeshProUGUI ruleBannerText;

        [Header("HUD & Progress")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private TextMeshProUGUI mascotCommentaryText;
        [SerializeField] private GameObject mascotBubble;

        [Header("Audio SFX Clips (Inspector Overrides)")]
        [Tooltip("Played on correct swipe")]
        public AudioClip correctSFX;
        [Tooltip("Played on wrong swipe")]
        public AudioClip wrongSFX;

        [Header("Card & Feedback Colors")]
        [Tooltip("Default card background color")]
        public Color defaultCardColor = Color.white;
        [Tooltip("Card color on correct swipe / answer")]
        public Color correctColor = new Color(0.2f, 0.82f, 0.45f, 1f);
        [Tooltip("Card color on wrong swipe / answer")]
        public Color wrongColor = new Color(0.92f, 0.3f, 0.3f, 1f);
        [Tooltip("Completed word text color upon correct match")]
        public Color correctTextColor = new Color(0.02f, 0.59f, 0.41f, 1f);

        [Header("Item Pool")]
        public List<GlideSwipeItem> roundAItems = new List<GlideSwipeItem>();
        public List<GlideSwipeItem> roundBItems = new List<GlideSwipeItem>();

        private int currentRound = 1; // 1 = oi vs oy, 2 = ou vs ow
        private int currentIndex = 0;
        private int totalScore = 0;
        private int currentStreak = 0;
        private bool isProcessing = false;

        private Vector2 cardInitialPos;
        private static Sprite proceduralRoundedSprite = null;

        public static Sprite GetOrCreateRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 30;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius - 1.5f)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
                        colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            Vector4 border = new Vector4(radius, radius, radius, radius);
            proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return proceduralRoundedSprite;
        }

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            StartActivity();
        }

        public void AutoBindHierarchyElements()
        {
            if (roundAItems == null || roundAItems.Count == 0)
                roundAItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity5ItemsRoundA();
            if (roundBItems == null || roundBItems.Count == 0)
                roundBItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity5ItemsRoundB();

            if (middleLeftBin == null)
            {
                Transform mTr = transform.Find("MiddleLeftBin") ?? transform.Find("LeftBin") ?? transform.Find("MiddleBin");
                if (mTr != null) middleLeftBin = mTr.GetComponent<RectTransform>();
            }
            if (endRightBin == null)
            {
                Transform eTr = transform.Find("EndRightBin") ?? transform.Find("RightBin") ?? transform.Find("EndBin");
                if (eTr != null) endRightBin = eTr.GetComponent<RectTransform>();
            }

            if (tapMiddleLeftBtn == null && middleLeftBin != null)
                tapMiddleLeftBtn = middleLeftBin.GetComponentInChildren<Button>(true);
            if (tapEndRightBtn == null && endRightBin != null)
                tapEndRightBtn = endRightBin.GetComponentInChildren<Button>(true);

            if (middleLeftBinTitle == null && middleLeftBin != null)
                middleLeftBinTitle = middleLeftBin.GetComponentInChildren<TextMeshProUGUI>(true);
            if (endRightBinTitle == null && endRightBin != null)
                endRightBinTitle = endRightBin.GetComponentInChildren<TextMeshProUGUI>(true);

            if (cardContainer == null)
            {
                Transform cTr = transform.Find("CardContainer") ?? transform.Find("SwipeCard") ?? transform.Find("Card");
                if (cTr != null) cardContainer = cTr.GetComponent<RectTransform>();
            }
            if (cardContainer != null)
            {
                if (cardCanvasGroup == null)
                {
                    cardCanvasGroup = cardContainer.GetComponent<CanvasGroup>();
                    if (cardCanvasGroup == null) cardCanvasGroup = cardContainer.gameObject.AddComponent<CanvasGroup>();
                }
                if (cardWordText == null)
                {
                    Transform wt = cardContainer.Find("WordText") ?? cardContainer.Find("Text") ?? cardContainer.Find("Word");
                    if (wt != null) cardWordText = wt.GetComponent<TextMeshProUGUI>();
                    else cardWordText = cardContainer.GetComponentInChildren<TextMeshProUGUI>();
                }
                if (cardPhoneticHintText == null)
                {
                    Transform ht = cardContainer.Find("HintText") ?? cardContainer.Find("PhoneticText");
                    if (ht != null) cardPhoneticHintText = ht.GetComponent<TextMeshProUGUI>();
                }

                Image cardImg = cardContainer.GetComponent<Image>();
                if (cardImg != null) cardImg.raycastTarget = true;

                var proxy = cardContainer.GetComponent<U7_GlideFamilies_CardDragProxy>();
                if (proxy == null)
                {
                    proxy = cardContainer.gameObject.AddComponent<U7_GlideFamilies_CardDragProxy>();
                }
                proxy.owner = this;
            }

            if (ruleBannerText == null)
            {
                Transform rb = transform.Find("RuleBanner/Text") ?? transform.Find("RuleBannerText") ?? transform.Find("RuleBanner");
                if (rb != null) ruleBannerText = rb.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (progressBar == null)
            {
                Transform pb = transform.Find("ProgressHUD/ProgressBar") 
                            ?? transform.Find("ProgressHUD/Progress_Bar")
                            ?? transform.Find("HUD/ProgressBar") 
                            ?? transform.Find("ProgressBar")
                            ?? transform.Find("Progress_Bar");
                if (pb != null) progressBar = pb.GetComponent<Slider>();
                if (progressBar == null) progressBar = GetComponentInChildren<Slider>(true);
            }
            if (progressText == null)
            {
                Transform pt = transform.Find("ProgressHUD/ProgressText") 
                            ?? transform.Find("ProgressHUD/Progress_Text") 
                            ?? transform.Find("ProgressHUD/ItemCounterText")
                            ?? transform.Find("HUD/ProgressText") 
                            ?? transform.Find("HUD/Progress_Text")
                            ?? transform.Find("ProgressText")
                            ?? transform.Find("Progress_Text")
                            ?? transform.Find("ItemCounterText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }
            if (scoreText == null)
            {
                Transform st = transform.Find("ScoreHUD/ScoreText") 
                            ?? transform.Find("ScoreHUD/Score_Text")
                            ?? transform.Find("HUD_Container/Score_Text") 
                            ?? transform.Find("HUD_Container/ScoreText") 
                            ?? transform.Find("ProgressHUD/Score_Text") 
                            ?? transform.Find("Score_HUD/Score_Text")
                            ?? transform.Find("HUD/ScoreText") 
                            ?? transform.Find("HUD/Score_Text")
                            ?? transform.Find("ScoreText")
                            ?? transform.Find("Score_Text");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }
            if (streakText == null)
            {
                Transform stt = transform.Find("ScoreHUD/StreakText") 
                             ?? transform.Find("ScoreHUD/Streak_Text") 
                             ?? transform.Find("HUD_Container/Streak_Text") 
                             ?? transform.Find("Score_HUD/Streak_Text")
                             ?? transform.Find("HUD/StreakText") 
                             ?? transform.Find("StreakText")
                             ?? transform.Find("Streak_Text");
                if (stt != null) streakText = stt.GetComponent<TextMeshProUGUI>();
            }

            if (replayAudioBtn == null)
            {
                Transform r = transform.Find("ReplayAudioBtn") 
                           ?? transform.Find("ReplayButton") 
                           ?? transform.Find("ReplayAudioButton")
                           ?? transform.Find("Header_Container/ReplayBtn") 
                           ?? transform.Find("HeaderRibbon/ReplayAudioBtn")
                           ?? transform.Find("Speaker_Button")
                           ?? transform.Find("SpeakerButton");
                if (r != null) replayAudioBtn = r.GetComponent<Button>();
                if (replayAudioBtn == null)
                {
                    var allBtns = GetComponentsInChildren<Button>(true);
                    foreach (var b in allBtns)
                    {
                        string bName = b.gameObject.name.ToLower();
                        if (bName.Contains("speaker") || bName.Contains("replay") || bName.Contains("audio"))
                        {
                            replayAudioBtn = b;
                            break;
                        }
                    }
                }
            }
            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentWordAudio);
            }

            if (tapMiddleLeftBtn != null)
            {
                tapMiddleLeftBtn.onClick.RemoveAllListeners();
                tapMiddleLeftBtn.onClick.AddListener(() => OnBinButtonClicked(DiphthongPositionRule.Middle));
            }
            if (tapEndRightBtn != null)
            {
                tapEndRightBtn.onClick.RemoveAllListeners();
                tapEndRightBtn.onClick.AddListener(() => OnBinButtonClicked(DiphthongPositionRule.End));
            }
        }

        public void ReplayCurrentWordAudio()
        {
            var items = GetCurrentRoundItems();
            if (currentIndex >= 0 && currentIndex < items.Count)
            {
                PlayWordAudio(items[currentIndex].word);
            }
        }

        public void StartActivity()
        {
            currentRound = 1;
            currentIndex = 0;
            totalScore = 0;
            currentStreak = 0;
            isProcessing = false;
            cardInitialPos = Vector2.zero;

            if (cardContainer != null)
            {
                cardContainer.anchoredPosition = Vector2.zero;
                cardContainer.localPosition = Vector3.zero;
                cardContainer.localScale = Vector3.one;
                cardContainer.localRotation = Quaternion.identity;
            }

            UpdateRoundVisuals();
            UpdateHUD();
            LoadCurrentCard();

            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip intro = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("U07_VO_act5_intro") ??
                                  U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Glide Families. Swipe left for Middle, right for End!");
                if (intro != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(intro);
            }
        }

        private List<GlideSwipeItem> GetCurrentRoundItems()
        {
            return (currentRound == 1) ? roundAItems : roundBItems;
        }

        private void UpdateRoundVisuals()
        {
            if (currentRound == 1)
            {
                if (ruleBannerText != null)
                    ruleBannerText.text = "<b>Rule:</b> <color=#00E5FF>oi</color> sits in the <b>MIDDLE</b>  |  <color=#FFD700>oy</color> sits at the <b>END</b>";

                if (middleLeftBinTitle != null)
                    middleLeftBinTitle.text = "<b><size=120%>oi</size></b>\n<color=#64748B><size=80%>MIDDLE</size></color>";
                if (endRightBinTitle != null)
                    endRightBinTitle.text = "<b><size=120%>oy</size></b>\n<color=#64748B><size=80%>END</size></color>";
            }
            else
            {
                if (ruleBannerText != null)
                    ruleBannerText.text = "<b>Rule:</b> <color=#00E5FF>ou</color> sits in the <b>MIDDLE</b>  |  <color=#FFD700>ow</color> sits at the <b>END / before L, N</b>";

                if (middleLeftBinTitle != null)
                    middleLeftBinTitle.text = "<b><size=120%>ou</size></b>\n<color=#64748B><size=80%>MIDDLE</size></color>";
                if (endRightBinTitle != null)
                    endRightBinTitle.text = "<b><size=120%>ow</size></b>\n<color=#64748B><size=80%>END / L, N</size></color>";
            }
        }

        private void LoadCurrentCard()
        {
            var items = GetCurrentRoundItems();
            if (currentIndex >= items.Count)
            {
                if (currentRound == 1)
                {
                    StartCoroutine(TransitionToRoundB());
                }
                else
                {
                    StartCoroutine(HandleActivityCompleted());
                }
                return;
            }

            var item = items[currentIndex];

            if (cardContainer != null)
            {
                cardContainer.anchoredPosition = Vector2.zero;
                cardContainer.localPosition = Vector3.zero;
                cardContainer.localScale = Vector3.one;
                cardContainer.localRotation = Quaternion.identity;
                var img = cardContainer.GetComponent<Image>();
                if (img != null) img.color = defaultCardColor;
                if (cardCanvasGroup != null) cardCanvasGroup.alpha = 1f;
            }

            if (cardWordText != null)
            {
                // Display gapped/highlighted diphthong word
                string displayWord = FormatGappedWord(item.word, item.team);
                cardWordText.text = $"<b>{displayWord}</b>";
                cardWordText.color = new Color(0.12f, 0.16f, 0.22f, 1f);
            }

            if (cardPhoneticHintText != null)
            {
                cardPhoneticHintText.text = $"Diphthong: <b>/{ (currentRound == 1 ? "ɔɪ" : "aʊ") }/</b>";
                cardPhoneticHintText.color = new Color(0.2f, 0.4f, 0.7f, 1f);
            }

            // Play word audio
            PlayWordAudio(item.word);
            UpdateHUD();
            isProcessing = false;
        }

        private string FormatGappedWord(string word, string team)
        {
            if (string.IsNullOrEmpty(word)) return "";
            if (word.Contains(team))
            {
                int idx = word.IndexOf(team);
                string before = word.Substring(0, idx);
                string after = word.Substring(idx + team.Length);
                return $"{before}<color=#0284C7>_ _</color>{after}";
            }
            return word;
        }

        private void PlayWordAudio(string word)
        {
            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip clip = U7_SA_AudioManager_Masters_Phonics.ResolveAudio(word) ??
                                 U7_SA_AudioManager_Masters_Phonics.ResolveAudio($"U07_WB_{word}");
                if (clip != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            }
        }

        // =====================================================================
        // Drag & Swipe Implementation
        // =====================================================================

        private Vector2 dragStartPos;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isProcessing) return;
            dragStartPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isProcessing || cardContainer == null) return;

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(cardContainer, eventData.position, cam, out Vector3 worldPoint))
            {
                cardContainer.position = worldPoint;
            }

            float deltaX = eventData.position.x - dragStartPos.x;
            float rotation = Mathf.Clamp(-deltaX * 0.05f, -15f, 15f);
            cardContainer.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isProcessing || cardContainer == null) return;

            float deltaX = eventData.position.x - dragStartPos.x;
            float threshold = Screen.width * 0.12f;

            if (deltaX < -threshold)
            {
                // Left swipe -> Middle rule
                EvaluateAnswer(DiphthongPositionRule.Middle, true);
            }
            else if (deltaX > threshold)
            {
                // Right swipe -> End rule
                EvaluateAnswer(DiphthongPositionRule.End, false);
            }
            else
            {
                // Snap back
                StartCoroutine(SnapBackCard());
            }
        }

        public void OnBinButtonClicked(DiphthongPositionRule chosenRule)
        {
            if (isProcessing) return;
            EvaluateAnswer(chosenRule, chosenRule == DiphthongPositionRule.Middle);
        }

        private void EvaluateAnswer(DiphthongPositionRule chosenRule, bool isLeft)
        {
            var items = GetCurrentRoundItems();
            if (currentIndex >= items.Count) return;

            isProcessing = true;
            var currentItem = items[currentIndex];

            bool isCorrect = (chosenRule == currentItem.positionRule);

            if (isCorrect)
            {
                currentStreak++;
                totalScore += 10 + (currentStreak > 2 ? 5 : 0);
                PlaySFX(correctSFX, true);

                var cardImg = cardContainer != null ? cardContainer.GetComponent<Image>() : null;
                if (cardImg != null) cardImg.color = correctColor;

                if (cardWordText != null)
                {
                    cardWordText.text = $"<b>{currentItem.word}</b>";
                    cardWordText.color = correctTextColor;
                }

                StartCoroutine(AnimateFlyToBin(isLeft ? middleLeftBin : endRightBin, () =>
                {
                    currentIndex++;
                    LoadCurrentCard();
                }));
            }
            else
            {
                currentStreak = 0;
                PlaySFX(wrongSFX, false);

                var cardImg = cardContainer != null ? cardContainer.GetComponent<Image>() : null;
                if (cardImg != null) cardImg.color = wrongColor;

                StartCoroutine(ShakeCardRoutine(() =>
                {
                    if (cardImg != null) cardImg.color = defaultCardColor;
                    isProcessing = false;
                }));
            }

            UpdateHUD();
        }

        private IEnumerator AnimateFlyToBin(RectTransform targetBin, System.Action onComplete)
        {
            if (cardContainer == null || targetBin == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 startPos = cardContainer.position;
            Vector3 endPos = targetBin.position;
            float duration = 0.28f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = Mathf.SmoothStep(0, 1, t);

                cardContainer.position = Vector3.Lerp(startPos, endPos, smoothT);
                cardContainer.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.3f, smoothT);
                if (cardCanvasGroup != null)
                    cardCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);

                yield return null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator SnapBackCard()
        {
            if (cardContainer == null) yield break;

            Vector2 currentPos = cardContainer.anchoredPosition;
            Quaternion currentRot = cardContainer.localRotation;
            float duration = 0.18f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = Mathf.SmoothStep(0, 1, t);

                cardContainer.anchoredPosition = Vector2.Lerp(currentPos, cardInitialPos, smoothT);
                cardContainer.localPosition = new Vector3(cardContainer.localPosition.x, cardContainer.localPosition.y, 0f);
                cardContainer.localRotation = Quaternion.Lerp(currentRot, Quaternion.identity, smoothT);
                yield return null;
            }

            cardContainer.anchoredPosition = cardInitialPos;
            cardContainer.localPosition = new Vector3(cardInitialPos.x, cardInitialPos.y, 0f);
            cardContainer.localScale = Vector3.one;
            cardContainer.localRotation = Quaternion.identity;
            isProcessing = false;
        }

        private IEnumerator ShakeCardRoutine(System.Action onComplete)
        {
            if (cardContainer == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float duration = 0.35f;
            float elapsed = 0f;
            float magnitude = 24f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float xOffset = Mathf.Sin(elapsed * 40f) * magnitude * (1f - (elapsed / duration));
                cardContainer.anchoredPosition = new Vector2(cardInitialPos.x + xOffset, cardInitialPos.y);
                cardContainer.localPosition = new Vector3(cardContainer.localPosition.x, cardContainer.localPosition.y, 0f);
                yield return null;
            }

            cardContainer.anchoredPosition = cardInitialPos;
            cardContainer.localPosition = new Vector3(cardInitialPos.x, cardInitialPos.y, 0f);
            cardContainer.localRotation = Quaternion.identity;
            onComplete?.Invoke();
        }

        private IEnumerator TransitionToRoundB()
        {
            isProcessing = true;
            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip rBClip = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("U07_VO_act5_round2") ??
                                   U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Great job! Now sort ou in the Middle and ow at the End!");
                if (rBClip != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(rBClip);
            }

            yield return new WaitForSeconds(1.2f);

            currentRound = 2;
            currentIndex = 0;
            UpdateRoundVisuals();
            UpdateHUD();
            LoadCurrentCard();
        }

        private IEnumerator HandleActivityCompleted()
        {
            isProcessing = true;
            int earnedStars = 3;
            int score = totalScore > 0 ? totalScore : 160;

            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip winClip = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("U07_VO_act5_complete") ??
                                    U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Amazing! You've mastered glide spelling positions!");
                if (winClip != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(winClip);
            }

            if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(5, earnedStars);
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(score);
            }

            yield return new WaitForSeconds(1.0f);

            U7_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform, 
                "Activity 5 — Glide Families", 
                earnedStars, 
                score, 
                () => {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity6();
                }
            );
        }

        private void PlaySFX(AudioClip clip, bool isCorrect)
        {
            if (U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                if (clip != null)
                {
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(clip);
                }
                else
                {
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlayAnswerFeedbackSFX(isCorrect);
                }
            }
        }

        private void UpdateHUD()
        {
            int totalItems = roundAItems.Count + roundBItems.Count;
            int completedItems = ((currentRound - 1) * roundAItems.Count) + currentIndex;

            if (progressBar != null)
            {
                progressBar.maxValue = totalItems;
                progressBar.value = completedItems;
                U7_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
            }

            if (progressText != null)
            {
                progressText.text = $"<b>Item {completedItems + 1} of {totalItems}</b>  <color=#94A3B8>(Round {currentRound}/2)</color>";
            }

            if (scoreText != null)
            {
                scoreText.text = $"Score: <b>{totalScore}</b>";
            }

            if (streakText != null)
            {
                streakText.text = currentStreak > 1 ? $"<b>Streak: {currentStreak}x</b>" : "";
            }
        }
    }

    public class U7_GlideFamilies_CardDragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public U7_SA_GM03s_GlideFamilies_Masters_Phonics owner;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner != null) owner.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner != null) owner.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner != null) owner.OnEndDrag(eventData);
        }
    }
}
