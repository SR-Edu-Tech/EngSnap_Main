#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class U1_SoundWallDataAssetGenerator
{
    private const string TARGET_FOLDER = "Assets/Data/unit 1";

    private static Dictionary<string, Sprite> cachedSpriteMap = null;
    private static Dictionary<string, AudioClip> cachedAudioMap = null;

    [MenuItem("Phonics/Generate Unit 1 Sound Wall Data Assets")]
    public static void GenerateDataAssets()
    {
        BuildCaches();

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
            AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(TARGET_FOLDER))
            AssetDatabase.CreateFolder("Assets/Data", "unit 1");

        var tileDefinitions = GetTileDefinitions();

        int updatedCount = 0;
        int newCount = 0;

        foreach (var def in tileDefinitions)
        {
            string safeFileName = SanitizeFileName($"SD_{def.keyword}");
            string assetPath = $"{TARGET_FOLDER}/{safeFileName}.asset";

            SD_SoundTileData_Phonics_Junior asset = AssetDatabase.LoadAssetAtPath<SD_SoundTileData_Phonics_Junior>(assetPath);
            bool isNew = false;
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<SD_SoundTileData_Phonics_Junior>();
                isNew = true;
            }

            asset.category = def.category;
            asset.grapheme = def.grapheme;
            asset.keyword  = def.keyword;

            // 1. Sprite Matching (STRICTLY use sliced sprites in U1_SEC_D.png)
            if (cachedSpriteMap != null)
            {
                string key = def.keyword.ToLower().Trim();
                if (cachedSpriteMap.TryGetValue(key, out Sprite sp))
                {
                    asset.image = sp;
                }
                else
                {
                    Debug.LogWarning($"[Unit 1 Section D] Could not find sliced sprite in U1_SEC_D for keyword '{key}'");
                }
            }

            // 2. IPA Sound Clip Matching (from IPA Phoneme symbol sounds folder)
            if (cachedAudioMap != null && !string.IsNullOrEmpty(def.soundClipName))
            {
                string soundKey = def.soundClipName.ToLower().Trim();
                if (cachedAudioMap.TryGetValue(soundKey, out AudioClip soundClip))
                {
                    asset.soundClip = soundClip;
                }
            }

            // 3. Keyword Word Clip Matching (from Sec D Sounds folder)
            if (cachedAudioMap != null)
            {
                string wordKey = def.keyword.ToLower().Trim();
                string wallWordKey = $"wall_{wordKey}";

                if (cachedAudioMap.TryGetValue(wordKey, out AudioClip wordClip))
                {
                    asset.keywordClip = wordClip;
                }
                else if (cachedAudioMap.TryGetValue(wallWordKey, out AudioClip wallWordClip))
                {
                    asset.keywordClip = wallWordClip;
                }
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
                newCount++;
            }
            else
            {
                EditorUtility.SetDirty(asset);
                updatedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Unit 1 Section D] Successfully created/updated {updatedCount + newCount} assets in '{TARGET_FOLDER}' with sliced U1_SEC_D.png card artwork!");
    }

    private static void BuildCaches()
    {
        // 1. Cache Sprites (STRICTLY load sub-sprites from Assets/Art/unit 1/U1_SEC_D.png)
        cachedSpriteMap = new Dictionary<string, Sprite>();

        string exactSheetPath = "Assets/Art/unit 1/U1_SEC_D.png";
        Object[] sheetAssets = AssetDatabase.LoadAllAssetsAtPath(exactSheetPath);
        if (sheetAssets != null && sheetAssets.Length > 0)
        {
            foreach (Object obj in sheetAssets)
            {
                if (obj is Sprite sp)
                {
                    string key = sp.name.ToLower().Trim();
                    if (!cachedSpriteMap.ContainsKey(key))
                    {
                        cachedSpriteMap[key] = sp;
                    }
                }
            }
        }

        if (cachedSpriteMap.Count == 0)
        {
            string[] secDGuids = AssetDatabase.FindAssets("U1_SEC_D t:Texture2D");
            foreach (string guid in secDGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                sheetAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                if (sheetAssets != null)
                {
                    foreach (Object obj in sheetAssets)
                    {
                        if (obj is Sprite sp)
                        {
                            string key = sp.name.ToLower().Trim();
                            if (!cachedSpriteMap.ContainsKey(key))
                            {
                                cachedSpriteMap[key] = sp;
                            }
                        }
                    }
                }
            }
        }

        Debug.Log($"[Unit 1 Section D] Cached {cachedSpriteMap.Count} sliced card sprites from 'Assets/Art/unit 1/U1_SEC_D.png'");

        // 2. Cache Audio Clips
        cachedAudioMap = new Dictionary<string, AudioClip>();
        string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip");
        foreach (string guid in audioGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null && !string.IsNullOrEmpty(clip.name))
            {
                string key = clip.name.ToLower().Trim();
                if (!cachedAudioMap.ContainsKey(key))
                {
                    cachedAudioMap[key] = clip;
                }
            }
        }
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }

    private class TileDef
    {
        public SoundCategory category;
        public string grapheme;
        public string keyword;
        public string soundClipName;

        public TileDef(SoundCategory cat, string graph, string key, string soundName)
        {
            category = cat;
            grapheme = graph;
            keyword  = key;
            soundClipName = soundName;
        }
    }

    private static List<TileDef> GetTileDefinitions()
    {
        List<TileDef> list = new List<TileDef>();

        // --- MONOPHTHONGS (12) ---
        list.Add(new TileDef(SoundCategory.Monophthong, "i:", "sheep", "ee"));
        list.Add(new TileDef(SoundCategory.Monophthong, "ɪ", "ship", "ih"));
        list.Add(new TileDef(SoundCategory.Monophthong, "ʊ", "good", "ooh"));
        list.Add(new TileDef(SoundCategory.Monophthong, "u:", "shoot", "oo"));
        list.Add(new TileDef(SoundCategory.Monophthong, "e", "bed", "eh"));
        list.Add(new TileDef(SoundCategory.Monophthong, "ə", "teacher", "uh"));
        list.Add(new TileDef(SoundCategory.Monophthong, "3:", "bird", "er"));
        list.Add(new TileDef(SoundCategory.Monophthong, "ɔ:", "door", "or"));
        list.Add(new TileDef(SoundCategory.Monophthong, "æ", "cat", "ah"));
        list.Add(new TileDef(SoundCategory.Monophthong, "ʌ", "up", "uh"));
        list.Add(new TileDef(SoundCategory.Monophthong, "ɑ:", "far", "ar"));
        list.Add(new TileDef(SoundCategory.Monophthong, "ɒ", "on", "off"));

        // --- DIPHTHONGS (8) ---
        list.Add(new TileDef(SoundCategory.Diphthong, "ɪə", "here", "ear"));
        list.Add(new TileDef(SoundCategory.Diphthong, "eɪ", "wait", "ay"));
        list.Add(new TileDef(SoundCategory.Diphthong, "ʊə", "tourist", "oor"));
        list.Add(new TileDef(SoundCategory.Diphthong, "ɔɪ", "boy", "oy"));
        list.Add(new TileDef(SoundCategory.Diphthong, "əʊ", "show", "oh"));
        list.Add(new TileDef(SoundCategory.Diphthong, "eə", "hair", "air"));
        list.Add(new TileDef(SoundCategory.Diphthong, "aɪ", "my", "eye"));
        list.Add(new TileDef(SoundCategory.Diphthong, "aʊ", "cow", "ow"));

        // --- CONSONANTS (24) ---
        list.Add(new TileDef(SoundCategory.Consonant, "p", "pea", "puh"));
        list.Add(new TileDef(SoundCategory.Consonant, "b", "boat", "buh"));
        list.Add(new TileDef(SoundCategory.Consonant, "t", "tea", "tuh"));
        list.Add(new TileDef(SoundCategory.Consonant, "d", "dog", "duh"));
        list.Add(new TileDef(SoundCategory.Consonant, "tʃ", "cheese", "ch"));
        list.Add(new TileDef(SoundCategory.Consonant, "dʒ", "june", "juh"));
        list.Add(new TileDef(SoundCategory.Consonant, "k", "car", "kuh"));
        list.Add(new TileDef(SoundCategory.Consonant, "g", "go", "guh"));
        list.Add(new TileDef(SoundCategory.Consonant, "f", "fly", "fff"));
        list.Add(new TileDef(SoundCategory.Consonant, "v", "video", "vuh"));
        list.Add(new TileDef(SoundCategory.Consonant, "θ", "think", "th"));
        list.Add(new TileDef(SoundCategory.Consonant, "ð", "this", "the"));
        list.Add(new TileDef(SoundCategory.Consonant, "s", "see", "sss"));
        list.Add(new TileDef(SoundCategory.Consonant, "z", "zoo", "zzz"));
        list.Add(new TileDef(SoundCategory.Consonant, "ʃ", "shall", "sh"));
        list.Add(new TileDef(SoundCategory.Consonant, "ʒ", "television", "zh"));
        list.Add(new TileDef(SoundCategory.Consonant, "m", "man", "mmm"));
        list.Add(new TileDef(SoundCategory.Consonant, "n", "now", "nnn"));
        list.Add(new TileDef(SoundCategory.Consonant, "ŋ", "sing", "ng"));
        list.Add(new TileDef(SoundCategory.Consonant, "h", "hat", "hhh"));
        list.Add(new TileDef(SoundCategory.Consonant, "l", "love", "lll"));
        list.Add(new TileDef(SoundCategory.Consonant, "r", "red", "rrr"));
        list.Add(new TileDef(SoundCategory.Consonant, "w", "wet", "wuh"));
        list.Add(new TileDef(SoundCategory.Consonant, "j", "yes", "yuh"));

        return list;
    }
}
#endif
