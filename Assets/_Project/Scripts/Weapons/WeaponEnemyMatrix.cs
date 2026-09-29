using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data-only mapping from (<see cref="ArchetypeId"/> × <see cref="RunWeaponFamily"/>) to the
/// <see cref="WeaponResponse"/> the weapon should reach for against that archetype — the tool
/// (ability slot / <c>AreaHitStep</c> / reaction intent), never a code path (R11.6). A pair with no
/// authored entry resolves the weapon's default response for the archetype's <see cref="CombatRole"/>
/// and logs missing coverage with a stable identifier (R11.7). The numbers (damage/timing/range) stay
/// on the ability and <c>AreaHitStep</c> assets; this matrix only names the intended answer.
/// <para>
/// All fallback and "every pair resolves to one response" logic lives in the pure
/// <see cref="WeaponEnemyResponseResolver"/> so it is testable without a live Unity scene; this SO is
/// just the authored data plus the once-per-pair missing-coverage log.
/// </para>
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 16.1.
/// Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7.
/// </remarks>
[CreateAssetMenu(menuName = "Tech Guy/Weapons/Weapon Enemy Matrix")]
public sealed class WeaponEnemyMatrix : ScriptableObject, WeaponEnemyResponseResolver.IEntryLookup
{
    /// <summary>
    /// One authored cell of the matrix: for a given archetype and weapon family, which slot (Q/W/E/R)
    /// is the intended tool and what kind of answer it represents. Authoring is entirely data; the
    /// slot is clamped to the four-ability kit when read (see <see cref="WeaponEnemyResponseResolver"/>).
    /// </summary>
    [Serializable]
    public struct Entry
    {
        [Tooltip("Which enemy archetype this response is for.")]
        public ArchetypeId archetype;

        [Tooltip("Which weapon family this response is for (Gauntlet / Bow / Spear).")]
        public RunWeaponFamily family;

        [Tooltip("Ability slot that is the intended tool: 0=Q, 1=W, 2=E, 3=R.")]
        [Range(0, 3)] public int slot;

        [Tooltip("The kind of answer this tool represents (anti-Heavy, counter-Charger, priority, swarm clear, general).")]
        public WeaponResponseKind kind;
    }

    [Tooltip("Authored (archetype × weapon family) responses. Pairs not listed fall back to the weapon's CombatRole default and log missing coverage.")]
    [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

    // Track which (archetype, family) fallbacks have already been logged so the console isn't spammed
    // when the same missing pair is resolved every frame (mirrors PreferredDistanceLayer's approach).
    [NonSerialized] private HashSet<WeaponEnemyResponseResolver.Key> _loggedMissing;

    /// <summary>Read-only view of the authored entries, for inspection and tests.</summary>
    public IReadOnlyList<Entry> Entries => _entries;

    /// <summary>
    /// Pure authored lookup used by <see cref="WeaponEnemyResponseResolver"/>. Returns false when no
    /// entry exists for the pair so the resolver applies the role fallback (R11.7). The returned slot
    /// is normalized to Q/W/E/R by the <see cref="WeaponResponse"/> constructor.
    /// </summary>
    public bool TryGet(ArchetypeId archetype, RunWeaponFamily family, out WeaponResponse response)
    {
        if (_entries != null)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].archetype == archetype && _entries[i].family == family)
                {
                    response = new WeaponResponse(_entries[i].slot, _entries[i].kind, fromMatrix: true);
                    return true;
                }
            }
        }

        response = default;
        return false;
    }

    /// <summary>
    /// Resolves the response for a (archetype, family) pair, applying the <see cref="CombatRole"/>
    /// fallback and logging missing coverage exactly once per pair (R11.7). No per weapon-enemy
    /// branching happens here — the decision is delegated to the pure resolver (R11.6).
    /// </summary>
    /// <param name="archetype">The enemy's archetype id.</param>
    /// <param name="family">The equipped weapon family.</param>
    /// <param name="role">The archetype's primary combat role, used for the fallback.</param>
    public WeaponResponse Resolve(ArchetypeId archetype, RunWeaponFamily family, CombatRole role)
    {
        WeaponResponse response = WeaponEnemyResponseResolver.Resolve(this, archetype, family, role);

        if (!response.FromMatrix)
        {
            LogMissingCoverage(archetype, family, role, response);
        }

        return response;
    }

    private void LogMissingCoverage(ArchetypeId archetype, RunWeaponFamily family, CombatRole role, WeaponResponse response)
    {
        _loggedMissing ??= new HashSet<WeaponEnemyResponseResolver.Key>();
        var key = new WeaponEnemyResponseResolver.Key(archetype, family);
        if (!_loggedMissing.Add(key))
        {
            return;
        }

        Debug.LogWarning(
            $"WeaponEnemyMatrix '{name}': no response declared for archetype '{archetype}' with " +
            $"weapon family '{family}'; applying the {role} CombatRole default " +
            $"({response.Kind} @slot {response.Slot}).",
            this);
    }
}
