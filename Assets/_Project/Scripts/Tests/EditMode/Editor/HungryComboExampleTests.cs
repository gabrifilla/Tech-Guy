using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for the HungryCombo / "Combo faminto" wiring
    /// (gauntlet-boon-playstyle-overhaul R6).
    ///
    /// The boon's whole contract is a thin coordinator: <c>HungryComboTracker</c> subscribes to
    /// <see cref="HookBus.OnBasicHit"/> and, per basic hit, calls
    /// <see cref="AbilityHolder.ReduceCooldowns(float)"/> with <c>0.3s * rank</c> on the live run-scoped
    /// cooldown timers (R6.1), never on a skill hit (which raises only the generic <c>OnHit</c>), and it
    /// drops the subscription on teardown (R6.4).
    ///
    /// These tests exercise the REAL <c>HungryComboTracker</c> against the REAL <see cref="HookBus"/> and
    /// a REAL <see cref="AbilityHolder"/>: a basic hit raised on the bus must shave the live cooldowns,
    /// a generic <c>OnHit</c> (skill path) must leave them untouched, and after the tracker is destroyed
    /// a further basic hit must have no effect because the subscription was removed.
    ///
    /// <c>AbilityHolder</c>'s cooldown timers/states arrays and its <c>AbilityState</c> enum are private,
    /// so — mirroring <see cref="ReduceCooldownsPropertyTests"/> — the holder is built on an inactive
    /// GameObject (no Awake/scene lifecycle) and its parallel arrays are seeded by reflection. The
    /// tracker is driven only through its public <c>Configure</c> + the bus, so the production
    /// subscribe/reduce/teardown path is what is under test.
    /// </summary>
    public sealed class HungryComboExampleTests
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;

        // The private nested AbilityHolder.AbilityState enum (Ready=0, Active=1, Cooldown=2), resolved by
        // name so the test does not hard-code the inaccessible type.
        private static readonly Type StateType =
            typeof(AbilityHolder).GetNestedType("AbilityState", BindingFlags.NonPublic);

        private static object State(string name) => Enum.Parse(StateType, name);

        private static FieldInfo Field(string name)
        {
            FieldInfo field = typeof(AbilityHolder).GetField(name, Instance);
            Assert.IsNotNull(field, $"Expected private field '{name}' on AbilityHolder (test seam).");
            return field;
        }

        // Builds an inactive-host AbilityHolder with a single slot in the given state and live timer.
        private static AbilityHolder BuildHolder(GameObject host, string state, float liveTimer, out Ability source)
        {
            source = ScriptableObject.CreateInstance<Ability>();
            source.name = "HungryComboSource";
            source.cooldownTime = 10f; // authored cooldown the boon must never touch

            var abilities = new[] { source };
            var cooldownTimers = new[] { liveTimer };
            var activeTimers = new[] { 0f };
            var states = Array.CreateInstance(StateType, 1);
            states.SetValue(State(state), 0);

            AbilityHolder holder = host.AddComponent<AbilityHolder>();
            Field("activeAbilities").SetValue(holder, abilities);
            Field("cooldownTimers").SetValue(holder, cooldownTimers);
            Field("activeTimers").SetValue(holder, activeTimers);
            Field("states").SetValue(holder, states);
            return holder;
        }

        private static float LiveCooldown(AbilityHolder holder) =>
            ((float[])Field("cooldownTimers").GetValue(holder))[0];

        // R6.1: a basic hit reduces the live cooldown by exactly 0.3 x rank.
        [Test]
        public void BasicHit_ReducesLiveCooldownByPointThreePerRank()
        {
            Assert.IsNotNull(StateType, "Could not resolve the private AbilityHolder.AbilityState enum.");

            var host = new GameObject("HungryComboHolder");
            host.SetActive(false);
            Ability source = null;
            try
            {
                AbilityHolder holder = BuildHolder(host, "Cooldown", 5f, out source);
                var bus = new HookBus();
                var tracker = host.AddComponent<HungryComboTracker>();
                tracker.Configure(null, bus, holder, 2); // rank 2 -> 0.6s per basic hit

                bus.RaiseBasicHit(null, 10f);

                Assert.That(LiveCooldown(holder), Is.EqualTo(5f - 0.6f).Within(1e-4f),
                    "A basic hit at rank 2 should shave 0.3 x 2 = 0.6s off the live cooldown.");
                Assert.That(source.cooldownTime, Is.EqualTo(10f),
                    "The authored source cooldown must never be touched.");
            }
            finally
            {
                if (source) UnityEngine.Object.DestroyImmediate(source);
                if (host) UnityEngine.Object.DestroyImmediate(host);
            }
        }

        // R6.1: multiple basic hits stack their reductions, clamped at zero by ReduceCooldowns.
        [Test]
        public void BasicHits_StackReductions_ClampedAtZero()
        {
            var host = new GameObject("HungryComboHolder");
            host.SetActive(false);
            Ability source = null;
            try
            {
                AbilityHolder holder = BuildHolder(host, "Cooldown", 1f, out source);
                var bus = new HookBus();
                var tracker = host.AddComponent<HungryComboTracker>();
                tracker.Configure(null, bus, holder, 3); // 0.9s per basic hit

                bus.RaiseBasicHit(null, 10f); // 1.0 -> 0.1
                Assert.That(LiveCooldown(holder), Is.EqualTo(0.1f).Within(1e-4f));

                bus.RaiseBasicHit(null, 10f); // 0.1 - 0.9 -> clamp 0
                Assert.That(LiveCooldown(holder), Is.EqualTo(0f),
                    "The live cooldown must clamp at zero and never go negative.");
            }
            finally
            {
                if (source) UnityEngine.Object.DestroyImmediate(source);
                if (host) UnityEngine.Object.DestroyImmediate(host);
            }
        }

        // A skill hit raises only the generic OnHit, which this boon never subscribes to, so a slot on
        // cooldown is left untouched.
        [Test]
        public void SkillHit_DoesNotTriggerTheReduction()
        {
            var host = new GameObject("HungryComboHolder");
            host.SetActive(false);
            Ability source = null;
            try
            {
                AbilityHolder holder = BuildHolder(host, "Cooldown", 5f, out source);
                var bus = new HookBus();
                var tracker = host.AddComponent<HungryComboTracker>();
                tracker.Configure(null, bus, holder, 3);

                bus.RaiseHit(null, 50f); // skill/area path: never raises OnBasicHit

                Assert.That(LiveCooldown(holder), Is.EqualTo(5f),
                    "A skill hit must not reduce cooldowns through the HungryCombo boon.");
            }
            finally
            {
                if (source) UnityEngine.Object.DestroyImmediate(source);
                if (host) UnityEngine.Object.DestroyImmediate(host);
            }
        }

        // R6.4: tearing the tracker down drops the OnBasicHit subscription, so a later basic hit on the
        // same bus has no effect. Teardown runs the tracker's real OnDestroy -> Unsubscribe path, which is
        // what the production run relies on when the tracker's GameObject is destroyed.
        //
        // The teardown is driven through the component's real OnDestroy (resolved by name, like the other
        // reflection seams in this file) rather than relying on DestroyImmediate alone: in EditMode Unity
        // does not run Awake/OnEnable for a component added via AddComponent at runtime, and so it never
        // calls OnDestroy for that component when the host is destroyed. Invoking the production OnDestroy
        // exercises the exact teardown code path (OnDestroy -> Unsubscribe) and keeps the R6.4 contract
        // under test; the host is still DestroyImmediate'd afterwards for cleanup.
        [Test]
        public void Teardown_DropsTheSubscription()
        {
            var holderHost = new GameObject("HungryComboHolder");
            holderHost.SetActive(false);
            var trackerHost = new GameObject("HungryComboTrackerHost");
            Ability source = null;
            try
            {
                AbilityHolder holder = BuildHolder(holderHost, "Cooldown", 5f, out source);
                var bus = new HookBus();
                var tracker = trackerHost.AddComponent<HungryComboTracker>();
                tracker.Configure(null, bus, holder, 3);

                // Run the tracker's real teardown (OnDestroy -> Unsubscribe), the same path the engine
                // invokes when the tracker's GameObject is destroyed in a live run. (The run-end path,
                // HookBus.Clear(), drops the subscription too — see HookBusClear_DropsTheSubscription.)
                MethodInfo onDestroy = typeof(HungryComboTracker).GetMethod("OnDestroy", Instance);
                Assert.IsNotNull(onDestroy, "Expected a private OnDestroy teardown on HungryComboTracker (R6.4).");
                onDestroy.Invoke(tracker, null);

                bus.RaiseBasicHit(null, 10f);

                Assert.That(LiveCooldown(holder), Is.EqualTo(5f),
                    "After teardown the tracker must no longer react to basic hits.");
            }
            finally
            {
                if (source) UnityEngine.Object.DestroyImmediate(source);
                if (trackerHost) UnityEngine.Object.DestroyImmediate(trackerHost);
                if (holderHost) UnityEngine.Object.DestroyImmediate(holderHost);
            }
        }

        // R6.4: the run-end teardown path, HookBus.Clear() (called by RunBoons on run end), also drops the
        // subscription, so a later basic hit on the cleared bus has no effect.
        [Test]
        public void HookBusClear_DropsTheSubscription()
        {
            var host = new GameObject("HungryComboHolder");
            host.SetActive(false);
            Ability source = null;
            try
            {
                AbilityHolder holder = BuildHolder(host, "Cooldown", 5f, out source);
                var bus = new HookBus();
                var tracker = host.AddComponent<HungryComboTracker>();
                tracker.Configure(null, bus, holder, 3);

                bus.Clear(); // run-end teardown drops every subscription

                bus.RaiseBasicHit(null, 10f);

                Assert.That(LiveCooldown(holder), Is.EqualTo(5f),
                    "After HookBus.Clear() the tracker must no longer react to basic hits.");
            }
            finally
            {
                if (source) UnityEngine.Object.DestroyImmediate(source);
                if (host) UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
