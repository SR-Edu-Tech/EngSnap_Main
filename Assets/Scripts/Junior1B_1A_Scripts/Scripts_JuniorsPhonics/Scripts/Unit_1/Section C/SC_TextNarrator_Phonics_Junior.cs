using System.Collections;
using TMPro;
using UnityEngine;

public class SC_TextNarrator_Phonics_Junior : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private MascotController_Phonics_Junior mascotController;

    private void Awake()
    {
        EnsureInit();
    }

    private void EnsureInit()
    {
        if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
        if (audioSource == null) audioSource = FindFirstObjectByType<AudioSource>();

        if (mascotController == null) mascotController = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
    }

    public IEnumerator Play(TMP_Text textUI, string message, AudioClip clip)
    {
        EnsureInit();
        StopAllCoroutines();

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            if (mascotController != null) mascotController.HideMascot();
        }

        if (textUI != null) textUI.text = "";

        if (clip != null)
        {
            if (mascotController != null)
            {
                mascotController.ShowMascot();
                mascotController.PlayHiAnimation();
            }

            if (audioSource != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
            }
        }

        float duration = clip != null ? clip.length : 2f;
        int len = !string.IsNullOrEmpty(message) ? message.Length : 1;
        float delay = duration / len;

        if (textUI != null && !string.IsNullOrEmpty(message))
        {
            foreach (char c in message)
            {
                textUI.text += c;
                yield return new WaitForSeconds(delay);
            }
        }

        if (clip != null && audioSource != null)
        {
            yield return new WaitWhile(() => audioSource != null && audioSource.isPlaying);
            if (mascotController != null) mascotController.HideMascot();
        }
    }

    public void StopNarration()
    {
        EnsureInit();
        StopAllCoroutines();

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (mascotController != null) mascotController.HideMascot();
    }
}