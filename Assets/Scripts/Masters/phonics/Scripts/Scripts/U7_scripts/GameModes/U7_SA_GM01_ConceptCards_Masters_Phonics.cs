using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U7_SA_GM01_ConceptCards_Masters_Phonics : MonoBehaviour
    {
        [Header("Card Hierarchy & Navigation")]
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private Button nextCardBtn;
        [SerializeField] private Button prevCardBtn;
        [SerializeField] private TextMeshProUGUI cardCounterText;
        [SerializeField] private GameObject[] cardPanels;

        [Header("Voice A Audio Clips")]
        public AudioClip card1IntroClip;
        public AudioClip card2IntroClip;
        public AudioClip card3IntroClip;
        public AudioClip card4IntroClip;
        public AudioClip card5IntroClip;

        [Header("Card Colors & Feedback")]
        [Tooltip("Default background color for card panels and example items")]
        public Color defaultCardColor = Color.white;
        [Tooltip("Highlight color when an interactive word / button is tapped")]
        public Color highlightColor = new Color(0.12f, 0.75f, 0.45f, 1f);

        private int currentCardIndex = 0;
        private const int TOTAL_CARDS = 5;
        private static Sprite proceduralRoundedSprite = null;

        public static Sprite GetOrCreateRoundedSprite(int size = 128, int radius = 24)
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

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
            currentCardIndex = 0;
            ShowCard(currentCardIndex);
        }

        public void AutoBindHierarchyElements()
        {
            // Find or create dedicated CardsContainer sized for phone screens
            if (cardsContainer == null || cardsContainer == transform)
            {
                Transform found = transform.Find("CardContainer") 
                               ?? transform.Find("Cards_Container") 
                               ?? transform.Find("DynamicCardContainer")
                               ?? transform.Find("CardsContainer");
                if (found != null && found != transform)
                {
                    cardsContainer = found;
                }
                else
                {
                    GameObject cObj = new GameObject("CardContainer", typeof(RectTransform));
                    cObj.transform.SetParent(transform, false);
                    cardsContainer = cObj.transform;
                }
            }

            if (cardsContainer != null && cardsContainer != transform)
            {
                RectTransform rt = cardsContainer.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0f, -15f);
                    rt.sizeDelta = new Vector2(1700f, 760f);
                }
            }

            if (nextCardBtn == null)
            {
                Transform n = transform.Find("NextButton") 
                           ?? transform.Find("NextCardButton") 
                           ?? transform.Find("Btn_Next") 
                           ?? transform.Find("NextBtn");
                if (n != null) nextCardBtn = n.GetComponent<Button>();
            }

            if (prevCardBtn == null)
            {
                Transform p = transform.Find("PrevButton") 
                           ?? transform.Find("PrevCardButton") 
                           ?? transform.Find("Btn_Prev") 
                           ?? transform.Find("PrevBtn");
                if (p != null) prevCardBtn = p.GetComponent<Button>();
            }

            if (cardCounterText == null)
            {
                Transform t = transform.Find("ProgressHUD/Progress_Text") 
                           ?? transform.Find("ProgressHUD/Card_Counter_Text")
                           ?? transform.Find("ProgressText")
                           ?? transform.Find("CardCounterText");
                if (t != null) cardCounterText = t.GetComponent<TextMeshProUGUI>();
            }

            if (nextCardBtn != null)
            {
                nextCardBtn.gameObject.SetActive(true);
                nextCardBtn.onClick.RemoveAllListeners();
                nextCardBtn.onClick.AddListener(OnNextCardTapped);
                nextCardBtn.transform.SetAsLastSibling();

                RectTransform nrt = nextCardBtn.GetComponent<RectTransform>();
                if (nrt != null && nrt.sizeDelta.x < 180f)
                {
                    nrt.sizeDelta = new Vector2(240f, 68f);
                }
            }

            if (prevCardBtn != null)
            {
                prevCardBtn.onClick.RemoveAllListeners();
                prevCardBtn.onClick.AddListener(OnPrevCardTapped);
                prevCardBtn.transform.SetAsLastSibling();

                RectTransform prt = prevCardBtn.GetComponent<RectTransform>();
                if (prt != null && prt.sizeDelta.x < 120f)
                {
                    prt.sizeDelta = new Vector2(180f, 68f);
                }
            }

            // Ensure TopBar and Back Button remain active and in front
            Transform topBar = transform.Find("TopBar") ?? transform.Find("Header");
            if (topBar != null)
            {
                topBar.gameObject.SetActive(true);
                topBar.SetAsLastSibling();
                foreach (Transform child in topBar)
                {
                    if (child != null) child.gameObject.SetActive(true);
                }
            }

            Transform backBtn = transform.Find("Back_Button") ?? transform.Find("BackButton") ?? transform.Find("TopBar/Back_Button");
            if (backBtn != null)
            {
                backBtn.gameObject.SetActive(true);
                backBtn.SetAsLastSibling();
            }

            Slider s = GetComponentInChildren<Slider>(true);
            if (s != null) U7_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(s);

#if UNITY_EDITOR
            if (card1IntroClip == null) card1IntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/U7_audio/U7_voiceA/A vowel team is two vowel letters sitting.mp3");
            if (card2IntroClip == null) card2IntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/U7_audio/U7_voiceA/Heres the annoying part One sound can be.mp3");
            if (card3IntroClip == null) card3IntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/U7_audio/U7_voiceA/Same two letters Different sound English does this.mp3");
            if (card4IntroClip == null) card4IntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/U7_audio/U7_voiceA/Coooiiin Hear that It started in one place.mp3");
            if (card5IntroClip == null) card5IntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/U7_audio/U7_voiceA/Careful here these two things arent opposites Vowel.mp3");
#endif
        }

        public void ShowCard(int index)
        {
            currentCardIndex = Mathf.Clamp(index, 0, TOTAL_CARDS - 1);
            U7_SA_AudioManager_Masters_Phonics.Instance?.StopAllAudio();

            UpdateCardCounter();

            // Check if pre-wired inspector card panels exist
            if (cardPanels != null && cardPanels.Length >= TOTAL_CARDS)
            {
                for (int i = 0; i < cardPanels.Length; i++)
                {
                    if (cardPanels[i] != null) cardPanels[i].SetActive(i == currentCardIndex);
                }
            }
            else
            {
                // Render dynamic card inside cardsContainer
                RenderDynamicCard(currentCardIndex);
            }

            // Ensure navigation buttons and back buttons stay on top
            if (prevCardBtn != null) prevCardBtn.transform.SetAsLastSibling();
            if (nextCardBtn != null) nextCardBtn.transform.SetAsLastSibling();

            PlayCurrentCardAudio();
        }

        private void UpdateCardCounter()
        {
            if (cardCounterText != null)
            {
                cardCounterText.text = $"<b>Card {currentCardIndex + 1} of {TOTAL_CARDS}</b>";
                cardCounterText.fontSize = 32;
                cardCounterText.color = Color.white;
            }

            if (prevCardBtn != null)
            {
                prevCardBtn.gameObject.SetActive(currentCardIndex > 0);
            }

            if (nextCardBtn != null)
            {
                nextCardBtn.gameObject.SetActive(true);
                var tmp = nextCardBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = (currentCardIndex == TOTAL_CARDS - 1) ? "<b>START ACTIVITY 1  >></b>" : "<b>NEXT CARD  >></b>";
                    tmp.fontSize = 28;
                    tmp.color = Color.white;
                }
            }
        }

        private void ClearContainer()
        {
            if (cardsContainer == null) return;
            for (int i = cardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardsContainer.GetChild(i).gameObject);
            }
        }

        private void RenderDynamicCard(int cardIdx)
        {
            if (cardsContainer == null) return;
            ClearContainer();

            GameObject cardGo = new GameObject($"Card_{cardIdx + 1}", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(cardsContainer, false);
            RectTransform rt = cardGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bgImg = cardGo.GetComponent<Image>();
            bgImg.sprite = GetOrCreateRoundedSprite(128, 28);
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color(0.06f, 0.1f, 0.18f, 0.98f); // Deep navy translucent background

            // Inner Outline Border
            Outline outline = cardGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.72f, 0.98f, 0.6f);
            outline.effectDistance = new Vector2(3, -3);

            switch (cardIdx)
            {
                case 0:
                    BuildCard1Content(cardGo.transform);
                    break;
                case 1:
                    BuildCard2Content(cardGo.transform);
                    break;
                case 2:
                    BuildCard3Content(cardGo.transform);
                    break;
                case 3:
                    BuildCard4Content(cardGo.transform);
                    break;
                case 4:
                    BuildCard5Content(cardGo.transform);
                    break;
            }
        }

        // =========================================================================
        // Dynamic Card Builders (Mobile Scaled & High Readability)
        // =========================================================================

        private void BuildCard1Content(Transform parent)
        {
            CreateCardHeader(parent, "CARD 1 — TWO LETTERS, ONE SOUND", 
                "\"Vowels that work in pairs\"", "#38BDF8");

            CreateCardSubBanner(parent, "<b>A vowel team is two vowel letters sitting together and making one vowel sound.\nTwo letters. One sound. That’s it.</b>", new Vector2(0f, 160f));

            // 4 Big Example Cards
            string[,] examples = new string[,]
            {
                { "ai", "rain", "First vowel A speaks: /eɪ/" },
                { "oa", "boat", "First vowel O speaks: /oʊ/" },
                { "ee", "tree", "Vowel team EE speaks: /iː/" },
                { "i_e", "kite", "Split vowel team: /aɪ/" }
            };

            float startX = -585f;
            float stepX = 390f;
            for (int i = 0; i < 4; i++)
            {
                string chunk = examples[i, 0];
                string word = examples[i, 1];
                string desc = examples[i, 2];
                CreateExampleBox(parent, chunk, word, desc, new Vector2(startX + i * stepX, -45f), new Color(0.12f, 0.22f, 0.38f));
            }
        }

        private void BuildCard2Content(Transform parent)
        {
            CreateCardHeader(parent, "CARD 2 — ONE SOUND, MANY SPELLINGS", 
                "\"English has options\"", "#F59E0B");

            CreateCardSubBanner(parent, "Notice how different spellings create the exact same sound (Tap words to listen):", new Vector2(0f, 185f));

            // 5 Tall Sound Rows (High touch area and bold on mobile)
            CreateSoundRow(parent, "/eɪ/  \"ay\"", "ai · ay · ei · a_e", new string[] { "rain", "day", "eight", "cake" }, new Vector2(0f, 105f), "#38BDF8");
            CreateSoundRow(parent, "/iː/  \"ee\"", "ee · ea · ey · ie", new string[] { "see", "eat", "key", "piece" }, new Vector2(0f, 25f), "#10B981");
            CreateSoundRow(parent, "/əʊ/  \"oh\"", "oa · ow · oe · o_e", new string[] { "boat", "slow", "toe", "bone" }, new Vector2(0f, -55f), "#F59E0B");
            CreateSoundRow(parent, "/aɪ/  \"eye\"", "igh · ie · y · i_e", new string[] { "light", "pie", "sky", "kite" }, new Vector2(0f, -135f), "#EC4899");
            CreateSoundRow(parent, "/uː/  \"oo\"", "oo · ue · ui · ew", new string[] { "moon", "glue", "fruit", "drew" }, new Vector2(0f, -215f), "#A855F7");
        }

        private void BuildCard3Content(Transform parent)
        {
            CreateCardHeader(parent, "CARD 3 — A DIPHTHONG GLIDES", 
                "\"A sound that moves\"", "#EC4899");

            CreateCardSubBanner(parent, "Most vowel sounds hold steady: <b>seeeee</b>.\nA diphthong glides from one sound into another in one syllable: <b>coin (o ... ee)</b>.", new Vector2(0f, 160f));

            // Dual Large Sound Comparison Panels
            CreateHoldVsGlidePanel(parent, "HOLD SOUND (Monophthong)", "see", "/iː/ · Steady Hold", 
                "Mouth stays completely still:\ns e e e e e e (steady soundwave)", new Vector2(-410f, -50f), "#38BDF8", false);

            CreateHoldVsGlidePanel(parent, "GLIDE SOUND (Diphthong)", "coin", "/ɔɪ/ · Vocal Glide", 
                "Mouth glides from /ɔ/ to /ɪ/:\nc o ... e e n (bending soundwave)", new Vector2(410f, -50f), "#F59E0B", true);
        }

        private void BuildCard4Content(Transform parent)
        {
            CreateCardHeader(parent, "CARD 4 — TEAM OR GLIDE? TWO QUESTIONS", 
                "\"They’re not opposites — Spelling vs Sound\"", "#A855F7");

            CreateCardSubBanner(parent, "<b>Vowel Team</b> = spelling (how many letters?). <b>Diphthong</b> = sound (does it glide?).\nA word can be both!", new Vector2(0f, 185f));

            // 4 Tall Matrix Comparison Rows
            CreateMatrixRow(parent, "boil", "Yes (o + i)", "Yes (glides /ɔɪ/)", "Vowel Team & Diphthong", new Vector2(0f, 100f), "#10B981");
            CreateMatrixRow(parent, "see",  "Yes (e + e)", "No (holds steady)",  "Vowel Team, not Diphthong", new Vector2(0f, 15f), "#38BDF8");
            CreateMatrixRow(parent, "cow",  "Yes (o + w)", "Yes (glides /aʊ/)", "Vowel Team & Diphthong", new Vector2(0f, -70f), "#10B981");
            CreateMatrixRow(parent, "look", "Yes (o + o)", "No (holds steady)",  "Vowel Team, not Diphthong", new Vector2(0f, -155f), "#38BDF8");
        }

        private void BuildCard5Content(Transform parent)
        {
            CreateCardHeader(parent, "CARD 5 — THE EIGHT DIPHTHONGS", 
                "\"The 8 Gliding Sounds of English\"", "#10B981");

            CreateCardSubBanner(parent, "Tap any diphthong box to hear the glide and its example words:", new Vector2(0f, 205f));

            // 8 Diphthongs arranged in a 2-Column Grid (4 Left, 4 Right) for maximum mobile readability
            // Left Column:
            CreateCompactDiphthongRow(parent, "/aɪ/", "\"ah\" to \"ee\"", "i_e · ie · y · igh", "hive, pie, sky", new Vector2(-410f, 125f));
            CreateCompactDiphthongRow(parent, "/eɪ/", "\"eh\" to \"ee\"", "a_e · ai · ay · ei", "take, maid, pay", new Vector2(-410f, 38f));
            CreateCompactDiphthongRow(parent, "/əʊ/", "\"uh\" to \"oo\"", "o_e · oa · ow · oe", "bone, moan, slow", new Vector2(-410f, -49f));
            CreateCompactDiphthongRow(parent, "/aʊ/", "\"ah\" to \"oo\"", "ou · ow", "round, brown, house", new Vector2(-410f, -136f));

            // Right Column:
            CreateCompactDiphthongRow(parent, "/ɔɪ/", "\"aw\" to \"ee\"", "oi · oy", "coin, boy, noise", new Vector2(410f, 125f));
            CreateCompactDiphthongRow(parent, "/ɪə/", "\"ee\" to \"uh\"", "ear · eer · ere", "hear, deer, here", new Vector2(410f, 38f));
            CreateCompactDiphthongRow(parent, "/eə/", "\"eh\" to \"uh\"", "air · are · ear", "hair, care, bear", new Vector2(410f, -49f));
            CreateCompactDiphthongRow(parent, "/ʊə/", "\"oo\" to \"uh\"", "ure · oor · our", "pure, cure, poor", new Vector2(410f, -136f));
        }

        // =========================================================================
        // UI Helper Generators (Mobile Scaled & High Readability)
        // =========================================================================

        private void CreateCardHeader(Transform parent, string title, string rule, string highlightColorHex)
        {
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(parent, false);
            RectTransform rt = titleObj.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0f, 305f);
            rt.sizeDelta = new Vector2(1650f, 58f);

            var tmp = titleObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<color={highlightColorHex}><b>{title}</b></color>";
            tmp.fontSize = 44;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            GameObject ruleObj = new GameObject("Rule", typeof(RectTransform), typeof(TextMeshProUGUI));
            ruleObj.transform.SetParent(parent, false);
            RectTransform rrt = ruleObj.GetComponent<RectTransform>();
            rrt.anchoredPosition = new Vector2(0f, 252f);
            rrt.sizeDelta = new Vector2(1650f, 48f);

            var rtmp = ruleObj.GetComponent<TextMeshProUGUI>();
            rtmp.text = $"<color=#FBBF24><i>{rule}</i></color>";
            rtmp.fontSize = 32;
            rtmp.alignment = TextAlignmentOptions.Center;
        }

        private void CreateCardSubBanner(Transform parent, string text, Vector2 pos)
        {
            GameObject obj = new GameObject("SubBanner", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1650f, 65f);

            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 28;
            tmp.color = new Color(0.9f, 0.95f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private void CreateExampleBox(Transform parent, string chunk, string word, string desc, Vector2 pos, Color bgCol)
        {
            GameObject box = new GameObject($"Box_{word}", typeof(RectTransform), typeof(Image), typeof(Button));
            box.transform.SetParent(parent, false);
            RectTransform rt = box.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(365f, 305f);

            Image img = box.GetComponent<Image>();
            img.sprite = GetOrCreateRoundedSprite(128, 22);
            img.type = Image.Type.Sliced;
            img.color = bgCol;

            // Chunk Badge
            GameObject chunkObj = new GameObject("Chunk", typeof(RectTransform), typeof(TextMeshProUGUI));
            chunkObj.transform.SetParent(box.transform, false);
            RectTransform crt = chunkObj.GetComponent<RectTransform>();
            crt.anchoredPosition = new Vector2(0f, 80f);
            crt.sizeDelta = new Vector2(330f, 75f);

            var ctmp = chunkObj.GetComponent<TextMeshProUGUI>();
            ctmp.text = $"<color=#FBBF24><b>{chunk}</b></color>";
            ctmp.fontSize = 72;
            ctmp.alignment = TextAlignmentOptions.Center;

            // Word
            GameObject wordObj = new GameObject("Word", typeof(RectTransform), typeof(TextMeshProUGUI));
            wordObj.transform.SetParent(box.transform, false);
            RectTransform wrt = wordObj.GetComponent<RectTransform>();
            wrt.anchoredPosition = new Vector2(0f, 5f);
            wrt.sizeDelta = new Vector2(330f, 55f);

            var wtmp = wordObj.GetComponent<TextMeshProUGUI>();
            wtmp.text = $"<b>{word}</b>";
            wtmp.fontSize = 42;
            wtmp.color = Color.white;
            wtmp.alignment = TextAlignmentOptions.Center;

            // Description
            GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(box.transform, false);
            RectTransform drt = descObj.GetComponent<RectTransform>();
            drt.anchoredPosition = new Vector2(0f, -68f);
            drt.sizeDelta = new Vector2(330f, 60f);

            var dtmp = descObj.GetComponent<TextMeshProUGUI>();
            dtmp.text = desc;
            dtmp.fontSize = 26;
            dtmp.color = new Color(0.82f, 0.92f, 1f);
            dtmp.alignment = TextAlignmentOptions.Center;

            Button btn = box.GetComponent<Button>();
            btn.onClick.AddListener(() => {
                PlayExampleWord(word);
            });
        }

        private void CreateSoundRow(Transform parent, string soundLabel, string spellings, string[] words, Vector2 pos, string colHex)
        {
            GameObject row = new GameObject($"SoundRow_{soundLabel}", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1640f, 74f);

            Image img = row.GetComponent<Image>();
            img.sprite = GetOrCreateRoundedSprite(128, 14);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.1f, 0.16f, 0.28f, 0.95f);

            // Sound Label
            GameObject sObj = new GameObject("Sound", typeof(RectTransform), typeof(TextMeshProUGUI));
            sObj.transform.SetParent(row.transform, false);
            RectTransform srt = sObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(-630f, 0f);
            srt.sizeDelta = new Vector2(280f, 60f);

            var stmp = sObj.GetComponent<TextMeshProUGUI>();
            stmp.text = $"<color={colHex}><b>{soundLabel}</b></color>";
            stmp.fontSize = 34;
            stmp.alignment = TextAlignmentOptions.Left;

            // Spellings Label
            GameObject spObj = new GameObject("Spellings", typeof(RectTransform), typeof(TextMeshProUGUI));
            spObj.transform.SetParent(row.transform, false);
            RectTransform sprt = spObj.GetComponent<RectTransform>();
            sprt.anchoredPosition = new Vector2(-320f, 0f);
            sprt.sizeDelta = new Vector2(330f, 60f);

            var sptmp = spObj.GetComponent<TextMeshProUGUI>();
            sptmp.text = $"<color=#FBBF24><b>{spellings}</b></color>";
            sptmp.fontSize = 30;
            sptmp.alignment = TextAlignmentOptions.Center;

            // 4 Example Word Buttons (Wide touch targets for mobile)
            float startX = -60f;
            float stepX = 215f;
            for (int i = 0; i < words.Length; i++)
            {
                string w = words[i];
                GameObject wBtn = new GameObject($"W_{w}", typeof(RectTransform), typeof(Image), typeof(Button));
                wBtn.transform.SetParent(row.transform, false);
                RectTransform wrt = wBtn.GetComponent<RectTransform>();
                wrt.anchoredPosition = new Vector2(startX + i * stepX, 0f);
                wrt.sizeDelta = new Vector2(205f, 58f);

                Image wImg = wBtn.GetComponent<Image>();
                wImg.sprite = GetOrCreateRoundedSprite(128, 12);
                wImg.type = Image.Type.Sliced;
                wImg.color = new Color(0.18f, 0.28f, 0.46f);

                GameObject wtxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                wtxt.transform.SetParent(wBtn.transform, false);
                RectTransform wtxtRt = wtxt.GetComponent<RectTransform>();
                wtxtRt.anchorMin = Vector2.zero;
                wtxtRt.anchorMax = Vector2.one;
                wtxtRt.offsetMin = Vector2.zero;
                wtxtRt.offsetMax = Vector2.zero;

                var wtmp = wtxt.GetComponent<TextMeshProUGUI>();
                wtmp.text = $"<b>{w}</b>";
                wtmp.fontSize = 30;
                wtmp.color = Color.white;
                wtmp.alignment = TextAlignmentOptions.Center;

                Button b = wBtn.GetComponent<Button>();
                b.onClick.AddListener(() => PlayExampleWord(w));
            }
        }

        private void CreateHoldVsGlidePanel(Transform parent, string title, string word, string ipa, string desc, Vector2 pos, string colHex, bool isGlide)
        {
            GameObject panel = new GameObject($"Panel_{word}", typeof(RectTransform), typeof(Image), typeof(Button));
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(790f, 350f);

            Image img = panel.GetComponent<Image>();
            img.sprite = GetOrCreateRoundedSprite(128, 22);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.12f, 0.18f, 0.32f);

            GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            tObj.transform.SetParent(panel.transform, false);
            RectTransform trt = tObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 120f);
            trt.sizeDelta = new Vector2(740f, 48f);

            var tmp = tObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<color={colHex}><b>{title}</b></color>";
            tmp.fontSize = 34;
            tmp.alignment = TextAlignmentOptions.Center;

            // Word Badge
            GameObject wObj = new GameObject("WordBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            wObj.transform.SetParent(panel.transform, false);
            RectTransform wrt = wObj.GetComponent<RectTransform>();
            wrt.anchoredPosition = new Vector2(0f, 60f);
            wrt.sizeDelta = new Vector2(740f, 58f);

            var wtmp = wObj.GetComponent<TextMeshProUGUI>();
            wtmp.text = $"<b><size=120%>{word}</size></b>   <color=#FBBF24>({ipa})</color>";
            wtmp.fontSize = 46;
            wtmp.color = Color.white;
            wtmp.alignment = TextAlignmentOptions.Center;

            // Waveform visual text
            GameObject waveObj = new GameObject("Wave", typeof(RectTransform), typeof(TextMeshProUGUI));
            waveObj.transform.SetParent(panel.transform, false);
            RectTransform waveRt = waveObj.GetComponent<RectTransform>();
            waveRt.anchoredPosition = new Vector2(0f, 0f);
            waveRt.sizeDelta = new Vector2(740f, 44f);

            var waveTmp = waveObj.GetComponent<TextMeshProUGUI>();
            waveTmp.text = isGlide ? "~~ <b><color=#F59E0B>SLIDING GLIDE</color></b> ~~" : "== <b><color=#38BDF8>FLAT STEADY HOLD</color></b> ==";
            waveTmp.fontSize = 30;
            waveTmp.alignment = TextAlignmentOptions.Center;

            // Desc
            GameObject dObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            dObj.transform.SetParent(panel.transform, false);
            RectTransform drt = dObj.GetComponent<RectTransform>();
            drt.anchoredPosition = new Vector2(0f, -72f);
            drt.sizeDelta = new Vector2(740f, 75f);

            var dtmp = dObj.GetComponent<TextMeshProUGUI>();
            dtmp.text = desc;
            dtmp.fontSize = 28;
            dtmp.color = new Color(0.88f, 0.94f, 1f);
            dtmp.alignment = TextAlignmentOptions.Center;

            Button b = panel.GetComponent<Button>();
            b.onClick.AddListener(() => PlayExampleWord(word));
        }

        private void CreateMatrixRow(Transform parent, string word, string twoVowels, string glides, string result, Vector2 pos, string resColHex)
        {
            GameObject row = new GameObject($"Matrix_{word}", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(parent, false);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(1640f, 80f);

            Image img = row.GetComponent<Image>();
            img.sprite = GetOrCreateRoundedSprite(128, 14);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.1f, 0.16f, 0.28f, 0.95f);

            // Word Button
            GameObject wBtn = new GameObject($"Btn_{word}", typeof(RectTransform), typeof(Image), typeof(Button));
            wBtn.transform.SetParent(row.transform, false);
            RectTransform wrt = wBtn.GetComponent<RectTransform>();
            wrt.anchoredPosition = new Vector2(-600f, 0f);
            wrt.sizeDelta = new Vector2(260f, 62f);

            Image wImg = wBtn.GetComponent<Image>();
            wImg.sprite = GetOrCreateRoundedSprite(128, 12);
            wImg.type = Image.Type.Sliced;
            wImg.color = new Color(0.18f, 0.28f, 0.46f);

            GameObject wtxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            wtxt.transform.SetParent(wBtn.transform, false);
            RectTransform wtxtRt = wtxt.GetComponent<RectTransform>();
            wtxtRt.anchorMin = Vector2.zero;
            wtxtRt.anchorMax = Vector2.one;
            wtxtRt.offsetMin = Vector2.zero;
            wtxtRt.offsetMax = Vector2.zero;

            var wtmp = wtxt.GetComponent<TextMeshProUGUI>();
            wtmp.text = $"<b>{word}</b>";
            wtmp.fontSize = 32;
            wtmp.color = Color.white;
            wtmp.alignment = TextAlignmentOptions.Center;

            Button b = wBtn.GetComponent<Button>();
            b.onClick.AddListener(() => PlayExampleWord(word));

            // Col 2: 2 Vowels?
            GameObject c2 = new GameObject("TwoVowels", typeof(RectTransform), typeof(TextMeshProUGUI));
            c2.transform.SetParent(row.transform, false);
            RectTransform c2rt = c2.GetComponent<RectTransform>();
            c2rt.anchoredPosition = new Vector2(-280f, 0f);
            c2rt.sizeDelta = new Vector2(310f, 60f);
            var c2tmp = c2.GetComponent<TextMeshProUGUI>();
            c2tmp.text = $"<color=#38BDF8><b>{twoVowels}</b></color>";
            c2tmp.fontSize = 30;
            c2tmp.alignment = TextAlignmentOptions.Center;

            // Col 3: Glides?
            GameObject c3 = new GameObject("Glides", typeof(RectTransform), typeof(TextMeshProUGUI));
            c3.transform.SetParent(row.transform, false);
            RectTransform c3rt = c3.GetComponent<RectTransform>();
            c3rt.anchoredPosition = new Vector2(75f, 0f);
            c3rt.sizeDelta = new Vector2(310f, 60f);
            var c3tmp = c3.GetComponent<TextMeshProUGUI>();
            c3tmp.text = glides.StartsWith("Yes") ? $"<color=#10B981><b>{glides}</b></color>" : $"<color=#94A3B8>{glides}</color>";
            c3tmp.fontSize = 30;
            c3tmp.alignment = TextAlignmentOptions.Center;

            // Col 4: Result
            GameObject c4 = new GameObject("Result", typeof(RectTransform), typeof(TextMeshProUGUI));
            c4.transform.SetParent(row.transform, false);
            RectTransform c4rt = c4.GetComponent<RectTransform>();
            c4rt.anchoredPosition = new Vector2(460f, 0f);
            c4rt.sizeDelta = new Vector2(430f, 60f);
            var c4tmp = c4.GetComponent<TextMeshProUGUI>();
            c4tmp.text = $"<color={resColHex}><b>{result}</b></color>";
            c4tmp.fontSize = 30;
            c4tmp.alignment = TextAlignmentOptions.Left;
        }

        private void CreateCompactDiphthongRow(Transform parent, string ipa, string glideFromTo, string spellings, string examples, Vector2 pos)
        {
            GameObject row = new GameObject($"Diph_{ipa}", typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(parent, false);
            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(790f, 76f);

            Image img = row.GetComponent<Image>();
            img.sprite = GetOrCreateRoundedSprite(128, 12);
            img.type = Image.Type.Sliced;
            img.color = new Color(0.1f, 0.16f, 0.28f, 0.95f);

            // IPA
            GameObject iObj = new GameObject("IPA", typeof(RectTransform), typeof(TextMeshProUGUI));
            iObj.transform.SetParent(row.transform, false);
            RectTransform irt = iObj.GetComponent<RectTransform>();
            irt.anchoredPosition = new Vector2(-290f, 0f);
            irt.sizeDelta = new Vector2(170f, 60f);
            var itmp = iObj.GetComponent<TextMeshProUGUI>();
            itmp.text = $"<color=#38BDF8><b>{ipa}</b></color>\n<size=75%><color=#FBBF24>{glideFromTo}</color></size>";
            itmp.fontSize = 30;
            itmp.alignment = TextAlignmentOptions.Left;

            // Spellings & Examples
            GameObject spObj = new GameObject("Details", typeof(RectTransform), typeof(TextMeshProUGUI));
            spObj.transform.SetParent(row.transform, false);
            RectTransform sprt = spObj.GetComponent<RectTransform>();
            sprt.anchoredPosition = new Vector2(85f, 0f);
            sprt.sizeDelta = new Vector2(550f, 60f);
            var sptmp = spObj.GetComponent<TextMeshProUGUI>();
            sptmp.text = $"<color=#BAE6FD><b>{spellings}</b></color>\n<size=90%><color=white>{examples}</color></size>";
            sptmp.fontSize = 26;
            sptmp.alignment = TextAlignmentOptions.Left;

            Button b = row.GetComponent<Button>();
            string firstWord = examples.Split(',')[0].Trim();
            b.onClick.AddListener(() => PlayExampleWord(firstWord));
        }

        // =========================================================================
        // Audio Narration & Interactions
        // =========================================================================

        private void PlayCurrentCardAudio()
        {
            AudioClip clipToPlay = null;
            string fallbackVoice = "";

            switch (currentCardIndex)
            {
                case 0:
                    clipToPlay = card1IntroClip;
                    fallbackVoice = "U07_VO_card1";
                    break;
                case 1:
                    clipToPlay = card2IntroClip;
                    fallbackVoice = "U07_VO_card2";
                    break;
                case 2:
                    clipToPlay = card3IntroClip;
                    fallbackVoice = "U07_VO_card3";
                    break;
                case 3:
                    clipToPlay = card4IntroClip;
                    fallbackVoice = "U07_VO_card4";
                    break;
                case 4:
                    clipToPlay = card5IntroClip;
                    fallbackVoice = "U07_VO_card5";
                    break;
            }

            if (clipToPlay != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clipToPlay);
            }
            else if (!string.IsNullOrEmpty(fallbackVoice))
            {
                U7_SA_AudioManager_Masters_Phonics.Instance?.PlayVoicePrompt(fallbackVoice);
            }
        }

        public void OnNextCardTapped()
        {
            U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");

            if (currentCardIndex < TOTAL_CARDS - 1)
            {
                ShowCard(currentCardIndex + 1);
            }
            else
            {
                // Seamless transition to Activity 1 (Team Up)
                if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.MarkLearnCompleted();
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.OpenSection(1);
                }
            }
        }

        public void OnPrevCardTapped()
        {
            U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
            if (currentCardIndex > 0)
            {
                ShowCard(currentCardIndex - 1);
            }
        }

        public void PlayExampleWord(string word)
        {
            U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
            U7_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(word);
        }
    }
}
