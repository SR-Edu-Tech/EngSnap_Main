using UnityEngine;
using UnityEditor;
using MastersPhonics;

namespace MastersPhonics.EditorTools
{
    // =========================================================================
    // Custom Inspectors for Unit 8 One-Click Auto-Assignment in the Inspector
    // =========================================================================

    public abstract class U8_BaseCustomEditor : UnityEditor.Editor
    {
        protected void DrawAutoAssignButton(string label, System.Action onClick)
        {
            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.12f, 0.75f, 0.45f, 1f);
            if (GUILayout.Button(label, GUILayout.Height(36)))
            {
                U8_HierarchyAutomator.ClearCaches();
                onClick?.Invoke();
                serializedObject.Update();
                EditorUtility.SetDirty(target);
                if (target is Component comp && comp != null)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(comp.gameObject.scene);
                }
                GUI.changed = true;
                Repaint();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(6);
        }
    }

    [CustomEditor(typeof(U8_SA_UnitFlowManager_Masters_Phonics))]
    public class U8_UnitFlowManagerEditor : U8_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Flow Manager & Panel References", () =>
            {
                var mgr = (U8_SA_UnitFlowManager_Masters_Phonics)target;
                U8_HierarchyAutomator.AutoAssignFlowManager(mgr);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U8_SA_AudioManager_Masters_Phonics))]
    public class U8_AudioManagerEditor : U8_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Audio Clips & Reused SFX", () =>
            {
                var mgr = (U8_SA_AudioManager_Masters_Phonics)target;
                U8_HierarchyAutomator.AutoAssignAudioManager(mgr);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U8_SA_GM01_ConceptCards_Masters_Phonics))]
    public class U8_GM01ConceptCardsEditor : U8_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Concept Cards (Learn) Audio & UI", () =>
            {
                var gm = (U8_SA_GM01_ConceptCards_Masters_Phonics)target;
                U8_HierarchyAutomator.AutoAssignConceptCards(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U8_SA_GM04_BossyR_Masters_Phonics))]
    public class U8_GM04BossyREditor : U8_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Bossy R (Act 1) Pairs & UI", () =>
            {
                var gm = (U8_SA_GM04_BossyR_Masters_Phonics)target;
                U8_HierarchyAutomator.AutoAssignBossyR(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U8_SA_GM03_ThreeSounds_Masters_Phonics))]
    public class U8_GM03ThreeSoundsEditor : U8_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Three Sounds (Act 2) Bins & UI", () =>
            {
                var gm = (U8_SA_GM03_ThreeSounds_Masters_Phonics)target;
                U8_HierarchyAutomator.AutoAssignThreeSounds(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U8_SA_GM03_FiveFamilies_Masters_Phonics))]
    public class U8_GM03FiveFamiliesEditor : U8_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Five Families (Act 3) Rounds & UI", () =>
            {
                var gm = (U8_SA_GM03_FiveFamilies_Masters_Phonics)target;
                U8_HierarchyAutomator.AutoAssignFiveFamilies(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U8_SA_GM03s_ErRule_Masters_Phonics))]
    public class U8_GM03sErRuleEditor : U8_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign The -er Rule (Act 4) Swipe & UI", () =>
            {
                var gm = (U8_SA_GM03s_ErRule_Masters_Phonics)target;
                U8_HierarchyAutomator.AutoAssignErRule(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U8_SA_GM05_SpellingLab_Masters_Phonics))]
    public class U8_GM05SpellingLabEditor : U8_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Spelling Lab (Act 5) Tiles & UI", () =>
            {
                var gm = (U8_SA_GM05_SpellingLab_Masters_Phonics)target;
                U8_HierarchyAutomator.AutoAssignSpellingLab(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U8_SA_SevenTypesMap_Masters_Phonics))]
    public class U8_SevenTypesMapEditor : U8_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Seven Types Map Audio & UI", () =>
            {
                var gm = (U8_SA_SevenTypesMap_Masters_Phonics)target;
                U8_HierarchyAutomator.AutoAssignSevenTypesMap(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U8_SA_UnitChallenge_Masters_Phonics))]
    public class U8_UnitChallengeEditor : U8_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Unit Challenge Audio & UI", () =>
            {
                var gm = (U8_SA_UnitChallenge_Masters_Phonics)target;
                U8_HierarchyAutomator.AutoAssignUnitChallenge(gm);
            });
            DrawDefaultInspector();
        }
    }
}
