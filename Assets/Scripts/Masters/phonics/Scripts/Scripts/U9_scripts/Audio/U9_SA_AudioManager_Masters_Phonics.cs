using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MastersPhonics
{
    public class U9_SA_AudioManager_Masters_Phonics : MonoBehaviour
    {
        public static U9_SA_AudioManager_Masters_Phonics Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceASource;  // Mascot / Narration
        [SerializeField] private AudioSource voiceBSource;  // Word / Vowel Model
        [SerializeField] private AudioSource sfxSource;     // Sound effects

        [Header("Unit 9 Voice A Narration Clips (Mascot)")]
        public AudioClip unitIntroClip;             // "Unit Nine The last syllable type and the.mp3"
        public AudioClip card1Clip;                 // "Consonant plus le at the end of a.mp3"
        public AudioClip card1bClip;                // "Although say turtle slowly Turtul Theres a tiny.mp3"
        public AudioClip card2Clip;                 // "Heres the rule If a word ends in.mp3"
        public AudioClip card3Clip;                 // "Count the consonants sitting just before the le.mp3"
        public AudioClip act1IntroClip;             // "Tap the gap where the word splits Count.mp3"
        public AudioClip act1PickleClip;            // "Careful with this one The c and k.mp3"
        public AudioClip act1SprinkleClip;          // "Sprinkle Start at the end and count back.mp3"
        public AudioClip act1LongerClip;            // "Longer ones Two splits this time.mp3"
        public AudioClip act2OneConsonantClip;      // "One consonant The syllable ends in a vowel.mp3"
        public AudioClip act2TwoConsonantsClip;     // "Two consonants Door shut vowel short Same as.mp3"
        public AudioClip act3IntroClip;             // "Pick the ending then decide whether the last.mp3"
        public AudioClip act3Round2Clip;            // "Notice anything Every base in this round already.mp3"
        public AudioClip act4IntroClip;             // "Now find them in real sentences Listen then.mp3"
        public AudioClip act4MultiClip;             // "More than one hiding in these Find them.mp3"
        public AudioClip challengeIntroClip;        // "Ten questions The last ones a word youve.mp3"
        public AudioClip mapIntroClip;              // "And thats all seven Closed open magic e.mp3"
        public AudioClip mapSummaryClip;            // "And heres something the book doesnt tell you.mp3"
        public AudioClip unitCompleteClip;          // "Unit Nine done Thirtytwo words split Badge unlocked.mp3"

        [Header("Global SFX (Reused)")]
        public AudioClip correctClip;
        public AudioClip wrongClip;
        public AudioClip celebrationClip;
        public AudioClip clickClip;

        [Header("Unit 9 Specific SFX")]
        [Tooltip("U09_SFX_countback: Count-back-three ticks (from Unit 3)")]
        public AudioClip countbackTickClip;
        [Tooltip("U09_SFX_split_click: Syllable divider drops (from Unit 3)")]
        public AudioClip splitClickClip;
        [Tooltip("U09_SFX_door_slam: Closed door slam for short vowel (from Unit 5)")]
        public AudioClip doorSlamClip;
        [Tooltip("U09_SFX_gate_open: Open gate swing for long vowel (from Unit 5)")]
        public AudioClip gateOpenClip;
        [Tooltip("U09_SFX_dial_click: Doubling switch toggle click (from Unit 2)")]
        public AudioClip dialClickClip;
        [Tooltip("U09_SFX_word_lift: Soft upward rise when word lifts out of sentence")]
        public AudioClip wordLiftClip;
        [Tooltip("U04_SFX_map_complete: Grand map completion fanfare")]
        public AudioClip mapCompleteFanfareClip;

        [Header("Mascot Object")]
        [SerializeField] private GameObject mascotObject;

        // In-memory cache for audio clips
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
            CacheAllU9Audio();
        }

        private void EnsureDefaultSFX()
        {
#if UNITY_EDITOR
            if (correctClip == null) correctClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? ResolveAudio("Correct");
            if (wrongClip == null) wrongClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? ResolveAudio("Incorrect");
            if (clickClip == null) clickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/AB_Pop_boosted_220.mp3");
            if (celebrationClip == null) celebrationClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav");
            if (countbackTickClip == null) countbackTickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U03_SFX_countback.wav") ?? clickClip;
            if (splitClickClip == null) splitClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U03_SFX_split_click.wav") ?? correctClip;
            if (doorSlamClip == null) doorSlamClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_door_slam.wav") ?? wrongClip;
            if (gateOpenClip == null) gateOpenClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_gate_open.wav") ?? correctClip;
            if (dialClickClip == null) dialClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_dial_click.wav") ?? clickClip;
            if (wordLiftClip == null) wordLiftClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_stretch.wav") ?? clickClip;
            if (mapCompleteFanfareClip == null) mapCompleteFanfareClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav") ?? celebrationClip;
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
                // 1. Search in Canvas hierarchy (including inactive)
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

                // 2. Search root / siblings
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

        public void CacheAllU9Audio()
        {
#if UNITY_EDITOR
            List<string> validPaths = new List<string> { "Assets/Audio/U9_audio", "Assets/SFX" };

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

            // Direct field bindings for Voice A (ONLY when explicit voice key is requested)
            if (target.StartsWith("u09_vo_") || target.StartsWith("vo_"))
            {
                if (target.Contains("unit_intro") && unitIntroClip != null) return unitIntroClip;
                if (target.Contains("card1b") && card1bClip != null) return card1bClip;
                if (target.Contains("card1") && card1Clip != null) return card1Clip;
                if (target.Contains("card2") && card2Clip != null) return card2Clip;
                if (target.Contains("card3") && card3Clip != null) return card3Clip;
                if (target.Contains("a1_intro") && act1IntroClip != null) return act1IntroClip;
                if ((target.Contains("pick_le") || target.Contains("pickle")) && act1PickleClip != null) return act1PickleClip;
                if ((target.Contains("sprin_kle") || target.Contains("sprinkle")) && act1SprinkleClip != null) return act1SprinkleClip;
                if (target.Contains("longer") && act1LongerClip != null) return act1LongerClip;
                if (target.Contains("open") && act2OneConsonantClip != null) return act2OneConsonantClip;
                if (target.Contains("closed") && act2TwoConsonantsClip != null) return act2TwoConsonantsClip;
                if (target.Contains("a3_intro") && act3IntroClip != null) return act3IntroClip;
                if (target.Contains("round2") && act3Round2Clip != null) return act3Round2Clip;
                if (target.Contains("a4_intro") && act4IntroClip != null) return act4IntroClip;
                if (target.Contains("multi") && act4MultiClip != null) return act4MultiClip;
                if (target.Contains("challenge") && challengeIntroClip != null) return challengeIntroClip;
                if (target.Contains("map_intro") && mapIntroClip != null) return mapIntroClip;
                if (target.Contains("map_summary") && mapSummaryClip != null) return mapSummaryClip;
                if (target.Contains("complete") && unitCompleteClip != null) return unitCompleteClip;
            }

            // Word and Sentence Aliases
            Dictionary<string, string> textAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Verb inflections & plurals -> Base word audio
                { "giggled", "giggle" },
                { "gig gled", "gig gle" },
                { "gig-gled", "gig gle" },
                { "bubbles", "bubble" },
                { "bub bles", "bub ble" },
                { "circled", "circle" },
                { "cir cled", "cir cle" },
                { "stumbles", "stumbles" },
                { "stum bles", "stum bles" },
                { "staple", "staple" },
                { "sta ple", "sta ple" },
                { "maple", "maple" },
                { "ma ple", "ma ple" },

                // Sentence IDs
                { "u09_sent_01", "I play the bugle." },
                { "u09_sent_02", "Eva likes to play with the bubbles." },
                { "u09_sent_03", "My favourite colour is purple." },
                { "u09_sent_04", "The maple tree is very large." },
                { "u09_sent_05", "The boy giggled at the joke." },
                { "u09_sent_06", "We have a pet turtle at home." },
                { "u09_sent_07", "I saw the castle on the hill." },
                { "u09_sent_08", "I will eat the waffle for breakfast." },
                { "u09_sent_09", "She likes to cuddle the puppy." },
                { "u09_sent_10", "We will eat dinner on the table." },
                { "u09_sent_b1", "The little turtle stumbles into the puddle." },
                { "u09_sent_b2", "I can juggle an apple and a bottle." },
                { "u09_sent_b3", "The eagle circled above the castle." },
                { "u09_sent_q7", "The gentle eagle landed." }
            };

            if (textAliases.TryGetValue(key, out string mappedVal))
            {
                key = mappedVal;
            }

            // 1. Direct dictionary cache lookup
            if (audioCache.TryGetValue(key, out AudioClip cached))
                return cached;

            // 2. Cleaned key lookup (lowercase letters & digits)
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

            // 4. Safe prefix / word match (Only match if filename starts with or equals the clean key)
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

        public void PlaySyllables(string word, System.Action onComplete = null)
        {
            // Syllable separated audio is formatted as "ta ble" or "tur tle"
            string clean = word.Replace("|", " ").Replace("-", " ").Replace(",", "").ToLower().Trim();
            while (clean.Contains("  ")) clean = clean.Replace("  ", " ");

            AudioClip clip = ResolveAudio(clean) ?? ResolveAudio(word);
            if (clip != null)
            {
                PlayVoiceB(clip, onComplete);
            }
            else
            {
                PlayWord(word, onComplete);
            }
        }

        public void PlayEndingChunk(string ending, System.Action onComplete = null)
        {
            PlayVoiceB(ending.ToLower().Trim(), onComplete);
        }

        public void PlayBaseChunk(string baseChunk, System.Action onComplete = null)
        {
            PlayVoiceB(baseChunk.ToLower().Trim(), onComplete);
        }

        public void PlaySentence(string sentenceIdOrText, System.Action onComplete = null)
        {
            PlayVoiceB(sentenceIdOrText, onComplete);
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
        public void PlayCountbackTick() => PlaySFX(countbackTickClip ?? clickClip);
        public void PlaySplitClick() => PlaySFX(splitClickClip ?? correctClip);
        public void PlayDoorSlam() => PlaySFX(doorSlamClip ?? wrongClip);
        public void PlayGateOpen() => PlaySFX(gateOpenClip ?? correctClip);
        public void PlayDialClick() => PlaySFX(dialClickClip ?? clickClip);
        public void PlayWordLift() => PlaySFX(wordLiftClip ?? clickClip);
        public void PlayMapCompleteFanfare() => PlaySFX(mapCompleteFanfareClip ?? celebrationClip);

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
            CacheAllU9Audio();

            string vAPath = "Assets/Audio/U9_audio/u9_MP_voiceA";
            unitIntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Unit Nine The last syllable type and the.mp3") ?? unitIntroClip;
            card1Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Consonant plus le at the end of a.mp3") ?? card1Clip;
            card1bClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Although say turtle slowly Turtul Theres a tiny.mp3") ?? card1bClip;
            card2Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Heres the rule If a word ends in.mp3") ?? card2Clip;
            card3Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Count the consonants sitting just before the le.mp3") ?? card3Clip;
            act1IntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Tap the gap where the word splits Count.mp3") ?? act1IntroClip;
            act1PickleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Careful with this one The c and k.mp3") ?? act1PickleClip;
            act1SprinkleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Sprinkle Start at the end and count back.mp3") ?? act1SprinkleClip;
            act1LongerClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Longer ones Two splits this time.mp3") ?? act1LongerClip;
            act2OneConsonantClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/One consonant The syllable ends in a vowel.mp3") ?? act2OneConsonantClip;
            act2TwoConsonantsClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Two consonants Door shut vowel short Same as.mp3") ?? act2TwoConsonantsClip;
            act3IntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Pick the ending then decide whether the last.mp3") ?? act3IntroClip;
            act3Round2Clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Notice anything Every base in this round already.mp3") ?? act3Round2Clip;
            act4IntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Now find them in real sentences Listen then.mp3") ?? act4IntroClip;
            act4MultiClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/More than one hiding in these Find them.mp3") ?? act4MultiClip;
            challengeIntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Ten questions The last ones a word youve.mp3") ?? challengeIntroClip;
            mapIntroClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/And thats all seven Closed open magic e.mp3") ?? mapIntroClip;
            mapSummaryClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/And heres something the book doesnt tell you.mp3") ?? mapSummaryClip;
            unitCompleteClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Unit Nine done Thirtytwo words split Badge unlocked.mp3") ?? unitCompleteClip;

            correctClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") 
                       ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Correct Answer 1.mp3");
            wrongClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3");
            clickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/AB_Pop_boosted_220.mp3");
            celebrationClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav");
            countbackTickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_beat_strong.wav");
            splitClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_chunk_snap.wav");
            doorSlamClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_door_slam.wav");
            gateOpenClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_gate_open.wav");
            dialClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_dial_click.wav");
            wordLiftClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_stretch.wav");
            mapCompleteFanfareClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav");

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 9 Audio Manager] Auto-Assigned All Audio Clips, SFX & Mascot!</b></color>");
        }
#endif
    }
}
