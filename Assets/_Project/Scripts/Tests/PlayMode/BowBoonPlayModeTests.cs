using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.AI;

namespace TechGuy.Tests
{
    public sealed class BowBoonPlayModeTests
    {
        private ArsenalProjectileTestRig _rig;
        private BowBoonCombat _bow;
        private AbilityHolder _holder;
        private ProtocolRuntime _runtime;
        private ArsenalAbility[] _abilities;
        private float _savedScale, _savedCapture;
        private Camera[] _sceneCameras;
        private NavMeshData _navigation;
        private NavMeshDataInstance _navigationInstance;
        private static void Field(object target, string name, object value)
        { var type = target.GetType(); FieldInfo field = null; while (type != null && field == null) { field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); type = type.BaseType; } Assert.NotNull(field, name); field.SetValue(target, value); }
        [SetUp] public void SetUp()
        {
            _savedScale = Time.timeScale; _savedCapture = Time.captureDeltaTime; Time.timeScale = 1; Time.captureDeltaTime = 1f / 60f;
            // These fixtures explicitly aim with transform.forward. A camera retained by a
            // preceding scene would reinterpret the physical cursor during charge/release.
            _sceneCameras = Camera.allCameras;
            foreach (var camera in _sceneCameras) if (camera) camera.enabled = false;
            _rig = new ArsenalProjectileTestRig();
        }
        [TearDown] public void TearDown()
        {
            foreach (var camera in _sceneCameras) if (camera) camera.enabled = true;
            Time.timeScale = _savedScale; Time.captureDeltaTime = _savedCapture;
            var ownerField = typeof(ArsenalProjectile).GetField("_owner", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var arrow in UnityEngine.Object.FindObjectsByType<ArsenalProjectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if ((PlayerActor)ownerField.GetValue(arrow) == _rig.Owner) UnityEngine.Object.DestroyImmediate(arrow.gameObject);
            _rig?.TearDown();
            if (_navigationInstance.valid) _navigationInstance.Remove();
            if (_navigation) UnityEngine.Object.DestroyImmediate(_navigation);
            if (_abilities != null) foreach (var ability in _abilities) if (ability) UnityEngine.Object.DestroyImmediate(ability);
            ArsenalProjectileTestRig.ResetArrowPoolReferences();
        }
        private IEnumerator Build(bool bond = true, bool judgment = true, bool mines = true, bool seal = false, bool returning = false, bool duo = false, int sightLimit = 128, bool navigation = false, bool eclipse = false)
        {
            var player = _rig.BuildOwner(); Field(_rig.Weapon, "_firesArrows", true);
            if (navigation)
            {
                var navSource = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box, size = new Vector3(30, .2f, 30),
                    transform = Matrix4x4.TRS(Vector3.down * .1f, Quaternion.identity, Vector3.one) };
                _navigation = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                    new System.Collections.Generic.List<NavMeshBuildSource> { navSource }, new Bounds(Vector3.zero, Vector3.one * 40), Vector3.zero, Quaternion.identity);
                Assert.NotNull(_navigation); _navigationInstance = NavMesh.AddNavMeshData(_navigation);
                var agent = player.gameObject.AddComponent<NavMeshAgent>(); Assert.IsTrue(agent.isOnNavMesh);
            }
            _abilities = new ArsenalAbility[4]; _rig.Weapon.abilities = new Ability[4];
            for (int i = 0; i < 4; i++)
            {
                var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
                JsonUtility.FromJsonOverwrite("{\"_kind\":0,\"_windup\":0.02,\"_hits\":1,\"_interval\":0.05,\"_range\":12,\"_width\":1,\"_damageMultiplier\":3}", ability);
                ability.ConfigureRunPresentation("Bow test", 10, SkillGlyphKind.Arrow); ability.cooldownTime = .1f;
                _abilities[i] = ability; _rig.Weapon.abilities[i] = ability;
            }
            _holder = player.gameObject.AddComponent<AbilityHolder>(); _holder.RefreshLoadout();
            var source = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Field(source.Limits, "_sightTestsPerQuery", sightLimit);
            Assert.IsTrue(source.TryCreateSnapshot(out var catalog, out var diagnostic), diagnostic); UnityEngine.Object.DestroyImmediate(source);
            _runtime = player.gameObject.AddComponent<ProtocolRuntime>(); _runtime.Configure(player, new ProtocolInventory(Guid.NewGuid(), catalog));
            _bow = player.GetComponent<BowBoonCombat>() ?? player.gameObject.AddComponent<BowBoonCombat>();
            yield return null;
            _bow.Configure(bond, judgment, mines, seal, returning, eclipse, duo);
            // Awake applies the project's starting balance after the generic rig assigns stats.
            // This deterministic fixture owns its damage and a real resource maximum after Awake.
            player.Stats.baseDamage = 0f; player.Stats.criticalChance = 0f;
            Field(player, "baseMaxMana", 100f); Field(player, "_manaRegenerationPercentPerSecond", 0f);
            player.RefreshResourceStats(fillToMax: true); player.transform.rotation = Quaternion.identity;
            Assert.AreEqual(100f, player.maxMana); Assert.AreEqual(100f, player.mana);
            Assert.AreEqual(10f, player.RollAttackDamage(_rig.Weapon.attackDamage).Amount, .001f);
        }
        private IEnumerator Idle()
        { float end = Time.realtimeSinceStartup + 5; while (_bow.IsExecuting && Time.realtimeSinceStartup < end) yield return null; Assert.IsFalse(_bow.IsExecuting); }
        private IEnumerator Until(Func<bool> condition, string reason = null)
        { float end = Time.realtimeSinceStartup + 5; while (!condition() && Time.realtimeSinceStartup < end) yield return null; Assert.IsTrue(condition(), reason ?? "Bow scenario exceeded its real-time guard."); }
        [UnityTest] public IEnumerator BowBoonNativeQMarksOnceAndPriorityRequiresLosConeRangeAndGeneration()
        {
            yield return Build(); var target = _rig.BuildEnemy(new Vector3(1,0,4));
            // Aim directly at the target for Q; priority must not substitute a target before confirmation.
            _rig.Owner.transform.forward = new Vector3(1,0,4).normalized;
            int used = 0; _holder.AbilityUsed += _ => used++;
            float mana = _rig.Owner.mana; long roots = _runtime.Budget.RootsCreated;
            Assert.IsTrue(_holder.TryUseAbility(0)); yield return Until(() => _bow.PriorityTarget == target);
            Assert.AreEqual(mana - 10, _rig.Owner.mana, .001f); Assert.AreEqual(1, used); Assert.AreEqual(1, _runtime.Budget.RootsCreated - roots);
            Vector3 desired = BowBoonCombat.ResolvePriority(_rig.Owner, Vector3.up, Vector3.forward, 12);
            Assert.Greater(desired.x, .1f);
            Assert.AreEqual(Vector3.forward, BowBoonCombat.ResolvePriority(_rig.Owner, Vector3.up, Vector3.forward, 2));
            Assert.AreEqual(Vector3.back, BowBoonCombat.ResolvePriority(_rig.Owner, Vector3.up, Vector3.back, 12));
            var wall = _rig.BuildWall(new Vector3(.5f,1,2), new Vector3(2,2,.2f)); Physics.SyncTransforms();
            Assert.AreEqual(Vector3.forward, BowBoonCombat.ResolvePriority(_rig.Owner, Vector3.up, Vector3.forward, 12));
            wall.SetActive(false); target.gameObject.SetActive(false); target.gameObject.SetActive(true); Physics.SyncTransforms();
            Assert.IsNull(_bow.PriorityTarget); yield return Idle();
        }
        [UnityTest] public IEnumerator BowBoonJudgmentShortAndFullUseDifferentCorridorsAndOneManaDebit()
        {
            yield return Build(); var center = _rig.BuildEnemy(Vector3.forward * 4); var lateral = _rig.BuildEnemy(new Vector3(.9f,0,5));
            Assert.IsTrue(_holder.TryUseAbility(3)); yield return Idle(); yield return new WaitForSeconds(.6f);
            Assert.AreEqual(982, center.health, .01f); Assert.AreEqual(1000, lateral.health, .01f);
            yield return new WaitForSeconds(.2f); _rig.Owner.mana = 100;
            Assert.IsTrue(_holder.TryUseAbility(3)); Field(_bow, "_held", BowBoonModel.FullChargeSeconds);
            yield return Idle(); yield return new WaitForSeconds(.6f);
            Assert.AreEqual(952, center.health, .01f); Assert.AreEqual(970, lateral.health, .01f); Assert.AreEqual(90, _rig.Owner.mana, .001f);
        }
        [UnityTest] public IEnumerator BowBoonChargedShotConsumesBondOnlyAfterConfirmedNativeHp()
        {
            yield return Build(); var target = _rig.BuildEnemy(Vector3.forward * 4);
            Assert.IsTrue(_holder.TryUseAbility(0)); yield return Until(() => _bow.PriorityTarget == target); yield return Idle();
            float before = target.health;
            Assert.IsTrue(_holder.TryUseAbility(3)); Field(_bow, "_held", BowBoonModel.FullChargeSeconds);
            yield return Idle(); yield return new WaitForSeconds(.5f);
            Assert.AreEqual(before - 37.5f, target.health, .01f); Assert.IsNull(_bow.PriorityTarget);
        }
        [UnityTest] public IEnumerator BowBoonMineGardenArmsOnceAndEncounterCleanupDropsAllReservations()
        {
            yield return Build(); Assert.IsTrue(_holder.TryUseAbility(2)); yield return Idle(); Assert.AreEqual(3, _bow.ActiveMines);
            var target = _rig.BuildEnemy(Vector3.forward * 5);
            yield return new WaitForSeconds(.4f);
            Assert.AreEqual(1, _bow.MinesDetonated); Assert.AreEqual(990, target.health, .01f); Assert.AreEqual(2, _bow.ActiveMines);
            _bow.StopEncounter(); Assert.AreEqual(0, _bow.ActiveMines); Assert.AreEqual(0, _runtime.Budget.LiveRoots);
            float hp = target.health; yield return new WaitForSeconds(.3f); Assert.AreEqual(hp, target.health);
        }
        [UnityTest] public IEnumerator BowBoonPauseAndHitstopFreezeChargeAndMines()
        {
            yield return Build(); Assert.IsTrue(_holder.TryUseAbility(2)); yield return Idle();
            var target = _rig.BuildEnemy(Vector3.forward * 5);
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(.2f); Assert.AreEqual(1000, target.health); Assert.AreEqual(3, _bow.ActiveMines);
            Time.timeScale = 1; var hitstop = _rig.Owner.GetComponent<HitStopRunner>() ?? _rig.Owner.gameObject.AddComponent<HitStopRunner>(); hitstop.Apply(.3f);
            yield return new WaitForSecondsRealtime(.1f); Assert.AreEqual(1000, target.health); Assert.AreEqual(3, _bow.ActiveMines);
            yield return Until(() => _bow.MinesDetonated == 1);
            Assert.IsTrue(_holder.TryUseAbility(3)); Time.timeScale = 0;
            float charge = _bow.ChargeRatio; yield return new WaitForSecondsRealtime(.2f); Assert.AreEqual(charge, _bow.ChargeRatio); Assert.IsTrue(_bow.IsCharging);
            Time.timeScale = 1; _bow.Cancel(); Assert.IsFalse(_bow.IsCharging);
        }
        [UnityTest] public IEnumerator BowBoonDamageInterruptsChargeAndWallsBlockBothJudgmentVariants()
        {
            yield return Build(); var target = _rig.BuildEnemy(Vector3.forward * 5);
            _rig.BuildWall(new Vector3(0,1,2), new Vector3(8,3,.3f)); Physics.SyncTransforms();
            Assert.IsTrue(_holder.TryUseAbility(3)); _rig.Owner.TakeDamage(1); Assert.IsFalse(_bow.IsExecuting);
            yield return new WaitForSeconds(.3f); Assert.AreEqual(1000, target.health);
            Assert.IsTrue(_holder.TryUseAbility(3)); Field(_bow, "_held", BowBoonModel.FullChargeSeconds);
            yield return Idle(); yield return new WaitForSeconds(.5f); Assert.AreEqual(1000, target.health);
        }
        [UnityTest] public IEnumerator BowBoonReturnUsesOriginalRootAndCannotMarkOrReturnTwice()
        {
            yield return Build(returning: true, seal: true); var target = _rig.BuildEnemy(Vector3.forward * 4);
            long before = _runtime.Budget.RootsCreated;
            Assert.IsTrue(_holder.TryUseAbility(3)); Field(_bow, "_held", BowBoonModel.FullChargeSeconds);
            yield return Idle(); yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(958, target.health, .01f); Assert.IsFalse(_bow.HasConsumableMark(target));
            Assert.AreEqual(1, _runtime.Budget.RootsCreated - before); Assert.AreEqual(0, _runtime.Budget.LiveRoots);
            float hp = target.health; yield return new WaitForSeconds(.4f); Assert.AreEqual(hp, target.health);
        }
        [UnityTest] public IEnumerator BowBoonDetonatorConsumesOneSealAndOnlyOneEligibleMine()
        {
            yield return Build(seal: true, duo: true);
            Assert.IsTrue(_holder.TryUseAbility(2)); yield return Idle(); yield return new WaitForSeconds(.2f);
            var target = _rig.BuildEnemy(new Vector3(-5,0,5));
            _rig.Owner.transform.forward = target.transform.position.normalized;
            Assert.IsTrue(_holder.TryUseAbility(0)); yield return Until(() => _bow.PriorityTarget == target); yield return Idle();
            Assert.AreEqual(0, _bow.MinesDetonated, "Marked target is outside the 2.5m proximity trigger.");
            ArsenalProjectile.Fire(_rig.Owner, Vector3.up, target.transform.position.normalized, 10, 1, 12, false, Color.white);
            yield return Until(() => _bow.MinesDetonated == 1);
            Assert.AreEqual(2, _bow.ActiveMines);
            var seals = (System.Collections.IDictionary)typeof(BowBoonCombat).GetField("_seals", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_bow);
            Assert.AreEqual(0, seals.Count, "The Duo consumes its seal; Q priority remains a separate mark.");
            yield return new WaitForSeconds(.5f); Assert.AreEqual(1, _bow.MinesDetonated);
        }
        [UnityTest] public IEnumerator BowBoonMineCrowdsShareOriginalRootAndNeverExceed32DerivedImpacts()
        {
            yield return Build(bond: false, judgment: false);
            foreach (int count in new[] { 8, 32, 64 })
            {
                Assert.IsTrue(_holder.TryUseAbility(2)); yield return Idle();
                var mines = (System.Collections.IList)typeof(BowBoonCombat).GetField("_mines", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_bow);
                var execution = (ProtocolExecution)mines[0].GetType().GetField("Execution").GetValue(mines[0]); var root = execution.Root;
                var targets = new System.Collections.Generic.List<Actor>();
                for (int i = 0; i < count; i++) targets.Add(_rig.BuildEnemy(new Vector3((i % 3 - 1) * 1.6f, 0, 5 + i / 3 * .01f)));
                int detonations = _bow.MinesDetonated;
                yield return Until(() => _bow.MinesDetonated == detonations + 3);
                Assert.Greater(root.ImpactAttempts, 0); Assert.LessOrEqual(root.ImpactAttempts, 32);
                if (count == 64) Assert.AreEqual(32, root.ImpactAttempts);
                foreach (var actor in targets) actor.gameObject.SetActive(false);
                _bow.StopEncounter(); yield return new WaitForSeconds(.2f);
            }
        }
        [UnityTest] public IEnumerator BowBoonImmunityDoesNotConsumeBondOrManufactureAConfirmedHit()
        {
            yield return Build(); var target = _rig.BuildEnemy(Vector3.forward * 4);
            Assert.IsTrue(_holder.TryUseAbility(0)); yield return Until(() => _bow.PriorityTarget == target); yield return Idle();
            target.AddDamageImmunity(_bow); float hp = target.health;
            Assert.IsTrue(_holder.TryUseAbility(3)); Field(_bow, "_held", BowBoonModel.FullChargeSeconds);
            yield return Idle(); yield return new WaitForSeconds(.5f);
            Assert.AreEqual(hp, target.health); Assert.AreEqual(target, _bow.PriorityTarget);
            target.RemoveDamageImmunity(_bow);
        }
        [UnityTest] public IEnumerator BowBoonDetonatorDefersWithoutConsumingMineOrSealWhenQueryBudgetIsExhausted()
        {
            yield return Build(seal: true, duo: true);
            Assert.IsTrue(_holder.TryUseAbility(2)); yield return Idle(); yield return new WaitForSeconds(.3f);
            var target = _rig.BuildEnemy(new Vector3(-5, 0, 5));
            _bow.ConfirmArrow(target, target.CombatGeneration, true, false);
            while (_runtime.Budget.ReserveQuery()) { }
            int dropped = _runtime.Budget.DroppedQueries;
            _bow.ConfirmArrow(target, target.CombatGeneration, true, false);
            Assert.AreEqual(3, _bow.ActiveMines); Assert.AreEqual(0, _bow.MinesDetonated);
            Assert.Greater(_runtime.Budget.DroppedQueries, dropped);
            var seals = (System.Collections.IDictionary)typeof(BowBoonCombat).GetField("_seals", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_bow);
            Assert.AreEqual(1, seals.Count, "Admission failure must preserve the sealed target for a later native hit.");
            yield return null;
            _bow.ConfirmArrow(target, target.CombatGeneration, true, false);
            Assert.AreEqual(1, _bow.MinesDetonated); Assert.AreEqual(2, _bow.ActiveMines); Assert.AreEqual(0, seals.Count);
        }
        [UnityTest] public IEnumerator BowBoonDetonatorHonorsConfiguredSightBudgetInsteadOfSearchingAllMines()
        {
            yield return Build(seal: true, duo: true, sightLimit: 1);
            Assert.IsTrue(_holder.TryUseAbility(2)); yield return Idle(); yield return new WaitForSeconds(.3f);
            var target = _rig.BuildEnemy(new Vector3(-2, 0, 8));
            _rig.BuildWall(new Vector3(-1.8f, 1, 6.5f), new Vector3(.15f, 2, .15f)); Physics.SyncTransforms();
            _bow.ConfirmArrow(target, target.CombatGeneration, true, false);
            _bow.ConfirmArrow(target, target.CombatGeneration, true, false);
            Assert.AreEqual(0, _bow.MinesDetonated, "The first mine's blocked sight consumes the sole LOS test; a second clear mine must not be tested.");
            Assert.AreEqual(3, _bow.ActiveMines);
        }
        [UnityTest] public IEnumerator BowBoonMineDamageReentryCannotRestoreMarksAfterEncounterCleanup()
        {
            yield return Build(seal: true, duo: true);
            Assert.IsTrue(_holder.TryUseAbility(2)); yield return Idle(); yield return new WaitForSeconds(.3f);
            var target = _rig.BuildEnemy(new Vector3(-5, 0, 5));
            _bow.ConfirmArrow(target, target.CombatGeneration, true, false);
            var victim = _rig.BuildEnemy(new Vector3(-1.6f, 0, 5));
            victim.DamageReceived += (_, __) => _bow.StopEncounter();
            _bow.ConfirmArrow(target, target.CombatGeneration, true, false);
            Assert.Less(victim.health, 1000); Assert.AreEqual(0, _bow.ActiveMines); Assert.IsNull(_bow.PriorityTarget);
            Assert.IsFalse(_bow.HasConsumableMark(target)); Assert.AreEqual(0, _runtime.Budget.LiveRoots);
            yield return null; Assert.IsFalse(_bow.HasConsumableMark(target));
        }
        [UnityTest] public IEnumerator BowBoonQAndEReuseConfiguredReducedMovementAndRestoreItOnCancelAndCompletion()
        {
            yield return Build(navigation: true);
            var agent = _rig.Owner.GetComponent<NavMeshAgent>();
            var arsenal = _rig.Owner.GetComponent<ArsenalCombat>(); Assert.NotNull(arsenal);
            var config = new BowFireMovementConfig { MoveFraction = .3f };
            Field(arsenal, "_bowFireMovementConfig", config); Field(arsenal, "_bowFireMovement", new BowFireMovement(config));
            agent.speed = 7; agent.isStopped = true; bool rotation = agent.updateRotation;
            Assert.IsTrue(_holder.TryUseAbility(0)); Assert.IsTrue(_bow.AllowsMovementWhileFiring);
            Assert.AreEqual(2.1f, agent.speed, .001f); Assert.IsFalse(agent.isStopped);
            _bow.Cancel(); Assert.AreEqual(7, agent.speed, .001f); Assert.IsTrue(agent.isStopped); Assert.AreEqual(rotation, agent.updateRotation);
            Assert.IsTrue(_holder.TryUseAbility(2)); Assert.AreEqual(2.1f, agent.speed, .001f);
            yield return Idle(); Assert.AreEqual(7, agent.speed, .001f); Assert.IsTrue(agent.isStopped);
            Assert.IsTrue(_holder.TryUseAbility(3)); Assert.IsTrue(agent.isStopped); Assert.AreEqual(7, agent.speed, .001f);
            _bow.Cancel(); Assert.AreEqual(7, agent.speed, .001f); Assert.IsTrue(agent.isStopped);
        }
        [UnityTest] public IEnumerator BowBoonReboundRUsesActualHoldAndReleaseWithoutASecondActivation()
        {
            const string pref = "TechGuy.Settings.Binding.Skill4";
            bool existed = PlayerPrefs.HasKey(pref); int previous = PlayerPrefs.GetInt(pref);
            var oldKeyboard = UnityEngine.InputSystem.Keyboard.current;
            bool oldKeyboardEnabled = oldKeyboard != null && oldKeyboard.enabled;
            var settings = UnityEngine.InputSystem.InputSystem.settings;
            var oldBackground = settings.backgroundBehavior;
            var oldEditorInput = settings.editorInputBehaviorInPlayMode;
            UnityEngine.InputSystem.Keyboard keyboard = null;
            try
            {
                // MCP tests run without Game View focus; isolate the synthetic keyboard and
                // keep its held state routed to the actual player loop across updates.
                settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
                settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                if (oldKeyboardEnabled) UnityEngine.InputSystem.InputSystem.DisableDevice(oldKeyboard);
                keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>(); keyboard.MakeCurrent();
                PlayerPrefs.SetInt(pref, (int)KeyCode.F11);
                yield return Build(); var target = _rig.BuildEnemy(Vector3.forward * 4); int used = 0; _holder.AbilityUsed += _ => used++;
                Assert.AreEqual(KeyCode.F11, _holder.GetAbilityKey(3), "The actual Holder must resolve the rebound R binding.");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F11));
                yield return Until(() => _bow.IsCharging, "The real rebound key press did not start Judgment charging.");
                yield return Until(() => _bow.ChargeRatio >= 1, "The held rebound key did not retain charge until the full threshold.");
                Assert.AreEqual(1000, target.health, "Holding full charge must not fire before release or max hold.");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
                yield return Idle(); yield return new WaitForSeconds(.6f);
                Assert.AreEqual(970, target.health, .01f); Assert.AreEqual(90, _rig.Owner.mana, .001f); Assert.AreEqual(1, used);
            }
            finally
            {
                if (existed) PlayerPrefs.SetInt(pref, previous); else PlayerPrefs.DeleteKey(pref);
                if (keyboard != null) UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
                settings.backgroundBehavior = oldBackground; settings.editorInputBehaviorInPlayMode = oldEditorInput;
                if (oldKeyboardEnabled) UnityEngine.InputSystem.InputSystem.EnableDevice(oldKeyboard);
                if (oldKeyboard != null) oldKeyboard.MakeCurrent();
            }
        }
        [UnityTest]
        public IEnumerator BowBoonEclipseDuoConsumesBondForOneFocusedShotThenReturnsToTheWideCorridor()
        {
            yield return Build(eclipse: true);
            var center = _rig.BuildEnemy(Vector3.forward * 4);
            var lateral = _rig.BuildEnemy(new Vector3(.9f, 0, 5));
            Assert.IsTrue(_holder.TryUseAbility(0));
            yield return Until(() => _bow.PriorityTarget == center); yield return Idle();
            float centerBefore = center.health, lateralBefore = lateral.health, mana = _rig.Owner.mana;
            long roots = _runtime.Budget.RootsCreated; int uses = 0, contacts = 0;
            _holder.AbilityUsed += slot => { if (slot == 3) uses++; };
            center.DamageReceived += (_, __) => contacts++;

            Assert.IsTrue(_holder.TryUseAbility(3)); Field(_bow, "_held", BowBoonModel.FullChargeSeconds);
            yield return Idle(); yield return new WaitForSeconds(.6f);
            Assert.AreEqual(centerBefore - 45, center.health, .01f, "A valid Bond gives the focused native shot 50%, not the normal charged mark's 25%.");
            Assert.AreEqual(lateralBefore, lateral.health, .01f, "The Eclipse shot must use the narrow aimed corridor, rather than the normal charged sweep.");
            Assert.IsNull(_bow.PriorityTarget); Assert.AreEqual(1, contacts);
            Assert.AreEqual(mana - 10, _rig.Owner.mana, .001f); Assert.AreEqual(1, uses);
            Assert.AreEqual(roots + 1, _runtime.Budget.RootsCreated); Assert.AreEqual(0, _runtime.Budget.LiveRoots);

            // The consumed Bond cannot keep either the extra damage or the focused geometry alive.
            centerBefore = center.health; lateralBefore = lateral.health;
            Assert.IsTrue(_holder.TryUseAbility(3)); Field(_bow, "_held", BowBoonModel.FullChargeSeconds);
            yield return Idle(); yield return new WaitForSeconds(.6f);
            Assert.AreEqual(centerBefore - 30, center.health, .01f);
            Assert.AreEqual(lateralBefore - 30, lateral.health, .01f);
            Assert.AreEqual(2, contacts); Assert.AreEqual(2, uses); Assert.AreEqual(mana - 20, _rig.Owner.mana, .001f);
            Assert.AreEqual(roots + 2, _runtime.Budget.RootsCreated); Assert.AreEqual(0, _runtime.Budget.LiveRoots);
        }

        [UnityTest]
        public IEnumerator BowBoonEclipseDuoCannotTreatAPredatorSealAsAValidHuntingBond()
        {
            yield return Build(seal: true, eclipse: true);
            var center = _rig.BuildEnemy(Vector3.forward * 4);
            var lateral = _rig.BuildEnemy(new Vector3(.9f, 0, 5));
            // A real ordinary native arrow creates Selo, but it cannot create Q's priority Bond.
            ArsenalProjectile.Fire(_rig.Owner, Vector3.up, Vector3.forward, 10, 1, 12, false, Color.white);
            yield return Until(() => _bow.HasConsumableMark(center));
            Assert.IsNull(_bow.PriorityTarget);
            float centerBefore = center.health, lateralBefore = lateral.health;
            Assert.IsTrue(_holder.TryUseAbility(3)); Field(_bow, "_held", BowBoonModel.FullChargeSeconds);
            yield return Idle(); yield return new WaitForSeconds(.6f);
            Assert.AreEqual(centerBefore - 37.5f, center.health, .01f, "Selo may grant the ordinary 25% charged bonus, but never Eclipse's 50%.");
            Assert.AreEqual(lateralBefore - 30, lateral.health, .01f, "Without a valid Bond the charged corridor remains wide.");
            Assert.IsFalse(_bow.HasConsumableMark(center)); Assert.IsNull(_bow.PriorityTarget);
            Assert.AreEqual(0, _runtime.Budget.LiveRoots);
        }

        private IEnumerator BuildRunOwnedEclipseWithLegacyTwinShot(int rank)
        {
            yield return Build(bond: false, judgment: false, mines: false);
            var run = _rig.Owner.gameObject.AddComponent<RunBoons>(); yield return null;
            Assert.NotNull(run.RunWeapon); Assert.NotNull(run.WeaponModifiers);
            foreach (string id in new[] { BowBoonCatalog.HuntingBondId, BowBoonCatalog.JudgmentArrowId, BowBoonCatalog.EclipseArcherId })
                Assert.IsTrue(BoonExpansionCatalog.TryAcquire(id, run.WeaponModifiers, run.RunWeapon, run.Protocols, out var error), id + ": " + error);
            WeaponRunModifiers.Definition twin = null;
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == WeaponBoon.TwinShot) { twin = definition; break; }
            Assert.NotNull(twin);
            // Retired from today's offers, retained by the normal legacy inventory API.
            for (int i = 0; i < rank; i++) Assert.IsTrue(run.WeaponModifiers.Add(twin));
            Assert.AreEqual(rank, run.WeaponModifiers.Rank(WeaponBoon.TwinShot));
            _holder.RefreshLoadout(); _bow = _rig.Owner.GetComponent<BowBoonCombat>();
            _runtime = _rig.Owner.GetComponent<ProtocolRuntime>(); _rig.Owner.mana = 100;
            Assert.AreSame(run.WeaponModifiers, _rig.Owner.RunModifiers);
            Assert.AreSame(_bow, typeof(AbilityHolder).GetField("_bowBoonCombat", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_holder),
                "RefreshLoadout must bind the existing run-owned Bow executor instead of leaving Holder disconnected.");
            Assert.IsTrue(_bow.Handles((ArsenalAbility)_holder.ActiveAbilities[0]), "The acquired Hunting Bond must be configured on the active Q executor.");
        }

        private ArsenalProjectile[] OwnedActiveArrows()
        {
            var owned = new System.Collections.Generic.List<ArsenalProjectile>();
            var owner = typeof(ArsenalProjectile).GetField("_owner", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var arrow in UnityEngine.Object.FindObjectsByType<ArsenalProjectile>())
                if (arrow.isActiveAndEnabled && (PlayerActor)owner.GetValue(arrow) == _rig.Owner) owned.Add(arrow);
            return owned.ToArray();
        }

        [UnityTest]
        public IEnumerator BowBoonEclipseDuoConcentratesLegacyTwinShotFractionsIntoOneNativeProjectile()
        {
            const int twinRank = 2;
            yield return BuildRunOwnedEclipseWithLegacyTwinShot(twinRank);
            var target = _rig.BuildEnemy(Vector3.forward * 4);
            Assert.IsTrue(_holder.TryUseAbility(0));
            yield return Until(() => _bow.PriorityTarget == target, "The real run-owned Q did not create its Hunting Bond."); yield return Idle();
            // Drain Q's real fan before observing R; no fixture-created arrow is injected here.
            yield return new WaitForSeconds(.6f); Assert.AreEqual(0, OwnedActiveArrows().Length);
            float health = target.health, mana = _rig.Owner.mana; int contacts = 0, uses = 0;
            long roots = _runtime.Budget.RootsCreated;
            target.DamageReceived += (_, __) => contacts++;
            _holder.AbilityUsed += slot => { if (slot == 3) uses++; };
            Assert.IsTrue(_holder.TryUseAbility(3)); Field(_bow, "_held", BowBoonModel.FullChargeSeconds);
            yield return Until(() => OwnedActiveArrows().Length > 0, "The real run-owned R did not create its focused projectile.");
            Assert.AreEqual(1, OwnedActiveArrows().Length, "A focused Eclipse shot concentrates the retained five-arrow fan into one aimed projectile.");
            yield return Idle(); yield return new WaitForSeconds(.6f);
            float originalFanTotal = 30f * (1 + 2 * twinRank) / (1 + .5f * twinRank);
            Assert.AreEqual(health - originalFanTotal * 1.5f, target.health, .01f,
                "Concentration preserves the sum of the retained fan's fractions, including its per-arrow divisor.");
            Assert.AreEqual(1, contacts); Assert.AreEqual(1, uses); Assert.IsNull(_bow.PriorityTarget);
            Assert.AreEqual(mana - 10, _rig.Owner.mana, .001f); Assert.AreEqual(roots + 1, _runtime.Budget.RootsCreated);
            Assert.AreEqual(0, _runtime.Budget.LiveRoots);
        }
    }
}
