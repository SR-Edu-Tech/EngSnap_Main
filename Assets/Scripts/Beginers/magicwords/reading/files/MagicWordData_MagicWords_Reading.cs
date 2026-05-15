using UnityEngine;

/// <summary>
/// MagicWordData_MagicWords_Reading
/// ScriptableObject that holds all data for a single Magic Word entry.
/// Create via: Assets → Create → MagicWords → MagicWordData
/// </summary>
[CreateAssetMenu(
    fileName  = "MagicWordData",
    menuName  = "MagicWords/MagicWordData",
    order     = 0)]
public class MagicWordData_MagicWords_Reading : ScriptableObject
{
    // ── Identity ──────────────────────────────────────────────────────────────
    [Header("Identity")]
    [Tooltip("The magic word shown in large text (e.g. PLEASE)")]
    public string magicWord = "PLEASE";

    [Tooltip("Colour used for this word's bubble and card accent.")]
    public Color accentColor = Color.yellow;

    // ── Panel 1 – Word Bubble ─────────────────────────────────────────────────
    [Header("Panel 1 – Word Bubble")]
    [Tooltip("Short definition voiceover for the bubble pop. "
           + "e.g. 'PLEASE — we say this when we need something.'")]
    public AudioClip bubbleIntroAudio;

    [Tooltip("Icon/sprite shown inside the bubble (optional decorative element)")]
    public Sprite bubbleIcon;

    // ── Panel 2 – Situation Card ──────────────────────────────────────────────
    [Header("Panel 2 – Situation Card")]
    [Tooltip("Illustration sprite shown on the top half of the situation card")]
    public Sprite situationIllustration;

    [Tooltip("Full situation voiceover. "
           + "e.g. 'When you need something — say PLEASE!'")]
    public AudioClip situationAudio;

    [Tooltip("Short voiceover that plays when student taps only the magic word text")]
    public AudioClip wordOnlyAudio;

    [Tooltip("Card auto-read delay in seconds after card becomes visible")]
    [Range(0.2f, 3f)]
    public float cardAutoPlayDelay = 0.6f;
}
