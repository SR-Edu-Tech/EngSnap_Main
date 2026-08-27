
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class U2_SB_ShortVowelManager_Phonics_Junior : MonoBehaviour
    {
        [Header("Data Groups")]
        [Tooltip("Assign 5 groups for ă, ĕ, ĭ, ŏ, ŭ in order")]
        [SerializeField] private U2_SB_ShortVowelGroupData_Phonics_Junior[] vowelGroups;

        [Header("Main Panels")]
        [SerializeField] private GameObject sectionBPanel;
        [SerializeField] private GameObject wordPracticePanel;
        [SerializeField] private GameObject readAndTapPanel;
        [SerializeField] private GameObject vowelSortPanel;
        [SerializeField] private GameObject completionPanel;

        [Header("Tab UI")]
        [SerializeField] private Button[] tabButtons; // 5 Tab buttons for a, e, i, o, u
        [SerializeField] private Color activeTabColor = Color.yellow;
        [SerializeField] private Color inactiveTabColor = Color.white;
        [SerializeField] private Transform wordGridContainer;
        [SerializeField] private GameObject wordCardPrefab;

        [Header("Instruction & Audio")]
        [SerializeField] private GameObject instructionPanel;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private AudioClip instructionAudio;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float typingSpeed = 0.04f;

        [Header("Mascot & Idle")]
        [SerializeField] private MascotController_Phonics_Junior mascotController;
        [SerializeField] private float idleTime = 7f;
        [SerializeField] private AudioClip idleReminderClip;

        [Header("Mini-Game 1: Read & Tap")]
        [SerializeField] private Button replayWordAudioButton;
        [SerializeField] private Button[] readAndTapOptionButtons;
        [SerializeField] private TMP_Text[] readAndTapOptionTexts;

        [Header("Mini-Game 2: Same Vowel Sort")]
        [SerializeField] private TMP_Text vowelSortInstructionText;
        [SerializeField] private Button[] vowelSortWordButtons;
        [SerializeField] private TMP_Text[] vowelSortWordTexts;
        [SerializeField] private Button sortSubmitButton;

        [Header("Feedback Audio Clips")]
        [SerializeField] private AudioClip[] correctFeedbackClips;
        [SerializeField] private AudioClip[] wrongFeedbackClips;

         [Header("Navigation Controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button startMiniGamesButton;
        [SerializeField] private Button prevTabButton;
        [SerializeField] private Button nextTabButton;
        [SerializeField] private GameObject sectionSelectionPanel; // Main Section Selection Menu Panel


        // Internal state
        private int currentGroupIndex = 0;
        private Coroutine wordAudioCoroutine;
        private Coroutine idleCoroutine;
        private U2_SB_ShortVowelWordData_Phonics_Junior currentTargetWord;
        private List<U2_SB_ShortVowelWordData_Phonics_Junior> currentSortOptions = new();
        private HashSet<int> selectedSortIndices = new();
        private ShortVowelType targetSortVowel;

        private void Awake()
        {
            EnsureInit();
        }

        private void OnEnable()
        {
            EnsureInit();
        }

        private void EnsureInit()
        {
            if (sectionBPanel == null) sectionBPanel = gameObject;

            if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
            if (audioSource == null) audioSource = FindFirstObjectByType<AudioSource>();

            if (mascotController == null) mascotController = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);

            Transform searchRoot = transform;
            if (wordPracticePanel == null)
            {
                Transform t = searchRoot.Find("WordPracticePanel");
                if (t == null) t = searchRoot.Find("Word Practice Panel");
                if (t != null) wordPracticePanel = t.gameObject;
            }

            if (instructionPanel == null)
            {
                Transform t = searchRoot.Find("Instruction Panel");
                if (t != null) instructionPanel = t.gameObject;
            }

            if (readAndTapPanel == null)
            {
                Transform t = searchRoot.Find("ReadAndTapPanel");
                if (t == null) t = searchRoot.Find("Read And Tap Panel");
                if (t != null) readAndTapPanel = t.gameObject;
            }

            if (vowelSortPanel == null)
            {
                Transform t = searchRoot.Find("VowelSortPanel");
                if (t == null) t = searchRoot.Find("Vowel Sort Panel");
                if (t != null) vowelSortPanel = t.gameObject;
            }

            if (completionPanel == null)
            {
                Transform t = searchRoot.Find("CompletionPanel");
                if (t == null) t = searchRoot.Find("Completion Panel");
                if (t != null) completionPanel = t.gameObject;
            }

            if (backButton == null && wordPracticePanel != null)
            {
                Transform t = wordPracticePanel.transform.Find("BackButton");
                if (t == null) t = wordPracticePanel.transform.Find("Navigation/Previous Button");
                if (t == null) t = wordPracticePanel.transform.Find("Back Button");
                if (t == null) t = searchRoot.Find("Back Button");
                if (t != null) backButton = t.GetComponent<Button>();
            }
            if (backButton == null) backButton = GetComponentInChildren<Button>(true);

            if (nextTabButton == null && wordPracticePanel != null)
            {
                Transform t = wordPracticePanel.transform.Find("Navigation/Next Button");
                if (t == null) t = wordPracticePanel.transform.Find("Next Button");
                if (t != null) nextTabButton = t.GetComponent<Button>();
            }

            if (prevTabButton == null && wordPracticePanel != null)
            {
                Transform t = wordPracticePanel.transform.Find("Navigation/Previous Button");
                if (t == null) t = wordPracticePanel.transform.Find("Previous Button");
                if (t != null) prevTabButton = t.GetComponent<Button>();
            }

            if (startMiniGamesButton == null && wordPracticePanel != null)
            {
                Transform t = wordPracticePanel.transform.Find("StartMiniGameButton");
                if (t == null) t = wordPracticePanel.transform.Find("Mini Games Button");
                if (t != null) startMiniGamesButton = t.GetComponent<Button>();
            }

            if (sortSubmitButton == null && vowelSortPanel != null)
            {
                Transform t = vowelSortPanel.transform.Find("SubmitButton");
                if (t == null) t = vowelSortPanel.transform.Find("Submit Button");
                if (t == null) t = vowelSortPanel.transform.Find("Submit");
                if (t == null) t = vowelSortPanel.transform.Find("CheckButton");
                if (t == null) t = searchRoot.Find("SubmitButton");
                if (t == null) t = searchRoot.Find("Submit Button");
                if (t != null) sortSubmitButton = t.GetComponent<Button>();
            }

            if (sortSubmitButton != null)
            {
                sortSubmitButton.onClick.RemoveAllListeners();
                sortSubmitButton.onClick.AddListener(CheckVowelSortAnswer);
            }

            if (completionPanel != null)
            {
                Button completionNextBtn = completionPanel.GetComponentInChildren<Button>(true);
                if (completionNextBtn != null)
                {
                    completionNextBtn.onClick.RemoveAllListeners();
                    completionNextBtn.onClick.AddListener(OnCompletionNextButtonClicked);
                }
            }

            // Bind button listeners dynamically
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
            }

            if (nextTabButton != null)
            {
                nextTabButton.onClick.RemoveAllListeners();
                nextTabButton.onClick.AddListener(NextTab);
            }

            if (prevTabButton != null)
            {
                prevTabButton.onClick.RemoveAllListeners();
                prevTabButton.onClick.AddListener(PreviousTab);
            }

            if (startMiniGamesButton != null)
            {
                startMiniGamesButton.onClick.RemoveAllListeners();
                startMiniGamesButton.onClick.AddListener(StartReadAndTapGame);
            }

            if (tabButtons != null)
            {
                for (int i = 0; i < tabButtons.Length; i++)
                {
                    int idx = i;
                    if (tabButtons[i] != null)
                    {
                        tabButtons[i].onClick.RemoveAllListeners();
                        tabButtons[i].onClick.AddListener(() => SelectTab(idx));
                    }
                }
            }
        }

        private void Start()
        {
            EnsureInit();
        }

        public void OpenSectionB()
        {
            EnsureInit();

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

            // Force activate parent chain up to Canvas
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
            if (sectionBPanel != null && sectionBPanel != gameObject) sectionBPanel.SetActive(true);

            if (wordPracticePanel != null) wordPracticePanel.SetActive(false);
            if (readAndTapPanel != null) readAndTapPanel.SetActive(false);
            if (vowelSortPanel != null) vowelSortPanel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(false);

            StartCoroutine(ShowInstruction());
        }

        // ------------------ TAB NAVIGATION & WORD PRACTICE ------------------

        public void SelectTab(int index)
        {
            if (index < 0 || index >= vowelGroups.Length) return;

            currentGroupIndex = index;
            RestartIdleTimer();

            // Update tab highlights
            for (int i = 0; i < tabButtons.Length; i++)
            {
                Image tabImg = tabButtons[i].GetComponent<Image>();
                if (tabImg != null)
                {
                    tabImg.color = (i == currentGroupIndex) ? activeTabColor : inactiveTabColor;
                }
            }

            PopulateWordGrid(vowelGroups[currentGroupIndex]);

            // Update Mini Games Button visibility (only active on the last vowel tab 'u')
            if (startMiniGamesButton != null)
            {
                bool isLastTab = (currentGroupIndex == vowelGroups.Length - 1);
                startMiniGamesButton.gameObject.SetActive(isLastTab);
            }
        }

        private void PopulateWordGrid(U2_SB_ShortVowelGroupData_Phonics_Junior group)
        {
            // Clear existing grid
            foreach (Transform child in wordGridContainer)
            {
                Destroy(child.gameObject);
            }

            if (group == null || group.words == null) return;

            foreach (var wordData in group.words)
            {
                GameObject card = Instantiate(wordCardPrefab, wordGridContainer);
                TMP_Text txt = card.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = wordData.wordText;

                Button btn = card.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnWordCardClicked(wordData, card));
                }
            }
        }

        private void OnWordCardClicked(U2_SB_ShortVowelWordData_Phonics_Junior wordData, GameObject cardObj)
        {
            RestartIdleTimer();

            if (wordAudioCoroutine != null) StopCoroutine(wordAudioCoroutine);
            wordAudioCoroutine = StartCoroutine(PlaySequentialWordAudio(wordData, cardObj));
        }

        private IEnumerator PlaySequentialWordAudio(U2_SB_ShortVowelWordData_Phonics_Junior wordData, GameObject
  cardObj)
        {
            audioSource.Stop();

            // 1. Play Slow / Segmented Audio
            if (wordData.slowAudio != null)
            {
                audioSource.clip = wordData.slowAudio;
                audioSource.Play();
                yield return new WaitForSeconds(wordData.slowAudio.length + 0.25f);
            }

            // 2. Play Natural / Full Word Audio
            if (wordData.naturalAudio != null)
            {
                audioSource.clip = wordData.naturalAudio;
                audioSource.Play();
                yield return new WaitForSeconds(wordData.naturalAudio.length);
            }
        }

         // ------------------ NAVIGATION METHODS ------------------

    public void OnBackButtonClicked()
    {
        StopAllCoroutines();
        audioSource.Stop();
        if (mascotController != null) mascotController.HideMascot();

        if (vowelSortPanel != null && vowelSortPanel.activeSelf)
        {
            StartReadAndTapGame(); // Go back to Mini-Game 1
        }
        else if (readAndTapPanel != null && readAndTapPanel.activeSelf)
        {
            ReturnToWordPractice(); // Go back to Word Grid
        }
        else
        {
            CloseSectionB(); // Return to Main Menu
        }
    }

    public void ReturnToWordPractice()
    {
        wordPracticePanel.SetActive(true);
        readAndTapPanel.SetActive(false);
        vowelSortPanel.SetActive(false);
        completionPanel.SetActive(false);
    }

        public void CloseSectionB()
        {
            StopAllCoroutines();
            if (audioSource != null) audioSource.Stop();
            if (mascotController != null) mascotController.HideMascot();

            if (wordPracticePanel != null) wordPracticePanel.SetActive(false);
            if (readAndTapPanel != null) readAndTapPanel.SetActive(false);
            if (vowelSortPanel != null) vowelSortPanel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(false);
            if (instructionPanel != null) instructionPanel.SetActive(false);

            if (sectionBPanel != null) sectionBPanel.SetActive(false);
            gameObject.SetActive(false);
        }

        public void OnCompletionNextButtonClicked()
        {
            CloseSectionB();
            Unit_Selection_Panel_Phonics_Junior unitPanel = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
            if (unitPanel != null)
            {
                unitPanel.OpenUnit2SectionC();
            }
        }

    public void NextTab()
    {
        if (currentGroupIndex < vowelGroups.Length - 1)
        {
            SelectTab(currentGroupIndex + 1);
        }
    }

    public void PreviousTab()
    {
        if (currentGroupIndex > 0)
        {
            SelectTab(currentGroupIndex - 1);
        }
    }

        // ------------------ MINI-GAME 1: READ & TAP ------------------

        public void StartReadAndTapGame()
        {
              StopIdleTimer(); 
            wordPracticePanel.SetActive(false);
            readAndTapPanel.SetActive(true);
            vowelSortPanel.SetActive(false);

            SetupReadAndTapRound();
        }

        private void SetupReadAndTapRound()
        {
            var group = vowelGroups[currentGroupIndex];
            if (group == null || group.words.Count < 3) return;

            // Pick target word randomly
            int targetIdx = Random.Range(0, group.words.Count);
            currentTargetWord = group.words[targetIdx];

            // Prepare 3 options
            List<U2_SB_ShortVowelWordData_Phonics_Junior> options = new() { currentTargetWord };
            while (options.Count < 3)
            {
                var randomWord = group.words[Random.Range(0, group.words.Count)];
                if (!options.Contains(randomWord)) options.Add(randomWord);
            }

            // Shuffle options
            for (int i = 0; i < options.Count; i++)
            {
                int rnd = Random.Range(i, options.Count);
                (options[i], options[rnd]) = (options[rnd], options[i]);
            }

             // Display options cleanly
        for (int i = 0; i < readAndTapOptionButtons.Length && i < options.Count; i++)
       {
        int index = i;
        U2_SB_ShortVowelWordData_Phonics_Junior optWord = options[index];
        Button optButton = readAndTapOptionButtons[index];

        if (readAndTapOptionTexts[index] != null)
        {
            readAndTapOptionTexts[index].text = optWord.wordText;
        }

        if (optButton != null)
        {
            optButton.onClick.RemoveAllListeners();
            optButton.onClick.AddListener(() => CheckReadAndTapAnswer(optWord, optButton));
        }
        }
            PlayTargetWordSound();
        }

        public void PlayTargetWordSound()
        {
            if (currentTargetWord != null && currentTargetWord.naturalAudio != null)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(currentTargetWord.naturalAudio);
            }
        }

        private void CheckReadAndTapAnswer(U2_SB_ShortVowelWordData_Phonics_Junior selectedWord, Button
  clickedButton)
        {
            if (selectedWord == currentTargetWord)
            {
                StartCoroutine(PlayCorrectFeedback(clickedButton, () => StartVowelSortGame()));
            }
            else
            {
                StartCoroutine(PlayWrongFeedback(clickedButton));
            }
        }

        // ------------------ MINI-GAME 2: SAME VOWEL SORT ------------------

        public void StartVowelSortGame()
        {
            StopIdleTimer(); 
            readAndTapPanel.SetActive(false);
            vowelSortPanel.SetActive(true);

            SetupVowelSortRound();
        }

        private void SetupVowelSortRound()
        {
            selectedSortIndices.Clear();

            // Pick a target vowel type
            targetSortVowel = (ShortVowelType)currentGroupIndex;
            var targetGroup = vowelGroups[currentGroupIndex];

            vowelSortInstructionText.text = $"Tap all the words with the /{targetGroup.vowelSymbol}/ sound!";

            currentSortOptions.Clear();

            // Add 2 matching words from target group
            for (int i = 0; i < 2 && i < targetGroup.words.Count; i++)
            {
                currentSortOptions.Add(targetGroup.words[i]);
            }

            // Add 2 distractor words from other vowel groups
            int otherGroupIdx = (currentGroupIndex + 1) % vowelGroups.Length;
            var otherGroup = vowelGroups[otherGroupIdx];
            for (int i = 0; i < 2 && i < otherGroup.words.Count; i++)
            {
                currentSortOptions.Add(otherGroup.words[i]);
            }

            // Bind buttons
            for (int i = 0; i < vowelSortWordButtons.Length; i++)
            {
                int idx = i;
                vowelSortWordTexts[i].text = currentSortOptions[i].wordText;
                if (vowelSortWordButtons[i] != null)
                {
                    vowelSortWordButtons[i].GetComponent<Image>().color = Color.white;
                    vowelSortWordButtons[i].transform.localScale = Vector3.one;
                    vowelSortWordButtons[i].onClick.RemoveAllListeners();
                    vowelSortWordButtons[i].onClick.AddListener(() => ToggleSortSelection(idx));
                }
            }
        }

        private void ToggleSortSelection(int index)
        {
            if (index < 0 || index >= vowelSortWordButtons.Length) return;

            Image btnImg = vowelSortWordButtons[index].GetComponent<Image>();
            Transform btnTrans = vowelSortWordButtons[index].transform;

            if (selectedSortIndices.Contains(index))
            {
                selectedSortIndices.Remove(index);
                if (btnImg != null) btnImg.color = Color.white;
                if (btnTrans != null) btnTrans.localScale = Vector3.one;
            }
            else
            {
                selectedSortIndices.Add(index);
                if (btnImg != null) btnImg.color = new Color(1f, 0.88f, 0.2f, 1f); // Vibrant Gold Highlight!
                if (btnTrans != null) btnTrans.localScale = Vector3.one * 1.12f; // 12% Pop Scale!
            }
        }

        private void CheckVowelSortAnswer()
        {
            bool allCorrect = true;

            for (int i = 0; i < currentSortOptions.Count; i++)
            {
                bool isMatchingVowel = (currentSortOptions[i].vowelType == targetSortVowel);
                bool isSelected = selectedSortIndices.Contains(i);

                if (isMatchingVowel != isSelected)
                {
                    allCorrect = false;
                    break;
                }
            }

            if (allCorrect)
            {
                StartCoroutine(PlayCorrectFeedback(null, () => CompleteSection()));
            }
            else
            {
                StartCoroutine(PlayWrongFeedback(null));
            }
        }

        // ------------------ FEEDBACK & HELPERS ------------------

        private IEnumerator PlayCorrectFeedback(Button button, System.Action onComplete)
        {
            if (button != null) yield return StartCoroutine(FlashButton(button, Color.green));

            mascotController.ShowMascot();
            mascotController.PlayHiAnimation();

            if (correctFeedbackClips.Length > 0)
            {
                AudioClip clip = correctFeedbackClips[Random.Range(0, correctFeedbackClips.Length)];
                audioSource.PlayOneShot(clip);
                yield return new WaitForSeconds(clip.length);
            }

            mascotController.HideMascot();
            onComplete?.Invoke();
        }

        private IEnumerator PlayWrongFeedback(Button button)
        {
            if (button != null)
            {
                yield return StartCoroutine(FlashButton(button, Color.red));
                yield return StartCoroutine(ShakeTransform(button.transform));
            }

            if (wrongFeedbackClips.Length > 0)
            {
                AudioClip clip = wrongFeedbackClips[Random.Range(0, wrongFeedbackClips.Length)];
                audioSource.PlayOneShot(clip);
            }
        }

        private IEnumerator FlashButton(Button btn, Color flashColor)
        {
            Image img = btn.GetComponent<Image>();
            if (img == null) yield break;
            Color orig = img.color;
            img.color = flashColor;
            yield return new WaitForSeconds(0.2f);
            img.color = orig;
        }

        private IEnumerator ShakeTransform(Transform trans)
        {
            Vector3 orig = trans.localPosition;
            for (float t = 0; t < 0.2f; t += Time.deltaTime)
            {
                trans.localPosition = orig + Vector3.right * (Mathf.Sin(t * 50f) * 8f);
                yield return null;
            }
            trans.localPosition = orig;
        }

        private void CompleteSection()
        {
            StopIdleTimer(); 
            vowelSortPanel.SetActive(false);
            completionPanel.SetActive(true);
        }

        private IEnumerator ShowInstruction()
        {
            if (instructionPanel != null) instructionPanel.SetActive(true);
            if (instructionText != null) instructionText.text = "";

// Read out the words to discern the sound that the short vowels make.
            string msg = "Let's practice short vowels together!";

            if (instructionAudio != null && audioSource != null)
            {
                audioSource.PlayOneShot(instructionAudio);
            }

            if (instructionText != null)
            {
                foreach (char c in msg)
                {
                    instructionText.text += c;
                    yield return new WaitForSeconds(typingSpeed);
                }
            }

            float audioDuration = (instructionAudio != null) ? instructionAudio.length : 0f;
            float textDuration = msg.Length * typingSpeed;
            float remainingWait = Mathf.Max(0.5f, audioDuration - textDuration);

            yield return new WaitForSeconds(remainingWait);

            if (instructionPanel != null) instructionPanel.SetActive(false);

            if (wordPracticePanel != null) wordPracticePanel.SetActive(true);
            SelectTab(0);
        }

    private void StopIdleTimer()
    {
        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }
    }

        private void RestartIdleTimer()
        {
            if (idleCoroutine != null)
            {
                StopCoroutine(idleCoroutine);
                idleCoroutine = null;
            }

            if (gameObject.activeInHierarchy)
            {
                idleCoroutine = StartCoroutine(IdleReminder());
            }
        }

        private IEnumerator IdleReminder()
    {
        yield return new WaitForSeconds(idleTime);

        // Only play reminder if Word Practice panel is active and audio isn't playing
        if (wordPracticePanel != null && wordPracticePanel.activeSelf && !audioSource.isPlaying)
        {
            mascotController.ShowMascot();
            mascotController.PlayHiAnimation();
            audioSource.PlayOneShot(idleReminderClip);
            yield return new WaitForSeconds(idleReminderClip.length);
            mascotController.HideMascot();
        }
    }
    }