using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Phonics/Sound Wall Letter", fileName = "NewSoundWallLetter")]
public class SoundWallLetterData : ScriptableObject
{
    [Header("Letter Character")]
    public string letter;

    [Header("Audio")]
    [Tooltip("Full audio: pure sound + keyword word (e.g. 'a - apple')")]
    public AudioClip soundClip;

    [Tooltip("Pure phonetic sound for Find-It mini-game (e.g. 'aaa')")]
    public AudioClip pureSoundClip;

    [Header("Keyword Visuals")]
    public Sprite keywordImage;
    public string keywordWord;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(letter))
            return;

        EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            string assetPath = AssetDatabase.GetAssetPath(this);

            if (!string.IsNullOrEmpty(assetPath))
            {
                string targetName = letter;
                string currentName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (currentName != targetName)
                {
                    AssetDatabase.RenameAsset(assetPath, targetName);
                }
            }
        };
    }
#endif
}