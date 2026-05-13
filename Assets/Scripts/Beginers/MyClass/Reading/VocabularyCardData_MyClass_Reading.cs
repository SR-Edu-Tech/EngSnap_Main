using UnityEngine;

/// <summary>
/// ScriptableObject holding data for a single vocabulary card.
/// Create via: Assets > Create > ReadingGameplay > VocabularyCardData
/// </summary>
[CreateAssetMenu(fileName = "NewVocabularyCard", menuName = "ReadingGameplay/VocabularyCardData")]
public class VocabularyCardData_MyClass_Reading : ScriptableObject
{
    [Header("Display")]
    public string wordLabel;          // e.g. "teacher"
    public Sprite illustration;       // coloured illustration shown on top half

    [Header("Audio")]
    public AudioClip wordAudio;       // TTS / recorded word audio
}
