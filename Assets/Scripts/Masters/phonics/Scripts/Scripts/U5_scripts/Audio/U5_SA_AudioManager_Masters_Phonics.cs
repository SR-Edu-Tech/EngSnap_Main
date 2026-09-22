using System.Collections;
using UnityEngine;

namespace MastersPhonics
{
    public class U5_SA_AudioManager_Masters_Phonics : MonoBehaviour
    {
        public static U5_SA_AudioManager_Masters_Phonics Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceASource;  // Mascot / Narration
        [SerializeField] private AudioSource voiceBSource;  // Word / Vowel Model
        [SerializeField] private AudioSource sfxSource;     // Sound effects

        [Header("Global SFX")]
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip celebrationClip;
        public AudioClip clickClip;

        [Header("Unit 5 Specific SFX")]
        [Tooltip("U05_SFX_door_slam: Heavy wooden door shutting")]
        public AudioClip doorSlamClip;
        [Tooltip("U05_SFX_gate_open: Wooden/iron gate creaking open")]
        public AudioClip gateOpenClip;
        [Tooltip("U05_SFX_vowel_stretch: Rising pitch swell for vowel going long")]
        public AudioClip vowelStretchClip;
        [Tooltip("U05_SFX_vowel_snap: Snappy short acoustic sound for vowel going short")]
        public AudioClip vowelSnapClip;
        [Tooltip("U05_SFX_grid_select: Soft drag tone per selected letter cell")]
        public AudioClip gridSelectClip;
        [Tooltip("U05_SFX_grid_found: Sparkling chime on valid word found")]
        public AudioClip gridFoundClip;
        [Tooltip("U05_SFX_chunk_snap: Wooden click on syllable tile placed")]
        public AudioClip chunkSnapClip;

        [Header("Mascot Object")]
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

        // --- Voice Channels ---
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

        public void PlayVoicePrompt(AudioClip clip)
        {
            PlayVoiceA(clip);
        }

        public void PlayVoicePrompt(string clipName, string fallbackText = "")
        {
            AudioClip clip = Resources.Load<AudioClip>($"U5_audio/{clipName}");
            if (clip != null)
            {
                PlayVoiceA(clip);
            }
        }

        public void PlayWordAudio(AudioClip clip)
        {
            PlayVoiceB(clip);
        }

        public void PlayWordAudio(string word)
        {
            if (string.IsNullOrEmpty(word)) return;
            word = word.Trim().ToLower();

            AudioClip clip = Resources.Load<AudioClip>($"U5_audio/voice B/{word}")
                          ?? Resources.Load<AudioClip>($"U5_audio/{word}")
                          ?? Resources.Load<AudioClip>($"U5_audio/words MP U5/{word}")
                          ?? Resources.Load<AudioClip>($"U4_audio/words MP U4/{word}")
                          ?? Resources.Load<AudioClip>($"U4_audio/{word}");

            if (clip != null)
            {
                PlayVoiceB(clip);
            }
            else
            {
                Debug.LogWarning($"[U5 AudioManager] Word audio not found for '{word}'");
            }
        }

        // --- SFX Channels ---
        public void PlaySFXClip(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
                sfxSource.PlayOneShot(clip);
        }

        public void PlayDoorSlam()
        {
            if (doorSlamClip != null && sfxSource != null) 
                sfxSource.PlayOneShot(doorSlamClip);
        }

        public void PlayDoorSlamSFX() => PlayDoorSlam();

        public void PlayGateOpen()
        {
            if (gateOpenClip != null && sfxSource != null) 
                sfxSource.PlayOneShot(gateOpenClip);
        }

        public void PlayGateOpenSFX() => PlayGateOpen();

        public void PlayVowelStretch()
        {
            if (vowelStretchClip != null && sfxSource != null) 
                sfxSource.PlayOneShot(vowelStretchClip);
        }

        public void PlayVowelSnap()
        {
            if (vowelSnapClip != null && sfxSource != null) 
                sfxSource.PlayOneShot(vowelSnapClip);
        }

        public void PlayGridSelect()
        {
            if (gridSelectClip != null && sfxSource != null) 
                sfxSource.PlayOneShot(gridSelectClip);
        }

        public void PlayGridFound()
        {
            if (gridFoundClip != null && sfxSource != null) 
                sfxSource.PlayOneShot(gridFoundClip);
        }

        public void PlayChunkSnap()
        {
            if (chunkSnapClip != null && sfxSource != null) 
                sfxSource.PlayOneShot(chunkSnapClip);
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

        public void PlaySFX(string sfxName)
        {
            if (string.IsNullOrEmpty(sfxName)) return;
            string key = sfxName.ToLower();

            if (key.Contains("correct")) PlayCorrect();
            else if (key.Contains("wrong") || key.Contains("wobble")) PlayWrong();
            else if (key.Contains("door")) PlayDoorSlam();
            else if (key.Contains("gate")) PlayGateOpen();
            else if (key.Contains("complete") || key.Contains("celebrat")) PlayCelebration();
            else if (key.Contains("click")) PlayClick();
            else
            {
                AudioClip clip = Resources.Load<AudioClip>($"U5_audio/sfx_{key}");
                if (clip == null) clip = Resources.Load<AudioClip>($"U5_audio/{key}");
                if (clip != null) PlaySFXClip(clip);
            }
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
