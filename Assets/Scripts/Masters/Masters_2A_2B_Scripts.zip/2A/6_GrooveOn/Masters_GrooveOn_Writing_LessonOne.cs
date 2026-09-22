using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controller for Unit 6 (Groove On) Writing Branch - Stage W01: Complete the Greeting.
/// 10 Cloze fill-in items across 4 categories (birthday, party question, festival, preparation).
/// Features case-insensitive answer validation, first-letter hint on retry, 8/10 success condition,
/// ARIA voiceover readback, dynamic UI cleanups of Unit 1 base elements, and full progress tracking.
/// </summary>
public class Masters_GrooveOn_Writing_LessonOne : Masters_PolishedCommunication_Writing_LessonOne {

    [System.Serializable]
    public class W01GreetingItem {
        public string template;       // e.g. "Wish you a very ________!"
        public string acceptedAnswer; // e.g. "birthday"
        public string hintCategory;   // e.g. "birthday"
        public string readbackText;   // e.g. "Wish you a very happy birthday!"
        public string audioFileName;  // e.g. "Wish you a very happy birthday.mp3"
    }

    [Header("W01 Greeting Data (10 Items)")]
    [SerializeField] private W01GreetingItem[] greetingItems;

    [Header("UI Overrides & Feedback")]
    [SerializeField] private TextMeshProUGUI categoryHintTMP;
    [SerializeField] private TextMeshProUGUI feedbackTMP;
    [SerializeField] private TMP_InputField writingInputField;
    [SerializeField] private TextMeshProUGUI progressTMP;

    private int currentItemIndex = 0;
    private int correctCount = 0;
    private int attemptsOnCurrentItem = 0;
    private bool isCheckingAnswer = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;
        narratorSpeech = null; // Clear base narrator audio to prevent double audio overlap
        Initialize10ItemsIfEmpty();
        UpdateTitleAndUIComponents();
        HideIrrelevantUnit1UIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Writing;
        Initialize10ItemsIfEmpty();
        UpdateTitleAndUIComponents();
        SetupUIBindings();

        currentItemIndex = 0;
        correctCount = 0;

        // Stop any playing voiceover first and play ONLY the correct VO_W01_ARIA intro audio
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        PlayIntroVoiceover();

        LoadItem(0);
    }

    private void Initialize10ItemsIfEmpty() {
        if (greetingItems != null && greetingItems.Length >= 10) return;

        greetingItems = new W01GreetingItem[] {
            new W01GreetingItem {
                template = "Wish you a very ________!",
                acceptedAnswer = "birthday",
                hintCategory = "birthday",
                readbackText = "Wish you a very happy birthday!",
                audioFileName = "Wish you a very happy birthday.mp3"
            },
            new W01GreetingItem {
                template = "Many more happy ________ of the day!",
                acceptedAnswer = "returns",
                hintCategory = "birthday",
                readbackText = "Many more happy returns of the day!",
                audioFileName = "Many more happy returns of the day.mp3"
            },
            new W01GreetingItem {
                template = "______ birthday wishes! (the day has passed)",
                acceptedAnswer = "Belated",
                hintCategory = "birthday",
                readbackText = "Belated birthday wishes!",
                audioFileName = "Belated birthday wishes.mp3"
            },
            new W01GreetingItem {
                template = "Where's the ________? (party question)",
                acceptedAnswer = "party",
                hintCategory = "party question",
                readbackText = "Where's the party?",
                audioFileName = "Wheres the party.mp3"
            },
            new W01GreetingItem {
                template = "What about the ________? (party question)",
                acceptedAnswer = "theme",
                hintCategory = "party question",
                readbackText = "What about the theme?",
                audioFileName = "What about the theme.mp3"
            },
            new W01GreetingItem {
                template = "Wish you a Happy ________! (festival of lights)",
                acceptedAnswer = "Diwali",
                hintCategory = "festival",
                readbackText = "Wish you a Happy Diwali!",
                audioFileName = "Wish you a Happy Diwali.mp3"
            },
            new W01GreetingItem {
                template = "Eid ________! (festival)",
                acceptedAnswer = "Mubarak",
                hintCategory = "festival",
                readbackText = "Eid Mubarak!",
                audioFileName = "Eid Mubarak.mp3"
            },
            new W01GreetingItem {
                template = "______ Christmas to you! (festival)",
                acceptedAnswer = "Merry",
                hintCategory = "festival",
                readbackText = "Merry Christmas to you!",
                audioFileName = "Merry Christmas to you.mp3"
            },
            new W01GreetingItem {
                template = "Joy and ________ this Diwali! (festival)",
                acceptedAnswer = "prosperity",
                hintCategory = "festival",
                readbackText = "Joy and prosperity this Diwali!",
                audioFileName = "Joy and prosperity this Diwali.mp3"
            },
            new W01GreetingItem {
                template = "______ the house. (before decorating)",
                acceptedAnswer = "Clean",
                hintCategory = "preparation",
                readbackText = "Clean the house.",
                audioFileName = "Clean the house.mp3"
            }
        };
    }

    private void PlayIntroVoiceover() {
        if (Masters_AudioManager.Instance == null) return;
        AudioClip introClip = null;
#if UNITY_EDITOR
            introClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Writing/Fill the missing word complete the greeting.mp3");
        #endif
        if (introClip == null) {
            #if UNITY_EDITOR
            introClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Writing/Fill in the blank with the appropriate celebration word.mp3");
            #endif
        }
        if (introClip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introClip);
        }
    }

    private void SetupUIBindings() {
        if (writingInputField == null) {
            writingInputField = GetComponentInChildren<TMP_InputField>(true);
        }

        // Deactivate any Check/Submit button in prefab hierarchy
        Button[] btns = GetComponentsInChildren<Button>(true);
        foreach (var b in btns) {
            if (b == null) continue;
            string bName = b.name.ToLower();
            if (bName.Equals("check") || bName.Contains("checkbutton") || bName.Contains("submit")) {
                b.gameObject.SetActive(false);
            }
        }

        Transform checkTrans = transform.Find("Check");
        if (checkTrans != null) {
            checkTrans.gameObject.SetActive(false);
        }

        if (writingInputField != null) {
            writingInputField.onSubmit.RemoveAllListeners();
            writingInputField.onSubmit.AddListener(text => OnCheckSubmitted());
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";

            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("REWRITE") || textVal.Contains("CHANGE THE TONE") || textVal.Contains("Polished") || textVal.Contains("W01") || textVal.Contains("Arrange") || textVal.Contains("Word Order") || textVal.Contains("Complete")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "W01 Complete the Greeting";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("WRITING")) {
                tmp.text = "WRITING BRANCH (Word Craft)";
            }
            if (textVal.Contains("Target Tone") || textVal.Contains("Tone:") || lowerName.Contains("tone")) {
                tmp.gameObject.SetActive(false);
            }
        }

        HideIrrelevantUnit1UIComponents();
    }

    private void HideIrrelevantUnit1UIComponents() {
        // Hide old Unit 1 tone labels
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string nameLower = tmp.name.ToLower();
            string textVal = tmp.text ?? "";
            if (nameLower.Contains("tone") || textVal.Contains("Tone:") || textVal.Contains("REWRITE THE MESSAGE") || textVal.Contains("CHANGE THE TONE") || textVal.Contains("Target Tone")) {
                tmp.gameObject.SetActive(false);
            }
        }

        // Hide old 4-choice option buttons from Unit 1 base prefab
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons) {
            if (btn == null) continue;
            if (btn == nextButton) continue;

            string bName = btn.name.ToLower();
            TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>(true);
            string tVal = btnText != null ? (btnText.text ?? "") : "";

            if (bName.Contains("option") || bName.Contains("choice") || tVal.Contains("Hey") || tVal.Contains("Hello") || tVal.Contains("What") || tVal.Contains("How")) {
                btn.gameObject.SetActive(false);
            }
        }
    }

    private void LoadItem(int index) {
        if (greetingItems == null || index < 0 || index >= greetingItems.Length) {
            EvaluateLessonCompletion();
            return;
        }

        currentItemIndex = index;
        attemptsOnCurrentItem = 0;
        isCheckingAnswer = false;

        W01GreetingItem item = greetingItems[index];

        HideIrrelevantUnit1UIComponents();

        // Update Prompt / Greeting Card text
        TMP_Text prompt = GetPromptTMP();
        if (prompt != null) {
            prompt.gameObject.SetActive(true);
            prompt.text = item.template;
        }

        // Enable Hint Panel and update Hint Text
        if (hintPanel != null) {
            hintPanel.SetActive(true);
        }

        SetHintText($"Hint: {item.hintCategory}");

        // Update Feedback text
        if (feedbackTMP != null) {
            feedbackTMP.text = "";
            feedbackTMP.gameObject.SetActive(false);
        }

        // Update Progress Counter
        TMP_Text progress = GetProgressTMP();
        if (progress != null) {
            progress.gameObject.SetActive(true);
            progress.text = $"{index + 1} / {greetingItems.Length}";
        }

        // Reset Input Field
        if (writingInputField != null) {
            writingInputField.gameObject.SetActive(true);
            writingInputField.text = "";
            writingInputField.interactable = true;

            TMP_Text placeholder = writingInputField.placeholder as TMP_Text;
            if (placeholder != null) {
                placeholder.text = "Type missing word here...";
            }

            writingInputField.Select();
            writingInputField.ActivateInputField();
        }

        Transform checkBtn = transform.Find("Check");
        if (checkBtn != null) {
            checkBtn.gameObject.SetActive(false);
        }
    }

    private void SetHintText(string text) {
        if (hintTMP != null) {
            hintTMP.gameObject.SetActive(true);
            hintTMP.text = text;
        }
        if (categoryHintTMP != null) {
            categoryHintTMP.gameObject.SetActive(true);
            categoryHintTMP.text = text;
        }
        TMP_Text fallbackHint = GetHintTMP();
        if (fallbackHint != null && fallbackHint != hintTMP && fallbackHint != categoryHintTMP) {
            fallbackHint.gameObject.SetActive(true);
            fallbackHint.text = text;
        }
    }

    private TMP_Text GetPromptTMP() {
        if (promptTMP != null && promptTMP.gameObject != null) return promptTMP;
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) {
            if (t == null) continue;
            string n = t.name.ToLower();
            string txt = t.text ?? "";
            if (n.Contains("questions text") || n.Contains("prompt") || n.Contains("card") || n.Contains("greeting") || txt.Contains("Hello friend") || txt.Contains("________")) return t;
        }
        return promptTMP;
    }

    private TMP_Text GetHintTMP() {
        if (hintTMP != null) return hintTMP;
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) {
            if (t == null) continue;
            string n = t.name.ToLower();
            if (n.Contains("hint") || n.Contains("category") || n.Contains("message") || n.Contains("sub")) return t;
        }
        return null;
    }

    private TMP_Text GetProgressTMP() {
        if (progressCountTMP != null) return progressCountTMP;
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps) {
            if (t == null) continue;
            string n = t.name.ToLower();
            string txt = t.text ?? "";
            if (n.Contains("progression count") || n.Contains("progress") || n.Contains("count") || txt.Contains("0/4") || txt.Contains("/")) return t;
        }
        return progressCountTMP;
    }

    public void OnCheckSubmitted() {
        if (isCheckingAnswer || greetingItems == null || currentItemIndex >= greetingItems.Length) return;
        if (writingInputField == null) return;

        string userInput = writingInputField.text.Trim();
        if (string.IsNullOrEmpty(userInput)) return;

        W01GreetingItem currentItem = greetingItems[currentItemIndex];

        // Soft Key SFX on submit
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        // Case-insensitive, space-trimmed comparison
        bool isCorrect = CheckAnswerMatch(userInput, currentItem.acceptedAnswer);

        if (isCorrect) {
            StartCoroutine(HandleCorrectAnswer(currentItem));
        } else {
            HandleWrongAnswer(currentItem);
        }
    }

    private bool CheckAnswerMatch(string input, string target) {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(target)) return false;

        string cleanInput = input.Trim().ToLowerInvariant();
        string cleanTarget = target.Trim().ToLowerInvariant();

        if (cleanInput.Equals(cleanTarget)) return true;

        // Strip punctuation if student typed punctuation
        cleanInput = System.Text.RegularExpressions.Regex.Replace(cleanInput, @"[^\w]", "");
        cleanTarget = System.Text.RegularExpressions.Regex.Replace(cleanTarget, @"[^\w]", "");

        return cleanInput.Equals(cleanTarget);
    }

    private IEnumerator HandleCorrectAnswer(W01GreetingItem item) {
        isCheckingAnswer = true;
        correctCount++;

        if (writingInputField != null) writingInputField.interactable = false;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        // Fill the blank with the correct word and highlight
        TMP_Text prompt = GetPromptTMP();
        if (prompt != null) {
            string completedText = FormatCompletedGreeting(item.template, item.acceptedAnswer);
            prompt.text = completedText;
            prompt.transform.DOKill();
            prompt.transform.DOPunchScale(Vector3.one * 0.14f, 0.35f);
        }

        // Visual feedback on input field
        Image bg = writingInputField != null ? writingInputField.GetComponent<Image>() : null;
        if (bg != null) {
            bg.DOColor(Color.green, 0.3f);
        }

        // Play ARIA readback audio
        PlayReadbackAudio(item);

        yield return new WaitForSeconds(2.0f);

        // Revert input background color
        if (bg != null) {
            bg.DOColor(Color.white, 0.2f);
        }

        currentItemIndex++;
        if (currentItemIndex < greetingItems.Length) {
            LoadItem(currentItemIndex);
        } else {
            EvaluateLessonCompletion();
        }
    }

    private string FormatCompletedGreeting(string template, string answer) {
        if (string.IsNullOrEmpty(template)) return answer;
        string highlightedAnswer = $"<color=#22C55E><b>{answer}</b></color>";
        if (template.Contains("________")) return template.Replace("________", highlightedAnswer);
        if (template.Contains("______")) return template.Replace("______", highlightedAnswer);
        return $"{template} {highlightedAnswer}";
    }

    private void PlayReadbackAudio(W01GreetingItem item) {
        if (Masters_AudioManager.Instance == null || item == null) return;
        string path = $"Assets/Audio/2A/6_GrooveOn/Writing/{item.audioFileName}";
        AudioClip clip = null;
#if UNITY_EDITOR
        clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        #endif
        if (clip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }
    }

    private void HandleWrongAnswer(W01GreetingItem item) {
        attemptsOnCurrentItem++;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        // Shake input field
        if (writingInputField != null) {
            writingInputField.transform.DOKill();
            writingInputField.transform.DOShakePosition(0.35f, new Vector3(12f, 0, 0));
        }

        // Generate First-Letter Hint
        string firstLetter = !string.IsNullOrEmpty(item.acceptedAnswer) ? item.acceptedAnswer[0].ToString().ToUpper() : "";
        string firstLetterHint = $"{firstLetter}...";

        if (hintPanel != null) {
            hintPanel.SetActive(true);
        }

        SetHintText($"Hint: {item.hintCategory} ({firstLetterHint})");

        if (feedbackTMP != null) {
            feedbackTMP.gameObject.SetActive(true);
            feedbackTMP.text = $"Try again! Hint: {firstLetterHint}";
        }

        // Clear input and focus for retry
        if (writingInputField != null) {
            writingInputField.text = "";
            writingInputField.Select();
            writingInputField.ActivateInputField();
        }
    }

    private void EvaluateLessonCompletion() {
        Debug.Log($"[W01 Complete the Greeting] Activity finished! Score: {correctCount}/{greetingItems.Length}");

        if (correctCount >= 8) {
            // Success Condition: 8/10 or higher
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            if (nextButton == null) {
                Transform nbTrans = transform.Find("NextButton") ?? transform.Find("Next Button") ?? transform.Find("Next");
                if (nbTrans != null) nextButton = nbTrans.GetComponent<Button>();
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
                NextButtonAnimation();
            }
        } else {
            // Retry flow if < 8 correct
            Debug.LogWarning($"[W01] Score {correctCount}/10 is below 8/10 requirement. Restarting activity.");
            RestartActivity();
        }
    }

    public void RestartActivity() {
        currentItemIndex = 0;
        correctCount = 0;
        attemptsOnCurrentItem = 0;
        isCheckingAnswer = false;
        LoadItem(0);
    }

}