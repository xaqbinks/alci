using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.Build.Reporting;

/// <summary>
/// This script adds a new menu item to the Unity Editor to automate the build process.
/// </summary>
public class BuildScript
{
    /// <summary>
    /// This method is called when the user clicks "Build/Build for Windows".
    /// </summary>
    [MenuItem("Build/Build for Windows")]
    public static void PerformWindowsBuild()
    {
        // 1. Get all scenes that are enabled in the Build Settings window.
        string[] enabledScenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (enabledScenes.Length == 0)
        {
            Debug.LogError("Build Error: No scenes found in Build Settings or none are enabled. Please add your main scene via File > Build Settings.");
            return;
        }

        // 2. Define the build options for a Windows 64-bit standalone build.
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = enabledScenes;
        buildPlayerOptions.locationPathName = "Builds/Windows/AlienCivilizations.exe"; // Output path
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None; // Use BuildOptions.Development for a development build

        Debug.Log("Starting Windows build... Output path: " + buildPlayerOptions.locationPathName);

        // 3. Start the build process.
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        // 4. Report the result.
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build Succeeded! Size: {summary.totalSize / 1024 / 1024} MB. Path: {summary.outputPath}");
            // Optionally, open the output folder.
            EditorUtility.RevealInFinder(summary.outputPath);
        }
        else
        {
            Debug.LogError($"Build Failed: {summary.result}");
        }
    }
}
