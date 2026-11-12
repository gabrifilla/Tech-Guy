using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests
{
    /// <summary>Etapa 3 (11/11/2026): Volátil, Guarda, Shooter · Leque/Rajada, skins and the short lunge.</summary>
    public sealed class EnemyRedesignTests
    {
        private const string Archetypes = "Assets/_Project/Prefabs/EnemyArchetypes/";
        private const string Skins = "Assets/_Project/Prefabs/FirstSector/RunExp03/";
        private static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

        [Test]
        public void VolatileBombBlinksFasterAndFasterAndOnlyReachesItsRadius()
        {
            int Toggles(float from, float to)
            {
                int toggles = 0; bool last = VolatileBlastModel.IsLit(from);
                for (float t = from; t <= to; t += .002f) { bool lit = VolatileBlastModel.IsLit(t); if (lit != last) toggles++; last = lit; }
                return toggles;
            }
            float half = VolatileBlastModel.Fuse * .5f;
            Assert.Greater(Toggles(half, VolatileBlastModel.Fuse), Toggles(0f, half) * 1.5f, "Second half blinks clearly faster.");
            Assert.IsTrue(VolatileBlastModel.Hits(Vector3.zero, new Vector3(2.4f, .5f, 0)));
            Assert.IsFalse(VolatileBlastModel.Hits(Vector3.zero, new Vector3(0, 0, 2.6f)));
            Assert.AreEqual(2.5f, VolatileBlastModel.Radius); Assert.AreEqual(30f, VolatileBlastModel.Damage);
            Assert.AreEqual(.8f, VolatileBlastModel.Fuse, 1e-4f);
        }

        [Test]
        public void GuardaFrontBlocksAndOpensFromBehindOrWithPostureBroken()
        {
            Vector3 forward = Vector3.forward;
            Assert.IsTrue(FrontalArcReflection.GuardBlocks(true, forward, new Vector3(0, 0, 2), 120f));
            Assert.IsTrue(FrontalArcReflection.GuardBlocks(true, forward, new Vector3(1.5f, 0, 1.5f), 120f), "45° is inside 120°.");
            Assert.IsFalse(FrontalArcReflection.GuardBlocks(true, forward, new Vector3(2, 0, 0), 120f), "Flank opens it.");
            Assert.IsFalse(FrontalArcReflection.GuardBlocks(true, forward, new Vector3(0, 0, -2), 120f), "Back opens it.");
            Assert.IsFalse(FrontalArcReflection.GuardBlocks(false, forward, new Vector3(0, 0, 2), 120f), "Broken posture opens it.");
        }

        [Test]
        public void ModifiersChangeTheShooterAttackAndTheNamesShown()
        {
            Assert.AreEqual(EnemyAttackKind.SpreadShot, EnemyModifierRules.AttackFor(ArchetypeId.Shooter, EnemyModifier.Leque));
            Assert.AreEqual(EnemyAttackKind.BurstShot, EnemyModifierRules.AttackFor(ArchetypeId.Shooter, EnemyModifier.Rajada));
            Assert.IsNull(EnemyModifierRules.AttackFor(ArchetypeId.Shooter, EnemyModifier.None));
            Assert.IsNull(EnemyModifierRules.AttackFor(ArchetypeId.Grunt, EnemyModifier.Leque), "Only the Shooter reads modifiers.");
            Assert.AreEqual("Volátil", EnemyModifierRules.DisplayName(ArchetypeId.Fragile, EnemyModifier.None, "x"));
            Assert.AreEqual("Guarda", EnemyModifierRules.DisplayName(ArchetypeId.Mirror, EnemyModifier.None, "x"));
            Assert.AreEqual("Shooter · Leque", EnemyModifierRules.DisplayName(ArchetypeId.Shooter, EnemyModifier.Leque, "x"));
            Assert.AreEqual("Shooter · Rajada", EnemyModifierRules.DisplayName(ArchetypeId.Shooter, EnemyModifier.Rajada, "x"));
        }

        [Test]
        public void LungeOnlyAgainstAPlayerWalkingAwayJustOutOfReachAndOnCooldown()
        {
            Vector3 enemy = Vector3.zero, player = new Vector3(0, 0, 3.5f);
            Assert.IsTrue(LungeRules.ShouldLunge(enemy, player, new Vector3(0, 0, 5), 10f, 9f));
            Assert.IsFalse(LungeRules.ShouldLunge(enemy, player, new Vector3(0, 0, -5), 10f, 9f), "Approaching player: no lunge.");
            Assert.IsFalse(LungeRules.ShouldLunge(enemy, player, new Vector3(5, 0, 0), 10f, 9f), "Strafing is not walking away.");
            Assert.IsFalse(LungeRules.ShouldLunge(enemy, player, new Vector3(0, 0, 5), 10f, 11f), "Cooldown.");
            Assert.IsFalse(LungeRules.ShouldLunge(enemy, new Vector3(0, 0, 6), new Vector3(0, 0, 5), 10f, 9f), "Too far.");
            Assert.IsFalse(LungeRules.ShouldLunge(enemy, new Vector3(0, 0, 1.5f), new Vector3(0, 0, 5), 10f, 9f), "In reach: normal punch.");
            Assert.IsTrue(LungeRules.UsesLunge(ArchetypeId.Grunt) && LungeRules.UsesLunge(ArchetypeId.Rush));
            Assert.IsFalse(LungeRules.UsesLunge(ArchetypeId.Heavy));
        }

        [Test]
        public void VarietyPlanTurnsSpreadIntoLequeGuaranteesMiraAndAppliesSkinsAtTheirRates()
        {
            GameObject shooter = Load(Archetypes + "Shooter.prefab"), spread = Load(Archetypes + "SpreadShooter.prefab");
            GameObject sniper = Load(Archetypes + "Sniper.prefab"), rush = Load(Archetypes + "Rush.prefab");
            GameObject emissor = Load(Skins + "Emissor.prefab"), fragmento = Load(Skins + "Fragmento.prefab");
            Assert.IsNotNull(shooter); Assert.IsNotNull(spread); Assert.IsNotNull(emissor); Assert.IsNotNull(fragmento);
            Assert.AreEqual(ArchetypeId.Sniper, emissor.GetComponent<EnemyVariant>().ArchetypeId, "Emissor is a Sniper look.");
            Assert.AreEqual(ArchetypeId.Rush, fragmento.GetComponent<EnemyVariant>().ArchetypeId, "Fragmento is a Rush look.");

            var mira = new List<ManualEnemySpawn> { new ManualEnemySpawn(shooter, Vector3.zero), new ManualEnemySpawn(shooter, Vector3.right), new ManualEnemySpawn(shooter, Vector3.left) };
            var miraResult = EnemyVarietyPlan.Apply(mira, true, shooter, emissor, fragmento, new SeededRng(1));
            CollectionAssert.AreEquivalent(new[] { EnemyModifier.Leque, EnemyModifier.Rajada, EnemyModifier.None }, miraResult.Select(s => s.Modifier));
            CollectionAssert.AreEqual(mira.Select(s => s.Offset), miraResult.Select(s => s.Offset), "Positions are untouched.");

            var spreadResult = EnemyVarietyPlan.Apply(new[] { new ManualEnemySpawn(spread, Vector3.zero) }, false, shooter, emissor, fragmento, new SeededRng(2));
            Assert.AreSame(shooter, spreadResult[0].Prefab); Assert.AreEqual(EnemyModifier.Leque, spreadResult[0].Modifier);

            int leque = 0, emissorLooks = 0, fragmentoLooks = 0; const int runs = 3000;
            for (ulong seed = 0; seed < runs; seed++)
            {
                var roster = new[] { new ManualEnemySpawn(shooter, Vector3.zero), new ManualEnemySpawn(sniper, Vector3.zero), new ManualEnemySpawn(rush, Vector3.zero) };
                var result = EnemyVarietyPlan.Apply(roster, false, shooter, emissor, fragmento, new SeededRng(seed)) ?? roster.ToList();
                if (result[0].Modifier == EnemyModifier.Leque) leque++;
                if (result[1].Prefab == emissor) emissorLooks++;
                if (result[2].Prefab == fragmento) fragmentoLooks++;
            }
            Assert.That(leque / (float)runs, Is.InRange(.28f, .39f), "About 1 in 3 Shooters is Leque.");
            Assert.That(emissorLooks / (float)runs, Is.InRange(.44f, .56f));
            Assert.That(fragmentoLooks / (float)runs, Is.InRange(.44f, .56f));

            var elite = EnemyVarietyPlan.Apply(new[] { new ManualEnemySpawn(spread, Vector3.zero, EnemyRank.Elite) }, false, shooter, emissor, fragmento, new SeededRng(3));
            Assert.IsNull(elite, "Elites keep their authored prefab.");
        }
    }
}
