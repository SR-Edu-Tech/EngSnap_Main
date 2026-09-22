using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    public class U7_SA_AudioManager_Masters_Phonics : MonoBehaviour
    {
        public static U7_SA_AudioManager_Masters_Phonics Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceASource;  // Mascot / Narration
        [SerializeField] private AudioSource voiceBSource;  // Word / Vowel Model
        [SerializeField] private AudioSource sfxSource;     // Sound effects

        [Header("Global SFX")]
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip celebrationClip;
        public AudioClip clickClip;

        [Header("Unit 7 Specific SFX")]
        [Tooltip("U07_SFX_team_merge: Two tones sliding and merging into one")]
        public AudioClip teamMergeClip;
        [Tooltip("U07_SFX_wave_glide: Rising-falling sweep for glide wave")]
        public AudioClip waveGlideClip;
        [Tooltip("U07_SFX_wave_hold: Flat sustained tone for hold wave")]
        public AudioClip waveHoldClip;
        [Tooltip("U07_SFX_gap_fill: Soft snap when team tiles drop into gaps")]
        public AudioClip gapFillClip;
        [Tooltip("U07_SFX_bin_collect: High chime with rising pitch as collector fills")]
        public AudioClip binCollectClip;
        [Tooltip("U07_SFX_word_fly: Whoosh when word crosses screen in speed round")]
        public AudioClip wordFlyClip;

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
            EnsureDefaultSFX();
        }

        private void EnsureDefaultSFX()
        {
#if UNITY_EDITOR
            if (correctClip == null) correctClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? ResolveAudio("Correct");
            if (wrongClip == null) wrongClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? ResolveAudio("Incorrect");
            if (clickClip == null) clickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/AB_Pop_boosted_220.mp3");
            if (celebrationClip == null) celebrationClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav");
            if (teamMergeClip == null) teamMergeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_chunk_snap.wav");
            if (waveGlideClip == null) waveGlideClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_stretch.wav");
            if (waveHoldClip == null) waveHoldClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_snap.wav");
            if (gapFillClip == null) gapFillClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_tile_land.wav");
            if (binCollectClip == null) binCollectClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_grid_found.wav");
            if (wordFlyClip == null) wordFlyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_swipe_left.wav");
#endif
            if (correctClip == null) correctClip = ResolveAudio("Correct");
            if (wrongClip == null) wrongClip = ResolveAudio("Incorrect");
        }

        private void EnsureAudioSources()
        {
            if (voiceASource == null)
            {
                var go = new GameObject("VoiceA_Source", typeof(AudioSource));
                go.transform.SetParent(transform, false);
                voiceASource = go.GetComponent<AudioSource>();
                voiceASource.playOnAwake = false;
            }

            if (voiceBSource == null)
            {
                var go = new GameObject("VoiceB_Source", typeof(AudioSource));
                go.transform.SetParent(transform, false);
                voiceBSource = go.GetComponent<AudioSource>();
                voiceBSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                var go = new GameObject("SFX_Source", typeof(AudioSource));
                go.transform.SetParent(transform, false);
                sfxSource = go.GetComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
        }

        private void EnsureMascotBinding()
        {
            if (mascotObject == null)
            {
                Transform m = transform.Find("City theme boy Front view") 
                           ?? transform.Find("Mascot") 
                           ?? transform.Find("CityThemeBoy");
                if (m != null)
                {
                    mascotObject = m.gameObject;
                }
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

        private static Dictionary<string, AudioClip> audioClipCache = null;

        public static AudioClip ResolveAudio(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (audioClipCache == null)
            {
                audioClipCache = new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);
                AudioClip[] allClips = Resources.FindObjectsOfTypeAll<AudioClip>();
                foreach (var c in allClips)
                {
                    if (c == null) continue;
                    string cName = c.name.ToLower();
                    if (!audioClipCache.ContainsKey(cName)) audioClipCache[cName] = c;

                    string clean = cName.Replace("u07_vo_", "").Replace("u07_wrd_", "").Replace("u07_sfx_", "")
                                        .Replace("u07_chk_", "").Replace("u07_dip_", "").Replace("u07_str_", "")
                                        .Replace("u06_wrd_", "").Replace("u05_wrd_", "").Replace("chunk_", "").Trim();
                    if (!string.IsNullOrEmpty(clean) && !audioClipCache.ContainsKey(clean))
                        audioClipCache[clean] = c;
                }
            }

            string target = name.ToLower().Trim();
            if (audioClipCache.TryGetValue(target, out AudioClip direct)) return direct;
            if (audioClipCache.TryGetValue($"u07_wrd_{target}", out AudioClip wrdClip)) return wrdClip;
            if (audioClipCache.TryGetValue($"u07_vo_{target}", out AudioClip voClip)) return voClip;
            if (audioClipCache.TryGetValue($"u07_sfx_{target}", out AudioClip sfxClip)) return sfxClip;
            if (audioClipCache.TryGetValue($"u07_chk_{target}", out AudioClip chkClip)) return chkClip;
            if (audioClipCache.TryGetValue($"u07_dip_{target}", out AudioClip dipClip)) return dipClip;
            if (audioClipCache.TryGetValue($"u07_str_{target}", out AudioClip strClip)) return strClip;

            foreach (var kvp in audioClipCache)
            {
                if (kvp.Key.StartsWith(target) || kvp.Key.Contains(target) || target.Contains(kvp.Key))
                    return kvp.Value;
            }

            return null;
        }

        public void PlayVoicePrompt(string clipName, string fallbackText = "")
        {
            AudioClip clip = ResolveAudio(clipName);
            if (clip != null)
            {
                PlayVoiceA(clip);
            }
        }

        public void PlayWordAudio(string word)
        {
            if (string.IsNullOrEmpty(word)) return;
            AudioClip clip = ResolveAudio(word);
            if (clip != null)
            {
                PlayVoiceB(clip);
            }
        }

        public void PlayWordAudio(AudioClip clip)
        {
            if (clip != null)
            {
                PlayVoiceB(clip);
            }
        }

        public void PlayWord(string word) => PlayWordAudio(word);
        public void PlayWord(AudioClip clip) => PlayVoiceB(clip);

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayAnswerFeedbackSFX(bool isCorrect)
        {
            AudioClip clip = isCorrect ? correctClip : wrongClip;
            if (clip == null)
            {
                clip = isCorrect ? ResolveAudio("Correct") : ResolveAudio("Incorrect");
            }
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayAnswerFeedbackSFX(AudioClip clip, bool isCorrect)
        {
            if (clip != null)
            {
                PlaySFX(clip);
            }
            else
            {
                PlayAnswerFeedbackSFX(isCorrect);
            }
        }

        public void PlaySFX(string sfxName)
        {
            if (sfxSource == null) return;

            AudioClip clip = null;
            switch (sfxName.ToLower())
            {
                case "correct":
                    clip = correctClip;
                    break;
                case "wrong":
                case "incorrect":
                    clip = wrongClip;
                    break;
                case "celebration":
                case "celebrate":
                case "unit_complete":
                case "activity_complete":
                    clip = celebrationClip;
                    break;
                case "team_merge":
                case "merge":
                    clip = teamMergeClip;
                    break;
                case "wave_glide":
                case "glide":
                    clip = waveGlideClip;
                    break;
                case "wave_hold":
                case "hold":
                    clip = waveHoldClip;
                    break;
                case "gap_fill":
                case "snap":
                    clip = gapFillClip;
                    break;
                case "bin_collect":
                case "collect":
                    clip = binCollectClip;
                    break;
                case "word_fly":
                case "whoosh":
                    clip = wordFlyClip;
                    break;
                case "click":
                case "button_click":
                    clip = clickClip;
                    break;
            }

            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayCelebration()
        {
            if (celebrationClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(celebrationClip);
            }
        }

        private IEnumerator MascotSpeakingRoutine(float duration)
        {
            if (mascotObject != null)
            {
                mascotObject.SetActive(true);
            }

            yield return new WaitForSeconds(duration);

            if (mascotObject != null)
            {
                mascotObject.SetActive(false);
            }
        }
    }
}
