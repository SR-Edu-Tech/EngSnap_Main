using System;
using UnityEngine;

/// <summary>
/// Legacy wrapper for UnitProgressManager. All topic progress tracking is now handled by TopicProgressUI.
/// </summary>
public class UnitProgressManager : MonoBehaviour
{
    public static event Action<string, string> OnStopCompletedEvent;
    public static event Action<string> OnUnitCompletedEvent;

    public string UnitID => "Unit1";

    public static void MarkStopComplete(GameObject stopPanel)
    {
        TopicProgressUI.MarkTopicComplete(stopPanel);
    }

    public static void MarkStopComplete(string unit, string stopKey)
    {
        TopicProgressUI.MarkTopicComplete(unit, stopKey);
    }

    public static bool IsStopCompleted(string unit, GameObject stopPanel)
    {
        return TopicProgressUI.IsTopicCompleted(unit, stopPanel);
    }

    public static bool IsStopCompleted(string unit, string stopKey)
    {
        return TopicProgressUI.IsTopicCompleted(unit, stopKey);
    }

    public static void RefreshAllTopicTicks()
    {
        TopicProgressUI.RefreshAllTicks();
    }

    public static string NormalizeUnitID(string unit)
    {
        return TopicProgressUI.NormalizeUnitID(unit);
    }

    public static string NormalizeStopKey(string key)
    {
        return TopicProgressUI.NormalizeStopKey(key);
    }

    [ContextMenu("Reset Unit Progress")]
    public void ResetProgress()
    {
        TopicProgressUI ui = GetComponent<TopicProgressUI>();
        if (ui != null)
        {
            ui.ResetTopicProgress();
        }
    }
}
