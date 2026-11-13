using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thin runtime component that replaces the single <c>ARSENAL / ARMAS</c> panel of
/// <see cref="LobbyInteraction"/> with the Hades-style physical pedestals: it reads the nearest
/// pedestal by proximity, draws its weapon card anchored to the pedestal, and equips/unlocks the
/// weapon in place with the same interaction key already used in the lobby (<see cref="GamePreferences.InteractionAction"/>).
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
        (_cardView != null && _cardView.LastRect.Contains(new Vector2(screenPosition.x,
            Screen.height - screenPosition.y) / WeaponCardView.Scale) ||
         IcoMenuVisible && _icoMenu.ContainsPointer(screenPosition));

    // Cached parallel arrays for the pure proximity math, rebuilt only when the pedestal count changes.
    private Vector3[] _anchorPositions;
    private float[] _radii;

    private WeaponScript[] _weapons;
    private WeaponCardView _cardView;
    // Fase 1c: the Gauntlet's companion (Ico) lives on its pedestal — rapport on the card, bond panel on interaction.
    private MetaProgressStore _metaStore;
    private MetaProgressProfile _metaProfile;
    private readonly WeaponCardView.BondInfo _bond = new WeaponCardView.BondInfo();

    /// <summary>True when the pedestal's weapon has a companion with a bond (only the Gauntlet for now).</summary>
    private bool HasBond(int weaponIndex) =>
        _weapons != null && weaponIndex >= 0 && weaponIndex < _weapons.Length && _weapons[weaponIndex] &&
        WeaponRunModifiers.Identify(_weapons[weaponIndex]) == RunWeaponFamily.Gauntlet;

    private void ReloadMetaProfile()
    {
        _metaStore ??= MetaProgressStore.CreateDefault();
        _metaProfile = _metaStore.Load();
    }

    // Etapa 4b: no full-screen panel. Next to the equipped Gauntlet a mini menu offers "Conversar" (interaction key)
    // and "Entregar Easter Egg" (key 1); artifacts moved to their own cabinet (ArtifactCabinet).
    private readonly ProximityMenu _icoMenu = new ProximityMenu();
    private readonly List<ProximityMenu.Option> _icoOptions = new List<ProximityMenu.Option>(2);
    private readonly System.Random _chatRng = new System.Random();

    /// <summary>True when the mini menu with Ico is showing (the equipped Gauntlet's pedestal is in reach).</summary>
    public bool IcoMenuVisible => _nearestIndex != PedestalProximity.None && IsBondPedestal(_pedestals[_nearestIndex]);

    private bool IsBondPedestal(Pedestal pedestal)
    {
        if (pedestal == null || !HasBond(pedestal.weaponIndex)) return false;
        int equipped = _player && _player.CurrentWeapon != null && _weapons != null ? Array.IndexOf(_weapons, _player.CurrentWeapon) : -1;
        return equipped == pedestal.weaponIndex;
    }

    private CharControlScript InputOwner => _player && _player.TryGetComponent(out CharControlScript owner) ? owner : null;

    /// <summary>"Conversar": the free first conversation once, then a short chat line (public for tests and tools).</summary>
    public void TalkToIco()
    {
        if (DialoguePanel.IsOpen || GameplayModal.IsOpen) return;
        ReloadMetaProfile();
        string intro = NexusDialogues.IcoKey(0);
        if (!_metaProfile.HasSeenDialogue(intro))
        {
            DialoguePanel.Show(Rapport.IcoDisplayName, NexusDialogues.Ico(0), () => { _metaProfile = _metaStore.RecordDialogueSeen(intro); }, InputOwner);
            return;
        }
        string line = NexusDialogues.IcoChatLine(Rapport.IcoLevel(_metaProfile), _chatRng.NextDouble());
        DialoguePanel.Show(Rapport.IcoDisplayName, new[] { line }, null, InputOwner);
    }

    /// <summary>"Entregar Easter Egg": one gift to Ico and her next conversation. False without a gift or at her maximum.</summary>
    public bool TryGiveGiftToIco()
    {
        if (DialoguePanel.IsOpen || GameplayModal.IsOpen) return false;
        _metaStore ??= MetaProgressStore.CreateDefault();
        int before = Rapport.IcoLevel(_metaStore.Load());
        bool given = _metaStore.TryGiveGift(Rapport.Ico, out var profile);
        _metaProfile = profile;
        if (!given)
        {
            RunToast.Show(profile.gifts <= 0 ? "Você não tem nenhum " + Rapport.GiftName + "." : "A Ico já te deu tudo o que tinha.", BondFrameArt.Muted, 2.5f);
            return false;
        }
        int gifts = profile.GiftsGivenTo(Rapport.Ico), after = Rapport.IcoLevel(gifts);
        string key = NexusDialogues.IcoKey(gifts);
        DialoguePanel.Show(Rapport.IcoDisplayName, NexusDialogues.Ico(gifts), () =>
        {
            _metaProfile = _metaStore.RecordDialogueSeen(key);
            if (after > before)
                RunToast.Show("Vínculo nível " + after + " — " + BondFrameArt.TierName(after) + "!  " + Rapport.IcoRewardSummary(after), BondFrameArt.Gold, 5f);
            else RunToast.Show("A Ico guardou o " + Rapport.GiftName + ". Falta " + Rapport.IcoGiftsToNextLevel(gifts) + " para o próximo nível.", BondFrameArt.Gold, 4f);
        }, InputOwner);
        return true;
    }
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
    public bool HasInteractionInRange => _player && NearestInRange() != PedestalProximity.None;

    public bool BlocksAbilityInput(KeyCode key)
    {
        if (!_player || _pedestals == null) return false;
        if (key != GamePreferences.Binding(GamePreferences.InteractionAction) &&
            !(!GamePreferences.UsesDirectControls && _compactPresentation && key == GamePreferences.Binding(GameControl.Skill4))) return false;
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
        ReloadMetaProfile();
    }

    private void Update()
    {
        RefreshEquipmentMarkers();
        int previous = _nearestIndex;
        _nearestIndex = PedestalProximity.None;
        if (Time.timeScale <= 0f || !_player) return;
        if (_player.TryGetComponent(out PauseMenuUI pause) && pause.BlocksInput) return;
        if (_lobby && _lobby.IsPanelOpen) return;
        if (GameplayModal.IsOpen || DialoguePanel.IsOpen) return;

        _nearestIndex = NearestInRange();
        if (_nearestIndex != previous) _detailsRequested = false;
        if (_nearestIndex == PedestalProximity.None) return;
        if (!GamePreferences.UsesDirectControls && _compactPresentation && GamePreferences.WasPressed(GameControl.Skill4))
            _detailsRequested = !_detailsRequested;
        if (_player.TryGetComponent(out CharControlScript controls) && !controls.GameplayInputReady) return;
        if (IsBondPedestal(_pedestals[_nearestIndex]) && _icoMenu.WasChosen(1))
        { if (controls) controls.RequireAttackRelease(); TryGiveGiftToIco(); return; }
        if (!GamePreferences.WasPressed(GamePreferences.InteractionAction) &&
            !(IsBondPedestal(_pedestals[_nearestIndex]) && _icoMenu.WasChosen(0))) return;
        if (controls) controls.RequireAttackRelease();

        Interact(_pedestals[_nearestIndex]);
    }

    /// <summary>Resolves the pedestal action from the pure state and applies it via <see cref="WeaponLoadout"/>.</summary>
    private void Interact(Pedestal pedestal)
    {
        int index = pedestal.weaponIndex;
        WeaponPedestalState state = WeaponPedestalState.FromLoadout(index);
        // Fase 1b: no coin purchase. Locked weapons are released by universe progression (Fase 1d).
        int equipped = _player.CurrentWeapon != null && _weapons != null ? Array.IndexOf(_weapons, _player.CurrentWeapon) : -1;
        // The equipped weapon's pedestal opens the bond with its companion instead of re-equipping.
        if (state.IsUnlocked && state.IsEquipped(equipped) && HasBond(index)) { TalkToIco(); return; }
        if (state.IsUnlocked) { if (WeaponLoadout.Select(_player, index)) RefreshPlayerLoadout(); }
        else Debug.Log(WeaponLoadout.ProgressionLockedLabel + ".", this);
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
        {
            _detailsRequested = _cardView.DrawCompact(BuildCardContent(pedestal), playerPoint, _detailsRequested,
                GamePreferences.UsesDirectControls ? "Clique" : GamePreferences.BindingLabel(GameControl.Skill4),
                BuildBond(pedestal), !WeaponPedestalState.FromLoadout(pedestal.weaponIndex).IsUnlocked);
            DrawIcoMenu(pedestal);
        }
        else
        {
            var state = WeaponPedestalState.FromLoadout(pedestal.weaponIndex);
            _cardView.Draw(BuildCardContent(pedestal), playerPoint, BuildBond(pedestal), !state.IsUnlocked);
            DrawIcoMenu(pedestal);
        }
    }

    private void DrawIcoMenu(Pedestal pedestal)
    {
        if (!IsBondPedestal(pedestal) || DialoguePanel.IsOpen) return;
        if (_metaProfile == null) ReloadMetaProfile();
        int given = _metaProfile.GiftsGivenTo(Rapport.Ico), level = Rapport.IcoLevel(given), gifts = _metaProfile.gifts;
        bool canReceive = Rapport.CanReceive(Rapport.Ico, given);
        _icoOptions.Clear();
        _icoOptions.Add(new ProximityMenu.Option("Conversar",
            _metaProfile.HasSeenDialogue(NexusDialogues.IcoKey(0)) ? "" : "Ela parece querer dizer alguma coisa."));
        _icoOptions.Add(!canReceive
            ? new ProximityMenu.Option("Entregar " + Rapport.GiftName, "A Ico já te deu tudo o que tinha.", false, "COMPLETO")
            : gifts <= 0
                ? new ProximityMenu.Option("Entregar " + Rapport.GiftName, "Você não tem nenhum. Pesca na sala serena, elites e o boss.", false, "SEM ITEM")
                : new ProximityMenu.Option("Entregar " + Rapport.GiftName,
                    Rapport.IcoGiftsToNextLevel(given) == 1 ? "Leva ao nível " + (level + 1) + ": " + Rapport.IcoRewardSummary(level + 1)
                        : "Faltam " + Rapport.IcoGiftsToNextLevel(given) + " para o nível " + (level + 1) + ".", true, gifts + " em mãos"));
        _icoMenu.Draw("ICO", "Vínculo " + level + "/" + Rapport.IcoMaxLevel + "  ·  " + BondFrameArt.TierName(level), _icoOptions, level);
    }

    /// <summary>Rapport summary for the card, or null when the pedestal's weapon has no companion yet.</summary>
    private WeaponCardView.BondInfo BuildBond(Pedestal pedestal)
    {
        if (!HasBond(pedestal.weaponIndex)) return null;
        if (_metaProfile == null) ReloadMetaProfile();
        int given = _metaProfile.GiftsGivenTo(Rapport.Ico), level = Rapport.IcoLevel(given);
        _bond.CompanionName = Rapport.IcoDisplayName; _bond.Level = level; _bond.MaxLevel = Rapport.IcoMaxLevel;
        _bond.Progress = Rapport.IcoLevelProgress(given); _bond.GiftsInHand = _metaProfile.gifts;
        _bond.NextReward = level >= Rapport.IcoMaxLevel ? "" : Rapport.IcoRewardSummary(level + 1);
        _bond.ArtifactName = Artifacts.TryGet(_metaProfile.equippedArtifact, out var artifact) ? artifact.Name : "";
        int equipped = _player && _player.CurrentWeapon != null && _weapons != null ? Array.IndexOf(_weapons, _player.CurrentWeapon) : -1;
        _bond.Prompt = "";
        return _bond;
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
        else kind = WeaponCardContent.StateKind.ProgressionLocked;

        return WeaponCardContent.Build(weapon, kind, state.UnlockCost, GamePreferences.BindingLabel(GamePreferences.InteractionAction));
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
