using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit8 {

    /// <summary>
    /// Roleplay controller for Unit 8 Source of Inspiration: RP01 Studio Duet — Call & Echo.
    /// Features 5 call-and-echo twister chunks with ARIA and LEO + 1 final full duet performance.
    /// Supports microphone recording simulation, audio readbacks, color states, and smooth flow.
    /// </summary>
    public class Masters_SourceOfInspiration_Roleplay_LessonOne : Masters_Lesson {

        [Header("UI References")]
        public TextMeshProUGUI duetTitleTMP;
        public TextMeshProUGUI progressTMP;
        public TextMeshProUGUI leaderBadgeTMP;
        public TextMeshProUGUI lyricCardTMP;
        public TextMeshProUGUI statusTMP;

        public Button recordMicBtn;
        public Image recordMicImage;
        public Button listenAriaBtn;
        public Button nextChunkBtn;

        [Header("Audio Clips")]
        public AudioClip introAudio;
        public AudioClip[] ariaChunkAudios; // 5 chunks
        public AudioClip fullDuetAudio;

        [Header("Colors & Visuals")]
        public Color normalBtnColor = new Color(0.15f, 0.22f, 0.35f, 1f);
        public Color selectedColor = new Color(0.95f, 0.75f, 0.15f, 1f); // Gold Tapping Color
        public Color correctColor = new Color(0.2f, 0.8f, 0.26f, 1f);    // Right Answer Green
        public Color recordingColor = new Color(0.95f, 0.25f, 0.2f, 1f);  // Mic Recording Red

        private int currentChunkIndex = 0;
        private bool isRecording = false;
        private bool isChunkCompleted = false;

        private readonly string[] chunkLyrics = new string[] {
            "I wish to wish",
            "the wish you wish to wish,",
            "but if you wish the wish",
            "the witch wishes,",
            "I won't wish the wish you wish to wish."
        };

        private readonly string[] chunkLeaders = new string[] {
            "ARIA LEADS (YOU ECHO)",
            "ARIA LEADS (YOU ECHO)",
            "YOU LEAD (ARIA ECHOS)",
            "WITCH SPOTLIGHT: YOU LEAD!",
            "DUET: TOGETHER!"
        };

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Roleplay;
            if (recordMicBtn != null) {
                recordMicBtn.onClick.RemoveAllListeners();
                recordMicBtn.onClick.AddListener(OnRecordMicClicked);
            }
            if (listenAriaBtn != null) {
                listenAriaBtn.onClick.RemoveAllListeners();
                listenAriaBtn.onClick.AddListener(OnListenAriaClicked);
            }
            if (nextChunkBtn != null) {
                nextChunkBtn.onClick.RemoveAllListeners();
                nextChunkBtn.onClick.AddListener(OnNextChunkClicked);
            }

            if (recordMicBtn != null && recordMicImage == null) {
                recordMicImage = recordMicBtn.GetComponent<Image>();
            }
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Roleplay;
            StartDuetStudio();
        }

        public void StartDuetStudio() {
            currentChunkIndex = 0;
            isRecording = false;
            isChunkCompleted = false;

            if (introAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
            }

            LoadChunk(currentChunkIndex);
        }

        private void LoadChunk(int index) {
            if (index >= chunkLyrics.Length) {
                ShowFinalDuetPerformance();
                return;
            }

            currentChunkIndex = index;
            isChunkCompleted = false;
            isRecording = false;

            if (duetTitleTMP != null) duetTitleTMP.text = "RP01 Studio Duet — Call & Echo";
            if (progressTMP != null) progressTMP.text = $"Chunk {index + 1}/5";
            if (leaderBadgeTMP != null) {
                leaderBadgeTMP.text = chunkLeaders[index];
                leaderBadgeTMP.color = index == 3 ? new Color(1f, 0.4f, 0.8f) : new Color(0.95f, 0.8f, 0.2f);
            }
            if (lyricCardTMP != null) lyricCardTMP.text = $"\"{chunkLyrics[index]}\"";
            if (statusTMP != null) statusTMP.text = GetStatusForChunk(index);

            ResetMicVisuals();
            if (nextChunkBtn != null) nextChunkBtn.gameObject.SetActive(false);

            // Auto-play ARIA audio if ARIA leads
            if (index == 0 || index == 1) {
                PlayAriaChunkAudio(index);
            }
        }

        private string GetStatusForChunk(int index) {
            switch (index) {
                case 0:
                case 1:
                    return "ARIA has spoken! Tap MIC to echo back.";
                case 2:
                    return "Your turn to lead! Tap MIC and say the chunk aloud.";
                case 3:
                    return "TRICKY WORD SPOTLIGHT! Say 'the witch wishes' clearly!";
                case 4:
                    return "FINAL CHUNK! Tap MIC to sing together with ARIA!";
                default:
                    return "Perform the duet together!";
            }
        }

        private void PlayAriaChunkAudio(int index) {
            if (ariaChunkAudios != null && index < ariaChunkAudios.Length && ariaChunkAudios[index] != null) {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(ariaChunkAudios[index]);
                }
            }
        }

        private void OnListenAriaClicked() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            PlayAriaChunkAudio(currentChunkIndex);
        }

        private void OnRecordMicClicked() {
            if (isChunkCompleted) return;

            if (!isRecording) {
                // START RECORDING
                isRecording = true;
                if (recordMicImage != null) recordMicImage.color = recordingColor;
                if (recordMicBtn != null) recordMicBtn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
                if (statusTMP != null) statusTMP.text = "🔴 RECORDING... Speak into the mic now!";

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                }

                StartCoroutine(StopRecordingRoutine());
            }
        }

        private IEnumerator StopRecordingRoutine() {
            yield return new WaitForSeconds(2.5f);

            isRecording = false;
            isChunkCompleted = true;

            if (recordMicImage != null) recordMicImage.color = correctColor;
            if (statusTMP != null) statusTMP.text = "✔ PERFECT ECHO! ARIA echoes back...";

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // If student leads, ARIA echoes back now
            if (currentChunkIndex >= 2) {
                PlayAriaChunkAudio(currentChunkIndex);
            }

            if (nextChunkBtn != null) nextChunkBtn.gameObject.SetActive(true);

            StartCoroutine(AutoAdvanceRoutine());
        }

        private IEnumerator AutoAdvanceRoutine() {
            AudioClip currentClip = (ariaChunkAudios != null && currentChunkIndex < ariaChunkAudios.Length) ? ariaChunkAudios[currentChunkIndex] : null;
            float waitDuration = (currentClip != null && currentClip.length > 0.5f) ? currentClip.length + 0.5f : 2.5f;
            yield return new WaitForSeconds(waitDuration);

            currentChunkIndex++;
            LoadChunk(currentChunkIndex);
        }

        private void OnNextChunkClicked() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            currentChunkIndex++;
            LoadChunk(currentChunkIndex);
        }

        private void ShowFinalDuetPerformance() {
            if (duetTitleTMP != null) duetTitleTMP.text = "RP01 Studio Duet — Full Performance";
            if (progressTMP != null) progressTMP.text = "DUET COMPLETED!";
            if (leaderBadgeTMP != null) {
                leaderBadgeTMP.text = "ARIA & LEO DUET!";
                leaderBadgeTMP.color = correctColor;
            }
            if (lyricCardTMP != null) lyricCardTMP.text = "\"I wish to wish the wish you wish to wish,\nbut if you wish the wish the witch wishes,\nI won't wish the wish you wish to wish.\"";
            if (statusTMP != null) statusTMP.text = "🎶 Listen to your complete studio duet recording!";

            if (recordMicBtn != null) recordMicBtn.gameObject.SetActive(false);
            if (listenAriaBtn != null) listenAriaBtn.gameObject.SetActive(false);
            if (nextChunkBtn != null) nextChunkBtn.gameObject.SetActive(false);

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }

            if (fullDuetAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(fullDuetAudio);
            }
        }

        private void ResetMicVisuals() {
            if (recordMicImage != null) recordMicImage.color = normalBtnColor;
            if (recordMicBtn != null) recordMicBtn.gameObject.SetActive(true);
            if (listenAriaBtn != null) listenAriaBtn.gameObject.SetActive(true);
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Roleplay;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
