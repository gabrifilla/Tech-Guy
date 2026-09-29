using System;
using UnityEngine;

[Flags]
public enum EnemyAttackTraits { None = 0, Haste = 1, Frost = 2, Guard = 4 }
public enum EnemyAttackKind { Punch, DoublePunch, HeavySlam, Charge, FrostBolt, Shockwave }

/// <summary>Repertoire depends on distance and anatomy, rather than a rotation of ground shapes.</summary>
public static class EnemyAttackPatterns
{
    public static float EngagementRange(EnemyAttackTraits traits, int sequence, float meleeRange) =>
        (traits & EnemyAttackTraits.Frost) != 0 ? 9f :
        (traits & EnemyAttackTraits.Haste) != 0 ? 7f : Mathf.Min(meleeRange, 2.5f);

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
}
