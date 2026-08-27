using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data asset for a digraph word (e.g. "chick", "shell", "thumb", "bank").
/// Stores exact textbook arrow blending chunks (e.g. ["ch", "i", "ck"] or ["sh", "i", "p"]),
/// full word text, word audio clip, picture sprite, and target digraph substring.
/// </summary>
[CreateAssetMenu(fileName = "DigraphWordData_", menuName = "Phonics/Unit 9/Digraph Word Data")]
public class DigraphWordData : ScriptableObject
{
    public string       wordText;            // e.g. "chick", "shell", "thumb"
    public string       targetDigraph;       // e.g. "ch", "sh", "th", "wh", "ck", "nk", "ng", "ph", "kn"
    public List<string> arrowChunks;         // e.g. ["ch", "i", "ck"], ["th", "u", "mb"], ["dr", "i", "nk"]
    public AudioClip    wordAudio;           // Full spoken word clip
    public Sprite       pictureSprite;       // Picture for matching / game 41

    [Header("Pick-the-Digraph (Page 41)")]
    public string       incompleteWordText;  // e.g. "___eese", "___one", "___ee", "___eel"
}
