using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "U7_LongVowelTeamData", menuName = "Phonics/Unit 7/Long Vowel Team Data")]
public class U7_LongVowelTeamData : ScriptableObject
{
    public string teamSpelling; // e.g., "i_e", "ie", "igh", "o_e", "oa", "ow", "u_e", "ue", "ui"
    public List<CVCWordData> teamWords = new List<CVCWordData>();
}
