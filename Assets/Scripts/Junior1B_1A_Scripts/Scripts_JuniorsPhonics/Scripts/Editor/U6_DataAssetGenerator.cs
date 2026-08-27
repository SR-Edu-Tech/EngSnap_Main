#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class U6_DataAssetGenerator
{
    [MenuItem("Phonics/Generate Unit 6 Data Assets (Pages 24 & 25)")]
    public static void GenerateUnit6Assets()
    {
        string wordsDir = "Assets/Data/Unit6/Words";
        string teamsDir = "Assets/Data/Unit6/Teams";
        string levelsDir = "Assets/Data/Unit6/Levels";

        EnsureDirectoryExists(wordsDir);
        EnsureDirectoryExists(teamsDir);
        EnsureDirectoryExists(levelsDir);

        // --- PAGE 25: LONG VOWEL E TEAMS ---
        // 1. ee Team Words
        List<CVCWordData> eeWords = new List<CVCWordData>()
        {
            GetOrCreateWordData("wheel", wordsDir),
            GetOrCreateWordData("cheese", wordsDir),
            GetOrCreateWordData("bee", wordsDir),
            GetOrCreateWordData("tree", wordsDir),
            GetOrCreateWordData("queen", wordsDir),
            GetOrCreateWordData("feet", wordsDir),
            GetOrCreateWordData("seeds", wordsDir),
            GetOrCreateWordData("beet", wordsDir)
        };

        // 2. ea Team Words
        List<CVCWordData> eaWords = new List<CVCWordData>()
        {
            GetOrCreateWordData("leaf", wordsDir),
            GetOrCreateWordData("tea", wordsDir),
            GetOrCreateWordData("bead", wordsDir),
            GetOrCreateWordData("seal", wordsDir),
            GetOrCreateWordData("peach", wordsDir),
            GetOrCreateWordData("read", wordsDir),
            GetOrCreateWordData("eat", wordsDir),
            GetOrCreateWordData("beans", wordsDir)
        };

        // 3. ey Team Words
        List<CVCWordData> eyWords = new List<CVCWordData>()
        {
            GetOrCreateWordData("key", wordsDir),
            GetOrCreateWordData("turkey", wordsDir),
            GetOrCreateWordData("valley", wordsDir),
            GetOrCreateWordData("honey", wordsDir),
            GetOrCreateWordData("monkey", wordsDir),
            GetOrCreateWordData("money", wordsDir),
            GetOrCreateWordData("jersey", wordsDir),
            GetOrCreateWordData("chimney", wordsDir)
        };

        U6_LongVowelTeamData teamEE = GetOrCreateTeamData("Team_EE", "ee", eeWords, teamsDir);
        U6_LongVowelTeamData teamEA = GetOrCreateTeamData("Team_EA", "ea", eaWords, teamsDir);
        U6_LongVowelTeamData teamEY = GetOrCreateTeamData("Team_EY", "ey", eyWords, teamsDir);

        List<U6_LongVowelTeamData> eTeams = new List<U6_LongVowelTeamData>() { teamEE, teamEA, teamEY };
        U6_LevelData levelLongE = GetOrCreateLevelData("Level_Long_E_teams", "Long E Teams", eTeams, levelsDir);
        levelLongE.sillySentenceText = "The bee on the tree eats sweet honey with a key!";

        // --- PAGE 24: LONG VOWEL A TEAMS ---
        // 1. a_e Team Words
        List<CVCWordData> a_eWords = new List<CVCWordData>()
        {
            GetOrCreateWordData("cake", wordsDir),
            GetOrCreateWordData("tape", wordsDir),
            GetOrCreateWordData("cave", wordsDir),
            GetOrCreateWordData("cage", wordsDir),
            GetOrCreateWordData("face", wordsDir),
            GetOrCreateWordData("game", wordsDir),
            GetOrCreateWordData("flame", wordsDir),
            GetOrCreateWordData("frame", wordsDir)
        };

        // 2. ai Team Words
        List<CVCWordData> aiWords = new List<CVCWordData>()
        {
            GetOrCreateWordData("rain", wordsDir),
            GetOrCreateWordData("snail", wordsDir),
            GetOrCreateWordData("mail", wordsDir),
            GetOrCreateWordData("nail", wordsDir),
            GetOrCreateWordData("chain", wordsDir),
            GetOrCreateWordData("paint", wordsDir),
            GetOrCreateWordData("brain", wordsDir),
            GetOrCreateWordData("pail", wordsDir)
        };

        // 3. ay Team Words
        List<CVCWordData> ayWords = new List<CVCWordData>()
        {
            GetOrCreateWordData("day", wordsDir),
            GetOrCreateWordData("hay", wordsDir),
            GetOrCreateWordData("ray", wordsDir),
            GetOrCreateWordData("say", wordsDir),
            GetOrCreateWordData("play", wordsDir),
            GetOrCreateWordData("pray", wordsDir),
            GetOrCreateWordData("tray", wordsDir),
            GetOrCreateWordData("clay", wordsDir)
        };

        U6_LongVowelTeamData teamA_E = GetOrCreateTeamData("Team_A_E", "a_e", a_eWords, teamsDir);
        U6_LongVowelTeamData teamAI = GetOrCreateTeamData("Team_AI", "ai", aiWords, teamsDir);
        U6_LongVowelTeamData teamAY = GetOrCreateTeamData("Team_AY", "ay", ayWords, teamsDir);

        List<U6_LongVowelTeamData> aTeams = new List<U6_LongVowelTeamData>() { teamA_E, teamAI, teamAY };
        U6_LevelData levelLongA = GetOrCreateLevelData("Level_Long_A_teams", "Long A Teams", aTeams, levelsDir);
        levelLongA.sillySentenceText = "The snail plays in the rain with a cake on a tray!";

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Unit 6 Generator] Successfully updated Unit 6 Data Assets matching Page 24 & Page 25!");
    }

    private static void EnsureDirectoryExists(string dir)
    {
        if (!AssetDatabase.IsValidFolder(dir))
        {
            string parent = System.IO.Path.GetDirectoryName(dir).Replace("\\", "/");
            string folder = System.IO.Path.GetFileName(dir);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static CVCWordData GetOrCreateWordData(string word, string dir)
    {
        string path = $"{dir}/Word_{word}.asset";
        CVCWordData data = AssetDatabase.LoadAssetAtPath<CVCWordData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<CVCWordData>();
            data.word = word;
            AssetDatabase.CreateAsset(data, path);
        }
        else
        {
            data.word = word;
            EditorUtility.SetDirty(data);
        }

        // Auto-assign picture & audio if found in project
        string[] picGuids = AssetDatabase.FindAssets($"{word} t:Sprite");
        if (picGuids.Length > 0)
        {
            data.wordPicture = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(picGuids[0]));
        }

        string[] audioGuids = AssetDatabase.FindAssets($"{word} t:AudioClip");
        if (audioGuids.Length > 0)
        {
            data.fullWordAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(audioGuids[0]));
        }

        EditorUtility.SetDirty(data);
        return data;
    }

    private static U6_LongVowelTeamData GetOrCreateTeamData(string teamName, string spelling, List<CVCWordData> words, string dir)
    {
        string path = $"{dir}/{teamName}.asset";
        U6_LongVowelTeamData data = AssetDatabase.LoadAssetAtPath<U6_LongVowelTeamData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<U6_LongVowelTeamData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.teamSpelling = spelling;
        data.teamWords = words;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static U6_LevelData GetOrCreateLevelData(string levelName, string title, List<U6_LongVowelTeamData> teams, string dir)
    {
        string path = $"{dir}/{levelName}.asset";
        U6_LevelData data = AssetDatabase.LoadAssetAtPath<U6_LevelData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<U6_LevelData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.levelTitle = title;
        data.teams = teams;
        EditorUtility.SetDirty(data);
        return data;
    }
}
#endif
