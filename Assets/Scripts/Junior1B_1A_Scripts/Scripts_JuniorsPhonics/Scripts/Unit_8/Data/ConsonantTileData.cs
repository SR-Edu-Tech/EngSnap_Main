using UnityEngine;

[CreateAssetMenu(fileName = "ConsonantTile_", menuName = "Phonics/Unit8/ConsonantTileData")]
public class ConsonantTileData : ScriptableObject
{
    public string letter;          // e.g. "b", "c", "d"
    public string soundName;       // e.g. "/b/"
    public string keywordText;     // e.g. "beet"
    public AudioClip keywordAudio; // Sound + Keyword audio clip
    public Sprite keywordSprite;   // Picture illustration
    public bool isVoiced;          // True = Buzz, False = Whisper
}
