using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U4_SA_GM06_SchwaHunt_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Instruction Audio")]
        [Tooltip("U04_VO_a3_intro: 'Now find the lazy one. Listen to the word, then tap the letter that turns into \"uh\".' (7s)")]
        public AudioClip introInstructionClip;

        [Tooltip("Celebration fanfare clip when activity 3 completes")]
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Word Letter Tiles Container")]
        [SerializeField] private Transform letterTilesContainer;
        [SerializeField] private TextMeshProUGUI respellingTextTMP; // e.g. "Sounds like: cam-ul"
        [SerializeField] private TextMeshProUGUI clueBannerTMP;     // e.g. "Clue 1: Vowel before final L"

        [Header("4. Mascot Hammock Visual (From U4 Spritesheet)")]
        [Tooltip("Assign 'Schwa_Mascot_Hammock' sliced sprite")]
        public Sprite schwaMascotHammockSprite;
        [SerializeField] private Image schwaMascotImage;

        [Header("5. Audio & Replay")]
        [SerializeField] private Button replayAudioButton;

        private List<SchwaHuntItem> items;
        private int currentIndex = 0;
        private int currentScore = 0;
        private int currentRound = 1; // 1, 2, 3
        private bool isProcessingAnswer = false;
        private List<Button> spawnedLetterButtons = new List<Button>();
        private List<TextMeshProUGUI> spawnedLetterTexts = new List<TextMeshProUGUI>();
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
            currentRound = 1;
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
                PlayCurrentWordAudio();
            }
        }

        private IEnumerator DelayedPlayIntro()
        {
            yield return new WaitForSeconds(0.15f);
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
                yield return new WaitForSeconds(introInstructionClip.length + 0.2f);
                PlayCurrentWordAudio();
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

            if (letterTilesContainer == null)
            {
                Transform t = transform.Find("LetterTiles_Container") ?? transform.Find("Letters_Row") ?? transform.Find("WordContainer") ?? transform.Find("LettersContainer");
                if (t != null) letterTilesContainer = t;
            }

            if (respellingTextTMP == null)
            {
                Transform t = transform.Find("Respelling_Text") ?? transform.Find("RespellingText") ?? transform.Find("Phonetic_Text");
                if (t != null) respellingTextTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (clueBannerTMP == null)
            {
                Transform t = transform.Find("Clue_Banner") ?? transform.Find("ClueBanner") ?? transform.Find("RuleText");
                if (t != null) clueBannerTMP = t.GetComponent<TextMeshProUGUI>();
            }

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
                    PlayCurrentWordAudio();
                });
            }

            // Auto-load Hammock mascot sprite if available
#if UNITY_EDITOR
            if (schwaMascotHammockSprite == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("Schwa_Mascot_Hammock t:Sprite");
                if (guids.Length > 0)
                {
                    schwaMascotHammockSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
#endif
            if (schwaMascotImage != null && schwaMascotHammockSprite != null)
            {
                schwaMascotImage.sprite = schwaMascotHammockSprite;
            }
        }

        private void InitializeItems()
        {
            items = new List<SchwaHuntItem>
            {
                // Round A: Clue 1 Words (Before final L) (6 items)
                new SchwaHuntItem { word = "camel", schwaLetterIndices = new[] { 3 }, respelling = "cam-ul", clueRule = "Clue 1: Vowel before final L", roundIndex = 1 },
                new SchwaHuntItem { word = "dental", schwaLetterIndices = new[] { 4 }, respelling = "dent-ul", clueRule = "Clue 1: Vowel before final L", roundIndex = 1 },
                new SchwaHuntItem { word = "pencil", schwaLetterIndices = new[] { 4 }, respelling = "penc-ul", clueRule = "Clue 1: Vowel before final L", roundIndex = 1 },
                new SchwaHuntItem { word = "animal", schwaLetterIndices = new[] { 4 }, respelling = "anim-ul", clueRule = "Clue 1: Vowel before final L", roundIndex = 1 },
                new SchwaHuntItem { word = "decimal", schwaLetterIndices = new[] { 5 }, respelling = "decim-ul", clueRule = "Clue 1: Vowel before final L", roundIndex = 1 },
                new SchwaHuntItem { word = "vinyl", schwaLetterIndices = new[] { 3 }, respelling = "vin-ul", clueRule = "Clue 1: Vowel before final L", roundIndex = 1 },

                // Round B: a / e / i Columns (6 items)
                new SchwaHuntItem { word = "ago", schwaLetterIndices = new[] { 0 }, respelling = "uh-go", clueRule = "Round 2: a / e / i lazy sound", roundIndex = 2 },
                new SchwaHuntItem { word = "alarm", schwaLetterIndices = new[] { 0 }, respelling = "uh-larm", clueRule = "Round 2: a / e / i lazy sound", roundIndex = 2 },
                new SchwaHuntItem { word = "banana", schwaLetterIndices = new[] { 0, 5 }, respelling = "buh-nan-uh", clueRule = "Round 2: a / e / i lazy sound", roundIndex = 2 },
                new SchwaHuntItem { word = "blanket", schwaLetterIndices = new[] { 5 }, respelling = "blank-ut", clueRule = "Round 2: a / e / i lazy sound", roundIndex = 2 },
                new SchwaHuntItem { word = "children", schwaLetterIndices = new[] { 6 }, respelling = "child-run", clueRule = "Round 2: a / e / i lazy sound", roundIndex = 2 },
                new SchwaHuntItem { word = "cousin", schwaLetterIndices = new[] { 4 }, respelling = "cous-un", clueRule = "Round 2: a / e / i lazy sound", roundIndex = 2 },

                // Round C: o / u / y Columns (6 items)
                new SchwaHuntItem { word = "bacon", schwaLetterIndices = new[] { 3 }, respelling = "bac-un", clueRule = "Round 3: o / u / y lazy sound", roundIndex = 3 },
                new SchwaHuntItem { word = "lemon", schwaLetterIndices = new[] { 3 }, respelling = "lem-un", clueRule = "Round 3: o / u / y lazy sound", roundIndex = 3 },
                new SchwaHuntItem { word = "carrot", schwaLetterIndices = new[] { 4 }, respelling = "carr-ut", clueRule = "Round 3: o / u / y lazy sound", roundIndex = 3 },
                new SchwaHuntItem { word = "album", schwaLetterIndices = new[] { 3 }, respelling = "alb-um", clueRule = "Round 3: o / u / y lazy sound", roundIndex = 3 },
                new SchwaHuntItem { word = "focus", schwaLetterIndices = new[] { 3 }, respelling = "foc-us", clueRule = "Round 3: o / u / y lazy sound", roundIndex = 3 },
                new SchwaHuntItem { word = "catalyst", schwaLetterIndices = new[] { 5 }, respelling = "catal-ust", clueRule = "Round 3: o / u / y lazy sound", roundIndex = 3 }
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
            SchwaHuntItem item = items[currentIndex];

            if (item.roundIndex != currentRound)
            {
                currentRound = item.roundIndex;
            }

            if (titleTMP != null)
            {
                titleTMP.text = $"<b><color=#FFFFFF>SCHWA HUNT (ROUND {currentRound} OF 3)</color></b>";
                titleTMP.enableAutoSizing = true;
                titleTMP.fontSizeMin = 30;
                titleTMP.fontSizeMax = 46;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }

            if (promptTMP != null)
            {
                promptTMP.text = "<color=#FFFFFF><b>Which letter turns into the lazy \"uh\" sound? Tap the letter!</b></color>";
                promptTMP.enableAutoSizing = true;
                promptTMP.fontSizeMin = 26;
                promptTMP.fontSizeMax = 36;
                promptTMP.alignment = TextAlignmentOptions.Center;
            }

            if (progressBar != null)
            {
                progressBar.value = (float)currentIndex / items.Count;
            }

            if (clueBannerTMP != null)
            {
                clueBannerTMP.text = $"<b><color=#FFD54F>{item.clueRule}</color></b>";
                clueBannerTMP.fontSize = 36;
                clueBannerTMP.alignment = TextAlignmentOptions.Center;
            }

            if (respellingTextTMP != null)
            {
                respellingTextTMP.text = ""; // Hidden until correct answer
                respellingTextTMP.fontSize = 44;
                respellingTextTMP.alignment = TextAlignmentOptions.Center;
            }

            RenderLetterTiles(item);
            PlayCurrentWordAudio();
        }

        private void RenderLetterTiles(SchwaHuntItem item)
        {
            if (letterTilesContainer == null) return;
            foreach (Transform c in letterTilesContainer) Destroy(c.gameObject);
            spawnedLetterButtons.Clear();
            spawnedLetterTexts.Clear();

            // Set horizontal layout group
            var hlg = letterTilesContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = letterTilesContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 18;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Add ContentSizeFitter so the container expands symmetrically around center
            var csf = letterTilesContainer.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = letterTilesContainer.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            float tileWidth = item.word.Length > 6 ? 110f : 135f;
            float tileHeight = 160f;

            for (int i = 0; i < item.word.Length; i++)
            {
                int letterIdx = i;
                char ch = item.word[i];

                GameObject tileObj = new GameObject($"Letter_{ch}_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                tileObj.transform.SetParent(letterTilesContainer, false);
                RectTransform rt = tileObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(tileWidth, tileHeight);

                Image img = tileObj.GetComponent<Image>();
                img.sprite = GetRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = Color.white;

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(tileObj.transform, false);
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;

                var tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b><color=#000000>{ch}</color></b>";
                tmp.fontSize = tileWidth > 120 ? 74 : 64;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;

                Button btn = tileObj.GetComponent<Button>();
                btn.onClick.AddListener(() => OnLetterTileTapped(letterIdx, item, tileObj, tmp));

                spawnedLetterButtons.Add(btn);
                spawnedLetterTexts.Add(tmp);
            }
        }

        public void PlayCurrentWordAudio()
        {
            if (currentIndex >= items.Count) return;
            SchwaHuntItem item = items[currentIndex];

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                string word = item.word;
                AudioClip clip = item.wordAudio
                              ?? Resources.Load<AudioClip>($"U4_audio/words MP U4/{word}")
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

        private void OnLetterTileTapped(int letterIndex, SchwaHuntItem item, GameObject tileObj, TextMeshProUGUI textTMP)
        {
            if (isProcessingAnswer) return;

            bool isSchwa = false;
            foreach (int idx in item.schwaLetterIndices)
            {
                if (idx == letterIndex)
                {
                    isSchwa = true;
                    break;
                }
            }

            if (isSchwa)
            {
                StartCoroutine(HandleCorrectSchwa(item, tileObj, textTMP));
            }
            else
            {
                StartCoroutine(HandleWrongSchwa(tileObj));
            }
        }

        private IEnumerator HandleCorrectSchwa(SchwaHuntItem item, GameObject tileObj, TextMeshProUGUI textTMP)
        {
            isProcessingAnswer = true;
            currentScore += 100;
            UpdateScoreUI();

            if (U4_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U4_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(100);
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFFFFF><b>Found it! \"{item.word}\" sounds like \"{item.respelling}\"!</b></color>";
            }

            if (respellingTextTMP != null)
            {
                respellingTextTMP.text = $"Sounds like: <b><color=#FFD54F>\"{item.respelling}\"</color></b>";
            }

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlaySchwaMorph();
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();
            }

            // Morph letter into /e/ schwa with amber glowing bounce
            if (tileObj != null && textTMP != null)
            {
                var img = tileObj.GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 0.92f, 0.7f); // Warm amber
                textTMP.text = "<b><color=#E65100>/e/</color></b>";
                StartCoroutine(PunchScale(tileObj.transform, 1.25f));
            }

            yield return new WaitForSeconds(1.4f);

            currentIndex++;
            LoadCurrentItem();
        }

        private IEnumerator HandleWrongSchwa(GameObject tileObj)
        {
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            if (promptTMP != null)
            {
                promptTMP.text = "<color=#FFD54F><b>That letter keeps its regular vowel sound. Try another letter!</b></color>";
            }

            if (tileObj != null)
            {
                StartCoroutine(ShakeTile(tileObj.transform));
            }

            yield return new WaitForSeconds(0.8f);
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
                float xOffset = Mathf.Sin(elapsed * 45f) * 12f;
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
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>Schwa Hunt Complete! You spotted all the lazy sounds!</b></color>";
            if (progressBar != null) progressBar.value = 1f;

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null && celebrationClip != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(celebrationClip);
            }

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
