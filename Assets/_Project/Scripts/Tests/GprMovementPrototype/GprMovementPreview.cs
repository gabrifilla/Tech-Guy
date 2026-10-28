#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TechGuy.Tests.GprMovement
{
    // Opens an UNSAVED empty scene. Refuses to replace an active game or dirty scene.
    [InitializeOnLoad]
    public static class GprMovementPreview
    {
        private const string Pending = "GPR.C01.2a.Preview";
        private const string Previous = "GPR.C01.2a.Scene";
        static GprMovementPreview() { EditorApplication.playModeStateChanged += Changed; }

        [MenuItem("Tech-Guy/Experiments/C01.2a/Open isolated WASD arena")]
        public static void Begin()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || EditorSceneManager.sceneCount != 1 || string.IsNullOrEmpty(scene.path))
                throw new System.InvalidOperationException("Requires one saved clean scene and Edit Mode.");
            SessionState.SetString(Previous, scene.path);
            SessionState.SetBool(Pending, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.isPlaying = true;
        }

        private static void Changed(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Pending, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
                new GameObject("GPR-C01.2a temporary fixture").AddComponent<GprMovementArena>().Build(true);
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Pending, false);
                EditorSceneManager.OpenScene(SessionState.GetString(Previous, ""), OpenSceneMode.Single);
            }
        }
    }
}
#endif
