using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class RunBoons
{
    private ManualRoutePolicy _manualRoute;
    private RunRewardPolicy _manualRewards;
    private RunRewardTicket _manualTicket;
    private bool _manualAgentWasStopped;
    private readonly RunSelectionReleaseGate _manualSelectionGate = new RunSelectionReleaseGate();
    private RunRewardTicket _reportedRecovery;
    private RunRewardRequest? _dismissedManualReward;
    public bool WantsManualRewardPresentation => _manualTicket != null &&
        _manualTicket.State == RunRewardTicketState.Pending && !IsChoosing &&
        (!_dismissedManualReward.HasValue || !_dismissedManualReward.Value.Equals(_manualTicket.Request));
    public RunRewardTicket ManualTicket => _manualTicket;
    private bool _manualMenuOpen;
    public void SetManualMenuOpen(bool open) => _manualMenuOpen = open;
    public bool BlocksManualOffense => _manualRoute != null && (_manualMenuOpen || _manualRoute.Session.IsClosed || _manualRoute.Session.IsEncounterClosing);
    public bool ManualRecoveryRequired => _manualTicket != null &&
        (_manualTicket.State == RunRewardTicketState.Faulted || _manualTicket.AnnouncedKind == RunRewardKind.Mutation &&
         _manualTicket.Choices.Count == 0 && _manualRoute.Session.MutationAcquisitions == 0);
    public event Action<RunRewardTicket> ManualRewardChosen;
    private bool _manualSessionEnded;
    internal void EndManualSession()
    {
        if (_manualSessionEnded) return;
        _manualSessionEnded = true;
        CloseManualReward();
        if (_player) _player.Stats.RemoveModifiersFrom(this);
        if (_holder) _holder.BindRun(null);
        RewardChosen -= EvaluateSystemBreak;
        ManualRewardChosen = null;
        ClearRunState();
        // The player still owns the equipped clone until scene unload. OnDestroy releases its assets.
    }

    public bool BindManualRun(ManualRoutePolicy route)
    {
        if (route == null || _manualRoute != null || IsChoosing || WeaponModifiers == null ||
            WeaponModifiers.Family != RunWeaponFamily.Gauntlet) return false;
        if (!InitializeProtocolInventory() || !Protocols.TryBindFreshSession(route.Session.Id)) return false;
        _manualRoute = route; _manualRewards = new RunRewardPolicy(route); return true;
    }

    public bool OfferReward(RunRewardRequest request)
    {
        if (_manualRewards == null || !_weapon || !_player || _player.CurrentWeapon != _weapon ||
            WeaponModifiers?.Family != RunWeaponFamily.Gauntlet || !ManualRewardContext().CanConfirm) return false;
        var node = _manualRoute.Session.Route.Node(request.NodeId);
        if (node == null || IsChoosing && (_manualTicket == null || !_manualTicket.Request.Equals(request))) return false;
        var ticket = PrepareManualReward(request);
        if (ticket == null) return false;
        _dismissedManualReward = null;
        RewardRoom = request.NodeId;
        _choices.Clear(); _choices.AddRange(ticket.Choices);
        if (!IsChoosing && _agent && _agent.enabled && _agent.isOnNavMesh)
        { _manualAgentWasStopped = _agent.isStopped; _agent.ResetPath(); _agent.isStopped = true; }
        IsChoosing = true;
        if (TryGetComponent(out CharControlScript controls)) controls.CancelCombo();
        return true;
    }

    internal RunRewardTicket PrepareManualReward(RunRewardRequest request)
    {
        if(_manualRewards==null||!_player||_player.IsDead||_player.CurrentWeapon!=_weapon) return null;
        var node=_manualRoute.Session.Route.Node(request.NodeId); if(node==null) return null;
        var ticket = _manualRewards.Open(request, new RunActionContext(true),
            ManualInventoryKey(),
            ManualRewardCatalog.Eligible(node.RewardAt(request.Ordinal), WeaponModifiers, _weapon, Protocols,
                _manualRoute.Session.ProtocolAcquisitions + 1));
        if (ticket == null) return null;
        _manualTicket = ticket;
        if(ManualRecoveryRequired && _reportedRecovery != ticket)
        {
            _reportedRecovery=ticket;
            Debug.LogError("Manual Mutation guarantee unavailable for session " + request.SessionId + ": " + ticket.Diagnostic, this);
        }
        return ticket;
    }

    public void CloseManualReward()
    {
        if (_manualRewards == null || !IsChoosing || _manualTicket?.State == RunRewardTicketState.Committing) return;
        if (_manualTicket != null) _dismissedManualReward = _manualTicket.Request;
        IsChoosing = false; _choices.Clear(); ReleaseManualRewardMovement();
        // The ticket and sealed route remain pending; opening it again preserves the frozen choices.
    }

    private bool ChooseManual(int index)
    {
        if (!IsChoosing || !_manualSelectionGate.CanConfirm || _manualTicket == null || index < 0 || index >= _choices.Count) return false;
        var ticket = _manualTicket;
        // Refresh/revise a changed inventory before using the old index. A stale click must never select a new card.
        string key = ManualInventoryKey();
        if (key != ticket.InventoryKey)
        { OfferReward(ticket.Request); return false; }
        var offer = _choices[index];
        bool chosen = _manualRewards.TryChoose(ticket.Request, ticket.Revision, offer.Id, ManualRewardContext(),
            () => _player.CurrentWeapon == _weapon && WeaponModifiers.Family == RunWeaponFamily.Gauntlet &&
                ManualRewardContext().CanConfirm && (ticket.IsAlternative && offer.Id == "bulwark" ||
                ManualRewardCatalog.Eligible(ticket.AnnouncedKind, WeaponModifiers, _weapon, Protocols,
                    _manualRoute.Session.ProtocolAcquisitions + 1).Any(o => o.Id == offer.Id)),
            () =>
            {
                if (!ApplyOffer(offer, false)) return false;
                if (!ManualRewardContext().CanConfirm) throw new InvalidOperationException("Player state changed while applying reward.");
                _manualSelectionGate.RequireRelease(Time.frameCount);
                return true;
            },
            resolved => { ReleaseManualRewardMovement(); ManualRewardChosen?.Invoke(resolved); });
        if (ticket.State == RunRewardTicketState.Faulted)
        {
            IsChoosing = false; _choices.Clear(); ReleaseManualRewardMovement();
            Debug.LogError("Manual reward commit failed; abort this session rather than retrying: " + ticket.Diagnostic, this);
        }
        return chosen;
    }

    private RunActionContext ManualRewardContext() => new RunActionContext(_player && !_player.IsDead,
        _holder && _holder.IsGamePaused, _player && _player.IsCombatHitStopped, _holder && _holder.IsCasting,
        TryGetComponent(out CharControlScript controls) && (controls.isDashing || controls.dashScript && controls.dashScript.IsDashing(gameObject)));

    private void Update()
    {
        if(_manualRewards==null||_manualSelectionGate.CanConfirm) return;
        var keyboard=Keyboard.current; var mouse=Mouse.current;
        bool held=(keyboard!=null&&(keyboard.digit1Key.isPressed||keyboard.digit2Key.isPressed||keyboard.digit3Key.isPressed||keyboard.enterKey.isPressed))||
            mouse!=null&&mouse.leftButton.isPressed;
        _manualSelectionGate.Observe(Time.frameCount,held);
    }

    private void ReleaseManualRewardMovement()
    {
        if (_agent && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = _manualAgentWasStopped;
        if (TryGetComponent(out CharControlScript controls)) controls.RequireAttackRelease();
    }

    /// <summary>Preflight before PlayerActor cancels/equips anything. Compatible swaps retain this run's inventory.</summary>
    public bool TryPrepareManualWeapon(WeaponScript requested,out WeaponScript prepared)
    {
        prepared=requested;
        if(_manualRoute==null||_manualRoute.Session.IsClosed||requested==_weapon) return true;
        if(!ManualRouteValidator.ValidateLoadout(requested,out string error))
        { Debug.LogWarning("Manual run rejected weapon change before equip: " + error,this); return false; }
        var copy=Instantiate(requested); _owned.Add(copy);
        copy.abilities=new Ability[requested.abilities.Length];
        for(int i=0;i<copy.abilities.Length;i++)
        { copy.abilities[i]=Instantiate(requested.abilities[i]); _owned.Add(copy.abilities[i]); }
        _weapon=copy; _baseGauntletQ=copy.abilities[0] as BreakerGauntletAbility; _baseGauntletQFocused=false;
        prepared=copy; return true;
    }
}
