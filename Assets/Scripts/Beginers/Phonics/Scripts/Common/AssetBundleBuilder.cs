using System;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;

public static class AssetBundleBuilder
{
    [MenuItem("SREDU/CreateAssetBundle")]
    public static void BuildAssetBundle()
    {
        string path = "Assets/AssetBundleBuilds";
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        try
        {
            BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
            AssetDatabase.Refresh();
            Debug.Log("AssetBundles built successfully to: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}
#endif