using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Primitive provenance only. Never retains an Actor, ability asset or mutable run inventory.</summary>
public sealed class RunDamageSource
{
    public string SessionId { get; }
    public string Weapon { get; }
    public string Ability { get; }
    public string Execution { get; }
    public string MutationId { get; }
    public string Producer { get; }
    public string ProducerId { get; }
    public bool Secondary { get; }
    public string Attribution { get; }
    public bool ParentsTruncated { get; }
    public IReadOnlyList<string> Parents { get; }
    public IReadOnlyList<string> Modifiers { get; }
    public RunDamageSource(string session, string weapon, string ability, string execution,
        string mutation = "", string producer = "Direct", string producerId = "", bool secondary = false,
        string attribution = "confirmed", IEnumerable<string> parents = null, IEnumerable<string> modifiers = null,
        bool parentsTruncated = false)
    {
        SessionId = session; Weapon = weapon ?? "Unknown"; Ability = ability ?? "Unattributed";
        Execution = execution ?? "unavailable"; MutationId = mutation ?? "";
        Producer = producer; ProducerId = producerId ?? ""; Secondary = secondary;
        Attribution = attribution; ParentsTruncated = parentsTruncated;
        Parents = Array.AsReadOnly((parents ?? new[] { Execution }).Distinct().Take(8).ToArray());
        Modifiers = Array.AsReadOnly((modifiers ?? Array.Empty<string>()).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray());
    }
    public RunDamageSource WithProducer(string producer, string id, bool secondary) => new RunDamageSource(
        SessionId, Weapon, Ability, Execution, MutationId, producer, id, secondary, Attribution, Parents, Modifiers, ParentsTruncated);
    public RunDamageSource WithModifier(string id) => new RunDamageSource(SessionId, Weapon, Ability, Execution,
        MutationId, Producer, ProducerId, Secondary, Attribution, Parents, Modifiers.Concat(new[] { id }), ParentsTruncated);
    public static RunDamageSource MergeBurn(RunDamageSource previous, RunDamageSource incoming)
    {
        if (previous == null) return incoming;
        if (incoming == null) return new RunDamageSource("", "Unknown", "Unattributed", "mixed",
            attribution: "mixed_burn_origins", parents: previous.Parents, parentsTruncated: true);
        if (previous.SessionId == incoming.SessionId && previous.Execution == incoming.Execution && previous.Attribution == incoming.Attribution) return previous;
        var parents = previous.Parents.Concat(incoming.Parents).Distinct().ToArray();
        bool same = previous.SessionId == incoming.SessionId && previous.Weapon == incoming.Weapon &&
            previous.Ability == incoming.Ability && previous.MutationId == incoming.MutationId &&
            previous.Attribution != "mixed_burn_origins" && incoming.Attribution != "mixed_burn_origins";
        return new RunDamageSource(previous.SessionId == incoming.SessionId ? incoming.SessionId : "", same ? incoming.Weapon : "Unknown",
            same ? incoming.Ability : "Unattributed", "mixed", same ? incoming.MutationId : "",
            attribution: same ? "multiple_executions" : "mixed_burn_origins", parents: parents,
            parentsTruncated: parents.Length > 8 || previous.ParentsTruncated || incoming.ParentsTruncated);
    }
}

/// <summary>The final HP gate, before notifications. Attempt is after mitigation/absorption, not raw attack damage.</summary>
public readonly struct HpDamageTransaction
{
    public long Sequence { get; }
    public string TargetId { get; }
    public long Generation { get; }
    public float Before { get; }
    public float After { get; }
    public float Effective => Before - After;
    public float HpAttempt { get; }
    public bool OverkillKnown => !float.IsNaN(HpAttempt) && !float.IsInfinity(HpAttempt) && HpAttempt >= 0;
    public float Overkill => OverkillKnown ? Math.Max(0, HpAttempt - Before) : 0;
    public RunDamageSource Source { get; }
    public HpDamageTransaction(long sequence, string target, long generation, float before, float after, float hpAttempt, RunDamageSource source)
    { Sequence = sequence; TargetId = target; Generation = generation; Before = before; After = after; HpAttempt = hpAttempt; Source = source; }
}
