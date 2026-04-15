using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to the "Learn" Button inside each linked home screen GameObject.
///
/// Automatically wires itself to HomeScreenManager.OnLearnClicked() so
/// individual home screen prefabs need no Inspector references to the manager.
///
/// SETUP:
///   1. Add this component to the Learn button's GameObject.
///   2. No extra wiring needed — it finds HomeScreenManager at runtime.
/// </summary>
[RequireComponent(typeof(Button))]
public class LearnButton : MonoBehaviour
{
    private Button             _button;
    private HomeScreenManager  _manager;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnLearnClicked);
    }

    private void Start()
    {
        // FIX #4: Cache the manager reference once in Start() instead of calling
        // FindObjectOfType<HomeScreenManager>() on every button click.
        // FindObjectOfType iterates every active object in the scene — on a complex
        // scene this causes a visible stutter on tap.
        // Start() is used (not Awake()) to guarantee HomeScreenManager.Instance is
        // set by the time we read it (all Awake() calls complete before any Start()).
        _manager = HomeScreenManager.Instance;

        if (_manager == null)
            Debug.LogError("[LearnButton] HomeScreenManager.Instance not found. " +
                           "Make sure HomeScreenManager is in the scene and its " +
                           "Awake() has run before this Start().");
    }

    private void OnLearnClicked()
    {
        if (_manager == null)
        {
            // Fallback: try to find it now in case scene order was unusual.
            _manager = HomeScreenManager.Instance;
            if (_manager == null)
            {
                Debug.LogError("[LearnButton] HomeScreenManager not found in scene.");
                return;
            }
        }

        if (!AppSession.IsReady)
        {
            Debug.LogWarning("[LearnButton] No bundle/scene stored yet. " +
                             "Select a sub-button first.");
            return;
        }

        // FIX #5 (LearnButton side): Pass ourselves as the Button so
        // HomeScreenManager can disable us during loading and re-enable us
        // when the load completes or errors, preventing double-tap issues.
        _manager.OnLearnClicked(_button);
    }
}