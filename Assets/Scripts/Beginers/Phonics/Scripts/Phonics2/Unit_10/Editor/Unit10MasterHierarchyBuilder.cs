#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Phonics2.Unit10.Editor
{
    public static class Unit10MasterHierarchyBuilder
    {
        [MenuItem("EngSnap/Phonics2/Unit 10/Build FULL Unit 10 (Stops 1, 2, 3, 4) in Active Scene", false, 100)]
        public static void BuildFullUnit10InScene()
        {
            // 1. Ensure or find Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            }

            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
            }

            // 2. Ensure Unit10_UIPanel container
            Transform unit10PanelTransform = canvas.transform.Find("Unit10_UIPanel");
            GameObject unit10PanelGO;
            if (unit10PanelTransform == null)
            {
                unit10PanelGO = new GameObject("Unit10_UIPanel", typeof(RectTransform));
                unit10PanelGO.transform.SetParent(canvas.transform, false);
                SetStretchAll(unit10PanelGO.GetComponent<RectTransform>());
                Undo.RegisterCreatedObjectUndo(unit10PanelGO, "Create Unit10_UIPanel");
            }
            else
            {
                unit10PanelGO = unit10PanelTransform.gameObject;
            }

            // 3. Build each Stop individually
            ICanSeeHierarchyBuilder.BuildStop1HierarchyInScene();
            EllFamilyHierarchyBuilder.BuildStop2HierarchyInScene();
            DogInTheWellHierarchyBuilder.BuildStop3HierarchyInScene();
            GraduationHierarchyBuilder.BuildStop4HierarchyInScene();

            // 4. Link Panels across stops
            Transform stop1 = unit10PanelGO.transform.Find("Stop1_ICanSee");
            Transform stop2 = unit10PanelGO.transform.Find("Stop2_TheEllFamily");
            Transform stop3 = unit10PanelGO.transform.Find("Stop3_TheDogInTheWell");
            Transform stop4 = unit10PanelGO.transform.Find("Stop4_QuestionsAndGraduation");

            if (stop1 != null && stop2 != null)
            {
                ICanSeeController c1 = stop1.GetComponent<ICanSeeController>();
                if (c1 != null)
                {
                    SerializedObject s1 = new SerializedObject(c1);
                    s1.FindProperty("nextPanel").objectReferenceValue = stop2.gameObject;
                    s1.FindProperty("unitContentPanel").objectReferenceValue = unit10PanelGO;
                    s1.ApplyModifiedProperties();
                }
            }

            if (stop2 != null && stop3 != null)
            {
                EllFamilyController c2 = stop2.GetComponent<EllFamilyController>();
                if (c2 != null)
                {
                    SerializedObject s2 = new SerializedObject(c2);
                    s2.FindProperty("nextPanel").objectReferenceValue = stop3.gameObject;
                    s2.FindProperty("unitContentPanel").objectReferenceValue = unit10PanelGO;
                    s2.ApplyModifiedProperties();
                }
            }

            if (stop3 != null && stop4 != null)
            {
                DogInTheWellController c3 = stop3.GetComponent<DogInTheWellController>();
                if (c3 != null)
                {
                    SerializedObject s3 = new SerializedObject(c3);
                    s3.FindProperty("nextPanel").objectReferenceValue = stop4.gameObject;
                    s3.FindProperty("unitContentPanel").objectReferenceValue = unit10PanelGO;
                    s3.ApplyModifiedProperties();
                }
            }

            if (stop4 != null)
            {
                GraduationController c4 = stop4.GetComponent<GraduationController>();
                if (c4 != null)
                {
                    SerializedObject s4 = new SerializedObject(c4);
                    s4.FindProperty("unitContentPanel").objectReferenceValue = unit10PanelGO;
                    s4.ApplyModifiedProperties();
                }
            }

            // 5. Initial Activation State: Stop 1 Active, Stops 2, 3, 4 Inactive
            if (stop1 != null) stop1.gameObject.SetActive(true);
            if (stop2 != null) stop2.gameObject.SetActive(false);
            if (stop3 != null) stop3.gameObject.SetActive(false);
            if (stop4 != null) stop4.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Selection.activeGameObject = unit10PanelGO;

            Debug.Log("[EngSnap] ⭐ FULL Unit 10 (Stops 1, 2, 3, 4) built successfully and interconnected in active scene!");
        }

        private static void SetStretchAll(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
#endif
