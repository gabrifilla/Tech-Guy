using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Fase 1c (12/11/2026) on the run side: rapport gifts (rare drops from elites, the boss and fishing), the serene
/// fishing room, and the effects of the equipped artifact. Gifts are banked in MetaProgress the moment they are
/// found, so they survive death and abandon. The serene room is a run-only conversion applied after the route
/// draw with its own seeded stream, so the tested draw (09/09B) is unchanged.
/// </summary>
public sealed partial class ProgressionDirector
{
    public const string ForceSereneRoomPrefKey = "TechGuy.Prototype.ForceSereneRoom";
    private const ulong SereneStream = 0x5E2E_4E00_0000_0004UL, GiftStream = 0x61F7_0000_0000_0005UL;
    /// <summary>The serene room's rest: a small heal on entry (it is a calm room, not a repair station).</summary>
    public const float SereneRestFraction = .15f;
    private static readonly Color GiftColor = new Color(1f, .82f, .25f);

    private System.Random _giftRng;
    private int _runGifts, _sereneNode = -1;
    private bool _secondRoundUsed;
    private FishingSpot _fishingSpot;

    /// <summary>Rapport gifts found in this run (already banked).</summary>
    public int RunGifts => _runGifts;
    /// <summary>Node of this run's serene fishing room, or -1 when the run has none.</summary>
    public int ManualSereneNode => _sereneNode;
    public FishingSpot ManualFishingSpot => _fishingSpot;

    private bool Equipped(Artifact artifact) =>
        artifact != null && _metaProfileAtStart != null && _metaProfileAtStart.HasEquipped(artifact.Id);

    private static bool ForceSereneRoom
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(ForceSereneRoomPrefKey, false);
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Rare serene room: with <see cref="GiftEconomy.SereneRoomChance"/>, one branch Combat room becomes a calm
    /// fishing room, so entering it is always a choice at a fork (never forced, never the Mutation room, and never
    /// opposite the repair room).
    /// </summary>
    private void ApplySereneRoom(ManualRouteSnapshot route)
    {
        _sereneNode = -1;
        if (route == null) return;
        ulong seed = route.RewardSeed ?? 0UL;
        var rng = new SeededRng(SeedDerivation.SplitMix64(seed ^ SereneStream));
        bool wanted = rng.Roll(GiftEconomy.SereneRoomChance) || ForceSereneRoom;
        var candidates = SereneRoomPlan.Candidates(route);
        if (!wanted || candidates.Count == 0) return;
        var node = route.Node(candidates[rng.NextInt(0, candidates.Count)]);
        node.ConvertToSerene(SereneRestFraction);
        _roomPackByNode.Remove(node.Id);
        _sereneNode = node.Id;
    }

    /// <summary>Entering the serene room: the rest heal resolves at once (no menu) and the pond appears.</summary>
    private void EnterSereneRoom(ManualNodeDefinition node)
    {
        var id = ManualSession.Id;
        if (!TryResolveRepair(id, node.Id, true)) TryResolveRepair(id, node.Id, false);
        if (_fishingSpot) Destroy(_fishingSpot.gameObject);
        int seed = unchecked((int)SeedDerivation.SplitMix64((ManualSession.Route.RewardSeed ?? 0UL) ^ GiftStream ^ (ulong)node.Id));
        _fishingSpot = FishingSpot.Spawn(transform, CapsulePosition(node), _player.transform, GiftEconomy.CastsPerVisit, seed,
            () => ManualSession != null && ManualSession.Id == id && !ManualSession.IsClosed && _player && !_player.IsDead &&
                  !(_manualHolder && _manualHolder.IsGamePaused),
            ResolveCatch);
        RunToast.Show("Sala serena. Respire — e pesque, se quiser.", new Color(.38f, .86f, .78f), 3.5f);
    }

    /// <summary>What a successful pull brings: rarely a gift, otherwise a few Fragmentos.</summary>
    private string ResolveCatch()
    {
        _giftRng ??= new System.Random();
        var profile = _metaProgress != null ? _metaProgress.Load() : null;
        // The Anzol da Sorte must be the one equipped for this run, not swapped in afterwards.
        if (profile != null && _metaProfileAtStart != null) profile.equippedArtifact = _metaProfileAtStart.equippedArtifact;
        if (GiftEconomy.FishingGivesGift(profile, _giftRng.NextDouble()))
        {
            AwardGift(_fishingSpot ? _fishingSpot.transform.position : _player.transform.position);
            return "Você fisgou um " + Rapport.GiftName + "!";
        }
        int fragments = _giftRng.Next(GiftEconomy.FishFragmentsMin, GiftEconomy.FishFragmentsMax + 1);
        AddRunFragments(fragments); FlushRunFragments();
        return "Um peixe de dados: +" + fragments + " Fragmentos.";
    }

    private void TryDropGift(Vector3 position, double chance)
    {
        if (!_fragmentsTracking || _giftRng == null || _giftRng.NextDouble() >= chance) return;
        AwardGift(position);
    }

    private void AwardGift(Vector3 position)
    {
        if (ManualSession == null) return;
        _metaProgress ??= MetaProgressStore.CreateDefault();
        _metaProgress.AddGift(ManualSession.Id.ToString());
        _runGifts++;
        DamagePopup.ShowText(position + Vector3.up * 1.4f, Rapport.GiftName.ToUpperInvariant() + "!", GiftColor);
        RunToast.Show(Rapport.GiftName + " encontrado! Entregue a alguém no Nexus.", GiftColor, 4.5f);
    }

    /// <summary>After the run upgrades: rapport level for the boon pool and the equipped artifact's effects.</summary>
    private void ApplyRapportToRun()
    {
        _runGifts = 0; _secondRoundUsed = false;
        ulong seed = ManualSession != null && ManualSession.Route != null ? ManualSession.Route.RewardSeed ?? 0UL : 0UL;
        _giftRng = new System.Random(unchecked((int)SeedDerivation.SplitMix64(seed ^ GiftStream)));
        if (_boons) _boons.ConfigureManualRapport(Rapport.IcoLevel(_metaProfileAtStart), Equipped(Artifacts.Mouthguard) ? Artifacts.MouthguardExtraChoices : 0);
        if (_player) _player.LethalDamageInterceptor = Equipped(Artifacts.SecondRound) ? SecondRound : null;
    }

    private void EndRapportRun()
    {
        if (_player)
        {
            if (_player.LethalDamageInterceptor == SecondRound) _player.LethalDamageInterceptor = null;
            _player.RemoveDamageImmunity(this);
        }
        if (_fishingSpot) Destroy(_fishingSpot.gameObject);
        _fishingSpot = null;
    }

    /// <summary>Segundo Round: once per run the knockout is refused — back up with 40% health and a short immunity.</summary>
    private bool SecondRound(Actor actor)
    {
        if (_secondRoundUsed || !actor || actor != _player || ManualSession == null || ManualSession.IsClosed) return false;
        _secondRoundUsed = true;
        actor.Heal(actor.maxHealth * Artifacts.SecondRoundHealFraction);
        if (actor.health <= 0f) return false;
        actor.AddDamageImmunity(this);
        StartCoroutine(EndSecondRoundImmunity(actor));
        DamagePopup.ShowText(actor.transform.position + Vector3.up * 1.8f, "SEGUNDO ROUND!", GiftColor);
        RunToast.Show("Segundo Round! A Ico te pôs de pé.", GiftColor, 3.5f);
        return true;
    }

    private IEnumerator EndSecondRoundImmunity(Actor actor)
    {
        yield return new WaitForSeconds(Artifacts.SecondRoundImmunitySeconds);
        if (actor) actor.RemoveDamageImmunity(this);
    }
}

/// <summary>Pure choice of which rooms may become the serene room (testable without a scene).</summary>
public static class SereneRoomPlan
{
    /// <summary>
    /// Branch rooms of kind Combat whose own rewards never include a Mutation and whose fork does not already offer
    /// a Repair room on the other side. Ordered by id so the seeded pick is stable.
    /// </summary>
    public static List<int> Candidates(ManualRouteSnapshot route)
    {
        var result = new List<int>();
        if (route == null) return result;
        foreach (var group in route.Groups)
        {
            Consider(route, group.FirstBranch, group.SecondBranch, result);
            Consider(route, group.SecondBranch, group.FirstBranch, result);
        }
        result.Sort();
        return result;
    }

    private static void Consider(ManualRouteSnapshot route, IReadOnlyList<int> branch, IReadOnlyList<int> other, List<int> result)
    {
        if (branch == null || other == null) return;
        if (other.Any(id => route.Node(id) != null && route.Node(id).Kind == ManualNodeKind.Repair)) return;
        foreach (int id in branch)
        {
            var node = route.Node(id);
            if (node == null || node.Kind != ManualNodeKind.Combat || id == route.StartNode || id == route.BossNode) continue;
            bool mutation = false;
            for (int i = 0; i < node.TotalAcquisitions; i++) if (node.RewardAt(i) == RunRewardKind.Mutation) mutation = true;
            if (!mutation && !result.Contains(id)) result.Add(id);
        }
    }
}

/// <summary>A short line at the top of the screen for run events (gift found, serene room, Segundo Round).</summary>
public sealed class RunToast : MonoBehaviour
{
    private static RunToast _current;
    private string _text = "";
    private Color _color = Color.white;
    private float _until, _shownAt;
    private GUIStyle _style;

    public static string CurrentText => _current && Time.unscaledTime < _current._until ? _current._text : "";

    public static void Show(string text, Color color, float seconds)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (!_current) _current = new GameObject("Run toast").AddComponent<RunToast>();
        _current._text = text; _current._color = color;
        _current._shownAt = Time.unscaledTime; _current._until = Time.unscaledTime + Mathf.Max(.5f, seconds);
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || Time.unscaledTime >= _until) return;
        _style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        _style.fontSize = Mathf.RoundToInt(Screen.height / 30f);
        float alpha = Mathf.Clamp01(Mathf.Min((Time.unscaledTime - _shownAt) / .25f, (_until - Time.unscaledTime) / .5f));
        var rect = new Rect(0, Screen.height * .16f, Screen.width, Screen.height * .06f);
        _style.normal.textColor = new Color(0, 0, 0, alpha * .85f);
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), _text, _style);
        _style.normal.textColor = new Color(_color.r, _color.g, _color.b, alpha);
        GUI.Label(rect, _text, _style);
    }
}
