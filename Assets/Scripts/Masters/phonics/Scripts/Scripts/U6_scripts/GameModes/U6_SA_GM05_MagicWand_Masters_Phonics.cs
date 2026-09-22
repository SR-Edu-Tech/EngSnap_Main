using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MastersPhonics
{
    public class U6_SA_GM05_MagicWand_Masters_Phonics : MonoBehaviour
    {
        [Header("Transformation Display UI")]
        [SerializeField] private Image illustrationImage;
        [SerializeField] private TextMeshProUGUI wordDisplayText;
        [SerializeField] private RectTransform dropSlot;
        [SerializeField] private RectTransform draggableETile;
        [SerializeField] private CanvasGroup draggableCanvasGroup;

        [Header("Sound Check Choice UI (2 Options: Short vs Long)")]
        [SerializeField] private GameObject soundCheckContainer;
        [SerializeField] private TextMeshProUGUI soundCheckPromptText;
        [SerializeField] private Button shortSoundButton;
        [SerializeField] private Button longSoundButton;
        [SerializeField] private TextMeshProUGUI shortSoundButtonText;
        [SerializeField] private TextMeshProUGUI longSoundButtonText;

        [Header("HUD & Progress")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI mascotCommentaryText;
        [SerializeField] private GameObject mascotBubble;
        [SerializeField] private Button replayAudioBtn;

        [Header("Audio SFX Clips (Inspector Overrides)")]
        [Tooltip("Played when the magic e tile lands")]
        public AudioClip wandSparkleSFX;
        [Tooltip("Played when the vowel stretches")]
        public AudioClip vowelStretchSFX;
        [Tooltip("Played when correct vowel sound choice is picked")]
        public AudioClip correctSFX;
        [Tooltip("Played when wrong choice is picked")]
        public AudioClip wrongSFX;

        [Header("Item Pool")]
        [SerializeField] private List<WandTransformationItem> transformationItems = new List<WandTransformationItem>();

        private int currentIndex = 0;
        private int totalScore = 0;
        private int correctSoundChecks = 0;
        private bool isETilePlaced = false;
        private bool isProcessing = false;

        private Vector2 initialTilePos;
        private Color defaultShortBtnColor = Color.white;
        private Color defaultLongBtnColor = Color.white;
        private bool defaultColorsCaptured = false;
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
            if (transformationItems == null || transformationItems.Count == 0)
            {
                transformationItems = U6_SA_DataTypes_Masters_Phonics.GetDefaultTransformationItems();
            }

            if (illustrationImage == null)
            {
                Transform pic = transform.Find("TransformationCard/MorphPicture") ?? transform.Find("MorphPicture");
                if (pic != null) illustrationImage = pic.GetComponent<Image>();
            }

            if (wordDisplayText == null)
            {
                Transform wrd = transform.Find("TransformationCard/WordRow/BeforeWordText") 
                             ?? transform.Find("WordRow/BeforeWordText") 
                             ?? transform.Find("BeforeWordText");
                if (wrd != null) wordDisplayText = wrd.GetComponent<TextMeshProUGUI>();
            }

            if (dropSlot == null)
            {
                Transform slot = transform.Find("TransformationCard/WordRow/DropSlot") 
                              ?? transform.Find("WordRow/DropSlot") 
                              ?? transform.Find("DropSlot")
                              ?? transform.Find("TransformationCard/DropSlot")
                              ?? transform.Find("Slot");
                if (slot != null) dropSlot = slot.GetComponent<RectTransform>();
                else if (wordDisplayText != null) dropSlot = wordDisplayText.GetComponent<RectTransform>();
            }

            if (draggableETile == null)
            {
                Transform tile = transform.Find("TileSpawnZone/MagicETile") 
                              ?? transform.Find("MagicETile");
                if (tile != null) draggableETile = tile.GetComponent<RectTransform>();
            }

            if (draggableCanvasGroup == null && draggableETile != null)
            {
                draggableCanvasGroup = draggableETile.GetComponent<CanvasGroup>();
                if (draggableCanvasGroup == null) draggableCanvasGroup = draggableETile.gameObject.AddComponent<CanvasGroup>();
            }

            if (soundCheckContainer == null)
            {
                Transform sc = transform.Find("VowelSoundCheckContainer") ?? transform.Find("SoundCheckContainer");
                if (sc != null) soundCheckContainer = sc.gameObject;
            }

            if (soundCheckContainer != null)
            {
                if (shortSoundButton == null)
                {
                    Transform sBtn = soundCheckContainer.transform.Find("ShortVowelBtn") ?? soundCheckContainer.transform.Find("ShortBtn");
                    if (sBtn != null) shortSoundButton = sBtn.GetComponent<Button>();
                }
                if (longSoundButton == null)
                {
                    Transform lBtn = soundCheckContainer.transform.Find("LongVowelBtn") ?? soundCheckContainer.transform.Find("LongBtn");
                    if (lBtn != null) longSoundButton = lBtn.GetComponent<Button>();
                }
                if (shortSoundButton != null && shortSoundButtonText == null)
                {
                    shortSoundButtonText = shortSoundButton.GetComponentInChildren<TextMeshProUGUI>(true);
                }
                if (longSoundButton != null && longSoundButtonText == null)
                {
                    longSoundButtonText = longSoundButton.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }

            if (replayAudioBtn == null)
            {
                Transform rBtn = transform.Find("ReplayAudioButton") ?? transform.Find("AudioButton") ?? transform.Find("ReplayBtn");
                if (rBtn != null) replayAudioBtn = rBtn.GetComponent<Button>();
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
                        if ((n.Contains("progress") || n.Contains("counter") || n.Contains("count") || n.Contains("item")) &&
                            !n.Contains("score") && !n.Contains("streak") && !n.Contains("title") && !n.Contains("prompt") && !n.Contains("btn") && !n.Contains("button") && !n.Contains("bubble") && !n.Contains("feedback") && !n.Contains("sound"))
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

            if (shortSoundButton != null)
            {
                if (!defaultColorsCaptured && shortSoundButton.image != null) defaultShortBtnColor = shortSoundButton.image.color;
                shortSoundButton.onClick.RemoveAllListeners();
                shortSoundButton.onClick.AddListener(() => OnSoundCheckSelected(false));
            }

            if (longSoundButton != null)
            {
                if (!defaultColorsCaptured && longSoundButton.image != null) defaultLongBtnColor = longSoundButton.image.color;
                longSoundButton.onClick.RemoveAllListeners();
                longSoundButton.onClick.AddListener(() => OnSoundCheckSelected(true));
            }
            defaultColorsCaptured = true;

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
            }

            SetupDraggableETile();
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            currentIndex = 0;
            totalScore = 0;
            correctSoundChecks = 0;
            isProcessing = false;

            if (draggableETile != null)
            {
                initialTilePos = draggableETile.anchoredPosition;
            }

            UpdateHUD();
            LoadItem(0);
        }

        private static Dictionary<string, Sprite> runtimeSpriteCache = null;

        public static Sprite ResolveSprite(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;

            if (runtimeSpriteCache == null)
            {
                runtimeSpriteCache = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
                Sprite[] allSprites = Resources.FindObjectsOfTypeAll<Sprite>();
                foreach (var s in allSprites)
                {
                    if (s == null) continue;
                    string sName = s.name.ToLower();
                    if (!runtimeSpriteCache.ContainsKey(sName)) runtimeSpriteCache[sName] = s;

                    string cleanName = sName.Replace("u6_pic_", "").Replace("u6_ui_", "").Replace("pic_", "").Trim();
                    if (!string.IsNullOrEmpty(cleanName) && !runtimeSpriteCache.ContainsKey(cleanName))
                        runtimeSpriteCache[cleanName] = s;
                }
            }

            string clean = word.ToLower().Trim();
            if (runtimeSpriteCache.TryGetValue($"u6_pic_{clean}", out Sprite sprExact)) return sprExact;
            if (runtimeSpriteCache.TryGetValue(clean, out Sprite sprClean)) return sprClean;

            foreach (var kvp in runtimeSpriteCache)
            {
                if (kvp.Key.Contains(clean) || clean.Contains(kvp.Key))
                    return kvp.Value;
            }

            return null;
        }

        private void LoadItem(int index)
        {
            if (index >= transformationItems.Count)
            {
                FinishActivity();
                return;
            }

            currentIndex = index;
            isETilePlaced = false;
            isProcessing = false;

            WandTransformationItem item = transformationItems[currentIndex];

            if (soundCheckContainer != null)
                soundCheckContainer.SetActive(false);

            if (dropSlot != null)
            {
                dropSlot.gameObject.SetActive(true);
                Image slotImg = dropSlot.GetComponent<Image>();
                if (slotImg != null)
                {
                    if (slotImg.sprite == null) slotImg.sprite = GetOrCreateRoundedSprite();
                    slotImg.type = Image.Type.Sliced;
                    slotImg.color = new Color(1f, 1f, 1f, 0.22f); // Clean semi-transparent rounded box
                }
            }

            if (wordDisplayText != null)
            {
                wordDisplayText.text = $"<b>{item.beforeWord}</b>";
                wordDisplayText.fontSize = 54;
                wordDisplayText.fontStyle = FontStyles.Bold;
                wordDisplayText.color = new Color(0.12f, 0.16f, 0.28f, 1f);
            }

            if (illustrationImage != null)
            {
                Sprite beforeSpr = item.beforeSprite != null ? item.beforeSprite : ResolveSprite(item.beforeWord);
                if (beforeSpr != null)
                {
                    illustrationImage.sprite = beforeSpr;
                    illustrationImage.gameObject.SetActive(true);
                }
                else
                {
                    illustrationImage.gameObject.SetActive(false);
                }
            }

            ResetDraggableETile();
            UpdateHUD();

            // Play before word audio
            PlayBeforeWordAudio(item);

            ShowMascotCommentary($"Tap or Drag the Magic 'e' to transform <b>{item.beforeWord}</b>!");
        }

        private void SetupDraggableETile()
        {
            if (draggableETile == null) return;

            // Ensure child text does not block raycasts from parent draggable tile
            var tmpTexts = draggableETile.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmpTexts)
            {
                if (t != null) t.raycastTarget = false;
            }

            var img = draggableETile.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            var dragHandler = draggableETile.gameObject.GetComponent<WandDraggableETile>();
            if (dragHandler == null) dragHandler = draggableETile.gameObject.AddComponent<WandDraggableETile>();

            dragHandler.Init(this, draggableCanvasGroup, dropSlot);
        }

        public void ResetDraggableETile()
        {
            if (draggableETile != null)
            {
                draggableETile.anchoredPosition = initialTilePos;
                draggableETile.gameObject.SetActive(true);

                var tmpTexts = draggableETile.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmpTexts)
                {
                    if (t != null) t.raycastTarget = false;
                }

                Image img = draggableETile.GetComponent<Image>();
                if (img != null)
                {
                    img.raycastTarget = true;
                    if (img.sprite == null)
                    {
                        img.sprite = GetOrCreateRoundedSprite();
                        img.type = Image.Type.Sliced;
                    }
                }

                if (draggableCanvasGroup != null)
                {
                    draggableCanvasGroup.alpha = 1f;
                    draggableCanvasGroup.blocksRaycasts = true;
                }

                var dragHandler = draggableETile.GetComponent<WandDraggableETile>();
                if (dragHandler != null) dragHandler.Init(this, draggableCanvasGroup, dropSlot);
            }
        }

        public void OnETileDroppedSuccessfully()
        {
            if (isETilePlaced || isProcessing) return;
            isETilePlaced = true;
            isProcessing = true;

            StartCoroutine(TransformationSequenceRoutine());
        }

        private IEnumerator TransformationSequenceRoutine()
        {
            WandTransformationItem item = transformationItems[currentIndex];

            // 1. Immediately hide the draggable 'e' tile and drop slot so we never see duplicate 'e's
            if (draggableETile != null)
                draggableETile.gameObject.SetActive(false);

            if (dropSlot != null)
                dropSlot.gameObject.SetActive(false);

            // 2. Play Wand Sparkle SFX
            if (wandSparkleSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(wandSparkleSFX);
            else
                U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wand");

            yield return new WaitForSeconds(0.15f);

            // 3. Play Vowel Stretch SFX
            if (vowelStretchSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(vowelStretchSFX);
            else
                U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("vowel_stretch");

            // 4. Transform Word Text with Golden 'e' highlight & Morph Picture
            if (wordDisplayText != null)
            {
                string word = item.afterWord;
                if (!string.IsNullOrEmpty(word) && (word.EndsWith("e") || word.EndsWith("E")))
                {
                    string basePart = word.Substring(0, word.Length - 1);
                    wordDisplayText.text = $"<b>{basePart}<color=#FFD54F>e</color></b>";
                }
                else
                {
                    wordDisplayText.text = $"<b>{word}</b>";
                }
                wordDisplayText.color = new Color(0.12f, 0.45f, 0.85f, 1f);
                StartCoroutine(PunchScale(wordDisplayText.transform, 1.25f, 0.25f));
            }

            Sprite afterSpr = item.afterSprite != null ? item.afterSprite : ResolveSprite(item.afterWord);
            if (illustrationImage != null && afterSpr != null)
            {
                illustrationImage.sprite = afterSpr;
                illustrationImage.gameObject.SetActive(true);
                StartCoroutine(PunchScale(illustrationImage.transform, 1.15f, 0.2f));
            }

            if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U6_SA_UnitFlowManager_Masters_Phonics.Instance.RecordWordTransformed();
            }

            yield return new WaitForSeconds(0.5f);

            // 5. Play back-to-back words: "tub ... tube"
            PlayBeforeWordAudio(item);
            yield return new WaitForSeconds(0.7f);
            PlayAfterWordAudio(item);

            yield return new WaitForSeconds(0.6f);

            // 6. Open Sound Check Choice (Short vs Long)
            OpenSoundCheck(item);
        }

        private void OpenSoundCheck(WandTransformationItem item)
        {
            if (soundCheckContainer != null)
            {
                soundCheckContainer.SetActive(true);
                StartCoroutine(PunchScale(soundCheckContainer.transform, 1.05f, 0.15f));
            }

            if (soundCheckPromptText != null)
            {
                soundCheckPromptText.text = $"Which sound does the vowel make now in <b>{item.afterWord}</b>?";
                soundCheckPromptText.fontSize = 28;
                soundCheckPromptText.fontStyle = FontStyles.Bold;
            }

            if (shortSoundButton != null)
            {
                shortSoundButton.interactable = true;
                if (shortSoundButton.image != null)
                    shortSoundButton.image.color = defaultColorsCaptured ? defaultShortBtnColor : Color.white;
            }

            if (longSoundButton != null)
            {
                longSoundButton.interactable = true;
                if (longSoundButton.image != null)
                    longSoundButton.image.color = defaultColorsCaptured ? defaultLongBtnColor : Color.white;
            }

            if (shortSoundButtonText != null)
            {
                shortSoundButtonText.text = "<b>Short Sound</b>";
                shortSoundButtonText.fontStyle = FontStyles.Bold;
                shortSoundButtonText.fontSize = 22;
                shortSoundButtonText.color = new Color(0.12f, 0.18f, 0.32f, 1f);
            }

            if (longSoundButtonText != null)
            {
                longSoundButtonText.text = "<b>Long Sound</b>";
                longSoundButtonText.fontStyle = FontStyles.Bold;
                longSoundButtonText.fontSize = 22;
                longSoundButtonText.color = new Color(0.12f, 0.18f, 0.32f, 1f);
            }

            isProcessing = false;
        }

        private void OnSoundCheckSelected(bool choseLongSound)
        {
            if (isProcessing) return;
            isProcessing = true;

            if (shortSoundButton != null) shortSoundButton.interactable = false;
            if (longSoundButton != null) longSoundButton.interactable = false;

            // Lite green (#86EFAC / #A7F3D0) for correct, Lite red (#FDA4AF / #FECDD3) for incorrect
            Color liteGreen = new Color(0.55f, 0.94f, 0.68f, 1f);
            Color liteRed = new Color(0.99f, 0.65f, 0.65f, 1f);

            // In Magic Wand, all transformed words make the LONG vowel sound
            bool isCorrect = choseLongSound;

            if (isCorrect)
            {
                correctSoundChecks++;
                totalScore += 50;

                // Long Vowel is correct -> light green feedback
                if (longSoundButton != null && longSoundButton.image != null)
                {
                    longSoundButton.image.color = liteGreen;
                    StartCoroutine(PunchScale(longSoundButton.transform, 1.1f, 0.2f));
                }
                if (shortSoundButton != null && shortSoundButton.image != null)
                {
                    shortSoundButton.image.color = new Color(0.85f, 0.85f, 0.85f, 0.5f);
                }

                if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U6_SA_UnitFlowManager_Masters_Phonics.Instance.AddScore(50);

                if (correctSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                    U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(correctSFX);
                else
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("correct");

                ShowMascotCommentary($"Spot on! The Magic 'e' makes the vowel say its long name!");
            }
            else
            {
                totalScore += 20;

                // Short Vowel is wrong -> light red on short button, light green on long button to guide player
                if (shortSoundButton != null && shortSoundButton.image != null)
                {
                    shortSoundButton.image.color = liteRed;
                    StartCoroutine(PunchScale(shortSoundButton.transform, 1.06f, 0.15f));
                }
                if (longSoundButton != null && longSoundButton.image != null)
                {
                    longSoundButton.image.color = liteGreen;
                }

                if (wrongSFX != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                    U6_SA_AudioManager_Masters_Phonics.Instance.PlaySFX(wrongSFX);
                else
                    U6_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("wrong");

                ShowMascotCommentary($"Remember: Magic 'e' stretches the vowel into its long sound!");
            }

            UpdateHUD();
            StartCoroutine(AdvanceNextItemRoutine());
        }

        private IEnumerator AdvanceNextItemRoutine()
        {
            yield return new WaitForSeconds(1.6f);
            LoadItem(currentIndex + 1);
        }

        private void PlayBeforeWordAudio(WandTransformationItem item)
        {
            if (item.beforeAudio != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(item.beforeAudio);
            else
                U6_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(item.beforeWord);
        }

        private void PlayAfterWordAudio(WandTransformationItem item)
        {
            if (item.afterAudio != null && U6_SA_AudioManager_Masters_Phonics.Instance != null)
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(item.afterAudio);
            else
                U6_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(item.afterWord);
        }

        public void ReplayCurrentAudio()
        {
            if (currentIndex < transformationItems.Count)
            {
                WandTransformationItem item = transformationItems[currentIndex];
                if (isETilePlaced) PlayAfterWordAudio(item);
                else PlayBeforeWordAudio(item);
            }
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
                progressText.text = $"<b>Item {currentIndex + 1} of {transformationItems.Count}</b>";
                progressText.color = Color.white;
            }

            if (progressBar != null && transformationItems.Count > 0)
                progressBar.value = (float)currentIndex / transformationItems.Count;

            if (scoreText != null)
                scoreText.text = $"Score: {totalScore}";
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

            // Populate all transformation items from master phonics data
            transformationItems = U6_SA_DataTypes_Masters_Phonics.GetDefaultTransformationItems();

            // Find and assign all sprites and audio
            for (int i = 0; i < transformationItems.Count; i++)
            {
                var item = transformationItems[i];
                if (item == null) continue;

                item.beforeSprite = FindSpriteInEditor(item.beforeWord);
                item.afterSprite = FindSpriteInEditor(item.afterWord);

                item.beforeAudio = FindAudioInEditor(item.beforeWord);
                item.afterAudio = FindAudioInEditor(item.afterWord);
            }

            // Assign SFX clips
            if (wandSparkleSFX == null) wandSparkleSFX = FindAudioInEditor("wand");
            if (vowelStretchSFX == null) vowelStretchSFX = FindAudioInEditor("vowel_stretch");
            if (correctSFX == null) correctSFX = FindAudioInEditor("correct");
            if (wrongSFX == null) wrongSFX = FindAudioInEditor("wrong");

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log($"<color=#10B981><b>[Magic Wand] Successfully auto-assigned {transformationItems.Count} Transformation Items, Sprites, and Audio Clips!</b></color>");
        }

        private Sprite FindSpriteInEditor(string word)
        {
            if (string.IsNullOrEmpty(word)) return null;
            string clean = word.ToLower().Trim();

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new string[] { "Assets/Art/unit6_MP", "Assets/Art", "Assets/Icons" });
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object[] subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var sub in subAssets)
                {
                    if (sub is Sprite spr)
                    {
                        string sName = spr.name.ToLower();
                        if (sName == $"u6_pic_{clean}" || sName == clean || sName == $"pic_{clean}")
                            return spr;
                    }
                }
            }

            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object[] subAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var sub in subAssets)
                {
                    if (sub is Sprite spr)
                    {
                        string sName = spr.name.ToLower();
                        if (sName.Contains(clean)) return spr;
                    }
                }
            }
            return null;
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
    // Helper Draggable 'e' Tile
    // -------------------------------------------------------------
    public class WandDraggableETile : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        private U6_SA_GM05_MagicWand_Masters_Phonics parentGame;
        private CanvasGroup canvasGroup;
        private RectTransform dropSlot;
        private RectTransform rectTransform;
        private Canvas parentCanvas;
        private Vector2 startPos;
        private bool isDragging = false;
        private bool isFlying = false;

        public void Init(U6_SA_GM05_MagicWand_Masters_Phonics game, CanvasGroup cg, RectTransform slot)
        {
            parentGame = game;
            canvasGroup = cg;
            dropSlot = slot;
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            startPos = rectTransform.anchoredPosition;
            isDragging = false;
            isFlying = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging || isFlying) return;
            if (dropSlot == null || parentGame == null) return;

            StartCoroutine(FlyToSlotRoutine());
        }

        private IEnumerator FlyToSlotRoutine()
        {
            isFlying = true;
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;

            Vector3 initialPos = rectTransform.position;
            Vector3 targetPos = dropSlot.position;
            float elapsed = 0f;
            float dur = 0.2f;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / dur);
                rectTransform.position = Vector3.Lerp(initialPos, targetPos, t);
                yield return null;
            }

            rectTransform.position = targetPos;
            isFlying = false;
            parentGame?.OnETileDroppedSuccessfully();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (isFlying) return;
            isDragging = true;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.85f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isFlying) return;
            if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
            Camera cam = (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceCamera) ? parentCanvas.worldCamera : null;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, eventData.position, cam, out Vector3 worldPoint))
            {
                rectTransform.position = worldPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            if (isFlying) return;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            // Check if dropped near the target slot (generous drop tolerance for smooth mobile play)
            if (dropSlot != null && Vector2.Distance(rectTransform.position, dropSlot.position) < 220f)
            {
                rectTransform.position = dropSlot.position;
                parentGame?.OnETileDroppedSuccessfully();
            }
            else
            {
                // Snap back
                rectTransform.anchoredPosition = startPos;
            }
        }
    }
}
