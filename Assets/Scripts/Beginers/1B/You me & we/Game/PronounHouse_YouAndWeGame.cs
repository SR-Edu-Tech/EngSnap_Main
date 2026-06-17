using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ════════════════════════════════════════════════════════════════════
///  PronounHouse_YouAndWeGame
///  One of the 5 tree-house drop zones (I, He, She, It, We).
/// ════════════════════════════════════════════════════════════════════
///
///  PREFAB / GO STRUCTURE (place under HousesRow):
///  House_I           [this script]  [RectTransform]
///    ├─ HouseImage     Image   (treehouse sprite — normal state)
///    ├─ GlowImage      Image   (bright outline/glow — disabled normally)
///    ├─ PronounLabel   TMP_Text   ("I")
///    └─ DropZone       RectTransform  (empty RT, sized to the door area)
///
///  Inspector:
///    pronoun     → "I" / "He" / "She" / "It" / "We"
///    houseImage  → HouseImage
///    glowImage   → GlowImage  (set alpha 0 by default in prefab)
///    pronounLabel→ PronounLabel
///    dropZone    → DropZone RT
/// </summary>
public class PronounHouse_YouAndWeGame : MonoBehaviour
{
    [Header("Identity")]
    public string pronoun;   // "I" / "He" / "She" / "It" / "We"

    [Header("Visuals")]
    public Image        houseImage;
    public Image        glowImage;     // hint glow overlay
    public TMP_Text     pronounLabel;
    public RectTransform dropZone;     // drag-drop detection area

    [Header("Colors")]
    public Color normalColor  = Color.white;
    public Color correctColor = new Color(0.5f, 1f, 0.5f);   // green flash

    // ── State ─────────────────────────────────────────────────────────
    public bool IsOccupied { get; set; } = false;

    // ── Public API ────────────────────────────────────────────────────
    public void Reset()
    {
        IsOccupied = false;
        if (houseImage  != null) houseImage.color = normalColor;
        if (glowImage   != null) { var c = glowImage.color; c.a = 0f; glowImage.color = c; }
        StopAllCoroutines();
    }

    /// <summary>Flash green + bounce when a correct card is dropped in.</summary>
    public void PlayCorrectAnim()
    {
        StopAllCoroutines();
        StartCoroutine(CorrectFlash());
    }

    /// <summary>Glow hint on wrong house — dims automatically after a second.</summary>
    public void PlayHintGlow()
    {
        StopAllCoroutines();
        StartCoroutine(HintGlow());
    }

    // ── Coroutines ────────────────────────────────────────────────────
    IEnumerator CorrectFlash()
    {
        // Bounce scale
        float dur = 0.35f, t = 0f;
        Vector3 orig = transform.localScale;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            float s = 1f + Mathf.Sin(p * Mathf.PI) * 0.18f;
            transform.localScale = orig * s;
            yield return null;
        }
        transform.localScale = orig;

        // Color flash — white → correctColor → white
        if (houseImage != null)
        {
            float ft = 0f, fdur = 0.4f;
            while (ft < fdur)
            {
                ft += Time.deltaTime;
                float p = ft / fdur;
                houseImage.color = Color.Lerp(correctColor, normalColor, p);
                yield return null;
            }
            houseImage.color = normalColor;
        }
    }

    IEnumerator HintGlow()
    {
        if (glowImage == null) yield break;
        // Fade in
        float t = 0f, dur = 0.2f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetGlowAlpha(Mathf.Lerp(0f, 0.85f, t / dur));
            yield return null;
        }
        yield return new WaitForSeconds(0.7f);
        // Fade out
        t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SetGlowAlpha(Mathf.Lerp(0.85f, 0f, t / dur));
            yield return null;
        }
        SetGlowAlpha(0f);
    }

    void SetGlowAlpha(float a)
    {
        if (glowImage == null) return;
        var c = glowImage.color; c.a = a; glowImage.color = c;
    }
}
