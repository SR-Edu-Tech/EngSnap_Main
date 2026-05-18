using UnityEngine;

/// <summary>
/// ScriptableObject holding all data for the Magic Words gameplay.
/// Create via: Right-click > Create > BB1 > Magic Word Data
/// </summary>
[CreateAssetMenu(fileName = "MagicWordData_MagicWords", menuName = "BB1/MagicWords/Magic Word Data")]
public class MagicWordData_MagicWords_BB1 : ScriptableObject
{
    [System.Serializable]
    public class Round
    {
        [Header("Situation")]
        public Sprite      situationImage;
        public string      situationText;
        public AudioClip   questionAudio;   // "Which magic word do you use?"

        [Header("Options (fill all 3 — correct index is set below)")]
        public string    optionA;
        public string    optionB;
        public string    optionC;

        [Header("Correct Answer")]
        [Range(0, 2)]
        public int correctIndex;            // 0=A, 1=B, 2=C — randomised at runtime

        [Header("Feedback Audio")]
        public AudioClip correctAudio;      // "Well done! That is right!"
        public AudioClip wrongAudio;        // "Try again!"
    }

    [System.Serializable]
    public class ConversationLine
    {
        public string    speakerName;       // "Mike" or "Zoya"
        public Sprite    speakerAvatar;
        public string    lineText;          // Full text with blank shown as "________"
        public string    blankAnswer;       // Leave empty if no blank
        public AudioClip lineAudio;         // Audio before the blank (or full line if no blank)
        public AudioClip afterBlankAudio;   // Audio of the rest of the line after blank (optional)
    }

    [Header("=== SCREEN 1 — Magic Word Quiz ===")]
    public Round[] rounds;

    [Header("Shared Quiz Audio")]
    public AudioClip correctFX;            // short sparkle/chime
    public AudioClip wrongFX;              // short buzzer

    [Header("=== SCREEN 2 — Fill in the Blank ===")]
    public ConversationLine[] lines;

    [Header("Screen 2 Word Bank")]
    public string[] wordBankWords;         // e.g. {"Please","Thank You","Sorry","Excuse Me","Welcome"}
    public AudioClip wordSelectSound;      // soft pop when word tapped
    public AudioClip allDoneAudio;         // celebration clip after last blank filled

    [Header("Shared BGM")]
    public AudioClip bgmClip;
}
