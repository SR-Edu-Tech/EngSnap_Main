 using System.Collections;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class U2_SA_VowelPairsManager_Phonics_Junior : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private U2_VowelPairData_Phonics_Junior[] vowelPairs; // 5 Vowel Pairs (A, E, I, O, U)

        [Header("Exploration UI")]
        [SerializeField] private GameObject mainLessonScreen;
        [SerializeField] private TMP_Text vowelTitleText;

        // Short Vowel UI (Breve ˘)
        [SerializeField] private TMP_Text shortSymbolText;
        [SerializeField] private TMP_Text shortWordText;
        [SerializeField] private Image shortImageDisplay;
        [SerializeField] private Button shortCardButton;

        // Long Vowel UI (Macron ¯)
        [SerializeField] private TMP_Text longSymbolText;
        [SerializeField] private TMP_Text longWordText;
        [SerializeField] private Image longImageDisplay;
        [SerializeField] private Button longCardButton;

        // Navigation
        [SerializeField] private Button nextVowelButton;
        [SerializeField] private Button prevVowelButton;

       
        [Header("Mini-Check (Quiz) UI")]
         [SerializeField] private GameObject miniCheckPanel;
          [SerializeField] private TMP_Text quizVowelText;
           [SerializeField] private Button breveOptionButton;  // Breve (˘) Option Card
           [SerializeField] private Button macronOptionButton; // Macron (¯) Option Card
           [SerializeField] private Button replayAudioButton;

           [Header("Quiz Option Card Elements")]
            // Short Vowel Quiz Card Elements
          [SerializeField] private TMP_Text quizShortSymbolText;
          [SerializeField] private TMP_Text quizShortWordText;
        //    [SerializeField] private Image quizShortImageDisplay;

          // Long Vowel Quiz Card Elements
           [SerializeField] private TMP_Text quizLongSymbolText;
           [SerializeField] private TMP_Text quizLongWordText;
            // [SerializeField] private Image quizLongImageDisplay;

             [Header("Quiz Target Question Elements")]
        [SerializeField] private Image quizTargetImageDisplay; // The central target picture (e.g. VowelImage under SpeakerButton)

        [Header("Feedback Audio")]
        [SerializeField] private AudioClip correctClip;
        [SerializeField] private AudioClip wrongClip;

        [Header("Instruction & Narration")]
        [SerializeField] private GameObject instructionPanel;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private AudioClip instructionClip; // "The little curve means short. The straight line means long."
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private MascotController_Phonics_Junior mascotController;

        [Header("Completion")]
        [SerializeField] private GameObject completionPanel;

        private int currentVowelIndex = 0;
        private bool canClick = true;

        // Mini-check state
        private bool isTargetShort = true;
        private int quizQuestionCount = 0;

        private void Start()
        {

            if (shortCardButton != null) shortCardButton.onClick.AddListener(PlayShortSound);
            if (longCardButton != null) longCardButton.onClick.AddListener(PlayLongSound);

            // if (nextVowelButton != null) nextVowelButton.onClick.AddListener(NextVowel);
            // if (prevVowelButton != null) prevVowelButton.onClick.AddListener(PreviousVowel);

            if (breveOptionButton != null) breveOptionButton.onClick.AddListener(() => CheckMiniCheckAnswer(true));
            if (macronOptionButton != null) macronOptionButton.onClick.AddListener(() => CheckMiniCheckAnswer(false));
            if (replayAudioButton != null) replayAudioButton.onClick.AddListener(ReplayQuizSound);
        }

        private void EnsureInit()
        {
            if (mascotController == null)
            {
                mascotController = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
            }
            if (audioSource == null)
            {
                audioSource = GetComponentInChildren<AudioSource>(true);
                if (audioSource == null) audioSource = FindFirstObjectByType<AudioSource>();
            }
        }

        public void OpenSectionA()
        {
            EnsureInit();
            gameObject.SetActive(true);
            if (mainLessonScreen != null) mainLessonScreen.SetActive(true);
            if (miniCheckPanel != null) miniCheckPanel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(false);

            currentVowelIndex = 0;
            canClick = true;

            if (gameObject.activeInHierarchy)
            {
                StopAllCoroutines();
                StartCoroutine(ShowInstructionRoutine());
            }
        }

        private IEnumerator ShowInstructionRoutine()
        {
            canClick = false;
            EnsureInit();

            if (instructionPanel != null) instructionPanel.SetActive(true);
            if (mascotController != null)
            {
                mascotController.ShowMascot();
                mascotController.PlayHiAnimation();
            }

            if (instructionText != null)
                instructionText.text = "Let's learn short and long vowels together!";

            if (instructionClip != null && audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
            {
                audioSource.Stop();
                audioSource.PlayOneShot(instructionClip);
                yield return new WaitForSeconds(instructionClip.length + 0.3f);
            }
            else
            {
                yield return new WaitForSeconds(2.5f);
            }

            if (instructionPanel != null) instructionPanel.SetActive(false);
            if (mascotController != null) mascotController.HideMascot();
            canClick = true;
            DisplayCurrentVowelPair();
        }

        private IEnumerator AutoPlayVowelSounds(U2_VowelPairData_Phonics_Junior pair)
        {
            if (pair == null || audioSource == null) yield break;

            // 1. Highlight Short Card & Play Short Vowel Audio
            if (pair.shortSoundAudio != null)
            {
                if (shortCardButton != null && shortCardButton.gameObject.activeInHierarchy)
                {
                    var anim = shortCardButton.GetComponent<UIButtonAnimation_Phonics_Junior>();
                    if (anim != null) anim.PlayTapAnimation();
                }

                audioSource.Stop();
                audioSource.PlayOneShot(pair.shortSoundAudio);
                yield return new WaitForSeconds(pair.shortSoundAudio.length + 0.2f);
            }

            // 2. Highlight Long Card & Play Long Vowel Audio
            if (pair.longSoundAudio != null)
            {
                if (longCardButton != null && longCardButton.gameObject.activeInHierarchy)
                {
                    var anim = longCardButton.GetComponent<UIButtonAnimation_Phonics_Junior>();
                    if (anim != null) anim.PlayTapAnimation();
                }

                audioSource.Stop();
                audioSource.PlayOneShot(pair.longSoundAudio);
            }
        }

        private void DisplayCurrentVowelPair()
    {
        if (vowelPairs == null || vowelPairs.Length == 0) return;

        U2_VowelPairData_Phonics_Junior pair = vowelPairs[currentVowelIndex];

        // Highlight Active Vowel in "A E I O U" Header using Rich Text
        if (vowelTitleText != null)
        {
            string[] allVowels = { "A", "E", "I", "O", "U" };
            string formattedHeader = "";

            for (int i = 0; i < allVowels.Length; i++)
            {
                if (i == currentVowelIndex)
                {
                    // Active vowel: Gold/Yellow (#FFD700), Bold, 125% Size
                    formattedHeader += $"<color=#FFD700><b><size=125%>{allVowels[i]}</size></b></color>  ";
                }
                else
                {
                    // Inactive vowels: Soft White
                    formattedHeader += $"<color=#E0F7FA>{allVowels[i]}</color>  ";
                }
            }
            vowelTitleText.text = formattedHeader.TrimEnd();
        }

        // Short Vowel (Breve ˘)
        if (shortSymbolText != null) shortSymbolText.text = $"{pair.vowelLetter} {pair.shortSymbol}";
        if (shortWordText != null) shortWordText.text = pair.shortWord;
        if (shortImageDisplay != null) shortImageDisplay.sprite = pair.shortImage;

        // Long Vowel (Macron ¯)
        if (longSymbolText != null) longSymbolText.text = $"{pair.vowelLetter} {pair.longSymbol}";
        if (longWordText != null) longWordText.text = pair.longWord;
        if (longImageDisplay != null) longImageDisplay.sprite = pair.longImage;

        if (prevVowelButton != null) prevVowelButton.interactable = currentVowelIndex > 0;
        StopCoroutine(nameof(AutoPlayVowelSounds));
        StartCoroutine(AutoPlayVowelSounds(pair));
    }

         public void PlayShortSound()
     {
        if (!canClick || vowelPairs == null) return;

        // Highlight / Animate the Short Card on tap
        if (shortCardButton != null)
        {
            var anim = shortCardButton.GetComponent<UIButtonAnimation_Phonics_Junior>();
            if (anim != null) anim.PlayTapAnimation();
        }

        U2_VowelPairData_Phonics_Junior pair = vowelPairs[currentVowelIndex];
        if (pair != null && pair.shortSoundAudio != null && audioSource != null && audioSource.enabled)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(pair.shortSoundAudio);
        }
      }

         public void PlayLongSound()
         {
        if (!canClick || vowelPairs == null) return;

        // Highlight / Animate the Long Card on tap
        if (longCardButton != null)
        {
            var anim = longCardButton.GetComponent<UIButtonAnimation_Phonics_Junior>();
            if (anim != null) anim.PlayTapAnimation();
        }

        U2_VowelPairData_Phonics_Junior pair = vowelPairs[currentVowelIndex];
        if (pair != null && pair.longSoundAudio != null && audioSource != null && audioSource.enabled)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(pair.longSoundAudio);
        }
         }


        public void NextVowel()
        {
            if (!canClick) return;

            if (currentVowelIndex < vowelPairs.Length - 1)
            {
                currentVowelIndex++;
                DisplayCurrentVowelPair();
            }
            else
            {
                // Finish exploration -> Start Mini-Check Quiz
                StartMiniCheck();
            }
        }

        public void PreviousVowel()
        {
            if (!canClick) return;

            if (currentVowelIndex > 0)
            {
                currentVowelIndex--;
                DisplayCurrentVowelPair();
            }
        }

        // ------------------ Mini-Check (Quiz) ------------------

        public void StartMiniCheck()
        {
            if (mainLessonScreen != null) mainLessonScreen.SetActive(false);
            if (miniCheckPanel != null) miniCheckPanel.SetActive(true);

            quizQuestionCount = 0;
            SetupMiniCheckQuestion();
        }

         private void SetupMiniCheckQuestion()
    {
        if (quizQuestionCount >= vowelPairs.Length)
        {
            CompleteSection();
            return;
        }

        ResetQuizButtonColors();

        U2_VowelPairData_Phonics_Junior currentPair = vowelPairs[quizQuestionCount];

        if (quizVowelText != null && currentPair != null)
        {
            string letterStr = !string.IsNullOrEmpty(currentPair.vowelLetter) ? currentPair.vowelLetter.ToUpper() : "";
            quizVowelText.text = $"Vowel: {letterStr}";
        }
        // 1. Populate Left Option Card (Short Vowel Option: ă)
        if (quizShortSymbolText != null) quizShortSymbolText.text = currentPair.shortSymbol;
        if (quizShortWordText != null) quizShortWordText.text = currentPair.shortWord;

        // 2. Populate Right Option Card (Long Vowel Option: ā)
        if (quizLongSymbolText != null) quizLongSymbolText.text = currentPair.longSymbol;
        if (quizLongWordText != null) quizLongWordText.text = currentPair.longWord;

        // 3. Randomly pick whether this question tests Short or Long sound
        isTargetShort = Random.value > 0.5f;

        // 4. Show the SINGLE matching image in the central question box
        if (quizTargetImageDisplay != null)
        {
            quizTargetImageDisplay.sprite = isTargetShort ? currentPair.shortImage : currentPair.longImage;
        }

        // 5. Play the matching audio clip
        ReplayQuizSound();
    }
          public void ReplayQuizSound()
    {
        if (vowelPairs == null || quizQuestionCount >= vowelPairs.Length) return;
        U2_VowelPairData_Phonics_Junior currentPair = vowelPairs[quizQuestionCount];
        if (audioSource != null && audioSource.enabled)
        {
            audioSource.Stop();
            // Use dedicated quiz audio clips so the answer ("Short" or "Long") isn't spoken aloud
            AudioClip quizClip = isTargetShort ? currentPair.shortQuizAudio : currentPair.longQuizAudio;

            // Fallback to standard sound audio if quiz audio clip is not assigned
            if (quizClip == null)
            {
                quizClip = isTargetShort ? currentPair.shortSoundAudio : currentPair.longSoundAudio;
            }

            if (quizClip != null) audioSource.PlayOneShot(quizClip);
        }
    }
       
   
    public void CheckMiniCheckAnswer(bool choseShort)
    {
        Button clickedButton = choseShort ? breveOptionButton : macronOptionButton;

        if (choseShort == isTargetShort)
        {
            // Correct -> Light Green feedback
            SetButtonColor(clickedButton, new Color(0.55f, 0.95f, 0.55f));

            if (audioSource != null && correctClip != null && audioSource.enabled)
            {
                audioSource.PlayOneShot(correctClip);
            }

            if (mascotController != null)
            {
                mascotController.ShowMascot();
                mascotController.PlayHiAnimation();
            }

            quizQuestionCount++;
            Invoke(nameof(SetupMiniCheckQuestion), 1.2f);
        }
        else
        {
            // Wrong -> Light Red feedback
            SetButtonColor(clickedButton, new Color(1f, 0.55f, 0.55f));

            if (audioSource != null && wrongClip != null && audioSource.enabled)
            {
                audioSource.PlayOneShot(wrongClip);
            }

            if (mascotController != null)
            {
                mascotController.ShowMascot();
            }

            // Reset color back to white after 0.8s so user can try again
            Invoke(nameof(ResetQuizButtonColors), 0.8f);
        }
    }


        
    
    private void SetButtonColor(Button btn, Color targetColor)
    {
        if (btn == null) return;

        // 1. Update Unity UI Button ColorBlock transition
        ColorBlock cb = btn.colors;
        cb.normalColor = targetColor;
        cb.highlightedColor = targetColor;
        cb.pressedColor = targetColor;
        cb.selectedColor = targetColor;
        btn.colors = cb;

        // 2. Find and tint ALL Image components on the card and its child objects
        Image[] cardImages = btn.GetComponentsInChildren<Image>(true);
        foreach (Image img in cardImages)
        {
            img.color = targetColor;
        }
    }

    private void ResetQuizButtonColors()
    {
        SetButtonColor(breveOptionButton, Color.white);
        SetButtonColor(macronOptionButton, Color.white);
    }

        // ------------------ Completion & Transitions ------------------

        private void CompleteSection()
        {
            if (miniCheckPanel != null) miniCheckPanel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(true);

            if (mascotController != null)
            {
                mascotController.ShowMascot();
                mascotController.PlayHiAnimation();
            }
        }

        public void CloseSection()
        {
            if (miniCheckPanel != null) miniCheckPanel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(false);
            if (mainLessonScreen != null) mainLessonScreen.SetActive(true);
            gameObject.SetActive(false);
        }

        public void CloseSectionA() => CloseSection();

        public void OnCompletionNextButtonClicked()
        {
            CloseSection();
            Unit_Selection_Panel_Phonics_Junior unitPanel =  FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>();
            if (unitPanel != null)
            {
                unitPanel.OpenUnit2SectionB();
            }
        }

      
    }