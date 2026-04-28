using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public enum Masters_SFX {

    Incorrect,
    Correct,
    Pop,
    SelectPositive,
    SelectNegative

}


public class Masters_AudioManager : Masters_Singleton<Masters_AudioManager> {


    [SerializeField]
    private AudioSource voiceOverAudioSource;
    [SerializeField]
    private AudioSource sfxAudioSource;
    [SerializeField]
    private AudioClip correctAudioClip, incorrectAudioClip, popAudioClip, selectPositiveAudioClip, 
        selectNegativeAudioClip;
    [SerializeField]
    private float minimumPitch;
    [SerializeField]
    private float maximumPitch;


    private Coroutine playAudioClipsArrayCoroutine;


    public void PlayVoiceOver(AudioClip audioClip) {
        StartCoroutine(StopAndPlayVoiceOver(audioClip));
    }

    private IEnumerator StopAndPlayVoiceOver(AudioClip audioClip) {
        StopVoiceOver();
        yield return null;
        voiceOverAudioSource.PlayOneShot(audioClip);
    }

    public void StopVoiceOver() {
        if(playAudioClipsArrayCoroutine != null) {
            StopCoroutine(playAudioClipsArrayCoroutine);
        }

        if (voiceOverAudioSource.isPlaying) {
            voiceOverAudioSource.Stop();
        }
    }

    public IEnumerator WaitForVoiceOverEnd(Action onComplete) {
        yield return null;
        yield return new WaitWhile(() => voiceOverAudioSource.isPlaying);
        onComplete?.Invoke();
    }

    public void PlaySoundEffect(Masters_SFX SFX) {
        sfxAudioSource.pitch = Random.Range(minimumPitch, maximumPitch);

        switch(SFX) {
            case Masters_SFX.Incorrect:
                sfxAudioSource.PlayOneShot(incorrectAudioClip);
                break;
            case Masters_SFX.Correct:
                sfxAudioSource.PlayOneShot(correctAudioClip);
                break;
            case Masters_SFX.Pop:
                sfxAudioSource.PlayOneShot(popAudioClip);
                break;
            case Masters_SFX.SelectPositive:
                sfxAudioSource.PlayOneShot(selectPositiveAudioClip);
                break;
            case Masters_SFX.SelectNegative:
                sfxAudioSource.PlayOneShot(selectNegativeAudioClip);
                break;
        }
    }

    public void PlayAudioClipsArray(AudioClip[] audioClipArray, float timeBetweenAudioClips) {
        playAudioClipsArrayCoroutine = StartCoroutine(PlayAudioClipsArrayCoroutine(audioClipArray, timeBetweenAudioClips));
    }

    private IEnumerator PlayAudioClipsArrayCoroutine(AudioClip[] audioClipArray, float timeBetweenAudioClips) {
        for(int i = 0; i < audioClipArray.Length; i++) {
            voiceOverAudioSource.PlayOneShot(audioClipArray[i]);
            yield return new WaitForSeconds(audioClipArray[i].length + timeBetweenAudioClips);
        }
    }


}
