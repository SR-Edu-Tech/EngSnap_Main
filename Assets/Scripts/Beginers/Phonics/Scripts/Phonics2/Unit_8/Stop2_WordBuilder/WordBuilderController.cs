using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit8
{
    public class WordBuilderController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit8";
        [SerializeField] private string topicName = "WordBuilder";

        [Header("Data Asset")]
        [SerializeField] private WordBuilderData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Dialogue / Subtitle UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Swap Machine UI (3 Slots)")]
        [SerializeField] private GameObject swapMachinePanel;
        [SerializeField] private SwapMachineSlot frontSlot;
        [SerializeField] private SwapMachineSlot middleSlot;
        [SerializeField] private SwapMachineSlot endSlot;
        [SerializeField] private Button blendConfirmButton;
        [SerializeField] private TMP_Text currentWordDisplayTMP;

        [Header("Word Reveal / Picture")]
        [SerializeField] private GameObject picturePanel;
        [SerializeField] private Image wordPictureImage;
        [SerializeField] private TMP_Text wordPictureLabel;

        [Header("Silly Monster UI (Nonsense Words)")]
        [SerializeField] private GameObject sillyMonsterPanel;
        [SerializeField] private Image sillyMonsterImage;
        [SerializeField] private TMP_Text sillyMonsterText;

        [Header("Rhyming Family Wall UI")]
        [SerializeField] private GameObject familyWallPanel;
        [SerializeField] private Button toggleFamilyWallButton;
        [SerializeField] private TMP_Text wallWordCountTMP;
        [SerializeField] private Transform wallCardsContainer;
        [SerializeField] private GameObject wallCardPrefab;
        [SerializeField] private Button[] wallWordButtons;
        [SerializeField] private TMP_Text[] wallWordTexts;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;

        [Header("Rewards & Navigation")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentStepIndex = 0;
        private int totalSteps = 12;
        private bool isTransitioning = false;
        private bool hasInitialized = false;
        private SwapStepItem currentStep;
        private HashSet<string> builtWords = new HashSet<string>();

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();

            if (frontSlot != null) frontSlot.OnLetterChanged += (letter) => OnSlotLetterChanged();
            if (middleSlot != null) middleSlot.OnLetterChanged += (letter) => OnSlotLetterChanged();
            if (endSlot != null) endSlot.OnLetterChanged += (letter) => OnSlotLetterChanged();
        }

        private void Start()
        {
            SetupButtonListeners();
            hasInitialized = true;
            StartActivity();
        }

        private void OnEnable()
        {
            if (hasInitialized)
            {
                StartActivity();
            }
        }

        private void OnDisable()
        {
            StopAllAudio();
            StopAllCoroutines();
            DeactivateMascots();
        }

        private void StopAllAudio()
        {
            if (voiceAudioSource != null && voiceAudioSource.isPlaying) voiceAudioSource.Stop();
            if (sfxAudioSource != null && sfxAudioSource.isPlaying) sfxAudioSource.Stop();
        }

        private void EnsureAudioSources()
        {
            if (voiceAudioSource == null) voiceAudioSource = gameObject.AddComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.spatialBlend = 0f;
            sfxAudioSource.spatialBlend = 0f;
        }

        private void EnsureDataAssigned()
        {
            if (activityData == null)
            {
                activityData = Resources.Load<WordBuilderData>("Phonics2/Unit8/WordBuilderData_Unit8");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<WordBuilderData>();
                activityData.PopulateDefaultData();
            }
        }

        private void SetupButtonListeners()
        {
            if (blendConfirmButton != null)
            {
                blendConfirmButton.onClick.RemoveAllListeners();
                blendConfirmButton.onClick.AddListener(OnConfirmBlendClicked);
            }

            if (toggleFamilyWallButton != null)
            {
                toggleFamilyWallButton.onClick.RemoveAllListeners();
                toggleFamilyWallButton.onClick.AddListener(ToggleFamilyWall);
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

            // Wall word tap listeners
            if (wallWordButtons != null)
            {
                for (int i = 0; i < wallWordButtons.Length; i++)
                {
                    int index = i;
                    if (wallWordButtons[i] != null)
                    {
                        wallWordButtons[i].onClick.RemoveAllListeners();
                        wallWordButtons[i].onClick.AddListener(() => OnWallWordClicked(index));
                    }
                }
            }
        }

        public void ResetLevel()
        {
            StartActivity();
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            currentStepIndex = 0;
            isTransitioning = false;
            builtWords.Clear();

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (picturePanel != null) picturePanel.SetActive(false);
            if (sillyMonsterPanel != null) sillyMonsterPanel.SetActive(false);

            if (leoMascotObject != null) leoMascotObject.SetActive(true);

            StartCoroutine(StartActivityIntroRoutine());
        }

        private IEnumerator StartActivityIntroRoutine()
        {
            isTransitioning = true;

            // 1. Show Family Wall first during intro
            SetFamilyWallVisible(true);
            SetDialogue("Look at our Word Family Wall! As we change letters, we will build new word families!");

            if (activityData != null && activityData.familyWallRevealClip != null)
            {
                yield return PlayVoiceClip(activityData.familyWallRevealClip);
            }
            else
            {
                yield return new WaitForSeconds(2.8f);
            }

            yield return new WaitForSeconds(1.0f);

            // 2. Transition to Swap Machine slots
            SetFamilyWallVisible(false);
            SetDialogue("The machine is back — and now ALL THREE letters can turn!");

            if (activityData != null && activityData.leoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.leoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(1.8f);
            }

            isTransitioning = false;
            LoadStep(0);
        }

        private void LoadStep(int index)
        {
            if (activityData == null || activityData.swapSteps == null || index >= activityData.swapSteps.Length)
            {
                CompleteStop2();
                return;
            }

            StartCoroutine(LoadStepRoutine(index));
        }

        private IEnumerator LoadStepRoutine(int index)
        {
            isTransitioning = true;
            currentStepIndex = index;
            currentStep = activityData.swapSteps[index];

            if (picturePanel != null) picturePanel.SetActive(false);
            if (sillyMonsterPanel != null) sillyMonsterPanel.SetActive(false);

            string start = currentStep.startingWord;
            string startF = start.Length > 0 ? start[0].ToString() : "c";
            string startM = start.Length > 1 ? start[1].ToString() : "a";
            string startE = start.Length > 2 ? start[2].ToString() : "t";

            string[] frontLetters = GetFrontOptions(startF, currentStep.targetLetter);
            string[] middleLetters = GetMiddleOptions(startM, currentStep.targetLetter);
            string[] endLetters = GetEndOptions(startE, currentStep.targetLetter);

            // Setup slots with active position unlocked, others locked
            if (frontSlot != null)
            {
                frontSlot.SetupSlot(frontLetters, startF, currentStep.position != SwapSlotPosition.Front);
            }
            if (middleSlot != null)
            {
                middleSlot.SetupSlot(middleLetters, startM, currentStep.position != SwapSlotPosition.Middle);
            }
            if (endSlot != null)
            {
                endSlot.SetupSlot(endLetters, startE, currentStep.position != SwapSlotPosition.End);
            }

            UpdateCurrentWordDisplay();

            string posName = (currentStep.position == SwapSlotPosition.Front) ? "first" : (currentStep.position == SwapSlotPosition.Middle) ? "middle" : "last";
            SetDialogue($"Change the {posName} letter to '{currentStep.targetLetter}'. What does it say now? Spin and read!");

            if (currentStep.promptVoiceClip != null)
            {
                PlayVoiceClipNonBlocking(currentStep.promptVoiceClip);
            }

            UpdateProgressUI((float)index / totalSteps);
            isTransitioning = false;
            yield return null;
        }

        private void OnSlotLetterChanged()
        {
            PlaySFX(activityData != null ? activityData.slotSpinSfx : null);
            UpdateCurrentWordDisplay();
        }

        private void UpdateCurrentWordDisplay()
        {
            string f = (frontSlot != null) ? frontSlot.CurrentLetter : "c";
            string m = (middleSlot != null) ? middleSlot.CurrentLetter : "a";
            string e = (endSlot != null) ? endSlot.CurrentLetter : "t";
            string full = $"{f}{m}{e}".ToUpper();

            if (currentWordDisplayTMP != null)
            {
                currentWordDisplayTMP.text = full;
            }
        }

        private void OnConfirmBlendClicked()
        {
            if (isTransitioning || currentStep == null) return;
            StartCoroutine(CheckWordAttemptRoutine());
        }

        private IEnumerator CheckWordAttemptRoutine()
        {
            isTransitioning = true;
            string f = (frontSlot != null) ? frontSlot.CurrentLetter : "";
            string m = (middleSlot != null) ? middleSlot.CurrentLetter : "";
            string e = (endSlot != null) ? endSlot.CurrentLetter : "";
            string currentWord = $"{f}{m}{e}".ToLower();

            bool isTargetWord = string.Equals(currentWord, currentStep.resultingWord, System.StringComparison.OrdinalIgnoreCase);

            if (isTargetWord)
            {
                PlaySFX(activityData != null ? activityData.slotLockSfx : null);
                PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
                TriggerWiggleStarMeter();

                builtWords.Add(currentWord);
                UpdateWallCounter();

                if (picturePanel != null) picturePanel.SetActive(true);
                if (wordPictureLabel != null) wordPictureLabel.text = currentWord.ToUpper();
                if (wordPictureImage != null)
                {
                    if (currentStep.wordPictureSprite != null)
                    {
                        wordPictureImage.sprite = currentStep.wordPictureSprite;
                        wordPictureImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        wordPictureImage.gameObject.SetActive(false);
                    }
                }

                SetDialogue($"{currentWord.ToUpper()}! One letter changed and it is a whole new word!");
                if (currentStep.wordAudioClip != null)
                {
                    yield return PlayVoiceClip(currentStep.wordAudioClip);
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }

                yield return new WaitForSeconds(0.8f);

                currentStepIndex++;
                isTransitioning = false;
                LoadStep(currentStepIndex);
            }
            else
            {
                // Check if nonsense word or just wrong target
                bool isNonsense = !currentStep.isRealWord || (!IsKnownRealWord(currentWord));

                if (isNonsense)
                {
                    PlaySFX(activityData != null ? activityData.nonsenseRaspberrySfx : null);
                    if (sillyMonsterPanel != null)
                    {
                        sillyMonsterPanel.SetActive(true);
                        if (sillyMonsterText != null) sillyMonsterText.text = $"'{currentWord.ToUpper()}' is a silly monster sound!";
                    }

                    SetDialogue("Bppppt! Not a word — just a silly sound!");
                    if (activityData != null && activityData.nonsenseMonsterVoiceClip != null)
                    {
                        yield return PlayVoiceClip(activityData.nonsenseMonsterVoiceClip);
                    }
                    else
                    {
                        yield return new WaitForSeconds(1.2f);
                    }

                    if (sillyMonsterPanel != null) sillyMonsterPanel.SetActive(false);
                }
                else
                {
                    PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                    SetDialogue($"Try spinning to '{currentStep.targetLetter}' to make '{currentStep.resultingWord.ToUpper()}'!");
                    yield return new WaitForSeconds(1.0f);
                }

                isTransitioning = false;
            }
        }

        private string[] GetFrontOptions(string startLetter, string targetLetter)
        {
            List<string> list = new List<string> { "c", "b", "h", "m", "r" };
            if (!string.IsNullOrEmpty(startLetter) && !list.Contains(startLetter)) list.Insert(0, startLetter);
            if (!string.IsNullOrEmpty(targetLetter) && !list.Contains(targetLetter)) list.Add(targetLetter);
            return list.ToArray();
        }

        private string[] GetMiddleOptions(string startLetter, string targetLetter)
        {
            List<string> list = new List<string> { "a", "o", "e", "i", "u" };
            if (!string.IsNullOrEmpty(startLetter) && !list.Contains(startLetter)) list.Insert(0, startLetter);
            if (!string.IsNullOrEmpty(targetLetter) && !list.Contains(targetLetter)) list.Add(targetLetter);
            return list.ToArray();
        }

        private string[] GetEndOptions(string startLetter, string targetLetter)
        {
            List<string> list = new List<string> { "t", "p", "b", "g", "n", "d" };
            if (!string.IsNullOrEmpty(startLetter) && !list.Contains(startLetter)) list.Insert(0, startLetter);
            if (!string.IsNullOrEmpty(targetLetter) && !list.Contains(targetLetter)) list.Add(targetLetter);
            return list.ToArray();
        }

        private bool IsKnownRealWord(string w)
        {
            string[] real = new string[] { "cat", "bat", "hat", "mat", "rat", "can", "cap", "cab", "bed", "beg", "bet", "cot", "big", "bag", "pan", "pen", "pin", "pot" };
            return System.Array.Exists(real, x => x == w);
        }

        private void SetFamilyWallVisible(bool visible)
        {
            if (familyWallPanel != null)
            {
                familyWallPanel.SetActive(visible);
                if (visible)
                {
                    PopulateFamilyWallVisuals();
                }
            }

            if (swapMachinePanel != null)
            {
                swapMachinePanel.SetActive(!visible);
            }

            if (visible)
            {
                if (picturePanel != null) picturePanel.SetActive(false);
                if (sillyMonsterPanel != null) sillyMonsterPanel.SetActive(false);
            }
        }

        private void ToggleFamilyWall()
        {
            if (familyWallPanel == null) return;
            bool active = !familyWallPanel.activeSelf;
            SetFamilyWallVisible(active);
        }

        private void PopulateFamilyWallVisuals()
        {
            if (familyWallPanel == null) return;

            EnsureDataAssigned();

            // Find or create container for cards if needed
            Transform container = wallCardsContainer;
            if (container == null)
            {
                // Look for a child named "CardsContainer", "Content", "FamilyColumns", "Grid", or use familyWallPanel transform
                Transform found = familyWallPanel.transform.Find("CardsContainer") ??
                                 familyWallPanel.transform.Find("Content") ??
                                 familyWallPanel.transform.Find("FamilyColumns") ??
                                 familyWallPanel.transform.Find("Grid");
                container = found != null ? found : familyWallPanel.transform;
            }

            // Gather all words from family groups
            List<string> allWords = new List<string>();
            if (activityData != null && activityData.familyGroups != null)
            {
                for (int g = 0; g < activityData.familyGroups.Length; g++)
                {
                    if (activityData.familyGroups[g] != null && activityData.familyGroups[g].words != null)
                    {
                        for (int w = 0; w < activityData.familyGroups[g].words.Length; w++)
                        {
                            string wd = activityData.familyGroups[g].words[w];
                            if (!string.IsNullOrEmpty(wd) && !allWords.Contains(wd))
                            {
                                allWords.Add(wd);
                            }
                        }
                    }
                }
            }

            if (allWords.Count == 0)
            {
                allWords.AddRange(new string[] {
                    "cat", "bat", "hat", "mat", "rat",
                    "can", "fan", "man", "pan", "ran",
                    "cap", "map", "tap", "nap", "lap",
                    "bed", "red", "fed", "led", "wed",
                    "pen", "ten", "hen", "men", "den",
                    "net", "pet", "wet", "get", "let"
                });
            }

            // Update live counter
            if (wallWordCountTMP != null)
            {
                wallWordCountTMP.text = $"Words Built: {allWords.Count}";
            }

            // Check if existing text components are assigned or present in children
            List<TMP_Text> targetTexts = new List<TMP_Text>();
            List<Button> targetButtons = new List<Button>();

            if (wallWordTexts != null && wallWordTexts.Length > 0)
            {
                for (int i = 0; i < wallWordTexts.Length; i++)
                {
                    if (wallWordTexts[i] != null) targetTexts.Add(wallWordTexts[i]);
                }
            }

            if (wallWordButtons != null && wallWordButtons.Length > 0)
            {
                for (int i = 0; i < wallWordButtons.Length; i++)
                {
                    if (wallWordButtons[i] != null) targetButtons.Add(wallWordButtons[i]);
                }
            }

            // If no explicit array assigned, find existing child cards (ignoring headers/titles)
            if (targetTexts.Count == 0 && container != null)
            {
                TMP_Text[] allChildTexts = container.GetComponentsInChildren<TMP_Text>(true);
                Button[] allChildButtons = container.GetComponentsInChildren<Button>(true);

                for (int i = 0; i < allChildTexts.Length; i++)
                {
                    string objName = allChildTexts[i].gameObject.name.ToLower();
                    if (allChildTexts[i] == dialogueText || allChildTexts[i] == wallWordCountTMP || allChildTexts[i] == progressText) continue;
                    if (objName.Contains("title") || objName.Contains("header") || objName.Contains("count") || objName.Contains("dialogue")) continue;
                    targetTexts.Add(allChildTexts[i]);
                }

                for (int i = 0; i < allChildButtons.Length; i++)
                {
                    string objName = allChildButtons[i].gameObject.name.ToLower();
                    if (allChildButtons[i] == toggleFamilyWallButton || allChildButtons[i] == blendConfirmButton) continue;
                    if (objName.Contains("close") || objName.Contains("back") || objName.Contains("toggle")) continue;
                    targetButtons.Add(allChildButtons[i]);
                }
            }

            // If we have matching text elements, populate them directly
            if (targetTexts.Count >= allWords.Count / 2 && targetTexts.Count > 0)
            {
                for (int i = 0; i < targetTexts.Count; i++)
                {
                    if (i < allWords.Count)
                    {
                        string word = allWords[i];
                        targetTexts[i].gameObject.SetActive(true);
                        targetTexts[i].text = word.ToUpper();
                        targetTexts[i].color = Color.white;

                        if (i < targetButtons.Count && targetButtons[i] != null)
                        {
                            string capturedWord = word;
                            targetButtons[i].onClick.RemoveAllListeners();
                            targetButtons[i].onClick.AddListener(() => OnWallWordClicked(capturedWord));
                        }
                    }
                }
            }
            else if (container != null && wallCardPrefab != null)
            {
                // Instantiate cards from prefab if container has fewer children
                while (container.childCount < allWords.Count)
                {
                    Instantiate(wallCardPrefab, container);
                }

                TMP_Text[] instantiatedTexts = container.GetComponentsInChildren<TMP_Text>(true);
                Button[] instantiatedButtons = container.GetComponentsInChildren<Button>(true);

                for (int i = 0; i < allWords.Count && i < instantiatedTexts.Length; i++)
                {
                    string word = allWords[i];
                    instantiatedTexts[i].gameObject.SetActive(true);
                    instantiatedTexts[i].text = word.ToUpper();
                    instantiatedTexts[i].color = Color.white;

                    if (i < instantiatedButtons.Length && instantiatedButtons[i] != null)
                    {
                        string capturedWord = word;
                        instantiatedButtons[i].onClick.RemoveAllListeners();
                        instantiatedButtons[i].onClick.AddListener(() => OnWallWordClicked(capturedWord));
                    }
                }
            }
            else if (container != null && container.childCount == 0)
            {
                // Auto-create lightweight dynamic cards so words appear guaranteed
                CreateDynamicWallCards(container, allWords);
            }
        }

        private void CreateDynamicWallCards(Transform container, List<string> words)
        {
            // Ensure container has a layout group for neat layout
            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (grid == null && container.GetComponent<HorizontalLayoutGroup>() == null && container.GetComponent<VerticalLayoutGroup>() == null)
            {
                grid = container.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(140f, 55f);
                grid.spacing = new Vector2(15f, 15f);
                grid.childAlignment = TextAnchor.MiddleCenter;
            }

            for (int i = 0; i < words.Count; i++)
            {
                string word = words[i];

                GameObject cardObj = new GameObject($"Card_{word}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardObj.transform.SetParent(container, false);

                Image cardImg = cardObj.GetComponent<Image>();
                cardImg.color = new Color(0.18f, 0.28f, 0.45f, 0.95f);

                Button cardBtn = cardObj.GetComponent<Button>();
                string capturedWord = word;
                cardBtn.onClick.AddListener(() => OnWallWordClicked(capturedWord));

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObj.transform.SetParent(cardObj.transform, false);

                TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
                tmp.text = word.ToUpper();
                tmp.fontSize = 28;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;

                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
        }

        private void OnWallWordClicked(string word)
        {
            if (string.IsNullOrEmpty(word)) return;

            SetDialogue($"{word.ToUpper()}!");
            PlaySFX(activityData != null ? activityData.wallPopSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
        }

        private void OnWallWordClicked(int index)
        {
            if (wallWordTexts != null && index < wallWordTexts.Length && wallWordTexts[index] != null)
            {
                string word = wallWordTexts[index].text;
                OnWallWordClicked(word);
            }
        }

        private void UpdateWallCounter()
        {
            int count = Mathf.Max(builtWords.Count, currentStepIndex + 1);
            if (wallWordCountTMP != null)
            {
                wallWordCountTMP.text = $"Words Built: {count}";
            }
        }

        private void CompleteStop2()
        {
            StartCoroutine(CompleteStop2Sequence());
        }

        private IEnumerator CompleteStop2Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetFamilyWallVisible(true);

            SetDialogue("Your wall is getting big. Every one of those, you can read!");
            if (activityData != null && activityData.closingWallCountClip != null)
            {
                yield return PlayVoiceClip(activityData.closingWallCountClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            yield return new WaitForSeconds(1.0f);
            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
        }

        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();
            DeactivateMascots();

            if (nextPanel != null) nextPanel.SetActive(true);
            else if (unitContentPanel != null) unitContentPanel.SetActive(true);

            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
                if (unitContentPanel != null) unitContentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }

        private void SetDialogue(string message)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, message, dialogueCanvasGroup);
        }

        private void PlaySFX(AudioClip clip)
        {
            if (sfxAudioSource != null && clip != null)
            {
                sfxAudioSource.PlayOneShot(clip);
            }
        }

        private void PlayVoiceClipNonBlocking(AudioClip clip)
        {
            if (voiceAudioSource != null && clip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
            }
        }

        private IEnumerator PlayVoiceClip(AudioClip clip)
        {
            if (voiceAudioSource != null && clip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }

        private void UpdateProgressUI(float fillAmount)
        {
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillAmount;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillAmount * 100)}%";
        }

        private void TriggerWiggleStarMeter()
        {
            if (starMeterRect != null) StartCoroutine(PopScaleRoutine(starMeterRect, 1.15f, 0.3f));
        }

        private IEnumerator PopScaleRoutine(Transform target, float maxScaleMul, float duration)
        {
            if (target == null) yield break;
            Vector3 startScale = target.localScale;
            Vector3 maxScale = startScale * maxScaleMul;
            float half = duration * 0.5f;

            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(startScale, maxScale, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(maxScale, startScale, elapsed / half);
                yield return null;
            }

            target.localScale = startScale;
        }
    }
}
