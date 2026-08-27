using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class VowelData_Phonics_Junior
{
    public string letter;
    public Button button;
    public AudioClip sound;

    [HideInInspector] public bool completed;
}