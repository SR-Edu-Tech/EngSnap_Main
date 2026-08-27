#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor Data Asset Generator for Unit 10 — Consonant Blends (Pages 42–48).
/// Scans sliced textbook images in Assets/Art/unit 10 & audio clips in Assets/Audio Clips/Unit 10/
/// and populates ScriptableObject assets at Assets/Data/unit 10/.
/// </summary>
public class U10_DataAssetGenerator
{
    private const string TARGET_FOLDER = "Assets/Data/unit 10";
    private const string ART_FOLDER    = "Assets/Art/unit 10";
    private const string AUDIO_FOLDER  = "Assets/Audio Clips/Unit 10";

    private static Dictionary<string, Sprite> spriteCache     = new Dictionary<string, Sprite>();
    private static Dictionary<string, AudioClip> audioCache   = new Dictionary<string, AudioClip>();

    [MenuItem("Phonics/Generate Unit 10 Data Assets")]
    public static void GenerateDataAssets()
    {
        EnsureFolderStructure();

        BuildSpriteCache();
        BuildAudioCache();

        // 1. Blend Tile Assets (Beginning & Ending)
        List<BlendTileData> begBlends = CreateBeginningBlendTiles();
        List<BlendTileData> endBlends = CreateEndingBlendTiles();

        // 2. Section A: Beginning Blend Builder Words (Pages 42-44)
        List<BlendWordData_Phonics_Junior> begBuildWords = CreateBeginningBuilderWords();

        // 3. Section B: Start it Right Game Words (Pages 43 & 45)
        List<BlendWordData_Phonics_Junior> startGameWords = CreateStartItRightWords();

        // 4. Section C: Ending Blend Builder Words (Pages 46-47)
        List<BlendWordData_Phonics_Junior> endBuildWords = CreateEndingBuilderWords();

        // 5. Section D: Finish it Right Game Words (Page 48 Pen the Word)
        List<BlendWordData_Phonics_Junior> finishGameWords = CreateFinishItRightWords();

        // 6. Master Level Data Asset
        CreateMasterLevelData(begBlends, endBlends, begBuildWords, startGameWords, endBuildWords, finishGameWords);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Unit 10] Data Generation Complete! Successfully mapped {spriteCache.Count} sliced textbook image sprites and {audioCache.Count} audio clips across all Unit 10 ScriptableObjects at '{TARGET_FOLDER}'.");
    }

    private static void EnsureFolderStructure()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(TARGET_FOLDER)) AssetDatabase.CreateFolder("Assets/Data", "unit 10");
        if (!AssetDatabase.IsValidFolder($"{TARGET_FOLDER}/Blends")) AssetDatabase.CreateFolder(TARGET_FOLDER, "Blends");
        if (!AssetDatabase.IsValidFolder($"{TARGET_FOLDER}/Words"))  AssetDatabase.CreateFolder(TARGET_FOLDER, "Words");
    }

    private static void BuildSpriteCache()
    {
        spriteCache.Clear();
        string[] artGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ART_FOLDER });
        foreach (string guid in artGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object obj in assets)
            {
                if (obj is Sprite s)
                {
                    string key = CleanKey(s.name);
                    if (!spriteCache.ContainsKey(key)) spriteCache[key] = s;
                }
            }
        }
    }

    private static void BuildAudioCache()
    {
        audioCache.Clear();
        string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { AUDIO_FOLDER });
        foreach (string guid in audioGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                string key = CleanKey(Path.GetFileNameWithoutExtension(path));
                if (!audioCache.ContainsKey(key)) audioCache[key] = clip;
            }
        }
    }

    private static string CleanKey(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";
        string k = name.ToLower().Trim();
        k = k.Replace("-", "_").Replace(" ", "_");
        k = System.Text.RegularExpressions.Regex.Replace(k, @"^u10_pg\d+_", "");
        k = System.Text.RegularExpressions.Regex.Replace(k, @"^u10_", "");
        k = System.Text.RegularExpressions.Regex.Replace(k, @"_imgs?$", "");
        k = System.Text.RegularExpressions.Regex.Replace(k, @"_img$", "");

        if (k.Contains("card_"))
        {
            string[] parts = k.Split('_');
            foreach (string p in parts)
            {
                string cleanSub = p.Replace("card", "").Trim();
                if (!string.IsNullOrEmpty(cleanSub) && System.Text.RegularExpressions.Regex.IsMatch(cleanSub, @"^[a-z]+$"))
                {
                    k = cleanSub;
                    break;
                }
            }
        }
        return k;
    }

    private static List<BlendTileData> CreateBeginningBlendTiles()
    {
        List<BlendTileData> list = new List<BlendTileData>();
        var defs = new (string blend, string word)[]
        {
            ("bl", "blue"),   ("br", "bread"),  ("cl", "cloud"),
            ("cr", "crayons"),("dr", "drum"),   ("fl", "flower"),
            ("fr", "frog"),   ("gl", "glue"),   ("gr", "grapes"),
            ("pl", "plate"),  ("pr", "pretzel"),("sc", "scooter"),
            ("sk", "skate"),  ("sl", "slide"),  ("sm", "smile"),
            ("sn", "snake"),  ("sp", "spoon"),  ("st", "star"),
            ("sw", "swing"),  ("tr", "tree"),   ("tw", "twenty")
        };

        foreach (var def in defs)
        {
            string path = $"{TARGET_FOLDER}/Blends/Tile_BB_{def.blend}.asset";
            BlendTileData tile = AssetDatabase.LoadAssetAtPath<BlendTileData>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<BlendTileData>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.blendKey         = def.blend;
            tile.displayText      = def.blend;
            tile.exampleWord      = def.word;
            tile.isBeginningBlend = true;
            tile.blendSoundClip   = FindAudioClip($"{def.blend} {def.word}") ?? FindAudioClip($"u10_bb_{def.blend}") ?? FindAudioClip(def.blend);
            tile.wordAudioClip    = FindAudioClip(def.word) ?? FindAudioClip($"u10_w_{def.word}");
            tile.exampleSprite    = FindSprite(def.word);
            tile.blendIcon        = tile.exampleSprite;

            EditorUtility.SetDirty(tile);
            list.Add(tile);
        }
        return list;
    }

    private static List<BlendTileData> CreateEndingBlendTiles()
    {
        List<BlendTileData> list = new List<BlendTileData>();
        var defs = new (string blend, string word)[]
        {
            ("nd", "pond"),  ("nk", "skunk"), ("nt", "ant"),   ("ng", "ring"),  ("mp", "stamp"),
            ("st", "nest"),  ("sk", "mask"),  ("ft", "gift"),  ("ct", "elect"), ("pt", "slept"),
            ("lt", "belt"),  ("lk", "chalk"), ("ld", "gold"),  ("lf", "golf"),  ("lp", "help"),
            ("lm", "palm"),  ("rm", "worm"),  ("rn", "yarn"),  ("rp", "harp"),  ("rt", "heart"),
            ("rd", "card"),  ("rf", "scarf"), ("rk", "shark"), ("rl", "girl"),  ("mb", "thumb")
        };

        foreach (var def in defs)
        {
            string path = $"{TARGET_FOLDER}/Blends/Tile_EB_{def.blend}.asset";
            BlendTileData tile = AssetDatabase.LoadAssetAtPath<BlendTileData>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<BlendTileData>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.blendKey         = def.blend;
            tile.displayText      = def.blend;
            tile.exampleWord      = def.word;
            tile.isBeginningBlend = false;
            tile.blendSoundClip   = FindAudioClip($"{def.blend} {def.word}") ?? FindAudioClip($"u10_eb_{def.blend}") ?? FindAudioClip(def.blend);
            tile.wordAudioClip    = FindAudioClip(def.word) ?? FindAudioClip($"u10_w_{def.word}");
            tile.exampleSprite    = FindSprite(def.word);
            tile.blendIcon        = tile.exampleSprite;

            EditorUtility.SetDirty(tile);
            list.Add(tile);
        }
        return list;
    }

    private static List<BlendWordData_Phonics_Junior> CreateBeginningBuilderWords()
    {
        List<BlendWordData_Phonics_Junior> list = new List<BlendWordData_Phonics_Junior>();
        var defs = new (string word, string blend, string[] chunks)[]
        {
            ("blue",    "bl", new[] { "bl", "ue" }),
            ("bread",   "br", new[] { "br", "ea", "d" }),
            ("cloud",   "cl", new[] { "cl", "ou", "d" }),
            ("crayons", "cr", new[] { "cr", "a", "yo", "ns" }),
            ("drum",    "dr", new[] { "dr", "u", "m" }),
            ("flower",  "fl", new[] { "fl", "ow", "er" }),
            ("frog",    "fr", new[] { "fr", "o", "g" }),
            ("glue",    "gl", new[] { "gl", "ue" }),
            ("grapes",  "gr", new[] { "gr", "a", "pe", "s" }),
            ("plate",   "pl", new[] { "pl", "a", "te" }),
            ("pretzel", "pr", new[] { "pr", "et", "ze", "l" }),
            ("scooter", "sc", new[] { "sc", "oo", "te", "r" }),
            ("skate",   "sk", new[] { "sk", "a", "te" }),
            ("slide",   "sl", new[] { "sl", "i", "de" }),
            ("smile",   "sm", new[] { "sm", "i", "le" }),
            ("snake",   "sn", new[] { "sn", "a", "ke" }),
            ("spoon",   "sp", new[] { "sp", "oo", "n" }),
            ("star",    "st", new[] { "st", "a", "r" }),
            ("swing",   "sw", new[] { "sw", "i", "ng" }),
            ("tree",    "tr", new[] { "tr", "ee" }),
            ("twenty",  "tw", new[] { "tw", "e", "nty" })
        };

        foreach (var def in defs)
        {
            list.Add(CreateWordAsset(def.word, def.blend, true, $"___{def.word}", def.chunks, $"BegBuilder_{def.word}"));
        }
        return list;
    }

    private static List<BlendWordData_Phonics_Junior> CreateStartItRightWords()
    {
        List<BlendWordData_Phonics_Junior> list = new List<BlendWordData_Phonics_Junior>();
        var defs = new (string word, string blend, string incText, string[] chunks)[]
        {
            ("snail",   "sn", "__ail",   new[] { "sn", "ai", "l" }),
            ("clock",   "cl", "__ock",   new[] { "cl", "o", "ck" }),
            ("gloves",  "gl", "__oves",  new[] { "gl", "o", "ve", "s" }),
            ("dragon",  "dr", "__agon",  new[] { "dr", "a", "go", "n" }),
            ("flag",    "fl", "__ag",    new[] { "fl", "a", "g" }),
            ("crab",    "cr", "__ab",    new[] { "cr", "a", "b" }),
            ("broom",   "br", "__oom",   new[] { "br", "oo", "m" }),
            ("drum",    "dr", "__um",    new[] { "dr", "u", "m" }),
            ("blocks",  "bl", "__ocks",  new[] { "bl", "o", "ck", "s" }),
            ("floss",   "fl", "__oss",   new[] { "fl", "o", "ss" }),
            ("plum",    "pl", "__um",    new[] { "pl", "u", "m" }),
            ("prize",   "pr", "__ize",   new[] { "pr", "i", "ze" }),
            ("scale",   "sc", "__ale",   new[] { "sc", "a", "le" }),
            ("sky",     "sk", "__y",     new[] { "sk", "y" }),
            ("smile",   "sm", "__ile",   new[] { "sm", "i", "le" }),
            ("snake",   "sn", "__ake",   new[] { "sn", "a", "ke" }),
            ("spoon",   "sp", "__oon",   new[] { "sp", "oo", "n" }),
            ("star",    "st", "__ar",    new[] { "st", "a", "r" }),
            ("swim",    "sw", "__im",    new[] { "sw", "i", "m" }),
            ("tree",    "tr", "__ee",    new[] { "tr", "ee" }),
            ("twig",    "tw", "__ig",    new[] { "tw", "i", "g" })
        };

        foreach (var def in defs)
        {
            list.Add(CreateWordAsset(def.word, def.blend, true, def.incText, def.chunks, $"StartGame_{def.word}"));
        }
        return list;
    }

    private static List<BlendWordData_Phonics_Junior> CreateEndingBuilderWords()
    {
        List<BlendWordData_Phonics_Junior> list = new List<BlendWordData_Phonics_Junior>();
        var defs = new (string word, string blend, string[] chunks)[]
        {
            ("pond",  "nd", new[] { "po", "nd" }),
            ("skunk", "nk", new[] { "sku", "nk" }),
            ("ant",   "nt", new[] { "a", "nt" }),
            ("ring",  "ng", new[] { "ri", "ng" }),
            ("stamp", "mp", new[] { "sta", "mp" }),
            ("nest",  "st", new[] { "ne", "st" }),
            ("mask",  "sk", new[] { "ma", "sk" }),
            ("gift",  "ft", new[] { "gi", "ft" }),
            ("elect", "ct", new[] { "ele", "ct" }),
            ("slept", "pt", new[] { "sle", "pt" }),
            ("belt",  "lt", new[] { "be", "lt" }),
            ("chalk", "lk", new[] { "cha", "lk" }),
            ("gold",  "ld", new[] { "go", "ld" }),
            ("golf",  "lf", new[] { "go", "lf" }),
            ("help",  "lp", new[] { "he", "lp" }),
            ("palm",  "lm", new[] { "pa", "lm" }),
            ("worm",  "rm", new[] { "wo", "rm" }),
            ("yarn",  "rn", new[] { "ya", "rn" }),
            ("harp",  "rp", new[] { "ha", "rp" }),
            ("heart", "rt", new[] { "hea", "rt" }),
            ("card",  "rd", new[] { "ca", "rd" }),
            ("scarf", "rf", new[] { "sca", "rf" }),
            ("shark", "rk", new[] { "sha", "rk" }),
            ("girl",  "rl", new[] { "gi", "rl" }),
            ("thumb", "mb", new[] { "thu", "mb" })
        };

        foreach (var def in defs)
        {
            list.Add(CreateWordAsset(def.word, def.blend, false, $"{def.word}___", def.chunks, $"EndBuilder_{def.word}"));
        }
        return list;
    }

    private static List<BlendWordData_Phonics_Junior> CreateFinishItRightWords()
    {
        List<BlendWordData_Phonics_Junior> list = new List<BlendWordData_Phonics_Junior>();
        // Page 48 Pen the word items (Exact Ending Blend match!)
        var defs = new (string word, string blend, string incText, string[] chunks)[]
        {
            ("stamp",  "mp", "sta___",  new[] { "sta", "mp" }),
            ("skunk",  "nk", "sku___",  new[] { "sku", "nk" }),
            ("mask",   "sk", "ma___",   new[] { "ma", "sk" }),
            ("ant",    "nt", "a___",    new[] { "a", "nt" }),
            ("chalk",  "lk", "cha___",  new[] { "cha", "lk" }),
            ("nest",   "st", "ne___",   new[] { "ne", "st" }),
            ("pond",   "nd", "po___",   new[] { "po", "nd" }),
            ("gift",   "ft", "gi___",   new[] { "gi", "ft" }),
            ("lamp",   "mp", "la___",   new[] { "la", "mp" }),
            ("plant",  "nt", "pla___",  new[] { "pla", "nt" }),
            ("bank",   "nk", "ba___",   new[] { "ba", "nk" }),
            ("tent",   "nt", "te___",   new[] { "te", "nt" }),
            ("clasp",  "sp", "cla___",  new[] { "cla", "sp" }),
            ("desk",   "sk", "de___",   new[] { "de", "sk" }),
            ("hand",   "nd", "ha___",   new[] { "ha", "nd" })
        };

        foreach (var def in defs)
        {
            list.Add(CreateWordAsset(def.word, def.blend, false, def.incText, def.chunks, $"EndGame_{def.word}"));
        }
        return list;
    }

    private static BlendWordData_Phonics_Junior CreateWordAsset(string word, string blend, bool isBeg, string incText, string[] chunks, string assetName)
    {
        string path = $"{TARGET_FOLDER}/Words/{assetName}.asset";
        BlendWordData_Phonics_Junior asset = AssetDatabase.LoadAssetAtPath<BlendWordData_Phonics_Junior>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<BlendWordData_Phonics_Junior>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.wordText           = word;
        asset.targetBlend        = blend;
        asset.isBeginningBlend   = isBeg;
        asset.incompleteWordText = incText;
        asset.wordAudio          = FindAudioClip(word) ?? FindAudioClip($"u10_w_{word}");
        asset.pictureSprite      = FindSprite(word);

        if (asset.blendChunks == null) asset.blendChunks = new List<string>();
        asset.blendChunks.Clear();
        asset.blendChunks.AddRange(chunks);

        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void CreateMasterLevelData(List<BlendTileData> begBlends, List<BlendTileData> endBlends, List<BlendWordData_Phonics_Junior> begBuild, List<BlendWordData_Phonics_Junior> startGame, List<BlendWordData_Phonics_Junior> endBuild, List<BlendWordData_Phonics_Junior> finishGame)
    {
        string path = $"{TARGET_FOLDER}/Unit10_MasterLevelData.asset";
        Unit10LevelData master = AssetDatabase.LoadAssetAtPath<Unit10LevelData>(path);
        if (master == null)
        {
            master = ScriptableObject.CreateInstance<Unit10LevelData>();
            AssetDatabase.CreateAsset(master, path);
        }

        master.beginningBlends         = begBlends;
        master.beginningBuilderWords   = begBuild;
        master.startItRightGameWords   = startGame;
        master.endingBlends            = endBlends;
        master.endingBuilderWords      = endBuild;
        master.finishItRightGameWords  = finishGame;

        EditorUtility.SetDirty(master);
    }

    private static Sprite FindSprite(string keyWord)
    {
        string clean = CleanKey(keyWord);
        if (spriteCache.TryGetValue(clean, out Sprite s)) return s;
        foreach (var kvp in spriteCache)
        {
            if (kvp.Key.Contains(clean) || clean.Contains(kvp.Key))
                return kvp.Value;
        }
        return null;
    }

    private static AudioClip FindAudioClip(string keyWord)
    {
        string clean = CleanKey(keyWord);
        if (audioCache.TryGetValue(clean, out AudioClip c)) return c;
        foreach (var kvp in audioCache)
        {
            if (kvp.Key.Contains(clean) || clean.Contains(kvp.Key))
                return kvp.Value;
        }
        return null;
    }
}
#endif
