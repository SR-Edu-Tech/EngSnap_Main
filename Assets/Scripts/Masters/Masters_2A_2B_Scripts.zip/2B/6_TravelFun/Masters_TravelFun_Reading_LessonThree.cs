using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



public class Masters_TravelFun_Reading_LessonThree : Masters_Lesson {

[System.Serializable]
public class JourneyPointData {
    public int orderIndex;
    public string travelPhrase;
    public string momentMeaning;
    public AudioClip pointAudio;
    public AudioClip slowPointAudio;
}

    [Header("R03 10 Sequential Journey Points")]
    [SerializeField]
    private JourneyPointData[] journeyPoints;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;

    [Header("Journey Map & Tray Containers")]
    [SerializeField]
    private Transform journeyMapArea;
    [SerializeField]
    private Transform phraseTrayArea;

    [Header("Audio Controls")]
    [SerializeField]
    private AudioClip recapAudio;
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Colors & Styling")]
    [SerializeField]
    private Color defaultTileColor = new Color(0.14f, 0.38f, 0.58f, 1f);
    [SerializeField]
    private Color selectedTileColor = new Color(0.95f, 0.75f, 0.15f, 1f);
    [SerializeField]
    private Color lockedSlotColor = new Color(0.14f, 0.65f, 0.28f, 1f);
    [SerializeField]
    private Color emptySlotColor = new Color(0.12f, 0.22f, 0.35f, 0.95f);
    [SerializeField]
    private Color wrongColor = new Color(0.80f, 0.20f, 0.20f, 1f);

    private Button[] slotButtons;
    private Image[] slotImages;
    private TextMeshProUGUI[] slotHeaderTexts;
    private TextMeshProUGUI[] slotMeaningTexts;

    private Button[] trayButtons;
    private Image[] trayImages;
    private TextMeshProUGUI[] trayTexts;

    private int selectedTrayIndex = -1;
    private int lockedCount = 0;
    private int mistakeCount = 0;
    private bool isProcessingInput = false;
    private bool[] slotFilled;
    private int[] trayToPointMapping;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;

        PurgeLegacyChildren();
        BindUIReferences();

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        BindUIReferences();

        StartCoroutine(StartWithIntroRoutine());
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "Sentence tmt", "Rule", "PhraseCardsGrid", "KeepButton", "FixButton", "OptionButton_01",
            "OptionButton_02", "OptionButton_03", "OptionButton_04", "Darken", "QuestionStatements",
            "WordsOne", "FillInTheBlanks", "Statements", "Words"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }
    }

    private void BindUIReferences() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) headerTMP.text = "READING BRANCH (Travel Fun)";

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("LessonTitle") ?? transform.Find("Title");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) titleTMP.text = "R03 Plan the Journey — Put It in Order";

        if (progressTMP == null) {
            Transform pTrans = transform.Find("HeaderContainer/Progress") ?? transform.Find("ExpressionCountTMP") ?? transform.Find("Progress");
            if (pTrans != null) progressTMP = pTrans.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP != null) progressTMP.text = "0/10";

        if (journeyMapArea == null) {
            journeyMapArea = transform.Find("JourneyMapArea");
        }

        if (phraseTrayArea == null) {
            phraseTrayArea = transform.Find("PhraseTrayArea");
        }

        // Bind 10 Station Slots
        if (journeyMapArea != null) {
            int slotCount = journeyMapArea.childCount > 0 ? journeyMapArea.childCount : 10;
            slotButtons = new Button[slotCount];
            slotImages = new Image[slotCount];
            slotHeaderTexts = new TextMeshProUGUI[slotCount];
            slotMeaningTexts = new TextMeshProUGUI[slotCount];

            for (int i = 0; i < slotCount; i++) {
                string slotName = $"StationSlot_{i + 1:D2}";
                Transform sTrans = journeyMapArea.Find(slotName) ?? (i < journeyMapArea.childCount ? journeyMapArea.GetChild(i) : null);
                if (sTrans != null) {
                    slotButtons[i] = sTrans.GetComponent<Button>();
                    slotImages[i] = sTrans.GetComponent<Image>();

                    Transform hTr = sTrans.Find("HeaderText");
                    if (hTr != null) slotHeaderTexts[i] = hTr.GetComponent<TextMeshProUGUI>();

                    Transform mTr = sTrans.Find("MeaningText");
                    if (mTr != null) {
                        slotMeaningTexts[i] = mTr.GetComponent<TextMeshProUGUI>();
                        if (slotMeaningTexts[i] != null) {
                            slotMeaningTexts[i].enableAutoSizing = false;
                        }
                    }
                }
            }
        }

        // Bind 10 Tray Tiles
        if (phraseTrayArea != null) {
            int trayCount = phraseTrayArea.childCount > 0 ? phraseTrayArea.childCount : 10;
            trayButtons = new Button[trayCount];
            trayImages = new Image[trayCount];
            trayTexts = new TextMeshProUGUI[trayCount];

            for (int i = 0; i < trayCount; i++) {
                string tileName = $"PhraseTile_{i + 1:D2}";
                Transform tTr = phraseTrayArea.Find(tileName) ?? (i < phraseTrayArea.childCount ? phraseTrayArea.GetChild(i) : null);
                if (tTr != null) {
                    trayButtons[i] = tTr.GetComponent<Button>();
                    trayImages[i] = tTr.GetComponent<Image>();

                    Transform txtTr = tTr.Find("Text");
                    if (txtTr != null) trayTexts[i] = txtTr.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        // Bind Back and Next Buttons
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }
    }

    private IEnumerator StartWithIntroRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 3.0f;
            yield return new WaitForSeconds(delay);
        } else {
            yield return new WaitForSeconds(0.4f);
        }

        SetupJourneyGame();
    }

    private void SetupJourneyGame() {
        if (journeyPoints == null || journeyPoints.Length == 0) return;

        lockedCount = 0;
        mistakeCount = 0;
        selectedTrayIndex = -1;
        isProcessingInput = false;

        int count = journeyPoints.Length;
        slotFilled = new bool[count];

        UpdateProgressDisplay();

        // 1. Reset Station Slots
        if (slotButtons != null) {
            for (int i = 0; i < slotButtons.Length; i++) {
                if (slotButtons[i] == null) continue;
                int slotIdx = i;
                bool hasData = (i < count);
                slotButtons[i].gameObject.SetActive(hasData);
                slotButtons[i].interactable = hasData;

                if (slotImages != null && slotImages[i] != null) slotImages[i].color = emptySlotColor;
                if (slotHeaderTexts != null && slotHeaderTexts[i] != null) slotHeaderTexts[i].text = $"<b>Step {i + 1}</b>";
                if (slotMeaningTexts != null && slotMeaningTexts[i] != null) slotMeaningTexts[i].text = "";

                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnSlotClicked(slotIdx));
            }
        }

        // 2. Setup Shuffled Tray
        List<int> indices = new List<int>();
        for (int i = 0; i < count; i++) indices.Add(i);
        ShuffleList(indices);
        trayToPointMapping = indices.ToArray();

        if (trayButtons != null) {
            for (int i = 0; i < trayButtons.Length; i++) {
                if (trayButtons[i] == null) continue;
                int trayIdx = i;
                bool hasData = (i < count);
                trayButtons[i].gameObject.SetActive(hasData);
                trayButtons[i].interactable = hasData;
                trayButtons[i].transform.localScale = Vector3.one;

                if (trayImages != null && trayImages[i] != null) trayImages[i].color = defaultTileColor;

                if (hasData) {
                    int ptIdx = trayToPointMapping[i];
                    if (trayTexts != null && trayTexts[i] != null) trayTexts[i].text = journeyPoints[ptIdx].travelPhrase;

                    trayButtons[i].onClick.RemoveAllListeners();
                    trayButtons[i].onClick.AddListener(() => OnTrayTileClicked(trayIdx));
                }
            }
        }
    }

    private void ShuffleList<T>(IList<T> list) {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private void OnTrayTileClicked(int trayIndex) {
        if (isProcessingInput || trayIndex < 0 || trayToPointMapping == null || trayIndex >= trayToPointMapping.Length) return;

        // Reset previous selected visual
        if (selectedTrayIndex >= 0 && trayImages != null && selectedTrayIndex < trayImages.Length && trayImages[selectedTrayIndex] != null) {
            trayImages[selectedTrayIndex].color = defaultTileColor;
        }

        selectedTrayIndex = trayIndex;

        // Highlight selected
        if (trayImages != null && trayIndex < trayImages.Length && trayImages[trayIndex] != null) {
            trayImages[trayIndex].color = selectedTileColor;
        }
        if (trayButtons != null && trayIndex < trayButtons.Length && trayButtons[trayIndex] != null) {
            trayButtons[trayIndex].transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0f), 0.2f, 5, 0.5f);
        }

        // Play Phrase Audio
        int ptIdx = trayToPointMapping[trayIndex];
        if (journeyPoints != null && ptIdx < journeyPoints.Length && journeyPoints[ptIdx].pointAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(journeyPoints[ptIdx].pointAudio);
        }
    }

    private void OnSlotClicked(int slotIndex) {
        if (isProcessingInput || slotIndex < 0 || journeyPoints == null || slotIndex >= journeyPoints.Length) return;
        if (slotFilled != null && slotFilled[slotIndex]) return; // Already locked

        if (selectedTrayIndex < 0 || trayToPointMapping == null) return; // No tile selected

        int expectedPointIndex = slotIndex;
        int selectedPointIndex = trayToPointMapping[selectedTrayIndex];

        bool isCorrect = (selectedPointIndex == expectedPointIndex);

        StartCoroutine(ProcessPlacementRoutine(selectedTrayIndex, slotIndex, selectedPointIndex, isCorrect));
    }

    private IEnumerator ProcessPlacementRoutine(int trayIdx, int slotIdx, int pointIdx, bool isCorrect) {
        isProcessingInput = true;

        if (isCorrect) {
            // Correct Placement!
            lockedCount++;
            if (slotFilled != null && slotIdx < slotFilled.Length) slotFilled[slotIdx] = true;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Lock slot and reveal meaning
            if (slotImages != null && slotIdx < slotImages.Length && slotImages[slotIdx] != null) {
                slotImages[slotIdx].color = lockedSlotColor;
            }
            if (slotHeaderTexts != null && slotIdx < slotHeaderTexts.Length && slotHeaderTexts[slotIdx] != null) {
                slotHeaderTexts[slotIdx].text = $"<b>{slotIdx + 1}. {journeyPoints[pointIdx].travelPhrase}</b>";
            }
            if (slotMeaningTexts != null && slotIdx < slotMeaningTexts.Length && slotMeaningTexts[slotIdx] != null) {
                slotMeaningTexts[slotIdx].text = journeyPoints[pointIdx].momentMeaning;
            }

            if (slotButtons != null && slotIdx < slotButtons.Length && slotButtons[slotIdx] != null) {
                slotButtons[slotIdx].interactable = false;
                slotButtons[slotIdx].transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.3f, 6, 0.5f);
            }

            // Hide placed tray tile
            if (trayButtons != null && trayIdx < trayButtons.Length && trayButtons[trayIdx] != null) {
                trayButtons[trayIdx].transform.DOScale(Vector3.zero, 0.25f);
                trayButtons[trayIdx].interactable = false;
            }

            UpdateProgressDisplay();

            yield return new WaitForSeconds(0.4f);

            selectedTrayIndex = -1;
            isProcessingInput = false;

            if (lockedCount >= journeyPoints.Length) {
                StartCoroutine(CompleteJourneyRoutine());
            }
        } else {
            // Wrong Placement!
            mistakeCount++;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            // Flash slot red & shake
            if (slotImages != null && slotIdx < slotImages.Length && slotImages[slotIdx] != null) {
                slotImages[slotIdx].color = wrongColor;
            }
            if (slotButtons != null && slotIdx < slotButtons.Length && slotButtons[slotIdx] != null) {
                slotButtons[slotIdx].transform.DOShakePosition(0.35f, new Vector3(10f, 0f, 0f), 10, 90f);
            }

            // Shake tray tile
            if (trayButtons != null && trayIdx < trayButtons.Length && trayButtons[trayIdx] != null) {
                trayButtons[trayIdx].transform.DOShakePosition(0.35f, new Vector3(10f, 0f, 0f), 10, 90f);
            }

            yield return new WaitForSeconds(0.45f);

            // Reset slot color
            if (slotImages != null && slotIdx < slotImages.Length && slotImages[slotIdx] != null) {
                slotImages[slotIdx].color = emptySlotColor;
            }

            // Reset tray tile color
            if (trayImages != null && trayIdx < trayImages.Length && trayImages[trayIdx] != null) {
                trayImages[trayIdx].color = defaultTileColor;
            }

            selectedTrayIndex = -1;
            isProcessingInput = false;
        }
    }

    private IEnumerator CompleteJourneyRoutine() {
        isProcessingInput = true;

        if (recapAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
            float delay = recapAudio.length > 0 ? recapAudio.length : 6.0f;
            yield return new WaitForSeconds(delay);
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        ShowResults();
    }

    private void UpdateProgressDisplay() {
        if (progressTMP != null) {
            int total = (journeyPoints != null) ? journeyPoints.Length : 10;
            progressTMP.text = $"{lockedCount}/{total}";
        }
    }

    private void OnReplayAudioClicked() {
        if (isProcessingInput) return;
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
    }

    private void ShowResults() {
        isProcessingInput = true;

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        int total = (journeyPoints != null) ? journeyPoints.Length : 10;
        int finalScore = Mathf.Max(0, total - mistakeCount);

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Score: {finalScore} / {total}";
        }

        bool passed = finalScore >= 8; // Success condition: at least 8 of 10 phrases

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ?
                "“Outstanding! You have planned and completed the whole travel journey!”" :
                "“Good effort! Try again to place at least 8 of the 10 journey steps in order!”";
            resultStatusTMP.color = passed ? new Color(0.2f, 0.9f, 0.4f) : new Color(1f, 0.6f, 0.2f);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(false);
            }
        } else {
            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(true);
                retryBtn.interactable = true;
            }
            if (nextButton != null) {
                nextButton.gameObject.SetActive(false);
            }
        }
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
        SetupJourneyGame();
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

