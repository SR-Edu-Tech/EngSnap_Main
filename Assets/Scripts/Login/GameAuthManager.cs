using System;
using System.Collections;
using System.Text;
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

    [Header("Level Select Carousel")]
    [Tooltip("Drag the LevelSelectCarousel component here so the carousel " +
             "auto-scrolls to the first unlocked button after the API response.")]
    public LevelSelectCarousel levelSelectCarousel;

    [Header("User Greeting")]
    [Tooltip("Drag ALL GreetingText TMP objects from every home screen here. " +
             "All of them will be updated at the same time.")]
    public TextMeshProUGUI[] greetingTexts;

    [Tooltip("Format string — {0} is replaced with the player's name.")]
    public string greetingFormat = "Hi, {0}!";

    private string baseUrl = "https://gamedevpanel.com/api";

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        loginPanel.SetActive(false);
        classSelectionPanel.SetActive(false);

        // Restore username from saved token on every Play mode start.
        // AppSession is static and resets to null when Play mode restarts,
        // but the token is persisted in PlayerPrefs — decode it immediately.
        if (PlayerPrefs.HasKey("ACCESS_TOKEN") &&
            string.IsNullOrEmpty(AppSession.UserName))
        {
            string savedToken = PlayerPrefs.GetString("ACCESS_TOKEN");
            string userName   = DecodeUserNameFromJwt(savedToken);
            AppSession.UserName = userName;
            Debug.Log($"[GameAuthManager] Restored user from saved token: {userName}");
        }
    }

    // ── Play button ───────────────────────────────────────────────────────────

    public void OnPlayButton()
    {
        classSelectionPanel.SetActive(false);

        if (PlayerPrefs.HasKey("ACCESS_TOKEN"))
        {
            Debug.Log("Already Logged In");
            ShowGreeting(AppSession.UserName);
            StartCoroutine(GetStudentCourses());
        }
        else
        {
            loginPanel.SetActive(true);
            classSelectionPanel.SetActive(false);
        }
    }

    // ── Login button ──────────────────────────────────────────────────────────

    public void OnLoginButton()
    {
        StartCoroutine(LoginCoroutine());
    }

    IEnumerator LoginCoroutine()
    {
        statusText.text = "Logging in...";

        WWWForm form = new WWWForm();
        form.AddField("login", LoginInput.text.Trim());
        form.AddField("password", passwordInput.text.Trim());

        UnityWebRequest request = UnityWebRequest.Post(baseUrl + "/auth/login", form);
        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        Debug.Log("Login Response : " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            LoginResponse response =
                JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

            if (!string.IsNullOrEmpty(response.access_token))
            {
                PlayerPrefs.SetString("ACCESS_TOKEN", response.access_token);
                PlayerPrefs.Save();

                string userName     = DecodeUserNameFromJwt(response.access_token);
                AppSession.UserName = userName;
                ShowGreeting(userName);

                Debug.Log($"Token Saved | User from JWT: {userName}");

                statusText.text = "Login Successful";
                loginPanel.SetActive(false);

                StartCoroutine(GetStudentCourses());
            }
            else
            {
                statusText.text = "Email or password is incorrect";
            }
        }
        else
        {
            Debug.LogError(request.error);
            statusText.text = "Email or password is incorrect";
        }
    }

    // ── JWT decoder ───────────────────────────────────────────────────────────

    private string DecodeUserNameFromJwt(string jwt)
    {
        try
        {
            string[] parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                Debug.LogError("[JWT] Invalid token format.");
                return string.Empty;
            }

            string payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "=";  break;
            }

            string json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            Debug.Log($"[JWT] Payload: {json}");

            JwtPayload jwtPayload = JsonUtility.FromJson<JwtPayload>(json);
            return jwtPayload?.user_name ?? string.Empty;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JWT] Failed to decode token: {ex.Message}");
            return string.Empty;
        }
    }

    // ── Greeting helper — updates ALL assigned TMP fields ─────────────────────

    private void ShowGreeting(string userName)
    {
        if (greetingTexts == null || greetingTexts.Length == 0)
        {
            Debug.LogWarning("[GameAuthManager] No greetingTexts assigned in Inspector.");
            return;
        }

        string text = !string.IsNullOrEmpty(userName)
            ? string.Format(greetingFormat, userName)
            : string.Empty;

        foreach (TextMeshProUGUI tmp in greetingTexts)
        {
            if (tmp != null)
                tmp.text = text;
        }
    }

    // ── Courses ───────────────────────────────────────────────────────────────

    IEnumerator GetStudentCourses()
    {
        statusText.text = "Getting Courses...";

        string studentUUID = "315f5cb6-7313-4066-b5d7-cc63174314dc";
        string url = baseUrl + "/student-courses-with-lock/" + studentUUID;

        UnityWebRequest request = UnityWebRequest.Get(url);
        string token = PlayerPrefs.GetString("ACCESS_TOKEN");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        request.SetRequestHeader("Accept", "application/json");

        yield return request.SendWebRequest();

        Debug.Log("Courses Response : " + request.downloadHandler.text);

        if (request.result == UnityWebRequest.Result.Success)
        {
            statusText.text = "Courses Loaded";

            CoursesResponse response =
                JsonUtility.FromJson<CoursesResponse>(request.downloadHandler.text);

            if (response.data.assigned_courses.Length > 0)
            {
                OpenClassSelection(response.data.assigned_courses);
                LoginInput.text    = "";
                passwordInput.text = "";
                statusText.text    = "";
            }
        }
        else
        {
            statusText.text = "Failed To Load Courses";
            Debug.LogError(request.error);
            Debug.LogError(request.downloadHandler.text);
        }
    }

    // ── Logout ────────────────────────────────────────────────────────────────

    public void Logout()
    {
        PlayerPrefs.DeleteKey("ACCESS_TOKEN");
        AppSession.ClearAll();
        ShowGreeting(null);
    }

    // ── Class selection ───────────────────────────────────────────────────────

    void OpenClassSelection(AssignedCourse[] assignedCourses)
    {
        classSelectionPanel.SetActive(true);

        // Default: all locked
        beginnersButton.interactable = false;
        juniorsButton.interactable   = false;
        seniorsButton.interactable   = false;
        mastersButton.interactable   = false;

        beginnersLock.SetActive(true);
        juniorsLock.SetActive(true);
        seniorsLock.SetActive(true);
        mastersLock.SetActive(true);

        // Track which button indices are unlocked by the API response.
        // Carousel button order: 0=Beginners 1=Juniors 2=Seniors 3=Masters
        // Matches ContentXForIndex in LevelSelectCarousel.
        bool[] unlocked = new bool[4]; // all false by default

        foreach (AssignedCourse course in assignedCourses)
        {
            Debug.Log("Assigned Course : " + course.name);
            switch (course.id)
            {
                case 12:
                    beginnersButton.interactable = true;
                    beginnersLock.SetActive(false);
                    unlocked[0] = true;
                    break;
                case 14:
                    juniorsButton.interactable = true;
                    juniorsLock.SetActive(false);
                    unlocked[1] = true;
                    break;
                case 16:
                    seniorsButton.interactable = true;
                    seniorsLock.SetActive(false);
                    unlocked[2] = true;
                    break;
                case 19:
                    mastersButton.interactable = true;
                    mastersLock.SetActive(false);
                    unlocked[3] = true;
                    break;
            }
        }

        // Find the first unlocked index and pass it directly to the carousel.
        // This avoids any timing issue with reading GameObject.activeSelf.
        int firstUnlocked = 0;
        for (int i = 0; i < unlocked.Length; i++)
        {
            if (unlocked[i]) { firstUnlocked = i; break; }
        }

        Debug.Log($"[GameAuthManager] First unlocked index: {firstUnlocked}");

        if (levelSelectCarousel != null)
            levelSelectCarousel.ScrollToIndex(firstUnlocked);
        else
            Debug.LogWarning("[GameAuthManager] levelSelectCarousel not assigned.");
    }
}

// ── Data models ───────────────────────────────────────────────────────────────

[System.Serializable]
public class LoginResponse
{
    public int    status;
    public string message;
    public string access_token;
}

[System.Serializable]
public class JwtPayload
{
    public string user_name;
    public string user_email;
    public string role_name;
    public string user_id;
}

[System.Serializable]
public class CoursesResponse
{
    public bool        success;
    public CoursesData data;
    public int         status;
}

[System.Serializable]
public class CoursesData
{
    public AssignedCourse[] assigned_courses;
}

[System.Serializable]
public class AssignedCourse
{
    public int    id;
    public string name;
    public string assigned_course_id;
}