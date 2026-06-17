using UnityEngine;

namespace Beginners.ActItOut
{
    public class ActItOut_GameManager : MonoBehaviour, IUnitCompletable
    {
        public static ActItOut_GameManager Instance { get; private set; }

        [Header("Screen Panels")]
        public ActItOut_MatchingGameController matchingPanel;   // Screen 1
        public ActItOut_ActionGameController   actionPanel;     // Screen 2

        [Header("State")]
        [HideInInspector] public SharedUnitButton           ownerUnitButton;
        [HideInInspector] public SharedUnitPanelController  ownerUnitPanel;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
        {
            ownerUnitPanel  = sharedPanel;
            ownerUnitButton = sharedButton;

            // Start the gameplay cycle
            StartUnit();
        }

        /// <summary>
        /// Resets panels and starts the game from Screen 1 matching panel.
        /// </summary>
        public void StartUnit()
        {
            // Reset and hide Action Panel (Screen 2)
            if (actionPanel != null)
            {
                actionPanel.ResetPanel();
                actionPanel.gameObject.SetActive(false);
            }

            // Show and start Matching Panel (Screen 1)
            if (matchingPanel != null)
            {
                matchingPanel.gameObject.SetActive(true);
                // Wire callback to transition to Screen 2 when finished
                matchingPanel.OnFinished = TransitionToActionGame;
                matchingPanel.StartGame();
            }
            else
            {
                Debug.LogError("[ActItOut_GameManager] matchingPanel is not assigned!");
            }
        }

        private void TransitionToActionGame()
        {
            Debug.Log("[ActItOut_GameManager] Transitioning to Action Game (Screen 2)");

            if (matchingPanel != null)
            {
                matchingPanel.gameObject.SetActive(false);
            }

            if (actionPanel != null)
            {
                actionPanel.gameObject.SetActive(true);
                // Wire callback to complete unit when Screen 2 finishes
                actionPanel.OnFinished = OnUnitComplete;
                actionPanel.StartGame();
            }
            else
            {
                Debug.LogError("[ActItOut_GameManager] actionPanel is not assigned!");
                OnUnitComplete();
            }
        }

        private void OnUnitComplete()
        {
            Debug.Log("[ActItOut_GameManager] All gameplays complete! Finishing unit.");
            if (ownerUnitPanel != null && ownerUnitButton != null)
            {
                ownerUnitPanel.UnitFinished(ownerUnitButton);
            }
            else
            {
                Debug.LogWarning("[ActItOut_GameManager] Owner references are missing — cannot return to unit panel.");
            }
        }
    }
}
