using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    public class U8_SA_AudioManager_Masters_Phonics : MonoBehaviour
    {
        public static U8_SA_AudioManager_Masters_Phonics Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceASource;  // Mascot / Narration
        [SerializeField] private AudioSource voiceBSource;  // Word / Vowel Model
        [SerializeField] private AudioSource sfxSource;     // Sound effects

        [Header("Global SFX (Reused)")]
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip celebrationClip;
        public AudioClip clickClip;

        [Header("Unit 8 Specific SFX (Reused)")]
        [Tooltip("U08_SFX_r_takeover: Low authoritative snap when R character puffs up")]
        public AudioClip rTakeoverClip;
        [Tooltip("U08_SFX_vowel_drain: Descending tone as vowel loses its own sound")]
        public AudioClip vowelDrainClip;
        [Tooltip("U08_SFX_merge: Three tones resolving into one")]
        public AudioClip mergeClip;
        [Tooltip("U08_SFX_bin_collect: High chime with rising pitch as collector fills")]
        public AudioClip binCollectClip;
        [Tooltip("U08_SFX_swipe_fly: Whoosh when card swipes in Activity 4")]
        public AudioClip swipeFlyClip;

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
            if (rTakeoverClip == null) rTakeoverClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_chunk_snap.wav");
            if (vowelDrainClip == null) vowelDrainClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_stretch.wav");
            if (mergeClip == null) mergeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U07_SFX_team_merge.wav");
            if (binCollectClip == null) binCollectClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_grid_found.wav");
            if (swipeFlyClip == null) swipeFlyClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_swipe_left.wav");
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

        public void EnsureMascotBinding()
        {
            if (mascotObject == null)
            {
                Transform m = transform.Find("City theme boy Front view") 
                           ?? transform.Find("Mascot") 
                           ?? transform.Find("MascotObject")
                           ?? transform.Find("Character");
                if (m != null) mascotObject = m.gameObject;
            }
        }

        public void PlayVoiceA(AudioClip clip)
        {
            if (clip == null) return;
            EnsureAudioSources();
            voiceASource.Stop();
            voiceASource.clip = clip;
            voiceASource.Play();

            if (mascotObject != null)
            {
                StopCoroutine("MascotSpeakingRoutine");
                StartCoroutine(MascotSpeakingRoutine(clip.length));
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

        public void StopVoiceA()
        {
            if (voiceASource != null) voiceASource.Stop();
            if (mascotObject != null) mascotObject.SetActive(false);
        }

        public void StopVoiceB()
        {
            if (voiceBSource != null) voiceBSource.Stop();
        }

        public void StopAllSpeech()
        {
            StopVoiceA();
            StopVoiceB();
        }

        // =====================================================================
        // Audio Resolution & Cache Management
        // =====================================================================

        private static Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);

        public static void ClearCache()
        {
            audioClipCache.Clear();
        }

        public static AudioClip ResolveAudio(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            if (audioClipCache.Count == 0)
            {
#if UNITY_EDITOR
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { 
                    "Assets/Audio/U8_audio", 
                    "Assets/SFX", 
                    "Assets/Audio/U7_audio",
                    "Assets/Audio/U5_audio",
                    "Assets/Audio/U4_audio" 
                });
                foreach (var g in guids)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                    AudioClip ac = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (ac != null)
                    {
                        string fn = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                        if (!audioClipCache.ContainsKey(fn)) audioClipCache[fn] = ac;
                        if (!audioClipCache.ContainsKey(ac.name.ToLower())) audioClipCache[ac.name.ToLower()] = ac;
                    }
                }
#endif
                var resClips = Resources.LoadAll<AudioClip>("");
                foreach (var ac in resClips)
                {
                    if (ac != null)
                    {
                        string k = ac.name.ToLower();
                        if (!audioClipCache.ContainsKey(k)) audioClipCache[k] = ac;
                    }
                }
            }

            string target = name.ToLower().Trim();
            if (audioClipCache.TryGetValue(target, out AudioClip direct)) return direct;
            if (audioClipCache.TryGetValue($"u08_wrd_{target}", out AudioClip wrdClip)) return wrdClip;
            if (audioClipCache.TryGetValue($"u08_vo_{target}", out AudioClip voClip)) return voClip;
            if (audioClipCache.TryGetValue($"u08_sfx_{target}", out AudioClip sfxClip)) return sfxClip;
            if (audioClipCache.TryGetValue($"u08_chk_{target}", out AudioClip chkClip)) return chkClip;

            // Explicit Voice A alias dictionary for Unit 8
            Dictionary<string, string> voAliases = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "u08_vo_unit_intro", "unit eight one letter that bullies" },
                { "u08_vo_card1", "the bossy r thinks hes royal" },
                { "u08_vo_card1b", "and look ca should be an open syllable" },
                { "u08_vo_card2", "five spellings tap each one to hear it" },
                { "u08_vo_card3", "now the important bit ar has its own" },
                { "u08_vo_card3b", "thats good news for reading youll never get" },
                { "u08_vo_card4", "there is one rule that helps when that" },
                { "u08_vo_a1_intro", "two words one has the bossy r one" },
                { "u08_vo_a1_tut1", "bid now bird hear what the r did" },
                { "u08_vo_a1_tut2", "its not bird the i and the r" },
                { "u08_vo_a2_intro", "three bins by sound ignore how its spelled" },
                { "u08_vo_a2_reveal", "look at that bin three different spellings all" },
                { "u08_vo_a3_intro", "now by spelling find the two letters and" },
                { "u08_vo_a3_rb", "this round is harder and its supposed to" },
                { "u08_vo_a3_open", "your turn add your own words to each" },
                { "u08_vo_a4_intro", "heres the rule doing real work if the" },
                { "u08_vo_a4_outro", "a warning a few words break it her" },
                { "u08_vo_a5_intro", "last one and ill be straight with you" },
                { "u08_vo_a5_wrong", "listen er ir ur all the same arent" },
                { "u08_vo_a5_rule", "this one you can work out the sounds" },
                { "u08_vo_challenge_intro", "ten questions last ones a word youve never" },
                { "u08_vo_unit_complete", "unit eight done sixty words read fifteen spelled" }
            };

            if (voAliases.TryGetValue(target, out string phraseKey))
            {
                foreach (var kvp in audioClipCache)
                {
                    if (kvp.Key.Contains(phraseKey)) return kvp.Value;
                }
            }

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
                case "r_takeover":
                case "puff":
                    clip = rTakeoverClip;
                    break;
                case "vowel_drain":
                case "drain":
                    clip = vowelDrainClip;
                    break;
                case "merge":
                case "team_merge":
                    clip = mergeClip;
                    break;
                case "bin_collect":
                case "collect":
                    clip = binCollectClip;
                    break;
                case "swipe":
                case "whoosh":
                    clip = swipeFlyClip;
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

        public void PlayCelebration()
        {
            if (celebrationClip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(celebrationClip);
            }
            else
            {
                PlaySFX("celebration");
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
