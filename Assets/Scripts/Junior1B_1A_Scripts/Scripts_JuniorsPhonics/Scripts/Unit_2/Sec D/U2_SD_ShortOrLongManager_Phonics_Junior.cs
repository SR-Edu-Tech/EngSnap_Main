
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class U2_SD_ShortOrLongManager_Phonics_Junior : MonoBehaviour
    {
        public class WordItem
        {
            public string wordText;
            public AudioClip naturalAudio;
            public bool isShortVowel; // true = Short (Breve ˘), false = Long (Macron ¯)
        }

        [Header("Data Source")]
        [SerializeField] private U2_SB_ShortVowelGroupData_Phonics_Junior[] shortVowelGroups;
        [SerializeField] private U2_SC_LongVowelGroupData_Phonics_Junior[] longVowelGroups;

        [Header("Main UI Panels")]
        [SerializeField] private GameObject sectionDPanel;
        [SerializeField] private GameObject gamePanel;
        [SerializeField] private GameObject completionPanel;

        [Header("Word Card UI")]
        [SerializeField] private GameObject wordCardObject;
        [SerializeField] private TMP_Text wordCardText;
        [SerializeField] private Button replayAudioButton;

        [Header("Baskets UI")]
        [SerializeField] private Button shortBasketButton; // Short Basket (breve ˘)
        [SerializeField] private Button longBasketButton;  // Long Basket (macron ¯)

        [Header("Progress & Audio")]
        [SerializeField] private TMP_Text progressText;  // e.g. "1 / 6"
        [SerializeField] private TMP_Text starCountText; // e.g. "0" (Star counter next to mascot)
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip instructionAudio;
        [SerializeField] private AudioClip correctSFX;
        [SerializeField] private AudioClip wrongSFX;
        [SerializeField] private MascotController_Phonics_Junior mascotController;

        [Header("Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private GameObject sectionSelectionPanel;

        [Header("Round Settings")]
        [SerializeField] private int wordsPerRound = 6;

        // Internal State
        private List<WordItem> roundWordPool = new List<WordItem>();
        private int currentWordIndex = 0;
        private int starScore = 0;
        private WordItem currentWord;
        private U2_SD_DraggableWordCard_Phonics_Junior draggableCard;
        private bool isProcessingAnswer = false;

        private void Awake()
        {
            InitializeManager();
        }

        private void InitializeManager()
        {
            if (wordCardObject != null)
            {
                draggableCard = wordCardObject.GetComponent<U2_SD_DraggableWordCard_Phonics_Junior>();
            }

            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(PlayCurrentWordSound);
            }
            if (shortBasketButton != null)
            {
                shortBasketButton.onClick.RemoveAllListeners();
                shortBasketButton.onClick.AddListener(() => OnCardDroppedOnBasket(true));
            }
            if (longBasketButton != null)
            {
                longBasketButton.onClick.RemoveAllListeners();
                longBasketButton.onClick.AddListener(() => OnCardDroppedOnBasket(false));
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(CloseSectionD);
            }
        }

        private void HideSectionSelectionPanels()
        {
            Unit_Selection_Panel_Phonics_Junior unitSel = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
            if (unitSel != null)
            {
                unitSel.HideSelectionPanels();
            }

            GameObject sel1 = GameObject.Find("Unit_1_Section_Selection_Panels");
            if (sel1 != null) sel1.SetActive(false);

            GameObject sel2 = GameObject.Find("Unit_2_Section_Selection_Panels");
            if (sel2 != null) sel2.SetActive(false);
        }

        public void OpenSectionD()
        {
            InitializeManager();

            // Deactivate all sibling sections under Unit_2_Sections
            if (transform.parent != null)
            {
                foreach (Transform sibling in transform.parent)
                {
                    if (sibling != transform)
                    {
                        sibling.gameObject.SetActive(false);
                        foreach (Transform sub in sibling)
                        {
                            sub.gameObject.SetActive(false);
                        }
                    }
                }
            }

            // Force activate entire parent chain up to Canvas
            Transform curr = transform;
            while (curr != null && curr.gameObject.name != "Canvas")
            {
                if (!curr.gameObject.activeSelf)
                {
                    curr.gameObject.SetActive(true);
                }
                curr = curr.parent;
            }

            gameObject.SetActive(true);
            if (sectionDPanel != null && sectionDPanel != gameObject) sectionDPanel.SetActive(true);

            HideSectionSelectionPanels();

            // Stop any leftover audio from other sections
            StopAllCoroutines();
            if (audioSource != null) audioSource.Stop();

            if (gamePanel != null) gamePanel.SetActive(true);
            if (completionPanel != null) completionPanel.SetActive(false);
            if (wordCardObject != null) wordCardObject.SetActive(false);

            BuildRoundWordPool();
            currentWordIndex = 0;
            starScore = 0;
            UpdateStarDisplay();
            isProcessingAnswer = false;

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(ShowInstructionRoutine());
            }
            else
            {
                DisplayCurrentQuestion();
            }
        }

        private void BuildRoundWordPool()
        {
            roundWordPool.Clear();
            List<WordItem> allShorts = new List<WordItem>();
            List<WordItem> allLongs = new List<WordItem>();

            // Collect Short Vowel Words
            if (shortVowelGroups != null)
            {
                foreach (var g in shortVowelGroups)
                {
                    if (g == null || g.words == null) continue;
                    foreach (var w in g.words)
                    {
                        if (w == null) continue;
                        AudioClip clip = (w.naturalAudio != null) ? w.naturalAudio : w.slowAudio;
                        allShorts.Add(new WordItem { wordText = w.wordText, naturalAudio = clip, isShortVowel = true
  });
                    }
                }
            }

            // Collect Long Vowel Words
            if (longVowelGroups != null)
            {
                foreach (var g in longVowelGroups)
                {
                    if (g == null || g.words == null) continue;
                    foreach (var w in g.words)
                    {
                        if (w == null) continue;
                        AudioClip clip = (w.naturalAudio != null) ? w.naturalAudio : w.slowAudio;
                        allLongs.Add(new WordItem { wordText = w.wordText, naturalAudio = clip, isShortVowel = false
  });
                    }
                }
            }

            ShuffleList(allShorts);
            ShuffleList(allLongs);

            int targetHalf = wordsPerRound / 2;
            for (int i = 0; i < targetHalf && i < allShorts.Count; i++) roundWordPool.Add(allShorts[i]);
            for (int i = 0; i < targetHalf && i < allLongs.Count; i++) roundWordPool.Add(allLongs[i]);

            ShuffleList(roundWordPool);
        }

        private void DisplayCurrentQuestion()
        {
            if (currentWordIndex >= roundWordPool.Count)
            {
                CompleteSection();
                return;
            }

            currentWord = roundWordPool[currentWordIndex];
            isProcessingAnswer = false;

            if (wordCardText != null) wordCardText.text = currentWord.wordText;

            if (wordCardObject != null)
            {
                wordCardObject.SetActive(true);
                if (draggableCard != null) draggableCard.ResetPosition();
            }

            if (progressText != null)
            {
                progressText.text = $"{currentWordIndex + 1} / {roundWordPool.Count}";
            }

            PlayCurrentWordSound();
        }

       
    public void PlayCurrentWordSound()
    {
        if (currentWord != null && currentWord.naturalAudio != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = currentWord.naturalAudio;
            audioSource.Play();
        }
    }

        public void OnCardDroppedOnBasket(bool choseShort)
        {
            if (isProcessingAnswer || currentWord == null) return;
            isProcessingAnswer = true;

            bool isCorrect = (choseShort == currentWord.isShortVowel);

            if (isCorrect)
            {
                StartCoroutine(PlayCorrectRoutine());
            }
            else
            {
                StartCoroutine(PlayWrongRoutine());
            }
        }

        private IEnumerator PlayCorrectRoutine()
        {
            starScore++;
            UpdateStarDisplay();

            if (audioSource != null && correctSFX != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(correctSFX);
            }

            if (mascotController != null)
            {
                mascotController.ShowMascot();
                mascotController.PlayHiAnimation();
            }

            if (wordCardObject != null) wordCardObject.SetActive(false);

            yield return new WaitForSeconds(0.8f);
            if (mascotController != null) mascotController.HideMascot();

            currentWordIndex++;
            DisplayCurrentQuestion();
        }

        private IEnumerator PlayWrongRoutine()
    {
        // 1. Play Wrong / Try Again SFX
        if (audioSource != null)
        {
            audioSource.Stop();
            if (wrongSFX != null)
            {
                audioSource.PlayOneShot(wrongSFX);
                yield return new WaitForSeconds(wrongSFX.length + 0.15f);
            }
            else
            {
                yield return new WaitForSeconds(0.4f);
            }
        }

        // 2. Reset card back to center
        if (draggableCard != null) draggableCard.ResetPosition();

        // 3. Replay the word sound clearly so the child can try again
        PlayCurrentWordSound();

        isProcessingAnswer = false;
    }

        private IEnumerator ShowInstructionRoutine()
        {
            if (instructionAudio != null && audioSource != null)
            {
                if (mascotController != null) mascotController.ShowMascot();
                audioSource.Stop();
                audioSource.PlayOneShot(instructionAudio);
                yield return new WaitForSeconds(instructionAudio.length);
                if (mascotController != null) mascotController.HideMascot();
            }
            DisplayCurrentQuestion();
        }

        private void UpdateStarDisplay()
        {
            if (starCountText != null)
            {
                starCountText.text = starScore.ToString();
            }
        }

        private void CompleteSection()
        {
            if (gamePanel != null) gamePanel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(true);

            if (mascotController != null)
            {
                mascotController.ShowMascot();
                mascotController.PlayHiAnimation();
            }
        }

        public void CloseSectionD()
        {
            StopAllCoroutines();
            if (audioSource != null) audioSource.Stop();
            if (mascotController != null) mascotController.HideMascot();

            if (sectionDPanel != null) sectionDPanel.SetActive(false);
            if (sectionSelectionPanel != null) sectionSelectionPanel.SetActive(true);
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, list.Count);
                (list[i], list[rnd]) = (list[rnd], list[i]);
            }
        }
    }