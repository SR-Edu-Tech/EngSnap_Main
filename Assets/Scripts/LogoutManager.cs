using UnityEngine;

public class LogoutManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject loginPanel;
    public GameObject classSelectionPanel;

    // LOGOUT BUTTON
    public void Logout()
    {
        // REMOVE LOGIN TOKEN
        PlayerPrefs.DeleteKey("ACCESS_TOKEN");

        // OPTIONAL
        PlayerPrefs.Save();

        // CLOSE CLASS PANEL
        classSelectionPanel.SetActive(false);

        // OPEN LOGIN PANEL
        loginPanel.SetActive(true);

        Debug.Log("Logged Out");
    }
}