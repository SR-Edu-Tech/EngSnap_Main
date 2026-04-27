using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Attach to any GameObject with a TMP_Text component.
/// Reveals letters one by one with a rope/wave ripple effect.
/// Automatically re-triggers whenever the text content changes.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TMPTextRevealPop : MonoBehaviour
{
    [Header("Reveal Settings")]
    public float letterDelay      = 0.05f;
    public bool  autoPlayOnEnable = true;

    [Header("Wave / Rope Settings")]
    public float waveAmplitude   = 18f;
    public float waveFrequency   = 12f;
    public float waveDuration    = 0.45f;
    public float wavePhaseOffset = 0.35f;

    [Header("Scale Punch")]
    public bool  useScalePunch = true;
    public float punchScale    = 1.35f;
    public float punchDuration = 0.2f;

    // ── Private ────────────────────────────────────────────────────────────────
    private TMP_Text  _tmp;
    private Coroutine _revealCoroutine;

    private bool[]  _letterRevealed;
    private float[] _letterRevealTime;
    private int     _allocatedSize;

    private string _lastText = null; // change-detection

    // ──────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _tmp = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (autoPlayOnEnable)
            Reveal();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _revealCoroutine = null;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Kick off (or restart) the reveal animation.</summary>
    public void Reveal()
    {
        if (!gameObject.activeInHierarchy) return;

        if (_revealCoroutine != null)
            StopCoroutine(_revealCoroutine);

        _revealCoroutine = StartCoroutine(RevealRoutine());
    }

    /// <summary>Show all text instantly with no animation.</summary>
    public void ShowInstant()
    {
        if (_revealCoroutine != null)
        {
            StopCoroutine(_revealCoroutine);
            _revealCoroutine = null;
        }

        _tmp.ForceMeshUpdate();
        int count = _tmp.textInfo.characterCount;
        AllocateArrays(count);

        for (int i = 0; i < count; i++)
        {
            _letterRevealed[i]   = true;
            _letterRevealTime[i] = -9999f;
        }

        SetCharacterAlphas(255);
        _lastText = _tmp.text;
    }

    // ── Update: change detection + mesh animation ──────────────────────────────

    private void Update()
    {
        // Auto-detect text change and re-trigger reveal
        if (_tmp.text != _lastText)
        {
            _lastText = _tmp.text;
            Reveal();
            return;
        }

        AnimateMesh();
    }

    // ── Core Coroutine ─────────────────────────────────────────────────────────

    private IEnumerator RevealRoutine()
    {
        // Wait one frame so TMP finishes processing any text set this frame
        yield return null;

        _tmp.ForceMeshUpdate();

        int total = _tmp.textInfo.characterCount;
        AllocateArrays(total);

        SetCharacterAlphas(0);

        for (int i = 0; i < total; i++)
        {
            // Re-check bounds after every yield (text could change mid-reveal)
            if (i >= _allocatedSize) break;

            bool visible = i < _tmp.textInfo.characterCount &&
                           _tmp.textInfo.characterInfo[i].isVisible;

            _letterRevealed[i]   = true;
            _letterRevealTime[i] = Time.time;

            if (!visible)
            {
                _letterRevealTime[i] = -9999f; // no wave for spaces
                yield return new WaitForSeconds(letterDelay * 0.4f);
                continue;
            }

            yield return new WaitForSeconds(letterDelay);
        }

        _revealCoroutine = null;
    }

    // ── Per-Frame Mesh Animation ───────────────────────────────────────────────

    private void AnimateMesh()
    {
        if (_letterRevealed == null) return;

        _tmp.ForceMeshUpdate();

        TMP_TextInfo textInfo = _tmp.textInfo;
        int charCount = textInfo.characterCount;
        int safeCount = Mathf.Min(charCount, _allocatedSize);

        bool meshDirty = false;

        for (int i = 0; i < charCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIndex  = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            // ── Bounds guards ──
            if (matIndex  >= textInfo.meshInfo.Length)               continue;
            var meshInfo = textInfo.meshInfo[matIndex];
            if (vertIndex + 3 >= meshInfo.vertices.Length)           continue;
            if (vertIndex + 3 >= meshInfo.colors32.Length)           continue;

            Vector3[] verts  = meshInfo.vertices;
            Color32[] colors = meshInfo.colors32;

            bool  revealed   = i < safeCount && _letterRevealed[i];
            float revealTime = i < safeCount  ? _letterRevealTime[i] : -9999f;

            if (!revealed)
            {
                for (int v = 0; v < 4; v++) colors[vertIndex + v].a = 0;
                meshDirty = true;
                continue;
            }

            float elapsed = Time.time - revealTime;

            // Alpha fade-in
            float alpha = Mathf.Clamp01(elapsed / Mathf.Max(waveDuration * 0.3f, 0.01f));
            byte  alphaB = (byte)(alpha * 255f);
            for (int v = 0; v < 4; v++) colors[vertIndex + v].a = alphaB;

            // Rope wave: decaying sine with per-letter phase offset
            float decay = Mathf.Clamp01(1f - elapsed / Mathf.Max(waveDuration, 0.01f));
            float phase = i * wavePhaseOffset;
            float wave  = Mathf.Sin(Time.time * waveFrequency - phase) * waveAmplitude * decay;

            // Scale punch (spring overshoot)
            float scale = 1f;
            if (useScalePunch)
            {
                float pt = Mathf.Clamp01(elapsed / Mathf.Max(punchDuration, 0.01f));
                scale = Mathf.Lerp(punchScale, 1f, EaseOutBack(pt));
            }

            // Apply wave + scale to each vertex around the character center
            Vector3 center = (verts[vertIndex]     + verts[vertIndex + 1] +
                              verts[vertIndex + 2] + verts[vertIndex + 3]) * 0.25f;

            for (int v = 0; v < 4; v++)
            {
                Vector3 local = (verts[vertIndex + v] - center) * scale;
                verts[vertIndex + v] = center + local + new Vector3(0f, wave, 0f);
            }

            meshDirty = true;
        }

        if (!meshDirty) return;

        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            Mesh mesh = textInfo.meshInfo[m].mesh;
            if (mesh == null) continue;
            mesh.vertices = textInfo.meshInfo[m].vertices;
            mesh.colors32 = textInfo.meshInfo[m].colors32;
            _tmp.UpdateGeometry(mesh, m);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void AllocateArrays(int count)
    {
        if (_letterRevealed == null || count > _allocatedSize)
        {
            _letterRevealed   = new bool[count];
            _letterRevealTime = new float[count];
            _allocatedSize    = count;
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                _letterRevealed[i]   = false;
                _letterRevealTime[i] = 0f;
            }
        }
    }

    private void SetCharacterAlphas(byte alpha)
    {
        _tmp.ForceMeshUpdate();
        TMP_TextInfo textInfo = _tmp.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int matIndex  = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            if (matIndex  >= textInfo.meshInfo.Length)              continue;
            var meshInfo = textInfo.meshInfo[matIndex];
            if (vertIndex + 3 >= meshInfo.colors32.Length)          continue;

            for (int v = 0; v < 4; v++)
                meshInfo.colors32[vertIndex + v].a = alpha;
        }

        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            var mesh = textInfo.meshInfo[m].mesh;
            if (mesh == null) continue;
            mesh.colors32 = textInfo.meshInfo[m].colors32;
            _tmp.UpdateGeometry(mesh, m);
        }
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}