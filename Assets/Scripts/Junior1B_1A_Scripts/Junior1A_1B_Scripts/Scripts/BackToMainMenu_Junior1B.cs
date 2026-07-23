using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Junior1B
{
    // The class name here MUST match your file name: BackToMenu_Junior1B
    public class BackToMenu_Junior1B : MonoBehaviour
    {
        public void backbuttonclick()
        {
            // Create a new GameObject outside the bundle scene
            // so its coroutine survives the unload
            GameObject runner = new GameObject("BackToMainMenuRunner_Junior1B");
            DontDestroyOnLoad(runner);
            
            // Connects cleanly to the runner class right below it
            runner.AddComponent<BackToMainMenuRunner>().Run();
        }
    }

    // This runner class safely handles the scene shifting logic inside the Junior1B namespace
    public class BackToMainMenuRunner : MonoBehaviour
    {
        public void Run()
        {
            StartCoroutine(ReturnToMain());
        }

        private IEnumerator ReturnToMain()
        {
            Debug.Log("[BackToMainMenuRunner_Junior1B] Starting return to main.");

            // Find scenes before unload
            Scene bundleScene = default;
            Scene mainScene = default;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                // Main scene is always index 0 (first loaded)
                if (i == 0) mainScene = s;
                else bundleScene = s;
            }

            if (!bundleScene.IsValid() || !mainScene.IsValid())
            {
                Debug.LogError("[BackToMainMenuRunner_Junior1B] Could not find both scenes.");
                Destroy(gameObject);
                yield break;
            }

            Debug.Log($"[BackToMainMenuRunner_Junior1B] Main: '{mainScene.name}' | " +
                      $"Bundle: '{bundleScene.name}'");

            SceneManager.SetActiveScene(mainScene);

            // Unload bundle — this runner survives because it's in DontDestroyOnLoad
            yield return SceneManager.UnloadSceneAsync(bundleScene);
            Debug.Log("[BackToMainMenuRunner_Junior1B] Bundle unloaded.");

            yield return Resources.UnloadUnusedAssets();
            Debug.Log("[BackToMainMenuRunner_Junior1B] Assets cleaned.");

            // Find MainSceneReceiver including inactive objects
            GameObject receiver = null;
            foreach (GameObject go in FindObjectsOfType<GameObject>(true))
            {
                if (go.name == "MainSceneReceiver")
                {
                    receiver = go;
                    break;
                }
            }

            if (receiver != null)
            {
                Debug.Log("[BackToMainMenuRunner_Junior1B] Found MainSceneReceiver. Sending message.");
                receiver.SetActive(true);
                receiver.SendMessage("OnBundleSceneExited", SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                Debug.LogError("[BackToMainMenuRunner_Junior1B] MainSceneReceiver not found.");
            }

            // Clean up runner
            Destroy(gameObject);
        }
    }
}