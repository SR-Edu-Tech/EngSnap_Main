using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U6_SA_GM03s_SoftOrHard_Masters_Phonics : MonoBehaviour
    {
        [Header("Card & Swipe UI")]
        [SerializeField] private RectTransform cardContainer;
        [SerializeField] private TextMeshProUGUI cardWordText;
        [SerializeField] private TextMeshProUGUI cardPhoneticHintText;
        [SerializeField] private CanvasGroup cardCanvasGroup;

        [Header("Target Bins / Direction Indicators")]
        [SerializeField] private RectTransform hardLeftBin;
        [SerializeField] private RectTransform softRightBin;
        [SerializeField] private Button tapHardLeftBtn;
        [SerializeField] private Button tapSoftRightBtn;

        [Header("Rule Banner")]
        [SerializeField] private TextMeshProUGUI ruleBannerText;

        [Header("HUD & Progress")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private TextMeshProUGUI mascotCommentaryText;
        [SerializeField] private GameObject mascotBubble;

        [Header("Audio SFX Clips (Inspector Overrides)")]
        [Tooltip("Played on soft c / soft g match")]
        public AudioClip softSoundSFX;
        [Tooltip("Played on hard c / hard g match")]
        public AudioClip hardSoundSFX;
        [Tooltip("Played on correct swipe")]
        public AudioClip correctSFX;
        [Tooltip("Played on wrong swipe")]
        public AudioClip wrongSFX;

        [Header("Item Pool")]
        [SerializeField] private List<SoftHardSwipeItem> swipeItems = new List<SoftHardSwipeItem>();

        private int currentIndex = 0;
        private int totalScore = 0;
        private int currentStreak = 0;
        private bool isProcessing = false;

        private Vector2 cardInitialPos;
        private static Sprite proceduralRoundedSprite = null;

        public static Sprite GetOrCreateRoundedSprite()
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 30;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius - 1.5f)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
                        colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            Vector4 border = new Vector4(radius, radius, radius, radius);
            proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return proceduralRoundedSprite;
        }

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
            if (swipeItems == null || swipeItems.Count == 0)
            {
                swipeItems = U6_SA_DataTypes_Masters_Phonics.GetDefaultSoftHardItems();
            }

            if (hardLeftBin == null)
            {
                Transform hTr = transform.Find("HardIndicator") ?? transform.Find("LeftBin") ?? transform.Find("HardBin");
                if (hTr != null) hardLeftBin = hTr.GetComponent<RectTransform>();
            }

            if (softRightBin == null)
            {
                Transform sTr = transform.Find("SoftIndicator") ?? transform.Find("RightBin") ?? transform.Find("SoftBin");
                if (sTr != null) softRightBin = sTr.GetComponent<RectTransform>();
            }

            if (tapHardLeftBtn == null && hardLeftBin != null)
            {
                tapHardLeftBtn = hardLeftBin.GetComponent<Button>();
                if (tapHardLeftBtn == null) tapHardLeftBtn = hardLeftBin.gameObject.AddComponent<Button>();
            }

            if (tapSoftRightBtn == null && softRightBin != null)
            {
                tapSoftRightBtn = softRightBin.GetComponent<Button>();
                if (tapSoftRightBtn == null) tapSoftRightBtn = softRightBin.gameObject.AddComponent<Button>();
            }

            if (cardContainer == null)
            {
                Transform cc = transform.Find("CardSpawnZone") ?? transform.Find("CardContainer") ?? transform.Find("Card");
                if (cc != null) cardContainer = cc.GetComponent<RectTransform>();
            }

            if (cardContainer != null)
            {
                if (cardCanvasGroup == null)
                {
                    cardCanvasGroup = cardContainer.GetComponent<CanvasGroup>();
                    if (cardCanvasGroup == null) cardCanvasGroup = cardContainer.gameObject.AddComponent<CanvasGroup>();
                }

                Image cardImg = cardContainer.GetComponent<Image>();
                if (cardImg == null)
                {
                    cardImg = cardContainer.gameObject.AddComponent<Image>();
                    cardImg.sprite = GetOrCreateRoundedSprite();
                    cardImg.type = Image.Type.Sliced;
                    cardImg.color = new Color(0.96f, 0.98f, 1f, 1f);
                }

                if (cardWordText == null)
                {
                    Transform wt = cardContainer.Find("WordText") ?? cardContainer.Find("CardWordText");
                    if (wt != null) cardWordText = wt.GetComponent<TextMeshProUGUI>();
                    else
                    {
                        GameObject wGo = new GameObject("WordText", typeof(RectTransform), typeof(TextMeshProUGUI));
                        wGo.transform.SetParent(cardContainer, false);
                        cardWordText = wGo.GetComponent<TextMeshProUGUI>();
                        cardWordText.alignment = TextAlignmentOptions.Center;
                        cardWordText.fontSize = 56;
                        cardWordText.fontStyle = FontStyles.Bold;
                        cardWordText.color = new Color(0.1f, 0.15f, 0.28f, 1f);
                        RectTransform rt = wGo.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(0f, 20f);
                        rt.sizeDelta = new Vector2(480f, 100f);
                    }
                }

                if (cardPhoneticHintText == null)
                {
                    Transform ht = cardContainer.Find("HintText") ?? cardContainer.Find("CardHintText");
                    if (ht != null) cardPhoneticHintText = ht.GetComponent<TextMeshProUGUI>();
                    else
                    {
                        GameObject hGo = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
                        hGo.transform.SetParent(cardContainer, false);
                        cardPhoneticHintText = hGo.GetComponent<TextMeshProUGUI>();
                        cardPhoneticHintText.alignment = TextAlignmentOptions.Center;
                        cardPhoneticHintText.fontSize = 24;
                        cardPhoneticHintText.color = new Color(0.4f, 0.45f, 0.55f, 1f);
                        RectTransform rt = hGo.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(0f, -50f);
                        rt.sizeDelta = new Vector2(480f, 50f);
                    }
                }

                cardInitialPos = Vector2.zero;
                SetupCardDragHandler();
            }

            if (progressText == null)
            {
                Transform pTxt = transform.Find("ProgressHUD/Progress_Text") 
                              ?? transform.Find("ProgressHUD/ProgressText") 
                              ?? transform.Find("Progress_Text") 
                              ?? transform.Find("ProgressText")
                              ?? transform.Find("ItemCounterText")
                              ?? transform.Find("Counter_Text");
                if (pTxt != null) progressText = pTxt.GetComponent<TextMeshProUGUI>();

                if (progressText == null)
                {
                    TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var tmp in tmps)
                    {
                        string n = tmp.gameObject.name.ToLower();
                        if ((n.Contains("progress") || n.Contains("counter") || n.Contains("count") || n.Contains("item") || n.Contains("card")) &&
                            !n.Contains("score") && !n.Contains("streak") && !n.Contains("title") && !n.Contains("prompt") && !n.Contains("btn") && !n.Contains("button") && !n.Contains("bubble") && !n.Contains("feedback") && !n.Contains("hard") && !n.Contains("soft"))
                        {
                            progressText = tmp;
                            break;
                        }
                    }
                }
            }

            if (progressBar == null)
            {
                Transform pb = transform.Find("ProgressHUD/ProgressBar") ?? transform.Find("ProgressBar");
                if (pb != null) progressBar = pb.GetComponent<Slider>();
            }
            if (progressBar != null) U6_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);

            if (scoreText == null)
            {
                Transform sTxt = transform.Find("ProgressHUD/Score_Text") ?? transform.Find("Score_Text");
                if (sTxt != null) scoreText = sTxt.GetComponent<TextMeshProUGUI>();
            }

            if (streakText == null)
            {
                Transform stTxt = transform.Find("ProgressHUD/Streak_Text") ?? transform.Find("Streak_Text");
                if (stTxt != null) streakText = stTxt.GetComponent<TextMeshProUGUI>();
            }

            if (tapHardLeftBtn != null)
            {
                tapHardLeftBtn.onClick.RemoveAllListeners();
                tapHardLeftBtn.onClick.AddListener(() => OnChoiceSelected(SoftHardType.Hard));
            }

            if (tapSoftRightBtn != null)
            {
                tapSoftRightBtn.onClick.RemoveAllListeners();
                tapSoftRightBtn.onClick.AddListener(() => OnChoiceSelected(SoftHardType.Soft));
            }
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            currentIndex = 0;
            totalScore = 0;
            currentStreak = 0;
            isProcessing = false;

            if (ruleBannerText != null)
            {
                ruleBannerText.text = "<b>c</b> and <b>g</b> go <b><color=#15803D>SOFT</color></b> before <b>e, i, y</b>!";
                ruleBannerText.fontSize = 28;
                ruleBannerText.fontStyle = FontStyles.Bold;
            }

            UpdateHUD();
            LoadItem(0);
        }

        private void LoadItem(int index)
        {
            if (index >= swipeItems.Count)
            {
                FinishActivity();
                return;
            }

            currentIndex = index;
            isProcessing = false;
            SoftHardSwipeItem item = swipeItems[currentIndex];

            // Format card text highlighting the target c/g and the decisive vowel following it
            if (cardWordText != null)
            {
                cardWordText.text = FormatWordHighlight(item);
                cardWordText.fontSize = 52;
                cardWordText.fontStyle = FontStyles.Bold;
                cardWordText.color = new Color(0.1f, 0.15f, 0.28f, 1f);
            }

            if (cardPhoneticHintText != null)
            {
                cardPhoneticHintText.text = $"Letter: <b>{item.targetLetter.ToUpper()}</b>";
            }

            UpdateHUD();

            // Smooth entrance deal animation
            StartCoroutine(AnimateCardEntranceRoutine());

            // Play word audio
            PlayItemWordAudio(item);
        }

        private IEnumerator AnimateCardEntranceRoutine()
        {
            if (cardContainer == null) yield break;

            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = 0f;
                cardCanvasGroup.blocksRaycasts = false;
            }

            Vector2 spawnPos = cardInitialPos + new Vector2(0f, 140f);
            Vector3 startScale = new Vector3(0.85f, 0.85f, 1f);
            Quaternion startRot = Quaternion.Euler(0, 0, Random.Range(-4f, 4f));

            cardContainer.anchoredPosition = spawnPos;
            cardContainer.localScale = startScale;
            cardContainer.localRotation = startRot;

            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3); // Ease-Out Cubic

                cardContainer.anchoredPosition = Vector2.Lerp(spawnPos, cardInitialPos, ease);
                cardContainer.localScale = Vector3.Lerp(startScale, Vector3.one, ease);
                cardContainer.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, ease);

                if (cardCanvasGroup != null)
                {
                    cardCanvasGroup.alpha = Mathf.Clamp01(t * 1.5f);
                }

                yield return null;
            }

            cardContainer.anchoredPosition = cardInitialPos;
            cardContainer.localScale = Vector3.one;
            cardContainer.localRotation = Quaternion.identity;

            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = 1f;
                cardCanvasGroup.blocksRaycasts = true;
            }
        }

        private string FormatWordHighlight(SoftHardSwipeItem item)
        {
            string w = item.word;
            int idx = item.targetLetterIndex;

            if (idx >= 0 && idx < w.Length)
            {
                string target = w[idx].ToString();
                string nextChar = (idx + 1 < w.Length) ? w[idx + 1].ToString() : "";

                if (!string.IsNullOrEmpty(nextChar))
                {
                    // Highlight c/g and following letter
                    return w.Substring(0, idx) 
                         + $"<color=#2563EB><b>{target}</b></color><color=#15803D><b>{nextChar}</b></color>" 
                         + (idx + 2 < w.Length ? w.Substring(idx + 2) : "");
                }
                else
                {
                    return w.Substring(0, idx) + $"<color=#2563EB><b>{target}</b></color>" + w.Substring(idx + 1);
                }
            }
            return w;
        }

        private void SetupCardDragHandler()
        {
            if (cardContainer == null) return;

            var drag = cardContainer.gameObject.GetComponent<SoftHardCardSwipeHandler>();
            if (drag == null) drag = cardContainer.gameObject.AddComponent<SoftHardCardSwipeHandler>();

            drag.Init(this, cardCanvasGroup);
        }

        public void ResetCardPosition()
        {
            if (cardContainer != null)
            {
                cardContainer.anchoredPosition = cardInitialPos;
                cardContainer.localScale = Vector3.one;
                cardContainer.localRotation = Quaternion.identity;

                Image img = cardContainer.GetComponent<Image>();
                if (img != null && img.sprite == null)
                {
                    img.sprite = GetOrCreateRoundedSprite();
                    img.type = Image.Type.Sliced;
                }
            }

            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = 1f;
                cardCanvasGroup.blocksRaycasts = true;
            }
        }

        public void OnChoiceSelected(SoftHardType chosenType)
        {
            if (isProcessing) return;
            isProcessing = true;

            SoftHardSwipeItem item = swipeItems[currentIndex];
            bool isCorrect = (chosenType == item.soundType);

            StartCoroutine(EvaluateChoiceRoutine(chosenType, isCorrect, item));
        }

        private IEnumerator EvaluateChoiceRoutine(SoftHardType chosenType, bool isCorrect, SoftHardSwipeItem item)
        {
            if (cardCanvasGroup != null)
                cardCanvasGroup.blocksRaycasts = false;

            // Throw card completely off-screen and fade it out smoothly
            float throwDirection = (chosenType == SoftHardType.Hard) ? -1f : 1f;
            Vector2 startPos = cardContainer.anchoredPosition;
            Vector2 targetPos = new Vector2(throwDirection * 1500f, startPos.y - 120f);
            Quaternion startRot = cardContainer.localRotation;
            Quaternion targetRot = Quaternion.Euler(0, 0, throwDirection * -35f);

            float throwDuration = 0.22f;
            float elapsed = 0f;

            while (elapsed < throwDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / throwDuration);
                float ease = t * t; // Ease-In acceleration

                cardContainer.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);
                cardContainer.localRotation = Quaternion.Slerp(startRot, targetRot, ease);

                if (cardCanvasGroup != null)
                {
                    cardCanvasGroup.alpha = Mathf.Clamp01(1f - (t * 1.3f));
                }

                yield return null;
            }

            if (cardCanvasGroup != null)
            {
                cardCanvasGroup.alpha = 0f;
            }

            if (isCorrect)
            {
                currentStreak++;
                int points = 40 + (currentStreak >= 3 ? 15 : 0);
                totalScore += points;

                if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U6_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(points);

                if (item.soundType == SoftHardType.Soft)
                {
                    if (softSoundSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                        U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(softSoundSFX);
                    else
                        U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("soften");
                }
                else
                {
                    if (hardSoundSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                        U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(hardSoundSFX);
                    else
                        U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");
                }

                ShowMascotCommentary($"Spot on! In <b>{item.word}</b>, the {item.targetLetter} makes the {item.soundType} sound {item.phonemeSound}!");
            }
            else
            {
                currentStreak = 0;

                if (wrongSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                    U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(wrongSFX);
                else
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");

                ShowMascotCommentary($"Look at the next letter! In <b>{item.word}</b>, it makes the {item.soundType} sound {item.phonemeSound}.");
            }

            UpdateHUD();
            yield return new WaitForSeconds(1.2f);

            LoadItem(currentIndex + 1);
        }

        private void PlayItemWordAudio(SoftHardSwipeItem item)
        {
            if (item == null) return;
            AudioClip clip = item.wordAudio != null ? item.wordAudio : U6_SA_AudioManager_Masters_Phonics.ResolveAudio(item.word);
            if (clip != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            else
                U6_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(item.word);
        }

        private void ShowMascotCommentary(string msg)
        {
            if (mascotCommentaryText != null)
                mascotCommentaryText.text = msg;

            if (mascotBubble != null)
            {
                mascotBubble.SetActive(true);
                StartCoroutine(PunchScale(mascotBubble.transform, 1.05f, 0.12f));
            }
        }

        private void UpdateHUD()
        {
            if (progressText != null)
            {
                progressText.text = $"<b>Card {currentIndex + 1} of {swipeItems.Count}</b>";
                progressText.color = Color.white;
            }

            if (progressBar != null && swipeItems.Count > 0)
                progressBar.value = (float)currentIndex / swipeItems.Count;

            if (scoreText != null)
                scoreText.text = $"Score: {totalScore}";

            if (streakText != null)
                streakText.text = currentStreak > 1 ? $"Streak: {currentStreak}x!" : "";
        }

        private void FinishActivity()
        {
            if (progressBar != null) progressBar.value = 1f;
            U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("activity_complete");

            StartCoroutine(CompleteRoutine());
        }

        private IEnumerator CompleteRoutine()
        {
            yield return new WaitForSeconds(2.0f);
            if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U6_SA_UnitFlowManager_Masters_Phonics.Instance.CompleteCurrentActivity();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign All Items, Sprites & Audio")]
        public void EditorAutoAssignEverything()
        {
            AutoBindHierarchyElements();

            swipeItems = U6_SA_DataTypes_Masters_Phonics.GetDefaultSoftHardItems();

            for (int i = 0; i < swipeItems.Count; i++)
            {
                var item = swipeItems[i];
                if (item == null) continue;
                item.wordAudio = FindAudioInEditor(item.word);
            }

            if (softSoundSFX == null) softSoundSFX = FindAudioInEditor("soften");
            if (hardSoundSFX == null) hardSoundSFX = FindAudioInEditor("correct");
            if (wrongSFX == null) wrongSFX = FindAudioInEditor("wrong");

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log($"<color=#10B981><b>[Soft or Hard] Auto-assigned {swipeItems.Count} items & Audio Clips!</b></color>");
        }

        private AudioClip FindAudioInEditor(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;
            string clean = word.ToLower().Trim();

            if (clean == "correct")
            {
                AudioClip c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3");
                if (c != null) return c;
            }
            else if (clean == "wrong" || clean == "incorrect")
            {
                AudioClip c = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3");
                if (c != null) return c;
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new string[] { "Assets/Audio/U6_audio", "Assets/Audio", "Assets/SFX", "Assets/Resources" });
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (filename == clean || filename == $"u06_wrd_{clean}" || filename == $"u06_sfx_{clean}")
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }

            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string filename = System.IO.Path.GetFileNameWithoutExtension(path).ToLower();
                if (filename.Contains(clean))
                {
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            return null;
        }
#endif

        private IEnumerator PunchScale(Transform target, float scale, float duration)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            target.localScale = original * scale;
            yield return new WaitForSeconds(duration);
            target.localScale = original;
        }
    }

    // -------------------------------------------------------------
    // Helper Card Swipe Handler
    // -------------------------------------------------------------
    public class SoftHardCardSwipeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private U6_SA_GM03s_SoftOrHard_Masters_Phonics parentGame;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private Vector2 dragStartPointerPos;
        private Vector2 dragStartAnchoredPos;
        private Coroutine snapCoroutine;

        public void Init(U6_SA_GM03s_SoftOrHard_Masters_Phonics game, CanvasGroup cg)
        {
            parentGame = game;
            canvasGroup = cg;
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (snapCoroutine != null)
            {
                StopCoroutine(snapCoroutine);
                snapCoroutine = null;
            }

            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

            dragStartPointerPos = eventData.position;
            dragStartAnchoredPos = rectTransform.anchoredPosition;

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

            float scale = (parentCanvas != null && parentCanvas.scaleFactor > 0.001f) ? parentCanvas.scaleFactor : 1f;
            Vector2 delta = (eventData.position - dragStartPointerPos) / scale;

            // Dampen vertical movement so the card stays on the desk table
            float dampY = Mathf.Clamp(delta.y * 0.25f, -60f, 60f);
            rectTransform.anchoredPosition = new Vector2(dragStartAnchoredPos.x + delta.x, dragStartAnchoredPos.y + dampY);

            // Dynamic card tilt based on horizontal drag
            float tiltAngle = Mathf.Clamp(-delta.x * 0.045f, -22f, 22f);
            rectTransform.localRotation = Quaternion.Euler(0, 0, tiltAngle);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            float deltaX = rectTransform.anchoredPosition.x;

            if (deltaX < -120f)
            {
                // Swiped Left -> Hard
                parentGame?.OnChoiceSelected(SoftHardType.Hard);
            }
            else if (deltaX > 120f)
            {
                // Swiped Right -> Soft
                parentGame?.OnChoiceSelected(SoftHardType.Soft);
            }
            else
            {
                // Incomplete swipe -> Smooth Spring Snap-Back to center
                if (snapCoroutine != null) StopCoroutine(snapCoroutine);
                snapCoroutine = StartCoroutine(SmoothSnapBackRoutine());
            }
        }

        private IEnumerator SmoothSnapBackRoutine()
        {
            Vector2 startPos = rectTransform.anchoredPosition;
            Quaternion startRot = rectTransform.localRotation;
            float duration = 0.16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - Mathf.Pow(1f - t, 3); // Ease-Out Cubic

                rectTransform.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, ease);
                rectTransform.localRotation = Quaternion.Slerp(startRot, Quaternion.identity, ease);
                yield return null;
            }

            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;

            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = true;

            snapCoroutine = null;
        }
    }
}
