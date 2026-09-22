using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_EnglishIsImportant_Listening_LessonOne : Masters_Lesson {

public enum ListeningL01Function {
    AskToRepeat = 0,
    CheckThem = 1,
    CheckMe = 2,
    AnotherWay = 3
}

[System.Serializable]
public class ListeningL01RoundData {
    public AudioClip phraseAudioClip;
    public AudioClip slowAudioClip;
    public string phraseText;
    public ListeningL01Function correctFunction;
}
    [Header("L01 Configurable Rounds")]
    [SerializeField]
    private ListeningL01RoundData[] roundDataArray;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI lessonTitleTMP;

    [Header("Interactive Answer Chips")]
    [SerializeField]
    private Button askToRepeatBtn;
    [SerializeField]
    private Button checkThemBtn;
    [SerializeField]
    private Button checkMeBtn;
    [SerializeField]
    private Button anotherWayBtn;

    [Header("Audio & Speed Controls")]
    [SerializeField]
    private Button replayAudioBtn;
    [SerializeField]
    private Toggle repeatThisToggle;
    [SerializeField]
    private Toggle slowToggle;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Colors & Animation")]
    [SerializeField]
    private Color correctColor = new Color(0.2f, 0.8f, 0.26f, 1f);
    [SerializeField]
    private Color wrongColor = new Color(0.8f, 0.26f, 0.2f, 1f);

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool isSlowMode = false;

    private Image askToRepeatImg;
    private Image checkThemImg;
    private Image checkMeImg;
    private Image anotherWayImg;

    private Color originalAskColor = Color.white;
    private Color originalCheckThemColor = Color.white;
    private Color originalCheckMeColor = Color.white;
    private Color originalAnotherWayColor = Color.white;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;

        // 1. Auto-bind missing references if unassigned in Inspector
        AutoBindReferences();

        // 2. Add click listeners & cache original image colors
        if (askToRepeatBtn != null) {
            askToRepeatImg = askToRepeatBtn.GetComponent<Image>();
            if (askToRepeatImg != null) originalAskColor = askToRepeatImg.color;
            askToRepeatBtn.onClick.RemoveAllListeners();
            askToRepeatBtn.onClick.AddListener(() => OnFunctionSelected(ListeningL01Function.AskToRepeat, askToRepeatBtn, askToRepeatImg));
        }
        if (checkThemBtn != null) {
            checkThemImg = checkThemBtn.GetComponent<Image>();
            if (checkThemImg != null) originalCheckThemColor = checkThemImg.color;
            checkThemBtn.onClick.RemoveAllListeners();
            checkThemBtn.onClick.AddListener(() => OnFunctionSelected(ListeningL01Function.CheckThem, checkThemBtn, checkThemImg));
        }
        if (checkMeBtn != null) {
            checkMeImg = checkMeBtn.GetComponent<Image>();
            if (checkMeImg != null) originalCheckMeColor = checkMeImg.color;
            checkMeBtn.onClick.RemoveAllListeners();
            checkMeBtn.onClick.AddListener(() => OnFunctionSelected(ListeningL01Function.CheckMe, checkMeBtn, checkMeImg));
        }
        if (anotherWayBtn != null) {
            anotherWayImg = anotherWayBtn.GetComponent<Image>();
            if (anotherWayImg != null) originalAnotherWayColor = anotherWayImg.color;
            anotherWayBtn.onClick.RemoveAllListeners();
            anotherWayBtn.onClick.AddListener(() => OnFunctionSelected(ListeningL01Function.AnotherWay, anotherWayBtn, anotherWayImg));
        }

        // 3. Audio Repeat & Slow controls
        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (repeatThisToggle != null) {
            repeatThisToggle.onValueChanged.RemoveAllListeners();
            repeatThisToggle.onValueChanged.AddListener((isOn) => {
                OnReplayAudioClicked();
            });

            Button togBtn = repeatThisToggle.GetComponent<Button>();
            if (togBtn != null) {
                togBtn.onClick.RemoveAllListeners();
                togBtn.onClick.AddListener(OnReplayAudioClicked);
            }
        }

        if (slowToggle != null) {
            slowToggle.onValueChanged.RemoveAllListeners();
            slowToggle.onValueChanged.AddListener(OnSlowToggleChanged);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        // 4. Ensure button text labels are visible and correct
        EnsureButtonLabels();
        EnsureLessonTitle();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Listening;

        EnsureButtonLabels();
        EnsureLessonTitle();

        // Failsafe: Populate default rounds if roundDataArray is unassigned or empty
        if (roundDataArray == null || roundDataArray.Length == 0) {
            PopulateFailsafeRounds();
        }

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private void EnsureLessonTitle() {
        if (lessonTitleTMP != null) {
            lessonTitleTMP.text = "L01 Hear It — What Is the Speaker Doing?";
        }
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(Mathf.Max(2.5f, narratorSpeech.length));
        }

        StartLesson();
    }

    private void AutoBindReferences() {
        // Find Toggles
        Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
        foreach (Toggle t in toggles) {
            string tn = t.gameObject.name.ToLower();
            if (slowToggle == null && tn.Contains("slow")) slowToggle = t;
            else if (repeatThisToggle == null && tn.Contains("repeat")) repeatThisToggle = t;
        }

        if (repeatThisToggle == null) {
            Transform repTr = transform.Find("PhraseCardsGrid/Controls/RepeatThisToggle") 
                           ?? transform.Find("Controls/RepeatThisToggle") 
                           ?? transform.Find("RepeatThisToggle");
            if (repTr != null) repeatThisToggle = repTr.GetComponent<Toggle>();
        }

        if (slowToggle == null) {
            Transform slowTr = transform.Find("PhraseCardsGrid/Controls/SlowToggle") 
                            ?? transform.Find("Controls/SlowToggle") 
                            ?? transform.Find("SlowToggle");
            if (slowTr != null) slowToggle = slowTr.GetComponent<Toggle>();
        }

        // Find Buttons
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> optionButtons = new List<Button>();

        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("optionbutton")) {
                optionButtons.Add(btn);
            } else if (replayAudioBtn == null && (n.Contains("replay") || (n.Contains("repeat") && repeatThisToggle == null) || n.Contains("speakericon"))) {
                replayAudioBtn = btn;
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (nextButton == null && n.Contains("next")) {
                nextButton = btn;
            }
        }

        // Assign 4 option buttons in order: OptionButton, OptionButton (1), OptionButton (2), OptionButton (3)
        if (optionButtons.Count >= 4) {
            if (askToRepeatBtn == null) askToRepeatBtn = optionButtons[0];
            if (checkThemBtn == null) checkThemBtn = optionButtons[1];
            if (checkMeBtn == null) checkMeBtn = optionButtons[2];
            if (anotherWayBtn == null) anotherWayBtn = optionButtons[3];
        }

        // Bind TMP components
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("expressioncount") || n.Contains("progress") || n.Contains("counter"))) {
                progressTMP = tmp;
            } else if (lessonTitleTMP == null && (n.Contains("lessontitle") || tmp.transform.parent.name.ToLower().Contains("lessontitle"))) {
                lessonTitleTMP = tmp;
            }
        }
    }

    private void EnsureButtonLabels() {
        SetButtonLabel(askToRepeatBtn, "ASK TO REPEAT");
        SetButtonLabel(checkThemBtn, "CHECK THEM");
        SetButtonLabel(checkMeBtn, "CHECK ME");
        SetButtonLabel(anotherWayBtn, "ANOTHER WAY");
    }

    private void SetButtonLabel(Button btn, string labelText) {
        if (btn == null) return;
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) {
            tmp.text = labelText;
            tmp.enabled = true;
            tmp.gameObject.SetActive(true);
        } else {
            UnityEngine.UI.Text txt = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (txt != null) {
                txt.text = labelText;
                txt.enabled = true;
                txt.gameObject.SetActive(true);
            }
        }
    }

    private void PopulateFailsafeRounds() {
        roundDataArray = new ListeningL01RoundData[10];
        (string phrase, ListeningL01Function func, string audioPath, string slowPath)[] samples = new (string, ListeningL01Function, string, string)[] {
            ("Would/Could you say that again, please?", ListeningL01Function.AskToRepeat, "Assets/Audio/2B/1_EnglishIsImportant/Listening/eii_l01_p01.mp3", "Assets/Audio/2B/1_EnglishIsImportant/Listening/Slow/eii_l01_p01_slow.mp3"),
            ("Can you speak louder, please?", ListeningL01Function.AskToRepeat, "Assets/Audio/2B/1_EnglishIsImportant/Listening/eii_l01_p02.mp3", "Assets/Audio/2B/1_EnglishIsImportant/Listening/Slow/eii_l01_p02_slow.mp3"),
            ("What does the word... mean?", ListeningL01Function.AskToRepeat, "Assets/Audio/2B/1_EnglishIsImportant/Listening/eii_l01_p03.mp3", "Assets/Audio/2B/1_EnglishIsImportant/Listening/Slow/eii_l01_p03_slow.mp3"),
            ("I'm sorry, I didn't hear what you said.", ListeningL01Function.AskToRepeat, "Assets/Audio/2B/1_EnglishIsImportant/Listening/eii_l01_p04.mp3", "Assets/Audio/2B/1_EnglishIsImportant/Listening/Slow/eii_l01_p04_slow.mp3"),
            ("Do you know what I mean?", ListeningL01Function.CheckThem, "Assets/Audio/2B/1_EnglishIsImportant/Listening/eii_l01_p05.mp3", "Assets/Audio/2B/1_EnglishIsImportant/Listening/Slow/eii_l01_p05_slow.mp3"),
            ("Do I make myself clear?", ListeningL01Function.CheckThem, "Assets/Audio/2B/1_EnglishIsImportant/Listening/eii_l01_p06.mp3", "Assets/Audio/2B/1_EnglishIsImportant/Listening/Slow/eii_l01_p06_slow.mp3"),
            ("Got the message?", ListeningL01Function.CheckThem, "Assets/Audio/2B/1_EnglishIsImportant/Listening/eii_l01_p07.mp3", "Assets/Audio/2B/1_EnglishIsImportant/Listening/Slow/eii_l01_p07_slow.mp3"),
            ("Do you mean...?", ListeningL01Function.CheckMe, "Assets/Audio/2B/1_EnglishIsImportant/Listening/eii_l01_p08.mp3", "Assets/Audio/2B/1_EnglishIsImportant/Listening/Slow/eii_l01_p08_slow.mp3"),
            ("Does that mean...?", ListeningL01Function.CheckMe, "Assets/Audio/2B/1_EnglishIsImportant/Listening/eii_l01_p09.mp3", "Assets/Audio/2B/1_EnglishIsImportant/Listening/Slow/eii_l01_p09_slow.mp3"),
            ("In other words...", ListeningL01Function.AnotherWay, "Assets/Audio/2B/1_EnglishIsImportant/Listening/eii_l01_p10.mp3", "Assets/Audio/2B/1_EnglishIsImportant/Listening/Slow/eii_l01_p10_slow.mp3")
        };

        for (int i = 0; i < 10; i++) {
            roundDataArray[i] = new ListeningL01RoundData {
                phraseText = samples[i].phrase,
                correctFunction = samples[i].func,
#if UNITY_EDITOR
                phraseAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(samples[i].audioPath),
                slowAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(samples[i].slowPath)
#endif
            };
        }
    }

    public void StartLesson() {
        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        EnsureLessonTitle();
        PlayCurrentRound();
    }

    private void PlayCurrentRound() {
        if (roundDataArray == null || roundDataArray.Length == 0) {
            Debug.LogWarning("[L01 Listening] roundDataArray is empty or unassigned.");
            return;
        }

        if (currentRoundIndex >= roundDataArray.Length) {
            CompleteLesson();
            return;
        }

        isProcessingInput = false;
        ResetChipVisuals();
        EnsureButtonLabels();
        EnsureLessonTitle();

        if (progressTMP != null) {
            progressTMP.text = $"{currentRoundIndex + 1}/{roundDataArray.Length}";
        }

        PlayPhraseAudio();
    }

    private void PlayPhraseAudio() {
        if (roundDataArray == null || currentRoundIndex >= roundDataArray.Length) return;
        ListeningL01RoundData round = roundDataArray[currentRoundIndex];
        AudioClip clipToPlay = (isSlowMode && round.slowAudioClip != null) ? round.slowAudioClip : round.phraseAudioClip;

        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
        }
    }

    private void ResetChipVisuals() {
        if (askToRepeatImg != null) askToRepeatImg.color = originalAskColor;
        if (checkThemImg != null) checkThemImg.color = originalCheckThemColor;
        if (checkMeImg != null) checkMeImg.color = originalCheckMeColor;
        if (anotherWayImg != null) anotherWayImg.color = originalAnotherWayColor;

        if (askToRepeatBtn != null) askToRepeatBtn.interactable = true;
        if (checkThemBtn != null) checkThemBtn.interactable = true;
        if (checkMeBtn != null) checkMeBtn.interactable = true;
        if (anotherWayBtn != null) anotherWayBtn.interactable = true;
    }

    private void OnFunctionSelected(ListeningL01Function selectedFunction, Button btn, Image img) {
        if (isProcessingInput) return;

        ListeningL01RoundData round = roundDataArray[currentRoundIndex];
        bool isCorrect = (selectedFunction == round.correctFunction);

        if (isCorrect) {
            isProcessingInput = true;
            score++;

            if (img != null) img.color = correctColor;
            if (btn != null) btn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(NextRoundRoutine());
        } else {
            if (img != null) img.color = wrongColor;
            if (btn != null) {
                btn.transform.DOShakePosition(0.4f, new Vector3(15f, 0f, 0f), 10, 90, false, true);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    private IEnumerator NextRoundRoutine() {
        yield return new WaitForSeconds(1.2f);
        currentRoundIndex++;
        PlayCurrentRound();
    }

    private void OnSlowToggleChanged(bool isOn) {
        isSlowMode = isOn;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        PlayPhraseAudio();
    }

    private void OnReplayAudioClicked() {
        PlayPhraseAudio();
    }

    private void OnRetryButtonClicked() {
        StartLesson();
    }

    private void CompleteLesson() {
        isProcessingInput = true;

        if (resultPanel != null) {
            resultPanel.SetActive(true);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"{score}/{roundDataArray.Length}";
        }

        bool passed = score >= 8;

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ? "GREAT JOB! LESSON COMPLETED!" : "TRY AGAIN TO PASS (8/10 REQUIRED)";
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            Debug.Log($"[L01 Listening] Passed with score {score}/{roundDataArray.Length}!");
        } else {
            Debug.Log($"[L01 Listening] Failed with score {score}/{roundDataArray.Length}. Retry available.");
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
