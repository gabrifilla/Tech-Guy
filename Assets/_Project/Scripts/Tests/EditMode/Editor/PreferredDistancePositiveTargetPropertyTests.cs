using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure preferred-distance resolution and positioning math in
    /// <see cref="PreferredDistanceResolver"/> — task 7.2 of weapon-gameplay-swarm-rework.
    ///
    /// The layer that keeps a non-attacking enemy at its preferred distance is otherwise scene/agent
    /// driven (<c>PreferredDistanceLayer</c> over <c>EnemyAI</c>); the "a resolved preferred distance
    /// is always &gt; 0" and "the positioning target tends toward that distance" invariants (Property
    /// 17, Requisito 6.1) were extracted into <see cref="PreferredDistanceResolver"/> so they can be
    /// property-checked without a live Unity scene. This project cannot resolve FsCheck/CsCheck
    /// packages on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives
    /// &gt;= 100 deterministic generated cases per property (min 128) and reports the exact failing
    /// case as a counterexample.
    /// </summary>
    public sealed class PreferredDistancePositiveTargetPropertyTests
    {
        private static readonly CombatRole[] Roles = (CombatRole[])System.Enum.GetValues(typeof(CombatRole));
        private static readonly ArchetypeId[] Archetypes = (ArchetypeId[])System.Enum.GetValues(typeof(ArchetypeId));

        // Feature: weapon-gameplay-swarm-rework, Property 17: Distância preferida positiva e alvo de posicionamento
        // Para todo arquétipo com distância preferida declarada, o valor resolvido é maior que 0 e o
        // alvo de posicionamento tende a essa distância: PositioningTarget coloca o inimigo exatamente
        // à distância preferida (planar) do jogador. Cobre o caminho declarado (profile) e o fallback
        // de CombatRole (sempre > 0), com posições de jogador/inimigo arbitrárias — inclusive quando o
        // inimigo está em cima do jogador (raio de comprimento zero).
        // Validates: Requirements 6.1
        [Test]
        public void ResolvedPreferredDistanceIsPositiveAndPositioningTargetHoldsThatDistance()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                CombatRole role = Roles[rng.Next(Roles.Length)];
                float engagementBand = RandomBand(rng);

                // Resolve either through a declared profile entry or the CombatRole fallback so the
                // property covers "para todo arquétipo com distância preferida declarada" as well as
                // the (>0) fallback path the same resolver guarantees.
                PreferredDistanceResolver.Resolution resolution = ResolveRandom(rng, role, engagementBand, out string source);

                // R6.1: a resolved preferred distance is always strictly > 0, never below the floor.
                PropertyCheck.That(resolution.PreferredDistance > 0f,
                    $"{source}: resolved PreferredDistance {resolution.PreferredDistance} is not > 0");
                PropertyCheck.That(
                    resolution.PreferredDistance >= PreferredDistanceResolver.MinPreferredDistance - 1e-6f,
                    $"{source}: resolved PreferredDistance {resolution.PreferredDistance} fell below the floor " +
                    $"{PreferredDistanceResolver.MinPreferredDistance}");

                // Separation radius is a data channel that must stay non-negative regardless of authoring.
                PropertyCheck.That(resolution.SeparationRadius >= 0f,
                    $"{source}: resolved SeparationRadius {resolution.SeparationRadius} is negative");

                // The direct RoleDefaultDistance fallback is also always > 0 for every role/band.
                float roleDefault = PreferredDistanceResolver.RoleDefaultDistance(role, engagementBand);
                PropertyCheck.That(roleDefault > 0f,
                    $"RoleDefaultDistance({role}, band={engagementBand}) = {roleDefault} is not > 0");

                float preferred = resolution.PreferredDistance;

                // Positioning target: the enemy should tend toward exactly `preferred` from the player.
                Vector3 player = RandomPoint(rng);
                Vector3 enemy = RandomEnemyPoint(rng, player);
                Vector3 target = PreferredDistanceResolver.PositioningTarget(player, enemy, preferred);

                // R6.2/Property 17: the planar (ground-plane) distance from the player to the target is
                // exactly the preferred distance, whether the enemy was too close or too far.
                Vector3 planar = target - player;
                planar.y = 0f;
                float planarDistance = planar.magnitude;
                PropertyCheck.That(Mathf.Abs(planarDistance - preferred) <= 1e-3f,
                    $"{source}: player={player} enemy={enemy} preferred={preferred}: planar target distance " +
                    $"{planarDistance} != preferred {preferred}");

                // The target stays on the enemy's own plane (Y preserved from the enemy).
                PropertyCheck.That(Mathf.Approximately(target.y, enemy.y),
                    $"{source}: target Y {target.y} did not preserve enemy Y {enemy.y}");

                // "Tends toward" the preferred distance: when the enemy shares a planar direction with
                // the player, the target lies along that same outward ray (never on the opposite side).
                Vector3 away = enemy - player;
                away.y = 0f;
                if (away.magnitude > 1e-3f)
                {
                    Vector3 dir = away.normalized;
                    float projection = Vector3.Dot(planar, dir);
                    PropertyCheck.That(projection > 0f,
                        $"{source}: target does not lie on the player->enemy ray (projection {projection})");
                    // On that ray the exact-distance target implies the point is dir * preferred.
                    PropertyCheck.That(Mathf.Abs(projection - preferred) <= 1e-3f,
                        $"{source}: projection {projection} onto the away direction != preferred {preferred}");
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Resolves through a declared <see cref="PreferredDistanceProfile"/> entry (declared path) or
        /// the <see cref="CombatRole"/> fallback (null/absent profile), so the property spans both. A
        /// freshly created ScriptableObject is used only to seed a declared entry; it is destroyed
        /// before the case returns so no scene/asset state leaks between cases.
        /// </summary>
        private static PreferredDistanceResolver.Resolution ResolveRandom(
            System.Random rng, CombatRole role, float engagementBand, out string source)
        {
            int mode = rng.Next(0, 3);
            ArchetypeId archetype = Archetypes[rng.Next(Archetypes.Length)];

            if (mode == 0)
            {
                // Fallback path: no profile assigned.
                source = $"[fallback role={role} band={engagementBand} archetype={archetype}]";
                return PreferredDistanceResolver.Resolve(null, archetype, role, engagementBand);
            }

            // Declared path: build a profile with an entry for this archetype, including intentionally
            // non-positive authored values so the >0 clamp guarantee is exercised too.
            float authoredDistance = mode == 1
                ? 0.1f + (float)rng.NextDouble() * 30f       // healthy positive value
                : ((float)rng.NextDouble() - 0.6f) * 5f;     // may be zero/negative -> must clamp to >0
            float authoredSeparation = ((float)rng.NextDouble() - 0.3f) * 3f; // may be negative -> clamps to >=0

            var profile = ScriptableObject.CreateInstance<PreferredDistanceProfile>();
            try
            {
                var entry = new PreferredDistanceProfile.Entry
                {
                    archetype = archetype,
                    preferredDistance = authoredDistance,
                    separationRadius = authoredSeparation
                };
                var field = typeof(PreferredDistanceProfile).GetField(
                    "_entries",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field.SetValue(profile, new[] { entry });

                source = $"[declared role={role} archetype={archetype} authored={authoredDistance}]";
                return PreferredDistanceResolver.Resolve(profile, archetype, role, engagementBand);
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        /// <summary>Engagement bands spanning zero (fixed role constants), small, and large values.</summary>
        private static float RandomBand(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;                                   // use fixed role constants
                case 1: return (float)rng.NextDouble() * 0.05f;      // tiny -> clamped to the floor
                case 2: return (float)rng.NextDouble() * 5f;         // small band
                default: return 5f + (float)rng.NextDouble() * 25f;  // large band
            }
        }

        private static Vector3 RandomPoint(System.Random rng)
        {
            return new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 60f,
                ((float)rng.NextDouble() - 0.5f) * 20f,
                ((float)rng.NextDouble() - 0.5f) * 60f);
        }

        /// <summary>
        /// An enemy position relative to the player: sometimes exactly on top of the player (zero-length
        /// planar ray, exercising the arbitrary stable direction), sometimes close, sometimes far.
        /// </summary>
        private static Vector3 RandomEnemyPoint(System.Random rng, Vector3 player)
        {
            switch (rng.Next(0, 3))
            {
                case 0:
                    // Directly on top of the player in the plane (only Y may differ).
                    return new Vector3(player.x, player.y + ((float)rng.NextDouble() - 0.5f) * 4f, player.z);
                default:
                    return player + RandomPoint(rng);
            }
        }
    }
}
