using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U3_SA_GM03_SyllableSort_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Voice A Audio (Official Unit 3 Doc)")]
        [Tooltip("U03_VO_a3_intro: 'Four bins, four names. Tap the word to hear it first if you need to — that’s not cheating, that’s the method.' (8s)")]
        public AudioClip introInstructionClip;

        [Tooltip("Celebration fanfare clip when activity 3 completes")]
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Draggable Word Card (Center Deck)")]
        [SerializeField] private GameObject draggableCardPrefab;
        [SerializeField] private Transform cardSpawnAnchor;
        [SerializeField] private TextMeshProUGUI deckRemainingTMP;

        [Header("4. 4 Syllable Bins")]
        [SerializeField] private RectTransform binMono1; // Monosyllabic (1 beat)
        [SerializeField] private RectTransform binDi2;   // Disyllabic (2 beats)
        [SerializeField] private RectTransform binTri3;  // Trisyllabic (3 beats)
        [SerializeField] private RectTransform binPoly4; // Polysyllabic (4+ beats)

        private List<SyllableWordItem> deck;
        private int currentScore = 0;
        private int totalDeckSize = 16;
        private GameObject currentActiveCard;
        private Canvas parentCanvas;
        private Camera eventCamera;
        private bool isProcessingAnswer = false;

        private void Awake()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            AutoBindHierarchyElements();
            InitializeDeck();
        }

        private void Start()
        {
            PlayIntroInstruction();
        }

        private void OnEnable()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            AutoBindHierarchyElements();
            currentScore = 0;
            UpdateScoreUI();
            InitializeDeck();
            SpawnNextCard();

            PlayIntroInstruction();
        }

        private void PlayIntroInstruction()
        {
            if (introInstructionClip != null)
            {
                StartCoroutine(DelayedPlayIntro());
            }
        }

        private IEnumerator DelayedPlayIntro()
        {
            yield return new WaitForSeconds(0.15f);
            if (U3_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
            }
        }

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("Title BG/TitleText") ?? transform.Find("Title BG") ?? transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (promptTMP == null)
            {
                var t = transform.Find("Prompt_Text") ?? transform.Find("prompt bg/Prompt_Text") ?? transform.Find("PromptText") ?? transform.Find("InstructionText");
                if (t != null) promptTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (scoreTMP == null)
            {
                var t = transform.Find("ScoreText") ?? transform.Find("Score_Text") ?? transform.Find("Score");
                if (t != null) scoreTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("ProgressBar (1)") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            if (cardSpawnAnchor == null)
            {
                Transform t = transform.Find("CardSpawnAnchor") ?? transform.Find("Deck_Anchor") ?? transform.Find("Center_Anchor");
                if (t != null) cardSpawnAnchor = t;
            }

            if (deckRemainingTMP == null)
            {
                Transform t = transform.Find("DeckRemaining_Text") ?? transform.Find("RemainingText");
                if (t != null) deckRemainingTMP = t.GetComponent<TextMeshProUGUI>();
            }

            // 4 Bins
            Transform binsGroup = transform.Find("Bins_Row") ?? transform.Find("Sort_Bins") ?? transform;
            if (binMono1 == null) binMono1 = FindBin(binsGroup, "mono", "bin1", "1");
            if (binDi2 == null) binDi2 = FindBin(binsGroup, "di", "bin2", "2");
            if (binTri3 == null) binTri3 = FindBin(binsGroup, "tri", "bin3", "3");
            if (binPoly4 == null) binPoly4 = FindBin(binsGroup, "poly", "bin4", "4");
        }

        private RectTransform FindBin(Transform parent, params string[] keywords)
        {
            RectTransform[] all = parent.GetComponentsInChildren<RectTransform>(true);
            foreach (var r in all)
            {
                string n = r.name.ToLower();
                foreach (var kw in keywords)
                {
                    if (n.Contains(kw)) return r;
                }
            }
            return null;
        }

        private void InitializeDeck()
        {
            deck = new List<SyllableWordItem>
            {
                // Monosyllabic (4 items)
                new SyllableWordItem { word = "chant", syllableCount = 1, hyphenatedSplit = "chant", category = SyllableCategory.Monosyllabic },
                new SyllableWordItem { word = "draft", syllableCount = 1, hyphenatedSplit = "draft", category = SyllableCategory.Monosyllabic },
                new SyllableWordItem { word = "quack", syllableCount = 1, hyphenatedSplit = "quack", category = SyllableCategory.Monosyllabic },
                new SyllableWordItem { word = "land", syllableCount = 1, hyphenatedSplit = "land", category = SyllableCategory.Monosyllabic },

                // Disyllabic (4 items)
                new SyllableWordItem { word = "pencil", syllableCount = 2, hyphenatedSplit = "pen-cil", category = SyllableCategory.Disyllabic },
                new SyllableWordItem { word = "rabbit", syllableCount = 2, hyphenatedSplit = "rab-bit", category = SyllableCategory.Disyllabic },
                new SyllableWordItem { word = "window", syllableCount = 2, hyphenatedSplit = "win-dow", category = SyllableCategory.Disyllabic },
                new SyllableWordItem { word = "candle", syllableCount = 2, hyphenatedSplit = "can-dle", category = SyllableCategory.Disyllabic },

                // Trisyllabic (4 items)
                new SyllableWordItem { word = "carnival", syllableCount = 3, hyphenatedSplit = "car-ni-val", category = SyllableCategory.Trisyllabic },
                new SyllableWordItem { word = "carpenter", syllableCount = 3, hyphenatedSplit = "car-pen-ter", category = SyllableCategory.Trisyllabic },
                new SyllableWordItem { word = "harmony", syllableCount = 3, hyphenatedSplit = "har-mo-ny", category = SyllableCategory.Trisyllabic },
                new SyllableWordItem { word = "pharmacy", syllableCount = 3, hyphenatedSplit = "phar-ma-cy", category = SyllableCategory.Trisyllabic },

                // Polysyllabic (4 items)
                new SyllableWordItem { word = "comedian", syllableCount = 4, hyphenatedSplit = "co-me-di-an", category = SyllableCategory.Polysyllabic },
                new SyllableWordItem { word = "convenient", syllableCount = 4, hyphenatedSplit = "con-ve-ni-ent", category = SyllableCategory.Polysyllabic },
                new SyllableWordItem { word = "material", syllableCount = 4, hyphenatedSplit = "ma-te-ri-al", category = SyllableCategory.Polysyllabic },
                new SyllableWordItem { word = "thermometer", syllableCount = 4, hyphenatedSplit = "ther-mom-e-ter", category = SyllableCategory.Polysyllabic }
            };

            totalDeckSize = deck.Count;

            // Shuffle
            for (int i = 0; i < deck.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, deck.Count);
                var temp = deck[i];
                deck[i] = deck[rnd];
                deck[rnd] = temp;
            }
        }

        private Sprite generatedSortCardSprite;

        private Sprite GetRoundedSortCardSprite()
        {
            if (generatedSortCardSprite != null) return generatedSortCardSprite;

            int w = 360;
            int h = 130;
            int r = 28;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color faceColor = new Color(0.98f, 0.99f, 1f, 1f);
            Color borderColor = new Color(0.7f, 0.8f, 0.92f, 1f);
            Color shadowColor = new Color(0.3f, 0.4f, 0.55f, 0.5f);

            Color[] colors = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = 0;
                    float dy = 0;

                    if (x < r) dx = r - x;
                    else if (x >= w - r) dx = x - (w - r - 1);

                    if (y < r) dy = r - y;
                    else if (y >= h - r) dy = y - (h - r - 1);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > r)
                    {
                        colors[y * w + x] = Color.clear;
                    }
                    else if (dist > r - 1.5f)
                    {
                        float alpha = Mathf.Clamp01(r - dist);
                        colors[y * w + x] = new Color(shadowColor.r, shadowColor.g, shadowColor.b, alpha * shadowColor.a);
                    }
                    else if (dist > r - 4f || y < 5 || y > h - 5 || x < 5 || x > w - 5)
                    {
                        colors[y * w + x] = borderColor;
                    }
                    else
                    {
                        colors[y * w + x] = faceColor;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            generatedSortCardSprite = Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                100f
            );

            return generatedSortCardSprite;
        }

        private void SpawnNextCard()
        {
            if (deck.Count == 0)
            {
                OnSortCompleted();
                return;
            }

            if (currentActiveCard != null) Destroy(currentActiveCard);

            SyllableWordItem item = deck[0];
            deck.RemoveAt(0);

            if (titleTMP != null) titleTMP.text = "<b>SYLLABLE SORT</b>";
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>Drag each word to its syllable category bin!</b></color>";

            if (deckRemainingTMP != null)
            {
                deckRemainingTMP.text = $"Deck: <b>{deck.Count} left</b>";
            }

            if (progressBar != null)
            {
                progressBar.value = (float)(totalDeckSize - deck.Count) / totalDeckSize;
            }

            // Create card
            Transform parent = cardSpawnAnchor != null ? cardSpawnAnchor : transform;
            currentActiveCard = new GameObject("SortCard_" + item.word, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            currentActiveCard.transform.SetParent(parent, false);
            RectTransform rt = currentActiveCard.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360, 130);
            rt.anchoredPosition = Vector2.zero;

            Image img = currentActiveCard.GetComponent<Image>();
            img.sprite = GetRoundedSortCardSprite();
            img.type = Image.Type.Simple;
            img.color = Color.white;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(currentActiveCard.transform, false);
            RectTransform textRT = txtObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b><color=#000000>{item.word}</color></b>";
            tmp.fontSize = 44;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false; // Prevents child text from blocking clicks on the card

            // Hook Drag & Click Logic
            var dragHandler = currentActiveCard.AddComponent<SyllableSortDragHandler>();
            dragHandler.Initialize(item, this, parentCanvas, binMono1, binDi2, binTri3, binPoly4);

            // Audio Playback
            PlayWordAudio(item.word);
        }

        public void PlayWordAudio(string word)
        {
            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip clip = Resources.Load<AudioClip>($"U3_audio/{word}") 
                              ?? Resources.Load<AudioClip>($"Audio/U3_audio/{word}")
                              ?? Resources.Load<AudioClip>($"U3_audio/U03_WRD_{word}")
                              ?? Resources.Load<AudioClip>($"Audio/U3_audio/U03_WRD_{word}");
                if (clip != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
                }
            }
        }

        public void OnCardDroppedInBin(SyllableWordItem item, SyllableCategory chosenCategory, GameObject cardObj, Transform targetBin)
        {
            if (isProcessingAnswer) return;

            bool isCorrect = (item.category == chosenCategory);

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectDrop(item, cardObj, targetBin));
            }
            else
            {
                StartCoroutine(HandleIncorrectDrop(item, cardObj, targetBin));
            }
        }

        private IEnumerator HandleCorrectDrop(SyllableWordItem item, GameObject cardObj, Transform targetBin)
        {
            isProcessingAnswer = true;
            currentScore += 100;
            UpdateScoreUI();

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFFFFF><b>Correct! \"{item.word}\" is {item.category} ({item.hyphenatedSplit})!</b></color>";
            }

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();

                AudioClip sepClip = item.separatedAudio ?? Resources.Load<AudioClip>($"U3_audio/sep_{item.word}") ?? Resources.Load<AudioClip>($"Audio/U3_audio/sep_{item.word}");
                if (sepClip != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(sepClip);
                }
            }

            // 1. Bin bounce effect
            if (targetBin != null)
            {
                StartCoroutine(PunchScale(targetBin, 1.18f));
            }

            // 2. Card Funnel-Drop Animation (Sucks into the bin opening while shrinking and fading)
            if (cardObj != null)
            {
                RectTransform cardRT = cardObj.GetComponent<RectTransform>();
                CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
                if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();

                Vector3 startPos = cardRT.position;
                Vector3 targetPos = targetBin != null ? targetBin.position + new Vector3(0, -30f, 0) : startPos;

                float elapsed = 0f;
                float duration = 0.38f;

                while (elapsed < duration)
                {
                    if (cardRT == null) break;
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float smoothT = t * t * (3f - 2f * t);

                    // Arc curve into the bin mouth
                    Vector3 currentPos = Vector3.Lerp(startPos, targetPos, smoothT);
                    currentPos.y += Mathf.Sin(t * Mathf.PI) * 40f; // Soft arc hop into chute
                    cardRT.position = currentPos;

                    cardRT.localScale = Vector3.Lerp(Vector3.one * 1.08f, Vector3.zero, smoothT);
                    cardRT.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 15f, smoothT));
                    if (cg != null) cg.alpha = 1f - (smoothT * smoothT);

                    yield return null;
                }

                Destroy(cardObj);
            }

            yield return new WaitForSeconds(0.4f);
            isProcessingAnswer = false;
            SpawnNextCard();
        }

        private IEnumerator HandleIncorrectDrop(SyllableWordItem item, GameObject cardObj, Transform targetBin)
        {
            isProcessingAnswer = true;

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFD54F><b>Try again! \"{item.word}\" has {item.syllableCount} beat{(item.syllableCount > 1 ? "s" : "")}.</b></color>";
            }

            // Shake the incorrect bin
            if (targetBin != null)
            {
                Vector3 origPos = targetBin.localPosition;
                for (int i = 0; i < 6; i++)
                {
                    targetBin.localPosition = origPos + new Vector3((i % 2 == 0 ? 14 : -14), 0, 0);
                    yield return new WaitForSeconds(0.035f);
                }
                targetBin.localPosition = origPos;
            }

            // Snap card back to spawn anchor with smooth spring bounce
            if (cardObj != null)
            {
                RectTransform cardRT = cardObj.GetComponent<RectTransform>();
                Vector3 startPos = cardRT.anchoredPosition;
                float elapsed = 0f;
                float duration = 0.22f;

                while (elapsed < duration)
                {
                    if (cardRT == null) break;
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    cardRT.anchoredPosition = Vector3.Lerp(startPos, Vector2.zero, t * t);
                    yield return null;
                }
                if (cardRT != null) cardRT.anchoredPosition = Vector2.zero;
            }

            yield return new WaitForSeconds(0.15f);
            isProcessingAnswer = false;
        }

        private IEnumerator PunchScale(Transform target, float targetScale)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                target.localScale = Vector3.Lerp(original * targetScale, original, t);
                yield return null;
            }
            if (target != null) target.localScale = original;
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
        }

        private void OnSortCompleted()
        {
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>All 16 Words Sorted Successfully!</b></color>";
            if (progressBar != null) progressBar.value = 1f;

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null && celebrationClip != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(celebrationClip);
            }

            if (U3_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U3_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }
        }
    }

    public class SyllableSortDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private SyllableWordItem item;
        private U3_SA_GM03_SyllableSort_Masters_Phonics manager;
        private Canvas parentCanvas;
        private RectTransform rt;
        private Vector3 startPos;
        private RectTransform bin1, bin2, bin3, bin4;

        public void Initialize(SyllableWordItem item, U3_SA_GM03_SyllableSort_Masters_Phonics manager, Canvas canvas, RectTransform b1, RectTransform b2, RectTransform b3, RectTransform b4)
        {
            this.item = item;
            this.manager = manager;
            this.parentCanvas = canvas;
            this.bin1 = b1;
            this.bin2 = b2;
            this.bin3 = b3;
            this.bin4 = b4;
            this.rt = GetComponent<RectTransform>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (manager != null && item != null)
            {
                manager.PlayWordAudio(item.word);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            startPos = rt.anchoredPosition;
            transform.SetAsLastSibling();
            transform.localScale = Vector3.one * 1.08f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, cam, out Vector3 worldPoint))
            {
                rt.position = worldPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            transform.localScale = Vector3.one;

            RectTransform[] bins = new RectTransform[] { bin1, bin2, bin3, bin4 };
            SyllableCategory[] categories = new SyllableCategory[] {
                SyllableCategory.Monosyllabic,
                SyllableCategory.Disyllabic,
                SyllableCategory.Trisyllabic,
                SyllableCategory.Polysyllabic
            };

            RectTransform closestBin = null;
            SyllableCategory chosenCategory = SyllableCategory.Monosyllabic;
            float minDistance = float.MaxValue;
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

            for (int i = 0; i < bins.Length; i++)
            {
                RectTransform b = bins[i];
                if (b == null) continue;

                float dist = Vector3.Distance(rt.position, b.position);
                bool contains = RectTransformUtility.RectangleContainsScreenPoint(b, eventData.position, cam);

                if (contains || dist < 320f)
                {
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestBin = b;
                        chosenCategory = categories[i];
                    }
                }
            }

            if (closestBin != null)
            {
                manager.OnCardDroppedInBin(item, chosenCategory, gameObject, closestBin);
            }
            else
            {
                StartCoroutine(SnapBackToCenter());
            }
        }

        private IEnumerator SnapBackToCenter()
        {
            Vector3 from = rt.anchoredPosition;
            float elapsed = 0f;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                rt.anchoredPosition = Vector3.Lerp(from, Vector2.zero, t * t);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = Vector2.zero;
        }
    }
}
