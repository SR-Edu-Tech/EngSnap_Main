using UnityEngine;


public class UnitPanelController_BB1 : MonoBehaviour
{
    [Header("Wiring")]
    public TopicRegistry_BB1 registry;
    public GameObject unitButtonsRoot;   // Parent that holds all UnitButton_BB1s

    // ── called by TopicRegistry when this topic is selected ───────────────
    public void Open()
    {
        gameObject.SetActive(true);
        ShowButtons();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // ── called by UnitButton_BB1 when a unit is clicked ───────────────────
    public void StartUnit(UnitButton_BB1 unitButton)
    {
        unitButtonsRoot.SetActive(false);
        unitButton.unitGameObject.SetActive(true);


          var speaking = unitButton.unitGameObject.GetComponent<SpeakingGameController>();
    if (speaking != null)
    {
        //speaking.ResetGame();
    }
    }

    // ── called by IntroManager_BB1 / ListeningScreen_BB1 when unit finishes ─
    public void UnitFinished(UnitButton_BB1 unitButton)
    {
        unitButton.unitGameObject.SetActive(false);
        unitButton.MarkCompleted();
        ShowButtons();
    }

    // ── called by a Back button if you want topic → topic-selection nav ───
    public void BackToTopics()
    {
        registry.BackToTopicSelection(this);
    }

    // ─────────────────────────────────────────────────────────────────────
    private void ShowButtons()
    {
        unitButtonsRoot.SetActive(true);
    }
}
