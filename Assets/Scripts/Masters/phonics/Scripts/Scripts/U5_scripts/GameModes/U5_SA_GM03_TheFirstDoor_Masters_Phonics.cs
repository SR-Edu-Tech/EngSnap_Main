using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U5_SA_GM03_TheFirstDoor_Masters_Phonics : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Audio SFX Clips (Inspector Overrides)")]
        [Tooltip("Plays on correct drop (Default: Correct SFX)")]
        public AudioClip correctSFX;
        [Tooltip("Plays on wrong drop (Default: Incorrect SFX)")]
        public AudioClip wrongSFX;
        [Tooltip("Plays on Open Gate drop (Default: Gate Open SFX)")]
        public AudioClip gateOpenSFX;
        [Tooltip("Plays on Closed Door drop (Default: Door Slam SFX)")]
        public AudioClip doorSlamSFX;

        [Header("UI - Word Card")]
        [SerializeField] private GameObject wordCard;
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private CanvasGroup cardCanvasGroup;
        [SerializeField] private TextMeshProUGUI wordText;
        [SerializeField] private TextMeshProUGUI syllableSplitText;
        [SerializeField] private TextMeshProUGUI ruleNoteText;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image cardDoorIcon;

        [Header("UI - Drop Bins")]
        [SerializeField] private RectTransform openBinRect;
        [SerializeField] private RectTransform closedBinRect;
        [SerializeField] private TextMeshProUGUI openBinLabel;
        [SerializeField] private TextMeshProUGUI closedBinLabel;

        [Header("UI - Progress & HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private Button replayAudioBtn;

        [Header("Sprites")]
        [SerializeField] private Sprite openDoorSprite;
        [SerializeField] private Sprite closedDoorSprite;
        [SerializeField] private Sprite normalCardSprite;
        [SerializeField] private Sprite correctCardSprite;
        [SerializeField] private Sprite incorrectCardSprite;

        [Header("Colors")]
        [SerializeField] private Color openColor = new Color(0.2f, 0.7f, 1f, 1f);      // Soft sky blue
        [SerializeField] private Color closedColor = new Color(1f, 0.55f, 0.2f, 1f);   // Warm amber orange
        [SerializeField] private Color correctColor = new Color(0.18f, 0.8f, 0.44f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.92f, 0.3f, 0.3f, 1f);

        [Header("Items Pool")]
        [SerializeField] private List<FirstDoorItem> allItems = new List<FirstDoorItem>();

        // State
        private List<FirstDoorItem> sessionItems = new List<FirstDoorItem>();
        private int currentIndex = 0;
        private int currentStreak = 0;
        private int totalScore = 0;
        private bool isProcessing = false;
        private bool isDragging = false;
        private Vector2 initialCardPos;
        private Canvas parentCanvas;

        private void Awake()
        {
            parentCanvas = GetComponentInParent<Canvas>();
            AutoBindHierarchyElements();

            if (replayAudioBtn != null)
                replayAudioBtn.onClick.AddListener(ReplayCurrentWordAudio);

            BuildDefaultItemsPool();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();

            if (cardRect != null)
                initialCardPos = cardRect.anchoredPosition;

            StartActivity();
        }

        private void AutoBindHierarchyElements()
        {
            Transform fb = transform.Find("FeedbackPanel");
            if (fb != null)
            {
                if (Application.isPlaying)
                    Destroy(fb.gameObject);
                else
                    fb.gameObject.SetActive(false);
            }

            if (openBinRect == null)
            {
                Transform ob = transform.Find("OpenBin") ?? transform.Find("Open_Bin") ?? transform.Find("LeftBin");
                if (ob != null) openBinRect = ob.GetComponent<RectTransform>();
            }
            if (openBinRect != null)
            {
                openBinRect.gameObject.SetActive(true);
                openBinRect.localScale = Vector3.one;
            }

            if (closedBinRect == null)
            {
                Transform cb = transform.Find("ClosedBin") ?? transform.Find("Closed_Bin") ?? transform.Find("RightBin");
                if (cb != null) closedBinRect = cb.GetComponent<RectTransform>();
            }
            if (closedBinRect != null)
            {
                closedBinRect.gameObject.SetActive(true);
                closedBinRect.localScale = Vector3.one;
            }

            if (wordCard == null)
            {
                Transform c = transform.Find("DraggableWordCard") ?? transform.Find("WordCard") ?? transform.Find("Card");
                if (c != null) wordCard = c.gameObject;
            }
            if (wordCard != null)
            {
                if (cardRect == null) cardRect = wordCard.GetComponent<RectTransform>();
                if (cardCanvasGroup == null) cardCanvasGroup = wordCard.GetComponent<CanvasGroup>();
                if (wordText == null)
                {
                    Transform wt = wordCard.transform.Find("WordText");
                    if (wt != null) wordText = wt.GetComponent<TextMeshProUGUI>();
                }
                if (ruleNoteText == null)
                {
                    Transform st = wordCard.transform.Find("SubText");
                    if (st != null) ruleNoteText = st.GetComponent<TextMeshProUGUI>();
                }

                Transform iconTr = wordCard.transform.Find("Icon");
                if (iconTr != null)
                {
                    if (Application.isPlaying)
                        Destroy(iconTr.gameObject);
                    else
                        iconTr.gameObject.SetActive(false);
                }
            }

            if (ruleNoteText != null)
            {
                ruleNoteText.text = "";
                ruleNoteText.gameObject.SetActive(false);
            }
            if (syllableSplitText != null)
            {
                syllableSplitText.text = "";
                syllableSplitText.gameObject.SetActive(false);
            }

            if (progressText == null)
            {
                Transform pt = transform.Find("ProgressText") ?? transform.Find("HUD/ProgressText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }
            if (scoreText == null)
            {
                Transform st = transform.Find("ScoreText") ?? transform.Find("HUD/ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }
            if (progressBar == null)
            {
                Transform pb = transform.Find("ProgressBar") ?? transform.Find("HUD/ProgressBar");
                if (pb != null) progressBar = pb.GetComponent<Slider>();
            }
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            isProcessing = false;
            isDragging = false;
            currentIndex = 0;
            currentStreak = 0;
            totalScore = 0;

            // Pick 20 items from pool and shuffle
            sessionItems = new List<FirstDoorItem>(allItems);
            ShuffleList(sessionItems);
            if (sessionItems.Count > 20)
                sessionItems = sessionItems.GetRange(0, 20);

            UpdateHUD();
            LoadCurrentCard();
        }

        private void LoadCurrentCard()
        {
            if (currentIndex >= sessionItems.Count)
            {
                FinishActivity();
                return;
            }

            isProcessing = false;
            isDragging = false;

            FirstDoorItem item = sessionItems[currentIndex];

            if (cardRect != null)
            {
                cardRect.anchoredPosition = initialCardPos;
                cardRect.localScale = Vector3.one;
                cardRect.localRotation = Quaternion.identity;
            }

            if (cardCanvasGroup != null)
                cardCanvasGroup.alpha = 1f;

            if (wordText != null)
            {
                wordText.text = item.word;
                wordText.alignment = TextAlignmentOptions.Center;
                wordText.gameObject.SetActive(true);
            }

            if (syllableSplitText != null)
            {
                syllableSplitText.text = "";
                syllableSplitText.gameObject.SetActive(false);
            }

            if (ruleNoteText != null)
            {
                ruleNoteText.text = "";
                ruleNoteText.gameObject.SetActive(false);
            }

            if (cardBackground != null && normalCardSprite != null)
                cardBackground.sprite = normalCardSprite;

            if (cardDoorIcon != null)
                cardDoorIcon.gameObject.SetActive(false);

            ResetBinHighlights();
            UpdateHUD();

            // Play word audio
            PlayItemAudio(item);
        }

        private void PlayItemAudio(FirstDoorItem item)
        {
            if (item.wordAudio != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(item.wordAudio);
            }
            else
            {
                AudioClip clip = Resources.Load<AudioClip>($"U5_audio/words/{item.word}");
                if (clip == null) clip = Resources.Load<AudioClip>($"U5_audio/{item.word}");
                if (clip != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(clip);
            }
        }

        public void ReplayCurrentWordAudio()
        {
            if (currentIndex < sessionItems.Count)
            {
                PlayItemAudio(sessionItems[currentIndex]);
                StartCoroutine(PunchScale(replayAudioBtn.transform, 1.15f, 0.15f));
            }
        }

        // -------------------------------------------------------------
        // Drag Handling
        // -------------------------------------------------------------
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isProcessing) return;
            isDragging = true;
            if (cardCanvasGroup != null)
                cardCanvasGroup.blocksRaycasts = false;

            if (cardRect != null)
                StartCoroutine(PunchScale(cardRect, 1.05f, 0.12f));
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || isProcessing) return;

            if (cardRect != null && parentCanvas != null)
            {
                cardRect.anchoredPosition += eventData.delta / parentCanvas.scaleFactor;
                float tilt = Mathf.Clamp((cardRect.anchoredPosition.x - initialCardPos.x) / 35f, -8f, 8f);
                cardRect.localRotation = Quaternion.Euler(0f, 0f, -tilt);
                CheckBinHover();
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging || isProcessing) return;
            isDragging = false;

            if (cardCanvasGroup != null)
                cardCanvasGroup.blocksRaycasts = true;

            EvaluateDrop();
        }

        private void CheckBinHover()
        {
            if (cardRect == null) return;

            bool hoverOpen = IsOverlapping(cardRect, openBinRect);
            bool hoverClosed = IsOverlapping(cardRect, closedBinRect);

            if (openBinRect != null)
            {
                openBinRect.localScale = hoverOpen ? Vector3.one * 1.08f : Vector3.one;
            }
            if (closedBinRect != null)
            {
                closedBinRect.localScale = hoverClosed ? Vector3.one * 1.08f : Vector3.one;
            }
        }

        private bool IsOverlapping(RectTransform card, RectTransform bin)
        {
            if (card == null || bin == null) return false;

            Vector3[] cardCorners = new Vector3[4];
            Vector3[] binCorners = new Vector3[4];
            card.GetWorldCorners(cardCorners);
            bin.GetWorldCorners(binCorners);

            Rect cardWorldRect = new Rect(cardCorners[0].x, cardCorners[0].y,
                cardCorners[2].x - cardCorners[0].x, cardCorners[2].y - cardCorners[0].y);
            Rect binWorldRect = new Rect(binCorners[0].x, binCorners[0].y,
                binCorners[2].x - binCorners[0].x, binCorners[2].y - binCorners[0].y);

            return cardWorldRect.Overlaps(binWorldRect);
        }

        private void EvaluateDrop()
        {
            bool dropOpen = IsOverlapping(cardRect, openBinRect);
            bool dropClosed = IsOverlapping(cardRect, closedBinRect);

            ResetBinHighlights();

            if (dropOpen)
            {
                ProcessAnswer(SyllableDoorType.Open, openBinRect);
            }
            else if (dropClosed)
            {
                ProcessAnswer(SyllableDoorType.Closed, closedBinRect);
            }
            else
            {
                // Returned to center smoothly
                StartCoroutine(SmoothReturnCard());
            }
        }

        private void ProcessAnswer(SyllableDoorType chosenDoor, RectTransform targetBin)
        {
            isProcessing = true;
            FirstDoorItem item = sessionItems[currentIndex];
            bool isCorrect = (chosenDoor == item.firstDoorType);

            if (isCorrect)
            {
                currentStreak++;
                int points = 20 + (currentStreak >= 3 ? 10 : 0);
                totalScore += points;

                if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U5_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(points);

                StartCoroutine(CorrectSequence(item, chosenDoor, targetBin));
            }
            else
            {
                currentStreak = 0;
                StartCoroutine(IncorrectSequence(item));
            }
        }

        private IEnumerator CorrectSequence(FirstDoorItem item, SyllableDoorType chosenDoor, RectTransform targetBin)
        {
            PlayCorrectFeedback(chosenDoor);

            // Smooth snap towards bin center
            if (targetBin != null && cardRect != null)
            {
                Vector3 targetWorld = targetBin.position + new Vector3(0, 30f, 0);
                Vector3 startWorld = cardRect.position;
                Vector3 startScale = cardRect.localScale;
                Quaternion startRot = cardRect.localRotation;
                float elapsed = 0f;
                float duration = 0.25f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float ease = 1f - Mathf.Pow(1f - t, 2f);
                    cardRect.position = Vector3.Lerp(startWorld, targetWorld, ease);
                    cardRect.localScale = Vector3.Lerp(startScale, Vector3.one * 0.75f, ease);
                    cardRect.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, ease);
                    yield return null;
                }
            }

            if (cardDoorIcon != null)
            {
                if (chosenDoor == SyllableDoorType.Open && openDoorSprite != null)
                {
                    cardDoorIcon.sprite = openDoorSprite;
                    cardDoorIcon.gameObject.SetActive(true);
                }
                else if (chosenDoor == SyllableDoorType.Closed && closedDoorSprite != null)
                {
                    cardDoorIcon.sprite = closedDoorSprite;
                    cardDoorIcon.gameObject.SetActive(true);
                }
            }

            // Split Syllable reveal directly on WordText
            if (wordText != null)
            {
                wordText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(chosenDoor == SyllableDoorType.Open ? openColor : closedColor)}><b>{item.firstSyllable}</b></color> | {item.syllableDivision.Substring(item.firstSyllable.Length).TrimStart('-')}";
                wordText.gameObject.SetActive(true);
            }

            if (cardBackground != null && correctCardSprite != null)
                cardBackground.sprite = correctCardSprite;

            yield return new WaitForSeconds(0.9f);

            // Fade card out smoothly
            float fadeElapsed = 0f;
            float fadeDuration = 0.2f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                float t = fadeElapsed / fadeDuration;
                if (cardCanvasGroup != null)
                    cardCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            currentIndex++;
            LoadCurrentCard();
        }

        private IEnumerator IncorrectSequence(FirstDoorItem item)
        {
            PlayWrongFeedback();

            if (cardBackground != null && incorrectCardSprite != null)
                cardBackground.sprite = incorrectCardSprite;

            // Elastic Shake
            Vector2 origin = cardRect.anchoredPosition;
            float shakeDuration = 0.32f;
            float shakeElapsed = 0f;
            while (shakeElapsed < shakeDuration)
            {
                shakeElapsed += Time.deltaTime;
                float progress = shakeElapsed / shakeDuration;
                float offset = Mathf.Sin(progress * Mathf.PI * 6f) * (1f - progress) * 20f;
                cardRect.anchoredPosition = origin + new Vector2(offset, 0f);
                yield return null;
            }
            cardRect.anchoredPosition = origin;

            // Voice hint
            U5_SA_AudioManager_Masters_Phonics.Instance?.PlayVoicePrompt("hint_first_door",
                "Look at the first syllable: does a consonant close it?");

            yield return SmoothReturnCard();

            if (cardBackground != null && normalCardSprite != null)
                cardBackground.sprite = normalCardSprite;

            isProcessing = false;
        }

        private IEnumerator SmoothReturnCard()
        {
            if (cardRect == null) yield break;

            Vector2 startPos = cardRect.anchoredPosition;
            Quaternion startRot = cardRect.localRotation;
            Vector3 startScale = cardRect.localScale;
            float elapsed = 0f;
            float duration = 0.22f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float ease = 1f - Mathf.Pow(1f - t, 3f);
                cardRect.anchoredPosition = Vector2.Lerp(startPos, initialCardPos, ease);
                cardRect.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, ease);
                cardRect.localScale = Vector3.Lerp(startScale, Vector3.one, ease);
                yield return null;
            }
            cardRect.anchoredPosition = initialCardPos;
            cardRect.localRotation = Quaternion.identity;
            cardRect.localScale = Vector3.one;
        }

        private void PlayCorrectFeedback(SyllableDoorType chosenDoor)
        {
            if (correctSFX != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFXClip(correctSFX);
            }
            else
            {
                U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");
            }

            if (chosenDoor == SyllableDoorType.Open)
            {
                if (gateOpenSFX != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFXClip(gateOpenSFX);
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayGateOpenSFX();
            }
            else
            {
                if (doorSlamSFX != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFXClip(doorSlamSFX);
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayDoorSlamSFX();
            }
        }

        private void PlayWrongFeedback()
        {
            if (wrongSFX != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFXClip(wrongSFX);
            }
            else
            {
                U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");
            }
        }

        private void ResetBinHighlights()
        {
            if (openBinRect != null)
            {
                openBinRect.gameObject.SetActive(true);
                openBinRect.localScale = Vector3.one;
            }
            if (closedBinRect != null)
            {
                closedBinRect.gameObject.SetActive(true);
                closedBinRect.localScale = Vector3.one;
            }
        }

        private void UpdateHUD()
        {
            if (progressText != null)
                progressText.text = $"{Mathf.Min(currentIndex + 1, sessionItems.Count)} / {sessionItems.Count}";

            if (progressBar != null && sessionItems.Count > 0)
                progressBar.value = (float)currentIndex / sessionItems.Count;

            if (scoreText != null)
                scoreText.text = $"Score: {totalScore}";

            if (streakText != null)
                streakText.text = currentStreak > 1 ? $"Streak: {currentStreak}x!" : "";
        }

        private void FinishActivity()
        {
            if (progressBar != null) progressBar.value = 1f;
            U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("activity_complete");
            StartCoroutine(CompleteRoutine());
        }

        private IEnumerator CompleteRoutine()
        {
            yield return new WaitForSeconds(2.0f);
            if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U5_SA_UnitFlowManager_Masters_Phonics.Instance.CompleteCurrentActivity();
            }
        }

        private IEnumerator PunchScale(Transform target, float scale, float duration)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            target.localScale = original * scale;
            yield return new WaitForSeconds(duration);
            target.localScale = original;
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        private void BuildDefaultItemsPool()
        {
            if (allItems != null && allItems.Count > 0) return;

            allItems = new List<FirstDoorItem>()
            {
                // Open first syllable
                new FirstDoorItem { word = "bacon", syllableDivision = "ba-con", firstSyllable = "ba", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long a: door is open!" },
                new FirstDoorItem { word = "baby", syllableDivision = "ba-by", firstSyllable = "ba", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long a: open gate!" },
                new FirstDoorItem { word = "began", syllableDivision = "be-gan", firstSyllable = "be", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long e: door is open!" },
                new FirstDoorItem { word = "silent", syllableDivision = "si-lent", firstSyllable = "si", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long i: open gate!" },
                new FirstDoorItem { word = "pilot", syllableDivision = "pi-lot", firstSyllable = "pi", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long i: open gate!" },
                new FirstDoorItem { word = "tiger", syllableDivision = "ti-ger", firstSyllable = "ti", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long i: door is open!" },
                new FirstDoorItem { word = "hotel", syllableDivision = "ho-tel", firstSyllable = "ho", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long o: door is open!" },
                new FirstDoorItem { word = "robot", syllableDivision = "ro-bot", firstSyllable = "ro", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long o: open gate!" },
                new FirstDoorItem { word = "music", syllableDivision = "mu-sic", firstSyllable = "mu", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long u: door is open!" },
                new FirstDoorItem { word = "human", syllableDivision = "hu-man", firstSyllable = "hu", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long u: open gate!" },
                new FirstDoorItem { word = "tulip", syllableDivision = "tu-lip", firstSyllable = "tu", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long u: door is open!" },
                new FirstDoorItem { word = "spider", syllableDivision = "spi-der", firstSyllable = "spi", firstDoorType = SyllableDoorType.Open, vowelRuleNote = "Long i: open gate!" },

                // Closed first syllable
                new FirstDoorItem { word = "basket", syllableDivision = "bas-ket", firstSyllable = "bas", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short a: closed by 's'!" },
                new FirstDoorItem { word = "napkin", syllableDivision = "nap-kin", firstSyllable = "nap", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short a: closed by 'p'!" },
                new FirstDoorItem { word = "rabbit", syllableDivision = "rab-bit", firstSyllable = "rab", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short a: closed by 'b'!" },
                new FirstDoorItem { word = "velvet", syllableDivision = "vel-vet", firstSyllable = "vel", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short e: closed by 'l'!" },
                new FirstDoorItem { word = "helmet", syllableDivision = "hel-met", firstSyllable = "hel", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short e: closed by 'l'!" },
                new FirstDoorItem { word = "muffin", syllableDivision = "muf-fin", firstSyllable = "muf", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short u: closed by 'f'!" },
                new FirstDoorItem { word = "cactus", syllableDivision = "cac-tus", firstSyllable = "cac", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short a: closed by 'c'!" },
                new FirstDoorItem { word = "monster", syllableDivision = "mon-ster", firstSyllable = "mon", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short o: closed by 'n'!" },
                new FirstDoorItem { word = "puppet", syllableDivision = "pup-pet", firstSyllable = "pup", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short u: closed by 'p'!" },
                new FirstDoorItem { word = "traffic", syllableDivision = "traf-fic", firstSyllable = "traf", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short a: closed by 'f'!" },
                new FirstDoorItem { word = "sunset", syllableDivision = "sun-set", firstSyllable = "sun", firstDoorType = SyllableDoorType.Closed, vowelRuleNote = "Short u: closed by 'n'!" }
            };
        }
    }
}
