using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U5_SA_GM04_LongOrShort_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Instruction & Audio Clips")]
        [Tooltip("U05_VO_a2_intro: Ears only this time. No word on screen. Listen, and tell me which vowel sound you heard.")]
        public AudioClip introInstructionClip;
        [Tooltip("U05_VO_a2_pair: Listen to both. Hear the door close?")]
        public AudioClip pairContrastClip;

        [Header("Audio SFX Clips (Inspector Overrides)")]
        [Tooltip("Plays on correct answer (Default: Correct SFX)")]
        public AudioClip correctSFX;
        [Tooltip("Plays on wrong answer (Default: Incorrect SFX)")]
        public AudioClip wrongSFX;
        [Tooltip("Plays on open door / long vowel (Default: Vowel Stretch SFX)")]
        public AudioClip vowelStretchSFX;
        [Tooltip("Plays on closed door / short vowel (Default: Vowel Snap SFX)")]
        public AudioClip vowelSnapSFX;

        [Header("2. UI Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioButton;

        [Header("3. Vowel Sound Option Buttons (4 Buttons)")]
        [SerializeField] private Button optionBtn1;
        [SerializeField] private Button optionBtn2;
        [SerializeField] private Button optionBtn3;
        [SerializeField] private Button optionBtn4;

        [Header("4. Center Word Reveal Anchor")]
        [SerializeField] private TextMeshProUGUI revealedWordTMP;
        [SerializeField] private GameObject speakerIconGraphic;

        private List<LongOrShortItem> items;
        private int currentIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;
        private Sprite proceduralRoundedSprite;

        private void Awake()
        {
            AutoBindHierarchyElements();
            InitializeItems();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            currentIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            InitializeItems();
            LoadCurrentItem();

            if (U5_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
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

            if (progressTMP == null)
            {
                var t = transform.Find("ProgressText") ?? transform.Find("Progress_Text") ?? transform.Find("HUD/ProgressText") ?? transform.Find("ProgressBar/ProgressText");
                if (t != null) progressTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            if (revealedWordTMP == null)
            {
                Transform t = transform.Find("RevealedWord_Text") ?? transform.Find("WordReveal") ?? transform.Find("CenterWord");
                if (t != null) revealedWordTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (speakerIconGraphic == null)
            {
                Transform t = transform.Find("Speaker_Prompt") ?? transform.Find("AudioPrompt") ?? transform.Find("CenterSpeaker");
                if (t != null) speakerIconGraphic = t.gameObject;
            }

            Transform optRow = transform.Find("Options_Row") ?? transform.Find("VowelOptions") ?? transform.Find("Buttons_Row") ?? transform;
            if (optionBtn1 == null) optionBtn1 = FindBtn(optRow, "Option_1", "Btn_1", "1");
            if (optionBtn2 == null) optionBtn2 = FindBtn(optRow, "Option_2", "Btn_2", "2");
            if (optionBtn3 == null) optionBtn3 = FindBtn(optRow, "Option_3", "Btn_3", "3");
            if (optionBtn4 == null) optionBtn4 = FindBtn(optRow, "Option_4", "Btn_4", "4");

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
                    PlayCurrentItemAudio();
                });
            }
        }

        private Button FindBtn(Transform parent, params string[] names)
        {
            foreach (var n in names)
            {
                Transform t = parent.Find(n);
                if (t != null) return t.GetComponent<Button>();
            }
            return null;
        }

        private void InitializeItems()
        {
            // 14 minimal pairs in structured progression
            items = new List<LongOrShortItem>
            {
                new LongOrShortItem { word = "go", correctVowel = VowelSoundCategory.LongO, vowelLabel = "long o (/oh/)", optionLabels = new[] { "short o", "long o", "short e", "long e" }, correctOptionIndex = 1, doorType = SyllableDoorType.Open, minimalPairWord = "got" },
                new LongOrShortItem { word = "got", correctVowel = VowelSoundCategory.ShortO, vowelLabel = "short o (/ah/)", optionLabels = new[] { "short o", "long o", "short u", "long a" }, correctOptionIndex = 0, doorType = SyllableDoorType.Closed, minimalPairWord = "go" },
                new LongOrShortItem { word = "me", correctVowel = VowelSoundCategory.LongE, vowelLabel = "long e (/ee/)", optionLabels = new[] { "short e", "long e", "short a", "long i" }, correctOptionIndex = 1, doorType = SyllableDoorType.Open, minimalPairWord = "met" },
                new LongOrShortItem { word = "met", correctVowel = VowelSoundCategory.ShortE, vowelLabel = "short e (/eh/)", optionLabels = new[] { "short e", "long e", "short i", "long o" }, correctOptionIndex = 0, doorType = SyllableDoorType.Closed, minimalPairWord = "me" },
                new LongOrShortItem { word = "hi", correctVowel = VowelSoundCategory.LongI, vowelLabel = "long i (/eye/)", optionLabels = new[] { "short i", "long i", "short e", "long e" }, correctOptionIndex = 1, doorType = SyllableDoorType.Open, minimalPairWord = "hip" },
                new LongOrShortItem { word = "hip", correctVowel = VowelSoundCategory.ShortI, vowelLabel = "short i (/ih/)", optionLabels = new[] { "short i", "long i", "short e", "long a" }, correctOptionIndex = 0, doorType = SyllableDoorType.Closed, minimalPairWord = "hi" },
                new LongOrShortItem { word = "be", correctVowel = VowelSoundCategory.LongE, vowelLabel = "long e (/ee/)", optionLabels = new[] { "short e", "long e", "short i", "long u" }, correctOptionIndex = 1, doorType = SyllableDoorType.Open, minimalPairWord = "bed" },
                new LongOrShortItem { word = "bed", correctVowel = VowelSoundCategory.ShortE, vowelLabel = "short e (/eh/)", optionLabels = new[] { "short e", "long e", "short a", "long o" }, correctOptionIndex = 0, doorType = SyllableDoorType.Closed, minimalPairWord = "be" },
                new LongOrShortItem { word = "so", correctVowel = VowelSoundCategory.LongO, vowelLabel = "long o (/oh/)", optionLabels = new[] { "short o", "long o", "short u", "long i" }, correctOptionIndex = 1, doorType = SyllableDoorType.Open, minimalPairWord = "sob" },
                new LongOrShortItem { word = "sob", correctVowel = VowelSoundCategory.ShortO, vowelLabel = "short o (/ah/)", optionLabels = new[] { "short o", "long o", "short a", "long e" }, correctOptionIndex = 0, doorType = SyllableDoorType.Closed, minimalPairWord = "so" },
                new LongOrShortItem { word = "we", correctVowel = VowelSoundCategory.LongE, vowelLabel = "long e (/ee/)", optionLabels = new[] { "short e", "long e", "short a", "long a" }, correctOptionIndex = 1, doorType = SyllableDoorType.Open, minimalPairWord = "wet" },
                new LongOrShortItem { word = "wet", correctVowel = VowelSoundCategory.ShortE, vowelLabel = "short e (/eh/)", optionLabels = new[] { "short e", "long e", "short i", "long o" }, correctOptionIndex = 0, doorType = SyllableDoorType.Closed, minimalPairWord = "we" },
                new LongOrShortItem { word = "flu", correctVowel = VowelSoundCategory.LongU, vowelLabel = "long u (/you/)", optionLabels = new[] { "short u", "long u", "short o", "long o" }, correctOptionIndex = 1, doorType = SyllableDoorType.Open, minimalPairWord = "club" },
                new LongOrShortItem { word = "club", correctVowel = VowelSoundCategory.ShortU, vowelLabel = "short u (/uh/)", optionLabels = new[] { "short u", "long u", "short o", "long e" }, correctOptionIndex = 0, doorType = SyllableDoorType.Closed, minimalPairWord = "flu" }
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
            LongOrShortItem item = items[currentIndex];

            if (titleTMP != null)
            {
                titleTMP.text = $"<b><color=#FFFFFF>LONG OR SHORT? ({currentIndex + 1} OF {items.Count})</color></b>";
                titleTMP.enableAutoSizing = true;
                titleTMP.fontSizeMin = 28;
                titleTMP.fontSizeMax = 44;
            }
            if (promptTMP != null)
            {
                promptTMP.text = "<color=#FFFFFF><b>Which vowel sound did you hear? Listen and tap!</b></color>";
                promptTMP.enableAutoSizing = true;
                promptTMP.fontSizeMin = 20;
                promptTMP.fontSizeMax = 30;
            }
            if (progressBar != null)
            {
                progressBar.value = (float)currentIndex / items.Count;
            }
            if (progressTMP != null)
            {
                progressTMP.text = $"{Mathf.Min(currentIndex + 1, items.Count)} / {items.Count}";
            }

            // Hide text before answering
            if (revealedWordTMP != null)
            {
                revealedWordTMP.gameObject.SetActive(false);
            }
            if (speakerIconGraphic != null)
            {
                speakerIconGraphic.SetActive(true);
            }

            // Setup buttons
            SetupOptionButton(optionBtn1, 0, item);
            SetupOptionButton(optionBtn2, 1, item);
            SetupOptionButton(optionBtn3, 2, item);
            SetupOptionButton(optionBtn4, 3, item);

            PlayCurrentItemAudio();
        }

        private void SetupOptionButton(Button btn, int idx, LongOrShortItem item)
        {
            if (btn == null) return;
            btn.gameObject.SetActive(true);

            Image img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = GetRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }

            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null && idx < item.optionLabels.Length)
            {
                tmp.text = $"<b><size=38><color=#000000>{item.optionLabels[idx]}</color></size></b>";
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (isProcessingAnswer) return;
                isProcessingAnswer = true;
                StartCoroutine(HandleOptionSelected(btn, idx, item));
            });
        }

        private IEnumerator HandleOptionSelected(Button btn, int selectedIndex, LongOrShortItem item)
        {
            StartCoroutine(PunchScale(btn.transform, 1.15f));

            if (selectedIndex == item.correctOptionIndex)
            {
                currentScore += 100;
                UpdateScoreUI();

                if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                {
                    U5_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(100);
                }

                PlayCorrectFeedbackAudio(item.doorType);

                Image img = btn.GetComponent<Image>();
                if (img != null) img.color = new Color(0.85f, 1f, 0.85f);

                // Reveal word with door state indicator
                if (revealedWordTMP != null)
                {
                    string doorStateText = item.doorType == SyllableDoorType.Open ? "[ OPEN GATE ]" : "[ CLOSED DOOR ]";
                    string doorColor = item.doorType == SyllableDoorType.Open ? "#1565C0" : "#C62828";
                    revealedWordTMP.text = $"<b><size=64><color=#000000>{item.word}</color></size></b>\n<size=28><color={doorColor}><b>{doorStateText} -> {item.vowelLabel}</b></color></size>";
                    revealedWordTMP.gameObject.SetActive(true);
                }

                yield return new WaitForSeconds(1.0f);
                currentIndex++;
                LoadCurrentItem();
            }
            else
            {
                PlayWrongFeedbackAudio();

                float wrongAudioDuration = 1.4f;
                if (wrongSFX != null)
                {
                    wrongAudioDuration = wrongSFX.length;
                }
                else if (U5_SA_AudioManager_Masters_Phonics.Instance != null && U5_SA_AudioManager_Masters_Phonics.Instance.wrongClip != null)
                {
                    wrongAudioDuration = U5_SA_AudioManager_Masters_Phonics.Instance.wrongClip.length;
                }

                Image img = btn.GetComponent<Image>();
                if (img != null) img.color = new Color(1f, 0.85f, 0.85f);

                yield return new WaitForSeconds(Mathf.Max(1.2f, wrongAudioDuration + 0.25f));
                currentIndex++;
                LoadCurrentItem();
            }
        }

        private void PlayCorrectFeedbackAudio(SyllableDoorType doorType)
        {
            if (correctSFX != null)
            {
                if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlaySFXClip(correctSFX);
            }
            else if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();
            }

            if (doorType == SyllableDoorType.Open)
            {
                if (vowelStretchSFX != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFXClip(vowelStretchSFX);
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayVowelStretch();
            }
            else
            {
                if (vowelSnapSFX != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFXClip(vowelSnapSFX);
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayVowelSnap();
            }
        }

        private void PlayWrongFeedbackAudio()
        {
            if (wrongSFX != null)
            {
                if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlaySFXClip(wrongSFX);
            }
            else if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
            if (progressTMP != null && items != null)
            {
                progressTMP.text = $"{Mathf.Min(currentIndex + 1, items.Count)} / {items.Count}";
            }
        }

        public void PlayCurrentItemAudio()
        {
            if (currentIndex >= items.Count) return;
            string word = items[currentIndex].word;
            PlayWordAudio(word);
        }

        private void PlayWordAudio(string word)
        {
            if (string.IsNullOrEmpty(word)) return;
            word = word.Trim().ToLower();

            if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayWordAudio(word);
            }
        }

        private void OnActivityComplete()
        {
            if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U5_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
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
                    float dx = Mathf.Max(0, Mathf.Abs(x - size / 2f) - (size / 2f - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - size / 2f) - (size / 2f - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    colors[y * size + x] = dist <= radius ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return proceduralRoundedSprite;
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
