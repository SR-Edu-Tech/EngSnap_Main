using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI ANIMATOR UTILITY
/// Static helper — call from anywhere in the project.
/// All animations are pure Coroutines, no Animator component needed.
///
/// To start a coroutine from a static context, use:
///   MonoBehaviourHost_MyClass_Game.StartGlobal(UIAnimator_MyClass_Game.ButtonPop(myTransform));
/// </summary>
public static class UIAnimator_MyClass_Game
{
    // ── BUTTON POP (call on any button press) ──────────────────────────────
    public static IEnumerator ButtonPop(Transform t)
    {
        Vector3 orig = t.localScale;
        float   dur  = 0.25f;
        float   e    = 0f;

        // Quick squish down
        while (e < dur * 0.4f)
        {
            e += Time.deltaTime;
            float s = Mathf.Lerp(1f, 0.88f, e / (dur * 0.4f));
            t.localScale = orig * s;
            yield return null;
        }

        // Overshoot spring back
        e = 0f;
        while (e < dur * 0.6f)
        {
            e += Time.deltaTime;
            float s = Mathf.Lerp(0.88f, 1.12f, EaseOut(e / (dur * 0.6f)));
            t.localScale = orig * s;
            yield return null;
        }

        // Settle
        e = 0f;
        while (e < dur * 0.3f)
        {
            e += Time.deltaTime;
            float s = Mathf.Lerp(1.12f, 1f, EaseOut(e / (dur * 0.3f)));
            t.localScale = orig * s;
            yield return null;
        }

        t.localScale = orig;
    }

    // ── FLOATING IDLE (mascots, characters gently bobbing) ─────────────────
    public static IEnumerator FloatIdle(Transform t, float amplitude = 8f, float speed = 1.4f)
    {
        Vector3 orig = t.localPosition;
        while (true)
        {
            float y = Mathf.Sin(Time.time * speed) * amplitude;
            t.localPosition = orig + Vector3.up * y;
            yield return null;
        }
    }

    // ── CONFETTI BURST (spawn emoji / particle RectTransforms) ─────────────
    public static IEnumerator ConfettiBurst(RectTransform parent, GameObject[] confettiPrefabs, int count = 18)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject    prefab = confettiPrefabs[Random.Range(0, confettiPrefabs.Length)];
            GameObject    piece  = Object.Instantiate(prefab, parent);
            RectTransform prt    = piece.GetComponent<RectTransform>();

            prt.anchoredPosition = new Vector2(Random.Range(-80f, 80f), Random.Range(-30f, 30f));

            Vector2 vel      = new Vector2(Random.Range(-300f, 300f), Random.Range(400f, 700f));
            float   rotSpeed = Random.Range(-360f, 360f);
            float   life     = Random.Range(1f, 1.8f);

            MonoBehaviourHost_MyClass_Game.StartGlobal(FlyPiece(prt, vel, rotSpeed, life));
            yield return new WaitForSeconds(0.04f);
        }
    }

    static IEnumerator FlyPiece(RectTransform rt, Vector2 vel, float rotSpeed, float life)
    {
        float       e  = 0f;
        CanvasGroup cg = rt.GetComponent<CanvasGroup>();

        while (e < life)
        {
            e         += Time.deltaTime;
            vel.y     -= 600f * Time.deltaTime;     // gravity
            rt.anchoredPosition += vel * Time.deltaTime;
            rt.localRotation     = Quaternion.Euler(0, 0,
                rt.localRotation.eulerAngles.z + rotSpeed * Time.deltaTime);

            if (cg) cg.alpha = Mathf.Lerp(1f, 0f, (e / life) * (e / life));
            yield return null;
        }

        Object.Destroy(rt.gameObject);
    }

    // ── STAR EARN FLOAT ────────────────────────────────────────────────────
    public static IEnumerator StarFloat(TextMeshProUGUI label)
    {
        Vector3 orig     = label.transform.localScale;
        Color   startCol = new Color(1f, 0.9f, 0.1f);
        Color   endCol   = label.color;

        label.color = startCol;
        float e = 0f, dur = 0.5f;

        while (e < dur)
        {
            e += Time.deltaTime;
            float p = e / dur;
            label.transform.localScale = orig * (1f + 0.45f * Mathf.Sin(p * Mathf.PI));
            label.color                = Color.Lerp(startCol, endCol, p);
            yield return null;
        }

        label.transform.localScale = orig;
        label.color                = endCol;
    }

    // ── SCREEN FLASH (white overlay that fades) ────────────────────────────
    public static IEnumerator ScreenFlash(Image flashImage, float duration = 0.3f)
    {
        flashImage.gameObject.SetActive(true);
        flashImage.color = new Color(1, 1, 1, 0.85f);

        float e = 0f;
        while (e < duration)
        {
            e += Time.deltaTime;
            flashImage.color = new Color(1, 1, 1, Mathf.Lerp(0.85f, 0f, e / duration));
            yield return null;
        }

        flashImage.gameObject.SetActive(false);
    }

    static float EaseOut(float t) => 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
}

// ─────────────────────────────────────────────────────────────────────────────
// MonoBehaviourHost_MyClass_Game
// A persistent singleton MonoBehaviour used to run coroutines from static classes.
//
// SETUP: No manual setup needed — the singleton creates itself on first access.
// It uses DontDestroyOnLoad so it survives scene transitions.
//
// BUG FIX: Added Awake() with duplicate-instance check.
// Without this, loading Screen 1 and Screen 2 in the same session would create
// a second host object, causing confetti and UI animations to run twice or throw
// "coroutine couldn't be started because the the game object is inactive" errors.
// ─────────────────────────────────────────────────────────────────────────────
public class MonoBehaviourHost_MyClass_Game : MonoBehaviour
{
    private static MonoBehaviourHost_MyClass_Game _instance;

    void Awake()
    {
        // BUG FIX: destroy duplicate instances that appear when a new scene loads
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    static MonoBehaviourHost_MyClass_Game Instance
    {
        get
        {
            if (_instance == null)
            {
                // Auto-create if no instance exists yet
                var go = new GameObject("MonoBehaviourHost_MyClass_Game");
                // Awake will assign _instance and call DontDestroyOnLoad
                go.AddComponent<MonoBehaviourHost_MyClass_Game>();
            }
            return _instance;
        }
    }

    public static void StartGlobal(IEnumerator routine)
    {
        Instance.StartCoroutine(routine);
    }
}