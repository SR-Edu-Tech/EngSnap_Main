using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    [Serializable]
    public class U8MapTileData
    {
        public string typeName;
        public string pattern;
        public string exampleWords;
        public int taughtInUnit;
        public bool isUnlocked;
        public bool isNewInThisUnit;
        public string colorHex;
    }

    public class U8_SA_SevenTypesMap_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Header")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;

        [Header("2. 7 Map Tiles Container")]
        [SerializeField] private Transform tilesContainer;

        [Header("3. Audio & Narration")]
        [Tooltip("Unit 8 Map Audio")]
        public AudioClip mapIntroClip;
        [SerializeField] private Button replayAudioButton;

        [Header("4. Next / Continue")]
        [SerializeField] private Button continueBtn;

        private List<U8MapTileData> mapTiles;
        private static Sprite proceduralRoundedSprite = null;

        public static Sprite GetOrCreateRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 24;
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
            InitializeMapData();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            InitializeMapData();
            RenderMapTiles();
            PlayMapIntroAudio();
        }

        public void PlayMapIntroAudio()
        {
            AudioClip clip = mapIntroClip 
                          ?? U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_unit_complete") 
                          ?? U8_SA_AudioManager_Masters_Phonics.ResolveAudio("Seven Syllable Types");
            if (clip != null)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(clip);
            }
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;

            if (titleTMP == null)
            {
                var t = root.Find("Title BG/TitleText") ?? root.Find("TitleText") ?? root.Find("Header_Container/Title") ?? root.Find("HeaderRibbon/Title") ?? root.Find("Title");
                if (t != null) titleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (subtitleTMP == null)
            {
                var t = root.Find("prompt bg/Prompt_Text") ?? root.Find("Prompt_Text") ?? root.Find("Header_Container/Subtitle") ?? root.Find("HeaderRibbon/Subtitle") ?? root.Find("SubtitleText") ?? root.Find("Subtitle");
                if (t != null) subtitleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (tilesContainer == null)
            {
                Transform t = root.Find("Tiles_Container") ?? root.Find("Grid") ?? root.Find("MapGrid") ?? root.Find("TilesContainer") ?? root.Find("Cards");
                if (t != null) tilesContainer = t;
            }

            if (replayAudioButton == null)
            {
                Transform r = root.Find("ReplayAudioBtn") ?? root.Find("ReplayButton") ?? root.Find("Audio_Button") ?? root.Find("Speaker_Button");
                if (r != null) replayAudioButton = r.GetComponent<Button>();
            }

            if (continueBtn == null)
            {
                Transform c = root.Find("ContinueButton") ?? root.Find("ContinueBtn") ?? root.Find("NextButton") ?? root.Find("NextBtn") ?? root.Find("DoneBtn") ?? root.Find("BottomNav/NextBtn");
                if (c != null) continueBtn = c.GetComponent<Button>();
            }

            AttachListeners();
        }

        private void AttachListeners()
        {
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(PlayMapIntroAudio);
            }

            if (continueBtn != null)
            {
                continueBtn.onClick.RemoveAllListeners();
                continueBtn.onClick.AddListener(OnContinueClicked);
            }
        }

        private void InitializeMapData()
        {
            mapTiles = new List<U8MapTileData>
            {
                new U8MapTileData { typeName = "1. Closed", pattern = "CVC", exampleWords = "cat, bed, fin, hop, tub", taughtInUnit = 1, isUnlocked = true, isNewInThisUnit = false, colorHex = "#1D4ED8" },
                new U8MapTileData { typeName = "2. Open", pattern = "CV", exampleWords = "he, me, go, hi, no", taughtInUnit = 3, isUnlocked = true, isNewInThisUnit = false, colorHex = "#047857" },
                new U8MapTileData { typeName = "3. Magic / Silent E", pattern = "VCe", exampleWords = "cake, bike, home, flute", taughtInUnit = 6, isUnlocked = true, isNewInThisUnit = false, colorHex = "#6D28D9" },
                new U8MapTileData { typeName = "4. Vowel Team", pattern = "VV (Steady)", exampleWords = "rain, boat, see, green", taughtInUnit = 7, isUnlocked = true, isNewInThisUnit = false, colorHex = "#B45309" },
                new U8MapTileData { typeName = "5. Diphthong", pattern = "VV (Gliding)", exampleWords = "coin, boy, house, cow", taughtInUnit = 7, isUnlocked = true, isNewInThisUnit = false, colorHex = "#BE185D" },
                new U8MapTileData { typeName = "6. R-Controlled", pattern = "Vr", exampleWords = "car, bird, corn, star, fern", taughtInUnit = 8, isUnlocked = true, isNewInThisUnit = true, colorHex = "#D97706" },
                new U8MapTileData { typeName = "7. Consonant-le", pattern = "-le", exampleWords = "table, little, candle", taughtInUnit = 9, isUnlocked = false, isNewInThisUnit = false, colorHex = "#475569" }
            };
        }

        public void RenderMapTiles()
        {
            if (tilesContainer == null) return;

            if (titleTMP != null)
                titleTMP.text = "<b>7 SYLLABLE TYPES MAP</b>";
            if (subtitleTMP != null)
                subtitleTMP.text = "<b><color=#10B981>6 of 7 Syllable Types Mastered!</color></b>   <color=#FBBF24>Bossy R (R-Controlled Vowels) Unlocked!</color>";

            // Configure container grid layout
            RectTransform containerRT = tilesContainer.GetComponent<RectTransform>();
            if (containerRT != null)
            {
                containerRT.anchorMin = new Vector2(0.5f, 0.5f);
                containerRT.anchorMax = new Vector2(0.5f, 0.5f);
                containerRT.pivot = new Vector2(0.5f, 0.5f);
                containerRT.anchoredPosition = new Vector2(0f, -25f);
                containerRT.sizeDelta = new Vector2(1650f, 430f);
            }

            var glg = tilesContainer.GetComponent<GridLayoutGroup>();
            if (glg == null)
                glg = tilesContainer.gameObject.AddComponent<GridLayoutGroup>();

            glg.cellSize = new Vector2(390f, 180f);
            glg.spacing = new Vector2(24f, 18f);
            glg.childAlignment = TextAnchor.MiddleCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;

            // Remove previous dynamically generated tiles if any
            for (int i = tilesContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = tilesContainer.GetChild(i);
                if (child.name.StartsWith("Tile_") || child.name.StartsWith("Card_"))
                    Destroy(child.gameObject);
            }

            Sprite rounded = GetOrCreateRoundedSprite();

            for (int i = 0; i < mapTiles.Count; i++)
            {
                var tile = mapTiles[i];
                GameObject tileObj = new GameObject($"Tile_{i + 1}_{tile.typeName}", typeof(RectTransform), typeof(Image), typeof(Button));
                tileObj.transform.SetParent(tilesContainer, false);

                RectTransform rt = tileObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(390f, 180f);

                Image img = tileObj.GetComponent<Image>();
                img.sprite = rounded;
                img.type = Image.Type.Sliced;

                // High-contrast clean card backgrounds
                if (tile.isUnlocked)
                {
                    if (tile.isNewInThisUnit)
                        img.color = new Color(0.99f, 0.98f, 0.92f, 1f); // Warm ivory gold card for new unlocks
                    else
                        img.color = new Color(0.96f, 0.98f, 1f, 1f);    // Crisp ice-white card for passed units
                }
                else
                {
                    img.color = new Color(0.91f, 0.93f, 0.96f, 0.95f);  // Subtle locked grey card
                }

                // Inner content text
                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(tileObj.transform, false);
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(18f, 14f);
                textRT.offsetMax = new Vector2(-18f, -14f);

                TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;
                tmp.fontSize = 24f;
                tmp.lineSpacing = 10f;
                tmp.enableWordWrapping = true;

                if (tile.isUnlocked)
                {
                    string badge = tile.isNewInThisUnit 
                        ? "<color=#D97706><b> [NEW IN UNIT 8]</b></color>" 
                        : "<color=#059669><b> [PASSED]</b></color>";

                    tmp.text = $"<b><size=30><color={tile.colorHex}>{tile.typeName}</color></size></b> <size=20>{badge}</size>\n" +
                               $"<size=24><color=#1E293B>Pattern: <b>{tile.pattern}</b></color></size>\n" +
                               $"<size=22><color=#0369A1><i>Examples: {tile.exampleWords}</i></color></size>";
                }
                else
                {
                    tmp.text = $"<b><size=28><color=#475569>{tile.typeName}</color></size></b> <color=#DC2626><size=20><b>[LOCKED]</b></size></color>\n" +
                               $"<size=24><color=#475569>Pattern: <b>{tile.pattern}</b></color></size>\n" +
                               $"<size=22><color=#64748B><i>Unlocks in Unit {tile.taughtInUnit}</i></color></size>";
                }

                // Interactive tap with punch animation
                Button btn = tileObj.GetComponent<Button>();
                int capturedIndex = i;
                btn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(tileObj.transform, 1.08f));
                    OnTileTapped(capturedIndex);
                });
            }
        }

        private IEnumerator PunchScale(Transform tr, float targetScale)
        {
            if (tr == null) yield break;
            Vector3 original = Vector3.one;
            float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                tr.localScale = Vector3.Lerp(original, original * targetScale, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                tr.localScale = Vector3.Lerp(original * targetScale, original, elapsed / duration);
                yield return null;
            }
            tr.localScale = original;
        }

        private void OnTileTapped(int index)
        {
            if (index < 0 || index >= mapTiles.Count) return;
            var tile = mapTiles[index];

            if (!tile.isUnlocked)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayAnswerFeedbackSFX(false);
                return;
            }

            U8_SA_AudioManager_Masters_Phonics.Instance?.PlayAnswerFeedbackSFX(true);
        }

        private void OnContinueClicked()
        {
            if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.OpenCompletionReport();
            }
        }
    }
}
