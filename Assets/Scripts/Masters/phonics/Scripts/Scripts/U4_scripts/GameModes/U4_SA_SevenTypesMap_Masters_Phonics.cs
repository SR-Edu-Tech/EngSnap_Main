using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    [Serializable]
    public class MapTileData
    {
        public SyllableTypeCategory category;
        public string typeName;
        public string pattern;
        public string exampleWords;
        public int taughtInUnit;
        public bool isUnlocked;
        public string colorHex;
    }

    public class U4_SA_SevenTypesMap_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Header")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private Button backButton;

        [Header("2. 7 Map Tiles Container")]
        [SerializeField] private Transform tilesContainer;

        [Header("3. Audio & Narration")]
        [Tooltip("U04_VO_map: 'Seven types. Two done, five to go. Come back here whenever you like.' (6s)")]
        public AudioClip mapIntroClip;
        [SerializeField] private Button replayAudioButton;

        private List<MapTileData> mapTiles;
        private Sprite proceduralRoundedSprite;
        private HashSet<string> tappedCategories = new HashSet<string>();

        private void Awake()
        {
            AutoBindHierarchyElements();
            InitializeMapData();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            InitializeMapData();
            tappedCategories.Clear();
            RenderMapTiles();

            StartCoroutine(EnsureNextButtonVisibleDelayed());

            PlayMapIntroAudio();
        }

        public void PlayMapIntroAudio()
        {
            AudioClip clip = mapIntroClip ?? Resources.Load<AudioClip>("U4_audio/Seven types Two done five to go Come");
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null && clip != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
            }
        }

        private IEnumerator EnsureNextButtonVisibleDelayed()
        {
            yield return new WaitForSeconds(0.1f);
            if (U4_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U4_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }
        }

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("Title BG/TitleText") ?? transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (subtitleTMP == null)
            {
                var t = transform.Find("prompt bg/Prompt_Text") ?? transform.Find("Prompt_Text") ?? transform.Find("SubtitleText") ?? transform.Find("Subtitle");
                if (t != null) subtitleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (tilesContainer == null)
            {
                Transform t = transform.Find("Tiles_Container") ?? transform.Find("MapGrid") ?? transform.Find("Grid") ?? transform.Find("TilesContainer");
                if (t != null) tilesContainer = t;
            }

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
                    PlayMapIntroAudio();
                });
            }
        }

        private void InitializeMapData()
        {
            mapTiles = new List<MapTileData>
            {
                new MapTileData { category = SyllableTypeCategory.Closed, typeName = "Closed", pattern = "v c", exampleWords = "cat · rab-bit · nap-kin", taughtInUnit = 5, isUnlocked = true, colorHex = "#2E7D32" },
                new MapTileData { category = SyllableTypeCategory.Open, typeName = "Open", pattern = "v", exampleWords = "ti-ger · ba-by · pa-per", taughtInUnit = 5, isUnlocked = true, colorHex = "#1565C0" },
                new MapTileData { category = SyllableTypeCategory.MagicE, typeName = "Magic 'e'", pattern = "v c e", exampleWords = "bake · pine · bone", taughtInUnit = 6, isUnlocked = false, colorHex = "#7B1FA2" },
                new MapTileData { category = SyllableTypeCategory.VowelTeam, typeName = "Vowel Team", pattern = "v v", exampleWords = "team · float · seed", taughtInUnit = 7, isUnlocked = false, colorHex = "#E65100" },
                new MapTileData { category = SyllableTypeCategory.RControlled, typeName = "r-Controlled", pattern = "v r", exampleWords = "car · bird · fort", taughtInUnit = 8, isUnlocked = false, colorHex = "#C2185B" },
                new MapTileData { category = SyllableTypeCategory.Diphthong, typeName = "Diphthong", pattern = "(v v)", exampleWords = "boil · cloud", taughtInUnit = 7, isUnlocked = false, colorHex = "#00796B" },
                new MapTileData { category = SyllableTypeCategory.ConsonantLe, typeName = "Consonant + le", pattern = "c + le", exampleWords = "bub-ble · sta-ple · cir-cle", taughtInUnit = 9, isUnlocked = false, colorHex = "#5D4037" }
            };
        }

        private void RenderMapTiles()
        {
            if (tilesContainer == null) return;
            foreach (Transform c in tilesContainer) Destroy(c.gameObject);

            if (titleTMP != null)
            {
                titleTMP.text = "<b><color=#FFFFFF>THE 7 SYLLABLE TYPES MAP</color></b>";
                titleTMP.enableAutoSizing = true;
                titleTMP.fontSizeMin = 28;
                titleTMP.fontSizeMax = 44;
            }
            if (subtitleTMP != null)
            {
                subtitleTMP.text = "<color=#FFFFFF><b>Tap any card to preview the full syllabus!</b></color>";
                subtitleTMP.enableAutoSizing = true;
                subtitleTMP.fontSizeMin = 22;
                subtitleTMP.fontSizeMax = 32;
            }

            // Format Container RectTransform to fill table area
            RectTransform containerRT = tilesContainer.GetComponent<RectTransform>();
            if (containerRT != null)
            {
                containerRT.anchorMin = new Vector2(0.5f, 0.5f);
                containerRT.anchorMax = new Vector2(0.5f, 0.5f);
                containerRT.pivot = new Vector2(0.5f, 0.5f);
                containerRT.anchoredPosition = new Vector2(50f, -60f);
                containerRT.sizeDelta = new Vector2(1580f, 440f);
            }

            // Grid Layout: 4 columns in Row 1, 3 columns in Row 2
            var glg = tilesContainer.GetComponent<GridLayoutGroup>();
            if (glg == null)
            {
                glg = tilesContainer.gameObject.AddComponent<GridLayoutGroup>();
            }
            glg.cellSize = new Vector2(360f, 195f);
            glg.spacing = new Vector2(24f, 20f);
            glg.childAlignment = TextAnchor.MiddleCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;

            foreach (var tile in mapTiles)
            {
                MapTileData currentTile = tile;
                GameObject tileObj = new GameObject($"MapTile_{tile.typeName}", typeof(RectTransform), typeof(Image), typeof(Button));
                tileObj.transform.SetParent(tilesContainer, false);
                RectTransform rt = tileObj.GetComponent<RectTransform>();

                Image img = tileObj.GetComponent<Image>();
                img.sprite = GetRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(tileObj.transform, false);
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(12, 10);
                textRT.offsetMax = new Vector2(-12, -10);

                var tmp = textObj.GetComponent<TextMeshProUGUI>();
                string badge = tile.isUnlocked 
                    ? "<color=#2E7D32><b>[ Mastered in Unit 4 & 5 ]</b></color>" 
                    : $"<color=#E65100><b>[ Unlocks in Unit {tile.taughtInUnit} ]</b></color>";

                tmp.text = $"<b><size=44><color={tile.colorHex}>{tile.typeName}</color></size></b>\n<size=28><color=#424242>Pattern: <b>{tile.pattern}</b></color></size>\n<size=26><color=#1565C0><i>{tile.exampleWords}</i></color></size>\n\n<size=22>{badge}</size>";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;

                Button btn = tileObj.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(tileObj.transform, 1.12f));
                    ShowSyllabusDetail(currentTile);
                });
            }

            // Remove any runtime procedural Next_Button if present
            Transform spawnedNext = transform.Find("Next_Button");
            if (spawnedNext != null)
            {
                Destroy(spawnedNext.gameObject);
            }
        }

        private void ShowSyllabusDetail(MapTileData tile)
        {
            tappedCategories.Add(tile.typeName);

            // Re-trigger Next Button visibility on card tap
            if (U4_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U4_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }

            if (subtitleTMP != null)
            {
                subtitleTMP.text = $"<color=#FFFFFF><b>{tile.typeName} Syllables ({tile.pattern}):</b> e.g. {tile.exampleWords} {(tile.isUnlocked ? "(Mastered)" : $"- Unlocks in Unit {tile.taughtInUnit}")}</color>";
            }
        }

        private void OnBackTapped()
        {
            if (U4_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U4_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }
        }

        private IEnumerator PunchScale(Transform target, float targetScale)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            float elapsed = 0f;
            float duration = 0.18f;

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
}
