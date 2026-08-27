using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject representing a single Word Asset for Unit 10.
/// Holds the word text, target blend, beginning/ending flag,
/// chunk tiles ([b][l] + [ue]), incomplete text, picture sprite, and word audio.
/// </summary>
[CreateAssetMenu(fileName = "NewBlendWord", menuName = "Phonics/Unit 10/Blend Word Data")]
public class BlendWordData_Phonics_Junior : ScriptableObject
{
    [Header("Word Info")]
    public string wordText;           // e.g. "blue", "pond", "snail", "stamp"
    public string targetBlend;        // e.g. "bl", "nd", "sn", "mp"
    public bool   isBeginningBlend;   // true = beginning blend, false = ending blend
    public string incompleteWordText; // e.g. "___ail" or "sta___"

    [Header("Word Chunks (Builder Mode)")]
    // e.g. for "blue" -> ["b", "l", "ue"] (or blend chunk ["bl", "ue"])
    public List<string> blendChunks = new List<string>();

    [Header("Audio & Visuals")]
    public AudioClip wordAudio;
    public Sprite    pictureSprite;
}
