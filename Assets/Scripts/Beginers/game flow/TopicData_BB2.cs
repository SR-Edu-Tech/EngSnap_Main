using UnityEngine;

/// <summary>
/// Attach this to each topic's content panel GameObject in the scene.
/// e.g. on "EverydayGreetings" GameObject under Units_ContentPanel
///
/// HIERARCHY example:
///   Units_ContentPanel
///     └── EverydayGreetings     ← Add TopicData_BB2 here
///           ├── Intro           ← drag into unitEntries Element 0
///           ├── Listening       ← drag into unitEntries Element 1
///           └── ...
///     └── AboutMe               ← Add TopicData_BB2 here
///           ├── Intro
///           └── ...
/// </summary>
public class TopicData_BB2 : MonoBehaviour
{
    [Header("Topic Identity")]
    public string topicID;           // e.g. "EverydayGreetings" — must be UNIQUE

    [Header("Reward Panel")]
    public string championTitle;     // e.g. "Everyday Greetings Champion"
    [Tooltip("Words learned in this topic shown in the reward strip")]
    public string[] learnedWords;    // e.g. "Hello", "Thank You", "Goodbye"

    [Header("Unit Content GameObjects")]
    public TopicUnitEntry[] unitEntries;

    public GameObject GetContentObject(UnitType_BB1 unitType)
    {
        foreach (var entry in unitEntries)
            if (entry.unitType == unitType)
                return entry.contentGameObject;

        Debug.LogWarning($"TopicData_BB2 [{topicID}]: No content found for '{unitType}'");
        return null;
    }

    /// <summary>e.g. "EverydayGreetings_Intro"</summary>
    public string GetSaveKey(UnitType_BB1 unitType) => $"{topicID}_{unitType}";
}

[System.Serializable]
public class TopicUnitEntry
{
    public UnitType_BB1 unitType;
    public GameObject   contentGameObject;
    [Tooltip("Label shown on the star when it pops in the reward panel")]
    public string       unitDisplayName;   // e.g. "Intro", "Listening", "Reading"
}