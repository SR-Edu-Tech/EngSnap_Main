using UnityEngine;

namespace Beginners.ItsPlayTime.Reading
{
    /// <summary>
    /// GAME FLOW MANAGER
    ///
    /// CRITICAL FIX: Start() was firing AFTER OnUnitStart() and calling
    /// screen1Panel.SetActive(false), which re-hid the panel that OpenGame()
    /// had just shown. Removed all SetActive calls from Start() entirely.
    /// Panels are now only controlled by OpenGame() / GoToScreen2() / GoToUnitPanel().
    /// </summary>
    public class GameFlowManager_ItsPlayTime_Reading : MonoBehaviour, IUnitCompletable
    {
        [Header("── PANELS ──")]
        public GameObject screen1Panel;
        public GameObject screen2Panel;

        [Header("── SCREEN MANAGERS ──")]
        public Screen1_PlaygroundGallery_ItsPlayTime_Reading screen1Manager;
        public Screen2_LetsPlay_ItsPlayTime_Reading screen2Manager;

        [Header("── STANDALONE MODE ──")]
        [Tooltip("Tick ON when running this scene directly in the Editor. Untick in production.")]
        public bool standaloneMode = false;

        private SharedUnitPanelController _sharedPanel;
        private SharedUnitButton          _sharedButton;
        private bool                      _gameRunning = false;

        public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
        {
            Debug.Log($"[GameFlowManager] OnUnitStart() received — _gameRunning={_gameRunning}");

            if (_gameRunning)
            {
                Debug.LogWarning("[GameFlowManager] OnUnitStart() duplicate call — IGNORED.");
                return;
            }

            _sharedPanel  = sharedPanel;
            _sharedButton = sharedButton;
            _gameRunning  = true;
            OpenGame();
        }

        private void Awake()
        {
            // Hide panels here in Awake — guaranteed to run before OnUnitStart().
            // Never touch panel visibility in Start() — Start() can fire AFTER OnUnitStart()
            // and would undo whatever OpenGame() already set.
            Debug.Log("[GameFlowManager] Awake() — hiding panels for clean initial state.");
            if (screen1Panel != null) screen1Panel.SetActive(false);
            if (screen2Panel != null) screen2Panel.SetActive(false);
        }

        private void Start()
        {
            // START() INTENTIONALLY DOES NOT TOUCH PANELS.
            // OnUnitStart() fires between Awake() and Start() in Unity's execution order.
            // Any SetActive() call here would undo what OpenGame() already did.
            Debug.Log($"[GameFlowManager] Start() — standaloneMode={standaloneMode}  _gameRunning={_gameRunning}");

            if (standaloneMode && !_gameRunning)
            {
                Debug.Log("[GameFlowManager] standaloneMode ON — launching OpenGame() from Start().");
                _gameRunning = true;
                OpenGame();
            }
        }

        public void OpenGame()
        {
            Debug.Log("[GameFlowManager] OpenGame() called.");

            if (screen2Panel != null) screen2Panel.SetActive(false);

            if (screen1Panel == null)
            {
                Debug.LogError("[GameFlowManager] screen1Panel is NULL — assign it in the Inspector!");
                return;
            }

            screen1Panel.SetActive(true);
            Debug.Log($"[GameFlowManager] screen1Panel '{screen1Panel.name}' SetActive(true).");

            if (screen1Manager == null)
            {
                Debug.LogError("[GameFlowManager] screen1Manager is NULL — assign Screen1 component in the Inspector!");
                return;
            }

            screen1Manager.ResetAndStart();
        }

        public void GoToScreen2()
        {
            Debug.Log("[GameFlowManager] GoToScreen2() called.");
            if (screen1Panel != null) screen1Panel.SetActive(false);

            if (screen2Panel == null)
            {
                Debug.LogError("[GameFlowManager] screen2Panel is NULL!");
                return;
            }

            screen2Panel.SetActive(true);

            if (screen2Manager == null)
            {
                Debug.LogError("[GameFlowManager] screen2Manager is NULL!");
                return;
            }

            screen2Manager.ResetAndStart();
        }

        public void GoToUnitPanel()
        {
            Debug.Log("[GameFlowManager] GoToUnitPanel() called.");
            if (screen1Panel != null) screen1Panel.SetActive(false);
            if (screen2Panel != null) screen2Panel.SetActive(false);
            _gameRunning = false;

            if (_sharedPanel != null)
                _sharedPanel.UnitFinished(_sharedButton);
            else
                Debug.LogWarning("[GameFlowManager] _sharedPanel is null.");
        }

        public void PlayAgain()
        {
            _gameRunning = true;
            OpenGame();
        }
    }
}