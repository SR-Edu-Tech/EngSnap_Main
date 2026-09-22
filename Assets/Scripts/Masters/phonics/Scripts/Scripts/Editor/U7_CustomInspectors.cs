using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MastersPhonics;

namespace MastersPhonics.EditorTools
{
    // =========================================================================
    // Custom Inspectors for One-Click Auto-Assignment in the Inspector
    // =========================================================================

    public abstract class U7_BaseCustomEditor : UnityEditor.Editor
    {
        protected void DrawAutoAssignButton(string label, System.Action onClick)
        {
            EditorGUILayout.Space(6);
            GUI.backgroundColor = new Color(0.12f, 0.75f, 0.45f, 1f);
            if (GUILayout.Button(label, GUILayout.Height(36)))
            {
                U7_HierarchyAutomator.ClearCaches();
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

    [CustomEditor(typeof(U7_SA_UnitFlowManager_Masters_Phonics))]
    public class U7_UnitFlowManagerEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Flow Manager & Panel References", () =>
            {
                var mgr = (U7_SA_UnitFlowManager_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignFlowManager(mgr);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U7_SA_AudioManager_Masters_Phonics))]
    public class U7_AudioManagerEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Audio Clips & SFX", () =>
            {
                var mgr = (U7_SA_AudioManager_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignAudioManager(mgr);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U7_SA_GM01_ConceptCards_Masters_Phonics))]
    public class U7_GM01ConceptCardsEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Concept Cards Audio & UI", () =>
            {
                var gm = (U7_SA_GM01_ConceptCards_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignConceptCards(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U7_SA_GM04_TeamUp_Masters_Phonics))]
    public class U7_GM04TeamUpEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Team Up (Act 1) Sprites & UI", () =>
            {
                var gm = (U7_SA_GM04_TeamUp_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignTeamUp(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U7_SA_GM03_WordFamilies_Masters_Phonics))]
    public class U7_GM03WordFamiliesEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Word Families (Act 2) Audio & UI", () =>
            {
                var gm = (U7_SA_GM03_WordFamilies_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignWordFamilies(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U7_SA_GM03_SameSound_Masters_Phonics))]
    public class U7_GM03SameSoundEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Same Sound (Act 3) Audio & UI", () =>
            {
                var gm = (U7_SA_GM03_SameSound_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignSameSound(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U7_SA_GM04_GlideOrHold_Masters_Phonics))]
    public class U7_GM04GlideOrHoldEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Glide or Hold (Act 4) Audio & UI", () =>
            {
                var gm = (U7_SA_GM04_GlideOrHold_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignGlideOrHold(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U7_SA_GM03s_GlideFamilies_Masters_Phonics))]
    public class U7_GM03sGlideFamiliesEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Glide Families (Act 5) UI & SFX", () =>
            {
                var gm = (U7_SA_GM03s_GlideFamilies_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignGlideFamilies(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U7_SA_GM08_TeamRush_Masters_Phonics))]
    public class U7_GM08TeamRushEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Team Rush (Act 6) UI & SFX", () =>
            {
                var gm = (U7_SA_GM08_TeamRush_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignTeamRush(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U7_SA_SevenTypesMap_Masters_Phonics))]
    public class U7_SevenTypesMapEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Seven Types Map Audio & UI", () =>
            {
                var gm = (U7_SA_SevenTypesMap_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignSevenTypesMap(gm);
            });
            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(U7_SA_UnitChallenge_Masters_Phonics))]
    public class U7_UnitChallengeEditor : U7_BaseCustomEditor
    {
        public override void OnInspectorGUI()
        {
            DrawAutoAssignButton("Auto-Assign Unit Challenge Audio & UI", () =>
            {
                var gm = (U7_SA_UnitChallenge_Masters_Phonics)target;
                U7_HierarchyAutomator.AutoAssignUnitChallenge(gm);
            });
            DrawDefaultInspector();
        }
    }
}
