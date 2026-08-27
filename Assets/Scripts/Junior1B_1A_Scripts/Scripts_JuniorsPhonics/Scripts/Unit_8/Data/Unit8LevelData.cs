using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Unit8Level_", menuName = "Phonics/Unit8/Unit8LevelData")]
public class Unit8LevelData : ScriptableObject
{
    public string levelTitle = "Consonant Explorer";
    public int sectionIndex;

    [Header("Section A: Consonant Sound Wall")]
    public List<ConsonantTileData> consonantsList = new List<ConsonantTileData>();

    [Header("Section B: Buzz or Whisper")]
    public List<BuzzWhisperData> buzzWhisperList = new List<BuzzWhisperData>();

    [Header("Section C: Connect the Sound")]
    public List<ConsonantTileData> connectPairs = new List<ConsonantTileData>();

    [Header("Section D: Consonant Safari")]
    public List<string> safariConsonants = new List<string>();
    public List<string> safariVowels = new List<string> { "a", "e", "i", "o", "u" };
}
