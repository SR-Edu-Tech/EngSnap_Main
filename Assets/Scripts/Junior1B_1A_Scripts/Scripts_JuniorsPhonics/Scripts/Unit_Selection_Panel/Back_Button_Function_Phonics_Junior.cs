using UnityEngine;

public class Back_Button_Function_Phonics_Junior : MonoBehaviour
{
    [SerializeField] private GameObject[] panels;

    public void OpenPanel(int panelIndex)
    {
        CloseAllPanels();

        if (panels != null && panelIndex >= 0 && panelIndex < panels.Length)
        {
            if (panels[panelIndex] != null)
            {
                panels[panelIndex].SetActive(true);
                Debug.Log("Panel " + panelIndex + " opened");
            }
        }
    }

    private void CloseAllPanels()
    {
        if (panels == null) return;
        foreach (GameObject panel in panels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
    private void OnValidate()
    {
        if (panels != null)
        {
            System.Collections.Generic.List<GameObject> validPanels = new System.Collections.Generic.List<GameObject>();
            foreach (var p in panels)
            {
                if (p != null) validPanels.Add(p);
            }
            if (validPanels.Count != panels.Length)
            {
                panels = validPanels.ToArray();
            }
        }
    }

    private void Awake()
    {
        if (panels == null) panels = new GameObject[0];
    }

    private void OnEnable()
    {
        if (this == null || gameObject == null) return;
        Debug.Log($"{gameObject.name} - Back Button ENABLED");
    }

    private void OnDisable()
    {
        if (this == null || gameObject == null) return;
        Debug.Log($"{gameObject.name} - Back Button DISABLED");
    }
}