using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public sealed partial class ProgressionDirector
{
    [Header("New room route")]
    [SerializeField] private bool _randomizeRooms;
    [SerializeField] private RunProgressionMode _progressionMode;
    [SerializeField] private ManualRunDefinition _manualDefinition;
    [SerializeField] private ManualStageEnvironment _manualEnvironment;
    private ManualRoutePolicy _manualPolicy;
    private AbilityHolder _manualHolder;
    private CharControlScript _manualControls;
    private readonly HashSet<Actor> _manualActors = new HashSet<Actor>();
    private GameObject _manualEncounterRoot;
    private readonly HashSet<SpawnerBehavior> _manualSpawners = new HashSet<SpawnerBehavior>();
    private bool _manualCompletionCandidate;
    public RunProgressionMode ActiveProgressionMode { get; private set; }
    public RunSessionState ManualSession => _manualPolicy?.Session;
    public RoomGraph ManualProjection { get; private set; }
    public bool HasManualCompletionCandidate => _manualCompletionCandidate;
    public int ManualEnemiesRemaining => _manualActors.Count;
    public ManualStageEnvironment ManualEnvironment => _manualEnvironment;
    public System.Collections.Generic.IReadOnlyList<int> ManualDestinations => _manualPolicy.AvailableDestinations;
    public bool CanOpenManualMenu => ManualSession != null && !ManualSession.IsClosed && ManualContext().CanConfirm;

    // Called before procedural dependency checks and generation. Selection uses the same existing
    // persisted arsenal source as PlayerActor.Awake, independent of script Awake ordering.
    private bool TrySelectManualInitialization()
    {
        if (!Enum.IsDefined(typeof(RunProgressionMode), _progressionMode))
        { Debug.LogError("Undefined progression mode.", this); enabled = false; return true; }
        ActiveProgressionMode = _progressionMode;
        if (_progressionMode == RunProgressionMode.ProceduralLegacy)
        { if (_manualEnvironment) _manualEnvironment.SetActive(false); return false; }
        WeaponScript selected = WeaponLoadout.LoadSelected();
        if (!_player || !_manualEnvironment)
        { Debug.LogError("Manual run requires player and authored environment.", this); enabled = false; return true; }
        if (!ManualRouteValidator.TrySnapshot(_manualDefinition, selected, out var route, out string error) ||
            !_manualEnvironment.Validate(route, out error))
        { Debug.LogError("Manual run refused: " + error, this); enabled = false; return true; }
        if (_randomizeRooms)
        {
            try
            {
                var prefabs = new List<GameObject>();
                if (_archetypePrefabs != null) foreach (var variant in _archetypePrefabs) if (variant) prefabs.Add(variant.gameObject);
                route = ManualRunRandomizer.Generate(route, AcquireRunSeed(), prefabs);
                if (_environment) _environment.SetGeometryActive(false);
                if (!_manualEnvironment.BuildGenerated(route, out error))
                { Debug.LogError(error, this); enabled = false; return true; }
            }
            catch (Exception exception)
            { Debug.LogError("New room generation refused: " + exception.Message, this); enabled = false; return true; }
        }
        route = TitanicConductorPlaytestRewards.Apply(route, selected);
        _manualPolicy = new ManualRoutePolicy(route);
        MarkStatisticsStart();
        ManualProjection = ManualRoomProjection.Create(route, _manualEnvironment);
        _manualHolder = _player.GetComponent<AbilityHolder>();
        if (_manualHolder) _manualHolder.SetManualInitializationBlocked(true);
        _manualControls = _player.GetComponent<CharControlScript>();
        if (_environment) _environment.SetGeometryActive(false);
        _manualEnvironment.SetActive(true);
        _manualEnvironment.ApplyGateState(ManualSession);
        if (!_player.gameObject.activeSelf)
        {
            _player.transform.position=ManualSession.Route.Node(ManualSession.Route.StartNode).Center;
            _player.gameObject.SetActive(true);
        }
        _stageBuilt = true;
        StartCoroutine(InitializeManualPlayer());
        return true;
    }

    private IEnumerator InitializeManualPlayer()
    {
        // All actors finish Awake before touching player navigation/resources. The mode and geometry
        // were already chosen, so this cannot accidentally materialize a procedural stage.
        yield return null;
        if (!_player || _manualPolicy == null) yield break;
        if (!ManualRouteValidator.ValidateLoadout(_player.CurrentWeapon, out string error))
        {
            Debug.LogError("Manual player initialization refused: " + error, this);
            _manualPolicy.TryEndRun(ManualSession.Id, RunOutcome.Aborted, ManualContext());
            _stageBuilt = false; _manualEnvironment.SetActive(false); enabled = false;
            yield break;
        }
        Vector3 start = ManualSession.Route.Node(ManualSession.Route.StartNode).Center;
        var agent = _player.GetComponent<NavMeshAgent>();
        if (agent && agent.isActiveAndEnabled && agent.isOnNavMesh) agent.Warp(start);
        else _player.transform.position = start;
        _boons = _player.GetComponent<RunBoons>() ?? _player.gameObject.AddComponent<RunBoons>();
        if (_manualDefinition.ProtocolCatalog && !_boons.ConfigureProtocolCatalog(_manualDefinition.ProtocolCatalog))
        { Debug.LogError("Manual Protocol catalog could not initialize.", this); enabled = false; yield break; }
        yield return null; // RunBoons.Start creates the isolated weapon/inventory before binding.
        if (!_boons.BindManualRun(_manualPolicy))
        {
            Debug.LogError("Manual reward inventory could not bind to its session.", this);
            _manualPolicy.TryEndRun(ManualSession.Id, RunOutcome.Aborted, ManualContext());
            yield break;
        }
        InitializeManualResourcesOnce();
        StartRunStatistics();
        _manualHolder.SetManualInitializationBlocked(false);
        _boons.ManualRewardChosen += OnManualRewardResolved;
        StartCoroutine(ManualEntryLoop());
    }

    private RunActionContext ManualContext() => new RunActionContext(_player && !_player.IsDead,
        Time.timeScale <= 0f || (_manualHolder && _manualHolder.IsGamePaused), _player && _player.IsCombatHitStopped,
        _manualHolder && _manualHolder.IsCasting,
        _manualControls && _manualControls.dashScript && _manualControls.dashScript.IsDashing(_player.gameObject),
        _manualCleanupPending || ManualSession != null && ManualSession.IsEncounterClosing);

    public bool TryCommitDestination(Guid sessionId, int nodeId)
    {
        if (_manualPolicy == null || !_manualEnvironment || !_manualPolicy.TryCommitDestination(sessionId, nodeId, ManualContext())) return false;
        _manualEnvironment.ApplyGateState(ManualSession); return true;
    }

    public bool TryEnterNode(Guid sessionId, int nodeId)
    {
        if (_manualPolicy == null || !_player || !_manualEnvironment || !_manualEnvironment.ContainsPlayer(nodeId, _player.transform.position) ||
            !_manualPolicy.TryEnterNode(sessionId, nodeId, ManualContext())) return false;
        _manualEnvironment.ApplyGateState(ManualSession);
        if (ManualSession.Phase == RunPhase.Encounter) SpawnManualEncounter(nodeId);
        return true;
    }

    private IEnumerator ManualEntryLoop()
    {
        var interval = new WaitForSeconds(.15f);
        while (_player && ManualSession != null && !ManualSession.IsClosed)
        {
            if (ManualSession.Phase == RunPhase.Traversal) TryEnterNode(ManualSession.Id, ManualSession.PendingDestination);
            yield return interval;
        }
    }

    private void SpawnManualEncounter(int nodeId)
    {
        _manualCompletionCandidate = false;
        ManualSession.IsEncounterClosing=false;
        var node = ManualSession.Route.Node(nodeId);
        _manualEncounterRoot = new GameObject("Manual encounter " + nodeId);
        _manualEncounterRoot.transform.SetParent(transform, false);
        _manualEncounterRoot.SetActive(false);
        _manualEncounterRoot.AddComponent<SwarmAttackCoordinator>().ConfigureCapacity(_maxConcurrentMeleePerRoom);
        foreach (var spawn in node.Spawns)
        {
            GameObject instance = Instantiate(spawn.Prefab, node.Center + spawn.Offset, Quaternion.identity, _manualEncounterRoot.transform);
            if (instance.TryGetComponent<EnemyAI>(out var ai))
            { ai.player = _player.transform; ai.ConfigureEncounterBounds(node.Center, node.ArenaSize); }
            if (instance.TryGetComponent<SpawnerBehavior>(out var spawner))
            {
                spawner.ConfigureArenaBounds(node.Center, node.ArenaSize);
                _manualSpawners.Add(spawner); spawner.Produced += OnManualEnemyProduced;
            }
            if (instance.TryGetComponent<HazardCasterBehavior>(out var caster)) caster.ConfigureEncounterEffects(_manualEncounterRoot.transform);
            var reaction = instance.GetComponent<CombatReactionController>() ?? instance.AddComponent<CombatReactionController>();
            reaction.ConfigureRank(spawn.Rank);
            if (spawn.Rank == EnemyRank.Boss)
            {
                var boss = instance.GetComponent<SectorBoss>() ?? instance.AddComponent<SectorBoss>();
                boss.Configure(_player);
            }
            Actor actor = instance.GetComponent<Actor>();
            var membership = instance.GetComponent<ProtocolEncounterMembership>() ?? instance.AddComponent<ProtocolEncounterMembership>();
            membership.Configure(ManualSession.Id, ManualSession.CurrentNode);
            _manualActors.Add(actor); actor.Died += OnManualEnemyDied;
            _runStatistics?.Watch(actor);
            instance.SetActive(true);
        }
        _manualEncounterRoot.SetActive(true);
    }

    private void OnManualEnemyProduced(Actor actor)
    {
        if (!actor) return;
        if (ManualSession == null || ManualSession.IsClosed || ManualSession.Phase != RunPhase.Encounter)
        { Destroy(actor.gameObject); return; }
        var node = ManualSession.Route.Node(ManualSession.CurrentNode);
        if (_manualEncounterRoot) actor.transform.SetParent(_manualEncounterRoot.transform, true);
        if (actor.TryGetComponent<EnemyAI>(out var ai))
        { ai.player = _player.transform; ai.ConfigureEncounterBounds(node.Center, node.ArenaSize); }
        var membership = actor.GetComponent<ProtocolEncounterMembership>() ?? actor.gameObject.AddComponent<ProtocolEncounterMembership>();
        membership.Configure(ManualSession.Id, node.Id);
        if (_manualActors.Add(actor)) actor.Died += OnManualEnemyDied;
        _runStatistics?.Watch(actor);
        _manualCompletionCandidate = false; ManualSession.IsEncounterClosing = false;
    }

    private void DetachManualSpawners()
    {
        foreach (var spawner in _manualSpawners) if (spawner) spawner.Produced -= OnManualEnemyProduced;
        _manualSpawners.Clear();
    }

    private void OnManualEnemyDied(Actor actor)
    {
        if (actor) actor.Died -= OnManualEnemyDied;
        if (_manualActors.Remove(actor) && _manualActors.Count == 0 && ManualSession != null && ManualSession.Phase == RunPhase.Encounter)
        { _manualCompletionCandidate = true; ManualSession.IsEncounterClosing=true; }
        // Intentionally no cleanup, reward or transition in this callback. Task 11 owns the frontier.
    }

    private void DisposeManualState()
    {
        if (_boons) _boons.ManualRewardChosen -= OnManualRewardResolved;
        foreach (var actor in _manualActors)
        {
            if (!actor) continue;
            actor.Died -= OnManualEnemyDied;
            Destroy(actor.gameObject);
        }
        _manualActors.Clear(); _manualCompletionCandidate = false;
        DetachManualSpawners();
        if (_manualEncounterRoot) Destroy(_manualEncounterRoot);
    }

    public bool TryOpenManualReward(Guid sessionId,int nodeId,int ordinal)
    {
        if(_manualPolicy==null||!_boons||!ManualContext().CanConfirm) return false;
        return _boons.OfferReward(new RunRewardRequest(sessionId,nodeId,ordinal));
    }

    private void OnManualRewardResolved(RunRewardTicket ticket)
    {
        if(ManualSession==null||ticket.Request.SessionId!=ManualSession.Id||ManualSession.IsClosed) return;
        _runStatistics?.Reward(ticket);
        _manualEnvironment.ApplyGateState(ManualSession);
        if(ManualSession.Phase==RunPhase.RewardPending)
            TryOpenManualReward(ManualSession.Id,ManualSession.CurrentNode,ManualSession.RewardOrdinal);
    }

    public bool TryAbortManualRun(Guid sessionId)
    {
        if(_manualPolicy==null||!ManualContext().CanConfirm||
            !_manualPolicy.TryEndRun(sessionId,RunOutcome.Aborted,ManualContext())) return false;
        _manualEnvironment.ApplyGateState(ManualSession);
        BeginManualReturn();
        return true;
    }
}
