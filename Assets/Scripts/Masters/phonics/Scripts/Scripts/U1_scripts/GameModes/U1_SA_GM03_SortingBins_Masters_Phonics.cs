using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    [System.Serializable]
    public class SortWordItem
    {
        public string word;
        public string prefixFamily; // "un", "re", "dis", "mis"
        public AudioClip wordAudio;
    }

    public class U1_SA_BinDropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public U1_SA_GM03_SortingBins_Masters_Phonics gm03;
        public string prefixFamily;
        private Vector3 originalScale;

        private void Awake()
        {
            originalScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.dragging && gm03 != null && !gm03.IsProcessingAnswer)
            {
                transform.localScale = originalScale * 1.08f;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = originalScale;
        }

        public void OnDrop(PointerEventData eventData)
        {
            transform.localScale = originalScale;
            if (gm03 != null && !gm03.IsProcessingAnswer)
            {
                gm03.OnCardDroppedOnBin(prefixFamily, transform);
            }
        }
    }

    public class U1_SA_DraggableWordCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public U1_SA_GM03_SortingBins_Masters_Phonics gm03;
        public RectTransform rectTransform;
        public Canvas canvas;
        public CanvasGroup canvasGroup;
        private Vector3 originalPosition;
        private Transform originalParent;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvas = GetComponentInParent<Canvas>();
        }

        public void SetOrigin(Vector3 pos)
        {
            originalPosition = pos;
            originalParent = transform.parent;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (gm03 != null && gm03.IsProcessingAnswer) return;
            originalPosition = rectTransform.localPosition;
            canvasGroup.blocksRaycasts = false;
            transform.SetAsLastSibling();
            transform.localScale = Vector3.one * 1.08f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (gm03 != null && gm03.IsProcessingAnswer) return;
            if (canvas != null)
            {
                RectTransform canvasRT = canvas.transform as RectTransform;
                if (canvasRT != null)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRT,
                        eventData.position,
                        eventData.pressEventCamera,
                        out localPoint);
                    rectTransform.position = canvasRT.TransformPoint(localPoint);
                }
                else
                {
                    rectTransform.position = eventData.position;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;
            transform.localScale = Vector3.one;

            if (gm03 != null)
            {
                gm03.CheckDropOnBins(this, originalPosition, eventData);
            }
            else
            {
                rectTransform.localPosition = originalPosition;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            if (gm03 != null) gm03.OnCardTapped();
        }
    }

    public class U1_SA_GM03_SortingBins_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Activity Data (Optional)")]
        public U1_SA_ActivityDataSO_Masters_Phonics activityData;

        [Header("2. Narration Audio Clips (Voice A)")]
        public AudioClip introInstructionClip;
        public AudioClip correctFeedbackClip;
        public AudioClip failHintClip;
        public AudioClip celebrationClip;

        [Header("3. UI Header & Status (Auto-Bound to Hierarchy)")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private Slider progressBar;

        [Header("4. Gameplay Containers (Auto-Bound to Hierarchy)")]
        public Transform wordDeckContainer;
        public Transform binsContainer;

        private List<SortWordItem> wordsToSort;
        private int currentWordIndex = 0;
        private int currentScore = 0;
        private bool isProcessingAnswer = false;
        public bool IsProcessingAnswer => isProcessingAnswer;

        private bool dropHandledThisFrame = false;
        private Coroutine progressTweenCoroutine;

        // Existing Card in WordDeck_Container
        private GameObject activeCardObj;
        private TextMeshProUGUI activeCardText;
        private U1_SA_DraggableWordCard activeDraggable;
        private Vector3 cardOriginalLocalPos = Vector3.zero;

        // Existing Bins in Bins_Container
        private List<GameObject> binObjects = new List<GameObject>();
        private List<string> binPrefixKeys = new List<string>();

        private void Awake()
        {
            AutoBindHierarchy();
            InitializeWordList();
        }

        private void OnEnable()
        {
            AutoBindHierarchy();
            currentWordIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            InitializeWordList();
            BindSceneBins();
            BindSceneCard();
            LoadCurrentCard();

            if (U1_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
            }
        }

        public void Initialize(U1_SA_ActivityDataSO_Masters_Phonics data)
        {
            activityData = data;
            currentWordIndex = 0;
            currentScore = 0;
            UpdateScoreUI();
            InitializeWordList();
            BindSceneBins();
            BindSceneCard();
            LoadCurrentCard();
        }

        private void AutoBindHierarchy()
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

            if (wordDeckContainer == null)
            {
                var d = transform.Find("WordDeck_Container") ?? transform.Find("WordDeck");
                if (d != null) wordDeckContainer = d;
            }

            if (binsContainer == null)
            {
                var b = transform.Find("Bins_Container") ?? transform.Find("BinsContainer") ?? transform.Find("Bins");
                if (b != null) binsContainer = b;
            }

            StyleProgressBar();
        }

        private void StyleProgressBar()
        {
            if (progressBar == null) return;

            Transform handle = progressBar.transform.Find("Handle Slide Area");
            if (handle != null) handle.gameObject.SetActive(false);

            Transform fill = progressBar.transform.Find("Fill Area/Fill");
            if (fill != null)
            {
                Image fillImg = fill.GetComponent<Image>();
                if (fillImg != null) fillImg.color = new Color(0.20f, 0.85f, 0.45f, 1f);
            }
        }

        private void InitializeWordList()
        {
            wordsToSort = new List<SortWordItem>
            {
                new SortWordItem { word = "unknown", prefixFamily = "un" },
                new SortWordItem { word = "refill", prefixFamily = "re" },
                new SortWordItem { word = "dislike", prefixFamily = "dis" },
                new SortWordItem { word = "misplace", prefixFamily = "mis" },
                new SortWordItem { word = "unreal", prefixFamily = "un" },
                new SortWordItem { word = "remix", prefixFamily = "re" },
                new SortWordItem { word = "disagree", prefixFamily = "dis" },
                new SortWordItem { word = "misprint", prefixFamily = "mis" }
            };

            // Link wordAudio from activityData if available
            if (activityData != null && activityData.targetWords != null)
            {
                foreach (var item in wordsToSort)
                {
                    var found = activityData.targetWords.Find(w => w != null && w.fullWord.ToLower() == item.word.ToLower());
                    if (found != null)
                    {
                        item.wordAudio = found.fullWordAudioClip;
                    }
                }
            }

            for (int i = 0; i < wordsToSort.Count; i++)
            {
                SortWordItem temp = wordsToSort[i];
                int r = UnityEngine.Random.Range(i, wordsToSort.Count);
                wordsToSort[i] = wordsToSort[r];
                wordsToSort[r] = temp;
            }
        }

        private void BindSceneBins()
        {
            if (binsContainer == null) return;

            binObjects.Clear();
            binPrefixKeys.Clear();

            foreach (Transform child in binsContainer)
            {
                if (child == null) continue;

                string rawName = child.name.ToLower();
                string prefix = "un";
                if (rawName.Contains("un")) prefix = "un";
                else if (rawName.Contains("re")) prefix = "re";
                else if (rawName.Contains("dis")) prefix = "dis";
                else if (rawName.Contains("mis")) prefix = "mis";
                else if (rawName.Contains("pre")) prefix = "pre";

                binObjects.Add(child.gameObject);
                binPrefixKeys.Add(prefix);

                U1_SA_BinDropTarget dropTarget = child.GetComponent<U1_SA_BinDropTarget>();
                if (dropTarget == null) dropTarget = child.gameObject.AddComponent<U1_SA_BinDropTarget>();
                dropTarget.gm03 = this;
                dropTarget.prefixFamily = prefix;

                Button btn = child.GetComponent<Button>();
                if (btn == null) btn = child.gameObject.AddComponent<Button>();

                string capturedPrefix = prefix;
                Transform capturedTransform = child;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnBinTapped(capturedPrefix, capturedTransform));
            }
        }

        private void BindSceneCard()
        {
            if (wordDeckContainer == null) return;

            if (wordDeckContainer.childCount > 0)
            {
                activeCardObj = wordDeckContainer.GetChild(0).gameObject;
            }
            else
            {
                activeCardObj = wordDeckContainer.gameObject;
            }

            if (activeCardObj != null)
            {
                activeCardText = activeCardObj.GetComponentInChildren<TextMeshProUGUI>(true);

                activeDraggable = activeCardObj.GetComponent<U1_SA_DraggableWordCard>();
                if (activeDraggable == null) activeDraggable = activeCardObj.AddComponent<U1_SA_DraggableWordCard>();
                activeDraggable.gm03 = this;

                cardOriginalLocalPos = activeCardObj.transform.localPosition;
                activeDraggable.SetOrigin(cardOriginalLocalPos);
            }
        }

        public void LoadCurrentCard()
        {
            if (wordsToSort == null || wordsToSort.Count == 0) InitializeWordList();

            if (currentWordIndex >= wordsToSort.Count)
            {
                OnAllWordsSorted();
                return;
            }

            isProcessingAnswer = false;
            dropHandledThisFrame = false;
            SortWordItem currentItem = wordsToSort[currentWordIndex];

            if (promptTMP != null) promptTMP.text = "<b>Drag the word or tap its matching prefix bin!</b>";

            UpdateProgressBar(currentWordIndex, wordsToSort.Count);

            if (activeCardObj != null)
            {
                activeCardObj.transform.localPosition = cardOriginalLocalPos;

                if (activeCardText != null)
                {
                    activeCardText.text = $"<b>{currentItem.word}</b>";
                }

                activeCardObj.SetActive(true);
                StartCoroutine(PunchScale(activeCardObj.transform, 1.12f));
            }
        }

        public void OnCardTapped()
        {
            SortWordItem item = wordsToSort[currentWordIndex];
            if (U1_SA_AudioManager_Masters_Phonics.Instance != null && item.wordAudio != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(item.wordAudio);
            }
            if (activeCardObj != null)
            {
                StartCoroutine(PunchScale(activeCardObj.transform, 1.06f));
            }
        }

        public void OnCardDroppedOnBin(string targetPrefix, Transform targetBin)
        {
            if (isProcessingAnswer) return;
            dropHandledThisFrame = true;

            SortWordItem currentItem = wordsToSort[currentWordIndex];
            string cleanDropped = targetPrefix.Trim().TrimEnd('-').ToLower();
            string cleanTarget = currentItem.prefixFamily.Trim().TrimEnd('-').ToLower();

            if (cleanDropped == cleanTarget)
            {
                StartCoroutine(HandleCorrectSort(targetBin, currentItem));
            }
            else
            {
                StartCoroutine(HandleIncorrectSort(targetBin));
                if (activeCardObj != null)
                {
                    StartCoroutine(SnapBackCard(activeCardObj.GetComponent<RectTransform>(), cardOriginalLocalPos));
                }
            }
        }

        public void CheckDropOnBins(U1_SA_DraggableWordCard card, Vector3 originPos, PointerEventData eventData)
        {
            if (isProcessingAnswer)
            {
                StartCoroutine(SnapBackCard(card.rectTransform, originPos));
                return;
            }

            if (dropHandledThisFrame)
            {
                dropHandledThisFrame = false;
                return;
            }

            GameObject closestBin = null;
            string closestPrefix = null;
            float minDistance = float.MaxValue;

            for (int i = 0; i < binObjects.Count; i++)
            {
                var bin = binObjects[i];
                if (bin == null) continue;
                RectTransform binRT = bin.GetComponent<RectTransform>();

                float dist = Vector3.Distance(card.rectTransform.position, binRT.position);
                bool contains = RectTransformUtility.RectangleContainsScreenPoint(binRT, eventData.position, eventData.pressEventCamera);

                if (contains || dist < 240f)
                {
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestBin = bin;
                        closestPrefix = (i < binPrefixKeys.Count) ? binPrefixKeys[i] : "un";
                    }
                }
            }

            if (closestBin != null && closestPrefix != null)
            {
                OnCardDroppedOnBin(closestPrefix, closestBin.transform);
            }
            else
            {
                StartCoroutine(SnapBackCard(card.rectTransform, originPos));
            }
        }

        private IEnumerator SnapBackCard(RectTransform rt, Vector3 origin)
        {
            if (rt == null) yield break;
            float elapsed = 0f;
            float duration = 0.18f;
            Vector3 start = rt.localPosition;
            while (elapsed < duration)
            {
                if (rt == null) yield break;
                elapsed += Time.deltaTime;
                rt.localPosition = Vector3.Lerp(start, origin, elapsed / duration);
                yield return null;
            }
            if (rt != null) rt.localPosition = origin;
        }

        private void OnBinTapped(string tappedPrefix, Transform binTransform)
        {
            if (isProcessingAnswer) return;

            SortWordItem currentItem = wordsToSort[currentWordIndex];
            string cleanTapped = tappedPrefix.Trim().TrimEnd('-').ToLower();
            string cleanTarget = currentItem.prefixFamily.Trim().TrimEnd('-').ToLower();

            bool isCorrect = (cleanTapped == cleanTarget);

            if (isCorrect)
            {
                StartCoroutine(HandleCorrectSort(binTransform, currentItem));
            }
            else
            {
                StartCoroutine(HandleIncorrectSort(binTransform));
            }
        }

        private IEnumerator HandleCorrectSort(Transform binTransform, SortWordItem item)
        {
            isProcessingAnswer = true;

            StartCoroutine(PunchScale(binTransform, 1.18f));

            // Audio: Play Word Audio (Voice B) or Correct Feedback (Voice A)
            if (U1_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
                
                if (item.wordAudio != null)
                {
                    U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(item.wordAudio);
                }
                else if (correctFeedbackClip != null)
                {
                    U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(correctFeedbackClip);
                }
            }

            currentScore += 100;
            UpdateScoreUI();

            if (activeCardObj != null)
            {
                StartCoroutine(PunchScale(activeCardObj.transform, 0.8f));
            }

            yield return new WaitForSeconds(0.75f);

            currentWordIndex++;
            LoadCurrentCard();
        }

        private IEnumerator HandleIncorrectSort(Transform binTransform)
        {
            isProcessingAnswer = true;

            if (U1_SA_AudioManager_Masters_Phonics.Instance != null && failHintClip != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(failHintClip);
            }

            if (binTransform != null)
            {
                Vector3 origPos = binTransform.localPosition;
                for (int i = 0; i < 6; i++)
                {
                    binTransform.localPosition = origPos + new Vector3((i % 2 == 0 ? 14 : -14), 0, 0);
                    yield return new WaitForSeconds(0.04f);
                }
                binTransform.localPosition = origPos;
            }

            yield return new WaitForSeconds(0.3f);
            isProcessingAnswer = false;
        }

        private void UpdateProgressBar(int currentStep, int totalSteps)
        {
            if (progressBar != null)
            {
                progressBar.minValue = 0;
                progressBar.maxValue = totalSteps;

                if (progressTweenCoroutine != null) StopCoroutine(progressTweenCoroutine);
                progressTweenCoroutine = StartCoroutine(TweenSlider(currentStep));
            }
        }

        private IEnumerator TweenSlider(float targetVal)
        {
            float startVal = progressBar.value;
            float elapsed = 0f;
            float dur = 0.35f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                progressBar.value = Mathf.Lerp(startVal, targetVal, elapsed / dur);
                yield return null;
            }
            progressBar.value = targetVal;
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null)
            {
                scoreTMP.text = $"Score: <b>{currentScore}</b>";
            }
        }

        private void OnAllWordsSorted()
        {
            Debug.Log("<color=green>Family Sort Completed! Advancing to Suffix Party...</color>");

            if (promptTMP != null) promptTMP.text = "<b>Fantastic! All words sorted!</b>";

            if (U1_SA_AudioManager_Masters_Phonics.Instance != null && celebrationClip != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(celebrationClip);
            }

            if (U1_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U1_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
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
