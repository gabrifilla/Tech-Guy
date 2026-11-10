#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// Development-only boss shortcut (Editor and Development Build; this whole file is compiled out of release builds).
/// Press F9 in FirstSector while the route choice is open: every room before the boss is cleared through the real
/// route policy, encounter frontier and reward tickets (first offered card each time, so the required Mutation is
/// acquired), the player is healed to full and placed inside the boss room, and the fight starts normally.
/// The player is damage-immune only while the shortcut clears the earlier rooms. No combat, boon or room rule changes.
/// </summary>
[DisallowMultipleComponent]
public sealed class DevBossShortcut : MonoBehaviour
{
    public const Key Trigger = Key.F9;
    private const float StepTimeout = 10f;

    private ProgressionDirector _director;
    private PlayerActor _player;
    private bool _running;

    internal static void Attach(ProgressionDirector director, PlayerActor player)
    {
        if (!director || !player) return;
        var shortcut = director.GetComponent<DevBossShortcut>() ?? director.gameObject.AddComponent<DevBossShortcut>();
        shortcut._director = director; shortcut._player = player;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (_running || keyboard == null || !keyboard[Trigger].wasPressedThisFrame) return;
        var session = _director ? _director.ManualSession : null;
        if (session == null || session.Phase != RunPhase.RouteChoice || !_player || _player.IsDead)
        { Debug.Log("[DevBossShortcut] F9 funciona só com a escolha de rota aberta numa run ativa."); return; }
        StartCoroutine(RunToBoss(session));
    }

    private IEnumerator RunToBoss(RunSessionState session)
    {
        _running = true;
        var route = session.Route;
        var path = route.Paths.FirstOrDefault(p => p.Length > 0 && p[0] == session.CurrentNode && p.Contains(route.BossNode))
            ?? route.Paths.FirstOrDefault(p => p.Contains(session.CurrentNode) && p.Contains(route.BossNode));
        if (path == null) { Fail("nenhum caminho do nó atual até o boss."); yield break; }
        var run = _player.GetComponent<RunBoons>();
        _player.AddDamageImmunity(this);
        Debug.Log("[DevBossShortcut] Limpando a rota " + string.Join("/", path) + " até o boss (nó " + route.BossNode + ").");
        try
        {
            foreach (int nodeId in path.SkipWhile(id => id != session.CurrentNode).Skip(1))
            {
                if (session.IsClosed) { Fail("a sessão terminou durante o atalho."); yield break; }
                if (!_director.TryCommitDestination(session.Id, nodeId)) { Fail("commit recusado no nó " + nodeId + "."); yield break; }
                if (nodeId == route.BossNode)
                {
                    _player.RemoveDamageImmunity(this);
                    _player.RestoreHealthToMax();
                }
                if (!WarpInto(nodeId) || !_director.TryEnterNode(session.Id, nodeId)) { Fail("entrada recusada no nó " + nodeId + "."); yield break; }
                if (nodeId == route.BossNode) { Debug.Log("[DevBossShortcut] Sala do boss: luta iniciada."); yield break; }
                if (session.Phase == RunPhase.RepairPending) { _director.TryResolveRepair(session.Id, nodeId, false); yield return null; continue; }

                float deadline = Time.realtimeSinceStartup + StepTimeout;
                while (session.Phase == RunPhase.Encounter || session.Phase == RunPhase.FinalResolution)
                {
                    foreach (var enemy in _director.GetComponentsInChildren<Actor>())
                        if (enemy && !enemy.IsDead && enemy != _player) enemy.TakeDamage(1000000f);
                    if (Time.realtimeSinceStartup > deadline) { Fail("o encontro do nó " + nodeId + " não terminou."); yield break; }
                    yield return null;
                }
                deadline = Time.realtimeSinceStartup + StepTimeout;
                while (session.Phase == RunPhase.RewardPending)
                {
                    if (run && !run.IsChoosing) _director.TryOpenManualReward(session.Id, nodeId, session.RewardOrdinal);
                    yield return null;
                    if (run && run.IsChoosing && run.Choices.Count > 0) run.Choose(0);
                    if (Time.realtimeSinceStartup > deadline) { Fail("a recompensa do nó " + nodeId + " não foi resolvida."); yield break; }
                    yield return null;
                }
            }
        }
        finally
        {
            if (_player) _player.RemoveDamageImmunity(this);
            _running = false;
        }
    }

    // Spatial rooms (RUN-EXP-09B) may have a solid centre; stand on clear planned floor instead.
    private bool WarpInto(int nodeId)
    {
        var center = _director.ManualSession.Route.Node(nodeId).Center;
        var target = center;
        var layout = _director.ManualEnvironment.NodeLayout(nodeId);
        if (layout != null && !layout.Clear(Vector3.zero, 1.5f))
        {
            bool found = false;
            for (int r = 1; r < 12 && !found; r++)
                for (int x = -r; x <= r && !found; x++)
                    for (int z = -r; z <= r && !found; z++)
                        if (layout.Clear(new Vector3(x, 0, z), 1.5f)) { target = center + new Vector3(x, 0, z); found = true; }
        }
        var agent = _player.GetComponent<NavMeshAgent>();
        if (agent && agent.isActiveAndEnabled) return agent.Warp(target);
        _player.transform.position = target; return true;
    }

    private void Fail(string reason)
    {
        Debug.LogWarning("[DevBossShortcut] Parado: " + reason);
        if (_player) _player.RemoveDamageImmunity(this);
        _running = false;
    }
}
#endif
