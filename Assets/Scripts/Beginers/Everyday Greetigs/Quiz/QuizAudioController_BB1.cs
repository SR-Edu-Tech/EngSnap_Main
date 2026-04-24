using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Centralised audio controller for the Quiz screen.
///
/// WIRING (Inspector):
///   bgmSource      → AudioSource set to loop, lower volume (background music)
///   voSource       → AudioSource for VO / question audio (not looping)
///   fxSource       → AudioSource for short UI FX (correct / wrong stings)
///
/// Usage:
///   PlayBGM(clip)               — starts looping BGM
///   StopBGM()                   — fades out and stops BGM
///   PlayVO(clip, onDone)        — plays VO, calls onDone when finished
///   PlayVOThenUnlock(clip, secondaryClip, onDone) — chains two clips
///   PlayFX(clip)                — fire-and-forget FX
///   StopAll()                   — stops everything immediately
/// </summary>
public class QuizAudioController_BB1 : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource voSource;
    public AudioSource fxSource;

    [Header("BGM")]
    [Range(0f, 1f)] public float bgmVolume  = 0.35f;
    [Range(0f, 1f)] public float voVolume   = 1.0f;
    public float bgmFadeDuration = 0.8f;

    private Coroutine voCoroutine   = null;
    private Coroutine bgmCoroutine  = null;

    // ── BGM ───────────────────────────────────────────────────────────────

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null) return;
        if (bgmCoroutine != null) StopCoroutine(bgmCoroutine);

        bgmSource.clip   = clip;
        bgmSource.loop   = true;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource == null) return;
        if (bgmCoroutine != null) StopCoroutine(bgmCoroutine);
        bgmCoroutine = StartCoroutine(FadeOut(bgmSource, bgmFadeDuration));
    }

    // ── VO ────────────────────────────────────────────────────────────────

    /// <summary>Plays a single VO clip; calls onDone (may be null) when it finishes.</summary>
    public void PlayVO(AudioClip clip, Action onDone = null)
    {
        if (voCoroutine != null) StopCoroutine(voCoroutine);
        voCoroutine = StartCoroutine(PlayVORoutine(clip, null, onDone));
    }

    /// <summary>Plays primaryClip, then optionally secondaryClip, then calls onDone.</summary>
    public void PlayVOChained(AudioClip primary, AudioClip secondary, Action onDone = null)
    {
        if (voCoroutine != null) StopCoroutine(voCoroutine);
        voCoroutine = StartCoroutine(PlayVORoutine(primary, secondary, onDone));
    }

    /// <summary>Stops any playing VO immediately.</summary>
    public void StopVO()
    {
        if (voCoroutine != null) { StopCoroutine(voCoroutine); voCoroutine = null; }
        if (voSource != null) voSource.Stop();
    }

    // ── FX ────────────────────────────────────────────────────────────────

    public void PlayFX(AudioClip clip)
    {
        if (clip == null || fxSource == null) return;
        fxSource.PlayOneShot(clip);
    }

    // ── Stop All ─────────────────────────────────────────────────────────

    public void StopAll()
    {
        StopAllCoroutines();
        if (bgmSource != null) bgmSource.Stop();
        if (voSource  != null) voSource.Stop();
        if (fxSource  != null) fxSource.Stop();
    }

    // ── Coroutines ────────────────────────────────────────────────────────

    private IEnumerator PlayVORoutine(AudioClip primary, AudioClip secondary, Action onDone)
    {
        if (voSource == null) { onDone?.Invoke(); yield break; }

        voSource.volume = voVolume;

        // Play primary
        if (primary != null)
        {
            voSource.clip = primary;
            voSource.Play();
            yield return new WaitUntil(() => voSource.isPlaying);
            yield return new WaitWhile(() => voSource.isPlaying);
        }

        // Small gap between chained clips
        if (secondary != null)
        {
            yield return new WaitForSeconds(0.3f);
            voSource.clip = secondary;
            voSource.Play();
            yield return new WaitUntil(() => voSource.isPlaying);
            yield return new WaitWhile(() => voSource.isPlaying);
        }

        voCoroutine = null;
        onDone?.Invoke();
    }

    private IEnumerator FadeOut(AudioSource source, float duration)
    {
        float start = source.volume;
        float t     = 0f;
        while (t < duration)
        {
            t              += Time.deltaTime;
            source.volume   = Mathf.Lerp(start, 0f, t / duration);
            yield return null;
        }
        source.Stop();
        source.volume = start; // restore for next use
    }
}
