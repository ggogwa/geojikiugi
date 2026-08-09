using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class VideoBuild
{
    public static void BuildWindows()
    {
        Directory.CreateDirectory("VideoBuild");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/BattleScene.unity" },
            locationPathName = "VideoBuild/BeggarEstateDefense.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Video build failed: " + report.summary.result);
    }
}
