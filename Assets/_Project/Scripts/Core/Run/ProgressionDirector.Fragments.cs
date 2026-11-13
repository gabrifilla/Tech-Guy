using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Fase 1b — Fragmentos de Dados, terminal upgrades and the first rescuable ally, wired onto the existing manual-run
/// lifecycle. Fragmentos are banked in MetaProgress at every room clear, at the boss death and at teardown, so what
/// was collected is kept on victory, death and abandon. Upgrades only use existing extension points: reward policy
/// (cards per offer, reroll), route reward steps (first room), repair policy and a player stat modifier.
/// </summary>
public sealed partial class ProgressionDirector
{
    private MetaProgressProfile _metaProfileAtStart;
    private int _runFragments, _pendingFragments, _combatRoomsWon, _npcTargetRoom;
    private bool _fragmentsTracking, _encounterHit;
    private float _encounterHealth;
    private RunFragmentsCounter _fragmentsCounter;
    private RescueCapsule _rescueCapsule;
    private int _rescueCapsuleNode = -1;

    /// <summary>Fragmentos earned in this run (drops + bonuses), already or about to be banked.</summary>
    public int RunFragments => _runFragments;
    /// <summary>Combat room (1-based) where the locked ally capsule waits this run (0 = none: already rescued).</summary>
    public int ManualAllyTargetRoom => _npcTargetRoom;
    public RescueCapsule ManualRescueCapsule => _rescueCapsule;
    public MetaProgressProfile MetaProfileAtRunStart => _metaProfileAtStart;

    private bool Owns(string upgradeId) => _metaProfileAtStart != null && _metaProfileAtStart.HasUpgrade(upgradeId);

    /// <summary>Before the route policy exists: loads the profile the run's upgrades are read from. The route itself is
    /// no longer changed ("Oferta inicial", a free extra boon, was retired on 10/11/2026).</summary>
    private void ApplyMetaRouteUpgrades(ManualRouteSnapshot route)
    {
        _metaProgress ??= MetaProgressStore.CreateDefault();
        _metaProfileAtStart = _metaProgress.Load();
    }

    /// <summary>After resources and the repair policy exist: health, repair and offer upgrades for this run.</summary>
    private void ApplyMetaRunUpgrades()
    {
        if (Owns(MetaUpgrades.Vitality) && _player && _player.Stats != null)
        {
            _player.Stats.AddModifier(new PlayerStatModifier(PlayerStatType.MaxHealthBonus, PlayerStatModifierMode.Flat, MetaUpgrades.VitalityMaxHealthBonus), this);
            _player.RefreshResourceStats(fillToMax: true);
        }
        if (_manualRepair != null && Owns(MetaUpgrades.Repair)) _manualRepair.FractionBonus = MetaUpgrades.RepairFractionBonus;
        if (_boons) _boons.ConfigureManualRunUpgrades(Owns(MetaUpgrades.FourthOption) ? MetaUpgrades.OfferCountWithFourthOption : 3,
            MetaUpgrades.RerollsFor(_metaProfileAtStart) + (Equipped(Artifacts.SparringWrap) ? Artifacts.SparringWrapRerolls : 0));
        ApplyRapportToRun();
    }

    private void BeginFragmentRun()
    {
        if (_fragmentsTracking) return;
        _fragmentsTracking = true; _runFragments = _pendingFragments = _combatRoomsWon = 0;
        CoinPickup.FragmentsCollected += OnFragmentsCollected;
        if (_player) _player.HealthChanged += OnPlayerHealthChangedForMeta;
        _fragmentsCounter = RunFragmentsCounter.Create(transform);
    }

    private void EndFragmentRun()
    {
        if (!_fragmentsTracking) return;
        FlushRunFragments();
        _fragmentsTracking = false;
        CoinPickup.FragmentsCollected -= OnFragmentsCollected;
        if (_player) { _player.HealthChanged -= OnPlayerHealthChangedForMeta; if (_player.Stats != null) _player.Stats.RemoveModifiersFrom(this); }
        EndRapportRun();
    }

    private void OnFragmentsCollected(int value) => AddRunFragments(value);

    private void AddRunFragments(int amount)
    {
        if (!_fragmentsTracking || amount <= 0) return;
        _runFragments += amount; _pendingFragments += amount;
        if (_fragmentsCounter) _fragmentsCounter.Set(_runFragments);
    }

    private void FlushRunFragments()
    {
        if (_pendingFragments <= 0 || _metaProgress == null || ManualSession == null) return;
        _metaProgress.AddFragments(_pendingFragments, ManualSession.Id.ToString());
        _pendingFragments = 0;
    }

    private void BeginEncounterFragments(ManualNodeDefinition node)
    {
        _encounterHit = false; _encounterHealth = _player ? _player.health : 0;
        // The ally is already there when the fight starts: locked, calling for help, freed only when the room is won.
        if (node != null && (node.Kind == ManualNodeKind.Combat || node.Kind == ManualNodeKind.Elite) &&
            _npcTargetRoom > 0 && _combatRoomsWon + 1 == _npcTargetRoom && !_rescueCapsule)
            SpawnRescueCapsule(node);
    }

    private void OnPlayerHealthChangedForMeta(Actor actor)
    {
        if (!actor) return;
        if (actor.health < _encounterHealth - .01f) _encounterHit = true;
        _encounterHealth = actor.health;
    }

    private void OnManualEliteDied(Actor elite)
    {
        if (elite) elite.Died -= OnManualEliteDied;
        AddRunFragments(FragmentEconomy.EliteKilled);
        if (elite) TryDropGift(elite.transform.position, GiftEconomy.EliteChance);
    }

    /// <summary>The encounter frontier resolved this room: floor pickup, room bonuses, ally capsule, then bank.</summary>
    private void OnManualEncounterWonForMeta(int nodeId)
    {
        if (ManualSession == null) return;
        CoinPickup.CollectAllLive();
        var node = ManualSession.Route.Node(nodeId);
        if (node != null && (node.Kind == ManualNodeKind.Combat || node.Kind == ManualNodeKind.Elite))
        {
            AddRunFragments(FragmentEconomy.CombatRoomCleared);
            if (!_encounterHit && Owns(MetaUpgrades.CleanRoom)) AddRunFragments(MetaUpgrades.CleanRoomFragments);
            _combatRoomsWon++;
            if (_rescueCapsule && !_rescueCapsule.Rescued && _rescueCapsuleNode == nodeId) _rescueCapsule.Unlock();
        }
        FlushRunFragments();
    }

    private void OnManualBossVictoryFragments()
    {
        AddRunFragments(FragmentEconomy.BossVictory);
        FlushRunFragments();
        if (_player) TryDropGift(_player.transform.position, GiftEconomy.BossChance);
    }

    private void SpawnRescueCapsule(ManualNodeDefinition node)
    {
        var id = ManualSession.Id;
        _rescueCapsuleNode = node.Id;
        _rescueCapsule = RescueCapsule.Spawn(transform, CapsulePosition(node), _player, MetaAllies.UnknownDisplayName, MetaAllies.UnknownRescueLines,
            MetaAllies.UnknownBarks,
            () => ManualSession != null && ManualSession.Id == id && !ManualSession.IsClosed && !(_boons && _boons.IsChoosing) &&
                  !(_manualHolder && _manualHolder.IsGamePaused),
            () =>
            {
                // Saved at once: a later death or abandon keeps the rescue.
                _metaProgress?.RecordAllyRescued(MetaAllies.Unknown, id.ToString());
                _npcTargetRoom = 0;
                // Immediate payoff: the ally patches the player up before the dialogue.
                if (_player && !_player.IsDead) _player.Heal(_player.maxHealth * MetaAllies.RescueHealFraction);
            });
    }

    /// <summary>Clear planned floor a few metres from the centre (spatial rooms can have a solid centre or covers).</summary>
    private Vector3 CapsulePosition(ManualNodeDefinition node)
    {
        var layout = _manualEnvironment ? _manualEnvironment.NodeLayout(node.Id) : null;
        foreach (float radius in new[] { 3.5f, 4.5f, 2.5f, 5.5f })
            for (int step = 0; step < 8; step++)
            {
                float angle = step * Mathf.PI * .25f;
                var local = new Vector3(Mathf.Sin(angle) * radius, 0, Mathf.Cos(angle) * radius);
                if (layout != null && !layout.Clear(local, 1.6f)) continue;
                if (NavMesh.SamplePosition(node.Center + local, out var hit, .4f, NavMesh.AllAreas)) return hit.position;
            }
        return node.Center;
    }
}
