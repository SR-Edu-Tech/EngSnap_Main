using UnityEngine;

public class MainSceneReceiver : MonoBehaviour
{
    void Awake()
    {
        Debug.Log("[MainSceneReceiver] Awake called — DontDestroyOnLoad set.");
        DontDestroyOnLoad(gameObject);
        gameObject.SetActive(true);
    }

    void Start()
    {
        Debug.Log("[MainSceneReceiver] Start called. I am alive.");
    }

    void OnBundleSceneExited()
    {
        Debug.Log("[MainSceneReceiver] OnBundleSceneExited received.");
        HomeScreenManager mgr = HomeScreenManager.Instance;
        if (mgr != null)
            mgr.RestoreAfterBundle();
        else
            Debug.LogError("[MainSceneReceiver] HomeScreenManager.Instance is null.");
    }
}