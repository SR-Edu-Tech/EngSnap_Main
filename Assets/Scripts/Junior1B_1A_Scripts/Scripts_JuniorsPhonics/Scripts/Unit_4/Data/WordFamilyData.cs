using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Word Family Data", menuName = "Phonics/Unit 4/Word Family Data")]
public class WordFamilyData : ScriptableObject
{
    public string chunkName; // e.g. "-at", "-an", "-en"
    public AudioClip chunkAudio;
    public List<CVCWordData> familyWords = new List<CVCWordData>();
}
