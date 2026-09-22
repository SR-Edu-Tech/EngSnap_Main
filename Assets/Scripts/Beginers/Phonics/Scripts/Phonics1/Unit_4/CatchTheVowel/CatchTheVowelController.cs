using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Unit4
{
    public class CatchTheVowelController : MonoBehaviour
    {
        [Header("Mascot & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Shape Background & Tiles")]
        [SerializeField] private Image shapeBackgroundImage;
        [SerializeField] private TMP_Text shapeTitleText;
        [SerializeField] private CatchTheVowelTile[] letterTiles;
        [SerializeField] private Transform tilesParent;
        [SerializeField] private CanvasGroup shapeCanvasGroup;
        [SerializeField] private float fadeDuration = 0.5f;

        [Header("Star Meter & Badge")]
        [SerializeField] private Image starMeterFillImage;
        [SerializeField] private TMP_Text starMeterCountText;
        [SerializeField] private GameObject vowelStarBadge;

        [Header("Shape Game Data Sets (Apple & Dolphin)")]
        [SerializeField] private CatchTheVowelData[] shapeDataSets;

        [Header("Voice Script Audio Clips")]
        [SerializeField] private AudioClip introClip;               // "Find and tap all the vowels! Ready?"
        [SerializeField] private AudioClip consonantWarningClip;   // "That's a consonant - keep looking for vowels!"
        [SerializeField] private AudioClip shapeTransitionClip;    // "Awesome! You completed the Apple section! Now let's find the vowels on the Dolphin!"
        [SerializeField] private AudioClip victoryUnlockClip;       // "You caught them all! You are a Vowel Star! Unit 5 is open!"
        [SerializeField] private AudioClip vowelPopSfx;
        [SerializeField] private AudioClip consonantWobbleSfx;
        [SerializeField] private AudioClip starJingleSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentShapeIndex = 0;
        private int totalVowelsInShape = 0;
        private int caughtVowelsCount = 0;
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
            currentShapeIndex = 0;
            caughtVowelsCount = 0;
            isTransitioning = false;
            isActivityCompleted = false;

            if (shapeCanvasGroup != null) shapeCanvasGroup.alpha = 1f;
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (vowelStarBadge != null) vowelStarBadge.SetActive(false);

            LoadShapeGame(currentShapeIndex);
            SetSubtitles("Find and tap all the vowels! Ready?");
        }

        private void LoadShapeGame(int index)
        {
            caughtVowelsCount = 0;
            totalVowelsInShape = 0;

            CatchTheVowelData data = (shapeDataSets != null && index < shapeDataSets.Length) ? shapeDataSets[index] : null;
            if (data != null)
            {
                if (shapeTitleText != null) shapeTitleText.text = data.shapeName;
                if (shapeBackgroundImage != null && data.shapeBackgroundSprite != null)
                {
                    shapeBackgroundImage.sprite = data.shapeBackgroundSprite;
                }

                if (data.scatteredLetters != null && letterTiles != null)
                {
                    for (int i = 0; i < letterTiles.Length; i++)
                    {
                        if (letterTiles[i] != null)
                        {
                            if (i < data.scatteredLetters.Count)
                            {
                                CatchTheVowelData.LetterTileItem item = data.scatteredLetters[i];
                                letterTiles[i].Setup(item, this);
                                letterTiles[i].ResetTile();
                                if (item.isVowel) totalVowelsInShape++;
                            }
                            else
                            {
                                letterTiles[i].gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }
            else
            {
                // Fallback counting from assigned tiles if data is null
                if (letterTiles != null)
                {
                    foreach (var tile in letterTiles)
                    {
                        if (tile != null && tile.gameObject.activeSelf)
                        {
                            tile.ResetTile();
                            if (tile.IsVowel) totalVowelsInShape++;
                        }
                    }
                }
            }

            if (totalVowelsInShape == 0) totalVowelsInShape = 5; // Default fallback
            UpdateStarMeterUI();
        }

        private IEnumerator IntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Find and tap all the vowels! Ready?");

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

        public void OnVowelCaught(CatchTheVowelTile tile)
        {
            if (tile == null || isTransitioning) return;

            StartCoroutine(VowelCaughtSequence(tile));
        }

        private IEnumerator VowelCaughtSequence(CatchTheVowelTile tile)
        {
            isTransitioning = true;

            caughtVowelsCount++;
            UpdateStarMeterUI();

            if (vowelPopSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(vowelPopSfx);
            }

            CatchTheVowelData.LetterTileItem item = tile.DataItem;
            string letter = (item != null) ? item.letter.ToLower() : "vowel";
            SetSubtitles($"Vowel caught: '{letter}'!");

            if (item != null && item.phonemeSoundClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = item.phonemeSoundClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(item.phonemeSoundClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.4f);
            }

            isTransitioning = false;

            if (caughtVowelsCount >= totalVowelsInShape && !isActivityCompleted)
            {
                // Check if another shape dataset exists (e.g. Dolphin after Apple)
                if (shapeDataSets != null && currentShapeIndex + 1 < shapeDataSets.Length)
                {
                    yield return StartCoroutine(ShapeTransitionSequence(currentShapeIndex + 1));
                }
                else
                {
                    yield return StartCoroutine(CompletionSequence());
                }
            }
        }

        private IEnumerator ShapeTransitionSequence(int nextIndex)
        {
            isTransitioning = true;

            CatchTheVowelData currentData = (shapeDataSets != null && currentShapeIndex < shapeDataSets.Length) ? shapeDataSets[currentShapeIndex] : null;
            CatchTheVowelData nextData = (shapeDataSets != null && nextIndex < shapeDataSets.Length) ? shapeDataSets[nextIndex] : null;

            string currentName = (currentData != null) ? currentData.shapeName : "section";
            string nextName = (nextData != null) ? nextData.shapeName : "next section";

            string msg = $"Awesome! You completed the {currentName} section! Now let's find the vowels on the {nextName}!";
            SetSubtitles(msg);

            if (currentData != null && currentData.sectionCompletedClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = currentData.sectionCompletedClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(currentData.sectionCompletedClip.length + 0.2f);
            }
            else if (shapeTransitionClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = shapeTransitionClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(shapeTransitionClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            // Fade out current shape
            if (shapeCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    shapeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                    yield return null;
                }
                shapeCanvasGroup.alpha = 0f;
            }

            // Load next shape game
            currentShapeIndex = nextIndex;
            LoadShapeGame(currentShapeIndex);

            // Fade in next shape
            if (shapeCanvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    shapeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                    yield return null;
                }
                shapeCanvasGroup.alpha = 1f;
            }

            SetSubtitles($"Find and tap all the vowels on the {nextName}!");
            isTransitioning = false;
        }

        public void OnConsonantTapped(CatchTheVowelTile tile)
        {
            if (tile == null || isTransitioning) return;

            StartCoroutine(ConsonantTappedSequence(tile));
        }

        private IEnumerator ConsonantTappedSequence(CatchTheVowelTile tile)
        {
            isTransitioning = true;

            if (consonantWobbleSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(consonantWobbleSfx);
            }

            SetSubtitles("That's a consonant - keep looking for vowels!");

            if (consonantWarningClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = consonantWarningClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(consonantWarningClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            isTransitioning = false;
        }

        private void UpdateStarMeterUI()
        {
            if (starMeterCountText != null)
            {
                starMeterCountText.text = $"{caughtVowelsCount} / {totalVowelsInShape}";
            }

            if (starMeterFillImage != null && totalVowelsInShape > 0)
            {
                starMeterFillImage.fillAmount = (float)caughtVowelsCount / totalVowelsInShape;
            }
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            if (vowelStarBadge != null) vowelStarBadge.SetActive(true);
            if (starJingleSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(starJingleSfx);
            }

            SetSubtitles("You caught them all! You are a Vowel Star! Unit 5 is open!");

            if (victoryUnlockClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = victoryUnlockClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(victoryUnlockClip.length + 0.3f);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            TopicProgressUI.MarkTopicComplete("Unit4", "CatchTheVowel");

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
