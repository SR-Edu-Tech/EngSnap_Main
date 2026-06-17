using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Beginners.ActItOut
{
    [System.Serializable]
    public class ActItOut_CharacterVisual
    {
        [Tooltip("Base sprite shown while the base verb is displayed")]
        public Sprite baseSprite;

        [Tooltip("Action sprite shown after + ing is tapped")]
        public Sprite ingSprite;
    }

    [System.Serializable]
    public class ActItOut_ActionRoundData
    {
        [Tooltip("Base form shown on the card, e.g. 'sing'")]
        public string baseVerb;

        [Tooltip("Conjugated form shown after the flip, e.g. 'singing'")]
        public string ingVerb;

        [Tooltip("Audio clip that plays when + ing is tapped, e.g. the word 'singing'")]
        public AudioClip wordAudio;

        [Tooltip("Sprites for this round's character")]
        public ActItOut_CharacterVisual characterVisual;
    }

    public class ActItOut_ActionGameController : MonoBehaviour
    {
        [Header("8 Rounds Configuration")]
        public ActItOut_ActionRoundData[] rounds = new ActItOut_ActionRoundData[8];

        [Header("UI References")]
        public RectTransform verbCardTransform;
        public TMP_Text      verbCardText;
        public Button        ingButton;
        public CanvasGroup   mainCanvasGroup;

        [Tooltip("The ONE shared Image used to show character sprites across all rounds")]
        public Image characterImage;

        [Header("Audio")]
        [Tooltip("Attach an AudioSource component on this GameObject and assign it here")]
        public AudioSource audioSource;
        public AudioClip   completionVO;

        [Header("Appearance")]
        public Color baseVerbColor = Color.white;
        public Color ingVerbColor  = new Color(1f, 0.41f, 0.7f, 1f);

        [SerializeField] private float cardFlipDuration = 0.35f;

        [HideInInspector] public System.Action OnFinished;

        private int     _currentRoundIndex   = 0;
        private bool    _isInteractionLocked = false;
        private Vector3 _originalCardRotation;

        void Awake()
        {
            if (verbCardTransform != null)
                _originalCardRotation = verbCardTransform.localEulerAngles;

            // Auto-grab AudioSource on this GameObject if not assigned
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        public void StartGame()
        {
            _currentRoundIndex   = 0;
            _isInteractionLocked = false;

            if (ingButton != null)
            {
                ingButton.onClick.RemoveListener(OnIngButtonClicked);
                ingButton.onClick.AddListener(OnIngButtonClicked);
                ingButton.interactable = true;
            }

            StartCoroutine(FadeIn(() => LoadRound(0)));
        }

        public void ResetPanel()
        {
            StopAllCoroutines();
            _currentRoundIndex   = 0;
            _isInteractionLocked = false;

            if (verbCardTransform != null)
                verbCardTransform.localEulerAngles = _originalCardRotation;

            if (verbCardText != null)
            {
                verbCardText.text  = string.Empty;
                verbCardText.color = baseVerbColor;
            }

            if (characterImage != null)
                characterImage.gameObject.SetActive(false);

            if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;
        }

        private void LoadRound(int index)
        {
            if (index >= rounds.Length) return;

            var round = rounds[index];

            if (characterImage != null)
            {
                characterImage.gameObject.SetActive(true);
                if (round.characterVisual != null && round.characterVisual.baseSprite != null)
                    characterImage.sprite = round.characterVisual.baseSprite;
            }

            if (verbCardTransform != null)
                verbCardTransform.localEulerAngles = _originalCardRotation;

            if (verbCardText != null)
            {
                verbCardText.text  = round.baseVerb;
                verbCardText.color = baseVerbColor;
            }

            if (ingButton != null)
                ingButton.interactable = true;

            _isInteractionLocked = false;
        }

        private void OnIngButtonClicked()
        {
            if (_isInteractionLocked) return;
            _isInteractionLocked = true;

            if (ingButton != null)
                ingButton.interactable = false;

            var round = rounds[_currentRoundIndex];
            PlayClip(round.wordAudio);

            StartCoroutine(FlipAndActivateSequence());
        }

        private IEnumerator FlipAndActivateSequence()
        {
            var round = rounds[_currentRoundIndex];

            // Flip 0 → 90°
            yield return StartCoroutine(RotateCardY(0f, 90f, cardFlipDuration));

            // Swap text and sprite at 90° (edge-on, invisible)
            if (verbCardText != null)
            {
                verbCardText.text  = round.ingVerb;
                verbCardText.color = ingVerbColor;
            }

            if (characterImage != null
                && round.characterVisual != null
                && round.characterVisual.ingSprite != null)
            {
                characterImage.sprite = round.characterVisual.ingSprite;
            }

            // Flip 90 → 180°
            yield return StartCoroutine(RotateCardY(90f, 180f, cardFlipDuration));

            // Pulse the ing verb text 3×
            yield return StartCoroutine(PulseTextThreeTimes());

            // Wait 1.5 s then move to next round or end
            yield return new WaitForSeconds(1.5f);

            if (_currentRoundIndex + 1 < rounds.Length)
            {
                yield return StartCoroutine(ResetCardRotationSequence());
                _currentRoundIndex++;
                LoadRound(_currentRoundIndex);
            }
            else
            {
                StartCoroutine(CompleteActionGame());
            }
        }

        private IEnumerator RotateCardY(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                verbCardTransform.localEulerAngles =
                    new Vector3(0f, Mathf.Lerp(from, to, elapsed / duration), 0f);
                yield return null;
            }
            verbCardTransform.localEulerAngles = new Vector3(0f, to, 0f);
        }

        private IEnumerator PulseTextThreeTimes()
        {
            if (verbCardText == null) yield break;
            Transform t    = verbCardText.transform;
            Vector3   orig = t.localScale;
            float     dur  = 0.12f;

            for (int i = 0; i < 3; i++)
            {
                float e = 0f;
                while (e < dur)
                {
                    e += Time.deltaTime;
                    t.localScale = orig * Mathf.Lerp(1f, 1.3f, e / dur);
                    yield return null;
                }
                t.localScale = orig * 1.3f;

                e = 0f;
                while (e < dur)
                {
                    e += Time.deltaTime;
                    t.localScale = orig * Mathf.Lerp(1.3f, 1f, e / dur);
                    yield return null;
                }
                t.localScale = orig;

                yield return new WaitForSeconds(0.08f);
            }
        }

        private IEnumerator ResetCardRotationSequence()
        {
            yield return StartCoroutine(RotateCardY(180f, 360f, 0.25f));
            verbCardTransform.localEulerAngles = _originalCardRotation;
            if (verbCardText != null) verbCardText.color = baseVerbColor;
        }

        private IEnumerator CompleteActionGame()
        {
            VFXManager.Instance?.SpawnConfetti();
            PlayClip(completionVO);

            float waitTime = completionVO != null ? completionVO.length + 0.2f : 2f;
            yield return new WaitForSeconds(waitTime);

            StartCoroutine(FadeOut(() =>
            {
                gameObject.SetActive(false);
                OnFinished?.Invoke();
            }));
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }

        private IEnumerator FadeIn(System.Action onDone)
        {
            if (mainCanvasGroup == null) { onDone?.Invoke(); yield break; }
            mainCanvasGroup.alpha = 0f;
            float t = 0f, dur = 0.4f;
            while (t < dur) { t += Time.deltaTime; mainCanvasGroup.alpha = t / dur; yield return null; }
            mainCanvasGroup.alpha = 1f;
            onDone?.Invoke();
        }

        private IEnumerator FadeOut(System.Action onDone)
        {
            if (mainCanvasGroup == null) { onDone?.Invoke(); yield break; }
            float t = 0f, dur = 0.3f;
            while (t < dur) { t += Time.deltaTime; mainCanvasGroup.alpha = 1f - t / dur; yield return null; }
            mainCanvasGroup.alpha = 0f;
            onDone?.Invoke();
        }
    }
}