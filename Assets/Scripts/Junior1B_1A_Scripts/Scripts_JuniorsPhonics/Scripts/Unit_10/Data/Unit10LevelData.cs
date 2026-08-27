using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Master ScriptableObject holding all data lists for Unit 10 — Consonant Blends.
/// Holds beginning blends (pg 42), beginning builder/game words (pg 43-45),
/// ending blends (pg 46), and ending builder/game words (pg 47-48).
/// </summary>
[CreateAssetMenu(fileName = "Unit10LevelData", menuName = "Phonics/Unit 10/Master Level Data")]
public class Unit10LevelData : ScriptableObject
{
    [Header("Beginning Blends (Pages 42–45)")]
    public List<BlendTileData> beginningBlends = new List<BlendTileData>();
    public List<BlendWordData_Phonics_Junior> beginningBuilderWords = new List<BlendWordData_Phonics_Junior>();
    public List<BlendWordData_Phonics_Junior> startItRightGameWords = new List<BlendWordData_Phonics_Junior>();

    [Header("Ending Blends (Pages 46–48)")]
    public List<BlendTileData> endingBlends = new List<BlendTileData>();
    public List<BlendWordData_Phonics_Junior> endingBuilderWords = new List<BlendWordData_Phonics_Junior>();
    public List<BlendWordData_Phonics_Junior> finishItRightGameWords = new List<BlendWordData_Phonics_Junior>();
}
