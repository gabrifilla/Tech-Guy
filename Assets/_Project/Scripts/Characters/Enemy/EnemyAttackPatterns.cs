using System;
using UnityEngine;

[Flags]
public enum EnemyAttackTraits { None = 0, Haste = 1, Frost = 2, Guard = 4 }
public enum EnemyAttackKind { Punch, DoublePunch, HeavySlam, Charge, FrostBolt, Shockwave, AimedShot, SpreadShot, SniperShot, LobShot, HookLine, HazardPlace }

/// <summary>Repertoire depends on distance and anatomy, rather than a rotation of ground shapes.</summary>
/// <remarks>
/// Feature: enemy-swarm-core-archetypes. Requirements: 1.2, 1.3, 7.2, 9.2.
/// <para>
/// The <see cref="ArchetypeId"/> overloads narrow the repertoire to a single attack shape for the
/// attack-shaped ranged/control archetypes (Shooter, Spread_Shooter, Sniper, Bomber, Hooker,
/// Hazard_Caster) while distance/traits still pick the concrete melee kind for the melee archetypes.
/// The trait-only overloads preserve the pre-archetype behavior and remain the default so existing
/// callers (e.g. <c>EnemyAI</c>) keep compiling and behaving identically.
/// </para>
/// </remarks>
public static class EnemyAttackPatterns
{
    /// <summary>Fallback melee engagement band (meters) used by trait-only selection and melee archetypes.</summary>
    public const float MeleeEngagementRange = 2.5f;

    /// <summary>Medium (Shooter-class) engagement band in meters.</summary>
    public const float MediumEngagementRange = 12f;

    /// <summary>Long (Sniper-class) engagement band in meters, strictly greater than the medium band (R9.2).</summary>
    public const float LongEngagementRange = 22f;

    /// <summary>Existing trait-driven engagement range. Preserved verbatim for backward compatibility.</summary>
    public static float EngagementRange(EnemyAttackTraits traits, int sequence, float meleeRange) =>
        (traits & EnemyAttackTraits.Frost) != 0 ? 9f :
        (traits & EnemyAttackTraits.Haste) != 0 ? 7f : Mathf.Min(meleeRange, 2.5f);

    /// <summary>
    /// Archetype-scoped engagement range. Ranged/control archetypes open the attack at their own band
    /// (long for Sniper, medium for the other ranged/control shapes); melee archetypes and any archetype
    /// without a dedicated band fall back to the trait-driven <see cref="EngagementRange(EnemyAttackTraits,int,float)"/>.
    /// </summary>
    public static float EngagementRange(EnemyAttackTraits traits, int sequence, float meleeRange, ArchetypeId? archetype)
    {
        if (archetype is ArchetypeId id)
        {
            switch (id)
            {
                case ArchetypeId.Sniper:
                    return LongEngagementRange;
                case ArchetypeId.Shooter:
                case ArchetypeId.SpreadShooter:
                case ArchetypeId.Bomber:
                case ArchetypeId.HazardCaster:
                case ArchetypeId.Hooker:
                    return MediumEngagementRange;
                case ArchetypeId.Rush:
                case ArchetypeId.Grunt:
                case ArchetypeId.Heavy:
                case ArchetypeId.Charger:
                    return Mathf.Min(meleeRange, MeleeEngagementRange);
            }
        }
        return EngagementRange(traits, sequence, meleeRange);
    }

    /// <summary>Existing trait-driven selection. Preserved verbatim for backward compatibility.</summary>
    public static EnemyAttackKind Select(EnemyAttackTraits traits, int sequence, float distance)
    {
        bool frost = (traits & EnemyAttackTraits.Frost) != 0;
        bool haste = (traits & EnemyAttackTraits.Haste) != 0;
        if (frost && distance > 3.2f && (!haste || distance > 7 || sequence % 2 == 0)) return EnemyAttackKind.FrostBolt;
        if (haste && distance > 3.2f) return EnemyAttackKind.Charge;
        if ((traits & EnemyAttackTraits.Guard) != 0 && sequence % 2 == 1) return EnemyAttackKind.HeavySlam;
        if (haste && sequence % 2 == 0) return EnemyAttackKind.DoublePunch;
        return EnemyAttackKind.Punch;
    }

    /// <summary>
    /// Archetype-scoped selection. The attack-shaped ranged/control archetypes narrow the repertoire to
    /// their single kind (Sniper → <see cref="EnemyAttackKind.SniperShot"/>, Shooter → AimedShot,
    /// Spread_Shooter → SpreadShot, Bomber → LobShot, Hooker → HookLine, Hazard_Caster → HazardPlace).
    /// The melee archetypes narrow the repertoire to their signature shape:
    /// <list type="bullet">
    /// <item>Rush and Grunt use <see cref="EnemyAttackKind.Punch"/> (the concrete windup differs per
    /// archetype via <see cref="MeleeWindup"/>: Rush &lt; Grunt).</item>
    /// <item>Heavy alternates a <see cref="EnemyAttackKind.HeavySlam"/> and a ground-pound
    /// <see cref="EnemyAttackKind.Shockwave"/> (slam on even beats, ground-pound on odd) so a single
    /// engagement reads as "slam then ground-pound" (R5.3).</item>
    /// <item>Charger commits to a locked-direction <see cref="EnemyAttackKind.Charge"/> (R6.3).</item>
    /// </list>
    /// Any archetype without a dedicated repertoire defers to the trait/distance selection so distance
    /// and traits still choose the concrete melee kind for existing (non-archetype) enemies.
    /// </summary>
    public static EnemyAttackKind Select(EnemyAttackTraits traits, int sequence, float distance, ArchetypeId? archetype)
    {
        if (archetype is ArchetypeId id)
        {
            switch (id)
            {
                case ArchetypeId.Shooter:
                    return EnemyAttackKind.AimedShot;
                case ArchetypeId.SpreadShooter:
                    return EnemyAttackKind.SpreadShot;
                case ArchetypeId.Sniper:
                    return EnemyAttackKind.SniperShot;
                case ArchetypeId.Bomber:
                    return EnemyAttackKind.LobShot;
                case ArchetypeId.Hooker:
                    return EnemyAttackKind.HookLine;
                case ArchetypeId.HazardCaster:
                    return EnemyAttackKind.HazardPlace;
                case ArchetypeId.Rush:
                case ArchetypeId.Grunt:
                    // Baseline melee jab. Rush/Grunt differ by windup (MeleeWindup), not by kind.
                    return EnemyAttackKind.Punch;
                case ArchetypeId.Heavy:
                    // Slam then ground-pound: alternate the two beats so the engagement reads as both
                    // a heavy slam and its follow-up ground-pound area attack (R5.3).
                    return sequence % 2 == 0 ? EnemyAttackKind.HeavySlam : EnemyAttackKind.Shockwave;
                case ArchetypeId.Charger:
                    // Locked-direction committed dash (R6.2/R6.3).
                    return EnemyAttackKind.Charge;
            }
        }
        return Select(traits, sequence, distance);
    }

    /// <summary>Grunt baseline melee telegraph floor in seconds. The Grunt's melee windup is at least
    /// this long (R4.3), Rush is shorter than this (R3.4), and Heavy's ground-area windup is at least
    /// this long as well (R5.4).</summary>
    public const float GruntMeleeWindup = 0.4f;

    /// <summary>Rush melee telegraph in seconds. Strictly shorter than <see cref="GruntMeleeWindup"/>
    /// so a Rush winds up faster than a Grunt (R3.4) while still clearing the 0.25s global floor (R2.1).</summary>
    public const float RushMeleeWindup = 0.3f;

    /// <summary>
    /// Archetype-scoped melee windup (telegraph duration in seconds) for the melee <see cref="Punch"/>
    /// beat, enforcing the ordering Rush &lt; Grunt (R3.4/R4.3). Returns a non-positive sentinel
    /// (<c>-1</c>) for any archetype that does not override the per-attack default, so callers keep the
    /// attack's own authored windup (e.g. Heavy's slam/ground-pound and Charger's charge own their
    /// telegraph timing inside <see cref="EnemyCombatActions"/>). This lets a single melee windup
    /// override flow from <c>EnemyAI</c> into <c>Perform</c> without disturbing non-melee shapes.
    /// </summary>
    /// <returns>The windup in seconds for Rush/Grunt Punch, or a negative sentinel to keep the default.</returns>
    public static float MeleeWindup(ArchetypeId? archetype)
    {
        if (archetype is ArchetypeId id)
        {
            switch (id)
            {
                case ArchetypeId.Rush:
                    return RushMeleeWindup;   // < Grunt (R3.4)
                case ArchetypeId.Grunt:
                    return GruntMeleeWindup;  // >= 0.4s baseline (R4.3)
            }
        }
        return -1f; // No override: keep the attack's own authored windup.
    }
}
