using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    public class U2_SA_AudioManager_Masters_Phonics : MonoBehaviour
    {
        public static U2_SA_AudioManager_Masters_Phonics Instance { get; private set; }

        [Header("Mascot Character Control")]
        [Tooltip("The 'City theme boy Front view' GameObject representing the Mascot.")]
        [SerializeField] private GameObject mascotGameObject;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceASource; // Mascot narration & instructions
        [SerializeField] private AudioSource voiceBSource; // Word models & pronunciations
        [SerializeField] private AudioSource sfxSource;    // UI pops, clicks, correct, wrong, fanfare

        [Header("Common Unit 2 SFX")]
        [SerializeField] private AudioClip dialClickClip;
        [SerializeField] private AudioClip letterDoubleClip;
        [SerializeField] private AudioClip letterDropClip;
        [SerializeField] private AudioClip swipeClip;
        [SerializeField] private AudioClip correctChimeClip;
        [SerializeField] private AudioClip wrongBuzzerClip;
        [SerializeField] private AudioClip celebrateFanfareClip;

        private Coroutine mascotVisibilityCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureAudioSources();
            AutoBindMascot();
        }

        private void AutoBindMascot()
        {
            if (mascotGameObject == null)
            {
                // Search for "City theme boy Front view" or mascot in scene
                GameObject mascot = GameObject.Find("City theme boy Front view");
                if (mascot == null)
                {
                    Transform t = transform.root.Find("City theme boy Front view") ??
                                  transform.Find("City theme boy Front view");
                    if (t != null) mascot = t.gameObject;
                }

                if (mascot != null)
                {
                    mascotGameObject = mascot;
                }
            }

            // Keep mascot hidden by default
            SetMascotActive(false);
        }

        private void EnsureAudioSources()
        {
            if (voiceASource == null)
            {
                GameObject va = new GameObject("VoiceA_Source");
                va.transform.SetParent(transform);
                voiceASource = va.AddComponent<AudioSource>();
                voiceASource.playOnAwake = false;
            }

            if (voiceBSource == null)
            {
                GameObject vb = new GameObject("VoiceB_Source");
                vb.transform.SetParent(transform);
                voiceBSource = vb.AddComponent<AudioSource>();
                voiceBSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                GameObject sfx = new GameObject("SFX_Source");
                sfx.transform.SetParent(transform);
                sfxSource = sfx.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        public void PlayVoiceA(AudioClip clip)
        {
            if (clip == null) return;
            EnsureAudioSources();

            voiceASource.Stop();
            voiceASource.clip = clip;
            voiceASource.Play();

            // Show mascot while Voice A instruction is speaking, then hide when finished
            if (mascotVisibilityCoroutine != null) StopCoroutine(mascotVisibilityCoroutine);
            mascotVisibilityCoroutine = StartCoroutine(ShowMascotWhilePlaying(clip.length));
        }

        private IEnumerator ShowMascotWhilePlaying(float duration)
        {
            SetMascotActive(true);
            yield return new WaitForSeconds(duration + 0.1f);
            SetMascotActive(false);
            mascotVisibilityCoroutine = null;
        }

        public void SetMascotActive(bool active)
        {
            if (mascotGameObject != null)
            {
                mascotGameObject.SetActive(active);
            }
        }

        public void PlayVoiceB(AudioClip clip)
        {
            if (clip == null) return;
            EnsureAudioSources();
            voiceBSource.Stop();
            voiceBSource.clip = clip;
            voiceBSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            EnsureAudioSources();
            sfxSource.PlayOneShot(clip);
        }

        public void PlayDialClick() => PlaySFX(dialClickClip);
        public void PlayLetterDouble() => PlaySFX(letterDoubleClip);
        public void PlayLetterDrop() => PlaySFX(letterDropClip);
        public void PlaySwipe() => PlaySFX(swipeClip);
        public void PlayCorrect() => PlaySFX(correctChimeClip);
        public void PlayWrong() => PlaySFX(wrongBuzzerClip);
        public void PlayCelebration() => PlaySFX(celebrateFanfareClip);

        public void StopAllAudio()
        {
            if (voiceASource != null) voiceASource.Stop();
            if (voiceBSource != null) voiceBSource.Stop();
            if (sfxSource != null) sfxSource.Stop();

            if (mascotVisibilityCoroutine != null)
            {
                StopCoroutine(mascotVisibilityCoroutine);
                mascotVisibilityCoroutine = null;
            }
            SetMascotActive(false);
        }
    }
}
