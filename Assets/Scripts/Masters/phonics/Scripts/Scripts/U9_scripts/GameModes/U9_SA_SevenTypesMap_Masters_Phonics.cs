using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    [Serializable]
    public class U9MapTileData
    {
        public string typeName;
        public string pattern;
        public string exampleWords;
        public int taughtInUnit;
        public bool isUnlocked;
        public bool isNewInThisUnit;
        public string colorHex;
    }

    public class U9_SA_SevenTypesMap_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Header")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;

        [Header("2. 7 Map Tiles Container")]
        [SerializeField] private Transform tilesContainer;

        [Header("3. Audio & Narration")]
        [Tooltip("Unit 9 Map Audio")]
        public AudioClip mapIntroClip;
        [SerializeField] private Button replayAudioButton;

        [Header("4. Next / Continue")]
        [SerializeField] private Button continueBtn;

        [Header("5. HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        private List<U9MapTileData> mapTiles;
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
            UpdateHUD();
            PlayMapIntroAudio();
        }

        public void PlayMapIntroAudio()
        {
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayCelebration();
                
                if (mapIntroClip != null)
                {
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(mapIntroClip, OnNarrationComplete);
                }
                else
                {
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_map_complete", OnNarrationComplete);
                }
            }
            else
            {
                OnNarrationComplete();
            }
        }

        private void OnNarrationComplete()
        {
            if (continueBtn != null)
            {
                continueBtn.gameObject.SetActive(true);
                continueBtn.transform.SetAsLastSibling();
                StartCoroutine(CoPulseButton(continueBtn.transform));
            }

            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.SetNextActivityButtonActive(true);
            }
        }

        private IEnumerator CoPulseButton(Transform tr)
        {
            if (tr == null) yield break;
            Vector3 baseScale = Vector3.one;
            while (gameObject.activeInHierarchy)
            {
                for (float t = 0; t < 1f; t += Time.deltaTime * 2.5f)
                {
                    if (tr == null) yield break;
                    float s = Mathf.Sin(t * Mathf.PI);
                    tr.localScale = baseScale * (1f + 0.06f * s);
                    yield return null;
                }
                yield return new WaitForSeconds(0.8f);
            }
            if (tr != null) tr.localScale = baseScale;
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;

            if (titleTMP == null)
            {
                var t = root.Find("Title BG/TitleText") ?? root.Find("TitleText") ?? root.Find("Title_Text") ?? root.Find("Header_Container/Title") ?? root.Find("HeaderRibbon/Title") ?? root.Find("Title");
                if (t != null) titleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (subtitleTMP == null)
            {
                var t = root.Find("prompt bg/Prompt_Text") ?? root.Find("Prompt_Text") ?? root.Find("Subtitle_Text") ?? root.Find("Header_Container/Subtitle") ?? root.Find("HeaderRibbon/Subtitle") ?? root.Find("SubtitleText") ?? root.Find("Subtitle");
                if (t != null) subtitleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (tilesContainer == null)
            {
                Transform t = root.Find("Tiles_Container") ?? root.Find("Grid") ?? root.Find("MapGrid") ?? root.Find("TilesContainer") ?? root.Find("Cards");
                if (t != null) tilesContainer = t;
            }

            if (replayAudioButton == null)
            {
                Transform r = root.Find("ReplayAudioBtn") ?? root.Find("ReplayButton") ?? root.Find("Audio_Button") ?? root.Find("Speaker_Button") ?? root.Find("ReplayBtn");
                if (r != null) replayAudioButton = r.GetComponent<Button>();
            }

            if (continueBtn == null)
            {
                Transform c = root.Find("ContinueButton") ?? root.Find("ContinueBtn") ?? root.Find("NextButton") ?? root.Find("NextBtn") ?? root.Find("DoneBtn") ?? root.Find("BottomNav/NextBtn");
                if (c != null) continueBtn = c.GetComponent<Button>();
            }

            if (progressText == null)
            {
                Transform pt = root.Find("ProgressHUD/Progress_Text")
                            ?? root.Find("ProgressHUD/ProgressText")
                            ?? root.Find("ProgressText")
                            ?? root.Find("HUD/ProgressText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            if (scoreText == null)
            {
                Transform st = root.Find("ProgressHUD/Score_Text")
                            ?? root.Find("ProgressHUD/ScoreText")
                            ?? root.Find("ScoreText")
                            ?? root.Find("HUD/ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }

            AttachListeners();
        }

        private void AttachListeners()
        {
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(() =>
                {
                    U9_SA_AudioManager_Masters_Phonics.Instance?.PlayClick();
                    PlayMapIntroAudio();
                });
            }

            if (continueBtn != null)
            {
                continueBtn.onClick.RemoveAllListeners();
                continueBtn.onClick.AddListener(OnContinueClicked);
            }
        }

        private void InitializeMapData()
        {
            mapTiles = new List<U9MapTileData>
            {
                new U9MapTileData { typeName = "1. Closed", pattern = "CVC", exampleWords = "cat, bed, fin, hop, tub", taughtInUnit = 1, isUnlocked = true, isNewInThisUnit = false, colorHex = "#1D4ED8" },
                new U9MapTileData { typeName = "2. Open", pattern = "CV", exampleWords = "he, me, go, hi, no", taughtInUnit = 3, isUnlocked = true, isNewInThisUnit = false, colorHex = "#047857" },
                new U9MapTileData { typeName = "3. Magic / Silent E", pattern = "VCe", exampleWords = "cake, bike, home, flute", taughtInUnit = 6, isUnlocked = true, isNewInThisUnit = false, colorHex = "#6D28D9" },
                new U9MapTileData { typeName = "4. Vowel Team", pattern = "VV (Steady)", exampleWords = "rain, boat, see, green", taughtInUnit = 7, isUnlocked = true, isNewInThisUnit = false, colorHex = "#B45309" },
                new U9MapTileData { typeName = "5. Diphthong", pattern = "VV (Gliding)", exampleWords = "coin, boy, house, cow", taughtInUnit = 7, isUnlocked = true, isNewInThisUnit = false, colorHex = "#BE185D" },
                new U9MapTileData { typeName = "6. R-Controlled", pattern = "Vr", exampleWords = "car, bird, corn, star, fern", taughtInUnit = 8, isUnlocked = true, isNewInThisUnit = false, colorHex = "#D97706" },
                new U9MapTileData { typeName = "7. Consonant-le", pattern = "-le", exampleWords = "table, little, candle, turtle", taughtInUnit = 9, isUnlocked = true, isNewInThisUnit = true, colorHex = "#0D9488" }
            };
        }

        public void RenderMapTiles()
        {
            if (tilesContainer == null) return;

            if (titleTMP != null)
                titleTMP.text = "<b>7 SYLLABLE TYPES MAP</b>";
            if (subtitleTMP != null)
                subtitleTMP.text = "<b><color=#10B981>ALL 7 of 7 Syllable Types Mastered!</color>   <color=#FBBF24>Consonant + le Unlocked — Complete Syllable Mastery!</color></b>";

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
                if (child.name.StartsWith("Tile_") || child.name.StartsWith("Card_") || child.name.StartsWith("MapTile_"))
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
                if (tile.isNewInThisUnit)
                    img.color = new Color(0.99f, 0.98f, 0.92f, 1f); // Warm ivory gold card for new unlock
                else
                    img.color = new Color(0.96f, 0.98f, 1f, 1f);    // Crisp ice-white card for passed units

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

                string badge = tile.isNewInThisUnit 
                    ? "<color=#D97706><b> [NEW IN UNIT 9]</b></color>" 
                    : "<color=#059669><b> [PASSED]</b></color>";

                tmp.text = $"<b><size=30><color={tile.colorHex}>{tile.typeName}</color></size></b> <size=20>{badge}</size>\n" +
                           $"<size=24><color=#1E293B>Pattern: <b>{tile.pattern}</b></color></size>\n" +
                           $"<size=22><color=#0369A1><i>Examples: {tile.exampleWords}</i></color></size>";

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
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayCorrect();
        }

        private void UpdateHUD()
        {
            if (progressText != null)
            {
                progressText.text = "<b>Grand Finale: All 7 Mastered!</b>";
            }

            if (progressBar != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
                progressBar.value = 1.0f;
            }

            if (scoreText != null && U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                scoreText.text = $"<b>Score: {U9_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore()}</b>";
            }
        }

        private void OnContinueClicked()
        {
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                U9_SA_AudioManager_Masters_Phonics.Instance.StopAll();
            }

            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.OpenCompletionPanel();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 9 Seven Types Map] Auto-Assigned Map Grid & Finale Button!</b></color>");
        }
#endif
    }
}
