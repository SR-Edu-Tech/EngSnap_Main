//// ── iOSBuildPostProcess.cs ────────────────────────────────────────────────────
//// Automatically adds required iOS frameworks and Info.plist entries when
//// building for iOS. No manual Xcode editing needed.
//// Place in: Assets/Editor/iOSBuildPostProcess.cs
//// ─────────────────────────────────────────────────────────────────────────────

//#if UNITY_EDITOR
//using UnityEditor;
//using UnityEditor.Callbacks;
//using UnityEditor.iOS.Xcode;
//using System.IO;

//public class iOSBuildPostProcess
//{
//    [PostProcessBuild(1)]
//    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
//    {
//        if (target != BuildTarget.iOS) return;

//        // ── Xcode project: add frameworks ────────────────────────────────────
//        string projectPath = PBXProject.GetPBXProjectPath(buildPath);
//        var project = new PBXProject();
//        project.ReadFromFile(projectPath);

//        string targetGuid = project.GetUnityMainTargetGuid();
//        project.AddFrameworkToProject(targetGuid, "Speech.framework",       false);
//        project.AddFrameworkToProject(targetGuid, "AVFoundation.framework", false);

//        project.WriteToFile(projectPath);

//        // ── Info.plist: add usage descriptions ───────────────────────────────
//        string plistPath = Path.Combine(buildPath, "Info.plist");
//        var plist = new PlistDocument();
//        plist.ReadFromFile(plistPath);

//        plist.root.SetString(
//            "NSMicrophoneUsageDescription",
//            "This app uses the microphone to capture your voice for speech recognition.");

//        plist.root.SetString(
//            "NSSpeechRecognitionUsageDescription",
//            "This app uses speech recognition to convert your voice into text for gameplay.");

//        plist.WriteToFile(plistPath);

//        UnityEngine.Debug.Log("[iOSBuildPostProcess] Speech.framework and plist entries added.");
//    }
//}
//#endif
