using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public enum JobChuteCategory
    {
        MoreThanOne,  // -s, -es (Plural)
        Past,         // -ed (Already happened)
        HappeningNow, // -ing (Progressive)
        Compare       // -er, -est (Comparison)
    }

    [System.Serializable]
    public class JobBoardWordItem
    {
        public string word;
        public string endingHighlight; // e.g. "ing"
        public JobChuteCategory targetCategory;
        public AudioClip wordAudio;
    }

    public class U2_SA_GM03_JobBoard_Masters_Phonics : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [Header("1. Narration & Audio Clips (Voice A)")]
        public AudioClip introInstructionClip;
        public AudioClip correctFeedbackClip;
        public AudioClip wrongFeedbackClip;
        public AudioClip celebrationClip;

        [Header("2. Scene Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("3. Draggable Word Card (Center)")]
        [SerializeField] private GameObject draggableCard;
        [SerializeField] private TextMeshProUGUI draggableWordText;
        private CanvasGroup cardCanvasGroup;
        private RectTransform cardRectTransform;
        private Vector3 cardInitialLocalPos;

        [Header("4. The 4 Job Chutes / Bins (Bottom)")]
        [SerializeField] private GameObject chuteMoreThanOne;  // -s, -es
        [SerializeField] private GameObject chutePast;         // -ed
        [SerializeField] private GameObject chuteHappeningNow; // -ing
        [SerializeField] private GameObject chuteCompare;      // -er, -est

        private Canvas parentCanvas;
        private List<JobBoardWordItem> wordDeck;
        private int currentWordIndex = 0;
        private int currentScore = 0;
        private bool isProcessingDrop = false;

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

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
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

            // Bind Draggable Card
            if (draggableCard == null)
            {
                Transform c = transform.Find("DraggableCard") 
                           ?? transform.Find("WordCard") 
                           ?? transform.Find("Word_Card") 
                           ?? transform.Find("Card");
                if (c != null) draggableCard = c.gameObject;
            }

            if (draggableCard != null)
            {
                cardRectTransform = draggableCard.GetComponent<RectTransform>();
                cardInitialLocalPos = cardRectTransform.localPosition;

                cardCanvasGroup = draggableCard.GetComponent<CanvasGroup>();
                if (cardCanvasGroup == null) cardCanvasGroup = draggableCard.AddComponent<CanvasGroup>();

                if (draggableWordText == null)
                    draggableWordText = draggableCard.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Bind 4 Chutes / Bins
            Transform binsParent = transform.Find("Bins") ?? transform.Find("SortingBins") ?? transform.Find("Chutes") ?? transform;

            if (chuteMoreThanOne == null)
                chuteMoreThanOne = FindChute(binsParent, "more", "plural", "bin_un", "bin_1", "bin 1");

            if (chutePast == null)
                chutePast = FindChute(binsParent, "past", "ed", "bin_re", "bin_2", "bin 2");

            if (chuteHappeningNow == null)
                chuteHappeningNow = FindChute(binsParent, "now", "ing", "bin_dis", "bin_3", "bin 3");

            if (chuteCompare == null)
                chuteCompare = FindChute(binsParent, "compare", "er", "est", "bin_mis", "bin_4", "bin 4");
        }

        private GameObject FindChute(Transform parent, params string[] keywords)
        {
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                string name = child.name.ToLower();
                foreach (var kw in keywords)
                {
                    if (name.Contains(kw)) return child.gameObject;
                }
            }
            return null;
        }

        private void InitializeDeck()
        {
            wordDeck = new List<JobBoardWordItem>
            {
                new JobBoardWordItem { word = "dogs", endingHighlight = "s", targetCategory = JobChuteCategory.MoreThanOne },
                new JobBoardWordItem { word = "boxes", endingHighlight = "es", targetCategory = JobChuteCategory.MoreThanOne },
                new JobBoardWordItem { word = "dishes", endingHighlight = "es", targetCategory = JobChuteCategory.MoreThanOne },
                new JobBoardWordItem { word = "plants", endingHighlight = "s", targetCategory = JobChuteCategory.MoreThanOne },

                new JobBoardWordItem { word = "walked", endingHighlight = "ed", targetCategory = JobChuteCategory.Past },
                new JobBoardWordItem { word = "cracked", endingHighlight = "ed", targetCategory = JobChuteCategory.Past },
                new JobBoardWordItem { word = "hopped", endingHighlight = "ed", targetCategory = JobChuteCategory.Past },
                new JobBoardWordItem { word = "shouted", endingHighlight = "ed", targetCategory = JobChuteCategory.Past },

                new JobBoardWordItem { word = "hopping", endingHighlight = "ing", targetCategory = JobChuteCategory.HappeningNow },
                new JobBoardWordItem { word = "jumping", endingHighlight = "ing", targetCategory = JobChuteCategory.HappeningNow },
                new JobBoardWordItem { word = "reading", endingHighlight = "ing", targetCategory = JobChuteCategory.HappeningNow },
                new JobBoardWordItem { word = "running", endingHighlight = "ing", targetCategory = JobChuteCategory.HappeningNow },

                new JobBoardWordItem { word = "stronger", endingHighlight = "er", targetCategory = JobChuteCategory.Compare },
                new JobBoardWordItem { word = "tallest", endingHighlight = "est", targetCategory = JobChuteCategory.Compare },
                new JobBoardWordItem { word = "nicer", endingHighlight = "er", targetCategory = JobChuteCategory.Compare },
                new JobBoardWordItem { word = "brightest", endingHighlight = "est", targetCategory = JobChuteCategory.Compare }
            };

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
                OnAllWordsSorted();
                return;
            }

            isProcessingDrop = false;
            JobBoardWordItem currentItem = wordDeck[currentWordIndex];

            if (progressBar != null)
            {
                progressBar.value = (float)currentWordIndex / wordDeck.Count;
            }

            if (draggableCard != null)
            {
                draggableCard.SetActive(true);
                if (cardRectTransform != null) cardRectTransform.localPosition = cardInitialLocalPos;
                if (cardCanvasGroup != null)
                {
                    cardCanvasGroup.alpha = 1f;
                    cardCanvasGroup.blocksRaycasts = true;
                }
            }

            if (draggableWordText != null)
            {
                draggableWordText.text = currentItem.word;
            }
        }

        // ------------------ Smooth Drag & Hover Handlers ------------------

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isProcessingDrop || cardRectTransform == null) return;

            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.blocksRaycasts = false;
                cardCanvasGroup.alpha = 1f;
            }

            cardRectTransform.SetAsLastSibling(); // Bring to front while dragging
            cardRectTransform.localScale = Vector3.one * 1.08f; // Gentle lift feedback
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isProcessingDrop || cardRectTransform == null) return;

            // Correctly convert Screen Point into Canvas World/Local position
            Camera eventCam = eventData.pressEventCamera;
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                eventCam = null;
            }

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                cardRectTransform, 
                eventData.position, 
                eventCam, 
                out Vector3 worldPoint))
            {
                cardRectTransform.position = worldPoint;
            }
            else
            {
                cardRectTransform.position = eventData.position;
            }

            // Check if hovering over any chute for magnetic hover feedback
            GameObject hoveredChute = CheckDropTarget(eventData);
            UpdateHoverFeedback(hoveredChute);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isProcessingDrop) return;

            if (cardCanvasGroup != null) cardCanvasGroup.blocksRaycasts = true;

            // Reset all chute scales from hover
            ResetAllChuteHovers();

            GameObject targetChute = CheckDropTarget(eventData);

            if (targetChute != null)
            {
                JobChuteCategory droppedCat = GetChuteCategory(targetChute);
                JobBoardWordItem currentItem = wordDeck[currentWordIndex];

                if (droppedCat == currentItem.targetCategory)
                {
                    StartCoroutine(HandleCorrectDrop(targetChute, currentItem));
                }
                else
                {
                    StartCoroutine(HandleWrongDrop(targetChute));
                }
            }
            else
            {
                StartCoroutine(SnapBackCard());
            }
        }

        private GameObject lastHoveredChute = null;

        private void UpdateHoverFeedback(GameObject currentHovered)
        {
            if (currentHovered == lastHoveredChute) return;

            if (lastHoveredChute != null)
                lastHoveredChute.transform.localScale = Vector3.one;

            if (currentHovered != null)
                currentHovered.transform.localScale = Vector3.one * 1.08f; // Slight magnetic swell

            lastHoveredChute = currentHovered;
        }

        private void ResetAllChuteHovers()
        {
            if (chuteMoreThanOne != null) chuteMoreThanOne.transform.localScale = Vector3.one;
            if (chutePast != null) chutePast.transform.localScale = Vector3.one;
            if (chuteHappeningNow != null) chuteHappeningNow.transform.localScale = Vector3.one;
            if (chuteCompare != null) chuteCompare.transform.localScale = Vector3.one;
            lastHoveredChute = null;
        }

        private GameObject CheckDropTarget(PointerEventData eventData)
        {
            List<GameObject> chutes = new List<GameObject> { chuteMoreThanOne, chutePast, chuteHappeningNow, chuteCompare };

            // 1. Raycast check
            if (eventData.pointerCurrentRaycast.gameObject != null)
            {
                GameObject hit = eventData.pointerCurrentRaycast.gameObject;
                foreach (var c in chutes)
                {
                    if (c != null && (hit == c || hit.transform.IsChildOf(c.transform)))
                        return c;
                }
            }

            // 2. Screen Proximity Fallback (within 150px)
            GameObject closest = null;
            float minDist = 160f;

            foreach (var c in chutes)
            {
                if (c == null) continue;
                float dist = Vector2.Distance(eventData.position, c.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = c;
                }
            }

            return closest;
        }

        private JobChuteCategory GetChuteCategory(GameObject chute)
        {
            if (chute == chutePast) return JobChuteCategory.Past;
            if (chute == chuteHappeningNow) return JobChuteCategory.HappeningNow;
            if (chute == chuteCompare) return JobChuteCategory.Compare;
            return JobChuteCategory.MoreThanOne;
        }

        private IEnumerator HandleCorrectDrop(GameObject chute, JobBoardWordItem item)
        {
            isProcessingDrop = true;

            // Highlight ending letters
            if (!string.IsNullOrEmpty(item.word) && !string.IsNullOrEmpty(item.endingHighlight) && item.word.Length >= item.endingHighlight.Length)
            {
                string basePart = item.word.Substring(0, item.word.Length - item.endingHighlight.Length);
                if (draggableWordText != null)
                {
                    draggableWordText.text = $"{basePart}<color=#FFE57F><b>-{item.endingHighlight}</b></color>";
                }
            }

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();
                if (item.wordAudio != null)
                {
                    U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(item.wordAudio);
                }
            }

            if (chute != null) StartCoroutine(PunchScale(chute.transform, 1.25f));

            // Smooth funnel animation: card glides into the bin, scales down, and fades into the basket
            if (cardRectTransform != null && chute != null)
            {
                float duration = 0.3f;
                float elapsed = 0f;
                Vector3 startPos = cardRectTransform.position;
                Vector3 endPos = chute.transform.position;
                Vector3 startScale = cardRectTransform.localScale;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                    // Curved arch into the bin
                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                    currentPos.y += Mathf.Sin(t * Mathf.PI) * 25f; // Slight arch upwards before dropping in

                    cardRectTransform.position = currentPos;
                    cardRectTransform.localScale = Vector3.Lerp(startScale, Vector3.one * 0.2f, t);

                    if (cardCanvasGroup != null)
                        cardCanvasGroup.alpha = Mathf.Lerp(0.95f, 0.2f, t);

                    yield return null;
                }

                if (cardCanvasGroup != null) cardCanvasGroup.alpha = 0f;
                cardRectTransform.localScale = Vector3.one;
            }

            currentScore += 100;
            UpdateScoreUI();

            currentWordIndex++;
            yield return new WaitForSeconds(0.25f);

            LoadCurrentWord();
        }

        private IEnumerator HandleWrongDrop(GameObject chute)
        {
            isProcessingDrop = true;

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }

            if (chute != null) StartCoroutine(PunchScale(chute.transform, 0.9f));
            yield return StartCoroutine(SnapBackCard());

            isProcessingDrop = false;
        }

        private IEnumerator SnapBackCard()
        {
            if (cardRectTransform == null) yield break;

            float elapsed = 0f;
            float duration = 0.22f;
            Vector3 startPos = cardRectTransform.localPosition;
            Vector3 startScale = cardRectTransform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cardRectTransform.localPosition = Vector3.Lerp(startPos, cardInitialLocalPos, t);
                cardRectTransform.localScale = Vector3.Lerp(startScale, Vector3.one, t);

                if (cardCanvasGroup != null)
                    cardCanvasGroup.alpha = Mathf.Lerp(0.95f, 1f, t);

                yield return null;
            }

            cardRectTransform.localPosition = cardInitialLocalPos;
            cardRectTransform.localScale = Vector3.one;
            if (cardCanvasGroup != null) cardCanvasGroup.alpha = 1f;
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
        }

        private void OnAllWordsSorted()
        {
            Debug.Log("<color=green>Job Board (Activity 1) Completed! Advancing...</color>");

            if (promptTMP != null) promptTMP.text = "<b>Fantastic! All Job Chutes Sorted!</b>";

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
