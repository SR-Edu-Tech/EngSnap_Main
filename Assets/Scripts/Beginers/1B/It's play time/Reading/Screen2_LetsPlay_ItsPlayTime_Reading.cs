using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Beginners.ItsPlayTime.Reading
{
    [System.Serializable]
    public struct CommandData
    {
        [Tooltip("The command reading text, e.g., 'Climb on the jungle gym.'")]
        public string commandText;
        [Tooltip("The playground item illustration to show on the stage")]
        public Sprite itemSprite;
        [Tooltip("Audio clip pronouncing this command")]
        public AudioClip audioClip;
        // kidSprite removed — no character images used in Screen 2
    }

    /// <summary>
    /// SCREEN 2 — LET'S PLAY!
    /// Controls the vertical command list on the left and the illustration stage on the right.
    /// Character/mascot images and sprites fully removed per design decision.
    /// </summary>
    public class Screen2_LetsPlay_ItsPlayTime_Reading : MonoBehaviour
    {
        [Header("── NAVIGATION ──")]
        public GameFlowManager_ItsPlayTime_Reading flowManager;

        [Header("── COMMANDS CONFIGURATION ──")]
        [Tooltip("The command data entries (any count — completion auto-adapts)")]
        public CommandData[] commands = new CommandData[9];
        [Tooltip("The vertical command row views (must match commands array length)")]
        public CommandItemView_ItsPlayTime_Reading[] commandViews = new CommandItemView_ItsPlayTime_Reading[9];

        [Header("── STAGE ──")]
        [Tooltip("Canvas Group of the stage panel for fade transitions")]
        public CanvasGroup stageCanvasGroup;
        [Tooltip("The single Image component showing the playground item illustration")]
        public Image playgroundItemImage;
        // playKidImage removed — no character sprites

        [Header("── PLAY COUNTER UI ──")]
        [Tooltip("Counter text, e.g. 'Played: 0 / 9'")]
        public TMP_Text playCounterText;

        [Header("── BUTTONS ──")]
        public Button btnNext;
        public Button btnReplay;

        [Header("── AUDIO ──")]
        public AudioSource sfxSource;
        public AudioSource voiceSource;
        [Space]
        public AudioClip voice_LetsPlayIntro;
        public AudioClip voice_CoachingIntro;
        public AudioClip voice_AllDone;
        public AudioClip sfx_Pop;
        public AudioClip sfx_Tap;
        public AudioClip sfx_Fanfare;

        [Header("── SETTINGS ──")]
        public float pauseBetweenCommands = 0.35f;
        public float stageFadeDuration    = 0.2f;

        private HashSet<int> _playedCommands        = new HashSet<int>();
        private bool         _autoplayActive         = false;
        private bool         _voicePlaying           = false;
        private int          _activeHighlightedIndex = -1;

        private void Awake()
        {
            if (btnNext   != null) btnNext.onClick.AddListener(OnNextPressed);
            if (btnReplay != null) btnReplay.onClick.AddListener(OnReplayPressed);
        }

        private void Start()
        {
            if (btnNext   != null) btnNext.gameObject.SetActive(false);
            if (btnReplay != null) btnReplay.gameObject.SetActive(false);
        }

        public void ResetAndStart()
        {
            StopAllCoroutines();
            _autoplayActive = false;
            _voicePlaying   = false;

            // Clear all highlights cleanly
            int viewCount = Mathf.Min(commandViews.Length, commands.Length);
            for (int i = 0; i < viewCount; i++)
            {
                if (commandViews[i] != null)
                    commandViews[i].SetHighlight(false);
            }
            _activeHighlightedIndex = -1;

            _playedCommands.Clear();
            UpdateCounterText();

            if (btnNext   != null) btnNext.gameObject.SetActive(false);
            if (btnReplay != null) btnReplay.gameObject.SetActive(false);

            if (stageCanvasGroup != null) stageCanvasGroup.alpha = 0f;

            for (int i = 0; i < viewCount; i++)
            {
                if (commandViews[i] == null) continue;
                int index = i;
                commandViews[i].Setup(commands[i].commandText, () => OnCommandTapped(index));
                commandViews[i].SetInteractable(false);
                commandViews[i].SetHighlight(false);
            }

#if UNITY_EDITOR
            if (commandViews.Length != commands.Length)
                Debug.LogWarning($"[Screen2] commandViews.Length ({commandViews.Length}) != commands.Length ({commands.Length}).");
#endif

            StartCoroutine(AutoplaySequence());
        }

        private IEnumerator AutoplaySequence()
        {
            _autoplayActive = true;

            yield return new WaitForSeconds(0.4f);

            if (voice_LetsPlayIntro != null)
            {
                PlayVoice(voice_LetsPlayIntro);
                yield return new WaitForSeconds(voice_LetsPlayIntro.length + 0.2f);
            }

            int count = Mathf.Min(commandViews.Length, commands.Length);
            for (int i = 0; i < count; i++)
            {
                if (commandViews[i] == null) continue;

                _activeHighlightedIndex = i;
                commandViews[i].SetHighlight(true);

                yield return StartCoroutine(SwapStageSprite(commands[i].itemSprite));

                if (commands[i].audioClip != null)
                {
                    PlayVoice(commands[i].audioClip);
                    yield return new WaitForSeconds(commands[i].audioClip.length + pauseBetweenCommands);
                }
                else
                {
                    yield return new WaitForSeconds(1.2f);
                }

                commandViews[i].SetHighlight(false);
            }

            _activeHighlightedIndex = -1;
            _autoplayActive         = false;

            if (btnReplay != null) btnReplay.gameObject.SetActive(true);

            for (int i = 0; i < count; i++)
            {
                if (commandViews[i] != null)
                    commandViews[i].SetInteractable(true);
            }

            if (voice_CoachingIntro != null)
                PlayVoice(voice_CoachingIntro);
        }

        private void OnCommandTapped(int index)
        {
            if (_autoplayActive) return;
            if (_voicePlaying)   return;

            PlaySFX(sfx_Tap);

            if (index >= commands.Length) return;

            // Clear previous highlight
            if (_activeHighlightedIndex >= 0 && _activeHighlightedIndex < commandViews.Length)
            {
                if (commandViews[_activeHighlightedIndex] != null)
                    commandViews[_activeHighlightedIndex].SetHighlight(false);
            }

            _activeHighlightedIndex = index;
            if (commandViews[index] != null)
                commandViews[index].SetHighlight(true);

            StartCoroutine(SwapStageSprite(commands[index].itemSprite));

            if (commands[index].audioClip != null)
                StartCoroutine(PlayVoiceAndTrack(commands[index].audioClip));

            _playedCommands.Add(index);
            UpdateCounterText();

            if (_playedCommands.Count == commands.Length)
            {
                float clipLen = commands[index].audioClip != null ? commands[index].audioClip.length : 1f;
                StartCoroutine(CompleteCoachingFlow(clipLen));
            }
        }

        private IEnumerator PlayVoiceAndTrack(AudioClip clip)
        {
            if (clip == null || voiceSource == null) yield break;
            _voicePlaying = true;
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
            yield return new WaitForSeconds(clip.length);
            _voicePlaying = false;
        }

        /// <summary>
        /// Fades the stage out, swaps to the new item illustration, fades back in with a bounce.
        /// No character/kid sprite — illustration only.
        /// </summary>
        private IEnumerator SwapStageSprite(Sprite newItem)
        {
            if (stageCanvasGroup == null)
            {
                // No canvas group — just swap directly
                if (playgroundItemImage != null)
                {
                    playgroundItemImage.sprite = newItem;
                    playgroundItemImage.gameObject.SetActive(newItem != null);
                }
                yield break;
            }

            // Fade out
            float elapsed = 0f;
            while (elapsed < stageFadeDuration)
            {
                elapsed += Time.deltaTime;
                stageCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / stageFadeDuration);
                yield return null;
            }
            stageCanvasGroup.alpha = 0f;

            // Swap illustration
            if (playgroundItemImage != null)
            {
                playgroundItemImage.sprite = newItem;
                playgroundItemImage.gameObject.SetActive(newItem != null);
            }

            // Fade in
            elapsed = 0f;
            while (elapsed < stageFadeDuration)
            {
                elapsed += Time.deltaTime;
                stageCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / stageFadeDuration);
                yield return null;
            }
            stageCanvasGroup.alpha = 1f;

            // Pop bounce on stage
            Vector3 originalScale = stageCanvasGroup.transform.localScale;
            float   bounceDur     = 0.25f;
            elapsed = 0f;
            while (elapsed < bounceDur)
            {
                elapsed += Time.deltaTime;
                float s = 1f + 0.08f * Mathf.Sin((elapsed / bounceDur) * Mathf.PI);
                stageCanvasGroup.transform.localScale = originalScale * s;
                yield return null;
            }
            stageCanvasGroup.transform.localScale = originalScale;
        }

        private IEnumerator CompleteCoachingFlow(float delay)
        {
            int count = Mathf.Min(commandViews.Length, commands.Length);
            for (int i = 0; i < count; i++)
            {
                if (commandViews[i] != null)
                    commandViews[i].SetInteractable(false);
            }
            if (btnReplay != null) btnReplay.interactable = false;

            yield return new WaitForSeconds(delay + 0.3f);

            PlaySFX(sfx_Fanfare);

            if (voice_AllDone != null)
            {
                PlayVoice(voice_AllDone);
                yield return new WaitForSeconds(voice_AllDone.length + 0.5f);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            if (btnNext != null)
            {
                btnNext.gameObject.SetActive(true);
                StartCoroutine(PopIn(btnNext.transform, 0.4f));
            }
        }

        private void UpdateCounterText()
        {
            if (playCounterText != null)
                playCounterText.text = "Played: " + _playedCommands.Count + " / " + commands.Length;
        }

        private void OnReplayPressed()
        {
            if (_autoplayActive) return;
            PlaySFX(sfx_Pop);
            ResetAndStart();
        }

        private void OnNextPressed()
        {
            PlaySFX(sfx_Pop);
            if (flowManager != null)
                flowManager.GoToUnitPanel();
            else
                Debug.LogError("[Screen2] flowManager is NULL!");
        }

        // ── AUDIO ──────────────────────────────────────────────────────────────

        private void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
                sfxSource.PlayOneShot(clip);
        }

        private void PlayVoice(AudioClip clip)
        {
            if (clip == null || voiceSource == null) return;
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
        }

        // ── ANIMATION ─────────────────────────────────────────────────────────

        private IEnumerator PopIn(Transform t, float duration)
        {
            t.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / duration;
                float s = p < 0.7f
                    ? Mathf.Lerp(0f, 1.15f, p / 0.7f)
                    : Mathf.Lerp(1.15f, 1f, (p - 0.7f) / 0.3f);
                t.localScale = Vector3.one * s;
                yield return null;
            }
            t.localScale = Vector3.one;
        }
    }
}