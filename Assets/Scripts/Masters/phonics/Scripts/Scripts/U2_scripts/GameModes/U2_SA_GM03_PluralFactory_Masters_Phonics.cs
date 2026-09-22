using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public enum PluralEndingChoice
    {
        AddS,  // +s
        AddES  // +es
    }

    [System.Serializable]
    public class PluralFactoryItem
    {
        public string singularWord;
        public string pluralWord;
        public PluralEndingChoice correctChoice;
        public AudioClip wordAudio;
    }

    public class U2_SA_GM03_PluralFactory_Masters_Phonics : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [Header("1. Narration & Audio (Voice A & B)")]
        public AudioClip introInstructionClip;
        public AudioClip correctFeedbackClip;
        public AudioClip wrongFeedbackClip;
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Rule Banner")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI ruleBannerTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Center Word Card")]
        [SerializeField] private GameObject wordCardObj;
        [SerializeField] private TextMeshProUGUI wordCardText;
        private RectTransform cardRect;
        private CanvasGroup cardCanvasGroup;
        private Vector3 cardInitialPos;

        [Header("4. Two Plural Choice Bins / Buttons")]
        [SerializeField] private Button btnAddS;  // Left: +s
        [SerializeField] private Button btnAddES; // Right: +es

        private List<PluralFactoryItem> wordDeck;
        private int currentWordIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;

        private Canvas parentCanvas;

        private void Awake()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            AutoBindHierarchyElements();
            InitializeDeck();
        }

        private void OnEnable()
        {
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            AutoBindHierarchyElements();
            currentWordIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            InitializeDeck();
            LoadCurrentWord();

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
            }
        }

        // ------------------ Intuitive Smooth Swipe Gesture Handlers ------------------

        private Vector2 dragStartPointerPos;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isProcessingAnswer || cardRect == null) return;
            dragStartPointerPos = eventData.position;

            if (cardCanvasGroup != null) cardCanvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isProcessingAnswer || cardRect == null) return;

            // Camera-aware delta tracking
            Camera eventCam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : eventData.pressEventCamera;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                cardRect, 
                eventData.position, 
                eventCam, 
                out Vector3 worldPoint))
            {
                cardRect.position = worldPoint;
            }

            // Intuitive Physics: Dynamic rotation tilt based on swipe distance
            float deltaX = cardRect.localPosition.x - cardInitialPos.x;
            float tiltAngle = Mathf.Clamp(-deltaX * 0.08f, -22f, 22f);
            cardRect.localRotation = Quaternion.Euler(0f, 0f, tiltAngle);

            // Responsive Button Highlight Feedback
            if (deltaX < -60f) // Swiping Left (+s)
            {
                if (btnAddS != null) btnAddS.transform.localScale = Vector3.one * 1.15f;
                if (btnAddES != null) btnAddES.transform.localScale = Vector3.one;
            }
            else if (deltaX > 60f) // Swiping Right (+es)
            {
                if (btnAddES != null) btnAddES.transform.localScale = Vector3.one * 1.15f;
                if (btnAddS != null) btnAddS.transform.localScale = Vector3.one;
            }
            else
            {
                if (btnAddS != null) btnAddS.transform.localScale = Vector3.one;
                if (btnAddES != null) btnAddES.transform.localScale = Vector3.one;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isProcessingAnswer || cardRect == null) return;
            if (cardCanvasGroup != null) cardCanvasGroup.blocksRaycasts = true;

            // Reset button scales
            if (btnAddS != null) btnAddS.transform.localScale = Vector3.one;
            if (btnAddES != null) btnAddES.transform.localScale = Vector3.one;

            float deltaX = cardRect.localPosition.x - cardInitialPos.x;
            float swipeThreshold = 120f; // Responsive threshold

            if (deltaX < -swipeThreshold) // Swiped Left -> +s
            {
                StartCoroutine(FlingCardAndSelect(PluralEndingChoice.AddS, -1));
            }
            else if (deltaX > swipeThreshold) // Swiped Right -> +es
            {
                StartCoroutine(FlingCardAndSelect(PluralEndingChoice.AddES, 1));
            }
            else
            {
                StartCoroutine(SnapBackCard());
            }
        }

        private IEnumerator FlingCardAndSelect(PluralEndingChoice choice, int direction)
        {
            if (cardRect == null) yield break;

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlaySwipe();
            }

            // Smooth fling off screen in swipe direction
            float elapsed = 0f;
            float duration = 0.2f;
            Vector3 startPos = cardRect.localPosition;
            Vector3 targetPos = startPos + new Vector3(direction * 900f, 0f, 0f);
            Quaternion startRot = cardRect.localRotation;
            Quaternion targetRot = Quaternion.Euler(0f, 0f, direction * -35f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cardRect.localPosition = Vector3.Lerp(startPos, targetPos, t);
                cardRect.localRotation = Quaternion.Lerp(startRot, targetRot, t);

                if (cardCanvasGroup != null) cardCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            // Reset rotation & position for answer resolution
            cardRect.localRotation = Quaternion.identity;
            cardRect.localPosition = cardInitialPos;
            if (cardCanvasGroup != null) cardCanvasGroup.alpha = 1f;

            OnChoiceSelected(choice);
        }

        private IEnumerator SnapBackCard()
        {
            if (cardRect == null) yield break;

            float elapsed = 0f;
            float duration = 0.22f;
            Vector3 startPos = cardRect.localPosition;
            Quaternion startRot = cardRect.localRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cardRect.localPosition = Vector3.Lerp(startPos, cardInitialPos, t);
                cardRect.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, t);
                yield return null;
            }

            cardRect.localPosition = cardInitialPos;
            cardRect.localRotation = Quaternion.identity;
        }

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (ruleBannerTMP == null)
            {
                var t = transform.Find("RuleBanner") ?? transform.Find("Rule_Banner") ?? transform.Find("RuleText");
                if (t != null) ruleBannerTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (promptTMP == null)
            {
                var t = transform.Find("PromptText") ?? transform.Find("Prompt_Text") ?? transform.Find("InstructionText");
                if (t != null) promptTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (scoreTMP == null)
            {
                var t = transform.Find("ScoreText") ?? transform.Find("Score_Text") ?? transform.Find("Score");
                if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("ProgressBar (1)") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            // Word Card
            if (wordCardObj == null)
            {
                Transform c = transform.Find("WordCard") ?? transform.Find("DraggableCard") ?? transform.Find("Card");
                if (c != null) wordCardObj = c.gameObject;
            }

            if (wordCardObj != null)
            {
                cardRect = wordCardObj.GetComponent<RectTransform>();
                cardInitialPos = cardRect.localPosition;

                cardCanvasGroup = wordCardObj.GetComponent<CanvasGroup>();
                if (cardCanvasGroup == null) cardCanvasGroup = wordCardObj.AddComponent<CanvasGroup>();

                if (wordCardText == null)
                    wordCardText = wordCardObj.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Plural buttons / swipe targets
            if (btnAddS == null)
            {
                Transform t = transform.Find("Btn_AddS") ?? transform.Find("LeftButton") ?? transform.Find("Bin_UN") ?? transform.Find("Bin_1");
                if (t != null) btnAddS = t.GetComponent<Button>();
            }

            if (btnAddES == null)
            {
                Transform t = transform.Find("Btn_AddES") ?? transform.Find("RightButton") ?? transform.Find("Bin_RE") ?? transform.Find("Bin_2");
                if (t != null) btnAddES = t.GetComponent<Button>();
            }

            if (btnAddS != null)
            {
                btnAddS.onClick.RemoveAllListeners();
                btnAddS.onClick.AddListener(() => OnChoiceSelected(PluralEndingChoice.AddS));
            }

            if (btnAddES != null)
            {
                btnAddES.onClick.RemoveAllListeners();
                btnAddES.onClick.AddListener(() => OnChoiceSelected(PluralEndingChoice.AddES));
            }
        }

        private void SetButtonText(Button btn, string text)
        {
            if (btn == null) return;
            TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = text;
        }

        private void InitializeDeck()
        {
            // 16 curated balanced items: 8 take +s, 8 take +es
            wordDeck = new List<PluralFactoryItem>
            {
                // Add -s
                new PluralFactoryItem { singularWord = "dog", pluralWord = "dogs", correctChoice = PluralEndingChoice.AddS },
                new PluralFactoryItem { singularWord = "plant", pluralWord = "plants", correctChoice = PluralEndingChoice.AddS },
                new PluralFactoryItem { singularWord = "table", pluralWord = "tables", correctChoice = PluralEndingChoice.AddS },
                new PluralFactoryItem { singularWord = "bike", pluralWord = "bikes", correctChoice = PluralEndingChoice.AddS },
                new PluralFactoryItem { singularWord = "pillow", pluralWord = "pillows", correctChoice = PluralEndingChoice.AddS },
                new PluralFactoryItem { singularWord = "finger", pluralWord = "fingers", correctChoice = PluralEndingChoice.AddS },
                new PluralFactoryItem { singularWord = "book", pluralWord = "books", correctChoice = PluralEndingChoice.AddS },
                new PluralFactoryItem { singularWord = "pencil", pluralWord = "pencils", correctChoice = PluralEndingChoice.AddS },

                // Add -es
                new PluralFactoryItem { singularWord = "bus", pluralWord = "buses", correctChoice = PluralEndingChoice.AddES },
                new PluralFactoryItem { singularWord = "dish", pluralWord = "dishes", correctChoice = PluralEndingChoice.AddES },
                new PluralFactoryItem { singularWord = "box", pluralWord = "boxes", correctChoice = PluralEndingChoice.AddES },
                new PluralFactoryItem { singularWord = "fox", pluralWord = "foxes", correctChoice = PluralEndingChoice.AddES },
                new PluralFactoryItem { singularWord = "glass", pluralWord = "glasses", correctChoice = PluralEndingChoice.AddES },
                new PluralFactoryItem { singularWord = "watch", pluralWord = "watches", correctChoice = PluralEndingChoice.AddES },
                new PluralFactoryItem { singularWord = "bench", pluralWord = "benches", correctChoice = PluralEndingChoice.AddES },
                new PluralFactoryItem { singularWord = "brush", pluralWord = "brushes", correctChoice = PluralEndingChoice.AddES }
            };

            // Shuffle deck
            for (int i = 0; i < wordDeck.Count; i++)
            {
                int rnd = UnityEngine.Random.Range(i, wordDeck.Count);
                var temp = wordDeck[i];
                wordDeck[i] = wordDeck[rnd];
                wordDeck[rnd] = temp;
            }
        }

        private void LoadCurrentWord()
        {
            if (currentWordIndex >= wordDeck.Count)
            {
                OnAllWordsCompleted();
                return;
            }

            isProcessingAnswer = false;
            PluralFactoryItem currentItem = wordDeck[currentWordIndex];

            if (titleTMP != null) titleTMP.text = "<b>PLURAL FACTORY</b>";
            if (promptTMP != null) promptTMP.text = $"Make <b>\"{currentItem.singularWord}\"</b> plural: Swipe or tap <b>+s</b> or <b>+es</b>!";

            if (progressBar != null)
            {
                progressBar.value = (float)currentWordIndex / wordDeck.Count;
            }

            if (wordCardObj != null)
            {
                wordCardObj.SetActive(true);
                if (cardRect != null) cardRect.localPosition = cardInitialPos;
                if (cardCanvasGroup != null)
                {
                    cardCanvasGroup.alpha = 1f;
                    cardCanvasGroup.blocksRaycasts = true;
                }
            }

            if (wordCardText != null)
            {
                wordCardText.text = $"<size=220%><b><color=#0E1E38>{currentItem.singularWord}</color></b></size>";
            }
        }

        public void OnChoiceSelected(PluralEndingChoice choice)
        {
            if (isProcessingAnswer || currentWordIndex >= wordDeck.Count) return;

            PluralFactoryItem currentItem = wordDeck[currentWordIndex];
            if (choice == currentItem.correctChoice)
            {
                StartCoroutine(HandleCorrectChoice(choice, currentItem));
            }
            else
            {
                StartCoroutine(HandleWrongChoice(choice, currentItem));
            }
        }

        private IEnumerator HandleCorrectChoice(PluralEndingChoice choice, PluralFactoryItem item)
        {
            isProcessingAnswer = true;

            // Highlight plural formation boldly with large clear text
            if (wordCardText != null)
            {
                string endingStr = (choice == PluralEndingChoice.AddS) ? "-s" : "-es";
                wordCardText.text = $"<size=170%><b><color=#0E1E38>{item.singularWord}</color><color=#008822> + {endingStr}</color>  ->  <color=#006600>{item.pluralWord}</color></b></size>";
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<size=120%><color=#007722><b>Correct! {item.singularWord} + {(choice == PluralEndingChoice.AddS ? "s" : "es")} = {item.pluralWord}</b></color></size>";
            }

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();
                
                // Play Word Pronunciation
                AudioClip pronunciation = item.wordAudio ?? Resources.Load<AudioClip>($"U2_audio/{item.pluralWord}") ?? Resources.Load<AudioClip>($"Audio/U2_audio/{item.pluralWord}");
                if (pronunciation != null)
                {
                    U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(pronunciation);
                }
            }

            Button targetBtn = (choice == PluralEndingChoice.AddS) ? btnAddS : btnAddES;
            if (targetBtn != null) StartCoroutine(PunchScale(targetBtn.transform, 1.25f));

            currentScore += 100;
            UpdateScoreUI();

            // Give the child 1.3 seconds to see, hear, and read the built plural word!
            yield return new WaitForSeconds(1.3f);

            currentWordIndex++;
            LoadCurrentWord();
        }

        private IEnumerator HandleWrongChoice(PluralEndingChoice choice, PluralFactoryItem item)
        {
            isProcessingAnswer = true;

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            string wrongEnding = (choice == PluralEndingChoice.AddS) ? "-s" : "-es";
            string correctEnding = (item.correctChoice == PluralEndingChoice.AddS) ? "-s" : "-es";

            if (wordCardText != null)
            {
                wordCardText.text = $"<size=170%><b><color=#990000><s>{item.singularWord}{wrongEnding}</s></color>  ->  <color=#006600>{item.pluralWord}</color></b></size>";
            }

            if (promptTMP != null)
            {
                promptTMP.text = $"<size=120%><color=#AA1111><b>Watch the rule! \"{item.singularWord}\" needs {correctEnding} -> {item.pluralWord}</b></color></size>";
            }

            Button targetBtn = (choice == PluralEndingChoice.AddS) ? btnAddS : btnAddES;
            if (targetBtn != null) StartCoroutine(PunchScale(targetBtn.transform, 0.85f));

            // Give 1.4 seconds to read the rule correction
            yield return new WaitForSeconds(1.4f);

            isProcessingAnswer = false;
            LoadCurrentWord();
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
        }

        private void OnAllWordsCompleted()
        {
            Debug.Log("<color=green>Plural Factory (Activity 2) Completed! Advancing...</color>");

            if (promptTMP != null) promptTMP.text = "<b>Fantastic! Plural Factory Complete!</b>";

            if (progressBar != null) progressBar.value = 1f;

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null && celebrationClip != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(celebrationClip);
            }

            if (U2_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U2_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }
        }

        private IEnumerator PunchScale(Transform tr, float scaleMult)
        {
            if (tr == null) yield break;
            Vector3 orig = Vector3.one;
            tr.localScale = orig * scaleMult;
            yield return new WaitForSeconds(0.12f);
            if (tr != null) tr.localScale = orig;
        }
    }
}
