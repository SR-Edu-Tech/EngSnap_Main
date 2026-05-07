using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class Imageoptionquestion_AboutMe : MonoBehaviour
{
    // Start is called before the first frame update
    [Header("Question Display")]
    [Tooltip("Optional image shown for this question. Leave empty to show only the question text.")]
    public Sprite image;
    [Tooltip("Optional text shown as the question. Leave empty to show only the image.")]
    public string questionText;

    [Header("Audio")]
    public AudioClip questionAudio;
    public AudioClip correctAudio;
    public AudioClip wrongAudio;

    [Header("Options")]
    public string[] options;     // size = 3
    public int correctIndex;   
    }
      // 0,1,2

