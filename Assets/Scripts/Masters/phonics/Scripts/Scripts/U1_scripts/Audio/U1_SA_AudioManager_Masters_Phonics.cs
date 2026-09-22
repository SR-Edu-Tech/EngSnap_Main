using UnityEngine;

namespace MastersPhonics
{
    public class U1_SA_AudioManager_Masters_Phonics : MonoBehaviour
    {
        public static U1_SA_AudioManager_Masters_Phonics Instance { get; private set; }

        [Header("Mascot Character Control")]
        [Tooltip("The 'City theme boy Front view' GameObject representing the Mascot.")]
        [SerializeField] private GameObject mascotGameObject;

        [Header("Audio Sources")]
        [Tooltip("Voice A: Mascot (Narration, Instructions, Feedback)")]
        [SerializeField] private AudioSource voiceASource;
        
        [Tooltip("Voice B: Word Model (Clean word/syllable pronunciation)")]
        [SerializeField] private AudioSource voiceBSource;

        private Coroutine mascotVisibilityCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            AutoBindMascot();
        }

        private void AutoBindMascot()
        {
            if (mascotGameObject == null)
            {
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

            SetMascotActive(false);
        }

        public void SetMascotActive(bool active)
        {
            if (mascotGameObject != null)
            {
                mascotGameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Plays audio on Voice A channel (Mascot). Shows mascot while speaking, hides when done.
        /// </summary>
        public void PlayVoiceA(AudioClip clip)
        {
            if (clip == null) return;
            if (voiceASource == null) return;

            voiceASource.Stop();
            voiceASource.clip = clip;
            voiceASource.Play();

            if (mascotVisibilityCoroutine != null) StopCoroutine(mascotVisibilityCoroutine);
            mascotVisibilityCoroutine = StartCoroutine(ShowMascotWhilePlaying(clip.length));
        }

        private System.Collections.IEnumerator ShowMascotWhilePlaying(float duration)
        {
            SetMascotActive(true);
            yield return new WaitForSeconds(duration + 0.1f);
            SetMascotActive(false);
            mascotVisibilityCoroutine = null;
        }

        /// <summary>
        /// Plays audio on Voice B channel (Word Model). Interrupts any currently playing Voice B audio.
        /// </summary>
        public void PlayVoiceB(AudioClip clip)
        {
            if (clip == null) return;
            if (voiceBSource == null) return;
            voiceBSource.Stop();
            voiceBSource.clip = clip;
            voiceBSource.Play();
        }
        
        public void StopAllAudio()
        {
            if (voiceASource != null) voiceASource.Stop();
            if (voiceBSource != null) voiceBSource.Stop();

            if (mascotVisibilityCoroutine != null)
            {
                StopCoroutine(mascotVisibilityCoroutine);
                mascotVisibilityCoroutine = null;
            }
            SetMascotActive(false);
        }
    }
}
