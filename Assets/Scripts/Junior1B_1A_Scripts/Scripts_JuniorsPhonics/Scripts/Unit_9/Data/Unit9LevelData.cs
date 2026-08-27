using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Master Level Data container for Unit 9 — Consonant Digraphs (Pages 32–41).
/// Stores intro digraphs, stage word lists (Stage 1: ch/sh, Stage 2: th/wh, Stage 3: ck/nk/ng),
/// and Pick-the-Digraph (Page 41) items.
/// </summary>
[CreateAssetMenu(fileName = "Unit9Level_Main", menuName = "Phonics/Unit 9/Master Level Data")]
public class Unit9LevelData : ScriptableObject
{
    [Header("Page 32 — Digraph Chart")]
    public List<DigraphTileData> introDigraphs;

    [Header("Stage 1 — ch & sh (Pages 33 & 34)")]
    public List<DigraphWordData> stage1Words;

    [Header("Stage 2 — th & wh (Pages 35, 36 & 37)")]
    public List<DigraphWordData> stage2Words;

    [Header("Stage 3 — ck, nk & ng (Pages 38, 39 & 40)")]
    public List<DigraphWordData> stage3Words;

    [Header("Page 41 — Pick the Digraph Game")]
    public List<DigraphWordData> pickDigraphWords;
}
