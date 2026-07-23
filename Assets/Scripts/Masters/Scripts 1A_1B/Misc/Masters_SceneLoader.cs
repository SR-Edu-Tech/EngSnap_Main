using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class Masters_SceneLoader : MonoBehaviour {
    
    
    [SerializeField] private string SceneName;


    private string URL = "https://drive.usercontent.google.com/u/0/uc?id=1l55BG7u2TYhvJmTY78svkx4W63AY0GpH&export=download";
    private AssetBundle bundle;
    private bool isLoading = false;


    public IEnumerator Start() {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
        }
#endif
        if (Microphone.devices.Length > 0) {
            Debug.Log("[Masters_SceneLoader] Mic detected: " + Microphone.devices[0]);
        }

        if (!isLoading) {
            isLoading = true;
        }
        else {
            yield break;
        }

        foreach (AssetBundle asset in AssetBundle.GetAllLoadedAssetBundles().ToArray()) {
            asset.Unload(false);
        }

        using (UnityWebRequest WWW = UnityWebRequestAssetBundle.GetAssetBundle(URL)) {
            if (!bundle) {
                yield return WWW.SendWebRequest();
                if (WWW.result != UnityWebRequest.Result.Success) Debug.LogWarning(WWW.error);
                else bundle = DownloadHandlerAssetBundle.GetContent(WWW);
            }

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            while (!asyncLoad.isDone) {
                yield return null;
            }
            bundle.Unload(false);
        }

        isLoading = false;
    }


}
