using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U5_SA_GM03s_DoorOrGate_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Instruction & Audio Clips")]
        [Tooltip("U05_VO_a1_intro: Quick round. Swipe left if the gate's open, right if the door's closed. Look at the last letter.")]
        public AudioClip introInstructionClip;
        [Tooltip("U05_VO_a1_y: Careful - that y is acting as a vowel. It's holding the gate open.")]
        public AudioClip yVowelAsideClip;

        [Header("Audio SFX Clips (Inspector Overrides)")]
        [Tooltip("Plays on correct swipe (Default: Correct SFX)")]
        public AudioClip correctSFX;
        [Tooltip("Plays on wrong swipe (Default: Incorrect SFX)")]
        public AudioClip wrongSFX;
        [Tooltip("Plays on Open Gate swipe (Default: Gate Open SFX)")]
        public AudioClip gateOpenSFX;
        [Tooltip("Plays on Closed Door swipe (Default: Door Slam SFX)")]
        public AudioClip doorSlamSFX;

        [Header("2. UI Headers & Status")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI promptTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI streakTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button replayAudioButton;

        [Header("3. Swipe Anchors & Bins")]
        [SerializeField] private RectTransform cardSpawnAnchor;
        [SerializeField] private RectTransform openGateBin;   // Left target
        [SerializeField] private RectTransform closedDoorBin; // Right target

        private List<DoorOrGateItem> deck;
        private int currentScore = 0;
        private int currentStreak = 0;
        private int currentCardIndex = 0;
        private const int TOTAL_DECK_SIZE = 18;
        private GameObject currentActiveCard;
        private bool isProcessingAnswer = false;
        private bool hasShownYAside = false;
        private Sprite proceduralRoundedSprite;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            currentScore = 0;
            currentStreak = 0;
            currentCardIndex = 0;
            hasShownYAside = false;
            UpdateScoreUI();
            InitializeDeck();
            SpawnNextCard();

            if (U5_SA_AudioManager_Masters_Phonics.Instance != null && introInstructionClip != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(introInstructionClip);
            }
        }

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("Title BG/TitleText") ?? transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (promptTMP == null)
            {
                var t = transform.Find("prompt bg/Prompt_Text") ?? transform.Find("Prompt_Text") ?? transform.Find("PromptText") ?? transform.Find("InstructionText");
                if (t != null) promptTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (scoreTMP == null)
            {
                var t = transform.Find("ScoreText") ?? transform.Find("Score_Text") ?? transform.Find("Score");
                if (t != null) scoreTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (streakTMP == null)
            {
                var t = transform.Find("StreakText") ?? transform.Find("Streak_Text");
                if (t != null) streakTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (progressTMP == null)
            {
                var t = transform.Find("ProgressText") ?? transform.Find("Progress_Text") ?? transform.Find("HUD/ProgressText") ?? transform.Find("ProgressBar/ProgressText");
                if (t != null) progressTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (progressBar == null)
            {
                var t = transform.Find("ProgressBar") ?? transform.Find("Progress_Bar");
                if (t != null) progressBar = t.GetComponent<Slider>();
            }

            if (cardSpawnAnchor == null)
            {
                var t = transform.Find("CardSpawnAnchor") ?? transform.Find("CardAnchor") ?? transform.Find("CenterAnchor");
                if (t != null) cardSpawnAnchor = t.GetComponent<RectTransform>();
            }

            if (openGateBin == null)
            {
                var t = transform.Find("OpenGateIndicator") ?? transform.Find("OpenGateBin") ?? transform.Find("LeftBin");
                if (t != null) openGateBin = t.GetComponent<RectTransform>();
            }

            if (closedDoorBin == null)
            {
                var t = transform.Find("ClosedDoorIndicator") ?? transform.Find("ClosedDoorBin") ?? transform.Find("RightBin");
                if (t != null) closedDoorBin = t.GetComponent<RectTransform>();
            }

            if (replayAudioButton == null)
            {
                var t = transform.Find("ReplayAudioButton") ?? transform.Find("AudioButton") ?? transform.Find("SpeakerButton");
                if (t != null) replayAudioButton = t.GetComponent<Button>();
            }
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayCurrentCardAudio);
            }
        }

        private void InitializeDeck()
        {
            deck = new List<DoorOrGateItem>()
            {
                new DoorOrGateItem { word = "go", doorType = SyllableDoorType.Open, vowelSoundText = "Long o" },
                new DoorOrGateItem { word = "got", doorType = SyllableDoorType.Closed, vowelSoundText = "Short o" },
                new DoorOrGateItem { word = "he", doorType = SyllableDoorType.Open, vowelSoundText = "Long e" },
                new DoorOrGateItem { word = "hen", doorType = SyllableDoorType.Closed, vowelSoundText = "Short e" },
                new DoorOrGateItem { word = "hi", doorType = SyllableDoorType.Open, vowelSoundText = "Long i" },
                new DoorOrGateItem { word = "hip", doorType = SyllableDoorType.Closed, vowelSoundText = "Short i" },
                new DoorOrGateItem { word = "me", doorType = SyllableDoorType.Open, vowelSoundText = "Long e" },
                new DoorOrGateItem { word = "met", doorType = SyllableDoorType.Closed, vowelSoundText = "Short e" },
                new DoorOrGateItem { word = "so", doorType = SyllableDoorType.Open, vowelSoundText = "Long o" },
                new DoorOrGateItem { word = "sob", doorType = SyllableDoorType.Closed, vowelSoundText = "Short o" },
                new DoorOrGateItem { word = "flu", doorType = SyllableDoorType.Open, vowelSoundText = "Long u" },
                new DoorOrGateItem { word = "club", doorType = SyllableDoorType.Closed, vowelSoundText = "Short u" },
                new DoorOrGateItem { word = "we", doorType = SyllableDoorType.Open, vowelSoundText = "Long e" },
                new DoorOrGateItem { word = "wet", doorType = SyllableDoorType.Closed, vowelSoundText = "Short e" },
                new DoorOrGateItem { word = "be", doorType = SyllableDoorType.Open, vowelSoundText = "Long e" },
                new DoorOrGateItem { word = "bed", doorType = SyllableDoorType.Closed, vowelSoundText = "Short e" },
                new DoorOrGateItem { word = "shed", doorType = SyllableDoorType.Closed, vowelSoundText = "Short e" },
                new DoorOrGateItem { word = "by", doorType = SyllableDoorType.Open, vowelSoundText = "Long i (y)", isYVowel = true }
            };

            Shuffle(deck);
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int r = UnityEngine.Random.Range(i, list.Count);
                list[i] = list[r];
                list[r] = temp;
            }
        }

        private void SpawnNextCard()
        {
            isProcessingAnswer = false;

            if (currentActiveCard != null)
            {
                Destroy(currentActiveCard);
                currentActiveCard = null;
            }

            if (currentCardIndex >= deck.Count || currentCardIndex >= TOTAL_DECK_SIZE)
            {
                OnActivityComplete();
                return;
            }

            int totalCards = Mathf.Min(deck != null ? deck.Count : TOTAL_DECK_SIZE, TOTAL_DECK_SIZE);
            if (progressBar != null)
            {
                progressBar.value = (float)currentCardIndex / totalCards;
            }
            if (progressTMP != null)
            {
                progressTMP.text = $"{Mathf.Min(currentCardIndex + 1, totalCards)} / {totalCards}";
            }

            DoorOrGateItem item = deck[currentCardIndex];

            // Trigger Y-vowel voice-over aside if applicable
            if (item.isYVowel && !hasShownYAside && yVowelAsideClip != null)
            {
                hasShownYAside = true;
                if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(yVowelAsideClip);
                }
            }

            Transform parent = (cardSpawnAnchor != null) ? cardSpawnAnchor : transform;
            currentActiveCard = new GameObject("SwipeCard_" + item.word, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            currentActiveCard.transform.SetParent(parent, false);

            RectTransform rt = currentActiveCard.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(480f, 320f);

            Image img = currentActiveCard.GetComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            GameObject textObj = new GameObject("WordText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(currentActiveCard.transform, false);
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(20, 20);
            textRT.offsetMax = new Vector2(-20, -20);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = $"<b><size=68><color=#000000>{item.word}</color></size></b>\n<size=24><color=#0288D1><b>[ <- SWIPE OR TAP -> ]</b></color></size>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            var swiper = currentActiveCard.AddComponent<U5DoorOrGateSwipeHandler>();
            swiper.Initialize(item, this);

            PlayWordAudio(item.word);
        }

        public void OnSwipeDecision(DoorOrGateItem item, SyllableDoorType swipedType, GameObject cardObj)
        {
            if (isProcessingAnswer) return;
            isProcessingAnswer = true;

            if (item.doorType == swipedType)
            {
                StartCoroutine(HandleCorrectSwipe(item, cardObj, swipedType));
            }
            else
            {
                StartCoroutine(HandleWrongSwipe(item, cardObj, swipedType));
            }
        }

        private IEnumerator HandleCorrectSwipe(DoorOrGateItem item, GameObject cardObj, SyllableDoorType swiped)
        {
            currentScore += 100;
            currentStreak++;
            UpdateScoreUI();

            if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U5_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(100);
                if (swiped == SyllableDoorType.Open)
                    U5_SA_UnitFlowManager_Masters_Phonics.Instance.RecordDoorOpened();
                else
                    U5_SA_UnitFlowManager_Masters_Phonics.Instance.RecordDoorClosed();
            }

            PlayCorrectFeedbackAudio(swiped);

            Image img = cardObj != null ? cardObj.GetComponent<Image>() : null;
            if (img != null) img.color = new Color(0.82f, 1f, 0.82f);

            RectTransform rt = cardObj != null ? cardObj.GetComponent<RectTransform>() : null;
            if (rt != null)
            {
                // Smoothly fly card into target bin (left or right)
                float flyElapsed = 0f;
                float flyDuration = 0.22f;
                Vector2 fromPos = rt.anchoredPosition;
                Vector2 targetPos = swiped == SyllableDoorType.Open ? new Vector2(-750f, fromPos.y) : new Vector2(750f, fromPos.y);
                CanvasGroup cg = cardObj.GetComponent<CanvasGroup>();
                if (cg == null) cg = cardObj.AddComponent<CanvasGroup>();

                while (flyElapsed < flyDuration)
                {
                    flyElapsed += Time.deltaTime;
                    float t = flyElapsed / flyDuration;
                    rt.anchoredPosition = Vector2.Lerp(fromPos, targetPos, t);
                    if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, t);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(0.35f);
            currentCardIndex++;
            SpawnNextCard();
        }

        private IEnumerator HandleWrongSwipe(DoorOrGateItem item, GameObject cardObj, SyllableDoorType swiped)
        {
            currentStreak = 0;
            UpdateScoreUI();

            PlayWrongFeedbackAudio();

            Image img = cardObj != null ? cardObj.GetComponent<Image>() : null;
            if (img != null) img.color = new Color(1f, 0.82f, 0.82f);

            RectTransform rt = cardObj != null ? cardObj.GetComponent<RectTransform>() : null;
            if (rt != null)
            {
                Vector2 draggedPos = rt.anchoredPosition;
                // Shake at release position
                for (int i = 0; i < 4; i++)
                {
                    rt.anchoredPosition = draggedPos + new Vector2((i % 2 == 0 ? 1 : -1) * 18f, 0);
                    yield return new WaitForSeconds(0.04f);
                }
                rt.anchoredPosition = draggedPos;

                // Smoothly return card to original center position
                float returnElapsed = 0f;
                float returnDuration = 0.25f;
                Vector2 fromPos = rt.anchoredPosition;
                Quaternion fromRot = rt.localRotation;
                while (returnElapsed < returnDuration)
                {
                    returnElapsed += Time.deltaTime;
                    float t = returnElapsed / returnDuration;
                    rt.anchoredPosition = Vector2.Lerp(fromPos, Vector2.zero, t);
                    rt.localRotation = Quaternion.Lerp(fromRot, Quaternion.identity, t);
                    yield return null;
                }
                rt.anchoredPosition = Vector2.zero;
                rt.localRotation = Quaternion.identity;
                if (img != null) img.color = Color.white;
            }

            yield return new WaitForSeconds(0.7f);

            currentCardIndex++;
            SpawnNextCard();
        }

        private void PlayCorrectFeedbackAudio(SyllableDoorType swiped)
        {
            if (correctSFX != null)
            {
                if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlaySFXClip(correctSFX);
            }
            else if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();
            }

            if (swiped == SyllableDoorType.Open)
            {
                if (gateOpenSFX != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFXClip(gateOpenSFX);
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayGateOpen();
            }
            else
            {
                if (doorSlamSFX != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlaySFXClip(doorSlamSFX);
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayDoorSlam();
            }
        }

        private void PlayWrongFeedbackAudio()
        {
            if (wrongSFX != null)
            {
                if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlaySFXClip(wrongSFX);
            }
            else if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayWrong();
            }
        }

        private void UpdateScoreUI()
        {
            if (scoreTMP != null) scoreTMP.text = $"Score: <b>{currentScore}</b>";
            if (streakTMP != null) streakTMP.text = $"Streak: <b>{currentStreak}</b>";
        }

        private void OnActivityComplete()
        {
            if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U5_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
            }
        }

        public void ReplayCurrentCardAudio()
        {
            if (currentCardIndex < deck.Count)
            {
                PlayWordAudio(deck[currentCardIndex].word);
            }
        }

        private void PlayWordAudio(string word)
        {
            if (string.IsNullOrEmpty(word)) return;
            word = word.Trim().ToLower();

            if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayWordAudio(word);
            }
        }

        private Sprite GetRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 28;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - size / 2f) - (size / 2f - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - size / 2f) - (size / 2f - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    colors[y * size + x] = dist <= radius ? Color.white : Color.clear;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            return proceduralRoundedSprite;
        }

        private IEnumerator PunchScale(Transform tr, float scale)
        {
            if (tr == null) yield break;
            Vector3 orig = Vector3.one;
            float elapsed = 0f;
            float duration = 0.18f;

            while (elapsed < duration)
            {
                if (tr == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                tr.localScale = Vector3.Lerp(orig * scale, orig, t);
                yield return null;
            }
            if (tr != null) tr.localScale = orig;
        }
    }

    public class U5DoorOrGateSwipeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private DoorOrGateItem itemData;
        private U5_SA_GM03s_DoorOrGate_Masters_Phonics manager;
        private RectTransform rt;
        private Vector2 startPos;
        private const float SWIPE_THRESHOLD = 120f;

        public void Initialize(DoorOrGateItem item, U5_SA_GM03s_DoorOrGate_Masters_Phonics mgr)
        {
            itemData = item;
            manager = mgr;
            rt = GetComponent<RectTransform>();
            startPos = rt.anchoredPosition;
        }

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            rt.anchoredPosition += new Vector2(eventData.delta.x, 0f);
            float rot = (rt.anchoredPosition.x / 400f) * -12f;
            rt.localRotation = Quaternion.Euler(0, 0, rot);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (rt.anchoredPosition.x < -SWIPE_THRESHOLD)
            {
                // Swiped Left -> OPEN Gate
                manager.OnSwipeDecision(itemData, SyllableDoorType.Open, gameObject);
            }
            else if (rt.anchoredPosition.x > SWIPE_THRESHOLD)
            {
                // Swiped Right -> CLOSED Door
                manager.OnSwipeDecision(itemData, SyllableDoorType.Closed, gameObject);
            }
            else
            {
                // Snap back
                StartCoroutine(SnapBack());
            }
        }

        private IEnumerator SnapBack()
        {
            float elapsed = 0f;
            float duration = 0.15f;
            Vector2 cur = rt.anchoredPosition;
            Quaternion curRot = rt.localRotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                rt.anchoredPosition = Vector2.Lerp(cur, startPos, elapsed / duration);
                rt.localRotation = Quaternion.Lerp(curRot, Quaternion.identity, elapsed / duration);
                yield return null;
            }
            rt.anchoredPosition = startPos;
            rt.localRotation = Quaternion.identity;
        }
    }
}
