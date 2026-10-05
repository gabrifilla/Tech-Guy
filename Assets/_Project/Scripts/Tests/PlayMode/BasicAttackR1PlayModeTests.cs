using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode integration tests for combat-foundation-rework — task 13.6 (Requirement 1:
    /// "Remoção do auto-combate persistente"). These exercise the <see cref="CharControlScript"/>
    /// WIRING of the pure <c>BasicAttackDriver</c> against a live player loop, a real
    /// <see cref="NavMeshAgent"/> on a baked mesh and a live enemy <see cref="Interactable"/>/
    /// <see cref="Actor"/>. Attacks are COUNTED through the real observable seam
    /// <c>CharControlScript.BasicAttackPerformed</c> (R1.8), never a stubbed decision helper.
    ///
    /// <para>
    /// The pure cadence math (tap = exactly one, hold = one per interval, no-input = zero) is already
    /// proven at the EditMode layer by <c>BasicAttackDriverPropertyTests</c> (task 7.2, Property P1).
    /// What these PlayMode tests add is the CharControlScript end: that merely owning a target never
    /// attacks (R1.1/R1.7/R1.11), that a Toque_de_Ataque drives exactly one <c>BasicAttackPerformed</c>
    /// through the follow/approach path (R1.1), that Segurar_Ataque repeats at the configured
    /// <c>attackInterval</c> (R1.3), that release stops future repeats without cutting a started hit
    /// (R1.4) while a tap's single execution is preserved (R1.5), that a Ataque_Direcional strikes in
    /// place without approaching or selecting a target (R1.2), and that an Ordem_de_Movimento outside
    /// the Zona_Morta_de_Movimento moves and clears the intent while a click inside the deadzone
    /// preserves it (R1.9/R1.10).
    /// </para>
    ///
    /// <para>
    /// <c>CharControlScript</c>'s real input comes from <c>GamePreferences</c>/mouse plus pointer
    /// raycasts, which cannot be simulated headlessly. Following the reflection pattern established by
    /// <see cref="KitingStepPlayModeTests"/>, these tests drive the observable seams directly: the
    /// private <c>BasicAttackDriver</c>, the private <c>target</c>/<c>attackInterval</c>/<c>playerBusy</c>
    /// fields, and the private <c>SetTarget</c>/<c>TryAttackTarget</c>/<c>MoveToPosition</c> methods. No
    /// production surface is widened for the test.
    /// </para>
    /// </summary>
    public sealed partial class BasicAttackR1PlayModeTests
    {
        [UnityTest]
        public IEnumerator GauntletPhase3AuthoredCadenceUsesCapturedProfileAndDuration()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.CadencedAsura);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var authoring = ScriptableObject.CreateInstance<GauntletAuthoring>();
            var source = ScriptableObject.CreateInstance<Ability>();
            try
            {
                var profile = CombatActionProfile.CadencedStrike(0);
                JsonUtility.FromJsonOverwrite("{\"startupEnd\":0.25,\"activeEnd\":0.6,\"impactEvents\":[{\"index\":0,\"at\":0.25,\"areaHitStepIndex\":0,\"hitStopProfileIndex\":-1}]}", profile);
                SetPrivate(source, "_combatProfile", profile);
                var r = authoring.FindContent(GauntletContentAuthoring.ForMutation(GauntletMutation.CadencedAsura).Id);
                JsonUtility.FromJsonOverwrite("{\"_overrideDuration\":true,\"_duration\":0.8,\"_overrideInterval\":true,\"_interval\":0.4,\"_overrideProfile\":true}", r);
                SetPrivate(r, "_profileSource", source); SetPrivate(combat, "_phase3Authoring", authoring);
                _player.mana = 500f; combat.AddAsuraEnergy(100);
                var enemy = BuildEnemy(Vector3.forward * 1.2f, 10000f).actor;
                int hits = 0; enemy.DamageReceived += (_, __) => hits++;
                Physics.SyncTransforms(); Assert.IsTrue(holder.TryUseAbility(3));
                Assert.AreEqual(.8f, combat.ExecutionSnapshot.ActiveTime);
                JsonUtility.FromJsonOverwrite("{\"_duration\":0.1}", r);
                // Drive the real scheduler on both sides of the captured boundary, without
                // depending on Editor frame duration or a coroutine wait overshooting it.
                var fixedTick = typeof(BreakerGauntletCombat).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                SetPrivate(combat, "_elapsed", .199f); fixedTick.Invoke(combat, null);
                Assert.AreEqual(0, hits, "captured .2 s impact must not emit early");
                SetPrivate(combat, "_elapsed", .2f); fixedTick.Invoke(combat, null);
                Assert.AreEqual(1, hits, "captured impact emits exactly once at its boundary");
                Assert.IsFalse(combat.IsCadencedWaiting);
                Assert.AreEqual(.8f, combat.ExecutionSnapshot.ActiveTime);
                float deadline = Time.realtimeSinceStartup + 2f;
                while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(combat.IsCadencedWaiting);
                Time.timeScale = 0f; yield return new WaitForSecondsRealtime(.5f);
                Assert.IsTrue(combat.IsCadencedWaiting); Time.timeScale = 1f;
                yield return new WaitForSeconds(.2f); Assert.IsTrue(combat.IsCadencedWaiting);
                yield return new WaitForSeconds(.25f); Assert.IsFalse(combat.IsExecuting);
                Assert.AreEqual(1, hits); Assert.AreEqual(0, combat.Energy);
            }
            finally { Time.timeScale = 1f; combat.FinishForDodge(); SetPrivate(combat, "_phase3Authoring", null); UnityEngine.Object.DestroyImmediate(authoring); UnityEngine.Object.DestroyImmediate(source); }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3AuthoredZeroChildDelaysKeepProducerFirst()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            _player.mana = 500f;
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.PressureField);
            foreach (var def in WeaponRunModifiers.Catalog) if (def.Kind == WeaponBoon.FlurryEcho) mods.Add(def);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var authoring = ScriptableObject.CreateInstance<GauntletAuthoring>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_overrideInterval\":true,\"_interval\":0}", authoring.FindContent(GauntletContentAuthoring.ForMutation(GauntletMutation.PressureField).Id));
                JsonUtility.FromJsonOverwrite("{\"_overrideDuration\":true,\"_duration\":0,\"_overrideInterval\":true,\"_interval\":0}", authoring.FindContent("weapon_FlurryEcho"));
                SetPrivate(combat, "_phase3Authoring", authoring);
                var enemy = BuildEnemy(Vector3.forward * 1.5f, 10000f).actor;
                bool producer = false; int echoes = 0, pulses = 0;
                enemy.DamageReceived += (_, __) => {
                    var context = _player.CurrentGauntletImpactContext;
                    if (context.Origin == GauntletImpactOrigin.Direct && context.ImpactIndex == context.Snapshot.LastDirectProducer) producer = true;
                    if (context.Origin == GauntletImpactOrigin.Echo) { Assert.IsTrue(producer); echoes++; }
                    if (context.Origin == GauntletImpactOrigin.Persistent) { Assert.IsTrue(producer); pulses++; }
                };
                Physics.SyncTransforms(); Assert.IsTrue(holder.TryUseAbility(1));
                float deadline = Time.realtimeSinceStartup + 4f;
                while ((combat.IsExecuting || combat.PendingChildCount > 0) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(producer); Assert.AreEqual(2, echoes); Assert.AreEqual(4, pulses); Assert.AreEqual(10, combat.Energy);
                Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
            }
            finally { combat.FinishForDodge(); SetPrivate(combat, "_phase3Authoring", null); UnityEngine.Object.DestroyImmediate(authoring); }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3SharedHudUsesFullRAvailabilityWithoutMechanicalSideEffects()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(); _player.mana = 500f;
            var run = _player.gameObject.AddComponent<RunBoons>();
            var hud = BuildSharedHud(); yield return null;
            run.RunWeapon.abilities[3].cooldownTime = 5f; // Exercise the configured cooldown gate without changing the shipped zero-CD asset.
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            int used = 0, rejected = 0; holder.AbilityUsed += _ => used++; holder.AbilityRejected += (_, __) => rejected++;
            yield return new WaitForSecondsRealtime(.07f);
            Assert.IsTrue(hud.IsAsuraVisible); StringAssert.Contains("0 / 100", hud.AsuraText);
            combat.AddAsuraEnergy(100); _player.mana = 0f;
            yield return new WaitForSecondsRealtime(.07f);
            StringAssert.Contains("CHEIO", hud.AsuraText); StringAssert.Contains("SEM MANA", hud.AsuraText);
            Assert.AreEqual("SEM MANA", hud.Slots[3].Status);
            for (int i = 0; i < 10; i++) Assert.AreEqual(AbilityUseFailure.NotEnoughMana, holder.GetGauntletRUnavailability());
            Assert.AreEqual(0, used); Assert.AreEqual(0, rejected); Assert.AreEqual(100, combat.Energy); Assert.AreEqual(0f, _player.mana);
            _player.mana = 500f; yield return new WaitForSecondsRealtime(.07f);
            Assert.AreEqual("ASURA PRONTO", hud.AsuraText);
            Time.timeScale = 0f; yield return new WaitForSecondsRealtime(.1f);
            StringAssert.Contains("BLOQUEADO", hud.AsuraText); Assert.AreEqual(100, combat.Energy);
            Time.timeScale = 1f; Assert.IsTrue(holder.TryUseAbility(0)); yield return new WaitForSecondsRealtime(.07f);
            StringAssert.Contains("AGUARDE", hud.AsuraText);
            combat.FinishForDodge(); holder.RefreshLoadout();
            Assert.IsTrue(holder.TryUseAbility(3)); Assert.AreEqual(2, used);
            combat.FinishForDodge(); yield return null; yield return null;
            float cooldownDeadline = Time.realtimeSinceStartup + 5f;
            while (holder.GetRemainingCooldown(3) <= 0f && Time.realtimeSinceStartup < cooldownDeadline) yield return null;
            combat.AddAsuraEnergy(100); yield return new WaitForSecondsRealtime(.07f);
            Assert.Greater(holder.GetRemainingCooldown(3), 0f); StringAssert.Contains("RECARGA", hud.AsuraText);
            Assert.AreEqual(0, rejected, "preview never publishes rejection");
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow")); holder.RefreshLoadout();
            yield return new WaitForSecondsRealtime(.07f);
            Assert.IsFalse(hud.IsAsuraVisible); Assert.AreEqual("", hud.FeedbackText);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3SharedHudGroupsEffectiveGainsShowsMutationsAndUnbinds()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(); _player.mana = 500f;
            var run = _player.gameObject.AddComponent<RunBoons>(); var hud = BuildSharedHud(); yield return null;
            Assert.IsTrue(run.ImportLegacyGauntletRanks(new[] {
                new WeaponRunModifiers.LegacyRankRecord("weapon_AsuraFist", 1),
                new WeaponRunModifiers.LegacyRankRecord("weapon_GuardBreaker", 1),
                new WeaponRunModifiers.LegacyRankRecord("weapon_MomentumStrike", 1),
                new WeaponRunModifiers.LegacyRankRecord("weapon_AsuraReserve", 1) }));
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            var reaction = enemy.gameObject.AddComponent<CombatReactionController>();
            reaction.ConfigureStance(EnemyRank.Normal, 30f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
            int hits = 0; run.Hooks.OnHit += (_, __) => hits++; Physics.SyncTransforms();
            for (int i = 0; i < 3; i++)
            {
                var root = combat.BeginBasicExecution(); Assert.AreEqual(1, ResolveGauntletTestBatch(combat, root, 0));
                combat.EnergyPolicy.Close(root);
            }
            Assert.AreEqual(3, hits); Assert.AreEqual(35, combat.Energy);
            StringAssert.Contains("+25 ASURA", hud.FeedbackText); StringAssert.Contains("BÁSICO", hud.FeedbackText);
            StringAssert.Contains("QUEBRA", hud.FeedbackText); StringAssert.Contains("PUNHO DE ASURA", hud.FeedbackText);
            StringAssert.Contains("GUARDA PARTIDA", hud.FeedbackText); StringAssert.Contains("ÍMPETO x3", hud.FeedbackText);
            yield return new WaitForSecondsRealtime(.3f); enemy.transform.position = Vector3.forward * 1.5f; Physics.SyncTransforms();
            combat.Configure(null); combat.Configure(run.RunWeapon); combat.AddAsuraEnergy(98);
            var capped = combat.BeginBasicExecution(); Assert.AreEqual(1, ResolveGauntletTestBatch(combat, capped, 0)); combat.EnergyPolicy.Close(capped);
            Assert.AreEqual(100, combat.Energy); StringAssert.Contains("+2 ASURA", hud.FeedbackText);
            foreach (GauntletMutation mutation in Enum.GetValues(typeof(GauntletMutation))) Assert.IsTrue(run.WeaponModifiers.GauntletMutations.Select(mutation));
            yield return new WaitForSecondsRealtime(.07f);
            for (int i = 0; i < 4; i++)
            {
                var label = (TMPro.TMP_Text)typeof(SkillSlotView).GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud.Slots[i]);
                Assert.AreEqual(GauntletContentAuthoring.MutationTitle((GauntletMutation)i).ToUpperInvariant(), label.text);
            }
            Assert.IsTrue(holder.TryUseAbility(3));
            StringAssert.Contains("ASURA CADENCIADA", hud.FeedbackText); StringAssert.Contains("reserva 20", hud.FeedbackText);
            float deadline = Time.realtimeSinceStartup + 2f;
            while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(combat.IsCadencedWaiting); yield return new WaitForSecondsRealtime(.07f);
            StringAssert.Contains("AGUARDA R", hud.AsuraText); Assert.AreEqual("AGUARDA R", hud.Slots[3].Status);
            int frozenHits = hits, energy = combat.Energy;
            Time.timeScale = 0f; yield return new WaitForSecondsRealtime(1.8f);
            Assert.AreEqual(energy, combat.Energy); Assert.AreEqual(frozenHits, hits); Assert.IsTrue(combat.IsCadencedWaiting);
            Assert.AreEqual("", hud.FeedbackText, "cosmetic unscaled expiry cannot change mechanical state");
            Time.timeScale = 1f; combat.FinishForDodge();
            hud.enabled = false; Assert.IsFalse(hud.IsAsuraVisible);
            combat.Configure(null); combat.Configure(run.RunWeapon); combat.AddAsuraEnergy(10);
            Assert.AreEqual("", hud.FeedbackText, "disabled HUD has no remaining subscriber");
            hud.enabled = true; yield return new WaitForSecondsRealtime(.07f);
            enemy.transform.position = Vector3.forward * 1.5f; Physics.SyncTransforms();
            var next = combat.BeginBasicExecution(); Assert.AreEqual(1, ResolveGauntletTestBatch(combat, next, 0)); combat.EnergyPolicy.Close(next);
            Assert.AreEqual(15, combat.Energy); StringAssert.Contains("+5 ASURA", hud.FeedbackText, "rebind observes once");
        }

        private PlayerHUD BuildSharedHud()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PlayerHUD.prefab");
            Assert.IsTrue(prefab);
            var instance = UnityEngine.Object.Instantiate(prefab); _spawned.Add(instance);
            var hud = instance.GetComponent<PlayerHUD>(); Assert.IsTrue(hud);
            hud.Configure(_player); SetPrivate(_controls, "_playerHUD", hud);
            return hud;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3RoomRewardPreservesConfirmedEnergyAndCategoryAcrossReequip()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var enemy = BuildEnemy(Vector3.forward * 1.2f, 10000f).actor;
            float hp = enemy.health; Physics.SyncTransforms();
            Assert.IsTrue(holder.TryUseAbility(1));
            float deadline = Time.realtimeSinceStartup + 5f;
            while (combat.IsExecuting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(combat.IsExecuting); Assert.Less(enemy.health, hp); Assert.Greater(combat.Energy, 0);
            var momentum = (AsuraMomentum)typeof(BreakerGauntletCombat).GetField("_momentum", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
            bool opposite = !((BreakerGauntletAbility)run.RunWeapon.abilities[1]).ShockSkill;
            Assert.IsTrue(momentum.IsAlternation(opposite));
            int energy = combat.Energy;
            run.OfferReward(2); Assert.AreEqual(energy, combat.Energy); Assert.IsTrue(momentum.IsAlternation(opposite));
            Assert.IsTrue(run.Choose(0)); Assert.AreEqual(energy, combat.Energy); Assert.IsTrue(momentum.IsAlternation(opposite));
            var inventory = run.WeaponModifiers;
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == WeaponBoon.AsuraReserve) Assert.IsTrue(inventory.Add(definition));
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow")); holder.RefreshLoadout();
            Assert.AreEqual(0, combat.Energy); Assert.IsFalse(momentum.IsAlternation(opposite));
            Assert.AreEqual(1, inventory.Rank(WeaponBoon.AsuraReserve));
            _player.EquipWeapon(run.RunWeapon); holder.RefreshLoadout();
            Assert.AreEqual(0, combat.Energy); Assert.AreEqual(1, inventory.Rank(WeaponBoon.AsuraReserve));
        }

        [UnityTest]
        public IEnumerator RunFocusDiscountsEffectiveQCostOnceForEachWeaponFamily()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            foreach (string key in new[] { "Weapons/Melee/Gauntlet/Gauntlet", "Weapons/Ranged/Bow_arrow/Bow", "Weapons/Melee/Spear/Spear" })
            {
                var source = Resources.Load<WeaponScript>(key); Assert.IsTrue(source);
                float sourceCost = source.abilities[0].ManaCost, sourceCooldown = source.abilities[0].cooldownTime;
                _player.EquipWeapon(source); holder.RefreshLoadout();
                var run = _player.gameObject.AddComponent<RunBoons>(); SetPrivate(run, "_rewriteRoll", (Func<double>)(() => 1d));
                yield return null;
                float cost = run.RunWeapon.abilities[0].ManaCost, cooldown = run.RunWeapon.abilities[0].cooldownTime;
                run.OfferReward(1); int index = -1;
                for (int i = 0; i < run.Choices.Count; i++) if (run.Choices[i].Id == "focus") index = i;
                Assert.GreaterOrEqual(index, 0); Assert.IsTrue(run.Choose(index));
                Assert.AreEqual(cost * .75f, run.RunWeapon.abilities[0].ManaCost, .0001f, key);
                Assert.AreEqual(cooldown * .65f, run.RunWeapon.abilities[0].cooldownTime, .0001f, key);
                Assert.AreEqual(sourceCost, source.abilities[0].ManaCost); Assert.AreEqual(sourceCooldown, source.abilities[0].cooldownTime);
                UnityEngine.Object.Destroy(run); yield return null;
            }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3LegacyImportRejectsMidCastSelectionAndReusesRunTrackers()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var source = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(source); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            var records = new[] {
                new WeaponRunModifiers.LegacyRankRecord("weapon_MomentumStrike", 1),
                new WeaponRunModifiers.LegacyRankRecord("weapon_HungryCombo", 1),
                new WeaponRunModifiers.LegacyRankRecord("weapon_GuardBreaker", 1),
                new WeaponRunModifiers.LegacyRankRecord("weapon_AsuraFist", 1),
                new WeaponRunModifiers.LegacyRankRecord("weapon_ShockRing", 3),
                new WeaponRunModifiers.LegacyRankRecord("weapon_ComboNova", 2),
                new WeaponRunModifiers.LegacyRankRecord("weapon_Shockwave", 2) };
            for (int cycle = 0; cycle < 2; cycle++)
            {
                var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
                Assert.IsTrue(run.ImportLegacyGauntletRanks(records));
                Assert.IsTrue(run.ImportLegacyGauntletRanks(records));
                Assert.AreEqual(records.Length, run.Acquired.Count);
                Assert.AreEqual(records.Length, run.WeaponModifiers.LegacySource.Count);
                Assert.AreEqual(1, _player.GetComponents<MomentumStacks>().Length);
                Assert.AreEqual(1, _player.GetComponents<HungryComboTracker>().Length);
                Assert.AreEqual(1, _player.GetComponents<ImpactGuardTracker>().Length);
                Assert.AreEqual(1, _player.GetComponents<AsuraSurge>().Length);
                Assert.AreEqual(1, _player.GetComponents<RunRewardUI>().Length);
                var combat = _player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(100);
                Assert.IsTrue(holder.TryUseAbility(3));
                Assert.IsTrue(run.ImportLegacyGauntletRanks(new[] { new WeaponRunModifiers.LegacyRankRecord("weapon_AsuraReserve", 3) }));
                Assert.AreEqual(0, combat.Energy, "acquisition during R cannot change the captured reserve transaction");
                Assert.IsTrue(combat.IsExecuting);
                run.OfferReward(cycle + 1);
                foreach (var offer in run.Choices)
                {
                    Assert.AreNotEqual("weapon_ShockRing", offer.Id);
                    Assert.AreNotEqual("weapon_ComboNova", offer.Id);
                    Assert.AreNotEqual("weapon_Shockwave", offer.Id);
                    Assert.AreNotEqual(GauntletContentAuthoring.ForMutation(GauntletMutation.Epicenter).Id, offer.Id);
                }
                int count = run.Acquired.Count;
                Assert.IsFalse(run.Choose(0), "a live cast cannot acquire another offensive shape");
                Assert.AreEqual(count, run.Acquired.Count); Assert.IsTrue(combat.IsExecuting);
                var inventory = run.WeaponModifiers;
                combat.Configure(null); combat.Configure(_player.CurrentWeapon);
                Assert.AreEqual(0, combat.Energy);
                Assert.AreEqual(3, inventory.Rank(WeaponBoon.ShockRing), "weapon reset preserves acquisitions");
                UnityEngine.Object.Destroy(run); yield return null;
                Assert.AreEqual(0, inventory.Rank(WeaponBoon.ShockRing)); Assert.AreEqual(0, inventory.LegacySource.Count);
                Assert.IsFalse(_player.GetComponent<MomentumStacks>().enabled);
                _player.EquipWeapon(source); holder.RefreshLoadout(); _player.mana = 500f;
            }
        }

        // gauntlet-2-rework task 14: real reward selection, explicit legacy Q replacement and run cleanup.
        [UnityTest]
        public IEnumerator GauntletPhase3RewardsOfferUniqueMutationsAndPreserveLegacyQUntilSelected()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var sourceWeapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(sourceWeapon); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var mods = run.WeaponModifiers;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (GauntletContentAuthoring.IsSelectedTechnique(definition.Kind))
                    for (int rank = 0; rank < 3; rank++) Assert.IsTrue(mods.Add(definition));
            var legacyQ = ScriptableObject.CreateInstance<ArsenalAbility>(); legacyQ.ConfigureRunTransformation(false);
            var oldQ = run.RunWeapon.abilities[0];
            run.RunWeapon.abilities[0] = legacyQ;
            var acquired = (List<RunBoons.Offer>)typeof(RunBoons).GetField("_acquired", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(run);
            acquired.Add(new RunBoons.Offer("transform", "Nova de impacto", "Q legado"));
            var owned = (List<ScriptableObject>)typeof(RunBoons).GetField("_owned", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(run);
            owned.Add(legacyQ); holder.RefreshLoadout();
            combat.AddAsuraEnergy(72);
            run.OfferReward(1);
            Assert.AreSame(legacyQ, run.RunWeapon.abilities[0], "opening rewards does not automatically migrate Q");
            Assert.AreEqual(72, combat.Energy, "room transition preserves energy");
            int focus = -1;
            for (int i = 0; i < run.Choices.Count; i++) if (run.Choices[i].Id == "focus") focus = i;
            Assert.GreaterOrEqual(focus, 0); Assert.IsTrue(run.Choose(focus));
            Assert.AreSame(legacyQ, run.RunWeapon.abilities[0]);
            var selected = new HashSet<GauntletMutation>();
            for (int room = 2; room <= 5; room++)
            {
                run.OfferReward(room);
                int choice = -1;
                for (int i = 0; i < run.Choices.Count; i++)
                {
                    var offer = run.Choices[i];
                    Assert.AreNotEqual("transform", offer.Id);
                    Assert.IsFalse(offer.Id.StartsWith("weapon_"), "max-rank Techniques and legacy boons are absent");
                    if (!GauntletContentAuthoring.TryMutation(offer.Id, out var mutation)) continue;
                    Assert.IsFalse(selected.Contains(mutation));
                    if (mutation == GauntletMutation.DemolishingAdvance)
                        StringAssert.Contains("SUBSTITUI EXPLICITAMENTE", offer.Description);
                    choice = i;
                }
                Assert.GreaterOrEqual(choice, 0, "remaining mutation occupies the family reward choice");
                var id = run.Choices[choice].Id; GauntletContentAuthoring.TryMutation(id, out var chosen);
                Assert.IsTrue(run.Choose(choice)); Assert.IsTrue(selected.Add(chosen));
                Assert.AreEqual(1, RunModifierPresentation.Count(run, id));
                Assert.AreEqual(72, combat.Energy);
            }
            Assert.AreEqual(4, selected.Count);
            Assert.AreSame(oldQ, run.RunWeapon.abilities[0]); Assert.IsTrue(legacyQ);
            Assert.AreEqual(sourceWeapon.abilities[0].ManaCost * .75f, oldQ.ManaCost, .0001f);
            Assert.IsTrue(acquired.Exists(offer => offer.Id == "transform"));
            run.OfferReward(6);
            foreach (var offer in run.Choices) Assert.IsFalse(GauntletContentAuthoring.TryMutation(offer.Id, out _));
            UnityEngine.Object.Destroy(run); yield return null;
            Assert.AreEqual(0, combat.Energy); Assert.AreEqual(0, mods.Rank(WeaponBoon.AsuraReserve));
            foreach (GauntletMutation mutation in Enum.GetValues(typeof(GauntletMutation)))
                Assert.IsFalse(mods.GauntletMutations.TryGet(GauntletMutationSelection.SlotOf(mutation), out _));
            Assert.AreEqual(0, acquired.Count);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3CadencedWaitPreflightsLegacyQBeforeCanceling()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.CadencedAsura);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var legacyQ = ScriptableObject.CreateInstance<ArsenalAbility>();
            legacyQ.ConfigureRunPresentation("legacy Q", 10f, legacyQ.Glyph);
            try
            {
                _player.CurrentWeapon.abilities[0] = legacyQ; holder.RefreshLoadout();
                var combat = _player.GetComponent<BreakerGauntletCombat>(); var arsenal = _player.GetComponent<ArsenalCombat>();
                combat.AddAsuraEnergy(100); Assert.IsTrue(holder.TryUseAbility(3));
                float deadline = Time.realtimeSinceStartup + 3f;
                while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(combat.IsCadencedWaiting); arsenal.enabled = false;
                float mana = _player.mana;
                Assert.IsFalse(holder.TryUseAbility(0)); Assert.IsTrue(combat.IsCadencedWaiting); Assert.AreEqual(mana, _player.mana);
                arsenal.enabled = true; Assert.IsTrue(holder.TryUseAbility(0));
                Assert.IsFalse(combat.IsExecuting); Assert.IsTrue(arsenal.IsExecuting); arsenal.Cancel();
                Assert.AreEqual(mana - 10f, _player.mana, .01f); Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
            }
            finally { UnityEngine.Object.DestroyImmediate(legacyQ); }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3CadencedDashWindowAndWaitCleanup()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var cameraObject = new GameObject("Cadenced dash camera"); _spawned.Add(cameraObject);
            cameraObject.tag = "MainCamera"; cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(0f, 10f, -10f); cameraObject.transform.LookAt(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f); _player.RefreshResourceStats();
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.CadencedAsura);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var dash = ScriptableObject.CreateInstance<DashScript>();
            dash.dashTime = .05f; dash.dashVelocity = 0f; dash.dashAnimation = string.Empty;
            SetPrivate(dash, "_iframeWindow", 0f); dash.ConfigureRunPresentation("Cadenced dash", 10f, dash.Glyph);
            try
            {
                _player.mana = 500f; combat.AddAsuraEnergy(100); Assert.IsTrue(holder.TryUseAbility(3));
                SetPrivate(combat, "_elapsed", .309f); Assert.IsFalse(combat.CanDodgeCancel);
                Assert.IsFalse(holder.TryUseDash(dash)); Assert.IsTrue(combat.IsExecuting);
                SetPrivate(combat, "_elapsed", .311f); Assert.IsTrue(combat.CanDodgeCancel);
                typeof(AbilityHolder).GetMethod("TickDashBuffer", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(holder, null);
                Assert.IsFalse(combat.IsExecuting); Assert.AreEqual(0, combat.PendingChildCount);
                yield return new WaitForSeconds(.1f);
                holder.RefreshLoadout(); _player.mana = 500f; combat.AddAsuraEnergy(100); Assert.IsTrue(holder.TryUseAbility(3));
                float deadline = Time.realtimeSinceStartup + 3f;
                while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(combat.IsCadencedWaiting);
                _player.mana = 0f; Assert.IsFalse(holder.TryUseDash(dash)); Assert.IsTrue(combat.IsCadencedWaiting);
                _player.mana = 500f;
                // Make availability deterministic independently of the previous dash's authored cooldown.
                var nextReady = (System.Collections.Generic.Dictionary<GameObject, float>)typeof(DashScript).GetField("nextReadyTimes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(dash);
                nextReady[_player.gameObject] = Time.time;
                Assert.IsTrue(holder.TryUseDash(dash)); Assert.IsFalse(combat.IsExecuting);
                Assert.AreEqual(490f, _player.mana); Assert.AreEqual(0, combat.PendingChildCount);
                yield return new WaitForSeconds(.1f);
            }
            finally { UnityEngine.Object.DestroyImmediate(dash); }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3CadencedTeardownDiscardsActiveAndWaitingContinuations()
        {
            Assert.IsTrue(TryBakeFullFloor());
            for (int scenario = 0; scenario < 6; scenario++)
            {
                if (_player) UnityEngine.Object.DestroyImmediate(_player.gameObject);
                if (_weapon) UnityEngine.Object.DestroyImmediate(_weapon);
                BuildPlayer(Vector3.zero);
                var holder = _player.gameObject.AddComponent<AbilityHolder>();
                _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
                var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
                SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.CadencedAsura);
                typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
                var combat = _player.GetComponent<BreakerGauntletCombat>();
                var source = new GameObject("other mitigation owner"); _spawned.Add(source);
                _player.SetDamageTakenMultiplier(source, .5f);
                combat.AddAsuraEnergy(100); Assert.IsTrue(holder.TryUseAbility(3)); long execution = combat.CadencedExecution;
                if (scenario >= 3)
                {
                    float deadline = Time.realtimeSinceStartup + 3f;
                    while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.IsTrue(combat.IsCadencedWaiting);
                }
                else { SetPrivate(combat, "_elapsed", .32f); Assert.IsTrue(holder.TryUseAbility(3)); }
                if (scenario % 3 == 0) _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow"));
                else if (scenario % 3 == 1) combat.enabled = false;
                else _player.TakeDamage(_player.health * 100f);
                Assert.IsFalse(combat.IsExecuting); Assert.IsFalse(combat.EnergyPolicy.IsAsuraActive);
                Assert.IsFalse(combat.TryContinueCadenced(execution)); Assert.AreEqual(0, combat.PendingChildCount);
                Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
                Assert.IsFalse((bool)typeof(BreakerGauntletCombat).GetField("_agentLocked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat));
                if (_agent.enabled && _agent.isOnNavMesh) Assert.IsFalse(_agent.isStopped);
                if (!_player.IsDead)
                { float hp = _player.health; _player.TakeDamage(10f); Assert.AreEqual(5f, hp - _player.health, .01f); }
                yield return null;
                Assert.IsFalse(combat.IsExecuting);
            }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3CadencedBoundaryTimeoutAndRecoveryBuffer()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f); _player.RefreshResourceStats();
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.CadencedAsura);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            for (int boundary = 0; boundary < 3; boundary++)
            {
                holder.RefreshLoadout(); _player.mana = 500f; combat.AddAsuraEnergy(100);
                Assert.IsTrue(holder.TryUseAbility(3)); float mana = _player.mana;
                float deadline = Time.realtimeSinceStartup + 3f;
                while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(combat.IsCadencedWaiting);
                SetPrivate(combat, "_cadencedWaitUntil", combat.CadencedNow + (boundary == 0 ? .01f : boundary == 1 ? 0f : -.01f));
                Assert.AreEqual(boundary < 2, holder.TryUseAbility(3));
                Assert.AreEqual(mana, _player.mana); Assert.AreEqual(0, combat.Energy);
                if (boundary < 2) Assert.IsTrue(combat.IsExecuting); else Assert.IsFalse(combat.IsExecuting);
                combat.FinishForDodge(); Assert.AreEqual(0, combat.PendingChildCount);
            }
            holder.RefreshLoadout(); _player.mana = 500f; combat.AddAsuraEnergy(100);
            Assert.IsTrue(holder.TryUseAbility(3));
            SetPrivate(combat, "_elapsed", .32f);
            Assert.IsTrue(holder.TryUseAbility(3), "new recovery press uses the existing single-slot buffer");
            Time.timeScale = 0f; yield return new WaitForSecondsRealtime(.2f);
            Assert.IsTrue(combat.IsExecuting); Time.timeScale = 1f;
            // Exercise the real runner's tiny positive scale, not just the menu pause's zero.
            using (_player.BeginImpactFeedback(0f)) { }
            var hitStop = _player.GetComponent<HitStopRunner>(); hitStop.Apply(.2f);
            float clock = combat.CadencedNow;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.IsTrue(hitStop.IsActive); Assert.AreEqual(clock, combat.CadencedNow);
            while (hitStop.IsActive) yield return null;
            float end = Time.realtimeSinceStartup + 3f;
            while ((!combat.IsCadencedWaiting || (int)typeof(BreakerGauntletCombat).GetField("_cadencedStrike", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat) != 1)
                && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsTrue(combat.IsCadencedWaiting); Assert.AreEqual(1, typeof(BreakerGauntletCombat).GetField("_cadencedStrike", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat));
            Assert.AreEqual(0, combat.PendingChildCount);
            // No further press: holding/absence of input never manufactures the third producer.
            yield return new WaitForSeconds(.85f);
            Assert.IsFalse(combat.IsExecuting); Assert.AreEqual(0, combat.PendingChildCount);
            Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3CadencedAsuraUsesThreePressesAndOneCost()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(); _player.mana = 500f; _player.Stats.criticalChance = 0f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.CadencedAsura);
            foreach (var def in WeaponRunModifiers.Catalog)
            {
                if (def.Kind == WeaponBoon.AsuraReserve) { mods.Add(def); mods.Add(def); }
                if (def.Kind == WeaponBoon.AsuraEcho) mods.Add(def);
            }
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var enemy = BuildEnemy(Vector3.forward * 1.2f).actor; enemy.health = 10000f;
            var indices = new System.Collections.Generic.List<int>(); int uses = 0, echoes = 0;
            holder.AbilityUsed += slot => { if (slot == 3) uses++; };
            enemy.DamageReceived += (_, __) => {
                if (_player.CurrentGauntletImpactContext?.Origin == GauntletImpactOrigin.Direct)
                {
                    int index = _player.CurrentGauntletImpactContext.ImpactIndex;
                    indices.Add(index);
                    if (index == 2) Assert.AreEqual(1, combat.PendingChildCount, "rank 1 produces one echo, only on the performed third strike");
                }
                else if (_player.CurrentGauntletImpactContext?.Origin == GauntletImpactOrigin.Echo) echoes++;
            };
            Physics.SyncTransforms(); combat.AddAsuraEnergy(100);
            float mana = _player.mana, cost = holder.ActiveAbilities[3].ManaCost;
            Assert.IsTrue(holder.TryUseAbility(3)); Assert.IsFalse(holder.TryUseAbility(3), "startup is committed");
            float hp = _player.health; _player.TakeDamage(10f); Assert.AreEqual(1.5f, hp - _player.health, .01f);
            for (int strike = 0; strike < 2; strike++)
            {
                float deadline = Time.realtimeSinceStartup + 3f;
                while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(combat.IsCadencedWaiting); Assert.AreEqual(strike + 1, indices.Count);
                Assert.AreEqual(0, echoes); Assert.AreEqual(0, combat.PendingChildCount);
                Assert.IsTrue(holder.MovementAllowedWhileCasting); Assert.IsFalse(_agent.isStopped);
                hp = _player.health; _player.TakeDamage(10f); Assert.AreEqual(10f, hp - _player.health, .01f);
                combat.AddAsuraEnergy(10); Assert.AreEqual(40, combat.Energy, "R wait still blocks energy");
                Time.timeScale = 0f; yield return new WaitForSecondsRealtime(.15f);
                Assert.IsTrue(combat.IsCadencedWaiting); Assert.IsFalse(holder.TryUseAbility(3)); Time.timeScale = 1f;
                Assert.IsTrue(holder.TryUseAbility(3)); Assert.IsFalse(combat.IsCadencedWaiting);
                Assert.AreEqual(mana - cost, _player.mana, .01f);
            }
            float end = Time.realtimeSinceStartup + 3f;
            while (combat.IsExecuting && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsFalse(combat.IsExecuting); CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indices);
            Assert.AreEqual(1, uses); Assert.AreEqual(mana - cost, _player.mana, .01f);
            Assert.AreEqual(1, echoes); Assert.AreEqual(40, combat.Energy);
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(0, combat.PendingChildCount); Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3CadencedWaitRejectsUnavailableSkillsAndAcceptsQWEOrBasic()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats();
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.CadencedAsura);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var enemy = BuildEnemy(Vector3.forward * 1.2f).actor; enemy.health = 10000f;
            for (int destination = -1; destination < 3; destination++)
            {
                _agent.Warp(Vector3.zero); _player.transform.rotation = Quaternion.identity; Physics.SyncTransforms();
                enemy.transform.position = Vector3.forward * 1.2f; Physics.SyncTransforms();
                holder.RefreshLoadout(); _player.mana = 500f; combat.AddAsuraEnergy(100);
                Assert.IsTrue(holder.TryUseAbility(3));
                float deadline = Time.realtimeSinceStartup + 3f;
                while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(combat.IsCadencedWaiting); long execution = combat.CadencedExecution;
                if (destination < 0)
                {
                    SetPrivate(_controls, "playerBusy", true); Assert.IsFalse(_controls.TryDirectionalBasicAttack(Vector3.forward * 2f));
                    Assert.IsTrue(combat.IsCadencedWaiting); SetPrivate(_controls, "playerBusy", false);
                    Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward * 2f)); Assert.IsFalse(combat.IsExecuting);
                    Assert.AreEqual(3, combat.Energy, "accepted basic earns energy after ending R");
                }
                else
                {
                    _player.mana = 0f; Assert.IsFalse(holder.TryUseAbility(destination)); Assert.IsTrue(combat.IsCadencedWaiting);
                    Assert.AreEqual(execution, combat.CadencedExecution);
                    _player.mana = 500f; Assert.IsTrue(holder.TryUseAbility(destination));
                    if (destination == 0) { enemy.transform.position = Vector3.forward * 3.5f; Physics.SyncTransforms(); }
                    Assert.IsFalse(combat.IsCadencedAsura); Assert.IsTrue(combat.IsExecuting);
                    Assert.IsFalse(combat.TryContinueCadenced(execution), "stale continuation must not cancel the new committed action");
                    Assert.IsTrue(combat.IsExecuting);
                    deadline = Time.realtimeSinceStartup + 3f;
                    while (combat.Energy == 0 && combat.IsExecuting && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.Greater(combat.Energy, 0, $"accepted slot {destination} earns energy after ending R"); combat.FinishForDodge();
                }
                Assert.IsFalse(combat.IsCadencedAsura); Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
                yield return new WaitForSeconds(.4f);
            }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3DemolishingTraversalRestoresOnSwapDisableAndDeath()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var weapon = _player.CurrentWeapon; var combat = _player.GetComponent<BreakerGauntletCombat>();
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats();
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.DemolishingAdvance);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var avoidance = _agent.obstacleAvoidanceType;
            var collider = _player.gameObject.AddComponent<CapsuleCollider>(); collider.center = Vector3.up;
            for (int teardown = 0; teardown < 3; teardown++)
            {
                try
                {
                combat.enabled = true; _player.EquipWeapon(weapon); combat.Configure(weapon);
                holder.RefreshLoadout(); _player.mana = 500f; _agent.Warp(Vector3.zero);
                Assert.IsTrue(holder.TryUseAbility(0));
                Assert.IsTrue(collider.enabled);
                if (teardown == 0) _player.EquipWeapon(_weapon);
                else if (teardown == 1) combat.enabled = false;
                else _player.TakeDamage(_player.health + 1f);
                Assert.IsFalse(combat.IsExecuting); Assert.AreEqual(avoidance, _agent.obstacleAvoidanceType);
                Assert.IsTrue(collider.enabled); Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
                }
                catch (System.Exception exception) { Assert.Fail($"teardown {teardown}: {exception}"); }
            }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3DemolishingAdvanceTraversesBodiesAndLaunchesOnlyOnBreak()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var weapon = _player.CurrentWeapon; var combat = _player.GetComponent<BreakerGauntletCombat>();
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(); _player.mana = 500f; _player.Stats.criticalChance = 0f;
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            mods.GauntletMutations.Select(GauntletMutation.DemolishingAdvance);
            foreach (var def in WeaponRunModifiers.Catalog) if (def.Kind == WeaponBoon.RocketAdvance || def.Kind == WeaponBoon.SeismicFist)
                for (int i = 0; i < 3; i++) mods.Add(def);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var body = BuildEnemy(Vector3.forward * 1.2f).actor;
            var bodyAgent = body.gameObject.AddComponent<NavMeshAgent>(); bodyAgent.Warp(body.transform.position); bodyAgent.isStopped = true;
            body.DamageReceived += (_, __) => Assert.GreaterOrEqual(_player.transform.position.z, 7.8f,
                $"uppercut emitted before validated advance; player {_player.transform.position}, next {_agent.nextPosition}, body {body.transform.position}");
            var hurtbox = _player.gameObject.AddComponent<CapsuleCollider>(); hurtbox.center = Vector3.up; hurtbox.height = 2f;
            var common = BuildEnemy(Vector3.forward * 9.5f - Vector3.right * .2f).actor;
            var boss = BuildEnemy(Vector3.forward * 9.5f + Vector3.right * .2f).actor;
            var heavy = BuildEnemy(Vector3.forward * 9.5f).actor;
            var heavyReaction = heavy.gameObject.AddComponent<CombatReactionController>();
            heavyReaction.ConfigureStance(EnemyRank.Normal, 10f, 1f, 0f, 0f, .5f, .25f, .25f, .25f);
            heavyReaction.ConfigureReaction(new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob));
            bool heavyLaunched = false;
            heavyReaction.StanceBroken += _ => {
                var state = (PostureReactionState)typeof(CombatReactionController).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(heavyReaction);
                heavyLaunched = state.ActiveMotion == StanceBreakEffect.KnockUp;
            };
            var a = common.gameObject.AddComponent<CombatReactionController>();
            var b = boss.gameObject.AddComponent<CombatReactionController>();
            a.ConfigureStance(EnemyRank.Normal, 10f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
            a.ConfigureReaction(new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob));
            b.ConfigureStance(EnemyRank.Boss, 10f, 1f, 0f, 0f, 1f, 1f, 1f, 1f);
            b.ConfigureReaction(new ReactionConfiguration(EnemyRank.Boss, ReactionCategory.Boss));
            bool launched = false, bossOpened = false; int commonHits = 0, bossHits = 0;
            a.StanceBroken += _ => {
                launched |= a.IsStunned;
                var state = (PostureReactionState)typeof(CombatReactionController).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(a);
                Assert.AreEqual(StanceBreakEffect.KnockUp, state.ActiveMotion);
            };
            b.StanceBroken += _ => { bossOpened = true; Assert.IsFalse(b.IsStunned); };
            common.DamageReceived += (_, __) => commonHits++; boss.DamageReceived += (_, __) => bossHits++;
            var q = (BreakerGauntletAbility)holder.ActiveAbilities[0]; float multiplier = 0f, bonus = 0f;
            foreach (var step in q.HitSteps) { multiplier += step.damageMultiplier; bonus += step.bonusDamage; }
            var expected = _player.Stats.RollAttackDamage(_player.Stats.CaptureAttackDamage(weapon.attackDamage, multiplier, bonus)).Amount;
            var avoidance = _agent.obstacleAvoidanceType; float bodyHp = body.health;
            Physics.SyncTransforms(); Assert.IsTrue(holder.TryUseAbility(0));
            float health = _player.health; _player.TakeDamage(10f); Assert.AreEqual(10f, health - _player.health, .001f);
            Assert.IsTrue(hurtbox.enabled);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (combat.IsExecuting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(combat.IsExecuting); Assert.AreEqual(8f, _player.transform.position.z, .2f);
            Assert.AreEqual(bodyHp, body.health, $"no trajectory damage; body position {body.transform.position}, player {_player.transform.position}"); Assert.IsTrue(body.GetComponent<Collider>().enabled);
            Assert.AreEqual(1, commonHits); Assert.AreEqual(1, bossHits);
            Assert.AreEqual(1000f - expected, common.health, .01f); Assert.AreEqual(1000f - expected, boss.health, .01f);
            Assert.IsTrue(launched); Assert.IsTrue(bossOpened); Assert.AreEqual(50, combat.Energy);
            Assert.IsTrue(heavyLaunched); Assert.AreEqual(1000f - expected, heavy.health, .01f);
            Assert.AreEqual(avoidance, _agent.obstacleAvoidanceType); Assert.IsTrue(hurtbox.enabled);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3DemolishingAdvanceStopsAtSolidAndRestoresOnCancel()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.DemolishingAdvance);
            foreach (var def in WeaponRunModifiers.Catalog) if (def.Kind == WeaponBoon.RocketAdvance)
                for (int i = 0; i < 3; i++) mods.Add(def);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var avoidance = _agent.obstacleAvoidanceType;
            var wall = new GameObject("Mutation Q solid"); _spawned.Add(wall); wall.transform.position = new Vector3(0f, 1f, 3f);
            wall.AddComponent<BoxCollider>().size = new Vector3(10f, 3f, .5f); Physics.SyncTransforms();
            Assert.IsTrue(holder.TryUseAbility(0)); float deadline = Time.realtimeSinceStartup + 4f;
            while (combat.IsExecuting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(combat.IsExecuting); Assert.Less(_player.transform.position.z, 2.76f);
            Assert.IsTrue(_agent.isOnNavMesh); Assert.AreEqual(0, combat.Energy); Assert.AreEqual(avoidance, _agent.obstacleAvoidanceType);
            _agent.Warp(Vector3.zero); holder.RefreshLoadout(); _player.mana = 500f;
            Assert.IsTrue(holder.TryUseAbility(0)); combat.FinishForDodge();
            Assert.IsFalse(combat.IsExecuting); Assert.AreEqual(avoidance, _agent.obstacleAvoidanceType);
            Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3EpicenterIsCenteredRadialWithThreeBreakEventsButTwoRewards()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            _player.Stats.criticalChance = 0f;
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.Epicenter);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var targets = new List<Actor>(); int breaks = 0;
            foreach (var position in new[] { Vector3.left * 1.1f, Vector3.right * 1.1f, Vector3.back * 1.1f })
            {
                var actor = BuildEnemy(position).actor; targets.Add(actor);
                var reaction = actor.gameObject.AddComponent<CombatReactionController>();
                reaction.ConfigureStance(EnemyRank.Normal, 1f, 1f, 0f, 10f, 0f, 0f, 0f, 0f);
                reaction.StanceBroken += _ => breaks++;
            }
            var combat = _player.GetComponent<BreakerGauntletCombat>(); Physics.SyncTransforms();
            Assert.IsTrue(holder.TryUseAbility(2)); float deadline = Time.realtimeSinceStartup + 4f;
            while (combat.IsExecuting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(combat.IsExecuting); Assert.AreEqual(3, breaks); Assert.AreEqual(50, combat.Energy);
            foreach (var actor in targets) Assert.Less(actor.health, 1000f, "radial impact must reach sides and rear");
            Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3PressureFieldHasFourDistinctPulsesWithoutEchoReproduction()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(); _player.mana = 500f; _player.Stats.criticalChance = 0f;
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            mods.GauntletMutations.Select(GauntletMutation.PressureField);
            foreach (var def in WeaponRunModifiers.Catalog) if (def.Kind == WeaponBoon.FlurryEcho) { mods.Add(def); mods.Add(def); }
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var authoredW = (BreakerGauntletAbility)holder.ActiveAbilities[1];
            authoredW.HitSteps[authoredW.HitSteps.Count - 1].bonusDamage = 10f; // Owned runtime clone, never the resource asset.
            var enemy = BuildEnemy(Vector3.forward * 1.5f, 10000f).actor;
            var reaction = enemy.gameObject.AddComponent<CombatReactionController>();
            reaction.ConfigureStance(EnemyRank.Normal, 1000f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
            int echoes = 0, pulses = 0; float producerDamage = 0f; var indices = new HashSet<int>();
            float expectedPosture = 0f;
            Action<Actor, float> observePressure = (_, damage) => {
                var ctx = _player.CurrentGauntletImpactContext;
                if (ctx.Origin == GauntletImpactOrigin.Direct && ctx.ImpactIndex == ctx.Snapshot.LastDirectProducer)
                {
                    producerDamage = damage;
                    foreach (var step in ctx.Snapshot.CopySteps()) expectedPosture += step.stanceDamage;
                    expectedPosture += ctx.Snapshot.CopySteps()[ctx.Snapshot.LastDirectProducer].stanceDamage;
                    Assert.AreEqual(5, combat.PendingChildCount); combat.FinishForDodge();
                    _agent.Warp(Vector3.right * 8f); // The field and echoes remain at their producer's spatial snapshot.
                }
                if (ctx.Origin == GauntletImpactOrigin.Echo) { echoes++; Assert.AreEqual(producerDamage * .55f, damage, .01f); }
                if (ctx.Origin == GauntletImpactOrigin.Persistent)
                {
                    pulses++; Assert.IsTrue(indices.Add(ctx.ImpactIndex)); Assert.IsFalse(ctx.EnergyEligible);
                    Assert.AreEqual(producerDamage * .25f, damage, .01f);
                    Assert.AreEqual(GauntletContentAuthoring.ForMutation(GauntletMutation.PressureField).Id, ctx.ProducerId);
                    Assert.IsFalse(combat.IsExecuting);
                }
            };
            // Phase 5 keeps native hooks Direct-only; typed receipts retain every child assertion.
            run.Hooks.OnHit += observePressure;
            run.Hooks.OnProtocolImpact += impact => { if (impact.Producer != ProtocolProducer.Direct) observePressure(null, impact.Damage); };
            Physics.SyncTransforms(); Assert.IsTrue(holder.TryUseAbility(1));
            float deadline = Time.realtimeSinceStartup + 6f;
            while ((combat.IsExecuting || combat.PendingChildCount > 0) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(4, echoes); Assert.AreEqual(4, pulses); Assert.AreEqual(10, combat.Energy);
            Assert.AreEqual(1000f - expectedPosture, reaction.CurrentStance, .01f);
            Assert.AreEqual(0, combat.PendingChildCount); Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
            Assert.AreEqual(500f - holder.ActiveAbilities[1].ManaCost, _player.mana, .001f);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3GuardPreservesBossImmunityAndLethalFirstConfirmation()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(weapon); holder.RefreshLoadout();
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var run = _player.gameObject.AddComponent<RunBoons>(); var hooks = new HookBus();
            typeof(RunBoons).GetProperty("Hooks").SetValue(run, hooks); SetPrivate(run, "_weapon", weapon);
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            foreach (var def in WeaponRunModifiers.Catalog) if (def.Kind == WeaponBoon.GuardBreaker || def.Kind == WeaponBoon.SeismicFist)
                for (int i = 0; i < 3; i++) mods.Add(def);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var tracker = _player.gameObject.AddComponent<ImpactGuardTracker>(); tracker.Configure(_player, hooks, holder, 3);
            var boss = BuildEnemy(Vector3.forward * 1.5f).actor;
            var reaction = boss.gameObject.AddComponent<CombatReactionController>();
            reaction.ConfigureStance(EnemyRank.Boss, 30f, 1f, 0f, 0f, 1f, 1f, 1f, 1f);
            reaction.ConfigureReaction(new ReactionConfiguration(EnemyRank.Boss, ReactionCategory.SmallMob));
            int breaks = 0; reaction.StanceBroken += _ => { breaks++; Assert.IsFalse(reaction.IsStunned); };
            Physics.SyncTransforms();
            for (int hit = 0; hit < 3; hit++)
            {
                var root = combat.BeginBasicExecution(); Assert.AreEqual(1, ResolveGauntletTestBatch(combat, root, 0));
                combat.EnergyPolicy.Close(root);
            }
            Assert.AreEqual(1, breaks); Assert.AreEqual(29, combat.Energy); Assert.IsFalse(reaction.IsStunned);
            UnityEngine.Object.DestroyImmediate(boss.gameObject);
            combat.Configure(null); combat.Configure(weapon); tracker.enabled = false; tracker.enabled = true;
            var victim = BuildEnemy(Vector3.forward * 1.5f).actor;
            var immune = BuildEnemy(Vector3.forward * 1.5f + Vector3.right * .3f).actor;
            immune.AddDamageImmunity(tracker);
            var otherReaction = immune.gameObject.AddComponent<CombatReactionController>();
            otherReaction.ConfigureStance(EnemyRank.Normal, 1000f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
            Physics.SyncTransforms();
            for (int hit = 0; hit < 3; hit++)
            {
                if (hit == 2) victim.health = 1f;
                var root = combat.BeginBasicExecution(); Assert.AreEqual(1, ResolveGauntletTestBatch(combat, root, 0));
                combat.EnergyPolicy.Close(root);
            }
            Assert.IsTrue(victim.IsDead); Assert.AreEqual(0, tracker.Streak);
            Assert.AreEqual(1000f, otherReaction.CurrentStance, "the lethal HP target must not redirect Guard to another target");
            Assert.AreEqual(9, combat.Energy);
            var empty = combat.BeginBasicExecution();
            UnityEngine.Object.DestroyImmediate(victim.gameObject);
            Assert.AreEqual(0, ResolveGauntletTestBatch(combat, empty, 0)); Assert.AreEqual(0, tracker.Streak);
            combat.EnergyPolicy.Close(empty); Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
            yield break;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3LegacyShockwaveKeepsItsEffectEventsAndDistinctProducer()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            foreach (var def in WeaponRunModifiers.Catalog)
            { if (def.Kind == WeaponBoon.FlurryEcho) { mods.Add(def); mods.Add(def); }
              if (def.Kind == WeaponBoon.Shockwave) mods.Add(def); }
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            var enemy = BuildEnemy(Vector3.forward * 1.5f, 10000f).actor;
            int waves = 0, echoes = 0; float echoDamage = 0f;
            run.Hooks.OnHit += (_, damage) => {
                var ctx = _player.CurrentGauntletImpactContext;
                if (ctx.Origin == GauntletImpactOrigin.Echo) { echoes++; echoDamage = damage; }
                if (ctx.ProducerId == WeaponRunModifiers.CatalogId(WeaponBoon.Shockwave))
                {
                    waves++; Assert.AreEqual(GauntletImpactOrigin.Technique, ctx.Origin);
                    Assert.IsFalse(ctx.EnergyEligible); Assert.AreEqual(echoDamage * .6f, damage, .01f);
                }
            };
            _player.Stats.criticalChance = 0f; Physics.SyncTransforms(); Assert.IsTrue(holder.TryUseAbility(1));
            float deadline = Time.realtimeSinceStartup + 5f;
            while ((combat.IsExecuting || combat.PendingChildCount > 0) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(4, echoes); Assert.AreEqual(1, waves); Assert.AreEqual(10, combat.Energy);
            Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3EchoClockPausesAndTeardownDropsRemainingChildren()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var weapon = _player.CurrentWeapon; var combat = _player.GetComponent<BreakerGauntletCombat>();
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats();
            for (int teardown = 0; teardown < 3; teardown++)
            {
                combat.enabled = true; combat.Configure(null); combat.Configure(weapon); holder.RefreshLoadout(); _player.mana = 500f;
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                mods.GauntletMutations.Select(GauntletMutation.PressureField);
                foreach (var def in WeaponRunModifiers.Catalog) if (def.Kind == WeaponBoon.FlurryEcho)
                    for (int i = 0; i < 3; i++) mods.Add(def);
                typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
                var enemy = BuildEnemy(Vector3.forward * 1.5f, 10000f).actor;
                bool producer = false; int echoes = 0;
                Action<Actor, float> hit = (_, __) => {
                    var ctx = _player.CurrentGauntletImpactContext;
                    if (ctx.Origin == GauntletImpactOrigin.Direct && ctx.ImpactIndex == ctx.Snapshot.LastDirectProducer)
                    { producer = true; combat.FinishForDodge(); Time.timeScale = 0f; }
                    if ((ctx.Origin == GauntletImpactOrigin.Echo || ctx.Origin == GauntletImpactOrigin.Persistent)) echoes++;
                };
                run.Hooks.OnHit += hit; Physics.SyncTransforms(); Assert.IsTrue(holder.TryUseAbility(1));
                float deadline = Time.realtimeSinceStartup + 4f;
                while (!producer && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(producer); yield return new WaitForSecondsRealtime(.12f);
                Assert.AreEqual(7, combat.PendingChildCount); Assert.AreEqual(0, echoes);
                Time.timeScale = 1f;
                float health = _player.health; _player.TakeDamage(10f);
                Assert.AreEqual(10f, health - _player.health, .001f, "echo lifetime grants no damage reduction");
                if (teardown == 0) combat.Configure(null);
                else if (teardown == 1) combat.enabled = false;
                else _player.TakeDamage(_player.health + 1f);
                Assert.AreEqual(0, combat.PendingChildCount); Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
                Time.timeScale = 1f; yield return new WaitForSeconds(.3f); Assert.AreEqual(0, echoes);
                run.Hooks.OnHit -= hit; UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3EchoesRequireExecutedProducerAndSurviveOrdinaryCancel()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var weapon = _player.CurrentWeapon;
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(); _player.Stats.criticalChance = 0f;
            foreach (int slot in new[] { 1, 3 }) for (int rank = 0; rank <= 3; rank++)
            {
                _agent.Warp(Vector3.zero); combat.Configure(null); combat.Configure(weapon); holder.RefreshLoadout(); _player.mana = 500f;
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                foreach (var def in WeaponRunModifiers.Catalog)
                    if (def.Kind == WeaponBoon.FlurryEcho || def.Kind == WeaponBoon.AsuraEcho)
                        for (int i = 0; i < rank; i++) mods.Add(def);
                typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
                var enemy = BuildEnemy(Vector3.forward * 1.5f, 10000f).actor;
                bool producer = false; int echoes = 0; float directDamage = 0f;
                var childIds = new HashSet<long>();
                Action<Actor, float> hit = (_, damage) => {
                    var ctx = _player.CurrentGauntletImpactContext; Assert.IsNotNull(ctx);
                    if (ctx.Origin == GauntletImpactOrigin.Direct && ctx.ImpactIndex == ctx.Snapshot.LastDirectProducer)
                    {
                        producer = true; directDamage = damage;
                        Assert.AreEqual(slot == 1 ? 2 * rank : rank, combat.PendingChildCount);
                        combat.FinishForDodge(); // Same cancel execution path, inside the confirmed producer callback.
                        Assert.IsFalse(combat.IsExecuting); Assert.IsFalse(combat.IsAsuraActive);
                    }
                    if (ctx.Origin == GauntletImpactOrigin.Echo)
                    {
                        echoes++; Assert.IsTrue(childIds.Add(ctx.Execution)); Assert.IsFalse(ctx.EnergyEligible);
                        Assert.AreEqual(directDamage * (slot == 1 ? .55f : .65f), damage, .01f);
                        Assert.IsFalse(combat.IsExecuting); Assert.IsFalse(combat.IsAsuraActive);
                    }
                };
                Action<ProtocolCombatEvent> secondary = impact => { if (impact.Producer != ProtocolProducer.Direct) hit(null, impact.Damage); };
                run.Hooks.OnProtocolImpact += secondary;
                run.Hooks.OnHit += hit; Physics.SyncTransforms();
                if (slot == 3) combat.AddAsuraEnergy(100);
                Assert.IsTrue(holder.TryUseAbility(slot));
                float deadline = Time.realtimeSinceStartup + 6f;
                while ((!producer || combat.PendingChildCount > 0) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(producer); Assert.AreEqual(slot == 1 ? 2 * rank : rank, echoes);
                Assert.AreEqual(0, combat.PendingChildCount); Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
                Assert.AreEqual(slot == 1 ? 10 : 0, combat.Energy);
                Assert.AreEqual(500f - holder.ActiveAbilities[slot].ManaCost, _player.mana, .001f);
                run.Hooks.OnProtocolImpact -= secondary;
                run.Hooks.OnHit -= hit; UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3CancelBeforeProducerDoesNotFabricateFinisherOrEcho()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var weapon = _player.CurrentWeapon; var combat = _player.GetComponent<BreakerGauntletCombat>();
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats();
            foreach (int slot in new[] { 1, 3 })
            {
                combat.Configure(null); combat.Configure(weapon); holder.RefreshLoadout(); _player.mana = 500f;
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                mods.GauntletMutations.Select(GauntletMutation.PressureField);
                foreach (var def in WeaponRunModifiers.Catalog) if (def.Kind == WeaponBoon.FlurryEcho || def.Kind == WeaponBoon.AsuraEcho)
                    for (int i = 0; i < 3; i++) mods.Add(def);
                typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
                var enemy = BuildEnemy(Vector3.forward * 1.5f, 10000f).actor; int hits = 0;
                Action<Actor, float> hit = (_, __) => {
                    hits++; var ctx = _player.CurrentGauntletImpactContext;
                    Assert.AreEqual(GauntletImpactOrigin.Direct, ctx.Origin);
                    Assert.Less(ctx.ImpactIndex, ctx.Snapshot.LastDirectProducer);
                    combat.FinishForDodge();
                };
                run.Hooks.OnHit += hit; Physics.SyncTransforms(); if (slot == 3) combat.AddAsuraEnergy(100);
                Assert.IsTrue(holder.TryUseAbility(slot));
                float deadline = Time.realtimeSinceStartup + 3f;
                while (hits == 0 && Time.realtimeSinceStartup < deadline) yield return null;
                yield return new WaitForSeconds(.7f);
                Assert.AreEqual(1, hits); Assert.AreEqual(0, combat.PendingChildCount);
                Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount); Assert.IsFalse(combat.IsExecuting);
                run.Hooks.OnHit -= hit; UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3GuardCountsExecutionsAndCommitsBreakBeforePublicHp()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.Stats.baseDamage = 0f; _player.Stats.criticalChance = 0f;
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(weapon); holder.RefreshLoadout();
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var run = _player.gameObject.AddComponent<RunBoons>(); var hooks = new HookBus();
            typeof(RunBoons).GetProperty("Hooks").SetValue(run, hooks); SetPrivate(run, "_weapon", weapon);
            var tracker = _player.gameObject.AddComponent<ImpactGuardTracker>();
            for (int rank = 0; rank <= 3; rank++)
            {
                combat.Configure(null); combat.Configure(weapon);
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                foreach (var def in WeaponRunModifiers.Catalog) if (def.Kind == WeaponBoon.GuardBreaker || def.Kind == WeaponBoon.SeismicFist)
                    for (int i = 0; i < rank; i++) mods.Add(def);
                typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
                tracker.enabled = false; tracker.enabled = true; tracker.Configure(_player, hooks, holder, rank);
                var left = BuildEnemy(new Vector3(-.3f, 0f, 1.5f)).actor;
                var right = BuildEnemy(new Vector3(.3f, 0f, 1.5f)).actor;
                var a = left.gameObject.AddComponent<CombatReactionController>();
                var b = right.gameObject.AddComponent<CombatReactionController>();
                a.ConfigureStance(EnemyRank.Normal, 30f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
                b.ConfigureStance(EnemyRank.Boss, 30f, 1f, 0f, 0f, 1f, 1f, 1f, 1f);
                b.ConfigureReaction(new ReactionConfiguration(EnemyRank.Boss, ReactionCategory.SmallMob));
                int broken = 0, hpEvents = 0, basicEvents = 0, execution = 0;
                a.StanceBroken += _ => broken++;
                b.StanceBroken += _ => { broken++; Assert.IsFalse(b.IsStunned); };
                Action<Actor, float> hit = (_, __) => {
                    hpEvents++;
                    if (execution == 3) Assert.AreEqual(rank > 0 ? 29 : 9, combat.Energy,
                        "native basic and Guard break must both precede HP publication");
                };
                hooks.OnHit += hit; hooks.OnBasicHit += (_, __) => basicEvents++;
                Physics.SyncTransforms();
                for (execution = 1; execution <= 3; execution++)
                {
                    var context = combat.BeginBasicExecution();
                    Assert.AreEqual(2, ResolveGauntletTestBatch(combat, context, 0));
                    Assert.AreEqual(rank > 0 ? execution % 3 : 0, tracker.Streak);
                    tracker.Configure(_player, hooks, holder, rank); // Reconfiguration must preserve this run's progress.
                    combat.EnergyPolicy.Close(context);
                }
                Assert.AreEqual(rank > 0 ? 1 : 0, broken); Assert.AreEqual(6, hpEvents); Assert.AreEqual(6, basicEvents);
                Assert.AreEqual(rank > 0 ? 29 : 9, combat.Energy);
                Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
                // Q/W/E accepted reset, R/dash do not. Exercise the same subscribed callback without starting another cast.
                hooks.RaiseBasicHit(left, 1f); hooks.RaiseBasicHit(left, 1f);
                typeof(ImpactGuardTracker).GetMethod("OnAbilityUsed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tracker, new object[] { 3 });
                Assert.AreEqual(rank > 0 ? 2 : 0, tracker.Streak);
                typeof(ImpactGuardTracker).GetMethod("OnAbilityUsed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tracker, new object[] { 1 });
                Assert.AreEqual(0, tracker.Streak);
                tracker.enabled = false; hooks.RaiseBasicHit(left, 1f); Assert.AreEqual(0, tracker.Streak);
                UnityEngine.Object.DestroyImmediate(left.gameObject); UnityEngine.Object.DestroyImmediate(right.gameObject);
                hooks.Clear();
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3MomentumTechniquePaysConfirmedSkillOnceAtEveryRank()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(weapon); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats();
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var run = _player.gameObject.AddComponent<RunBoons>();
            yield return null; // Finish the real run bootstrap before capturing equipment/contexts.
            weapon = _player.CurrentWeapon;
            var hooks = run.Hooks;
            for (int rank = 0; rank <= 3; rank++)
            {
                _agent.Warp(Vector3.zero); combat.Configure(null); combat.Configure(weapon); holder.RefreshLoadout(); _player.mana = 500f;
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                foreach (var entry in WeaponRunModifiers.Catalog) if (entry.Kind == WeaponBoon.Momentum)
                    for (int i = 0; i < rank; i++) mods.Add(entry);
                typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
                var left = BuildEnemy(new Vector3(-.2f, 0f, 4f), 1f).actor;
                var right = BuildEnemy(new Vector3(.2f, 0f, 4f)).actor;
                int hpEvents = 0; hooks.OnHit += (_, __) => hpEvents++;
                Assert.IsTrue(holder.TryUseAbility(0));
                var q = (BreakerGauntletAbility)holder.ActiveAbilities[0];
                Vector3 final = _player.transform.position + _player.transform.forward * (q.AdvanceDistance + 1.5f);
                left.transform.position = final - _player.transform.right * .2f;
                right.transform.position = final + _player.transform.right * .2f; Physics.SyncTransforms();
                Assert.AreEqual(0, combat.Energy);
                float deadline = Time.realtimeSinceStartup + 4f;
                while (combat.IsExecuting && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsFalse(combat.IsExecuting); Assert.IsTrue(left == null || left.IsDead);
                Assert.GreaterOrEqual(hpEvents, 2); Assert.AreEqual(10 + 10 * rank, combat.Energy);
                Assert.AreEqual(500f - q.ManaCost, _player.mana, .001f);
                if (left) UnityEngine.Object.DestroyImmediate(left.gameObject);
                if (right) UnityEngine.Object.DestroyImmediate(right.gameObject);
                hooks.Clear();
            }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3HungryAndFistPayOncePerBasicIncludingLethalAndReconfigure()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.Stats.baseDamage = 0f; _player.Stats.criticalChance = 0f;
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(weapon); holder.RefreshLoadout();
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var run = _player.gameObject.AddComponent<RunBoons>();
            var hooks = new HookBus(); typeof(RunBoons).GetProperty("Hooks").SetValue(run, hooks);
            SetPrivate(run, "_weapon", weapon);
            var hungry = _player.gameObject.AddComponent<HungryComboTracker>();
            var surge = _player.gameObject.AddComponent<AsuraSurge>();
            for (int rank = 0; rank <= 3; rank++)
            {
                combat.Configure(null); combat.Configure(weapon); holder.RefreshLoadout();
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Kind == WeaponBoon.HungryCombo || definition.Kind == WeaponBoon.AsuraFist)
                        for (int i = 0; i < rank; i++) mods.Add(definition);
                typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
                for (int i = 0; i < 4; i++) { hungry.Configure(_player, hooks, holder, rank); surge.Configure(_player, hooks, combat, rank); }
                SeedGauntletCooldowns(holder, 2f);
                var left = BuildEnemy(new Vector3(-.3f, 0f, 1.5f), 1f).actor;
                var right = BuildEnemy(new Vector3(.3f, 0f, 1.5f)).actor;
                int basicHits = 0, basicKills = 0;
                Action<Actor, float> hit = (_, __) => basicHits++;
                Action<Actor> kill = _ => basicKills++;
                hooks.OnBasicHit += hit; hooks.OnBasicKill += kill;
                var context = combat.BeginBasicExecution(); Physics.SyncTransforms();
                Assert.AreEqual(2, ResolveGauntletTestBatch(combat, context, 0));
                Assert.IsTrue(left.IsDead); Assert.AreEqual(2, basicHits); Assert.AreEqual(1, basicKills);
                Assert.AreEqual(3 + 2 * rank, combat.Energy);
                Assert.AreEqual(2f - .3f * rank, holder.GetRemainingCooldown(0), .0001f);
                Assert.AreEqual(2f, holder.GetRemainingCooldown(1)); Assert.AreEqual(2f, holder.GetRemainingCooldown(2));
                Assert.AreEqual(2f - .3f * rank, holder.GetRemainingCooldown(3), .0001f);
                ResolveGauntletTestBatch(combat, context, 0);
                Assert.AreEqual(2f - .3f * rank, holder.GetRemainingCooldown(0), .0001f);
                Assert.AreEqual(3 + 2 * rank, combat.Energy);
                hooks.OnBasicHit -= hit; hooks.OnBasicKill -= kill; combat.EnergyPolicy.Close(context);
                UnityEngine.Object.DestroyImmediate(left.gameObject); UnityEngine.Object.DestroyImmediate(right.gameObject);
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3HungryLeavesExecutingRCooldownAndDisabledConsumersInactive()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(weapon); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var run = _player.gameObject.AddComponent<RunBoons>(); var hooks = new HookBus();
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            foreach (var entry in WeaponRunModifiers.Catalog)
                if (entry.Kind == WeaponBoon.HungryCombo || entry.Kind == WeaponBoon.AsuraFist) mods.Add(entry);
            typeof(RunBoons).GetProperty("Hooks").SetValue(run, hooks);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods); SetPrivate(run, "_weapon", weapon);
            var hungry = _player.gameObject.AddComponent<HungryComboTracker>(); hungry.Configure(_player, hooks, holder, 1);
            var surge = _player.gameObject.AddComponent<AsuraSurge>(); surge.Configure(_player, hooks, combat, 1);
            var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            var pending = combat.BeginBasicExecution(); combat.AddAsuraEnergy(100);
            Assert.IsTrue(holder.TryUseAbility(3)); SeedGauntletCooldowns(holder, 2f); Physics.SyncTransforms();
            Assert.AreEqual(1, ResolveGauntletTestBatch(combat, pending, 0));
            Assert.AreEqual(1.7f, holder.GetRemainingCooldown(0), .0001f);
            Assert.AreEqual(2f, holder.GetRemainingCooldown(3)); Assert.AreEqual(0, combat.Energy);
            combat.EnergyPolicy.Close(pending);
            typeof(BreakerGauntletCombat).GetMethod("CancelCast", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combat, null);
            hungry.enabled = false; surge.enabled = false;
            hooks.RaiseBasicHit(enemy, 1f);
            Assert.AreEqual(1.7f, holder.GetRemainingCooldown(0), .0001f); Assert.AreEqual(0, combat.Energy);
            hungry.enabled = true; surge.enabled = true; hooks.RaiseBasicHit(enemy, 1f);
            Assert.AreEqual(1.4f, holder.GetRemainingCooldown(0), .0001f); Assert.AreEqual(2, combat.Energy);
            Assert.AreEqual(1, mods.Rank(WeaponBoon.HungryCombo)); Assert.AreEqual(1, mods.Rank(WeaponBoon.AsuraFist));
            yield break;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3MomentumResetIgnoresImmunityAbsorptionAndDoesNotRestorePreDamageHits()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(weapon); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); var hooks = new HookBus();
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            foreach (var entry in WeaponRunModifiers.Catalog) if (entry.Kind == WeaponBoon.MomentumStrike) mods.Add(entry);
            typeof(RunBoons).GetProperty("Hooks").SetValue(run, hooks);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods); SetPrivate(run, "_weapon", weapon);
            var momentum = _player.gameObject.AddComponent<MomentumStacks>(); momentum.Configure(_player, hooks, 1, run);
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            hooks.RaiseHit(enemy, 1f); hooks.RaiseHit(enemy, 1f); Assert.AreEqual(2, momentum.Stacks);
            _player.AddDamageImmunity(momentum); _player.TakeDamage(1f); Assert.AreEqual(2, momentum.Stacks);
            _player.RemoveDamageImmunity(momentum);
            var shield = _player.gameObject.AddComponent<Shield>(); shield.Configure(10f, 3f);
            _player.TakeDamage(1f); Assert.AreEqual(2, momentum.Stacks);
            UnityEngine.Object.DestroyImmediate(shield);
            hooks.OnHit += (_, __) => _player.TakeDamage(1f);
            var context = combat.BeginBasicExecution(); Physics.SyncTransforms();
            Assert.AreEqual(1, ResolveGauntletTestBatch(combat, context, 0)); Assert.AreEqual(0, momentum.Stacks);
            combat.EnergyPolicy.Close(context);
            momentum.enabled = false; Assert.AreEqual(0, momentum.Stacks);
            foreach (var modifier in _player.Stats.RuntimeModifiers) Assert.AreNotEqual(run, modifier.source);
            Assert.AreEqual(1, mods.Rank(WeaponBoon.MomentumStrike));
            yield break;
        }

        private static void SeedGauntletCooldowns(AbilityHolder holder, float seconds)
        {
            var stateType = typeof(AbilityHolder).GetNestedType("AbilityState", BindingFlags.NonPublic);
            var states = Array.CreateInstance(stateType, 4);
            string[] names = { "Cooldown", "Ready", "Active", "Cooldown" };
            for (int i = 0; i < 4; i++) states.SetValue(Enum.Parse(stateType, names[i]), i);
            SetPrivate(holder, "states", states); SetPrivate(holder, "cooldownTimers", new[] { seconds, seconds, seconds, seconds });
        }
        private int ResolveGauntletTestBatch(BreakerGauntletCombat combat, GauntletImpactContext context, int index)
            => _player.TryApplyGauntletAreaDamage(Vector3.zero, Vector3.forward, 2.2f, new Vector3(2f, 2f, 2f),
                AreaHitShape.Box, 0f, Physics.DefaultRaycastLayers, 10f, 1f, 0f, null, false, null, context, combat.EnergyPolicy, index);

        // gauntlet-2-rework P9/P10/P15: real HP pipeline, frozen batch and causal stacks.
        [UnityTest]
        public IEnumerator GauntletPhase3MomentumBatchIsUniformAndDescendantsNeverStack()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.Stats.baseDamage = 0f; _player.Stats.criticalChance = 0f;
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(weapon); holder.RefreshLoadout();
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var run = _player.gameObject.AddComponent<RunBoons>();
            var hooks = new HookBus();
            typeof(RunBoons).GetProperty("Hooks").SetValue(run, hooks); SetPrivate(run, "_weapon", weapon);
            var momentum = _player.gameObject.AddComponent<MomentumStacks>();
            var left = BuildEnemy(new Vector3(-.4f, 0f, 1.5f)).actor;
            var right = BuildEnemy(new Vector3(.4f, 0f, 1.5f)).actor;
            var registry = (GauntletContextRegistry)typeof(BreakerGauntletCombat)
                .GetField("_contexts", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat);
            Physics.SyncTransforms();
            for (int rank = 0; rank <= 3; rank++)
            {
                momentum.enabled = false; momentum.enabled = true;
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Kind == WeaponBoon.MomentumStrike) for (int i = 0; i < rank; i++) mods.Add(definition);
                typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
                momentum.Configure(_player, hooks, rank, run);
                hooks.RaiseHit(left, 1f); hooks.RaiseHit(left, 1f); // two pre-existing stacks at nonzero rank
                int initial = rank > 0 ? 2 : 0;
                var context = combat.BeginBasicExecution();
                float leftBefore = left.health, rightBefore = right.health;
                // Acquire a new rank during an impact callback; this execution must keep its old rank.
                bool acquired = false;
                Action<Actor, float> acquire = (_, __) => {
                    if (acquired) return; acquired = true;
                    foreach (var entry in WeaponRunModifiers.Catalog)
                        if (entry.Kind == WeaponBoon.MomentumStrike && rank < 3) mods.Add(entry);
                    momentum.Configure(_player, hooks, mods.Rank(WeaponBoon.MomentumStrike), run);
                };
                hooks.OnHit += acquire;
                Assert.AreEqual(2, _player.TryApplyGauntletAreaDamage(Vector3.zero, Vector3.forward, 2.2f,
                    new Vector3(2f, 2f, 2f), AreaHitShape.Box, 0f, Physics.DefaultRaycastLayers,
                    10f, 1f, 0f, null, false, null, context, combat.EnergyPolicy, 0));
                hooks.OnHit -= acquire;
                float expected = 10f * (1f + .02f * rank * initial);
                Assert.AreEqual(expected, leftBefore - left.health, .001f);
                Assert.AreEqual(expected, rightBefore - right.health, .001f);
                Assert.AreEqual(rank > 0 ? 4 : 0, momentum.Stacks);
                // A callback replay still emits existing public events but cannot add causal stacks.
                _player.TryApplyGauntletAreaDamage(Vector3.zero, Vector3.forward, 2.2f, new Vector3(2f, 2f, 2f),
                    AreaHitShape.Box, 0f, Physics.DefaultRaycastLayers, 10f, 1f, 0f, null, false, null,
                    context, combat.EnergyPolicy, 0);
                Assert.AreEqual(rank > 0 ? 4 : 0, momentum.Stacks);
                var child = registry.BeginChild(context, GauntletImpactOrigin.Echo, WeaponRunModifiers.CatalogId(WeaponBoon.FlurryEcho));
                _player.TryApplyGauntletAreaDamage(Vector3.zero, Vector3.forward, 2.2f, new Vector3(2f, 2f, 2f),
                    AreaHitShape.Box, 0f, Physics.DefaultRaycastLayers, 10f, 1f, 0f, null, false, null,
                    child, combat.EnergyPolicy, 1);
                Assert.AreEqual(rank > 0 ? 4 : 0, momentum.Stacks);
                combat.EnergyPolicy.Close(child); combat.EnergyPolicy.Close(context);
                float health = _player.health;
                _player.TakeDamage(1f); Assert.Less(_player.health, health); Assert.AreEqual(0, momentum.Stacks);
            }
            yield break;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3StartedRUsesExistingDashBufferWithoutRefundOrDoubleCost()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var cameraObject = new GameObject("GauntletRTestCamera"); _spawned.Add(cameraObject);
            cameraObject.tag = "MainCamera"; cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(0f, 10f, -10f); cameraObject.transform.LookAt(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(); _player.mana = 500f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var dash = ScriptableObject.CreateInstance<DashScript>();
            dash.dashTime = .1f; dash.dashVelocity = 0f; dash.dashAnimation = string.Empty;
            SetPrivate(dash, "_iframeWindow", 0f); dash.ConfigureRunPresentation("Test Dash", 10f, dash.Glyph);
            try
            {
                int dashUses = 0; holder.AbilityUsed += slot => { if (slot == 4) dashUses++; };
                float rCost = holder.ActiveAbilities[3].ManaCost;
                combat.AddAsuraEnergy(100); Assert.IsTrue(holder.TryUseAbility(3));
                // The cast/cost are real; drive progress deterministically across the authored boundary.
                // A frame can skip this short window, which is unsuitable for a boundary assertion.
                SetPrivate(combat, "_elapsed", combat.ExecutionDuration * .049f);
                Assert.IsFalse(combat.CanDodgeCancel); Assert.IsFalse(holder.TryUseDash(dash), "early press buffers without executing");
                Assert.IsTrue(combat.IsExecuting); Assert.AreEqual(500f - rCost, _player.mana);
                SetPrivate(combat, "_elapsed", combat.ExecutionDuration * .051f);
                typeof(AbilityHolder).GetMethod("TickDashBuffer", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(holder, null);
                float deadline = Time.realtimeSinceStartup + 2f;
                while (dashUses == 0 && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual(1, dashUses); Assert.IsFalse(combat.IsExecuting); Assert.IsFalse(combat.EnergyPolicy.IsAsuraActive);
                Assert.AreEqual(0, combat.Energy); Assert.AreEqual(500f - rCost - 10f, _player.mana);
                while (dash.IsDashing(_player.gameObject) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsFalse(dash.IsDashing(_player.gameObject)); Assert.AreEqual(1, dashUses);
                Assert.AreEqual(500f - rCost - 10f, _player.mana);
            }
            finally { UnityEngine.Object.DestroyImmediate(dash); }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3PreviousConfirmationDuringRDoesNotReplayAfterR()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var context = (GauntletImpactContext)typeof(BreakerGauntletCombat)
                .GetMethod("BeginBasicExecution", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combat, null);
            var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            var resolve = typeof(PlayerActor).GetMethod("TryApplyGauntletAreaDamage", BindingFlags.Instance | BindingFlags.NonPublic);
            object[] args = { Vector3.zero, Vector3.forward, 2.2f, new Vector3(2f, 2f, 2f), AreaHitShape.Box,
                0f, (LayerMask)Physics.DefaultRaycastLayers, 10f, 1f, 0f, null, false, null, context, combat.EnergyPolicy, 0 };
            combat.AddAsuraEnergy(100); Assert.IsTrue(holder.TryUseAbility(3)); Physics.SyncTransforms();
            Assert.AreEqual(1, (int)resolve.Invoke(_player, args)); Assert.AreEqual(0, combat.Energy);
            typeof(BreakerGauntletCombat).GetMethod("CancelCast", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combat, null);
            float health = enemy.health;
            Assert.AreEqual(1, (int)resolve.Invoke(_player, args)); Assert.Less(enemy.health, health);
            Assert.AreEqual(0, combat.Energy, "blocked confirmation is consumed and cannot grant after R");
            combat.EnergyPolicy.Close(context); health = enemy.health;
            Assert.AreEqual(0, (int)resolve.Invoke(_player, args)); Assert.AreEqual(health, enemy.health);
            Assert.AreEqual(0, combat.EnergyPolicy.RetainedRootCount);
            yield break;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3RConsumesOnceReservesRanksAndPublishesAtomicResources()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(); _player.mana = 500f;
            var run = _player.gameObject.AddComponent<RunBoons>();
            yield return null; // Initialize the actual run and its cloned weapon before casting.
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var enemy = BuildEnemy(Vector3.forward * 1.5f, 10000f).actor;
            int currentRank = 0, manaEvents = 0, reserveEvents = 0, activations = 0;
            holder.AbilityUsed += slot => { if (slot == 3) activations++; };
            _player.ManaChanged += _ =>
            {
                Assert.IsTrue(combat.IsAsuraActive); Assert.AreEqual(currentRank * 20, combat.Energy);
                Assert.IsFalse(holder.TryUseAbility(3), "resource observer cannot reenter R");
                manaEvents++;
            };
            combat.EnergyPolicy.Committed += gain =>
            {
                if (gain.Reserve <= 0) return;
                Assert.IsTrue(combat.IsAsuraActive); Assert.AreEqual(currentRank * 20, combat.Energy);
                reserveEvents++;
            };
            for (currentRank = 0; currentRank <= 3; currentRank++)
            {
                if (currentRank > 0)
                    foreach (var definition in WeaponRunModifiers.Catalog)
                        if (definition.Kind == WeaponBoon.AsuraReserve) Assert.IsTrue(run.WeaponModifiers.Add(definition));
                combat.Configure(null); combat.Configure(_player.CurrentWeapon); holder.RefreshLoadout();
                _player.mana = 500f; combat.AddAsuraEnergy(100); Physics.SyncTransforms();
                float cost = holder.ActiveAbilities[3].ManaCost;
                Assert.IsTrue(holder.TryUseAbility(3));
                Assert.AreEqual(500f - cost, _player.mana); Assert.AreEqual(currentRank * 20, combat.Energy);
                combat.AddAsuraEnergy(50); Assert.AreEqual(currentRank * 20, combat.Energy, "external combat gain is blocked during R");
                float deadline = Time.realtimeSinceStartup + 4f;
                while (combat.IsExecuting && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsFalse(combat.IsExecuting); Assert.IsFalse(combat.EnergyPolicy.IsAsuraActive);
                Assert.AreEqual(currentRank * 20, combat.Energy, "R hits cannot recharge or refund");
                Assert.AreEqual(500f - cost, _player.mana);
                Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward * 2f), "normal input resumes after R");
                Assert.AreEqual(currentRank * 20 + 3, combat.Energy, "new basic can generate after R ends");
                combat.AddAsuraEnergy(100);
                Assert.IsFalse(holder.TryUseAbility(3), "full energy does not bypass the existing cooldown");
                Assert.AreEqual(100, combat.Energy); Assert.AreEqual(500f - cost, _player.mana);
            }
            Assert.AreEqual(4, activations); Assert.AreEqual(4, manaEvents); Assert.AreEqual(3, reserveEvents);
            Assert.Less(enemy.health, 10000f);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3RRejectionPreservesEnergyManaAndCurrentAction()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            combat.AddAsuraEnergy(99);
            Assert.IsFalse(holder.TryUseAbility(3)); Assert.AreEqual(99, combat.Energy); Assert.AreEqual(500f, _player.mana);
            Assert.IsTrue(holder.TryUseAbility(0)); var current = combat.ActiveAbility;
            float mana = _player.mana;
            SetPrivate(combat, "_elapsed", combat.ExecutionDuration * .9f);
            Assert.IsFalse(holder.TryUseAbility(3));
            Assert.IsTrue(combat.IsExecuting); Assert.AreSame(current, combat.ActiveAbility);
            Assert.AreEqual(mana, _player.mana); Assert.AreEqual(99, combat.Energy); Assert.AreEqual(0f, holder.GetRemainingCooldown(3));
            combat.AddAsuraEnergy(1); _player.mana = 0f;
            Assert.IsFalse(holder.TryUseAbility(3)); Assert.IsTrue(combat.IsExecuting); Assert.AreSame(current, combat.ActiveAbility);
            Assert.AreEqual(100, combat.Energy); Assert.AreEqual(0f, _player.mana);
            yield break;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3RCancelSwapDisableAndDeathCleanOnlyOwnedState()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(weapon); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            _player.Stats.armor = 0f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            _player.SetDamageTakenMultiplier(weapon, .5f);
            for (int path = 0; path < 4; path++)
            {
                combat.enabled = true; _player.EquipWeapon(weapon); holder.RefreshLoadout();
                _player.mana = 500f; combat.AddAsuraEnergy(100);
                _agent.isStopped = false;
                Assert.IsTrue(holder.TryUseAbility(3)); Assert.IsTrue(_agent.isStopped);
                float before = _player.health; _player.TakeDamage(10f); Assert.AreEqual(.75f, before - _player.health, .001f);
                if (path == 0) typeof(BreakerGauntletCombat).GetMethod("CancelCast", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combat, null);
                else if (path == 1) _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow"));
                else if (path == 2) combat.enabled = false;
                else { _player.health = 1f; _player.TakeDamage(10000f); }
                Assert.IsFalse(combat.IsExecuting); Assert.IsFalse(combat.EnergyPolicy.IsAsuraActive); Assert.AreEqual(0, combat.Energy);
                if (_agent.enabled && _agent.isOnNavMesh) Assert.IsFalse(_agent.isStopped);
                Assert.IsFalse((bool)typeof(BreakerGauntletCombat).GetField("_agentLocked", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(combat));
                Assert.IsTrue(_agent.updateRotation);
                if (path < 3) { before = _player.health; _player.TakeDamage(10f); Assert.AreEqual(5f, before - _player.health, .001f); }
            }
            Assert.IsTrue(_player.IsDead);
            yield break;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3PublicEventOrderSeesCompleteGainAndNoDuplicateFist()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(weapon); holder.RefreshLoadout();
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var run = _player.gameObject.AddComponent<RunBoons>();
            var hooks = new HookBus();
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == WeaponBoon.AsuraFist) Assert.IsTrue(mods.Add(definition));
            typeof(RunBoons).GetProperty("Hooks").SetValue(run, hooks);
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, mods);
            SetPrivate(run, "_weapon", weapon);
            var surge = _player.gameObject.AddComponent<AsuraSurge>(); surge.Configure(_player, hooks, combat, 1);
            var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            var reaction = enemy.gameObject.AddComponent<CombatReactionController>();
            reaction.ConfigureStance(EnemyRank.Normal, 1f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
            var order = new List<string>();
            Action<string> record = label => { Assert.AreEqual(25, combat.Energy, label); order.Add(label); };
            enemy.DamageReceived += (_, __) => record("hp");
            enemy.HealthChanged += _ => record("health");
            hooks.OnHit += (_, __) => record("hit");
            hooks.OnBasicHit += (_, __) => record("basic");
            reaction.StanceBroken += _ => record("break");
            hooks.OnStanceBreak += _ => record("hook-break");
            holder.AttackHitsResolved += (_, __) => record("batch");
            Physics.SyncTransforms();
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward * 2f));
            CollectionAssert.AreEqual(new[] { "hp", "health", "hit", "basic", "break", "hook-break", "batch" }, order);
            Assert.AreEqual(25, combat.Energy, "3 native + 2 fist + 20 break, without legacy duplication");
            yield break;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3SharedAreaKeepsBowAndSpearDamageAndEventCounts()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.Stats.baseDamage = 0f; // Awake applies the project's starting-balance override.
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            int hp = 0, batches = 0;
            enemy.DamageReceived += (_, __) => hp++;
            holder.AttackHitsResolved += (_, targets) => { Assert.AreEqual(1, targets.Count); batches++; };
            foreach (string key in new[] { "Weapons/Ranged/Bow_arrow/Bow", "Weapons/Melee/Spear/Spear" })
            {
                var weapon = Resources.Load<WeaponScript>(key); Assert.IsNotNull(weapon, key);
                _player.EquipWeapon(weapon); holder.RefreshLoadout(); Physics.SyncTransforms();
                Assert.AreEqual(1, _player.TryApplyAreaDamage(Vector3.zero, Vector3.forward, 2.2f,
                    new Vector3(2f, 2f, 2f), Physics.DefaultRaycastLayers, 10f));
            }
            Assert.AreEqual(980f, enemy.health); Assert.AreEqual(2, hp); Assert.AreEqual(2, batches);
            yield break;
        }

        [UnityTest]
        public IEnumerator GauntletPhase3SkillsPayOnConfirmationAndCancellationKeepsCommittedGain()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"));
            holder.RefreshLoadout(); SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            Physics.SyncTransforms();
            int expected = 0;
            for (int slot = 0; slot < 3; slot++)
            {
                _agent.Warp(Vector3.zero); enemy.transform.position = Vector3.forward * 1.5f; Physics.SyncTransforms();
                Assert.IsTrue(holder.TryUseAbility(slot), "slot " + slot);
                var ability = (BreakerGauntletAbility)holder.ActiveAbilities[slot];
                enemy.transform.position = _player.transform.position + _player.transform.forward * (ability.AdvanceDistance + 1.5f);
                Physics.SyncTransforms();
                Assert.AreEqual(expected, combat.Energy, "acceptance itself cannot pay");
                float deadline = Time.realtimeSinceStartup + 4f;
                while (combat.Energy == expected && Time.realtimeSinceStartup < deadline) yield return null;
                expected += slot == 2 ? 25 : 10;
                Assert.AreEqual(expected, combat.Energy);
                typeof(BreakerGauntletCombat).GetMethod("CancelCast", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combat, null);
                Assert.AreEqual(expected, combat.Energy, "committed gain survives cancellation");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator GauntletPhase3ReentrantRequestWaitsUntilImpactAndRevalidatesMana()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"));
            holder.RefreshLoadout(); _player.mana = 0f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            int requests = 0, used = 0, rejected = 0;
            holder.AbilityUsed += _ => used++;
            holder.AbilityRejected += (_, reason) => { Assert.AreEqual(AbilityUseFailure.NotEnoughMana, reason); rejected++; };
            enemy.DamageReceived += (_, __) =>
            {
                Assert.IsTrue(_player.IsResolvingGauntletImpact);
                Assert.AreEqual(3, combat.Energy, "HP callback observes combined confirmation");
                Assert.IsTrue(holder.TryUseAbility(0)); requests++;
                Assert.IsFalse(_controls.TryDirectionalBasicAttack(Vector3.forward * 2f), "recursive basic cannot start");
                Assert.AreEqual(0, used);
            };
            Physics.SyncTransforms();
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward * 2f));
            Assert.AreEqual(3, combat.Energy); Assert.AreEqual(1, requests);
            yield return null; yield return null;
            Assert.AreEqual(0, used); Assert.AreEqual(1, rejected); Assert.AreEqual(0f, _player.mana);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3BasicWithoutRunPaysOnceForMultipleAndLethalTargets()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"));
            var first = BuildEnemy(Vector3.forward * 1.5f, 1f).actor;
            var second = BuildEnemy(Vector3.forward * 1.5f + Vector3.right * .3f).actor;
            Physics.SyncTransforms();
            Assert.IsNull(_player.GetComponent<RunBoons>());
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward * 2f));
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            Assert.IsNotNull(combat); Assert.AreEqual(3, combat.Energy);
            Assert.IsTrue(first.IsDead); Assert.Less(second.health, 1000f);
            first.gameObject.SetActive(false); second.gameObject.SetActive(false);
            yield return new WaitForSeconds(.5f);
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward * 2f));
            Assert.AreEqual(3, combat.Energy, "empty execution cannot pay");
        }

        [UnityTest]
        public IEnumerator GauntletPhase3BreaksWithoutHpPayOnlyTwoAndKeepAllEvents()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"));
            holder.RefreshLoadout(); _player.mana = 100f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            int breaks = 0;
            for (int i = 0; i < 3; i++)
            {
                var enemy = BuildEnemy(Vector3.forward * 1.5f + Vector3.right * ((i - 1) * .3f)).actor;
                enemy.AddDamageImmunity(_player);
                var reaction = enemy.gameObject.AddComponent<CombatReactionController>();
                reaction.ConfigureStance(EnemyRank.Boss, 1f, 1f, 0f, 0f, 1f, 1f, 1f, 1f);
                reaction.ConfigureReaction(new ReactionConfiguration(EnemyRank.Boss, ReactionCategory.SmallMob));
                reaction.StanceBroken += _ => { breaks++; Assert.GreaterOrEqual(combat.Energy, 30); Assert.IsFalse(reaction.IsStunned); };
            }
            Physics.SyncTransforms();
            Assert.IsTrue(holder.TryUseAbility(2));
            float deadline = Time.realtimeSinceStartup + 4f;
            while (breaks < 3 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(3, breaks); Assert.AreEqual(50, combat.Energy);
        }

        [UnityTest]
        public IEnumerator GauntletPhase3EmptySkillAndPreImpactCancellationDoNotPay()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"));
            holder.RefreshLoadout(); SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            Assert.IsTrue(holder.TryUseAbility(0), "Q accepted with full mana");
            while (combat.IsExecuting) yield return null;
            Assert.AreEqual(0, combat.Energy);
            var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            Physics.SyncTransforms();
            Assert.IsTrue(holder.TryUseAbility(1), "W accepted after Q finishes");
            typeof(BreakerGauntletCombat).GetMethod("CancelCast", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(combat, null);
            yield return null;
            Assert.AreEqual(0, combat.Energy);
            Assert.AreEqual(1000f, enemy.health, "cancellation before impact must not invent damage");
            Assert.IsFalse(holder.TryUseAbility(1), "accepted canceled action retains its cooldown");
            Assert.AreEqual(0, combat.Energy);
        }

        [UnityTest]
        public IEnumerator AuthoredGauntletEThroughAbilityInputLaunchesCommonAndPreservesBossImmunity()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            var gauntlet = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            _player.EquipWeapon(gauntlet);
            holder.RefreshLoadout();
            _player.mana = 100f; // The basic-only fixture starts with no authored mana reserve.
            var common = BuildEnemy(Vector3.forward * 1.5f).actor;
            var boss = BuildEnemy(Vector3.forward * 1.5f + Vector3.right * 0.4f).actor;
            var normalReaction = common.gameObject.AddComponent<CombatReactionController>();
            var bossReaction = boss.gameObject.AddComponent<CombatReactionController>();
            normalReaction.ConfigureStance(EnemyRank.Normal, 10f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
            bossReaction.ConfigureStance(EnemyRank.Boss, 10f, 1f, 0f, 0f, 1f, 1f, 1f, 1f);
            bossReaction.ConfigureReaction(new ReactionConfiguration(EnemyRank.Boss, ReactionCategory.SmallMob));
            bool commonLaunched = false, bossOpened = false;
            normalReaction.StanceBroken += _ => commonLaunched |= normalReaction.IsStunned;
            bossReaction.StanceBroken += _ => { bossOpened = true; Assert.IsFalse(bossReaction.IsStunned); };
            Physics.SyncTransforms();
            Assert.IsTrue(holder.TryUseAbility(2), "actual E command must activate");
            float deadline = Time.realtimeSinceStartup + 4f;
            while ((!commonLaunched || !bossOpened) && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(commonLaunched, "E must break and launch an eligible common target");
            Assert.IsTrue(bossOpened, "E must open the immune boss without physical CC");
            Assert.IsFalse(bossReaction.IsStunned); Assert.Less(common.health, 1000f); Assert.Less(boss.health, 1000f);
        }
        [UnityTest]
        public IEnumerator MigratedGauntletBasicInputsUseWeaponReactionWithoutLeakingToBow()
        {
            Assert.IsTrue(TryBakeFullFloor(), "Phase 2 input validation requires a baked NavMesh.");
            BuildPlayer(Vector3.zero);
            var gauntlet = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            Assert.IsTrue(gauntlet.OverridesBasicReaction); _player.EquipWeapon(gauntlet);
            var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            var reaction = enemy.gameObject.AddComponent<CombatReactionController>();
            reaction.ConfigureStance(EnemyRank.Normal, 1000f, 1f, 0f, 0f, 0f, 0f, 0f, 0f);
            int damageEvents = 0; enemy.DamageReceived += (_, __) => damageEvents++;
            Physics.SyncTransforms();
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(enemy.transform.position));
            Assert.AreEqual(1, damageEvents); Assert.AreEqual(988f, reaction.CurrentStance);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(_controls.TryBasicAttack(enemy.transform.position));
            Assert.AreEqual(2, damageEvents); Assert.AreEqual(976f, reaction.CurrentStance);
            var builder = typeof(CharControlScript).GetMethod("BuildBasicAttackReaction", BindingFlags.Instance | BindingFlags.NonPublic);
            var request = (HitReactionRequest)builder.Invoke(_controls, new object[] { 0, 2.2f });
            Assert.AreEqual(HitReactionType.Stagger, request.ReactionType); Assert.AreEqual(0.35f, request.InterruptContribution);
            SetPrivate(_controls, "comboReactions", new[] { HitReactionType.None });
            request = (HitReactionRequest)builder.Invoke(_controls, new object[] { 0, 2.2f });
            Assert.AreEqual(HitReactionType.None, request.ReactionType); Assert.AreEqual(0f, request.InterruptContribution);
            SetPrivate(_controls, "comboReactions", null);
            var bow = Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow"); _player.EquipWeapon(bow);
            typeof(CharControlScript).GetMethod("RefreshWeaponStats", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_controls, null);
            request = (HitReactionRequest)builder.Invoke(_controls, new object[] { 0, 2.2f });
            Assert.AreEqual(HitReactionType.Push, request.ReactionType); Assert.AreEqual(0f, request.InterruptContribution);
        }
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private NavMeshData _navMesh;
        private NavMeshDataInstance _navMeshInstance;
        private WeaponScript _weapon;

        private CharControlScript _controls;
        private PlayerActor _player;
        private NavMeshAgent _agent;
        private int _attackCount;

        // --- reflection handles ------------------------------------------------------------------

        private static readonly FieldInfo DriverField =
            typeof(CharControlScript).GetField("_basicAttackDriver",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo TargetField =
            typeof(CharControlScript).GetField("target",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AttackIntervalField =
            typeof(CharControlScript).GetField("attackInterval",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NextAttackTimeField =
            typeof(CharControlScript).GetField("nextAttackTime",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PlayerBusyField =
            typeof(CharControlScript).GetField("playerBusy",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo MovementDeadZoneField =
            typeof(CharControlScript).GetField("_movementClickDeadZone",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo SetTargetMethod =
            typeof(CharControlScript).GetMethod("SetTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo MoveToPositionMethod =
            typeof(CharControlScript).GetMethod("MoveToPosition",
                BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo ClearTargetMethod =
            typeof(CharControlScript).GetMethod("ClearTarget",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo DriverQueueTap =
            typeof(BasicAttackDriver).GetMethod("QueueTap");
        private static readonly MethodInfo DriverSetHold =
            typeof(BasicAttackDriver).GetMethod("SetHold");
        private static readonly MethodInfo DriverReleaseHold =
            typeof(BasicAttackDriver).GetMethod("ReleaseHold");

        // --- lifecycle ---------------------------------------------------------------------------

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_controls != null) _controls.BasicAttackPerformed -= OnBasicAttack;
            foreach (GameObject go in _spawned)
                if (go) UnityEngine.Object.DestroyImmediate(go);
            _spawned.Clear();
            if (_weapon) UnityEngine.Object.DestroyImmediate(_weapon);
            if (_navMeshInstance.valid) _navMeshInstance.Remove();
            if (_navMesh) UnityEngine.Object.DestroyImmediate(_navMesh);
            _controls = null;
            _player = null;
            _agent = null;
            _attackCount = 0;
        }

        // --- rig helpers -------------------------------------------------------------------------

        /// <summary>Bakes a flat 60x60 floor NavMesh so the player's agent can path/approach.</summary>
        private bool TryBakeFullFloor()
        {
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(60f, 0.2f, 60f),
                transform = Matrix4x4.TRS(Vector3.down * 0.1f, Quaternion.identity, Vector3.one),
                area = 0
            };
            _navMesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource> { source },
                new Bounds(Vector3.zero, new Vector3(60f, 5f, 60f)), Vector3.zero, Quaternion.identity);
            if (_navMesh == null) return false;
            _navMeshInstance = NavMesh.AddNavMeshData(_navMesh);
            return _navMeshInstance.valid;
        }

        /// <summary>
        /// Builds a minimal but real <see cref="PlayerActor"/> with a <see cref="NavMeshAgent"/> warped
        /// onto the mesh, an <see cref="Animator"/> (so ValidateComponents logs no error) and a wired
        /// <see cref="CharControlScript"/>. Mirrors the serialize-while-inactive pattern of the other
        /// PlayMode rigs. Subscribes the attack counter to the real BasicAttackPerformed event.
        /// </summary>
        private void BuildPlayer(Vector3 position)
        {
            _weapon = ScriptableObject.CreateInstance<WeaponScript>();
            _weapon.weaponName = "TestGauntlet";
            _weapon.attackDamage = 10f;
            // A short attack reach so the player must actually close before an attack can resolve.
            _weapon.attackDistance = 1.8f;
            _weapon.attackSpeed = 0.3f; // deterministic attackInterval; overwritten per-test as needed

            var go = new GameObject("TestPlayer");
            go.SetActive(false);
            _spawned.Add(go);

            var hand = new GameObject("Hand");
            hand.transform.SetParent(go.transform);

            _player = go.AddComponent<PlayerActor>();
            SetPrivate(_player, "handTransform", hand.transform);
            SetPrivate(_player, "startingWeapon", _weapon);
            SetPrivate(_player, "weapon", _weapon);
            SetPrivate(_player, "stats", new PlayerArpgStats
            {
                baseDamage = 0f, criticalChance = 0f, damageMultiplier = 1f,
                increasedDamagePercent = 0f, flatDamageBonus = 0f,
            });

            _agent = go.AddComponent<NavMeshAgent>();
            _agent.radius = 0.3f;
            _agent.height = 1.8f;
            _agent.speed = 8f;
            _agent.acceleration = 80f;
            _agent.angularSpeed = 999f;
            _agent.autoBraking = false;

            go.AddComponent<Animator>();

            _controls = go.AddComponent<CharControlScript>();
            _controls.playerActor = _player;

            go.transform.position = position;
            go.SetActive(true);
            if (_agent.enabled) _agent.Warp(position);

            _player.EquipWeapon(_weapon);

            _attackCount = 0;
            _controls.BasicAttackPerformed += OnBasicAttack;
        }

        private void OnBasicAttack() => _attackCount++;

        /// <summary>Creates a live enemy with an <see cref="Actor"/>, box collider and
        /// <see cref="Interactable"/> of type Enemy (so the target/follow path accepts it).</summary>
        private (Actor actor, Interactable interactable) BuildEnemy(Vector3 position, float health = 1000f)
        {
            var go = new GameObject("TestEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            var actor = go.AddComponent<Actor>();
            actor.health = health;

            var interactable = go.AddComponent<Interactable>();
            interactable.interactionType = InteractableType.Enemy;

            go.SetActive(true); // Awake wires Interactable.myActor + Actor.maxHealth
            return (actor, interactable);
        }

        // --- driver / state accessors ------------------------------------------------------------

        private BasicAttackDriver Driver() => (BasicAttackDriver)DriverField.GetValue(_controls);
        private void QueueTap() => DriverQueueTap.Invoke(Driver(), null);
        private void SetHold(bool held) => DriverSetHold.Invoke(Driver(), new object[] { held });
        private void ReleaseHold() => DriverReleaseHold.Invoke(Driver(), null);

        private void SetTarget(Interactable t) => SetTargetMethod.Invoke(_controls, new object[] { t });
        private void MoveToPosition(Vector3 p) => MoveToPositionMethod.Invoke(_controls, new object[] { p });
        private void ClearTarget() => ClearTargetMethod.Invoke(_controls, null);
        private Interactable CurrentTarget() => (Interactable)TargetField.GetValue(_controls);
        private float AttackInterval() => (float)AttackIntervalField.GetValue(_controls);
        private void SetAttackInterval(float v) => AttackIntervalField.SetValue(_controls, v);

        /// <summary>Pumps frames until the player is within attack reach of <paramref name="enemy"/>
        /// or the frame budget runs out. Returns true if it closed to reach.</summary>
        private IEnumerator ApproachUntilInRange(Actor enemy, int maxFrames = 240)
        {
            for (int frame = 0; frame < maxFrames; frame++)
            {
                yield return null;
                Vector3 d = enemy.transform.position - _player.transform.position;
                d.y = 0f;
                // EffectiveAttackRange for a melee weapon == attackDistance here (MeleeScale defaults to 1).
                if (d.magnitude <= _weapon.attackDistance + 0.05f) yield break;
            }
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(fi, Is.Not.Null, $"Expected field '{field}' on {target.GetType().Name}");
            fi.SetValue(target, value);
        }

        // --- tests -------------------------------------------------------------------------------

        // Feature: combat-foundation-rework, task 13.6
        // A target in range with NO attack input produces zero BasicAttackPerformed across many frames:
        // the removed auto-combat means mere target existence never attacks (R1.1/R1.7/R1.11). The
        // player sits inside reach of the enemy, holds the enemy as its Alvo_Interno, and the live Update
        // loop (FollowTarget -> TryAttackTarget) runs for N frames without a tap or hold being fed.
        // Validates: Requirements 1.1, 1.11
        [UnityTest]
        public IEnumerator TargetInRange_NoInput_FiresNoAttacks()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(1.2f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            // Select the enemy as the Alvo_Interno but queue NO tap and set NO hold.
            SetTarget(interactable);
            Assert.That(CurrentTarget(), Is.EqualTo(interactable), "the enemy must be the Alvo_Interno");

            for (int frame = 0; frame < 60; frame++) yield return null;

            Assert.That(_attackCount, Is.Zero,
                "a target in range with no input must never attack — auto-combat is removed (R1.1/R1.11)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // Ataque_Alvo: selecting an eligible enemy and queueing exactly one Toque_de_Ataque drives
        // exactly one BasicAttackPerformed through the approach/follow path (R1.1). The player closes to
        // reach under the live loop, then TryAttackTarget consumes the single pending tap into one swing.
        // Validates: Requirements 1.1
        [UnityTest]
        public IEnumerator TargetedTap_FiresExactlyOneAttack()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(5f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            // Ataque_Alvo = select the enemy under the cursor + queue a single tap (the exact pair
            // HandleInteractable performs on an explicit enemy click).
            SetTarget(interactable);
            QueueTap();

            // Let the player approach to reach and the live loop resolve the single swing.
            yield return ApproachUntilInRange(enemy);
            for (int frame = 0; frame < 30; frame++) yield return null;

            Assert.That(_attackCount, Is.EqualTo(1),
                "a single Ataque_Alvo tap must fire exactly one BasicAttackPerformed (R1.1)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // Segurar_Ataque: with the enemy in range and SetHold(true), the driver authorizes repeats at
        // the configured attackInterval, so the attack count grows by roughly one per interval over time
        // (R1.3). A long busy window would mask cadence, so the test keeps the enemy at huge health and
        // measures that MORE than one but a bounded number of attacks land across a few intervals.
        // Validates: Requirements 1.3
        [UnityTest]
        public IEnumerator Hold_RepeatsAtAttackInterval()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(1.0f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            SetTarget(interactable);
            // Let one frame prime RefreshWeaponStats' cache so our interval override below is not
            // immediately recomputed from the weapon's attackSpeed on the next Update.
            yield return null;
            // Start already in reach so cadence (not travel) governs the count. Give a short but
            // measurable interval so a few repeats fit a bounded frame budget.
            SetAttackInterval(0.1f);
            SetHold(true);

            float interval = AttackInterval();
            Assert.That(interval, Is.GreaterThan(0f), "the test needs a positive attack interval");

            // Run for ~4 intervals of wall-clock time under the live loop.
            float elapsed = 0f;
            float window = interval * 4f;
            int guard = 0;
            while (elapsed < window && guard < 2000)
            {
                yield return null;
                elapsed += Time.deltaTime;
                guard++;
            }

            // The first hold attack is allowed immediately, then one per interval: across ~4 intervals
            // we expect strictly more than one and no more than the frame budget could ever allow.
            Assert.That(_attackCount, Is.GreaterThan(1),
                "holding attack must repeat across intervals, not fire only once (R1.3)");
            Assert.That(_attackCount, Is.LessThanOrEqualTo(8),
                "hold repeats must be gated by attackInterval, not fire every frame (R1.3)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // Releasing after a hold stops FUTURE repeats without cutting an already-authorized hit (R1.4),
        // and releasing after a single tap preserves that one execution (R1.5). This drives the driver
        // seam directly against a frozen clock-free counter: a tap-only sequence keeps its one swing after
        // release; a hold sequence stops producing further swings once released.
        // Validates: Requirements 1.4, 1.5
        [UnityTest]
        public IEnumerator Release_StopsHoldRepeats_PreservesTapExecution()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(1.0f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            SetTarget(interactable);
            // Prime the weapon-stat cache before overriding the interval (see Hold test note).
            yield return null;
            SetAttackInterval(0.1f);

            // --- R1.5: a single tap, then release, preserves exactly the one authorized execution.
            QueueTap();
            ReleaseHold(); // release after a tap must NOT remove the pending/executed tap
            yield return null;
            for (int frame = 0; frame < 20; frame++) yield return null;
            Assert.That(_attackCount, Is.EqualTo(1),
                "release after a tap preserves the single authorized execution (R1.5)");

            // --- R1.4: hold a while to accrue repeats, then release and confirm no further repeats.
            SetHold(true);
            float interval = AttackInterval();
            float elapsed = 0f;
            int guard = 0;
            while (elapsed < interval * 3f && guard < 2000)
            {
                yield return null;
                elapsed += Time.deltaTime;
                guard++;
            }
            int afterHold = _attackCount;
            Assert.That(afterHold, Is.GreaterThan(1), "the hold must have produced repeats before release");

            ReleaseHold();
            // Pump well beyond several intervals: with hold released, no new repeat may be authorized.
            elapsed = 0f; guard = 0;
            while (elapsed < interval * 4f && guard < 2000)
            {
                yield return null;
                elapsed += Time.deltaTime;
                guard++;
            }
            Assert.That(_attackCount, Is.EqualTo(afterHold),
                "releasing hold stops future repeats without cutting the started hit (R1.4)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // Ataque_Direcional strikes in the aimed direction WITHOUT approaching or selecting a target:
        // TryDirectionalBasicAttack clears any target, never paths toward an enemy, and still fires one
        // BasicAttackPerformed in place (R1.2). The enemy sits well out of reach; the player must not
        // close any meaningful distance toward it and the Alvo_Interno must stay cleared.
        // Validates: Requirements 1.2
        [UnityTest]
        public IEnumerator DirectionalAttack_StrikesWithoutApproachingOrSelecting()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(10f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            float distBefore = Vector3.Distance(_player.transform.position, enemy.transform.position);

            // Aim toward the enemy but issue a Ataque_Direcional: it strikes in place, no approach.
            bool struck = _controls.TryDirectionalBasicAttack(enemy.transform.position);
            Assert.That(struck, Is.True, "a directional basic from a valid state must execute (R1.2)");

            for (int frame = 0; frame < 30; frame++) yield return null;

            Assert.That(_attackCount, Is.EqualTo(1),
                "a directional attack fires exactly one BasicAttackPerformed in place (R1.2)");
            Assert.That(CurrentTarget(), Is.Null,
                "a directional attack never selects a target (R1.2)");

            float distAfter = Vector3.Distance(_player.transform.position, enemy.transform.position);
            Assert.That(distAfter, Is.GreaterThan(distBefore - 0.5f),
                "a directional attack must not auto-approach the enemy (R1.2)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // Ordem_de_Movimento outside the Zona_Morta_de_Movimento moves the player and clears the current
        // combat intent/target (R1.9); a click inside the deadzone preserves the current intent (R1.10).
        // The move path (MoveToPosition) clears the Alvo_Interno and the deadzone check is the exact
        // WorldClickResolver.IsNearPlayer seam ClickToMove gates on.
        // Validates: Requirements 1.9, 1.10
        [UnityTest]
        public IEnumerator MoveOrder_OutsideDeadzoneClears_InsideDeadzonePreserves()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(3f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            float deadZone = (float)MovementDeadZoneField.GetValue(_controls);
            Assert.That(deadZone, Is.GreaterThan(0f), "the movement deadzone must be positive");

            // --- R1.10: a click INSIDE the deadzone is not a move order; the intent is preserved.
            SetTarget(interactable);
            QueueTap();
            Vector3 insidePoint = _player.transform.position + new Vector3(deadZone * 0.5f, 0f, 0f);
            Assert.That(WorldClickResolver.IsNearPlayer(insidePoint, _player.transform.position, deadZone),
                Is.True, "the inside point must fall within the deadzone");
            // ClickToMove would early-return here (no MoveToPosition), so the target/intent survives.
            Assert.That(CurrentTarget(), Is.EqualTo(interactable),
                "a click inside the deadzone preserves the current Intencao_de_Comando (R1.10)");

            // --- R1.9: a move order OUTSIDE the deadzone moves and clears the pending combat intent.
            Vector3 outsidePoint = _player.transform.position + new Vector3(6f, 0f, 0f);
            Assert.That(WorldClickResolver.IsNearPlayer(outsidePoint, _player.transform.position, deadZone),
                Is.False, "the outside point must fall beyond the deadzone");

            Vector3 startPos = _player.transform.position;
            MoveToPosition(outsidePoint);
            Assert.That(CurrentTarget(), Is.Null,
                "a move order outside the deadzone clears the combat target/intent (R1.9)");

            // The move order must actually displace the player toward the destination.
            for (int frame = 0; frame < 120; frame++)
            {
                yield return null;
                if (Vector3.Distance(_player.transform.position, startPos) > 1f) break;
            }
            Assert.That(Vector3.Distance(_player.transform.position, startPos), Is.GreaterThan(1f),
                "a move order outside the deadzone moves the player (R1.9)");

            // And the cleared intent must not attack even though an enemy existed.
            Assert.That(_attackCount, Is.Zero,
                "after a move order the superseded combat intent fires no attack (R1.9)");
        }

        // Feature: combat-foundation-rework, task 13.6
        // No eligible target under the cursor makes a Ataque_Alvo a total no-op: with no Alvo_Interno
        // selected, queuing a tap and running the live loop fires no BasicAttackPerformed, selects no
        // enemy by proximity, and leaves state untouched (R1.11). An enemy exists in the scene but is
        // NOT selected, standing in for "nothing eligible under the cursor".
        // Validates: Requirements 1.11
        [UnityTest]
        public IEnumerator NoEligibleTarget_TargetedAttackIsNoOp()
        {
            if (!TryBakeFullFloor()) Assert.Ignore("Harness could not bake a runtime NavMesh.");

            BuildPlayer(Vector3.zero);
            // An enemy exists but is deliberately NOT selected as the target.
            (Actor enemy, Interactable interactable) = BuildEnemy(new Vector3(2f, 0f, 0f));
            Assert.That(_agent.isOnNavMesh, Is.True);

            Assert.That(CurrentTarget(), Is.Null, "no target is selected for this case");

            // A Ataque_Alvo with nothing selected: a stray tap on the driver must not spontaneously pick
            // an enemy by proximity, so the follow/attack path stays idle.
            QueueTap();
            for (int frame = 0; frame < 60; frame++) yield return null;

            Assert.That(_attackCount, Is.Zero,
                "a targeted attack with no eligible target is a total no-op (R1.11)");
            Assert.That(CurrentTarget(), Is.Null,
                "no enemy is selected by proximity when nothing is under the cursor (R1.11)");
        }
    }
}
