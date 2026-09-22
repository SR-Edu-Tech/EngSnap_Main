using System.Collections;
using UnityEngine;

namespace MastersPhonics
{
    public class U3_SA_AudioManager_Masters_Phonics : MonoBehaviour
    {
        public static U3_SA_AudioManager_Masters_Phonics Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceASource;  // Mascot / Narration (Speech)
        [SerializeField] private AudioSource voiceBSource;  // Word / Pronunciation clips
        [SerializeField] private AudioSource sfxSource;     // Sound effects (clicks, chimes)

        [Header("Global SFX")]
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip celebrationClip;
        public AudioClip clickClip;

        [Header("Unit 3 Specific SFX")]
        [Tooltip("Soft drum beat or clap on beat pad tap")]
        public AudioClip beatTapClip;       // U03_SFX_beat

        [Tooltip("Sharp snip sound when divider drops into gap")]
        public AudioClip splitClickClip;    // U03_SFX_split_click

        [Tooltip("Reverse snip sound when divider removed")]
        public AudioClip splitUndoClip;     // U03_SFX_split_undo

        [Tooltip("Three descending ticks for count-back-three turtle rule animation")]
        public AudioClip countBackClip;     // U03_SFX_countback

        [Tooltip("Low tone for C (Consonant) button")]
        public AudioClip patternCToneClip;  // U03_SFX_pattern_c

        [Tooltip("Higher tone for V (Vowel) button")]
        public AudioClip patternVToneClip;  // U03_SFX_pattern_v

        [Header("Mascot Character Animator / Object")]
        [Tooltip("Mascot object (City theme boy Front view) that should only show while Voice A speaks")]
        [SerializeField] private GameObject mascotObject;

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
            EnsureMascotBinding();
        }

        private void EnsureAudioSources()
        {
            if (voiceASource == null)
            {
                var go = new GameObject("VoiceA_Source", typeof(AudioSource));
                go.transform.SetParent(transform);
                voiceASource = go.GetComponent<AudioSource>();
                voiceASource.playOnAwake = false;
            }

            if (voiceBSource == null)
            {
                var go = new GameObject("VoiceB_Source", typeof(AudioSource));
                go.transform.SetParent(transform);
                voiceBSource = go.GetComponent<AudioSource>();
                voiceBSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                var go = new GameObject("SFX_Source", typeof(AudioSource));
                go.transform.SetParent(transform);
                sfxSource = go.GetComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        private void EnsureMascotBinding()
        {
            if (mascotObject == null)
            {
                var found = GameObject.Find("City theme boy Front view") 
                         ?? GameObject.Find("Mascot") 
                         ?? GameObject.Find("Boy_Mascot");
                if (found != null) mascotObject = found;
            }

            if (mascotObject != null)
            {
                mascotObject.SetActive(false);
            }
        }

        public void PlayVoiceA(AudioClip clip)
        {
            if (clip == null || voiceASource == null) return;
            voiceASource.Stop();
            voiceASource.clip = clip;
            voiceASource.Play();

            StopCoroutine(nameof(MascotSpeakingRoutine));
            StartCoroutine(MascotSpeakingRoutine(clip.length));
        }

        public void PlayVoiceB(AudioClip clip)
        {
            if (clip == null || voiceBSource == null) return;
            voiceBSource.Stop();
            voiceBSource.clip = clip;
            voiceBSource.Play();
        }

        public void PlayBeat()
        {
            if (beatTapClip != null && sfxSource != null)
                sfxSource.PlayOneShot(beatTapClip);
        }

        public void PlaySplitClick()
        {
            if (splitClickClip != null && sfxSource != null)
                sfxSource.PlayOneShot(splitClickClip);
        }

        public void PlaySplitUndo()
        {
            if (splitUndoClip != null && sfxSource != null)
                sfxSource.PlayOneShot(splitUndoClip);
        }

        public void PlayPatternC()
        {
            if (patternCToneClip != null && sfxSource != null)
                sfxSource.PlayOneShot(patternCToneClip);
        }

        public void PlayPatternV()
        {
            if (patternVToneClip != null && sfxSource != null)
                sfxSource.PlayOneShot(patternVToneClip);
        }

        public void PlayCorrect()
        {
            if (correctClip != null && sfxSource != null)
                sfxSource.PlayOneShot(correctClip);
        }

        public void PlayWrong()
        {
            if (wrongClip != null && sfxSource != null)
                sfxSource.PlayOneShot(wrongClip);
        }

        public void PlayCelebration()
        {
            if (celebrationClip != null && sfxSource != null)
                sfxSource.PlayOneShot(celebrationClip);
        }

        public void PlayClick()
        {
            if (clickClip != null && sfxSource != null)
                sfxSource.PlayOneShot(clickClip);
        }

        public void StopAllAudio()
        {
            StopAllCoroutines();
            if (voiceASource != null) voiceASource.Stop();
            if (voiceBSource != null) voiceBSource.Stop();
            if (sfxSource != null) sfxSource.Stop();
            if (mascotObject != null) mascotObject.SetActive(false);
        }

        private IEnumerator MascotSpeakingRoutine(float duration)
        {
            if (mascotObject != null)
            {
                mascotObject.SetActive(true);
            }

            yield return new WaitForSeconds(duration + 0.1f);

            if (mascotObject != null)
            {
                mascotObject.SetActive(false);
            }
        }
    }
}
