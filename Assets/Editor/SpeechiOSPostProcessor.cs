#if UNITY_EDITOR && UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public class SpeechiOSPostProcessor
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
            return;

        string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict rootDict = plist.root;

        // Add Microphone permission usage description if not already set
        if (rootDict["NSMicrophoneUsageDescription"] == null)
        {
            rootDict.SetString("NSMicrophoneUsageDescription", "This app requires access to the microphone to record your voice for speech recognition exercises.");
        }

        // Add Speech Recognition permission usage description if not already set
        if (rootDict["NSSpeechRecognitionUsageDescription"] == null)
        {
            rootDict.SetString("NSSpeechRecognitionUsageDescription", "This app requires speech recognition to evaluate and help improve your pronunciation.");
        }

        plist.WriteToFile(plistPath);
    }
}
#endif
