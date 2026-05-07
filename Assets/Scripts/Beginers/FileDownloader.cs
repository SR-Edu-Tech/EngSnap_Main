using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class FileDownloader : MonoBehaviour
{
    [Header("Download Settings")]
    [SerializeField] private string fileUrl = "https://drive.google.com/uc?export=download&id=1rqeuEO4ILcLXH4dUlCNAjd0g2-IG06Gj";

    // This matches exactly where WhisperManager looks:
    // Application.persistentDataPath + "Whisper/ggml-base.bin"
    [SerializeField] private string subFolder = "Whisper";
    [SerializeField] private string fileName = "ggml-base.bin";

    private string FullPath => Path.Combine(Application.persistentDataPath, subFolder, fileName);

    void Start()
    {
        Debug.Log($"[FileDownloader] Target path: {FullPath}");

        if (!File.Exists(FullPath))
        {
            Debug.Log("[FileDownloader] Model not found. Starting download...");
            StartCoroutine(DownloadFile(fileUrl, FullPath));
        }
        else
        {
            Debug.Log("[FileDownloader] Model already exists. Skipping download.");
        }
    }

    IEnumerator DownloadFile(string url, string path)
    {
        // Ensure the Whisper subfolder exists before downloading
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Debug.Log($"[FileDownloader] Created folder: {directory}");
        }

        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
        request.downloadHandler = new DownloadHandlerFile(path);
        request.SendWebRequest();

        while (!request.isDone)
        {
            float progress = request.downloadProgress * 100f;
            Debug.Log($"[FileDownloader] Downloading: {progress:F1}%");
            yield return new WaitForSeconds(0.5f); // log every 0.5s instead of every frame
        }

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[FileDownloader] Download complete! Saved to: {path}");
        }
        else
        {
            Debug.LogError($"[FileDownloader] Download failed: {request.error}");

            // Clean up incomplete file if download failed
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("[FileDownloader] Cleaned up incomplete file.");
            }
        }
    }
}