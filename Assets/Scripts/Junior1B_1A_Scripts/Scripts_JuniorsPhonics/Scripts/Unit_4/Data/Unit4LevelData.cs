using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Word Family Level Data", menuName = "Phonics/Word Family Level Data")]
public class Unit4LevelData : ScriptableObject
{
    public string levelTitle; // e.g. "Short a Families" or "Short e Families"
    public Sprite vowelBadge;
    public List<WordFamilyData> families = new List<WordFamilyData>();
    [TextArea(2, 4)]
    public string sillySentenceText;
    public AudioClip sillySentenceAudio;
}
