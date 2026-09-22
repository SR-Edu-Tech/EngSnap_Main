using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U10_SA_GM04_WhichOneFits_Masters_Phonics : MonoBehaviour
    {
        [Header("Sentence Card & Gap Display")]
        [SerializeField] private TextMeshProUGUI sentenceDisplayText;
        [SerializeField] private Button playBothAudiosBtn;
        [SerializeField] private TextMeshProUGUI soundNoteText;

        [Header("Homophone Option Buttons")]
        [SerializeField] private Button option1Btn;
        [SerializeField] private Button option2Btn;
        [SerializeField] private Button option3Btn;
        public Button[] optionButtons;

        [Header("Wrong Answer Comparison View")]
        [SerializeField] private GameObject comparisonPanel;
        [SerializeField] private TextMeshProUGUI wrongSentenceText;
        [SerializeField] private TextMeshProUGUI correctSentenceText;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        private List<U10WhichOneFitsItem> items = new List<U10WhichOneFitsItem>();
        private int currentItemIndex = 0;
        private int firstAttemptCorrectCount = 0;
        private int roundBCorrectCount = 0;
        private bool isProcessing = false;

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
            Transform root = transform;
            U10_UI_Utils.FormatHeaderTypography(root, "WHICH ONE FITS? — MEANING IN CONTEXT", "Pick the word that fits the sentence");

            if (sentenceDisplayText == null)
            {
                Transform st = FindDeepChild(root, "SentenceText") ?? FindDeepChild(root, "Sentence") ?? root.Find("SentenceCard/Text");
                if (st != null) sentenceDisplayText = st.GetComponent<TextMeshProUGUI>();
            }

            if (playBothAudiosBtn == null)
            {
                playBothAudiosBtn = FindButton(root, "PlayBothBtn", "ListenBothBtn", "AudioBtn", "Speaker_Button");
            }

            if (soundNoteText == null)
            {
                Transform sn = FindDeepChild(root, "SoundNoteText") ?? FindDeepChild(root, "NoteText");
                if (sn != null) soundNoteText = sn.GetComponent<TextMeshProUGUI>();
            }

            // Bind Option Buttons
            if (option1Btn == null) option1Btn = FindButton(root, "Option_1", "Option1", "Btn_Opt1");
            if (option2Btn == null) option2Btn = FindButton(root, "Option_2", "Option2", "Btn_Opt2");
            if (option3Btn == null) option3Btn = FindButton(root, "Option_3", "Option3", "Btn_Opt3");

            optionButtons = new Button[] { option1Btn, option2Btn, option3Btn };

            // Comparison Panel
            if (comparisonPanel == null)
            {
                Transform cp = FindDeepChild(root, "ComparisonPanel") ?? FindDeepChild(root, "WrongFeedbackPanel");
                if (cp != null) comparisonPanel = cp.gameObject;
            }

            if (comparisonPanel != null)
            {
                Transform cpTr = comparisonPanel.transform;
                if (wrongSentenceText == null) wrongSentenceText = (FindDeepChild(cpTr, "WrongText") ?? FindDeepChild(cpTr, "WrongSentence"))?.GetComponent<TextMeshProUGUI>();
                if (correctSentenceText == null) correctSentenceText = (FindDeepChild(cpTr, "CorrectText") ?? FindDeepChild(cpTr, "CorrectSentence"))?.GetComponent<TextMeshProUGUI>();
            }

            U10_UI_Utils.EnsureHUD(root, ref progressText, ref scoreText);

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
            }

            if (playBothAudiosBtn != null)
            {
                playBothAudiosBtn.onClick.RemoveAllListeners();
                playBothAudiosBtn.onClick.AddListener(OnPlayBothAudiosTapped);
            }
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"SentenceCard/{name}") 
                           ?? root.Find($"OptionsContainer/{name}") 
                           ?? root.Find($"Content/{name}") 
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



        public void StartActivity()
        {
            items = U10_DataBank.GetWhichOneFitsItems();
            currentItemIndex = 0;
            firstAttemptCorrectCount = 0;
            roundBCorrectCount = 0;
            isProcessing = false;

            if (comparisonPanel != null) comparisonPanel.SetActive(false);

            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_a2_intro", () =>
            {
                if (this != null && gameObject.activeInHierarchy)
                {
                    LoadItem(0);
                }
            });
        }

        private void LoadItem(int index)
        {
            if (!gameObject.activeInHierarchy) return;

            if (index >= items.Count)
            {
                OnActivityComplete();
                return;
            }

            currentItemIndex = index;
            isProcessing = false;

            if (comparisonPanel != null) comparisonPanel.SetActive(false);

            U10WhichOneFitsItem item = items[index];
            UpdateHUD();

            // Special Voice A alert for Round B high frequency items
            if (index == 10)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_a2_rb");
            }

            if (sentenceDisplayText != null)
            {
                sentenceDisplayText.text = $"<b>{item.sentenceTemplate}</b>";
            }

            if (soundNoteText != null)
            {
                soundNoteText.text = "<i>both options sound identical [ LISTEN ]</i>";
            }

            // Bind Options
            for (int i = 0; i < optionButtons.Length; i++)
            {
                Button btn = optionButtons[i];
                if (btn == null) continue;

                if (i < item.options.Length)
                {
                    btn.gameObject.SetActive(true);
                    var txt = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null)
                    {
                        txt.text = $"<b>{item.options[i]}</b>";
                        txt.color = Color.white;
                    }

                    var img = btn.GetComponent<Image>();
                    if (img != null) U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.15f, 0.22f, 0.35f, 0.95f));

                    int optIdx = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnOptionSelected(optIdx, btn));
                }
                else
                {
                    btn.gameObject.SetActive(false);
                }
            }
        }

        private void OnOptionSelected(int selectedIdx, Button btn)
        {
            if (isProcessing) return;
            isProcessing = true;

            U10WhichOneFitsItem item = items[currentItemIndex];
            bool isCorrect = (selectedIdx == item.correctIndex);

            if (isCorrect)
            {
                firstAttemptCorrectCount++;
                if (item.isHighFrequencyRoundB) roundBCorrectCount++;

                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.AddScore(150);
                UpdateHUD();
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayCorrect();

                if (btn != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null) U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.12f, 0.75f, 0.38f));
                }

                // Show completed sentence with green highlighted word
                if (sentenceDisplayText != null)
                {
                    string completed = item.sentenceTemplate.Replace("______", $"<color=#10B981><b>{item.options[selectedIdx]}</b></color>");
                    sentenceDisplayText.text = completed;
                }

                // Read full sentence
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlaySentence(item.fullCorrectSentence);

                StartCoroutine(CoAdvance(1.6f));
            }
            else
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.RecordMistake(item.sentenceTemplate, item.isHighFrequencyRoundB);

                if (btn != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null) U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.85f, 0.25f, 0.25f));
                }

                // Show Comparison Screen (Child's version vs Correct version)
                if (comparisonPanel != null)
                {
                    comparisonPanel.SetActive(true);
                    if (wrongSentenceText != null)
                    {
                        string wrongSent = item.sentenceTemplate.Replace("______", $"<s><color=#EF4444>{item.options[selectedIdx]}</color></s>");
                        wrongSentenceText.text = $"<s>{wrongSent}</s> (Mismatch!)";
                    }
                    if (correctSentenceText != null)
                    {
                        string rightSent = item.sentenceTemplate.Replace("______", $"<color=#10B981><b>{item.options[item.correctIndex]}</b></color>");
                        correctSentenceText.text = rightSent;
                    }
                }

                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_a2_wrong");

                StartCoroutine(CoAdvance(2.4f));
            }
        }

        private void OnPlayBothAudiosTapped()
        {
            if (currentItemIndex < items.Count)
            {
                U10WhichOneFitsItem item = items[currentItemIndex];
                if (item.options.Length >= 2)
                {
                    U10_SA_AudioManager_Masters_Phonics.Instance?.PlayBothWordsSequence(item.options[0], item.options[1]);
                }
                if (playBothAudiosBtn != null)
                {
                    StartCoroutine(CoPunchTransform(playBothAudiosBtn.transform));
                }
            }
        }

        private IEnumerator CoAdvance(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadItem(currentItemIndex + 1);
        }

        private void OnActivityComplete()
        {
            int stars = 1;
            if (firstAttemptCorrectCount >= 14 && roundBCorrectCount >= 5) stars = 3;
            else if (firstAttemptCorrectCount >= 12) stars = 2;

            int points = stars * 150;
            U10_SA_UnitFlowManager_Masters_Phonics.Instance?.ShowActivityCompletionDialog(stars, points);
        }

        private void UpdateHUD()
        {
            string progressStr = $"<b>Sentence {currentItemIndex + 1} of {items.Count}</b>";
            if (progressText != null)
            {
                progressText.text = progressStr;
            }

            var allTMP = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var pt in allTMP)
            {
                if (pt != null && (pt.name.Equals("ProgressText", StringComparison.OrdinalIgnoreCase) || 
                                   pt.name.Equals("Progress_Text", StringComparison.OrdinalIgnoreCase)))
                {
                    pt.text = progressStr;
                }
            }

            if (progressBar != null)
            {
                progressBar.value = (float)(currentItemIndex + 1) / items.Count;
            }

            if (scoreText != null)
            {
                int sc = U10_SA_UnitFlowManager_Masters_Phonics.Instance != null 
                    ? U10_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore() 
                    : 0;
                scoreText.text = $"<b>Score: {sc}</b>";
            }
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
            Debug.Log("<color=#10B981><b>[Unit 10 Which One Fits] Auto-Assigned Hierarchy & Buttons!</b></color>");
        }
#endif
    }
}
