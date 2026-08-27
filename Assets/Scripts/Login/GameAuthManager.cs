using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("No course assigned UI")]
    public GameObject noCourseAssignedPanel;
    public TMP_Text noCourseAssignedText;

    [Header("Class Selection")]
    public GameObject classSelectionPanel;

    [Tooltip("The Image component on each category's lock icon (same GameObject you " +
             "were previously showing/hiding — now its sprite gets swapped instead).")]
    public Image beginnersLock;
    public Image juniorsLock;
    public Image seniorsLock;
    public Image mastersLock;

    [Header("Lock Icon Sprites")]
    [Tooltip("Sprite shown on a category's lock Image while it's locked.")]
    public Sprite lockedSprite;
    [Tooltip("Sprite shown on a category's lock Image once it's unlocked.")]
    public Sprite unlockedSprite;

    [Header("Level Select Carousel")]
    public LevelSelectCarousel levelSelectCarousel;

    [Header("User Greeting")]
    [Tooltip("Drag ALL GreetingText TMP objects from every home screen here.")]
    public TextMeshProUGUI[] greetingTexts;
    public string greetingFormat = "Hi, {0}!";

    // ── Course ID → carousel category ─────────────────────────────────────────
    [Header("Course ID Mapping (all level ids per category)")]
    [Tooltip("Every course id across all levels of Beginners (Level 1, 2, 3, 4...). " +
             "Beginners unlocks if the student is assigned ANY of these. " +
             "Only Level 1 & 2 ids confirmed so far — add 3/4 once known.")]
    public int[] beginnersCourseIds = { 12, 13 };

    [Tooltip("Every course id across all levels of Juniors. " +
             "Only Level 1 & 2 ids confirmed so far — add 3/4 once known.")]
    public int[] juniorsCourseIds = { 14, 15 };

    [Tooltip("Every course id across all levels of Seniors.")]
    public int[] seniorsCourseIds = { 16, 17, 18 };

    [Tooltip("Every course id across all levels of Masters.")]
    public int[] mastersCourseIds = { 19, 20, 21 };

    private string baseUrl = "https://gamedevpanel.com/api";

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // ── Windows 7 TLS Fix ────────────────────────────────────────────────
        try
        {
            System.Net.ServicePointManager.SecurityProtocol =
                System.Net.SecurityProtocolType.Tls12 |
                System.Net.SecurityProtocolType.Tls11 |
                System.Net.SecurityProtocolType.Tls;

            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                delegate { return true; };

            Debug.Log("[TLS] Security protocol set: TLS 1.0, 1.1, 1.2 enabled.");
            Debug.Log("[TLS] Current protocol: " + System.Net.ServicePointManager.SecurityProtocol);
        }
        catch (Exception ex)
        {
            Debug.LogError("[TLS] Failed to set security protocol: " + ex.Message);
        }
        // ── End Windows 7 TLS Fix ────────────────────────────────────────────
    }

    void Start()
    {
        loginPanel.SetActive(false);
        classSelectionPanel.SetActive(false);
        noCourseAssignedPanel.SetActive(false);

        Debug.Log("[GameAuthManager] Start() called.");
        Debug.Log("[GameAuthManager] Platform: " + Application.platform);
        Debug.Log("[GameAuthManager] Internet reachability: " + Application.internetReachability);

        if (PlayerPrefs.HasKey("ACCESS_TOKEN") &&
            string.IsNullOrEmpty(AppSession.UserName))
        {
            string saved = PlayerPrefs.GetString("ACCESS_TOKEN");
            Debug.Log("[GameAuthManager] Found saved ACCESS_TOKEN. Restoring session...");
            AppSession.UserName  = DecodeUserNameFromJwt(saved);
            AppSession.StudentId = DecodeStudentIdFromJwt(saved);
            Debug.Log("RESTORED STUDENT ID = " + AppSession.StudentId);
            Debug.Log("RESTORED USER NAME = " + AppSession.UserName);
        }
        else
        {
            Debug.Log("[GameAuthManager] No saved session found. User needs to login.");
        }

        StartCoroutine(RefreshUINextFrame());
    }

    private IEnumerator RefreshUINextFrame()
    {
        yield return null;

        if (!string.IsNullOrEmpty(AppSession.UserName))
        {
            ShowGreeting(AppSession.UserName);
            Debug.Log("[GameAuthManager] Greeting refreshed on startup: " + AppSession.UserName);
        }
    }

    public void ApplySessionState()
    {
        ShowGreeting(AppSession.UserName);

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("[GameAuthManager] ApplySessionState: No internet. Loading cache...");
            if (LoadCachedCourses()) return;
        }

        if (string.IsNullOrEmpty(AppSession.StudentId))
        {
            Debug.LogWarning("[GameAuthManager] ApplySessionState: no student ID. Checking for cache...");
            LoadCachedCourses();
            return;
        }

        StartCoroutine(GetStudentCourses(AppSession.StudentId));
    }

    // ── Play button ───────────────────────────────────────────────────────────
    public void OnPlayButton()
    {
        classSelectionPanel.SetActive(false);
        Debug.Log("[GameAuthManager] OnPlayButton() called.");

        if (PlayerPrefs.HasKey("ACCESS_TOKEN"))
        {
            Debug.Log("[GameAuthManager] Already Logged In. Token found.");
            ShowGreeting(AppSession.UserName);

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.Log("[GameAuthManager] No internet on play button. Loading cache...");
                if (LoadCachedCourses()) return;
            }

            if (string.IsNullOrEmpty(AppSession.StudentId))
            {
                Debug.LogWarning("[GameAuthManager] OnPlayButton: no student ID. Checking for cache...");
                LoadCachedCourses();
                return;
            }

            StartCoroutine(GetStudentCourses(AppSession.StudentId));
        }
        else
        {
            Debug.Log("[GameAuthManager] No token found. Showing login panel.");
            loginPanel.SetActive(true);
        }
    }

    // ── Login button ──────────────────────────────────────────────────────────
    public void OnLoginButton() => StartCoroutine(LoginCoroutine());

    IEnumerator LoginCoroutine()
    {
        Debug.Log("[LOGIN] ========== LOGIN ATTEMPT STARTED ==========");
        Debug.Log("[LOGIN] Platform: " + Application.platform);
        Debug.Log("[LOGIN] Internet Reachability: " + Application.internetReachability);
        Debug.Log("[LOGIN] Base URL: " + baseUrl);
        Debug.Log("[LOGIN] TLS Protocol: " + System.Net.ServicePointManager.SecurityProtocol);

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("[LOGIN] No internet connection detected.");
            statusText.text = "No Internet Connection";
            yield break;
        }

        if (string.IsNullOrEmpty(LoginInput.text.Trim()))
        {
            Debug.LogWarning("[LOGIN] Email field is empty!");
            statusText.text = "Please enter your email.";
            yield break;
        }

        if (string.IsNullOrEmpty(passwordInput.text.Trim()))
        {
            Debug.LogWarning("[LOGIN] Password field is empty!");
            statusText.text = "Please enter your password.";
            yield break;
        }

        Debug.Log("[LOGIN] Email entered: " + LoginInput.text.Trim());
        Debug.Log("[LOGIN] Password length: " + passwordInput.text.Trim().Length);

        statusText.text = "Logging in...";

        WWWForm form = new WWWForm();
        form.AddField("login",    LoginInput.text.Trim());
        form.AddField("password", passwordInput.text.Trim());

        string loginUrl = baseUrl + "/auth/login";
        Debug.Log("[LOGIN] Sending POST request to: " + loginUrl);

        UnityWebRequest req = UnityWebRequest.Post(loginUrl, form);
        req.SetRequestHeader("Accept", "application/json");
        req.timeout = 30;
        req.certificateHandler = new BypassCertificate();

        Debug.Log("[LOGIN] Certificate handler: BypassCertificate set.");
        Debug.Log("[LOGIN] Timeout: 30 seconds");
        Debug.Log("[LOGIN] Sending request...");

        float startTime = Time.time;
        yield return req.SendWebRequest();
        float elapsed = Time.time - startTime;

        Debug.Log("[LOGIN] ========== LOGIN RESPONSE ==========");
        Debug.Log("[LOGIN] Time taken: " + elapsed.ToString("F2") + " seconds");
        Debug.Log("[LOGIN] Result: " + req.result);
        Debug.Log("[LOGIN] Response Code: " + req.responseCode);
        Debug.Log("[LOGIN] Error: " + (string.IsNullOrEmpty(req.error) ? "None" : req.error));
        Debug.Log("[LOGIN] Response Body: " + (req.downloadHandler != null ? req.downloadHandler.text : "NULL"));

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[LOGIN] Request succeeded. Parsing response...");

            LoginResponse response = null;
            try
            {
                response = JsonUtility.FromJson<LoginResponse>(req.downloadHandler.text);
                Debug.Log("[LOGIN] JSON parsed successfully.");
                Debug.Log("[LOGIN] access_token empty: " + string.IsNullOrEmpty(response.access_token));
                Debug.Log("[LOGIN] status: " + response.status);
                Debug.Log("[LOGIN] message: " + response.message);
            }
            catch (Exception ex)
            {
                Debug.LogError("[LOGIN] JSON parsing failed: " + ex.Message);
                statusText.text = "Login failed. Invalid server response.";
                yield break;
            }

            if (!string.IsNullOrEmpty(response.access_token))
            {
                Debug.Log("[LOGIN] Access token received! Saving session...");
                PlayerPrefs.SetString("ACCESS_TOKEN", response.access_token);
                PlayerPrefs.Save();

                AppSession.UserName  = DecodeUserNameFromJwt(response.access_token);
                AppSession.StudentId = DecodeStudentIdFromJwt(response.access_token);

                Debug.Log("[LOGIN] User Name: " + AppSession.UserName);
                Debug.Log("[LOGIN] Student ID: " + AppSession.StudentId);

                ShowGreeting(AppSession.UserName);
                statusText.text = "Login Successful";
                loginPanel.SetActive(false);

                Debug.Log("[LOGIN] Login successful! Loading courses...");
                StartCoroutine(GetStudentCourses(AppSession.StudentId));
            }
            else
            {
                Debug.LogWarning("[LOGIN] Access token is EMPTY in response!");
                Debug.LogWarning("[LOGIN] Full response: " + req.downloadHandler.text);
                statusText.text = "Email or password is incorrect";
            }
        }
        else
        {
            Debug.LogError("[LOGIN] ========== LOGIN FAILED ==========");
            Debug.LogError("[LOGIN] Result: " + req.result);
            Debug.LogError("[LOGIN] Response Code: " + req.responseCode);
            Debug.LogError("[LOGIN] Error Message: " + req.error);
            Debug.LogError("[LOGIN] Response Body: " + (req.downloadHandler != null ? req.downloadHandler.text : "NULL"));
            Debug.LogError("[LOGIN] Time taken: " + elapsed.ToString("F2") + " seconds");
            Debug.LogError("[LOGIN] TLS Protocol at failure: " + System.Net.ServicePointManager.SecurityProtocol);

            if (req.responseCode == 0)
            {
                Debug.LogError("[LOGIN] Code 0 = Connection completely failed!");
                Debug.LogError("[LOGIN] Causes: TLS mismatch, firewall, proxy, no internet");
                statusText.text = "Network error. Check connection, proxy, or TLS.";
            }
            else if (req.responseCode == 401)
            {
                Debug.LogError("[LOGIN] 401 = Wrong credentials or expired token");
                statusText.text = "Email or password is incorrect.";
            }
            else if (req.responseCode == 403)
            {
                Debug.LogError("[LOGIN] 403 = Account disabled or blocked");
                statusText.text = "Access denied. Contact administrator.";
            }
            else if (req.responseCode == 404)
            {
                Debug.LogError("[LOGIN] 404 = Wrong API URL: " + loginUrl);
                statusText.text = "Server error. Please contact support.";
            }
            else if (req.responseCode == 405)
            {
                Debug.LogError("[LOGIN] 405 = Wrong HTTP method used");
                statusText.text = "Server error: Method Not Allowed.";
            }
            else if (req.responseCode >= 500)
            {
                Debug.LogError("[LOGIN] 500+ = Backend server error");
                statusText.text = "Server error. Please try again later.";
            }
            else if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError("[LOGIN] ConnectionError = Network/TLS/Proxy issue");
                statusText.text = "Connection failed. Check your network.";
            }
            else if (req.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("[LOGIN] ProtocolError = Server returned error response");
                statusText.text = "Login failed. Code: " + req.responseCode;
            }
            else
            {
                Debug.LogError("[LOGIN] Unknown error");
                statusText.text = "Login failed. See console for details.";
            }
        }

        Debug.Log("[LOGIN] ========== LOGIN ATTEMPT ENDED ==========");
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
                Debug.LogError("[JWT] Invalid token format. Parts: " + parts.Length);
                return null;
            }

            string payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "=";  break;
            }

            string json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            Debug.Log($"[JWT] Payload decoded: {json}");
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

        if (HomeScreenManager.Instance != null)
            HomeScreenManager.Instance.RefreshGreeting();
    }

    private bool LoadCachedCourses()
    {
        Debug.Log("[CACHE] Attempting to load cached courses...");
        if (PlayerPrefs.HasKey("CACHED_COURSES"))
        {
            try
            {
                string cachedJson = PlayerPrefs.GetString("CACHED_COURSES");
                if (!string.IsNullOrEmpty(cachedJson))
                {
                    CoursesResponse response = JsonUtility.FromJson<CoursesResponse>(cachedJson);
                    if (response?.data?.assigned_courses != null && response.data.assigned_courses.Length > 0)
                    {
                        Debug.Log("[CACHE] Loaded successfully. Count: " + response.data.assigned_courses.Length);
                        if (statusText != null) statusText.text = "";
                        OpenClassSelection(response.data.assigned_courses);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CACHE] Error loading: {ex.Message}");
            }
        }
        Debug.LogWarning("[CACHE] No cached courses found.");
        return false;
    }

    // ── Courses API ───────────────────────────────────────────────────────────
    IEnumerator GetStudentCourses(string studentId)
    {
        Debug.Log("[COURSES] ========== FETCHING COURSES ==========");
        Debug.Log("[COURSES] Student ID: " + studentId);
        Debug.Log("[COURSES] Internet: " + Application.internetReachability);

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("[COURSES] No internet. Loading cached courses...");
            if (LoadCachedCourses()) yield break;
            statusText.text = "No Internet Connection";
            yield break;
        }

        statusText.text = "Getting Courses...";

        string token = PlayerPrefs.GetString("ACCESS_TOKEN");
        string coursesUrl = baseUrl + "/student-courses-with-lock/" + studentId;

        Debug.Log("[COURSES] URL: " + coursesUrl);
        Debug.Log("[COURSES] Token length: " + token.Length);

        UnityWebRequest req = UnityWebRequest.Get(coursesUrl);
        req.SetRequestHeader("Authorization", "Bearer " + token);
        req.SetRequestHeader("Accept", "application/json");
        req.certificateHandler = new BypassCertificate();

        float startTime = Time.time;
        yield return req.SendWebRequest();
        float elapsed = Time.time - startTime;

        Debug.Log("[COURSES] ========== COURSES RESPONSE ==========");
        Debug.Log("[COURSES] Time: " + elapsed.ToString("F2") + "s");
        Debug.Log("[COURSES] Result: " + req.result);
        Debug.Log("[COURSES] Code: " + req.responseCode);
        Debug.Log("[COURSES] Error: " + (string.IsNullOrEmpty(req.error) ? "None" : req.error));
        Debug.Log("[COURSES] Response: " + req.downloadHandler?.text);

        if (req.result == UnityWebRequest.Result.Success)
        {
            statusText.text = "Courses Loaded";

            CoursesResponse response = null;
            try
            {
                response = JsonUtility.FromJson<CoursesResponse>(req.downloadHandler.text);
                Debug.Log("[COURSES] JSON parsed successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[COURSES] Json parsing failed: {ex.Message}");
            }

            if (response?.data?.assigned_courses != null &&
                response.data.assigned_courses.Length > 0)
            {
                Debug.Log("[COURSES] Courses found: " + response.data.assigned_courses.Length);
                PlayerPrefs.SetString("CACHED_COURSES", req.downloadHandler.text);
                PlayerPrefs.Save();

                OpenClassSelection(response.data.assigned_courses);
                LoginInput.text    = "";
                passwordInput.text = "";
                statusText.text    = "";
            }
            else
            {
                Debug.LogWarning("[COURSES] No courses assigned to student.");
                statusText.text = "No courses assigned.";

                noCourseAssignedPanel.SetActive(true);
                noCourseAssignedText.text = "No Courses Assigned to this user. Please contact your administrator.";
                
            }
        }
        else
        {
            Debug.LogError("[COURSES] ========== COURSES FAILED ==========");
            Debug.LogError("[COURSES] Result: " + req.result);
            Debug.LogError("[COURSES] Code: " + req.responseCode);
            Debug.LogError("[COURSES] Error: " + req.error);

            if (req.responseCode == 401)
            {
                Debug.LogWarning("[COURSES] 401 = Token expired. Logging out...");
                Logout();
                statusText.text = "Session expired. Please log in again.";
                yield break;
            }

            Debug.LogWarning("[COURSES] API failed. Loading cached courses...");
            if (LoadCachedCourses()) yield break;

            statusText.text = "Failed To Load Courses";
            if (req.downloadHandler != null)
                Debug.LogError(req.error + "\n" + req.downloadHandler.text);
            else
                Debug.LogError(req.error);
        }

        Debug.Log("[COURSES] ========== COURSES FETCH ENDED ==========");
    }
    
    public void OnNoCoursesOkButton()
    {
        noCourseAssignedPanel.SetActive(false);
        loginPanel.SetActive(true);
        statusText.text = "";
        LoginInput.text = "";
        passwordInput.text = "";

        Logout();
    }
    // ── Logout ────────────────────────────────────────────────────────────────
    public void Logout()
    {
        Debug.Log("[GameAuthManager] Logout() called. Clearing session...");
        PlayerPrefs.DeleteKey("ACCESS_TOKEN");
        PlayerPrefs.DeleteKey("CACHED_COURSES");
        PlayerPrefs.Save();
        AppSession.ClearAll();
        ShowGreeting(null);

        if (HomeScreenManager.Instance != null)
        {
            HomeScreenManager.Instance.SetCategoryLockStates(new bool[4]);
            HomeScreenManager.Instance.SetUnlockedCourseIds(new HashSet<int>());
        }

        if (classSelectionPanel != null) classSelectionPanel.SetActive(false);
        if (loginPanel != null)          loginPanel.SetActive(true);
        Debug.Log("[GameAuthManager] Logout complete.");
    }

    // ── Class selection ───────────────────────────────────────────────────────
    private void OpenClassSelection(AssignedCourse[] courses)
    {
        Debug.Log("[CLASS] Opening class selection. Total courses: " + courses.Length);
        classSelectionPanel.SetActive(true);

        bool[] unlocked = new bool[4];
        var unlockedCourseIds = new HashSet<int>();

        SetLockSprite(beginnersLock, true);
        SetLockSprite(juniorsLock,   true);
        SetLockSprite(seniorsLock,   true);
        SetLockSprite(mastersLock,   true);

        foreach (AssignedCourse c in courses)
        {
            Debug.Log($"[CLASS] Course: {c.name} (id={c.id})");
            unlockedCourseIds.Add(c.id);

            if (Contains(beginnersCourseIds, c.id)) { unlocked[0] = true; SetLockSprite(beginnersLock, false); Debug.Log("[CLASS] Beginners UNLOCKED"); }
            if (Contains(juniorsCourseIds,   c.id)) { unlocked[1] = true; SetLockSprite(juniorsLock,   false); Debug.Log("[CLASS] Juniors UNLOCKED"); }
            if (Contains(seniorsCourseIds,   c.id)) { unlocked[2] = true; SetLockSprite(seniorsLock,   false); Debug.Log("[CLASS] Seniors UNLOCKED"); }
            if (Contains(mastersCourseIds,   c.id)) { unlocked[3] = true; SetLockSprite(mastersLock,   false); Debug.Log("[CLASS] Masters UNLOCKED"); }
        }

        if (HomeScreenManager.Instance != null)
        {
            HomeScreenManager.Instance.SetCategoryLockStates(unlocked);
            HomeScreenManager.Instance.SetUnlockedCourseIds(unlockedCourseIds);
        }
        else
        {
            Debug.LogWarning("[CLASS] HomeScreenManager.Instance is null.");
        }

        int first = Array.FindIndex(unlocked, u => u);
        if (first < 0) first = 0;

        Debug.Log($"[CLASS] First unlocked index: {first}");

        if (levelSelectCarousel != null)
            levelSelectCarousel.ScrollToIndex(first);
        else
            Debug.LogWarning("[CLASS] levelSelectCarousel not assigned.");
    }

    private void SetLockSprite(Image img, bool locked)
    {
        if (img == null) return;
        img.sprite = locked ? lockedSprite : unlockedSprite;
    }

    private static bool Contains(int[] ids, int id) =>
        ids != null && Array.IndexOf(ids, id) >= 0;
}

// ── Certificate bypass for Windows 7 SSL ─────────────────────────────────────
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        Debug.Log("[CERT] Certificate validation bypassed for Windows 7 compatibility.");
        return true;
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
