using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Team_New", menuName = "Phonics/Unit 6/Team Data")]
public class U6_LongVowelTeamData : ScriptableObject
{
    public string teamSpelling; // e.g. "a_e", "ai", "ay", "ee", "ea", "ey"
    public AudioClip spellingAudio; // Pronunciation of team spelling
    public List<CVCWordData> teamWords = new List<CVCWordData>();
}
