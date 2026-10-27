using System;
using System.IO;
using RoxxParking;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class PrototypeBuild
{
    private const string ScenePath = "Assets/Scenes/Parking.unity";

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode) Prepare();
        };
    }

    [MenuItem("Roxx Parking/Prepare prototype")]
    public static void Prepare()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Resources");
        TharModelBuilder.EnsureBuilt();
        EnvironmentAssetsBuilder.Prepare();
        WorkshopAssetsBuilder.Prepare();
        if (!File.Exists("Assets/Resources/PrototypeSurface.mat"))
        {
            Material material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, "Assets/Resources/PrototypeSurface.mat");
        }
        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Roxx Parking game", typeof(RoxxGame));
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        PlayerSettings.companyName = "Independent Games";
        PlayerSettings.productName = "Roxx Parking India";
        PlayerSettings.bundleVersion = "0.6.1";
        PlayerSettings.Android.bundleVersionCode = 7;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.indie.roxxparkingindia");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.runInBackground = false;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
        // Keep one input backend for the legacy touch/keyboard implementation on Android.
        var settings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
        SerializedProperty input = settings.FindProperty("activeInputHandler");
        if (input != null) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Debug.Log("ROXX_PREPARED: scene, desktop/Android settings and prototype shader ready.");
    }

    [MenuItem("Roxx Parking/Build Windows preview")]
    public static void Windows()
    {
        Prepare(); Build(BuildTarget.StandaloneWindows64, "Builds/Windows-0.6.1/RoxxParkingIndia.exe");
    }

    public static void SmokeTest()
    {
        Prepare();
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Roxx Parking/Build Android APK")]
    public static void Android()
    {
        Prepare(); EditorUserBuildSettings.buildAppBundle = false;
        Build(BuildTarget.Android, "Builds/Android/RoxxParkingIndia.apk");
    }

    [MenuItem("Roxx Parking/Build phone browser version")]
    public static void Web()
    {
        Prepare();
        PlayerSettings.WebGL.template="PROJECT:RoxxPhone";
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=false;
        PlayerSettings.WebGL.threadsSupport=false;
        PlayerSettings.WebGL.initialMemorySize=128;
        PlayerSettings.WebGL.maximumMemorySize=768;
        PlayerSettings.WebGL.memoryGrowthMode=WebGLMemoryGrowthMode.Geometric;
        PlayerSettings.WebGL.dataCaching=true;
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL,Il2CppCodeGeneration.OptimizeSize);
        Build(BuildTarget.WebGL,"Builds/Web-0.6.1");
    }

    private static void Build(BuildTarget target, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath }, target = target,
            subtarget = target==BuildTarget.WebGL?(int)WebGLTextureSubtarget.ETC2:0,
            locationPathName = output, options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Build failed: " + report.summary.result + "; inspect Editor.log for details.");
        Debug.Log("ROXX_BUILD_OK: " + Path.GetFullPath(output));
    }
}
