using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U9_SA_CompletionPanel_Masters_Phonics : MonoBehaviour
    {
        [Header("Badge & Visuals")]
        public Image badgeIconImage;
        public Image allSevenBadgeImage;
        public Sprite turtleKeeperBadgeSprite;
        public Sprite allSevenBadgeSprite;

        [Header("Score & Statistics")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI accuracyStatsText;
        [SerializeField] private TextMeshProUGUI wordsSplitStatsText;

        [Header("Buttons")]
        [SerializeField] private Button continueLessonsBtn;
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
            U9_UI_Utils.FormatHeaderTypography(root, "UNIT 9 COMPLETE!", "Consonant + le Mastery");

            if (badgeIconImage == null)
            {
                Transform bi = root.Find("BadgeIcon") ?? root.Find("Badge_Image") ?? root.Find("Badge");
                if (bi != null) badgeIconImage = bi.GetComponent<Image>();
            }

            if (allSevenBadgeImage == null)
            {
                Transform asb = root.Find("AllSevenBadge") ?? root.Find("BonusBadge");
                if (asb != null) allSevenBadgeImage = asb.GetComponent<Image>();
            }

            if (finalScoreText == null)
            {
                Transform fs = root.Find("ScoreText") ?? root.Find("FinalScore_Text") ?? root.Find("Score_Value");
                if (fs != null) finalScoreText = fs.GetComponent<TextMeshProUGUI>();
            }

            if (accuracyStatsText == null)
            {
                Transform acc = root.Find("AccuracyText") ?? root.Find("Accuracy_Value") ?? root.Find("Stats_Accuracy");
                if (acc != null) accuracyStatsText = acc.GetComponent<TextMeshProUGUI>();
            }

            if (wordsSplitStatsText == null)
            {
                Transform ws = root.Find("WordsSplitText") ?? root.Find("Words_Value") ?? root.Find("Stats_Words");
                if (ws != null) wordsSplitStatsText = ws.GetComponent<TextMeshProUGUI>();
            }

            if (continueLessonsBtn == null)
            {
                continueLessonsBtn = FindButton(root, "ContinueBtn", "LessonsMenuBtn", "BackToLessonsBtn", "Return_Btn", "Btn_Continue");
            }

            if (replayUnitBtn == null)
            {
                replayUnitBtn = FindButton(root, "ReplayUnitBtn", "RetryBtn", "PlayAgainBtn");
            }

            Transform cardTr = root.Find("Card") ?? root.Find("CompletionCard");
            if (cardTr != null && cardTr.GetComponent<Image>() != null)
            {
                U9_UI_Utils.ApplyRoundedCardStyle(cardTr.GetComponent<Image>(), new Color(0.12f, 0.18f, 0.28f, 0.96f));
            }

            if (continueLessonsBtn != null)
            {
                if (continueLessonsBtn.GetComponent<Image>() != null)
                    U9_UI_Utils.ApplyRoundedButtonStyle(continueLessonsBtn.GetComponent<Image>(), new Color(0.12f, 0.75f, 0.38f));
                continueLessonsBtn.onClick.RemoveAllListeners();
                continueLessonsBtn.onClick.AddListener(OnContinueToLessonsTapped);
            }

            if (replayUnitBtn != null)
            {
                if (replayUnitBtn.GetComponent<Image>() != null)
                    U9_UI_Utils.ApplyRoundedButtonStyle(replayUnitBtn.GetComponent<Image>(), new Color(0.2f, 0.4f, 0.7f));
                replayUnitBtn.onClick.RemoveAllListeners();
                replayUnitBtn.onClick.AddListener(OnReplayUnitTapped);
            }

            EnsureBadgeSprites();
        }

        private void EnsureBadgeSprites()
        {
#if UNITY_EDITOR
            if (turtleKeeperBadgeSprite == null || allSevenBadgeSprite == null)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/unit9_MP/sprite U9 MP.png");
                if (assets != null)
                {
                    foreach (var a in assets)
                    {
                        if (a is Sprite s)
                        {
                            if (s.name == "U09_Badge_TurtleKeeper" || s.name.IndexOf("TurtleKeeper", StringComparison.OrdinalIgnoreCase) >= 0 || s.name.IndexOf("Turtle_Keeper", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                if (turtleKeeperBadgeSprite == null) turtleKeeperBadgeSprite = s;
                            }
                            if (s.name == "U09_Badge_AllSevenMaster" || s.name.IndexOf("AllSeven", StringComparison.OrdinalIgnoreCase) >= 0 || s.name.IndexOf("All_Seven", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                if (allSevenBadgeSprite == null) allSevenBadgeSprite = s;
                            }
                        }
                    }
                }
            }
#endif
            if (badgeIconImage != null && turtleKeeperBadgeSprite != null)
            {
                badgeIconImage.sprite = turtleKeeperBadgeSprite;
                badgeIconImage.preserveAspect = true;
            }

            if (allSevenBadgeImage != null && allSevenBadgeSprite != null)
            {
                allSevenBadgeImage.sprite = allSevenBadgeSprite;
                allSevenBadgeImage.preserveAspect = true;
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
            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                score = U9_SA_UnitFlowManager_Masters_Phonics.Instance.GetCumulativeScore();
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = $"<b>Score: {score}</b>";
            }

            if (accuracyStatsText != null)
            {
                int mistakes = U9_SA_UnitFlowManager_Masters_Phonics.Instance != null ? U9_SA_UnitFlowManager_Masters_Phonics.Instance.mistakeQueue.Count : 0;
                int acc = Mathf.Clamp(100 - (mistakes * 2), 75, 100);
                accuracyStatsText.text = $"<b>Accuracy: {acc}%</b>";
            }

            if (wordsSplitStatsText != null)
            {
                int splitCount = (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null && U9_SA_UnitFlowManager_Masters_Phonics.Instance.wordsSplitCount > 0)
                               ? U9_SA_UnitFlowManager_Masters_Phonics.Instance.wordsSplitCount : 32;
                wordsSplitStatsText.text = $"<b>Words Split: {splitCount}</b>";
            }

            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayCelebration();
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_unit_complete");
            }
        }

        public void OnContinueToLessonsTapped()
        {
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                U9_SA_AudioManager_Masters_Phonics.Instance.StopAll();
            }

            if (Unit_Selection_Panel_Masters_Phonics.Instance != null)
            {
                Unit_Selection_Panel_Masters_Phonics.Instance.BackToUnitSelection();
            }
        }

        public void OnReplayUnitTapped()
        {
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                U9_SA_AudioManager_Masters_Phonics.Instance.StopAll();
            }

            if (U9_SA_UnitFlowManager_Masters_Phonics.Instance != null)
            {
                U9_SA_UnitFlowManager_Masters_Phonics.Instance.OpenSignboard();
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Assets")]
        public void EditorAutoAssignHierarchyAndAssets()
        {
            EnsureBadgeSprites();
            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 9 Completion Panel] Auto-Assigned Badges, Stats & Buttons!</b></color>");
        }
#endif
    }
}
