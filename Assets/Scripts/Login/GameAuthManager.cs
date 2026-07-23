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
    // Each category can have multiple levels (Level 1, 2, 3, 4...), each with its
    // own course id from the backend. List EVERY level id that belongs to a
    // category here — the category unlocks if the student is assigned ANY one
    // of them. Fill these in from your backend's actual course ids as new
    // levels are added; the ones below are just the ids seen so far.
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
        ShowGreeting(AppSession.UserName);

        // Offline mode check first
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            if (LoadCachedCourses())
            {
                return;
            }
        }

        if (string.IsNullOrEmpty(AppSession.StudentId))
        {
            Debug.LogWarning("[GameAuthManager] ApplySessionState: no student ID. Checking for cache...");
            LoadCachedCourses();
            return;
        }

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

            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                if (LoadCachedCourses())
                {
                    return;
                }
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

    private bool LoadCachedCourses()
    {
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
                        if (statusText != null) statusText.text = "";
                        OpenClassSelection(response.data.assigned_courses);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameAuthManager] Error loading cached courses: {ex.Message}");
            }
        }
        return false;
    }

    // ── Courses API ───────────────────────────────────────────────────────────
    IEnumerator GetStudentCourses(string studentId)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("[GameAuthManager] Offline mode detected. Checking for cached courses...");
            if (LoadCachedCourses())
            {
                yield break;
            }
            statusText.text = "No Internet Connection";
            yield break;
        }

        statusText.text = "Getting Courses...";

        string token = PlayerPrefs.GetString("ACCESS_TOKEN");
        UnityWebRequest req = UnityWebRequest.Get(
            baseUrl + "/student-courses-with-lock/" + studentId);
        req.SetRequestHeader("Authorization", "Bearer " + token);
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        Debug.Log("Courses Response: " + req.downloadHandler?.text);

        if (req.result == UnityWebRequest.Result.Success)
        {
            statusText.text = "Courses Loaded";

            CoursesResponse response = null;
            try
            {
                response = JsonUtility.FromJson<CoursesResponse>(req.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameAuthManager] Json parsing failed: {ex.Message}");
            }

            if (response?.data?.assigned_courses != null &&
                response.data.assigned_courses.Length > 0)
            {
                PlayerPrefs.SetString("CACHED_COURSES", req.downloadHandler.text);
                PlayerPrefs.Save();

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
            if (req.responseCode == 401)
            {
                // Token was rejected outright (expired/invalid) rather than a
                // network hiccup — don't fall back to stale cached courses,
                // since that leaves the user staring at a "locked" class
                // selection screen with no obvious way back to login.
                Debug.LogWarning("[GameAuthManager] Token rejected (401). Session expired — logging out.");
                Logout();
                statusText.text = "Session expired. Please log in again.";
                yield break;
            }

            Debug.LogWarning("[GameAuthManager] API call failed. Loading courses from cache...");
            if (LoadCachedCourses())
            {
                yield break;
            }

            statusText.text = "Failed To Load Courses";
            if (req.downloadHandler != null)
            {
                Debug.LogError(req.error + "\n" + req.downloadHandler.text);
            }
            else
            {
                Debug.LogError(req.error);
            }
        }
    }

    // ── Logout ────────────────────────────────────────────────────────────────
    public void Logout()
    {
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

        // Always land back on the login screen after logout — previously this
        // only cleared data and reset locks but never touched panel visibility,
        // so a logout could leave the (now stale/locked) class selection panel
        // on screen with no way back to login.
        if (classSelectionPanel != null) classSelectionPanel.SetActive(false);
        if (loginPanel != null)          loginPanel.SetActive(true);
    }

    // ── Class selection ───────────────────────────────────────────────────────
    private void OpenClassSelection(AssignedCourse[] courses)
    {
        classSelectionPanel.SetActive(true);

        bool[] unlocked = new bool[4];

        // Every course id the student is assigned, at whatever level (Beginners
        // Level 1 = 12, Level 2 = 13, etc.) — used to lock/unlock individual
        // sub-buttons inside each category's sub-panel, not just the top-level
        // category buttons.
        var unlockedCourseIds = new HashSet<int>();

        SetLockSprite(beginnersLock, true);
        SetLockSprite(juniorsLock,   true);
        SetLockSprite(seniorsLock,   true);
        SetLockSprite(mastersLock,   true);

        foreach (AssignedCourse c in courses)
        {
            Debug.Log($"[GameAuthManager] Assigned: {c.name} (id={c.id})");

            unlockedCourseIds.Add(c.id);

            if (Contains(beginnersCourseIds, c.id)) { unlocked[0] = true; SetLockSprite(beginnersLock, false); }
            if (Contains(juniorsCourseIds,   c.id)) { unlocked[1] = true; SetLockSprite(juniorsLock,   false); }
            if (Contains(seniorsCourseIds,   c.id)) { unlocked[2] = true; SetLockSprite(seniorsLock,   false); }
            if (Contains(mastersCourseIds,   c.id)) { unlocked[3] = true; SetLockSprite(mastersLock,   false); }
        }

        // Apply to HomeScreenManager's category buttons + per-level sub-buttons
        if (HomeScreenManager.Instance != null)
        {
            HomeScreenManager.Instance.SetCategoryLockStates(unlocked);
            HomeScreenManager.Instance.SetUnlockedCourseIds(unlockedCourseIds);
        }
        else
        {
            Debug.LogWarning("[GameAuthManager] HomeScreenManager.Instance is null — " +
                             "lock states not applied.");
        }

        // Scroll carousel to first unlocked index
        int first = Array.FindIndex(unlocked, u => u);
        if (first < 0) first = 0;

        Debug.Log($"[GameAuthManager] First unlocked index: {first}");

        if (levelSelectCarousel != null)
            levelSelectCarousel.ScrollToIndex(first);
        else
            Debug.LogWarning("[GameAuthManager] levelSelectCarousel not assigned.");
    }

    private void SetLockSprite(Image img, bool locked)
    {
        if (img == null) return;
        img.sprite = locked ? lockedSprite : unlockedSprite;
    }

    private static bool Contains(int[] ids, int id) =>
        ids != null && Array.IndexOf(ids, id) >= 0;
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