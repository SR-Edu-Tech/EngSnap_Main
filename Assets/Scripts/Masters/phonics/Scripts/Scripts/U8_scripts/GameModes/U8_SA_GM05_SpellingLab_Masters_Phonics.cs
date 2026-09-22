using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U8_SA_GM05_SpellingLab_Masters_Phonics : MonoBehaviour
    {
        [Header("Item Pool (15 items)")]
        public List<SpellingLabItem> items;
        private int currentIndex = 0;
        private int score = 0;
        private int firstAttemptCorrectCount = 0;
        private bool isProcessing = false;
        private bool isFirstAttemptOnItem = true;

        [Header("Prompt Display UI")]
        [SerializeField] private TextMeshProUGUI promptWordText;
        [SerializeField] private TextMeshProUGUI promptRuleHintText;

        [Header("3 Choice Tiles")]
        [SerializeField] private Button erTileBtn;
        [SerializeField] private Button irTileBtn;
        [SerializeField] private Button urTileBtn;

        [Header("Tile Colors")]
        public Color defaultTileColor = new Color(0.12f, 0.16f, 0.24f, 1f);
        public Color correctTileColor = new Color(0.12f, 0.72f, 0.35f, 1f);
        public Color wrongTileColor = new Color(0.92f, 0.25f, 0.25f, 1f);

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioBtn;

        [Header("SFX")]
        public AudioClip correctSFX;
        public AudioClip wrongSFX;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            StartActivity();
        }

        public void AutoBindHierarchyElements()
        {
            if (items == null || items.Count == 0)
                items = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity5Items();

            Transform root = transform;

            if (promptWordText == null)
            {
                Transform t = root.Find("WordContainer/PromptWordText") ?? root.Find("PromptText") ?? root.Find("WordText");
                if (t != null) promptWordText = t.GetComponent<TextMeshProUGUI>();
            }
            if (promptRuleHintText == null)
            {
                Transform t = root.Find("WordContainer/RuleHintText") ?? root.Find("HintText");
                if (t != null) promptRuleHintText = t.GetComponent<TextMeshProUGUI>();
            }

            if (erTileBtn == null)
            {
                Transform t = root.Find("Tiles/Tile_er") ?? root.Find("Tile_er") ?? root.Find("ErBtn");
                if (t != null) erTileBtn = t.GetComponent<Button>();
            }
            if (irTileBtn == null)
            {
                Transform t = root.Find("Tiles/Tile_ir") ?? root.Find("Tile_ir") ?? root.Find("IrBtn");
                if (t != null) irTileBtn = t.GetComponent<Button>();
            }
            if (urTileBtn == null)
            {
                Transform t = root.Find("Tiles/Tile_ur") ?? root.Find("Tile_ur") ?? root.Find("UrBtn");
                if (t != null) urTileBtn = t.GetComponent<Button>();
            }

            if (replayAudioBtn == null)
            {
                Transform r = root.Find("ReplayAudioBtn") 
                           ?? root.Find("ReplayButton") 
                           ?? root.Find("ReplayAudioButton")
                           ?? root.Find("HeaderRibbon/ReplayAudioBtn")
                           ?? root.Find("Speaker_Button");
                if (r != null) replayAudioBtn = r.GetComponent<Button>();
            }

            if (progressText == null)
            {
                Transform pt = root.Find("ProgressHUD/ProgressText") 
                            ?? root.Find("ProgressHUD/Progress_Text") 
                            ?? root.Find("ProgressText")
                            ?? root.Find("HUD/ProgressText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }
            if (scoreText == null)
            {
                Transform st = root.Find("ScoreHUD/ScoreText") 
                            ?? root.Find("ScoreHUD/Score_Text") 
                            ?? root.Find("ScoreText")
                            ?? root.Find("HUD/ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }
            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            AttachListeners();
        }

        private void AttachListeners()
        {
            if (erTileBtn != null)
            {
                erTileBtn.onClick.RemoveAllListeners();
                erTileBtn.onClick.AddListener(() => OnTileSelected("er"));
            }
            if (irTileBtn != null)
            {
                irTileBtn.onClick.RemoveAllListeners();
                irTileBtn.onClick.AddListener(() => OnTileSelected("ir"));
            }
            if (urTileBtn != null)
            {
                urTileBtn.onClick.RemoveAllListeners();
                urTileBtn.onClick.AddListener(() => OnTileSelected("ur"));
            }

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentWordAudio);
            }
        }

        public void StartActivity()
        {
            currentIndex = 0;
            score = 0;
            firstAttemptCorrectCount = 0;
            isProcessing = false;

            AudioClip introClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_a5_intro");
            if (introClip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(introClip);

            LoadCurrentItem();
        }

        private void LoadCurrentItem()
        {
            if (currentIndex >= items.Count)
            {
                StartCoroutine(HandleActivityCompleted());
                return;
            }

            var item = items[currentIndex];
            isFirstAttemptOnItem = true;
            isProcessing = false;

            ResetTileVisuals();

            if (promptWordText != null)
            {
                promptWordText.text = $"<b>{item.displayPrompt}</b>";
            }

            if (promptRuleHintText != null)
            {
                promptRuleHintText.text = item.isRuleReward ? "<color=#00E5FF><b>Tip:</b> Quiet /ər/ at the END of a word?</color>" : "";
            }

            UpdateHUD();
            ReplayCurrentWordAudio();
        }

        public void ReplayCurrentWordAudio()
        {
            if (currentIndex < items.Count)
            {
                var item = items[currentIndex];
                PlayWordAudio(item.word, item.wordAudio);
            }
        }

        private void PlayWordAudio(string word, AudioClip overrideClip)
        {
            if (overrideClip != null)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(overrideClip);
                return;
            }
            AudioClip clip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio($"U08_WRD_{word}") ??
                             U8_SA_AudioManager_Masters_Phonics.ResolveAudio(word);
            if (clip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(clip);
        }

        public void OnTileSelected(string chosenTeam)
        {
            if (isProcessing || currentIndex >= items.Count) return;
            isProcessing = true;

            var item = items[currentIndex];
            bool isCorrect = (chosenTeam.ToLower() == item.correctTeam.ToLower());

            Button chosenBtn = (chosenTeam == "er") ? erTileBtn : (chosenTeam == "ir") ? irTileBtn : urTileBtn;

            if (isCorrect)
            {
                if (isFirstAttemptOnItem) firstAttemptCorrectCount++;
                score += 20;
                UpdateHUD();

                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordWordsSpelled(1);

                StartCoroutine(HandleCorrectFeedback(chosenBtn, item));
            }
            else
            {
                isFirstAttemptOnItem = false;
                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordMistake(item.word);

                StartCoroutine(HandleWrongFeedback(chosenBtn, item));
            }
        }

        private IEnumerator HandleCorrectFeedback(Button btn, SpellingLabItem item)
        {
            if (btn != null)
            {
                Image img = btn.GetComponent<Image>();
                if (img != null) img.color = correctTileColor;
            }

            if (promptWordText != null)
            {
                promptWordText.text = $"<b>{item.word}</b>";
            }

            PlaySFX(correctSFX, true);
            yield return new WaitForSeconds(0.8f);

            currentIndex++;
            LoadCurrentItem();
        }

        private IEnumerator HandleWrongFeedback(Button wrongBtn, SpellingLabItem item)
        {
            if (wrongBtn != null)
            {
                Image img = wrongBtn.GetComponent<Image>();
                if (img != null) img.color = wrongTileColor;
            }

            PlaySFX(wrongSFX, false);
            yield return new WaitForSeconds(0.6f);

            // Play explanation that they sound identical
            AudioClip wrongExplanation = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_a5_wrong");
            if (wrongExplanation != null)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(wrongExplanation);
                yield return new WaitForSeconds(wrongExplanation.length + 0.3f);
            }

            // Highlight the correct tile
            Button correctBtn = (item.correctTeam == "er") ? erTileBtn : (item.correctTeam == "ir") ? irTileBtn : urTileBtn;
            if (correctBtn != null)
            {
                Image img = correctBtn.GetComponent<Image>();
                if (img != null) img.color = correctTileColor;
            }

            if (promptWordText != null)
            {
                promptWordText.text = $"<b>{item.word}</b>";
            }

            yield return new WaitForSeconds(1.2f);

            ResetTileVisuals();
            if (promptWordText != null) promptWordText.text = $"<b>{item.displayPrompt}</b>";
            isProcessing = false;
        }

        private void ResetTileVisuals()
        {
            if (erTileBtn != null)
            {
                Image img = erTileBtn.GetComponent<Image>();
                if (img != null) img.color = defaultTileColor;
            }
            if (irTileBtn != null)
            {
                Image img = irTileBtn.GetComponent<Image>();
                if (img != null) img.color = defaultTileColor;
            }
            if (urTileBtn != null)
            {
                Image img = urTileBtn.GetComponent<Image>();
                if (img != null) img.color = defaultTileColor;
            }
        }

        private void PlaySFX(AudioClip clip, bool isCorrect)
        {
            if (clip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX(clip);
            else
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayAnswerFeedbackSFX(isCorrect);
        }

        private void UpdateHUD()
        {
            if (progressBar != null)
            {
                progressBar.maxValue = items.Count;
                progressBar.value = currentIndex + 1;
                U8_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
            }

            if (progressText != null)
                progressText.text = $"<b>Item {currentIndex + 1} of {items.Count}</b>";

            if (scoreText != null)
                scoreText.text = $"Score: <b>{score}</b>";
        }

        private IEnumerator HandleActivityCompleted()
        {
            isProcessing = true;
            if (progressBar != null)
            {
                progressBar.value = items.Count;
            }

            int earnedStars = 1;
            if (firstAttemptCorrectCount >= 13) earnedStars = 3;
            else if (firstAttemptCorrectCount >= 10) earnedStars = 2;

            if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(5, earnedStars);
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(score);
            }

            yield return new WaitForSeconds(0.8f);

            U8_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform,
                "Activity 5 — Spelling Lab",
                earnedStars,
                score,
                () => {
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenUnitChallenge();
                }
            );
        }
    }
}
