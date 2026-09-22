using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U2_SA_GM01_ConceptCards_Masters_Phonics : MonoBehaviour
    {
        [Header("1. Mascot Voice A Clips")]
        public AudioClip voiceACard1; // "An inflectional ending goes on the end..."
        public AudioClip voiceACard2; // "Eight endings, eight jobs..."
        public AudioClip voiceACard3; // "Most words just take an s..."
        public AudioClip voiceACard4; // "Three spelling changes..."

        [Header("2. Scene UI References (Auto-Bound)")]
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI descriptionTMP;
        [SerializeField] private Transform dynamicCardContainer;
        [SerializeField] private Button nextCardButton;
        [SerializeField] private Button replayAudioButton;

        [Header("3. Custom Manual Card Displays (Optional: Drag in from Hierarchy)")]
        [Tooltip("If you place Card1..4 objects manually in the Hierarchy, drag them here. If left empty, they will auto-generate.")]
        [SerializeField] private GameObject manualCard1;
        [SerializeField] private GameObject manualCard2;
        [SerializeField] private GameObject manualCard3;
        [SerializeField] private GameObject manualCard4;

        [Header("4. Rounded Sprites (From Sprite Sheet)")]
        public Sprite roundedCardFrameBlue;   // Top-Left Large Blue Frame
        public Sprite roundedCardFrameGreen;  // Top-Right Large Green Frame
        public Sprite roundedEndingPillBlue;  // The 8 Blue/Gold Rounded Badges
        public Sprite roundedFrameOrange;     // Bottom Small Orange Frame
        public Sprite roundedFrameCyan;       // Bottom Small Cyan Frame
        public Sprite roundedFramePurple;     // Bottom Small Purple Frame

        [Header("5. 8 Ending Cards Audio Clips (Voice B)")]
        [Tooltip("Card 1: Plural (-s / -es) -> 'the tables, boxes'")]
        public AudioClip audioEndingPlural;        // -s / -es

        [Tooltip("Card 2: Possessive ('s) -> 'dad's bag'")]
        public AudioClip audioEndingPossessive;    // 's

        [Tooltip("Card 3: 3rd Person Verb (-s) -> 'Sharon sings'")]
        public AudioClip audioEndingVerbS;         // -s

        [Tooltip("Card 4: Present Continuous (-ing) -> 'Leah is skating'")]
        public AudioClip audioEndingIng;           // -ing

        [Tooltip("Card 5: Past Tense (-ed) -> 'Aaron slipped'")]
        public AudioClip audioEndingEd;            // -ed

        [Tooltip("Card 6: Participle (-en) -> 'Steve has fallen'")]
        public AudioClip audioEndingEn;            // -en

        [Tooltip("Card 7: Comparative (-er) -> 'Joe is stronger'")]
        public AudioClip audioEndingEr;            // -er

        [Tooltip("Card 8: Superlative (-est) -> 'Vrinda is tallest'")]
        public AudioClip audioEndingEst;           // -est

        [Header("6. Additional Examples (Voice B)")]
        public AudioClip audioWalk;
        public AudioClip audioWalked;
        public AudioClip audioBus;
        public AudioClip audioBuses;

        private int currentCardIndex = 0;

        private void Awake()
        {
            AutoBindUI();
        }

        private void OnEnable()
        {
            AutoBindUI();
            currentCardIndex = 0;
            ShowCard(currentCardIndex);
        }

        private void AutoBindUI()
        {
            if (titleTMP == null)
            {
                var t = transform.Find("TitleText") ?? transform.Find("Title_Text") ?? transform.Find("Header_Title");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (descriptionTMP == null)
            {
                var t = transform.Find("DescriptionText") ?? transform.Find("Desc_Text") ?? transform.Find("SubTitle_Text");
                if (t != null) descriptionTMP = t.GetComponent<TextMeshProUGUI>();
            }

            if (nextCardButton == null)
            {
                Button[] allBtns = GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns)
                {
                    if (b == null) continue;
                    string bName = b.name.ToLower();
                    if (bName.Contains("next") || bName.Contains("continue") || bName.Contains("arrow"))
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
                }
                else
                {
                    GameObject go = new GameObject("DynamicCardContainer", typeof(RectTransform));
                    go.transform.SetParent(transform, false);
                    RectTransform rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.15f, 0.18f);
                    rt.anchorMax = new Vector2(0.85f, 0.72f);
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    dynamicCardContainer = go.transform;
                }
            }
        }

        public void ShowCard(int cardIndex)
        {
            currentCardIndex = cardIndex;
            AutoBindUI();

            // Disable all manual and dynamic cards first
            if (manualCard1 != null) manualCard1.SetActive(false);
            if (manualCard2 != null) manualCard2.SetActive(false);
            if (manualCard3 != null) manualCard3.SetActive(false);
            if (manualCard4 != null) manualCard4.SetActive(false);

            if (dynamicCardContainer != null)
            {
                foreach (Transform child in dynamicCardContainer)
                {
                    child.gameObject.SetActive(false);
                }
            }

            TMP_FontAsset font = titleTMP != null ? titleTMP.font : null;

            switch (cardIndex)
            {
                case 0:
                    SetHeader("Same Word, New Shape", "An inflectional ending changes number, tense or comparison—it doesn't make a new word!");
                    if (manualCard1 != null) manualCard1.SetActive(true);
                    else RenderCard1(font);
                    break;

                case 1:
                    SetHeader("The 8 Endings & Their Jobs", "Tap any row to hear its sound and example sentence:");
                    if (manualCard2 != null)
                    {
                        manualCard2.SetActive(true);
                        BindManualCardButtons(manualCard2);
                    }
                    else RenderCard2(font);
                    break;

                case 2:
                    SetHeader("Making Plurals (-s & -es)", "Add -es after s, ss, sh, ch, x, z to add the extra syllable sound!");
                    if (manualCard3 != null) manualCard3.SetActive(true);
                    else RenderCard3(font);
                    break;

                case 3:
                    SetHeader("3 Big Spelling Rules", "Watch what happens to the base word before adding an ending:");
                    if (manualCard4 != null) manualCard4.SetActive(true);
                    else RenderCard4(font);
                    break;
            }

            if (nextCardButton != null)
            {
                nextCardButton.gameObject.SetActive(true);
                StartCoroutine(PulseNextButtonDelayed(1.5f));
            }

            AudioClip voice = GetVoiceAClip(cardIndex);
            if (U2_SA_AudioManager_Masters_Phonics.Instance != null && voice != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(voice);
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

        private AudioClip GetVoiceAClip(int index)
        {
            switch (index)
            {
                case 0: return voiceACard1;
                case 1: return voiceACard2;
                case 2: return voiceACard3;
                case 3: return voiceACard4;
                default: return null;
            }
        }

        private void RenderCard1(TMP_FontAsset font)
        {
            Transform existing = dynamicCardContainer.Find("Card1_Display");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            GameObject card = new GameObject("Card1_Display", typeof(RectTransform));
            card.transform.SetParent(dynamicCardContainer, false);
            SetFullStretch(card.GetComponent<RectTransform>());

            HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 25;
            hlg.padding = new RectOffset(15, 15, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            // Left Side: Unit 1 Suffix (New Word)
            CreateBadgeBox(card.transform, "UNIT 1: SUFFIX", "kind + ness -> kindness", "Makes a NEW word!", new Color(0.9f, 0.95f, 1f, 0.95f), new Color(0.08f, 0.22f, 0.45f), new Color(0.15f, 0.2f, 0.3f), font);

            // Right Side: Unit 2 Ending (Same Word)
            CreateBadgeBox(card.transform, "UNIT 2: ENDING", "walk + ed -> walked", "Changes the SAME word (Past)", new Color(0.92f, 0.98f, 0.9f, 0.95f), new Color(0.06f, 0.35f, 0.18f), new Color(0.15f, 0.2f, 0.3f), font);
        }        private void RenderCard2(TMP_FontAsset font)
        {
            Transform existing = dynamicCardContainer.Find("Card2_Display");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            GameObject card = new GameObject("Card2_Display", typeof(RectTransform));
            card.transform.SetParent(dynamicCardContainer, false);
            SetFullStretch(card.GetComponent<RectTransform>());

            GridLayoutGroup glg = card.AddComponent<GridLayoutGroup>();
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;
            glg.cellSize = new Vector2(430, 210); // Generous size to fill the 2000x600 container
            glg.spacing = new Vector2(25, 20);
            glg.padding = new RectOffset(20, 20, 15, 15);
            glg.childAlignment = TextAnchor.MiddleCenter;

            CreateEndingButton(card.transform, "-s / -es", "Plural (more than one)", "the tables / boxes", font);
            CreateEndingButton(card.transform, "'s", "Possessive (belongs to)", "dad's bag", font);
            CreateEndingButton(card.transform, "-s", "3rd Person Verb", "Sharon sings", font);
            CreateEndingButton(card.transform, "-ing", "Happening Now", "Leah is skating", font);
            CreateEndingButton(card.transform, "-ed", "Past (already happened)", "Aaron slipped", font);
            CreateEndingButton(card.transform, "-en", "Participle (has happened)", "Steve has fallen", font);
            CreateEndingButton(card.transform, "-er", "Comparing Two", "Joe is stronger", font);
            CreateEndingButton(card.transform, "-est", "Comparing Three+", "Vrinda is tallest", font);
        }

        private void RenderCard3(TMP_FontAsset font)
        {
            Transform existing = dynamicCardContainer.Find("Card3_Display");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            GameObject card = new GameObject("Card3_Display", typeof(RectTransform));
            card.transform.SetParent(dynamicCardContainer, false);
            SetFullStretch(card.GetComponent<RectTransform>());

            VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.padding = new RectOffset(30, 30, 20, 20);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            CreateBadgeBox(card.transform, "STANDARD PLURAL (+s)", "dog -> dogs   |   table -> tables   |   plant -> plants", "Most nouns just add -s", new Color(0.92f, 0.98f, 0.92f, 0.95f), new Color(0.08f, 0.35f, 0.15f), new Color(0.12f, 0.2f, 0.15f), font);
            CreateBadgeBox(card.transform, "EXTRA SYLLABLE (+es)", "bus -> buses   |   box -> boxes   |   dish -> dishes", "Ends in s, ss, sh, ch, x, z -> add -es", new Color(1f, 0.92f, 0.92f, 0.95f), new Color(0.55f, 0.1f, 0.1f), new Color(0.25f, 0.12f, 0.12f), font);
        }

        private void RenderCard4(TMP_FontAsset font)
        {
            Transform existing = dynamicCardContainer.Find("Card4_Display");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return;
            }

            GameObject card = new GameObject("Card4_Display", typeof(RectTransform));
            card.transform.SetParent(dynamicCardContainer, false);
            SetFullStretch(card.GetComponent<RectTransform>());

            HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(20, 20, 20, 20);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            CreateBadgeBox(card.transform, "1. DOUBLE IT", "hop -> hopping\nbat -> batted", "1 syllable, 1 vowel + 1 consonant", new Color(1f, 0.96f, 0.88f, 0.95f), new Color(0.5f, 0.28f, 0.05f), new Color(0.25f, 0.18f, 0.1f), font);
            CreateBadgeBox(card.transform, "2. DROP THE E", "like -> liked\nnice -> nicer", "Silent e + vowel ending", new Color(0.9f, 0.96f, 1f, 0.95f), new Color(0.08f, 0.3f, 0.5f), new Color(0.12f, 0.2f, 0.3f), font);
            CreateBadgeBox(card.transform, "3. Y TO I", "cry -> cried\nstory -> stories", "Consonant + y (y stays before -ing)", new Color(0.96f, 0.9f, 1f, 0.95f), new Color(0.4f, 0.1f, 0.5f), new Color(0.22f, 0.1f, 0.25f), font);
        }

        private void CreateBadgeBox(Transform parent, string title, string formula, string ruleNote, Color bgColor, Color titleColor, Color textColor, TMP_FontAsset font)
        {
            GameObject box = new GameObject(title, typeof(RectTransform), typeof(Image));
            box.transform.SetParent(parent, false);
            Image img = box.GetComponent<Image>();

            if (roundedCardFrameBlue != null)
            {
                img.sprite = roundedCardFrameBlue;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                img.color = bgColor;
            }

            VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 12;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            tObj.transform.SetParent(box.transform, false);
            var tTMP = tObj.GetComponent<TextMeshProUGUI>();
            if (font != null) tTMP.font = font;
            tTMP.text = $"<size=130%><b>{title}</b></size>";
            tTMP.alignment = TextAlignmentOptions.Center;
            tTMP.color = titleColor;

            GameObject fObj = new GameObject("Formula", typeof(RectTransform), typeof(TextMeshProUGUI));
            fObj.transform.SetParent(box.transform, false);
            var fTMP = fObj.GetComponent<TextMeshProUGUI>();
            if (font != null) fTMP.font = font;
            fTMP.text = $"<size=150%><b>{formula}</b></size>";
            fTMP.alignment = TextAlignmentOptions.Center;
            fTMP.color = new Color(0.08f, 0.08f, 0.12f); // Deep Bold Black

            GameObject rObj = new GameObject("Rule", typeof(RectTransform), typeof(TextMeshProUGUI));
            rObj.transform.SetParent(box.transform, false);
            var rTMP = rObj.GetComponent<TextMeshProUGUI>();
            if (font != null) rTMP.font = font;
            rTMP.text = $"<size=115%><b>{ruleNote}</b></size>";
            rTMP.alignment = TextAlignmentOptions.Center;
            rTMP.color = new Color(0.15f, 0.15f, 0.2f); // Dark Charcoal
        }

        private void CreateEndingButton(Transform parent, string ending, string job, string example, TMP_FontAsset font)
        {
            GameObject btnObj = new GameObject(ending, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            Image img = btnObj.GetComponent<Image>();

            if (roundedEndingPillBlue != null)
            {
                img.sprite = roundedEndingPillBlue;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else
            {
                // Soft tinted pastel card with strong dark border feel
                img.color = new Color(0.88f, 0.94f, 1f, 0.95f);
            }

            VerticalLayoutGroup vlg = btnObj.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 14, 14);
            vlg.spacing = 6;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            // 1. Ending Header (Deep Royal Blue / Black)
            GameObject topObj = new GameObject("EndingTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            topObj.transform.SetParent(btnObj.transform, false);
            var topTMP = topObj.GetComponent<TextMeshProUGUI>();
            if (font != null) topTMP.font = font;
            topTMP.text = $"<color=#0A2E6B><b><size=140%>{ending}</size></b></color>";
            topTMP.alignment = TextAlignmentOptions.Center;

            // 2. Grammatical Function (Dark Slate Blue)
            GameObject jobObj = new GameObject("JobText", typeof(RectTransform), typeof(TextMeshProUGUI));
            jobObj.transform.SetParent(btnObj.transform, false);
            var jobTMP = jobObj.GetComponent<TextMeshProUGUI>();
            if (font != null) jobTMP.font = font;
            jobTMP.text = $"<color=#1C4E80><size=105%><b>{job}</b></size></color>";
            jobTMP.alignment = TextAlignmentOptions.Center;

            // 3. Concrete Example (Deep Bold Black)
            GameObject botObj = new GameObject("ExampleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            botObj.transform.SetParent(btnObj.transform, false);
            var botTMP = botObj.GetComponent<TextMeshProUGUI>();
            if (font != null) botTMP.font = font;
            botTMP.text = $"<color=#111118><size=115%><b><i>\"{example}\"</i></b></size></color>";
            botTMP.alignment = TextAlignmentOptions.Center;

            Button b = btnObj.GetComponent<Button>();
            b.onClick.AddListener(() => {
                StartCoroutine(PunchScale(btnObj.transform));
                PlayEndingCardAudio(ending, example);
            });
        }

        private void BindManualCardButtons(GameObject cardObj)
        {
            if (cardObj == null) return;
            Button[] buttons = cardObj.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                string btnName = btn.name;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    StartCoroutine(PunchScale(btn.transform));
                    PlayEndingCardAudio(btnName, btnName);
                });
            }
        }

        private void PlayEndingCardAudio(string ending, string example)
        {
            if (U2_SA_AudioManager_Masters_Phonics.Instance == null) return;

            AudioClip clip = null;
            string key = ending.ToLower();

            if (key.Contains("es") || key.Contains("plural") || (key.Contains("s") && key.Contains("/")))
                clip = audioEndingPlural;
            else if (key.Contains("'s") || key.Contains("possessive"))
                clip = audioEndingPossessive;
            else if (key.Contains("verb") || key.Contains("3rd") || key.Contains("sing"))
                clip = audioEndingVerbS;
            else if (key.Contains("ing") || key.Contains("skat"))
                clip = audioEndingIng;
            else if (key.Contains("ed") || key.Contains("slip") || key.Contains("past"))
                clip = audioEndingEd;
            else if (key.Contains("en") || key.Contains("fall") || key.Contains("participle"))
                clip = audioEndingEn;
            else if (key.Contains("er") || key.Contains("strong") || key.Contains("compar"))
                clip = audioEndingEr;
            else if (key.Contains("est") || key.Contains("tall") || key.Contains("superl"))
                clip = audioEndingEst;

            // Fallback to Resources if not assigned in Inspector
            if (clip == null)
            {
                string cleanEnding = ending.Replace("-", "").Replace("'", "").Replace(" ", "").Replace("/", "_").ToLower();
                clip = Resources.Load<AudioClip>($"U2_audio/{cleanEnding}") 
                    ?? Resources.Load<AudioClip>($"Audio/U2_audio/{cleanEnding}")
                    ?? Resources.Load<AudioClip>($"U2_audio/{example.Split(' ')[0]}")
                    ?? Resources.Load<AudioClip>($"Audio/U2_audio/{example.Split(' ')[0]}");
            }

            if (clip != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(clip);
            }
            else
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayCorrect();
            }
        }

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
            AudioClip voice = GetVoiceAClip(currentCardIndex);
            if (U2_SA_AudioManager_Masters_Phonics.Instance != null && voice != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(voice);
            }
        }

        private void OnConceptCardsComplete()
        {
            Debug.Log("<color=green>Concept Cards (Unit 2 Learn) Finished! Launching Activity 1...</color>");
            if (U2_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U2_SA_UnitFlowManager_Masters_Phonics.Instance.OpenActivity1();
            }
        }

        private void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private IEnumerator PunchScale(Transform tr)
        {
            if (tr == null) yield break;
            Vector3 orig = Vector3.one;
            tr.localScale = orig * 1.15f;
            yield return new WaitForSeconds(0.15f);
            if (tr != null) tr.localScale = orig;
        }
    }
}
