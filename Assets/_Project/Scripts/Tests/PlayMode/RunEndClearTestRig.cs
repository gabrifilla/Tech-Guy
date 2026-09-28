using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TechGuy.Tests
{
    /// <summary>
    /// Shared scaffolding for the R11 Property 26 (run-end leaves no residual state) test of
    /// modifier-synergies-theme17.
    ///
    /// The production run-end clear is <c>RunBoons.ClearRunState()</c>, invoked from
    /// <c>RunBoons.OnDestroy</c>. It is a single atomic operation that (1) calls
    /// <see cref="PlayerOnHitEffects.Clear"/> on the player's element registry — which cascades to
    /// <see cref="RunSynergyEffects.Clear"/> — (2) calls <see cref="HookBus.Clear"/> to drop every
    /// subscriber, and (3) calls <see cref="SystemBreakState.Reset"/> to zero the escalation tier.
    ///
    /// <para><b>Seam used to reach the clear.</b> <c>RunBoons</c> is a sealed <see cref="MonoBehaviour"/>
    /// whose <c>Awake</c>/<c>Start</c> stand up a whole run (instantiate a weapon copy, add
    /// <c>RunRewardUI</c>, require an equipped weapon or self-disable), and whose <c>ClearRunState</c>
    /// / <c>Hooks</c> / <c>_systemBreak</c> members are private. To exercise the <em>real</em> clear
    /// method without booting an entire run, this rig builds the <c>RunBoons</c> on an <b>inactive</b>
    /// GameObject (so no lifecycle callback runs), wires the private <c>_player</c>, <c>Hooks</c> and
    /// <c>_systemBreak</c> fields by reflection to a configured player host, and then invokes the
    /// private <c>ClearRunState</c> by reflection — the same method <c>OnDestroy</c> calls. This
    /// mirrors how the other rigs reach non-public members (e.g. <c>DetonationTestRig</c> invoking
    /// <c>BurnStatus.Tick</c>). No <c>FindObjectOfType</c>/<c>GameObject.Find</c>/magic strings are
    /// used; every reference is created and held explicitly here.</para>
    ///
    /// The player host carries a real <see cref="PlayerOnHitEffects"/> (and the
    /// <see cref="RunSynergyEffects"/> it lazily owns), which are real MonoBehaviour components with
    /// status/component lookups, so this test runs in PlayMode.
    /// </summary>
    public sealed class RunEndClearTestRig
    {
        private static readonly MethodInfo ClearRunStateMethod =
            typeof(RunBoons).GetMethod("ClearRunState", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PlayerField =
            typeof(RunBoons).GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo SystemBreakField =
            typeof(RunBoons).GetField("_systemBreak", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly PropertyInfo HooksProperty =
            typeof(RunBoons).GetProperty("Hooks", BindingFlags.Instance | BindingFlags.Public);

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public RunBoons Run { get; private set; }
        public PlayerActor Player { get; private set; }
        public PlayerOnHitEffects Effects { get; private set; }
        public RunSynergyEffects Synergies { get; private set; }
        public HookBus Hooks { get; private set; }
        public SystemBreakState SystemBreak { get; private set; }

        /// <summary>
        /// Builds the inactive RunBoons + player host graph and wires the private run-scoped members.
        /// Both GameObjects stay inactive for their whole lifetime so no Unity lifecycle callback runs;
        /// the rig drives only the clear method under test.
        /// </summary>
        public void Build()
        {
            if (ClearRunStateMethod == null) throw new InvalidOperationException("RunBoons.ClearRunState not found");
            if (PlayerField == null || SystemBreakField == null || HooksProperty == null)
                throw new InvalidOperationException("RunBoons private run-state members not found");

            // Player host: real PlayerActor (kept inactive so Awake never runs) that owns the element
            // registry ClearRunState reads via _player.TryGetComponent(out PlayerOnHitEffects).
            var playerGo = new GameObject("RunEndClearPlayer");
            playerGo.SetActive(false);
            _spawned.Add(playerGo);
            Player = playerGo.AddComponent<PlayerActor>();
            Effects = playerGo.AddComponent<PlayerOnHitEffects>();
            Synergies = Effects.Synergies; // lazily adds RunSynergyEffects to the same (inactive) GameObject

            // RunBoons host: inactive so Awake/Start do not boot a run; only ClearRunState is driven.
            var runGo = new GameObject("RunEndClearRunBoons");
            runGo.SetActive(false);
            _spawned.Add(runGo);
            Run = runGo.AddComponent<RunBoons>();

            Hooks = new HookBus();
            SystemBreak = new SystemBreakState();

            PlayerField.SetValue(Run, Player);
            HooksProperty.SetValue(Run, Hooks);
            SystemBreakField.SetValue(Run, SystemBreak);
        }

        /// <summary>Configures a burning + chilling element registry with synergy ranks so state exists to clear.</summary>
        public void ArmRunState(int detonation, int conductor, int reactor, int resonance)
        {
            Effects.EnableBurn(4f, 5f);
            Effects.EnableChill(0.5f, 5f, 1f);
            for (int i = 0; i < detonation; i++) Synergies.Add(RunSynergy.Detonation);
            for (int i = 0; i < conductor; i++) Synergies.Add(RunSynergy.Conductor);
            for (int i = 0; i < reactor; i++) Synergies.Add(RunSynergy.Reactor);
            for (int i = 0; i < resonance; i++) Synergies.Add(RunSynergy.Resonance);
        }

        /// <summary>Raises the escalation tier to a non-zero value by evaluating an interacting-modifier count.</summary>
        public void RaiseEscalationTier(int interactingCount) => SystemBreak.Evaluate(interactingCount);

        /// <summary>Invokes the production atomic clear (the method RunBoons.OnDestroy calls).</summary>
        public void InvokeRunEndClear() => ClearRunStateMethod.Invoke(Run, Array.Empty<object>());

        /// <summary>Destroys everything this rig created.</summary>
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go) UnityEngine.Object.DestroyImmediate(go);
            _spawned.Clear();
        }
    }
}
