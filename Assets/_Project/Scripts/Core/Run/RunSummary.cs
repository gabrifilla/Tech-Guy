using System;
using System.Collections.Generic;

[Serializable]
public sealed class RunSummary
{
    public int SchemaVersion = 1;
    public string SessionId, Seed, InitialWeapon, StartedUtc, Outcome = "InProgress";
    public bool Finalized;
    public double WallSeconds, ScaledSeconds;
    public double EffectiveDamage, ObservedEnemyHpLoss, UnattributedEnemyHpLoss, DamageReceived, Overkill, LargestImpact;
    public bool OverkillComplete = true;
    public int EnemiesDefeated, PlayerCreditedKills, ConfirmedHpTransactions, DuplicateReceiptsIgnored, ObserverErrors;
    public string Diagnostic, HighestDamageAbility, HighestDamageSource;
    public string DurationPolicy = "Start of manual session to claimed outcome; wall includes pause; scaled follows Time.timeAsDouble. No DPS.";
    public List<RunDamageBucket> Damage = new List<RunDamageBucket>();
    public List<RunAbilitySummary> Abilities = new List<RunAbilitySummary>();
    public List<RunRewardSummary> Rewards = new List<RunRewardSummary>();
    public List<RunBoonSummary> Build = new List<RunBoonSummary>();
    public List<RunProgressSummary> Progress = new List<RunProgressSummary>();
    public List<RunDamageSample> RecentDamage = new List<RunDamageSample>();
}
[Serializable] public sealed class RunDamageBucket
{
    public string Weapon, Ability, MutationId, Producer, ProducerId, Attribution, ModifierTags;
    public bool PlayerAttributed, Secondary;
    public double Effective, Overkill;
    public int HpTransactions;
}
[Serializable] public sealed class RunAbilitySummary
{
    public string Weapon, Ability;
    public int Uses, NativeHpTransactions, SecondaryHpTransactions;
    public double EffectiveIncludingChildren;
}
[Serializable] public sealed class RunRewardSummary
{ public int Node, Ordinal, Revision; public string Id, Kind; public bool Alternative; public double ScaledSeconds; }
[Serializable] public sealed class RunBoonSummary
{ public string Id, Family; public int Acquisitions; }
[Serializable] public sealed class RunProgressSummary
{ public int Node; public string Phase; public double ScaledSeconds; }
[Serializable] public sealed class RunDamageSample
{
    public string TargetId, Weapon, Ability, Execution, MutationId, Producer, ProducerId, Attribution;
    public string[] Parents;
    public long Generation, Transaction;
    public int Frame;
    public bool PlayerAttributed, Secondary, ParentsTruncated, OverkillKnown;
    public float Before, After, Effective, HpAttempt, Overkill;
}
