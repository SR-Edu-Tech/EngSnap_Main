using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EngSnap.Unit4
{
    public class VowelOrConsonantController : MonoBehaviour
    {
        [Header("Mascot & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Baskets UI")]
        [SerializeField] private VowelOrConsonantBasket vowelBasket;
        [SerializeField] private VowelOrConsonantBasket consonantBasket;

        [Header("Letter Tile Elements")]
        [SerializeField] private VowelOrConsonantTile singleActiveTile; // Single tile drop mode
        [SerializeField] private VowelOrConsonantTile[] poolTiles;       // Multi tile drop mode
        [SerializeField] private Transform tileSpawnParent;

        [Header("Falling Tile Settings")]
        [Tooltip("Optional transform for Point A boundary (top-left or top-start).")]
        [SerializeField] private RectTransform spawnPointA;
        [Tooltip("Optional transform for Point B boundary (bottom-right or top-end).")]
        [SerializeField] private RectTransform spawnPointB;
        [Tooltip("Y position near top of screen where tile spawns (used if Point A & B unassigned).")]
        [SerializeField] private float topSpawnY = 400f;
        [Tooltip("Y position in play area where tile lands after falling (used if Point A & B unassigned).")]
        [SerializeField] private float landingY = 50f;
        [Tooltip("Min X range for random top spawn (used if Point A & B unassigned).")]
        [SerializeField] private float minSpawnX = -250f;
        [Tooltip("Max X range for random top spawn (used if Point A & B unassigned).")]
        [SerializeField] private float maxSpawnX = 250f;
        [Tooltip("Duration of fall animation from top to landing position.")]
        [SerializeField] private float fallDuration = 0.8f;
        [Tooltip("If true, letter order is randomized on level reset.")]
        [SerializeField] private bool randomizeLetterOrder = true;

        [Header("Sorting Data Sets")]
        [SerializeField] private VowelOrConsonantData[] roundLetterData;

        [Header("Voice Script Audio Clips")]
        [SerializeField] private AudioClip introClip;             // "Is it a vowel or a consonant? Drop each letter in the right basket!"
        [SerializeField] private AudioClip hintVowelsClip;        // "Remember the vowels: a, e, i, o, u!"
        [SerializeField] private AudioClip genericPraiseClip;     // "Yes! That's right!"
        [SerializeField] private AudioClip tryAgainClip;          // "Oops - is that one a vowel? Try again!"
        [SerializeField] private AudioClip completionPraiseClip;  // "Awesome job sorting vowels and consonants!"
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip wrongWobbleSfx;

        [Header("Hint Button")]
        [SerializeField] private Button hintButton;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentTileIndex = 0;
        private int correctDropCount = 0;
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;
        private List<VowelOrConsonantData> activeLetterList = new List<VowelOrConsonantData>();

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

            if (hintButton != null)
            {
                hintButton.onClick.RemoveAllListeners();
                hintButton.onClick.AddListener(PlayVowelHint);
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
            currentTileIndex = 0;
            correctDropCount = 0;
            isTransitioning = false;
            isActivityCompleted = false;

            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);

            activeLetterList.Clear();
            if (roundLetterData != null && roundLetterData.Length > 0)
            {
                activeLetterList.AddRange(roundLetterData);
                if (randomizeLetterOrder)
                {
                    for (int i = 0; i < activeLetterList.Count; i++)
                    {
                        int rnd = Random.Range(i, activeLetterList.Count);
                        VowelOrConsonantData temp = activeLetterList[i];
                        activeLetterList[i] = activeLetterList[rnd];
                        activeLetterList[rnd] = temp;
                    }
                }
            }

            if (poolTiles != null)
            {
                for (int i = 0; i < poolTiles.Length; i++)
                {
                    if (poolTiles[i] != null)
                    {
                        poolTiles[i].gameObject.SetActive(false);
                    }
                }
            }

            LoadNextTile();
            SetSubtitles("Is it a vowel or a consonant? Drop each letter in the right basket!");
        }

        private void LoadNextTile()
        {
            if (activeLetterList == null || activeLetterList.Count == 0) return;

            if (singleActiveTile != null)
            {
                if (currentTileIndex < activeLetterList.Count)
                {
                    VowelOrConsonantData data = activeLetterList[currentTileIndex];

                    float minX = minSpawnX;
                    float maxX = maxSpawnX;
                    float startY = topSpawnY;
                    float endY = landingY;

                    if (spawnPointA != null && spawnPointB != null)
                    {
                        minX = Mathf.Min(spawnPointA.anchoredPosition.x, spawnPointB.anchoredPosition.x);
                        maxX = Mathf.Max(spawnPointA.anchoredPosition.x, spawnPointB.anchoredPosition.x);
                        startY = Mathf.Max(spawnPointA.anchoredPosition.y, spawnPointB.anchoredPosition.y);

                        float lowerY = Mathf.Min(spawnPointA.anchoredPosition.y, spawnPointB.anchoredPosition.y);
                        // If Point A and B are at different heights, use lowerY for endY. Otherwise use landingY.
                        if (Mathf.Abs(spawnPointA.anchoredPosition.y - spawnPointB.anchoredPosition.y) > 20f)
                        {
                            endY = lowerY;
                        }
                        else
                        {
                            endY = landingY;
                        }
                    }
                    else if (spawnPointA != null)
                    {
                        startY = spawnPointA.anchoredPosition.y;
                    }
                    else if (spawnPointB != null)
                    {
                        endY = spawnPointB.anchoredPosition.y;
                    }

                    // Safety guarantee: startY must be higher than endY for tile to fall down
                    if (startY <= endY)
                    {
                        startY = endY + 350f;
                    }

                    float centerX = 0f;
                    if (spawnPointA != null && spawnPointB != null)
                    {
                        centerX = (spawnPointA.anchoredPosition.x + spawnPointB.anchoredPosition.x) * 0.5f;
                    }
                    else if (spawnPointA != null)
                    {
                        centerX = spawnPointA.anchoredPosition.x;
                    }
                    else if (spawnPointB != null)
                    {
                        centerX = spawnPointB.anchoredPosition.x;
                    }

                    // Straight vertical falling down Y-axis keeping X centered
                    Vector2 spawnPos = new Vector2(centerX, startY);
                    Vector2 landingPos = new Vector2(centerX, endY);

                    singleActiveTile.Setup(data, this);
                    singleActiveTile.AnimateFallFromTop(spawnPos, landingPos, fallDuration);
                }
                else
                {
                    singleActiveTile.gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator IntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Is it a vowel or a consonant? Drop each letter in the right basket!");

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

        public void PlayVowelHint()
        {
            if (isTransitioning) return;
            StartCoroutine(HintSequence());
        }

        private IEnumerator HintSequence()
        {
            isTransitioning = true;
            SetSubtitles("Remember the vowels: a, e, i, o, u!");

            if (hintVowelsClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = hintVowelsClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(hintVowelsClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
        }

        public void CheckTileDrop(VowelOrConsonantTile tile, PointerEventData eventData)
        {
            if (tile == null) return;

            Camera cam = eventData.pressEventCamera;
            if (vowelBasket != null && vowelBasket.ContainsPosition(eventData.position, cam))
            {
                EvaluateTileDrop(tile, vowelBasket);
            }
            else if (consonantBasket != null && consonantBasket.ContainsPosition(eventData.position, cam))
            {
                EvaluateTileDrop(tile, consonantBasket);
            }
            else
            {
                tile.ReturnToStartPosition();
            }
        }

        public void EvaluateTileDrop(VowelOrConsonantTile tile, VowelOrConsonantBasket targetBasket)
        {
            if (tile == null || targetBasket == null || isTransitioning) return;

            bool isVowelBasket = (targetBasket.Type == VowelOrConsonantBasket.BasketType.Vowel);
            bool isCorrect = (tile.Data != null) && (tile.Data.isVowel == isVowelBasket);

            if (isCorrect)
            {
                StartCoroutine(CorrectDropSequence(tile, targetBasket));
            }
            else
            {
                StartCoroutine(WrongDropSequence(tile));
            }
        }

        private IEnumerator CorrectDropSequence(VowelOrConsonantTile tile, VowelOrConsonantBasket basket)
        {
            isTransitioning = true;

            basket.PlayDropBounceAnimation();
            tile.SetCorrectDrop(basket.transform.position);

            if (correctChimeSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(correctChimeSfx);
            }

            correctDropCount++;
            string letter = (tile.Data != null) ? tile.Data.letter.ToLower() : "letter";
            bool isVowel = (tile.Data != null) && tile.Data.isVowel;

            string praiseText = isVowel ? $"Yes! '{letter}' is a vowel!" : $"Yes! '{letter}' is a consonant!";
            SetSubtitles(praiseText);

            if (genericPraiseClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = genericPraiseClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(genericPraiseClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            currentTileIndex++;
            int totalTarget = (activeLetterList != null && activeLetterList.Count > 0) ? activeLetterList.Count : ((roundLetterData != null) ? roundLetterData.Length : 5);

            if (currentTileIndex >= totalTarget && !isActivityCompleted)
            {
                yield return StartCoroutine(CompletionSequence());
            }
            else
            {
                LoadNextTile();
                isTransitioning = false;
            }
        }

        private IEnumerator WrongDropSequence(VowelOrConsonantTile tile)
        {
            isTransitioning = true;

            tile.PlayWrongWobble();

            if (wrongWobbleSfx != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(wrongWobbleSfx);
            }

            SetSubtitles("Oops - is that one a vowel? Try again!");

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

            SetSubtitles("Great sorting! You know your vowels and consonants!");

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

            TopicProgressUI.MarkTopicComplete("Unit4", "VowelOrConsonant");

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
