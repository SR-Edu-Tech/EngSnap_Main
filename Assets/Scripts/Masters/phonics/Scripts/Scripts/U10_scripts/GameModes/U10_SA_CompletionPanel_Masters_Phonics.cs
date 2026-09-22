using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U10_SA_CompletionPanel_Masters_Phonics : MonoBehaviour
    {
        [Header("Badge & Visuals")]
        public Image wordTwinBadgeImage;
        public Sprite wordTwinBadgeSprite;

        [Header("Score & Statistics")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI accuracyStatsText;
        [SerializeField] private TextMeshProUGUI wordsPairedStatsText;

        [Header("Buttons")]
        [SerializeField] private Button continueFinaleBtn;
        [SerializeField] private Button replayUnitBtn;

        private void Awake()
        {
            AutoBindHierarchyElements();
        }

        private void OnEnable()
        {
            AutoBindHierarchyElements();
            PopulateCompletionReport();
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;
            U10_UI_Utils.FormatHeaderTypography(root, "UNIT 10 COMPLETE!", "Meaning & Morphology Mastery");

            if (wordTwinBadgeImage == null)
            {
                Transform bi = FindDeepChild(root, "BadgeIcon") ?? FindDeepChild(root, "Badge_Image") ?? FindDeepChild(root, "WordTwinBadge") ?? FindDeepChild(root, "Badge");
                if (bi != null) wordTwinBadgeImage = bi.GetComponent<Image>();
            }

            if (finalScoreText == null)
            {
                Transform fs = FindDeepChild(root, "ScoreText") ?? FindDeepChild(root, "Score_Text") ?? FindDeepChild(root, "FinalScore_Text") ?? FindDeepChild(root, "Score_Value");
                if (fs != null) finalScoreText = fs.GetComponent<TextMeshProUGUI>();
            }

            if (accuracyStatsText == null)
            {
                Transform acc = FindDeepChild(root, "AccuracyText") ?? FindDeepChild(root, "Accuracy_Value") ?? FindDeepChild(root, "Stats_Accuracy");
                if (acc != null) accuracyStatsText = acc.GetComponent<TextMeshProUGUI>();
            }

            if (wordsPairedStatsText == null)
            {
                Transform ws = FindDeepChild(root, "WordsPairedText") ?? FindDeepChild(root, "WordsSplitText") ?? FindDeepChild(root, "Stats_Words");
                if (ws != null) wordsPairedStatsText = ws.GetComponent<TextMeshProUGUI>();
            }

            if (continueFinaleBtn == null)
            {
                continueFinaleBtn = FindButton(root, "ContinueBtn", "CourseFinaleBtn", "FinaleBtn", "Btn_Continue");
            }

            if (replayUnitBtn == null)
            {
                replayUnitBtn = FindButton(root, "ReplayUnitBtn", "RetryBtn", "PlayAgainBtn");
            }

            Transform cardTr = FindDeepChild(root, "Card") ?? FindDeepChild(root, "CompletionCard");
            if (cardTr != null && cardTr.GetComponent<Image>() != null)
            {
                U10_UI_Utils.ApplyRoundedCardStyle(cardTr.GetComponent<Image>(), new Color(0.12f, 0.18f, 0.28f, 0.96f));
            }

            if (continueFinaleBtn != null)
            {
                var img = continueFinaleBtn.GetComponent<Image>();
                if (img != null && img.sprite == null)
                    U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.12f, 0.75f, 0.38f));
                continueFinaleBtn.onClick.RemoveAllListeners();
                continueFinaleBtn.onClick.AddListener(OnContinueToFinaleTapped);
            }

            if (replayUnitBtn != null)
            {
                var img = replayUnitBtn.GetComponent<Image>();
                if (img != null && img.sprite == null)
                    U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.2f, 0.4f, 0.7f));
                replayUnitBtn.onClick.RemoveAllListeners();
                replayUnitBtn.onClick.AddListener(OnReplayUnitTapped);
            }

            EnsureBadgeSprite();
        }

        private void EnsureBadgeSprite()
        {
#if UNITY_EDITOR
            if (wordTwinBadgeSprite == null)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/All_19_Badges.png");
                if (assets != null)
                {
                    foreach (var a in assets)
                    {
                        if (a is Sprite s && (s.name.Contains("10") || s.name.IndexOf("Twin", StringComparison.OrdinalIgnoreCase) >= 0 || s.name.IndexOf("Word", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            wordTwinBadgeSprite = s;
                            break;
                        }
                    }
                }

                if (wordTwinBadgeSprite == null)
                {
                    wordTwinBadgeSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/StarFish.png");
                }
            }
#endif
            if (wordTwinBadgeImage != null && wordTwinBadgeSprite != null)
            {
                wordTwinBadgeImage.sprite = wordTwinBadgeSprite;
                wordTwinBadgeImage.preserveAspect = true;
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

        public void PopulateCompletionReport()
        {
            int score = 0;
            if (U10_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                score = U10_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore();
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = $"<b>Score: {score:N0}</b>";
            }

            if (accuracyStatsText != null)
            {
                int mistakes = U10_SA_UnitFlowManager_Masters_Phonics.Instance != null ? U10_SA_UnitFlowManager_Masters_Phonics.Instance.mistakeQueue.Count : 0;
                int acc = Mathf.Clamp(100 - (mistakes * 2), 75, 100);
                accuracyStatsText.text = $"<b>Accuracy: {acc}%</b>";
            }

            if (wordsPairedStatsText != null)
            {
                int pairedCount = (U10_SA_UnitFlowManager_Masters_Phonics.Instance != null && U10_SA_UnitFlowManager_Masters_Phonics.Instance.wordsPairedCount > 0)
                                ? U10_SA_UnitFlowManager_Masters_Phonics.Instance.wordsPairedCount : 30;
                wordsPairedStatsText.text = $"<b>Word Twins Found: {pairedCount}</b>";
            }

            if (U10_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayCelebration();
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U10_VO_unit_complete");
            }
        }

        public void OnContinueToFinaleTapped()
        {
            if (U10_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                U10_SA_AudioManager_Masters_Phonics.Instance.StopAll();
            }

            if (U10_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U10_SA_UnitFlowManager_Masters_Phonics.Instance.OpenCourseFinale();
            }
        }

        public void OnReplayUnitTapped()
        {
            if (U10_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U10_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                U10_SA_AudioManager_Masters_Phonics.Instance.StopAll();
            }

            if (U10_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U10_SA_UnitFlowManager_Masters_Phonics.Instance.OpenSignboard();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            EnsureBadgeSprite();
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 10 Completion Panel] Auto-Assigned Badge, Stats & Buttons!</b></color>");
        }
#endif
    }
}
