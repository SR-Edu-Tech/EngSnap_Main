using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unit 6 Colour Your Speech — Listening L01 Controller.
/// Hear the Idiom — What Does It Mean?
/// Complete Visual UI implementation matching Screenshot 2:
/// 1. Native Unit 6 background (Lesson Bg night.png) filling screen at 100% full opacity.
/// 2. Top-Left: Back Button.
/// 3. Top-Center: Title "HEAR THE IDIOM — WHAT DOES IT MEAN?".
/// 4. Top-Right: Score / Count display "0 / 6".
/// 5. ARIA Intro sequence: reads intro line before revealing questions.
/// 6. Large horizontal rounded blue Question Panel with integrated Speaker Button on the right.
/// 7. Exactly 3 large rounded blue meaning cards: Card 1 (Left), Card 2 (Right), Card 3 (Bottom-Center).
/// 8. Visible, functional Slow and Repeat this controls centered at bottom below Card 3.
/// 9. Next Button in Bottom-Right (enables after lesson completion).
/// 10. 6 rounds, 5/6 pass threshold, score tracking, answer retry, return to Unit 6 Hub.
/// </summary>
public class Masters_ColourYourSpeech_Listening_LessonOne : Masters_Lesson {

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [System.Serializable]
    public class IdiomListeningRound {
        public string spokenSentence;
        public string correctMeaning;
        public string distractor1;
        public string distractor2;
        public AudioClip sentenceAudio;
    }

    [Header("Lesson Data (6 Rounds)")]
    [SerializeField] private IdiomListeningRound[] idiomRounds = new IdiomListeningRound[] {
        new IdiomListeningRound {
            spokenSentence = "\"He was on cloud nine when he heard the good news.\"",
            correctMeaning = "In a state of happiness or bliss or extreme excitement.",
            distractor1 = "High up in the sky near the clouds.",
            distractor2 = "Forgetting what happened yesterday."
        },
        new IdiomListeningRound {
            spokenSentence = "\"The exam paper was a piece of cake.\"",
            correctMeaning = "Very easy.",
            distractor1 = "Made out of sweet dessert.",
            distractor2 = "Very difficult and confusing."
        },
        new IdiomListeningRound {
            spokenSentence = "\"If I'm late, my dad will go bananas.\"",
            correctMeaning = "Becoming crazy / losing your temper.",
            distractor1 = "Buying yellow fruit at the market.",
            distractor2 = "Going to sleep early."
        },
        new IdiomListeningRound {
            spokenSentence = "\"You can be such a silly fruitcake sometimes.\"",
            correctMeaning = "Really strange or crazy.",
            distractor1 = "A delicious baked cake with dried fruits.",
            distractor2 = "Very smart and serious."
        },
        new IdiomListeningRound {
            spokenSentence = "\"Moms are always full of sugar and spice.\"",
            correctMeaning = "Behaving in a kind and friendly way; very sweet and nice.",
            distractor1 = "Cooking with sweet seasonings in the kitchen.",
            distractor2 = "Shouting loudly when angry."
        },
        new IdiomListeningRound {
            spokenSentence = "\"He made some tongue-in-cheek comment about his teammates.\"",
            correctMeaning = "Saying something as a joke, not to be taken seriously.",
            distractor1 = "Holding your tongue with your hand while speaking.",
            distractor2 = "Eating food without chewing properly."
        }
    };

    [Header("Audio Clips")]
    [SerializeField] private AudioClip voAriaIntro;

    [Header("Settings")]
    [SerializeField] private int passThreshold = 5;

    // Auto-bound runtime UI references (automatically discovered by FindAndSetupAllUIElements)
    private Transform questionPanelTransform;
    private TextMeshProUGUI lessonTitleTMP;
    private TextMeshProUGUI progressTMP;
    private TextMeshProUGUI scoreTMP;
    private TextMeshProUGUI sentenceTMP;
    private Button audioReplayButton;
    private Button backButton;
    private Button[] optionButtons;
    private GameObject controlsContainer;
    private Toggle slowToggle;
    private Toggle repeatToggle;

    private int currentRoundIndex = 0;
    private int correctScore = 0;
    private int correctOptionIndex = 0;
    private bool isAnswering = false;
    private bool isSlowMode = false;
    private bool isIntroActive = true;
    private bool isStartupComplete = false;
    private Coroutine startupCoroutine;

    protected override void Awake() {
        topic = Masters_Topic.Listening;

        AutoLoadAudioClips();
        FindAndSetupAllUIElements();
    }

    private void AutoLoadAudioClips() {
#if UNITY_EDITOR
        string folder = "Assets/Audio/3A/6_ColourYourSpeech/Listening/";

        if (voAriaIntro == null) {
            voAriaIntro = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(folder + "'Listen for the idiom - what does it really mean'.mp3");
        }

        string[] fileNames = new string[] {
            "He was on cloud nine when he heard the good news.mp3",
            "The exam paper was a piece of cake.mp3",
            "If I'm late my dad will go bananas.mp3",
            "You can be such a silly fruitcake sometimes.mp3",
            "Moms are always full of sugar and spice.mp3",
            "He made some tongue-in-cheek comment about his teammates.mp3"
        };

        if (idiomRounds != null) {
            for (int i = 0; i < idiomRounds.Length && i < fileNames.Length; i++) {
                if (idiomRounds[i] != null && idiomRounds[i].sentenceAudio == null) {
                    idiomRounds[i].sentenceAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(folder + fileNames[i]);
                }
            }
        }
#endif
    }

    private void DisableTypeWriters(Transform root) {
        if (root == null) return;
        Masters_TextTypeWriter[] tws = root.GetComponentsInChildren<Masters_TextTypeWriter>(true);
        if (tws != null) {
            foreach (var tw in tws) {
                tw.enabled = false;
            }
        }
    }

    private void ApplyText(TMP_Text tmp, string text, bool autoSize = false, float minSize = 14f, float maxSize = 28f, bool enableWordWrapping = true) {
        if (tmp == null) return;
        tmp.gameObject.SetActive(true);
        tmp.DOKill(true);
        tmp.maxVisibleCharacters = int.MaxValue;
        tmp.enableWordWrapping = enableWordWrapping;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.enableAutoSizing = autoSize;
        if (autoSize) {
            tmp.fontSizeMin = minSize;
            tmp.fontSizeMax = maxSize;
        }
        tmp.text = text;
        tmp.ForceMeshUpdate();
    }

    private void FindAndSetupAllUIElements() {
        // 0. Ensure root RectTransform stretches to LessonCanvas
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect != null) {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.localScale = Vector3.one;
            rootRect.localPosition = Vector3.zero;
        }

        // 1. Setup Background (100% full screen, native Lesson Bg night sprite)
        Transform bgTrans = transform.Find("Background");
        if (bgTrans == null) bgTrans = FindChildRecursive(transform, "Background");
        if (bgTrans != null) {
            bgTrans.gameObject.SetActive(true);
            bgTrans.SetAsFirstSibling();
            Image bgImg = bgTrans.GetComponent<Image>();
            if (bgImg != null) {
                bgImg.enabled = true;
                bgImg.color = Color.white;
                bgImg.raycastTarget = false;
#if UNITY_EDITOR
                if (bgImg.sprite == null || !bgImg.sprite.name.Contains("Lesson Bg night")) {
                    bgImg.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Common/Lesson Bg night.png");
                }
#endif
            }
            RectTransform bgRect = bgTrans.GetComponent<RectTransform>();
            if (bgRect != null) {
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;
                bgRect.pivot = new Vector2(0.5f, 0.5f);
                bgRect.localScale = Vector3.one;
                bgRect.localPosition = Vector3.zero;
            }
        }

        // 2. Setup Darken Overlay
        Transform darkenTrans = transform.Find("Darken");
        if (darkenTrans == null) darkenTrans = FindChildRecursive(transform, "Darken");
        if (darkenTrans != null) {
            darkenTrans.gameObject.SetActive(true);
            darkenTrans.SetSiblingIndex(1);
            Image darkImg = darkenTrans.GetComponent<Image>();
            if (darkImg != null) {
                darkImg.enabled = true;
                darkImg.color = new Color(0f, 0f, 0f, 0.35f);
                darkImg.raycastTarget = false;
            }
            RectTransform darkRect = darkenTrans.GetComponent<RectTransform>();
            if (darkRect != null) {
                darkRect.anchorMin = Vector2.zero;
                darkRect.anchorMax = Vector2.one;
                darkRect.offsetMin = Vector2.zero;
                darkRect.offsetMax = Vector2.zero;
                darkRect.pivot = new Vector2(0.5f, 0.5f);
                darkRect.localScale = Vector3.one;
                darkRect.localPosition = Vector3.zero;
            }
        }

        // 3. Setup Back Button
        Transform backBtnTrans = transform.Find("BackButton");
        if (backBtnTrans == null) backBtnTrans = FindChildRecursive(transform, "BackButton");
        if (backBtnTrans != null) {
            backBtnTrans.gameObject.SetActive(true);
            backButton = backBtnTrans.GetComponentInChildren<Button>(true);
            if (backButton != null) {
                // If BackButton already has the standard Masters_BackButton component, let it handle navigation to avoid duplicate callbacks
                Masters_BackButton mastersBack = backBtnTrans.GetComponentInChildren<Masters_BackButton>(true);
                if (mastersBack == null) {
                    backButton.onClick.RemoveAllListeners();
                    backButton.onClick.AddListener(OnBackButtonClicked);
                }
            }
        }

        // 4. Setup Header Title
        Transform titleTrans = transform.Find("LessonTitle");
        if (titleTrans == null) titleTrans = FindChildRecursive(transform, "LessonTitle");
        if (titleTrans != null) {
            titleTrans.gameObject.SetActive(true);
            lessonTitleTMP = titleTrans.GetComponentInChildren<TextMeshProUGUI>(true);
            if (lessonTitleTMP != null) {
                lessonTitleTMP.gameObject.SetActive(true);
                lessonTitleTMP.enableWordWrapping = false;
                lessonTitleTMP.overflowMode = TextOverflowModes.Overflow;
                lessonTitleTMP.text = "Hear the Idiom \u2014 What Does It Mean?";
                lessonTitleTMP.ForceMeshUpdate();
            }
        }

        // 5. Setup Score / Progress Display
        Transform scoreTrans = transform.Find("ExpressionCountTMP");
        if (scoreTrans == null) scoreTrans = FindChildRecursive(transform, "ExpressionCountTMP");
        if (scoreTrans == null) scoreTrans = FindChildRecursive(transform, "ScoreText");
        if (scoreTrans != null) {
            scoreTrans.gameObject.SetActive(true);
            DisableTypeWriters(scoreTrans);
            scoreTMP = scoreTrans.GetComponent<TextMeshProUGUI>();
            progressTMP = scoreTMP;
            if (scoreTMP != null) {
                ApplyText(scoreTMP, $"1/{idiomRounds.Length}", false);
            }
        }

        // 6. Setup Container (PhraseCardsGrid)
        Transform container = transform.Find("PhraseCardsGrid");
        if (container == null) container = FindChildRecursive(transform, "PhraseCardsGrid");
        if (container != null) {
            container.gameObject.SetActive(true);
        }

        // 7. Setup Question Panel (OptionButton)
        Transform qPanel = container != null ? container.Find("OptionButton") : transform.Find("OptionButton");
        if (qPanel == null) qPanel = FindChildRecursive(transform, "OptionButton");

        if (qPanel != null) {
            questionPanelTransform = qPanel;
            qPanel.gameObject.SetActive(true);
            DisableTypeWriters(qPanel);

            Button qBtn = qPanel.GetComponent<Button>();
            if (qBtn != null) qBtn.enabled = false;

            sentenceTMP = qPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (sentenceTMP != null) {
                sentenceTMP.gameObject.SetActive(true);
                sentenceTMP.maxVisibleCharacters = int.MaxValue;
                sentenceTMP.enableWordWrapping = true;
                sentenceTMP.overflowMode = TextOverflowModes.Overflow;
            }

            // Speaker button on Question Panel
            Transform replayTrans = qPanel.Find("SpeakerIcon");
            if (replayTrans == null) replayTrans = FindChildRecursive(qPanel, "SpeakerIcon");
            if (replayTrans != null) {
                audioReplayButton = replayTrans.GetComponent<Button>();
                if (audioReplayButton == null) audioReplayButton = replayTrans.gameObject.AddComponent<Button>();

                replayTrans.gameObject.SetActive(true);
                audioReplayButton.interactable = true;
                audioReplayButton.onClick.RemoveAllListeners();
                audioReplayButton.onClick.AddListener(OnReplayButtonClicked);
            }
        }

        // 8. Setup Three Meaning Cards (OptionButton (1), (2), (3))
        List<Button> cardsList = new List<Button>();
        string[] cardNames = new string[] { "OptionButton (1)", "OptionButton (2)", "OptionButton (3)" };

        for (int i = 0; i < cardNames.Length; i++) {
            Transform btnTrans = container != null ? container.Find(cardNames[i]) : transform.Find(cardNames[i]);
            if (btnTrans == null) btnTrans = FindChildRecursive(transform, cardNames[i]);
            if (btnTrans != null) {
                btnTrans.gameObject.SetActive(true);
                DisableTypeWriters(btnTrans);

                Button btn = btnTrans.GetComponent<Button>();
                if (btn != null) {
                    int cardIdx = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnOptionSelected(cardIdx));
                    cardsList.Add(btn);
                }
            }
        }

        optionButtons = cardsList.ToArray();

        // Hide OptionButton (4..7)
        for (int i = 4; i <= 7; i++) {
            string unusedName = $"OptionButton ({i})";
            Transform unusedTrans = container != null ? container.Find(unusedName) : transform.Find(unusedName);
            if (unusedTrans == null) unusedTrans = FindChildRecursive(transform, unusedName);
            if (unusedTrans != null) {
                unusedTrans.gameObject.SetActive(false);
            }
        }

        // 9. Setup Controls (Slow & Repeat this)
        Transform controlsTrans = container != null ? container.Find("Controls") : transform.Find("Controls");
        if (controlsTrans == null) controlsTrans = FindChildRecursive(transform, "Controls");
        if (controlsTrans != null) {
            controlsContainer = controlsTrans.gameObject;
            controlsContainer.SetActive(true);

            Transform slowT = controlsTrans.Find("SlowToggle");
            if (slowT == null) slowT = FindChildRecursive(controlsTrans, "SlowToggle");
            if (slowT != null) {
                slowToggle = slowT.GetComponent<Toggle>();
                slowT.gameObject.SetActive(true);
                if (slowToggle != null) {
                    slowToggle.onValueChanged.RemoveAllListeners();
                    slowToggle.onValueChanged.AddListener(OnSlowToggleChanged);
                }
            }

            Transform repT = controlsTrans.Find("RepeatThisToggle");
            if (repT == null) repT = FindChildRecursive(controlsTrans, "RepeatThisToggle");
            if (repT != null) {
                repeatToggle = repT.GetComponent<Toggle>();
                repT.gameObject.SetActive(true);
                if (repeatToggle != null) {
                    repeatToggle.onValueChanged.RemoveAllListeners();
                    repeatToggle.onValueChanged.AddListener(OnRepeatToggleChanged);
                }
            }
        }

        // 10. Setup Next Button
        if (nextButton == null) {
            Transform nextBtnTrans = transform.Find("NextButton");
            if (nextBtnTrans == null) nextBtnTrans = FindChildRecursive(transform, "NextButton");
            if (nextBtnTrans != null) nextButton = nextBtnTrans.GetComponentInChildren<Button>(true);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    protected override void Start() {
        if (startupCoroutine != null) {
            StopCoroutine(startupCoroutine);
            startupCoroutine = null;
        }
        startupCoroutine = StartCoroutine(StartupSequenceRoutine());
    }

    private void OnDisable() {
        if (startupCoroutine != null) {
            StopCoroutine(startupCoroutine);
            startupCoroutine = null;
        }

        if (nextButton != null) {
            nextButton.transform.DOKill();
        }
    }

    private IEnumerator StartupSequenceRoutine() {
        isIntroActive = true;
        isStartupComplete = false;
        currentRoundIndex = 0;
        correctScore = 0;

        // Display title and progress UI; hide questions, answer cards, and controls initially
        SetQuestionVisibility(false);
        UpdateScoreDisplay();

        // Play ARIA / title intro audio completely and wait for it to finish
        AudioClip introClip = voAriaIntro != null ? voAriaIntro : narratorSpeech;
        if (introClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introClip);
            yield return null;
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        }

        // Brief delay after intro VO completes for smooth pacing
        yield return new WaitForSeconds(0.2f);

        isIntroActive = false;
        isStartupComplete = true;

        // Reveal question, answer cards, and controls for Round 1
        SetQuestionVisibility(true);

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].transform.localScale = Vector3.one;
                }
            }
        }

        // Initialize Round 1 and play Round 1 idiom audio
        LoadRound(0);
        startupCoroutine = null;
    }

    private void UpdateScoreDisplay() {
        string displayStr = $"{Mathf.Min(currentRoundIndex + 1, idiomRounds.Length)}/{idiomRounds.Length}";
        if (progressTMP != null) {
            ApplyText(progressTMP, displayStr, false);
        } else if (scoreTMP != null) {
            ApplyText(scoreTMP, displayStr, false);
        }
    }

    private void SetQuestionVisibility(bool visible) {
        if (questionPanelTransform != null) {
            questionPanelTransform.gameObject.SetActive(visible);
        }

        if (controlsContainer != null) {
            controlsContainer.SetActive(visible);
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].gameObject.SetActive(visible);
                }
            }
        }
    }

    private void LoadRound(int roundIndex) {
        if (idiomRounds == null || roundIndex >= idiomRounds.Length) {
            OnAllRoundsCompleted();
            return;
        }

        currentRoundIndex = roundIndex;
        isAnswering = false;
        UpdateScoreDisplay();

        IdiomListeningRound round = idiomRounds[currentRoundIndex];

        if (sentenceTMP != null) {
            ApplyText(sentenceTMP, round.spokenSentence, true, 16f, 28f);
        }

        SetupMeaningCards(round);

        PlayRoundAudio();
    }

    private void SetupMeaningCards(IdiomListeningRound round) {
        if (optionButtons == null || optionButtons.Length < 3) return;

        List<string> choices = new List<string> {
            round.correctMeaning,
            round.distractor1,
            round.distractor2
        };

        // Shuffle choices for random card positions
        for (int i = 0; i < choices.Count; i++) {
            string temp = choices[i];
            int randomIndex = Random.Range(i, choices.Count);
            choices[i] = choices[randomIndex];
            choices[randomIndex] = temp;
        }

        correctOptionIndex = choices.IndexOf(round.correctMeaning);

        for (int i = 0; i < optionButtons.Length && i < 3; i++) {
            if (optionButtons[i] != null) {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;
                optionButtons[i].transform.DOKill();
                optionButtons[i].transform.localScale = Vector3.one;

                SetButtonTextAndLayout(optionButtons[i], choices[i]);
            }
        }
    }

    private void SetButtonTextAndLayout(Button btn, string cardText) {
        if (btn == null) return;

        DisableTypeWriters(btn.transform);

        Transform childSpeaker = btn.transform.Find("SpeakerIcon");
        if (childSpeaker != null) {
            childSpeaker.gameObject.SetActive(false);
        }

        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) {
            ApplyText(tmp, cardText, true, 14f, 26f);
        } else {
            Text legacy = btn.GetComponentInChildren<Text>(true);
            if (legacy != null) {
                legacy.gameObject.SetActive(true);
                legacy.text = cardText;
            }
        }
    }

    public void OnSlowToggleChanged(bool isOn) {
        if (isIntroActive || !isStartupComplete) return;
        isSlowMode = isOn;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        PlayRoundAudio();
    }

    public void OnRepeatToggleChanged(bool isOn) {
        if (isIntroActive || !isStartupComplete) return;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        PlayRoundAudio();
    }

    public void OnReplayButtonClicked() {
        if (isIntroActive || !isStartupComplete) return;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (audioReplayButton != null) {
            audioReplayButton.transform.DOKill(true);
            audioReplayButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f);
        }

        PlayRoundAudio();
    }

    private void PlayRoundAudio() {
        if (isIntroActive || !isStartupComplete || idiomRounds == null || currentRoundIndex >= idiomRounds.Length) return;

        IdiomListeningRound round = idiomRounds[currentRoundIndex];
        if (round != null && round.sentenceAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(round.sentenceAudio);
        }
    }

    private void OnOptionSelected(int buttonIndex) {
        if (isIntroActive || !isStartupComplete || isAnswering || idiomRounds == null || currentRoundIndex >= idiomRounds.Length) return;

        if (buttonIndex == correctOptionIndex) {
            isAnswering = true;
            correctScore++;
            UpdateScoreDisplay();

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (optionButtons != null && buttonIndex < optionButtons.Length && optionButtons[buttonIndex] != null) {
                optionButtons[buttonIndex].transform.DOPunchScale(Vector3.one * 0.22f, 0.35f);
            }

            StartCoroutine(NextRoundRoutine());
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (optionButtons != null && buttonIndex < optionButtons.Length && optionButtons[buttonIndex] != null) {
                optionButtons[buttonIndex].transform.DOShakePosition(0.4f, new Vector3(12f, 0, 0));
                optionButtons[buttonIndex].interactable = false;
            }
        }
    }

    private IEnumerator NextRoundRoutine() {
        yield return new WaitForSeconds(1.2f);
        LoadRound(currentRoundIndex + 1);
    }

    private void OnAllRoundsCompleted() {
        UpdateScoreDisplay();

        if (correctScore >= passThreshold) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                NextButtonAnimation();
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        } else {
            // Restart from round 1 if under pass threshold
            currentRoundIndex = 0;
            correctScore = 0;
            UpdateScoreDisplay();
            LoadRound(0);
        }
    }

    private void OnBackButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        } else {
            gameObject.SetActive(false);
        }
    }

    protected override void OnNextButtonClicked() {
        if (nextButton != null) {
            nextButton.transform.DOKill();
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            M3A_U6_HubProgress.MarkComplete(M3A_U6_Branch.Listening);

            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Listening);
            }
        }
    }
}
