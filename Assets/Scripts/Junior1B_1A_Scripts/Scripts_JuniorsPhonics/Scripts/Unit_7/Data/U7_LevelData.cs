using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "U7_LevelData", menuName = "Phonics/Unit 7/Level Data")]
public class U7_LevelData : ScriptableObject
{
    public string levelTitle;
    public List<U7_LongVowelTeamData> teams = new List<U7_LongVowelTeamData>();
    public AudioClip sillySentenceAudio;
    public string sillySentenceText;
}
