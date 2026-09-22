using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U10_SA_GM01_ConceptCards_Masters_Phonics : MonoBehaviour
    {
        [Header("Card Navigation UI")]
        [SerializeField] private Button nextCardBtn;
        [SerializeField] private Button prevCardBtn;
        [SerializeField] private TextMeshProUGUI cardCounterText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Card Content Displays")]
        [SerializeField] private TextMeshProUGUI cardHeadingText;
        [SerializeField] private TextMeshProUGUI cardSubHeadingText;
        [SerializeField] private TextMeshProUGUI cardBodyText;
        [SerializeField] private Transform exampleRowsContainer;
        [SerializeField] private Button replayCardAudioBtn;

        [Header("Card 1 Morphology Split Interactive Elements")]
        [SerializeField] private GameObject morphologyDemoGroup;
        [SerializeField] private TextMeshProUGUI splitPrefixLabel;
        [SerializeField] private TextMeshProUGUI splitRootLabel;

        [Header("Card 3 Poem Interactive Player")]
        [SerializeField] private GameObject poemPlayerGroup;
        [SerializeField] private TextMeshProUGUI poemLine1Text;
        [SerializeField] private TextMeshProUGUI poemLine2Text;
        [SerializeField] private Button playPoemVerseBtn;

        private List<U10ConceptCard> cards = new List<U10ConceptCard>();
        private int currentCardIndex = 0;
        private int currentPoemVerseIndex = 0;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            StartConceptCards();
        }

        private void OnDisable()
        {
            U10_SA_UnitFlowManager_Masters_Phonics.Instance?.SetNextActivityButtonActive(false);
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;
            U10_UI_Utils.FormatHeaderTypography(root, "LEARN — MEANING & ROOTS", "Homonyms, Homophones & Homographs");

            if (nextCardBtn == null) nextCardBtn = FindButton(root, "NextCard_Btn", "NextBtn", "Btn_NextCard", "ContinueBtn");
            if (prevCardBtn == null) prevCardBtn = FindButton(root, "PrevCard_Btn", "PrevBtn", "Btn_PrevCard", "BackCardBtn");
            if (replayCardAudioBtn == null) replayCardAudioBtn = FindButton(root, "ReplayAudioBtn", "Speaker_Button", "AudioBtn", "SpeakerBtn");

            U10_UI_Utils.EnsureHUD(root, ref cardCounterText, ref scoreText);

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            // Card Displays (may be direct child or under Cards/Card)
            Transform cardTr = FindDeepChild(root, "Card") ?? root.Find("ConceptCard") ?? root.Find("Content");
            if (cardTr != null)
            {
                if (cardHeadingText == null) cardHeadingText = (FindDeepChild(cardTr, "Heading") ?? FindDeepChild(cardTr, "Title"))?.GetComponent<TextMeshProUGUI>();
                if (cardSubHeadingText == null) cardSubHeadingText = (FindDeepChild(cardTr, "SubHeading") ?? FindDeepChild(cardTr, "Subtitle"))?.GetComponent<TextMeshProUGUI>();
                if (cardBodyText == null) cardBodyText = (FindDeepChild(cardTr, "Body") ?? FindDeepChild(cardTr, "BodyText"))?.GetComponent<TextMeshProUGUI>();
                if (exampleRowsContainer == null) exampleRowsContainer = FindDeepChild(cardTr, "ExampleRows") ?? FindDeepChild(cardTr, "Examples");

                if (morphologyDemoGroup == null)
                {
                    Transform md = FindDeepChild(cardTr, "MorphologyDemo");
                    if (md != null) morphologyDemoGroup = md.gameObject;
                }

                if (poemPlayerGroup == null)
                {
                    Transform pp = FindDeepChild(cardTr, "PoemPlayer");
                    if (pp != null) poemPlayerGroup = pp.gameObject;
                }
            }

            if (poemPlayerGroup != null)
            {
                Transform ppTr = poemPlayerGroup.transform;
                if (poemLine1Text == null) poemLine1Text = (FindDeepChild(ppTr, "Line1") ?? FindDeepChild(ppTr, "Verse1"))?.GetComponent<TextMeshProUGUI>();
                if (poemLine2Text == null) poemLine2Text = (FindDeepChild(ppTr, "Line2") ?? FindDeepChild(ppTr, "Verse2"))?.GetComponent<TextMeshProUGUI>();
                if (playPoemVerseBtn == null) playPoemVerseBtn = FindButton(ppTr, "PlayVerseBtn", "NextVerseBtn", "Btn_Verse", "PoemPlayer");
            }

            if (nextCardBtn != null)
            {
                nextCardBtn.onClick.RemoveAllListeners();
                nextCardBtn.onClick.AddListener(OnNextCardTapped);
            }

            if (prevCardBtn != null)
            {
                prevCardBtn.onClick.RemoveAllListeners();
                prevCardBtn.onClick.AddListener(OnPrevCardTapped);
            }

            if (replayCardAudioBtn != null)
            {
                replayCardAudioBtn.onClick.RemoveAllListeners();
                replayCardAudioBtn.onClick.AddListener(ReplayCurrentCardAudio);
            }

            if (playPoemVerseBtn != null)
            {
                playPoemVerseBtn.onClick.RemoveAllListeners();
                playPoemVerseBtn.onClick.AddListener(PlayNextPoemVerse);
            }
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"Card/{name}") 
                           ?? root.Find($"Cards/Card/{name}") 
                           ?? root.Find($"Content/{name}") 
                           ?? root.Find($"HUD/{name}") 
                           ?? FindDeepChild(root, name);
                if (t != null)
                {
                    Button b = t.GetComponent<Button>() ?? t.GetComponentInChildren<Button>(true);
                    if (b != null) return b;
                }
            }
            return null;
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        public void StartConceptCards()
        {
            cards = U10_DataBank.GetConceptCards();
            currentCardIndex = 0;
            currentPoemVerseIndex = 0;
            LoadCard(0);
        }

        private void LoadCard(int index)
        {
            if (cards == null || cards.Count == 0 || index < 0 || index >= cards.Count) return;

            currentCardIndex = index;
            U10ConceptCard card = cards[index];

            if (cardHeadingText != null) cardHeadingText.text = $"<b>{card.heading}</b>";
            if (cardSubHeadingText != null) cardSubHeadingText.text = $"<b>{card.subHeading}</b>";
            if (cardBodyText != null) cardBodyText.text = card.bodyText;

            // Update Counter & Progress Bar
            if (cardCounterText != null)
            {
                cardCounterText.text = $"<b>Card {index + 1} of {cards.Count}</b>";
            }

            if (scoreText != null)
            {
                int sc = U10_SA_UnitFlowManager_Masters_Phonics.Instance != null 
                    ? U10_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore() 
                    : 0;
                scoreText.text = $"<b>Score: {sc}</b>";
            }

            if (progressBar != null)
            {
                U10_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
                progressBar.value = (float)(index + 1) / cards.Count;
            }

            if (prevCardBtn != null)
            {
                prevCardBtn.gameObject.SetActive(index > 0);
            }

            bool isLastCard = (index >= cards.Count - 1);
            if (nextCardBtn != null)
            {
                nextCardBtn.gameObject.SetActive(!isLastCard);
                var txt = nextCardBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (txt != null)
                {
                    txt.text = "<b>NEXT >></b>";
                }
            }

            if (U10_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U10_SA_UnitFlowManager_Masters_Phonics.Instance.SetNextActivityButtonActive(isLastCard);
            }

            // Interactive Groups
            if (morphologyDemoGroup != null) morphologyDemoGroup.SetActive(index == 0);
            if (poemPlayerGroup != null) poemPlayerGroup.SetActive(index == 2);

            // Populate Example Buttons
            PopulateExampleRow(card.exampleTerms);

            // Play Card Audio
            PlayCardAudio(card);
        }

        private void PopulateExampleRow(string[] examples)
        {
            if (exampleRowsContainer == null || examples == null) return;

            int childCount = exampleRowsContainer.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = exampleRowsContainer.GetChild(i);
                if (i < examples.Length)
                {
                    child.gameObject.SetActive(true);
                    var txt = child.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) txt.text = $"<b>{examples[i]}</b>";

                    var btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        string word = examples[i].Split('/')[0].Trim();
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() =>
                        {
                            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayWord(word);
                            StartCoroutine(CoPunchTransform(child));
                        });
                    }
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void PlayCardAudio(U10ConceptCard card)
        {
            if (U10_SA_AudioManager_Masters_Phonics.Instance != null && !string.IsNullOrEmpty(card.audioKey))
            {
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(card.audioKey);
            }
        }

        public void ReplayCurrentCardAudio()
        {
            if (currentCardIndex < cards.Count)
            {
                PlayCardAudio(cards[currentCardIndex]);
                if (replayCardAudioBtn != null)
                {
                    StartCoroutine(CoPunchTransform(replayCardAudioBtn.transform));
                }
            }
        }

        public void OnNextCardTapped()
        {
            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayClick();

            if (currentCardIndex < cards.Count - 1)
            {
                LoadCard(currentCardIndex + 1);
            }
            else
            {
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.SetNextActivityButtonActive(true);
            }
        }

        public void OnPrevCardTapped()
        {
            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayClick();
            if (currentCardIndex > 0)
            {
                LoadCard(currentCardIndex - 1);
            }
        }

        private void PlayNextPoemVerse()
        {
            string[] poemVerses = new string[]
            {
                "I have such a <color=#10B981><b>fit</b></color> (tantrum) / When these words don't <color=#10B981><b>fit</b></color> (match)",
                "And the lions feel they <color=#10B981><b>might</b></color> (perhaps) / Want to show their strength and <color=#10B981><b>might</b></color> (power)",
                "When the monkeys <color=#10B981><b>swing</b></color> (sway) / From a vine like a <color=#10B981><b>swing</b></color> (hanging seat)",
                "And the roar of the <color=#10B981><b>bear</b></color> (animal) / Is it too loud for you to <color=#10B981><b>bear</b></color> (endure)?"
            };

            currentPoemVerseIndex = (currentPoemVerseIndex + 1) % poemVerses.Length;
            string verse = poemVerses[currentPoemVerseIndex];
            string[] parts = verse.Split('/');

            if (poemLine1Text != null && parts.Length > 0) poemLine1Text.text = parts[0].Trim();
            if (poemLine2Text != null && parts.Length > 1) poemLine2Text.text = parts[1].Trim();

            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayClick();
            if (playPoemVerseBtn != null) StartCoroutine(CoPunchTransform(playPoemVerseBtn.transform));
        }

        private IEnumerator CoPunchTransform(Transform target)
        {
            if (target == null) yield break;
            Vector3 orig = Vector3.one;
            target.localScale = orig * 1.15f;
            yield return new WaitForSeconds(0.12f);
            if (target != null) target.localScale = orig;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 10 Concept Cards] Auto-Assigned Hierarchy & Buttons!</b></color>");
        }
#endif
    }
}
