using System.Collections;
using UnityEngine;

/// <summary>
/// SparkleEffect_MagicWords_Reading
/// Lightweight sparkle-burst controller that can be triggered
/// programmatically from any bubble or card interaction.
///
/// Attach to a GameObject that has a ParticleSystem component.
/// The particle system is configured via the ParticleSystem component
/// in the Inspector — this script just adds play/stop helpers and
/// auto-destroy after a burst.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SparkleEffect_MagicWords_Reading : MonoBehaviour
{
    [Header("Burst Settings")]
    [Tooltip("If true, the GameObject destroys itself after the burst completes")]
    public bool destroyAfterBurst = true;

    [Tooltip("How long to wait after emission stops before destroying (seconds)")]
    [Range(0.5f, 5f)]
    public float lifetimeAfterEmission = 1.5f;

    [Header("Colour Cycle")]
    [Tooltip("If true, randomises the start colour from this list on each Play()")]
    public bool randomColour = true;
    public Color[] colourPalette = new Color[]
    {
        new Color(1f,   0.87f, 0.2f),  // golden yellow
        new Color(0.98f,0.36f, 0.68f), // hot pink
        new Color(0.35f,0.85f, 0.96f), // sky blue
        new Color(0.55f,0.95f, 0.45f), // lime green
        new Color(1f,   0.55f, 0.15f), // orange
    };

    // ─────────────────────────────────────────────────────────────────────────

    private ParticleSystem _ps;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    /// <summary>Trigger a sparkle burst at this object's current position.</summary>
    public void Burst()
    {
        if (randomColour && colourPalette != null && colourPalette.Length > 0)
        {
            var main      = _ps.main;
            main.startColor = colourPalette[Random.Range(0, colourPalette.Length)];
        }

        _ps.Play();

        if (destroyAfterBurst)
            StartCoroutine(DestroyAfterDelay());
    }

    /// <summary>Burst at a specific world position.</summary>
    public void BurstAt(Vector3 worldPos)
    {
        transform.position = worldPos;
        Burst();
    }

    private IEnumerator DestroyAfterDelay()
    {
        // Wait until emission stops, then wait for particles to die out
        yield return new WaitUntil(() => !_ps.isEmitting);
        yield return new WaitForSeconds(lifetimeAfterEmission);
        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Convenience factory
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Instantiate a sparkle prefab, position it, and burst it.
    /// Prefab must have SparkleEffect_MagicWords_Reading attached.
    /// </summary>
    public static void SpawnBurst(GameObject sparklePrefab, Vector3 worldPos)
    {
        if (sparklePrefab == null) return;
        var go = Instantiate(sparklePrefab, worldPos, Quaternion.identity);
        var fx = go.GetComponent<SparkleEffect_MagicWords_Reading>();
        fx?.Burst();
    }

    /// <summary>
    /// Spawn inside a Canvas (UI space). Converts anchored position to world pos.
    /// </summary>
    public static void SpawnBurstUI(
        GameObject sparklePrefab,
        RectTransform parent,
        Vector2      anchoredPos)
    {
        if (sparklePrefab == null || parent == null) return;

        var go = Instantiate(sparklePrefab, parent);
        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = anchoredPos;

        var fx = go.GetComponent<SparkleEffect_MagicWords_Reading>();
        fx?.Burst();
    }
}
