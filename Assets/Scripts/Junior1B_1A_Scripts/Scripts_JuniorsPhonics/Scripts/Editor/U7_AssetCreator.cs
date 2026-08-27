using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class U7_AssetCreator
{
    [MenuItem("Phonics/Unit 7/Generate All Assets")]
    public static void GenerateAllUnit7Assets()
    {
        string wordsPath = "Assets/Data/Unit7/Words";
        string teamsPath = "Assets/Data/Unit7/Teams";
        string levelsPath = "Assets/Data/Unit7/Levels";

        if (!Directory.Exists(wordsPath)) Directory.CreateDirectory(wordsPath);
        if (!Directory.Exists(teamsPath)) Directory.CreateDirectory(teamsPath);
        if (!Directory.Exists(levelsPath)) Directory.CreateDirectory(levelsPath);

        // ==========================================
        // 1. LEVEL 1: LONG I (i_e, ie, igh)
        // ==========================================
        string[] i_eWords = { "pipe", "lime", "fire", "mice", "kite", "knife", "tire", "hive" };
        string[] ieWords = { "fried", "pie", "tie", "fries", "tied", "pot pie", "flies" };
        string[] ighWords = { "sight", "night", "lightening", "bright", "high", "light", "thigh", "knight" };

        U7_LongVowelTeamData team_i_e = CreateTeamAsset(teamsPath, wordsPath, "i_e", i_eWords);
        U7_LongVowelTeamData team_ie = CreateTeamAsset(teamsPath, wordsPath, "ie", ieWords);
        U7_LongVowelTeamData team_igh = CreateTeamAsset(teamsPath, wordsPath, "igh", ighWords);

        U7_LevelData levelLongI = ScriptableObject.CreateInstance<U7_LevelData>();
        levelLongI.levelTitle = "Level 1: Long I";
        levelLongI.sillySentenceText = "The knight in the night flies a kite high!";
        levelLongI.teams = new List<U7_LongVowelTeamData> { team_i_e, team_ie, team_igh };
        AssetDatabase.CreateAsset(levelLongI, $"{levelsPath}/U7_Level_LongI.asset");

        // ==========================================
        // 2. LEVEL 2: LONG O (o_e, oa, ow)
        // ==========================================
        string[] o_eWords = { "bone", "rose", "rope", "hole", "cone", "globe", "vote", "robe" };
        string[] oaWords = { "boat", "toast", "soap", "coal", "road", "moat", "coach", "coat" };
        string[] owWords = { "bow", "blow", "snow", "bowl", "mow", "glow", "grow", "crow" };

        U7_LongVowelTeamData team_o_e = CreateTeamAsset(teamsPath, wordsPath, "o_e", o_eWords);
        U7_LongVowelTeamData team_oa = CreateTeamAsset(teamsPath, wordsPath, "oa", oaWords);
        U7_LongVowelTeamData team_ow = CreateTeamAsset(teamsPath, wordsPath, "ow", owWords);

        U7_LevelData levelLongO = ScriptableObject.CreateInstance<U7_LevelData>();
        levelLongO.levelTitle = "Level 2: Long O";
        levelLongO.sillySentenceText = "The crow in a coat rowed a boat in the snow!";
        levelLongO.teams = new List<U7_LongVowelTeamData> { team_o_e, team_oa, team_ow };
        AssetDatabase.CreateAsset(levelLongO, $"{levelsPath}/U7_Level_LongO.asset");

        // ==========================================
        // 3. LEVEL 3: LONG U (u_e, ue, ui)
        // ==========================================
        string[] u_eWords = { "cube", "cute", "huge", "dune", "tube", "mule" };
        string[] ueWords = { "blue", "tissue", "fuel", "clue", "statue", "glue" };
        string[] uiWords = { "juice", "fruit", "sluice", "bruise", "cruise", "suit" };

        U7_LongVowelTeamData team_u_e = CreateTeamAsset(teamsPath, wordsPath, "u_e", u_eWords);
        U7_LongVowelTeamData team_ue = CreateTeamAsset(teamsPath, wordsPath, "ue", ueWords);
        U7_LongVowelTeamData team_ui = CreateTeamAsset(teamsPath, wordsPath, "ui", uiWords);

        U7_LevelData levelLongU = ScriptableObject.CreateInstance<U7_LevelData>();
        levelLongU.levelTitle = "Level 3: Long U";
        levelLongU.sillySentenceText = "The cute mule drank blue fruit juice!";
        levelLongU.teams = new List<U7_LongVowelTeamData> { team_u_e, team_ue, team_ui };
        AssetDatabase.CreateAsset(levelLongU, $"{levelsPath}/U7_Level_LongU.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Unit 7 Asset Creator] Successfully generated all Unit 7 word, team, and level data assets!");
    }

    private static U7_LongVowelTeamData CreateTeamAsset(string teamsPath, string wordsPath, string spelling, string[] words)
    {
        U7_LongVowelTeamData teamData = ScriptableObject.CreateInstance<U7_LongVowelTeamData>();
        teamData.teamSpelling = spelling;
        teamData.teamWords = new List<CVCWordData>();

        foreach (string w in words)
        {
            string cleanName = w.Replace(" ", "_");
            string assetPath = $"{wordsPath}/Word_{cleanName}.asset";

            CVCWordData wordData = AssetDatabase.LoadAssetAtPath<CVCWordData>(assetPath);
            if (wordData == null)
            {
                wordData = ScriptableObject.CreateInstance<CVCWordData>();
                wordData.word = w;

                // Try linking audio clip
                AudioClip wordAudio = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio Clips/Unit 7/u7_w_{cleanName}.mp3");
                if (wordAudio == null) wordAudio = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio Clips/Unit 6/Word_{cleanName}.mp3");
                if (wordAudio != null) wordData.fullWordAudio = wordAudio;

                // Try linking picture sprite across Art folders
                string[] searchFolders = { "Assets/Art/Unit 7", "Assets/Art/unit 6", "Assets/Art/Unit 6", "Assets/Art/General", "Assets/Art" };
                Sprite picSprite = null;
                foreach (string folder in searchFolders)
                {
                    picSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{cleanName}.png");
                    if (picSprite == null) picSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{cleanName}.jpg");
                    if (picSprite == null) picSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{cleanName}.JPEG");
                    if (picSprite != null) break;
                }
                if (picSprite != null) wordData.wordPicture = picSprite;

                AssetDatabase.CreateAsset(wordData, assetPath);
            }

            teamData.teamWords.Add(wordData);
        }

        AssetDatabase.CreateAsset(teamData, $"{teamsPath}/Team_{spelling}.asset");
        return teamData;
    }
}
