#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class U9_DataAssetGenerator
{
    private const string TARGET_FOLDER = "Assets/Data/unit 9";
    private static readonly string[] AUDIO_SEARCH_FOLDERS = new[]
    {
        "Assets/Audio Clips/Unit 9",
        "Assets/Audio Clips/Unit 9/sound clips",
        "Assets/Audio Clips/Unit 9/Whole-word clips",
        "Assets/Audio Clips/General"
    };

    private static readonly string[] SPRITE_SEARCH_FOLDERS = new[]
    {
        "Assets/Art/Unit 9",
        "Assets/Art"
    };

    private static Dictionary<string, Sprite> cachedSpriteMap = null;

    [MenuItem("Phonics/Generate Unit 9 Data Assets")]
    public static void GenerateDataAssets()
    {
        // Cache all sprites in project with case-insensitive keys
        BuildSpriteCache();

        // 1. Ensure output folders exist in Assets/Data/unit 9
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(TARGET_FOLDER))
            AssetDatabase.CreateFolder("Assets/Data", "unit 9");
        if (!AssetDatabase.IsValidFolder($"{TARGET_FOLDER}/Digraphs"))
            AssetDatabase.CreateFolder(TARGET_FOLDER, "Digraphs");
        if (!AssetDatabase.IsValidFolder($"{TARGET_FOLDER}/Words"))
            AssetDatabase.CreateFolder(TARGET_FOLDER, "Words");

        // 2. Generate 8 Digraph Tile Data assets matching Page 32 textbook EXACTLY
        List<DigraphTileData> digraphTiles = CreateDigraphTileData();

        // 3. Generate Digraph Word Data
        List<DigraphWordData> stage1Words = CreateStage1Words();
        List<DigraphWordData> stage2Words = CreateStage2Words();
        List<DigraphWordData> stage3Words = CreateStage3Words();
        List<DigraphWordData> pickWords   = CreatePickDigraphWords();

        // 4. Create Master Level Data
        CreateMasterLevelData(digraphTiles, stage1Words, stage2Words, stage3Words, pickWords);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Unit 9] Data Generation Complete! Generated ScriptableObjects at '{TARGET_FOLDER}' for 8 Page-32 Digraph tiles (with matched audio clips & sprites), {stage1Words.Count} Stage 1 words, {stage2Words.Count} Stage 2 words, {stage3Words.Count} Stage 3 words, and {pickWords.Count} Pick-Digraph words.");
    }

    private static void BuildSpriteCache()
    {
        cachedSpriteMap = new Dictionary<string, Sprite>();
        string[] validFolders = GetValidFolders(SPRITE_SEARCH_FOLDERS);

        foreach (string folder in validFolders)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (string g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (Object obj in assets)
                {
                    if (obj is Sprite s && !string.IsNullOrEmpty(s.name))
                    {
                        string cleanKey = CleanKey(s.name);
                        if (!cachedSpriteMap.ContainsKey(cleanKey))
                            cachedSpriteMap[cleanKey] = s;
                    }
                }
            }

            // Also check individual Sprites
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            foreach (string g in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s != null && !string.IsNullOrEmpty(s.name))
                {
                    string cleanKey = CleanKey(s.name);
                    if (!cachedSpriteMap.ContainsKey(cleanKey))
                        cachedSpriteMap[cleanKey] = s;
                }
            }
        }
    }

    private static string CleanKey(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        string k = input.ToLower().Trim();
        if (k.EndsWith(" 1")) k = k.Substring(0, k.Length - 2).Trim();
        return k;
    }

    private static List<DigraphTileData> CreateDigraphTileData()
    {
        List<DigraphTileData> list = new List<DigraphTileData>();

        // Exact 8 items on Page 32 Textbook Chart:
        // Left col: ch- chain, sh- shark, th- three, wh- wheel
        // Right col: -ch switch, -sh trash, -th earth, -ck duck
        var defs = new (string key, string word, string soundKey, string assetName)[]
        {
            ("ch-", "chain",  "ch",   "Tile_CH_Start"),
            ("-ch", "switch", "ch",   "Tile_CH_End"),
            ("sh-", "shark",  "sh",   "Tile_SH_Start"),
            ("-sh", "trash",  "sh",   "Tile_SH_End"),
            ("th-", "three",  "th",   "Tile_TH_Start"),
            ("-th", "earth",  "th",   "Tile_TH_End"),
            ("wh-", "wheel",  "wh",   "Tile_WH_Start"),
            ("-ck", "duck",   "k",    "Tile_CK_End")
        };

        foreach (var def in defs)
        {
            string path = $"{TARGET_FOLDER}/Digraphs/{def.assetName}.asset";
            DigraphTileData tile = AssetDatabase.LoadAssetAtPath<DigraphTileData>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<DigraphTileData>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.digraphKey  = def.key;
            tile.displayText = def.key;
            tile.startWord   = def.word;
            tile.endWord     = def.word;

            // Link digraph sound clip (e.g. /ch/ chain) AND word audio clip (e.g. chain.mp3, switch.mp3, trash.mp3...)
            tile.digraphSoundClip = FindAudioClip($"{def.soundKey} ") ?? FindAudioClip($"u9_dg_{def.soundKey}") ?? FindAudioClip(def.soundKey);
            tile.wordAudioClip    = FindAudioClip(def.word) ?? FindAudioClip($"u9_w_{def.word}");
            tile.startWordSprite  = FindSprite(def.word);
            tile.endWordSprite    = tile.startWordSprite;
            tile.digraphIcon      = tile.startWordSprite;

            EditorUtility.SetDirty(tile);
            list.Add(tile);
        }

        return list;
    }

    private static List<DigraphWordData> CreateStage1Words()
    {
        List<DigraphWordData> list = new List<DigraphWordData>();

        // ch (pg 33) & sh (pg 34)
        var defs = new (string word, string dg, string[] chunks)[]
        {
            ("chick", "ch", new[] { "ch", "i", "ck" }),
            ("chin",  "ch", new[] { "ch", "i", "n" }),
            ("chill", "ch", new[] { "ch", "i", "ll" }),
            ("chimp", "ch", new[] { "ch", "i", "m", "p" }),
            ("chips", "ch", new[] { "ch", "i", "p", "s" }),
            ("chess", "ch", new[] { "ch", "e", "ss" }),
            ("check", "ch", new[] { "ch", "e", "ck" }),
            ("chest", "ch", new[] { "ch", "e", "s", "t" }),
            ("chop",  "ch", new[] { "ch", "o", "p" }),
            ("chum",  "ch", new[] { "ch", "u", "m" }),

            ("ship",  "sh", new[] { "sh", "i", "p" }),
            ("fish",  "sh", new[] { "f", "i", "sh" }),
            ("dish",  "sh", new[] { "d", "i", "sh" }),
            ("shop",  "sh", new[] { "sh", "o", "p" }),
            ("shot",  "sh", new[] { "sh", "o", "t" }),
            ("shell", "sh", new[] { "sh", "e", "ll" }),
            ("cash",  "sh", new[] { "c", "a", "sh" }),
            ("mash",  "sh", new[] { "m", "a", "sh" }),
            ("rush",  "sh", new[] { "r", "u", "sh" }),
            ("shut",  "sh", new[] { "sh", "u", "t" })
        };

        foreach (var def in defs)
        {
            list.Add(CreateWordAsset(def.word, def.dg, def.chunks, $"Stage1_{def.word}"));
        }
        return list;
    }

    private static List<DigraphWordData> CreateStage2Words()
    {
        List<DigraphWordData> list = new List<DigraphWordData>();

        // th (pg 35-36) & wh (pg 37)
        var defs = new (string word, string dg, string[] chunks)[]
        {
            ("thin",  "th", new[] { "th", "i", "n" }),
            ("thud",  "th", new[] { "th", "u", "d" }),
            ("thumb", "th", new[] { "th", "u", "mb" }),
            ("bath",  "th", new[] { "b", "a", "th" }),
            ("math",  "th", new[] { "m", "a", "th" }),
            ("think", "th", new[] { "th", "i", "nk" }),
            ("thorn", "th", new[] { "th", "o", "rn" }),
            ("path",  "th", new[] { "p", "a", "th" }),
            ("moth",  "th", new[] { "m", "o", "th" }),
            ("with",  "th", new[] { "w", "i", "th" }),
            ("this",  "th", new[] { "th", "i", "s" }),
            ("that",  "th", new[] { "th", "a", "t" }),
            ("them",  "th", new[] { "th", "e", "m" }),
            ("then",  "th", new[] { "th", "e", "n" }),
            ("these", "th", new[] { "th", "e", "se" }),
            ("those", "th", new[] { "th", "o", "se" }),

            ("which", "wh", new[] { "wh", "i", "ch" }),
            ("while", "wh", new[] { "wh", "i", "le" }),
            ("where", "wh", new[] { "wh", "e", "re" }),
            ("when",  "wh", new[] { "wh", "e", "n" }),
            ("whiz",  "wh", new[] { "wh", "i", "z" }),
            ("whip",  "wh", new[] { "wh", "i", "p" })
        };

        foreach (var def in defs)
        {
            list.Add(CreateWordAsset(def.word, def.dg, def.chunks, $"Stage2_{def.word}"));
        }
        return list;
    }

    private static List<DigraphWordData> CreateStage3Words()
    {
        List<DigraphWordData> list = new List<DigraphWordData>();

        // ck (pg 38), nk (pg 39), ng (pg 40)
        var defs = new (string word, string dg, string[] chunks)[]
        {
            ("back", "ck", new[] { "b", "a", "ck" }),
            ("sack", "ck", new[] { "s", "a", "ck" }),
            ("neck", "ck", new[] { "n", "e", "ck" }),
            ("peck", "ck", new[] { "p", "e", "ck" }),
            ("kick", "ck", new[] { "k", "i", "ck" }),
            ("sick", "ck", new[] { "s", "i", "ck" }),
            ("rock", "ck", new[] { "r", "o", "ck" }),
            ("sock", "ck", new[] { "s", "o", "ck" }),
            ("duck", "ck", new[] { "d", "u", "ck" }),
            ("luck", "ck", new[] { "l", "u", "ck" }),

            ("bank",  "nk", new[] { "b", "a", "nk" }),
            ("thank", "nk", new[] { "th", "a", "nk" }),
            ("wink",  "nk", new[] { "w", "i", "nk" }),
            ("drink", "nk", new[] { "dr", "i", "nk" }),

            ("wing",  "ng", new[] { "w", "i", "ng" }),
            ("bring", "ng", new[] { "br", "i", "ng" }),
            ("long",  "ng", new[] { "l", "o", "ng" }),
            ("hung",  "ng", new[] { "h", "u", "ng" }),
            ("king",  "ng", new[] { "k", "i", "ng" })
        };

        foreach (var def in defs)
        {
            list.Add(CreateWordAsset(def.word, def.dg, def.chunks, $"Stage3_{def.word}"));
        }
        return list;
    }

    private static List<DigraphWordData> CreatePickDigraphWords()
    {
        List<DigraphWordData> list = new List<DigraphWordData>();

        // Page 41 pick items
        var defs = new (string word, string dg, string incText, string[] chunks)[]
        {
            ("cheese",    "ch", "___eese",     new[] { "ch", "ee", "se" }),
            ("phone",     "ph", "___one",      new[] { "ph", "o", "ne" }),
            ("knee",      "kn", "___ee",       new[] { "kn", "ee" }),
            ("wheel",     "wh", "___eel",      new[] { "wh", "ee", "l" }),
            ("thorn",     "th", "___orn",      new[] { "th", "o", "rn" }),
            ("sheep",     "sh", "___eep",      new[] { "sh", "ee", "p" }),
            ("wheat",     "wh", "___eat",      new[] { "wh", "ea", "t" }),
            ("thumb",     "th", "___umb",      new[] { "th", "u", "mb" }),
            ("shark",     "sh", "___ark",      new[] { "sh", "a", "rk" }),
            ("kneel",     "kn", "___eel",      new[] { "kn", "ee", "l" }),
            ("photo",     "ph", "___oto",      new[] { "ph", "o", "to" }),
            ("chicken",   "ch", "___icken",    new[] { "ch", "i", "ck", "en" }),
            ("ship",      "sh", "___ip",       new[] { "sh", "i", "p" }),
            ("chat",      "ch", "___at",       new[] { "ch", "a", "t" }),
            ("telephone", "ph", "tele___one",  new[] { "te", "le", "ph", "o", "ne" }),
            ("shoe",      "sh", "___oe",       new[] { "sh", "oe" }),
            ("cheek",     "ch", "___eek",      new[] { "ch", "ee", "k" }),
            ("cherry",    "ch", "___erry",     new[] { "ch", "e", "rry" }),
            ("phoenix",   "ph", "___oenix",    new[] { "ph", "oe", "nix" }),
            ("whale",     "wh", "___ale",      new[] { "wh", "a", "le" }),
            ("three",     "th", "___ree",      new[] { "th", "r", "ee" }),
            ("what",      "wh", "___at",       new[] { "wh", "a", "t" })
        };

        foreach (var def in defs)
        {
            string path = $"{TARGET_FOLDER}/Words/Pick_{def.word}.asset";
            DigraphWordData asset = AssetDatabase.LoadAssetAtPath<DigraphWordData>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<DigraphWordData>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.wordText           = def.word;
            asset.targetDigraph      = def.dg;
            asset.incompleteWordText = def.incText;
            asset.wordAudio          = FindAudioClip(def.word) ?? FindAudioClip($"u9_w_{def.word}");
            asset.pictureSprite      = FindSprite(def.word);

            if (asset.arrowChunks == null) asset.arrowChunks = new List<string>();
            asset.arrowChunks.Clear();
            asset.arrowChunks.AddRange(def.chunks);

            EditorUtility.SetDirty(asset);
            list.Add(asset);
        }
        return list;
    }

    private static DigraphWordData CreateWordAsset(string word, string dg, string[] chunks, string assetName)
    {
        string path = $"{TARGET_FOLDER}/Words/{assetName}.asset";
        DigraphWordData asset = AssetDatabase.LoadAssetAtPath<DigraphWordData>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<DigraphWordData>();
            AssetDatabase.CreateAsset(asset, path);
        }

        asset.wordText      = word;
        asset.targetDigraph = dg;

        if (asset.arrowChunks == null) asset.arrowChunks = new List<string>();
        asset.arrowChunks.Clear();
        asset.arrowChunks.AddRange(chunks);

        asset.wordAudio     = FindAudioClip(word) ?? FindAudioClip($"u9_w_{word}");
        asset.pictureSprite = FindSprite(word);

        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static void CreateMasterLevelData(
        List<DigraphTileData> digraphs,
        List<DigraphWordData> stage1,
        List<DigraphWordData> stage2,
        List<DigraphWordData> stage3,
        List<DigraphWordData> pickWords)
    {
        string path = $"{TARGET_FOLDER}/Unit9Level_Main.asset";
        Unit9LevelData levelData = AssetDatabase.LoadAssetAtPath<Unit9LevelData>(path);
        if (levelData == null)
        {
            levelData = ScriptableObject.CreateInstance<Unit9LevelData>();
            AssetDatabase.CreateAsset(levelData, path);
        }

        levelData.introDigraphs    = digraphs;
        levelData.stage1Words      = stage1;
        levelData.stage2Words      = stage2;
        levelData.stage3Words      = stage3;
        levelData.pickDigraphWords = pickWords;

        EditorUtility.SetDirty(levelData);
    }

    private static string[] GetValidFolders(string[] folders)
    {
        List<string> valid = new List<string>();
        foreach (string f in folders)
        {
            if (AssetDatabase.IsValidFolder(f)) valid.Add(f);
        }
        return valid.ToArray();
    }

    private static AudioClip FindAudioClip(string searchPattern)
    {
        if (string.IsNullOrEmpty(searchPattern)) return null;

        string[] validFolders = GetValidFolders(AUDIO_SEARCH_FOLDERS);
        if (validFolders.Length > 0)
        {
            string[] guids = AssetDatabase.FindAssets($"{searchPattern} t:AudioClip", validFolders);
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }

        // Fallback global audio search
        string[] globalGuids = AssetDatabase.FindAssets($"{searchPattern} t:AudioClip");
        if (globalGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(globalGuids[0]);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        return null;
    }

    private static Sprite FindSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName) || cachedSpriteMap == null) return null;

        string cleanKey = CleanKey(spriteName);
        if (cachedSpriteMap.TryGetValue(cleanKey, out Sprite found))
        {
            return found;
        }

        // Fallback fuzzy search in cache
        foreach (var pair in cachedSpriteMap)
        {
            if (pair.Key.Contains(cleanKey) || cleanKey.Contains(pair.Key))
            {
                return pair.Value;
            }
        }

        return null;
    }
}
#endif
