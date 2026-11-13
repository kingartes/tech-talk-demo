using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace TechTalk
{
    public static class TechTalkBuild
    {
        internal const string ScenePath="Assets/Demo/From Vertices to Pixels.unity";

        [MenuItem("Tech Talk/Build Windows x64")]
        public static void BuildWindows()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)==null) CreateScene();
            string project=Path.GetDirectoryName(Application.dataPath);
            string executable=Path.GetFullPath(Path.Combine(project,"..","Build","Windows","From Vertices to Pixels.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(executable));
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            var result=BuildPipeline.BuildPlayer(new[]{ScenePath},executable,BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(result.summary.result!=BuildResult.Succeeded)
                throw new InvalidOperationException($"Windows build {result.summary.result}: {result.summary.totalErrors} errors.");
            Debug.Log("Tech Talk Windows build complete: "+executable);
        }

        public static void CreateScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)==null)
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                new GameObject("Graphics Pipeline Demo",typeof(DemoStateController));
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth=1920;
            PlayerSettings.defaultScreenHeight=1080;
            AssetDatabase.SaveAssets();
        }
    }
}
