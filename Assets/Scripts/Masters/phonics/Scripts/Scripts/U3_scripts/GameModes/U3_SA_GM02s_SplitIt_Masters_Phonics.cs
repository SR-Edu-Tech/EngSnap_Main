using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U3_SA_GM02s_SplitIt_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration & Voice A Audio (Official Unit 3 Doc)")]
        [Tooltip("U03_VO_a2_intro: 'Now we split them in writing. Tap the gap where the word breaks, then press SPLIT.' (7s)")]
        public AudioClip introInstructionClip;

        [Tooltip("U03_VO_a2_r1: 'Round one. When two of the same consonant sit together, the split goes right between them.' (7s)")]
        public AudioClip voiceARound1; // Double consonants

        [Tooltip("U03_VO_a2_r2: 'Round two. Compound words are two words stuck together. Split them back apart.' (6s)")]
        public AudioClip voiceARound2; // Compound words

        [Tooltip("U03_VO_a2_r3: 'Round three — you already know this one from Unit One. Prefixes and suffixes always split off on their own.' (8s)")]
        public AudioClip voiceARound3; // Prefix / Suffix

        [Tooltip("U03_VO_a2_r4: 'Round four. Two different consonants in the middle? The split goes between them.' (6s)")]
        public AudioClip voiceARound4; // VCCV Two consonants

        [Tooltip("U03_VO_a2_r5: 'Round five, and this one’s a trick you’ll use forever. If a word ends in a consonant and l-e, count back three letters and split there. Turtle. Tur-tle.' (12s)")]
        public AudioClip voiceARound5; // Consonant + le

        [Tooltip("Celebration fanfare clip when activity 2 completes")]
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Splitter Word & Gap Container")]
        [SerializeField] private Transform letterTilesContainer;
        [SerializeField] private Button splitConfirmButton;
        [SerializeField] private Button replayWordAudioButton;
        [SerializeField] private GameObject resultDisplayObj;
        [SerializeField] private TextMeshProUGUI resultDisplayText;

        [Header("4. Sprites for Tiles & Divider Bar")]
        public Sprite letterTileSprite;
        public Sprite dividerBarSprite;

        private List<SyllableWordItem> rounds;
        private int currentRoundIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;
        private HashSet<int> userSelectedGaps = new HashSet<int>(); // 1-based index (after letter 1, after letter 2, etc.)
        private List<Image> spawnedDividerBars = new List<Image>();

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
        }

        private IEnumerator DelayedPlayIntro()
        {
            yield return new WaitForSeconds(0.15f);
            if (U3_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
            }
        }

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (promptTMP == null)
            {
                var t = transform.Find("PromptText") ?? transform.Find("Prompt_Text") ?? transform.Find("InstructionText");
                if (t != null) promptTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (scoreTMP == null)
            {
                var t = transform.Find("ScoreText") ?? transform.Find("Score_Text") ?? transform.Find("Score");
                if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("ProgressBar (1)") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            if (letterTilesContainer == null)
            {
                Transform t = transform.Find("Letter_Tiles_Container") ?? transform.Find("Tiles_Row") ?? transform.Find("LetterContainer") ?? transform.Find("WordTiles_Container");
                if (t != null) letterTilesContainer = t;
            }

            if (splitConfirmButton == null)
            {
                Transform t = transform.Find("Btn_Split") ?? transform.Find("SplitButton") ?? transform.Find("Btn_Submit");
                if (t != null) splitConfirmButton = t.GetComponent<Button>();
            }
            if (splitConfirmButton != null)
            {
                splitConfirmButton.onClick.RemoveAllListeners();
                splitConfirmButton.onClick.AddListener(OnSplitConfirmPressed);
            }

            if (replayWordAudioButton == null)
            {
                Transform t = transform.Find("ReplayButton") ?? transform.Find("AudioButton") ?? transform.Find("SpeakerButton") ?? transform.Find("Speaker_Button");
                if (t != null) replayWordAudioButton = t.GetComponent<Button>();
            }
            if (replayWordAudioButton != null)
            {
                replayWordAudioButton.onClick.RemoveAllListeners();
                replayWordAudioButton.onClick.AddListener(PlayCurrentWordAudio);
            }

            if (resultDisplayObj == null)
            {
                Transform t = transform.Find("Result_Display") ?? transform.Find("ResultDisplay") ?? transform.Find("SplitResult");
                if (t != null) resultDisplayObj = t.gameObject;
            }
            if (resultDisplayObj != null && resultDisplayText == null)
                resultDisplayText = resultDisplayObj.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void InitializeRounds()
        {
            rounds = new List<SyllableWordItem>
            {
                // Round 1: Double Consonants (Rule 2)
                new SyllableWordItem { word = "butter", hyphenatedSplit = "but-ter", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.DoubleConsonants },
                new SyllableWordItem { word = "rabbit", hyphenatedSplit = "rab-bit", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.DoubleConsonants },
                new SyllableWordItem { word = "pillow", hyphenatedSplit = "pil-low", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.DoubleConsonants },

                // Round 2: Compound Words (Rule 3)
                new SyllableWordItem { word = "mailbox", hyphenatedSplit = "mail-box", splitIndices = new[] { 4 }, primaryRule = SyllableSplitRule.CompoundWords },
                new SyllableWordItem { word = "doghouse", hyphenatedSplit = "dog-house", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.CompoundWords },
                new SyllableWordItem { word = "laptop", hyphenatedSplit = "lap-top", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.CompoundWords },

                // Round 3: Prefix / Suffix (Rule 6)
                new SyllableWordItem { word = "preview", hyphenatedSplit = "pre-view", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.PrefixSuffix },
                new SyllableWordItem { word = "kindness", hyphenatedSplit = "kind-ness", splitIndices = new[] { 4 }, primaryRule = SyllableSplitRule.PrefixSuffix },
                new SyllableWordItem { word = "unfold", hyphenatedSplit = "un-fold", splitIndices = new[] { 2 }, primaryRule = SyllableSplitRule.PrefixSuffix },

                // Round 4: Two Consonants VCCV (Rule 4)
                new SyllableWordItem { word = "elbow", hyphenatedSplit = "el-bow", splitIndices = new[] { 2 }, primaryRule = SyllableSplitRule.VCCVTwoConsonants },
                new SyllableWordItem { word = "doctor", hyphenatedSplit = "doc-tor", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.VCCVTwoConsonants },
                new SyllableWordItem { word = "harvest", hyphenatedSplit = "har-vest", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.VCCVTwoConsonants },

                // Round 5: Consonant + le (Rule 5)
                new SyllableWordItem { word = "turtle", hyphenatedSplit = "tur-tle", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.ConsonantLE },
                new SyllableWordItem { word = "cradle", hyphenatedSplit = "cra-dle", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.ConsonantLE },
                new SyllableWordItem { word = "candle", hyphenatedSplit = "can-dle", splitIndices = new[] { 3 }, primaryRule = SyllableSplitRule.ConsonantLE }
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
            userSelectedGaps.Clear();
            SyllableWordItem item = rounds[currentRoundIndex];

            if (titleTMP != null) titleTMP.text = "<b>SPLIT IT</b>";

            string rulePrompt = GetRulePromptText(item.primaryRule);
            if (promptTMP != null) promptTMP.text = $"<color=#FFFFFF><b>{rulePrompt}</b></color>";

            if (progressBar != null)
            {
                progressBar.value = (float)currentRoundIndex / rounds.Count;
            }

            if (resultDisplayText != null)
            {
                resultDisplayText.text = "<b><color=#000000>?</color></b>";
            }

            RenderLetterTilesWithGaps(item.word);
            PlayCurrentWordAudio();
        }

        public void PlayCurrentWordAudio()
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

        private string GetRulePromptText(SyllableSplitRule rule)
        {
            switch (rule)
            {
                case SyllableSplitRule.DoubleConsonants:
                    return "Rule 2: Split between double consonants!";
                case SyllableSplitRule.CompoundWords:
                    return "Rule 3: Split between compound words!";
                case SyllableSplitRule.PrefixSuffix:
                    return "Rule 6: Split off prefixes and suffixes!";
                case SyllableSplitRule.VCCVTwoConsonants:
                    return "Rule 4: Split between two consonants (VCCV)!";
                case SyllableSplitRule.ConsonantLE:
                    return "Rule 5: Consonant + le (Count back 3 letters)!";
                default:
                    return "Tap the gap where the word breaks, then press SPLIT!";
            }
        }

        private Sprite generatedTileSprite;
        private Sprite generatedDividerSprite;

        private Sprite GetRoundedTileSprite()
        {
            if (generatedTileSprite != null) return generatedTileSprite;

            int w = 120;
            int h = 150;
            int r = 32; // 32px smooth corner curve
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color faceColor = new Color(0.98f, 0.98f, 1f, 1f);
            Color borderColor = new Color(0.65f, 0.75f, 0.88f, 1f);
            Color shadowColor = new Color(0.4f, 0.5f, 0.65f, 0.6f);

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
                    else if (dist > r - 5f || y < 6 || y > h - 6 || x < 6 || x > w - 6)
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

            generatedTileSprite = Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                100f
            );

            return generatedTileSprite;
        }

        private Sprite GetDividerBarSprite()
        {
            if (generatedDividerSprite != null) return generatedDividerSprite;

            int w = 32;
            int h = 150;
            int r = 14;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color barColor = new Color(0.08f, 0.72f, 0.98f, 1f);
            Color trimColor = new Color(0.02f, 0.45f, 0.75f, 1f);

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
                        colors[y * w + x] = new Color(trimColor.r, trimColor.g, trimColor.b, alpha);
                    }
                    else if (dist > r - 3f || x < 3 || x > w - 3)
                    {
                        colors[y * w + x] = trimColor;
                    }
                    else
                    {
                        colors[y * w + x] = barColor;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            generatedDividerSprite = Sprite.Create(
                tex,
                new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f),
                100f
            );

            return generatedDividerSprite;
        }

        private void RenderLetterTilesWithGaps(string word)
        {
            if (letterTilesContainer == null) return;

            foreach (Transform child in letterTilesContainer)
            {
                Destroy(child.gameObject);
            }
            spawnedDividerBars.Clear();

            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];

                // 1. Large Rounded Letter Tile (100x125 px)
                GameObject tile = new GameObject($"Tile_{i}_{c}", typeof(RectTransform), typeof(Image));
                tile.transform.SetParent(letterTilesContainer, false);
                RectTransform tileRT = tile.GetComponent<RectTransform>();
                tileRT.sizeDelta = new Vector2(100, 125);
                
                Image tileImg = tile.GetComponent<Image>();
                tileImg.sprite = GetRoundedTileSprite();
                tileImg.type = Image.Type.Simple;
                tileImg.color = Color.white;

                GameObject letterText = new GameObject("Letter", typeof(RectTransform), typeof(TextMeshProUGUI));
                letterText.transform.SetParent(tile.transform, false);
                RectTransform textRT = letterText.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;

                var tmp = letterText.GetComponent<TextMeshProUGUI>();
                tmp.text = $"<b><color=#000000>{c}</color></b>";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 58;

                // 2. Wide Tappable Gap with Large Prominent Divider Bar
                if (i < word.Length - 1)
                {
                    int gapIndex = i + 1; // 1-based index after letter (i + 1)
                    GameObject gapObj = new GameObject($"Gap_{gapIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
                    gapObj.transform.SetParent(letterTilesContainer, false);
                    RectTransform gapRT = gapObj.GetComponent<RectTransform>();
                    gapRT.sizeDelta = new Vector2(40, 125);

                    Image gapImg = gapObj.GetComponent<Image>();
                    gapImg.color = new Color(0.1f, 0.7f, 1f, 0.05f); // invisible hit area

                    // Large glowing vertical divider bar
                    GameObject barObj = new GameObject("DividerBar", typeof(RectTransform), typeof(Image));
                    barObj.transform.SetParent(gapObj.transform, false);
                    RectTransform barRT = barObj.GetComponent<RectTransform>();
                    barRT.sizeDelta = new Vector2(18, 120);

                    Image barImg = barObj.GetComponent<Image>();
                    barImg.sprite = GetDividerBarSprite();
                    barImg.type = Image.Type.Simple;
                    barImg.color = Color.white; // Full vibrant sprite color
                    barObj.SetActive(false);
                    spawnedDividerBars.Add(barImg);

                    Button gapBtn = gapObj.GetComponent<Button>();
                    gapBtn.onClick.AddListener(() => OnGapTapped(gapIndex, barObj));
                }
            }
        }

        private void OnGapTapped(int gapIndex, GameObject barObj)
        {
            if (isProcessingAnswer) return;

            if (userSelectedGaps.Contains(gapIndex))
            {
                userSelectedGaps.Remove(gapIndex);
                barObj.SetActive(false);
                if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlaySplitUndo();
                }
            }
            else
            {
                userSelectedGaps.Add(gapIndex);
                barObj.SetActive(true);
                if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlaySplitClick();
                }
            }
        }

        public void OnSplitConfirmPressed()
        {
            if (isProcessingAnswer || currentRoundIndex >= rounds.Count) return;

            SyllableWordItem item = rounds[currentRoundIndex];
            bool isCorrect = (userSelectedGaps.Count == item.splitIndices.Length);
            if (isCorrect)
            {
                foreach (int idx in item.splitIndices)
                {
                    if (!userSelectedGaps.Contains(idx))
                    {
                        isCorrect = false;
                        break;
                    }
                }
            }

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectSplit(item));
            }
            else
            {
                StartCoroutine(HandleWrongSplit(item));
            }
        }

        private IEnumerator HandleCorrectSplit(SyllableWordItem item)
        {
            isProcessingAnswer = true;

            if (resultDisplayText != null)
            {
                resultDisplayText.text = $"<b><color=#000000>{item.hyphenatedSplit}</color></b>";
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFFFFF><b>Perfect! \"{item.word}\" splits into: {item.hyphenatedSplit}</b></color>";
            }

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();

                AudioClip sepClip = item.separatedAudio ?? Resources.Load<AudioClip>($"U3_audio/sep_{item.word}") ?? Resources.Load<AudioClip>($"Audio/U3_audio/sep_{item.word}");
                if (sepClip != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(sepClip);
                }
            }

            currentScore += 100;
            UpdateScoreUI();

            yield return new WaitForSeconds(1.5f);

            currentRoundIndex++;
            LoadCurrentRound();
        }

        private IEnumerator HandleWrongSplit(SyllableWordItem item)
        {
            isProcessingAnswer = true;

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<color=#FFD54F><b>Check the rule: {GetRulePromptText(item.primaryRule)} -> {item.hyphenatedSplit}</b></color>";
            }

            yield return new WaitForSeconds(1.8f);

            isProcessingAnswer = false;
            LoadCurrentRound();
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
        }

        private void OnAllRoundsComplete()
        {
            if (promptTMP != null) promptTMP.text = "<color=#FFFFFF><b>All 5 Splitting Rules Mastered!</b></color>";
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
    }
}
