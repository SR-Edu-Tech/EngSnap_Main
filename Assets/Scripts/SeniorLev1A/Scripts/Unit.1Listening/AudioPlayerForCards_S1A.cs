using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioPlayerForCards_S1A : MonoBehaviour
{
    [SerializeField] private AudioSource parentAudioSource;
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private TextMeshProUGUI phraseCardText;
    [SerializeField] private GameObject[] activePhraseCards;

    private void OnEnable()
    {
        if (!TryGetComponent<GridLayoutGroup>(out _))
        {
            AudioPlayer();
        }
        else
        {
            parentAudioSource.clip = audioClip;
            parentAudioSource.Play();
            StartCoroutine(IntroAudioEnd());
        }
    }
    #region Each Button Logic
    public void AudioPlayer()
    {
        parentAudioSource.clip = audioClip;
        parentAudioSource.Play();
        phraseCardText.color = Color.blue;
        StartCoroutine(TextColorBackToNormal());
    }
    private IEnumerator TextColorBackToNormal()
    {
        yield return new WaitForSeconds(audioClip.length + 0.4f);
        phraseCardText.color = Color.black;
    }
    #endregion
    private IEnumerator IntroAudioEnd()
    {
        yield return new WaitForSeconds(audioClip.length + 0.5f);
        foreach (GameObject phraseCard in activePhraseCards)
        {
            phraseCard.SetActive(true);
            yield return new WaitForSeconds(2f);
        }
    }
    
}
