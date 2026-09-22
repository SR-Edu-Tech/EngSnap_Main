using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U9_SA_GM06_SentenceHunt_Masters_Phonics : MonoBehaviour
    {
        [Header("Sentence Text Container")]
        [SerializeField] private Transform sentenceWordsContainer;
        [SerializeField] private TextMeshProUGUI foundSyllablesText;
        [SerializeField] private Button replaySentenceBtn;

        [Header("Instruction / Prompt")]
        [SerializeField] private TextMeshProUGUI promptInstructionText;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        private List<SentenceHuntItem> items = new List<SentenceHuntItem>();
        private int currentItemIndex = 0;
        private int firstAttemptSuccesses = 0;
        private bool hasFailedCurrentItem = false;
        private bool isProcessing = false;
        private HashSet<string> remainingWordsToFind = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private List<GameObject> activeWordGOs = new List<GameObject>();

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            UpdateHUD();
            StartActivity();
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;
            U9_UI_Utils.FormatHeaderTypography(root, "ACTIVITY 4: SENTENCE HUNT", "Find Consonant + le Words");

            if (promptInstructionText == null)
            {
                Transform pt = root.Find("InstructionText") ?? root.Find("PromptText") ?? root.Find("Prompt") ?? root.Find("Header/PromptText");
                if (pt != null) promptInstructionText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (promptInstructionText != null)
            {
                promptInstructionText.text = "<b>Tap all the consonant + le words hiding in the sentence!</b>";
                promptInstructionText.fontSize = 24;
                promptInstructionText.alignment = TextAlignmentOptions.Center;
                promptInstructionText.color = new Color(0.85f, 0.92f, 1f);
            }

            if (sentenceWordsContainer == null)
            {
                sentenceWordsContainer = root.Find("SentencePanel/WordsContainer")
                                      ?? root.Find("WordsContainer")
                                      ?? root.Find("SentenceContainer")
                                      ?? root.Find("SentencePanel");
            }

            if (foundSyllablesText == null)
            {
                Transform f = root.Find("FoundSyllablesText") ?? root.Find("SplitResultText") ?? root.Find("FoundText");
                if (f != null) foundSyllablesText = f.GetComponent<TextMeshProUGUI>();
            }

            if (replaySentenceBtn == null)
            {
                replaySentenceBtn = FindButton(root, "ReplayBtn", "Speaker_Button", "AudioBtn", "ReplaySentenceBtn");
            }

            if (progressText == null)
            {
                Transform pt = root.Find("ProgressHUD/Progress_Text")
                            ?? root.Find("ProgressHUD/ProgressText")
                            ?? root.Find("ProgressText")
                            ?? root.Find("HUD/ProgressText");
                if (pt != null) progressText = pt.GetComponent<TextMeshProUGUI>();
            }

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            if (scoreText == null)
            {
                Transform st = root.Find("ProgressHUD/Score_Text")
                            ?? root.Find("ScoreText")
                            ?? root.Find("HUD/ScoreText");
                if (st != null) scoreText = st.GetComponent<TextMeshProUGUI>();
            }

            if (replaySentenceBtn != null)
            {
                replaySentenceBtn.onClick.RemoveAllListeners();
                replaySentenceBtn.onClick.AddListener(ReplayCurrentSentenceAudio);
            }
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"Content/{name}") 
                           ?? root.Find($"HUD/{name}") 
                           ?? root.Find($"ProgressHUD/{name}") 
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

        public void StartActivity()
        {
            items = U9_DataBank.GetActivity4Pool();
            currentItemIndex = 0;
            firstAttemptSuccesses = 0;
            isProcessing = false;

            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_a4_intro", () =>
                {
                    if (this != null && gameObject.activeInHierarchy)
                    {
                        LoadCurrentItem();
                    }
                });
            }
            else
            {
                LoadCurrentItem();
            }
        }

        private void LoadCurrentItem()
        {
            if (!gameObject.activeInHierarchy) return;

            if (currentItemIndex >= items.Count)
            {
                CompleteActivity();
                return;
            }

            hasFailedCurrentItem = false;
            isProcessing = false;

            if (foundSyllablesText != null) foundSyllablesText.text = "";
            UpdateHUD();

            SentenceHuntItem item = items[currentItemIndex];
            remainingWordsToFind = new HashSet<string>(item.targetCleWords, StringComparer.OrdinalIgnoreCase);

            BuildSentenceUI(item);
            ReplayCurrentSentenceAudio();
        }

        private void BuildSentenceUI(SentenceHuntItem item)
        {
            ClearWords();
            if (sentenceWordsContainer == null) return;

            string[] tokens = item.sentenceText.Split(' ');
            foreach (string rawToken in tokens)
            {
                string cleanToken = CleanPunctuation(rawToken);
                GameObject wordGO = new GameObject($"Word_{cleanToken}", typeof(RectTransform), typeof(Image), typeof(Button));
                wordGO.transform.SetParent(sentenceWordsContainer, false);

                RectTransform rt = wordGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(Mathf.Max(90, rawToken.Length * 28), 65);

                Image img = wordGO.GetComponent<Image>();
                U9_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.95f, 0.97f, 1f, 0.9f));

                GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(wordGO.transform, false);
                RectTransform txtRt = txtGO.GetComponent<RectTransform>();
                txtRt.sizeDelta = rt.sizeDelta;

                TextMeshProUGUI txt = txtGO.GetComponent<TextMeshProUGUI>();
                txt.text = $"<b>{rawToken}</b>";
                txt.fontSize = 32;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.black; // High-contrast black

                Button btn = wordGO.GetComponent<Button>();
                string capturedWord = cleanToken;
                GameObject capturedGO = wordGO;
                btn.onClick.AddListener(() => OnWordTapped(capturedWord, capturedGO));

                activeWordGOs.Add(wordGO);
            }
        }

        private void OnWordTapped(string tappedWord, GameObject wordGO)
        {
            if (isProcessing) return;

            SentenceHuntItem item = items[currentItemIndex];
            if (remainingWordsToFind.Contains(tappedWord))
            {
                StartCoroutine(CoHandleCorrectWord(tappedWord, wordGO, item));
            }
            else
            {
                StartCoroutine(CoHandleWrongWord(wordGO));
            }
        }

        private IEnumerator CoHandleCorrectWord(string word, GameObject wordGO, SentenceHuntItem item)
        {
            isProcessing = true;
            remainingWordsToFind.Remove(word);

            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayWordLift();

            // Visual lift & highlight
            if (wordGO != null)
            {
                var img = wordGO.GetComponent<Image>();
                if (img != null) img.color = new Color(0.2f, 0.9f, 0.45f);
            }

            // Find matching syllable representation
            string splitRep = "";
            for (int i = 0; i < item.targetCleWords.Length; i++)
            {
                if (string.Equals(item.targetCleWords[i], word, StringComparison.OrdinalIgnoreCase))
                {
                    splitRep = item.splitRepresentations[i];
                    break;
                }
            }

            if (foundSyllablesText != null)
            {
                foundSyllablesText.text = $"<b>Found: {splitRep}</b>";
            }

            U9_SA_AudioManager_Masters_Phonics.Instance?.PlaySyllables(splitRep, () =>
            {
                U9_SA_AudioManager_Masters_Phonics.Instance?.PlayWord(word);
            });

            yield return new WaitForSeconds(1.5f);

            if (remainingWordsToFind.Count == 0)
            {
                if (!hasFailedCurrentItem)
                {
                    firstAttemptSuccesses++;
                    U9_SA_UnitFlowManager_Masters_Phonics.Instance?.AddScore(100);
                }

                UpdateHUD();

                currentItemIndex++;
                LoadCurrentItem();
            }
            else
            {
                isProcessing = false;
            }
        }

        private IEnumerator CoHandleWrongWord(GameObject wordGO)
        {
            hasFailedCurrentItem = true;
            U9_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();

            if (wordGO != null)
            {
                var img = wordGO.GetComponent<Image>();
                if (img != null) img.color = new Color(0.95f, 0.4f, 0.4f);
                yield return new WaitForSeconds(0.4f);
                if (img != null) img.color = new Color(0.95f, 0.97f, 1f, 0.9f);
            }
        }

        private void ReplayCurrentSentenceAudio()
        {
            if (currentItemIndex < items.Count && U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                SentenceHuntItem item = items[currentItemIndex];
                U9_SA_AudioManager_Masters_Phonics.Instance.PlaySentence(item.sentenceText);
            }
        }

        private void UpdateHUD()
        {
            if (progressText != null)
            {
                progressText.text = $"<b>Sentence {currentItemIndex + 1} of {items.Count}</b>";
            }

            if (progressBar != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(progressBar);
                progressBar.value = (float)(currentItemIndex + 1) / items.Count;
            }

            if (scoreText != null && U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                scoreText.text = $"<b>Score: {U9_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore()}</b>";
            }
        }

        private void ClearWords()
        {
            foreach (var go in activeWordGOs)
            {
                if (go != null) Destroy(go);
            }
            activeWordGOs.Clear();
        }

        private static string CleanPunctuation(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private void CompleteActivity()
        {
            int earnedStars = 1;
            if (firstAttemptSuccesses >= 9) earnedStars = 3;
            else if (firstAttemptSuccesses >= 8) earnedStars = 2;

            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.ShowActivityCompletionDialog(
                    transform, 4, earnedStars, firstAttemptSuccesses * 100, () =>
                    {
                        U9_SA_UnitFlowManager_Masters_Phonics.Instance.OpenChallenge();
                    });
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 9 Sentence Hunt] Auto-Assigned Sentence Container & Result Text!</b></color>");
        }
#endif
    }
}
