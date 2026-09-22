using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U7_SA_GM03_WordFamilies_Masters_Phonics : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Round Configuration (3 Rounds of 4 Bins)")]
        public List<WordFamilyRound> rounds = new List<WordFamilyRound>();

        [Header("UI Containers & HUD")]
        [SerializeField] private TextMeshProUGUI itemCounterText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI roundTitleText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioButton;

        [Header("Card Setup")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private TextMeshProUGUI wordCardTMP;
        [SerializeField] private CanvasGroup cardCanvasGroup;

        [Header("4 Drop Bins")]
        [SerializeField] private RectTransform bin1Transform;
        [SerializeField] private RectTransform bin2Transform;
        [SerializeField] private RectTransform bin3Transform;
        [SerializeField] private RectTransform bin4Transform;

        [SerializeField] private TextMeshProUGUI bin1LabelTMP;
        [SerializeField] private TextMeshProUGUI bin2LabelTMP;
        [SerializeField] private TextMeshProUGUI bin3LabelTMP;
        [SerializeField] private TextMeshProUGUI bin4LabelTMP;

        [Header("Audio SFX Clips")]
        public AudioClip correctSFX;
        public AudioClip wrongSFX;

        [Header("Feedback Colors")]
        [Tooltip("Card color on correct drop")]
        public Color correctColor = new Color(0.2f, 0.82f, 0.45f, 1f);
        [Tooltip("Card color on wrong drop")]
        public Color wrongColor = new Color(0.92f, 0.3f, 0.3f, 1f);
        [Tooltip("Default card background color")]
        public Color defaultCardColor = Color.white;

        private int currentRoundIndex = 0;
        private int currentItemInRound = 0;
        private int totalCompletedItems = 0;
        private int totalItemsAcrossAllRounds = 24;
        private int totalScore = 0;
        private bool isProcessing = false;

        private Vector3 cardOriginLocalPos;
        private Canvas parentCanvas;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            if (rounds == null || rounds.Count == 0)
            {
                rounds = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity2Rounds();
            }

            currentRoundIndex = 0;
            currentItemInRound = 0;
            totalCompletedItems = 0;
            isProcessing = false;

            CalculateTotalItems();
            LoadRound(currentRoundIndex);
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

            if (roundTitleText == null)
            {
                Transform t = transform.Find("Header_Container/Subtitle_Text") 
                           ?? transform.Find("HeaderRibbon/SubtitleText")
                           ?? transform.Find("RoundTitleText")
                           ?? transform.Find("SubtitleText");
                if (t != null) roundTitleText = t.GetComponent<TextMeshProUGUI>();
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
                Transform c = transform.Find("CurrentWordCard") 
                           ?? transform.Find("CardContainer") 
                           ?? transform.Find("WordCardContainer");
                if (c != null) cardContainer = c.GetComponent<RectTransform>();
            }

            if (cardContainer != null)
            {
                cardOriginLocalPos = cardContainer.localPosition;
                if (wordCardTMP == null)
                {
                    wordCardTMP = cardContainer.GetComponentInChildren<TextMeshProUGUI>(true);
                }
                if (cardCanvasGroup == null)
                {
                    cardCanvasGroup = cardContainer.GetComponent<CanvasGroup>();
                    if (cardCanvasGroup == null) cardCanvasGroup = cardContainer.gameObject.AddComponent<CanvasGroup>();
                }

                Image cardImg = cardContainer.GetComponent<Image>();
                if (cardImg != null) cardImg.raycastTarget = true;

                // Attach Card Drag Proxy to ensure card drag events are always captured
                var proxy = cardContainer.GetComponent<U7_WordFamilies_CardDragProxy>();
                if (proxy == null)
                {
                    proxy = cardContainer.gameObject.AddComponent<U7_WordFamilies_CardDragProxy>();
                }
                proxy.owner = this;
            }

            // Bins
            Transform binsGroup = transform.Find("BinsContainer") ?? transform.Find("FourBinsGroup") ?? transform;
            if (bin1Transform == null) bin1Transform = (binsGroup.Find("Bin_1") ?? binsGroup.Find("Bin1"))?.GetComponent<RectTransform>();
            if (bin2Transform == null) bin2Transform = (binsGroup.Find("Bin_2") ?? binsGroup.Find("Bin2"))?.GetComponent<RectTransform>();
            if (bin3Transform == null) bin3Transform = (binsGroup.Find("Bin_3") ?? binsGroup.Find("Bin3"))?.GetComponent<RectTransform>();
            if (bin4Transform == null) bin4Transform = (binsGroup.Find("Bin_4") ?? binsGroup.Find("Bin4"))?.GetComponent<RectTransform>();

            if (bin1LabelTMP == null && bin1Transform != null) bin1LabelTMP = bin1Transform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (bin2LabelTMP == null && bin2Transform != null) bin2LabelTMP = bin2Transform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (bin3LabelTMP == null && bin3Transform != null) bin3LabelTMP = bin3Transform.GetComponentInChildren<TextMeshProUGUI>(true);
            if (bin4LabelTMP == null && bin4Transform != null) bin4LabelTMP = bin4Transform.GetComponentInChildren<TextMeshProUGUI>(true);

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
        }

        private void CalculateTotalItems()
        {
            totalItemsAcrossAllRounds = 0;
            foreach (var r in rounds)
            {
                if (r != null && r.items != null)
                    totalItemsAcrossAllRounds += r.items.Count;
            }
            if (totalItemsAcrossAllRounds == 0) totalItemsAcrossAllRounds = 24;
        }

        public void LoadRound(int roundIdx)
        {
            if (rounds == null || roundIdx >= rounds.Count)
            {
                StartCoroutine(CompleteActivityRoutine());
                return;
            }

            currentRoundIndex = roundIdx;
            currentItemInRound = 0;
            isProcessing = false;

            WordFamilyRound r = rounds[currentRoundIndex];

            // Set Bin Labels
            SetBinLabel(bin1LabelTMP, r.binTeams.Length > 0 ? r.binTeams[0] : VowelTeamType.AI);
            SetBinLabel(bin2LabelTMP, r.binTeams.Length > 1 ? r.binTeams[1] : VowelTeamType.AY);
            SetBinLabel(bin3LabelTMP, r.binTeams.Length > 2 ? r.binTeams[2] : VowelTeamType.EA);
            SetBinLabel(bin4LabelTMP, r.binTeams.Length > 3 ? r.binTeams[3] : VowelTeamType.EE);

            if (roundTitleText != null)
            {
                roundTitleText.text = $"<b>{r.roundName}: Sort by Vowel Team Spelling!</b>";
            }

            LoadCurrentCard();
        }

        private void SetBinLabel(TextMeshProUGUI tmp, VowelTeamType team)
        {
            if (tmp != null)
            {
                tmp.text = $"<b>-{team.ToString().ToLower()}-</b>";
                tmp.color = Color.white;
            }
        }

        public void LoadCurrentCard()
        {
            if (currentRoundIndex >= rounds.Count) return;
            WordFamilyRound r = rounds[currentRoundIndex];

            if (currentItemInRound >= r.items.Count)
            {
                // Round Complete -> Next Round
                StartCoroutine(AdvanceNextRoundRoutine());
                return;
            }

            isProcessing = false;
            WordFamilyItem item = r.items[currentItemInRound];
            ResetCardPosition();

            if (wordCardTMP != null)
            {
                wordCardTMP.text = $"<b>{item.word}</b>";
                wordCardTMP.color = new Color(0.1f, 0.15f, 0.28f, 1f); // Charcoal black
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
                if (img != null)
                {
                    img.color = defaultCardColor;
                    img.raycastTarget = true;
                }
                if (cardCanvasGroup != null)
                {
                    cardCanvasGroup.alpha = 1f;
                    cardCanvasGroup.blocksRaycasts = true;
                }
            }
        }

        // =========================================================================
        // Drag & Drop Handling
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

            int droppedBinIndex = GetDroppedBinIndex(eventData);

            if (droppedBinIndex >= 0)
            {
                StartCoroutine(EvaluateDropRoutine(droppedBinIndex));
            }
            else
            {
                StartCoroutine(SnapBackRoutine());
            }
        }

        private int GetDroppedBinIndex(PointerEventData eventData)
        {
            RectTransform[] bins = new RectTransform[] { bin1Transform, bin2Transform, bin3Transform, bin4Transform };
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
            WordFamilyRound r = rounds[currentRoundIndex];
            WordFamilyItem item = r.items[currentItemInRound];

            VowelTeamType targetTeam = (binIndex < r.binTeams.Length) ? r.binTeams[binIndex] : VowelTeamType.AI;
            bool isCorrect = (item.team == targetTeam);

            RectTransform[] bins = new RectTransform[] { bin1Transform, bin2Transform, bin3Transform, bin4Transform };
            RectTransform targetBin = (binIndex < bins.Length) ? bins[binIndex] : null;

            if (isCorrect)
            {
                totalScore += 40;
                totalCompletedItems++;

                if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(40);
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordWordLearned();
                }

                if (correctSFX != null && U7_SA_AudioManager_Masters_Phonics.Instance != null)
                    U7_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(correctSFX);
                else
                    U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

                var cardImg = cardContainer != null ? cardContainer.GetComponent<Image>() : null;
                if (cardImg != null) cardImg.color = correctColor;

                // Smooth suck into bin
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
                currentItemInRound++;
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

        private IEnumerator AdvanceNextRoundRoutine()
        {
            U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("celebration");
            yield return new WaitForSeconds(1.2f);
            LoadRound(currentRoundIndex + 1);
        }

        private void UpdateHUD()
        {
            if (progressBar != null && totalItemsAcrossAllRounds > 0)
            {
                progressBar.value = (float)totalCompletedItems / totalItemsAcrossAllRounds;
            }

            if (itemCounterText != null)
            {
                itemCounterText.text = $"<b>Item {totalCompletedItems + 1} of {totalItemsAcrossAllRounds}</b>";
                itemCounterText.color = Color.white;
            }

            if (scoreText != null)
            {
                scoreText.text = $"<b>Score: {totalScore}</b>";
                scoreText.color = Color.white;
            }
        }

        private void PlayWordAudio(WordFamilyItem item)
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
            if (currentRoundIndex < rounds.Count && currentItemInRound < rounds[currentRoundIndex].items.Count)
            {
                PlayWordAudio(rounds[currentRoundIndex].items[currentItemInRound]);
            }
        }

        private IEnumerator CompleteActivityRoutine()
        {
            int earnedStars = 3;
            int score = (totalScore > 0) ? totalScore : (totalCompletedItems * 10);

            if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.RecordSectionCompleted(2, earnedStars);
                U7_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(score);
            }

            U7_SA_AudioManager_Masters_Phonics.Instance?.PlayCelebration();
            yield return new WaitForSeconds(1.0f);

            U7_SA_UnitFlowManager_Masters_Phonics.ShowActivityCompletionDialog(
                transform, 
                "Activity 2 — Word Families", 
                earnedStars, 
                score, 
                () => {
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity3();
                }
            );
        }
    }

    public class U7_WordFamilies_CardDragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public U7_SA_GM03_WordFamilies_Masters_Phonics owner;

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
