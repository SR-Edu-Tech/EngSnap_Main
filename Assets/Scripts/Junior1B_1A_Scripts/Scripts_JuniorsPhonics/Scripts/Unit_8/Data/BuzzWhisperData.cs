using UnityEngine;

[CreateAssetMenu(fileName = "BuzzWhisper_", menuName = "Phonics/Unit8/BuzzWhisperData")]
public class BuzzWhisperData : ScriptableObject
{
    public string phonemeKey;     // e.g. "/z/", "/s/", "/v/", "/f/"
    public string sampleWord;     // e.g. "zoo", "sun", "van", "fan"
    public AudioClip soundAudio;   // Short clear sound audio clip
    public bool isVoiced;          // True = Buzz (vibrates throat), False = Whisper (no vibration)
}
