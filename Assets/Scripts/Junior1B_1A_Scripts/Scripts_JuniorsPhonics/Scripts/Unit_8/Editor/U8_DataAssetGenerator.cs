#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class U8_DataAssetGenerator
{
    private const string TARGET_FOLDER = "Assets/Resources/Data/Unit8";
    private const string AUDIO_BASE_FOLDER = "Assets/Audio Clips/Unit8";
    private const string AUDIO_WORD_FOLDER = "Assets/Audio Clips/General";
    private const string SPRITE_SHEET_PATH = "Assets/Sprites/Unit8/Unit 8.png";

    [MenuItem("Phonics/Generate Unit 8 Data Assets")]
    public static void GenerateDataAssets()
    {
        // 1. Ensure output folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Data"))
            AssetDatabase.CreateFolder("Assets/Resources", "Data");
        if (!AssetDatabase.IsValidFolder(TARGET_FOLDER))
            AssetDatabase.CreateFolder("Assets/Resources/Data", "Unit8");

        // 2. Create ConsonantTileData objects
        List<ConsonantTileData> tiles = CreateConsonantTileData();

        // 3. Create BuzzWhisperData objects
        List<BuzzWhisperData> bwItems = CreateBuzzWhisperData();

        // 4. Create Master Unit8LevelData
        CreateMasterLevelData(tiles, bwItems);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Unit8] Data Generation Complete! Generated {tiles.Count} consonant tiles and {bwItems.Count} Buzz/Whisper items.");
    }

    private static List<ConsonantTileData> CreateConsonantTileData()
    {
        List<ConsonantTileData> tiles = new List<ConsonantTileData>();

        var consonantDefs = new (string letter, string keyword, string audioPattern)[]
        {
            ("b", "beet", "*b*beet*"),
            ("c", "cat", "*c*cat*"),
            ("d", "duck", "*d*duck*"),
            ("f", "fish", "*f*fish*"),
            ("g", "goat", "*g*goat*"),
            ("h", "house", "*h*house*"),
            ("j", "jam", "*j*jam*"),
            ("k", "kite", "*k*kite*"),
            ("l", "leaf", "*l*leaf*"),
            ("m", "moon", "*m*moon*"),
            ("n", "nest", "*n*nest*"),
            ("p", "pen", "*p*pen*"),
            ("q", "queen", "*q*queen*"),
            ("r", "ring", "*r*ring*"),
            ("s", "sun", "*s*sun*"),
            ("t", "top", "*t*top*"),
            ("v", "van", "*v*van*"),
            ("w", "web", "*w*web*"),
            ("x", "box", "*x*box*"),
            ("y", "yacht", "*y*yacht*"),
            ("z", "zebra", "*z*zebra*")
        };

        // Try loading sliced sprites from sprite sheet
        Dictionary<string, Sprite> spriteMap = LoadSpriteSheetSlices(SPRITE_SHEET_PATH);

        foreach (var def in consonantDefs)
        {
            string assetPath = $"{TARGET_FOLDER}/Tile_{def.letter.ToUpper()}.asset";
            ConsonantTileData tile = AssetDatabase.LoadAssetAtPath<ConsonantTileData>(assetPath);

            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<ConsonantTileData>();
                AssetDatabase.CreateAsset(tile, assetPath);
            }

            tile.letter = def.letter;
            tile.keywordText = def.keyword;

            // Audio lookup
            tile.keywordAudio = FindAudioClip(def.audioPattern, AUDIO_WORD_FOLDER) ?? FindAudioClip(def.letter, AUDIO_BASE_FOLDER);

            // Sprite lookup from sliced sheet or individual asset
            if (spriteMap.TryGetValue(def.keyword.ToLower(), out Sprite slicedSprite))
            {
                tile.keywordSprite = slicedSprite;
            }
            else
            {
                string[] guids = AssetDatabase.FindAssets($"{def.keyword} t:Sprite", new[] { "Assets/Sprites" });
                if (guids.Length > 0)
                {
                    tile.keywordSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            EditorUtility.SetDirty(tile);
            tiles.Add(tile);
        }

        return tiles;
    }

    private static List<BuzzWhisperData> CreateBuzzWhisperData()
    {
        List<BuzzWhisperData> items = new List<BuzzWhisperData>();

        var bwDefs = new (string key, string word, bool isVoiced, string audioPattern)[]
        {
            ("b", "bat", true, "*b*bat*"),
            ("d", "dog", true, "*d*dog*"),
            ("g", "girl", true, "*g*girl*"),
            ("v", "van", true, "*v*van*"),
            ("th", "this", true, "*th*this*"),
            ("z", "zoo", true, "*z*zoo*"),
            ("j", "jam", true, "*j*jam*"),
            ("m", "man", true, "*m*man*"),
            ("n", "nail", true, "*n*nail*"),
            ("ng", "sing", true, "*ng*sing*"),
            ("l", "lamp", true, "*l*lamp*"),
            ("r", "root", true, "*r*root*"),
            ("w", "well", true, "*w*well*"),
            ("y", "yoyo", true, "*y*yoyo*"),
            ("p", "pen", false, "*p*pen*"),
            ("t", "tall", false, "*t*tall*"),
            ("k", "cap", false, "*k*cap*"),
            ("f", "fan", false, "*f*fan*"),
            ("th", "thin", false, "*th*thin*"),
            ("s", "sun", false, "*s*sun*"),
            ("sh", "shore", false, "*sh*shore*"),
            ("h", "hot", false, "*h*hot*"),
            ("ch", "chips", false, "*ch*chips*")
        };

        foreach (var def in bwDefs)
        {
            string prefix = def.isVoiced ? "Buzz" : "Whisper";
            string assetPath = $"{TARGET_FOLDER}/BW_{prefix}_{def.key}.asset";
            BuzzWhisperData bw = AssetDatabase.LoadAssetAtPath<BuzzWhisperData>(assetPath);

            if (bw == null)
            {
                bw = ScriptableObject.CreateInstance<BuzzWhisperData>();
                AssetDatabase.CreateAsset(bw, assetPath);
            }

            bw.phonemeKey = def.key;
            bw.sampleWord = def.word;
            bw.isVoiced = def.isVoiced;
            bw.soundAudio = FindAudioClip(def.audioPattern, AUDIO_WORD_FOLDER) ?? FindAudioClip(def.key, AUDIO_BASE_FOLDER);

            EditorUtility.SetDirty(bw);
            items.Add(bw);
        }

        return items;
    }

    private static void CreateMasterLevelData(List<ConsonantTileData> tiles, List<BuzzWhisperData> bwItems)
    {
        string assetPath = $"{TARGET_FOLDER}/Unit8Level_Main.asset";
        Unit8LevelData levelData = AssetDatabase.LoadAssetAtPath<Unit8LevelData>(assetPath);

        if (levelData == null)
        {
            levelData = ScriptableObject.CreateInstance<Unit8LevelData>();
            AssetDatabase.CreateAsset(levelData, assetPath);
        }

        levelData.consonantsList = tiles;
        levelData.buzzWhisperList = bwItems;

        // Auto-populate connectPairs (Section C) for letters p, d, b, t, m
        List<string> connectTargets = new List<string> { "p", "d", "b", "t", "m" };
        levelData.connectPairs = tiles.FindAll(t => t != null && connectTargets.Contains(t.letter.ToLower()));

        // Auto-populate safariConsonants (Section D)
        levelData.safariConsonants = tiles.ConvertAll(t => t.letter);

        EditorUtility.SetDirty(levelData);
    }

    private static AudioClip FindAudioClip(string searchPattern, string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets($"{searchPattern} t:AudioClip", new[] { folderPath });
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }

    private static Dictionary<string, Sprite> LoadSpriteSheetSlices(string spriteSheetPath)
    {
        Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath);

        foreach (Object obj in assets)
        {
            if (obj is Sprite sprite)
            {
                spriteMap[sprite.name.ToLower()] = sprite;
            }
        }
        return spriteMap;
    }
}
#endif