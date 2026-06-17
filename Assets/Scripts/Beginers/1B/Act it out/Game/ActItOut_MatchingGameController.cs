using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Beginners.ActItOut
{
    [System.Serializable]
    public class ActItOut_MatchingPair
    {
        [Tooltip("The 'I am ___' sentence, e.g. 'I am eating.'")]
        public string sentence;

        [Tooltip("The corresponding action illustration card sprite")]
        public Sprite illustrationSprite;
    }

    public class ActItOut_MatchingGameController : MonoBehaviour
    {
        [Header("Matching Pairs (Configure 7 pairs)")]
        public ActItOut_MatchingPair[] pairs = new ActItOut_MatchingPair[7];

        [Header("Prefabs")]
        public ActItOut_WordLabel        wordLabelPrefab;
        public ActItOut_IllustrationCard illustrationCardPrefab;

        [Header("Layout Columns")]
        public Transform wordColumn;
        public Transform cardColumn;

        [Header("Wiring")]
        public ActItOut_LineDrawer lineDrawer;
        public CanvasGroup         mainCanvasGroup;

        [Header("Next Navigation Button")]
        public Button nextButton;

        [Header("Audio")]
        [Tooltip("Attach an AudioSource component on this GameObject and assign it here")]
        public AudioSource audioSource;
        public AudioClip   correctSFX;
        public AudioClip   wrongSFX;
        public AudioClip   roundCompleteSFX;

        [HideInInspector] public System.Action OnFinished;

        private int _matchesCompleted = 0;
        private ActItOut_WordLabel _draggingWord;

        private readonly List<ActItOut_WordLabel>        _wordLabels = new();
        private readonly List<ActItOut_IllustrationCard> _cards      = new();

        void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        public void StartGame()
        {
            Debug.Log("[ActItOut_MatchingGameController] StartGame called");

            if (lineDrawer == null)
            {
                lineDrawer = FindObjectOfType<ActItOut_LineDrawer>();
                if (lineDrawer == null)
                    Debug.LogError("[ActItOut_MatchingGameController] ActItOut_LineDrawer not found!");
            }

            _matchesCompleted = 0;
            _draggingWord     = null;

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
                nextButton.onClick.RemoveListener(OnNextButtonPressed);
                nextButton.onClick.AddListener(OnNextButtonPressed);
            }

            ClearSpawnedItems();
            lineDrawer?.ClearAll();

            StartCoroutine(FadeIn(SpawnMatchingBoard));
        }

        public void RestartGame()
        {
            StopAllCoroutines();
            ClearSpawnedItems();
            lineDrawer?.ClearAll();

            _matchesCompleted = 0;
            _draggingWord     = null;

            if (nextButton != null)
                nextButton.gameObject.SetActive(false);

            if (mainCanvasGroup != null) mainCanvasGroup.alpha = 1f;

            SpawnMatchingBoard();
        }

        private void SpawnMatchingBoard()
        {
            ClearSpawnedItems();
            lineDrawer?.ClearAll();

            int count = pairs.Length;
            if (count == 0)
            {
                Debug.LogWarning("[ActItOut_MatchingGameController] No pairs assigned!");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var lbl = Instantiate(wordLabelPrefab, wordColumn);
                lbl.Initialise(i, pairs[i].sentence, null, OnDragBegin, OnDragging, OnDragEnd);
                _wordLabels.Add(lbl);
            }

            var shuffledIndices = ShuffleIndices(count);
            foreach (int idx in shuffledIndices)
            {
                var card = Instantiate(illustrationCardPrefab, cardColumn);
                card.Initialise(idx, pairs[idx].illustrationSprite, OnCardDropped);
                _cards.Add(card);
                StartCoroutine(CardEntrance(card.GetComponent<RectTransform>(), _cards.Count * 0.05f));
            }
        }

        private void OnDragBegin(ActItOut_WordLabel word)
        {
            _draggingWord = word;
            lineDrawer?.BeginDragLine(word.GetComponent<RectTransform>());
        }

        private void OnDragging(ActItOut_WordLabel word, Vector2 screenPos) =>
            lineDrawer?.UpdateDragLine(screenPos);

        private void OnDragEnd(ActItOut_WordLabel word, PointerEventData eventData)
        {
            lineDrawer?.EndDragLine();
            _draggingWord = null;
        }

        private void OnCardDropped(ActItOut_IllustrationCard card)
        {
            if (_draggingWord == null)   return;
            if (card.IsMatched)          return;
            if (_draggingWord.IsMatched) return;
            CheckMatch(_draggingWord, card);
        }

        private void CheckMatch(ActItOut_WordLabel word, ActItOut_IllustrationCard card)
        {
            bool correct = word.PairIndex == card.CorrectPairIndex;
            lineDrawer?.CommitLine(
                word.GetComponent<RectTransform>(),
                card.GetComponent<RectTransform>(),
                correct);

            if (correct)
            {
                word.SetMatched();
                card.SetMatched();
                PlayClip(correctSFX);
                _matchesCompleted++;
                if (_matchesCompleted >= pairs.Length)
                    RoundComplete();
            }
            else
            {
                word.SetWrong();
                card.SetWrong();
                PlayClip(wrongSFX);
                VFXManager.Instance?.ScreenShake(8f, 0.2f);
            }
        }

        private void RoundComplete()
        {
            PlayClip(roundCompleteSFX);
            VFXManager.Instance?.SpawnConfetti();

            if (nextButton != null)
                nextButton.gameObject.SetActive(true);
            else
                StartCoroutine(TransitionToScreen2Sequence());
        }

        public void OnNextButtonPressed()
        {
            PlayClip(roundCompleteSFX);
            if (nextButton != null) nextButton.gameObject.SetActive(false);
            StartCoroutine(TransitionToScreen2Sequence());
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }

        private IEnumerator TransitionToScreen2Sequence()
        {
            yield return StartCoroutine(FadeOut(null));
            gameObject.SetActive(false);
            OnFinished?.Invoke();
        }

        private IEnumerator CardEntrance(RectTransform rt, float delay)
        {
            yield return new WaitForSeconds(delay);
            Vector3 target = rt.localScale;
            rt.localScale  = Vector3.zero;
            float e = 0f, dur = 0.2f;
            while (e < dur)
            {
                e += Time.deltaTime;
                rt.localScale = Vector3.LerpUnclamped(Vector3.zero, target, EaseOutBack(e / dur));
                yield return null;
            }
            rt.localScale = target;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private IEnumerator FadeIn(System.Action onDone)
        {
            if (mainCanvasGroup == null) { onDone?.Invoke(); yield break; }
            mainCanvasGroup.alpha = 0f;
            float t = 0f, dur = 0.35f;
            while (t < dur) { t += Time.deltaTime; mainCanvasGroup.alpha = t / dur; yield return null; }
            mainCanvasGroup.alpha = 1f;
            onDone?.Invoke();
        }

        private IEnumerator FadeOut(System.Action onDone)
        {
            if (mainCanvasGroup == null) { onDone?.Invoke(); yield break; }
            float t = 0f, dur = 0.25f;
            while (t < dur) { t += Time.deltaTime; mainCanvasGroup.alpha = 1f - t / dur; yield return null; }
            mainCanvasGroup.alpha = 0f;
            onDone?.Invoke();
        }

        private void ClearSpawnedItems()
        {
            foreach (var lbl  in _wordLabels) if (lbl)  Destroy(lbl.gameObject);
            foreach (var card in _cards)      if (card) Destroy(card.gameObject);
            _wordLabels.Clear();
            _cards.Clear();
        }

        private static List<int> ShuffleIndices(int count)
        {
            var list = new List<int>();
            for (int i = 0; i < count; i++) list.Add(i);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
    }
}