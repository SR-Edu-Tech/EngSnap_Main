using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U8_SA_GM03_FiveFamilies_Masters_Phonics : MonoBehaviour
    {
        [Header("Round Items")]
        public List<FiveFamiliesItem> roundAItems;
        public List<FiveFamiliesItem> roundBItems;

        private int currentRound = 1;
        private int currentIndex = 0;
        private int score = 0;
        private int firstAttemptCorrectCount = 0;
        private int strikeCountOnCurrentItem = 0;
        private bool isProcessing = false;

        [Header("3 Bins (Spelling Families)")]
        [SerializeField] private RectTransform bin1;
        [SerializeField] private RectTransform bin2;
        [SerializeField] private RectTransform bin3;
        [SerializeField] private Button bin1Btn;
        [SerializeField] private Button bin2Btn;
        [SerializeField] private Button bin3Btn;
        [SerializeField] private TextMeshProUGUI bin1Title;
        [SerializeField] private TextMeshProUGUI bin2Title;
        [SerializeField] private TextMeshProUGUI bin3Title;

        [Header("Card Container")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private TextMeshProUGUI cardWordText;
        [SerializeField] private TextMeshProUGUI cardHintText;
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
            if (roundAItems == null || roundAItems.Count == 0)
                roundAItems = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity3ItemsRoundA();
            if (roundBItems == null || roundBItems.Count == 0)
                roundBItems = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity3ItemsRoundB();

            Transform root = transform;

            if (bin1 == null)
            {
                Transform t = root.Find("Bins/Bin1") ?? root.Find("Bin1") ?? root.Find("LeftBin");
                if (t != null) bin1 = t.GetComponent<RectTransform>();
            }
            if (bin2 == null)
            {
                Transform t = root.Find("Bins/Bin2") ?? root.Find("Bin2") ?? root.Find("MiddleBin");
                if (t != null) bin2 = t.GetComponent<RectTransform>();
            }
            if (bin3 == null)
            {
                Transform t = root.Find("Bins/Bin3") ?? root.Find("Bin3") ?? root.Find("RightBin");
                if (t != null) bin3 = t.GetComponent<RectTransform>();
            }

            if (bin1 != null)
            {
                if (bin1Btn == null) bin1Btn = bin1.GetComponentInChildren<Button>(true);
                if (bin1Title == null) bin1Title = bin1.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (bin2 != null)
            {
                if (bin2Btn == null) bin2Btn = bin2.GetComponentInChildren<Button>(true);
                if (bin2Title == null) bin2Title = bin2.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (bin3 != null)
            {
                if (bin3Btn == null) bin3Btn = bin3.GetComponentInChildren<Button>(true);
                if (bin3Title == null) bin3Title = bin3.GetComponentInChildren<TextMeshProUGUI>(true);
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
                if (cardHintText == null)
                {
                    Transform ht = cardContainer.Find("HintText");
                    if (ht != null) cardHintText = ht.GetComponent<TextMeshProUGUI>();
                }

                if (Application.isPlaying)
                {
                    var proxy = cardContainer.GetComponent<U8_FiveFamilies_CardDragProxy>();
                    if (proxy == null) proxy = cardContainer.gameObject.AddComponent<U8_FiveFamilies_CardDragProxy>();
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
            if (bin1Btn != null)
            {
                bin1Btn.onClick.RemoveAllListeners();
                bin1Btn.onClick.AddListener(() => OnBinSelected(0));
            }
            if (bin2Btn != null)
            {
                bin2Btn.onClick.RemoveAllListeners();
                bin2Btn.onClick.AddListener(() => OnBinSelected(1));
            }
            if (bin3Btn != null)
            {
                bin3Btn.onClick.RemoveAllListeners();
                bin3Btn.onClick.AddListener(() => OnBinSelected(2));
            }

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentWordAudio);
            }
        }

        public void StartActivity()
        {
            currentRound = 1;
            currentIndex = 0;
            score = 0;
            firstAttemptCorrectCount = 0;
            isProcessing = false;

            if (cardContainer != null)
            {
                cardInitialAnchoredPos = cardContainer.anchoredPosition;
                cardInitialLocalPos = cardContainer.localPosition;
            }

            UpdateRoundBins();

            AudioClip introClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_a3_intro");
            if (introClip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(introClip);

            LoadCurrentCard();
        }

        private List<FiveFamiliesItem> GetCurrentRoundItems()
        {
            return (currentRound == 1) ? roundAItems : roundBItems;
        }

        private void UpdateRoundBins()
        {
            if (currentRound == 1)
            {
                if (bin1Title != null) bin1Title.text = "<b><size=120%>ar</size></b>\n<color=#64748B><size=80%>FAMILY</size></color>";
                if (bin2Title != null) bin2Title.text = "<b><size=120%>or</size></b>\n<color=#64748B><size=80%>FAMILY</size></color>";
                if (bin3Title != null) bin3Title.text = "<b><size=120%>er</size></b>\n<color=#64748B><size=80%>FAMILY</size></color>";
            }
            else
            {
                if (bin1Title != null) bin1Title.text = "<b><size=120%>ir</size></b>\n<color=#64748B><size=80%>FAMILY</size></color>";
                if (bin2Title != null) bin2Title.text = "<b><size=120%>ur</size></b>\n<color=#64748B><size=80%>FAMILY</size></color>";
                if (bin3Title != null) bin3Title.text = "<b><size=120%>er</size></b>\n<color=#64748B><size=80%>FAMILY</size></color>";
            }
        }

        private void LoadCurrentCard()
        {
            var items = GetCurrentRoundItems();
            if (currentIndex >= items.Count)
            {
                if (currentRound == 1)
                {
                    StartCoroutine(TransitionToRoundB());
                }
                else
                {
                    StartCoroutine(HandleActivityCompleted());
                }
                return;
            }

            var item = items[currentIndex];
            strikeCountOnCurrentItem = 0;
            isProcessing = false;

            if (cardContainer != null)
            {
                if (snapBackCoroutine != null)
                {
                    StopCoroutine(snapBackCoroutine);
                    snapBackCoroutine = null;
                }
                cardContainer.anchoredPosition = cardInitialAnchoredPos;
                cardContainer.localPosition = cardInitialLocalPos;
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
            if (cardHintText != null)
            {
                cardHintText.text = "";
            }

            UpdateHUD();
            ReplayCurrentWordAudio();
        }

        public void ReplayCurrentWordAudio()
        {
            var items = GetCurrentRoundItems();
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

        public void OnBinSelected(int binIndex)
        {
            var items = GetCurrentRoundItems();
            if (isProcessing || currentIndex >= items.Count) return;
            isProcessing = true;

            var item = items[currentIndex];
            RControlledPattern targetPattern = (currentRound == 1) ?
                (binIndex == 0 ? RControlledPattern.AR : binIndex == 1 ? RControlledPattern.OR : RControlledPattern.ER) :
                (binIndex == 0 ? RControlledPattern.IR : binIndex == 1 ? RControlledPattern.UR : RControlledPattern.ER);

            bool isCorrect = (item.family == targetPattern);
            RectTransform targetBin = (binIndex == 0) ? bin1 : (binIndex == 1) ? bin2 : bin3;

            if (isCorrect)
            {
                if (strikeCountOnCurrentItem == 0) firstAttemptCorrectCount++;
                score += 15;
                UpdateHUD();
                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordWordsRead(1);

                StartCoroutine(HandleCorrectSort(targetBin));
            }
            else
            {
                strikeCountOnCurrentItem++;
                if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordMistake(item.word);

                // Strike-2 hint in Round B: Show first two letters
                if (currentRound == 2 && strikeCountOnCurrentItem >= 2 && cardHintText != null && item.word.Length >= 2)
                {
                    cardHintText.text = $"Hint: Starts with <color=#FFD700><b>{item.word.Substring(0, 2)}</b></color>";
                }

                StartCoroutine(HandleWrongSort());
            }
        }

        private IEnumerator HandleCorrectSort(RectTransform targetBin)
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

            yield return new WaitForSeconds(0.3f);

            currentIndex++;
            LoadCurrentCard();
        }

        private IEnumerator HandleWrongSort()
        {
            PlaySFX(wrongSFX, false);

            if (cardContainer != null)
            {
                yield return SnapBackRoutine();

                float elapsed = 0f;
                while (elapsed < 0.4f)
                {
                    elapsed += Time.deltaTime;
                    float xOffset = Mathf.Sin(elapsed * 45f) * 16f;
                    cardContainer.anchoredPosition = new Vector2(cardInitialAnchoredPos.x + xOffset, cardInitialAnchoredPos.y);
                    yield return null;
                }
                cardContainer.anchoredPosition = cardInitialAnchoredPos;
                cardContainer.localPosition = cardInitialLocalPos;
            }

            yield return new WaitForSeconds(0.4f);
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

            if (bin1 != null && RectTransformUtility.RectangleContainsScreenPoint(bin1, eventData.position, cam))
            {
                OnBinSelected(0);
            }
            else if (bin2 != null && RectTransformUtility.RectangleContainsScreenPoint(bin2, eventData.position, cam))
            {
                OnBinSelected(1);
            }
            else if (bin3 != null && RectTransformUtility.RectangleContainsScreenPoint(bin3, eventData.position, cam))
            {
                OnBinSelected(2);
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

        private IEnumerator TransitionToRoundB()
        {
            isProcessing = true;
            AudioClip rbClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_a3_rb");
            if (rbClip != null)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(rbClip);
                yield return new WaitForSeconds(rbClip.length + 0.5f);
            }

            currentRound = 2;
            currentIndex = 0;
            UpdateRoundBins();
            LoadCurrentCard();
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
            int totalItems = roundAItems.Count + roundBItems.Count;
            int completedItems = ((currentRound - 1) * roundAItems.Count) + currentIndex;

            if (progressBar != null)
            {
                progressBar.maxValue = totalItems;
                progressBar.value = completedItems + 1;
                U8_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
            }

            if (progressText != null)
                progressText.text = $"<b>Item {completedItems + 1} of {totalItems}</b>  <color=#94A3B8>(Round {currentRound}/2)</color>";

            if (scoreText != null)
                scoreText.text = $"Score: <b>{score}</b>";
        }

        private IEnumerator HandleActivityCompleted()
        {
            isProcessing = true;
            if (progressBar != null)
            {
                progressBar.value = roundAItems.Count + roundBItems.Count;
            }

            int earnedStars = 1;
            if (firstAttemptCorrectCount >= 21) earnedStars = 3;
            else if (firstAttemptCorrectCount >= 18) earnedStars = 2;

            if (U8_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(3, earnedStars);
                U8_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(score);
            }

            yield return new WaitForSeconds(0.8f);

            U8_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform,
                "Activity 3 — Five Families",
                earnedStars,
                score,
                () => {
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity4();
                }
            );
        }
    }
}
