using System;
using UnityEngine;

/// <summary>
/// Thin runtime component that replaces the single <c>ARSENAL / ARMAS</c> panel of
/// <see cref="LobbyInteraction"/> with the Hades-style physical pedestals: it reads the nearest
/// pedestal by proximity, draws its weapon card anchored to the pedestal, and equips/unlocks the
/// weapon in place with the same interaction key already used in the lobby (<see cref="GameControl.Skill3"/>).
///
/// All decision logic lives in the pure, scene-free classes this MonoBehaviour consumes:
/// <see cref="PedestalProximity"/> (nearest-in-range selection), <see cref="WeaponPedestalState"/>
/// (locked/unlocked/equipped -> action) and <see cref="WeaponCardContent"/> (card text assembly),
/// drawn by <see cref="WeaponCardView"/>. This component only orchestrates and projects to screen,
/// keeping the MonoBehaviour thin (AGENTS.md).
/// </summary>
public sealed class LobbyArsenal : MonoBehaviour
{
    /// <summary>A single weapon pedestal: its interaction anchor, weapon index and proximity reach.</summary>
    [Serializable]
    public sealed class Pedestal
    {
        [Tooltip("Interaction anchor; the card is projected from this world position and proximity is measured to it.")]
        public Transform anchor;

        [Tooltip("Weapon index into WeaponLoadout (0 Gauntlet, 1 Bow, 2 Spear).")]
        public int weaponIndex;

        [Tooltip("Proximity radius; the card shows and the interaction is allowed within this reach.")]
        [Min(0f)] public float proximityRange = 3.5f;

        [Tooltip("Scene-owned physical marker; visible only for the weapon actually equipped.")]
        public GameObject equippedIndicator;
    }

    [SerializeField] private PlayerActor _player;
    [SerializeField] private Camera _camera;
    [SerializeField] private LobbyInteraction _lobby;
    [SerializeField] private Pedestal[] _pedestals;
    [Tooltip("Experimental scene only: compact proximity card with explicit kit disclosure.")]
    [SerializeField] private bool _compactPresentation;
    private bool _detailsRequested;
    public bool DetailsRequested => _detailsRequested;
    public int FocusedWeaponIndex => _nearestIndex >= 0 ? _pedestals[_nearestIndex].weaponIndex : -1;
    public bool BlocksPointer(Vector2 screenPosition) => _compactPresentation && _nearestIndex >= 0 &&
        _cardView != null && _cardView.LastRect.Contains(new Vector2(screenPosition.x,
            Screen.height - screenPosition.y) / WeaponCardView.Scale);

    // Cached parallel arrays for the pure proximity math, rebuilt only when the pedestal count changes.
    private Vector3[] _anchorPositions;
    private float[] _radii;

    private WeaponScript[] _weapons;
    private WeaponCardView _cardView;
    private int _nearestIndex = PedestalProximity.None;
    public Rect VisibleCardRect => _nearestIndex != PedestalProximity.None && _cardView != null ? _cardView.LastRect : default;

    private void RefreshEquipmentMarkers()
    {
        if (_pedestals == null || _weapons == null || !_player) return;
        int equipped = Array.IndexOf(_weapons, _player.CurrentWeapon);
        foreach (var pedestal in _pedestals)
        {
            if (pedestal?.equippedIndicator == null) continue;
            pedestal.equippedIndicator.SetActive(pedestal.weaponIndex == equipped);
            // A nearby pedestal may project behind the training HUD. Keep its label out of that strip.
            if (_camera && pedestal.equippedIndicator.TryGetComponent(out Renderer label))
            {
                Vector3 point = _camera.WorldToViewportPoint(pedestal.equippedIndicator.transform.position);
                label.enabled = point.z > 0f && point.y > .24f;
            }
        }
    }

    /// <summary>Configures the arsenal from the scene builder (no scene lookups at runtime).</summary>
    public void Configure(PlayerActor player, Camera lobbyCamera, Pedestal[] pedestals)
    {
        _player = player;
        _camera = lobbyCamera;
        _pedestals = pedestals;
        EnsureBuffers();
    }

    /// <summary>
    /// Mirrors <see cref="LobbyInteraction.BlocksAbilityInput"/> so the Skill3 interaction near a
    /// pedestal is not also consumed as an ability: returns true when the pressed key is the
    /// interaction key and the player stands within a pedestal's reach.
    /// </summary>
    public bool BlocksAbilityInput(KeyCode key)
    {
        if (!_player || _pedestals == null) return false;
        if (key != GamePreferences.Binding(GameControl.Skill3) &&
            !(_compactPresentation && key == GamePreferences.Binding(GameControl.Skill4))) return false;
        return NearestInRange() != PedestalProximity.None;
    }

    private void Awake()
    {
        if (!_player)
        {
            Debug.LogError("LobbyArsenal requires a player reference.", this);
            enabled = false;
            return;
        }
        if (_pedestals == null || _pedestals.Length == 0)
        {
            Debug.LogError("LobbyArsenal requires at least one pedestal.", this);
            enabled = false;
            return;
        }

        _weapons = Array.ConvertAll(WeaponLoadout.ResourcePaths, path => Resources.Load<WeaponScript>(path));
        EnsureBuffers();
    }

    private void Update()
    {
        RefreshEquipmentMarkers();
        int previous = _nearestIndex;
        _nearestIndex = PedestalProximity.None;
        if (Time.timeScale <= 0f || !_player) return;
        if (_player.TryGetComponent(out PauseMenuUI pause) && pause.BlocksInput) return;
        if (_lobby && _lobby.IsPanelOpen) return;

        _nearestIndex = NearestInRange();
        if (_nearestIndex != previous) _detailsRequested = false;
        if (_nearestIndex == PedestalProximity.None) return;
        if (_compactPresentation && GamePreferences.WasPressed(GameControl.Skill4))
            _detailsRequested = !_detailsRequested;
        if (!GamePreferences.WasPressed(GameControl.Skill3)) return;

        Interact(_pedestals[_nearestIndex]);
    }

    /// <summary>Resolves the pedestal action from the pure state and applies it via <see cref="WeaponLoadout"/>.</summary>
    private void Interact(Pedestal pedestal)
    {
        int index = pedestal.weaponIndex;
        WeaponPedestalState state = WeaponPedestalState.FromLoadout(index);
        bool canAfford = CurrencyWallet.CanAfford(state.UnlockCost);

        switch (state.Resolve(canAfford))
        {
            case PedestalAction.Equip:
                if (WeaponLoadout.Select(_player, index)) RefreshPlayerLoadout();
                break;
            case PedestalAction.Unlock:
                if (WeaponLoadout.TryUnlock(index) && WeaponLoadout.Select(_player, index)) RefreshPlayerLoadout();
                break;
            case PedestalAction.Denied:
                // Locked and cannot afford: communicate the lack of coins without changing any state.
                Debug.Log("Moedas insuficientes para liberar esta arma.", this);
                break;
        }
    }

    /// <summary>Refreshes the player's ability loadout so HUD and Training Dummy reflect the new weapon.</summary>
    private void RefreshPlayerLoadout()
    {
        if (_player.TryGetComponent(out AbilityHolder holder)) holder.RefreshLoadout();
    }

    private void OnGUI()
    {
        if (Time.timeScale <= 0f || !_player) return;
        if (_nearestIndex == PedestalProximity.None) return;
        if (_player.TryGetComponent(out PauseMenuUI pause) && pause.BlocksInput) return;
        if (_lobby && _lobby.IsPanelOpen) return;

        Camera camera = _camera ? _camera : Camera.main;
        if (!camera) return; // never throw when no camera is available

        Pedestal pedestal = _pedestals[_nearestIndex];
        if (pedestal?.anchor == null) return;

        Vector3 projected = camera.WorldToScreenPoint(pedestal.anchor.position);
        if (projected.z <= 0f) return; // anchor is behind the camera; skip drawing

        _cardView ??= new WeaponCardView();
        Vector3 playerProjected = camera.WorldToScreenPoint(_player.transform.position);
        var playerPoint = new Vector2(playerProjected.x, Screen.height - playerProjected.y);
        if (_compactPresentation)
            _detailsRequested = _cardView.DrawCompact(BuildCardContent(pedestal), playerPoint, _detailsRequested,
                GamePreferences.BindingLabel(GameControl.Skill4));
        else _cardView.Draw(BuildCardContent(pedestal), playerPoint);
    }

    /// <summary>Assembles the card content for a pedestal from its weapon and lock/equip state.</summary>
    private WeaponCardContent.CardContent BuildCardContent(Pedestal pedestal)
    {
        int index = pedestal.weaponIndex;
        WeaponScript weapon = index >= 0 && _weapons != null && index < _weapons.Length ? _weapons[index] : null;
        WeaponPedestalState state = WeaponPedestalState.FromLoadout(index);
        int equipped = _player && _player.CurrentWeapon != null && _weapons != null
            ? Array.IndexOf(_weapons, _player.CurrentWeapon)
            : -1;

        WeaponCardContent.StateKind kind;
        if (state.IsEquipped(equipped)) kind = WeaponCardContent.StateKind.Equipped;
        else if (state.IsUnlocked) kind = WeaponCardContent.StateKind.Equippable;
        else if (CurrencyWallet.CanAfford(state.UnlockCost)) kind = WeaponCardContent.StateKind.Unlockable;
        else kind = WeaponCardContent.StateKind.Insufficient;

        return WeaponCardContent.Build(weapon, kind, state.UnlockCost, GamePreferences.BindingLabel(GameControl.Skill3));
    }

    /// <summary>Runs the pure proximity selection over the cached anchor/radius buffers.</summary>
    private int NearestInRange()
    {
        EnsureBuffers();
        if (_anchorPositions == null || _radii == null) return PedestalProximity.None;
        for (int i = 0; i < _pedestals.Length; i++)
        {
            Pedestal pedestal = _pedestals[i];
            _anchorPositions[i] = pedestal?.anchor ? pedestal.anchor.position : new Vector3(float.PositiveInfinity, 0f, 0f);
            _radii[i] = pedestal?.anchor ? pedestal.proximityRange : 0f;
        }
        return PedestalProximity.SelectNearest(_player.transform.position, _anchorPositions, _radii);
    }

    /// <summary>Allocates the proximity buffers when the pedestal array changes.</summary>
    private void EnsureBuffers()
    {
        int count = _pedestals?.Length ?? 0;
        if (count == 0)
        {
            _anchorPositions = null;
            _radii = null;
            return;
        }
        if (_anchorPositions == null || _anchorPositions.Length != count)
        {
            _anchorPositions = new Vector3[count];
            _radii = new float[count];
        }
    }

    private void OnDestroy()
    {
        _cardView?.Dispose();
        _cardView = null;
    }
}
