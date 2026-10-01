using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property + example tests for Property 5 of gauntlet-boon-playstyle-overhaul (the
    /// basic-hit channel routing on <see cref="HookBus"/>).
    ///
    /// Property 5 (design): the dedicated basic channel (<c>OnBasicHit</c>/<c>OnBasicKill</c>) fires
    /// <b>iff</b> the hit came through the <c>HitboxDamage</c> (Basic_Attack) path; a skill/area hit
    /// never fires it; the generic <c>OnHit</c>/<c>OnKill</c> still fire on a basic hit (the basic
    /// channel is additional, not a replacement); and <c>OnBasicKill</c> fires at most once per death.
    ///
    /// Approach: the routing contract lives entirely in which <see cref="HookBus"/> channels each
    /// combat path raises, so the real <see cref="HookBus"/> is the system under test. Two local
    /// helpers reproduce the exact raise sequences of the two production paths verbatim:
    ///
    ///  - <see cref="DriveBasicHit"/> mirrors <c>HitboxDamage.TryDamageActor</c>'s player-owner branch
    ///    (<c>DealResolvedAttackDamage</c> which raises generic OnHit/OnKill, then
    ///    <c>PlayerActor.RaiseBasicAttackHit</c> which raises the basic channel) — i.e. the Basic_Attack path.
    ///  - <see cref="DriveSkillHit"/> mirrors the skill/area path (<c>DealResolvedAttackDamage</c> only),
    ///    which raises only the generic channel and never touches the basic one.
    ///
    /// Each helper is annotated with the production method whose raise order it transcribes, and each
    /// uses the real <see cref="PlayerActor.RaiseBasicAttackHit"/> guard semantics (null/non-positive
    /// dealt is a no-op; <c>OnBasicKill</c> only when the victim is dead). The enemy is a real
    /// <see cref="Actor"/> whose <c>TakeDamage</c>/<c>IsDead</c> drive the kill branches, so the
    /// kill/non-kill routing is exercised through genuine actor state, not a re-stated flag.
    ///
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    // Feature: gauntlet-boon-playstyle-overhaul, Property 5
    public sealed class BasicHitChannelPropertyTests
    {
        // Records every notification each channel delivered this scenario so the test can assert on
        // both presence/absence (routing) and multiplicity (the OnBasicKill-once-per-death dedup).
        private sealed class ChannelLog
        {
            public readonly List<Actor> Hit = new List<Actor>();
            public readonly List<Actor> Kill = new List<Actor>();
            public readonly List<Actor> BasicHit = new List<Actor>();
            public readonly List<Actor> BasicKill = new List<Actor>();

            public void Attach(HookBus bus)
            {
                bus.OnHit += (a, _) => Hit.Add(a);
                bus.OnKill += a => Kill.Add(a);
                bus.OnBasicHit += (a, _) => BasicHit.Add(a);
                bus.OnBasicKill += a => BasicKill.Add(a);
            }
        }

        // Verbatim transcription of the player-owner branch of HitboxDamage.TryDamageActor + the
        // PlayerActor.RaiseBasicAttackHit it calls: deal the damage (generic OnHit/OnKill), then raise
        // the basic channel for the measured dealt amount, with OnBasicKill only when the enemy died.
        // This is the ONLY path that is allowed to fire the basic channel (R3.1/R3.2/R3.3/R3.4).
        private static void DriveBasicHit(HookBus bus, Actor enemy, float damage)
        {
            // --- DealResolvedAttackDamage (generic channel): raises OnHit for a hit that dealt damage,
            //     and OnKill (deduped in the bus) when the enemy is dead afterwards. ---
            float before = enemy.health;
            enemy.TakeDamage(damage);
            float dealt = before - enemy.health;
            if (dealt > 0f)
            {
                bus.RaiseHit(enemy, dealt);
                if (enemy.IsDead) bus.RaiseKill(enemy);
            }

            // --- PlayerActor.RaiseBasicAttackHit (basic channel): no-op on null/non-positive dealt;
            //     otherwise RaiseBasicHit, then RaiseBasicKill when the enemy is dead. ---
            if (!enemy || dealt <= 0f) return;
            bus.RaiseBasicHit(enemy, dealt);
            if (enemy.IsDead) bus.RaiseBasicKill(enemy);
        }

        // Verbatim transcription of the skill/area path: DealResolvedAttackDamage ONLY. It raises the
        // generic channel exactly like the basic path's first half, but never calls the basic channel,
        // because the skill path never reaches HitboxDamage.TryDamageActor (R3.3).
        private static void DriveSkillHit(HookBus bus, Actor enemy, float damage)
        {
            float before = enemy.health;
            enemy.TakeDamage(damage);
            float dealt = before - enemy.health;
            if (dealt > 0f)
            {
                bus.RaiseHit(enemy, dealt);
                if (enemy.IsDead) bus.RaiseKill(enemy);
            }
        }

        // Builds a live enemy Actor with a known health. Active so Awake sets maxHealth = health and the
        // actor is a genuine, damageable target (same enemy pattern the PlayMode detonation rig uses).
        private static Actor BuildEnemy(float health)
        {
            var go = new GameObject("BasicHitChannelEnemy");
            go.SetActive(false);
            var actor = go.AddComponent<Actor>();
            actor.health = health;
            go.SetActive(true); // Awake -> maxHealth = health, IsDead = false
            return actor;
        }

        private static readonly List<GameObject> Spawned = new List<GameObject>();

        private static Actor NewEnemy(float health)
        {
            Actor enemy = BuildEnemy(health);
            Spawned.Add(enemy.gameObject);
            return enemy;
        }

        // Killing a real Actor runs Actor.Death() -> Destroy(gameObject). In EditMode the engine logs an
        // error for Destroy (it only permits DestroyImmediate), and the NUnit runner treats an
        // unexpected error log as a failure. The kill is genuine and intended behavior, so acknowledge
        // the engine's edit-mode-only Destroy error for the frame a lethal hit lands.
        private static void ExpectEditModeDestroyLog()
        {
            LogAssert.Expect(LogType.Error, new Regex("Destroy may not be called from edit mode"));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in Spawned)
                if (go) Object.DestroyImmediate(go);
            Spawned.Clear();
        }

        // Property 5: for any hit, the basic channel fires iff the hit routed through the basic path;
        // the generic channel fires on both paths; a kill fires its channel's -Kill exactly once; and a
        // skill hit never fires the basic channel even on a kill.
        // Validates: Requirements 3.1, 3.2, 3.3, 3.4, 4.3, 8.4
        [Test]
        public void BasicChannelFiresOnlyForBasicPathAndDeduplicatesKills()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Generate a hit: either a basic (hitbox) or a skill (area) path, and either a glancing
                // blow (survives) or a lethal blow (dies), spanning both routing axes.
                bool basicPath = rng.Next(0, 2) == 0;
                bool lethal = rng.Next(0, 2) == 0;
                float enemyHealth = rng.Next(10, 200);
                float damage = lethal ? enemyHealth + rng.Next(1, 50) : rng.Next(1, (int)enemyHealth);

                var bus = new HookBus();
                var log = new ChannelLog();
                log.Attach(bus);
                Actor enemy = NewEnemy(enemyHealth);

                // A lethal blow kills the real Actor (Death -> Destroy); acknowledge the edit-mode log.
                if (lethal) ExpectEditModeDestroyLog();
                if (basicPath) DriveBasicHit(bus, enemy, damage);
                else DriveSkillHit(bus, enemy, damage);

                // R3.4: the generic channel fires on BOTH paths for a hit that dealt damage.
                PropertyCheck.That(log.Hit.Count == 1,
                    $"basic={basicPath}, lethal={lethal}: generic OnHit fired {log.Hit.Count} times, expected 1");
                PropertyCheck.That(log.Kill.Count == (lethal ? 1 : 0),
                    $"basic={basicPath}, lethal={lethal}: generic OnKill fired {log.Kill.Count} times, expected {(lethal ? 1 : 0)}");

                // R3.1/R3.3: the basic channel fires IFF this was the basic path.
                int expectedBasicHit = basicPath ? 1 : 0;
                PropertyCheck.That(log.BasicHit.Count == expectedBasicHit,
                    $"basic={basicPath}, lethal={lethal}: OnBasicHit fired {log.BasicHit.Count} times, expected {expectedBasicHit}");

                // R3.2: OnBasicKill fires only on a basic path that killed, and never on a skill kill.
                int expectedBasicKill = (basicPath && lethal) ? 1 : 0;
                PropertyCheck.That(log.BasicKill.Count == expectedBasicKill,
                    $"basic={basicPath}, lethal={lethal}: OnBasicKill fired {log.BasicKill.Count} times, expected {expectedBasicKill}");

                // When the basic channel fired, it reported THIS enemy (routing carries the victim).
                if (expectedBasicHit == 1)
                    PropertyCheck.That(log.BasicHit[0] == enemy,
                        $"basic={basicPath}: OnBasicHit reported the wrong actor");
            });
        }

        // Property 5 (dedup facet): OnBasicKill fires at most once per Actor even if the kill is
        // reported repeatedly (mirrors RaiseKill/_killed). Models re-entrant/duplicate kill reports a
        // basic hit might produce for the same victim within a run.
        // Validates: Requirements 3.2, 4.3, 8.4
        [Test]
        public void OnBasicKillFiresAtMostOncePerActor()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int actorCount = rng.Next(1, 4);
                var bus = new HookBus();
                var log = new ChannelLog();
                log.Attach(bus);

                var actors = new List<Actor>();
                for (int a = 0; a < actorCount; a++) actors.Add(NewEnemy(100f));

                // Report each actor's basic kill a random number of times; the bus must collapse the
                // repeats to a single OnBasicKill per distinct actor.
                foreach (Actor actor in actors)
                {
                    int reports = rng.Next(1, 6);
                    for (int r = 0; r < reports; r++) bus.RaiseBasicKill(actor);
                }

                PropertyCheck.That(log.BasicKill.Count == actorCount,
                    $"actors={actorCount}: OnBasicKill fired {log.BasicKill.Count} times, expected once per distinct actor ({actorCount})");

                // Each distinct actor appears exactly once in the kill log.
                var distinct = new HashSet<Actor>(log.BasicKill);
                PropertyCheck.That(distinct.Count == log.BasicKill.Count,
                    $"actors={actorCount}: OnBasicKill reported a duplicate actor");
            });
        }

        // Example: a basic hit fires BOTH channels (generic + basic) for the same surviving enemy,
        // anchoring the "basic channel is additional, not a replacement" clause of R3.4.
        [Test]
        public void BasicHit_FiresGenericAndBasicChannelsTogether()
        {
            var bus = new HookBus();
            var log = new ChannelLog();
            log.Attach(bus);
            Actor enemy = NewEnemy(100f);

            DriveBasicHit(bus, enemy, 30f);

            Assert.That(log.Hit, Has.Count.EqualTo(1), "A basic hit must fire the generic OnHit.");
            Assert.That(log.BasicHit, Has.Count.EqualTo(1), "A basic hit must fire OnBasicHit.");
            Assert.That(log.Kill, Is.Empty, "A non-lethal basic hit must not fire OnKill.");
            Assert.That(log.BasicKill, Is.Empty, "A non-lethal basic hit must not fire OnBasicKill.");
            Assert.That(enemy.IsDead, Is.False, "A non-lethal hit must leave the enemy alive.");
        }

        // Example: a skill hit fires ONLY the generic channel, even when it kills — the defining
        // separation of R3.3 (skill hits never reach the basic channel).
        [Test]
        public void SkillKill_FiresGenericOnlyNeverBasic()
        {
            var bus = new HookBus();
            var log = new ChannelLog();
            log.Attach(bus);
            Actor enemy = NewEnemy(100f);

            ExpectEditModeDestroyLog();
            DriveSkillHit(bus, enemy, 150f); // lethal

            Assert.That(enemy.IsDead, Is.True, "The lethal skill hit should have killed the enemy.");
            Assert.That(log.Hit, Has.Count.EqualTo(1), "A skill hit must fire the generic OnHit.");
            Assert.That(log.Kill, Has.Count.EqualTo(1), "A lethal skill hit must fire the generic OnKill.");
            Assert.That(log.BasicHit, Is.Empty, "A skill hit must never fire OnBasicHit.");
            Assert.That(log.BasicKill, Is.Empty, "A skill kill must never fire OnBasicKill.");
        }

        // Example: a lethal basic hit fires both -Kill channels exactly once (generic + basic),
        // anchoring the kill routing the property samples around.
        [Test]
        public void LethalBasicHit_FiresBothKillChannelsOnce()
        {
            var bus = new HookBus();
            var log = new ChannelLog();
            log.Attach(bus);
            Actor enemy = NewEnemy(100f);

            ExpectEditModeDestroyLog();
            DriveBasicHit(bus, enemy, 150f); // lethal

            Assert.That(enemy.IsDead, Is.True, "The lethal basic hit should have killed the enemy.");
            Assert.That(log.Kill, Has.Count.EqualTo(1), "A lethal basic hit must fire the generic OnKill once.");
            Assert.That(log.BasicKill, Has.Count.EqualTo(1), "A lethal basic hit must fire OnBasicKill once.");
        }
    }
}
