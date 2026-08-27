using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Unit5
{
    public class SoundSortController : MonoBehaviour
    {
        [Header("Mascot & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Vowel Buckets (a, e, i, o, u)")]
        [SerializeField] private SoundSortBucket[] vowelBuckets;
        [Tooltip("Number of active buckets visible on screen at a time (e.g. 3 for LKG/UKG).")]
        [SerializeField] private int activeBucketsCount = 3;

        [Header("Word Card UI")]
        [SerializeField] private SoundSortCard activeCard;

        [Header("Sorting Data Sets")]
        [SerializeField] private SoundSortData[] roundWordData;

        [Header("Voice Script Audio Clips")]
        [SerializeField] private AudioClip introClip;             // "Listen to the middle sound. Put the word in the right vowel bucket!"
        [SerializeField] private AudioClip tryAgainClip;          // "Listen again - what sound is in the middle?"
        [SerializeField] private AudioClip completionPraiseClip;  // "Awesome job sorting words by their vowel sound!"
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip wrongWobbleSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentCardIndex = 0;
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;
        private List<SoundSortData> activeWordList = new List<SoundSortData>();

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
            currentCardIndex = 0;
            isTransitioning = false;
            isActivityCompleted = false;

            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);

            activeWordList.Clear();
            if (roundWordData != null && roundWordData.Length > 0)
            {
                activeWordList.AddRange(roundWordData);
                for (int i = 0; i < activeWordList.Count; i++)
                {
                    int rnd = Random.Range(i, activeWordList.Count);
                    SoundSortData temp = activeWordList[i];
                    activeWordList[i] = activeWordList[rnd];
                    activeWordList[rnd] = temp;
                }
            }

            LoadNextCard();
            SetSubtitles("Listen to the middle sound. Put the word in the right vowel bucket!");
        }

        private void LoadNextCard()
        {
            if (activeWordList == null || activeWordList.Count == 0) return;

            if (activeCard != null)
            {
                if (currentCardIndex < activeWordList.Count)
                {
                    SoundSortData data = activeWordList[currentCardIndex];
                    activeCard.Setup(data, this);
                    UpdateActiveBucketsForWord(data);
                    SpeakCardWord(data);
                }
                else
                {
                    activeCard.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateActiveBucketsForWord(SoundSortData data)
        {
            if (vowelBuckets == null || vowelBuckets.Length == 0) return;
            if (data == null) return;

            string targetVowel = data.targetVowel.ToLowerInvariant();

            int countToUse = Mathf.Min(activeBucketsCount, vowelBuckets.Length);

            List<string> vowelPool = new List<string> { "a", "e", "i", "o", "u" };
            if (!vowelPool.Contains(targetVowel))
            {
                vowelPool.Add(targetVowel);
            }

            List<string> distractorPool = new List<string>(vowelPool);
            distractorPool.RemoveAll(v => string.Equals(v, targetVowel, System.StringComparison.OrdinalIgnoreCase));

            for (int i = 0; i < distractorPool.Count; i++)
            {
                int rnd = Random.Range(i, distractorPool.Count);
                string temp = distractorPool[i];
                distractorPool[i] = distractorPool[rnd];
                distractorPool[rnd] = temp;
            }

            int correctSlotIndex = Random.Range(0, countToUse);

            int distractorIndex = 0;
            for (int i = 0; i < vowelBuckets.Length; i++)
            {
                if (vowelBuckets[i] == null) continue;

                if (i < countToUse)
                {
                    vowelBuckets[i].gameObject.SetActive(true);

                    if (i == correctSlotIndex)
                    {
                        vowelBuckets[i].SetVowelKey(targetVowel);
                    }
                    else
                    {
                        string distractorVowel = (distractorIndex < distractorPool.Count)
                            ? distractorPool[distractorIndex++]
                            : "a";
                        vowelBuckets[i].SetVowelKey(distractorVowel);
                    }
                }
                else
                {
                    vowelBuckets[i].gameObject.SetActive(false);
                }
            }
        }

        public void PlayWordAudio(AudioClip clip)
        {
            if (clip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
            }
        }

        public void SpeakCardWord(SoundSortData data)
        {
            if (data == null) return;

            if (data.wordAudioClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = data.wordAudioClip;
                voiceAudioSource.Play();
            }

            if (!string.IsNullOrEmpty(data.word))
            {
                SetSubtitles($"This word is '{data.word}'. Put it in the right vowel bucket!");
            }
        }

        private IEnumerator IntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Listen to the middle sound. Put the word in the right vowel bucket!");

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

        public void CheckCardDrop(SoundSortCard card, PointerEventData eventData)
        {
            if (card == null || vowelBuckets == null) return;

            Camera cam = eventData.pressEventCamera;
            foreach (var bucket in vowelBuckets)
            {
                if (bucket != null && bucket.gameObject.activeInHierarchy && bucket.ContainsPosition(eventData.position, cam))
                {
                    EvaluateCardDrop(card, bucket);
                    return;
                }
            }

            card.ReturnToStartPosition();
        }

        public void EvaluateCardDrop(SoundSortCard card, SoundSortBucket bucket)
        {
            if (card == null || bucket == null || isTransitioning) return;

            bool isCorrect = (card.Data != null) && string.Equals(card.Data.targetVowel, bucket.VowelKey, System.StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                StartCoroutine(CorrectDropSequence(card, bucket));
            }
            else
            {
                StartCoroutine(WrongDropSequence(card));
            }
        }

        private IEnumerator CorrectDropSequence(SoundSortCard card, SoundSortBucket bucket)
        {
            isTransitioning = true;

            bucket.PlayDropBounceAnimation();
            card.SetCorrectDrop(bucket.transform.position);

            if (correctChimeSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(correctChimeSfx);
            }

            string word = (card.Data != null) ? card.Data.word : "word";
            string vowel = (card.Data != null) ? card.Data.targetVowel.ToLower() : "vowel";

            SetSubtitles($"Yes! '{word}' has the short {vowel} sound!");

            yield return new WaitForSeconds(0.8f);

            currentCardIndex++;
            if (currentCardIndex >= activeWordList.Count && !isActivityCompleted)
            {
                yield return StartCoroutine(CompletionSequence());
            }
            else
            {
                LoadNextCard();
                isTransitioning = false;
            }
        }

        private IEnumerator WrongDropSequence(SoundSortCard card)
        {
            isTransitioning = true;

            card.PlayWrongWobble();

            if (wrongWobbleSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(wrongWobbleSfx);
            }

            SetSubtitles("Listen again - what sound is in the middle?");

            if (tryAgainClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = tryAgainClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(tryAgainClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            isTransitioning = false;
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            SetSubtitles("Great sorting! You know your middle vowel sounds!");

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
            TopicProgressUI.MarkTopicComplete("Unit5", "SoundSort");

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
