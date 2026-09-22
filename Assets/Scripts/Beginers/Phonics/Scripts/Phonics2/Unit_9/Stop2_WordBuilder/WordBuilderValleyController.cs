using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;
using EngSnap.Phonics2.Unit8;

namespace EngSnap.Phonics2.Unit9
{
    public class WordBuilderValleyController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit9";
        [SerializeField] private string topicName = "WordBuilder";

        [Header("Data Asset")]
        [SerializeField] private WordBuilderValleyData activityData;

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

        [Header("Rhyming Family Wall UI")]
        [SerializeField] private GameObject familyWallPanel;
        [SerializeField] private Button toggleFamilyWallButton;
        [SerializeField] private TMP_Text wallWordCountTMP;
        [SerializeField] private Button[] wallWordButtons;
        [SerializeField] private TMP_Text[] wallWordTexts;

        [Header("Fluency Reading UI (Phase 2)")]
        [SerializeField] private GameObject fluencyPanel;
        [SerializeField] private TMP_Text fluencyPromptTMP;
        [SerializeField] private Button[] fluencyWordButtons = new Button[6];
        [SerializeField] private TMP_Text[] fluencyWordTexts = new TMP_Text[6];
        [SerializeField] private Button fluencyDoneButton;

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
        private int currentFluencyRowIndex = 0;
        private bool isFluencyPhase = false;
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
                activityData = Resources.Load<WordBuilderValleyData>("Phonics2/Unit9/WordBuilderValleyData_Unit9");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<WordBuilderValleyData>();
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

            if (fluencyDoneButton != null)
            {
                fluencyDoneButton.onClick.RemoveAllListeners();
                fluencyDoneButton.onClick.AddListener(OnFluencyRowCompleted);
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

            // Wall word listeners
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

            // Fluency word buttons
            for (int i = 0; i < fluencyWordButtons.Length; i++)
            {
                int index = i;
                if (fluencyWordButtons[i] != null)
                {
                    fluencyWordButtons[i].onClick.RemoveAllListeners();
                    fluencyWordButtons[i].onClick.AddListener(() => OnFluencyWordClicked(index));
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
            currentFluencyRowIndex = 0;
            isFluencyPhase = false;
            isTransitioning = false;
            builtWords.Clear();

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (picturePanel != null) picturePanel.SetActive(false);
            if (fluencyPanel != null) fluencyPanel.SetActive(false);

            if (leoMascotObject != null) leoMascotObject.SetActive(true);

            StartCoroutine(StartIntroRoutine());
        }

        private IEnumerator StartIntroRoutine()
        {
            isTransitioning = true;

            // 1. Show Family Wall first during intro
            SetFamilyWallVisible(true);
            SetDialogue("More words for your wall! Let's check out our word families.");

            if (activityData != null && activityData.leoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.leoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(2.5f);
            }

            yield return new WaitForSeconds(1.0f);

            // 2. Transition to Swap Machine slots
            SetFamilyWallVisible(false);
            SetDialogue("Turn the dial and build new words!");
            yield return new WaitForSeconds(1.0f);

            isTransitioning = false;
            LoadStep(0);
        }

        private void LoadStep(int index)
        {
            if (activityData == null || activityData.swapSteps == null || index >= activityData.swapSteps.Length)
            {
                StartFluencyPhase();
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

            string start = currentStep.startingWord;
            string startF = start.Length > 0 ? start[0].ToString() : "p";
            string startM = start.Length > 1 ? start[1].ToString() : "i";
            string startE = start.Length > 2 ? start[2].ToString() : "g";

            string[] frontLetters = GetFrontOptions(startF, currentStep.targetLetter);
            string[] middleLetters = GetMiddleOptions(startM, currentStep.targetLetter);
            string[] endLetters = GetEndOptions(startE, currentStep.targetLetter);

            if (frontSlot != null) frontSlot.SetupSlot(frontLetters, startF, currentStep.position != SwapSlotPosition.Front);
            if (middleSlot != null) middleSlot.SetupSlot(middleLetters, startM, currentStep.position != SwapSlotPosition.Middle);
            if (endSlot != null) endSlot.SetupSlot(endLetters, startE, currentStep.position != SwapSlotPosition.End);

            UpdateCurrentWordDisplay();

            string posName = (currentStep.position == SwapSlotPosition.Front) ? "first" : (currentStep.position == SwapSlotPosition.Middle) ? "middle" : "last";
            SetDialogue($"Change the {posName} letter to '{currentStep.targetLetter}'. What does it say now?");

            if (currentStep.promptVoiceClip != null)
            {
                PlayVoiceClipNonBlocking(currentStep.promptVoiceClip);
            }

            UpdateProgressUI((float)index / 14f);
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
            string f = (frontSlot != null) ? frontSlot.CurrentLetter : "p";
            string m = (middleSlot != null) ? middleSlot.CurrentLetter : "i";
            string e = (endSlot != null) ? endSlot.CurrentLetter : "g";
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

            bool isTarget = string.Equals(currentWord, currentStep.resultingWord, System.StringComparison.OrdinalIgnoreCase);

            if (isTarget)
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

                SetDialogue($"{currentWord.ToUpper()}! Great word!");

                if (currentStep.wordAudioClip != null)
                {
                    yield return PlayVoiceClip(currentStep.wordAudioClip);
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }

                if (currentStepIndex == 2)
                {
                    SetDialogue("pig, big, dig, wig — they all end in 'ig'. A whole family!");
                    if (activityData != null && activityData.familyRevealClip != null)
                    {
                        yield return PlayVoiceClip(activityData.familyRevealClip);
                    }
                }

                currentStepIndex++;
                isTransitioning = false;
                LoadStep(currentStepIndex);
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue($"Try spinning to '{currentStep.targetLetter}' to make '{currentStep.resultingWord.ToUpper()}'!");
                yield return new WaitForSeconds(1.0f);
                isTransitioning = false;
            }
        }

        #region Phase 2: Fluency Reading (Read the Wall)

        private void StartFluencyPhase()
        {
            isFluencyPhase = true;
            if (swapMachinePanel != null) swapMachinePanel.SetActive(false);
            if (picturePanel != null) picturePanel.SetActive(false);
            if (fluencyPanel != null) fluencyPanel.SetActive(true);

            currentFluencyRowIndex = 0;
            LoadFluencyRow(0);
        }

        private void LoadFluencyRow(int rowIndex)
        {
            if (activityData == null || activityData.fluencyRows == null || rowIndex >= activityData.fluencyRows.Length)
            {
                CompleteStop2();
                return;
            }

            currentFluencyRowIndex = rowIndex;
            var row = activityData.fluencyRows[rowIndex];

            if (fluencyPromptTMP != null) fluencyPromptTMP.text = "Read this row at your own speed:";
            SetDialogue("Read this row to me, at your own speed. I will wait.");

            if (activityData != null && activityData.fluencyRowIntroClip != null && rowIndex == 0)
            {
                PlayVoiceClipNonBlocking(activityData.fluencyRowIntroClip);
            }

            for (int i = 0; i < fluencyWordButtons.Length; i++)
            {
                if (fluencyWordButtons[i] != null)
                {
                    bool active = row.words != null && i < row.words.Length;
                    fluencyWordButtons[i].gameObject.SetActive(active);

                    if (active && fluencyWordTexts != null && i < fluencyWordTexts.Length && fluencyWordTexts[i] != null)
                    {
                        fluencyWordTexts[i].text = row.words[i];
                    }
                }
            }

            UpdateProgressUI(0.85f + ((float)rowIndex / 2f) * 0.15f);
        }

        private void OnFluencyWordClicked(int index)
        {
            if (fluencyWordTexts != null && index < fluencyWordTexts.Length && fluencyWordTexts[index] != null)
            {
                string word = fluencyWordTexts[index].text;
                SetDialogue(word.ToUpper());
                PlaySFX(activityData != null ? activityData.wallPopSfx : null);
                if (fluencyWordButtons != null && index < fluencyWordButtons.Length && fluencyWordButtons[index] != null)
                {
                    StartCoroutine(PopScaleRoutine(fluencyWordButtons[index].transform, 1.2f, 0.25f));
                }
            }
        }

        private void OnFluencyRowCompleted()
        {
            if (isTransitioning) return;
            StartCoroutine(HandleFluencyRowSuccessRoutine());
        }

        private IEnumerator HandleFluencyRowSuccessRoutine()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            SetDialogue("Smooth and quick. That is called reading fluently!");
            if (activityData != null && activityData.fluencyPraiseClip != null)
            {
                yield return PlayVoiceClip(activityData.fluencyPraiseClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            currentFluencyRowIndex++;
            isTransitioning = false;
            LoadFluencyRow(currentFluencyRowIndex);
        }

        #endregion

        private string[] GetFrontOptions(string startLetter, string targetLetter)
        {
            List<string> list = new List<string> { "p", "f", "s", "d", "t", "r", "h", "b", "c", "m" };
            if (!string.IsNullOrEmpty(startLetter) && !list.Contains(startLetter)) list.Insert(0, startLetter);
            if (!string.IsNullOrEmpty(targetLetter) && !list.Contains(targetLetter)) list.Add(targetLetter);
            return list.ToArray();
        }

        private string[] GetMiddleOptions(string startLetter, string targetLetter)
        {
            List<string> list = new List<string> { "i", "o", "u", "a", "e" };
            if (!string.IsNullOrEmpty(startLetter) && !list.Contains(startLetter)) list.Insert(0, startLetter);
            if (!string.IsNullOrEmpty(targetLetter) && !list.Contains(targetLetter)) list.Add(targetLetter);
            return list.ToArray();
        }

        private string[] GetEndOptions(string startLetter, string targetLetter)
        {
            List<string> list = new List<string> { "g", "t", "x", "p", "d", "n", "b" };
            if (!string.IsNullOrEmpty(startLetter) && !list.Contains(startLetter)) list.Insert(0, startLetter);
            if (!string.IsNullOrEmpty(targetLetter) && !list.Contains(targetLetter)) list.Add(targetLetter);
            return list.ToArray();
        }

        private void SetFamilyWallVisible(bool visible)
        {
            if (familyWallPanel != null)
            {
                familyWallPanel.SetActive(visible);
                if (visible) PopulateFamilyWallVisuals();
            }

            if (swapMachinePanel != null) swapMachinePanel.SetActive(!visible && !isFluencyPhase);
            if (fluencyPanel != null) fluencyPanel.SetActive(!visible && isFluencyPhase);

            if (visible)
            {
                if (picturePanel != null) picturePanel.SetActive(false);
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

            Transform container = familyWallPanel.transform.Find("CardsContainer") ??
                                 familyWallPanel.transform.Find("Content") ??
                                 familyWallPanel.transform.Find("FamilyColumns") ??
                                 familyWallPanel.transform.Find("Grid") ??
                                 familyWallPanel.transform;

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
                    "pig", "big", "fig", "dig", "wig",
                    "sit", "pit", "fit", "hit", "kit",
                    "pot", "cot", "hot", "lot", "dot",
                    "dog", "log", "fog", "jog", "bog",
                    "bug", "rug", "mug", "jug", "tug",
                    "cup", "pup", "son", "ton", "fox"
                });
            }

            if (wallWordCountTMP != null)
            {
                wallWordCountTMP.text = $"Words You Can Read: {allWords.Count}";
            }

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
            else if (container != null && container.childCount == 0)
            {
                CreateDynamicWallCards(container, allWords);
            }
        }

        private void CreateDynamicWallCards(Transform container, List<string> words)
        {
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
            int count = Mathf.Max(builtWords.Count + 18, currentStepIndex + 24);
            if (wallWordCountTMP != null)
            {
                wallWordCountTMP.text = $"Words You Can Read: {count}";
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

            SetDialogue("Over a hundred words! You are a master word builder!");
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
