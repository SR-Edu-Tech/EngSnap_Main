using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U8_SA_GM03_ThreeSounds_Masters_Phonics : MonoBehaviour
    {
        [Header("Item Pool (18 items)")]
        public List<ThreeSoundsItem> items;
        private int currentIndex = 0;
        private int score = 0;
        private int firstAttemptCorrectCount = 0;
        private bool isProcessing = false;
        private bool isFirstAttemptOnItem = true;

        [Header("Bins (3 Sound Bins)")]
        [SerializeField] private RectTransform arBin;
        [SerializeField] private RectTransform orBin;
        [SerializeField] private RectTransform erBin;
        [SerializeField] private Button arBinBtn;
        [SerializeField] private Button orBinBtn;
        [SerializeField] private Button erBinBtn;

        [Header("Spelling Collector Strip on /er/ Bin")]
        [SerializeField] private TextMeshProUGUI erCollectorText;
        private bool foundErSpelling = false;
        private bool foundIrSpelling = false;
        private bool foundUrSpelling = false;
        private bool hasPlayedRevealVoiceline = false;

        [Header("Card & Word UI")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private TextMeshProUGUI cardWordText;
        [SerializeField] private CanvasGroup cardCanvasGroup;
        private Vector2 cardInitialAnchoredPos;
        private Vector3 cardInitialLocalPos;
        private Vector3 dragOffset;
        private Coroutine snapBackCoroutine;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioBtn;

        [Header("SFX")]
        public AudioClip correctSFX;
        public AudioClip wrongSFX;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            StartActivity();
        }

        public void AutoBindHierarchyElements()
        {
            if (items == null || items.Count == 0)
                items = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity2Items();

            Transform root = transform;

            if (arBin == null)
            {
                Transform t = root.Find("Bins/ArBin") ?? root.Find("ArBin") ?? root.Find("LeftBin");
                if (t != null) arBin = t.GetComponent<RectTransform>();
            }
            if (orBin == null)
            {
                Transform t = root.Find("Bins/OrBin") ?? root.Find("OrBin") ?? root.Find("MiddleBin");
                if (t != null) orBin = t.GetComponent<RectTransform>();
            }
            if (erBin == null)
            {
                Transform t = root.Find("Bins/ErBin") ?? root.Find("ErBin") ?? root.Find("RightBin");
                if (t != null) erBin = t.GetComponent<RectTransform>();
            }

            if (arBin != null && arBinBtn == null) arBinBtn = arBin.GetComponentInChildren<Button>(true);
            if (orBin != null && orBinBtn == null) orBinBtn = orBin.GetComponentInChildren<Button>(true);
            if (erBin != null && erBinBtn == null) erBinBtn = erBin.GetComponentInChildren<Button>(true);

            if (erBin != null && erCollectorText == null)
            {
                Transform ct = erBin.Find("CollectorStrip/Text") ?? erBin.Find("FoundSpellingsText") ?? erBin.Find("CollectorText");
                if (ct != null) erCollectorText = ct.GetComponent<TextMeshProUGUI>();
            }

            if (cardContainer == null)
            {
                Transform c = root.Find("CardContainer") ?? root.Find("WordCard") ?? root.Find("Card");
                if (c != null) cardContainer = c.GetComponent<RectTransform>();
            }

            if (cardContainer != null)
            {
                if (cardCanvasGroup == null)
                {
                    cardCanvasGroup = cardContainer.GetComponent<CanvasGroup>();
                    if (cardCanvasGroup == null) cardCanvasGroup = cardContainer.gameObject.AddComponent<CanvasGroup>();
                }
                if (cardWordText == null)
                {
                    Transform wt = cardContainer.Find("WordText") ?? cardContainer.Find("Text");
                    if (wt != null) cardWordText = wt.GetComponent<TextMeshProUGUI>();
                    else cardWordText = cardContainer.GetComponentInChildren<TextMeshProUGUI>();
                }

                if (Application.isPlaying)
                {
                    var proxy = cardContainer.GetComponent<U8_ThreeSounds_CardDragProxy>();
                    if (proxy == null) proxy = cardContainer.gameObject.AddComponent<U8_ThreeSounds_CardDragProxy>();
                    proxy.owner = this;
                }
            }

            if (replayAudioBtn == null)
            {
                Transform r = root.Find("ReplayAudioBtn") 
                           ?? root.Find("ReplayButton") 
                           ?? root.Find("ReplayAudioButton")
                           ?? root.Find("HeaderRibbon/ReplayAudioBtn")
                           ?? root.Find("Speaker_Button");
                if (r != null) replayAudioBtn = r.GetComponent<Button>();
            }

            if (progressText == null)
            {
                Transform pt = root.Find("ProgressHUD/ProgressText") 
                            ?? root.Find("ProgressHUD/Progress_Text") 
                            ?? root.Find("ProgressText")
                            ?? root.Find("HUD/ProgressText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }
            if (scoreText == null)
            {
                Transform st = root.Find("ScoreHUD/ScoreText") 
                            ?? root.Find("ScoreHUD/Score_Text") 
                            ?? root.Find("ScoreText")
                            ?? root.Find("HUD/ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }
            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            AttachListeners();
        }

        private void AttachListeners()
        {
            if (arBinBtn != null)
            {
                arBinBtn.onClick.RemoveAllListeners();
                arBinBtn.onClick.AddListener(() => OnBinSelected(RControlledSound.Sound_AR));
            }
            if (orBinBtn != null)
            {
                orBinBtn.onClick.RemoveAllListeners();
                orBinBtn.onClick.AddListener(() => OnBinSelected(RControlledSound.Sound_OR));
            }
            if (erBinBtn != null)
            {
                erBinBtn.onClick.RemoveAllListeners();
                erBinBtn.onClick.AddListener(() => OnBinSelected(RControlledSound.Sound_ER));
            }

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentWordAudio);
            }
        }

        public void StartActivity()
        {
            currentIndex = 0;
            score = 0;
            firstAttemptCorrectCount = 0;
            isProcessing = false;
            foundErSpelling = false;
            foundIrSpelling = false;
            foundUrSpelling = false;
            hasPlayedRevealVoiceline = false;

            if (cardContainer != null)
            {
                cardInitialAnchoredPos = cardContainer.anchoredPosition;
                cardInitialLocalPos = cardContainer.localPosition;
            }

            UpdateCollectorStripUI();

            AudioClip introClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_a2_intro");
            if (introClip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(introClip);

            LoadCurrentCard();
        }

        private void LoadCurrentCard()
        {
            if (currentIndex >= items.Count)
            {
                StartCoroutine(HandleActivityCompleted());
                return;
            }

            var item = items[currentIndex];
            isFirstAttemptOnItem = true;
            isProcessing = false;

            if (cardContainer != null)
            {
                cardContainer.localPosition = cardInitialLocalPos;
                cardContainer.anchoredPosition = cardInitialAnchoredPos;
                cardContainer.localRotation = Quaternion.identity;
                cardContainer.localScale = Vector3.one;
                if (cardCanvasGroup != null)
                {
                    cardCanvasGroup.alpha = 1f;
                    cardCanvasGroup.blocksRaycasts = true;
                }
            }

            if (cardWordText != null)
            {
                cardWordText.text = $"<b>{item.word}</b>";
            }

            UpdateHUD();
            ReplayCurrentWordAudio();
        }

        public void ReplayCurrentWordAudio()
        {
            if (currentIndex < items.Count)
            {
                var item = items[currentIndex];
                PlayWordAudio(item.word, item.wordAudio);
            }
        }

        private void PlayWordAudio(string word, AudioClip overrideClip)
        {
            if (overrideClip != null)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(overrideClip);
                return;
            }
            AudioClip clip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio($"U08_WRD_{word}") ??
                             U8_SA_AudioManager_Masters_Phonics.ResolveAudio(word);
            if (clip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(clip);
        }

        public void OnBinSelected(RControlledSound chosenSound)
        {
            if (isProcessing || currentIndex >= items.Count) return;
            isProcessing = true;

            var item = items[currentIndex];
            bool isCorrect = (chosenSound == item.targetSound);

            RectTransform targetBin = (chosenSound == RControlledSound.Sound_AR) ? arBin :
                                      (chosenSound == RControlledSound.Sound_OR) ? orBin : erBin;

            if (isCorrect)
            {
                if (isFirstAttemptOnItem) firstAttemptCorrectCount++;
                score += 15;
                UpdateHUD();
                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordWordsRead(1);

                // Update collector strip if this was an /er/ sound
                if (item.targetSound == RControlledSound.Sound_ER)
                {
                    if (item.spellingPattern == RControlledPattern.ER) foundErSpelling = true;
                    if (item.spellingPattern == RControlledPattern.IR) foundIrSpelling = true;
                    if (item.spellingPattern == RControlledPattern.UR) foundUrSpelling = true;
                    UpdateCollectorStripUI();
                }

                StartCoroutine(HandleCorrectSort(targetBin, item));
            }
            else
            {
                isFirstAttemptOnItem = false;
                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordMistake(item.word);

                StartCoroutine(HandleWrongSort(chosenSound, item));
            }
        }

        private void UpdateCollectorStripUI()
        {
            if (erCollectorText != null)
            {
                string erStr = foundErSpelling ? "<color=#00E5FF><b>er</b></color>" : "<color=#475569>er</color>";
                string irStr = foundIrSpelling ? "<color=#FFD700><b>ir</b></color>" : "<color=#475569>ir</color>";
                string urStr = foundUrSpelling ? "<color=#A855F7><b>ur</b></color>" : "<color=#475569>ur</color>";
                erCollectorText.text = $"Found: {erStr}  {irStr}  {urStr}";
            }

            if (!hasPlayedRevealVoiceline && foundErSpelling && foundIrSpelling && foundUrSpelling)
            {
                hasPlayedRevealVoiceline = true;
                AudioClip revClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_a2_reveal");
                if (revClip != null)
                    U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(revClip);
            }
        }

        private IEnumerator HandleCorrectSort(RectTransform targetBin, ThreeSoundsItem item)
        {
            PlaySFX(correctSFX, true);

            if (cardContainer != null && targetBin != null)
            {
                Vector3 startPos = cardContainer.position;
                Vector3 targetPos = targetBin.position;
                float elapsed = 0f;
                float dur = 0.35f;

                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / dur;
                    cardContainer.position = Vector3.Lerp(startPos, targetPos, t);
                    cardContainer.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.3f, t);
                    if (cardCanvasGroup != null) cardCanvasGroup.alpha = 1f - t;
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.4f);

            currentIndex++;
            LoadCurrentCard();
        }

        private IEnumerator HandleWrongSort(RControlledSound chosenSound, ThreeSoundsItem item)
        {
            PlaySFX(wrongSFX, false);

            if (cardContainer != null)
            {
                // Smooth snapback to origin before shake
                yield return SnapBackRoutine();

                // Shake animation around initial origin
                float elapsed = 0f;
                while (elapsed < 0.35f)
                {
                    elapsed += Time.deltaTime;
                    float xOffset = Mathf.Sin(elapsed * 45f) * 16f;
                    cardContainer.anchoredPosition = new Vector2(cardInitialAnchoredPos.x + xOffset, cardInitialAnchoredPos.y);
                    yield return null;
                }
                cardContainer.anchoredPosition = cardInitialAnchoredPos;
                cardContainer.localPosition = cardInitialLocalPos;
            }

            // Play anchor word comparison
            string anchorWord = (chosenSound == RControlledSound.Sound_AR) ? "car" :
                                (chosenSound == RControlledSound.Sound_OR) ? "corn" : "bird";
            PlayWordAudio(item.word, item.wordAudio);
            yield return new WaitForSeconds(0.9f);
            PlayWordAudio(anchorWord, null);
            yield return new WaitForSeconds(0.9f);

            isProcessing = false;
        }

        // =====================================================================
        // Drag Proxy Integration
        // =====================================================================

        public void OnCardBeginDrag(PointerEventData eventData)
        {
            if (isProcessing || cardContainer == null) return;

            if (snapBackCoroutine != null)
            {
                StopCoroutine(snapBackCoroutine);
                snapBackCoroutine = null;
            }

            if (cardCanvasGroup != null) cardCanvasGroup.blocksRaycasts = false;

            Canvas c = GetComponentInParent<Canvas>();
            Camera cam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(cardContainer, eventData.position, cam, out Vector3 worldPoint))
            {
                dragOffset = cardContainer.position - worldPoint;
            }
            else
            {
                dragOffset = Vector3.zero;
            }
        }

        public void OnCardDrag(PointerEventData eventData)
        {
            if (isProcessing || cardContainer == null) return;

            Canvas c = GetComponentInParent<Canvas>();
            Camera cam = (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) ? c.worldCamera : null;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(cardContainer, eventData.position, cam, out Vector3 worldPoint))
            {
                cardContainer.position = worldPoint + dragOffset;
            }
        }

        public void OnCardDragEnd(PointerEventData eventData)
        {
            if (cardCanvasGroup != null) cardCanvasGroup.blocksRaycasts = true;
            if (isProcessing || cardContainer == null) return;

            Camera cam = null;
            Canvas c = GetComponentInParent<Canvas>();
            if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) cam = c.worldCamera;

            if (arBin != null && RectTransformUtility.RectangleContainsScreenPoint(arBin, eventData.position, cam))
            {
                OnBinSelected(RControlledSound.Sound_AR);
            }
            else if (orBin != null && RectTransformUtility.RectangleContainsScreenPoint(orBin, eventData.position, cam))
            {
                OnBinSelected(RControlledSound.Sound_OR);
            }
            else if (erBin != null && RectTransformUtility.RectangleContainsScreenPoint(erBin, eventData.position, cam))
            {
                OnBinSelected(RControlledSound.Sound_ER);
            }
            else
            {
                StartSnapBack();
            }
        }

        public void StartSnapBack()
        {
            if (snapBackCoroutine != null) StopCoroutine(snapBackCoroutine);
            snapBackCoroutine = StartCoroutine(SnapBackRoutine());
        }

        private IEnumerator SnapBackRoutine()
        {
            if (cardContainer == null) yield break;

            Vector3 startPos = cardContainer.position;
            Vector3 targetPos = (cardContainer.parent != null) 
                ? cardContainer.parent.TransformPoint(cardInitialLocalPos) 
                : cardContainer.position;

            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                cardContainer.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }

            cardContainer.localPosition = cardInitialLocalPos;
            cardContainer.anchoredPosition = cardInitialAnchoredPos;
            snapBackCoroutine = null;
        }

        private void PlaySFX(AudioClip clip, bool isCorrect)
        {
            if (clip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX(clip);
            else
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayAnswerFeedbackSFX(isCorrect);
        }

        private void UpdateHUD()
        {
            if (progressBar != null)
            {
                progressBar.maxValue = items.Count;
                progressBar.value = currentIndex + 1;
                U8_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
            }

            if (progressText != null)
                progressText.text = $"<b>Item {currentIndex + 1} of {items.Count}</b>";

            if (scoreText != null)
                scoreText.text = $"Score: <b>{score}</b>";
        }

        private IEnumerator HandleActivityCompleted()
        {
            isProcessing = true;
            if (progressBar != null)
            {
                progressBar.value = items.Count;
            }

            int earnedStars = 1;
            if (firstAttemptCorrectCount >= 16) earnedStars = 3;
            else if (firstAttemptCorrectCount >= 14) earnedStars = 2;

            if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(2, earnedStars);
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(score);
            }

            yield return new WaitForSeconds(0.8f);

            U8_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform,
                "Activity 2 — Three Sounds",
                earnedStars,
                score,
                () => {
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity3();
                }
            );
        }
    }
}
