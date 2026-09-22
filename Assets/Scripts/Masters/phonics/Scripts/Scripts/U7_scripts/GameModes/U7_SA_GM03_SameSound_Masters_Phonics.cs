using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U7_SA_GM03_SameSound_Masters_Phonics : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Item Pool (18 Items: 6 per sound bin)")]
        public List<SameSoundItem> sessionItems = new List<SameSoundItem>();

        [Header("UI Containers & HUD")]
        [SerializeField] private TextMeshProUGUI itemCounterText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioButton;

        [Header("Central Card")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private TextMeshProUGUI wordCardTMP;
        [SerializeField] private CanvasGroup cardCanvasGroup;

        [Header("3 Sound Bins")]
        [SerializeField] private RectTransform binAYTransform;  // /eɪ/ "ay"
        [SerializeField] private RectTransform binEETransform;  // /iː/ "ee"
        [SerializeField] private RectTransform binOHTransform;  // /əʊ/ "oh"

        [SerializeField] private TextMeshProUGUI binAYCollectorTMP;
        [SerializeField] private TextMeshProUGUI binEECollectorTMP;
        [SerializeField] private TextMeshProUGUI binOHCollectorTMP;

        [Header("Audio SFX Clips")]
        public AudioClip correctSFX;
        public AudioClip wrongSFX;
        public AudioClip binCollectSFX;

        [Header("Feedback Colors")]
        [Tooltip("Card color on correct drop")]
        public Color correctColor = new Color(0.2f, 0.82f, 0.45f, 1f);
        [Tooltip("Card color on wrong drop")]
        public Color wrongColor = new Color(0.92f, 0.3f, 0.3f, 1f);
        [Tooltip("Default card background color")]
        public Color defaultCardColor = Color.white;

        private int currentIndex = 0;
        private int totalScore = 0;
        private bool isProcessing = false;

        private HashSet<string> collectedAYSpellings = new HashSet<string>();
        private HashSet<string> collectedEESpellings = new HashSet<string>();
        private HashSet<string> collectedOHSpellings = new HashSet<string>();

        private Vector3 cardOriginLocalPos;
        private Canvas parentCanvas;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            if (sessionItems == null || sessionItems.Count == 0)
            {
                sessionItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity3Items();
            }

            currentIndex = 0;
            isProcessing = false;
            collectedAYSpellings.Clear();
            collectedEESpellings.Clear();
            collectedOHSpellings.Clear();

            UpdateAllCollectors();
            LoadCurrentCard();
        }

        public void AutoBindHierarchyElements()
        {
            parentCanvas = GetComponentInParent<Canvas>();

            if (itemCounterText == null)
            {
                Transform t = transform.Find("ProgressHUD/ProgressText")
                           ?? transform.Find("ProgressHUD/Progress_Text") 
                           ?? transform.Find("ProgressHUD/ItemCounterText") 
                           ?? transform.Find("HUD/ProgressText")
                           ?? transform.Find("HUD/Progress_Text")
                           ?? transform.Find("ItemCounterText")
                           ?? transform.Find("ProgressText")
                           ?? transform.Find("Progress_Text");
                if (t != null) itemCounterText = t.GetComponent<TextMeshProUGUI>();
            }

            if (scoreText == null)
            {
                Transform t = transform.Find("ScoreHUD/ScoreText")
                           ?? transform.Find("ScoreHUD/Score_Text")
                           ?? transform.Find("HUD_Container/Score_Text") 
                           ?? transform.Find("HUD_Container/ScoreText")
                           ?? transform.Find("ProgressHUD/Score_Text") 
                           ?? transform.Find("Score_HUD/Score_Text")
                           ?? transform.Find("HUD/ScoreText")
                           ?? transform.Find("HUD/Score_Text")
                           ?? transform.Find("ScoreText")
                           ?? transform.Find("Score_Text");
                if (t != null) scoreText = t.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                Transform pb = transform.Find("ProgressHUD/ProgressBar") 
                            ?? transform.Find("ProgressHUD/Progress_Bar")
                            ?? transform.Find("HUD/ProgressBar") 
                            ?? transform.Find("ProgressBar")
                            ?? transform.Find("Progress_Bar");
                if (pb != null) progressBar = pb.GetComponent<Slider>();
                if (progressBar == null) progressBar = GetComponentInChildren<Slider>(true);
            }

            if (replayAudioButton == null)
            {
                Transform r = transform.Find("ReplayAudioButton") 
                           ?? transform.Find("ReplayAudioBtn")
                           ?? transform.Find("ReplayButton") 
                           ?? transform.Find("Header_Container/ReplayBtn")
                           ?? transform.Find("HeaderRibbon/ReplayAudioBtn")
                           ?? transform.Find("Speaker_Button")
                           ?? transform.Find("SpeakerButton");
                if (r != null) replayAudioButton = r.GetComponent<Button>();
                if (replayAudioButton == null)
                {
                    var allBtns = GetComponentsInChildren<Button>(true);
                    foreach (var b in allBtns)
                    {
                        string bName = b.gameObject.name.ToLower();
                        if (bName.Contains("speaker") || bName.Contains("replay") || bName.Contains("audio"))
                        {
                            replayAudioButton = b;
                            break;
                        }
                    }
                }
            }

            if (cardContainer == null)
            {
                Transform c = transform.Find("AuditoryWordCard") 
                           ?? transform.Find("CardContainer") 
                           ?? transform.Find("WordCardContainer");
                if (c != null) cardContainer = c.GetComponent<RectTransform>();
            }

            if (cardContainer != null)
            {
                cardOriginLocalPos = cardContainer.localPosition;
                if (wordCardTMP == null) wordCardTMP = cardContainer.GetComponentInChildren<TextMeshProUGUI>(true);
                if (cardCanvasGroup == null)
                {
                    cardCanvasGroup = cardContainer.GetComponent<CanvasGroup>();
                    if (cardCanvasGroup == null) cardCanvasGroup = cardContainer.gameObject.AddComponent<CanvasGroup>();
                }

                Image cardImg = cardContainer.GetComponent<Image>();
                if (cardImg != null) cardImg.raycastTarget = true;

                var proxy = cardContainer.GetComponent<U7_SameSound_CardDragProxy>();
                if (proxy == null)
                {
                    proxy = cardContainer.gameObject.AddComponent<U7_SameSound_CardDragProxy>();
                }
                proxy.owner = this;
            }

            Transform binsGroup = transform.Find("SoundBinsContainer") ?? transform.Find("BinsContainer") ?? transform.Find("ThreeBinsGroup") ?? transform;
            if (binAYTransform == null) binAYTransform = (binsGroup.Find("SoundBin_1") ?? binsGroup.Find("BinAY") ?? binsGroup.Find("Bin1"))?.GetComponent<RectTransform>();
            if (binEETransform == null) binEETransform = (binsGroup.Find("SoundBin_2") ?? binsGroup.Find("BinEE") ?? binsGroup.Find("Bin2"))?.GetComponent<RectTransform>();
            if (binOHTransform == null) binOHTransform = (binsGroup.Find("SoundBin_3") ?? binsGroup.Find("BinOH") ?? binsGroup.Find("Bin3"))?.GetComponent<RectTransform>();

            if (binAYCollectorTMP == null && binAYTransform != null) 
                binAYCollectorTMP = binAYTransform.Find("CollectorStrip/Text")?.GetComponent<TextMeshProUGUI>() ?? binAYTransform.Find("CollectedText")?.GetComponent<TextMeshProUGUI>();
            if (binEECollectorTMP == null && binEETransform != null) 
                binEECollectorTMP = binEETransform.Find("CollectorStrip/Text")?.GetComponent<TextMeshProUGUI>() ?? binEETransform.Find("CollectedText")?.GetComponent<TextMeshProUGUI>();
            if (binOHCollectorTMP == null && binOHTransform != null) 
                binOHCollectorTMP = binOHTransform.Find("CollectorStrip/Text")?.GetComponent<TextMeshProUGUI>() ?? binOHTransform.Find("CollectedText")?.GetComponent<TextMeshProUGUI>();

            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayCurrentWordAudio);
            }

            if (progressBar != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
            }

            EnsureSFXClips();
        }

        private void EnsureSFXClips()
        {
#if UNITY_EDITOR
            if (correctSFX == null)
                correctSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3");
            if (wrongSFX == null)
                wrongSFX = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3");
#endif
            if (correctSFX == null) correctSFX = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Correct");
            if (wrongSFX == null) wrongSFX = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("Incorrect");
            if (binCollectSFX == null) binCollectSFX = U7_SA_AudioManager_Masters_Phonics.ResolveAudio("U07_SFX_bin_collect") ?? U7_SA_AudioManager_Masters_Phonics.ResolveAudio("bin_collect");
        }

        public void LoadCurrentCard()
        {
            if (sessionItems == null || sessionItems.Count == 0) return;

            if (currentIndex >= sessionItems.Count)
            {
                StartCoroutine(CompleteActivityRoutine());
                return;
            }

            isProcessing = false;
            SameSoundItem item = sessionItems[currentIndex];
            ResetCardPosition();

            if (wordCardTMP != null)
            {
                wordCardTMP.text = $"<b>{item.word}</b>";
                wordCardTMP.color = new Color(0.1f, 0.15f, 0.28f, 1f); // Solid charcoal black
            }

            UpdateHUD();
            PlayWordAudio(item);
        }

        private void ResetCardPosition()
        {
            if (cardContainer != null)
            {
                cardContainer.localPosition = cardOriginLocalPos;
                cardContainer.localScale = Vector3.one;
                cardContainer.localRotation = Quaternion.identity;
                var img = cardContainer.GetComponent<Image>();
                if (img != null) img.color = defaultCardColor;
                if (cardCanvasGroup != null)
                {
                    cardCanvasGroup.alpha = 1f;
                    cardCanvasGroup.blocksRaycasts = true;
                }
            }
        }

        // =========================================================================
        // Drag & Drop
        // =========================================================================

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isProcessing || cardContainer == null) return;
            if (cardCanvasGroup != null) cardCanvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isProcessing || cardContainer == null) return;

            Camera cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) 
                ? parentCanvas.worldCamera 
                : null;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(cardContainer, eventData.position, cam, out Vector3 worldPoint))
            {
                cardContainer.position = worldPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isProcessing || cardContainer == null) return;
            if (cardCanvasGroup != null) cardCanvasGroup.blocksRaycasts = true;

            int droppedBin = GetDroppedBinIndex(eventData);

            if (droppedBin >= 0)
            {
                StartCoroutine(EvaluateDropRoutine(droppedBin));
            }
            else
            {
                StartCoroutine(SnapBackRoutine());
            }
        }

        private int GetDroppedBinIndex(PointerEventData eventData)
        {
            RectTransform[] bins = new RectTransform[] { binAYTransform, binEETransform, binOHTransform };
            Camera cam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay) 
                ? parentCanvas.worldCamera 
                : null;

            for (int i = 0; i < bins.Length; i++)
            {
                if (bins[i] != null && RectTransformUtility.RectangleContainsScreenPoint(bins[i], eventData.position, cam))
                {
                    return i;
                }
            }
            return -1;
        }

        private IEnumerator EvaluateDropRoutine(int binIndex)
        {
            isProcessing = true;
            SameSoundItem item = sessionItems[currentIndex];
            SoundCategoryType targetSound = (binIndex == 0) ? SoundCategoryType.LongA_AY 
                                          : (binIndex == 1) ? SoundCategoryType.LongE_EE 
                                          : SoundCategoryType.LongO_OH;
            bool isCorrect = (item.targetSound == targetSound);

            RectTransform[] bins = new RectTransform[] { binAYTransform, binEETransform, binOHTransform };
            RectTransform targetBin = (binIndex < bins.Length) ? bins[binIndex] : null;

            if (isCorrect)
            {
                totalScore += 30;
                if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(30);
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordWordLearned();
                }

                // Add to collector strip
                AddSpellingToCollector(binIndex, item.spellingTeam);

                if (correctSFX != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(correctSFX);
                else
                    U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

                // Play collector chime
                if (binCollectSFX != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(binCollectSFX);
                }

                var cardImg = cardContainer != null ? cardContainer.GetComponent<Image>() : null;
                if (cardImg != null) cardImg.color = correctColor;

                if (targetBin != null && cardContainer != null)
                {
                    Vector3 startPos = cardContainer.position;
                    Vector3 endPos = targetBin.position;
                    float elapsed = 0f;
                    float duration = 0.2f;

                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float t = elapsed / duration;
                        cardContainer.position = Vector3.Lerp(startPos, endPos, t);
                        cardContainer.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.5f, 0.5f, 1f), t);
                        if (cardCanvasGroup != null) cardCanvasGroup.alpha = 1f - t;
                        yield return null;
                    }
                }

                yield return new WaitForSeconds(0.4f);
                currentIndex++;
                LoadCurrentCard();
                isProcessing = false;
            }
            else
            {
                var cardImg = cardContainer != null ? cardContainer.GetComponent<Image>() : null;
                if (cardImg != null) cardImg.color = wrongColor;

                if (wrongSFX != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(wrongSFX);
                else
                    U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");

                yield return SnapBackRoutine();
                if (cardImg != null) cardImg.color = defaultCardColor;
                isProcessing = false;
            }

            UpdateHUD();
        }

        private void AddSpellingToCollector(int binIdx, string spelling)
        {
            if (string.IsNullOrEmpty(spelling)) return;

            if (binIdx == 0) collectedAYSpellings.Add(spelling);
            else if (binIdx == 1) collectedEESpellings.Add(spelling);
            else if (binIdx == 2) collectedOHSpellings.Add(spelling);

            UpdateAllCollectors();
        }

        private void UpdateAllCollectors()
        {
            if (binAYCollectorTMP != null)
            {
                string s = string.Join("  ", collectedAYSpellings);
                binAYCollectorTMP.text = string.IsNullOrEmpty(s) ? "<color=#888888>ai  ay  ei</color>" : $"<b>{s}</b>";
            }

            if (binEECollectorTMP != null)
            {
                string s = string.Join("  ", collectedEESpellings);
                binEECollectorTMP.text = string.IsNullOrEmpty(s) ? "<color=#888888>ee  ea  ey</color>" : $"<b>{s}</b>";
            }

            if (binOHCollectorTMP != null)
            {
                string s = string.Join("  ", collectedOHSpellings);
                binOHCollectorTMP.text = string.IsNullOrEmpty(s) ? "<color=#888888>oa  ow  oe</color>" : $"<b>{s}</b>";
            }
        }

        private IEnumerator SnapBackRoutine()
        {
            if (cardContainer == null) yield break;

            Vector3 startLocal = cardContainer.localPosition;
            float elapsed = 0f;
            float duration = 0.18f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                cardContainer.localPosition = Vector3.Lerp(startLocal, cardOriginLocalPos, t * t);
                yield return null;
            }
            cardContainer.localPosition = cardOriginLocalPos;
            isProcessing = false;
        }

        private void UpdateHUD()
        {
            if (progressBar != null && sessionItems.Count > 0)
            {
                progressBar.value = (float)currentIndex / sessionItems.Count;
            }

            if (itemCounterText != null && sessionItems.Count > 0)
            {
                itemCounterText.text = $"<b>Item {currentIndex + 1} of {sessionItems.Count}</b>";
                itemCounterText.color = Color.white;
            }

            if (scoreText != null)
            {
                scoreText.text = $"<b>Score: {totalScore}</b>";
                scoreText.color = Color.white;
            }
        }

        private void PlayWordAudio(SameSoundItem item)
        {
            if (item == null) return;
            AudioClip clip = item.wordAudio != null ? item.wordAudio : U7_SA_AudioManager_Masters_Phonics.ResolveAudio(item.word);
            if (clip != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U7_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            }
        }

        public void ReplayCurrentWordAudio()
        {
            if (currentIndex >= 0 && currentIndex < sessionItems.Count)
            {
                PlayWordAudio(sessionItems[currentIndex]);
            }
        }

        private IEnumerator CompleteActivityRoutine()
        {
            int earnedStars = 3;
            int score = totalScore > 0 ? totalScore : (sessionItems.Count * 10);

            if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(3, earnedStars);
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(score);
            }

            U7_SA_AudioManager_Masters_Phonics.Instance?.PlayCelebration();
            yield return new WaitForSeconds(1.0f);

            U7_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform, 
                "Activity 3 — Same Sound", 
                earnedStars, 
                score, 
                () => {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity4();
                }
            );
        }
    }

    public class U7_SameSound_CardDragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public U7_SA_GM03_SameSound_Masters_Phonics owner;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner != null) owner.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner != null) owner.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner != null) owner.OnEndDrag(eventData);
        }
    }
}
