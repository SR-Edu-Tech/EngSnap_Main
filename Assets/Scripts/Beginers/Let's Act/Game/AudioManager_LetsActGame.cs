using System.Collections;
using UnityEngine;

/// <summary>
/// AudioManager — singleton.
/// Assign AudioClips in the Inspector.
/// Attach this to a persistent GameObject (same as GameManager or its own).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    public AudioClip bgMusicMatching;
    public AudioClip bgMusicSimon;

    [Header("UI / Feedback SFX")]
    public AudioClip sfxButtonTap;          // soft "pop" on any button press
    public AudioClip sfxButtonHover;        // subtle whoosh on hover (optional)
    public AudioClip sfxCorrect;            // sparkle / chime
    public AudioClip sfxWrong;              // soft thud / cartoon boing
    public AudioClip sfxLineDraw;           // pencil scratch (matching panel)
    public AudioClip sfxLineSnap;           // snap/connect sound on successful match
    public AudioClip sfxRoundComplete;      // fanfare / jingle
    public AudioClip sfxGameComplete;       // big celebration fanfare
    public AudioClip sfxTimerTick;          // tick for speed round
    public AudioClip sfxTimerEnd;           // buzzer when timer runs out
    public AudioClip sfxCardFlip;           // card reveal in Simon Says
    public AudioClip sfxNextPanel;          // whoosh scene transition

    [Header("VO Clips — Simon Says Commands")]
    // Name these exactly: vo_jump, vo_walk, vo_sit, vo_stand, vo_play, vo_paste etc.
    public AudioClip[] voCommands;          // indexed by SimonSaysController.rounds[i].voIndex

    [Header("Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voSource;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-create sources if not assigned
        if (musicSource == null) musicSource = CreateSource("MusicSource", true, 0.45f);
        if (sfxSource   == null) sfxSource   = CreateSource("SFXSource",   false, 1f);
        if (voSource    == null) voSource     = CreateSource("VOSource",    false, 1f);
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public void PlayMusic(AudioClip clip, float fadeTime = 0.5f)
    {
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        StartCoroutine(CrossfadeMusic(clip, fadeTime));
    }

    public void PlaySFX(AudioClip clip, float pitchVariance = 0.05f)
    {
        if (clip == null) return;
        sfxSource.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        sfxSource.PlayOneShot(clip);
    }

    public void PlayVO(AudioClip clip)
    {
        if (clip == null) return;
        voSource.Stop();
        voSource.clip = clip;
        voSource.Play();
    }

    public void PlayVO(int index)
    {
        if (voCommands == null || index < 0 || index >= voCommands.Length) return;
        PlayVO(voCommands[index]);
    }

    public void StopVO() => voSource.Stop();

    // ── Helpers ─────────────────────────────────────────────────────────────

    private AudioSource CreateSource(string goName, bool loop, float volume)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop = loop;
        src.volume = volume;
        src.playOnAwake = false;
        return src;
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip, float duration)
    {
        float startVol = musicSource.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();
        t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, startVol, t / duration);
            yield return null;
        }
        musicSource.volume = startVol;
    }
}
