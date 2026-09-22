using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U1_SA_GM01_ConceptCards_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Activity Data (Optional)")]
        public U1_SA_ActivityDataSO_Masters_Phonics activityData;

        [Header("2. Mascot Lesson Clips (Voice A)")]
        public AudioClip card1Audio;
        public AudioClip card2Audio;
        public AudioClip card3Audio;
        public AudioClip card4Audio;

        [Header("3. Word & Affix Pronunciation Clips (Voice B)")]
        public AudioClip audioDis;
        public AudioClip audioLike;
        public AudioClip audioDislike;
        public AudioClip audioGreat;
        public AudioClip audioNess;
        public AudioClip audioGreatness;

        [Tooltip("Direct word audio clips for reference lists (un, re, pre, mis, ful, less, etc.)")]
        public List<AudioClip> referenceWordAudioClips = new List<AudioClip>();

        [Header("4. Dynamic Display Container")]
        public Transform dynamicCardContainer;

        [Header("5. Header & Navigation (Auto-Bound)")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI descriptionTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private Button nextCardButton;
        [SerializeField] private Button replayAudioButton;

        private int currentCardIndex = 0;
        private List<GameObject> activeSpawnedTiles = new List<GameObject>();
        private static Sprite roundedCardSprite;

        private void Awake()
        {
            EnsureRoundedSprite();
            AutoBindUI();
        }

        private void Start()
        {
            if (nextCardButton != null)
            {
                nextCardButton.onClick.RemoveAllListeners();
                nextCardButton.onClick.AddListener(NextCard);
            }

            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayAudio);
            }
        }

        private void OnEnable()
        {
            EnsureRoundedSprite();
            AutoBindUI();
            currentCardIndex = 0;
            ShowCard(currentCardIndex);
        }

        public void Initialize(U1_SA_ActivityDataSO_Masters_Phonics data)
        {
            activityData = data;
            currentCardIndex = 0;
            ShowCard(currentCardIndex);
        }

        private void EnsureRoundedSprite()
        {
            if (roundedCardSprite != null) return;

            // Generate a crisp, antialiased 9-sliced rounded rectangle sprite procedurally
            int size = 128;
            int radius = 36;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - size / 2f + 0.5f) - (size / 2f - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - size / 2f + 0.5f) - (size / 2f - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < radius - 1.5f)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else if (dist < radius)
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

            // 9-slice borders: 36px from left, bottom, right, top
            roundedCardSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private void AutoBindUI()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null)
                {
                    titleTMP = t.GetComponent<TextMeshProUGUI>();
                    if (titleTMP != null) titleTMP.fontSize = 54;
                }
            }

            if (descriptionTMP == null)
            {
                var t = transform.Find("description text") ?? transform.Find("DescriptionText") ?? transform.Find("Description");
                if (t != null)
                {
                    descriptionTMP = t.GetComponent<TextMeshProUGUI>();
                    if (descriptionTMP != null) descriptionTMP.fontSize = 32;
                }
            }

            if (progressTMP == null)
            {
                var t = transform.Find("ProgressIndicator_Text") ?? transform.Find("ProgressText") ?? transform.Find("Progress");
                if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (nextCardButton == null)
            {
                Button[] allBtns = GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns)
                {
                    if (b == null) continue;
                    string bName = b.name.ToLower();
                    if (bName.Contains("next") || bName.Contains("continue") || bName.Contains("btn_next") || bName.Contains("arrow"))
                    {
                        nextCardButton = b;
                        break;
                    }
                }
            }

            if (nextCardButton != null)
            {
                nextCardButton.gameObject.SetActive(true);
                nextCardButton.onClick.RemoveAllListeners();
                nextCardButton.onClick.AddListener(NextCard);
            }

            if (replayAudioButton == null)
            {
                var t = transform.Find("ReplayButton") ?? transform.Find("Replay_Button") ?? transform.Find("SpeakerButton");
                if (t != null) replayAudioButton = t.GetComponent<Button>();
            }

            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayAudio);
            }

            if (dynamicCardContainer == null)
            {
                Transform existing = transform.Find("DynamicCardContainer") ?? transform.Find("Machine_Display") ?? transform.Find("Card_Display");
                if (existing != null)
                {
                    dynamicCardContainer = existing;
                    // Hide any legacy raw image background
                    Image img = existing.GetComponent<Image>();
                    if (img != null) img.color = new Color(1, 1, 1, 0);
                }
                else
                {
                    GameObject cont = new GameObject("DynamicCardContainer", typeof(RectTransform));
                    cont.transform.SetParent(transform, false);
                    RectTransform rt = cont.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0, -30f);
                    rt.sizeDelta = new Vector2(1600f, 480f);
                    dynamicCardContainer = cont.transform;
                }
            }
        }

        public void ShowCard(int cardIndex)
        {
            currentCardIndex = cardIndex;
            ClearSpawnedTiles();

            int totalCards = 4;
            if (cardIndex >= totalCards)
            {
                OnConceptCardsComplete();
                return;
            }

            if (progressTMP != null)
            {
                progressTMP.text = $"Card {cardIndex + 1} / {totalCards}";
            }

            TMP_FontAsset font = titleTMP != null ? titleTMP.font : (descriptionTMP != null ? descriptionTMP.font : null);

            switch (cardIndex)
            {
                case 0:
                    SetHeader("A prefix goes in front", "Tap each card to hear its sound, then tap the completed word:");
                    RenderFormulaCard("dis-", "like", "dislike", "like (enjoy) -> dislike (opposite)", audioDis, audioLike, audioDislike, 0, font);
                    break;

                case 1:
                    SetHeader("Each prefix carries a meaning", "Tap any prefix card to hear its sound:");
                    RenderPrefixReferenceList(font);
                    break;

                case 2:
                    SetHeader("A suffix goes at the end", "Tap each card to hear its sound, then tap the completed word:");
                    RenderFormulaCard("great", "-ness", "greatness", "great (describing) -> greatness (state of being)", audioGreat, audioNess, audioGreatness, 1, font);
                    break;

                case 3:
                    SetHeader("Suffixes have meanings too", "Tap any suffix card to hear its sound:");
                    RenderSuffixReferenceList(font);
                    break;
            }

            // Ensure Next Button is active so user can freely advance between cards
            if (nextCardButton != null)
            {
                nextCardButton.gameObject.SetActive(true);
                StartCoroutine(PulseNextButtonDelayed(1.5f));
            }

            // Play Voice A mascot lesson
            AudioClip voice = GetVoiceAClip(cardIndex);
            if (U1_SA_AudioManager_Masters_Phonics.Instance != null && voice != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(voice);
            }
        }

        private IEnumerator PulseNextButtonDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (nextCardButton != null)
            {
                StartCoroutine(PunchScale(nextCardButton.transform));
            }
        }

        private void SetHeader(string title, string desc)
        {
            if (titleTMP != null) titleTMP.text = title;
            if (descriptionTMP != null) descriptionTMP.text = desc;
        }

        // ------------------ Formula & List Renderers ------------------

        private void RenderFormulaCard(string partA, string partB, string resultWord, string explanation, AudioClip clipA, AudioClip clipB, AudioClip clipRes, int wordIndex, TMP_FontAsset font)
        {
            if (dynamicCardContainer == null) return;

            // Fallback to ScriptableObject data if direct clips aren't assigned
            U1_SA_WordDataSO_Masters_Phonics wordSO = GetWordData(wordIndex);
            if (clipA == null && wordSO != null && wordSO.componentAudioClips != null && wordSO.componentAudioClips.Count > 0) clipA = wordSO.componentAudioClips[0];
            if (clipB == null && wordSO != null && wordSO.componentAudioClips != null && wordSO.componentAudioClips.Count > 1) clipB = wordSO.componentAudioClips[1];
            if (clipRes == null && wordSO != null) clipRes = wordSO.fullWordAudioClip;

            // Large, chunky rounded tiles with 68pt bold font
            CreateRoundedTile(dynamicCardContainer, new Vector2(-430, 45), new Vector2(320, 160), new Color(0.18f, 0.52f, 0.92f), partA, font, 66, () => PlayTappedWordAudio(clipA, partA));
            CreateSymbol(dynamicCardContainer, new Vector2(-225, 45), "+", font, 68, new Color(0.2f, 0.2f, 0.3f));
            CreateRoundedTile(dynamicCardContainer, new Vector2(-20, 45), new Vector2(320, 160), new Color(0.22f, 0.70f, 0.28f), partB, font, 66, () => PlayTappedWordAudio(clipB, partB));
            CreateSymbol(dynamicCardContainer, new Vector2(185, 45), "->", font, 56, new Color(0.2f, 0.2f, 0.3f));
            CreateRoundedTile(dynamicCardContainer, new Vector2(460, 45), new Vector2(380, 160), new Color(0.58f, 0.30f, 0.88f), resultWord, font, 68, () => PlayTappedWordAudio(clipRes, resultWord));

            // Rounded Subtitle Meaning Banner
            CreateRoundedBanner(dynamicCardContainer, new Vector2(0, -95), new Vector2(1150, 80), explanation, font);
        }

        private void RenderPrefixReferenceList(TMP_FontAsset font)
        {
            if (dynamicCardContainer == null) return;

            string[] items = new string[]
            {
                "un- / dis-  = not  (unfair, dislike)",
                "re-  = again  (refill, rewrite)",
                "pre-  = before  (preheat, prepay)",
                "mis-  = wrongly  (mispronounce, mistake)"
            };

            float startY = 120f;
            float spacingY = 82f;

            for (int i = 0; i < items.Length; i++)
            {
                string text = items[i];
                AudioClip clip = (referenceWordAudioClips != null && i < referenceWordAudioClips.Count) ? referenceWordAudioClips[i] : null;
                Vector2 pos = new Vector2(0, startY - (i * spacingY));
                CreateRoundedListRow(dynamicCardContainer, pos, new Vector2(1250, 72), text, new Color(0.18f, 0.52f, 0.92f, 0.18f), font, 38, () => PlayTappedWordAudio(clip, text));
            }
        }

        private void RenderSuffixReferenceList(TMP_FontAsset font)
        {
            if (dynamicCardContainer == null) return;

            string[] items = new string[]
            {
                "-ful  = full of  (cheerful, hopeful)",
                "-less  = without  (careless, spotless)",
                "-ness  = state of being  (kindness)",
                "-ship / -ment  = state of / result  (friendship, enjoyment)"
            };

            float startY = 120f;
            float spacingY = 82f;

            for (int i = 0; i < items.Length; i++)
            {
                string text = items[i];
                AudioClip clip = (referenceWordAudioClips != null && (i + 4) < referenceWordAudioClips.Count) ? referenceWordAudioClips[i + 4] : null;
                Vector2 pos = new Vector2(0, startY - (i * spacingY));
                CreateRoundedListRow(dynamicCardContainer, pos, new Vector2(1250, 72), text, new Color(0.58f, 0.30f, 0.88f, 0.18f), font, 38, () => PlayTappedWordAudio(clip, text));
            }
        }

        // ------------------ Procedural Rounded UI Builders ------------------

        private void CreateRoundedTile(Transform parent, Vector2 pos, Vector2 size, Color bgColor, string text, TMP_FontAsset font, float fontSize, UnityEngine.Events.UnityAction onClick)
        {
            GameObject tile = new GameObject($"Tile_{text}", typeof(RectTransform), typeof(Image), typeof(Button));
            tile.transform.SetParent(parent, false);
            activeSpawnedTiles.Add(tile);

            RectTransform rt = tile.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = tile.GetComponent<Image>();
            img.sprite = roundedCardSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            Button btn = tile.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(tile.transform));
                    onClick.Invoke();
                });
            }

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(tile.transform, false);

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = $"<b>{text}</b>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
        }

        private void CreateSymbol(Transform parent, Vector2 pos, string symbol, TMP_FontAsset font, float fontSize, Color color)
        {
            GameObject sym = new GameObject("Symbol", typeof(RectTransform), typeof(TextMeshProUGUI));
            sym.transform.SetParent(parent, false);
            activeSpawnedTiles.Add(sym);

            RectTransform rt = sym.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(90, 90);

            TextMeshProUGUI tmp = sym.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = $"<b>{symbol}</b>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.raycastTarget = false;
        }

        private void CreateRoundedBanner(Transform parent, Vector2 pos, Vector2 size, string text, TMP_FontAsset font)
        {
            GameObject banner = new GameObject("Banner", typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(parent, false);
            activeSpawnedTiles.Add(banner);

            RectTransform rt = banner.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = banner.GetComponent<Image>();
            img.sprite = roundedCardSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(1f, 1f, 1f, 0.92f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(banner.transform, false);

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = $"<b>{text}</b>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 32;
            tmp.color = new Color(0.15f, 0.15f, 0.22f);
            tmp.raycastTarget = false;
        }

        private void CreateRoundedListRow(Transform parent, Vector2 pos, Vector2 size, string text, Color bgColor, TMP_FontAsset font, float fontSize, UnityEngine.Events.UnityAction onClick)
        {
            GameObject row = new GameObject("ListRow", typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(parent, false);
            activeSpawnedTiles.Add(row);

            RectTransform rt = row.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = row.GetComponent<Image>();
            img.sprite = roundedCardSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            Button btn = row.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            if (onClick != null)
            {
                btn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(row.transform));
                    onClick.Invoke();
                });
            }

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(row.transform, false);

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = new Vector2(-40, 0);

            TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = $"<b>{text}</b>";
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.12f, 0.12f, 0.2f);
            tmp.raycastTarget = false;
        }

        private void ClearSpawnedTiles()
        {
            foreach (var t in activeSpawnedTiles)
            {
                if (t != null) Destroy(t);
            }
            activeSpawnedTiles.Clear();
        }

        // ------------------ Clean Audio Playback (No Double Voice) ------------------

        private void PlayTappedWordAudio(AudioClip clip, string wordLabel)
        {
            if (U1_SA_AudioManager_Masters_Phonics.Instance == null) return;

            // When tapping a word tile, stop any currently playing mascot voice so the word pronunciation is loud, crisp, and clear
            U1_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();

            if (clip != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            }
            else
            {
                Debug.Log($"Word audio for '{wordLabel}' tapped. (Assign clip in Inspector)");
            }
        }

        private AudioClip GetVoiceAClip(int cardIndex)
        {
            switch (cardIndex)
            {
                case 0: return card1Audio;
                case 1: return card2Audio;
                case 2: return card3Audio;
                case 3: return card4Audio;
                default: return null;
            }
        }

        private U1_SA_WordDataSO_Masters_Phonics GetWordData(int index)
        {
            if (activityData != null && activityData.targetWords != null && index < activityData.targetWords.Count)
            {
                return activityData.targetWords[index];
            }
            return null;
        }

        private IEnumerator PunchScale(Transform tr)
        {
            if (tr == null) yield break;
            Vector3 orig = Vector3.one;
            tr.localScale = orig * 1.10f;
            yield return new WaitForSeconds(0.10f);
            if (tr != null) tr.localScale = orig;
        }

        // ------------------ Navigation ------------------

        public void NextCard()
        {
            currentCardIndex++;
            if (currentCardIndex < 4)
            {
                ShowCard(currentCardIndex);
            }
            else
            {
                OnConceptCardsComplete();
            }
        }

        public void ReplayAudio()
        {
            AudioClip clip = GetVoiceAClip(currentCardIndex);
            if (U1_SA_AudioManager_Masters_Phonics.Instance != null && clip != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
            }
        }

        private void OnConceptCardsComplete()
        {
            Debug.Log("<color=green>Concept Cards (Learn) Finished! Launching Activity 1...</color>");
            if (U1_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U1_SA_UnitFlowManager_Masters_Phonics.Instance.OpenActivity1();
            }
        }
    }
}
