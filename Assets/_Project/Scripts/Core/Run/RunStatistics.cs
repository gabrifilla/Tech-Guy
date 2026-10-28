using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Manual-session observer of existing callbacks. No damage decisions and no event bus.</summary>
public sealed class RunStatistics : IDisposable
{
    private PlayerActor _player;
    private readonly RunSummary _data;
    private readonly double _wallStart, _scaledStart;
    private readonly HashSet<Actor> _actors = new HashSet<Actor>();
    private readonly Dictionary<string, List<long[]>> _receipts = new Dictionary<string, List<long[]>>();
    private readonly Dictionary<string, bool> _lethalCredit = new Dictionary<string, bool>();
    private readonly HashSet<string> _deaths = new HashSet<string>(), _rewards = new HashSet<string>();
    private AbilityHolder _holder;
    private CharControlScript _controls;
    private long _nextAction;
    private RunDamageSource _current;
    private bool _disposed;
    public RunDamageSource CurrentSource => _current;
    public RunStatistics(PlayerActor player, Guid session, ulong seed, double wallStart, double scaledStart)
    {
        if (!player || session == Guid.Empty) throw new ArgumentException("Run statistics require a player and session.");
        _player = player; _wallStart = wallStart; _scaledStart = scaledStart;
        _data = new RunSummary { SessionId = session.ToString(), Seed = seed.ToString(),
            InitialWeapon = Family(), StartedUtc = DateTime.UtcNow.ToString("o") };
        player.RunStatistics = this;
        _holder = player.GetComponent<AbilityHolder>(); _controls = player.GetComponent<CharControlScript>();
        if (_holder) _holder.AbilityUsed += OnAbilityUsed;
        if (_controls) _controls.BasicAttackPerformed += OnBasic;
        Watch(player);
    }
    private string Family() => _player && _player.CurrentWeapon ? WeaponRunModifiers.Identify(_player.CurrentWeapon).ToString() : "Unknown";
    public IDisposable BeginAction(string ability)
    {
        var previous = _current;
        string mutation = "";
        if (ability.Length == 1 && "QWER".IndexOf(ability, StringComparison.Ordinal) >= 0 &&
            _player.TryGetComponent(out RunBoons run))
            mutation = run.WeaponModifiers?.Expansion.MutationId((BoonSlot)"QWER".IndexOf(ability, StringComparison.Ordinal)) ?? "";
        _current = new RunDamageSource(_data.SessionId, Family(), ability, "action:" + checked(++_nextAction), mutation);
        return new ActionScope(() => _current = previous);
    }
    public RunDamageSource UnknownSource() => new RunDamageSource(_data.SessionId, "Unknown", "Unattributed",
        "unavailable", attribution: "known_player_unknown_ability");
    public RunDamageSource SourceFor(GauntletImpactContext context)
    {
        if (context == null || context.Owner != _player) return _current ?? UnknownSource();
        string mutation = context.Snapshot?.Mutation.HasValue == true
            ? GauntletContentAuthoring.ForMutation(context.Snapshot.Mutation.Value).Id : context.Snapshot?.BasicMutationId ?? "";
        return new RunDamageSource(_data.SessionId, context.Family.ToString(), context.Slot.ToString(),
            "gauntlet:" + context.Epoch + ":" + context.Execution, mutation,
            parents: new[] { "gauntlet:" + context.Epoch + ":" + context.RootExecution });
    }
    public void Watch(Actor actor)
    {
        if (_disposed || !actor || !_actors.Add(actor)) return;
        actor.DamageReceived += OnDamage;
        actor.Died += OnDeath;
    }
    private void OnDamage(Actor actor, float amount)
    {
        try
        {
            if (_disposed || _data.Finalized || !actor.PublishedDamageTransaction.HasValue) return;
            var tx = actor.PublishedDamageTransaction.Value;
            if (!ReactionNumeric.IsFinite(tx.Effective) || tx.Effective <= 0) return;
            string life = tx.TargetId + ":" + tx.Generation;
            if (!RememberReceipt(life, tx.Sequence))
            { _data.DuplicateReceiptsIgnored++; return; }
            if (actor == _player) { _data.DamageReceived += tx.Effective; return; }
            if (actor.Affiliation != CombatAffiliation.Enemy) return;
            var source = tx.Source;
            bool owned = source != null && source.SessionId == _data.SessionId;
            string attribution = owned ? source.Attribution : source == null ? "no_player_provenance" : "stale_session_provenance";
            string weapon = owned ? source.Weapon : "Unknown", ability = owned ? source.Ability : "Unattributed";
            string mutation = owned ? source.MutationId : "", producer = owned ? source.Producer : "Unattributed";
            string producerId = owned ? source.ProducerId : "", modifiers = owned ? string.Join(",", source.Modifiers) : "";
            bool secondary = owned && source.Secondary;
            var bucket = _data.Damage.Find(x => x.Weapon == weapon && x.Ability == ability && x.MutationId == mutation &&
                x.Producer == producer && x.ProducerId == producerId && x.PlayerAttributed == owned &&
                x.Secondary == secondary && x.Attribution == attribution && x.ModifierTags == modifiers);
            if (bucket == null)
            {
                bucket = new RunDamageBucket { Weapon = weapon, Ability = ability, MutationId = mutation,
                    Producer = producer, ProducerId = producerId, PlayerAttributed = owned, Secondary = secondary,
                    Attribution = attribution, ModifierTags = modifiers };
                _data.Damage.Add(bucket);
            }
            bucket.Effective += tx.Effective; bucket.HpTransactions++;
            if (tx.OverkillKnown) bucket.Overkill += tx.Overkill;
            _data.ObservedEnemyHpLoss += tx.Effective;
            if (owned)
            {
                _data.EffectiveDamage += tx.Effective; _data.ConfirmedHpTransactions++;
                if (tx.OverkillKnown) _data.Overkill += tx.Overkill; else _data.OverkillComplete = false;
                _data.LargestImpact = Math.Max(_data.LargestImpact, tx.Effective);
                var a = Ability(weapon, ability); a.EffectiveIncludingChildren += tx.Effective;
                if (secondary) a.SecondaryHpTransactions++; else a.NativeHpTransactions++;
            }
            else _data.UnattributedEnemyHpLoss += tx.Effective;
            _lethalCredit[life] = owned && tx.After <= 0;
            if (_data.RecentDamage.Count == 64) _data.RecentDamage.RemoveAt(0);
            _data.RecentDamage.Add(new RunDamageSample { TargetId = tx.TargetId, Generation = tx.Generation,
                Transaction = tx.Sequence, Frame = Time.frameCount, Before = tx.Before, After = tx.After,
                Effective = tx.Effective, HpAttempt = tx.HpAttempt, Overkill = tx.Overkill, OverkillKnown = tx.OverkillKnown,
                PlayerAttributed = owned, Weapon = weapon, Ability = ability, MutationId = mutation,
                Producer = producer, ProducerId = producerId, Attribution = attribution, Secondary = secondary,
                Execution = source?.Execution, Parents = source?.Parents.ToArray(), ParentsTruncated = source?.ParentsTruncated ?? false });
        }
        catch (Exception ex) { Fault(ex); }
    }
    // Compress contiguous transaction identities; out-of-order publication creates only temporary gaps.
    private bool RememberReceipt(string life, long sequence)
    {
        if (!_receipts.TryGetValue(life, out var ranges))
            _receipts.Add(life, ranges = new List<long[]>());
        int index = 0;
        while (index < ranges.Count && ranges[index][1] < sequence - 1) index++;
        if (index < ranges.Count && ranges[index][0] <= sequence && sequence <= ranges[index][1]) return false;
        if (index == ranges.Count || sequence < ranges[index][0] - 1) ranges.Insert(index, new[] { sequence, sequence });
        else { ranges[index][0] = Math.Min(ranges[index][0], sequence); ranges[index][1] = Math.Max(ranges[index][1], sequence); }
        if (index + 1 < ranges.Count && ranges[index][1] + 1 >= ranges[index + 1][0])
        { ranges[index][1] = ranges[index + 1][1]; ranges.RemoveAt(index + 1); }
        return true;
    }
    private void OnDeath(Actor actor)
    {
        try
        {
            if (_disposed || _data.Finalized || actor == _player || actor.Affiliation != CombatAffiliation.Enemy) return;
            string life = UnityEngine.EntityId.ToULong(actor.GetEntityId()) + ":" + actor.CombatGeneration;
            if (!_deaths.Add(life)) return;
            _data.EnemiesDefeated++;
            if (_lethalCredit.TryGetValue(life, out bool credited) && credited) _data.PlayerCreditedKills++;
            actor.DamageReceived -= OnDamage; actor.Died -= OnDeath; _actors.Remove(actor);
        }
        catch (Exception ex) { Fault(ex); }
    }
    private void Fault(Exception ex) { _data.ObserverErrors++; _data.Diagnostic = ex.GetType().Name; }
    private RunAbilitySummary Ability(string weapon, string ability)
    {
        var value = _data.Abilities.Find(x => x.Weapon == weapon && x.Ability == ability);
        if (value != null) return value;
        value = new RunAbilitySummary { Weapon = weapon, Ability = ability }; _data.Abilities.Add(value); return value;
    }
    private void OnAbilityUsed(int index)
    { if (!_disposed && !_data.Finalized && index >= 0 && index < 4) Ability(Family(), "QWER"[index].ToString()).Uses++; }
    private void OnBasic() { if (!_disposed && !_data.Finalized) Ability(Family(), "Basic").Uses++; }
    public void Progress(RunSessionState session)
    {
        if (_disposed || _data.Finalized || session == null || session.Id.ToString() != _data.SessionId) return;
        var last = _data.Progress.LastOrDefault();
        if (last != null && last.Node == session.CurrentNode && last.Phase == session.Phase.ToString()) return;
        _data.Progress.Add(new RunProgressSummary { Node = session.CurrentNode, Phase = session.Phase.ToString(),
            ScaledSeconds = Math.Max(0, Time.timeAsDouble - _scaledStart) });
    }
    public void Reward(RunRewardTicket ticket)
    {
        if (_disposed || _data.Finalized || ticket == null || ticket.State != RunRewardTicketState.Resolved ||
            ticket.Request.SessionId.ToString() != _data.SessionId || !_rewards.Add(ticket.Request.NodeId + ":" + ticket.Request.Ordinal)) return;
        _data.Rewards.Add(new RunRewardSummary { Node = ticket.Request.NodeId, Ordinal = ticket.Request.Ordinal,
            Revision = ticket.Revision, Id = ticket.ResultId, Kind = ticket.AnnouncedKind.ToString(), Alternative = ticket.IsAlternative,
            ScaledSeconds = Math.Max(0, Time.timeAsDouble - _scaledStart) });
        RefreshBuild();
    }
    private void RefreshBuild()
    {
        if (!_player || !_player.TryGetComponent(out RunBoons run)) return;
        _data.Build = run.Acquired.GroupBy(x => x.Id).Select(g => new RunBoonSummary {
            Id = g.Key, Acquisitions = g.Count(), Family = BoonFamilyOf(g.Key) }).OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
    }
    private static string BoonFamilyOf(string id)
    {
        if (ProtocolInventory.TryKind(id, out _)) return "Protocol";
        if (GauntletContentAuthoring.TryMutation(id, out _)) return "Mutation";
        if (id == BoonReworkCatalog.ViolentFallId) return "Augment";
        if (BoonExpansionCatalog.TryDefinition(id, out var definition)) return definition.Family.ToString();
        return "TechniqueOrLegacy";
    }
    public RunSummary Snapshot()
    {
        if (!_data.Finalized)
        {
            _data.WallSeconds = Math.Max(0, Time.realtimeSinceStartupAsDouble - _wallStart);
            _data.ScaledSeconds = Math.Max(0, Time.timeAsDouble - _scaledStart); RefreshBuild();
        }
        _data.HighestDamageAbility = _data.Abilities.Where(x => x.Ability != "Unattributed" && x.EffectiveIncludingChildren > 0)
            .OrderByDescending(x => x.EffectiveIncludingChildren).ThenBy(x => x.Weapon + x.Ability, StringComparer.Ordinal)
            .Select(x => x.Weapon + ":" + x.Ability).FirstOrDefault();
        _data.HighestDamageSource = _data.Damage.Where(x => x.PlayerAttributed)
            .OrderByDescending(x => x.Effective).ThenBy(x => x.Weapon + x.Ability + x.Producer, StringComparer.Ordinal)
            .Select(x => x.Weapon + ":" + x.Ability + ":" + x.Producer).FirstOrDefault();
        return JsonUtility.FromJson<RunSummary>(JsonUtility.ToJson(_data));
    }
    public void Finish(RunSessionState session)
    {
        if (_data.Finalized || session == null || session.Id.ToString() != _data.SessionId || session.Outcome == RunOutcome.None) return;
        Progress(session); Snapshot();
        _data.Outcome = session.Outcome == RunOutcome.Extracted ? "Victory" : session.Outcome == RunOutcome.Dead ? "Death" : "Aborted";
        _data.Finalized = true;
        if (!RunSummaryStore.TryWrite(Snapshot(), out string diagnostic))
        { _data.Diagnostic = "Persistence: " + diagnostic; Debug.LogWarning(_data.Diagnostic); }
        Dispose();
    }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        foreach (var actor in _actors) if (actor) { actor.DamageReceived -= OnDamage; actor.Died -= OnDeath; }
        _actors.Clear();
        if (_holder) _holder.AbilityUsed -= OnAbilityUsed;
        if (_controls) _controls.BasicAttackPerformed -= OnBasic;
        if (_player && ReferenceEquals(_player.RunStatistics, this)) _player.RunStatistics = null;
        _player = null; _holder = null; _controls = null; _current = null;
    }
    private sealed class ActionScope : IDisposable
    {
        private Action _end;
        public ActionScope(Action end) { _end = end; }
        public void Dispose() { var end = _end; _end = null; end?.Invoke(); }
    }
}
