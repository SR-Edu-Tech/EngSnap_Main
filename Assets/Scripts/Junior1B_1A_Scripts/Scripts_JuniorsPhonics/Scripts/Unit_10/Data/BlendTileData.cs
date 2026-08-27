using UnityEngine;

/// <summary>
/// ScriptableObject representing a single Consonant Blend Tile (e.g. bl, st, nd, mp).
/// Used in Unit 10 Intro, Builder, and Game screens.
/// </summary>
[CreateAssetMenu(fileName = "NewBlendTile", menuName = "Phonics/Unit 10/Blend Tile Data")]
public class BlendTileData : ScriptableObject
{
    [Header("Blend Info")]
    public string blendKey;           // e.g. "bl", "st", "nd", "mp"
    public string displayText;        // e.g. "bl"
    public string exampleWord;        // e.g. "blue", "pond"
    public bool   isBeginningBlend;   // true = beginning blend, false = ending blend

    [Header("Audio & Visuals")]
    public AudioClip blendSoundClip;  // e.g. u10_bb_bl: "/bl/... blue!"
    public AudioClip wordAudioClip;   // e.g. blue.mp3
    public Sprite    exampleSprite;   // Sprite for example word
    public Sprite    blendIcon;       // Icon sprite
}
