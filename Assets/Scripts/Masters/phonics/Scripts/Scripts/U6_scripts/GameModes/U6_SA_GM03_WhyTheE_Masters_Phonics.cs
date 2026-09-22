using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U6_SA_GM03_WhyTheE_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Header & HUD")]
        [SerializeField] private TextMeshProUGUI itemCounterText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioButton;

        [Header("2. Word Card (Draggable)")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private GameObject wordCardPrefab;
        [SerializeField] private TextMeshProUGUI wordCardTMP;
        [SerializeField] private CanvasGroup cardCanvasGroup;

        [Header("3. Chutes / Bins (3 Bins)")]
        [SerializeField] private RectTransform bin1Transform; // Makes Vowel Long (Job 1)
        [SerializeField] private RectTransform bin2Transform; // Holds a Place (Job 3: no i, u, v at end)
        [SerializeField] private RectTransform bin3Transform; // Not a Plural (Job 4: house, mouse, etc.)
        [SerializeField] private TextMeshProUGUI bin1LabelTMP;
        [SerializeField] private TextMeshProUGUI bin2LabelTMP;
        [SerializeField] private TextMeshProUGUI bin3LabelTMP;

        [Header("4. Mascot & Hints")]
        [SerializeField] private GameObject mascotSpeechBubble;
        [SerializeField] private TextMeshProUGUI mascotBubbleTMP;

        [Header("5. Audio SFX Clips (Inspector Overrides)")]
        [Tooltip("Played on correct bin drop")]
        public AudioClip correctSFX;
        [Tooltip("Played on incorrect bin drop")]
        public AudioClip wrongSFX;
        [Tooltip("Played on protected letter highlight")]
        public AudioClip highlightSFX;
        [Tooltip("Played on deck deal")]
        public AudioClip dealCardSFX;

        [Header("6. Optional Feedback Panel")]
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private TextMeshProUGUI feedbackText;

        [Header("7. Word Pool")]
        [SerializeField] private List<WhyTheEItem> sessionPool = new List<WhyTheEItem>();

        // Runtime State
        private int currentItemIndex = 0;
        private int currentStreak = 0;
        private int totalScore = 0;
        private int firstTryCorrectCount = 0;
        private int holdsAPlaceCorrectCount = 0;
        private int consecutiveErrorsOnCurrent = 0;
        private bool isInteracting = false;
        private Vector2 cardOriginalAnchoredPos;
        private WhyTheEItem currentItem;
        private GameObject currentCardInstance;

        private static Sprite proceduralRoundedSprite = null;

        public static Sprite GetOrCreateRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 28;
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
            StartSession();
        }

        public void StartSession()
        {
            StopAllCoroutines();
            currentItemIndex = 0;
            currentStreak = 0;
            firstTryCorrectCount = 0;
            holdsAPlaceCorrectCount = 0;
            consecutiveErrorsOnCurrent = 0;
            isInteracting = false;

            BuildBalancedDeck();
            UpdateHUD();
            LoadCurrentCard();
        }

        public void AutoBindHierarchyElements()
        {
            if (cardContainer == null)
            {
                Transform cc = transform.Find("CardContainer") ?? transform.Find("CardSpawnZone") ?? transform.Find("CardZone");
                if (cc != null) cardContainer = cc.GetComponent<RectTransform>();
            }

            if (bin1Transform == null)
            {
                Transform b1 = transform.Find("Bin1_MakesVowelLong") ?? transform.Find("Bin1") ?? transform.Find("MakesVowelLongBin") ?? transform.Find("Job1Bin");
                if (b1 != null) bin1Transform = b1.GetComponent<RectTransform>();
            }

            if (bin2Transform == null)
            {
                Transform b2 = transform.Find("Bin2_HoldsAPlace") ?? transform.Find("Bin2") ?? transform.Find("HoldsAPlaceBin") ?? transform.Find("Job3Bin");
                if (b2 != null) bin2Transform = b2.GetComponent<RectTransform>();
            }

            if (bin3Transform == null)
            {
                Transform b3 = transform.Find("Bin3_NotAPlural") ?? transform.Find("Bin3") ?? transform.Find("NotAPluralBin") ?? transform.Find("Job4Bin");
                if (b3 != null) bin3Transform = b3.GetComponent<RectTransform>();
            }

            if (bin1LabelTMP == null && bin1Transform != null)
                bin1LabelTMP = bin1Transform.GetComponentInChildren<TextMeshProUGUI>();

            if (bin2LabelTMP == null && bin2Transform != null)
                bin2LabelTMP = bin2Transform.GetComponentInChildren<TextMeshProUGUI>();

            if (bin3LabelTMP == null && bin3Transform != null)
                bin3LabelTMP = bin3Transform.GetComponentInChildren<TextMeshProUGUI>();

            // Setup high-contrast clear white bin labels
            if (bin1LabelTMP != null)
            {
                bin1LabelTMP.text = "<b><size=34><color=#FFFFFF>MAKES VOWEL LONG</color></size></b>\n<size=22><color=#F3F4F6>Job 1 (cake, hide, cube)</color></size>";
                bin1LabelTMP.color = Color.white;
                bin1LabelTMP.alignment = TextAlignmentOptions.Center;
            }
            if (bin2LabelTMP != null)
            {
                bin2LabelTMP.text = "<b><size=34><color=#FFFFFF>HOLDS A PLACE</color></size></b>\n<size=22><color=#F3F4F6>Job 3: no i, u, v at end\n(give, have, blue)</color></size>";
                bin2LabelTMP.color = Color.white;
                bin2LabelTMP.alignment = TextAlignmentOptions.Center;
            }
            if (bin3LabelTMP != null)
            {
                bin3LabelTMP.text = "<b><size=34><color=#FFFFFF>NOT A PLURAL</color></size></b>\n<size=22><color=#F3F4F6>Job 4: not more than one\n(house, mouse, nurse)</color></size>";
                bin3LabelTMP.color = Color.white;
                bin3LabelTMP.alignment = TextAlignmentOptions.Center;
            }

            // Enable optional direct tapping on bins
            SetupBinClick(bin1Transform, MagicEJobType.MakesVowelLong);
            SetupBinClick(bin2Transform, MagicEJobType.HoldsAPlace);
            SetupBinClick(bin3Transform, MagicEJobType.NotAPlural);

            if (replayAudioButton == null)
            {
                Transform rBtn = transform.Find("ReplayAudioButton") ?? transform.Find("ReplayButton") ?? transform.Find("btn_replay");
                if (rBtn != null) replayAudioButton = rBtn.GetComponent<Button>();
            }

            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayCurrentWordAudio);
            }

            if (itemCounterText == null)
            {
                Transform pTxt = transform.Find("ProgressHUD/Progress_Text") 
                              ?? transform.Find("ProgressHUD/ProgressText") 
                              ?? transform.Find("Progress_Text") 
                              ?? transform.Find("ProgressText") 
                              ?? transform.Find("HUD/Progress_Text")
                              ?? transform.Find("HUD/ProgressText")
                              ?? transform.Find("ItemCounterText")
                              ?? transform.Find("Counter_Text");
                if (pTxt != null) itemCounterText = pTxt.GetComponent<TextMeshProUGUI>();

                if (itemCounterText == null)
                {
                    TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var tmp in tmps)
                    {
                        string n = tmp.gameObject.name.ToLower();
                        if ((n.Contains("progress") || n.Contains("counter") || n.Contains("count") || n.Contains("item") || n.Contains("card")) &&
                            !n.Contains("score") && !n.Contains("streak") && !n.Contains("title") && !n.Contains("prompt") && !n.Contains("btn") && !n.Contains("button") && !n.Contains("bin") && !n.Contains("bubble") && !n.Contains("feedback"))
                        {
                            itemCounterText = tmp;
                            break;
                        }
                    }
                }
            }

            if (progressBar == null)
            {
                Transform pb = transform.Find("ProgressHUD/ProgressBar") ?? transform.Find("ProgressBar") ?? transform.Find("HUD/ProgressBar");
                if (pb != null) progressBar = pb.GetComponent<Slider>();
            }
            if (progressBar != null) U6_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);

            if (scoreText == null)
            {
                Transform sTxt = transform.Find("ProgressHUD/Score_Text") ?? transform.Find("Score_Text") ?? transform.Find("HUD/Score_Text");
                if (sTxt != null) scoreText = sTxt.GetComponent<TextMeshProUGUI>();
            }

            if (streakText == null)
            {
                Transform stTxt = transform.Find("ProgressHUD/Streak_Text") ?? transform.Find("Streak_Text") ?? transform.Find("HUD/Streak_Text");
                if (stTxt != null) streakText = stTxt.GetComponent<TextMeshProUGUI>();
            }

            if (mascotSpeechBubble == null)
            {
                Transform sb = transform.Find("MascotSpeechBubble") ?? transform.Find("SpeechBubble") ?? transform.Find("MascotBubble");
                if (sb != null)
                {
                    mascotSpeechBubble = sb.gameObject;
                    mascotBubbleTMP = sb.GetComponentInChildren<TextMeshProUGUI>();
                }
            }
        }

        private void SetupBinClick(RectTransform binRT, MagicEJobType jobType)
        {
            if (binRT == null) return;
            Button btn = binRT.GetComponent<Button>();
            if (btn == null) btn = binRT.gameObject.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (!isInteracting && currentCardInstance != null)
                {
                    bool isCorrect = (jobType == currentItem.jobType);
                    StartCoroutine(HandleCardResolution(jobType, isCorrect, binRT));
                }
            });
        }

        private void BuildBalancedDeck()
        {
            var defaultPool = U6_SA_DataTypes_Masters_Phonics.GetDefaultWhyTheEItems();
            List<WhyTheEItem> job1 = new List<WhyTheEItem>();
            List<WhyTheEItem> job3 = new List<WhyTheEItem>();
            List<WhyTheEItem> job4 = new List<WhyTheEItem>();

            foreach (var item in defaultPool)
            {
                if (item.jobType == MagicEJobType.MakesVowelLong) job1.Add(item);
                else if (item.jobType == MagicEJobType.HoldsAPlace) job3.Add(item);
                else if (item.jobType == MagicEJobType.NotAPlural) job4.Add(item);
            }

            ShuffleList(job1);
            ShuffleList(job3);
            ShuffleList(job4);

            sessionPool = new List<WhyTheEItem>();
            int countPerBin = 6;
            for (int i = 0; i < countPerBin; i++)
            {
                if (i < job1.Count) sessionPool.Add(job1[i]);
                if (i < job3.Count) sessionPool.Add(job3[i]);
                if (i < job4.Count) sessionPool.Add(job4[i]);
            }

            ShuffleList(sessionPool);
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int r = UnityEngine.Random.Range(0, i + 1);
                T tmp = list[i];
                list[i] = list[r];
                list[r] = tmp;
            }
        }

        private void LoadCurrentCard()
        {
            if (currentItemIndex >= sessionPool.Count)
            {
                FinishActivity();
                return;
            }

            currentItem = sessionPool[currentItemIndex];
            consecutiveErrorsOnCurrent = 0;
            isInteracting = false;

            if (mascotSpeechBubble != null) mascotSpeechBubble.SetActive(false);

            if (currentCardInstance != null) Destroy(currentCardInstance);

            // Create new draggable card
            currentCardInstance = new GameObject("DraggableWordCard", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            currentCardInstance.transform.SetParent(cardContainer != null ? cardContainer : transform, false);

            RectTransform cardRT = currentCardInstance.GetComponent<RectTransform>();
            cardRT.sizeDelta = new Vector2(480f, 220f);
            cardRT.anchoredPosition = Vector2.zero;
            cardOriginalAnchoredPos = Vector2.zero;

            Image cardImg = currentCardInstance.GetComponent<Image>();
            cardImg.sprite = GetOrCreateRoundedSprite();
            cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.96f, 0.97f, 1f, 1f);

            cardCanvasGroup = currentCardInstance.GetComponent<CanvasGroup>();

            // Add text child
            GameObject textObj = new GameObject("WordText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(currentCardInstance.transform, false);
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(20, 20);
            textRT.offsetMax = new Vector2(-20, -20);

            wordCardTMP = textObj.GetComponent<TextMeshProUGUI>();
            wordCardTMP.text = $"<b>{currentItem.word}</b>";
            wordCardTMP.fontSize = 64;
            wordCardTMP.fontStyle = FontStyles.Bold;
            wordCardTMP.alignment = TextAlignmentOptions.Center;
            wordCardTMP.color = new Color(0.12f, 0.16f, 0.28f, 1f);
            wordCardTMP.raycastTarget = false;

            // Attach drag handler
            var dragHandler = currentCardInstance.AddComponent<WhyTheECardDragHandler>();
            dragHandler.Initialize(this, cardRT, cardCanvasGroup);

            // Animate card entrance deal
            StartCoroutine(AnimateCardDealRoutine(cardRT, cardCanvasGroup));

            // Deal SFX & Voice Audio
            PlayDealAudio();
            PlayCurrentWordAudio();
            UpdateHUD();
        }

        private IEnumerator AnimateCardDealRoutine(RectTransform cardRT, CanvasGroup cg)
        {
            if (cardRT == null) yield break;

            if (cg != null)
            {
                cg.alpha = 0f;
                cg.blocksRaycasts = false;
            }

            Vector2 spawnPos = cardOriginalAnchoredPos + new Vector2(0f, 140f);
            cardRT.anchoredPosition = spawnPos;
            cardRT.localScale = new Vector3(0.85f, 0.85f, 1f);

            float duration = 0.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3);

                cardRT.anchoredPosition = Vector2.Lerp(spawnPos, cardOriginalAnchoredPos, ease);
                cardRT.localScale = Vector3.Lerp(new Vector3(0.85f, 0.85f, 1f), Vector3.one, ease);

                if (cg != null) cg.alpha = Mathf.Clamp01(t * 1.5f);
                yield return null;
            }

            cardRT.anchoredPosition = cardOriginalAnchoredPos;
            cardRT.localScale = Vector3.one;

            if (cg != null)
            {
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
            }
        }

        private void PlayDealAudio()
        {
            if (dealCardSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(dealCardSFX);
            else
                U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("card_deal");
        }

        public void PlayCurrentWordAudio()
        {
            if (currentItem == null) return;
            AudioClip clip = currentItem.wordAudio != null ? currentItem.wordAudio : U6_SA_AudioManager_Masters_Phonics.ResolveAudio(currentItem.word);
            if (clip != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            }
            else
            {
                U6_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(currentItem.word);
            }
        }

        public void ReplayCurrentWordAudio()
        {
            if (replayAudioButton != null)
                StartCoroutine(PunchScale(replayAudioButton.transform, 1.15f, 0.15f));
            PlayCurrentWordAudio();
        }

        public void OnCardDropped(Vector2 dropScreenPos)
        {
            if (isInteracting || currentCardInstance == null) return;

            RectTransform cardRT = currentCardInstance.GetComponent<RectTransform>();
            RectTransform matchedBin = null;
            MagicEJobType targetJob = EvaluateDropZone(dropScreenPos, cardRT, out matchedBin);

            if (targetJob == 0)
            {
                // Dropped outside all bins -> snap back
                StartCoroutine(SnapBackRoutine(cardRT));
                return;
            }

            bool isCorrect = (targetJob == currentItem.jobType);
            StartCoroutine(HandleCardResolution(targetJob, isCorrect, matchedBin));
        }

        private MagicEJobType EvaluateDropZone(Vector2 pointerScreenPos, RectTransform cardRT, out RectTransform matchedBin)
        {
            matchedBin = null;
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Camera cam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera) ? parentCanvas.worldCamera : null;

            Vector2 cardCenterScreenPos = cardRT != null ? (Vector2)RectTransformUtility.WorldToScreenPoint(cam, cardRT.position) : pointerScreenPos;

            // 1. Check Bin 1
            if (CheckBinOverlap(bin1Transform, pointerScreenPos, cardCenterScreenPos, cam))
            {
                matchedBin = bin1Transform;
                return MagicEJobType.MakesVowelLong;
            }

            // 2. Check Bin 2
            if (CheckBinOverlap(bin2Transform, pointerScreenPos, cardCenterScreenPos, cam))
            {
                matchedBin = bin2Transform;
                return MagicEJobType.HoldsAPlace;
            }

            // 3. Check Bin 3
            if (CheckBinOverlap(bin3Transform, pointerScreenPos, cardCenterScreenPos, cam))
            {
                matchedBin = bin3Transform;
                return MagicEJobType.NotAPlural;
            }

            return 0; // None
        }

        private bool CheckBinOverlap(RectTransform binRT, Vector2 pointerPos, Vector2 cardPos, Camera cam)
        {
            if (binRT == null) return false;

            // Direct pointer inside bin
            if (RectTransformUtility.RectangleContainsScreenPoint(binRT, pointerPos, cam))
                return true;

            // Card center inside bin
            if (RectTransformUtility.RectangleContainsScreenPoint(binRT, cardPos, cam))
                return true;

            // Distance check in screen space (generous threshold of 240 pixels)
            Vector2 binScreenPos = RectTransformUtility.WorldToScreenPoint(cam, binRT.position);
            if (Vector2.Distance(pointerPos, binScreenPos) < 240f || Vector2.Distance(cardPos, binScreenPos) < 240f)
                return true;

            return false;
        }

        private IEnumerator HandleCardResolution(MagicEJobType droppedJob, bool isCorrect, RectTransform targetBin)
        {
            isInteracting = true;
            RectTransform cardRT = currentCardInstance != null ? currentCardInstance.GetComponent<RectTransform>() : null;
            Image cardImg = currentCardInstance != null ? currentCardInstance.GetComponent<Image>() : null;

            if (cardCanvasGroup != null)
                cardCanvasGroup.blocksRaycasts = false;

            if (isCorrect)
            {
                if (consecutiveErrorsOnCurrent == 0)
                {
                    firstTryCorrectCount++;
                    if (currentItem.jobType == MagicEJobType.HoldsAPlace)
                    {
                        holdsAPlaceCorrectCount++;
                    }
                }

                currentStreak++;
                int points = 35 + (currentStreak >= 3 ? 15 : 0);
                totalScore += points;
                if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                {
                    U6_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(points);
                    U6_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSilentEExplained();
                }

                if (cardImg != null) cardImg.color = new Color(0.2f, 0.82f, 0.45f, 1f);
                if (wordCardTMP != null) wordCardTMP.color = Color.white;

                // Highlight protected letter if Job 3
                if (currentItem.jobType == MagicEJobType.HoldsAPlace && !string.IsNullOrEmpty(currentItem.clueLetter))
                {
                    HighlightProtectedLetter(currentItem.word, currentItem.clueLetter);
                }

                if (correctSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                    U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(correctSFX);
                else
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

                ShowMascotHint(currentItem.explanation, false);

                // Smoothly suck/drop card into the target bin
                if (cardRT != null && targetBin != null)
                {
                    Vector3 startPos = cardRT.position;
                    Vector3 targetPos = targetBin.position;
                    float dropDuration = 0.22f;
                    float elapsed = 0f;

                    while (elapsed < dropDuration)
                    {
                        elapsed += Time.deltaTime;
                        float t = Mathf.Clamp01(elapsed / dropDuration);
                        float ease = t * t;

                        cardRT.position = Vector3.Lerp(startPos, targetPos, ease);
                        cardRT.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.5f, 0.5f, 1f), ease);
                        if (cardCanvasGroup != null) cardCanvasGroup.alpha = Mathf.Clamp01(1f - t);
                        yield return null;
                    }
                }
                else if (cardRT != null)
                {
                    StartCoroutine(PunchScale(cardRT, 1.12f, 0.2f));
                }

                yield return new WaitForSeconds(1.4f);

                currentItemIndex++;
                LoadCurrentCard();
            }
            else
            {
                currentStreak = 0;
                consecutiveErrorsOnCurrent++;

                if (cardImg != null) cardImg.color = new Color(0.92f, 0.3f, 0.3f, 1f);
                if (wordCardTMP != null) wordCardTMP.color = Color.white;

                if (wrongSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                    U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(wrongSFX);
                else
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");

                if (consecutiveErrorsOnCurrent >= 2)
                {
                    ShowMascotHint("Listen to the vowel. Is it long or short? If it stays short, the 'e' is just holding a place!", true);
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlayVoicePrompt("U06_VO_a4_hint", "Listen to the vowel. Is it long or short?");
                }
                else
                {
                    ShowMascotHint("Not quite! Think about what job the silent 'e' is doing here.", false);
                }

                yield return new WaitForSeconds(1.2f);

                if (cardImg != null) cardImg.color = new Color(0.96f, 0.97f, 1f, 1f);
                if (wordCardTMP != null)
                {
                    wordCardTMP.text = $"<b>{currentItem.word}</b>";
                    wordCardTMP.color = new Color(0.12f, 0.16f, 0.28f, 1f);
                }

                yield return StartCoroutine(SnapBackRoutine(cardRT));

                if (cardCanvasGroup != null)
                    cardCanvasGroup.blocksRaycasts = true;

                isInteracting = false;
            }

            UpdateHUD();
        }

        private void HighlightProtectedLetter(string word, string clueLetter)
        {
            if (wordCardTMP == null) return;

            int letterIdx = word.LastIndexOf(clueLetter, StringComparison.OrdinalIgnoreCase);
            if (letterIdx >= 0 && letterIdx < word.Length)
            {
                string before = word.Substring(0, letterIdx);
                string highlight = $"<color=#FFE57F><u>{word[letterIdx]}</u></color>";
                string after = word.Substring(letterIdx + 1);
                wordCardTMP.text = $"<b>{before}{highlight}{after}</b>";
            }
        }

        private void ShowMascotHint(string message, bool isUrgent)
        {
            if (mascotSpeechBubble != null)
            {
                mascotSpeechBubble.SetActive(true);
                if (mascotBubbleTMP != null)
                {
                    mascotBubbleTMP.text = $"<b>{(isUrgent ? "<color=#E65100>Tip:</color> " : "")}{message}</b>";
                    mascotBubbleTMP.fontSize = 26;
                }
            }
        }

        private IEnumerator SnapBackRoutine(RectTransform cardRT)
        {
            if (cardRT == null) yield break;
            Vector2 start = cardRT.anchoredPosition;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                if (cardRT == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3);
                cardRT.anchoredPosition = Vector2.Lerp(start, cardOriginalAnchoredPos, ease);
                yield return null;
            }

            if (cardRT != null) cardRT.anchoredPosition = cardOriginalAnchoredPos;
        }

        private void UpdateHUD()
        {
            if (itemCounterText != null)
            {
                itemCounterText.text = $"<b>Card {Mathf.Min(currentItemIndex + 1, sessionPool.Count)} of {sessionPool.Count}</b>";
                itemCounterText.color = Color.white;
            }

            if (streakText != null)
                streakText.text = currentStreak > 1 ? $"Streak: <b>{currentStreak}x</b>" : "";

            if (progressBar != null && sessionPool.Count > 0)
                progressBar.value = (float)currentItemIndex / sessionPool.Count;
        }

        private void FinishActivity()
        {
            if (progressBar != null) progressBar.value = 1f;

            int stars = 1;
            if (firstTryCorrectCount >= 16 && holdsAPlaceCorrectCount >= 5) stars = 3;
            else if (firstTryCorrectCount >= 13) stars = 2;

            if (feedbackPanel != null)
            {
                feedbackPanel.SetActive(true);
                if (feedbackText != null)
                    feedbackText.text = $"Activity Complete!\nStars: {stars} / 3\nCorrect First Try: {firstTryCorrectCount}/{sessionPool.Count}";
            }

            U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("activity_complete");

            if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U6_SA_UnitFlowManager_Masters_Phonics.Instance.CompleteCurrentActivity();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign All Items, Sprites & Audio")]
        public void EditorAutoAssignEverything()
        {
            AutoBindHierarchyElements();

            sessionPool = U6_SA_DataTypes_Masters_Phonics.GetDefaultWhyTheEItems();

            for (int i = 0; i < sessionPool.Count; i++)
            {
                var item = sessionPool[i];
                if (item == null) continue;
                item.wordAudio = FindAudioInEditor(item.word);
            }

            if (dealCardSFX == null) dealCardSFX = FindAudioInEditor("card_deal");
            if (correctSFX == null) correctSFX = FindAudioInEditor("correct");
            if (wrongSFX == null) wrongSFX = FindAudioInEditor("wrong");

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log($"<color=#10B981><b>[Why The E] Auto-assigned {sessionPool.Count} items & Audio Clips!</b></color>");
        }

        private AudioClip FindAudioInEditor(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;
            string clean = word.ToLower().Trim();

            if (clean == "correct")
            {
                AudioClip c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3");
                if (c != null) return c;
            }
            else if (clean == "wrong" || clean == "incorrect")
            {
                AudioClip c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3");
                if (c != null) return c;
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new string[] { "Assets/Audio/U6_audio", "Assets/Audio", "Assets/SFX", "Assets/Resources" });
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (filename == clean || filename == $"u06_wrd_{clean}" || filename == $"u06_sfx_{clean}")
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }

            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (filename.Contains(clean))
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            return null;
        }
#endif

        private IEnumerator PunchScale(Transform target, float scale, float duration)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            target.localScale = original * scale;
            yield return new WaitForSeconds(duration);
            if (target != null) target.localScale = original;
        }
    }

    // Helper Drag Handler for GM-03 Draggable Card
    public class WhyTheECardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private U6_SA_GM03_WhyTheE_Masters_Phonics parentActivity;
        private RectTransform cardRT;
        private CanvasGroup canvasGroup;
        private Canvas parentCanvas;

        public void Initialize(U6_SA_GM03_WhyTheE_Masters_Phonics parent, RectTransform rt, CanvasGroup cg)
        {
            parentActivity = parent;
            cardRT = rt;
            canvasGroup = cg;
            parentCanvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.85f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (cardRT != null && parentCanvas != null)
            {
                float scale = parentCanvas.scaleFactor > 0.001f ? parentCanvas.scaleFactor : 1f;
                cardRT.anchoredPosition += eventData.delta / scale;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            if (parentActivity != null)
            {
                parentActivity.OnCardDropped(eventData.position);
            }
        }
    }
}
