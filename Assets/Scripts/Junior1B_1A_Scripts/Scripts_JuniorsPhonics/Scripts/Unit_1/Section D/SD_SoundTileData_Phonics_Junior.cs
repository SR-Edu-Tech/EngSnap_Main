using UnityEngine;

public enum SoundCategory
{
    Vowel,
    Monophthong,
    Diphthong,
    Consonant,
    MoreSound
}

[CreateAssetMenu(fileName = "SD_SoundTileData", menuName = "Phonics/Section D/Sound Tile")]
public class SD_SoundTileData_Phonics_Junior : ScriptableObject
{
    public SoundCategory category;

    public string grapheme;
    public string keyword;

    public Sprite image;

    public AudioClip soundClip;
    public AudioClip keywordClip;
}