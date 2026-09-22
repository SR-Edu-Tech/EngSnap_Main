using System.Collections;
using UnityEngine;

namespace MastersPhonics
{
    public class U4_SA_AudioManager_Masters_Phonics : MonoBehaviour
    {
        public static U4_SA_AudioManager_Masters_Phonics Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceASource;  // Mascot / Narration (Speech)
        [SerializeField] private AudioSource voiceBSource;  // Word / Pronunciation clips
        [SerializeField] private AudioSource sfxSource;     // Sound effects (clicks, chimes)

        [Header("Global SFX")]
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip celebrationClip;
        public AudioClip clickClip;

        [Header("Unit 4 Specific SFX")]
        [Tooltip("Full drum hit for strong accented syllable")]
        public AudioClip beatStrongClip;   // U04_SFX_beat_strong
        [Tooltip("Muted tap for weak unaccented syllable")]
        public AudioClip beatWeakClip;     // U04_SFX_beat_weak
        [Tooltip("Soft warp/whoosh when letter morphs into upside-down schwa /ə/")]
        public AudioClip morphSchwaClip;   // U04_SFX_morph
        [Tooltip("Small door click on closed syllable door drop")]
        public AudioClip doorCloseClip;    // U04_SFX_door_close
        [Tooltip("Small door creak on open syllable door drop")]
        public AudioClip doorOpenClip;     // U04_SFX_door_open
        [Tooltip("Rising shimmer when map tile fills in")]
        public AudioClip mapFillClip;      // U04_SFX_map_fill

        [Header("Isolated Schwa Audio")]
        [Tooltip("Lazy 'uh' in isolation /ə/")]
        public AudioClip schwaUhClip;      // U04_CHK_schwa

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
                voiceASource.spatialBlend = 0f;
                voiceASource.volume = 1f;
                voiceASource.mute = false;
            }

            if (voiceBSource == null)
            {
                var go = new GameObject("VoiceB_Source", typeof(AudioSource));
                go.transform.SetParent(transform);
                voiceBSource = go.GetComponent<AudioSource>();
                voiceBSource.playOnAwake = false;
                voiceBSource.spatialBlend = 0f;
                voiceBSource.volume = 1f;
                voiceBSource.mute = false;
            }

            if (sfxSource == null)
            {
                var go = new GameObject("SFX_Source", typeof(AudioSource));
                go.transform.SetParent(transform);
                sfxSource = go.GetComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f;
                sfxSource.volume = 1f;
                sfxSource.mute = false;
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

        public void StopAllAudio()
        {
            StopVoiceA();
            StopVoiceB();
            StopSFX();
        }

        public void StopVoiceA()
        {
            if (voiceASource != null)
            {
                voiceASource.Stop();
            }
            StopCoroutine(nameof(MascotSpeakingRoutine));
            if (mascotObject != null)
            {
                mascotObject.SetActive(false);
            }
        }

        public void StopVoiceB()
        {
            if (voiceBSource != null)
            {
                voiceBSource.Stop();
            }
        }

        public void StopSFX()
        {
            if (sfxSource != null)
            {
                sfxSource.Stop();
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

        public void PlayBeatStrong()
        {
            if (beatStrongClip != null && sfxSource != null)
                sfxSource.PlayOneShot(beatStrongClip);
        }

        public void PlayBeatWeak()
        {
            if (beatWeakClip != null && sfxSource != null)
                sfxSource.PlayOneShot(beatWeakClip);
        }

        public void PlayMorphSchwa()
        {
            if (morphSchwaClip != null && sfxSource != null)
                sfxSource.PlayOneShot(morphSchwaClip);
        }

        public void PlaySchwaMorph()
        {
            PlayMorphSchwa();
        }

        public void PlayDoorClose()
        {
            if (doorCloseClip != null && sfxSource != null)
                sfxSource.PlayOneShot(doorCloseClip);
        }

        public void PlayDoorOpen()
        {
            if (doorOpenClip != null && sfxSource != null)
                sfxSource.PlayOneShot(doorOpenClip);
        }

        public void PlayMapFill()
        {
            if (mapFillClip != null && sfxSource != null)
                sfxSource.PlayOneShot(mapFillClip);
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
