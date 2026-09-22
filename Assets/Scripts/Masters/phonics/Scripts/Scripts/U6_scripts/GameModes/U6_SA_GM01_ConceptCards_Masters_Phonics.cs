using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U6_SA_GM01_ConceptCards_Masters_Phonics : MonoBehaviour
    {
        [Header("Card Navigation UI")]
        [SerializeField] private Button prevCardBtn;
        [SerializeField] private Button nextCardBtn;
        [SerializeField] private TextMeshProUGUI cardCounterText;
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private GameObject[] cardPanels;

        [Header("Narration Audio Clips (Inspector Overrides)")]
        public AudioClip card1IntroClip;
        public AudioClip card2IntroClip;
        public AudioClip card3IntroClip;
        public AudioClip card4IntroClip;

        private int currentCardIndex = 0;
        private const int TOTAL_CARDS = 4;
        private Sprite cachedRoundedSprite;

        private void Awake()
        {
            AutoBindHierarchy();
        }

        private void OnEnable()
        {
            AutoBindHierarchy();
            currentCardIndex = 0;
            ShowCard(0);
        }

        private Coroutine enableNextRoutine;

        private void AutoBindHierarchy()
        {
            if (cardsContainer == null)
            {
                cardsContainer = transform.Find("CardContainer") 
                              ?? transform.Find("CardsContainer") 
                              ?? transform.Find("DynamicCardContainer");
            }

            if (prevCardBtn == null)
            {
                Transform p = transform.Find("PrevButton") ?? transform.Find("PrevCardBtn") ?? transform.Find("Btn_Prev");
                if (p != null) prevCardBtn = p.GetComponent<Button>();
            }
            if (prevCardBtn != null)
            {
                prevCardBtn.onClick.RemoveAllListeners();
                prevCardBtn.onClick.AddListener(OnPrevCardClicked);
                prevCardBtn.transform.SetAsLastSibling();
            }

            if (nextCardBtn == null)
            {
                Transform n = transform.Find("NextButton") 
                           ?? transform.Find("NextCardButton") 
                           ?? transform.Find("Btn_Next") 
                           ?? transform.Find("Next_Button")
                           ?? transform.Find("Next");
                if (n != null) nextCardBtn = n.GetComponent<Button>();

                if (nextCardBtn == null)
                {
                    Button[] allButtons = GetComponentsInChildren<Button>(true);
                    foreach (var b in allButtons)
                    {
                        string bName = b.gameObject.name.ToLower();
                        if ((bName.Contains("next") || bName.Contains("forward") || bName.Contains("arrow")) && !bName.Contains("activity") && !bName.Contains("unit"))
                        {
                            nextCardBtn = b;
                            break;
                        }
                    }
                }
            }

            // Fallback: If no Next button exists in hierarchy, create one dynamically
            if (nextCardBtn == null)
            {
                GameObject nextBtnObj = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button));
                nextBtnObj.transform.SetParent(transform, false);
                RectTransform rt = nextBtnObj.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(720f, -420f);
                rt.sizeDelta = new Vector2(220f, 75f);

                Image img = nextBtnObj.GetComponent<Image>();
                img.sprite = U6_SA_GM05_MagicWand_Masters_Phonics.GetOrCreateRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = new Color(0.18f, 0.8f, 0.44f); // Vibrant Emerald Green

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtObj.transform.SetParent(nextBtnObj.transform, false);
                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;

                var tmp = txtObj.GetComponent<TextMeshProUGUI>();
                tmp.text = "<b>NEXT</b>";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 28;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = Color.white;
                tmp.raycastTarget = false;

                nextCardBtn = nextBtnObj.GetComponent<Button>();
            }

            if (nextCardBtn != null)
            {
                nextCardBtn.gameObject.SetActive(true);
                nextCardBtn.onClick.RemoveAllListeners();
                nextCardBtn.onClick.AddListener(OnNextCardClicked);
                nextCardBtn.transform.SetAsLastSibling();
            }

            if (cardCounterText == null)
            {
                Transform c = transform.Find("Progress_Text") 
                           ?? transform.Find("ProgressText")
                           ?? transform.Find("CardCounterText") 
                           ?? transform.Find("Counter_Text")
                           ?? transform.Find("ProgressHUD/Progress_Text")
                           ?? transform.Find("ProgressHUD/ProgressText");
                if (c != null) cardCounterText = c.GetComponent<TextMeshProUGUI>();

                if (cardCounterText == null)
                {
                    TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var tmp in tmps)
                    {
                        string n = tmp.gameObject.name.ToLower();
                        if ((n.Contains("progress") || n.Contains("counter") || n.Contains("count") || n.Contains("card")) &&
                            !n.Contains("title") && !n.Contains("prompt") && !n.Contains("btn") && !n.Contains("button") && !n.Contains("desc"))
                        {
                            cardCounterText = tmp;
                            break;
                        }
                    }
                }
            }
        }

        public void ShowCard(int index)
        {
            currentCardIndex = Mathf.Clamp(index, 0, TOTAL_CARDS - 1);

            if (cardCounterText != null)
            {
                cardCounterText.text = $"<b>Card {currentCardIndex + 1} of {TOTAL_CARDS}</b>";
                cardCounterText.color = Color.white;
                cardCounterText.fontStyle = FontStyles.Bold;
            }

            if (prevCardBtn != null)
            {
                prevCardBtn.gameObject.SetActive(currentCardIndex > 0);
                prevCardBtn.transform.SetAsLastSibling();
            }

            if (nextCardBtn != null)
            {
                nextCardBtn.gameObject.SetActive(true);
                nextCardBtn.interactable = true;
                nextCardBtn.transform.SetAsLastSibling();
                var nextTxt = nextCardBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (nextTxt != null)
                {
                    nextTxt.text = currentCardIndex < TOTAL_CARDS - 1 ? "<b>NEXT</b>" : "<b>START ACTIVITY 1</b>";
                }
            }

            // If static card panels are wired in inspector, toggle them
            if (cardPanels != null && cardPanels.Length >= TOTAL_CARDS)
            {
                for (int i = 0; i < cardPanels.Length; i++)
                {
                    if (cardPanels[i] != null) cardPanels[i].SetActive(i == currentCardIndex);
                }
            }
            else
            {
                // Dynamic clean card rendering
                RenderDynamicCard(currentCardIndex);
            }

            // Ensure buttons stay in front of the rendered cards
            if (prevCardBtn != null) prevCardBtn.transform.SetAsLastSibling();
            if (nextCardBtn != null) nextCardBtn.transform.SetAsLastSibling();

            float audioDur = PlayCardAudio(currentCardIndex);

            // Unlock and pulse next button after audio
            if (enableNextRoutine != null) StopCoroutine(enableNextRoutine);
            enableNextRoutine = StartCoroutine(EnableNextButtonAfterAudio(audioDur));
        }

        private IEnumerator EnableNextButtonAfterAudio(float duration)
        {
            if (nextCardBtn != null)
            {
                nextCardBtn.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(Mathf.Max(0.5f, duration));

            UnlockNextButton();
        }

        public void UnlockNextButton()
        {
            if (nextCardBtn != null)
            {
                nextCardBtn.gameObject.SetActive(true);
                nextCardBtn.interactable = true;
                nextCardBtn.transform.SetAsLastSibling();
                StartCoroutine(PunchScale(nextCardBtn.transform, 1.15f, 0.15f));
            }
        }

        private void ClearContainer()
        {
            if (cardsContainer == null) return;
            for (int i = cardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardsContainer.GetChild(i).gameObject);
            }
        }

        private void RenderDynamicCard(int cardIdx)
        {
            if (cardsContainer == null) return;
            ClearContainer();

            GameObject cardRoot = new GameObject($"Card_{cardIdx + 1}", typeof(RectTransform));
            cardRoot.transform.SetParent(cardsContainer, false);
            RectTransform rootRt = cardRoot.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            switch (cardIdx)
            {
                case 0: RenderCard1(cardRoot.transform); break;
                case 1: RenderCard2(cardRoot.transform); break;
                case 2: RenderCard3(cardRoot.transform); break;
                case 3: RenderCard4(cardRoot.transform); break;
            }
        }

        // -------------------------------------------------------------------------
        // Card 1: Bossy 'e' / Magic 'e' Rhyme & Transformation
        // -------------------------------------------------------------------------
        private void RenderCard1(Transform parent)
        {
            // Card background box
            GameObject box = CreateCardBox(parent, "Box", new Vector2(0f, 0f), new Vector2(1120f, 540f), new Color(0.12f, 0.2f, 0.35f, 0.92f));

            // Main Rhyme Text
            TextMeshProUGUI rhymeTMP = CreateTMP(box.transform, "RhymeText", 
                "<b><color=#FFE082>\"Bossy 'e' says nothing at all,\nbut pinches the vowel to make it say its name!\"</color></b>", 34);
            rhymeTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 175f);
            rhymeTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(1040f, 100f);

            // Demonstration Transformation Row
            GameObject demoRow = CreateUIContainer(box.transform, "DemoRow", new Vector2(0f, 15f), new Vector2(980f, 190f));
            
            // Before: tub
            GameObject tubBox = CreateCardBox(demoRow.transform, "TubBox", new Vector2(-270f, 0f), new Vector2(300f, 170f), new Color(0.2f, 0.32f, 0.52f, 0.9f));
            CreateTMP(tubBox.transform, "Word", "<b><color=white>t u b</color></b>", 54).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 28f);
            CreateTMP(tubBox.transform, "Sound", "<b><color=#90CAF9>Short /uh/ sound</color></b>", 24).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -38f);
            AddAudioButton(tubBox, "tub");

            // Arrow with magic wand text
            TextMeshProUGUI arrowTMP = CreateTMP(demoRow.transform, "Arrow", "<b><color=#FFD54F>+ e -></color></b>\n<size=22><color=#FFF59D>Magic 'e'</color></size>", 40);
            arrowTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);

            // After: tube
            GameObject tubeBox = CreateCardBox(demoRow.transform, "TubeBox", new Vector2(270f, 0f), new Vector2(300f, 170f), new Color(0.15f, 0.48f, 0.35f, 0.9f));
            CreateTMP(tubeBox.transform, "Word", "<b><color=white>t u b <color=#FFD54F>e</color></color></b>", 54).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 28f);
            CreateTMP(tubeBox.transform, "Sound", "<b><color=#A5D6A7>Long /you/ sound</color></b>", 24).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -38f);
            AddAudioButton(tubeBox, "tube");

            // Subtitle explanation
            TextMeshProUGUI explTMP = CreateTMP(box.transform, "ExplText", 
                "<b>Tap any word to listen! Notice the 'e' at the end is completely silent.</b>", 25);
            explTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -180f);
            explTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(1000f, 55f);
            explTMP.color = Color.white;
        }

        // -------------------------------------------------------------------------
        // Card 2: The 7 Jobs of Silent 'e' Grid
        // -------------------------------------------------------------------------
        private void RenderCard2(Transform parent)
        {
            GameObject box = CreateCardBox(parent, "Box", new Vector2(0f, 0f), new Vector2(1150f, 560f), new Color(0.12f, 0.2f, 0.35f, 0.92f));

            TextMeshProUGUI headerTMP = CreateTMP(box.transform, "Header", "<b><color=#FFE082>Silent 'e' has 7 DIFFERENT JOBS in English!</color></b>\n<size=22><color=white>Tap each job to hear its example word:</size></color>", 28);
            headerTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 220f);
            headerTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(1080f, 75f);

            GameObject gridObj = CreateUIContainer(box.transform, "JobsGrid", new Vector2(0f, -30f), new Vector2(1100f, 390f));
            var glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(520f, 78f);
            glg.spacing = new Vector2(24f, 12f);
            glg.childAlignment = TextAnchor.MiddleCenter;

            string[] jobs = new string[]
            {
                "<b>Job 1:</b> Makes vowel long (<i>cake, kite</i>)",
                "<b>Job 2:</b> Softens c & g to /s/, /j/ (<i>dance, age</i>)",
                "<b>Job 3:</b> No word ends in v or u (<i>give, blue</i>)",
                "<b>Job 4:</b> Shows word not plural (<i>house, nurse</i>)",
                "<b>Job 5:</b> Syllable needs vowel (<i>table, little</i>)",
                "<b>Job 6:</b> Clarifies meaning (<i>breathe vs breath</i>)",
                "<b>Job 7:</b> Word history & silent 'e' (<i>bye, are</i>)"
            };

            string[] words = new string[] { "cake", "orange", "give", "house", "table", "breathe", "bye" };

            for (int i = 0; i < jobs.Length; i++)
            {
                int idx = i;
                GameObject jobTile = CreateCardBox(gridObj.transform, $"Job_{i+1}", Vector2.zero, Vector2.zero, new Color(0.18f, 0.32f, 0.52f, 0.9f));
                TextMeshProUGUI label = CreateTMP(jobTile.transform, "Label", $"<b>{jobs[i]}</b>", 22);
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = Color.white;
                label.GetComponent<RectTransform>().offsetMin = new Vector2(18f, 0f);
                label.GetComponent<RectTransform>().offsetMax = new Vector2(-18f, 0f);

                Button btn = jobTile.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(jobTile.transform, 1.08f, 0.15f));
                    if (idx < words.Length)
                    {
                        U6_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(words[idx]);
                    }
                });
            }
        }

        // -------------------------------------------------------------------------
        // Card 3: Soft 'c' and Soft 'g' (Job 2)
        // -------------------------------------------------------------------------
        private void RenderCard3(Transform parent)
        {
            GameObject box = CreateCardBox(parent, "Box", new Vector2(0f, 0f), new Vector2(1120f, 540f), new Color(0.12f, 0.2f, 0.35f, 0.92f));

            TextMeshProUGUI headerTMP = CreateTMP(box.transform, "Header", 
                "<b><color=#FFE082>Job 2: 'c' and 'g' turn SOFT before e, i, and y!</color></b>", 30);
            headerTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 200f);
            headerTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(1040f, 65f);

            GameObject colsRow = CreateUIContainer(box.transform, "ColsRow", new Vector2(0f, 10f), new Vector2(1000f, 280f));

            // Left Box: Hard sounds
            GameObject hardBox = CreateCardBox(colsRow.transform, "HardBox", new Vector2(-265f, 0f), new Vector2(460f, 260f), new Color(0.38f, 0.22f, 0.22f, 0.9f));
            CreateTMP(hardBox.transform, "Title", "<b><color=#FF8A80>HARD SOUNDS (Without 'e')</color></b>", 26).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);
            CreateTMP(hardBox.transform, "C", "<b>'c' says /k/</b> -> <i>cat, cup, cap</i>", 24).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 30f);
            CreateTMP(hardBox.transform, "G", "<b>'g' says /g/</b> -> <i>got, gap, gum</i>", 24).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -40f);

            // Right Box: Soft sounds
            GameObject softBox = CreateCardBox(colsRow.transform, "SoftBox", new Vector2(265f, 0f), new Vector2(460f, 260f), new Color(0.15f, 0.48f, 0.32f, 0.9f));
            CreateTMP(softBox.transform, "Title", "<b><color=#A5D6A7>SOFT SOUNDS (Before 'e')</color></b>", 26).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);
            CreateTMP(softBox.transform, "C", "<b>'c' says /s/</b> -> <i>face, rice, cent</i>", 24).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 30f);
            CreateTMP(softBox.transform, "G", "<b>'g' says /j/</b> -> <i>gem, page, giant</i>", 24).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -40f);

            TextMeshProUGUI footTMP = CreateTMP(box.transform, "Footer", 
                "<b>The silent 'e' changes hard /k/ into soft /s/ and hard /g/ into soft /j/!</b>", 25);
            footTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -185f);
            footTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(1040f, 55f);
            footTMP.color = Color.white;
        }

        // -------------------------------------------------------------------------
        // Card 4: Rule-Breakers & Ready for Magic Wand
        // -------------------------------------------------------------------------
        private void RenderCard4(Transform parent)
        {
            GameObject box = CreateCardBox(parent, "Box", new Vector2(0f, 0f), new Vector2(1120f, 540f), new Color(0.12f, 0.2f, 0.35f, 0.92f));

            TextMeshProUGUI headerTMP = CreateTMP(box.transform, "Header", 
                "<b><color=#FFE082>Job 3: English Words Do NOT End in V or U!</color></b>", 30);
            headerTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 200f);
            headerTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(1040f, 65f);

            GameObject rulesRow = CreateUIContainer(box.transform, "RulesRow", new Vector2(0f, 25f), new Vector2(1000f, 230f));

            // V Box
            GameObject vBox = CreateCardBox(rulesRow.transform, "VBox", new Vector2(-265f, 0f), new Vector2(460f, 210f), new Color(0.18f, 0.32f, 0.52f, 0.9f));
            CreateTMP(vBox.transform, "Rule", "<b><color=#90CAF9>Protector 'e' on 'V'</color></b>", 26).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 58f);
            CreateTMP(vBox.transform, "Words", "<b>have - give - live - glove</b>\n<size=20><color=#E0E0E0>(The 'e' protects the letter 'v')</color></size>", 25).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -22f);
            AddAudioButton(vBox, "give");

            // U Box
            GameObject uBox = CreateCardBox(rulesRow.transform, "UBox", new Vector2(265f, 0f), new Vector2(460f, 210f), new Color(0.18f, 0.32f, 0.52f, 0.9f));
            CreateTMP(uBox.transform, "Rule", "<b><color=#90CAF9>Protector 'e' on 'U'</color></b>", 26).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 58f);
            CreateTMP(uBox.transform, "Words", "<b>blue - true - glue - clue</b>\n<size=20><color=#E0E0E0>(English words never end in plain 'u')</color></size>", 25).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -22f);
            AddAudioButton(uBox, "blue");

            // Clean Footer prompt pointing to the bottom-right Next button
            TextMeshProUGUI footTMP = CreateTMP(box.transform, "Footer", 
                "<b><color=#FFE082>You are ready for the Magic Wand! Tap NEXT to begin Activity 1.</color></b>", 26);
            footTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -180f);
            footTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(1040f, 55f);
            footTMP.color = new Color(1f, 0.95f, 0.7f, 1f);
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------
        private void AddAudioButton(GameObject target, string word)
        {
            Button btn = target.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(target.transform, 1.1f, 0.15f));
                U6_SA_AudioManager_Masters_Phonics.Instance?.PlayWordAudio(word);
                UnlockNextButton();
            });
        }

        private GameObject CreateCardBox(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            if (size != Vector2.zero) rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.sprite = GetOrCreateRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = bgColor;
            return go;
        }

        private GameObject CreateUIContainer(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private TextMeshProUGUI CreateTMP(Transform parent, string name, string text, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return tmp;
        }

        private Sprite GetOrCreateRoundedSprite()
        {
            if (cachedRoundedSprite != null) return cachedRoundedSprite;
            int size = 64;
            int r = 16;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Min(x, size - 1 - x);
                    int dy = Mathf.Min(y, size - 1 - y);

                    if (dx < r && dy < r)
                    {
                        float dist = Vector2.Distance(new Vector2(dx, dy), new Vector2(r, r));
                        colors[y * size + x] = dist <= r ? Color.white : Color.clear;
                    }
                    else
                    {
                        colors[y * size + x] = Color.white;
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            cachedRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return cachedRoundedSprite;
        }

        private float PlayCardAudio(int cardIdx)
        {
            if (U6_SA_AudioManager_Masters_Phonics.Instance == null) return 1.5f;

            AudioClip clip = null;
            switch (cardIdx)
            {
                case 0:
                    clip = card1IntroClip;
                    if (clip != null) U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
                    else U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoicePrompt("U06_VO_card1", "Bossy e seems a bit rude...");
                    break;
                case 1:
                    clip = card2IntroClip;
                    if (clip != null) U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
                    else U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoicePrompt("U06_VO_card2", "Seven jobs. The first one is the big one...");
                    break;
                case 2:
                    clip = card3IntroClip;
                    if (clip != null) U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
                    else U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoicePrompt("U06_VO_card3", "c usually says /k/...");
                    break;
                case 3:
                    clip = card4IntroClip;
                    if (clip != null) U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
                    else U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoicePrompt("U06_VO_card4", "Now the rule-breakers...");
                    break;
            }

            return clip != null ? clip.length : 1.5f;
        }

        public void OnNextCardClicked()
        {
            if (currentCardIndex < TOTAL_CARDS - 1)
            {
                ShowCard(currentCardIndex + 1);
            }
            else
            {
                AdvanceToMagicWand();
            }
        }

        public void OnPrevCardClicked()
        {
            if (currentCardIndex > 0)
            {
                ShowCard(currentCardIndex - 1);
            }
        }

        public void AdvanceToMagicWand()
        {
            if (U6_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U6_SA_UnitFlowManager_Masters_Phonics.Instance.OpenActivity1();
            }
        }

        private IEnumerator PunchScale(Transform target, float scale, float duration)
        {
            if (target == null) yield break;
            Vector3 original = Vector3.one;
            target.localScale = original * scale;
            yield return new WaitForSeconds(duration);
            if (target != null) target.localScale = original;
        }
    }
}
