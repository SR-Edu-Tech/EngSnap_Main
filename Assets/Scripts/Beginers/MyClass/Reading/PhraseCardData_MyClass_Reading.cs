using UnityEngine;

/// <summary>
/// ScriptableObject holding data for a classroom-language phrase card (Screen 3).
/// Create via: Assets > Create > ReadingGameplay > PhraseCardData
/// </summary>
[CreateAssetMenu(fileName = "NewPhraseCard", menuName = "ReadingGameplay/PhraseCardData")]
public class PhraseCardData_MyClass_Reading : ScriptableObject
{
    [Header("Display")]
    public string phraseText;         // e.g. "May I come in?"

    [Header("Audio")]
    public AudioClip phraseAudio;     // Boy mascot voice line
}
