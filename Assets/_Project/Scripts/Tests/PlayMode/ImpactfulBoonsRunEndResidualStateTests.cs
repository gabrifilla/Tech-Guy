using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for Property 4 of impactful-weapon-boons (run-end leaves no residual state).
    ///
    /// Property 4 (design): <em>for any</em> combination of chosen new boons, when the run ends all
    /// run-scoped state SHALL be cleared — HookBus subscriptions removed, the momentum stat modifier
    /// removed, and the overflow rank reset — such that no split-arrow / momentum / overflow effect from
    /// the ended run fires in any subsequent run. <b>Validates: Requirements 1.4, 6.5.</b>
    ///
    /// This extends the modifier-synergies-theme17 run-end coverage (<see cref="RunEndResidualStateTests"/>)
    /// to the three new run-scoped channels this feature introduces:
    /// <list type="bullet">
    /// <item><see cref="SplitArrowCoordinator"/> subscribes to <c>HookBus.OnKill</c>; the subscription is
    /// dropped by <c>HookBus.Clear()</c> (R1.4).</item>
    /// <item><see cref="MomentumStacks"/> subscribes to <c>HookBus.OnHit</c> and installs a
    /// <see cref="PlayerStatModifier"/> tagged with the <c>RunBoons</c> source; the sub is dropped by
    /// <c>HookBus.Clear()</c> and the modifier by <c>Stats.RemoveModifiersFrom(source)</c> (R6.5).</item>
    /// <item><see cref="PlayerOnHitEffects"/> holds the Elemental Overflow rank; it is reset to 0 by
    /// <c>PlayerOnHitEffects.Clear()</c> (R1.4/R8).</item>
    /// </list>
    ///
    /// The test reproduces the exact <c>RunBoons.OnDestroy</c> sequence — first
    /// <c>Stats.RemoveModifiersFrom(source)</c>, then the atomic <c>ClearRunState()</c> body
    /// (<c>PlayerOnHitEffects.Clear()</c> + <c>HookBus.Clear()</c>) — against a real player host carrying
    /// the two coordinators and the element registry, then asserts every new-boon channel is inert
    /// afterwards. Runs in PlayMode because the coordinators/registry are real MonoBehaviour components
    /// with status/component lookups and a live PlayerActor damage pipeline.
    ///
    /// The seeded harness drives &gt;= 100 deterministic cases; each builds a randomized run (rank, stacks,
    /// overflow picks) and asserts the post-clear invariants.
    /// </summary>
    public sealed class ImpactfulBoonsRunEndResidualStateTests
    {
        private GameObject _playerGo;
        private GameObject _enemyGo;
        private Object _statSource;

        [TearDown]
        public void TearDown()
        {
            if (_playerGo) Object.DestroyImmediate(_playerGo);
            if (_enemyGo) Object.DestroyImmediate(_enemyGo);
            if (_statSource) Object.DestroyImmediate(_statSource);
            _playerGo = null; _enemyGo = null; _statSource = null;
        }

        // A throwaway ScriptableObject standing in for the RunBoons source the momentum modifier is tagged
        // with (RunBoons is a MonoBehaviour; a ScriptableObject is a valid UnityEngine.Object source tag
        // and mirrors how RemoveModifiersFrom(this) removes only same-source modifiers).
        private sealed class RunSourceStub : ScriptableObject { }

        private Actor BuildEnemy()
        {
            _enemyGo = new GameObject("ImpactfulRunEndEnemy");
            _enemyGo.SetActive(false);
            _enemyGo.AddComponent<BoxCollider>().size = Vector3.one;
            Actor actor = _enemyGo.AddComponent<Actor>();
            actor.health = 100000f;
            _enemyGo.SetActive(true);
            return actor;
        }

        // Feature: impactful-weapon-boons, Property 4: run-end leaves no residual state.
        // After the RunBoons.OnDestroy teardown sequence (RemoveModifiersFrom(source) + ClearRunState's
        // PlayerOnHitEffects.Clear() + HookBus.Clear()): the SplitArrow OnKill sub and Momentum OnHit sub
        // are dropped (their coordinators never react to a post-clear raise), the momentum stat modifier
        // is gone (GetStat bonus is zero), and the overflow rank is 0 (a status-carrying hit no longer
        // bursts). Holds for any rank / stack count / overflow pick count.
        // Validates: Requirements 1.4, 6.5
        [UnityTest]
        public IEnumerator Property4_RunEndClearsNewBoonState()
        {
            const int cases = 100;
            const float baseIncreasedPercent = 100f;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 4);

            for (int c = 0; c < cases; c++)
            {
                TearDown();

                // --- build a run with all three new run-scoped channels armed ---
                _playerGo = new GameObject("ImpactfulRunEndPlayer");
                _playerGo.SetActive(false); // configure before Awake
                PlayerActor player = _playerGo.AddComponent<PlayerActor>();
                player.health = 1000f;
                player.Stats.increasedDamagePercent = baseIncreasedPercent;

                // A hand transform so EquipWeapon (which may spawn a weapon model) has a mount point.
                var hand = new GameObject("Hand");
                hand.transform.SetParent(_playerGo.transform);
                typeof(PlayerActor).GetField("handTransform",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(player, hand.transform);

                // A bow-like weapon so the SplitArrow coordinator's FiresArrows gate would pass if it fired.
                var weapon = ScriptableObject.CreateInstance<WeaponScript>();
                weapon.weaponName = "ImpactfulRunEndBow";
                weapon.attackDamage = 10f;
                typeof(WeaponScript).GetField("_firesArrows",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(weapon, true);
                // `weapon` is a public field; `startingWeapon` is private serialized. Set both before Awake
                // so the equip path resolves to this deterministic bow without a Resources.Load fallback.
                player.weapon = weapon;
                typeof(PlayerActor).GetField("startingWeapon",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(player, weapon);

                PlayerOnHitEffects effects = _playerGo.AddComponent<PlayerOnHitEffects>();

                var hooks = new HookBus();
                _statSource = ScriptableObject.CreateInstance<RunSourceStub>();

                SplitArrowCoordinator split = _playerGo.AddComponent<SplitArrowCoordinator>();
                MomentumStacks momentum = _playerGo.AddComponent<MomentumStacks>();

                _playerGo.SetActive(true); // Awake -> EquipWeapon
                player.EquipWeapon(weapon);

                int splitRank = 1 + rng.Next(0, 3);      // 1..3
                int momentumRank = 1 + rng.Next(0, 3);   // 1..3
                int overflowPicks = 1 + rng.Next(0, 3);  // 1..3

                split.Configure(player, hooks, splitRank);
                momentum.Configure(player, hooks, momentumRank, _statSource);
                for (int p = 0; p < overflowPicks; p++) effects.EnableElementalOverflow();

                // Build momentum stacks so a non-zero stat modifier exists before the clear.
                int stacks = 1 + rng.Next(0, MomentumStacks.MaxStacks);
                for (int s = 0; s < stacks; s++) hooks.RaiseHit(null, 1f);

                Actor enemy = BuildEnemy();
                BurnStatus.Apply(enemy, 5f, 5f); // so overflow WOULD burst pre-clear

                // --- pre-clear sanity: every channel is live ---
                Assert.Greater(momentum.Stacks, 0, $"case {c}: momentum should hold stacks before the clear");
                Assert.Greater(InstalledBonus(player), 0f, $"case {c}: momentum stat modifier should exist before the clear");
                Assert.IsTrue(effects.HasAnyEffect, $"case {c}: overflow rank should register before the clear");

                float burnBefore = enemy.health;
                effects.ApplyTo(enemy, 20f);
                Assert.Less(enemy.health, burnBefore, $"case {c}: overflow burst should fire on a status-carrying hit before the clear");

                // --- reproduce RunBoons.OnDestroy: RemoveModifiersFrom(source) then ClearRunState body ---
                player.Stats.RemoveModifiersFrom(_statSource); // OnDestroy step 1 (R6.5)
                effects.Clear();                               // ClearRunState -> PlayerOnHitEffects.Clear (overflow reset)
                hooks.Clear();                                 // ClearRunState -> HookBus.Clear (drops OnKill/OnHit subs)

                // --- post-clear invariants ---

                // R6.5: the momentum stat modifier tagged with the RunBoons source is gone.
                Assert.That(InstalledBonus(player), Is.EqualTo(0f).Within(1e-3f),
                    $"case {c}: momentum stat modifier survived run-end RemoveModifiersFrom");
                Assert.That(PipelineBonus(player, baseIncreasedPercent), Is.EqualTo(0f).Within(1e-3f),
                    $"case {c}: momentum bonus still flows through GetStat after clear");

                // R1.4: the Momentum OnHit subscription is dropped — a post-clear hit no longer reaches
                // the coordinator, so it adds no new stacks and re-installs no modifier. (HookBus.Clear
                // nulls the event; the coordinator's own _stacks field is run-scoped state that the
                // torn-down component simply stops mutating — what matters is that a later run's hits
                // never drive it, which a post-clear raise adding nothing proves.)
                int stacksBeforePostClearHit = momentum.Stacks;
                hooks.RaiseHit(null, 1f);
                Assert.AreEqual(stacksBeforePostClearHit, momentum.Stacks,
                    $"case {c}: Momentum reacted to OnHit after HookBus.Clear (stacks {stacksBeforePostClearHit} -> {momentum.Stacks})");
                Assert.That(InstalledBonus(player), Is.EqualTo(0f).Within(1e-3f),
                    $"case {c}: a post-clear hit re-installed a momentum modifier");

                // R1.4: the SplitArrow OnKill subscription is dropped — a post-clear kill spawns no arrows.
                var beforeSplit = new System.Collections.Generic.HashSet<ArsenalProjectile>(
                    Object.FindObjectsByType<ArsenalProjectile>(FindObjectsSortMode.None));
                hooks.RaiseKill(enemy);
                int spawned = 0;
                foreach (ArsenalProjectile pr in Object.FindObjectsByType<ArsenalProjectile>(FindObjectsSortMode.None))
                    if (!beforeSplit.Contains(pr)) { spawned++; Object.DestroyImmediate(pr.gameObject); }
                Assert.AreEqual(0, spawned,
                    $"case {c}: SplitArrow spawned {spawned} arrows on a post-clear kill (OnKill sub not dropped)");

                // R1.4/R8: the overflow rank is reset — a status-carrying hit no longer bursts.
                Actor freshEnemy = BuildEnemyExtra();
                BurnStatus.Apply(freshEnemy, 5f, 5f);
                float postBefore = freshEnemy.health;
                effects.ApplyTo(freshEnemy, 20f);
                Assert.That(postBefore - freshEnemy.health, Is.EqualTo(0f).Within(1e-2f),
                    $"case {c}: overflow burst still fired after Clear reset the overflow rank");
                if (freshEnemy) Object.DestroyImmediate(freshEnemy.gameObject);

                if (weapon) Object.DestroyImmediate(weapon);

                yield return null;
            }
        }

        // A second enemy for the post-clear overflow check (kept separate from _enemyGo so TearDown of the
        // primary enemy doesn't interfere; destroyed inline).
        private static Actor BuildEnemyExtra()
        {
            var go = new GameObject("ImpactfulRunEndEnemyPost");
            go.SetActive(false);
            go.AddComponent<BoxCollider>().size = Vector3.one;
            Actor actor = go.AddComponent<Actor>();
            actor.health = 100000f;
            go.SetActive(true);
            return actor;
        }

        // Sum of the momentum modifier contribution (IncreasedDamagePercent, IncreasedPercent) on the player.
        private static float InstalledBonus(PlayerActor player)
        {
            float sum = 0f;
            foreach (PlayerStatModifier m in player.Stats.RuntimeModifiers)
                if (m != null && m.statType == PlayerStatType.IncreasedDamagePercent
                    && m.mode == PlayerStatModifierMode.IncreasedPercent)
                    sum += m.value;
            return sum;
        }

        private static float PipelineBonus(PlayerActor player, float baseValue)
        {
            float stat = player.Stats.GetStat(PlayerStatType.IncreasedDamagePercent);
            return (stat / baseValue - 1f) * 100f;
        }
    }
}
