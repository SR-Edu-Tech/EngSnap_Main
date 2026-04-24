using UnityEngine;

[System.Serializable]
public class ImageOptionQuestion
{
    public Sprite image;

    [Header("Audio")]
    public AudioClip questionAudio;
    public AudioClip correctAudio;
    public AudioClip wrongAudio;

    [Header("Options")]
    public string[] options;     // size = 3
    public int correctIndex;     // 0,1,2
}