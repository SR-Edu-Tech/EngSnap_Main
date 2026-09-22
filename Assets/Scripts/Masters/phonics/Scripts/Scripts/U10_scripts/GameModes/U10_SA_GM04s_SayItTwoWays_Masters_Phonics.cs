using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U10_SA_GM04s_SayItTwoWays_Masters_Phonics : MonoBehaviour
    {
        [Header("Sentence & Prompt Display")]
        [SerializeField] private TextMeshProUGUI sentenceDisplayText;
        [SerializeField] private TextMeshProUGUI questionPromptText;
        [SerializeField] private TextMeshProUGUI sameSpellingNoteText;

        [Header("Dual Pronunciation Option Buttons")]
        [SerializeField] private Button optionABtn;
        [SerializeField] private Button optionBBtn;
        [SerializeField] private TextMeshProUGUI optionTextA;
        [SerializeField] private TextMeshProUGUI optionTextB;

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI scoreText;

        private List<U10SayItTwoWaysItem> items = new List<U10SayItTwoWaysItem>();
        private int currentItemIndex = 0;
        private int firstAttemptCorrectCount = 0;
        private int stressShiftCorrectCount = 0;
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

            if (sentenceDisplayText == null)
            {
                Transform st = FindDeepChild(root, "SentenceText") ?? FindDeepChild(root, "Sentence") ?? root.Find("SentenceCard/Text");
                if (st != null) sentenceDisplayText = st.GetComponent<TextMeshProUGUI>();
            }

            if (questionPromptText == null)
            {
                Transform qp = FindDeepChild(root, "PromptText") ?? FindDeepChild(root, "Prompt") ?? FindDeepChild(root, "QuestionPrompt");
                if (qp != null) questionPromptText = qp.GetComponent<TextMeshProUGUI>();
            }

            if (sameSpellingNoteText == null)
            {
                Transform sn = FindDeepChild(root, "SameSpellingNote") ?? FindDeepChild(root, "NoteText") ?? FindDeepChild(root, "Note");
                if (sn != null) sameSpellingNoteText = sn.GetComponent<TextMeshProUGUI>();
            }

            // Options
            if (optionABtn == null) optionABtn = FindButton(root, "PronounceBtn_A", "Option_A", "Btn_A", "Option_1");
            if (optionBBtn == null) optionBBtn = FindButton(root, "PronounceBtn_B", "Option_B", "Btn_B", "Option_2");

            if (optionABtn != null && optionTextA == null) optionTextA = (FindDeepChild(optionABtn.transform, "Text") ?? FindDeepChild(optionABtn.transform, "PronounceText"))?.GetComponent<TextMeshProUGUI>() ?? optionABtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (optionBBtn != null && optionTextB == null) optionTextB = (FindDeepChild(optionBBtn.transform, "Text") ?? FindDeepChild(optionBBtn.transform, "PronounceText"))?.GetComponent<TextMeshProUGUI>() ?? optionBBtn.GetComponentInChildren<TextMeshProUGUI>(true);

            U10_UI_Utils.EnsureHUD(root, ref progressText, ref scoreText);

            if (progressBar == null)
            {
                progressBar = GetComponentInChildren<Slider>(true);
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
            items = U10_DataBank.GetSayItTwoWaysItems();
            currentItemIndex = 0;
            firstAttemptCorrectCount = 0;
            stressShiftCorrectCount = 0;
            isProcessing = false;

            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_a4_intro", () =>
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

            U10SayItTwoWaysItem item = items[index];
            UpdateHUD();

            // Stress shift intro for items 13 & 14
            if (index == 12)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_a4_stress");
            }

            if (sentenceDisplayText != null)
            {
                string formatted = item.sentenceText.Replace(item.homographWord, $"<color=#38BDF8><b>{item.homographWord}</b></color>");
                sentenceDisplayText.text = $"<b>{formatted}</b>";
            }

            if (questionPromptText != null)
            {
                questionPromptText.text = $"<b>How do you say \"{item.homographWord}\" here?</b>";
            }

            if (sameSpellingNoteText != null)
            {
                sameSpellingNoteText.text = "<i>(both are spelled the exact same way)</i>";
            }

            // Option A
            if (optionABtn != null)
            {
                var imgA = optionABtn.GetComponent<Image>();
                if (imgA != null) U10_UI_Utils.ApplyRoundedButtonStyle(imgA, new Color(0.15f, 0.22f, 0.35f, 0.95f));
                if (optionTextA != null) optionTextA.text = $"<b>{item.pronunciationLabelA}</b>";

                optionABtn.onClick.RemoveAllListeners();
                optionABtn.onClick.AddListener(() => OnOptionChosen(0, item.audioKeyA, optionABtn));
            }

            // Option B
            if (optionBBtn != null)
            {
                var imgB = optionBBtn.GetComponent<Image>();
                if (imgB != null) U10_UI_Utils.ApplyRoundedButtonStyle(imgB, new Color(0.15f, 0.22f, 0.35f, 0.95f));
                if (optionTextB != null) optionTextB.text = $"<b>{item.pronunciationLabelB}</b>";

                optionBBtn.onClick.RemoveAllListeners();
                optionBBtn.onClick.AddListener(() => OnOptionChosen(1, item.audioKeyB, optionBBtn));
            }
        }

        private void OnOptionChosen(int selectedIdx, string audioKey, Button btn)
        {
            if (isProcessing) return;

            // 1. Play the pronunciation audio so the child hears it
            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceB(audioKey);
            StartCoroutine(CoPunchTransform(btn.transform));

            U10SayItTwoWaysItem item = items[currentItemIndex];
            bool isCorrect = (selectedIdx == item.correctOptionIndex);

            if (isCorrect)
            {
                isProcessing = true;
                firstAttemptCorrectCount++;
                if (currentItemIndex >= 12) stressShiftCorrectCount++;

                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.AddScore(150);
                UpdateHUD();
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayCorrect();

                if (btn != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null) U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.12f, 0.75f, 0.38f));
                }

                StartCoroutine(CoAdvance(1.3f));
            }
            else
            {
                U10_SA_AudioManager_Masters_Phonics.Instance?.PlayWrong();
                U10_SA_UnitFlowManager_Masters_Phonics.Instance?.RecordMistake(item.sentenceText);

                if (btn != null)
                {
                    var img = btn.GetComponent<Image>();
                    if (img != null) U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.85f, 0.25f, 0.25f));
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
            if (firstAttemptCorrectCount >= 12 && stressShiftCorrectCount >= 2) stars = 3;
            else if (firstAttemptCorrectCount >= 10) stars = 2;

            int points = stars * 150;
            U10_SA_UnitFlowManager_Masters_Phonics.Instance?.ShowActivityCompletionDialog(stars, points);
        }

        private void UpdateHUD()
        {
            string progressStr = $"<b>Homograph {currentItemIndex + 1} of {items.Count}</b>";

            if (progressText != null)
            {
                progressText.text = progressStr;
            }

            // Also update any child ProgressText under ProgressHUD or root so bottom progress bar label never gets stuck
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
            Debug.Log("<color=#10B981><b>[Unit 10 Say It Two Ways] Auto-Assigned Hierarchy & Buttons!</b></color>");
        }
#endif
    }
}
