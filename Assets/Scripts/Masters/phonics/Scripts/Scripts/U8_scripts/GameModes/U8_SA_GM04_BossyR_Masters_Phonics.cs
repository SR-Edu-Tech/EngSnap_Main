using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U8_SA_GM04_BossyR_Masters_Phonics : MonoBehaviour
    {
        [Header("Item Data")]
        public List<BossyRMinimalPairItem> items;
        private int currentIndex = 0;
        private int score = 0;
        private int firstAttemptCorrectCount = 0;
        private bool isProcessing = false;
        private bool isFirstAttemptOnItem = true;

        [Header("UI Elements")]
        [SerializeField] private Button noRButton;
        [SerializeField] private Button withRButton;
        [SerializeField] private TextMeshProUGUI noRWordText;
        [SerializeField] private TextMeshProUGUI withRWordText;
        [SerializeField] private TextMeshProUGUI noRSubText;
        [SerializeField] private TextMeshProUGUI withRSubText;

        [Header("Speaker Buttons")]
        [SerializeField] private Button noRSpeakerBtn;
        [SerializeField] private Button withRSpeakerBtn;
        [SerializeField] private Button replayAudioBtn;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Slider progressBar;

        [Header("Visual Feedback Colors")]
        public Color defaultBtnColor = new Color(0.12f, 0.16f, 0.24f, 1f);
        public Color correctBtnColor = new Color(0.12f, 0.72f, 0.35f, 1f);
        public Color wrongBtnColor = new Color(0.92f, 0.25f, 0.25f, 1f);

        [Header("SFX (Overrides)")]
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
                items = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity1Items();

            Transform root = transform;

            if (noRButton == null)
            {
                Transform t = root.Find("Options/NoR_Button") ?? root.Find("NoR_Btn") ?? root.Find("Button_NoR") ?? root.Find("LeftOptionBtn");
                if (t != null) noRButton = t.GetComponent<Button>();
            }
            if (withRButton == null)
            {
                Transform t = root.Find("Options/WithR_Button") ?? root.Find("WithR_Btn") ?? root.Find("Button_WithR") ?? root.Find("RightOptionBtn");
                if (t != null) withRButton = t.GetComponent<Button>();
            }

            if (noRButton != null)
            {
                if (noRWordText == null)
                {
                    Transform wt = noRButton.transform.Find("WordText") ?? noRButton.transform.Find("Text");
                    if (wt != null) noRWordText = wt.GetComponent<TextMeshProUGUI>();
                    else noRWordText = noRButton.GetComponentInChildren<TextMeshProUGUI>();
                }
                if (noRSpeakerBtn == null)
                {
                    Transform st = noRButton.transform.Find("SpeakerBtn") ?? noRButton.transform.Find("Speaker");
                    if (st != null) noRSpeakerBtn = st.GetComponent<Button>();
                }
            }

            if (withRButton != null)
            {
                if (withRWordText == null)
                {
                    Transform wt = withRButton.transform.Find("WordText") ?? withRButton.transform.Find("Text");
                    if (wt != null) withRWordText = wt.GetComponent<TextMeshProUGUI>();
                    else withRWordText = withRButton.GetComponentInChildren<TextMeshProUGUI>();
                }
                if (withRSpeakerBtn == null)
                {
                    Transform st = withRButton.transform.Find("SpeakerBtn") ?? withRButton.transform.Find("Speaker");
                    if (st != null) withRSpeakerBtn = st.GetComponent<Button>();
                }
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
            if (noRButton != null)
            {
                noRButton.onClick.RemoveAllListeners();
                noRButton.onClick.AddListener(() => OnOptionChosen(false));
            }
            if (withRButton != null)
            {
                withRButton.onClick.RemoveAllListeners();
                withRButton.onClick.AddListener(() => OnOptionChosen(true));
            }

            if (noRSpeakerBtn != null)
            {
                noRSpeakerBtn.onClick.RemoveAllListeners();
                noRSpeakerBtn.onClick.AddListener(() =>
                {
                    if (currentIndex < items.Count)
                        PlayWordAudio(items[currentIndex].withoutRWord, items[currentIndex].withoutRAudio);
                });
            }
            if (withRSpeakerBtn != null)
            {
                withRSpeakerBtn.onClick.RemoveAllListeners();
                withRSpeakerBtn.onClick.AddListener(() =>
                {
                    if (currentIndex < items.Count)
                        PlayWordAudio(items[currentIndex].withRWord, items[currentIndex].withRAudio);
                });
            }

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentQuestionAudio);
            }
        }

        public void StartActivity()
        {
            currentIndex = 0;
            score = 0;
            firstAttemptCorrectCount = 0;
            isProcessing = false;

            AudioClip introClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_a1_intro");
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

            ResetButtonVisuals();

            if (noRWordText != null)
                noRWordText.text = $"<b>{item.withoutRWord}</b>";
            if (withRWordText != null)
                withRWordText.text = $"<b>{item.withRWord}</b>";

            UpdateHUD();
            ReplayCurrentQuestionAudio();
        }

        public void ReplayCurrentQuestionAudio()
        {
            if (currentIndex < items.Count)
            {
                var item = items[currentIndex];
                string targetWord = item.targetIsWithR ? item.withRWord : item.withoutRWord;
                AudioClip targetClip = item.targetIsWithR ? item.withRAudio : item.withoutRAudio;
                PlayWordAudio(targetWord, targetClip);
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

        private void OnOptionChosen(bool choseWithR)
        {
            if (isProcessing || currentIndex >= items.Count) return;
            isProcessing = true;

            var item = items[currentIndex];
            bool isCorrect = (choseWithR == item.targetIsWithR);

            Button selectedBtn = choseWithR ? withRButton : noRButton;
            Button otherBtn = choseWithR ? noRButton : withRButton;

            if (isCorrect)
            {
                if (isFirstAttemptOnItem) firstAttemptCorrectCount++;
                score += 15;
                UpdateHUD();
                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordWordsRead(1);

                StartCoroutine(HandleCorrectFeedback(selectedBtn, item));
            }
            else
            {
                isFirstAttemptOnItem = false;
                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordMistake(item.withRWord);

                StartCoroutine(HandleWrongFeedback(selectedBtn, otherBtn, item));
            }
        }

        private IEnumerator HandleCorrectFeedback(Button btn, BossyRMinimalPairItem item)
        {
            if (btn != null)
            {
                Image img = btn.GetComponent<Image>();
                if (img != null) img.color = correctBtnColor;
            }

            PlaySFX(correctSFX, true);
            yield return new WaitForSeconds(0.8f);

            currentIndex++;
            LoadCurrentItem();
        }

        private IEnumerator HandleWrongFeedback(Button wrongBtn, Button correctBtn, BossyRMinimalPairItem item)
        {
            if (wrongBtn != null)
            {
                Image img = wrongBtn.GetComponent<Image>();
                if (img != null) img.color = wrongBtnColor;
            }

            PlaySFX(wrongSFX, false);
            yield return new WaitForSeconds(1.0f);

            // Re-play contrast back-to-back
            PlayWordAudio(item.withoutRWord, item.withoutRAudio);
            yield return new WaitForSeconds(1.0f);
            PlayWordAudio(item.withRWord, item.withRAudio);
            yield return new WaitForSeconds(1.0f);

            ResetButtonVisuals();
            isProcessing = false;
        }

        private void ResetButtonVisuals()
        {
            if (noRButton != null)
            {
                Image img = noRButton.GetComponent<Image>();
                if (img != null) img.color = defaultBtnColor;
            }
            if (withRButton != null)
            {
                Image img = withRButton.GetComponent<Image>();
                if (img != null) img.color = defaultBtnColor;
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
            if (firstAttemptCorrectCount >= 11) earnedStars = 3;
            else if (firstAttemptCorrectCount >= 9) earnedStars = 2;

            if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(1, earnedStars);
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(score);
            }

            yield return new WaitForSeconds(0.8f);

            U8_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform,
                "Activity 1 — Bossy R",
                earnedStars,
                score,
                () => {
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity2();
                }
            );
        }
    }
}
