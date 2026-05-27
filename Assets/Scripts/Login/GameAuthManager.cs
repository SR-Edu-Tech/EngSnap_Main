using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class GameAuthManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static GameAuthManager Instance { get; private set; }

    [Header("Login UI")]
    public GameObject     loginPanel;
    public TMP_InputField LoginInput;
    public TMP_InputField passwordInput;
    public TMP_Text       statusText;

    [Header("Class Selection")]
    public GameObject classSelectionPanel;
    public GameObject beginnersLock;
    public GameObject juniorsLock;
    public GameObject seniorsLock;
    public GameObject mastersLock;

    [Header("Level Select Carousel")]
    public LevelSelectCarousel levelSelectCarousel;

    [Header("User Greeting")]
    [Tooltip("Drag ALL GreetingText TMP objects from every home screen here.")]
    public TextMeshProUGUI[] greetingTexts;
    public string greetingFormat = "Hi, {0}!";

    // ── Course ID → carousel index ────────────────────────────────────────────
    private const int COURSE_BEGINNERS = 12;
    private const int COURSE_JUNIORS   = 14;
    private const int COURSE_SENIORS   = 16;
    private const int COURSE_MASTERS   = 19;

    private string baseUrl = "https://gamedevpanel.com/api";

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        loginPanel.SetActive(false);
        classSelectionPanel.SetActive(false);

        // Restore session from saved token. AppSession is static and resets
        // on every Play-mode start, but PlayerPrefs persist.
        if (PlayerPrefs.HasKey("ACCESS_TOKEN") &&
            string.IsNullOrEmpty(AppSession.UserName))
        {
            string saved = PlayerPrefs.GetString("ACCESS_TOKEN");
            AppSession.UserName  = DecodeUserNameFromJwt(saved);
            AppSession.StudentId = DecodeStudentIdFromJwt(saved);
            Debug.Log("RESTORED STUDENT ID = " + AppSession.StudentId);
        }

        // FIX: Push the greeting into HomeScreenManager as soon as we have the
        // name. HomeScreenManager.Start() also calls RefreshGreeting(), but if
        // GameAuthManager.Start() runs AFTER it the name would be missed.
        // Calling it here guarantees it runs whenever the name is available.
        // We also wait one frame so all MonoBehaviours have finished their own
        // Start() before we touch HomeScreenManager.
        StartCoroutine(RefreshUINextFrame());
    }

    // Wait one frame so HomeScreenManager.Awake/Start has definitely run,
    // then push the restored name into its greeting label.
    private IEnumerator RefreshUINextFrame()
    {
        yield return null; // one frame

        if (!string.IsNullOrEmpty(AppSession.UserName))
        {
            ShowGreeting(AppSession.UserName);
            Debug.Log("[GameAuthManager] Greeting refreshed on startup: " +
                      AppSession.UserName);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Called by HomeScreenManager via FirstRunDownloader AFTER the selection
    //  panel is visible.  This is the correct time to apply lock states.
    //
    //  HOW TO WIRE:
    //  In FirstRunDownloader.OpenNextPanel(), after ShowSelectionPanel(),
    //  call GameAuthManager.Instance.ApplySessionState().
    //  (See FirstRunDownloader.cs — the call is already added there.)
    // ─────────────────────────────────────────────────────────────────────────
    public void ApplySessionState()
    {
        if (string.IsNullOrEmpty(AppSession.StudentId))
        {
            Debug.LogWarning("[GameAuthManager] ApplySessionState: no student ID.");
            return;
        }

        ShowGreeting(AppSession.UserName);
        StartCoroutine(GetStudentCourses(AppSession.StudentId));
    }

    // ── Play button (kept for scenes that wire it directly) ───────────────────
    public void OnPlayButton()
    {
        classSelectionPanel.SetActive(false);

        if (PlayerPrefs.HasKey("ACCESS_TOKEN"))
        {
            Debug.Log("Already Logged In");
            ShowGreeting(AppSession.UserName);
            StartCoroutine(GetStudentCourses(AppSession.StudentId));
        }
        else
        {
            loginPanel.SetActive(true);
        }
    }

    // ── Login button ──────────────────────────────────────────────────────────
    public void OnLoginButton() => StartCoroutine(LoginCoroutine());

    IEnumerator LoginCoroutine()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            statusText.text = "No Internet Connection";
            yield break;
        }

        statusText.text = "Logging in...";

        WWWForm form = new WWWForm();
        form.AddField("login",    LoginInput.text.Trim());
        form.AddField("password", passwordInput.text.Trim());

        UnityWebRequest req = UnityWebRequest.Post(baseUrl + "/auth/login", form);
        req.SetRequestHeader("Accept", "application/json");
        yield return req.SendWebRequest();

        Debug.Log("Login Response: " + req.downloadHandler.text);

        if (req.result == UnityWebRequest.Result.Success)
        {
            LoginResponse response =
                JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);

            if (!string.IsNullOrEmpty(response.access_token))
            {
                PlayerPrefs.SetString("ACCESS_TOKEN", response.access_token);
                PlayerPrefs.Save();

                AppSession.UserName  = DecodeUserNameFromJwt(response.access_token);
                AppSession.StudentId = DecodeStudentIdFromJwt(response.access_token);

                ShowGreeting(AppSession.UserName);

                Debug.Log($"Token Saved | User: {AppSession.UserName} | " +
                          $"Student: {AppSession.StudentId}");

                statusText.text = "Login Successful";
                loginPanel.SetActive(false);

                StartCoroutine(GetStudentCourses(AppSession.StudentId));
            }
            else
            {
                statusText.text = "Email or password is incorrect";
            }
        }
        else
        {
            Debug.LogError(req.error);
            statusText.text = "Email or password is incorrect";
        }
    }

    // ── JWT decoders ──────────────────────────────────────────────────────────
    private string DecodeUserNameFromJwt(string jwt) =>
        DecodeJwtPayload(jwt)?.user_name ?? string.Empty;

    private string DecodeStudentIdFromJwt(string jwt) =>
        DecodeJwtPayload(jwt)?.student_id ?? string.Empty;

    private JwtPayload DecodeJwtPayload(string jwt)
    {
        try
        {
            string[] parts = jwt.Split('.');
            if (parts.Length != 3)
            {
                Debug.LogError("[JWT] Invalid token format.");
                return null;
            }

            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "=";  break;
            }

            string json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            Debug.Log($"[JWT] Payload: {json}");
            return JsonUtility.FromJson<JwtPayload>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[JWT] Decode failed: {ex.Message}");
            return null;
        }
    }

    // ── Greeting ──────────────────────────────────────────────────────────────
    private void ShowGreeting(string userName)
    {
        string text = !string.IsNullOrEmpty(userName)
            ? string.Format(greetingFormat, userName)
            : string.Empty;

        if (greetingTexts != null)
            foreach (var tmp in greetingTexts)
                if (tmp != null) tmp.text = text;

        // Keep HomeScreenManager's own label in sync too
        if (HomeScreenManager.Instance != null)
            HomeScreenManager.Instance.RefreshGreeting();
    }

    // ── Courses API ───────────────────────────────────────────────────────────
    IEnumerator GetStudentCourses(string studentId)
    {
        statusText.text = "Getting Courses...";

        string token = PlayerPrefs.GetString("ACCESS_TOKEN");
        UnityWebRequest req = UnityWebRequest.Get(
            baseUrl + "/student-courses-with-lock/" + studentId);
        req.SetRequestHeader("Authorization", "Bearer " + token);
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        Debug.Log("Courses Response: " + req.downloadHandler.text);

        if (req.result == UnityWebRequest.Result.Success)
        {
            statusText.text = "Courses Loaded";

            CoursesResponse response =
                JsonUtility.FromJson<CoursesResponse>(req.downloadHandler.text);

            if (response?.data?.assigned_courses != null &&
                response.data.assigned_courses.Length > 0)
            {
                OpenClassSelection(response.data.assigned_courses);
                LoginInput.text    = "";
                passwordInput.text = "";
                statusText.text    = "";
            }
            else
            {
                statusText.text = "No courses assigned.";
                Debug.LogWarning("[GameAuthManager] assigned_courses is empty or null.");
            }
        }
        else
        {
            statusText.text = "Failed To Load Courses";
            Debug.LogError(req.error + "\n" + req.downloadHandler.text);
        }
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    public void Logout()
    {
        PlayerPrefs.DeleteKey("ACCESS_TOKEN");
        AppSession.ClearAll();
        ShowGreeting(null);

        if (HomeScreenManager.Instance != null)
            HomeScreenManager.Instance.SetCategoryLockStates(new bool[4]);
    }

    // ── Class selection ───────────────────────────────────────────────────────
    private void OpenClassSelection(AssignedCourse[] courses)
    {
        classSelectionPanel.SetActive(true);

        bool[] unlocked = new bool[4];

        SetLockOverlay(beginnersLock, true);
        SetLockOverlay(juniorsLock,   true);
        SetLockOverlay(seniorsLock,   true);
        SetLockOverlay(mastersLock,   true);

        foreach (AssignedCourse c in courses)
        {
            Debug.Log($"[GameAuthManager] Assigned: {c.name} (id={c.id})");
            switch (c.id)
            {
                case COURSE_BEGINNERS: unlocked[0] = true; SetLockOverlay(beginnersLock, false); break;
                case COURSE_JUNIORS:   unlocked[1] = true; SetLockOverlay(juniorsLock,   false); break;
                case COURSE_SENIORS:   unlocked[2] = true; SetLockOverlay(seniorsLock,   false); break;
                case COURSE_MASTERS:   unlocked[3] = true; SetLockOverlay(mastersLock,   false); break;
            }
        }

        // Apply to HomeScreenManager's category buttons
        if (HomeScreenManager.Instance != null)
            HomeScreenManager.Instance.SetCategoryLockStates(unlocked);
        else
            Debug.LogWarning("[GameAuthManager] HomeScreenManager.Instance is null — " +
                             "lock states not applied.");

        // Scroll carousel to first unlocked index
        int first = Array.FindIndex(unlocked, u => u);
        if (first < 0) first = 0;

        Debug.Log($"[GameAuthManager] First unlocked index: {first}");

        if (levelSelectCarousel != null)
            levelSelectCarousel.ScrollToIndex(first);
        else
            Debug.LogWarning("[GameAuthManager] levelSelectCarousel not assigned.");
    }

    private static void SetLockOverlay(GameObject go, bool show)
    {
        if (go != null) go.SetActive(show);
    }
}

// ── Data models ───────────────────────────────────────────────────────────────

[System.Serializable] public class LoginResponse
{
    public int    status;
    public string message;
    public string access_token;
}

[System.Serializable] public class JwtPayload
{
    public string user_name;
    public string user_email;
    public string role_name;
    public string student_id;
}

[System.Serializable] public class CoursesResponse
{
    public bool        success;
    public CoursesData data;
    public int         status;
}

[System.Serializable] public class CoursesData
{
    public AssignedCourse[] assigned_courses;
}

[System.Serializable] public class AssignedCourse
{
    public int    id;
    public string name;
    public string assigned_course_id;
}
