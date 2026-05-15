using System.Collections;
using UnityEngine;

/// <summary>
/// AudioManager_MagicWords_Reading
/// Centralised audio controller for the Magic Words unit.
///
/// Features:
///   • Separate AudioSources for music (looped BG) and SFX (one-shot)
///   • Voiceover queue – only one voiceover plays at a time
///   • Volume control for each channel
///   • Background music duck (auto-lower BG music while voiceover plays)
///
/// Attach to a persistent GameObject (can be on GameManager).
/// </summary>
public class AudioManager_MagicWords_Reading : MonoBehaviour
{
    public static AudioManager_MagicWords_Reading Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Background Music")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)]
    public float musicVolume = 0.35f;
    [Range(0f, 1f)]
    [Tooltip("Duck BG music to this level while voiceover is playing")]
    public float musicDuckVolume = 0.08f;
    [Range(0.1f, 2f)]
    public float duckFadeTime = 0.4f;

    [Header("SFX Volume")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    [Header("Voiceover Volume")]
    [Range(0f, 1f)]
    public float voiceVolume = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────────────────────────────────

    private AudioSource _musicSource;
    private AudioSource _sfxSource;
    private AudioSource _voiceSource;

    private Coroutine _duckCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create AudioSources
        _musicSource  = CreateSource("Music",  true,  musicVolume);
        _sfxSource    = CreateSource("SFX",    false, sfxVolume);
        _voiceSource  = CreateSource("Voice",  false, voiceVolume);
    }

    void Start()
    {
        if (backgroundMusic != null)
        {
            _musicSource.clip = backgroundMusic;
            _musicSource.Play();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Play a one-shot sound effect.</summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>Play voiceover (cancels any currently playing voiceover).
    ///          Ducks background music for duration.</summary>
    public void PlayVoiceover(AudioClip clip)
    {
        if (clip == null) return;

        _voiceSource.Stop();
        _voiceSource.clip   = clip;
        _voiceSource.volume = voiceVolume;
        _voiceSource.Play();

        // Duck music
        if (_duckCoroutine != null) StopCoroutine(_duckCoroutine);
        _duckCoroutine = StartCoroutine(DuckMusic(clip.length));
    }

    /// <summary>Stop any currently playing voiceover immediately.</summary>
    public void StopVoiceover() => _voiceSource.Stop();

    /// <summary>Returns true while a voiceover clip is playing.</summary>
    public bool IsVoiceoverPlaying() => _voiceSource.isPlaying;

    // ─────────────────────────────────────────────────────────────────────────
    //  Internal
    // ─────────────────────────────────────────────────────────────────────────

    private AudioSource CreateSource(string label, bool loop, float volume)
    {
        var go  = new GameObject($"AudioSource_{label}_MagicWords");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.loop       = loop;
        src.volume     = volume;
        src.playOnAwake = false;
        return src;
    }

    private IEnumerator DuckMusic(float voiceDuration)
    {
        // Fade down
        float elapsed = 0f;
        float startVol = _musicSource.volume;
        while (elapsed < duckFadeTime)
        {
            elapsed += Time.deltaTime;
            _musicSource.volume = Mathf.Lerp(startVol, musicDuckVolume,
                elapsed / duckFadeTime);
            yield return null;
        }
        _musicSource.volume = musicDuckVolume;

        // Wait for voice to finish + small buffer
        yield return new WaitForSeconds(Mathf.Max(0f, voiceDuration - duckFadeTime * 2f + 0.2f));

        // Fade back up
        elapsed  = 0f;
        startVol = _musicSource.volume;
        while (elapsed < duckFadeTime)
        {
            elapsed += Time.deltaTime;
            _musicSource.volume = Mathf.Lerp(startVol, musicVolume,
                elapsed / duckFadeTime);
            yield return null;
        }
        _musicSource.volume = musicVolume;
    }
}
