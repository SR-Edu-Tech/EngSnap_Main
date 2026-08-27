using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls tick/checkmark mark images on topic selection buttons for any Unit using GameObject references.
/// Activates checkmarks when topics are completed and opens the Unit Reward Panel
/// when all topics in the Unit are completed.
/// Progress is persisted using PlayerPrefs.
/// </summary>
public class TopicProgressUI : MonoBehaviour
{
    public static event Action<string, string> OnTopicCompletedEvent;

    [System.Serializable]
    public class TopicUIItem
    {
        [Tooltip("Drag the topic panel or stop panel GameObject directly here.")]
        public GameObject topicPanel;

        [Tooltip("The topic button GameObject.")]
        public GameObject topicButton;

        [Tooltip("The Tick / Checkmark Image GameObject on this topic button.")]
        public GameObject tickImage;

        [Tooltip("Optional topic name string if topicPanel is not assigned.")]
        public string topicName;
    }

    [Header("Unit Settings")]
    [Tooltip("Target Unit ID, e.g. 'Unit1', 'Unit2', 'Unit3'.")]
    [SerializeField] private string unitID = "Unit1";

    [Header("Topic Items & Tick Marks")]
    [Tooltip("List of topic buttons and their corresponding tick mark images.")]
    [SerializeField] private TopicUIItem[] topicItems;

    [Header("Reward Panel & Visuals")]
    [Tooltip("The Main Unit Panel GameObject to deactivate when reward panel shows.")]
    [SerializeField] private GameObject unitPanel;

    [Tooltip("The Unit Content Panel to hide upon unit completion.")]
    [SerializeField] private GameObject unitContentPanel;

    [Tooltip("Modular Topic Complete Panel shown upon completing an individual topic (e.g. Meet Phonics, Sound Wall).")]
    [SerializeField] private GameObject topicCompletePanel;

    [Tooltip("The Unit Reward Panel GameObject to show when all topics in the unit are completed.")]
    [SerializeField] private GameObject unitRewardPanel;

    [Tooltip("Confetti particle system to activate on unit completion.")]
    [SerializeField] private GameObject confettiParticles;

    [Tooltip("Audio clip played when unit reward panel is shown.")]
    [SerializeField] private AudioClip unitRewardSFX;

    [Tooltip("Dialogue voice audio clip played when unit reward panel is shown (e.g. 'Unit 1 Phonics Star!').")]
    [SerializeField] private AudioClip unitRewardDialogueSFX;

    [Tooltip("AudioSource to play unitRewardSFX.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Optional TopicData ScriptableObject to populate dynamic RewardPanel content.")]
    [SerializeField] private EngSnap.Common.TopicData topicData;

    [Header("Debug & Testing")]
    [Tooltip("Check this box in Inspector to automatically clear saved progress when starting in Unity Editor.")]
    [SerializeField] private bool clearProgressOnStart = false;

    private static HashSet<int> completedPanelInstances = new HashSet<int>();
    private static HashSet<string> shownTopicPanelKeys = new HashSet<string>();

    public string UnitID => unitID;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (clearProgressOnStart)
        {
            ResetTopicProgress();
        }
    }

    private void OnEnable()
    {
        OnTopicCompletedEvent += OnTopicCompleted;
        RefreshTopicProgress();
    }

    private void OnDisable()
    {
        OnTopicCompletedEvent -= OnTopicCompleted;
    }

    private void Start()
    {
        RefreshTopicProgress();
    }

    private void OnTopicCompleted(string unit, string key)
    {
        if (string.IsNullOrEmpty(unit) || string.Equals(NormalizeUnitID(unit), NormalizeUnitID(unitID), StringComparison.OrdinalIgnoreCase))
        {
            RefreshTopicProgress(isRealtimeEvent: true, completedTopicName: key);
        }
    }

    public void UpdateOnlyTicks()
    {
        if (topicItems == null || topicItems.Length == 0) return;

        foreach (var item in topicItems)
        {
            if (item == null) continue;

            bool isCompleted = false;

            if (item.topicPanel != null)
            {
                isCompleted = IsTopicCompleted(unitID, item.topicPanel);
            }
            else if (item.topicButton != null)
            {
                isCompleted = IsTopicCompleted(unitID, item.topicButton);
            }

            if (!isCompleted && !string.IsNullOrEmpty(item.topicName))
            {
                isCompleted = IsTopicCompleted(unitID, item.topicName);
            }

            if (item.tickImage != null)
            {
                item.tickImage.SetActive(isCompleted);
            }
        }
    }

    public void RefreshTopicProgress(bool isRealtimeEvent = false, string completedTopicName = "")
    {
        UpdateOnlyTicks();

        if (topicItems == null || topicItems.Length == 0) return;

        int completedCount = 0;
        foreach (var item in topicItems)
        {
            if (item == null) continue;

            bool isCompleted = false;
            if (item.topicPanel != null)
            {
                isCompleted = IsTopicCompleted(unitID, item.topicPanel);
            }
            else if (item.topicButton != null)
            {
                isCompleted = IsTopicCompleted(unitID, item.topicButton);
            }

            if (!isCompleted && !string.IsNullOrEmpty(item.topicName))
            {
                isCompleted = IsTopicCompleted(unitID, item.topicName);
            }

            if (isCompleted)
            {
                completedCount++;
            }
        }

        if (gameObject.activeInHierarchy && completedCount > 0 && completedCount >= topicItems.Length)
        {
            string rewardKey = topicData != null ? topicData.RewardShownPrefKey : $"{unitID}_rewardShown";
            bool wasShown = PlayerPrefs.GetInt(rewardKey, 0) == 1;

            if (isRealtimeEvent || !wasShown)
            {
                ShowUnitRewardPanel();
            }
        }
        else if (isRealtimeEvent && topicCompletePanel != null)
        {
            ShowSingleTopicCompletePanel(completedTopicName);
        }
    }

    public void ShowSingleTopicCompletePanel(string topicName)
    {
        if (topicCompletePanel == null) return;

        string formattedName = FormatTopicName(topicName);
        EngSnap.Common.TopicCompletePanel panelScript = topicCompletePanel.GetComponent<EngSnap.Common.TopicCompletePanel>();
        if (panelScript != null)
        {
            panelScript.Show(formattedName, "TOPIC COMPLETED!");
        }
        else
        {
            topicCompletePanel.SetActive(true);
        }
    }

    public void ShowUnitRewardPanel()
    {
        if (unitRewardPanel != null)
        {
            if (unitPanel != null && !unitRewardPanel.transform.IsChildOf(unitPanel.transform))
            {
                unitPanel.SetActive(false);
            }

            if (unitContentPanel != null && !unitRewardPanel.transform.IsChildOf(unitContentPanel.transform))
            {
                unitContentPanel.SetActive(false);
            }

            EngSnap.Common.RewardPanel rewardPanelScript = unitRewardPanel.GetComponent<EngSnap.Common.RewardPanel>();
            if (rewardPanelScript != null)
            {
                if (topicData != null && topicData.dialogueSound == null && unitRewardDialogueSFX != null)
                {
                    topicData.dialogueSound = unitRewardDialogueSFX;
                }
                rewardPanelScript.Show(topicData, this);
            }
            else
            {
                unitRewardPanel.SetActive(true);

                if (confettiParticles != null) confettiParticles.SetActive(true);

                if (unitRewardSFX != null && audioSource != null)
                {
                    audioSource.PlayOneShot(unitRewardSFX);
                }
                if (unitRewardDialogueSFX != null && audioSource != null)
                {
                    audioSource.PlayOneShot(unitRewardDialogueSFX);
                }
            }
        }

        string rewardKey = topicData != null ? topicData.RewardShownPrefKey : $"{unitID}_rewardShown";
        PlayerPrefs.SetInt(rewardKey, 1);
        PlayerPrefs.SetInt($"{unitID}_Completed", 1);
        PlayerPrefs.Save();
    }

    [ContextMenu("Reset Topic & Reward Progress")]
    public void ResetTopicProgress()
    {
        string normUnit = NormalizeUnitID(unitID);
        completedPanelInstances.Clear();
        shownTopicPanelKeys.Clear();

        if (topicItems != null)
        {
            foreach (var item in topicItems)
            {
                if (item == null) continue;

                if (item.topicPanel != null)
                {
                    DeleteStopKeysForGameObject(unitID, item.topicPanel);
                }
                if (item.topicButton != null)
                {
                    DeleteStopKeysForGameObject(unitID, item.topicButton);
                }
                if (!string.IsNullOrEmpty(item.topicName))
                {
                    DeleteStopKeys(unitID, item.topicName);
                }
            }
        }

        string rewardKey = topicData != null ? topicData.RewardShownPrefKey : $"{unitID}_rewardShown";
        PlayerPrefs.DeleteKey(rewardKey);
        PlayerPrefs.DeleteKey($"{normUnit}_rewardShown");
        PlayerPrefs.DeleteKey($"{unitID}_rewardShown");
        PlayerPrefs.DeleteKey($"{normUnit}_Completed");
        PlayerPrefs.DeleteKey($"{unitID}_Completed");
        PlayerPrefs.Save();

        if (unitRewardPanel != null) unitRewardPanel.SetActive(false);
        if (confettiParticles != null) confettiParticles.SetActive(false);

        RefreshAllTicks();
        Debug.Log($"<color=#FF5722><b>🗑️ [TopicProgressUI] Reset topic progress and reward status for [{unitID}] using GameObject references.</b></color>");
    }

    private void DeleteStopKeysForGameObject(string unit, GameObject go)
    {
        if (go == null) return;
        string normUnit = NormalizeUnitID(unit);

        completedPanelInstances.Remove(go.GetInstanceID());

        PlayerPrefs.DeleteKey($"TopicPanel_{go.GetInstanceID()}");
        PlayerPrefs.DeleteKey($"PanelID_{go.GetInstanceID()}");
        PlayerPrefs.DeleteKey($"{normUnit}_PanelID_{go.name}");
        PlayerPrefs.DeleteKey($"{unit}_PanelID_{go.name}");

        DeleteStopKeys(unit, go.name);
    }

    private void DeleteStopKeys(string unit, string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        string normUnit = NormalizeUnitID(unit);
        string normStop = NormalizeStopKey(key);

        PlayerPrefs.DeleteKey($"{normUnit}_Topic_{normStop}");
        PlayerPrefs.DeleteKey($"{normUnit}_Topic_{key}");
        PlayerPrefs.DeleteKey($"{normUnit}_Stop_{normStop}");
        PlayerPrefs.DeleteKey($"{normUnit}_Stop_{key}");
        if (!string.IsNullOrEmpty(unit))
        {
            PlayerPrefs.DeleteKey($"{unit}_Topic_{normStop}");
            PlayerPrefs.DeleteKey($"{unit}_Topic_{key}");
            PlayerPrefs.DeleteKey($"{unit}_Stop_{normStop}");
            PlayerPrefs.DeleteKey($"{unit}_Stop_{key}");
        }
    }

    public static void MarkTopicComplete(GameObject topicPanel)
    {
        if (topicPanel == null) return;

        completedPanelInstances.Add(topicPanel.GetInstanceID());

        string unit = InferUnitID(topicPanel);
        string normUnit = NormalizeUnitID(unit);

        PlayerPrefs.SetInt($"TopicPanel_{topicPanel.GetInstanceID()}", 1);
        PlayerPrefs.SetInt($"{normUnit}_PanelID_{topicPanel.name}", 1);
        PlayerPrefs.SetInt($"{unit}_PanelID_{topicPanel.name}", 1);

        Action onContinue = () =>
        {
            if (topicPanel != null)
            {
                topicPanel.SendMessage("GoToNextPanel", SendMessageOptions.DontRequireReceiver);
            }
        };

        MarkTopicComplete(unit, topicPanel.name, onContinue);
    }

    public static void MarkTopicComplete(string unit, string topicName, Action onContinue = null)
    {
        if (string.IsNullOrEmpty(topicName)) return;

        string normUnit = NormalizeUnitID(unit);
        string normStop = NormalizeStopKey(topicName);
        string uniqueKey = $"{normUnit}_{normStop}";

        PlayerPrefs.SetInt($"{normUnit}_Topic_{normStop}", 1);
        PlayerPrefs.SetInt($"{normUnit}_Topic_{topicName}", 1);
        PlayerPrefs.SetInt($"{normUnit}_Stop_{normStop}", 1);
        PlayerPrefs.SetInt($"{normUnit}_Stop_{topicName}", 1);
        if (!string.IsNullOrEmpty(unit))
        {
            PlayerPrefs.SetInt($"{unit}_Topic_{topicName}", 1);
            PlayerPrefs.SetInt($"{unit}_Topic_{normStop}", 1);
            PlayerPrefs.SetInt($"{unit}_Stop_{topicName}", 1);
            PlayerPrefs.SetInt($"{unit}_Stop_{normStop}", 1);
        }
        PlayerPrefs.Save();

        Debug.Log($"<color=#4CAF50><b>✅ [TopicProgressUI] FINISHED TOPIC:</b> [{topicName}] in [{unit}]!</color>");

        OnTopicCompletedEvent?.Invoke(unit, topicName);
        RefreshAllTicks();

        // Trigger Topic Complete Panel popup
        ShowTopicCompletePanel(topicName, onContinue);
    }

    public static void ShowTopicCompletePanel(string topicName, Action onContinue = null, Action onReplay = null, AudioClip voiceClip = null)
    {
        // 1. Find any TopicCompletePanel component in scene (including inactive)
        EngSnap.Common.TopicCompletePanel[] panels = FindObjectsOfType<EngSnap.Common.TopicCompletePanel>(true);
        if (panels != null && panels.Length > 0)
        {
            panels[0].Show(FormatTopicName(topicName), "TOPIC COMPLETED!", onContinue, onReplay, voiceClip);
            return;
        }

        // 2. Fallback to TopicProgressUI topicCompletePanel reference
        TopicProgressUI[] uiItems = FindObjectsOfType<TopicProgressUI>(true);
        foreach (var ui in uiItems)
        {
            if (ui != null && ui.topicCompletePanel != null)
            {
                EngSnap.Common.TopicCompletePanel panelScript = ui.topicCompletePanel.GetComponent<EngSnap.Common.TopicCompletePanel>();
                if (panelScript != null)
                {
                    panelScript.Show(FormatTopicName(topicName), "TOPIC COMPLETED!", onContinue, onReplay, voiceClip);
                    return;
                }
                else
                {
                    ui.topicCompletePanel.SetActive(true);
                    return;
                }
            }
        }
    }

    public static void HideTopicCompletePanel()
    {
        EngSnap.Common.TopicCompletePanel[] panels = UnityEngine.Object.FindObjectsOfType<EngSnap.Common.TopicCompletePanel>(true);
        if (panels != null)
        {
            foreach (var panel in panels)
            {
                if (panel != null) panel.Hide();
            }
        }

        EngSnap.Common.RewardPanel[] rewardPanels = UnityEngine.Object.FindObjectsOfType<EngSnap.Common.RewardPanel>(true);
        if (rewardPanels != null)
        {
            foreach (var rPanel in rewardPanels)
            {
                if (rPanel != null) rPanel.gameObject.SetActive(false);
            }
        }

        TopicProgressUI[] uiItems = UnityEngine.Object.FindObjectsOfType<TopicProgressUI>(true);
        foreach (var ui in uiItems)
        {
            if (ui != null)
            {
                if (ui.topicCompletePanel != null) ui.topicCompletePanel.SetActive(false);
                if (ui.unitRewardPanel != null) ui.unitRewardPanel.SetActive(false);
            }
        }
    }

    public static bool IsTopicCompleted(string unit, GameObject topicPanel)
    {
        if (topicPanel == null) return false;

        if (completedPanelInstances.Contains(topicPanel.GetInstanceID())) return true;
        if (PlayerPrefs.GetInt($"TopicPanel_{topicPanel.GetInstanceID()}", 0) == 1) return true;

        string normUnit = NormalizeUnitID(unit);
        if (PlayerPrefs.GetInt($"{normUnit}_PanelID_{topicPanel.name}", 0) == 1) return true;
        if (!string.IsNullOrEmpty(unit) && PlayerPrefs.GetInt($"{unit}_PanelID_{topicPanel.name}", 0) == 1) return true;

        return IsTopicCompleted(unit, topicPanel.name);
    }

    public static bool IsTopicCompleted(string unit, string topicName)
    {
        if (string.IsNullOrEmpty(topicName)) return false;

        string normUnit = NormalizeUnitID(unit);
        string normStop = NormalizeStopKey(topicName);

        if (PlayerPrefs.GetInt($"{normUnit}_Topic_{normStop}", 0) == 1) return true;
        if (PlayerPrefs.GetInt($"{normUnit}_Topic_{topicName}", 0) == 1) return true;
        if (PlayerPrefs.GetInt($"{normUnit}_Stop_{normStop}", 0) == 1) return true;

        if (!string.IsNullOrEmpty(unit))
        {
            if (PlayerPrefs.GetInt($"{unit}_Topic_{topicName}", 0) == 1) return true;
            if (PlayerPrefs.GetInt($"{unit}_Topic_{normStop}", 0) == 1) return true;
            if (PlayerPrefs.GetInt($"{unit}_Stop_{topicName}", 0) == 1) return true;
        }

        return false;
    }

    public static void HideAllTopicCompletePanels()
    {
        EngSnap.Common.TopicCompletePanel[] panels = FindObjectsOfType<EngSnap.Common.TopicCompletePanel>(true);
        foreach (var p in panels)
        {
            if (p != null) p.Hide();
        }
    }

    public static void RefreshAllTicks()
    {
        HideAllTopicCompletePanels();
        TopicProgressUI[] uiItems = FindObjectsOfType<TopicProgressUI>(true);
        foreach (var ui in uiItems)
        {
            if (ui != null)
            {
                ui.UpdateOnlyTicks();
            }
        }
    }

    public static string FormatTopicName(string key)
    {
        if (string.IsNullOrEmpty(key)) return "Topic Complete";

        string norm = NormalizeStopKey(key);
        switch (norm)
        {
            case "meetphonics": return "Meet Phonics";
            case "soundletter":
            case "soundandletter": return "Sound & Letter";
            case "soundwall": return "Sound Wall";
            case "starround": return "Star Round";
            case "meetletters":
            case "meetletter": return "Meet Letters";
            case "bigandsmallmatch":
            case "bigsmallmatch": return "Big & Small Match";
            case "whichletter": return "Which Letter";
            case "blendit": return "Blend It";
            case "soundssafari":
            case "soundsafari": return "Sound Safari";
            case "namevssound": return "Name vs Sound";
            case "missingsound": return "Missing Sound";
            case "thefivevowels":
            case "fivevowels": return "The Five Vowels";
            case "theconsonantcrew":
            case "consonantcrew": return "The Consonant Crew";
            case "vowelorconsonant": return "Vowel or Consonant?";
            case "catchthevowel": return "Catch the Vowel";
            case "shortandlong": return "Short & Long";
            case "soundsort": return "Sound Sort";
            case "whichsound": return "Which Sound?";
            case "bigears": return "Big Ears";
            case "sounddetective": return "Sound Detective";
            case "alphabetparade": return "Alphabet Parade";
            case "soundpictures": return "Sound Pictures";
            case "thefivesingers":
            case "fivesingers": return "The Five Singers";
            case "twovoices": return "Two Voices";
            case "singthevowels": return "Sing the Vowels";
            case "slidingsounds": return "Sliding Sounds";
            default:
                return System.Text.RegularExpressions.Regex.Replace(key, "([a-z])([A-Z])", "$1 $2").Replace("_", " ");
        }
    }

    private static string InferUnitID(GameObject panel)
    {
        if (panel == null) return "Unit1";

        Transform curr = panel.transform;
        while (curr != null)
        {
            string pName = curr.name.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");
            if (pName.Contains("unit10") || pName.Contains("u10")) return "Unit10";
            if (pName.Contains("unit9") || pName.Contains("u9")) return "Unit9";
            if (pName.Contains("unit8") || pName.Contains("u8")) return "Unit8";
            if (pName.Contains("unit7") || pName.Contains("u7")) return "Unit7";
            if (pName.Contains("unit6") || pName.Contains("u6")) return "Unit6";
            if (pName.Contains("unit5") || pName.Contains("u5")) return "Unit5";
            if (pName.Contains("unit4") || pName.Contains("u4")) return "Unit4";
            if (pName.Contains("unit3") || pName.Contains("u3")) return "Unit3";
            if (pName.Contains("unit2") || pName.Contains("u2")) return "Unit2";
            if (pName.Contains("unit1") || pName.Contains("u1")) return "Unit1";
            curr = curr.parent;
        }

        return "Unit1";
    }

    public static string NormalizeUnitID(string unit)
    {
        if (string.IsNullOrEmpty(unit)) return "Unit1";
        string u = unit.Replace(" ", "").Replace("_", "").Replace("Panel", "").ToLowerInvariant();
        if (u.Contains("unit10") || u == "10") return "Unit10";
        if (u.Contains("unit9") || u == "9") return "Unit9";
        if (u.Contains("unit8") || u == "8") return "Unit8";
        if (u.Contains("unit7") || u == "7") return "Unit7";
        if (u.Contains("unit6") || u == "6") return "Unit6";
        if (u.Contains("unit5") || u == "5") return "Unit5";
        if (u.Contains("unit4") || u == "4") return "Unit4";
        if (u.Contains("unit3") || u == "3") return "Unit3";
        if (u.Contains("unit2") || u == "2") return "Unit2";
        if (u.Contains("unit1") || u == "1") return "Unit1";
        return "Unit1";
    }

    public static string NormalizeStopKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        string norm = key.Trim();

        bool isAM = System.Text.RegularExpressions.Regex.IsMatch(norm, @"(A[\s\-–—]*M|A_M)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        bool isNZ = System.Text.RegularExpressions.Regex.IsMatch(norm, @"(N[\s\-–—]*Z|N_Z)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        norm = System.Text.RegularExpressions.Regex.Replace(norm, @"\(.*?\)", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        norm = System.Text.RegularExpressions.Regex.Replace(norm, @"[?\!:;'"".,/\\@#$%^&*+=<>\[\]{}–—\-]", "");

        norm = norm.Replace(" ", "")
                   .Replace("&", "And")
                   .Replace("_", "")
                   .Replace("Panel", "")
                   .Replace("Controller", "")
                   .ToLowerInvariant();

        norm = System.Text.RegularExpressions.Regex.Replace(norm, @"^(unit\d*|step\d*|stop\d*)+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (norm.EndsWith("starround")) norm = norm.Substring(0, norm.Length - "starround".Length);
        else if (norm.EndsWith("round") && !norm.EndsWith("starround")) norm = norm.Substring(0, norm.Length - "round".Length);

        if (norm.StartsWith("meettheletter"))
        {
            norm = "meetletter" + norm.Substring("meettheletter".Length);
        }

        if (norm.EndsWith("phonic")) norm += "s";
        if (norm.EndsWith("letter") && !norm.EndsWith("letters")) norm += "s";

        if (isAM && !norm.EndsWith("am")) norm += "am";
        else if (isNZ && !norm.EndsWith("nz")) norm += "nz";

        return norm;
    }
}
