using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core Writing 2 controller for Unit 3: Beyond the Horizon (Book 2A).
/// Subclasses PolishedCommunication_Writing_LessonTwo to inherit word-bank starter chips & NPC dialogue.
/// W02 — Write the Directions to a Place (across 3 map prompt rounds).
/// Manages map image sprite swapping per question (`mapImageDisplay` updated from `mapPrompts` / `mapSprites`).
/// Requires word-bank chip usage + at least 1 MOVEMENT phrase and 1 POSITION phrase to pass.
/// Displays helpful hint via dedicated `hintPanel`/`hintTMP` (or fallback to NPC bubble) after 2 wrong entries.
/// </summary>
public class Masters_BeyondTheHorizon_Writing_LessonTwo : Masters_PolishedCommunication_Writing_LessonTwo {

    [System.Serializable]
    public class MapPromptData {
        public string npcOfferText;
        public AudioClip npcOfferAudioClip;
        public string[] starterChipsText;
        public string[] validKeywords;
        public Sprite mapSprite;
        [TextArea(2, 4)]
        public string hintText;
    }

    [Header("Map Visuals & Prompts")]
    [SerializeField] private Image mapImageDisplay;
    [SerializeField] private MapPromptData[] mapPrompts;
    [SerializeField] private Sprite[] mapSprites; // Secondary fallback array if needed

    [Header("Hint UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintTMP;

    private int lastObservedIndex = -1;
    private int wrongAttempts = 0;
    private bool wasChipUsed = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;
        AutoFindUIElements();
        InitializeUnit3Prompts();
    }

    protected override void Start() {
        base.Start();
        // Rebind submit button to custom movement + position + chip validation
        Button submitBtn = GetSubmitButton();
        if (submitBtn != null) {
            submitBtn.onClick.RemoveAllListeners();
            submitBtn.onClick.AddListener(OnCustomSubmitClicked);
        }

        // Monitor chip button clicks to verify chip usage
        Button[] chipBtns = GetStarterChipButtons();
        if (chipBtns != null) {
            for (int i = 0; i < chipBtns.Length; i++) {
                int idx = i;
                if (chipBtns[idx] != null) {
                    chipBtns[idx].onClick.AddListener(() => { wasChipUsed = true; });
                }
            }
        }

        // Intercept base invoke and start custom prompt control + monitor
        CancelInvoke("StartFirstPrompt");
        Invoke(nameof(StartFirstPromptCustom), 1.0f);
        StartCoroutine(MonitorPromptIndexRoutine());
    }

    private void AutoFindUIElements() {
        if (mapImageDisplay == null) {
            Transform foundMap = FindChildRecursive(transform, "Image map");
            if (foundMap == null) foundMap = FindChildRecursive(transform, "ImageMap");
            if (foundMap == null) foundMap = FindChildRecursive(transform, "Map image");
            if (foundMap == null) foundMap = FindChildRecursive(transform, "MapImage");

            if (foundMap != null) {
                mapImageDisplay = foundMap.GetComponent<Image>();
                if (mapImageDisplay == null) mapImageDisplay = foundMap.GetComponentInChildren<Image>(true);
            }
        }

        if (hintPanel == null) {
            Transform foundHintPanel = FindChildRecursive(transform, "Hint Panel");
            if (foundHintPanel == null) foundHintPanel = FindChildRecursive(transform, "HintPanel");
            if (foundHintPanel == null) foundHintPanel = FindChildRecursive(transform, "Hint Box");
            if (foundHintPanel != null) hintPanel = foundHintPanel.gameObject;
        }

        if (hintTMP == null) {
            Transform foundHintTMP = FindChildRecursive(transform, "Hint TMP");
            if (foundHintTMP == null) foundHintTMP = FindChildRecursive(transform, "HintText");
            if (foundHintTMP == null && hintPanel != null) {
                hintTMP = hintPanel.GetComponentInChildren<TextMeshProUGUI>(true);
            } else if (foundHintTMP != null) {
                hintTMP = foundHintTMP.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name) {
        foreach (Transform child in parent) {
            if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void StartFirstPromptCustom() {
        wrongAttempts = 0;
        wasChipUsed = false;
        if (hintPanel != null) hintPanel.SetActive(false);
        else if (hintTMP != null) hintTMP.gameObject.SetActive(false);

        SetPromptIndex(0);
        LoadPromptSafe(0);
        UpdateMapImage(0);
    }

    private IEnumerator MonitorPromptIndexRoutine() {
        while (true) {
            int currentIdx = GetPromptIndex();
            if (currentIdx != lastObservedIndex && currentIdx >= 0) {
                lastObservedIndex = currentIdx;
                wrongAttempts = 0;
                wasChipUsed = false;
                if (hintPanel != null) hintPanel.SetActive(false);
                else if (hintTMP != null) hintTMP.gameObject.SetActive(false);

                UpdateMapImage(currentIdx);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void UpdateMapImage(int index) {
        if (mapImageDisplay == null) AutoFindUIElements();
        if (mapImageDisplay == null) return;

        Sprite targetSprite = null;

        // 1. Check mapPrompts array first
        if (mapPrompts != null && index >= 0 && index < mapPrompts.Length) {
            targetSprite = mapPrompts[index].mapSprite;
        }

        // 2. Fallback check to mapSprites array
        if (targetSprite == null && mapSprites != null && index >= 0 && index < mapSprites.Length) {
            targetSprite = mapSprites[index];
        }

        if (targetSprite != null) {
            mapImageDisplay.sprite = targetSprite;
            mapImageDisplay.enabled = true;
        } else {
            // Keep enabled if existing sprite or fallback
            if (mapImageDisplay.sprite == null) {
                mapImageDisplay.enabled = false;
            }
        }
    }

#if UNITY_EDITOR
    private void Reset() {
        AutoFindUIElements();
        InitializeUnit3Prompts();
    }

    private void OnValidate() {
        AutoFindUIElements();
        InitializeUnit3Prompts();
    }
#endif

    private void InitializeUnit3Prompts() {
        if (mapPrompts != null && mapPrompts.Length > 0 && mapPrompts[0].npcOfferText.Contains("Library") && !string.IsNullOrEmpty(mapPrompts[0].hintText)) {
            SyncBasePromptsFromMapPrompts();
            return;
        }

        string[] wordBankChips = new string[] {
            "Go straight...",
            "Turn left / right from the junction.",
            "It is beside...",
            "It's opposite to...",
            "The... is on your right / left."
        };

        mapPrompts = new MapPromptData[] {
            new MapPromptData {
                npcOfferText = "Write directions to the Library using the map!",
                npcOfferAudioClip = LoadAudio("Write directions to the Library using the map"),
                starterChipsText = wordBankChips,
                validKeywords = new string[] { "library" },
                mapSprite = (mapSprites != null && mapSprites.Length > 0) ? mapSprites[0] : null,
                hintText = "Hint: Use a movement phrase like 'Go straight' or 'Turn left', then add that it is 'beside the school'."
            },
            new MapPromptData {
                npcOfferText = "Write directions to the Admin Office using the map!",
                npcOfferAudioClip = LoadAudio("Write directions to the Admin Office using the map"),
                starterChipsText = wordBankChips,
                validKeywords = new string[] { "admin" },
                mapSprite = (mapSprites != null && mapSprites.Length > 1) ? mapSprites[1] : null,
                hintText = "Hint: Try using 'Go straight' and 'Turn right from the junction', then state that it is 'on your left'."
            },
            new MapPromptData {
                npcOfferText = "Write directions to the Clinic using the map!",
                npcOfferAudioClip = LoadAudio("Write directions to the Clinic using the map"),
                starterChipsText = wordBankChips,
                validKeywords = new string[] { "clinic" },
                mapSprite = (mapSprites != null && mapSprites.Length > 2) ? mapSprites[2] : null,
                hintText = "Hint: Try 'Go straight' past the park, and state that it is 'opposite across the road' to the Post Office."
            }
        };

        SyncBasePromptsFromMapPrompts();
    }

    private void SyncBasePromptsFromMapPrompts() {
        if (mapPrompts == null) return;
        WritingPrompt[] unit3Prompts = new WritingPrompt[mapPrompts.Length];
        for (int i = 0; i < mapPrompts.Length; i++) {
            unit3Prompts[i] = new WritingPrompt {
                npcOfferText = mapPrompts[i].npcOfferText,
                npcOfferAudioClip = mapPrompts[i].npcOfferAudioClip,
                starterChipsText = mapPrompts[i].starterChipsText,
                validKeywords = mapPrompts[i].validKeywords
            };
        }
        SetWritingPrompts(unit3Prompts);
    }

    private bool CheckIfTextMatchesAnyChip(string userInput, string[] chips) {
        if (chips == null || string.IsNullOrEmpty(userInput)) return false;
        foreach (string chip in chips) {
            if (string.IsNullOrEmpty(chip)) continue;
            string cleanChip = chip.ToLowerInvariant().Replace("...", "").Replace("/", "").Replace(".", "").Trim();
            string[] words = cleanChip.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2) {
                string checkPhrase = words[0] + " " + words[1];
                if (userInput.Contains(checkPhrase)) return true;
            } else if (words.Length == 1) {
                if (userInput.Contains(words[0])) return true;
            }
        }
        return false;
    }

    private void OnCustomSubmitClicked() {
        TMP_InputField inputField = GetInputField();
        if (inputField == null) return;

        string userInput = inputField.text.ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(userInput)) return;

        int currentIdx = GetPromptIndex();
        string[] currentChips = (mapPrompts != null && currentIdx >= 0 && currentIdx < mapPrompts.Length && mapPrompts[currentIdx] != null)
                                ? mapPrompts[currentIdx].starterChipsText : null;

        bool hasUsedChip = wasChipUsed || CheckIfTextMatchesAnyChip(userInput, currentChips);

        bool hasMovement = userInput.Contains("straight") || userInput.Contains("turn") || userInput.Contains("left") ||
                           userInput.Contains("right") || userInput.Contains("walk") || userInput.Contains("go") ||
                           userInput.Contains("past") || userInput.Contains("along") || userInput.Contains("junction");

        bool hasPosition = userInput.Contains("opposite") || userInput.Contains("beside") || userInput.Contains("next") ||
                           userInput.Contains("between") || userInput.Contains("near") || userInput.Contains("behind") ||
                           userInput.Contains("on your");

        Image bgImg = GetInputFieldBackgroundImage();
        Button submitBtn = GetSubmitButton();

        if (hasUsedChip && hasMovement && hasPosition) {
            if (bgImg != null) bgImg.color = Color.green;
            if (inputField != null) inputField.interactable = false;
            if (submitBtn != null) submitBtn.interactable = false;

            if (hintPanel != null) hintPanel.SetActive(false);
            else if (hintTMP != null) hintTMP.gameObject.SetActive(false);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            Invoke(nameof(LoadNextPromptCustom), 2.0f);
        } else {
            wrongAttempts++;
            if (bgImg != null) bgImg.color = new Color(1f, 0.4f, 0.4f);
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            string baseFeedback = "";
            if (!hasUsedChip) {
                baseFeedback = "Please tap and use at least one phrase from the word bank chips below to help write your directions!";
            } else if (!hasMovement || !hasPosition) {
                baseFeedback = "Almost! Make sure your directions contain both a MOVEMENT phrase (like 'Go straight' or 'Turn left') and a POSITION phrase (like 'beside' or 'opposite').";
            } else {
                baseFeedback = "Not quite! Try adjusting your direction phrase using the word bank.";
            }

            TextMeshProUGUI bubbleTMP = GetNpcBubbleTMP();

            // If 2+ wrong attempts, show hint in dedicated UI (if available) OR fallback inside bubble
            if (wrongAttempts >= 2 && mapPrompts != null && currentIdx >= 0 && currentIdx < mapPrompts.Length && mapPrompts[currentIdx] != null) {
                string hint = mapPrompts[currentIdx].hintText;
                if (!string.IsNullOrEmpty(hint)) {
                    if (hintPanel != null) {
                        if (hintTMP != null) hintTMP.text = hint;
                        hintPanel.SetActive(true);
                        if (bubbleTMP != null) bubbleTMP.text = baseFeedback;
                    } else if (hintTMP != null) {
                        hintTMP.text = hint;
                        hintTMP.gameObject.SetActive(true);
                        if (bubbleTMP != null) bubbleTMP.text = baseFeedback;
                    } else if (bubbleTMP != null) {
                        bubbleTMP.text = baseFeedback + "\n\n<color=#FFFF00><b>" + hint + "</b></color>";
                    }
                } else if (bubbleTMP != null) {
                    bubbleTMP.text = baseFeedback;
                }
            } else {
                if (bubbleTMP != null) bubbleTMP.text = baseFeedback;
            }
        }
    }

    private void LoadNextPromptCustom() {
        wrongAttempts = 0;
        wasChipUsed = false;
        if (hintPanel != null) hintPanel.SetActive(false);
        else if (hintTMP != null) hintTMP.gameObject.SetActive(false);

        int nextIdx = GetPromptIndex() + 1;
        if (mapPrompts != null && nextIdx < mapPrompts.Length) {
            SetPromptIndex(nextIdx);
            LoadPromptSafe(nextIdx);
            UpdateMapImage(nextIdx);
        } else {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
        }
    }

    private void LoadPromptSafe(int index) {
        var method = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetMethod("LoadPrompt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method != null) {
            method.Invoke(this, new object[] { index });
        }
    }

    private int GetPromptIndex() {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetField("promptIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(this) : 0;
    }

    private void SetPromptIndex(int index) {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetField("promptIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(this, index);
    }

    private Button[] GetStarterChipButtons() {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetField("starterChipButtons", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? field.GetValue(this) as Button[] : null;
    }

    private Button GetSubmitButton() {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetField("submitButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? field.GetValue(this) as Button : null;
    }

    private TMP_InputField GetInputField() {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetField("studentInputField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? field.GetValue(this) as TMP_InputField : null;
    }

    private Image GetInputFieldBackgroundImage() {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetField("studentInputFieldBackgroundImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? field.GetValue(this) as Image : null;
    }

    private TextMeshProUGUI GetNpcBubbleTMP() {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetField("npcSpeechBubbleTMP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? field.GetValue(this) as TextMeshProUGUI : null;
    }

    private AudioClip LoadAudio(string clipName) {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/3_BeyondTheHorizon/Writing/" + clipName + ".mp3");
#else
        return null;
#endif
    }

    public void SetWritingPrompts(WritingPrompt[] data) {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetField("writingPromptArray", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) {
            field.SetValue(this, data);
        }
    }

    public WritingPrompt[] GetWritingPrompts() {
        var field = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetField("writingPromptArray", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) {
            return field.GetValue(this) as WritingPrompt[];
        }
        return null;
    }

    public void SetMapPrompts(MapPromptData[] data) {
        mapPrompts = data;
        SyncBasePromptsFromMapPrompts();
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.LogWarning($"Topic not set for {this.name}!");
            return;
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        var nextSOField = typeof(Masters_PolishedCommunication_Writing_LessonTwo).GetField("nextLessonSO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Masters_LessonSO nextLessonSO = nextSOField != null ? nextSOField.GetValue(this) as Masters_LessonSO : null;

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
