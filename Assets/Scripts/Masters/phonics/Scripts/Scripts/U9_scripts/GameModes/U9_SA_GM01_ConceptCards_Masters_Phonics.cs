using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U9_SA_GM01_ConceptCards_Masters_Phonics : MonoBehaviour
    {
        [Header("Card Views (3 Cards)")]
        [SerializeField] private GameObject card1GO; // The Silent Partner
        [SerializeField] private GameObject card2GO; // The Turtle Rule (Count back 3)
        [SerializeField] private GameObject card3GO; // One Consonant or Two? (Open vs Closed)

        [Header("Navigation Buttons")]
        [SerializeField] private Button nextCardBtn;
        [SerializeField] private Button prevCardBtn;
        [SerializeField] private Button startAct1Btn; // "Count Back Three" / "Start Activity 1"
        [SerializeField] private Button replayAudioBtn;

        [Header("Audio Clips (Concept Cards)")]
        [SerializeField] private AudioClip card1VoiceClip;       // "Consonant plus le at the end of a.mp3"
        [SerializeField] private AudioClip card1SchwaAsideClip;   // "Although say turtle slowly Turtul Theres a tiny.mp3"
        [SerializeField] private AudioClip card2VoiceClip;       // "Heres the rule If a word ends in.mp3"
        [SerializeField] private AudioClip card3VoiceClip;       // "Count the consonants sitting just before the le.mp3"

        [Header("Card 1 - Interactive Schwa Aside & Turtle")]
        [SerializeField] private Button schwaAsideBtn;
        [SerializeField] private Button turtleInteractiveBtn;

        [Header("Card 2 - Interactive Examples")]
        [SerializeField] private Button puzzleExampleBtn;
        [SerializeField] private Button pickleExampleBtn;
        [SerializeField] private Button bubbleExampleBtn;

        [Header("Card 3 - Interactive Open vs Closed Pairs")]
        [SerializeField] private Button mapleApplePairBtn;
        [SerializeField] private Button nobleBottlePairBtn;
        [SerializeField] private Button buglePuzzlePairBtn;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI cardIndicatorText;
        [SerializeField] private Slider cardProgressSlider;
        [SerializeField] private TextMeshProUGUI scoreText;

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
            U9_UI_Utils.FormatHeaderTypography(root, "CONCEPT CARDS — CONSONANT + LE", "Consonant + le Syllables");

            if (card1GO == null) card1GO = FindChildGO(root, "Card_1", "Card1", "Card_SilentPartner");
            if (card2GO == null) card2GO = FindChildGO(root, "Card_2", "Card2", "Card_TurtleRule");
            if (card3GO == null) card3GO = FindChildGO(root, "Card_3", "Card3", "Card_OneOrTwo");

            if (card1GO != null && card1GO.GetComponent<Image>() != null) U9_UI_Utils.ApplyRoundedCardStyle(card1GO.GetComponent<Image>(), new Color(0.96f, 0.98f, 1f));
            if (card2GO != null && card2GO.GetComponent<Image>() != null) U9_UI_Utils.ApplyRoundedCardStyle(card2GO.GetComponent<Image>(), new Color(0.96f, 0.98f, 1f));
            if (card3GO != null && card3GO.GetComponent<Image>() != null) U9_UI_Utils.ApplyRoundedCardStyle(card3GO.GetComponent<Image>(), new Color(0.96f, 0.98f, 1f));

            if (nextCardBtn == null) nextCardBtn = FindButton(root, "NextBtn", "NextCardBtn", "ForwardBtn", "Next_Btn", "Next");
            if (prevCardBtn == null) prevCardBtn = FindButton(root, "PrevBtn", "PrevCardBtn", "BackBtn", "Prev_Btn", "Prev", "Back");
            if (startAct1Btn == null) startAct1Btn = FindButton(root, "StartActivity1Btn", "CountBackThreeBtn", "StartAct1Btn", "ContinueBtn");
            if (replayAudioBtn == null) replayAudioBtn = FindButton(root, "ReplayAudioBtn", "ReplayBtn", "Speaker_Button", "Replay_Button");

            if (cardIndicatorText == null)
            {
                Transform t = root.Find("ProgressHUD/ProgressText")
                           ?? root.Find("ProgressHUD/Progress_Text")
                           ?? root.Find("CardIndicatorText")
                           ?? root.Find("ProgressText")
                           ?? root.Find("HUD/ProgressText");
                if (t != null) cardIndicatorText = t.GetComponent<TextMeshProUGUI>();
            }

            if (cardProgressSlider == null)
            {
                cardProgressSlider = GetComponentInChildren<Slider>(true);
            }

            if (scoreText == null)
            {
                Transform st = root.Find("ProgressHUD/Score_Text")
                            ?? root.Find("ProgressHUD/ScoreText")
                            ?? root.Find("ScoreText")
                            ?? root.Find("HUD/ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }

            // Bind Card 1 interactive elements
            if (card1GO != null)
            {
                if (schwaAsideBtn == null) schwaAsideBtn = FindButton(card1GO.transform, "SchwaAsideBtn", "SchwaBtn", "TurtleSchwaBtn");
                if (turtleInteractiveBtn == null) turtleInteractiveBtn = FindButton(card1GO.transform, "TurtleBtn", "WordBtn", "Btn_turtle");
            }

            // Bind Card 2 interactive examples
            if (card2GO != null)
            {
                if (puzzleExampleBtn == null) puzzleExampleBtn = FindButton(card2GO.transform, "PuzzleBtn", "Btn_puzzle");
                if (pickleExampleBtn == null) pickleExampleBtn = FindButton(card2GO.transform, "PickleBtn", "Btn_pickle");
                if (bubbleExampleBtn == null) bubbleExampleBtn = FindButton(card2GO.transform, "BubbleBtn", "Btn_bubble");
            }

            // Bind Card 3 interactive pairs
            if (card3GO != null)
            {
                if (mapleApplePairBtn == null) mapleApplePairBtn = FindButton(card3GO.transform, "MapleAppleBtn", "Pair1Btn");
                if (nobleBottlePairBtn == null) nobleBottlePairBtn = FindButton(card3GO.transform, "NobleBottleBtn", "Pair2Btn");
                if (buglePuzzlePairBtn == null) buglePuzzlePairBtn = FindButton(card3GO.transform, "BuglePuzzleBtn", "Pair3Btn");
            }

            EnsureAudioClips();
            AttachListeners();
        }

        private void EnsureAudioClips()
        {
#if UNITY_EDITOR
            string vAPath = "Assets/Audio/U9_audio/u9_MP_voiceA";
            if (card1VoiceClip == null)
                card1VoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Consonant plus le at the end of a.mp3");
            if (card1SchwaAsideClip == null)
                card1SchwaAsideClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Although say turtle slowly Turtul Theres a tiny.mp3");
            if (card2VoiceClip == null)
                card2VoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Heres the rule If a word ends in.mp3");
            if (card3VoiceClip == null)
                card3VoiceClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Count the consonants sitting just before the le.mp3");
#endif
        }

        private GameObject FindChildGO(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"Cards/{name}") 
                           ?? root.Find($"Content/{name}") 
                           ?? FindDeepChild(root, name);
                if (t != null) return t.gameObject;
            }
            return null;
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"Navigation/{name}") 
                           ?? root.Find($"HUD/{name}") 
                           ?? root.Find($"ProgressHUD/{name}") 
                           ?? root.Find($"Content/{name}") 
                           ?? FindDeepChild(root, name);
                if (t != null)
                {
                    Button b = t.GetComponent<Button>();
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
                startAct1Btn.onClick.AddListener(OnStartActivity1Tapped);
            }

            if (replayAudioBtn != null)
            {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentCardAudio);
            }

            if (schwaAsideBtn != null)
            {
                schwaAsideBtn.onClick.RemoveAllListeners();
                schwaAsideBtn.onClick.AddListener(() =>
                {
                    if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
                    {
                        U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                        if (card1SchwaAsideClip != null)
                            U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(card1SchwaAsideClip);
                        else
                            U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_card1b");
                    }
                });
            }

            if (turtleInteractiveBtn != null)
            {
                AttachWordButton(turtleInteractiveBtn, "turtle", "tur tle");
            }

            AttachWordButton(puzzleExampleBtn, "puzzle", "puz zle");
            AttachWordButton(pickleExampleBtn, "pickle", "pick le");
            AttachWordButton(bubbleExampleBtn, "bubble", "bub ble");

            AttachPairButton(mapleApplePairBtn, "maple", "apple");
            AttachPairButton(nobleBottlePairBtn, "noble", "bottle");
            AttachPairButton(buglePuzzlePairBtn, "bugle", "puzzle");
        }

        private void AttachWordButton(Button btn, string word, string syl)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlaySyllables(syl, () =>
                    {
                        U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord(word);
                    });
                }
            });
        }

        private void AttachPairButton(Button btn, string openWord, string closedWord)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
                {
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayGateOpen();
                    U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord(openWord, () =>
                    {
                        U9_SA_AudioManager_Masters_Phonics.Instance.PlayDoorSlam();
                        U9_SA_AudioManager_Masters_Phonics.Instance.PlayWord(closedWord);
                    });
                }
            });
        }

        public void ShowCard(int cardIndex)
        {
            currentCardIndex = Mathf.Clamp(cardIndex, 1, 3);

            if (card1GO != null) card1GO.SetActive(currentCardIndex == 1);
            if (card2GO != null) card2GO.SetActive(currentCardIndex == 2);
            if (card3GO != null) card3GO.SetActive(currentCardIndex == 3);

            if (prevCardBtn != null) prevCardBtn.gameObject.SetActive(currentCardIndex > 1);
            if (nextCardBtn != null)
            {
                nextCardBtn.gameObject.SetActive(true);
                var nextTxt = nextCardBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (nextTxt != null)
                {
                    nextTxt.text = currentCardIndex == 3 ? "<b>START ACTIVITY 1 >></b>" : "<b>NEXT >></b>";
                }
            }

            if (startAct1Btn != null) startAct1Btn.gameObject.SetActive(false);

            // Control Global Navigation in Flow Manager
            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.SetGlobalBackButtonActive(true);
                // Main Next button appears when the learn panel ends (on Card 3) to advance to Activity 1
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.SetNextActivityButtonActive(currentCardIndex == 3);
            }

            if (cardIndicatorText != null)
            {
                cardIndicatorText.text = $"<b>Card {currentCardIndex} of 3</b>";
            }

            if (cardProgressSlider != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(cardProgressSlider);
                cardProgressSlider.value = (float)currentCardIndex / 3f;
            }

            if (scoreText != null && U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                scoreText.text = $"<b>Score: {U9_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore()}</b>";
            }

            PlayCurrentCardAudio();
        }

        private void PlayCurrentCardAudio()
        {
            if (activeCardRoutine != null) StopCoroutine(activeCardRoutine);
            if (U9_SA_AudioManager_Masters_Phonics.Instance == null) return;

            AudioClip clipToPlay = currentCardIndex switch
            {
                1 => card1VoiceClip ?? (U9_SA_AudioManager_Masters_Phonics.Instance != null ? U9_SA_AudioManager_Masters_Phonics.Instance.card1Clip : null),
                2 => card2VoiceClip ?? (U9_SA_AudioManager_Masters_Phonics.Instance != null ? U9_SA_AudioManager_Masters_Phonics.Instance.card2Clip : null),
                3 => card3VoiceClip ?? (U9_SA_AudioManager_Masters_Phonics.Instance != null ? U9_SA_AudioManager_Masters_Phonics.Instance.card3Clip : null),
                _ => card1VoiceClip
            };

            string fallbackKey = currentCardIndex switch
            {
                1 => "U09_VO_card1",
                2 => "U09_VO_card2",
                3 => "U09_VO_card3",
                _ => "U09_VO_card1"
            };

            if (clipToPlay != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(clipToPlay);
            }
            else
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(fallbackKey);
            }
        }

        public void ReplayCurrentCardAudio()
        {
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
            }
            PlayCurrentCardAudio();
        }

        public void OnNextCardTapped()
        {
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();

            if (currentCardIndex >= 3)
            {
                // Seamlessly advance to Activity 1 on final card click
                OnStartActivity1Tapped();
            }
            else
            {
                ShowCard(currentCardIndex + 1);
            }
        }

        public void OnPrevCardTapped()
        {
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
            ShowCard(currentCardIndex - 1);
        }

        public void OnStartActivity1Tapped()
        {
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                U9_SA_AudioManager_Masters_Phonics.Instance.StopAll();
            }

            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.OpenActivity1();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            EnsureAudioClips();
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 9 Concept Cards] Auto-Assigned Cards, Audio Clips & Navigation Elements!</b></color>");
        }
#endif
    }
}
