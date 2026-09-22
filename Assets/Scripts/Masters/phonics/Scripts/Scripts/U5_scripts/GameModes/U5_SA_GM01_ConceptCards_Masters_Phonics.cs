using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U5_SA_GM01_ConceptCards_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Narration Audio Clips (Voice A)")]
        [Tooltip("U05_VO_card1: Watch. Go. The gate's open, so the o says its name. Now add a t... and the door shuts. Got.")]
        public AudioClip card1VoiceClip;
        [Tooltip("U05_VO_card2: Take the t away and the gate swings open again. Go. When nothing follows the vowel, the vowel says its name.")]
        public AudioClip card2VoiceClip;
        [Tooltip("U05_VO_card3: Open on the left, closed on the right. Tap a column to hear all of them.")]
        public AudioClip card3VoiceClip;
        [Tooltip("U05_VO_card4: In a long word, look at the first syllable. B-a gives you bacon. Move the door and you get basket.")]
        public AudioClip card4VoiceClip;

        [Header("2. UI References")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI descriptionTMP;
        [SerializeField] private Transform dynamicCardContainer;
        [SerializeField] private Button nextCardButton;
        [SerializeField] private Button replayVoiceAButton;

        private int currentCardIndex = 0;
        private const int TOTAL_CARDS = 4;
        [Header("Card Sprite (Optional Override)")]
        [SerializeField] private Sprite cardRoundedSprite;
        private Sprite proceduralRoundedSprite;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private Coroutine enableNextRoutine;
        private HashSet<int> clickedCardsOnPage = new HashSet<int>();
        private int totalInteractiveCardsOnPage = 0;

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            currentCardIndex = 0;
            ShowCard(currentCardIndex);
        }

        private void AutoBindHierarchyElements()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("Title BG/TitleText") ?? transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Title");
                if (t != null) titleTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (descriptionTMP == null)
            {
                var t = transform.Find("prompt bg/Prompt_Text") ?? transform.Find("DescriptionText") ?? transform.Find("Prompt_Text") ?? transform.Find("Subtitle");
                if (t != null) descriptionTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (dynamicCardContainer == null)
            {
                Transform t = transform.Find("DynamicCardContainer") ?? transform.Find("Cards_Container") ?? transform.Find("CardAnchor") ?? transform.Find("CardContainer");
                if (t != null) dynamicCardContainer = t;
            }

            if (nextCardButton == null)
            {
                Transform existing = transform.Find("NextButton") 
                                  ?? transform.Find("NextCardButton") 
                                  ?? transform.Find("Btn_Next") 
                                  ?? transform.Find("Next_Button")
                                  ?? transform.Find("Btn_NextCard")
                                  ?? transform.Find("Next");
                if (existing != null) nextCardButton = existing.GetComponent<Button>();

                if (nextCardButton == null)
                {
                    Button[] allButtons = GetComponentsInChildren<Button>(true);
                    foreach (var b in allButtons)
                    {
                        string bName = b.gameObject.name.ToLower();
                        if ((bName.Contains("next") || bName.Contains("forward") || bName.Contains("arrow")) && !bName.Contains("activity"))
                        {
                            nextCardButton = b;
                            break;
                        }
                    }
                }
            }

            // Fallback: If no Next button exists in hierarchy, create one dynamically
            if (nextCardButton == null)
            {
                GameObject nextBtnObj = new GameObject("NextCardButton", typeof(RectTransform), typeof(Image), typeof(Button));
                nextBtnObj.transform.SetParent(transform, false);
                RectTransform rt = nextBtnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.85f, 0.08f);
                rt.anchorMax = new Vector2(0.85f, 0.08f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(220f, 70f);

                Image img = nextBtnObj.GetComponent<Image>();
                img.sprite = GetRoundedSprite();
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
                tmp.fontSize = 32;
                tmp.color = Color.white;
                tmp.raycastTarget = false;

                nextCardButton = nextBtnObj.GetComponent<Button>();
            }

            if (nextCardButton != null)
            {
                nextCardButton.gameObject.SetActive(true);
                nextCardButton.onClick.RemoveAllListeners();
                nextCardButton.onClick.AddListener(NextCard);
            }

            if (replayVoiceAButton == null)
            {
                Transform r = transform.Find("ReplayButton") ?? transform.Find("Audio_Button") ?? transform.Find("Speaker_Button") ?? transform.Find("Replay_Button") ?? transform.Find("SpeakerButton");
                if (r == null)
                {
                    Button[] btns = GetComponentsInChildren<Button>(true);
                    foreach (var b in btns)
                    {
                        string bName = b.gameObject.name.ToLower();
                        if (bName.Contains("replay") || bName.Contains("speaker") || bName.Contains("audio"))
                        {
                            replayVoiceAButton = b;
                            break;
                        }
                    }
                }
                else
                {
                    replayVoiceAButton = r.GetComponent<Button>();
                }
            }
            if (replayVoiceAButton != null)
            {
                replayVoiceAButton.onClick.RemoveAllListeners();
                replayVoiceAButton.onClick.AddListener(() =>
                {
                    StartCoroutine(PunchScale(replayVoiceAButton.transform, 1.15f));
                    ReplayCurrentCardAudio();
                });
            }
        }

        public void ShowCard(int index)
        {
            currentCardIndex = index;
            clickedCardsOnPage.Clear();
            ClearCardContainer();

            switch (index)
            {
                case 0:
                    totalInteractiveCardsOnPage = 3;
                    RenderCard1();
                    break;
                case 1:
                    totalInteractiveCardsOnPage = 3;
                    RenderCard2();
                    break;
                case 2:
                    totalInteractiveCardsOnPage = 2;
                    RenderCard3();
                    break;
                case 3:
                    totalInteractiveCardsOnPage = 2;
                    RenderCard4();
                    break;
            }

            // Ensure Next Card Button is visible and initially locked
            if (nextCardButton != null)
            {
                nextCardButton.gameObject.SetActive(true);
                nextCardButton.interactable = false;
                var btnTxt = nextCardButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (btnTxt != null)
                {
                    btnTxt.text = (index < TOTAL_CARDS - 1) ? "<b>NEXT</b>" : "<b>NEXT ACTIVITY</b>";
                }
            }

            AudioClip clip = GetCardAudio(index);
            float audioDuration = 1.5f;

            if (clip != null)
            {
                audioDuration = clip.length;
                if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
                }
            }

            // Lock Next Button while voice audio plays, then unlock when narration ends
            if (enableNextRoutine != null) StopCoroutine(enableNextRoutine);
            enableNextRoutine = StartCoroutine(EnableNextButtonAfterAudio(audioDuration));
        }

        private IEnumerator EnableNextButtonAfterAudio(float duration)
        {
            if (nextCardButton != null)
            {
                nextCardButton.gameObject.SetActive(true);
                nextCardButton.interactable = false;
            }

            // Wait for narration to finish
            yield return new WaitForSeconds(Mathf.Max(0.5f, duration));

            UnlockNextButton();
        }

        public void UnlockNextButton()
        {
            if (nextCardButton != null && !nextCardButton.interactable)
            {
                nextCardButton.gameObject.SetActive(true);
                nextCardButton.interactable = true;
                StartCoroutine(PunchScale(nextCardButton.transform, 1.18f));
            }
        }

        private void RegisterCardClicked(int cardSubIndex)
        {
            clickedCardsOnPage.Add(cardSubIndex);
            if (clickedCardsOnPage.Count >= totalInteractiveCardsOnPage)
            {
                UnlockNextButton();
            }
        }

        private void ClearCardContainer()
        {
            if (dynamicCardContainer == null) return;
            for (int i = dynamicCardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(dynamicCardContainer.GetChild(i).gameObject);
            }
        }

        // -------------------------------------------------------------
        // CARD 1: The Closed Door (Page 26)
        // -------------------------------------------------------------
        private void RenderCard1()
        {
            SetHeader("A CONSONANT SHUTS THE VOWEL IN", "A closed syllable ends in a consonant. The door is shut!");

            GameObject box = CreateCardBase(dynamicCardContainer, 3, new Vector2(480f, 480f));
            CreateInteractiveWordDoorCard(box.transform, 0, "go", "got", "The 't' shuts the door!\n'o' becomes short.", "#C62828", SyllableDoorType.Closed);
            CreateInteractiveWordDoorCard(box.transform, 1, "me", "met", "In 'met', the 'e' says\nshort sound /e/.", "#1565C0", SyllableDoorType.Closed);
            CreateInteractiveWordDoorCard(box.transform, 2, "bas", "basket", "In 'basket', both\nsyllables are closed!", "#2E7D32", SyllableDoorType.Closed);
        }

        // -------------------------------------------------------------
        // CARD 2: The Open Gate (Page 28)
        // -------------------------------------------------------------
        private void RenderCard2()
        {
            SetHeader("NO CONSONANT, NO PROBLEM!", "An open syllable ends in a vowel. The vowel says its name!");

            GameObject box = CreateCardBase(dynamicCardContainer, 3, new Vector2(480f, 480f));
            CreateInteractiveWordDoorCard(box.transform, 0, "got", "go", "The 't' leaves -> gate opens!\n'o' says its name!", "#1565C0", SyllableDoorType.Open);
            CreateInteractiveWordDoorCard(box.transform, 1, "met", "me", "In 'me', 'e' is open\nand says /ee/!", "#0288D1", SyllableDoorType.Open);
            CreateInteractiveWordDoorCard(box.transform, 2, "be", "began", "In 'began', 'be' is open,\n'gan' is closed!", "#E65100", SyllableDoorType.Open);
        }

        // -------------------------------------------------------------
        // CARD 3: Side by Side (Page 30 Comparison)
        // -------------------------------------------------------------
        private void RenderCard3()
        {
            SetHeader("OPEN VS CLOSED SYLLABLES", "Tap each wheel to hear the open and closed contrast!");

            GameObject box = CreateCardBase(dynamicCardContainer, 2, new Vector2(680f, 480f));
            CreateComparisonWheelCard(box.transform, 0, "OPEN SYLLABLES", new[] { "so", "go", "he", "be", "hi", "me", "we", "flu" }, "#1565C0", SyllableDoorType.Open);
            CreateComparisonWheelCard(box.transform, 1, "CLOSED SYLLABLES", new[] { "not", "club", "sob", "hip", "got", "hen", "bed", "shed" }, "#C62828", SyllableDoorType.Closed);
        }

        // -------------------------------------------------------------
        // CARD 4: The First Door (Transfer Card - Page 31)
        // -------------------------------------------------------------
        private void RenderCard4()
        {
            SetHeader("LONG WORDS HAVE DOORS TOO", "Look at the FIRST syllable: Where the first door sits changes the vowel!");

            GameObject box = CreateCardBase(dynamicCardContainer, 2, new Vector2(680f, 480f));
            CreateFirstDoorTransferCard(box.transform, 0, "bacon", "ba - con", "OPEN FIRST -> Long a\n(BAY - con)", "#1565C0", SyllableDoorType.Open);
            CreateFirstDoorTransferCard(box.transform, 1, "basket", "bas - ket", "CLOSED FIRST -> Short a\n(BAS - ket)", "#C62828", SyllableDoorType.Closed);
        }

        private void SetHeader(string title, string desc)
        {
            if (titleTMP != null)
            {
                titleTMP.text = $"<b><color=#FFFFFF>{title}</color></b>";
                titleTMP.enableAutoSizing = true;
                titleTMP.fontSizeMin = 28;
                titleTMP.fontSizeMax = 44;
            }
            if (descriptionTMP != null)
            {
                descriptionTMP.text = $"<color=#FFFFFF><b>{desc}</b></color>";
                descriptionTMP.enableAutoSizing = true;
                descriptionTMP.fontSizeMin = 22;
                descriptionTMP.fontSizeMax = 32;
            }
        }

        private GameObject CreateCardBase(Transform parent, int columnCount, Vector2 cellSize)
        {
            GameObject box = new GameObject("CardBox", typeof(RectTransform), typeof(GridLayoutGroup));
            box.transform.SetParent(parent, false);
            RectTransform rt = box.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -25f);
            rt.sizeDelta = new Vector2(1540f, 500f);

            var glg = box.GetComponent<GridLayoutGroup>();
            glg.cellSize = cellSize;
            glg.spacing = new Vector2(35f, 20f);
            glg.childAlignment = TextAnchor.MiddleCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = columnCount;
            return box;
        }

        private void CreateInteractiveWordDoorCard(Transform parent, int subIndex, string baseWord, string targetWord, string ruleText, string headerColor, SyllableDoorType doorState)
        {
            GameObject card = new GameObject("DoorCard_" + targetWord, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);

            Image img = card.GetComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var tmp = CreateCenteredTMP(card.transform);
            string doorIconText = doorState == SyllableDoorType.Closed ? "[ Consonant Shuts Door ]" : "[ Gate Swings Open ]";
            tmp.text = $"<b><size=56><color={headerColor}>{targetWord}</color></size></b>\n<size=30><color=#546E7A><b>{baseWord} -> {targetWord}</b></color></size>\n\n<size=30><color=#212121><b>{ruleText}</b></color></size>\n\n<size=24><color={headerColor}><b>{doorIconText}</b></color></size>";

            Button btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(card.transform, 1.12f));
                RegisterCardClicked(subIndex);

                if (doorState == SyllableDoorType.Closed)
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayDoorSlam();
                else
                    U5_SA_AudioManager_Masters_Phonics.Instance?.PlayGateOpen();

                PlayWordAudio(targetWord);
            });
        }

        private void CreateComparisonWheelCard(Transform parent, int subIndex, string columnTitle, string[] words, string headerColor, SyllableDoorType type)
        {
            GameObject card = new GameObject("WheelCard_" + columnTitle, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);

            Image img = card.GetComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var tmp = CreateCenteredTMP(card.transform);
            string wordList = string.Join("  •  ", words);
            string rule = type == SyllableDoorType.Open 
                ? "Ends in a vowel · Long sound\nVowel says its name!" 
                : "Ends in a consonant · Short sound\nConsonant shuts door!";

            tmp.text = $"<b><size=44><color={headerColor}>{columnTitle}</color></size></b>\n<size=26><color=#37474F><b>{rule}</b></color></size>\n\n<size=32><color=#1A237E><b>{wordList}</b></color></size>\n\n<size=24><color=#0288D1><b>[ Tap to Hear Sequence ]</b></color></size>";

            Button btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(card.transform, 1.12f));
                RegisterCardClicked(subIndex);
                StartSequence(words);
            });
        }

        private void CreateFirstDoorTransferCard(Transform parent, int subIndex, string word, string splitWord, string description, string headerColor, SyllableDoorType door)
        {
            GameObject card = new GameObject("TransferCard_" + word, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);

            Image img = card.GetComponent<Image>();
            img.sprite = GetRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var tmp = CreateCenteredTMP(card.transform);
            tmp.text = $"<b><size=58><color={headerColor}>{word}</color></size></b>\n<size=40><color=#37474F><b>{splitWord}</b></color></size>\n\n<size=30><color=#212121><b>{description}</b></color></size>\n\n<size=24><color=#0288D1><b>[ Tap to Hear Split ]</b></color></size>";

            Button btn = card.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                StartCoroutine(PunchScale(card.transform, 1.12f));
                RegisterCardClicked(subIndex);
                PlayWordAudio(word);
            });
        }

        private Coroutine sequenceRoutine;

        private void StartSequence(string[] words)
        {
            if (sequenceRoutine != null) StopCoroutine(sequenceRoutine);
            sequenceRoutine = StartCoroutine(PlayWordSequenceRoutine(words));
        }

        private IEnumerator PlayWordSequenceRoutine(string[] words)
        {
            foreach (var w in words)
            {
                PlayWordAudio(w);
                yield return new WaitForSeconds(0.85f);
            }
        }

        private TextMeshProUGUI CreateCenteredTMP(Transform parent)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(parent, false);
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 8);
            textRT.offsetMax = new Vector2(-10, -8);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        public void NextCard()
        {
            if (sequenceRoutine != null) StopCoroutine(sequenceRoutine);

            if (currentCardIndex < TOTAL_CARDS - 1)
            {
                currentCardIndex++;
                ShowCard(currentCardIndex);
            }
            else
            {
                if (U5_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                {
                    U5_SA_UnitFlowManager_Masters_Phonics.Instance.OnActivityComplete();
                }
            }
        }

        public void ReplayCurrentCardAudio()
        {
            AudioClip clip = GetCardAudio(currentCardIndex);
            if (U5_SA_AudioManager_Masters_Phonics.Instance != null && clip != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clip);
            }
        }

        private AudioClip GetCardAudio(int idx)
        {
            switch (idx)
            {
                case 0: return card1VoiceClip ?? Resources.Load<AudioClip>("U5_audio/voice A/Watch Go The gates open so the o says") ?? Resources.Load<AudioClip>("U5_audio/Watch Go The gates open so the o says");
                case 1: return card2VoiceClip ?? Resources.Load<AudioClip>("U5_audio/voice A/Take the t away and the gate swings open") ?? Resources.Load<AudioClip>("U5_audio/Take the t away and the gate swings open");
                case 2: return card3VoiceClip ?? Resources.Load<AudioClip>("U5_audio/voice A/Open on the left closed on the right") ?? Resources.Load<AudioClip>("U5_audio/Open on the left closed on the right");
                case 3: return card4VoiceClip ?? Resources.Load<AudioClip>("U5_audio/voice A/Heres the useful bit In a long word") ?? Resources.Load<AudioClip>("U5_audio/Heres the useful bit In a long word");
                default: return null;
            }
        }

        private void PlayWordAudio(string word)
        {
            if (string.IsNullOrEmpty(word)) return;
            word = word.Trim().ToLower();

            AudioClip clip = Resources.Load<AudioClip>($"U5_audio/voice B/{word}")
                          ?? Resources.Load<AudioClip>($"U5_audio/{word}")
                          ?? Resources.Load<AudioClip>($"U5_audio/words MP U5/{word}")
                          ?? Resources.Load<AudioClip>($"Audio/U5_audio/voice B/{word}")
                          ?? Resources.Load<AudioClip>($"U4_audio/words MP U4/{word}");

            if (clip != null && U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            }
            else if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayWordAudio(word);
            }
        }

        private Sprite GetRoundedSprite()
        {
            if (cardRoundedSprite != null) return cardRoundedSprite;
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            int size = 128;
            int radius = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
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
}
