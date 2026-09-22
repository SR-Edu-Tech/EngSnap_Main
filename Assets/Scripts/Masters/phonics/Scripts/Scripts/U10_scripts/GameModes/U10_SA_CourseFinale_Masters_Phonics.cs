using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U10_SA_CourseFinale_Masters_Phonics : MonoBehaviour
    {
        [Header("Badge Wall (All 11 Badges)")]
        [SerializeField] private Transform badgeWallContainer;
        [SerializeField] private Image[] badgeSlotImages;

        [Header("Lifetime Statistics Displays")]
        [SerializeField] private TextMeshProUGUI wordsReadText;
        [SerializeField] private TextMeshProUGUI wordsBuiltText;
        [SerializeField] private TextMeshProUGUI wordsSplitText;
        [SerializeField] private TextMeshProUGUI syllableTypesText;
        [SerializeField] private TextMeshProUGUI badgesUnlockedText;

        [Header("Graduation Certificate")]
        [SerializeField] private GameObject certificatePanel;
        [SerializeField] private TextMeshProUGUI studentNameText;
        [SerializeField] private TextMeshProUGUI completionDateText;
        [SerializeField] private Button printShareCertificateBtn;

        [Header("Navigation Button")]
        [SerializeField] private Button backToJourneyMapBtn;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            StartCoroutine(CoPlayFinaleSequence());
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;
            U10_UI_Utils.FormatHeaderTypography(root, "COURSE COMPLETE — MASTER PHONICS GRADUATION", "All 10 Units & 7 Syllable Types Mastered!");

            if (badgeWallContainer == null)
            {
                badgeWallContainer = FindDeepChild(root, "BadgeWall") 
                                  ?? FindDeepChild(root, "BadgesContainer")
                                  ?? root.Find("BadgeWallCard/BadgeWall");
            }

            if (badgeSlotImages == null || badgeSlotImages.Length == 0)
            {
                if (badgeWallContainer != null)
                {
                    badgeSlotImages = badgeWallContainer.GetComponentsInChildren<Image>(true);
                }
            }

            // Lifetime Statistics Displays
            if (wordsReadText == null) wordsReadText = (FindDeepChild(root, "WordsRead_Text") ?? FindDeepChild(root, "WordsRead"))?.GetComponent<TextMeshProUGUI>();
            if (wordsBuiltText == null) wordsBuiltText = (FindDeepChild(root, "WordsBuilt_Text") ?? FindDeepChild(root, "WordsBuilt"))?.GetComponent<TextMeshProUGUI>();
            if (wordsSplitText == null) wordsSplitText = (FindDeepChild(root, "WordsSplit_Text") ?? FindDeepChild(root, "WordsSplit"))?.GetComponent<TextMeshProUGUI>();
            if (syllableTypesText == null) syllableTypesText = (FindDeepChild(root, "SyllableTypes_Text") ?? FindDeepChild(root, "SyllableTypes"))?.GetComponent<TextMeshProUGUI>();
            if (badgesUnlockedText == null) badgesUnlockedText = (FindDeepChild(root, "Badges_Text") ?? FindDeepChild(root, "BadgesUnlocked"))?.GetComponent<TextMeshProUGUI>();

            // Certificate
            if (certificatePanel == null)
            {
                Transform cp = FindDeepChild(root, "CertificatePanel") ?? FindDeepChild(root, "Certificate");
                if (cp != null) certificatePanel = cp.gameObject;
            }

            if (certificatePanel != null)
            {
                Transform cpTr = certificatePanel.transform;
                if (studentNameText == null) studentNameText = (FindDeepChild(cpTr, "StudentName") ?? FindDeepChild(cpTr, "NameText"))?.GetComponent<TextMeshProUGUI>();
                if (completionDateText == null) completionDateText = (FindDeepChild(cpTr, "DateText") ?? FindDeepChild(cpTr, "Date"))?.GetComponent<TextMeshProUGUI>();
                if (printShareCertificateBtn == null) printShareCertificateBtn = FindButton(cpTr, "ShareBtn", "PrintBtn", "SaveCertificateBtn");
            }

            if (backToJourneyMapBtn == null)
            {
                backToJourneyMapBtn = FindButton(root, "JourneyMapBtn", "BackToJourneyBtn", "ContinueBtn", "Btn_Continue");
            }

            if (backToJourneyMapBtn != null)
            {
                if (backToJourneyMapBtn.GetComponent<Image>() != null)
                    U10_UI_Utils.ApplyRoundedButtonStyle(backToJourneyMapBtn.GetComponent<Image>(), new Color(0.12f, 0.75f, 0.38f));
                backToJourneyMapBtn.onClick.RemoveAllListeners();
                backToJourneyMapBtn.onClick.AddListener(OnBackToJourneyMapTapped);
            }

            if (printShareCertificateBtn != null)
            {
                printShareCertificateBtn.onClick.RemoveAllListeners();
                printShareCertificateBtn.onClick.AddListener(OnShareCertificateTapped);
            }
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"Card/{name}") 
                           ?? root.Find($"Content/{name}") 
                           ?? root.Find($"HUD/{name}") 
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

        private IEnumerator CoPlayFinaleSequence()
        {
            // Populate Lifetime Stats
            if (wordsReadText != null) wordsReadText.text = "<b>WORDS READ: 1,240</b>";
            if (wordsBuiltText != null) wordsBuiltText.text = "<b>WORDS BUILT: 310</b>";
            if (wordsSplitText != null) wordsSplitText.text = "<b>WORDS SPLIT: 75</b>";
            if (syllableTypesText != null) syllableTypesText.text = "<b>SYLLABLE TYPES: 7 of 7</b>";
            if (badgesUnlockedText != null) badgesUnlockedText.text = "<b>BADGES EARNED: 11 of 11</b>";

            // Populate Certificate
            if (studentNameText != null) studentNameText.text = "<b>MASTER SCHOLAR</b>";
            if (completionDateText != null) completionDateText.text = $"Awarded: {DateTime.Now:MMMM dd, yyyy}";

            // Sequential Badge Light-Up Animation
            if (badgeSlotImages != null && badgeSlotImages.Length > 0)
            {
                foreach (var img in badgeSlotImages)
                {
                    if (img != null)
                    {
                        img.transform.localScale = Vector3.one * 0.8f;
                        img.color = new Color(1f, 1f, 1f, 0.4f);
                    }
                }

                for (int i = 0; i < badgeSlotImages.Length; i++)
                {
                    var img = badgeSlotImages[i];
                    if (img != null)
                    {
                        img.color = Color.white;
                        img.transform.localScale = Vector3.one * 1.25f;
                        U10_SA_AudioManager_Masters_Phonics.Instance?.PlayTwinMatch();
                        yield return new WaitForSeconds(0.12f);
                        img.transform.localScale = Vector3.one;
                    }
                }
            }

            if (U10_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayFinaleFanfare();
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayCertificate();
            }

            yield return new WaitForSeconds(1.2f);

            // Play the final mascot narration line
            if (U10_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U10_VO_course_complete");
            }
        }

        public void OnShareCertificateTapped()
        {
            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayClick();
            Debug.Log("<color=#10B981><b>[Master Phonics] Certificate Shared / Saved!</b></color>");
        }

        public void OnBackToJourneyMapTapped()
        {
            if (U10_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                U10_SA_AudioManager_Masters_Phonics.Instance.StopAll();
            }

            if (Unit_Selection_Panel_Masters_Phonics.Instance != null)
            {
                Unit_Selection_Panel_Masters_Phonics.Instance.BackToUnitSelection();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 10 Course Finale] Auto-Assigned Hierarchy, Badges & Certificate!</b></color>");
        }
#endif
    }
}
