using System;

/// <summary>
/// The intent of a weapon's response to an enemy archetype — <b>which kind of tool</b> the matrix
/// points at, expressed as data rather than as a per weapon-enemy branch in gameplay code (R11.6).
/// A response never encodes numbers (damage/timing live on the ability/<c>AreaHitStep</c> assets); it
/// only names the intended answer so the matrix stays a readable lookup table.
/// </summary>
/// <remarks>Feature: weapon-gameplay-swarm-rework, task 16.1. Requirements: 11.1–11.7.</remarks>
public enum WeaponResponseKind
{
    /// <summary>The weapon's ordinary pressure answer for the archetype's <see cref="CombatRole"/>.</summary>
    GeneralPressure,

    /// <summary>Heavy/Breaker stance tool meant to break a Heavy's posture (Manopla E / Lança E / Arco W). R11.2.</summary>
    AntiHeavyBreaker,

    /// <summary>Thrust counter that beats a Charger's commit (Lança Q). R11.3.</summary>
    CounterCharger,

    /// <summary>Reach/priority answer against a priority target — Mark (Arco) or advance/thrust (Manopla/Lança). R11.4.</summary>
    PriorityReach,

    /// <summary>Area clear against Swarm — sweep (Lança W) / ring impact (Manopla E) / cone-rain (Arco E-R). R11.5.</summary>
    SwarmClear
}

/// <summary>
/// A single resolved response from the Weapon × Enemy matrix: which ability slot is the intended
/// tool (0..3 = Q/W/E/R), what kind of answer it is, and whether it came from an authored matrix
/// entry (<see cref="FromMatrix"/> true) or the <see cref="CombatRole"/> fallback (false, meaning the
/// caller should log missing coverage, R11.7). This is a pure value; it carries no scene state.
/// </summary>
/// <remarks>Feature: weapon-gameplay-swarm-rework, task 16.1. Requirements: 11.1, 11.6, 11.7.</remarks>
public readonly struct WeaponResponse : IEquatable<WeaponResponse>
{
    /// <summary>Q/W/E/R slot index this weapon should reach for against the archetype (0..3).</summary>
    public readonly int Slot;

    /// <summary>The intended kind of answer (anti-Heavy, counter-Charger, priority, swarm clear, ...).</summary>
    public readonly WeaponResponseKind Kind;

    /// <summary>True when the value came from an authored matrix entry; false when it is the role fallback (R11.7).</summary>
    public readonly bool FromMatrix;

    public WeaponResponse(int slot, WeaponResponseKind kind, bool fromMatrix)
    {
        // Slot is clamped to the fixed four-ability kit (R13.1) so a mis-authored entry can never
        // point outside Q/W/E/R.
        Slot = slot < 0 ? 0 : slot > 3 ? 3 : slot;
        Kind = kind;
        FromMatrix = fromMatrix;
    }

    public bool Equals(WeaponResponse other) =>
        Slot == other.Slot && Kind == other.Kind && FromMatrix == other.FromMatrix;

    public override bool Equals(object obj) => obj is WeaponResponse other && Equals(other);

    public override int GetHashCode() => (Slot * 397 ^ (int)Kind) * 397 ^ (FromMatrix ? 1 : 0);

    public override string ToString() =>
        $"{Kind} @slot {Slot}" + (FromMatrix ? "" : " (role fallback)");
}

/// <summary>
/// Pure, scene-free resolution of the Weapon × Enemy matrix. Given a set of authored
/// (<see cref="ArchetypeId"/> × <see cref="RunWeaponFamily"/>) entries, it returns the mapped
/// <see cref="WeaponResponse"/>, or — when a pair has no entry — the weapon's <b>default response for
/// the archetype's <see cref="CombatRole"/></b> with <see cref="WeaponResponse.FromMatrix"/> = false
/// so the caller logs missing coverage (R11.7).
/// <para>
/// The whole point of extracting this from <see cref="WeaponEnemyMatrix"/> is that the fallback table
/// and the "every pair resolves to exactly one response" contract can be property-tested without a
/// live Unity scene, and that gameplay code never grows a per weapon-enemy <c>switch</c> (R11.6): the
/// only switch here is a per-<see cref="RunWeaponFamily"/>×<see cref="CombatRole"/> <b>default</b>, not
/// a pair-specific behavior branch.
/// </para>
/// </summary>
/// <remarks>Feature: weapon-gameplay-swarm-rework, task 16.1. Requirements: 11.1, 11.6, 11.7.</remarks>
public static class WeaponEnemyResponseResolver
{
    /// <summary>Q slot index (advance/thrust/rapid-burst depending on weapon).</summary>
    public const int SlotQ = 0;
    /// <summary>W slot index (flurry/sweep/heavy-arrow).</summary>
    public const int SlotW = 1;
    /// <summary>E slot index (shock-ring/heavy-thrust/wide-volley).</summary>
    public const int SlotE = 2;
    /// <summary>R slot index (asura/dragon-wave/arrow-rain).</summary>
    public const int SlotR = 3;

    /// <summary>
    /// A lookup key for one authored matrix cell. Kept as a small value type so the SO can serialize a
    /// flat entry array and the resolver can compare pairs without allocating.
    /// </summary>
    public readonly struct Key : IEquatable<Key>
    {
        public readonly ArchetypeId Archetype;
        public readonly RunWeaponFamily Family;

        public Key(ArchetypeId archetype, RunWeaponFamily family)
        {
            Archetype = archetype;
            Family = family;
        }

        public bool Equals(Key other) => Archetype == other.Archetype && Family == other.Family;
        public override bool Equals(object obj) => obj is Key other && Equals(other);
        public override int GetHashCode() => (int)Archetype * 397 ^ (int)Family;
    }

    /// <summary>
    /// The interface the SO (or a test double) exposes to the resolver: a pure lookup of an authored
    /// pair. Returning false means "no authored entry" and triggers the role fallback (R11.7).
    /// </summary>
    public interface IEntryLookup
    {
        bool TryGet(ArchetypeId archetype, RunWeaponFamily family, out WeaponResponse response);
    }

    /// <summary>
    /// Resolves the response for a (archetype, family) pair. Prefers an authored entry from
    /// <paramref name="lookup"/>; otherwise returns the <see cref="RunWeaponFamily"/> × role default
    /// with <see cref="WeaponResponse.FromMatrix"/> = false. The result is never undefined: every pair
    /// resolves to exactly one response (R11.1), and the fallback path is what the caller logs (R11.7).
    /// </summary>
    /// <param name="lookup">The authored entries, or null when no matrix is assigned.</param>
    /// <param name="archetype">The enemy's archetype id.</param>
    /// <param name="family">The equipped weapon family (Gauntlet / Bow / Spear).</param>
    /// <param name="role">The archetype's primary combat role, used for the fallback.</param>
    public static WeaponResponse Resolve(IEntryLookup lookup, ArchetypeId archetype, RunWeaponFamily family, CombatRole role)
    {
        if (lookup != null && lookup.TryGet(archetype, family, out WeaponResponse authored))
        {
            // Re-stamp FromMatrix true so the fallback flag can't be forged by authored data.
            return new WeaponResponse(authored.Slot, authored.Kind, fromMatrix: true);
        }

        return RoleDefault(family, role);
    }

    /// <summary>
    /// The weapon's default response for a <see cref="CombatRole"/> when no pair-specific entry exists
    /// (R11.7). This is deliberately keyed on the <i>role</i>, not on the archetype, so it stays a
    /// small, weapon-shaped table rather than a per-enemy branch. Every branch returns a valid slot,
    /// and <see cref="WeaponResponse.FromMatrix"/> is always false so the caller logs missing coverage.
    /// </summary>
    public static WeaponResponse RoleDefault(RunWeaponFamily family, CombatRole role)
    {
        switch (family)
        {
            case RunWeaponFamily.Gauntlet:
                return GauntletDefault(role);
            case RunWeaponFamily.Bow:
                return BowDefault(role);
            default:
                return SpearDefault(role);
        }
    }

    // Manoplas: close-range breaker kit. E is the ring/anti-Heavy tool; Q is the advance for reach;
    // W flurry is the swarm answer; R (Asura) is the general finisher otherwise.
    private static WeaponResponse GauntletDefault(CombatRole role)
    {
        switch (role)
        {
            case CombatRole.PlayerDisplacement: // Heavy-shaped bruisers
                return new WeaponResponse(SlotE, WeaponResponseKind.AntiHeavyBreaker, fromMatrix: false);
            case CombatRole.SwarmFuel:
            case CombatRole.ComboFodder:
                return new WeaponResponse(SlotE, WeaponResponseKind.SwarmClear, fromMatrix: false);
            case CombatRole.AllySupport:
            case CombatRole.PriorityThreat:
                return new WeaponResponse(SlotQ, WeaponResponseKind.PriorityReach, fromMatrix: false);
            default:
                return new WeaponResponse(SlotR, WeaponResponseKind.GeneralPressure, fromMatrix: false);
        }
    }

    // Lança: reach and spacing. Q thrust counters commits; W sweep clears swarms; E heavy thrust
    // breaks Heavy; general pressure otherwise.
    private static WeaponResponse SpearDefault(CombatRole role)
    {
        switch (role)
        {
            case CombatRole.PlayerDisplacement: // Heavy-shaped bruisers
                return new WeaponResponse(SlotE, WeaponResponseKind.AntiHeavyBreaker, fromMatrix: false);
            case CombatRole.SwarmFuel:
            case CombatRole.ComboFodder:
                return new WeaponResponse(SlotW, WeaponResponseKind.SwarmClear, fromMatrix: false);
            case CombatRole.AllySupport:
            case CombatRole.PriorityThreat:
                return new WeaponResponse(SlotQ, WeaponResponseKind.PriorityReach, fromMatrix: false);
            default:
                return new WeaponResponse(SlotQ, WeaponResponseKind.GeneralPressure, fromMatrix: false);
        }
    }

    // Arco: ranged priority and area. W heavy arrow breaks Heavy; E/R cone-rain clears swarms; the
    // Mark (priority reach) handles priority targets; general pressure otherwise.
    private static WeaponResponse BowDefault(CombatRole role)
    {
        switch (role)
        {
            case CombatRole.PlayerDisplacement: // Heavy-shaped bruisers
                return new WeaponResponse(SlotW, WeaponResponseKind.AntiHeavyBreaker, fromMatrix: false);
            case CombatRole.SwarmFuel:
            case CombatRole.ComboFodder:
                return new WeaponResponse(SlotE, WeaponResponseKind.SwarmClear, fromMatrix: false);
            case CombatRole.AllySupport:
            case CombatRole.PriorityThreat:
                return new WeaponResponse(SlotQ, WeaponResponseKind.PriorityReach, fromMatrix: false);
            default:
                return new WeaponResponse(SlotQ, WeaponResponseKind.GeneralPressure, fromMatrix: false);
        }
    }
}
