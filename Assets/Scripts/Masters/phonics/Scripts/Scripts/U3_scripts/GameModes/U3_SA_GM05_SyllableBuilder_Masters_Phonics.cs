using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U3_SA_GM05_SyllableBuilder_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Voice A Audio (Official Unit 3 Doc)")]
        [Tooltip("U03_VO_a4_intro: 'Now the other way round. Here are the pieces — put the word back together.' (6s)")]
        public AudioClip introInstructionClip;

        [Tooltip("U03_VO_a4_roundc: 'Longest ones now. Four pieces, sometimes five.' (4s)")]
        public AudioClip voiceARoundCClip;

        [Tooltip("Celebration fanfare clip when activity 4 completes")]
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Syllable Slots & Tray Container")]
        [SerializeField] private Transform slotsContainer; // Target Slots Row
        [SerializeField] private Transform trayContainer;  // Scrambled Chunks Tray Row
        [SerializeField] private Button btnBuild;
        [SerializeField] private Button btnReplayAudio;

        [Header("4. Sprites for Slots and Chunk Tiles")]
        public Sprite chunkTileSprite;
        public Sprite slotEmptySprite;

        private List<SyllableWordItem> rounds;
        private int currentRoundIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;
        private List<string> placedChunks = new List<string>();
        private List<Button> spawnedSlotButtons = new List<Button>();
        private List<Button> spawnedTrayButtons = new List<Button>();

        private void Awake()
        {
            AutoBindHierarchyElements();
            InitializeRounds();
        }

        private void Start()
        {
            PlayIntroInstruction();
        }

        private void OnEnable()
        {
            if (U3_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U3_SA_UnitFlowManager_Masters_Phonics.Instance.SetCurrentActivityIndex(4);
            }
            AutoBindHierarchyElements();
            currentRoundIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            InitializeRounds();
            LoadCurrentRound();

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
            if (U3_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
                yield return new WaitForSeconds(introInstructionClip.length + 0.3f);
                PlayCurrentWordAudio();
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
                var t = transform.Find("prompttextBG/Prompt_Text") ?? transform.Find("prompttextBG") ?? transform.Find("Prompt_Text") ?? transform.Find("PromptText") ?? transform.Find("InstructionText");
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

            if (slotsContainer == null)
            {
                Transform t = transform.Find("Slots_Container") ?? transform.Find("Slots_Row") ?? transform.Find("TargetSlots");
                if (t != null) slotsContainer = t;
            }

            if (trayContainer == null)
            {
                Transform t = transform.Find("Tray_Container") ?? transform.Find("Chunks_Tray") ?? transform.Find("TrayRow");
                if (t != null) trayContainer = t;
            }

            if (btnBuild == null)
            {
                Transform t = transform.Find("Btn_Build") ?? transform.Find("BuildButton") ?? transform.Find("SubmitButton");
                if (t != null) btnBuild = t.GetComponent<Button>();
            }
            if (btnBuild != null)
            {
                btnBuild.onClick.RemoveAllListeners();
                btnBuild.onClick.AddListener(OnBuildPressed);
            }

            if (btnReplayAudio == null)
            {
                Transform t = transform.Find("ReplayButton") ?? transform.Find("Audio_Button") ?? transform.Find("SpeakerButton") ?? transform.Find("Speaker_Button") ?? transform.Find("Btn_Audio");
                if (t != null) btnReplayAudio = t.GetComponent<Button>();
            }
            if (btnReplayAudio != null)
            {
                btnReplayAudio.onClick.RemoveAllListeners();
                btnReplayAudio.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(btnReplayAudio.transform, 1.15f));
                    PlayCurrentWordAudio();
                });
            }
        }

        private void InitializeRounds()
        {
            rounds = new List<SyllableWordItem>
            {
                // Round A: 2 Syllables (Page 20 Read and Write)
                new SyllableWordItem { word = "water", chunks = new[] { "wa", "ter" }, hyphenatedSplit = "wa-ter" },
                new SyllableWordItem { word = "butter", chunks = new[] { "but", "ter" }, hyphenatedSplit = "but-ter" },
                new SyllableWordItem { word = "pillow", chunks = new[] { "pil", "low" }, hyphenatedSplit = "pil-low" },
                new SyllableWordItem { word = "tablet", chunks = new[] { "tab", "let" }, hyphenatedSplit = "tab-let" },
                new SyllableWordItem { word = "father", chunks = new[] { "fa", "ther" }, hyphenatedSplit = "fa-ther" },
                new SyllableWordItem { word = "guitar", chunks = new[] { "gui", "tar" }, hyphenatedSplit = "gui-tar" },

                // Round B: 3 Syllables (Page 21)
                new SyllableWordItem { word = "carpenter", chunks = new[] { "car", "pen", "ter" }, hyphenatedSplit = "car-pen-ter" },
                new SyllableWordItem { word = "pharmacy", chunks = new[] { "phar", "ma", "cy" }, hyphenatedSplit = "phar-ma-cy" },
                new SyllableWordItem { word = "departure", chunks = new[] { "de", "par", "ture" }, hyphenatedSplit = "de-par-ture" },
                new SyllableWordItem { word = "harmony", chunks = new[] { "har", "mo", "ny" }, hyphenatedSplit = "har-mo-ny" },
                new SyllableWordItem { word = "carnival", chunks = new[] { "car", "ni", "val" }, hyphenatedSplit = "car-ni-val" },

                // Round C: 4-5 Syllables (Page 22)
                new SyllableWordItem { word = "convenient", chunks = new[] { "con", "ve", "ni", "ent" }, hyphenatedSplit = "con-ve-ni-ent" },
                new SyllableWordItem { word = "material", chunks = new[] { "ma", "te", "ri", "al" }, hyphenatedSplit = "ma-te-ri-al" },
                new SyllableWordItem { word = "hippopotamus", chunks = new[] { "hip", "po", "pot", "a", "mus" }, hyphenatedSplit = "hip-po-pot-a-mus" }
            };
        }

        private void LoadCurrentRound()
        {
            if (currentRoundIndex >= rounds.Count)
            {
                OnAllRoundsComplete();
                return;
            }

            isProcessingAnswer = false;
            placedChunks.Clear();

            SyllableWordItem item = rounds[currentRoundIndex];

            if (titleTMP != null) titleTMP.text = "<b>SYLLABLE BUILDER</b>";
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>Hear the word, then tap the syllable chunks in order to build it!</b></color>";

            if (progressBar != null)
            {
                progressBar.value = (float)currentRoundIndex / rounds.Count;
            }

            RenderSlotsAndTray(item);
        }        public void PlayCurrentWordAudio()
        {
            if (currentRoundIndex >= rounds.Count) return;
            SyllableWordItem item = rounds[currentRoundIndex];

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                AudioClip clip = item.wordAudio 
                              ?? Resources.Load<AudioClip>($"U3_audio/words_U3_MP/{item.word}")
                              ?? Resources.Load<AudioClip>($"Audio/U3_audio/words_U3_MP/{item.word}")
                              ?? Resources.Load<AudioClip>($"U3_audio/{item.word}") 
                              ?? Resources.Load<AudioClip>($"Audio/U3_audio/{item.word}")
                              ?? Resources.Load<AudioClip>($"U3_audio/U03_WRD_{item.word}")
                              ?? Resources.Load<AudioClip>($"Audio/U3_audio/U03_WRD_{item.word}");
                if (clip != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
                }
            }
        }

        private Sprite generatedBuilderTileSprite;
        private Sprite generatedSlotSprite;

        private Sprite GetRoundedBuilderTileSprite()
        {
            if (generatedBuilderTileSprite != null) return generatedBuilderTileSprite;

            int w = 180;
            int h = 110;
            int r = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color faceColor = new Color(0.98f, 0.99f, 1f, 1f);
            Color borderColor = new Color(0.12f, 0.65f, 0.95f, 1f); // Vibrant blue border
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
                    else if (dist > r - 4f || y < 4 || y > h - 4 || x < 4 || x > w - 4)
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

            generatedBuilderTileSprite = Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                100f
            );

            return generatedBuilderTileSprite;
        }

        private Sprite GetRoundedSlotSprite()
        {
            if (generatedSlotSprite != null) return generatedSlotSprite;

            int w = 180;
            int h = 110;
            int r = 24;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color faceColor = new Color(0.92f, 0.95f, 0.98f, 0.65f); // Transparent soft slot
            Color borderColor = new Color(0.55f, 0.65f, 0.75f, 0.9f);

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
                        colors[y * w + x] = new Color(borderColor.r, borderColor.g, borderColor.b, alpha);
                    }
                    else if (dist > r - 4f || y < 4 || y > h - 4 || x < 4 || x > w - 4)
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

            generatedSlotSprite = Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                100f
            );

            return generatedSlotSprite;
        }

        private void RenderSlotsAndTray(SyllableWordItem item)
        {
            if (slotsContainer == null || trayContainer == null) return;

            foreach (Transform c in slotsContainer) Destroy(c.gameObject);
            foreach (Transform c in trayContainer) Destroy(c.gameObject);
            spawnedSlotButtons.Clear();
            spawnedTrayButtons.Clear();

            // 1. Target Empty Slots
            for (int i = 0; i < item.chunks.Length; i++)
            {
                int slotIdx = i;
                GameObject slotObj = new GameObject($"Slot_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
                slotObj.transform.SetParent(slotsContainer, false);
                RectTransform rt = slotObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(240, 130);

                Image img = slotObj.GetComponent<Image>();
                img.sprite = slotEmptySprite ?? GetRoundedSlotSprite();
                img.type = Image.Type.Simple;
                img.color = Color.white;

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(slotObj.transform, false);
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;

                var tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.text = "<b><color=#556677>?</color></b>";
                tmp.fontSize = 52;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;

                Button btn = slotObj.GetComponent<Button>();
                btn.onClick.AddListener(() => OnSlotTapped(slotIdx));
                spawnedSlotButtons.Add(btn);
            }

            // 2. Scrambled Chunks in Tray
            List<string> scrambled = new List<string>(item.chunks);
            for (int i = 0; i < scrambled.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, scrambled.Count);
                var temp = scrambled[i];
                scrambled[i] = scrambled[rnd];
                scrambled[rnd] = temp;
            }

            for (int i = 0; i < scrambled.Count; i++)
            {
                string chunk = scrambled[i];
                GameObject chunkObj = new GameObject($"Chunk_{chunk}", typeof(RectTransform), typeof(Image), typeof(Button));
                chunkObj.transform.SetParent(trayContainer, false);
                RectTransform rt = chunkObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(240, 130);

                Image img = chunkObj.GetComponent<Image>();
                img.sprite = chunkTileSprite ?? GetRoundedBuilderTileSprite();
                img.type = Image.Type.Simple;
                img.color = Color.white;

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(chunkObj.transform, false);
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;

                var tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b><color=#000000>{chunk}</color></b>";
                tmp.fontSize = 58;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;

                Button btn = chunkObj.GetComponent<Button>();
                btn.onClick.AddListener(() => OnTrayChunkTapped(chunk, btn));
                spawnedTrayButtons.Add(btn);
            }
        }

        private void OnTrayChunkTapped(string chunk, Button trayBtn)
        {
            if (isProcessingAnswer) return;

            SyllableWordItem item = rounds[currentRoundIndex];
            if (placedChunks.Count < item.chunks.Length)
            {
                placedChunks.Add(chunk);
                trayBtn.interactable = false;
                trayBtn.image.color = new Color(0.75f, 0.8f, 0.85f, 0.4f);

                if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayBeat();
                }

                UpdateSlotsDisplay();
            }
        }

        private void OnSlotTapped(int slotIndex)
        {
            if (isProcessingAnswer) return;

            if (slotIndex < placedChunks.Count)
            {
                string removedChunk = placedChunks[slotIndex];
                placedChunks.RemoveAt(slotIndex);

                // Reactivate corresponding tray button
                foreach (var b in spawnedTrayButtons)
                {
                    var tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp != null && tmp.text.Contains(removedChunk) && !b.interactable)
                    {
                        b.interactable = true;
                        b.image.color = new Color(0.96f, 0.98f, 1f);
                        break;
                    }
                }

                if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlaySplitUndo();
                }

                UpdateSlotsDisplay();
            }
        }

        private void UpdateSlotsDisplay()
        {
            for (int i = 0; i < spawnedSlotButtons.Count; i++)
            {
                var tmp = spawnedSlotButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp != null)
                {
                    if (i < placedChunks.Count)
                    {
                        tmp.text = $"<b><color=#000000>{placedChunks[i]}</color></b>";
                        spawnedSlotButtons[i].image.color = new Color(0.85f, 0.95f, 1f, 1f);
                    }
                    else
                    {
                        tmp.text = "<b><color=#556677>?</color></b>";
                        spawnedSlotButtons[i].image.color = new Color(0.9f, 0.95f, 1f, 0.9f);
                    }
                }
            }
        }

        public void OnBuildPressed()
        {
            if (isProcessingAnswer || currentRoundIndex >= rounds.Count) return;

            SyllableWordItem item = rounds[currentRoundIndex];
            if (placedChunks.Count < item.chunks.Length)
            {
                if (promptTMP != null) promptTMP.text = "<color=#FFD54F><b>Place all syllable chunks before building!</b></color>";
                return;
            }

            string builtWord = string.Join("", placedChunks);
            bool isCorrect = (builtWord == item.word);

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectBuild(item));
            }
            else
            {
                StartCoroutine(HandleWrongBuild(item));
            }
        }

        private IEnumerator HandleCorrectBuild(SyllableWordItem item)
        {
            isProcessingAnswer = true;

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFFFFF><b>Brilliant! {string.Join(" + ", item.chunks)} = {item.word}!</b></color>";
            }

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();

                AudioClip wordClip = item.wordAudio ?? Resources.Load<AudioClip>($"U3_audio/{item.word}") ?? Resources.Load<AudioClip>($"Audio/U3_audio/{item.word}");
                if (wordClip != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(wordClip);
                }
            }

            currentScore += 100;
            UpdateScoreUI();

            yield return new WaitForSeconds(1.5f);

            currentRoundIndex++;
            LoadCurrentRound();
        }

        private IEnumerator HandleWrongBuild(SyllableWordItem item)
        {
            isProcessingAnswer = true;

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFD54F><b>Listen closely to the syllable order: {item.hyphenatedSplit}</b></color>";
            }

            yield return new WaitForSeconds(1.8f);

            isProcessingAnswer = false;
            placedChunks.Clear();
            foreach (var b in spawnedTrayButtons)
            {
                b.interactable = true;
                b.image.color = new Color(0.96f, 0.98f, 1f);
            }
            UpdateSlotsDisplay();
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
        }

        private void OnAllRoundsComplete()
        {
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>All 14 Words Built Perfectly!</b></color>";
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
    }
}
