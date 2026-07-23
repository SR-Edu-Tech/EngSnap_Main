using UnityEngine;

/// <summary>
/// Bridge between lesson‑local flows and the global GameManager.
/// Ensures a single source of truth for slide navigation.
/// </summary>
public class LessonNavigationBridge : MonoBehaviour
{
    public static LessonNavigationBridge Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// Called by a lesson when it has finished its internal sequence.
    /// Delegates to the global GameManager to advance to the next slide.
    /// </summary>
    public void RequestNextSlide()
    {
        if (GameManager_Junior1A.Instance != null)
        {
            GameManager_Junior1A.Instance.Next(true);
        }
        else
        {
            Debug.LogWarning("[LessonNavigationBridge] GameManager instance missing – cannot advance slide.");
        }
    }
}
