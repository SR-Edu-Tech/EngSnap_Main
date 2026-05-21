using System.Collections;
using UnityEngine;

/// <summary>
/// VFXManager — Spawns particle effects at world/screen positions.
///
/// Setup:
///   1. Create a Canvas-space "VFX Layer" RectTransform at the top of your Canvas hierarchy.
///   2. Assign correctBurstPrefab  → a ParticleSystem prefab (sparkles/stars, ~0.6s lifetime)
///   3. Assign confettiPrefab      → a full-screen confetti ParticleSystem
///   4. Assign starPopPrefab       → a single glowing star that scales up then fades
///   5. Assign wrongPuffPrefab     → a small grey smoke puff
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Prefabs (ParticleSystem or Animator-based)")]
    public GameObject correctBurstPrefab;   // sparkle burst on correct tap
    public GameObject confettiPrefab;       // full-screen celebration
    public GameObject wrongPuffPrefab;      // wrong answer puff

    [Header("Spawn Parent")]
    public Transform vfxParent;             // Canvas → VFX Layer RectTransform

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// Spawn a sparkle burst at a UI element's position
    public void SpawnCorrectBurst(RectTransform target)
    {
        if (correctBurstPrefab == null) return;
        Vector3 worldPos = target.position;
        var go = Instantiate(correctBurstPrefab, vfxParent);
        go.transform.position = worldPos;
        AutoDestroy(go, 1.5f);
    }

    /// Full-screen confetti — call on round complete
    public void SpawnConfetti()
    {
        if (confettiPrefab == null) return;
        var go = Instantiate(confettiPrefab, vfxParent);
        AutoDestroy(go, 3f);
    }

    /// Small puff on wrong answer
    public void SpawnWrongPuff(RectTransform target)
    {
        if (wrongPuffPrefab == null) return;
        var go = Instantiate(wrongPuffPrefab, vfxParent);
        go.transform.position = target.position;
        AutoDestroy(go, 1f);
    }

    // ── Screen-shake (camera or Canvas) ─────────────────────────────────────

    /// Shake the vfxParent (or assign canvasTransform) for impact feel
    public void ScreenShake(float intensity = 12f, float duration = 0.25f)
    {
        StartCoroutine(ShakeCoroutine(vfxParent, intensity, duration));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void AutoDestroy(GameObject go, float delay) =>
        Destroy(go, delay);

    private IEnumerator DelayedPop(GameObject go, float delay)
    {
        go.SetActive(false);
        yield return new WaitForSeconds(delay);
        go.SetActive(true);
        // If the prefab has an Animator, it auto-plays. Otherwise scale-pop it:
        var anim = go.GetComponent<Animator>();
        if (anim == null)
        {
            yield return StartCoroutine(ScalePop(go.transform, delay));
        }
        Destroy(go, 1.5f);
    }

    private IEnumerator ScalePop(Transform t, float delay)
    {
        t.localScale = Vector3.zero;
        float dur = 0.2f, elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.LerpUnclamped(0f, 1.3f, elapsed / dur);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        elapsed = 0f; dur = 0.1f;
        while (elapsed < dur)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.LerpUnclamped(1.3f, 1f, elapsed / dur);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    private IEnumerator ShakeCoroutine(Transform t, float intensity, float duration)
    {
        Vector3 origin = t.localPosition;
        float elapsed  = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            float dampen   = 1f - progress;
            t.localPosition = origin + (Vector3)Random.insideUnitCircle * intensity * dampen;
            yield return null;
        }
        t.localPosition = origin;
    }
}
