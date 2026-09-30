# Design Document

## Overview

This feature adds a set of **impactful, playstyle-changing weapon boons** to Tech-Guy's per-run reward system. The goal is fun over numbers: where today many boons are flat stat nudges (movement speed, cooldown) or small geometry tweaks (Q advance, ability range), each new boon reshapes how a weapon family feels to play.

The design deliberately reuses the existing runtime-copy isolation (`RunBoons` instantiates copies of the weapon and abilities), the per-cast snapshot builders (`WeaponRunModifiers.Plan`/`GauntletSteps`), the bounded cascade engine (`RunSynergyEffects`, `MaxDepth = 4`, `MaxSecondaryHits = 32`), and the per-run event bus (`HookBus`). No new architecture is introduced; every boon plugs into an existing seam.

No numeric rebalancing is in scope. The values below are starting points and may be tuned during implementation.

### Design goals

- Each new boon changes what an ability *does* or how the family *plays*, not just a number (R1).
- Every new effect stays inside the existing cascade bounds and runtime-copy isolation (R1, R8).
- Weapon-family boons flow through the `WeaponRunModifiers.Catalog` + `Plan`/`GauntletSteps` snapshot path and are offered automatically via the family gate in `RunBoons.OfferReward`.
- Event-reactive boons subscribe to `HookBus` and rely on `HookBus.Clear()` for teardown.
- Keep `MonoBehaviour`s thin; put decision logic in plain C# classes (per AGENTS.md).
- Never mutate an original weapon/ability/status `ScriptableObject`; never touch `.meta`/GUIDs.

## Architecture

### System context

The run is owned by `RunBoons` on the player GameObject. It already instantiates runtime copies of the weapon and abilities, owns the `WeaponRunModifiers` rank table and the `HookBus`, and cleans up on `OnDestroy`. This feature adds seven new boons spread across the three weapon families plus one cross-family boon, and one plain-C# state holder (`MomentumStacks`).

```mermaid
graph TD
    subgraph Player GameObject
        RB[RunBoons<br/>offer + apply + teardown]
        WRM[WeaponRunModifiers<br/>ranks + cast plans]
        OHE[PlayerOnHitEffects<br/>element registry]
        RSE[RunSynergyEffects<br/>bounded cascades]
        PA[PlayerActor<br/>damage pipeline]
        CC[CharControlScript<br/>basic attacks]
        AH[AbilityHolder<br/>Q/W/E/R slots]
        HB[HookBus<br/>per-run event bus]
        MS[MomentumStacks<br/>NEW plain-C# state]
    end

    AC[ArsenalCombat<br/>cast execution]
    ARP[ArsenalProjectile<br/>arrow travel]
    GL[GauntletLoopSteps]<br/>step config

    RB -->|creates/owns| WRM
    RB -->|creates/owns| HB
    RB -->|creates/owns| MS
    RB -->|toggles| OHE
    AC -->|reads Plan| WRM
    AC -->|fires| ARP
    ARP -->|OnKill -> split| HB
    CC -->|Charged Shot| ARP
    PA -->|OnHit -> stack| MS
    PA -->|TakeDamage -> reset| MS
    MS -->|stat mod| PA
    HB -..subscribers..-> RB
```

### Where each boon plugs in

| Boon | Family | Seam reused | New code |
| --- | --- | --- | --- |
| Split Arrow (R2) | Bow | `HookBus.OnKill` + `ArsenalProjectile.Fire` | split generation guard |
| Charged Shot (R3) | Bow | `CharControlScript` basic attack + `ArsenalProjectile.Fire` | charge timer + HUD indicator |
| Perfect Spacing (R4) | Spear | `WeaponRunModifiers.DirectDamageMultiplier` | one distance-band term |
| Impaling Line (R5) | Spear | `ArsenalCastPlan` thrust + `SoftGroupingService` pull | plan flag + pull coroutine |
| Momentum Stacks (R6) | Gauntlet | `HookBus` + `PlayerArpgStats.AddModifier` | MomentumStacks holder |
| Shockwave Finisher (R7) | Gauntlet | `GauntletSteps` + `AreaHitStep` clone | appended sphere step |
| Elemental Overflow (R8) | Any | `PlayerOnHitEffects` + `RunSynergyEffects.ReportImpact` | overflow trigger |


## Components and Interfaces

All signatures below are grounded in the current code. New members are marked `// NEW`; changed members note the exact edit. Every new `WeaponBoon` value is added to the enum and the `Catalog` in `WeaponRunModifiers.cs` (one family each, `MaxRank = 3` unless noted), so it auto-appears in `RunBoons.OfferReward` via the existing family/rank gate.

### Catalog additions (`WeaponRunModifiers.cs`)

```csharp
// NEW enum values appended to WeaponBoon
enum WeaponBoon { /* ...existing... */
    SplitArrow, ChargedShot,          // Bow (R2, R3)
    PerfectSpacing, ImpalingLine,     // Spear (R4, R5)
    Momentum, Shockwave,              // Gauntlet (R6, R7) - note: "Momentum" already exists; this boon uses a distinct name MomentumStrike
    ElementalOverflow                 // Cross-family (R8)
}
```

> Note: the Gauntlet catalog already has a `Momentum` boon (Asura energy). The new aggression-stack boon is named `MomentumStrike` to avoid collision. The new area-burst finisher is `Shockwave` (distinct from the existing `ShockRing` E-slot boon).

Each gets a `Definition` row with a Portuguese title/description, following the existing catalog style.

### R2 — Split Arrow (bow, on kill)

Subscribes to `HookBus.OnKill` when the boon is chosen. The kill handler must know the killing arrow's position and damage. Since `OnKill` only carries the `Actor`, the split is spawned from the dying enemy's position using the bow's base damage, which keeps the handler self-contained and avoids threading projectile state through the bus.

```csharp
// NEW: SplitArrowCoordinator (plain MonoBehaviour component added by RunBoons when chosen)
// Guards: only one generation of splits (R2.3), bounded by a per-frame budget (R2.4).
public sealed class SplitArrowCoordinator : MonoBehaviour
{
    private PlayerActor _owner;
    private int _rank;
    private bool _spawning;               // re-entrancy guard (R2.3)
    private int _spawnedThisFrame;
    private int _frame;

    public void Configure(PlayerActor owner, HookBus hooks, int rank)
    {
        _owner = owner; _rank = rank;
        hooks.OnKill += OnKill;           // cleared by HookBus.Clear() at run end
    }

    private void OnKill(Actor victim)
    {
        if (_spawning || !victim || !_owner || _owner.IsDead) return;     // R2.3 no chain
        if (!_owner.CurrentWeapon || !_owner.CurrentWeapon.FiresArrows) return;
        if (Time.frameCount != _frame) { _frame = Time.frameCount; _spawnedThisFrame = 0; }
        if (_spawnedThisFrame + _rank > 32) return;                        // R2.4 bounded by cascade budget

        _spawning = true;
        try
        {
            Vector3 origin = victim.transform.position + Vector3.up;
            float damage = _owner.CurrentWeapon.attackDamage;
            for (int i = 0; i < _rank; i++)
            {
                float angle = (i - (_rank - 1) * .5f) * 40f; // distinct headings (R2.1)
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * RandomFlatDir();
                // R2.2: 50% of the killing arrow's damage, carried as the Fire multiplier.
                ArsenalProjectile.Fire(_owner, origin, dir, damage, .5f, 8f, false, Color.cyan);
                _spawnedThisFrame++;
            }
        }
        finally { _spawning = false; }
    }
}
```

Key decisions:
- **One generation only (R2.3)**: the `_spawning` guard is set while spawning splits. A split arrow that kills raises `OnKill` re-entrantly, but the guard makes that call a no-op. Because split arrows are fired with a fixed `.5f` multiplier (not re-reading a per-arrow "is split" flag), they cannot compound.
- **Bounded (R2.4)**: `_spawnedThisFrame` caps split spawns per frame at the same `32` ceiling the cascade engine uses, so a mass kill cannot explode the projectile count.
- **Subscription lifecycle (R1.4)**: `HookBus.Clear()` at run end drops the `OnKill` handler, so no split arrow fires in a later run.

### R3 — Charged Shot (bow, hold-to-overpower)

Basic bow attacks flow through `CharControlScript` (`TryBasicAttack`/`TryDirectionalBasicAttack` -> `PerformAttack`). Charged Shot adds a hold-timer gate in `CharControlScript` that, while the boon is active and the weapon fires arrows, tracks how long the primary attack input has been held and, on release, chooses between a normal and a charged arrow.

The charge decision is a pure helper so it can be tested scene-free:

```csharp
// NEW: pure decision, no Unity types
public static class ChargedShot
{
    public const float ChargeTime = .6f; // R3.1
    public static bool IsCharged(float heldSeconds) => heldSeconds >= ChargeTime; // R3.1/R3.3
    public static float DamageMultiplier(int rank) => 1f + .75f * rank;         // R3.2
}
```

In `CharControlScript`, when a bow basic resolves into an arrow and the boon is active, the held duration decides the shot:

```csharp
// when firing a bow basic arrow (CharControlScript)
int rank = playerActor.RunModifiers?.Rank(WeaponBoon.ChargedShot) ?? 0;
bool charged = rank > 0 && ChargedShot.IsCharged(_primaryHeldSeconds);  // R3.1/R3.3
float multiplier = charged ? ChargedShot.DamageMultiplier(rank) : 1f;    // R3.2
ArsenalProjectile.Fire(playerActor, origin, aim, weapon.attackDamage,
    multiplier, weapon.attackDistance, charged /* R3.2 piercing */, color);
```

- **Charge indicator (R3.4)**: `CharControlScript` already holds a `_playerHUD` reference. The charge ratio (`_primaryHeldSeconds / ChargedShot.ChargeTime`, clamped 0..1) is pushed to the HUD each frame while holding; it reaches full exactly at `ChargeTime`.
- **Normal fire preserved (R3.3)**: a release before `ChargeTime` yields `multiplier == 1f` and non-piercing, i.e. the existing basic arrow.


### R4 — Perfect Spacing (spear, distance-band reward)

`WeaponRunModifiers.DirectDamageMultiplier(distance, targetHealthRatio, playerHealthRatio, elements)` already scales direct-hit damage by distance for `Sniper` and `SpearTip`. Perfect Spacing adds one more term that rewards a mid-range band rather than "farther is better":

```csharp
// WeaponRunModifiers.DirectDamageMultiplier - added term (R4.1/R4.2)
// Perfect Spacing: +35% per rank when the hit distance is within [3.5, 6.5] meters.
if (distance >= 3.5f && distance <= 6.5f)
    result *= 1f + .35f * Rank(WeaponBoon.PerfectSpacing);
```

Because `DirectDamageMultiplier` is pure and reads only from ranks, this term:
- Leaves every source asset untouched (R4.4) — it is computed in the direct-hit damage path, not written back.
- Is non-decreasing with rank (R1.2) and applies only inside the band (R4.1/4.2).
- Multiplies cleanly with `Sniper`/`SpearTip` when the player stacks distance boons, so the band becomes a sweet spot the player actively holds.

**Feedback (R4.3)**: a small HUD/world cue escalates as consecutive in-band direct hits land and resets on an out-of-band hit. The counter lives on a small component that subscribes to the player's resolved-hit notification (same `NotifyAttackHits`/`OnAfterAttackHits` channel passives use); the visual is cosmetic and never gates the damage term.

### R5 — Impaling Line (spear, pierce-and-pull thrust)

The spear thrust is executed in `ArsenalCombat.Execute` via `TryApplyAreaDamage` along the thrust box, reading fields from the per-cast `ArsenalCastPlan`. Two flags are added to the plan, set from the boon rank in `WeaponRunModifiers.Plan`, mirroring the existing R7 spear flags (`ChainThrust`, `PhantomDelay`).

```csharp
// ArsenalCastPlan - NEW fields (value types, keeping the immutability invariant)
public bool ImpaleLine;    // R5.1: thrust damages every enemy along the line
public float ImpalePull;   // R5.2: meters pulled toward the player (0 = off)
```

```csharp
// WeaponRunModifiers.Plan - spear thrust branch (alongside EchoThrust/PhantomSpear/ChainThrust)
if (ability.Kind == ArsenalSkillKind.Thrust)
{
    // ...existing EchoThrust/Phantom/Chain terms...
    int impale = Rank(WeaponBoon.ImpalingLine);
    plan.ImpaleLine = impale > 0;                      // R5.1
    plan.ImpalePull  = .75f * impale;                  // R5.2 non-decreasing with rank
}
```

Consumption in `ArsenalCombat`:
- **Pierce the line (R5.1)**: the thrust already uses `TryApplyAreaDamage` with a box, which hits every enemy in the box (not just the first). When `ImpaleLine` is false the behavior is unchanged; when true the box length uses the full `plan.Range` so the whole line is covered.
- **Pull (R5.2/R5.3)**: for each connected enemy with a `SoftGroupingService`, a glide coroutine (modeled on the existing `GlideSweptEnemy`) moves the enemy *toward* the player by up to `ImpalePull` meters through its own locomotion (`ApplyExternalDisplacement`). Enemies without that service are not pulled (no hard impulse), so nothing teleports through scenery (R5.3).
- **Isolation (R5.4)**: all of this is driven off the per-cast `plan` snapshot; the source ability asset is never written.

```csharp
// ArsenalCombat - after the thrust primaryHits resolve (near ChainThrust/Phantom)
if (!radial && plan.ImpalePull > 0f && primaryHits > 0)
    ApplyImpalePull(center, direction, plan); // pulls connected enemies toward the player (R5.2)
```

### R6 — Momentum Strike (gauntlet, aggression stacks)

A plain-C# state holder driven by `HookBus` and the player's damage pipeline, feeding the run-scoped stat pipeline. Each Direct_Hit adds a stack; taking damage clears them.

```csharp
// NEW: MomentumStacks (plain MonoBehaviour component added by RunBoons when chosen)
public sealed class MomentumStacks : MonoBehaviour
{
    public const int MaxStacks = 10;           // R6.1
    private PlayerActor _player;
    private int _rank, _stacks;
    private PlayerStatModifier _mod;           // the single run-scoped modifier we rescale

    public void Configure(PlayerActor player, HookBus hooks, int rank) { /* subscribe; cache */ }
    // R6.1: a direct hit adds a stack (up to MaxStacks) -> rescale stat mod
    // R6.3: taking damage resets stacks to 0 -> rescale stat mod to zero
    // R6.2: outgoing damage += (2 * rank)% per stack, via IncreasedPercent
}
```

- **Stat pipeline (R6.2)**: momentum adds a single `PlayerStatModifier(IncreasedDamagePercent, IncreasedPercent, 2 * rank * stacks)` tagged with `RunBoons` as source. On each stack change the old modifier is removed and a fresh one added, so `GetStat` reflects the current stack count through the normal additive layer.
- **Reset on damage (R6.3)**: `PlayerActor` already raises a damage-taken path; `MomentumStacks` listens to it (or to `PlayerActor.HealthChanged` going down) and zeroes stacks.
- **HUD (R6.4)**: stack count pushed to the player HUD; clears to zero when stacks are lost.
- **Teardown (R6.5)**: `RunBoons.OnDestroy` already calls `Stats.RemoveModifiersFrom(this)`, which removes the momentum modifier because it is tagged with the same `RunBoons` source.

### R7 — Shockwave Finisher (gauntlet, combo end burst)

Gauntlet abilities run through `GauntletSteps(ability, slot)`, which deep-clones each authored `AreaHitStep` (JsonUtility round-trip) and appends echo steps for `FluryEcho`/`AsuraEcho`. Shockwave appends one extra cloned sphere step after the final authored step.

```csharp
// WeaponRunModifiers.GauntletSteps - after the existing clone loop and before/after echoes
int shock = Rank(WeaponBoon.Shockwave);
if (shock > 0 && steps.Count > 0)
{
    AreaHitStep finisher = steps[steps.Count - 1];
    AreaHitStep wave = JsonUtility.FromJson<AreaHitStep>(JsonUtility.ToJson(finisher)); // R7.4 clone, never the asset
    wave.hitShape = AreaHitShape.Sphere;                   // R7.1 spherical burst
    wave.sphereRadius = 3f * (1 + .2f * shock);            // R7.2 radius
    wave.localOffset = new Vector3(0, 1f - wave.sphereRadius, 0); // center on player (ShockRing pattern)
    wave.rangeOverride = .01f;
    wave.damageMultiplier *= .6f;                          // R7.2 fraction of finisher damage
    wave.delay += .08f;
    steps.Add(wave); // R7.1 appended after the final step
}
```

- **Stance + push (R7.3)**: the cloned step keeps the `stanceDamage` and `pushDistance` fields it inherited from the finisher, so the shockwave applies stance damage and outward push through the same `AreaHitStep` displacement channel as every other gauntlet step. These interact with `StanceCrusher` (stance/push scaling) and `ShockRing` (E rings) without special casing.
- **Isolation (R7.4)**: every step is a JsonUtility deep clone, so the source ability's authored steps are never mutated (same contract `GauntletSteps` already enforces via `AssetIsolationTests`).
- **Monotonicity (R1.2)**: `sphereRadius` scales by `(1 + .2 * shock)`, non-decreasing with rank.


### R8 — Elemental Overflow (cross-family, spend status for a burst)

This boon is catalogued outside the family gate (offered regardless of family, R8.5), so it is an inline `Offer` in `RunBoons.OfferReward` (like `conductor`/`reactor`) rather than a family `WeaponBoon`. Its rank is tracked by counting acquisitions (`RunModifierPresentation.Count`), the same way the repeatable stat/element boons scale.

The burst reuses `PlayerOnHitEffects` (which already knows about `BurnStatus`/`ChillStatus` and the Thermal Shock path) and `RunSynergyEffects.ReportImpact`, which routes a single bounded impact through the Resonance-amplified path without starting a new cascade.

```csharp
// PlayerOnHitEffects - NEW overflow (driven by RunBoons when chosen)
private int _overflowRank;        // 0 = off
public void EnableElementalOverflow() => _overflowRank++; // R8.2 scales with picks

// called from ApplyTo after elements/cascade resolve, on a direct hit that dealt damage
private void TryElementalOverflow(Actor enemy, float damageDealt)
{
    if (_overflowRank <= 0 || !enemy || enemy.IsDead || damageDealt <= 0f) return;
    bool burning = enemy.GetComponent<BurnStatus>();
    bool frozen  = enemy.GetComponent<ChillStatus>();
    if (!burning && !frozen) return;                       // R8.4
    float amount = damageDealt * .5f * _overflowRank;     // R8.2
    // R8.2/R8.3: bounded impact, Resonance-eligible, does not remove the status
    if (_synergies) _synergies.ReportImpact(enemy, amount, this);
    else enemy.TakeDamage(amount);  // no Resonance -> plain bounded hit
}
```

- **Trigger gate (R8.1/R8.4)**: only on a Direct_Hit (the `ApplyTo` call with `damageDealt > 0`) against an enemy that already carries an active `BurnStatus` or `ChillStatus`. No status -> no burst.
- **Bounded + Resonance-eligible (R8.3/1.2)**: routing through `ReportImpact` reuses the existing `LastSecondaryHits < MaxSecondaryHits` guard, so the burst cannot exceed the per-frame cascade budget and is amplified by Resonance when present.
- **Status preserved (R8.2)**: the burst is instantaneous damage; it never calls any status removal, so burn/chill remain with unchanged durations.
- **Combines with Thermal Shock**: a hit that both triggers Thermal Shock (fire↔ice) and Overflow produces two distinct bounded impacts, both under the same cascade budget.

### R9 — Offering and presentation

The family boons (R2–R7) appear automatically through the existing family/rank gate in `RunBoons.OfferReward` because they are catalogued `WeaponBoon`s (R9.1). The cross-family Overflow (R8) is added to the inline pool list next to the other synergy/element offers.

`RunModifierPresentation.For` already derives category/accent/scope from the catalog `Definition` for any `weapon_` id, so the family boons get their ARCO/LANÇA/MANOPLAS card styling for free. Two small edits:
- Add `ScopeFor` entries for the new `WeaponBoon` values (e.g. `ChargedShot`/`SplitArrow` -> `"FLECHAS"`, `PerfectSpacing`/`ImpalingLine` -> `"ESTOCADAS"` or `"BÁSICOS + SKILLS"`, `MomentumStrike` -> `"BÁSICOS"+`, `Shockwave` -> `"HABILIDADES"`).
- Add an inline `For` case for the Overflow id (category `"ELEMENTO"`, scope `"ACERTOS"`) and a Portuguese description (R9.2).

**Combination hints (R9.3)**: `RunModifierPresentation.Hint` gains a few cases:
- `weapon_ChargedShot` with `Piercing`/sniper: "COMBINAÇÃO - tiro carregado atravessa a fila."
- `weapon_SplitArrow` with `weapon_TwinShot`: "COMBINE - mais flechas, mais fragmentos."
- `weapon_MomentumStrike` with `bulwark`/vitality: "COMBINE - sobreviver mantém as pilhas."
- `overflow` with ignite/frost: "COMBINAÇÃO - consome fogo/gelo para um burst."

- **Choice count unchanged (R9.4)**: `OfferReward` still builds three choices; adding candidates to the pool does not change the `while (_choices.Count < 3)` cap.

## Data Models

### `WeaponBoon` enum + `Catalog` (extended)

| Field | Type | Notes |
| --- | --- | --- |
| `SplitArrow` | `WeaponBoon` | Bow, `MaxRank 3` |
| `ChargedShot` | `WeaponBoon` | Bow, `MaxRank 3` |
| `PerfectSpacing` | `WeaponBoon` | Spear, `MaxRank 3` |
| `ImpalingLine` | `WeaponBoon` | Spear, `MaxRank 3` |
| `MomentumStrike` | `WeaponBoon` | Gauntlet, `MaxRank 3` |
| `Shockwave` | `WeaponBoon` | Gauntlet, `MaxRank 3` |

### `ArsenalCastPlan` (extended)

Two new value-type fields: `bool ImpaleLine`, `float ImpalePull`. Both are value types, preserving the documented immutability invariant (no reference aliases an asset).

### Run-scoped state holders

| Holder | Lifetime | Teardown |
| --- | --- | --- |
| `SplitArrowCoordinator` | added when SplitArrow chosen | `HookBus.Clear()` drops its `OnKill` sub |
| `MomentumStacks` | added when MomentumStrike chosen | `Stats.RemoveModifiersFrom(RunBoons)` + `HookBus.Clear()` |
| `_overflowRank` (in `PlayerOnHitEffects`) | enabled when Overflow chosen | `PlayerOnHitEffects.Clear()` (called by `RunBoons.ClearRunState`) |


## Error Handling

- **Missing weapon/family mismatch**: `WeaponRunModifiers.Add` already refuses and logs (`LogWarning`) any boon whose family or rank is out of range; the new family boons inherit this gate with no extra code.
- **Null subscribers / throwing handlers**: `HookBus` wraps every dispatch in try/catch and logs via `Debug.LogException`, so a faulty SplitArrow/Momentum handler cannot break the combat pipeline.
- **Cascade exhaustion**: when the `MaxSecondaryHits` budget is reached, Split Arrow and Elemental Overflow silently stop (no exception, no partial state); this matches the existing cascade behavior.
- **Missing `SoftGroupingService`**: Impaling Line and the sweep displacement simply skip enemies that lack locomotion rather than applying a hard impulse, so no enemy is teleported or desynced.
- **Missing HUD**: the charge/momentum indicators null-guard the `_playerHUD` reference; gameplay behavior is unaffected when the HUD is absent.

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

Purely presentational, code-style, and process criteria (3.4, 4.4, 6.4, 9.1, 9.2, 9.3, 9.4) are covered by example/integration tests in the Testing Strategy, not by properties.

### Property 1: Asset isolation

*For any* application of any new boon at any rank, every original weapon, ability, and status `ScriptableObject` SHALL remain unchanged.

**Validates: Requirements 1.1, 4.4, 5.4, 7.4**

### Property 2: Monotonicity with rank

*For any* new boon and any two ranks `a < b` in `[1, MaxRank]`, the boon's primary named quantity at `b` SHALL be greater than or equal to its value at `a`.

**Validates: Requirements 1.2**

### Property 3: Order independence

*For any* two acquisition orderings that yield the same rank multiset, the Cast_Plan (and `GauntletSteps` output) SHALL be byte-identical.

**Validates: Requirements 1.3**

### Property 4: Run-end leaves no residual state

*For any* combination of chosen new boons, when the run ends all run-scoped state SHALL be cleared: HookBus subscriptions removed, momentum stat modifier removed, overflow rank reset, such that no split arrow/momentum/overflow effect from the ended run fires in any subsequent run.

**Validates: Requirements 1.4, 6.5**

### Property 5: Split Arrow generation bound

*For any* kill by a split arrow, no further split arrows SHALL be spawned (at most one generation), and the number of split arrows spawned in a single frame SHALL not exceed `MaxSecondaryHits`.

**Validates: Requirements 2.3, 2.4**

### Property 6: Split arrow count and damage

*For any* SplitArrow rank `r ≥ 1`, a kill SHALL spawn exactly `r` arrows on distinct headings, each carrying 50% of the killing arrow's damage multiplier.

**Validates: Requirements 2.1, 2.2**

### Property 7: Charged Shot threshold

*For any* held duration `h` and rank `r ≥ 1`, the shot SHALL be charged (damage × `(1 + 0.75r)`, piercing) if and only if `h ≥ 0.6`; otherwise it SHALL be a normal arrow (multiplier 1, non-piercing).

**Validates: Requirements 3.1, 3.2, 3.3**

### Property 8: Perfect Spacing band

*For any* direct-hit distance `d` and rank `r`, the Perfect Spacing multiplier SHALL equal `(1 + 0.35·r)` when `3.5 ≤ d ≤ 6.5` and exactly `1` otherwise.

**Validates: Requirements 4.1, 4.2**

### Property 9: Impaling Line pierce and pull

*For any* ImpalingLine rank `r ≥ 1`, a thrust SHALL damage every enemy along its line (not just the first), and each connected enemy with locomotion SHALL be displaced toward the player by at most `0.75·r` meters, never through solid scenery.

**Validates: Requirements 5.1, 5.2, 5.3**

### Property 10: Momentum stack and damage

*For any* MomentumStrike rank `r`, each direct hit SHALL add one stack up to 10, the outgoing-damage increase SHALL equal `(2 · r · stacks)%`, and taking any damage SHALL reset stacks (and the damage bonus) to zero.

**Validates: Requirements 6.1, 6.2, 6.3**

### Property 11: Shockwave append

*For any* Shockwave rank `r ≥ 1`, `GauntletSteps` SHALL append exactly one spherical step after the final authored step with radius `3 · (1 + 0.2r)`, and the source ability's authored steps SHALL remain unchanged.

**Validates: Requirements 7.1, 7.2, 7.4**

### Property 12: Elemental Overflow gate

*For any* direct hit of damage `d > 0` and rank `r ≥ 1`, an Overflow burst of magnitude `0.5r·d` SHALL trigger if and only if the enemy carries an active `BurnStatus` or `ChillStatus`, the status SHALL remain afterward, and the burst SHALL count against the cascade budget.

**Validates: Requirements 8.1, 8.2, 8.3, 8.4**

### Property 13: Cascade bounds invariant

*For any* enemy density and any combination of Split Arrow and Elemental Overflow, a single frame's secondary impacts SHALL not exceed `MaxSecondaryHits` (32).

**Validates: Requirements 2.4, 8.3**

## Testing Strategy

Tests follow the same conventions as the `modifier-synergies-theme17` spec: EditMode NUnit + a .NET PBT library (FsCheck/CsCheck) for properties (≥100 generated cases each, tagged `Feature: impactful-weapon-boons, Property {n}`), and PlayMode tests for physics-dependent projectile behavior. Test sub-tasks are optional in the plan.

### Property-based tests (EditMode unless noted)

- P1 Asset isolation: snapshot every source asset's serialized state, apply each new boon across ranks, assert unchanged (extends `AssetIsolationTests`).
- P2 Monotonicity: for each family boon, build plans/steps at ranks 1..3 and assert the named quantity is non-decreasing.
- P3 Order independence: apply boon permutations, assert identical plan/step output (extends the existing order-determinism tests).
- P5, P6 Split Arrow: PlayMode - spawn enemy layouts, kill with an arrow, assert split count/damage and that splits do not chain or exceed the frame budget.
- P7 Charged Shot: EditMode on the pure `ChargedShot` helper (threshold + multiplier).
- P8 Perfect Spacing: EditMode on `DirectDamageMultiplier` across distances and ranks.
- P9 Impaling Line: PlayMode - a line of enemies is all damaged; pull distance clamped and routed through locomotion.
- P10 Momentum: EditMode - simulate hit/damage events, assert stack count, stat-mod value, and reset.
- P11 Shockwave: EditMode - `GauntletSteps` appends exactly one sphere step of the expected radius; source steps unchanged.
- P12 Overflow: EditMode - burst triggers only with an active status, preserves it, and routes through `ReportImpact`.
- P13 Cascade bounds: EditMode + PlayMode - dense layouts never exceed `MaxSecondaryHits`.
- P4 Run-end cleanup: EditMode - after `RunBoons.OnDestroy`, Hook subscriptions, stat mods, and overflow rank are cleared.

### Example / integration tests

- Offering: with each family equipped, the family boons become eligible; choice count stays at 3 (R9.1, R9.4).
- Presentation: cards for the new boons render with Portuguese title/description and the `Nível X/Y` format (R9.2); combination hints appear (R9.3).
- Feedback cues: charge indicator (R3.4), spacing streak (R4.3), and momentum stack indicator (R6.4) update and clear as expected (smoke tests).
