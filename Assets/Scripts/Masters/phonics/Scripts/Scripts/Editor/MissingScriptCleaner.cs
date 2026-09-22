using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace MastersPhonics.Editor
{
    public class MissingScriptCleaner : EditorWindow
    {
        [MenuItem("Tools/Phonics/Clean Missing Scripts In Active Scene")]
        public static void CleanSceneMissingScripts()
        {
            int totalRemoved = 0;
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                totalRemoved += CleanGameObjectRecursive(root);
            }

            if (totalRemoved > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log($"<color=#4CAF50><b>[MissingScriptCleaner]</b> Successfully removed {totalRemoved} missing script component(s) from scene '{activeScene.name}'.</color>");
                EditorUtility.DisplayDialog("Missing Scripts Cleaned", $"Successfully removed {totalRemoved} missing script component(s) from active scene.", "OK");
            }
            else
            {
                Debug.Log("<color=#2196F3><b>[MissingScriptCleaner]</b> No missing script components found in active scene.</color>");
                EditorUtility.DisplayDialog("Clean", "No missing script components found in active scene.", "OK");
            }
        }

        [MenuItem("GameObject/Phonics - Remove Missing Scripts", false, 0)]
        public static void CleanSelectedGameObjects()
        {
            int totalRemoved = 0;
            foreach (var go in Selection.gameObjects)
            {
                totalRemoved += CleanGameObjectRecursive(go);
            }

            if (totalRemoved > 0)
            {
                Debug.Log($"<color=#4CAF50><b>[MissingScriptCleaner]</b> Successfully removed {totalRemoved} missing script component(s) from selected object(s).</color>");
            }
        }

        private static int CleanGameObjectRecursive(GameObject go)
        {
            if (go == null) return 0;

            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

            for (int i = 0; i < go.transform.childCount; i++)
            {
                count += CleanGameObjectRecursive(go.transform.GetChild(i).gameObject);
            }

            return count;
        }
    }
}
