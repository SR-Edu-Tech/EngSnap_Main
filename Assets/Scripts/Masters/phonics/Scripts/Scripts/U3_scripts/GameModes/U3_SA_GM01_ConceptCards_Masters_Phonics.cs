using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U3_SA_GM01_ConceptCards_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Voice A Audio (Official Unit 3 Doc)")]
        [Tooltip("U03_VO_card1: 'A syllable is one beat of a word, with one vowel sound in it. Eat — one beat. Eating — two. Put your hand under your chin and say it. Every time your chin drops, that’s a syllable.' (14s)")]
        public AudioClip voiceACard1; // U03_VO_card1

        [Tooltip("U03_VO_card2: 'Words get names based on how many beats they have. One beat, two beats, three, or four and more. Tap any row to hear it.' (8s)")]
        public AudioClip voiceACard2; // U03_VO_card2

        [Tooltip("U03_VO_card3: 'Six rules for finding the split. You won’t need all six every time — just the one that fits the word in front of you.' (8s)")]
        public AudioClip voiceACard3; // U03_VO_card3

        [Tooltip("U03_VO_card4: 'Here’s the rule underneath all the others. Every syllable has exactly one vowel sound. Not one vowel letter — one vowel sound. Cake has two vowel letters, but only one beat.' (13s)")]
        public AudioClip voiceACard4; // U03_VO_card4

        [Header("2. Scene UI References (Auto-Bound)")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI descriptionTMP;
        [SerializeField] private Transform dynamicCardContainer;
        [SerializeField] private Button nextCardButton;
        [SerializeField] private Button replayAudioButton;

        [Header("3. Custom Manual Card Displays (Optional)")]
        [SerializeField] private GameObject manualCard1;
        [SerializeField] private GameObject manualCard2;
        [SerializeField] private GameObject manualCard3;
        [SerializeField] private GameObject manualCard4;

        [Header("4. Rounded Sprites (From Sprite Sheet)")]
        public Sprite roundedCardFrameBlue;   // Large Blue Frame
        public Sprite roundedCardFrameGreen;  // Large Green Frame
        public Sprite roundedPillSprite;      // Category Pill / Badge
        public Sprite roundedFrameOrange;     // Orange Frame
        public Sprite nextArrowSprite;        // Next Button Sprite

        [Header("5. Category Audio Clips (Voice B)")]
        [Tooltip("Card 2 Row 1: 'pen' (1 beat)")]
        public AudioClip audioPen;
        [Tooltip("Card 2 Row 2: 'pen-cil' (2 beats)")]
        public AudioClip audioPencil;
        [Tooltip("Card 2 Row 3: 're-mem-ber' (3 beats)")]
        public AudioClip audioRemember;
        [Tooltip("Card 2 Row 4: 'ther-mo-me-ter' (4 beats)")]
        public AudioClip audioThermometer;

        private int currentCardIndex = 0;

        private void Awake()
        {
            AutoBindUI();
        }

        private void OnEnable()
        {
            AutoBindUI();
            currentCardIndex = 0;
            ShowCard(currentCardIndex);
        }

        private Sprite generatedRoundedSprite;

        private void AutoBindUI()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Header_Title");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (descriptionTMP == null)
            {
                var t = transform.Find("DescriptionText") ?? transform.Find("Prompt_Text") ?? transform.Find("Subtitle");
                if (t != null) descriptionTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (nextCardButton == null)
            {
                Transform existing = transform.Find("NextButton") ?? transform.Find("Next_Button") ?? transform.Find("Btn_Next");
                if (existing != null)
                {
                    nextCardButton = existing.GetComponent<Button>();
                }
                else
                {
                    Button[] btns = GetComponentsInChildren<Button>(true);
                    foreach (var b in btns)
                    {
                        string bName = b.name.ToLower();
                        if (bName.Contains("next") || bName.Contains("continue") || bName.Contains("arrow"))
                        {
                            nextCardButton = b;
                            break;
                        }
                    }
                }
            }

            // If not found or inactive, ensure NextButton is active and at top sibling index
            if (nextCardButton == null)
            {
                GameObject nextBtnObj = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button));
                nextBtnObj.transform.SetParent(transform, false);
                RectTransform rt = nextBtnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.85f, 0.08f);
                rt.anchorMax = new Vector2(0.96f, 0.22f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                Image img = nextBtnObj.GetComponent<Image>();
                img.sprite = GetRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = new Color(0.12f, 0.78f, 0.35f, 1f); // Vibrant Solid Green

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(nextBtnObj.transform, false);
                var tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.text = "<b>NEXT ➜</b>";
                tmp.fontSize = 30;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;

                nextCardButton = nextBtnObj.GetComponent<Button>();
            }

            if (nextCardButton != null)
            {
                nextCardButton.gameObject.SetActive(true);
                nextCardButton.transform.SetAsLastSibling();
                nextCardButton.onClick.RemoveAllListeners();
                nextCardButton.onClick.AddListener(NextCard);
            }

            if (replayAudioButton == null)
            {
                var t = transform.Find("ReplayButton") ?? transform.Find("Replay_Button") ?? transform.Find("SpeakerButton");
                if (t != null) replayAudioButton = t.GetComponent<Button>();
            }

            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayAudio);
            }

            if (dynamicCardContainer == null)
            {
                Transform existing = transform.Find("DynamicCardContainer") ?? transform.Find("Machine_Display") ?? transform.Find("Card_Display");
                if (existing != null)
                {
                    dynamicCardContainer = existing;
                }
                else
                {
                    GameObject go = new GameObject("DynamicCardContainer", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    RectTransform rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.15f, 0.18f);
                    rt.anchorMax = new Vector2(0.85f, 0.72f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    dynamicCardContainer = go.transform;
                }
            }
        }

        public void ShowCard(int cardIndex)
        {
            currentCardIndex = cardIndex;
            AutoBindUI();

            // Hide Next button until narration completes
            if (nextCardButton != null)
            {
                nextCardButton.gameObject.SetActive(false);
            }

            if (manualCard1 != null) manualCard1.SetActive(false);
            if (manualCard2 != null) manualCard2.SetActive(false);
            if (manualCard3 != null) manualCard3.SetActive(false);
            if (manualCard4 != null) manualCard4.SetActive(false);

            if (dynamicCardContainer != null)
            {
                for (int i = dynamicCardContainer.childCount - 1; i >= 0; i--)
                {
                    Transform child = dynamicCardContainer.GetChild(i);
                    if (child != null) Destroy(child.gameObject);
                }
            }

            TMP_FontAsset font = titleTMP != null ? titleTMP.font : null;

            switch (cardIndex)
            {
                case 0:
                    SetHeader("One Beat, One Vowel Sound", "A syllable is a part of a word with one vowel sound, said as one beat.\neat has 1 beat. eat-ing has 2 beats!");
                    if (manualCard1 != null) manualCard1.SetActive(true);
                    else RenderCard1(font);
                    break;

                case 1:
                    SetHeader("Counting the Beats (Syllabic Division)", "Tap any row to hear its beat separation:");
                    if (manualCard2 != null)
                    {
                        manualCard2.SetActive(true);
                        BindCategoryButtons(manualCard2);
                    }
                    else RenderCard2(font);
                    break;

                case 2:
                    SetHeader("6 Ways to Split a Word", "Learn the 6 rules to divide syllables accurately:");
                    if (manualCard3 != null) manualCard3.SetActive(true);
                    else RenderCard3(font);
                    break;

                case 3:
                    SetHeader("The Rule Behind All Rules", "Count the vowel sounds — NOT the vowel letters!\n'cake' has 2 vowel letters, but only 1 vowel sound (1 beat).");
                    if (manualCard4 != null) manualCard4.SetActive(true);
                    else RenderCard4(font);
                    break;
            }

            AudioClip voice = GetVoiceAClip(cardIndex);
            float duration = 2.5f; // Fallback duration
            if (voice != null)
            {
                duration = voice.length;
                if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(voice);
                }
            }

            // Show Next Button after narration finishes
            StartCoroutine(ShowNextButtonAfterNarration(duration));
        }

        private IEnumerator ShowNextButtonAfterNarration(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (nextCardButton != null)
            {
                nextCardButton.gameObject.SetActive(true);
                nextCardButton.transform.SetAsLastSibling();
                StartCoroutine(PunchScale(nextCardButton.transform));
            }
        }

        private void SetHeader(string title, string desc)
        {
            if (titleTMP != null) titleTMP.text = $"<b>{title}</b>";
            if (descriptionTMP != null) descriptionTMP.text = $"<color=#FFFFFF><b>{desc}</b></color>";
        }

        private AudioClip GetVoiceAClip(int index)
        {
            switch (index)
            {
                case 0: return voiceACard1;
                case 1: return voiceACard2;
                case 2: return voiceACard3;
                case 3: return voiceACard4;
                default: return null;
            }
        }

        private void RenderCard1(TMP_FontAsset font)
        {
            Transform existing = dynamicCardContainer.Find("Card1_Display");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            GameObject card = new GameObject("Card1_Display", typeof(RectTransform));
            card.transform.SetParent(dynamicCardContainer, false);
            SetFullStretch(card.GetComponent<RectTransform>());

            HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.padding = new RectOffset(20, 20, 15, 15);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            CreateBeatCardBox(card.transform, "eat", "1 Vowel Sound", "1 Beat (Monosyllabic)", new Color(0.92f, 0.98f, 1f, 0.95f), new Color(0.08f, 0.25f, 0.55f), font);
            CreateBeatCardBox(card.transform, "eat - ing", "2 Vowel Sounds", "2 Beats (Disyllabic)", new Color(0.92f, 1f, 0.92f, 0.95f), new Color(0.08f, 0.45f, 0.2f), font);
        }

        private void RenderCard2(TMP_FontAsset font)
        {
            Transform existing = dynamicCardContainer.Find("Card2_Display");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            GameObject card = new GameObject("Card2_Display", typeof(RectTransform));
            card.transform.SetParent(dynamicCardContainer, false);
            SetFullStretch(card.GetComponent<RectTransform>());

            GridLayoutGroup glg = card.AddComponent<GridLayoutGroup>();
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;
            glg.cellSize = new Vector2(430, 260);
            glg.spacing = new Vector2(25, 20);
            glg.padding = new RectOffset(15, 15, 15, 15);
            glg.childAlignment = TextAnchor.MiddleCenter;

            CreateCategoryRowButton(card.transform, "Monosyllabic", "1 Syllable", "pen", audioPen, font);
            CreateCategoryRowButton(card.transform, "Disyllabic", "2 Syllables", "pen - cil", audioPencil, font);
            CreateCategoryRowButton(card.transform, "Trisyllabic", "3 Syllables", "re - mem - ber", audioRemember, font);
            CreateCategoryRowButton(card.transform, "Polysyllabic", "4+ Syllables", "ther - mo - me - ter", audioThermometer, font);
        }

        private void RenderCard3(TMP_FontAsset font)
        {
            GameObject card = new GameObject("Card3_Display", typeof(RectTransform));
            card.transform.SetParent(dynamicCardContainer, false);
            SetFullStretch(card.GetComponent<RectTransform>());

            // 2 Columns x 3 Rows gives plenty of room for long text like "Rule 4: Two Consonants (VCCV)"
            GridLayoutGroup glg = card.AddComponent<GridLayoutGroup>();
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.cellSize = new Vector2(880, 140);
            glg.spacing = new Vector2(40, 20);
            glg.padding = new RectOffset(60, 60, 25, 25);
            glg.childAlignment = TextAnchor.MiddleCenter;

            CreateRuleBox(card.transform, "1. One Vowel Sound", "Every syllable needs exactly 1 vowel sound", new Color(0.92f, 0.98f, 1f, 0.95f), new Color(0.08f, 0.35f, 0.7f), font);
            CreateRuleBox(card.transform, "2. Double Consonants", "but | ter   •   rab | bit", new Color(1f, 0.95f, 0.9f, 0.95f), new Color(0.7f, 0.3f, 0.05f), font);
            CreateRuleBox(card.transform, "3. Compound Words", "mail | box   •   dog | house", new Color(0.92f, 1f, 0.92f, 0.95f), new Color(0.08f, 0.5f, 0.2f), font);
            CreateRuleBox(card.transform, "4. Two Consonants (VCCV)", "el | bow   •   doc | tor", new Color(0.96f, 0.92f, 1f, 0.95f), new Color(0.45f, 0.15f, 0.65f), font);
            CreateRuleBox(card.transform, "5. Consonant + le", "tur | tle   •   cra | dle", new Color(1f, 0.98f, 0.88f, 0.95f), new Color(0.6f, 0.45f, 0.05f), font);
            CreateRuleBox(card.transform, "6. Prefix / Suffix", "pre | view   •   kind | ness", new Color(0.9f, 0.98f, 0.98f, 0.95f), new Color(0.05f, 0.45f, 0.5f), font);
        }

        private void RenderCard4(TMP_FontAsset font)
        {
            GameObject card = new GameObject("Card4_Display", typeof(RectTransform));
            card.transform.SetParent(dynamicCardContainer, false);
            SetFullStretch(card.GetComponent<RectTransform>());

            HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 40;
            hlg.padding = new RectOffset(60, 60, 30, 30);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            CreateBeatCardBox(card.transform, "c a k e", "2 Vowel Letters ('a', 'e')", "Only 1 Vowel Sound! (Silent 'e' -> 1 Beat)", new Color(1f, 0.95f, 0.9f, 0.95f), new Color(0.6f, 0.25f, 0.05f), font);
            CreateBeatCardBox(card.transform, "b a n a n a", "3 Vowel Sounds ('ba-na-na')", "3 Beats (Trisyllabic)", new Color(0.92f, 0.98f, 1f, 0.95f), new Color(0.08f, 0.35f, 0.6f), font);
        }

        private Sprite GetRoundedSprite()
        {
            if (generatedRoundedSprite != null) return generatedRoundedSprite;

            int size = 128;
            int radius = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0;
                    float dy = 0;

                    if (x < radius) dx = radius - x;
                    else if (x >= size - radius) dx = x - (size - radius - 1);

                    if (y < radius) dy = radius - y;
                    else if (y >= size - radius) dy = y - (size - radius - 1);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius)
                    {
                        colors[y * size + x] = Color.clear;
                    }
                    else if (dist > radius - 1.5f)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
                        colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.white;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            generatedRoundedSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius)
            );

            return generatedRoundedSprite;
        }

        private void CreateBeatCardBox(Transform parent, string word, string subtitle, string rule, Color bgColor, Color themeColor, TMP_FontAsset font)
        {
            GameObject box = new GameObject(word, typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);
            Image img = box.GetComponent<Image>();
            
            img.sprite = roundedCardFrameBlue ?? GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = (roundedCardFrameBlue != null) ? Color.white : bgColor;

            VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(30, 30, 24, 24);
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            CreateTMPText(box.transform, $"<b><color=#000000><size=240%>{word}</size></color></b>", font);
            CreateTMPText(box.transform, $"<b><color=#{ColorUtility.ToHtmlStringRGB(themeColor)}><size=140%>{subtitle}</size></color></b>", font);
            CreateTMPText(box.transform, $"<b><color=#111118><size=125%>{rule}</size></color></b>", font);
        }

        private void CreateCategoryRowButton(Transform parent, string category, string syllableNote, string example, AudioClip clip, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject(category, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.GetComponent<Image>();

            img.sprite = roundedPillSprite ?? GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = (roundedPillSprite != null) ? Color.white : new Color(0.92f, 0.96f, 1f, 0.95f);

            VerticalLayoutGroup vlg = btnObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(18, 18, 16, 16);
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            CreateTMPText(btnObj.transform, $"<color=#0A2E6B><b><size=150%>{category}</size></b></color>", font);
            CreateTMPText(btnObj.transform, $"<color=#1C4E80><size=120%><b>{syllableNote}</b></size></color>", font);
            CreateTMPText(btnObj.transform, $"<color=#000000><size=165%><b><i>\"{example}\"</i></b></size></color>", font);

            Button b = btnObj.GetComponent<Button>();
            b.onClick.AddListener(() => {
                StartCoroutine(PunchScale(btnObj.transform));
                if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    if (clip != null) U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
                    else U3_SA_AudioManager_Masters_Phonics.Instance.PlayBeat();
                }
            });
        }

        private void BindCategoryButtons(GameObject cardObj)
        {
            if (cardObj == null) return;
            Button[] buttons = cardObj.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                string bName = btn.name.ToLower();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    StartCoroutine(PunchScale(btn.transform));
                    if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
                    {
                        AudioClip c = (bName.Contains("mono") || bName.Contains("pen")) ? audioPen :
                                      (bName.Contains("di") || bName.Contains("pencil")) ? audioPencil :
                                      (bName.Contains("tri") || bName.Contains("remember")) ? audioRemember :
                                      (bName.Contains("poly") || bName.Contains("thermo")) ? audioThermometer : null;
                        if (c != null) U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(c);
                        else U3_SA_AudioManager_Masters_Phonics.Instance.PlayBeat();
                    }
                });
            }
        }

        private void CreateRuleBox(Transform parent, string ruleTitle, string example, Color boxColor, Color titleColor, TMP_FontAsset font)
        {
            GameObject box = new GameObject(ruleTitle, typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);
            Image img = box.GetComponent<Image>();

            img.sprite = roundedFrameOrange ?? GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = boxColor;

            VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 16, 16);
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            CreateTMPText(box.transform, $"<color=#{ColorUtility.ToHtmlStringRGB(titleColor)}><b><size=125%>{ruleTitle}</size></b></color>", font);
            CreateTMPText(box.transform, $"<color=#000000><b><size=140%>{example}</size></b></color>", font);
        }

        private void CreateTMPText(Transform parent, string text, TMP_FontAsset font)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private IEnumerator PunchScale(Transform tr)
        {
            Vector3 orig = Vector3.one;
            tr.localScale = orig * 1.15f;
            yield return new WaitForSeconds(0.12f);
            tr.localScale = orig;
        }

        public void NextCard()
        {
            currentCardIndex++;
            if (currentCardIndex < 4)
            {
                ShowCard(currentCardIndex);
            }
            else
            {
                if (nextCardButton != null)
                {
                    nextCardButton.gameObject.SetActive(false);
                }
                OnConceptCardsComplete();
            }
        }

        public void ReplayAudio()
        {
            AudioClip voice = GetVoiceAClip(currentCardIndex);
            if (U3_SA_AudioManager_Masters_Phonics.Instance != null && voice != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(voice);
            }
        }

        private void OnConceptCardsComplete()
        {
            if (U3_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U3_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }
        }
    }
}
