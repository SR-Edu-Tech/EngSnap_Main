using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Subclass for Unit 7 (Collocations) Reading Lesson Three: R03 Spot the Odd Pair.
/// Gameplay:
/// 1. Player sees 4 collocation cards on bench (3 valid pairs, 1 cross-web mismatch).
/// 2. Player taps odd pair.
/// 3. Prompt asks: "Which hub does the stray partner really belong to?"
/// 4. Player selects from 4 Hub choices (GET, CATCH, IDEA, SAVE).
/// 5. Correct answer plays VO_R03_REHOME audio, animates stray tile moving to correct hub, snaps into hub with SFX_MAGNET_SNAP.
/// 6. Wrong answer plays SFX_MAGNET_REPEL with retry mechanism (1 retry per round).
/// 7. 6 complete sets. Pass threshold: >= 5 out of 6 sets.
/// </summary>
public class Masters_Collocations_Reading_LessonThree : Masters_Lesson {

    public enum CollocationHub {
        GET = 0,
        CATCH = 1,
        IDEA = 2,
        SAVE = 3
    }

    [System.Serializable]
    public class R03SetData {
        public string setId;                     // e.g. "SET 1"
        public string[] collocationCards;        // 4 cards e.g. ["get ready", "get dressed", "get a bus", "get married"]
        public int oddPairIndex;                 // Index of odd pair (e.g., 2 for "get a bus")
        public CollocationHub correctHub;       // e.g. CollocationHub.CATCH
        public string strayPartner;              // e.g. "a bus"
        public string correctedCollocation;      // e.g. "catch a bus"
        public AudioClip ariaPromptAudio;        // VO_R03_ARIA clip
        public AudioClip rehomeAudio;            // VO_R03_REHOME clip
    }

    [Header("R03 Configuration Data (12 Sets)")]
    [SerializeField] private R03SetData[] sets;

    [Header("Top Header UI")]
    [SerializeField] private TextMeshProUGUI r03TitleTMP;
    [SerializeField] private TextMeshProUGUI r03InstructionTMP;
    [SerializeField] private TextMeshProUGUI r03ProgressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;

    [Header("Bench Collocation Cards (4 Cards)")]
    [SerializeField] private Button[] cardButtons; // 4 cards on bench
    [SerializeField] private TextMeshProUGUI[] cardTexts;

    [Header("Hub Selection Area")]
    [SerializeField] private GameObject hubSelectionPanel;
    [SerializeField] private TextMeshProUGUI hubQuestionTMP;
    [SerializeField] private Button[] hubButtons; // 4 Hub chips: GET, CATCH, IDEA, SAVE
    [SerializeField] private TextMeshProUGUI[] hubTexts;
    [SerializeField] private RectTransform[] hubTransforms; // Transforms of the 4 Hub chips for tile re-homing target

    [Header("Result & Navigation UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTMP;
    [SerializeField] private Button retryButton;

    [Header("Audio & SFX")]
    [SerializeField] private AudioClip ariaIntroAudio;  // VO_R03_ARIA ("Three of these are real pairs — which one doesn't belong?")
    [SerializeField] private AudioClip sfxSnap;         // SFX_MAGNET_SNAP
    [SerializeField] private AudioClip sfxRepel;        // SFX_MAGNET_REPEL

    [Header("Pass Rules")]
    [SerializeField] private int passScore = 6;         // Must pass at least 6 of 12 sets

    // Runtime state variables
    private int currentSetIndex = 0;
    private int successfulSetsScore = 0;
    private bool isOddPairSelected = false;
    private bool isRoundCompleted = false;
    private int oddPairRetriesLeft = 1;
    private int hubRetriesLeft = 1;
    private bool setHadErrors = false;
    private Vector3[] initialCardPositions;
    private Color defaultCardColor = Color.white;
    private Color selectedOddPairColor = new Color(1f, 0.9f, 0.4f, 1f); // Warm yellow highlight
    private Color correctGreenColor = new Color(0.4f, 0.9f, 0.4f, 1f);
    private Color wrongRedColor = new Color(1f, 0.4f, 0.4f, 1f);

    protected override void Awake() {
        base.Awake();
        AutoFindUIReferences();
        CacheInitialCardPositions();
    }

    protected override void Start() {
        topic = Masters_Topic.Reading;
        UpdateTitleAndUIComponents();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        currentSetIndex = 0;
        successfulSetsScore = 0;

        StartCoroutine(InitializeR03Routine());
    }

    private void CacheInitialCardPositions() {
        if (cardButtons != null && cardButtons.Length > 0) {
            initialCardPositions = new Vector3[cardButtons.Length];
            for (int i = 0; i < cardButtons.Length; i++) {
                if (cardButtons[i] != null) {
                    initialCardPositions[i] = cardButtons[i].transform.localPosition;
                }
            }
        }
    }

    private void AutoFindUIReferences() {
        if (r03TitleTMP == null) {
            Transform t = transform.Find("Title") ?? transform.Find("TopHeader/TitleText");
            if (t != null) r03TitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (r03InstructionTMP == null) {
            Transform t = transform.Find("InstructionText") ?? transform.Find("TopHeader/InstructionText");
            if (t != null) r03InstructionTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (r03ProgressTMP == null) {
            Transform t = transform.Find("ProgressIndicator") ?? transform.Find("ProgressText");
            if (t != null) r03ProgressTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (scoreTMP == null) {
            Transform t = transform.Find("ScoreIndicator") ?? transform.Find("ScoreText");
            if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (cardButtons == null || cardButtons.Length < 4) {
            Transform bench = transform.Find("BenchCardsArea") ?? transform.Find("BenchCards") ?? transform.Find("ReadingBench");
            if (bench != null) {
                Button[] btns = bench.GetComponentsInChildren<Button>(true);
                if (btns.Length >= 4) {
                    cardButtons = new Button[4];
                    cardTexts = new TextMeshProUGUI[4];
                    for (int i = 0; i < 4; i++) {
                        cardButtons[i] = btns[i];
                        cardTexts[i] = btns[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    }
                }
            }
        }

        if (hubSelectionPanel == null) {
            Transform hPanel = transform.Find("HubSelectionPanel") ?? transform.Find("HubArea");
            if (hPanel != null) hubSelectionPanel = hPanel.gameObject;
        }

        if (hubButtons == null || hubButtons.Length < 4) {
            Transform hubContainer = (hubSelectionPanel != null) ? hubSelectionPanel.transform.Find("HubButtons") : transform.Find("HubButtons");
            if (hubContainer != null) {
                Button[] btns = hubContainer.GetComponentsInChildren<Button>(true);
                if (btns.Length >= 4) {
                    hubButtons = new Button[4];
                    hubTexts = new TextMeshProUGUI[4];
                    hubTransforms = new RectTransform[4];
                    for (int i = 0; i < 4; i++) {
                        hubButtons[i] = btns[i];
                        hubTexts[i] = btns[i].GetComponentInChildren<TextMeshProUGUI>(true);
                        hubTransforms[i] = btns[i].GetComponent<RectTransform>();
                    }
                }
            }
        }

        if (resultPanel == null) {
            Transform res = transform.Find("ResultPanel");
            if (res != null) resultPanel = res.gameObject;
        }
    }

    private void UpdateTitleAndUIComponents() {
        if (r03TitleTMP != null) r03TitleTMP.text = "R03 Spot the Odd Pair";
        if (r03InstructionTMP != null) r03InstructionTMP.text = "Which one doesn't belong?";
    }

    private IEnumerator InitializeR03Routine() {
        yield return new WaitForEndOfFrame();

        // Play intro ARIA voiceover
        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
        }

        LoadSet(0);
    }

    private void LoadSet(int setIndex) {
        if (sets == null || setIndex < 0 || setIndex >= sets.Length) {
            EvaluateFinalScore();
            return;
        }

        currentSetIndex = setIndex;
        isOddPairSelected = false;
        isRoundCompleted = false;
        oddPairRetriesLeft = 1;
        hubRetriesLeft = 1;
        setHadErrors = false;

        R03SetData currentSet = sets[currentSetIndex];

        // Update progress & score text
        if (r03ProgressTMP != null) r03ProgressTMP.text = $"Set {currentSetIndex + 1}/{sets.Length}";
        if (scoreTMP != null) scoreTMP.text = $"Score: {successfulSetsScore}/{sets.Length}";

        // Reset bench cards positions and visual state
        for (int i = 0; i < cardButtons.Length; i++) {
            if (cardButtons[i] != null) {
                cardButtons[i].gameObject.SetActive(true);
                cardButtons[i].interactable = true;
                cardButtons[i].transform.DOKill();

                if (initialCardPositions != null && i < initialCardPositions.Length) {
                    cardButtons[i].transform.localPosition = initialCardPositions[i];
                }
                cardButtons[i].transform.localScale = Vector3.one;

                Image img = cardButtons[i].GetComponent<Image>();
                if (img != null) img.color = defaultCardColor;

                if (cardTexts != null && i < cardTexts.Length && cardTexts[i] != null) {
                    if (currentSet.collocationCards != null && i < currentSet.collocationCards.Length) {
                        cardTexts[i].text = currentSet.collocationCards[i];
                    }
                    cardTexts[i].color = Color.white;
                }

                int cardIdx = i;
                cardButtons[i].onClick.RemoveAllListeners();
                cardButtons[i].onClick.AddListener(() => OnCardSelected(cardIdx));
            }
        }

        // Setup Hub chips
        if (hubSelectionPanel != null) {
            hubSelectionPanel.SetActive(false); // Hide hub panel until odd pair is identified
        }

        SetupHubButtons();
    }

    private void SetupHubButtons() {
        string[] hubNames = new string[] { "GET", "CATCH", "IDEA", "SAVE" };
        for (int i = 0; i < hubButtons.Length; i++) {
            if (hubButtons[i] != null) {
                hubButtons[i].interactable = true;
                Image img = hubButtons[i].GetComponent<Image>();
                if (img != null) img.color = defaultCardColor;

                if (hubTexts != null && i < hubTexts.Length && hubTexts[i] != null) {
                    hubTexts[i].text = (i < hubNames.Length) ? hubNames[i] : "";
                    hubTexts[i].color = Color.white;
                }

                int hubIdx = i;
                hubButtons[i].onClick.RemoveAllListeners();
                hubButtons[i].onClick.AddListener(() => OnHubSelected((CollocationHub)hubIdx, hubIdx));
            }
        }
    }

    private void OnCardSelected(int cardIndex) {
        if (isOddPairSelected || isRoundCompleted || sets == null || currentSetIndex >= sets.Length) return;

        R03SetData currentSet = sets[currentSetIndex];
        bool isCorrectOddPair = (cardIndex == currentSet.oddPairIndex);

        if (isCorrectOddPair) {
            // Correct Odd Pair Tapped!
            isOddPairSelected = true;

            // Highlight selected odd pair card
            if (cardIndex < cardButtons.Length && cardButtons[cardIndex] != null) {
                Image img = cardButtons[cardIndex].GetComponent<Image>();
                if (img != null) img.color = selectedOddPairColor;
                cardButtons[cardIndex].transform.DOKill(true);
                cardButtons[cardIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            // Lock all card buttons
            for (int i = 0; i < cardButtons.Length; i++) {
                if (cardButtons[i] != null) cardButtons[i].interactable = false;
            }

            // Play positive selection sound
            PlaySnapSFX();

            // Reveal Hub Question Panel
            if (hubSelectionPanel != null) {
                hubSelectionPanel.SetActive(true);
                hubSelectionPanel.transform.DOKill();
                hubSelectionPanel.transform.localScale = Vector3.zero;
                hubSelectionPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }

            if (hubQuestionTMP != null) {
                hubQuestionTMP.text = "Which hub does the stray partner really belong to?";
            }
        } else {
            // Wrong card tapped!
            setHadErrors = true;
            PlayRepelSFX();

            if (cardIndex < cardButtons.Length && cardButtons[cardIndex] != null) {
                cardButtons[cardIndex].transform.DOKill(true);
                cardButtons[cardIndex].transform.DOShakePosition(0.4f, new Vector3(12f, 0f, 0f), 15, 90f);
            }

            if (oddPairRetriesLeft > 0) {
                oddPairRetriesLeft--;
                // Student has 1 retry to tap the correct card
            } else {
                // Retry exhausted for odd pair: auto-highlight correct odd pair & move to hub selection
                isOddPairSelected = true;
                int correctIdx = currentSet.oddPairIndex;

                if (correctIdx < cardButtons.Length && cardButtons[correctIdx] != null) {
                    Image img = cardButtons[correctIdx].GetComponent<Image>();
                    if (img != null) img.color = selectedOddPairColor;
                }

                for (int i = 0; i < cardButtons.Length; i++) {
                    if (cardButtons[i] != null) cardButtons[i].interactable = false;
                }

                if (hubSelectionPanel != null) {
                    hubSelectionPanel.SetActive(true);
                    hubSelectionPanel.transform.DOKill();
                    hubSelectionPanel.transform.localScale = Vector3.zero;
                    hubSelectionPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
                }
            }
        }
    }

    private void OnHubSelected(CollocationHub selectedHub, int hubButtonIndex) {
        if (isRoundCompleted || sets == null || currentSetIndex >= sets.Length) return;

        R03SetData currentSet = sets[currentSetIndex];
        bool isCorrectHub = (selectedHub == currentSet.correctHub);

        if (isCorrectHub) {
            // Correct Hub Selected!
            isRoundCompleted = true;

            // Highlight Hub Button
            if (hubButtonIndex < hubButtons.Length && hubButtons[hubButtonIndex] != null) {
                Image img = hubButtons[hubButtonIndex].GetComponent<Image>();
                if (img != null) img.color = correctGreenColor;
                hubButtons[hubButtonIndex].transform.DOKill(true);
                hubButtons[hubButtonIndex].transform.DOPunchScale(Vector3.one * 0.2f, 0.35f);
            }

            // Lock all Hub Buttons
            for (int i = 0; i < hubButtons.Length; i++) {
                if (hubButtons[i] != null) hubButtons[i].interactable = false;
            }

            // Score set as successful when player selects correct hub
            if (!setHadErrors || hubRetriesLeft > 0) {
                successfulSetsScore++;
            }
            if (scoreTMP != null) scoreTMP.text = $"Score: {successfulSetsScore}/{sets.Length}";

            // Execute stray tile re-homing animation to the target hub chip
            StartCoroutine(AnimateStrayTileRehome(currentSet, hubButtonIndex));
        } else {
            // Incorrect Hub Selected!
            setHadErrors = true;
            PlayRepelSFX();

            if (hubButtonIndex < hubButtons.Length && hubButtons[hubButtonIndex] != null) {
                hubButtons[hubButtonIndex].transform.DOKill(true);
                hubButtons[hubButtonIndex].transform.DOShakePosition(0.4f, new Vector3(12f, 0f, 0f), 15, 90f);
            }

            if (hubRetriesLeft > 0) {
                hubRetriesLeft--;
                // Exactly 1 retry allowed
            } else {
                // Retry exhausted: Auto-highlight correct hub and trigger rehome
                isRoundCompleted = true;
                int correctHubIdx = (int)currentSet.correctHub;

                if (correctHubIdx < hubButtons.Length && hubButtons[correctHubIdx] != null) {
                    Image img = hubButtons[correctHubIdx].GetComponent<Image>();
                    if (img != null) img.color = correctGreenColor;
                }

                for (int i = 0; i < hubButtons.Length; i++) {
                    if (hubButtons[i] != null) hubButtons[i].interactable = false;
                }

                StartCoroutine(AnimateStrayTileRehome(currentSet, correctHubIdx));
            }
        }
    }

    private IEnumerator AnimateStrayTileRehome(R03SetData currentSet, int targetHubIndex) {
        int oddIdx = currentSet.oddPairIndex;
        Button strayTile = (oddIdx < cardButtons.Length) ? cardButtons[oddIdx] : null;

        // Play VO_R03_REHOME voiceover
        if (currentSet.rehomeAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(currentSet.rehomeAudio);
        }

        // Animate stray tile moving smoothly from bench toward target hub chip
        if (strayTile != null && targetHubIndex < hubTransforms.Length && hubTransforms[targetHubIndex] != null) {
            // Update tile text to corrected collocation
            if (oddIdx < cardTexts.Length && cardTexts[oddIdx] != null && !string.IsNullOrEmpty(currentSet.correctedCollocation)) {
                cardTexts[oddIdx].text = currentSet.correctedCollocation;
            }

            Transform targetT = hubTransforms[targetHubIndex];
            Vector3 startPos = strayTile.transform.position;
            Vector3 targetPos = targetT.position;

            strayTile.transform.DOMove(targetPos, 0.65f).SetEase(Ease.InOutQuad);
            strayTile.transform.DOScale(Vector3.one * 0.75f, 0.65f);

            yield return new WaitForSeconds(0.65f);

            // Snap tile into hub & Play SFX_MAGNET_SNAP sound
            PlaySnapSFX();
            strayTile.transform.DOPunchScale(Vector3.one * 0.25f, 0.3f);
        } else {
            PlaySnapSFX();
            yield return new WaitForSeconds(0.5f);
        }

        float voDuration = (currentSet.rehomeAudio != null) ? currentSet.rehomeAudio.length : 1.5f;
        yield return new WaitForSeconds(Mathf.Max(0.5f, voDuration - 0.65f + 0.3f));

        // Advance to next set
        LoadSet(currentSetIndex + 1);
    }

    private void PlaySnapSFX() {
        if (sfxSnap != null) {
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(sfxSnap, pos);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }
    }

    private void PlayRepelSFX() {
        if (sfxRepel != null) {
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(sfxRepel, pos);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private void EvaluateFinalScore() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        bool passed = (successfulSetsScore >= passScore);

        if (resultTMP != null) {
            if (passed) {
                resultTMP.text = $"GREAT JOB! Score: {successfulSetsScore}/{sets.Length}\nYou spotted all the odd pairs!";
            } else {
                resultTMP.text = $"TRY AGAIN! Score: {successfulSetsScore}/{sets.Length}\nYou need at least {passScore}/{sets.Length} to pass.";
            }
        }

        if (passed) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
                NextButtonAnimation();
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (retryButton != null) {
                retryButton.gameObject.SetActive(true);
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(RestartLesson);
            }
        }
    }

    public void RestartLesson() {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        currentSetIndex = 0;
        successfulSetsScore = 0;
        LoadSet(0);
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Reading;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}