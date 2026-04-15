using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CharacterAudio : MonoBehaviour
{
    [Header("Character UI")]
    public GameObject targetObject;
    public Text characterText;

    [Header("Audio + Text Pairs")]
    public AudioClip[] audioClips;
    [TextArea] public string[] texts;

    [Header("Timing")]
    public float startDelay = 2f;      
    public float interval = 5f;        

    private AudioSource audioSource;
    private int lastIndex = -1;
    private Coroutine loopRoutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        if (loopRoutine == null)
            loopRoutine = StartCoroutine(PlayLoop());
    }

    void OnDisable()
    {
        if (loopRoutine != null)
            StopCoroutine(loopRoutine);

        loopRoutine = null;
    }

    IEnumerator PlayLoop()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            PlayRandom();

            // Wait until audio finishes
            yield return new WaitWhile(() => audioSource.isPlaying);

            if (targetObject != null)
                targetObject.SetActive(false);

            yield return new WaitForSeconds(interval);
        }
    }

    void PlayRandom()
    {
        if (audioClips == null || audioClips.Length == 0)
            return;

        int index;

      
        do
        {
            index = Random.Range(0, audioClips.Length);
        }
        while (audioClips.Length > 1 && index == lastIndex);

        lastIndex = index;

       
        if (targetObject != null)
            targetObject.SetActive(true);

   
        if (characterText != null && texts != null && index < texts.Length)
            characterText.text = texts[index];

   
        if (audioClips[index] != null)
        {
            audioSource.clip = audioClips[index];
            audioSource.Play();
        }
    }
}
