using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Command-line entry points:
///   Unity -batchmode -quit -projectPath WhoMe -executeMethod Builder.Validate
///   Unity -batchmode -quit -projectPath WhoMe -executeMethod Builder.BuildWebGL
///   Unity -batchmode -quit -projectPath WhoMe -executeMethod Builder.BuildWindows
public static class Builder
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Who Me/Setup Scene + Settings")]
    public static void Setup()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.11f);
        camGo.AddComponent<AudioListener>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.companyName = "GameOff2026";
        PlayerSettings.productName = "Who, Me?";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        // itch.io friendly: gzip with JS fallback works without special server headers.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        AssetDatabase.SaveAssets();
        Debug.Log("[Builder] Scene + settings ready.");
    }

    /// Sanity-check case content: every case must stay solvable after every piece of evidence.
    [MenuItem("Who Me/Validate Cases")]
    public static void Validate()
    {
        int errors = 0;
        foreach (var c in Cases.All)
        {
            for (int r = 0; r <= c.evidence.Length; r++)
            {
                int alive = Enumerable.Range(0, c.suspects.Length)
                    .Count(s => !c.evidence.Take(r).Any(e => e.rules.ContainsKey(s)));
                if (alive == 0) { Debug.LogError($"[Validate] {c.title}: no valid suspect after evidence {r}"); errors++; }
                else Debug.Log($"[Validate] {c.title}: after {r} evidence -> {alive} valid suspect(s)");
            }
            foreach (var e in c.evidence)
                foreach (var k in e.rules.Keys)
                    if (k < 0 || k >= c.suspects.Length) { Debug.LogError($"[Validate] {c.title}/{e.title}: bad suspect index {k}"); errors++; }
        }
        if (errors > 0) throw new Exception($"Case validation failed with {errors} error(s)");
        Debug.Log("[Validate] All cases OK.");
    }

    public static void BuildWebGL() => Build(BuildTarget.WebGL, "Builds/WebGL");
    public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/WhoMe.exe");

    static void Build(BuildTarget target, string path)
    {
        Validate();
        Setup();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = path,
            target = target,
            options = BuildOptions.None,
        });
        var s = report.summary;
        Debug.Log($"[Builder] {target}: {s.result}, {s.totalSize / (1024 * 1024f):0.0} MB, {s.totalErrors} errors, {s.totalTime}");
        if (s.result != BuildResult.Succeeded) throw new Exception("Build failed: " + s.result);
    }
}
