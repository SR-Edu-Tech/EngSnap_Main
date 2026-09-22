using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U4_SA_GM03_SevenDoors_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Instruction Clips")]
        [Tooltip("U04_VO_a1_intro: 'Seven types, four bins at a time. The answer is written on every bin...' (8s)")]
        public AudioClip introInstructionClip;

        [Tooltip("U04_VO_a1_outro: 'Names learned. The real work on these starts in Unit Five.' (5s)")]
        public AudioClip outroClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI deckRemainingTMP;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioButton;

        [Header("3. Card Spawn Anchor")]
        [SerializeField] private Transform cardSpawnAnchor;

        [Header("4. 4 Door Bins (Auto-bound)")]
        [SerializeField] private RectTransform bin1;
        [SerializeField] private RectTransform bin2;
        [SerializeField] private RectTransform bin3;
        [SerializeField] private RectTransform bin4;

        [Header("Door Sprites")]
        public Sprite round1Door1Closed;
        public Sprite round1Door2Open;
        public Sprite round1Door3MagicE;
        public Sprite round1Door4VowelTeam;

        private List<SyllableTypeDoorItem> deck;
        private int currentScore = 0;
        private int totalDeckSize = 14;
        private int currentRound = 1; // 1 or 2
        private bool isProcessingAnswer = false;
        private GameObject currentActiveCard;
        private string currentActiveWord = "";
        private Sprite proceduralRoundedSprite;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void Start()
        {
            PlayIntroInstruction();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            currentScore = 0;
            currentRound = 1;
            UpdateScoreUI();
            InitializeDeck();
            SetupRoundBins(currentRound);
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
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
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
                var t = transform.Find("prompt bg/Prompt_Text") ?? transform.Find("Prompt_Text") ?? transform.Find("PromptText") ?? transform.Find("InstructionText");
                if (t != null) promptTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (scoreTMP == null)
            {
                var t = transform.Find("ScoreText") ?? transform.Find("Score_Text") ?? transform.Find("Score");
                if (t != null) scoreTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (deckRemainingTMP == null)
            {
                Transform t = transform.Find("DeckRemaining_Text") ?? transform.Find("RemainingText") ?? transform.Find("Deck_Text");
                if (t != null) deckRemainingTMP = t.GetComponent<TextMeshProUGUI>();
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

            // 4 Bins
            Transform binsGroup = transform.Find("Doors_Row") ?? transform.Find("Bins_Row") ?? transform.Find("Sort_Bins") ?? transform;
            if (bin1 == null) bin1 = FindBin(binsGroup, "door_1", "door1", "bin1", "1");
            if (bin2 == null) bin2 = FindBin(binsGroup, "door_2", "door2", "bin2", "2");
            if (bin3 == null) bin3 = FindBin(binsGroup, "door_3", "door3", "bin3", "3");
            if (bin4 == null) bin4 = FindBin(binsGroup, "door_4", "door4", "bin4", "4");

            // Replay Button
            if (replayAudioButton == null)
            {
                Transform r = transform.Find("ReplayButton") ?? transform.Find("Audio_Button") ?? transform.Find("Speaker_Button") ?? transform.Find("Replay_Button") ?? transform.Find("SpeakerButton");
                if (r == null)
                {
                    Button[] btns = GetComponentsInChildren<Button>(true);
                    foreach (var b in btns)
                    {
                        string bName = b.gameObject.name.ToLower();
                        if (bName.Contains("replay") || bName.Contains("speaker") || bName.Contains("audio"))
                        {
                            replayAudioButton = b;
                            break;
                        }
                    }
                }
                else
                {
                    replayAudioButton = r.GetComponent<Button>();
                }
            }
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(replayAudioButton.transform, 1.15f));
                    ReplayCurrentCardAudio();
                });
            }

            // Format Bin Dimensions & Position
            PositionDoorsRow(binsGroup);
        }

        private void PositionDoorsRow(Transform binsGroup)
        {
            // Do NOT overwrite user's custom DeckRemaining_Text or CardSpawnAnchor positions in the Inspector!
        }

        private IEnumerator HandleIncorrectDrop(SyllableTypeDoorItem item, GameObject cardObj, Transform targetBin)
        {
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            if (cardObj != null)
            {
                RectTransform cardRT = cardObj.GetComponent<RectTransform>();
                var handler = cardObj.GetComponent<U4SevenDoorsDragHandler>();
                Vector3 originPos = handler != null ? handler.OriginalLocalPosition : Vector3.zero;

                // 1. Shake card at drop location
                Vector3 currentLocal = cardRT.localPosition;
                for (int i = 0; i < 4; i++)
                {
                    if (cardRT == null) break;
                    cardRT.localPosition = currentLocal + new Vector3(UnityEngine.Random.Range(-18f, 18f), 0, 0);
                    yield return new WaitForSeconds(0.04f);
                }

                // 2. Smoothly animate snap-back to starting spawn anchor position
                if (cardRT != null)
                {
                    float elapsed = 0f;
                    float duration = 0.22f;
                    Vector3 startPos = cardRT.localPosition;

                    while (elapsed < duration)
                    {
                        if (cardRT == null) break;
                        elapsed += Time.deltaTime;
                        cardRT.localPosition = Vector3.Lerp(startPos, originPos, elapsed / duration);
                        yield return null;
                    }

                    if (cardRT != null) cardRT.localPosition = originPos;
                }
            }

            isProcessingAnswer = false;
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
            deck = new List<SyllableTypeDoorItem>
            {
                // Round 1 (8 items)
                new SyllableTypeDoorItem { word = "cat", category = SyllableTypeCategory.Closed, patternNotation = "v c", roundIndex = 1 },
                new SyllableTypeDoorItem { word = "napkin", category = SyllableTypeCategory.Closed, patternNotation = "v c", roundIndex = 1 },
                new SyllableTypeDoorItem { word = "tiger", category = SyllableTypeCategory.Open, patternNotation = "v", roundIndex = 1 },
                new SyllableTypeDoorItem { word = "paper", category = SyllableTypeCategory.Open, patternNotation = "v", roundIndex = 1 },
                new SyllableTypeDoorItem { word = "bake", category = SyllableTypeCategory.MagicE, patternNotation = "v c e", roundIndex = 1 },
                new SyllableTypeDoorItem { word = "bone", category = SyllableTypeCategory.MagicE, patternNotation = "v c e", roundIndex = 1 },
                new SyllableTypeDoorItem { word = "seed", category = SyllableTypeCategory.VowelTeam, patternNotation = "v v", roundIndex = 1 },
                new SyllableTypeDoorItem { word = "float", category = SyllableTypeCategory.VowelTeam, patternNotation = "v v", roundIndex = 1 },

                // Round 2 (6 items)
                new SyllableTypeDoorItem { word = "car", category = SyllableTypeCategory.RControlled, patternNotation = "v r", roundIndex = 2 },
                new SyllableTypeDoorItem { word = "bird", category = SyllableTypeCategory.RControlled, patternNotation = "v r", roundIndex = 2 },
                new SyllableTypeDoorItem { word = "boil", category = SyllableTypeCategory.Diphthong, patternNotation = "(v v)", roundIndex = 2 },
                new SyllableTypeDoorItem { word = "cloud", category = SyllableTypeCategory.Diphthong, patternNotation = "(v v)", roundIndex = 2 },
                new SyllableTypeDoorItem { word = "bubble", category = SyllableTypeCategory.ConsonantLe, patternNotation = "c + le", roundIndex = 2 },
                new SyllableTypeDoorItem { word = "circle", category = SyllableTypeCategory.ConsonantLe, patternNotation = "c + le", roundIndex = 2 }
            };

            totalDeckSize = deck.Count;
        }

        private void SetupRoundBins(int round)
        {
            if (round == 1)
            {
                SetBinLabels(bin1, "CLOSED", "cat (v c)", "#2E7D32");
                SetBinLabels(bin2, "OPEN", "ba-by (v)", "#1565C0");
                SetBinLabels(bin3, "MAGIC 'E'", "bake (v c e)", "#7B1FA2");
                SetBinLabels(bin4, "VOWEL TEAM", "team (v v)", "#E65100");
            }
            else
            {
                SetBinLabels(bin1, "r-CONTROLLED", "car (v r)", "#C2185B");
                SetBinLabels(bin2, "DIPHTHONG", "boil (v v)", "#00796B");
                SetBinLabels(bin3, "CONSONANT + LE", "bub-ble (c+le)", "#5D4037");
                SetBinLabels(bin4, "CLOSED", "cat (v c)", "#2E7D32");
            }
        }

        private void SetBinLabels(RectTransform bin, string title, string sub, string themeColorHex)
        {
            if (bin == null) return;

            // Delete or deactivate all legacy text objects inside the door bin
            for (int i = bin.childCount - 1; i >= 0; i--)
            {
                Transform c = bin.GetChild(i);
                if (c.name.Contains("DoorTitle") || c.name.Contains("DoorSub") || (c.name != "DoorLabelBanner" && c.GetComponent<TextMeshProUGUI>() != null))
                {
                    Destroy(c.gameObject);
                }
            }

            // Find or create dedicated banner below door
            Transform banner = bin.Find("DoorLabelBanner");
            if (banner == null)
            {
                GameObject bannerObj = new GameObject("DoorLabelBanner", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
                bannerObj.transform.SetParent(bin, false);
                banner = bannerObj.transform;

                RectTransform bannerRT = bannerObj.GetComponent<RectTransform>();
                bannerRT.anchorMin = new Vector2(0.5f, 0f);
                bannerRT.anchorMax = new Vector2(0.5f, 0f);
                bannerRT.pivot = new Vector2(0.5f, 1f);
                bannerRT.anchoredPosition = new Vector2(0f, -12f);
                bannerRT.sizeDelta = new Vector2(250f, 90f);

                Image img = bannerObj.GetComponent<Image>();
                img.sprite = GetRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = new Color(0.08f, 0.12f, 0.22f, 0.94f); // Deep navy translucent plaque

                var vlg = bannerObj.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(8, 8, 8, 8);
                vlg.spacing = 4;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;

                // Title TMP
                GameObject tObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                tObj.transform.SetParent(banner, false);
                var tTMP = tObj.GetComponent<TextMeshProUGUI>();
                tTMP.alignment = TextAlignmentOptions.Center;

                // Sub TMP
                GameObject sObj = new GameObject("SubText", typeof(RectTransform), typeof(TextMeshProUGUI));
                sObj.transform.SetParent(banner, false);
                var sTMP = sObj.GetComponent<TextMeshProUGUI>();
                sTMP.alignment = TextAlignmentOptions.Center;
            }

            var bannerTMPs = banner.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (bannerTMPs.Length > 0)
            {
                bannerTMPs[0].text = $"<b><size=28><color={themeColorHex}>{title}</color></size></b>";
            }
            if (bannerTMPs.Length > 1)
            {
                bannerTMPs[1].text = $"<size=24><color=#FFD54F><b>{sub}</b></color></size>";
            }
        }

        private void SpawnNextCard()
        {
            if (deck.Count == 0)
            {
                OnActivityComplete();
                return;
            }

            if (currentActiveCard != null) Destroy(currentActiveCard);

            SyllableTypeDoorItem item = deck[0];
            if (item.roundIndex != currentRound)
            {
                currentRound = item.roundIndex;
                SetupRoundBins(currentRound);
            }

            deck.RemoveAt(0);

            if (titleTMP != null)
            {
                titleTMP.text = $"<b><color=#FFFFFF>SEVEN DOORS (ROUND {currentRound} OF 2)</color></b>";
                titleTMP.enableAutoSizing = true;
                titleTMP.fontSizeMin = 28;
                titleTMP.fontSizeMax = 44;
                titleTMP.overflowMode = TextOverflowModes.Overflow;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }
            if (promptTMP != null)
            {
                promptTMP.text = "<color=#FFFFFF><b>Drag each word to its syllable type door!</b></color>";
                promptTMP.enableAutoSizing = true;
                promptTMP.fontSizeMin = 22;
                promptTMP.fontSizeMax = 32;
                promptTMP.alignment = TextAlignmentOptions.Center;
            }

            if (deckRemainingTMP != null)
            {
                deckRemainingTMP.text = $"Deck: <b>{deck.Count + 1} remaining</b>";
            }

            if (progressBar != null)
            {
                progressBar.value = (float)(totalDeckSize - deck.Count) / totalDeckSize;
            }

            // Create Draggable Card with Rounded Corners and Bold Typography
            Transform parent = cardSpawnAnchor != null ? cardSpawnAnchor : transform;
            currentActiveCard = new GameObject("DoorCard_" + item.word, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            currentActiveCard.transform.SetParent(parent, false);
            RectTransform rt = currentActiveCard.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340, 115);
            rt.anchoredPosition = Vector2.zero;

            Image img = currentActiveCard.GetComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(currentActiveCard.transform, false);
            RectTransform textRT = txtObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 8);
            textRT.offsetMax = new Vector2(-10, -8);

            var tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b><size=54><color=#000000>{item.word}</color></size></b>\n<size=22><color=#0288D1><b>[ Drag to Door ]</b></color></size>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // Drag Handler
            var handler = currentActiveCard.AddComponent<U4SevenDoorsDragHandler>();
            handler.Initialize(item, this, bin1, bin2, bin3, bin4);

            currentActiveWord = item.word;

            // Play word audio upon deal
            PlayWordAudio(item.word);
        }

        public void ReplayCurrentCardAudio()
        {
            if (!string.IsNullOrEmpty(currentActiveWord))
            {
                PlayWordAudio(currentActiveWord);
            }
            else if (introInstructionClip != null)
            {
                PlayIntroInstruction();
            }
        }

        private IEnumerator PunchScale(Transform tr, float scale)
        {
            if (tr == null) yield break;
            Vector3 orig = Vector3.one;
            float elapsed = 0f;
            float duration = 0.18f;

            while (elapsed < duration)
            {
                if (tr == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                tr.localScale = Vector3.Lerp(orig * scale, orig, t);
                yield return null;
            }
            if (tr != null) tr.localScale = orig;
        }

        private void PlayWordAudio(string word)
        {
            if (string.IsNullOrEmpty(word)) return;
            word = word.Trim();

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip clip = Resources.Load<AudioClip>($"U4_audio/words MP U4/{word}")
                              ?? Resources.Load<AudioClip>($"Audio/U4_audio/words MP U4/{word}")
                              ?? Resources.Load<AudioClip>($"U4_audio/Phonetic Schwa Respellings/{word}")
                              ?? Resources.Load<AudioClip>($"Audio/U4_audio/Phonetic Schwa Respellings/{word}")
                              ?? Resources.Load<AudioClip>($"U4_audio/words_U4_MP/{word}")
                              ?? Resources.Load<AudioClip>($"Audio/U4_audio/words_U4_MP/{word}")
                              ?? Resources.Load<AudioClip>($"U4_audio/{word}")
                              ?? Resources.Load<AudioClip>($"U3_audio/words_U3_MP/{word}");

#if UNITY_EDITOR
                if (clip == null)
                {
                    string[] guids = UnityEditor.AssetDatabase.FindAssets($"{word} t:AudioClip");
                    foreach (var g in guids)
                    {
                        string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                        if (System.IO.Path.GetFileNameWithoutExtension(p).Equals(word, StringComparison.OrdinalIgnoreCase))
                        {
                            clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                            break;
                        }
                    }
                }
#endif

                if (clip != null)
                {
                    U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
                }
            }
        }

        public void OnCardDroppedInBin(SyllableTypeDoorItem item, SyllableTypeCategory droppedCat, GameObject cardObj, Transform targetBin)
        {
            if (isProcessingAnswer) return;
            isProcessingAnswer = true;

            if (item.category == droppedCat)
            {
                StartCoroutine(HandleCorrectDrop(item, cardObj, targetBin));
            }
            else
            {
                StartCoroutine(HandleIncorrectDrop(item, cardObj, targetBin));
            }
        }

        private IEnumerator HandleCorrectDrop(SyllableTypeDoorItem item, GameObject cardObj, Transform targetBin)
        {
            currentScore += 100;
            UpdateScoreUI();

            if (U4_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U4_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(100);
            }

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayDoorOpen();
            }

            // Punch scale target door
            StartCoroutine(PunchDoor(targetBin));

            // Shrink and fade card
            if (cardObj != null)
            {
                float elapsed = 0f;
                float duration = 0.25f;
                Vector3 startScale = cardObj.transform.localScale;
                Vector3 startPos = cardObj.transform.position;
                Vector3 targetPos = targetBin.position;

                while (elapsed < duration)
                {
                    if (cardObj == null) break;
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    cardObj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                    cardObj.transform.position = Vector3.Lerp(startPos, targetPos, t);
                    yield return null;
                }
                Destroy(cardObj);
            }

            yield return new WaitForSeconds(0.2f);
            isProcessingAnswer = false;
            SpawnNextCard();
        }

        private IEnumerator PunchDoor(Transform door)
        {
            if (door == null) yield break;
            Vector3 orig = Vector3.one;
            door.localScale = orig * 1.15f;
            yield return new WaitForSeconds(0.18f);
            door.localScale = orig;
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null)
            {
                scoreTMP.text = $"Score: <b>{currentScore}</b>";
                scoreTMP.fontSize = 32;
            }
        }

        private void OnActivityComplete()
        {
            if (outroClip != null && U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(outroClip);
            }

            StartCoroutine(DelayedComplete());
        }

        private IEnumerator DelayedComplete()
        {
            yield return new WaitForSeconds(1.5f);
            if (U4_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U4_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }
        }

        private Sprite GetRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 24;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius - 1)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else if (dist <= radius)
                    {
                        colors[y * size + x] = new Color(1f, 1f, 1f, radius - dist);
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
    }

    public class U4SevenDoorsDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private SyllableTypeDoorItem itemData;
        private U4_SA_GM03_SevenDoors_Masters_Phonics manager;
        private RectTransform bin1, bin2, bin3, bin4;

        private RectTransform rt;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Vector3 originalLocalPos;
        public Vector3 OriginalLocalPosition => originalLocalPos;

        public void Initialize(SyllableTypeDoorItem item, U4_SA_GM03_SevenDoors_Masters_Phonics mgr, RectTransform b1, RectTransform b2, RectTransform b3, RectTransform b4)
        {
            itemData = item;
            manager = mgr;
            bin1 = b1;
            bin2 = b2;
            bin3 = b3;
            bin4 = b4;

            rt = GetComponent<RectTransform>();
            canvas = GetComponentInParent<Canvas>();
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            originalLocalPos = rt.localPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
            transform.localScale = Vector3.one * 1.08f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (canvas == null) return;
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rt, eventData.position, cam, out Vector3 worldPoint))
            {
                rt.position = worldPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
            transform.localScale = Vector3.one;

            // Find closest bin
            RectTransform[] bins = new RectTransform[] { bin1, bin2, bin3, bin4 };
            RectTransform closestBin = null;
            float minDistance = float.MaxValue;

            foreach (var b in bins)
            {
                if (b == null) continue;
                float dist = Vector3.Distance(rt.position, b.position);
                if (dist < minDistance && dist < 320f)
                {
                    minDistance = dist;
                    closestBin = b;
                }
            }

            if (closestBin != null)
            {
                SyllableTypeCategory chosenCat = GetCategoryForBin(closestBin);
                manager.OnCardDroppedInBin(itemData, chosenCat, gameObject, closestBin);
            }
            else
            {
                StartCoroutine(SnapBack());
            }
        }

        private SyllableTypeCategory GetCategoryForBin(RectTransform bin)
        {
            if (itemData.roundIndex == 1)
            {
                if (bin == bin1) return SyllableTypeCategory.Closed;
                if (bin == bin2) return SyllableTypeCategory.Open;
                if (bin == bin3) return SyllableTypeCategory.MagicE;
                return SyllableTypeCategory.VowelTeam;
            }
            else
            {
                if (bin == bin1) return SyllableTypeCategory.RControlled;
                if (bin == bin2) return SyllableTypeCategory.Diphthong;
                if (bin == bin3) return SyllableTypeCategory.ConsonantLe;
                return SyllableTypeCategory.Closed;
            }
        }

        private IEnumerator SnapBack()
        {
            float elapsed = 0f;
            float duration = 0.2f;
            Vector3 startPos = rt.localPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rt.localPosition = Vector3.Lerp(startPos, originalLocalPos, elapsed / duration);
                yield return null;
            }
            rt.localPosition = originalLocalPos;
        }
    }
}
