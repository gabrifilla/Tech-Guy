using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>Batch smoke check against the saved scene and the real player controller.</summary>
[InitializeOnLoad]
public static class LobbySceneValidation
{
    private const string PendingKey = "TechGuy.LobbyValidation.Pending";
    private static double _started;
    private static NavMeshAgent _agent;
    private static Vector3 _start;
    private static bool _moving;
    private static int _errors;

    static LobbySceneValidation()
    {
        if (SessionState.GetBool(PendingKey, false)) Subscribe();
    }

    public static void Run()
    {
        EditorSceneManager.OpenScene(LobbySceneBuilder.ScenePath);
        SessionState.SetBool(PendingKey, true);
        Subscribe();
        EditorApplication.EnterPlaymode();
    }

    private static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= OnState;
        EditorApplication.playModeStateChanged += OnState;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _errors++;
    }

    private static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        _started = EditorApplication.timeSinceStartup;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        try
        {
            double elapsed = EditorApplication.timeSinceStartup - _started;
            if (elapsed < 3) return;
            if (!_moving)
            {
                PlayerActor[] players = UnityEngine.Object.FindObjectsByType<PlayerActor>(FindObjectsSortMode.None);
                Require(players.Length == 1, "Expected exactly one player.");
                Require(UnityEngine.Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None).Length == 0, "Safe zone contains enemies.");
                PlayerActor player = players[0];
                Require(player.HandTransform && player.CurrentWeapon, "Player weapon setup did not survive scene cloning.");
                Require(Camera.main, "Main camera missing.");
                Require(Application.CanStreamedLevelBeLoaded("Playground"), "Expedition scene cannot be loaded.");
                _agent = player.GetComponent<NavMeshAgent>();
                Require(_agent && _agent.isOnNavMesh, "Player is not on the saved NavMesh.");
                _start = player.transform.position;
                Require(_agent.SetDestination(new Vector3(0, 0, -7)), "Movement request rejected.");
                _moving = true;
                _started = EditorApplication.timeSinceStartup;
                return;
            }
            if (elapsed < 5) return;
            Require((_agent.transform.position - _start).sqrMagnitude > 1f, "Player failed to move in Play Mode.");
            Require(_errors == 0, "Runtime errors were logged: " + _errors);
            Debug.Log("NEXUS_PLAYMODE_SUCCESS: player, weapon, camera, saved NavMesh, movement, safe zone and destination validated.");
            Finish(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish(1);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Finish(int exitCode)
    {
        SessionState.SetBool(PendingKey, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        EditorApplication.Exit(exitCode);
    }
}
