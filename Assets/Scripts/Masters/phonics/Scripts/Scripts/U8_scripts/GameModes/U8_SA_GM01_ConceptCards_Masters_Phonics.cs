using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U8_SA_GM01_ConceptCards_Masters_Phonics : MonoBehaviour
    {
        [Header("Card Views (4 Cards)")]
        [SerializeField] private GameObject card1GO; // Bossy R character & rule
        [SerializeField] private GameObject card2GO; // 5 Spellings Grid (ar, or, er, ir, ur)
        [SerializeField] private GameObject card3GO; // 5 Spellings -> 3 Sounds (her, bird, turn merge)
        [SerializeField] private GameObject card4GO; // The -er Rule (Quiet ending /ər/ -> er)

        [Header("Navigation Buttons")]
        [SerializeField] private Button nextCardBtn;
        [SerializeField] private Button prevCardBtn;
        [SerializeField] private Button startAct1Btn; // "Meet the Boss" / "Start Activity 1"
        [SerializeField] private Button replayAudioBtn;

        [Header("Card 2 - Interactive Grid Tiles")]
        [SerializeField] private Button arTileBtn;
        [SerializeField] private Button orTileBtn;
        [SerializeField] private Button erTileBtn;
        [SerializeField] private Button irTileBtn;
        [SerializeField] private Button urTileBtn;

        [Header("Card 3 - Merging Chunks Animation")]
        [SerializeField] private RectTransform chunkHer;
        [SerializeField] private RectTransform chunkBird;
        [SerializeField] private RectTransform chunkTurn;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI cardIndicatorText;
        [SerializeField] private Slider cardProgressSlider;

        private int currentCardIndex = 1;
        private Coroutine activeCardRoutine;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            ShowCard(1);
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;

            if (card1GO == null) card1GO = FindChildGO(root, "Card_1", "Card1", "Card_BossyR");
            if (card2GO == null) card2GO = FindChildGO(root, "Card_2", "Card2", "Card_FiveSpellings");
            if (card3GO == null) card3GO = FindChildGO(root, "Card_3", "Card3", "Card_ThreeSounds");
            if (card4GO == null) card4GO = FindChildGO(root, "Card_4", "Card4", "Card_ErRule");

            if (nextCardBtn == null) nextCardBtn = FindButton(root, "NextBtn", "NextCardBtn", "ForwardBtn", "Next_Btn", "Next");
            if (prevCardBtn == null) prevCardBtn = FindButton(root, "PrevBtn", "PrevCardBtn", "BackBtn", "Prev_Btn", "Prev", "Back");
            if (startAct1Btn == null) startAct1Btn = FindButton(root, "StartActivity1Btn", "MeetTheBossBtn", "StartAct1Btn");
            if (replayAudioBtn == null) replayAudioBtn = FindButton(root, "ReplayAudioBtn", "ReplayBtn", "Speaker_Button", "Replay_Button");

            if (cardIndicatorText == null)
            {
                Transform t = root.Find("ProgressHUD/ProgressText")
                           ?? root.Find("ProgressHUD/Progress_Text")
                           ?? root.Find("CardIndicatorText") 
                           ?? root.Find("ProgressText") 
                           ?? root.Find("HUD/ProgressText")
                           ?? root.Find("HUD/CardText");
                if (t != null) cardIndicatorText = t.GetComponent<TextMeshProUGUI>();
            }
            if (cardProgressSlider == null)
            {
                cardProgressSlider = GetComponentInChildren<Slider>(true);
            }

            // Card 2 Grid Buttons
            if (card2GO != null)
            {
                if (arTileBtn == null) arTileBtn = FindButton(card2GO.transform, "Tile_ar", "ArBtn", "AR");
                if (orTileBtn == null) orTileBtn = FindButton(card2GO.transform, "Tile_or", "OrBtn", "OR");
                if (erTileBtn == null) erTileBtn = FindButton(card2GO.transform, "Tile_er", "ErBtn", "ER");
                if (irTileBtn == null) irTileBtn = FindButton(card2GO.transform, "Tile_ir", "IrBtn", "IR");
                if (urTileBtn == null) urTileBtn = FindButton(card2GO.transform, "Tile_ur", "UrBtn", "UR");
            }

            AttachListeners();
        }

        private GameObject FindChildGO(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = FindRecursive(root, name);
                if (t != null) return t.gameObject;
            }
            return null;
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = FindRecursive(root, name);
                if (t != null)
                {
                    Button b = t.GetComponent<Button>();
                    if (b == null)
                    {
                        b = t.gameObject.AddComponent<Button>();
                    }
                    return b;
                }
            }
            return null;
        }

        private Transform FindRecursive(Transform root, string name)
        {
            if (root == null) return null;
            foreach (Transform child in root)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
                Transform found = FindRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void AttachListeners()
        {
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
            if (startAct1Btn != null)
            {
                startAct1Btn.onClick.RemoveAllListeners();
                startAct1Btn.onClick.AddListener(() =>
                {
                    U8_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity1();
                });
            }
            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentCardAudio);
            }

            // Card 2 grid listeners
            if (arTileBtn != null)
            {
                arTileBtn.onClick.RemoveAllListeners();
                arTileBtn.onClick.AddListener(() => PlayPatternModel("ar", "car"));
            }
            if (orTileBtn != null)
            {
                orTileBtn.onClick.RemoveAllListeners();
                orTileBtn.onClick.AddListener(() => PlayPatternModel("or", "corn"));
            }
            if (erTileBtn != null)
            {
                erTileBtn.onClick.RemoveAllListeners();
                erTileBtn.onClick.AddListener(() => PlayPatternModel("er", "pepper"));
            }
            if (irTileBtn != null)
            {
                irTileBtn.onClick.RemoveAllListeners();
                irTileBtn.onClick.AddListener(() => PlayPatternModel("ir", "bird"));
            }
            if (urTileBtn != null)
            {
                urTileBtn.onClick.RemoveAllListeners();
                urTileBtn.onClick.AddListener(() => PlayPatternModel("ur", "turtle"));
            }
        }

        public void ShowCard(int index)
        {
            currentCardIndex = Mathf.Clamp(index, 1, 4);

            if (card1GO != null) card1GO.SetActive(currentCardIndex == 1);
            if (card2GO != null) card2GO.SetActive(currentCardIndex == 2);
            if (card3GO != null) card3GO.SetActive(currentCardIndex == 3);
            if (card4GO != null) card4GO.SetActive(currentCardIndex == 4);

            // Small carpet buttons:
            // PrevBtn: Visible on Cards 2, 3, 4 (hidden on Card 1)
            // NextBtn: Visible on Cards 1, 2, 3 (hidden on Card 4)
            if (prevCardBtn != null)
            {
                prevCardBtn.gameObject.SetActive(currentCardIndex > 1);
                prevCardBtn.interactable = true;
                if (currentCardIndex > 1) prevCardBtn.transform.SetAsLastSibling();
            }
            if (nextCardBtn != null)
            {
                nextCardBtn.gameObject.SetActive(currentCardIndex < 4);
                nextCardBtn.interactable = true;
                if (currentCardIndex < 4) nextCardBtn.transform.SetAsLastSibling();
            }
            if (startAct1Btn != null)
            {
                startAct1Btn.gameObject.SetActive(false); // Small continue button not needed; Main Next Button is used!
            }

            // Hide Main Next Button while card audio is playing
            U8_SA_UnitFlowManager_Masters_Phonics.Instance?.HideNextActivityButton();

            if (cardIndicatorText != null)
                cardIndicatorText.text = $"<b>Card {currentCardIndex} of 4</b>";

            if (cardProgressSlider != null)
            {
                cardProgressSlider.maxValue = 4;
                cardProgressSlider.value = currentCardIndex;
                U8_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(cardProgressSlider);
            }

            if (activeCardRoutine != null) StopCoroutine(activeCardRoutine);
            activeCardRoutine = StartCoroutine(PlayCardVoiceSequence(currentCardIndex));
        }

        public void OnNextCardTapped()
        {
            if (currentCardIndex < 4)
            {
                ShowCard(currentCardIndex + 1);
            }
            else
            {
                // On Card 4, launches Activity 1 (Bossy R)
                U8_SA_UnitFlowManager_Masters_Phonics.Instance?.OpenActivity1();
            }
        }

        public void OnPrevCardTapped()
        {
            if (currentCardIndex > 1) ShowCard(currentCardIndex - 1);
        }

        public void ReplayCurrentCardAudio()
        {
            if (activeCardRoutine != null) StopCoroutine(activeCardRoutine);
            activeCardRoutine = StartCoroutine(PlayCardVoiceSequence(currentCardIndex));
        }

        private IEnumerator PlayCardVoiceSequence(int cardIdx)
        {
            U8_SA_AudioManager_Masters_Phonics.Instance?.StopAllSpeech();

            switch (cardIdx)
            {
                case 1:
                    AudioClip c1 = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_card1");
                    if (c1 != null)
                    {
                        U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(c1);
                        yield return new WaitForSeconds(c1.length + 0.3f);
                    }
                    AudioClip c1b = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_card1b");
                    if (c1b != null)
                    {
                        U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(c1b);
                    }
                    break;

                case 2:
                    AudioClip c2 = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_card2");
                    if (c2 != null)
                        U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(c2);
                    break;

                case 3:
                    AudioClip c3 = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_card3");
                    if (c3 != null)
                    {
                        U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(c3);
                        yield return new WaitForSeconds(c3.length + 0.3f);
                    }
                    U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("merge");
                    AudioClip c3b = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_card3b");
                    if (c3b != null)
                        U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(c3b);
                    break;

                case 4:
                    AudioClip c4 = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_card4");
                    if (c4 != null)
                    {
                        U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(c4);
                        yield return new WaitForSeconds(c4.length + 0.3f);
                    }
                    break;
            }

            if (cardIdx == 4)
            {
                // Reveal the Main Next Button so the player can proceed to Activity 1
                U8_SA_UnitFlowManager_Masters_Phonics.Instance?.ShowNextActivityButton();
            }
        }

        public int GetCurrentCardIndex() => currentCardIndex;

        private void PlayPatternModel(string pattern, string exampleWord)
        {
            StartCoroutine(PlayPatternModelRoutine(pattern, exampleWord));
        }

        private IEnumerator PlayPatternModelRoutine(string pattern, string exampleWord)
        {
            AudioClip chkClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio($"U08_CHK_{pattern}") ??
                                U8_SA_AudioManager_Masters_Phonics.ResolveAudio(pattern);
            if (chkClip != null)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(chkClip);
                yield return new WaitForSeconds(chkClip.length + 0.2f);
            }

            AudioClip wrdClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio($"U08_WRD_{exampleWord}") ??
                                U8_SA_AudioManager_Masters_Phonics.ResolveAudio(exampleWord);
            if (wrdClip != null)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(wrdClip);
            }
        }
    }
}
