using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Unit4
{
    public class FiveVowelsController : MonoBehaviour
    {
        [Header("Mascot & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Balloon UI Elements")]
        [SerializeField] private FiveVowelsBalloon[] vowelBalloons;
        [SerializeField] private GameObject chantStrip;
        [SerializeField] private TMP_Text chantStripText;

        [Header("Active Vowel Display UI Elements")]
        [SerializeField] private TMP_Text phonemeText;
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private Image wordImage;

        [Header("Vowel Data Sets (A, E, I, O, U)")]
        [SerializeField] private FiveVowelsData[] vowelsData;

        [Header("Voice Script Audio Clips")]
        [SerializeField] private AudioClip introClip;             // "Five letters are special. They are the vowels! Tap each one."
        [SerializeField] private AudioClip vowelChantClip;         // "a - e - i - o - u" chant clip
        [SerializeField] private AudioClip completionPraiseClip;  // "a, e, i, o, u - the five vowels! Well done!"
        [SerializeField] private AudioClip tapSfx;
        [SerializeField] private AudioClip completionSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private HashSet<string> exploredVowels = new HashSet<string>();
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            if (sfxAudioSource == null)
            {
                sfxAudioSource = GetComponent<AudioSource>();
                if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }
            sfxAudioSource.spatialBlend = 0f;
            sfxAudioSource.volume = 1f;

            if (voiceAudioSource == null) voiceAudioSource = sfxAudioSource;
            else voiceAudioSource.spatialBlend = 0f;
        }

        private void Start()
        {
            EnsureAudioSources();

            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }

            ResetLevel();
        }

        private void OnEnable()
        {
            ResetLevel();
            StartCoroutine(StartIntroOnNextFrame());
        }

        private IEnumerator StartIntroOnNextFrame()
        {
            yield return null;
            if (gameObject.activeInHierarchy && !isActivityCompleted)
            {
                yield return StartCoroutine(IntroSequence());
            }
        }

        public void ResetLevel()
        {
            exploredVowels.Clear();
            isTransitioning = false;
            isActivityCompleted = false;

            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (chantStrip != null) chantStrip.SetActive(true);

            if (phonemeText != null) phonemeText.text = "";
            if (wordText != null) wordText.text = "";
            if (wordImage != null)
            {
                wordImage.sprite = null;
                wordImage.gameObject.SetActive(false);
            }

            if (vowelsData != null && vowelBalloons != null)
            {
                for (int i = 0; i < vowelBalloons.Length; i++)
                {
                    if (vowelBalloons[i] != null)
                    {
                        FiveVowelsData data = (i < vowelsData.Length) ? vowelsData[i] : null;
                        vowelBalloons[i].Setup(data, this);
                        vowelBalloons[i].ResetBalloon();
                    }
                }
            }

            SetSubtitles("Five letters are special. They are the vowels! Tap each one.");
        }

        private IEnumerator IntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Five letters are special. They are the vowels! Tap each one.");

            if (introClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = introClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(introClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
        }

        public void OnVowelBalloonTapped(FiveVowelsBalloon balloon)
        {
            if (balloon == null || isTransitioning) return;

            StartCoroutine(VowelTapSequence(balloon));
        }

        private IEnumerator VowelTapSequence(FiveVowelsBalloon balloon)
        {
            isTransitioning = true;

            FiveVowelsData data = balloon.Data;
            if (data != null)
            {
                exploredVowels.Add(data.vowelLetter.ToUpper());

                string sub = $"{data.phonemeText} – {data.exampleWord}";
                SetSubtitles(sub);

                if (phonemeText != null) phonemeText.text = data.phonemeText;
                if (wordText != null) wordText.text = data.exampleWord;
                if (wordImage != null)
                {
                    wordImage.sprite = data.wordSprite;
                    wordImage.gameObject.SetActive(data.wordSprite != null);
                }

                if (tapSfx != null && sfxAudioSource != null)
                {
                    sfxAudioSource.PlayOneShot(tapSfx);
                }

                // Play soundAndWordClip or purePhonemeClip / wordAudioClip
                if (data.soundAndWordClip != null && voiceAudioSource != null)
                {
                    voiceAudioSource.Stop();
                    voiceAudioSource.clip = data.soundAndWordClip;
                    voiceAudioSource.Play();
                    yield return new WaitForSeconds(data.soundAndWordClip.length + 0.3f);
                }
                else
                {
                    if (data.purePhonemeClip != null && voiceAudioSource != null)
                    {
                        voiceAudioSource.Stop();
                        voiceAudioSource.clip = data.purePhonemeClip;
                        voiceAudioSource.Play();
                        yield return new WaitForSeconds(data.purePhonemeClip.length + 0.1f);
                    }

                    if (data.wordAudioClip != null && voiceAudioSource != null)
                    {
                        voiceAudioSource.Stop();
                        voiceAudioSource.clip = data.wordAudioClip;
                        voiceAudioSource.Play();
                        yield return new WaitForSeconds(data.wordAudioClip.length + 0.2f);
                    }
                }
            }

            isTransitioning = false;

            // Check if all 5 vowels have been tapped
            if (exploredVowels.Count >= 5 && !isActivityCompleted)
            {
                yield return StartCoroutine(VowelChantAndCompletionSequence());
            }
        }

        private IEnumerator VowelChantAndCompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            SetSubtitles("a – e – i – o – u!");

            // Sequential highlight during chant
            if (vowelChantClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = vowelChantClip;
                voiceAudioSource.Play();

                float stepDuration = vowelChantClip.length / Mathf.Max(1, vowelBalloons.Length);
                for (int i = 0; i < vowelBalloons.Length; i++)
                {
                    if (vowelBalloons[i] != null) vowelBalloons[i].SetChantHighlight(true);
                    yield return new WaitForSeconds(stepDuration);
                    if (vowelBalloons[i] != null) vowelBalloons[i].SetChantHighlight(false);
                }
                yield return new WaitForSeconds(0.3f);
            }
            else
            {
                for (int i = 0; i < vowelBalloons.Length; i++)
                {
                    if (vowelBalloons[i] != null) vowelBalloons[i].SetChantHighlight(true);
                    yield return new WaitForSeconds(0.4f);
                    if (vowelBalloons[i] != null) vowelBalloons[i].SetChantHighlight(false);
                }
            }

            // Play completion praise
            SetSubtitles("a, e, i, o, u - the five vowels! Well done!");
            if (completionPraiseClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = completionPraiseClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(completionPraiseClip.length + 0.3f);
            }

            if (completionSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(completionSfx);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            TopicProgressUI.MarkTopicComplete("Unit4", "TheFiveVowels");

            isTransitioning = false;
        }

        public void GoToNextPanel()
        {
            if (isActivityCompleted)
            {
                TopicProgressUI.MarkTopicComplete(gameObject);
            }

            ResetLevel();

            if (nextPanel != null)
            {
                nextPanel.SetActive(true);
                if (unitContentPanel != null && nextPanel != unitContentPanel && !nextPanel.transform.IsChildOf(unitContentPanel.transform))
                {
                    unitContentPanel.SetActive(false);
                }
            }
            else if (unitContentPanel != null)
            {
                unitContentPanel.SetActive(true);
            }

            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }

        private void SetSubtitles(string text)
        {
            EngSnap.Common.DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
        }
    }
}
