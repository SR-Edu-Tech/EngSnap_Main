using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace MastersPhonics
{
    /// <summary>
    /// Editor-only tool to diagnose why PrevBtn / NextBtn stay disabled in Unit 8.
    /// Run from menu: Tools > Unit 8 > Debug Concept Card Buttons
    /// </summary>
    public static class U8_ConceptCardDebugger
    {
        [MenuItem("Tools/Unit 8/Debug Concept Card Buttons")]
        public static void DebugButtons()
        {
            Debug.Log("=== U8 CONCEPT CARD BUTTON DEBUGGER ===");

            // 1. Find the ConceptCards script in scene
            var gm = Object.FindObjectOfType<U8_SA_GM01_ConceptCards_Masters_Phonics>(true);
            if (gm == null)
            {
                Debug.LogError("[DEBUG] U8_SA_GM01_ConceptCards_Masters_Phonics NOT FOUND in scene!");
                return;
            }

            Debug.Log($"[DEBUG] ConceptCards script found on: '{gm.gameObject.name}' | Active: {gm.gameObject.activeSelf} | ActiveInHierarchy: {gm.gameObject.activeInHierarchy} | Script Enabled: {gm.enabled}");

            // 2. Check parent chain
            Transform cur = gm.transform;
            string chain = gm.name;
            while (cur.parent != null)
            {
                cur = cur.parent;
                chain = cur.name + (cur.gameObject.activeSelf ? "(ON)" : "(OFF)") + " > " + chain;
            }
            Debug.Log($"[DEBUG] Hierarchy chain: {chain}");

            // 3. List ALL direct children of the panel
            Debug.Log($"[DEBUG] Direct children of '{gm.gameObject.name}' ({gm.transform.childCount} total):");
            for (int i = 0; i < gm.transform.childCount; i++)
            {
                Transform child = gm.transform.GetChild(i);
                var btn = child.GetComponent<Button>();
                string btnInfo = btn != null ? $"[HAS Button comp, interactable={btn.interactable}]" : "[NO Button comp]";
                Debug.Log($"  [{i}] '{child.name}' | activeSelf={child.gameObject.activeSelf} | activeInHierarchy={child.gameObject.activeInHierarchy} {btnInfo}");

                // Also check for missing scripts
                var components = child.GetComponents<Component>();
                for (int c = 0; c < components.Length; c++)
                {
                    if (components[c] == null)
                    {
                        Debug.LogWarning($"  [{i}] '{child.name}' has a MISSING SCRIPT at component index {c}!");
                    }
                }
            }

            // 4. Specifically look for PrevBtn and NextBtn recursively
            Transform prevBtnT = FindRecursive(gm.transform, "PrevBtn");
            Transform nextBtnT = FindRecursive(gm.transform, "NextBtn");

            if (prevBtnT == null)
                Debug.LogError("[DEBUG] PrevBtn NOT FOUND anywhere under the panel!");
            else
            {
                Debug.Log($"[DEBUG] PrevBtn FOUND at path: {GetPath(prevBtnT)} | activeSelf={prevBtnT.gameObject.activeSelf} | activeInHierarchy={prevBtnT.gameObject.activeInHierarchy}");
                var pb = prevBtnT.GetComponent<Button>();
                Debug.Log($"[DEBUG] PrevBtn Button component: {(pb != null ? $"EXISTS, interactable={pb.interactable}" : "MISSING")}");
            }

            if (nextBtnT == null)
                Debug.LogError("[DEBUG] NextBtn NOT FOUND anywhere under the panel!");
            else
            {
                Debug.Log($"[DEBUG] NextBtn FOUND at path: {GetPath(nextBtnT)} | activeSelf={nextBtnT.gameObject.activeSelf} | activeInHierarchy={nextBtnT.gameObject.activeInHierarchy}");
                var nb = nextBtnT.GetComponent<Button>();
                Debug.Log($"[DEBUG] NextBtn Button component: {(nb != null ? $"EXISTS, interactable={nb.interactable}" : "MISSING")}");
            }

            // 5. Check the serialized fields via SerializedObject
            var so = new SerializedObject(gm);
            var nextProp = so.FindProperty("nextCardBtn");
            var prevProp = so.FindProperty("prevCardBtn");

            Debug.Log($"[DEBUG] Serialized 'nextCardBtn': {(nextProp != null && nextProp.objectReferenceValue != null ? nextProp.objectReferenceValue.name : "NULL / NOT ASSIGNED")}");
            Debug.Log($"[DEBUG] Serialized 'prevCardBtn': {(prevProp != null && prevProp.objectReferenceValue != null ? prevProp.objectReferenceValue.name : "NULL / NOT ASSIGNED")}");

            // 6. Check UnitFlowManager's nextActivityBtn isn't accidentally pointing to a card button
            var ufm = Object.FindObjectOfType<U8_SA_UnitFlowManager_Masters_Phonics>(true);
            if (ufm != null)
            {
                var ufmSO = new SerializedObject(ufm);
                var mainNextProp = ufmSO.FindProperty("nextActivityBtn");
                var mainBackProp = ufmSO.FindProperty("globalBackBtn");

                string mainNextName = mainNextProp != null && mainNextProp.objectReferenceValue != null ? mainNextProp.objectReferenceValue.name : "NULL";
                string mainBackName = mainBackProp != null && mainBackProp.objectReferenceValue != null ? mainBackProp.objectReferenceValue.name : "NULL";

                Debug.Log($"[DEBUG] UnitFlowManager.nextActivityBtn -> '{mainNextName}'");
                Debug.Log($"[DEBUG] UnitFlowManager.globalBackBtn   -> '{mainBackName}'");

                // CRITICAL CHECK: Is the main next button accidentally the same as the card's NextBtn?
                if (nextBtnT != null && mainNextProp != null && mainNextProp.objectReferenceValue != null)
                {
                    Button mainBtn = mainNextProp.objectReferenceValue as Button;
                    if (mainBtn != null && mainBtn.gameObject == nextBtnT.gameObject)
                    {
                        Debug.LogError("[DEBUG] *** COLLISION! UnitFlowManager.nextActivityBtn IS the same object as the card's NextBtn! This is the bug! ***");
                    }
                }
            }

            Debug.Log("=== END U8 CONCEPT CARD BUTTON DEBUGGER ===");
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                Transform found = FindRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
