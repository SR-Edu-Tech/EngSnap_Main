using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U4_SA_GM04_StrongBeat_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Instruction Audio")]
        [Tooltip("U04_VO_a2_intro: 'Two beats. One is stronger. Listen and tap the loud one.' (5s)")]
        public AudioClip introInstructionClip;

        [Tooltip("U04_VO_a2_outro: 'Did you notice? The quiet beat always went lazy...' (6s)")]
        public AudioClip outroClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Syllable Choice Tiles")]
        [SerializeField] private Button tile1Button;
        [SerializeField] private Button tile2Button;
        [SerializeField] private TextMeshProUGUI tile1TextTMP;
        [SerializeField] private TextMeshProUGUI tile2TextTMP;

        [Header("4. Visual Drum / Pebble Sprites (From U4 Spritesheet)")]
        [Tooltip("Assign 'Tile_StrongBeat_Drum' sliced sprite")]
        public Sprite drumSprite;

        [Tooltip("Assign 'Tile_WeakBeat_Pebble' sliced sprite")]
        public Sprite pebbleSprite;

        [Header("5. Audio & Replay")]
        [SerializeField] private Button replayAudioButton;

        private List<StrongBeatItem> items;
        private int currentIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;
        private Sprite proceduralRoundedSprite;

        private void Awake()
        {
            AutoBindHierarchyElements();
            InitializeItems();
        }

        private void Start()
        {
            PlayIntroInstruction();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            currentIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            InitializeItems();
            LoadCurrentItem();

            PlayIntroInstruction();
        }

        private void PlayIntroInstruction()
        {
            if (introInstructionClip != null)
            {
                StartCoroutine(DelayedPlayIntro());
            }
            else
            {
                PlayCurrentWordPromptAudio();
            }
        }

        private IEnumerator DelayedPlayIntro()
        {
            yield return new WaitForSeconds(0.15f);
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
                yield return new WaitForSeconds(introInstructionClip.length + 0.2f);
                PlayCurrentWordPromptAudio();
            }
        }

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("Title BG/TitleText") ?? transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
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

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("ProgressBar (1)") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            // Tile 1
            if (tile1Button == null)
            {
                Transform t = transform.Find("Tile_1") ?? transform.Find("Syllable_1") ?? transform.Find("Btn_Syllable1") ?? transform.Find("Tiles_Row/Tile_1");
                if (t != null) tile1Button = t.GetComponent<Button>();
            }
            if (tile1Button != null && tile1TextTMP == null)
            {
                tile1TextTMP = tile1Button.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Tile 2
            if (tile2Button == null)
            {
                Transform t = transform.Find("Tile_2") ?? transform.Find("Syllable_2") ?? transform.Find("Btn_Syllable2") ?? transform.Find("Tiles_Row/Tile_2");
                if (t != null) tile2Button = t.GetComponent<Button>();
            }
            if (tile2Button != null && tile2TextTMP == null)
            {
                tile2TextTMP = tile2Button.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Replay Button
            if (replayAudioButton == null)
            {
                Transform t = transform.Find("ReplayButton") ?? transform.Find("Audio_Button") ?? transform.Find("Speaker_Button") ?? transform.Find("Replay_Button") ?? transform.Find("SpeakerButton");
                if (t == null)
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
                    replayAudioButton = t.GetComponent<Button>();
                }
            }
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(replayAudioButton.transform, 1.15f));
                    PlayCurrentWordPromptAudio();
                });
            }

            // Format Tile Buttons
            FormatTileButton(tile1Button, 0);
            FormatTileButton(tile2Button, 1);

            // Load Sprites
            LoadBadges();
        }

        private void LoadBadges()
        {
#if UNITY_EDITOR
            if (drumSprite == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("Tile_StrongBeat_Drum t:Sprite");
                if (guids.Length > 0)
                {
                    drumSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            if (pebbleSprite == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("Tile_WeakBeat_Pebble t:Sprite");
                if (guids.Length > 0)
                {
                    pebbleSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
#endif
        }

        private void FormatTileButton(Button btn, int tileIndex)
        {
            if (btn == null) return;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnTileTapped(tileIndex));

            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            RectTransform rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(320f, 150f);
            }
        }

        private void InitializeItems()
        {
            items = new List<StrongBeatItem>
            {
                // 1st Syllable Stress (Items 1 to 7)
                new StrongBeatItem { word = "rabbit", syllables = new[] { "RAB", "bit" }, stressedIndex = 0, weakVowelNote = "i goes lazy" },
                new StrongBeatItem { word = "bacon", syllables = new[] { "BA", "con" }, stressedIndex = 0, weakVowelNote = "o goes lazy" },
                new StrongBeatItem { word = "camel", syllables = new[] { "CAM", "el" }, stressedIndex = 0, weakVowelNote = "e goes lazy" },
                new StrongBeatItem { word = "husband", syllables = new[] { "HUS", "band" }, stressedIndex = 0, weakVowelNote = "a goes lazy" },
                new StrongBeatItem { word = "pencil", syllables = new[] { "PEN", "cil" }, stressedIndex = 0, weakVowelNote = "i goes lazy" },
                new StrongBeatItem { word = "lemon", syllables = new[] { "LE", "mon" }, stressedIndex = 0, weakVowelNote = "o goes lazy" },
                new StrongBeatItem { word = "carrot", syllables = new[] { "CA", "rrot" }, stressedIndex = 0, weakVowelNote = "o goes lazy" },

                // 2nd Syllable Stress Flip (Items 8 to 12)
                new StrongBeatItem { word = "ago", syllables = new[] { "a", "GO" }, stressedIndex = 1, weakVowelNote = "a goes lazy" },
                new StrongBeatItem { word = "alarm", syllables = new[] { "a", "LARM" }, stressedIndex = 1, weakVowelNote = "a goes lazy" },
                new StrongBeatItem { word = "alone", syllables = new[] { "a", "LONE" }, stressedIndex = 1, weakVowelNote = "a goes lazy" },
                new StrongBeatItem { word = "supply", syllables = new[] { "su", "PPLY" }, stressedIndex = 1, weakVowelNote = "u goes lazy" },
                new StrongBeatItem { word = "upon", syllables = new[] { "u", "PON" }, stressedIndex = 1, weakVowelNote = "u goes lazy" }
            };
        }

        private void LoadCurrentItem()
        {
            if (currentIndex >= items.Count)
            {
                OnActivityComplete();
                return;
            }

            isProcessingAnswer = false;
            StrongBeatItem item = items[currentIndex];

            if (titleTMP != null)
            {
                titleTMP.text = $"<b><color=#FFFFFF>STRONG BEAT (ITEM {currentIndex + 1} OF {items.Count})</color></b>";
                titleTMP.enableAutoSizing = true;
                titleTMP.fontSizeMin = 28;
                titleTMP.fontSizeMax = 44;
            }

            if (promptTMP != null)
            {
                promptTMP.text = "<color=#FFFFFF><b>Which beat is louder and stronger? Listen and tap!</b></color>";
                promptTMP.enableAutoSizing = true;
                promptTMP.fontSizeMin = 22;
                promptTMP.fontSizeMax = 32;
            }

            if (progressBar != null)
            {
                progressBar.value = (float)currentIndex / items.Count;
            }

            // Reset Tile 1
            if (tile1Button != null)
            {
                tile1Button.interactable = true;
                tile1Button.transform.localScale = Vector3.one;
                var img = tile1Button.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }
            if (tile1TextTMP != null)
            {
                tile1TextTMP.text = $"<b><size=62><color=#000000>{item.syllables[0]}</color></size></b>\n<size=22><color=#616161>[ Beat 1 ]</color></size>";
                tile1TextTMP.alignment = TextAlignmentOptions.Center;
            }

            // Reset Tile 2
            if (tile2Button != null)
            {
                tile2Button.interactable = true;
                tile2Button.transform.localScale = Vector3.one;
                var img = tile2Button.GetComponent<Image>();
                if (img != null) img.color = Color.white;
            }
            if (tile2TextTMP != null)
            {
                tile2TextTMP.text = $"<b><size=62><color=#000000>{item.syllables[1]}</color></size></b>\n<size=22><color=#616161>[ Beat 2 ]</color></size>";
                tile2TextTMP.alignment = TextAlignmentOptions.Center;
            }

            PlayCurrentWordPromptAudio();
        }

        public void PlayCurrentWordPromptAudio()
        {
            if (currentIndex >= items.Count) return;
            StrongBeatItem item = items[currentIndex];

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                string word = item.word;
                string stressedCombined = item.syllables != null && item.syllables.Length >= 2 ? (item.syllables[0] + item.syllables[1]) : word;

                AudioClip clip = item.normAudio 
                              ?? item.naturalAudio
                              ?? Resources.Load<AudioClip>($"U4_audio/words MP U4/strong beat words/{stressedCombined}")
                              ?? Resources.Load<AudioClip>($"U4_audio/words MP U4/{word}")
                              ?? Resources.Load<AudioClip>($"Audio/U4_audio/words MP U4/strong beat words/{stressedCombined}")
                              ?? Resources.Load<AudioClip>($"Audio/U4_audio/words MP U4/{word}")
                              ?? Resources.Load<AudioClip>($"U4_audio/words MP U4/strong beat words/{word}")
                              ?? Resources.Load<AudioClip>($"Audio/U4_audio/words MP U4/strong beat words/{word}")
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

        private void OnTileTapped(int selectedIndex)
        {
            if (isProcessingAnswer || currentIndex >= items.Count) return;
            isProcessingAnswer = true;

            StrongBeatItem item = items[currentIndex];

            if (selectedIndex == item.stressedIndex)
            {
                StartCoroutine(HandleCorrectChoice(selectedIndex, item));
            }
            else
            {
                StartCoroutine(HandleWrongChoice(selectedIndex, item));
            }
        }

        private IEnumerator HandleCorrectChoice(int chosenIdx, StrongBeatItem item)
        {
            currentScore += 100;
            UpdateScoreUI();

            if (U4_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U4_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(100);
            }

            Button chosenBtn = (chosenIdx == 0) ? tile1Button : tile2Button;
            Button unchosenBtn = (chosenIdx == 0) ? tile2Button : tile1Button;

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFFFFF><b>Spot on! \"{item.syllables[chosenIdx]}\" is the strong beat! ({item.weakVowelNote})</b></color>";
            }

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayBeatStrong();
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();
            }

            // Visual contrast: Strong grows & displays Golden Drum, weak shrinks & displays Pebble
            if (chosenBtn != null)
            {
                var img = chosenBtn.GetComponent<Image>();
                if (img != null) img.color = new Color(0.72f, 1f, 0.72f);
                StartCoroutine(PunchScale(chosenBtn.transform, 1.22f));

                // Display Golden Drum icon on winning tile
                if (drumSprite != null)
                {
                    Transform badge = chosenBtn.transform.Find("ResultBadge");
                    if (badge == null)
                    {
                        GameObject bObj = new GameObject("ResultBadge", typeof(RectTransform), typeof(Image));
                        bObj.transform.SetParent(chosenBtn.transform, false);
                        badge = bObj.transform;
                        RectTransform bRT = bObj.GetComponent<RectTransform>();
                        bRT.anchorMin = new Vector2(0.5f, 1f);
                        bRT.anchorMax = new Vector2(0.5f, 1f);
                        bRT.pivot = new Vector2(0.5f, 0.5f);
                        bRT.anchoredPosition = new Vector2(0f, 15f);
                        bRT.sizeDelta = new Vector2(70f, 70f);
                    }
                    var bImg = badge.GetComponent<Image>();
                    bImg.sprite = drumSprite;
                    badge.gameObject.SetActive(true);
                    StartCoroutine(PunchScale(badge, 1.35f));
                }
            }

            if (unchosenBtn != null)
            {
                var img = unchosenBtn.GetComponent<Image>();
                if (img != null) img.color = new Color(0.85f, 0.85f, 0.85f, 0.6f);
                unchosenBtn.transform.localScale = Vector3.one * 0.88f;

                // Display Pebble icon on lazy unaccented tile
                if (pebbleSprite != null)
                {
                    Transform badge = unchosenBtn.transform.Find("ResultBadge");
                    if (badge == null)
                    {
                        GameObject bObj = new GameObject("ResultBadge", typeof(RectTransform), typeof(Image));
                        bObj.transform.SetParent(unchosenBtn.transform, false);
                        badge = bObj.transform;
                        RectTransform bRT = bObj.GetComponent<RectTransform>();
                        bRT.anchorMin = new Vector2(0.5f, 1f);
                        bRT.anchorMax = new Vector2(0.5f, 1f);
                        bRT.pivot = new Vector2(0.5f, 0.5f);
                        bRT.anchoredPosition = new Vector2(0f, 15f);
                        bRT.sizeDelta = new Vector2(55f, 55f);
                    }
                    var bImg = badge.GetComponent<Image>();
                    bImg.sprite = pebbleSprite;
                    badge.gameObject.SetActive(true);
                }
            }

            yield return new WaitForSeconds(1.4f);

            // Clean up result badges before next item
            if (chosenBtn != null)
            {
                Transform b = chosenBtn.transform.Find("ResultBadge");
                if (b != null) Destroy(b.gameObject);
            }
            if (unchosenBtn != null)
            {
                Transform b = unchosenBtn.transform.Find("ResultBadge");
                if (b != null) Destroy(b.gameObject);
            }

            currentIndex++;
            LoadCurrentItem();
        }

        private IEnumerator HandleWrongChoice(int chosenIdx, StrongBeatItem item)
        {
            Button chosenBtn = (chosenIdx == 0) ? tile1Button : tile2Button;

            if (promptTMP != null)
            {
                promptTMP.text = "<color=#FFD54F><b>Listen again! The other syllable was louder and accented.</b></color>";
            }

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayBeatWeak();
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            if (chosenBtn != null)
            {
                StartCoroutine(ShakeTile(chosenBtn.transform));
            }

            yield return new WaitForSeconds(1.1f);
            isProcessingAnswer = false;
        }

        private IEnumerator ShakeTile(Transform target)
        {
            if (target == null) yield break;
            Vector3 original = target.localPosition;
            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.deltaTime;
                float xOffset = Mathf.Sin(elapsed * 45f) * 14f;
                target.localPosition = original + new Vector3(xOffset, 0, 0);
                yield return null;
            }
            if (target != null) target.localPosition = original;
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
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>All 12 Stress Beats Identified!</b></color>";
            if (progressBar != null) progressBar.value = 1f;

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null && outroClip != null)
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
}
