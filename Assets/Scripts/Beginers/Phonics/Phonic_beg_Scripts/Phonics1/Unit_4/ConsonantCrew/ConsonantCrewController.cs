using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Unit4
{
    public class ConsonantCrewController : MonoBehaviour
    {
        [Header("Mascot & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Progress & Pages UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressCountText;
        [SerializeField] private Button page1Button; // B - M
        [SerializeField] private Button page2Button; // N - Z
        [SerializeField] private GameObject page1GridContainer;
        [SerializeField] private GameObject page2GridContainer;

        [Header("Intro Vowels Display UI")]
        [SerializeField] private GameObject vowelsGrid;
        [SerializeField] private float vowelsDisplayDuration = 2.5f;
        [SerializeField] private float fadeDuration = 0.4f;

        [Header("Grid Cards")]
        [SerializeField] private ConsonantCrewCard[] page1Cards;
        [SerializeField] private ConsonantCrewCard[] page2Cards;

        [Header("Active Consonant Display UI Elements")]
        [SerializeField] private TMP_Text phonemeText;
        [SerializeField] private TMP_Text wordText;
        [SerializeField] private Image wordImage;

        [Header("Data Config (21 Consonants B-Z except A,E,I,O,U)")]
        [SerializeField] private ConsonantCrewData[] consonantsData21;

        [Header("Voice Script Audio Clips")]
        [SerializeField] private AudioClip introClip;             // "All the other letters are consonants. Tap one to hear its sound!"
        [SerializeField] private AudioClip completionPraiseClip;  // "So many consonants! Great tapping!"
        [SerializeField] private AudioClip tapSfx;
        [SerializeField] private AudioClip completionSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private HashSet<string> exploredConsonants = new HashSet<string>();
        private int currentPage = 1;
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

            if (page1Button != null)
            {
                page1Button.onClick.RemoveAllListeners();
                page1Button.onClick.AddListener(() => SwitchPage(1));
            }

            if (page2Button != null)
            {
                page2Button.onClick.RemoveAllListeners();
                page2Button.onClick.AddListener(() => SwitchPage(2));
            }

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
            exploredConsonants.Clear();
            currentPage = 1;
            isTransitioning = false;
            isActivityCompleted = false;

            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);

            if (phonemeText != null) phonemeText.text = "";
            if (wordText != null) wordText.text = "";
            if (wordImage != null)
            {
                wordImage.sprite = null;
                wordImage.gameObject.SetActive(false);
            }

            SetupPageCards();

            if (vowelsGrid != null)
            {
                vowelsGrid.SetActive(true);
                if (page1GridContainer != null) page1GridContainer.SetActive(false);
                if (page2GridContainer != null) page2GridContainer.SetActive(false);
            }
            else
            {
                SwitchPage(1);
            }

            UpdateProgressUI();

            SetSubtitles("All the other letters are consonants. Tap one to hear its sound!");
        }

        private List<ConsonantCrewData> p1List = new List<ConsonantCrewData>();
        private List<ConsonantCrewData> p2List = new List<ConsonantCrewData>();

        private void SetupPageCards()
        {
            if (consonantsData21 == null || consonantsData21.Length == 0) return;

            // Explicit 11 / 10 split: Page 1 gets first 11 consonants, Page 2 gets remaining 10 consonants
            p1List.Clear();
            p2List.Clear();

            for (int i = 0; i < consonantsData21.Length; i++)
            {
                if (consonantsData21[i] == null) continue;
                if (p1List.Count < 11) p1List.Add(consonantsData21[i]);
                else p2List.Add(consonantsData21[i]);
            }

            if (page1Cards != null)
            {
                for (int i = 0; i < page1Cards.Length; i++)
                {
                    if (page1Cards[i] != null)
                    {
                        ConsonantCrewData data = (i < p1List.Count) ? p1List[i] : null;
                        page1Cards[i].gameObject.SetActive(data != null);
                        if (data != null)
                        {
                            bool explored = exploredConsonants.Contains(data.letter.ToUpper());
                            page1Cards[i].Setup(data, this, explored);
                            if (!explored) page1Cards[i].ResetCard();
                        }
                    }
                }
            }

            if (page2Cards != null)
            {
                for (int i = 0; i < page2Cards.Length; i++)
                {
                    if (page2Cards[i] != null)
                    {
                        ConsonantCrewData data = (i < p2List.Count) ? p2List[i] : null;
                        page2Cards[i].gameObject.SetActive(data != null);
                        if (data != null)
                        {
                            bool explored = exploredConsonants.Contains(data.letter.ToUpper());
                            page2Cards[i].Setup(data, this, explored);
                            if (!explored) page2Cards[i].ResetCard();
                        }
                    }
                }
            }
        }

        public void SwitchPage(int targetPage)
        {
            currentPage = targetPage;

            if (page1GridContainer != null) page1GridContainer.SetActive(currentPage == 1);
            if (page2GridContainer != null) page2GridContainer.SetActive(currentPage == 2);

            if (page1Button != null) page1Button.interactable = (currentPage != 1);
            if (page2Button != null) page2Button.interactable = (currentPage != 2);
        }

        private IEnumerator IntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("All the other letters are consonants. Tap one to hear its sound!");

            CanvasGroup vowelsCG = null;
            if (vowelsGrid != null)
            {
                vowelsCG = vowelsGrid.GetComponent<CanvasGroup>();
                if (vowelsCG == null) vowelsCG = vowelsGrid.AddComponent<CanvasGroup>();

                vowelsCG.alpha = 0f;
                vowelsGrid.SetActive(true);

                if (page1GridContainer != null) page1GridContainer.SetActive(false);
                if (page2GridContainer != null) page2GridContainer.SetActive(false);

                // Smooth Fade In Vowels Grid
                yield return StartCoroutine(FadeCanvasGroup(vowelsCG, 0f, 1f, fadeDuration));
            }

            float displayTime = vowelsDisplayDuration;

            if (introClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = introClip;
                voiceAudioSource.Play();
                displayTime = Mathf.Max(displayTime, introClip.length + 0.2f);
            }

            yield return new WaitForSeconds(displayTime);

            if (vowelsCG != null)
            {
                // Smooth Fade Out Vowels Grid
                yield return StartCoroutine(FadeCanvasGroup(vowelsCG, 1f, 0f, fadeDuration));
                vowelsGrid.SetActive(false);
            }

            // Prepare Page 1 for smooth Fade In
            CanvasGroup page1CG = null;
            if (page1GridContainer != null)
            {
                page1CG = page1GridContainer.GetComponent<CanvasGroup>();
                if (page1CG == null) page1CG = page1GridContainer.AddComponent<CanvasGroup>();
                page1CG.alpha = 0f;
            }

            SwitchPage(1);

            if (page1CG != null)
            {
                yield return StartCoroutine(FadeCanvasGroup(page1CG, 0f, 1f, fadeDuration));
            }

            isTransitioning = false;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float targetAlpha, float duration)
        {
            if (cg == null) yield break;

            cg.gameObject.SetActive(true);
            cg.alpha = startAlpha;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            cg.alpha = targetAlpha;
        }

        public void OnConsonantCardTapped(ConsonantCrewCard card)
        {
            if (card == null || card.Data == null) return;

            ConsonantCrewData data = card.Data;
            bool isNewExploration = exploredConsonants.Add(data.letter.ToUpper());
            UpdateProgressUI();

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

            AudioClip voiceClipToPlay = (data.soundAndWordClip != null) ? data.soundAndWordClip : ((data.purePhonemeClip != null) ? data.purePhonemeClip : data.wordAudioClip);

            if (voiceClipToPlay != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = voiceClipToPlay;
                voiceAudioSource.Play();
            }

            // Auto switch to Page 2 when all 11 consonants on Page 1 are explored
            if (currentPage == 1 && isNewExploration && p1List.Count > 0)
            {
                int p1ExploredCount = 0;
                for (int i = 0; i < p1List.Count; i++)
                {
                    if (exploredConsonants.Contains(p1List[i].letter.ToUpper())) p1ExploredCount++;
                }

                if (p1ExploredCount >= p1List.Count)
                {
                    StartCoroutine(AutoSwitchToPage2AfterAudio(voiceClipToPlay));
                }
            }

            // Check activity completion when all 21 consonants are explored
            int totalTarget = (consonantsData21 != null && consonantsData21.Length > 0) ? consonantsData21.Length : 21;
            if (exploredConsonants.Count >= totalTarget && !isActivityCompleted)
            {
                StartCoroutine(CheckCompletionAfterDelay());
            }
        }

        private IEnumerator AutoSwitchToPage2AfterAudio(AudioClip clip)
        {
            float delay = clip != null ? clip.length + 0.3f : 1.2f;
            yield return new WaitForSeconds(delay);

            if (currentPage == 1 && !isActivityCompleted)
            {
                SwitchPage(2);
            }
        }

        private IEnumerator CheckCompletionAfterDelay()
        {
            float delay = (voiceAudioSource != null && voiceAudioSource.clip != null) ? voiceAudioSource.clip.length : 1.2f;
            yield return new WaitForSeconds(delay + 0.2f);

            int totalTarget = (consonantsData21 != null && consonantsData21.Length > 0) ? consonantsData21.Length : 21;
            if (exploredConsonants.Count >= totalTarget && !isActivityCompleted)
            {
                yield return StartCoroutine(CompletionSequence());
            }
        }

        private void UpdateProgressUI()
        {
            int totalTarget = (consonantsData21 != null && consonantsData21.Length > 0) ? consonantsData21.Length : 21;
            int current = exploredConsonants.Count;

            if (progressCountText != null)
            {
                progressCountText.text = $"{current} / {totalTarget}";
            }

            if (progressRingFillImage != null)
            {
                progressRingFillImage.fillAmount = (float)current / totalTarget;
            }
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            SetSubtitles("So many consonants! Great tapping!");

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

            TopicProgressUI.MarkTopicComplete("Unit4", "TheConsonantCrew");

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
