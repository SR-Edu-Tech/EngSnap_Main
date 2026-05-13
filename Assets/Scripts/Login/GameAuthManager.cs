using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class GameAuthManager : MonoBehaviour
{
    [Header("Login UI")]
    public GameObject loginPanel;

    public TMP_InputField LoginInput;
    public TMP_InputField passwordInput;

    public TMP_Text statusText;

    [Header("Class Selection")]

public GameObject classSelectionPanel;

public GameObject beginnersLock;
public GameObject juniorsLock;
public GameObject seniorsLock;
public GameObject mastersLock;

public Button beginnersButton;
public Button juniorsButton;
public Button seniorsButton;
public Button mastersButton;

    private string baseUrl = "https://gamedevpanel.com/api";
void Start()
{
    loginPanel.SetActive(false);
    classSelectionPanel.SetActive(false);
}
    // PLAY BUTTON
public void OnPlayButton()
{
    // HIDE CLASS PANEL INITIALLY
    classSelectionPanel.SetActive(false);

    if (PlayerPrefs.HasKey("ACCESS_TOKEN"))
    {
        Debug.Log("Already Logged In");

        // DIRECTLY LOAD COURSES
        StartCoroutine(GetStudentCourses());

       
    }
    else
    {
        loginPanel.SetActive(true);
        classSelectionPanel.SetActive(false);
    }
}
    // LOGIN BUTTON
    public void OnLoginButton()
    {
        StartCoroutine(LoginCoroutine());
    }

    IEnumerator LoginCoroutine()
    {
        statusText.text = "Logging in...";

        string url = baseUrl + "/auth/login";

        WWWForm form = new WWWForm();

        form.AddField("login", LoginInput.text.Trim());
        form.AddField("password", passwordInput.text.Trim());

        UnityWebRequest request = UnityWebRequest.Post(url, form);

        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        Debug.Log("Login Response : " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            LoginResponse response =
                JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

            if (!string.IsNullOrEmpty(response.access_token))
            {
                // SAVE TOKEN
                PlayerPrefs.SetString("ACCESS_TOKEN", response.access_token);
                PlayerPrefs.Save();

                statusText.text = "Login Successful";

                loginPanel.SetActive(false);

                Debug.Log("Token Saved");

                // CALL NEXT API
                StartCoroutine(GetStudentCourses());
            }
            else
            {
                statusText.text = "email or password is incorrect";
            }
        }
        else
        {
            Debug.LogError(request.error);

            statusText.text = "email or password is incorrect";
        }
    }

    

    IEnumerator GetStudentCourses()
    {
        statusText.text = "Getting Courses...";

        string studentUUID = "315f5cb6-7313-4066-b5d7-cc63174314dc";

        string url =
            baseUrl + "/student-courses-with-lock/" + studentUUID;

        UnityWebRequest request = UnityWebRequest.Get(url);

        // GET TOKEN
        string token = PlayerPrefs.GetString("ACCESS_TOKEN");

        // ADD AUTH HEADER
        request.SetRequestHeader("Authorization", "Bearer " + token);

        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        Debug.Log("Courses Response : " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            statusText.text = "Courses Loaded";

          CoursesResponse response =
          JsonUtility.FromJson<CoursesResponse>(
           request.downloadHandler.text);

       if (response.data.assigned_courses.Length > 0)
     {
      OpenClassSelection(response.data.assigned_courses);

        LoginInput.text = "";
        passwordInput.text = "";
        statusText.text = "";
     }
        }
        else
        {
            statusText.text = "Failed To Load Courses";

            Debug.LogError(request.error);
            Debug.LogError(request.downloadHandler.text);
        }
    }

    // OPTIONAL LOGOUT
    public void Logout()
    {
        PlayerPrefs.DeleteKey("ACCESS_TOKEN");
    }


void OpenClassSelection(AssignedCourse[] assignedCourses)
{
    classSelectionPanel.SetActive(true);

    // LOCK EVERYTHING FIRST

    beginnersButton.interactable = false;
    juniorsButton.interactable = false;
    seniorsButton.interactable = false;
    mastersButton.interactable = false;

    beginnersLock.SetActive(true);
    juniorsLock.SetActive(true);
    seniorsLock.SetActive(true);
    mastersLock.SetActive(true);

    // CHECK ASSIGNED COURSES

    foreach (AssignedCourse course in assignedCourses)
    {
        Debug.Log("Assigned Course : " + course.name);

        switch (course.id)
        {
            // BEGINNERS
            case 12:
                beginnersButton.interactable = true;
                beginnersLock.SetActive(false);
                break;

            // JUNIORS
            case 14:
                juniorsButton.interactable = true;
                juniorsLock.SetActive(false);
                break;

            // SENIORS
            case 16:
                seniorsButton.interactable = true;
                seniorsLock.SetActive(false);
                break;

            // MASTERS
            case 19:
                mastersButton.interactable = true;
                mastersLock.SetActive(false);
                break;
        }
    }
}
}



[System.Serializable]
public class LoginResponse
{
    public int status;
    public string message;
    public string access_token;
}

[System.Serializable]
public class CoursesResponse
{
    public bool success;
    public CoursesData data;
    public int status;
}

[System.Serializable]
public class CoursesData
{
    public AssignedCourse[] assigned_courses;
}

[System.Serializable]
public class AssignedCourse
{
    public int id;
    public string name;
    public string assigned_course_id;
}