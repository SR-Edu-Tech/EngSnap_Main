using UnityEngine;

public enum ShortVowelType
{
    Short_A,
    Short_E,
    Short_I,
    Short_O,
    Short_U
}

[CreateAssetMenu(fileName = "ShortVowelWord_", menuName = "Phonics/Short Vowel Word Data")]
public class U2_SB_ShortVowelWordData_Phonics_Junior : ScriptableObject
{
    [Header("Word Info")]
    public string wordText;
    public ShortVowelType vowelType;

    [Header("Audio Clips")]
    [Tooltip("Audio sounding the word slowly (segmented phonic blend)")]
    public AudioClip slowAudio;

    [Tooltip("Audio sounding the word naturally (full word)")]
    public AudioClip naturalAudio;

    [Header("Visual (Optional)")]
    public Sprite wordImage;
}