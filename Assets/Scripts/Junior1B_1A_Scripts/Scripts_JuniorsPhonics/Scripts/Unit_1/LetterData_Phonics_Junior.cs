using UnityEngine;

[CreateAssetMenu(fileName = "LetterData", menuName = "Phonics/Letter Data")]
public class LetterData_Phonics_Junior : ScriptableObject
{
    [Header("Letter")]
    public string upperCase;
    public string lowerCase;

    [Header("Audio")]
    public AudioClip letterNameAudio;
    public AudioClip letterSoundAudio;


    [Header("Section B")]
    public Sprite letterImage;
    public string objectName;

    public string CombinedText
    {
        get
        {
            if (!string.IsNullOrEmpty(upperCase) && !string.IsNullOrEmpty(lowerCase))
                return $"{upperCase}{lowerCase}";

            return upperCase ?? lowerCase ?? "";
        }
    }
}