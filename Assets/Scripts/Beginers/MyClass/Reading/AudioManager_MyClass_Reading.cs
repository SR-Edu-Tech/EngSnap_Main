using UnityEngine;

/// <summary>
/// Lightweight AudioManager singleton.
/// Place one instance in the scene (or use a persistent prefab).
/// Handles: word/phrase VO, UI SFX, background music.
/// </summary>
public class AudioManager_MyClass_Reading : MonoBehaviour
{
    public static AudioManager_MyClass_Reading Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] private AudioSource voiceSource;   // word / phrase playback
    [SerializeField] private AudioSource sfxSource;     // UI sound effects
    [SerializeField] private AudioSource musicSource;   // looping BG music

    [Header("SFX Clips")]
    public AudioClip cardPopSFX;        // card entrance pop
    public AudioClip cardTapSFX;        // card tapped
    public AudioClip cardGlowSFX;       // glow start pulse
    public AudioClip buttonTapSFX;      // any button pressed
    public AudioClip nextGroupSFX;      // Next Group button
    public AudioClip nextScreenSFX;     // Next / proceed button
    public AudioClip successChimeSFX;   // all cards done
    public AudioClip screenEntrySFX;    // screen load whoosh

    // ── Voice ─────────────────────────────────────────────────────────────

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;
        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    public void StopVoice() => voiceSource.Stop();

    public bool IsVoicePlaying => voiceSource.isPlaying;

    public float VoiceClipLength => voiceSource.clip != null ? voiceSource.clip.length : 0f;

    // ── SFX ───────────────────────────────────────────────────────────────

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void PlayCardPop()      => PlaySFX(cardPopSFX);
    public void PlayCardTap()      => PlaySFX(cardTapSFX);
    public void PlayCardGlow()     => PlaySFX(cardGlowSFX);
    public void PlayButtonTap()    => PlaySFX(buttonTapSFX);
    public void PlayNextGroup()    => PlaySFX(nextGroupSFX);
    public void PlayNextScreen()   => PlaySFX(nextScreenSFX);
    public void PlaySuccessChime() => PlaySFX(successChimeSFX);
    public void PlayScreenEntry()  => PlaySFX(screenEntrySFX);

    // ── Music ─────────────────────────────────────────────────────────────

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        musicSource.clip  = clip;
        musicSource.loop  = loop;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();

    // ── Unity ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
