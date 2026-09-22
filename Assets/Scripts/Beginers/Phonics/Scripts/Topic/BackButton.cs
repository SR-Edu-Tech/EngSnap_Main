using UnityEngine;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    [SerializeField] private GameObject currentPanel;
    [SerializeField] private GameObject UnitTopicPanel;
    [SerializeField] private GameObject UnitPanel;
    [SerializeField] private GameObject previousPanel;

    private Button button;

    private void Awake()
    {
        // Automatically attach click listener if a UI Button component is present
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(GoToPreviousPanel);
        }
    }

    private void Start()
    {
        // Auto-assign currentPanel to parent container if left empty
        if (currentPanel == null)
        {
            if (transform.parent != null)
            {
                currentPanel = transform.parent.gameObject;
            }
            else
            {
                currentPanel = gameObject;
            }
        }
    }

    public void GoToPreviousPanel()
    {
        DeactivateSceneMascots();

        // Show previous target panel if assigned
        if (previousPanel != null)
        {
            previousPanel.SetActive(true);
        }

        // Hide topic panel if assigned
        if (UnitTopicPanel != null)
        {
            UnitTopicPanel.SetActive(false);
        }

        // Hide unit panel if assigned
        if (UnitPanel != null)
        {
            UnitPanel.SetActive(false);
        }

        // Hide current panel
        if (currentPanel != null)
        {
            currentPanel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void OnBackClicked()
    {
        GoToPreviousPanel();
    }

    private void DeactivateSceneMascots()
    {
        var tf = FindObjectOfType<EngSnap.Phonics2.Unit2.TwoFamiliesController>();
        if (tf != null) tf.DeactivateMascots();

        var ll = FindObjectOfType<EngSnap.Phonics2.Unit2.LetterLibraryController>();
        if (ll != null) ll.DeactivateMascots();

        var fsm = FindObjectOfType<EngSnap.Phonics2.Unit2.FirstSoundMatchController>();
        if (fsm != null) fsm.DeactivateMascots();

        var ws = FindObjectOfType<EngSnap.Phonics2.Unit2.WriteAndSayController>();
        if (ws != null) ws.DeactivateMascots();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(GoToPreviousPanel);
        }
    }
}
