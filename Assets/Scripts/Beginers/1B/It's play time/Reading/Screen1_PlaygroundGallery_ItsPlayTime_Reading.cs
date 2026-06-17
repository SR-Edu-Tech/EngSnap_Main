using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Beginners.ItsPlayTime.Reading
{
    [System.Serializable]
    public struct PlaygroundTileData
    {
        [Tooltip("The name of the playground item, e.g., 'playground'")]
        public string itemName;
        [Tooltip("The word used in the sentence frame, e.g., 'playground' or 'swing'")]
        public string frameWord;
        [Tooltip("The illustration showing children using this playground item")]
        public Sprite illustration;
        [Tooltip("Pronunciation audio clip for this item")]
        public AudioClip audioClip;
    }

    public class Screen1_PlaygroundGallery_ItsPlayTime_Reading : MonoBehaviour
    {
        [Header("── NAVIGATION ──")]
        public GameFlowManager_ItsPlayTime_Reading flowManager;

        [Header("── TILE CONFIGURATION ──")]
        public PlaygroundTileData[] tileData = new PlaygroundTileData[9];
        public PlaygroundTileView_ItsPlayTime_Reading[] tileViews = new PlaygroundTileView_ItsPlayTime_Reading[9];

        [Header("── SENTENCE FRAME UI ──")]
        public TMP_Text sentenceFrameText;
        public RectTransform sentenceCardRoot;
        public RectTransform slidingWordInstance;
        public TMP_Text slidingWordText;
        public RectTransform sentenceBlankTarget;

        [Header("── CANVAS ──")]
        [Tooltip("The Canvas this UI lives on — required for correct RectTransform position conversion")]
        public Canvas parentCanvas;

        [Header("── BUTTONS ──")]
        public Button btnNext;
        public Button btnReplay;

        [Header("── AUDIO ──")]
        public AudioSource sfxSource;
        public AudioSource voiceSource;
        [Space]
        public AudioClip voice_TapAnyOneToPlay;
        public AudioClip sfx_Pop;
        public AudioClip sfx_Tap;
        public AudioClip sfx_WordSlide;
        public AudioClip sfx_SentenceComplete;

        [Header("── SETTINGS ──")]
        public float pauseBetweenWords = 0.35f;
        public float slideDuration = 0.5f;
        [Tooltip("Number of UNIQUE tiles the student must tap before Next is revealed")]
        public int uniqueTapsRequired = 3;

        private HashSet<int> _tappedTiles  = new HashSet<int>();
        private bool         _autoplayActive = false;
        private Coroutine    _autoplayCoroutine;
        private CanvasGroup  _slidingWordCG;

        private void Awake()
        {
            Debug.Log("[Screen1] Awake()");
            if (btnNext   != null) btnNext.onClick.AddListener(OnNextPressed);
            if (btnReplay != null) btnReplay.onClick.AddListener(OnReplayPressed);

            if (slidingWordInstance != null)
            {
                _slidingWordCG = slidingWordInstance.GetComponent<CanvasGroup>();
                if (_slidingWordCG == null)
                    Debug.LogWarning("[Screen1] slidingWordInstance has no CanvasGroup — alpha fade skipped. Add CanvasGroup component to it.");
            }
            else
            {
                Debug.LogWarning("[Screen1] slidingWordInstance is NULL — word slide animation disabled.");
            }
        }

        private void Start()
        {
            Debug.Log("[Screen1] Start()");
            if (slidingWordInstance != null) slidingWordInstance.gameObject.SetActive(false);
            if (btnNext   != null) btnNext.gameObject.SetActive(false);
            if (btnReplay != null) btnReplay.gameObject.SetActive(false);
        }

        public void ResetAndStart()
        {
            Debug.Log("[Screen1] ResetAndStart() called.");

            // ── Null-check all critical references and report clearly ──
            if (sfxSource   == null) Debug.LogError("[Screen1] sfxSource is NULL — assign AudioSource in Inspector.");
            if (voiceSource == null) Debug.LogError("[Screen1] voiceSource is NULL — assign AudioSource in Inspector.");
            if (sentenceFrameText == null) Debug.LogWarning("[Screen1] sentenceFrameText is NULL.");
            if (btnNext     == null) Debug.LogWarning("[Screen1] btnNext is NULL.");
            if (btnReplay   == null) Debug.LogWarning("[Screen1] btnReplay is NULL.");

            int tileCount = Mathf.Min(tileViews.Length, tileData.Length);
            Debug.Log($"[Screen1] tileCount = {tileCount}  (tileViews={tileViews.Length}, tileData={tileData.Length})");

            if (tileViews.Length != tileData.Length)
                Debug.LogWarning($"[Screen1] Array size mismatch — tileViews={tileViews.Length} vs tileData={tileData.Length}. Some tiles will be skipped.");

            // Report any null tile views
            for (int i = 0; i < tileViews.Length; i++)
            {
                if (tileViews[i] == null)
                    Debug.LogWarning($"[Screen1] tileViews[{i}] is NULL — slot empty in Inspector.");
            }

            StopAllCoroutines();
            _autoplayActive = false;
            _tappedTiles.Clear();

            if (sentenceFrameText   != null) sentenceFrameText.text = "I like the ____";
            if (slidingWordInstance != null) slidingWordInstance.gameObject.SetActive(false);
            if (btnNext             != null) btnNext.gameObject.SetActive(false);
            if (btnReplay           != null) btnReplay.gameObject.SetActive(false);

            for (int i = 0; i < tileCount; i++)
            {
                if (tileViews[i] == null) continue;
                int index = i;
                tileViews[i].Setup(tileData[i], () => OnTileTapped(index));
                tileViews[i].SetInteractable(false);
            }

            Debug.Log("[Screen1] Starting AutoplaySequence coroutine.");
            _autoplayCoroutine = StartCoroutine(AutoplaySequence());
        }

        private IEnumerator AutoplaySequence()
        {
            Debug.Log("[Screen1] AutoplaySequence — BEGIN");
            _autoplayActive = true;

            yield return new WaitForSeconds(0.4f);

            int tileCount = Mathf.Min(tileViews.Length, tileData.Length);

            for (int i = 0; i < tileCount; i++)
            {
                if (tileViews[i] == null)
                {
                    Debug.LogWarning($"[Screen1] AutoplaySequence — tileViews[{i}] is null, skipping.");
                    continue;
                }

                PlaygroundTileView_ItsPlayTime_Reading view = tileViews[i];
                PlaygroundTileData data = tileData[i];

                Debug.Log($"[Screen1] AutoplaySequence — tile {i}: '{data.itemName}'  audioClip={(data.audioClip != null ? data.audioClip.name : "NULL")}");

                view.StartGlow();
                StartCoroutine(view.GentleBounce(0.4f));

                if (data.audioClip != null)
                {
                    PlayVoice(data.audioClip);
                    yield return new WaitForSeconds(data.audioClip.length + pauseBetweenWords);
                }
                else
                {
                    Debug.LogWarning($"[Screen1] Tile {i} ('{data.itemName}') has no audioClip — waiting 0.8s fallback.");
                    yield return new WaitForSeconds(0.8f);
                }

                view.StopGlow();
            }

            _autoplayActive = false;
            Debug.Log("[Screen1] AutoplaySequence — COMPLETE. Enabling tiles and replay.");

            if (btnReplay != null) btnReplay.gameObject.SetActive(true);

            for (int i = 0; i < tileCount; i++)
            {
                if (tileViews[i] != null)
                    tileViews[i].SetInteractable(true);
            }

            if (voice_TapAnyOneToPlay != null)
                PlayVoice(voice_TapAnyOneToPlay);
            else
                Debug.LogWarning("[Screen1] voice_TapAnyOneToPlay clip is NULL.");
        }

        private void OnTileTapped(int index)
        {
            Debug.Log($"[Screen1] OnTileTapped({index}) — autoplayActive={_autoplayActive}");
            if (_autoplayActive) return;

            PlaySFX(sfx_Tap);

            if (index >= tileData.Length)
            {
                Debug.LogWarning($"[Screen1] OnTileTapped index {index} out of range (tileData.Length={tileData.Length})");
                return;
            }

            PlaygroundTileData data = tileData[index];
            PlaygroundTileView_ItsPlayTime_Reading view = tileViews[index];

            if (view != null)
                StartCoroutine(view.GentleBounce(0.35f));

            if (data.audioClip != null)
                PlayVoice(data.audioClip);
            else
                Debug.LogWarning($"[Screen1] Tile {index} ('{data.itemName}') tapped but has no audioClip.");

            _tappedTiles.Add(index);
            Debug.Log($"[Screen1] Unique tiles tapped so far: {_tappedTiles.Count} / {uniqueTapsRequired} required.");

            if (slidingWordInstance != null && sentenceBlankTarget != null)
            {
                RectTransform tileRect = view != null ? view.GetComponent<RectTransform>() : null;
                StartCoroutine(SlideWordFlow(data.frameWord, tileRect));
            }
            else
            {
                Debug.LogWarning($"[Screen1] Slide skipped — slidingWordInstance={(slidingWordInstance != null ? "OK" : "NULL")}  sentenceBlankTarget={(sentenceBlankTarget != null ? "OK" : "NULL")}. Updating text directly.");
                if (sentenceFrameText != null)
                    sentenceFrameText.text = "I like the " + data.frameWord + "!";
                CheckNextButtonReveal();
            }
        }

        private IEnumerator SlideWordFlow(string word, RectTransform sourceTileRect)
        {
            Debug.Log($"[Screen1] SlideWordFlow — word='{word}'");

            if (slidingWordInstance == null || sentenceBlankTarget == null) yield break;

            slidingWordInstance.gameObject.SetActive(false);
            if (slidingWordText != null) slidingWordText.text = word;

            Vector2 startAnchoredPos = Vector2.zero;
            if (sourceTileRect != null && parentCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, sourceTileRect.position),
                    parentCanvas.worldCamera,
                    out startAnchoredPos
                );
            }
            else
            {
                if (parentCanvas == null)
                    Debug.LogWarning("[Screen1] SlideWordFlow — parentCanvas is NULL. Word will slide from origin. Assign parentCanvas in Inspector.");
            }

            Vector2 endAnchoredPos = Vector2.zero;
            if (parentCanvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, sentenceBlankTarget.position),
                    parentCanvas.worldCamera,
                    out endAnchoredPos
                );
            }

            slidingWordInstance.anchoredPosition = startAnchoredPos;
            slidingWordInstance.localScale = Vector3.one * 0.6f;
            if (_slidingWordCG != null) _slidingWordCG.alpha = 0f;

            slidingWordInstance.gameObject.SetActive(true);
            PlaySFX(sfx_WordSlide);

            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / slideDuration);
                float t = p * (2f - p);

                slidingWordInstance.anchoredPosition = Vector2.Lerp(startAnchoredPos, endAnchoredPos, t);
                slidingWordInstance.localScale        = Vector3.Lerp(Vector3.one * 0.6f, Vector3.one, t);
                if (_slidingWordCG != null) _slidingWordCG.alpha = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            slidingWordInstance.gameObject.SetActive(false);
            PlaySFX(sfx_SentenceComplete);

            if (sentenceFrameText != null)
                sentenceFrameText.text = "I like the " + word + "!";

            if (sentenceCardRoot != null)
                StartCoroutine(PunchScale(sentenceCardRoot, 0.25f));

            CheckNextButtonReveal();
        }

        private void CheckNextButtonReveal()
        {
            Debug.Log($"[Screen1] CheckNextButtonReveal — unique taps={_tappedTiles.Count}  required={uniqueTapsRequired}");
            if (_tappedTiles.Count >= uniqueTapsRequired && btnNext != null && !btnNext.gameObject.activeSelf)
            {
                Debug.Log("[Screen1] Revealing Next button!");
                btnNext.gameObject.SetActive(true);
                StartCoroutine(PopIn(btnNext.transform, 0.4f));
            }
        }

        private void OnReplayPressed()
        {
            Debug.Log("[Screen1] Replay pressed.");
            if (_autoplayActive) return;
            PlaySFX(sfx_Pop);
            ResetAndStart();
        }

        private void OnNextPressed()
        {
            Debug.Log("[Screen1] Next pressed — going to Screen 2.");
            PlaySFX(sfx_Pop);
            if (flowManager != null)
                flowManager.GoToScreen2();
            else
                Debug.LogError("[Screen1] flowManager is NULL — assign GameFlowManager in Inspector!");
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

        // ── TWEENS ─────────────────────────────────────────────────────────────

        private IEnumerator PunchScale(RectTransform rect, float duration)
        {
            Vector3 origin = rect.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float s = 1f + 0.15f * Mathf.Sin((elapsed / duration) * Mathf.PI);
                rect.localScale = origin * s;
                yield return null;
            }
            rect.localScale = origin;
        }

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