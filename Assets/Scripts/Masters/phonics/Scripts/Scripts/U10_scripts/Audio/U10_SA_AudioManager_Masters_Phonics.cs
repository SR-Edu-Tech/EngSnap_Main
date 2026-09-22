using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MastersPhonics
{
    public class U10_SA_AudioManager_Masters_Phonics : MonoBehaviour
    {
        public static U10_SA_AudioManager_Masters_Phonics Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceASource;  // Mascot / Narration
        [SerializeField] private AudioSource voiceBSource;  // Word / Homograph / Sentence Model
        [SerializeField] private AudioSource sfxSource;     // Sound effects

        [Header("Unit 10 Voice A Narration Clips (Mascot)")]
        public AudioClip unitIntroClip;             // "Last unit. And we’re changing subject..."
        public AudioClip card1Clip;                 // "Look at these three words. They all start with homo..."
        public AudioClip card1bClip;                // "Phone turns up in telephone and microphone..."
        public AudioClip card2Clip;                 // "Homophones. Same sound, different spelling..."
        public AudioClip card3Clip;                 // "Homonyms. One spelling, one sound, two meanings..."
        public AudioClip card4Clip;                 // "Homographs. Now this is the interesting one..."
        public AudioClip act1IntroClip;             // "Find the pairs that sound the same..."
        public AudioClip act1Tut1Clip;              // "Sea and see. Different spellings, different meanings..."
        public AudioClip act1RbClip;                // "Now the ones that actually matter..."
        public AudioClip act1ThreeClip;             // "Careful — this one’s a set of three."
        public AudioClip act2IntroClip;             // "Now put them in sentences..."
        public AudioClip act2WrongClip;             // "Read that back. Does it make sense?"
        public AudioClip act2RbClip;                // "These next six are the ones worth getting right..."
        public AudioClip act3IntroClip;             // "Same word, twice, two different jobs..."
        public AudioClip act3PairClip;              // "Same word again. Different job this time."
        public AudioClip act4IntroClip;             // "Last activity. Same spelling, but two ways to say it..."
        public AudioClip act4StressClip;            // "This one’s Unit Four coming back. RE-cord and re-CORD..."
        public AudioClip challengeIntroClip;        // "Twelve questions, and then that’s the whole book."
        public AudioClip unitCompleteClip;          // "Unit Ten, done. Thirty word twins found..."
        public AudioClip courseCompleteClip;        // "And that’s it. All ten units. You’ve gone from prefixes..."

        [Header("Global SFX")]
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip celebrationClip;
        public AudioClip clickClip;

        [Header("Unit 10 Specific SFX")]
        public AudioClip twinMatchSFX;              // Two tones in unison
        public AudioClip meaningFlipSFX;            // Soft page-turn swipe
        public AudioClip certificateSFX;            // Paper unfurl + seal stamp
        public AudioClip finaleSFX;                 // Full orchestral grand fanfare resolve

        [Header("Mascot Object")]
        [SerializeField] private GameObject mascotObject;

        private readonly Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

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
            CacheAllU10Audio();
        }

        private void EnsureDefaultSFX()
        {
#if UNITY_EDITOR
            if (correctClip == null) correctClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? ResolveAudio("Correct");
            if (wrongClip == null) wrongClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? ResolveAudio("Incorrect");
            if (clickClip == null) clickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/AB_Pop_boosted_220.mp3");
            if (celebrationClip == null) celebrationClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav");
            if (twinMatchSFX == null) twinMatchSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_chunk_snap.wav") ?? correctClip;
            if (meaningFlipSFX == null) meaningFlipSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_dial_click.wav") ?? clickClip;
            if (certificateSFX == null) certificateSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_badge_unlock.wav") ?? celebrationClip;
            if (finaleSFX == null) finaleSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav") ?? celebrationClip;
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
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.Equals("City theme boy Front view", StringComparison.OrdinalIgnoreCase) ||
                            t.name.Equals("Mascot", StringComparison.OrdinalIgnoreCase) ||
                            t.name.Equals("Character", StringComparison.OrdinalIgnoreCase))
                        {
                            mascotObject = t.gameObject;
                            break;
                        }
                    }
                }

                if (mascotObject == null && transform.parent != null)
                {
                    Transform m = transform.parent.Find("City theme boy Front view")
                               ?? transform.parent.Find("Mascot")
                               ?? transform.parent.Find("Character");
                    if (m != null) mascotObject = m.gameObject;
                }
            }
        }

        public void SetMascotActive(bool active)
        {
            if (mascotObject != null)
            {
                mascotObject.SetActive(active);
                if (active) mascotObject.transform.SetAsLastSibling();
            }
        }

        public void CacheAllU10Audio()
        {
#if UNITY_EDITOR
            List<string> validPaths = new List<string> { "Assets/Audio/U10_audio", "Assets/Audio", "Assets/SFX" };

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", validPaths.ToArray());
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    string filename = Path.GetFileNameWithoutExtension(path);
                    audioCache[filename] = clip;

                    string cleanKey = CleanString(filename);
                    if (!audioCache.ContainsKey(cleanKey))
                    {
                        audioCache[cleanKey] = clip;
                    }
                }
            }
#endif
        }

        public AudioClip ResolveAudio(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            string target = key.ToLower().Trim();

            // Direct field bindings for Voice A
            if (target.StartsWith("u10_vo_") || target.StartsWith("vo_"))
            {
                if (target.Contains("unit_intro") && unitIntroClip != null) return unitIntroClip;
                if (target.Contains("card1b") && card1bClip != null) return card1bClip;
                if (target.Contains("card1") && card1Clip != null) return card1Clip;
                if (target.Contains("card2") && card2Clip != null) return card2Clip;
                if (target.Contains("card3") && card3Clip != null) return card3Clip;
                if (target.Contains("card4") && card4Clip != null) return card4Clip;
                if (target.Contains("a1_intro") && act1IntroClip != null) return act1IntroClip;
                if (target.Contains("a1_tut") && act1Tut1Clip != null) return act1Tut1Clip;
                if (target.Contains("a1_rb") && act1RbClip != null) return act1RbClip;
                if (target.Contains("a1_three") && act1ThreeClip != null) return act1ThreeClip;
                if (target.Contains("a2_intro") && act2IntroClip != null) return act2IntroClip;
                if (target.Contains("a2_wrong") && act2WrongClip != null) return act2WrongClip;
                if (target.Contains("a2_rb") && act2RbClip != null) return act2RbClip;
                if (target.Contains("a3_intro") && act3IntroClip != null) return act3IntroClip;
                if (target.Contains("a3_pair") && act3PairClip != null) return act3PairClip;
                if (target.Contains("a4_intro") && act4IntroClip != null) return act4IntroClip;
                if (target.Contains("a4_stress") && act4StressClip != null) return act4StressClip;
                if (target.Contains("challenge") && challengeIntroClip != null) return challengeIntroClip;
                if (target.Contains("unit_complete") && unitCompleteClip != null) return unitCompleteClip;
                if (target.Contains("course_complete") && courseCompleteClip != null) return courseCompleteClip;
            }

            // Word, Variant and Sentence Aliases
            Dictionary<string, string> textAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Homophone Sound Normalization
                { "see", "see" },
                { "sea", "see" },
                { "flower", "flour" },
                { "flour", "flour" },
                { "four", "for" },
                { "for", "for" },
                { "hear", "hear" },
                { "here", "hear" },
                { "male", "mail" },
                { "mail", "mail" },
                { "bee", "bee" },
                { "be", "bee" },
                { "tail", "tale" },
                { "tale", "tale" },
                { "knight", "night" },
                { "night", "night" },
                { "their", "there" },
                { "there", "there" },
                { "they're", "there" },
                { "to", "to" },
                { "too", "to" },
                { "two", "to" },
                { "your", "your" },
                { "you're", "your" },
                { "sun", "sun" },
                { "son", "sun" },
                { "write", "right" },
                { "right", "right" },
                { "new", "knew" },
                { "knew", "knew" },
                { "would", "wood" },
                { "wood", "wood" },
                { "way", "way" },
                { "weigh", "way" },
                { "hair", "hair" },
                { "hare", "hair" },
                { "feat", "feet" },
                { "feet", "feet" },
                { "stairs", "stairs" },
                { "stares", "stairs" },

                // Homograph Variants
                { "read_past", "red" },
                { "read_pres", "read" },
                { "lead_metal", "led" },
                { "lead_guide", "lead" },
                { "wind_turn", "wind" },
                { "wind_air", "wind" },
                { "live_verb", "live" },
                { "live_adj", "live" },
                { "close_shut", "close" },
                { "close_near", "close" },
                { "tear_rip", "tear" },
                { "tear_eye", "tear" },
                { "record_noun", "record" },
                { "record_verb", "record" },
                { "desert_noun", "desert" },
                { "desert_verb", "desert" }
            };

            if (textAliases.TryGetValue(key, out string mappedVal))
            {
                key = mappedVal;
            }

            // 1. Direct dictionary cache lookup
            if (audioCache.TryGetValue(key, out AudioClip cached))
                return cached;

            // 2. Cleaned key lookup
            string cleanKey = CleanString(key);
            if (audioCache.TryGetValue(cleanKey, out AudioClip cleanCached))
                return cleanCached;

            // 3. Exact matching over cache
            foreach (var kvp in audioCache)
            {
                string k = CleanString(kvp.Key);
                if (k.Equals(cleanKey, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            // 4. Safe prefix match
            foreach (var kvp in audioCache)
            {
                string k = CleanString(kvp.Key);
                if (k.StartsWith(cleanKey, StringComparison.OrdinalIgnoreCase) || 
                    cleanKey.StartsWith(k, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        // =====================================================================
        // Playback Methods
        // =====================================================================

        private Coroutine voiceACoroutine;
        private Coroutine voiceBCoroutine;

        public void PlayVoiceA(AudioClip clip, System.Action onComplete = null)
        {
            if (clip == null)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureAudioSources();
            EnsureMascotBinding();
            StopVoiceA();

            voiceASource.clip = clip;
            voiceASource.Play();

            SetMascotActive(true);
            voiceACoroutine = StartCoroutine(CoWaitForVoiceA(clip.length, onComplete));
        }

        public void PlayVoiceA(string clipKey, System.Action onComplete = null)
        {
            AudioClip clip = ResolveAudio(clipKey);
            if (clip != null)
            {
                PlayVoiceA(clip, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        public void PlayVoiceB(AudioClip clip, System.Action onComplete = null)
        {
            if (clip == null)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureAudioSources();
            StopVoiceB();

            voiceBSource.clip = clip;
            voiceBSource.Play();

            voiceBCoroutine = StartCoroutine(CoWaitForVoiceB(clip.length, onComplete));
        }

        public void PlayVoiceB(string clipKey, System.Action onComplete = null)
        {
            AudioClip clip = ResolveAudio(clipKey);
            if (clip != null)
            {
                PlayVoiceB(clip, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        public void PlayWord(string word, System.Action onComplete = null)
        {
            PlayVoiceB(word, onComplete);
        }

        public void PlaySentence(string sentence, System.Action onComplete = null)
        {
            PlayVoiceB(sentence, onComplete);
        }

        public void PlayBothWordsSequence(string word1, string word2, System.Action onComplete = null)
        {
            PlayVoiceB(word1, () =>
            {
                StartCoroutine(CoDelayPlayWord(0.25f, word2, onComplete));
            });
        }

        private IEnumerator CoDelayPlayWord(float delay, string word, System.Action onComplete)
        {
            yield return new WaitForSeconds(delay);
            PlayVoiceB(word, onComplete);
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void PlayCorrect() => PlaySFX(correctClip);
        public void PlayWrong() => PlaySFX(wrongClip);
        public void PlayCelebration() => PlaySFX(celebrationClip);
        public void PlayClick() => PlaySFX(clickClip);
        public void PlayTwinMatch() => PlaySFX(twinMatchSFX ?? correctClip);
        public void PlayMeaningFlip() => PlaySFX(meaningFlipSFX ?? clickClip);
        public void PlayCertificate() => PlaySFX(certificateSFX ?? celebrationClip);
        public void PlayFinaleFanfare() => PlaySFX(finaleSFX ?? celebrationClip);

        public void StopVoiceA()
        {
            if (voiceACoroutine != null)
            {
                StopCoroutine(voiceACoroutine);
                voiceACoroutine = null;
            }
            if (voiceASource != null && voiceASource.isPlaying)
            {
                voiceASource.Stop();
            }
            SetMascotActive(false);
        }

        public void StopVoiceB()
        {
            if (voiceBCoroutine != null)
            {
                StopCoroutine(voiceBCoroutine);
                voiceBCoroutine = null;
            }
            if (voiceBSource != null && voiceBSource.isPlaying)
            {
                voiceBSource.Stop();
            }
        }

        public void StopAll()
        {
            StopVoiceA();
            StopVoiceB();
            if (sfxSource != null) sfxSource.Stop();
        }

        private IEnumerator CoWaitForVoiceA(float duration, System.Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            SetMascotActive(false);
            onComplete?.Invoke();
        }

        private IEnumerator CoWaitForVoiceB(float duration, System.Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
        }

        private static string CleanString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Audio Clips & Mascot")]
        public void EditorAutoAssignAudioClipsAndMascot()
        {
            EnsureAudioSources();
            EnsureMascotBinding();
            EnsureDefaultSFX();
            CacheAllU10Audio();

            // Find all audio files under Assets/Audio/U10_audio
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/U10_audio", "Assets/Audio", "Assets/SFX" });
            Dictionary<string, AudioClip> allClips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
            foreach (string g in guids)
            {
                string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                if (clip != null)
                {
                    string fn = System.IO.Path.GetFileNameWithoutExtension(p);
                    allClips[fn] = clip;
                    string cl = CleanString(fn);
                    if (!allClips.ContainsKey(cl)) allClips[cl] = clip;
                }
            }

            AudioClip FindClip(params string[] matchKeywords)
            {
                foreach (var kvp in allClips)
                {
                    bool allMatch = true;
                    foreach (var kw in matchKeywords)
                    {
                        if (!kvp.Key.ToLower().Contains(kw.ToLower()))
                        {
                            allMatch = false;
                            break;
                        }
                    }
                    if (allMatch) return kvp.Value;
                }
                return null;
            }

            // Voice A exact or keyword match
            unitIntroClip = FindClip("Last unit And were changing") ?? FindClip("unit_intro") ?? unitIntroClip;
            card1Clip = FindClip("Look at these three words") ?? FindClip("card1") ?? card1Clip;
            card1bClip = FindClip("Phone turns up in telephone") ?? FindClip("card1b") ?? card1bClip;
            card2Clip = FindClip("Homophones Same sound different") ?? FindClip("card2") ?? card2Clip;
            card3Clip = FindClip("Homonyms One spelling one sound") ?? FindClip("card3") ?? card3Clip;
            card4Clip = FindClip("Homographs Now this is the interesting") ?? FindClip("card4") ?? card4Clip;
            act1IntroClip = FindClip("Find the pairs that sound") ?? FindClip("a1_intro") ?? act1IntroClip;
            act1Tut1Clip = FindClip("Sea and see Different spellings") ?? FindClip("a1_tut1") ?? act1Tut1Clip;
            act1RbClip = FindClip("Now the ones that actually matter") ?? FindClip("a1_rb") ?? act1RbClip;
            act1ThreeClip = FindClip("Careful this ones a set of three") ?? FindClip("a1_three") ?? act1ThreeClip;
            act2IntroClip = FindClip("Now put them in sentences") ?? FindClip("a2_intro") ?? act2IntroClip;
            act2WrongClip = FindClip("Read that back Does it make sense") ?? FindClip("a2_wrong") ?? act2WrongClip;
            act2RbClip = FindClip("These next six are the ones worth") ?? FindClip("a2_rb") ?? act2RbClip;
            act3IntroClip = FindClip("Same word twice two different") ?? FindClip("a3_intro") ?? act3IntroClip;
            act3PairClip = FindClip("Same word again Different job") ?? FindClip("a3_pair") ?? act3PairClip;
            act4IntroClip = FindClip("Last activity Same spelling but") ?? FindClip("a4_intro") ?? act4IntroClip;
            act4StressClip = FindClip("This ones Unit Four coming back") ?? FindClip("a4_stress") ?? act4StressClip;
            challengeIntroClip = FindClip("Twelve questions and then thats") ?? FindClip("challenge_intro") ?? challengeIntroClip;
            unitCompleteClip = FindClip("Unit Ten done Thirty word twins") ?? FindClip("unit_complete") ?? unitCompleteClip;
            courseCompleteClip = FindClip("And thats it All ten units") ?? FindClip("course_complete") ?? courseCompleteClip;

            // SFX - reuse from existing scene unit audio managers or asset paths
            var allAudioManagers = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var m in allAudioManagers)
            {
                if (m == null || m == this) continue;
                string typeName = m.GetType().Name;
                if (typeName.Contains("AudioManager"))
                {
                    var serialized = new UnityEditor.SerializedObject(m);
                    if (correctClip == null) correctClip = serialized.FindProperty("correctClip")?.objectReferenceValue as AudioClip;
                    if (wrongClip == null) wrongClip = serialized.FindProperty("wrongClip")?.objectReferenceValue as AudioClip;
                    if (clickClip == null) clickClip = serialized.FindProperty("clickClip")?.objectReferenceValue as AudioClip;
                    if (celebrationClip == null) celebrationClip = serialized.FindProperty("celebrationClip")?.objectReferenceValue as AudioClip;
                }
            }

            if (correctClip == null)
                correctClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") 
                           ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Correct Answer 1.mp3");
            if (wrongClip == null)
                wrongClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3");
            if (clickClip == null)
                clickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/AB_Pop_boosted_220.mp3");
            if (celebrationClip == null)
                celebrationClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav");
            if (twinMatchSFX == null)
                twinMatchSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_chunk_snap.wav") ?? correctClip;
            if (meaningFlipSFX == null)
                meaningFlipSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_dial_click.wav") ?? clickClip;
            if (certificateSFX == null)
                certificateSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_badge_unlock.wav") ?? celebrationClip;
            if (finaleSFX == null)
                finaleSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav") ?? celebrationClip;

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 10 Audio Manager] Auto-Assigned All Audio Clips, SFX & Mascot!</b></color>");
        }
#endif
    }
}
