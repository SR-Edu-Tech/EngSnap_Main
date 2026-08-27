using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Common.ShortVowels
{
    public class ShortVowelWordsController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [Tooltip("Target Unit ID e.g. 'Unit6', 'Unit7', 'Unit8', 'Unit9', 'Unit10'.")]
        [SerializeField] private string unitID = "Unit6";

        [Tooltip("Topic key name for progress tracking e.g. 'ShortVowelWords'.")]
        [SerializeField] private string topicName = "ShortVowelWords";

        [Header("Mascot & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("3 Separate Letter TMP_Texts for Word")]
        [Tooltip("First letter text e.g. 'c'")]
        [SerializeField] private TMP_Text letter1Text;

        [Tooltip("Middle vowel letter text e.g. 'a' (Glows on tap)")]
        [SerializeField] private TMP_Text letter2Text;

        [Tooltip("Third letter text e.g. 't'")]
        [SerializeField] private TMP_Text letter3Text;

        [Tooltip("Optional single word text fallback.")]
        [SerializeField] private TMP_Text fullWordText;

        [Header("Visual Display UI")]
        [SerializeField] private Image wordPictureImage;
        [SerializeField] private Button playWordAudioButton;
        [SerializeField] private Button nextCardButton;
        [SerializeField] private Button prevCardButton;

        [Header("Middle Vowel Glow & Highlight GameObject Settings")]
        [Tooltip("Optional GameObject for glow/sparkle highlight visual effect under or around the middle vowel.")]
        [SerializeField] private GameObject vowelHighlightGlowObject;
        [SerializeField] private Color defaultLetterColor = Color.black;
        [SerializeField] private Color glowVowelColor = new Color(1f, 0.84f, 0f, 1f); // Bright Gold #FFD700

        [Header("Progress Meter UI")]
        [SerializeField] private Image progressMeterFillImage;
        [SerializeField] private TMP_Text progressCountText;

        [Header("Word Data List")]
        [SerializeField] private ShortVowelWordData[] wordDataList;

        [Header("Voice Script & SFX Audio Clips")]
        [SerializeField] private AudioClip introClip;             // "These words all have the short vowel sound. Tap a picture!"
        [SerializeField] private AudioClip tryAgainClip;          // "Listen again!"
        [SerializeField] private AudioClip completionPraiseClip;  // "Awesome job learning short vowel words!"
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip wordClickSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;
        

        private int currentWordIndex = 0;
        private HashSet<int> exploredWordIndices = new HashSet<int>();
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;
        private Coroutine vowelWiggleCoroutine;

        public bool IsTransitioning => isTransitioning;
        public string UnitID => unitID;

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

            if (playWordAudioButton != null)
            {
                playWordAudioButton.onClick.RemoveAllListeners();
                playWordAudioButton.onClick.AddListener(OnCardClicked);
            }

            if (nextCardButton != null)
            {
                nextCardButton.onClick.RemoveAllListeners();
                nextCardButton.onClick.AddListener(GoToNextCard);
            }

            if (prevCardButton != null)
            {
                prevCardButton.onClick.RemoveAllListeners();
                prevCardButton.onClick.AddListener(GoToPreviousCard);
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
            currentWordIndex = 0;
            exploredWordIndices.Clear();
            isTransitioning = false;
            isActivityCompleted = false;

            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);

            UpdateProgressUI();
            LoadCurrentCard();
            SetSubtitles("These words have the short vowel sound. Tap the picture to hear!");
        }

        private void LoadCurrentCard()
        {
            if (wordDataList == null || wordDataList.Length == 0) return;

            ResetVowelGlow();

            if (currentWordIndex < wordDataList.Length)
            {
                ShortVowelWordData data = wordDataList[currentWordIndex];
                if (data != null)
                {
                    string word = data.word ?? "";

                    if (word.Length >= 3)
                    {
                        if (letter1Text != null) letter1Text.text = word[0].ToString();
                        if (letter2Text != null) letter2Text.text = word[1].ToString();
                        if (letter3Text != null) letter3Text.text = word[2].ToString();
                    }
                    else if (word.Length > 0)
                    {
                        if (letter1Text != null) letter1Text.text = word.Length > 0 ? word[0].ToString() : "";
                        if (letter2Text != null) letter2Text.text = word.Length > 1 ? word[1].ToString() : "";
                        if (letter3Text != null) letter3Text.text = word.Length > 2 ? word[2].ToString() : "";
                    }

                    if (fullWordText != null) fullWordText.text = word;
                    if (wordPictureImage != null && data.wordSprite != null) wordPictureImage.sprite = data.wordSprite;
                }
            }
        }

        public void OnCardClicked()
        {
            if (isTransitioning) return;
            if (wordDataList == null || currentWordIndex >= wordDataList.Length) return;

            ShortVowelWordData data = wordDataList[currentWordIndex];
            if (data == null) return;

            StartCoroutine(CardTapAndAutoAdvanceSequence(data));
        }

        private IEnumerator CardTapAndAutoAdvanceSequence(ShortVowelWordData data)
        {
            isTransitioning = true;

            if (wordClickSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(wordClickSfx);
            }

            // 1. Highlight & Glow Middle Vowel Letter (letter2Text)
            ApplyVowelGlow();

            // 2. Play Word Audio (e.g. 'cat')
            if (data.wordAudioClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = data.wordAudioClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(data.wordAudioClip.length + 0.15f);
            }

            // 3. Format Subtitles: "Hear it? c - a - t, cat! The short a sound!"
            string word = data.word;
            string vowel = string.IsNullOrEmpty(data.targetVowel) ? "a" : data.targetVowel.ToLower();
            string l1 = (word != null && word.Length > 0) ? word[0].ToString() : "";
            string l2 = (word != null && word.Length > 1) ? word[1].ToString() : "";
            string l3 = (word != null && word.Length > 2) ? word[2].ToString() : "";

            SetSubtitles($"Hear it? {l1} - {l2} - {l3}, {word}! The short {vowel} sound!");

            // 4. Play reinforcement audio clip if available
            if (data.reinforcementAudioClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = data.reinforcementAudioClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(data.reinforcementAudioClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            ResetVowelGlow();

            // 5. Track explored card progress
            exploredWordIndices.Add(currentWordIndex);
            UpdateProgressUI();

            // 6. Automatically advance to next card or trigger completion if all done
            if (exploredWordIndices.Count >= wordDataList.Length && currentWordIndex + 1 >= wordDataList.Length && !isActivityCompleted)
            {
                yield return StartCoroutine(CompletionSequence());
            }
            else
            {
                currentWordIndex++;
                if (currentWordIndex >= wordDataList.Length)
                {
                    currentWordIndex = 0; // Loop to start if repeating
                }
                LoadCurrentCard();
                isTransitioning = false;
            }
        }

        private void ApplyVowelGlow()
        {
            if (vowelHighlightGlowObject != null)
            {
                vowelHighlightGlowObject.SetActive(true);
            }

            if (letter2Text != null)
            {
                letter2Text.color = glowVowelColor;
                if (vowelWiggleCoroutine != null) StopCoroutine(vowelWiggleCoroutine);
                vowelWiggleCoroutine = StartCoroutine(WiggleVowelTextCoroutine(letter2Text.transform));
            }
        }

        private IEnumerator WiggleVowelTextCoroutine(Transform targetTransform, float duration = 0.5f)
        {
            if (targetTransform == null) yield break;

            Vector3 origScale = Vector3.one;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;

                // Scale pulse up to 1.3x and return
                float scaleFactor = 1f + Mathf.Sin(percent * Mathf.PI) * 0.3f;
                targetTransform.localScale = origScale * scaleFactor;

                // Tilt left and right (-12 deg to +12 deg)
                float rotZ = Mathf.Sin(percent * Mathf.PI * 2f) * 12f;
                targetTransform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

                yield return null;
            }

            targetTransform.localScale = origScale;
            targetTransform.localRotation = Quaternion.identity;
            vowelWiggleCoroutine = null;
        }

        private void ResetVowelGlow()
        {
            if (vowelWiggleCoroutine != null)
            {
                StopCoroutine(vowelWiggleCoroutine);
                vowelWiggleCoroutine = null;
            }

            if (vowelHighlightGlowObject != null)
            {
                vowelHighlightGlowObject.SetActive(false);
            }

            if (letter2Text != null)
            {
                letter2Text.color = defaultLetterColor;
                letter2Text.transform.localScale = Vector3.one;
                letter2Text.transform.localRotation = Quaternion.identity;
            }
        }

        public void GoToNextCard()
        {
            if (isTransitioning) return;
            if (wordDataList == null || wordDataList.Length == 0) return;

            currentWordIndex = (currentWordIndex + 1) % wordDataList.Length;
            LoadCurrentCard();
        }

        public void GoToPreviousCard()
        {
            if (isTransitioning) return;
            if (wordDataList == null || wordDataList.Length == 0) return;

            currentWordIndex = (currentWordIndex - 1 + wordDataList.Length) % wordDataList.Length;
            LoadCurrentCard();
        }

        private void UpdateProgressUI()
        {
            int total = (wordDataList != null) ? wordDataList.Length : 0;
            int explored = exploredWordIndices.Count;

            if (progressCountText != null)
            {
                progressCountText.text = $"{explored} / {total}";
            }

            if (progressMeterFillImage != null && total > 0)
            {
                progressMeterFillImage.fillAmount = (float)explored / total;
            }
        }

        private IEnumerator IntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("These words have the short vowel sound. Tap the picture to hear!");

            if (introClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = introClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(introClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            if (correctChimeSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(correctChimeSfx);
            }

            SetSubtitles("Great job exploring all the short vowel words!");

            if (completionPraiseClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = completionPraiseClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(completionPraiseClip.length + 0.3f);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);

            TopicProgressUI.MarkTopicComplete(unitID, topicName);

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
            EngSnap.Common.DialogueBoxAutoHider.SetDialogue(dialogueText, text, null);
        }
    }
}
