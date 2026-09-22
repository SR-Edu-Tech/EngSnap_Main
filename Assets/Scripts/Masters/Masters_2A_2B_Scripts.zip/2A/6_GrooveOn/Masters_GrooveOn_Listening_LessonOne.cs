using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


/// <summary>
/// Core Controller for Unit 6 (Groove On) Listening Lesson One:
/// L01 Pick the Category — Heard Greeting.
/// Preserves exact inspector/prefab button colors without changing or overriding them during play!
/// </summary>
public class Masters_GrooveOn_Listening_LessonOne : Masters_Lesson {

public enum Masters_GrooveOn_OccasionCategory {
    BIRTHDAY_WISH = 0,
    PARTY_QUESTION = 1,
    FESTIVAL_GREETING = 2,
    PREPARATION = 3
}

    [System.Serializable]
    public class GrooveOnListeningQuestionData {
        public AudioClip expressionAudio;
        public AudioClip slowAudio;
        public string expressionText;
        public Masters_GrooveOn_OccasionCategory correctCategory;
    }

    [Header("Unit 6 Listening L1 Data")]
    [SerializeField] private GrooveOnListeningQuestionData[] listeningQuestions;

    [Header("4-Card Category Setup")]
    [SerializeField] private Button[] optionButtons; // 4 Cards in PhraseCardsGrid
    [SerializeField] private string[] categoryLabels = new string[] {
        "BIRTHDAY WISH",
        "PARTY QUESTION",
        "FESTIVAL GREETING",
        "PREPARATION"
    };

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI headerTMP;

    [Header("Audio Controls")]
    [SerializeField] private Toggle slowToggle;
    [SerializeField] private Toggle repeatToggle;

    [Header("Next Lesson Routing")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentQuestionIndex = 0;
    private int correctScore = 0;
    private bool isAnswering = false;
    private bool isSlowed = false;
    private bool isRepeatOn = false;
    private Coroutine repeatCoroutine;

    private List<Color> originalCardColors = new List<Color>();

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;

        PurgeLegacyOptionsContainer();
        AutoFindPhraseCards();
        CacheOriginalCardColors();
        AutoFindToggles();

        if (slowToggle != null) {
            slowToggle.onValueChanged.RemoveAllListeners();
            slowToggle.onValueChanged.AddListener(OnSlowToggleChanged);
        }

        if (repeatToggle != null) {
            repeatToggle.onValueChanged.RemoveAllListeners();
            repeatToggle.onValueChanged.AddListener(OnRepeatToggleChanged);
        }
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Listening;

        if (listeningQuestions == null || listeningQuestions.Length == 0) {
            PopulateFailsafeQuestions();
        }

        AutoFindPhraseCards();
        CacheOriginalCardColors();
        UpdateTitleAndUIComponents();
        ConfigureCategoryCards();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        currentQuestionIndex = 0;
        correctScore = 0;
        isAnswering = false;

        StartCoroutine(StartGameRoutine());
    }

    private void CacheOriginalCardColors() {
        if (originalCardColors != null && originalCardColors.Count > 0) return;
        if (originalCardColors == null) originalCardColors = new List<Color>();
        originalCardColors.Clear();

        if (optionButtons != null) {
            foreach (var b in optionButtons) {
                if (b != null) {
                    Image img = b.GetComponent<Image>();
                    if (img != null) {
                        originalCardColors.Add(img.color);
                    } else {
                        originalCardColors.Add(Color.white);
                    }
                }
            }
        }
    }

    private void PurgeLegacyOptionsContainer() {
        Transform oldOpt = transform.Find("Options") ?? transform.Find("OptionsContainer") ?? transform.Find("Buttons");
        if (oldOpt != null && !oldOpt.name.Contains("PhraseCards")) {
            oldOpt.gameObject.SetActive(false);
        }
    }

    private void AutoFindPhraseCards() {
        Transform grid = transform.Find("PhraseCardsGrid") ?? transform.Find("PhraseCards") ?? transform.Find("Cards") ?? transform.Find("Grid");
        if (grid != null) {
            grid.gameObject.SetActive(true);
            Button[] btns = grid.GetComponentsInChildren<Button>(true);
            if (btns != null && btns.Length >= 4) {
                optionButtons = new Button[4];
                for (int i = 0; i < 4; i++) {
                    optionButtons[i] = btns[i];
                }
            }
        }

        if (optionButtons == null || optionButtons.Length == 0 || optionButtons[0] == null) {
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            List<Button> cardBtns = new List<Button>();
            foreach (var b in allBtns) {
                if (b == null) continue;
                string n = b.name.ToLower();
                if (!n.Contains("next") && !n.Contains("back") && !n.Contains("speaker") && !n.Contains("toggle")) {
                    cardBtns.Add(b);
                }
            }
            if (cardBtns.Count >= 4) {
                optionButtons = new Button[4];
                for (int i = 0; i < 4; i++) optionButtons[i] = cardBtns[i];
            } else if (cardBtns.Count > 0) {
                optionButtons = cardBtns.ToArray();
            }
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int btnIndex = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(btnIndex));
                }
            }
        }
    }

    private void AutoFindToggles() {
        if (slowToggle == null || repeatToggle == null) {
            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var t in toggles) {
                if (t == null) continue;
                string n = t.name.ToLower();
                if (slowToggle == null && (n.Contains("slow") || n.Contains("turtle"))) slowToggle = t;
                else if (repeatToggle == null && n.Contains("repeat")) repeatToggle = t;
            }
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in tmps) {
            if (t == null) continue;
            string n = t.name.ToLower();
            if (progressTMP == null && (n.Contains("progress") || n.Contains("count") || n.Contains("expressioncount"))) progressTMP = t;
            else if (titleTMP == null && (n.Contains("title") || n.Contains("lessontitle"))) titleTMP = t;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = t;
        }
    }

    private void ConfigureCategoryCards() {
        if (optionButtons == null) return;

        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] == null) continue;

            optionButtons[i].gameObject.SetActive(true);
            optionButtons[i].interactable = true;
            optionButtons[i].transition = Selectable.Transition.None;

            Image img = optionButtons[i].GetComponent<Image>();
            if (img != null) {
                img.raycastTarget = true;
                // PRESERVE ORIGINAL INSPECTOR COLOR
                if (originalCardColors != null && i < originalCardColors.Count) {
                    img.color = originalCardColors[i];
                }
            }

            TMP_Text tmp = optionButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) {
                tmp.raycastTarget = false;
                tmp.text = (i < categoryLabels.Length) ? categoryLabels[i] : $"Category {i + 1}";
            }
        }
    }

    private void UpdateTitleAndUIComponents() {
        if (titleTMP != null) {
            titleTMP.text = "L01 Pick the Category — Heard Greeting";
        }
        if (headerTMP != null) {
            headerTMP.text = "LISTENING BRANCH (Audio Board)";
        }
    }

    private IEnumerator StartGameRoutine() {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(narratorSpeech.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        LoadQuestion(0);
    }

    private void LoadQuestion(int index) {
        if (listeningQuestions == null || index >= listeningQuestions.Length) {
            EndGame();
            return;
        }

        currentQuestionIndex = index;
        isAnswering = false;

        ResetCardColors();
        UpdateProgressUI();
        PlayCurrentAudio();
    }

    private void PlayCurrentAudio() {
        if (repeatCoroutine != null) {
            StopCoroutine(repeatCoroutine);
            repeatCoroutine = null;
        }

        if (listeningQuestions == null || currentQuestionIndex >= listeningQuestions.Length) return;

        var q = listeningQuestions[currentQuestionIndex];
        AudioClip clip = (isSlowed && q.slowAudio != null) ? q.slowAudio : q.expressionAudio;
        if (clip == null) clip = q.expressionAudio;

        if (clip != null && Masters_AudioManager.Instance != null) {
            if (isRepeatOn) {
                repeatCoroutine = StartCoroutine(RepeatAudioRoutine(clip));
            } else {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }
    }

    private IEnumerator RepeatAudioRoutine(AudioClip clip) {
        while (isRepeatOn && !isAnswering) {
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
            float delay = (clip != null) ? clip.length + 1.8f : 3f;
            yield return new WaitForSeconds(delay);
        }
    }

    private void OnOptionSelected(int selectedIndex) {
        if (isAnswering || listeningQuestions == null || currentQuestionIndex >= listeningQuestions.Length) return;

        var q = listeningQuestions[currentQuestionIndex];
        int correctIdx = (int)q.correctCategory;

        if (selectedIndex >= optionButtons.Length || optionButtons[selectedIndex] == null) return;

        Button selBtn = optionButtons[selectedIndex];
        Image btnImg = selBtn.GetComponent<Image>();

        if (selectedIndex == correctIdx) {
            // CORRECT!
            isAnswering = true;
            correctScore++;
            UpdateProgressUI();

            if (btnImg != null) btnImg.color = new Color(0.13f, 0.77f, 0.36f, 1f); // Green Flash
            selBtn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(AdvanceAfterDelay(1.2f));
        } else {
            // WRONG
            if (btnImg != null) btnImg.color = new Color(0.9f, 0.2f, 0.2f, 1f); // Red Flash
            selBtn.transform.DOShakePosition(0.35f, 10f, 15, 90f);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            }

            StartCoroutine(RestoreCardColorAfterDelay(selectedIndex, 0.6f));
        }
    }

    private IEnumerator RestoreCardColorAfterDelay(int index, float delay) {
        yield return new WaitForSeconds(delay);
        if (index < optionButtons.Length && optionButtons[index] != null) {
            Image img = optionButtons[index].GetComponent<Image>();
            if (img != null && originalCardColors != null && index < originalCardColors.Count) {
                img.color = originalCardColors[index];
            }
        }
    }

    private void ResetCardColors() {
        if (optionButtons == null) return;
        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] != null) {
                optionButtons[i].interactable = true;
                Image img = optionButtons[i].GetComponent<Image>();
                if (img != null && originalCardColors != null && i < originalCardColors.Count) {
                    img.color = originalCardColors[i];
                }
            }
        }
    }

    private void UpdateProgressUI() {
        if (progressTMP != null && listeningQuestions != null) {
            progressTMP.text = $"SCORE: {correctScore} | PROGRESS: {currentQuestionIndex + 1}/{listeningQuestions.Length}";
        }
    }

    private void OnSlowToggleChanged(bool val) {
        isSlowed = val;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        PlayCurrentAudio();
    }

    private void OnRepeatToggleChanged(bool val) {
        isRepeatOn = val;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        PlayCurrentAudio();
    }

    private IEnumerator AdvanceAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        LoadQuestion(currentQuestionIndex + 1);
    }

    private void EndGame() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }

        if (progressTMP != null && listeningQuestions != null) {
            progressTMP.text = $"SCORE: {correctScore} / {listeningQuestions.Length} — COMPLETED!";
        }
    }

    public void PopulateFailsafeQuestions() {
        string audioDir = "Assets/Audio/2A/6_GrooveOn/Listening/";
#if UNITY_EDITOR
        listeningQuestions = new GrooveOnListeningQuestionData[] {
            new GrooveOnListeningQuestionData {
                expressionText = "Many more happy returns of the day!",
                correctCategory = Masters_GrooveOn_OccasionCategory.BIRTHDAY_WISH,
                expressionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Many more happy returns of the day.mp3")
            },
            new GrooveOnListeningQuestionData {
                expressionText = "Belated birthday wishes!",
                correctCategory = Masters_GrooveOn_OccasionCategory.BIRTHDAY_WISH,
                expressionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Belated birthday wishes.mp3")
            },
            new GrooveOnListeningQuestionData {
                expressionText = "Where's the party?",
                correctCategory = Masters_GrooveOn_OccasionCategory.PARTY_QUESTION,
                expressionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Where is the party.mp3")
            },
            new GrooveOnListeningQuestionData {
                expressionText = "What about the theme?",
                correctCategory = Masters_GrooveOn_OccasionCategory.PARTY_QUESTION,
                expressionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "What about the theme.mp3")
            },
            new GrooveOnListeningQuestionData {
                expressionText = "Wish you a Happy Diwali!",
                correctCategory = Masters_GrooveOn_OccasionCategory.FESTIVAL_GREETING,
                expressionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Wish you a Happy Diwali.mp3")
            },
            new GrooveOnListeningQuestionData {
                expressionText = "Eid Mubarak!",
                correctCategory = Masters_GrooveOn_OccasionCategory.FESTIVAL_GREETING,
                expressionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Eid Mubarak.mp3")
            },
            new GrooveOnListeningQuestionData {
                expressionText = "Merry Christmas!",
                correctCategory = Masters_GrooveOn_OccasionCategory.FESTIVAL_GREETING,
                expressionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Merry Christmas.mp3")
            },
            new GrooveOnListeningQuestionData {
                expressionText = "Decorate the house.",
                correctCategory = Masters_GrooveOn_OccasionCategory.PREPARATION,
                expressionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Decorate the house.mp3")
            },
            new GrooveOnListeningQuestionData {
                expressionText = "Do shopping for new clothes.",
                correctCategory = Masters_GrooveOn_OccasionCategory.PREPARATION,
                expressionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Do shopping for new clothes.mp3")
            },
            new GrooveOnListeningQuestionData {
                expressionText = "Make delicious food.",
                correctCategory = Masters_GrooveOn_OccasionCategory.PREPARATION,
                expressionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Make delicious food.mp3")
            }
        };
#endif
    }

    protected override void OnNextButtonClicked() {
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Listening);
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}