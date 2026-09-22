using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U4_SA_GM01_ConceptCards_Masters_Phonics : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI descriptionTMP;
        [SerializeField] private Transform dynamicCardContainer;
        [SerializeField] private Button replayVoiceAButton;
        [SerializeField] private Button nextCardButton;

        [Header("Audio Clips (Official Unit 4 Doc)")]
        public AudioClip card1VoiceClip; // U04_VO_card1
        public AudioClip card2VoiceClip; // U04_VO_card2
        public AudioClip card3VoiceClip; // U04_VO_card3
        public AudioClip card4VoiceClip; // U04_VO_card4
        public AudioClip card5VoiceClip; // U04_VO_card5

        private int currentCardIndex = 0;
        private const int TOTAL_CARDS = 5;
        private Sprite proceduralRoundedSprite;

        private void Awake()
        {
            AutoBindUI();
        }

        private void Start()
        {
            AutoBindUI();
        }

        private void OnEnable()
        {
            AutoBindUI();
            currentCardIndex = 0;
            ShowCard(currentCardIndex);
        }

        private void AutoBindUI()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("Title BG/TitleText") ?? transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Header_Title");
                if (t != null) titleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (descriptionTMP == null)
            {
                var t = transform.Find("DescriptionText") ?? transform.Find("prompt bg/Prompt_Text") ?? transform.Find("Prompt_Text") ?? transform.Find("Subtitle");
                if (t != null) descriptionTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (dynamicCardContainer == null)
            {
                Transform d = transform.Find("DynamicCardContainer") ?? transform.Find("Cards_Container") ?? transform.Find("CardContainer") ?? transform.Find("Center_Card");
                if (d != null) dynamicCardContainer = d;
            }

            if (nextCardButton == null)
            {
                Transform existing = transform.Find("NextButton") ?? transform.Find("Next_Button") ?? transform.Find("Btn_Next");
                if (existing != null) nextCardButton = existing.GetComponent<Button>();
            }

            if (nextCardButton != null)
            {
                nextCardButton.gameObject.SetActive(true);
                nextCardButton.onClick.RemoveAllListeners();
                nextCardButton.onClick.AddListener(NextCard);
            }

            if (replayVoiceAButton == null)
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
                            replayVoiceAButton = b;
                            break;
                        }
                    }
                }
                else
                {
                    replayVoiceAButton = r.GetComponent<Button>();
                }
            }
            if (replayVoiceAButton != null)
            {
                replayVoiceAButton.onClick.RemoveAllListeners();
                replayVoiceAButton.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(replayVoiceAButton.transform, 1.15f));
                    ReplayCurrentCardAudio();
                });
            }
        }

        public void ShowCard(int index)
        {
            currentCardIndex = index;
            ClearCardContainer();

            switch (index)
            {
                case 0:
                    RenderCard1();
                    break;
                case 1:
                    RenderCard2();
                    break;
                case 2:
                    RenderCard3();
                    break;
                case 3:
                    RenderCard4();
                    break;
                case 4:
                    RenderCard5();
                    break;
            }

            AudioClip clip = GetCardAudio(index);
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null && clip != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
            }
        }

        private void ClearCardContainer()
        {
            if (dynamicCardContainer == null) return;
            for (int i = dynamicCardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(dynamicCardContainer.GetChild(i).gameObject);
            }
        }

        // -------------------------------------------------------------
        // CARD 1: The Three Golden Rules (Page 23)
        // -------------------------------------------------------------
        private void RenderCard1()
        {
            SetHeader("ONE VOWEL SOUND, ONE SYLLABLE", "Count the vowel sounds to count the syllables!");

            GameObject box = CreateCardBase(dynamicCardContainer);
            CreateInteractiveCard(box.transform, "cat", "1 VOWEL SOUND", "1 Beat (Never Divided)", "#0288D1", "cat");
            CreateInteractiveCard(box.transform, "nap · kin", "2 VOWEL SOUNDS", "2 Beats (nap / kin)", "#2E7D32", "napkin");
            CreateInteractiveCard(box.transform, "strength", "1 VOWEL SOUND", "1 Beat (Never Splits)", "#E65100", "strength");
        }

        // -------------------------------------------------------------
        // CARD 2: The 7 Syllable Types Map (Page 23)
        // -------------------------------------------------------------
        private void RenderCard2()
        {
            SetHeader("THE 7 SYLLABLE TYPES", "Tap any card to hear the example word:");

            GameObject grid = CreateCardBase(dynamicCardContainer);
            CreateTypePill(grid.transform, "Closed", "cat", "v c", "#2E7D32");
            CreateTypePill(grid.transform, "Open", "tiger", "v", "#1565C0");
            CreateTypePill(grid.transform, "Magic 'e'", "bake", "v c e", "#7B1FA2");
            CreateTypePill(grid.transform, "Vowel Team", "team", "v v", "#E65100");
            CreateTypePill(grid.transform, "r-Controlled", "car", "v r", "#C2185B");
            CreateTypePill(grid.transform, "Diphthong", "boil", "v v", "#00796B");
            CreateTypePill(grid.transform, "Consonant + le", "bubble", "c + le", "#5D4037");
        }

        // -------------------------------------------------------------
        // CARD 3: Strong Beat vs Weak Beat (Page 24)
        // -------------------------------------------------------------
        private void RenderCard3()
        {
            SetHeader("STRONG BEAT, WEAK BEAT (WORD STRESS)", "One beat is always louder and stronger!");

            GameObject box = CreateCardBase(dynamicCardContainer);
            CreateStressCard(box.transform, "RAB - bit", "Accented Beat (Loud)", "1st beat is loud and strong", true, "rabbit");
            CreateStressCard(box.transform, "a - GO", "Unaccented Beat (Quiet)", "2nd beat is loud and strong", false, "ago");
        }

        // -------------------------------------------------------------
        // CARD 4: The Schwa Sound (Page 24)
        // -------------------------------------------------------------
        private void RenderCard4()
        {
            SetHeader("THE SCHWA /e/ - THE LAZIEST SOUND", "Unaccented vowels weaken to a lazy 'uh' sound:");

            GameObject box = CreateCardBase(dynamicCardContainer);
            CreateSchwaExampleCard(box.transform, "ago", "sounds like \"uh-go\"", "The 'a' goes lazy", "ago");
            CreateSchwaExampleCard(box.transform, "bacon", "sounds like \"bac-un\"", "The 'o' goes lazy", "bacon");
            CreateSchwaExampleCard(box.transform, "camel", "sounds like \"cam-ul\"", "The 'e' goes lazy", "camel");
        }

        // -------------------------------------------------------------
        // CARD 5: Two Clues to Spot a Schwa (Page 25)
        // -------------------------------------------------------------
        private void RenderCard5()
        {
            SetHeader("TWO CLUES TO SPOT A SCHWA", "Tap the words to hear the lazy sound in action:");

            GameObject box = CreateCardBase(dynamicCardContainer);
            CreateClueBox(box.transform, "CLUE 1: BEFORE FINAL L", "Vowels right before an 'L' at the end weaken to schwa:", new[] { "camel", "dental", "pencil" }, "#0288D1");
            CreateClueBox(box.transform, "CLUE 2: NOTHING ELSE FITS", "If short and long sounds do not fit, it is a schwa:", new[] { "zebra", "banana" }, "#E65100");
        }

        // -------------------------------------------------------------
        // Visual Card Builders
        // -------------------------------------------------------------
        private void SetHeader(string title, string desc)
        {
            if (titleTMP != null)
            {
                titleTMP.text = $"<b>{title}</b>";
                titleTMP.fontSize = 46;
            }
            if (descriptionTMP != null)
            {
                descriptionTMP.text = $"<color=#FFFFFF><b>{desc}</b></color>";
                descriptionTMP.fontSize = 32;
            }
        }

        private GameObject CreateCardBase(Transform parent)
        {
            GameObject container = new GameObject("CardGrid", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var hlg = container.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            return container;
        }

        private void CreateInteractiveCard(Transform parent, string word, string rule1, string rule2, string colorHex, string audioWord)
        {
            GameObject card = new GameObject("Card_" + word, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            var img = card.GetComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var tmp = CreateTMP(card.transform);
            tmp.text = $"<b><size=62><color={colorHex}>{word}</color></size></b>\n\n<size=38><color=#212121><b>{rule1}</b></color>\n<size=30><color=#616161>{rule2}</size></color></size>\n\n<size=28><color=#0288D1><b>[ Tap to Listen ]</b></color></size>";

            var btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                PlayWordAudio(audioWord);
                StartCoroutine(PunchScale(card.transform, 1.08f));
            });
        }

        private void CreateTypePill(Transform parent, string typeName, string example, string pattern, string colorHex)
        {
            GameObject pill = new GameObject("Pill_" + typeName, typeof(RectTransform), typeof(Image), typeof(Button));
            pill.transform.SetParent(parent, false);
            var img = pill.GetComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var tmp = CreateTMP(pill.transform);
            tmp.text = $"<b><size=38><color={colorHex}>{typeName}</color></size></b>\n<size=30><color=#616161>({pattern})</color></size>\n\n<b><size=46><color=#000000>{example}</color></size></b>\n\n<size=24><color=#0288D1><b>[ Listen ]</b></color></size>";

            var btn = pill.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                PlayWordAudio(example);
                StartCoroutine(PunchScale(pill.transform, 1.12f));
            });
        }

        private void CreateStressCard(Transform parent, string wordDisplay, string stressType, string desc, bool isFirstStress, string audioWord)
        {
            GameObject card = new GameObject("StressCard_" + audioWord, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            var img = card.GetComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = isFirstStress ? new Color(0.95f, 1f, 0.95f) : new Color(0.95f, 0.98f, 1f);

            var tmp = CreateTMP(card.transform);
            string col = isFirstStress ? "#2E7D32" : "#1565C0";
            tmp.text = $"<b><size=72><color={col}>{wordDisplay}</color></size></b>\n\n<b><size=42><color=#000000>{stressType}</color></size></b>\n<size=32><color=#424242>{desc}</color></size>\n\n<size=28><color={col}><b>[ Tap to Hear Beat ]</b></color></size>";

            var btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                PlayWordAudio(audioWord);
                StartCoroutine(PunchScale(card.transform, 1.08f));
            });
        }

        private void CreateSchwaExampleCard(Transform parent, string word, string respelling, string note, string audioWord)
        {
            GameObject card = new GameObject("Schwa_" + word, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            var img = card.GetComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var tmp = CreateTMP(card.transform);
            tmp.text = $"<b><size=64><color=#000000>{word}</color></size></b>\n<size=46><b><color=#E65100>{respelling}</color></b></size>\n\n<size=34><color=#424242><b>{note}</b></color></size>\n\n<size=28><color=#0288D1><b>[ Tap to Listen ]</b></color></size>";

            var btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                PlayWordAudio(audioWord);
                StartCoroutine(PunchScale(card.transform, 1.08f));
            });
        }

        private void CreateClueBox(Transform parent, string clueHeader, string explanation, string[] words, string colorHex)
        {
            GameObject box = new GameObject("ClueBox", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);
            var img = box.GetComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 16, 16);
            vlg.spacing = 16;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Header Text
            GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerObj.transform.SetParent(box.transform, false);
            var headerTMP = headerObj.GetComponent<TextMeshProUGUI>();
            headerTMP.text = $"<b><size=38><color={colorHex}>{clueHeader}</color></size></b>\n<size=28><color=#333333><b>{explanation}</b></color></size>";
            headerTMP.alignment = TextAlignmentOptions.Center;

            // Words Row
            GameObject row = new GameObject("WordsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(box.transform, false);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            foreach (var w in words)
            {
                GameObject btnObj = new GameObject("Btn_" + w, typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(row.transform, false);
                var btnImg = btnObj.GetComponent<Image>();
                btnImg.sprite = GetRoundedSprite();
                btnImg.type = Image.Type.Sliced;
                btnImg.color = new Color(0.92f, 0.96f, 1f);
                var btnRT = btnObj.GetComponent<RectTransform>();
                btnRT.sizeDelta = new Vector2(170, 75);

                var wTMP = CreateTMP(btnObj.transform);
                wTMP.text = $"<b><size=40><color=#000000>{w}</color></size></b>";

                var btn = btnObj.GetComponent<Button>();
                string wordToPlay = w;
                btn.onClick.AddListener(() =>
                {
                    PlayWordAudio(wordToPlay);
                    StartCoroutine(PunchScale(btnObj.transform, 1.15f));
                });
            }
        }

        private TextMeshProUGUI CreateTMP(Transform parent)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(parent, false);
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(8, 8);
            textRT.offsetMax = new Vector2(-8, -8);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void PlayWordAudio(string word)
        {
            if (string.IsNullOrEmpty(word)) return;
            word = word.Trim();

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                // Exact search across all potential project audio paths
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

        private Sprite GetRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 28;
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

        public void NextCard()
        {
            if (currentCardIndex < TOTAL_CARDS - 1)
            {
                currentCardIndex++;
                ShowCard(currentCardIndex);
            }
            else
            {
                if (U4_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                {
                    U4_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
                }
            }
        }

        public void ReplayCurrentCardAudio()
        {
            AudioClip clip = GetCardAudio(currentCardIndex);
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null && clip != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
            }
        }

        private AudioClip GetCardAudio(int idx)
        {
            switch (idx)
            {
                case 0: return card1VoiceClip ?? Resources.Load<AudioClip>("U4_audio/You know this one already Every syllable has");
                case 1: return card2VoiceClip ?? Resources.Load<AudioClip>("U4_audio/There are seven kinds of syllable and youll");
                case 2: return card3VoiceClip ?? Resources.Load<AudioClip>("U4_audio/Say rabbit RABbit One beat is louder than");
                case 3: return card4VoiceClip ?? Resources.Load<AudioClip>("U4_audio/Heres the secret When a beat is quiet");
                case 4: return card5VoiceClip ?? Resources.Load<AudioClip>("U4_audio/Two clues for finding it One the vowel");
                default: return null;
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
    }
}
