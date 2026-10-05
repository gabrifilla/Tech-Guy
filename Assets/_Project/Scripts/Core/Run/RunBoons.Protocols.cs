using System;
using UnityEngine;

public sealed partial class RunBoons
{
    [SerializeField] private ProtocolCatalogDefinition _protocolCatalog;
    private bool _applyingOffer;
    public ProtocolInventory Protocols { get; private set; }
    public RunSessionState ProtocolSession => _manualRoute?.Session;
    private ProtocolRuntime _protocolRuntime;
    public bool HasConfirmedProtocolBatch => _protocolRuntime && _protocolRuntime.HasConfirmedBatch;
    internal void CloseProtocolEncounter() { if (_protocolRuntime) _protocolRuntime.CloseEncounter(); }
    private void BindProtocolRuntime()
    {
        _protocolRuntime = GetComponent<ProtocolRuntime>() ?? gameObject.AddComponent<ProtocolRuntime>();
        _protocolRuntime.Configure(_player, Protocols, this);
    }

    /// <summary>Explicit scene/test injection. A fresh legacy session may be rebound before its first acquisition.</summary>
    public bool ConfigureProtocolCatalog(ProtocolCatalogDefinition catalog)
    {
        if (!catalog || _applyingOffer || Protocols != null && Protocols.Revision != 0) return false;
        if (!catalog.TryCreateSnapshot(out var snapshot, out string error))
        { Debug.LogError("Protocol catalog refused: " + error, this); return false; }
        _protocolCatalog = catalog;
        Protocols = new ProtocolInventory(Protocols?.SessionId ?? Guid.NewGuid(), snapshot);
        if (_protocolRuntime && _player) BindProtocolRuntime();
        return true;
    }

    private bool InitializeProtocolInventory()
    {
        if (Protocols != null) return true;
        if (_protocolCatalog) return ConfigureProtocolCatalog(_protocolCatalog);
        // Existing scenes and isolated consumers had no catalog field. Keep a local validated default,
        // never a singleton or Resources lookup. Published manual authoring injects its explicit asset.
        var defaults = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
        try
        {
            if (!defaults.TryCreateSnapshot(out var snapshot, out string error))
            { Debug.LogError(error, this); return false; }
            Protocols = new ProtocolInventory(Guid.NewGuid(), snapshot); return true;
        }
        finally { Destroy(defaults); }
    }

    private bool ApplyOffer(Offer offer, bool legacy)
    {
        if (_applyingOffer || offer == null) return false;
        _applyingOffer = true;
        try
        {
            if (!ProtocolInventory.TryKind(offer.Id, out var kind)) return ApplyOfferCore(offer, legacy);
            if (!InitializeProtocolInventory() || !_player || _player.IsDead || _manualSessionEnded) return false;
            if (!Protocols.TryAcquire(Protocols.SessionId, kind, Protocols.Revision, rank => ApplyProtocolEffect(kind))) return false;
            CompleteOffer(offer, legacy); return true;
        }
        finally { _applyingOffer = false; }
    }

    private bool ApplyProtocolEffect(ProtocolKind kind)
    {
        var definition = Protocols.Catalog.Get(kind);
        switch (kind)
        {
            case ProtocolKind.Ignite: _player.OnHitEffects.EnableBurn(definition.DamagePerRank, definition.Duration); break;
            case ProtocolKind.Frost: _player.OnHitEffects.EnableChill(definition.SlowFraction, definition.Duration, definition.ChancePerRank); break;
            case ProtocolKind.Conductor: _player.OnHitEffects.Synergies.Add(RunSynergy.Conductor); break;
            case ProtocolKind.Detonation: _player.OnHitEffects.Synergies.Add(RunSynergy.Detonation); break;
            case ProtocolKind.Reactor: _player.OnHitEffects.Synergies.Add(RunSynergy.Reactor); break;
            case ProtocolKind.Resonance: _player.OnHitEffects.Synergies.Add(RunSynergy.Resonance); break;
            case ProtocolKind.Overflow: _player.OnHitEffects.EnableElementalOverflow(); break;
            default: return false;
        }
        return true;
    }

    private string ManualInventoryKey() => ManualRewardCatalog.InventoryKey(WeaponModifiers, _weapon,
        _manualRoute.Session.InventoryRevision) + ":protocols:" + Protocols.InventoryKey();
}
