using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data asset for a single Consonant Digraph (e.g. 'ch', 'sh', 'th', 'wh', 'ck', 'nk', 'ng', 'ph', 'kn').
/// Holds the digraph sound clip, introductory keyword (e.g. "chain" for ch), and example words.
/// </summary>
[CreateAssetMenu(fileName = "DigraphTileData_", menuName = "Phonics/Unit 9/Digraph Tile Data")]
public class DigraphTileData : ScriptableObject
{
    public string    digraphKey;          // e.g. "ch-", "-ch", "sh-", "-sh", "th-", "-th", "wh-", "-ck"
    public string    displayText;         // e.g. "ch", "sh"
    public AudioClip digraphSoundClip;    // AI voiceover for the single digraph sound /ch/, /sh/
    public AudioClip wordAudioClip;       // Exact audio clip for word (e.g. chain, switch, shark, trash, three, earth, wheel, duck)
    public Sprite    digraphIcon;         // Optional icon

    [Header("Page 32 Intro Examples")]
    public string    startWord;           // e.g. "chain" (for ch-)
    public Sprite    startWordSprite;
    public string    endWord;             // e.g. "switch" (for -ch)
    public Sprite    endWordSprite;
}
