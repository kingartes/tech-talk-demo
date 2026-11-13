using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TechTalk
{
    public sealed class TechTalkControls : EditorWindow
    {
        [MenuItem("Tech Talk/Open Demo Controls")]
        public static void ShowWindow()
        {
            var window = GetWindow<TechTalkControls>("Demo Controls");
            window.minSize = new Vector2(380, 420);
            window.Show();
        }

        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            var demo = Object.FindFirstObjectByType<DemoStateController>();
            EditorGUILayout.LabelField("Graphics Pipeline Demo", EditorStyles.boldLabel);
            if (demo == null)
            {
                EditorGUILayout.HelpBox("Open the demo scene to start the presentation.", MessageType.Info);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("Open demo scene"))
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            EditorSceneManager.OpenScene(TechTalkBuild.ScenePath);
                    }
                }
                return;
            }
            DrawControls(demo);
        }

        internal static void DrawControls(DemoStateController demo)
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Start Play Mode to use the presentation controls.", MessageType.Info);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (GUILayout.Button("Start demo"))
                    {
                        EditorApplication.isPlaying = true;
                        ShowGame();
                    }
                }
            }
            else if (!demo.IsReady)
            {
                EditorGUILayout.HelpBox("The demo is initializing. If scripts were recompiled during Play Mode, stop and start Play Mode again.", MessageType.Info);
            }

            if (GUILayout.Button("Show Game view")) ShowGame();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Presentation states", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || !demo.IsReady))
            {
                int selected = GUILayout.SelectionGrid(demo.State, DemoStateController.Titles, 2,
                    GUILayout.Height(190));
                if (selected != demo.State)
                {
                    demo.Select(selected);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.HelpBox("Game view contains telemetry, sliders, pause, reset and failure replay. Keyboard shortcuts work when Game view has focus.", MessageType.None);
        }

        private static void ShowGame()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
    }

    [CustomEditor(typeof(DemoStateController))]
    internal sealed class DemoStateControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            TechTalkControls.DrawControls((DemoStateController)target);
            if (GUILayout.Button("Open Demo Controls window")) TechTalkControls.ShowWindow();
        }
    }
}
