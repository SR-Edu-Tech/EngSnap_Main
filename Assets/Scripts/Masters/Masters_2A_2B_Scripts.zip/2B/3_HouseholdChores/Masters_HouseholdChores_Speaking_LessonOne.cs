using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Unit 3: Household Chores — Speaking Lesson One (SP01 Say the Job Out Loud).
/// 6 Speaking Prompts:
/// 1. CHORE SENTENCE (Sweep the floor)
/// 2. CHORE SENTENCE (Wash up the dishes)
/// 3. OFFER (Today I can help you with the cleaning of our house)
/// 4. PLAN (I will wash the car and do a bit of gardening, too)
/// 5. THANK (Thank you. I really don't know what I would have done without your help)
/// 6. HOME PRACTICE (Take the trash out and feed the dog)
/// 
/// Controls locked during intro audio; auto-unlocked once intro finishes.
/// </summary>
public class Masters_HouseholdChores_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    [Header("Household Chores Speaking Config")]
    [SerializeField] private AudioClip introAudio;
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;

    private bool isIntroPhase = true;
    private Coroutine introRoutine;

    public void SetSpeechToTextData(SpeechToText[] data) {
        speechToTextArray = data;
    }

    protected override void Awake() {
        topic = Masters_Topic.Speaking;
        EnsureReferencesBound();
        base.Awake();
        PopulateDefaultPrompts();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Speaking;
        EnsureReferencesBound();
        EnsureHeaderAndTitle();

        // Lock mic & controls during introduction
        isIntroPhase = true;
        SetMicInteractable(false);

        introRoutine = StartCoroutine(BeginLessonRoutine());
    }

    private IEnumerator BeginLessonRoutine() {
        AudioClip clipToPlay = introAudio != null ? introAudio : narratorSpeech;
        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            yield return new WaitForSeconds(clipToPlay.length + 0.3f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        isIntroPhase = false;
        SetMicInteractable(true);
    }

    private void SetMicInteractable(bool interactable) {
        var micBtn = GetComponentInChildren<Masters_ToggleToTalkButton>(true);
        if (micBtn != null) {
            var btn = micBtn.GetComponent<Button>();
            if (btn != null) btn.interactable = interactable;
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP != null) headerTMP.text = "HOUSEHOLD CHORES 🧹";
        if (titleTMP != null) {
            titleTMP.text = "SP01 Say the Job Out Loud";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f);
        }
        if (subtitleTMP != null) subtitleTMP.text = "Speak the required line aloud for each card, then compare with ARIA's model.";
    }

    protected override void OnNextButtonClicked() {
        if (isIntroPhase) {
            if (introRoutine != null) StopCoroutine(introRoutine);
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.StopVoiceOver();
            isIntroPhase = false;
            SetMicInteractable(true);
            return;
        }

        base.OnNextButtonClicked();
    }

    private void EnsureReferencesBound() {
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var baseType = typeof(Masters_PolishedCommunication_Speaking_LessonOne);

        // PhraseCardReference
        var cardRefField = baseType.GetField("phraseCardReferenceGameObject", flags);
        if (cardRefField != null && cardRefField.GetValue(this) == null) {
            Transform t = transform.Find("PhraseCardReference");
            if (t != null) cardRefField.SetValue(this, t.gameObject);
        }

        // PhraseCardSpawnPoint
        var spawnField = baseType.GetField("phraseCardSpawnPointRectTransform", flags);
        if (spawnField != null && spawnField.GetValue(this) == null) {
            Transform t = transform.Find("PhraseCardSpawnPoint");
            if (t != null) spawnField.SetValue(this, t.GetComponent<RectTransform>());
        }

        // DebugText
        var debugField = baseType.GetField("debugTMP", flags);
        if (debugField != null && debugField.GetValue(this) == null) {
            Transform t = transform.Find("DebugText") ?? transform.Find("DebugTMP");
            if (t != null) debugField.SetValue(this, t.GetComponent<TextMeshProUGUI>());
        }

        // ProgressCountTMP
        var progField = baseType.GetField("progressCountTMP", flags);
        if (progField != null && progField.GetValue(this) == null) {
            Transform t = transform.Find("ProgressCountTMP") ?? transform.Find("progression count");
            if (t != null) progField.SetValue(this, t.GetComponent<TextMeshProUGUI>());
        }

        // Skip Button
        var skipField = baseType.GetField("skipButton", flags);
        if (skipField != null && skipField.GetValue(this) == null) {
            Transform t = transform.Find("SkipButton") ?? transform.Find("Skip");
            if (t != null) skipField.SetValue(this, t.GetComponent<Button>());
        }

        // Continue Button
        var contField = baseType.GetField("continueButton", flags);
        if (contField != null && contField.GetValue(this) == null) {
            Transform t = transform.Find("Continue") ?? transform.Find("ContinueButton");
            if (t != null) contField.SetValue(this, t.GetComponent<Button>());
        }

        // Slider / ProgressBar
        var sliderField = baseType.GetField("progressBar", flags);
        if (sliderField != null && sliderField.GetValue(this) == null) {
            Slider s = GetComponentInChildren<Slider>(true);
            if (s != null) sliderField.SetValue(this, s);
        }

        // Headers
        if (headerTMP == null) {
            Transform t = transform.Find("HeaderContainer/HeaderTMP") ?? transform.Find("HeaderTMP");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP == null) {
            Transform t = transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("LessonTitle") ?? transform.Find("Title");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP == null) {
            Transform t = transform.Find("HeaderContainer/SubtitleTMP") ?? transform.Find("SubtitleTMP");
            if (t != null) subtitleTMP = t.GetComponent<TextMeshProUGUI>();
        }
    }

    public void PopulateDefaultPrompts() {
        if (speechToTextArray != null && speechToTextArray.Length > 0) return;

        string aDir = "Assets/Audio/2B/3_HouseholdChores/Speaking/";

        speechToTextArray = new SpeechToText[] {
            new SpeechToText {
                phraseCardText = "<b>CHORE SENTENCE</b>\nLook at the picture of a dusty room and say the job:\n<color=#FFD80D>\"Sweep the floor.\"</color>",
                speechDetectionText = new string[] { "sweep the floor", "sweep floor", "sweep", "dust the floor", "clean the floor" }
#if UNITY_EDITOR
                , statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_sp01_model1.mp3")
#endif
            },
            new SpeechToText {
                phraseCardText = "<b>CHORE SENTENCE</b>\nThe sink is full of plates. Say the job:\n<color=#FFD80D>\"Wash up the dishes.\"</color>",
                speechDetectionText = new string[] { "wash up the dishes", "wash the dishes", "wash up dishes", "clean the dishes", "dry the dishes" }
#if UNITY_EDITOR
                , statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_sp01_model2.mp3")
#endif
            },
            new SpeechToText {
                phraseCardText = "<b>OFFER TO HELP MOM</b>\nOffer to help Mom before the festival:\n<color=#FFD80D>\"Today I can help you with the cleaning of our house.\"</color>",
                speechDetectionText = new string[] { "today i can help you with the cleaning of our house", "i can help you with the cleaning of our house", "today i can help you with cleaning", "i can help you clean the house" }
#if UNITY_EDITOR
                , statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_sp01_model3.mp3")
#endif
            },
            new SpeechToText {
                phraseCardText = "<b>MY CHORE PLAN</b>\nSay two jobs you will do and when:\n<color=#FFD80D>\"I will wash the car and do a bit of gardening, too.\"</color>",
                speechDetectionText = new string[] { "i will wash the car and do a bit of gardening too", "i will wash the car and do gardening too", "i will wash the car", "do a bit of gardening" }
#if UNITY_EDITOR
                , statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_sp01_model4.mp3")
#endif
            },
            new SpeechToText {
                phraseCardText = "<b>THANK YOUR HELPER</b>\nThank someone who helped you clean:\n<color=#FFD80D>\"Thank you. I really don't know what I would have done without your help.\"</color>",
                speechDetectionText = new string[] { "thank you i really dont know what i would have done without your help", "thank you i really don't know what i would have done without your help", "thank you without your help", "i really don't know what i would have done" }
#if UNITY_EDITOR
                , statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_sp01_model5.mp3")
#endif
            },
            new SpeechToText {
                phraseCardText = "<b>HOME PRACTICE</b>\nSay your household chores in English:\n<color=#FFD80D>\"Take the trash out and feed the dog.\"</color>",
                speechDetectionText = new string[] { "take the trash out and feed the dog", "take the trash out", "feed the dog", "iron the clothes", "make the bed" }
#if UNITY_EDITOR
                , statementAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_sp01_model6.mp3")
#endif
            }
        };
    }
}
