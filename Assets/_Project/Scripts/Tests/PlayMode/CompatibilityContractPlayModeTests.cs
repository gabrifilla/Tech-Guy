using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode compatibility/regression tests for task 18.3 of combat-foundation-rework. After the
    /// foundation refactor (tasks 13/15/16/18), these pin that the <em>audited event contract</em>
    /// (contract-audit.md, invariants O-1..O-11) the Arco/Lança flow and the boons still depend on keeps
    /// holding. They are deliberately targeted — a re-verification of the specific channels the audit
    /// flagged, not a full re-test of every boon — and complement
    /// <see cref="DashCancelResolverPlayModeTests"/> (which already covers the dash
    /// <c>AbilityUsed(4)</c>→<c>OnDash</c> ordering and the melee-cast locomotion pin) by covering the
    /// GAPS: the Arco side of the locomotion contract (O-9), the <see cref="HookBus.OnBasicHit"/> channel
    /// the Manopla boons subscribe to, and one representative boon reacting to its audited events.
    ///
    /// <list type="number">
    /// <item><b>Arco fires on the move (O-9 / R8.3):</b> while a bow cast reports
    /// <c>AllowsMovementWhileFiring</c>, the holder's <see cref="AbilityHolder.MovementAllowedWhileCasting"/>
    /// is <c>true</c>; a melee (Breaker/Manopla) cast reports <c>false</c> — the <c>(IsCasting,
    /// MovementAllowedWhileCasting)</c> pair keeps determining the same locomotion policy (Arco unpinned,
    /// melee pinned).</item>
    /// <item><b>Basic-hit channel intact (O-3 / audited HookBus.OnBasicHit):</b> raising the basic channel
    /// delivers to a subscriber exactly once per raise — the channel AsuraSurge / AdaptiveCadenceTracker /
    /// ImpactGuardTracker / HungryComboTracker all subscribe to is still live and fans out intact.</item>
    /// <item><b>A boon still receives its audited events:</b> the representative
    /// <see cref="ImpactGuardTracker"/> (Guard Breaker) still advances its streak on
    /// <see cref="HookBus.OnBasicHit"/> and resets it on <see cref="AbilityHolder.AbilityUsed"/> (any slot),
    /// wired through its real production subscriptions (O-3 + O-7).</item>
    /// </list>
    ///
    /// <para>
    /// Runs in PlayMode because <see cref="PlayerActor"/>/<see cref="AbilityHolder"/>/
    /// <see cref="ArsenalCombat"/>/<see cref="BreakerGauntletCombat"/>/<see cref="Actor"/> are
    /// MonoBehaviours with real lifecycle. The casting state is injected onto the real combat executors by
    /// writing their <c>IsExecuting</c>/<c>AllowsMovementWhileFiring</c> backing fields — exactly the
    /// members the holder's getters read (contract-audit §7/§8) — rather than standing up the full cast
    /// coroutine (which needs a weapon prefab, Animator, mana and a NavMeshAgent and would make the
    /// casting state non-deterministic), mirroring the injected-cast approach of
    /// <see cref="DashCancelResolverTestRig"/>. Every object is created and torn down explicitly; no
    /// <c>FindObjectOfType</c>/<c>GameObject.Find</c>.
    /// </para>
    /// </summary>
    public sealed class CompatibilityContractPlayModeTests
    {
        private const BindingFlags Instance =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private readonly System.Collections.Generic.List<GameObject> _spawned =
            new System.Collections.Generic.List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go) UnityEngine.Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        // ---------------------------------------------------------------------------------------------
        // (1) Arco fires on the move; melee is pinned — contract-audit O-9 (R8.3), the Arco side the
        // DashCancelResolverPlayModeTests do not cover.
        // ---------------------------------------------------------------------------------------------

        // O-9 / R8.3: while an Arco (ArsenalCombat) cast reports AllowsMovementWhileFiring, the holder's
        // MovementAllowedWhileCasting is true (the Arco keeps moving at reduced speed), and IsCasting is
        // true. This is the "firing-on-the-move" half of the locomotion contract the refactor preserves.
        [UnityTest]
        public IEnumerator ArcoCast_AllowsMovementWhileFiring_HolderReflectsIt()
        {
            (PlayerActor player, AbilityHolder holder) = BuildPlayer();

            ArsenalCombat arsenal = AddArsenal(player, holder);
            // Drive the arsenal into the Arco firing-on-the-move state: executing AND movement allowed.
            SetBackingProperty(arsenal, nameof(ArsenalCombat.IsExecuting), true);
            SetBackingProperty(arsenal, nameof(ArsenalCombat.AllowsMovementWhileFiring), true);

            Assert.IsTrue(holder.IsCasting,
                "an executing Arco cast must report IsCasting (contract-audit §7)");
            Assert.IsTrue(holder.MovementAllowedWhileCasting,
                "an Arco cast that AllowsMovementWhileFiring must leave MovementAllowedWhileCasting true "
                + "— the bow fires on the move (contract-audit O-9/§8)");

            yield return null;
        }

        // O-9 / R8.3: a melee (Breaker/Manopla) cast is pinned — IsCasting true but
        // MovementAllowedWhileCasting false, because MovementAllowedWhileCasting is driven ONLY by the
        // Arsenal's AllowsMovementWhileFiring. This is the "melee stays pinned" half of the same contract.
        [UnityTest]
        public IEnumerator MeleeCast_PinsPlayer_MovementNotAllowed()
        {
            (PlayerActor player, AbilityHolder holder) = BuildPlayer();

            BreakerGauntletCombat breaker = AddBreaker(player, holder);
            SetBackingProperty(breaker, nameof(BreakerGauntletCombat.IsExecuting), true);

            Assert.IsTrue(holder.IsCasting,
                "an executing melee Breaker cast must report IsCasting (contract-audit §7)");
            Assert.IsFalse(holder.MovementAllowedWhileCasting,
                "a melee cast pins the player: MovementAllowedWhileCasting must be false (contract-audit O-9/§8)");

            yield return null;
        }

        // O-9: an Arco cast that is NOT in its firing-on-the-move phase (AllowsMovementWhileFiring false)
        // leaves MovementAllowedWhileCasting false even though IsCasting is true — the two members track
        // their distinct observable states, exactly as the audit records.
        [UnityTest]
        public IEnumerator ArcoCast_WithoutFiringMovement_DoesNotAllowMovement()
        {
            (PlayerActor player, AbilityHolder holder) = BuildPlayer();

            ArsenalCombat arsenal = AddArsenal(player, holder);
            SetBackingProperty(arsenal, nameof(ArsenalCombat.IsExecuting), true);
            SetBackingProperty(arsenal, nameof(ArsenalCombat.AllowsMovementWhileFiring), false);

            Assert.IsTrue(holder.IsCasting, "the Arco cast is executing, so IsCasting must be true");
            Assert.IsFalse(holder.MovementAllowedWhileCasting,
                "when the Arco is not in its firing-on-the-move phase, MovementAllowedWhileCasting is false (O-9)");

            yield return null;
        }

        // ---------------------------------------------------------------------------------------------
        // (2) The basic-hit channel the boons subscribe to is intact — contract-audit §10 / O-3.
        // ---------------------------------------------------------------------------------------------

        // Audited HookBus.OnBasicHit: the channel every Manopla boon (AsuraSurge, AdaptiveCadenceTracker,
        // ImpactGuardTracker, HungryComboTracker) subscribes to still delivers exactly one callback per
        // RaiseBasicHit, carrying the struck actor and the damage — the bus channel contract the boons
        // depend on is unbroken (this is the channel-level assertion the task calls for; standing up the
        // full HitboxDamage path is heavy, so we pin the bus contract directly).
        [UnityTest]
        public IEnumerator OnBasicHitChannel_DeliversOncePerRaise_WithPayload()
        {
            var bus = new HookBus();
            Actor enemy = BuildEnemy();

            int fired = 0;
            Actor received = null;
            float receivedDamage = 0f;
            bus.OnBasicHit += (a, d) => { fired++; received = a; receivedDamage = d; };

            bus.RaiseBasicHit(enemy, 17.5f);

            Assert.AreEqual(1, fired, "OnBasicHit must deliver exactly once per RaiseBasicHit (the boon channel)");
            Assert.AreSame(enemy, received, "OnBasicHit must forward the struck actor unchanged");
            Assert.AreEqual(17.5f, receivedDamage, 1e-4f, "OnBasicHit must forward the dealt damage unchanged");

            // Two more raises fan out to the same single subscriber once each (channel still live, no dup).
            bus.RaiseBasicHit(enemy, 1f);
            bus.RaiseBasicHit(enemy, 1f);
            Assert.AreEqual(3, fired, "each RaiseBasicHit must deliver exactly once (no silent drop, no duplication)");

            yield return null;
        }

        // ---------------------------------------------------------------------------------------------
        // (3) A representative boon still receives its audited events — ImpactGuardTracker (Guard Breaker).
        // contract-audit O-3 (OnBasicHit advances) + O-7 (AbilityUsed resets), through its REAL subscriptions.
        // ---------------------------------------------------------------------------------------------

        // The Guard Breaker boon advances its streak on the audited HookBus.OnBasicHit channel and resets
        // it on AbilityHolder.AbilityUsed (any slot) — wired via its real Configure(...) subscriptions.
        // This confirms a boon that depends on the audited events still reacts after the refactor.
        [UnityTest]
        public IEnumerator ImpactGuardTracker_StillReactsToBasicHitAndAbilityUsed()
        {
            (PlayerActor player, AbilityHolder holder) = BuildPlayer();
            var bus = new HookBus();
            Actor enemy = BuildEnemy();

            ImpactGuardTracker tracker = player.gameObject.AddComponent<ImpactGuardTracker>();
            tracker.Configure(player, bus, holder, rank: 1);

            Assert.AreEqual(0, tracker.Streak, "a freshly configured tracker starts at streak 0");

            // Two basic hits on the audited channel advance the streak (third would break the guard; we
            // stop at two to assert the "advances on OnBasicHit" dependency without triggering a reaction).
            bus.RaiseBasicHit(enemy, 5f);
            Assert.AreEqual(1, tracker.Streak, "the boon must advance its streak on OnBasicHit (contract-audit O-3)");
            bus.RaiseBasicHit(enemy, 5f);
            Assert.AreEqual(2, tracker.Streak, "each OnBasicHit must advance the streak");

            // Any ability use (slot 0..4) resets the streak — the audited AbilityUsed dependency (O-7).
            RaiseAbilityUsed(holder, 2);
            Assert.AreEqual(0, tracker.Streak,
                "an AbilityUsed (any slot) must reset the boon's streak (contract-audit O-7)");

            // The channel keeps working after the reset: another basic hit advances again.
            bus.RaiseBasicHit(enemy, 5f);
            Assert.AreEqual(1, tracker.Streak, "the boon keeps reacting to OnBasicHit after a reset");

            // Teardown drops the subscriptions cleanly (O-11): destroying the tracker must not throw when
            // the bus later raises again.
            UnityEngine.Object.DestroyImmediate(tracker);
            Assert.DoesNotThrow(() => bus.RaiseBasicHit(enemy, 5f),
                "after the boon is destroyed its subscription is gone — a later raise is a safe no-op (O-11)");

            yield return null;
        }

        // --- rig helpers --------------------------------------------------------------------------------

        // Builds a minimal but real player: PlayerActor + AbilityHolder with a hand transform, a weapon,
        // and deterministic stats, so the combat executors' Awake (which reads PlayerActor) runs cleanly.
        private (PlayerActor, AbilityHolder) BuildPlayer()
        {
            var go = new GameObject("CompatTestPlayer");
            go.SetActive(false);
            _spawned.Add(go);

            var hand = new GameObject("Hand");
            hand.transform.SetParent(go.transform);

            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.weaponName = "CompatTestWeapon";
            weapon.attackDamage = 1f;

            var player = go.AddComponent<PlayerActor>();
            player.health = 1000f;
            player.mana = 1000f;
            SetPrivate(player, "handTransform", hand.transform);
            SetPrivate(player, "startingWeapon", weapon);
            SetPrivate(player, "weapon", weapon);
            SetPrivate(player, "stats", new PlayerArpgStats
            {
                baseDamage = 0f, criticalChance = 0f, damageMultiplier = 1f,
                increasedDamagePercent = 0f, flatDamageBonus = 0f,
            });

            var holder = go.AddComponent<AbilityHolder>();
            go.SetActive(true); // Awake/Start lifecycle
            return (player, holder);
        }

        // Adds a real ArsenalCombat to the player and wires it into the holder's private _arsenalCombat
        // field (the field the IsCasting/MovementAllowedWhileCasting getters read). ArsenalCombat.Awake
        // only needs the PlayerActor already on the GameObject.
        private ArsenalCombat AddArsenal(PlayerActor player, AbilityHolder holder)
        {
            ArsenalCombat arsenal = player.gameObject.GetComponent<ArsenalCombat>();
            if (!arsenal) arsenal = player.gameObject.AddComponent<ArsenalCombat>();
            SetPrivate(holder, "_arsenalCombat", arsenal);
            return arsenal;
        }

        private BreakerGauntletCombat AddBreaker(PlayerActor player, AbilityHolder holder)
        {
            BreakerGauntletCombat breaker = player.gameObject.GetComponent<BreakerGauntletCombat>();
            if (!breaker) breaker = player.gameObject.AddComponent<BreakerGauntletCombat>();
            SetPrivate(holder, "_breakerCombat", breaker);
            return breaker;
        }

        private Actor BuildEnemy()
        {
            var go = new GameObject("CompatTestEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            Actor actor = go.AddComponent<Actor>();
            actor.health = 1_000_000f;
            go.SetActive(true);
            return actor;
        }

        // Raises AbilityHolder.AbilityUsed(index) via reflection on the compiler-generated event backing
        // field, so the test can exercise the real subscriber (the boon) without a full ability use.
        private static void RaiseAbilityUsed(AbilityHolder holder, int index)
        {
            FieldInfo fi = GetField(typeof(AbilityHolder), "AbilityUsed");
            if (fi == null) throw new InvalidOperationException("AbilityUsed backing field not found on AbilityHolder");
            var handler = (Action<int>)fi.GetValue(holder);
            handler?.Invoke(index);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = GetField(target.GetType(), field);
            if (fi == null) throw new InvalidOperationException($"Field '{field}' not found on {target.GetType().Name}");
            fi.SetValue(target, value);
        }

        private static void SetBackingProperty(object target, string property, object value)
        {
            SetPrivate(target, $"<{property}>k__BackingField", value);
        }

        private static FieldInfo GetField(Type type, string field)
        {
            for (Type t = type; t != null; t = t.BaseType)
            {
                FieldInfo fi = t.GetField(field, Instance);
                if (fi != null) return fi;
            }
            return null;
        }
    }
}
