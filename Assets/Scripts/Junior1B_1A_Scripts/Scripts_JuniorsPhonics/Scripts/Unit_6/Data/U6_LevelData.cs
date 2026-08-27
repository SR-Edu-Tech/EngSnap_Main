using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_Unit6_New", menuName = "Phonics/Unit 6/Level Data")]
public class U6_LevelData : ScriptableObject
{
    public string levelTitle; // e.g. "Long A Teams", "Long E Teams"
    public List<U6_LongVowelTeamData> teams = new List<U6_LongVowelTeamData>();
    public AudioClip sillySentenceAudio;
    public string sillySentenceText;
}
