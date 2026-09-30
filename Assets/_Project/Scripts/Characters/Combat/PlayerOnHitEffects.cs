using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-run registry of on-hit status effects the player applies to every enemy they damage.
/// RunBoons toggles/upgrades these. PlayerActor reports each resolved attack exactly once,
/// including hitboxes, areas and projectiles. Secondary cascades apply elements without recursion.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerOnHitEffects : MonoBehaviour
{
    private bool _burnEnabled;
    private float _burnDps;
    private float _burnDuration;

    private bool _chillEnabled;
    private float _chillSlow;
    private float _chillDuration;
    private float _chillChance;
    private RunSynergyEffects _synergies;

    // impactful-weapon-boons R8: Elemental Overflow rank (0 = off). Raised once per pick by RunBoons.Choose.
    // Task 1 wires the enable/teardown seam; the burst trigger (TryElementalOverflow) is added in Task 8.
    private int _overflowRank;

    // Per-run combat-event bus (R8): owned and cleared by RunBoons, which lives on this same
    // GameObject (RunBoons requires PlayerActor/AbilityHolder). Reached through RunBoons ownership
    // via TryGetComponent — the same pattern PlayerActor uses for its cached _runBoons reference —
    // so no FindObjectOfType/GameObject.Find is needed. Cached lazily; null until a run's RunBoons
    // has created its bus, and every raise-site null-guards it.
    private RunBoons _runBoons;
    private HookBus Hooks
    {
        get
        {
            if (!_runBoons) TryGetComponent(out _runBoons);
            return _runBoons ? _runBoons.Hooks : null;
        }
    }

    // Thermal Shock (R2): configured multiple of the triggering hit's damage. Runtime-tunable, never
    // written to a shared asset.
    [SerializeField] private float _thermalShockMultiplier = 1.5f;
    // Re-entrancy guard: cascade-driven ApplyElements calls (from RunSynergyEffects) must not spawn
    // additional shocks (R2.7).
    private bool _resolvingShock;

    // Seedable RNG seam for the chill-chance roll. Defaults to UnityEngine.Random.value so runtime
    // behavior is unchanged; deterministic property tests inject a seeded source via
    // SetChillRollForTests (task 1.1 / R11.5). Kept internal and non-behavior-changing.
    private Func<float> _chillRoll;
    private float ChillRoll() => (_chillRoll ?? UnityRandomValue)();
    private static float UnityRandomValue() => UnityEngine.Random.value;

    /// <summary>
    /// Test-only seam: replaces the chill-chance roll source so RNG-dependent properties are
    /// deterministic. Passing <c>null</c> restores the default <see cref="UnityEngine.Random.value"/>
    /// behavior. Not used by any gameplay code path.
    /// </summary>
    internal void SetChillRollForTests(Func<float> roll) => _chillRoll = roll;

    public float ChillChance => _chillChance;
    public RunSynergyEffects Synergies => _synergies ? _synergies :
        (_synergies = GetComponent<RunSynergyEffects>() ?? gameObject.AddComponent<RunSynergyEffects>());

    public bool HasAnyEffect => _burnEnabled || _chillEnabled || _overflowRank > 0 || (_synergies && _synergies.HasModifiers);

    /// <summary>
    /// impactful-weapon-boons R8: raises the Elemental Overflow rank by one (repeatable pick). Enables the
    /// cross-family burst that spends an enemy's active burn/chill for extra damage. The burst trigger is
    /// implemented in Task 8; this method exists so RunBoons.Choose can wire the reward in Task 1.
    /// </summary>
    public void EnableElementalOverflow() => _overflowRank++;

    /// <summary>Enables/strengthens burn on hit. Repeated calls add damage and extend duration.</summary>
    public void EnableBurn(float damagePerSecond, float duration)
    {
        _burnEnabled = true;
        _burnDps += Mathf.Max(0f, damagePerSecond);
        _burnDuration = Mathf.Max(_burnDuration, duration);
    }

    /// <summary>Enables/strengthens chill on hit. chance 0..1 for the freeze/slow to trigger per hit.</summary>
    public void EnableChill(float slowFraction, float duration, float chance)
    {
        _chillEnabled = true;
        _chillSlow = Mathf.Max(_chillSlow, Mathf.Clamp01(slowFraction));
        _chillDuration = Mathf.Max(_chillDuration, duration);
        _chillChance = Mathf.Clamp01(_chillChance + Mathf.Clamp01(chance));
    }

    /// <summary>Applies every enabled effect to a single damaged enemy.</summary>
    public void ApplyTo(Actor enemy, float damageDealt = 0f)
    {
        if (!enemy || enemy is PlayerActor) return;
        ApplyElements(enemy, damageDealt);
        if (_synergies && damageDealt > 0f) _synergies.Resolve(enemy, damageDealt, this);
        TryElementalOverflow(enemy, damageDealt);
    }

    /// <summary>
    /// impactful-weapon-boons R8: Elemental Overflow. On a Direct_Hit that dealt damage, when the boon is
    /// active (<see cref="_overflowRank"/> &gt; 0) and the enemy already carries an active
    /// <see cref="BurnStatus"/> or <see cref="ChillStatus"/>, spends that status setup for one extra burst of
    /// <c>damageDealt * .5 * rank</c> (R8.1/R8.2). The burst is a single bounded impact routed through
    /// <see cref="RunSynergyEffects.ReportImpact"/> so it is Resonance-eligible and cannot exceed the cascade's
    /// per-frame <c>MaxSecondaryHits</c> budget (R8.3); it falls back to a plain <see cref="Actor.TakeDamage"/>
    /// when Resonance is inactive. The burst is instantaneous damage only — it never removes or refreshes the
    /// underlying status (R8.2). No-op when the enemy carries neither status (R8.4). Composes with Thermal Shock:
    /// a hit can produce both a shock and an overflow burst as two distinct bounded impacts under one budget.
    /// </summary>
    private void TryElementalOverflow(Actor enemy, float damageDealt)
    {
        if (_overflowRank <= 0 || !enemy || enemy.IsDead || damageDealt <= 0f) return;
        bool burning = enemy.GetComponent<BurnStatus>();
        bool frozen = enemy.GetComponent<ChillStatus>();
        if (!burning && !frozen) return;                 // R8.4: no status -> no burst.
        float amount = damageDealt * .5f * _overflowRank; // R8.2

        // R8.3: prefer the Resonance-amplified bounded-impact path (also bounded by MaxSecondaryHits);
        // fall back to a plain bounded hit when Resonance is inactive (ReportImpact is a no-op without it)
        // or no synergies component exists. Neither path touches the burn/chill timers, so the status
        // remains (R8.2).
        if (_synergies && _synergies.Rank(RunSynergy.Resonance) > 0) _synergies.ReportImpact(enemy, amount, this);
        else enemy.TakeDamage(amount);
    }

    /// <summary>Sets the Thermal Shock damage multiple (clamped to be non-negative). See <see cref="ApplyElements"/>.</summary>
    public void ConfigureThermalShock(float multiplier) => _thermalShockMultiplier = Mathf.Max(0f, multiplier);

    public void ApplyElements(Actor enemy, float damageDealt)
    {
        if (!enemy || enemy.IsDead || enemy is PlayerActor) return;

        // Sample the opposite elements as they were *before* this application, so a newly applied
        // element meeting a pre-existing one triggers Thermal Shock (R2.1/R2.2). Component presence is
        // the proxy for "remaining duration > 0": a StatusEffect destroys itself when its duration
        // elapses, so a live component means the effect is still active.
        bool hadBurn = enemy.GetComponent<BurnStatus>();
        bool hadChill = ChillActive(enemy);

        bool appliedFire = false, appliedFrost = false;
        if (_burnEnabled && _burnDps > 0f)
        {
            BurnStatus.Apply(enemy, _burnDps + damageDealt * (_synergies ? _synergies.BurnScaling : 0f), _burnDuration);
            appliedFire = true;
            // R8.4: a burn was applied/refreshed on this enemy.
            Hooks?.RaiseBurn(enemy);
        }

        if (_chillEnabled && _chillSlow > 0f && ChillRoll() <= _chillChance)
        {
            ChillStatus.Apply(enemy, _chillSlow, _chillDuration);
            appliedFrost = true;
            // R8.5: chill applied at or above the freeze threshold is a freeze. _chillSlow is the
            // accumulated slow fraction ChillStatus.Apply just used, so it reflects the frozen state.
            if (_chillSlow >= ChillStatus.FreezeThreshold) Hooks?.RaiseFreeze(enemy);
        }

        // R2.1/R2.2/R2.3/R2.7: fire onto an existing chill, or frost onto an existing burn, each
        // qualifies for exactly one shock (up to two per call when both qualify), and never chains.
        if (_resolvingShock || _thermalShockMultiplier <= 0f || damageDealt <= 0f) return;
        if (appliedFire && hadChill) TriggerThermalShock(enemy, damageDealt * _thermalShockMultiplier);
        if (appliedFrost && hadBurn) TriggerThermalShock(enemy, damageDealt * _thermalShockMultiplier);
    }

    /// <summary>True when the enemy carries a still-active chill (remaining duration &gt; 0).</summary>
    private static bool ChillActive(Actor enemy) => enemy.GetComponent<ChillStatus>();

    /// <summary>
    /// Applies one instantaneous Thermal Shock burst (R2.4) without touching the burn/chill timers
    /// (R2.5), then routes the dealt amount through the Resonance-amplified impact path (R2.6). The
    /// re-entrancy guard keeps cascade-driven <see cref="ApplyElements"/> calls from spawning more shocks.
    /// </summary>
    private void TriggerThermalShock(Actor enemy, float amount)
    {
        if (!enemy || enemy.IsDead || amount <= 0f) return;

        _resolvingShock = true;
        try
        {
            float before = enemy.health;
            enemy.TakeDamage(amount);            // R2.4 instantaneous; does not remove or refresh DoTs
            float dealt = before - enemy.health; // R2.5 both statuses remain with unchanged durations
            if (dealt > 0f && _synergies)
                _synergies.ReportImpact(enemy, dealt, this); // R2.6 Resonance-eligible impact path
        }
        finally { _resolvingShock = false; }
    }

    /// <summary>Applies every enabled effect to a batch of damaged enemies.</summary>
    public void ApplyTo(IReadOnlyList<Actor> enemies)
    {
        if (enemies == null) return;
        for (int i = 0; i < enemies.Count; i++) ApplyTo(enemies[i]);
    }

    /// <summary>Clears all on-hit effects. Called at the end of a run so modifiers don't leak into the next.</summary>
    public void Clear()
    {
        _burnEnabled = false; _burnDps = 0f; _burnDuration = 0f;
        _chillEnabled = false; _chillSlow = 0f; _chillDuration = 0f; _chillChance = 0f;
        _overflowRank = 0; // impactful-weapon-boons R8: reset overflow so it never leaks into a later run.
        if (_synergies) _synergies.Clear();
    }
}
