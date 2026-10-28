using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Raw slot authoring; construction does not clamp invalid input. The graph validates it before use.</summary>
[Serializable]
public sealed class BoonAbilityConfiguration
{
    [SerializeField] private BoonSlot _slot;
    [SerializeField] private BoonCapability _capabilities;
    [SerializeField] private float _manaCost;
    [SerializeField] private float _cooldown;
    public BoonSlot Slot => _slot;
    public BoonCapability Capabilities => _capabilities;
    public float ManaCost => _manaCost;
    public float Cooldown => _cooldown;
    public BoonAbilityConfiguration(BoonSlot slot, BoonCapability capabilities, float manaCost, float cooldown)
    { _slot = slot; _capabilities = capabilities; _manaCost = manaCost; _cooldown = cooldown; }
    internal BoonAbilityConfiguration Copy() => (BoonAbilityConfiguration)MemberwiseClone();
}

/// <summary>A stable ID/rank/target record, suitable for serialization and independent of acquisition order.</summary>
[Serializable]
public sealed class BoonAcquisition
{
    [SerializeField] private string _id;
    [SerializeField] private int _rank;
    [SerializeField] private BoonSlotMask _slots;
    public string Id => _id;
    public int Rank => _rank;
    public BoonSlotMask Slots => _slots;
    public BoonAcquisition(string id, int rank, BoonSlotMask slots) { _id = id; _rank = rank; _slots = slots; }
    public BoonAcquisition(string id, int rank, BoonSlot slot) : this(id, rank, BoonCapabilityGraph.Mask(slot)) { }
    internal BoonAcquisition Copy() => (BoonAcquisition)MemberwiseClone();
}

/// <summary>
/// Session-owned declarative build rules. It never executes a skill or grants a gameplay effect.
/// Capability requirements are resolved per target skill, after its replacement, before dependent supports.
/// Retained supports describe existing runtime behavior and do not consume the new replacement/Augment caps.
/// </summary>
public sealed class BoonCapabilityGraph
{
    public const int MaxAugmentsPerAbility = 2;
    public const int MaxResonancesPerRun = 2;
    private readonly BoonCatalogSnapshot _catalog;
    private readonly BoonAbilityConfiguration[] _baseAbilities = new BoonAbilityConfiguration[4];
    private Dictionary<string, BoonAcquisition> _acquisitions = new Dictionary<string, BoonAcquisition>(StringComparer.Ordinal);
    private BoonAbilityConfiguration[] _abilities;
    public BoonWeaponMask Weapon { get; }
    public IReadOnlyList<BoonAcquisition> Acquisitions => _acquisitions.Values.OrderBy(a => a.Id, StringComparer.Ordinal)
        .Select(a => a.Copy()).ToList().AsReadOnly();

    public BoonCapabilityGraph(BoonCatalogSnapshot catalog, BoonWeaponMask weapon, IReadOnlyList<BoonAbilityConfiguration> abilities)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        if (weapon != BoonWeaponMask.Gauntlet && weapon != BoonWeaponMask.Bow && weapon != BoonWeaponMask.Spear)
            throw new ArgumentException("A build must identify exactly one equipped weapon family.", nameof(weapon));
        if (abilities == null || abilities.Count != 4) throw new ArgumentException("A build requires Q/W/E/R configurations.", nameof(abilities));
        Weapon = weapon;
        foreach (var ability in abilities)
        {
            if (ability == null || !Enum.IsDefined(typeof(BoonSlot), ability.Slot) ||
                !BoonCatalogValidator.ValidCapabilities(ability.Capabilities) ||
                !BoonCatalogValidator.FiniteNonnegative(ability.ManaCost) || !BoonCatalogValidator.FiniteNonnegative(ability.Cooldown))
                throw new ArgumentException("Slot authoring must have known capabilities and finite nonnegative costs.", nameof(abilities));
            if (_baseAbilities[(int)ability.Slot] != null) throw new ArgumentException("Duplicate configured slot.", nameof(abilities));
            _baseAbilities[(int)ability.Slot] = ability.Copy();
        }
        _abilities = _baseAbilities.Select(a => a.Copy()).ToArray();
    }

    public int Rank(string id) => id != null && _acquisitions.TryGetValue(id, out var acquisition) ? acquisition.Rank : 0;
    public BoonCapability Capabilities(BoonSlot slot) => Ability(slot).Capabilities;
    public BoonAbilityConfiguration Ability(BoonSlot slot)
    {
        if (!Enum.IsDefined(typeof(BoonSlot), slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        return _abilities[(int)slot].Copy();
    }
    public static BoonSlotMask Mask(BoonSlot slot)
    {
        if (!Enum.IsDefined(typeof(BoonSlot), slot)) throw new ArgumentOutOfRangeException(nameof(slot));
        return (BoonSlotMask)(1 << (int)slot);
    }

    public bool CanAcquire(string id, BoonSlot slot, out string diagnostic) => CanAcquire(id, Mask(slot), out diagnostic);
    public bool CanAcquire(string id, BoonSlotMask slots, out string diagnostic)
        => TryPrepareAcquisition(id, slots, out _, out _, out diagnostic);
    public bool TryAcquire(string id, BoonSlot slot, out string diagnostic) => TryAcquire(id, Mask(slot), out diagnostic);
    public bool TryAcquire(string id, BoonSlotMask slots, out string diagnostic)
    {
        if (!TryPrepareAcquisition(id, slots, out var acquisitions, out var abilities, out diagnostic)) return false;
        _acquisitions = acquisitions; _abilities = abilities; return true;
    }

    /// <summary>Atomic restore validates the final build, including inactive identities, without replay/order effects.</summary>
    public bool TryRestore(IEnumerable<BoonAcquisition> records, out string diagnostic)
    {
        diagnostic = null;
        if (records == null) { diagnostic = "Missing acquisition records."; return false; }
        var acquisitions = new Dictionary<string, BoonAcquisition>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            if (record == null || string.IsNullOrEmpty(record.Id) || acquisitions.ContainsKey(record.Id))
            { diagnostic = "Null or duplicate acquisition ID in restored build."; return false; }
            acquisitions.Add(record.Id, record.Copy());
        }
        if (!TryResolve(acquisitions, out var abilities, out diagnostic)) return false;
        _acquisitions = acquisitions; _abilities = abilities; return true;
    }

    /// <summary>Removal is rejected if any remaining boon would lose its IDs or skill-local capabilities.</summary>
    public bool CanRemove(string id, out string diagnostic) => TryPrepareRemoval(id, out _, out _, out diagnostic);
    public bool TryRemove(string id, out string diagnostic)
    {
        if (!TryPrepareRemoval(id, out var acquisitions, out var abilities, out diagnostic)) return false;
        _acquisitions = acquisitions; _abilities = abilities; return true;
    }

    private bool TryPrepareAcquisition(string id, BoonSlotMask slots, out Dictionary<string, BoonAcquisition> acquisitions,
        out BoonAbilityConfiguration[] abilities, out string diagnostic)
    {
        acquisitions = null; abilities = null; diagnostic = null;
        if (!_catalog.TryGet(id, out var definition)) { diagnostic = "Unknown boon ID: " + id; return false; }
        if (!definition.EnabledForOffers) { diagnostic = id + ": definition is not enabled for offers."; return false; }
        if (definition.IsRetainedSupport)
            foreach (BoonSlot slot in Enum.GetValues(typeof(BoonSlot)))
                if ((slots & Mask(slot)) != 0 && !MeetsCapabilities(_abilities[(int)slot].Capabilities, definition))
                { diagnostic = id + ": requested skill has no compatible producer."; return false; }
        int rank = Rank(id);
        if (rank >= definition.MaxRank) { diagnostic = id + ": rank limit reached."; return false; }
        if (_acquisitions.TryGetValue(id, out var current) && current.Slots != slots)
        { diagnostic = id + ": a ranked boon cannot change its bound skills."; return false; }
        acquisitions = new Dictionary<string, BoonAcquisition>(_acquisitions, StringComparer.Ordinal);
        acquisitions[id] = new BoonAcquisition(id, rank + 1, slots);
        return TryResolve(acquisitions, out abilities, out diagnostic);
    }

    private bool TryPrepareRemoval(string id, out Dictionary<string, BoonAcquisition> acquisitions,
        out BoonAbilityConfiguration[] abilities, out string diagnostic)
    {
        acquisitions = null; abilities = null; diagnostic = null;
        if (id == null || !_acquisitions.ContainsKey(id)) { diagnostic = "Boon is not acquired: " + id; return false; }
        acquisitions = new Dictionary<string, BoonAcquisition>(_acquisitions, StringComparer.Ordinal);
        acquisitions.Remove(id);
        return TryResolve(acquisitions, out abilities, out diagnostic);
    }

    private static bool MeetsCapabilities(BoonCapability capabilities, BoonDefinitionSnapshot definition)
        => (capabilities & definition.RequiredCapabilities) == definition.RequiredCapabilities &&
           (definition.AnyRequiredCapabilities == BoonCapability.None || (capabilities & definition.AnyRequiredCapabilities) != 0);

    // Retained techniques execute globally by producer type. Their inferred masks follow real surviving
    // producers; player-selected new Augments keep their explicit, immutable target.
    private void RebindRetainedProducers(Dictionary<string, BoonAcquisition> acquisitions)
    {
        var shape = _baseAbilities.Select(a => a.Capabilities).ToArray();
        foreach (var acquisition in acquisitions.Values)
        {
            if (!_catalog.TryGet(acquisition.Id, out var definition) || definition.IsRetainedSupport ||
                definition.Family != BoonFamily.Mutation || !BoonCatalogValidator.SingleSlot(acquisition.Slots)) continue;
            foreach (BoonSlot slot in Enum.GetValues(typeof(BoonSlot)))
                if ((acquisition.Slots & Mask(slot)) != 0)
                    shape[(int)slot] = (shape[(int)slot] & ~definition.RemovedCapabilities) | definition.ProvidedCapabilities;
        }
        foreach (var acquisition in acquisitions.Values.ToArray())
        {
            if (!_catalog.TryGet(acquisition.Id, out var definition) || !definition.IsRetainedSupport ||
                acquisition.Slots == BoonSlotMask.None || (acquisition.Slots & ~definition.Slots) != 0) continue;
            bool knownProducer = true;
            foreach (BoonSlot slot in Enum.GetValues(typeof(BoonSlot)))
                if ((acquisition.Slots & Mask(slot)) != 0 && !MeetsCapabilities(_baseAbilities[(int)slot].Capabilities, definition) &&
                    !MeetsCapabilities(shape[(int)slot], definition)) knownProducer = false;
            if (!knownProducer) continue;
            var valid = BoonSlotMask.None;
            foreach (BoonSlot slot in Enum.GetValues(typeof(BoonSlot)))
                if ((definition.Slots & Mask(slot)) != 0 && MeetsCapabilities(shape[(int)slot], definition)) valid |= Mask(slot);
            if (valid != BoonSlotMask.None)
                acquisitions[acquisition.Id] = new BoonAcquisition(acquisition.Id, acquisition.Rank, valid);
        }
    }

    private bool TryResolve(Dictionary<string, BoonAcquisition> acquisitions, out BoonAbilityConfiguration[] abilities, out string diagnostic)
    {
        abilities = null; diagnostic = null;
        RebindRetainedProducers(acquisitions);
        var replacements = new BoonDefinitionSnapshot[4];
        var augmentCounts = new int[4]; int resonances = 0;
        var pending = new List<KeyValuePair<BoonAcquisition, BoonDefinitionSnapshot>>();
        foreach (var acquisition in acquisitions.Values.OrderBy(a => a.Id, StringComparer.Ordinal))
        {
            if (!_catalog.TryGet(acquisition.Id, out var definition))
            { diagnostic = "Unknown restored boon ID: " + acquisition.Id; return false; }
            if (definition.Family == BoonFamily.Mythic)
            { diagnostic = definition.Id + ": Mythics are disabled in this implementation stage."; return false; }
            if ((definition.Weapons & Weapon) == 0 || acquisition.Slots == BoonSlotMask.None ||
                (acquisition.Slots & ~definition.Slots) != 0 || (acquisition.Slots & ~BoonSlotMask.Any) != 0)
            { diagnostic = definition.Id + ": incompatible equipped weapon or target skill."; return false; }
            if (acquisition.Rank < 1 || acquisition.Rank > definition.MaxRank)
            { diagnostic = definition.Id + ": restored rank is outside its declared limit."; return false; }
            if (!definition.IsRetainedSupport && (definition.Family == BoonFamily.Mutation || definition.Family == BoonFamily.Augment) &&
                !BoonCatalogValidator.SingleSlot(acquisition.Slots))
            { diagnostic = definition.Id + ": replacement/augment must bind exactly one skill."; return false; }
            foreach (var requiredId in definition.RequiredBoonIds)
                if (!acquisitions.ContainsKey(requiredId)) { diagnostic = definition.Id + ": missing prerequisite " + requiredId; return false; }
            foreach (var blockedId in definition.BlockedBoonIds)
                if (acquisitions.ContainsKey(blockedId)) { diagnostic = definition.Id + ": blocked by " + blockedId; return false; }
            if (definition.Family == BoonFamily.Resonance && ++resonances > MaxResonancesPerRun)
            { diagnostic = "Resonance limit reached for this run."; return false; }
            foreach (BoonSlot slot in Enum.GetValues(typeof(BoonSlot)))
            {
                if ((acquisition.Slots & Mask(slot)) == 0) continue;
                if (!definition.IsRetainedSupport && definition.Family == BoonFamily.Mutation)
                {
                    if (replacements[(int)slot] != null) { diagnostic = slot + ": a replacement Mutation is already active."; return false; }
                    replacements[(int)slot] = definition;
                }
                if (!definition.IsRetainedSupport && definition.Family == BoonFamily.Augment && ++augmentCounts[(int)slot] > MaxAugmentsPerAbility)
                { diagnostic = slot + ": Augment limit reached for this skill."; return false; }
            }
            if (definition.IsRetainedSupport || definition.Family != BoonFamily.Mutation)
                pending.Add(new KeyValuePair<BoonAcquisition, BoonDefinitionSnapshot>(acquisition, definition));
        }
        var capabilities = _baseAbilities.Select(a => a.Capabilities).ToArray();
        var mana = _baseAbilities.Select(a => a.ManaCost).ToArray();
        var cooldowns = _baseAbilities.Select(a => a.Cooldown).ToArray();
        for (int i = 0; i < replacements.Length; i++)
        {
            var replacement = replacements[i]; if (replacement == null) continue;
            // Replacement requirements refer to the base skill it replaces, never to its own grants.
            if (!MeetsCapabilities(capabilities[i], replacement))
            { diagnostic = replacement.Id + ": base skill lacks required capabilities at " + (BoonSlot)i; return false; }
            capabilities[i] = (capabilities[i] & ~replacement.RemovedCapabilities) | replacement.ProvidedCapabilities;
            if (replacement.Cost.OverrideManaCost) mana[i] = replacement.Cost.ManaCost;
            if (replacement.Cost.OverrideCooldown) cooldowns[i] = replacement.Cost.Cooldown;
        }
        // Resolve providers before dependants. A boon cannot satisfy its own requirements or borrow a different slot.
        while (pending.Count > 0)
        {
            bool progressed = false;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var pair = pending[i]; bool eligible = true;
                foreach (BoonSlot slot in Enum.GetValues(typeof(BoonSlot)))
                    if ((pair.Key.Slots & Mask(slot)) != 0 &&
                        !MeetsCapabilities(capabilities[(int)slot], pair.Value))
                        eligible = false;
                if (!eligible) continue;
                foreach (BoonSlot slot in Enum.GetValues(typeof(BoonSlot)))
                    if ((pair.Key.Slots & Mask(slot)) != 0) capabilities[(int)slot] |= pair.Value.ProvidedCapabilities;
                pending.RemoveAt(i); progressed = true;
            }
            if (!progressed)
            { diagnostic = pending[0].Value.Id + ": missing capabilities on its bound skill; cross-slot and self-provided requirements are invalid."; return false; }
        }
        abilities = new BoonAbilityConfiguration[4];
        for (int i = 0; i < abilities.Length; i++)
            abilities[i] = new BoonAbilityConfiguration((BoonSlot)i, capabilities[i], mana[i], cooldowns[i]);
        return true;
    }
}
