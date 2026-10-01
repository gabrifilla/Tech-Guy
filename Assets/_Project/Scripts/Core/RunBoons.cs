using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Player-owned upgrades. All modified weapons and skills are isolated runtime copies.</summary>
[RequireComponent(typeof(PlayerActor), typeof(AbilityHolder))]
public sealed class RunBoons : MonoBehaviour
{
    public sealed class Offer
    {
        public readonly string Id, Title, Description;
        public Offer(string id, string title, string description) { Id = id; Title = title; Description = description; }
    }
    private readonly List<Offer> _choices = new List<Offer>();
    private readonly List<Offer> _acquired = new List<Offer>();
    private readonly List<ScriptableObject> _owned = new List<ScriptableObject>();
    private readonly System.Random _random = new System.Random();
    // Seedable seam for the Rewrite appearance roll (R9.3): null in production, so the gate falls back
    // to the shared RNG (_random.NextDouble); tests assign it (by reflection) to a deterministic
    // Func<double> so the <= 20% gate can be driven reproducibly. Never touches gameplay behavior
    // beyond which value the roll reads. Used only by OfferReward's Rewrite gate.
    private Func<double> _rewriteRoll;
    private PlayerActor _player;
    private AbilityHolder _holder;
    private WeaponScript _weapon;
    private NavMeshAgent _agent;
    public bool IsChoosing { get; private set; }
    public int RewardRoom { get; private set; }
    public IReadOnlyList<Offer> Choices => _choices;
    public IReadOnlyList<Offer> Acquired => _acquired;
    public event Action RewardChosen;
    public WeaponRunModifiers WeaponModifiers { get; private set; }
    public WeaponScript RunWeapon => _weapon;
    // Escalating "system breaking" feedback state (R10); owned and driven per run.
    private SystemBreakState _systemBreak;
    // Per-run combat-event bus (R8/R11); created per run and cleared on run end so a previous
    // run's subscribers never fire later. Reached by collaborators through this getter (RunBoons
    // ownership), never via scene lookups.
    public HookBus Hooks { get; private set; }
    // impactful-weapon-boons R2: the run-scoped Split Arrow coordinator, created lazily the first time
    // the boon is chosen and reconfigured (never re-added) on later picks so its rank tracks the catalog.
    // Its HookBus.OnKill subscription is dropped by Hooks.Clear() at run end (R1.4).
    private SplitArrowCoordinator _splitArrow;
    // impactful-weapon-boons R6: the run-scoped Momentum Strike stack holder, created lazily the first
    // time the boon is chosen and reconfigured (never re-added) on later picks so its rank tracks the
    // catalog. Its HookBus.OnHit subscription is dropped by Hooks.Clear() at run end; its stat modifier
    // is tagged with this RunBoons instance so OnDestroy's RemoveModifiersFrom(this) removes it (R6.5).
    private MomentumStacks _momentum;
    // gauntlet-boon-playstyle-overhaul R6: the run-scoped Hungry Combo tracker, created lazily the first
    // time the boon is chosen and reconfigured (never re-added) on later picks so its rank tracks the
    // catalog. Its HookBus.OnBasicHit subscription is dropped by Hooks.Clear() at run end (R6.4); it holds
    // no stat modifier, so it only needs the subscription teardown.
    private HungryComboTracker _hungryCombo;
    // gauntlet-boon-playstyle-overhaul R4: the run-scoped Asura Fist coordinator, created lazily the first
    // time the boon is chosen and reconfigured (never re-added) on later picks so its rank tracks the
    // catalog. Its HookBus.OnBasicHit subscription is dropped by Hooks.Clear() at run end (R4.4); it holds
    // no stat modifier, so it only needs the subscription teardown.
    private AsuraSurge _asuraSurge;
    // gauntlet-boon-playstyle-overhaul R5: the run-scoped Guard Breaker tracker, created lazily the first
    // time the boon is chosen and reconfigured (never re-added) on later picks so its rank tracks the
    // catalog. Its HookBus.OnBasicHit and AbilityHolder.AbilityUsed subscriptions are dropped by the
    // tracker's OnDestroy and (for the basic channel) by Hooks.Clear() at run end (R5.5); it holds no stat
    // modifier, so it only needs the subscription teardown.
    private ImpactGuardTracker _guardBreaker;
    // gauntlet-boon-playstyle-overhaul R8: the run-scoped Kiting Step coordinator, created lazily the first
    // time the Bow boon is chosen and reconfigured (never re-added) on later picks so its rank tracks the
    // catalog. It subscribes to CharControlScript.BasicAttackPerformed and repositions the player via the
    // player's NavMeshAgent; the subscription is dropped by the coordinator's OnDestroy when the run (and
    // this GameObject) is torn down (R8.5). It holds no stat modifier, so it only needs that teardown.
    private KitingStepCoordinator _kitingStep;
    // gauntlet-boon-playstyle-overhaul R9: the run-scoped Adaptive Cadence tracker, created lazily the
    // first time the boon is chosen and reconfigured (never re-added) on later picks so its rank tracks
    // the catalog. Its HookBus.OnBasicHit subscription is dropped by Hooks.Clear() at run end; its stat
    // modifier is tagged with this RunBoons instance so OnDestroy's RemoveModifiersFrom(this) removes it (R9.4).
    private AdaptiveCadenceTracker _adaptiveCadence;
    // gauntlet-boon-playstyle-overhaul R10: the run-scoped Rain Mark registry, created lazily the first
    // time the Bow ultimate boon is chosen and reconfigured (never re-added) on later picks so its
    // amplify/slow track the catalog rank. It holds no HookBus subscription or stat modifier; its own
    // OnDestroy clears every mark and restores any pending slow at run end (R10.3/R10.4). The direct-damage
    // pipeline and the rain pulse reach it through the RainMarks accessor below (no scene lookup).
    private RainMarkRegistry _rainMark;
    /// <summary>
    /// The run-scoped Rain Mark registry, or null when the Bow ultimate boon was never chosen this run.
    /// Consulted by the direct-damage pipeline (amplifier, R10.2) and the rain pulse resolution (mark,
    /// R10.1). Every caller null-guards it since it only exists after the boon is picked.
    /// </summary>
    public RainMarkRegistry RainMarks => _rainMark;
    // gauntlet-boon-playstyle-overhaul R13: the run-scoped Edge Strike vulnerability registry, created
    // lazily the first time the Spear edge boon is chosen and reconfigured (never re-added) on later picks
    // so its rank tracks the catalog. It subscribes to AbilityHolder.AttackHitsResolved (dropped by its own
    // OnDestroy) and holds no stat modifier; its OnDestroy clears every open window at run end (R13.4). The
    // direct-damage pipeline reaches it through the EdgeVulnerabilities accessor below (no scene lookup).
    private EdgeVulnerabilityRegistry _edgeVuln;
    /// <summary>
    /// The run-scoped Edge Strike vulnerability registry, or null when the Spear edge boon was never
    /// chosen this run. Consulted by the direct-damage pipeline (amplifier, R13.2). Every caller
    /// null-guards it since it only exists after the boon is picked.
    /// </summary>
    public EdgeVulnerabilityRegistry EdgeVulnerabilities => _edgeVuln;
    // gauntlet-boon-playstyle-overhaul R2: the family boons retired from the offer composition. "Retire"
    // means skip in OfferReward's family gate, NOT delete from the Catalog, so runs/saves that already
    // acquired them keep working (R2.5/R2.6). Preserved family boons and inline gameplay boons are untouched.
    private static readonly HashSet<WeaponBoon> RetiredFamilyBoons = new HashSet<WeaponBoon>
    {
        WeaponBoon.LongFists, WeaponBoon.StanceCrusher, WeaponBoon.Berserker,   // Gauntlet (R2.2)
        WeaponBoon.HeavyBolt, WeaponBoon.Sniper,        WeaponBoon.LongRain,    // Bow (R2.2)
        WeaponBoon.LongReach, WeaponBoon.TripleMoon,    WeaponBoon.Affliction   // Spear (R2.2)
    };

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _holder = GetComponent<AbilityHolder>();
        _agent = GetComponent<NavMeshAgent>();
        _holder.BindRun(this);
        gameObject.AddComponent<RunRewardUI>().Bind(this);
    }

    private void Start()
    {
        if (!_player.CurrentWeapon) { Debug.LogError("Run requires an equipped weapon.", this); enabled = false; return; }
        _weapon = Instantiate(_player.CurrentWeapon);
        WeaponModifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(_weapon));
        _owned.Add(_weapon);
        Ability[] source = _weapon.abilities;
        _weapon.abilities = new Ability[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            if (!source[i]) continue;
            _weapon.abilities[i] = Instantiate(source[i]);
            _owned.Add(_weapon.abilities[i]);
        }
        _player.EquipWeapon(_weapon);
        _holder.RefreshLoadout();
        // A fresh escalation state per run; recomputed from Acquired whenever a reward is chosen (R10.5/R11.4).
        _systemBreak = new SystemBreakState();
        // A fresh event bus per run; collaborators (raise-sites) reach it via the Hooks getter.
        Hooks = new HookBus();
        RewardChosen += EvaluateSystemBreak;
    }

    // Recomputes the interacting-modifier count from the acquired set and drives the escalation tiers (R10.1/R10.2).
    private void EvaluateSystemBreak() => _systemBreak?.Evaluate(_acquired.Count);

    public void OfferReward(int room)
    {
        if (IsChoosing || !_weapon || !_player || _player.IsDead) return;
        RewardRoom = room;
        var pool = new List<Offer>
        {
            new Offer("conductor", "Bobina encadeada", "Acertos saltam com 45% do dano e carregam fogo/gelo. Cada cópia: +1 alvo por salto e +0,5m de alcance (inicial: 3,5m)."),
            new Offer("detonation", "Reator de sucata", "Mortes por impacto explodem: 75% do dano em 3,5m. Cópias: +25 pontos percentuais e +0,5m. Explosões continuam a cadeia."),
            new Offer("reactor", "Combustível instável", "Com fogo adquirido, sua queimadura ganha por segundo +12% do dano do acerto por cópia. Combine com críticos e descargas."),
            new Offer("resonance", "Ressonância térmica", "Descargas e explosões: +40% de dano por efeito de fogo/gelo já presente no alvo, por cópia. Prepare a horda com elementos!"),
            // impactful-weapon-boons R8: cross-family Elemental Overflow. Offered regardless of family, so it
            // lives in the inline pool next to the other synergy/element offers rather than the family catalog.
            new Offer("overflow", "Sobrecarga elemental", "Acertos em alvos com fogo/gelo ativo disparam um burst de 50% do dano do golpe por cópia, sem remover o efeito."),
            new Offer("vitality", "Coração de ferro", "+100 de vida máxima e recuperação completa de vida."),
            new Offer("focus", "Foco eficiente", "Q custa 25% menos mana e recarrega 35% mais rápido."),
            // Bizarre property-changing modifiers: they alter what basic attacks DO.
            new Offer("ignite", "Lâmina incandescente", "BIZARRO: seus ataques agora causam QUEIMADURA, dano contínuo por 4s. Acumula."),
            new Offer("frost", "Toque glacial", "BIZARRO: seus ataques agora CONGELAM — chance de imobilizar e lentidão pesada por 2.5s."),
            new Offer("transform", _weapon.FiresArrows ? "Disparo prismático" : "Nova de impacto",
                _weapon.FiresArrows ? "TRANSFORMA Q: troca o disparo duplo por cinco flechas perfurantes em leque." :
                "TRANSFORMA Q: troca o avanço/estocada por uma explosão circular de 4m. Não gera Asura.")
        };
        foreach (var definition in WeaponRunModifiers.Catalog)
            if (definition.Family == WeaponModifiers.Family
                && !RetiredFamilyBoons.Contains(definition.Kind)                   // gauntlet-boon-playstyle-overhaul R2.2: skip retired family boons
                && WeaponModifiers.Rank(definition.Kind) < definition.MaxRank)
                pool.Add(new Offer(definition.Id, definition.Title, "Nível " + (WeaponModifiers.Rank(definition.Kind) + 1) + "/" + definition.MaxRank + "\n" + definition.Description));
        // Only single-use boons are removed once taken; repeatable ones (stats + elemental) stay in the pool.
        pool.RemoveAll(offer => IsSingleUse(offer.Id) && _acquired.Exists(owned => owned.Id == offer.Id));
        // Rewrite category (R9): exclude every Rewrite when no valid target ability exists (R9.5),
        // and gate its appearance behind a <= 20% roll per offer set (R9.3). At most one Rewrite is
        // ever added because only ability-replacing ids are classified as Rewrite and each is unique here.
        bool rewriteAllowed = HasRewriteTarget() && (_rewriteRoll ?? _random.NextDouble)() <= 0.2;
        if (!rewriteAllowed)
            pool.RemoveAll(offer => RunModifierPresentation.IsRewriteId(offer.Id));
        _choices.Clear();
        // Always include a skill-changing option while one remains, plus two different upgrades.
        // A Rewrite (when allowed) fills the skill slot; otherwise fall back to the focus upgrade.
        Offer skillOffer = pool.Find(offer => RunModifierPresentation.IsRewriteId(offer.Id)) ?? pool.Find(offer => offer.Id == "focus");
        if (skillOffer != null) { _choices.Add(skillOffer); pool.Remove(skillOffer); }
        var weaponOffers = pool.FindAll(offer => offer.Id.StartsWith("weapon_", StringComparison.Ordinal));
        if (weaponOffers.Count > 0)
        {
            Offer weaponOffer = weaponOffers[_random.Next(weaponOffers.Count)];
            _choices.Add(weaponOffer); pool.Remove(weaponOffer);
        }
        while (_choices.Count < 3 && pool.Count > 0)
        {
            int index = _random.Next(pool.Count);
            _choices.Add(pool[index]); pool.RemoveAt(index);
        }
        IsChoosing = true;
        if (_agent && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.ResetPath(); _agent.isStopped = true;
        }
        if (TryGetComponent(out CharControlScript controls)) controls.CancelCombo();
    }

    public bool Choose(int index)
    {
        if (Time.timeScale <= 0f || !IsChoosing || !_player || _player.IsDead || index < 0 || index >= _choices.Count || _holder.IsCasting) return false;
        if (_player.TryGetComponent(out PauseMenuUI pause) && pause.BlocksInput) return false;
        Offer offer = _choices[index];
        switch (offer.Id)
        {
            default:
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                // impactful-weapon-boons R4.3: on the first Perfect Spacing pick, attach the cosmetic
                // streak feedback. It subscribes to the resolved-hit channel and never gates the
                // rank-keyed damage term (that lives in WeaponRunModifiers.DirectDamageMultiplier).
                if (offer.Id == WeaponRunModifiers.CatalogId(WeaponBoon.PerfectSpacing))
                    EnsurePerfectSpacingFeedback();
                break;
            // impactful-weapon-boons R2: Split Arrow is a catalogued Bow WeaponBoon, so its rank is applied
            // through the same WeaponModifiers.Add gate as every other family boon. On top of that it needs a
            // run-scoped coordinator that reacts to kills. Create it once and reconfigure it (with the new rank)
            // on every pick so higher ranks fan more arrows; Configure re-subscribes idempotently.
            case "weapon_SplitArrow":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                if (!_splitArrow) _splitArrow = gameObject.AddComponent<SplitArrowCoordinator>();
                _splitArrow.Configure(_player, Hooks, WeaponModifiers.Rank(WeaponBoon.SplitArrow));
                break;
            // impactful-weapon-boons R6: MomentumStrike is a catalogued Gauntlet WeaponBoon, applied through
            // the same WeaponModifiers.Add gate. It also needs a run-scoped stack holder that reacts to
            // direct hits and to taking damage. Create it once and reconfigure it (with the new rank) on every
            // pick; Configure re-subscribes idempotently. Pass `this` (RunBoons) as the stat source so the
            // momentum stat modifier is torn down by OnDestroy's RemoveModifiersFrom(this) (R6.5).
            case "weapon_MomentumStrike":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                if (!_momentum) _momentum = gameObject.AddComponent<MomentumStacks>();
                _momentum.Configure(_player, Hooks, WeaponModifiers.Rank(WeaponBoon.MomentumStrike), this);
                break;
            // gauntlet-boon-playstyle-overhaul R6: HungryCombo is a catalogued Gauntlet WeaponBoon, applied
            // through the same WeaponModifiers.Add gate. It also needs a run-scoped tracker that reduces
            // live skill cooldowns on each basic hit. Create it once and reconfigure it (with the new rank)
            // on every pick so higher ranks cut more; Configure re-subscribes idempotently.
            case "weapon_HungryCombo":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                if (!_hungryCombo) _hungryCombo = gameObject.AddComponent<HungryComboTracker>();
                _hungryCombo.Configure(_player, Hooks, _holder, WeaponModifiers.Rank(WeaponBoon.HungryCombo));
                break;
            // gauntlet-boon-playstyle-overhaul R7: SeismicFist is a catalogued Gauntlet WeaponBoon, applied
            // through the same WeaponModifiers.Add gate. Its skill-side behavior is purely snapshot-driven
            // (WeaponRunModifiers.GauntletSteps declares Knockback with (1 + 0.3R) distance on the per-cast
            // clones), so nothing beyond the Add is needed there. For the Basic_Attack, the live basic
            // hitbox opts into the deliberate Stance_Break Knockback: when the hitbox exists we declare
            // StanceBreakEffect.Knockback on it (the per-hit pushDistance stays zeroed — the hard throw
            // comes only from the stance break). The source asset is never touched (R7.4); the hitbox is a
            // runtime instance owned by the player (R7.1/R7.2).
            case "weapon_SeismicFist":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                if (_player.CurrentHitbox && _player.CurrentHitbox.TryGetComponent(out HitboxDamage seismicHitbox))
                    seismicHitbox.SetStanceBreakEffect(StanceBreakEffect.Knockback);
                break;
            // gauntlet-boon-playstyle-overhaul R4: AsuraFist is a catalogued Gauntlet WeaponBoon, applied
            // through the same WeaponModifiers.Add gate. It also needs a run-scoped coordinator that charges
            // the Manopla's Asura meter on each basic hit. Create it once and reconfigure it (with the new
            // rank) on every pick so higher ranks charge faster; Configure re-subscribes idempotently. The
            // BreakerGauntletCombat is resolved from the player component (data-driven, no scene lookup).
            case "weapon_AsuraFist":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                if (!_asuraSurge) _asuraSurge = gameObject.AddComponent<AsuraSurge>();
                _player.TryGetComponent(out BreakerGauntletCombat breakerCombat);
                _asuraSurge.Configure(_player, Hooks, breakerCombat, WeaponModifiers.Rank(WeaponBoon.AsuraFist));
                break;
            // gauntlet-boon-playstyle-overhaul R5: GuardBreaker is a catalogued Gauntlet WeaponBoon, applied
            // through the same WeaponModifiers.Add gate. It also needs a run-scoped tracker that counts three
            // consecutive basic hits and, on the third, breaks the enemy's guard with a scaled stance hit and
            // a deliberate Knockback. Create it once and reconfigure it (with the new rank) on every pick so
            // higher ranks break harder; Configure re-subscribes idempotently.
            case "weapon_GuardBreaker":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                if (!_guardBreaker) _guardBreaker = gameObject.AddComponent<ImpactGuardTracker>();
                _guardBreaker.Configure(_player, Hooks, _holder, WeaponModifiers.Rank(WeaponBoon.GuardBreaker));
                break;
            // gauntlet-boon-playstyle-overhaul R8: KitingStep is a catalogued Bow WeaponBoon, applied through
            // the same WeaponModifiers.Add gate. The offer only surfaces while the equipped family is Bow
            // (the family gate in OfferReward), so this case runs only WHILE Bow (R8.5). It also needs a
            // run-scoped coordinator that gives the player a navmesh-routed reposition impulse when firing a
            // basic while retreating. Create it once and reconfigure it (with the new rank) on every pick so
            // higher ranks push farther; Configure re-subscribes idempotently. The CharControlScript and the
            // player's NavMeshAgent are resolved from the player component (data-driven, no scene lookup).
            case "weapon_KitingStep":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                if (WeaponModifiers.Family == RunWeaponFamily.Bow)
                {
                    if (!_kitingStep) _kitingStep = gameObject.AddComponent<KitingStepCoordinator>();
                    _player.TryGetComponent(out CharControlScript kitingControls);
                    _player.TryGetComponent(out NavMeshAgent kitingAgent);
                    _kitingStep.Configure(_player, kitingControls, kitingAgent, WeaponModifiers.Rank(WeaponBoon.KitingStep));
                }
                break;
            // gauntlet-boon-playstyle-overhaul R9: AdaptiveCadence is a catalogued Bow WeaponBoon, applied
            // through the same WeaponModifiers.Add gate. It also needs a run-scoped tracker that keeps an
            // AttackSpeedMultiplier modifier on while basic hits land at >= 6m and removes it on a closer
            // hit. Create it once and reconfigure it (with the new rank) on every pick so higher ranks
            // accelerate more; Configure re-subscribes idempotently. Pass `this` (RunBoons) as the stat
            // source so the cadence modifier is torn down by OnDestroy's RemoveModifiersFrom(this) (R9.4).
            case "weapon_AdaptiveCadence":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                if (!_adaptiveCadence) _adaptiveCadence = gameObject.AddComponent<AdaptiveCadenceTracker>();
                _adaptiveCadence.Configure(_player, Hooks, WeaponModifiers.Rank(WeaponBoon.AdaptiveCadence), this);
                break;
            // gauntlet-boon-playstyle-overhaul R11: SpacingRecoil is a catalogued Spear WeaponBoon whose
            // whole effect is snapshot-driven (WeaponRunModifiers.Plan sets the Cast_Plan.SpacingRecoil flag
            // from the rank, and ArsenalCombat reads the live rank to step the player back via the NavMesh
            // when a thrust connects). Like StanceCrusher/PikeWall it needs nothing beyond the Add gate — no
            // run-scoped coordinator, stat modifier, or hitbox configuration. Kept as an explicit case to
            // mirror the other weapon_ cases and document the snapshot-only wiring.
            case "weapon_SpacingRecoil":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                break;
            // gauntlet-boon-playstyle-overhaul R12: PikeWall is a catalogued Spear WeaponBoon whose entire
            // behavior is snapshot-driven — WeaponRunModifiers.Plan sets plan.ControlZone/ZonePush on the
            // per-cast Cast_Plan from its rank, and ArsenalCombat reads those to push caught enemies outward
            // through their own SoftGroupingService. So, like StanceCrusher/SpacingRecoil, the pick only
            // needs to raise the rank through the shared WeaponModifiers.Add gate; no run-scoped coordinator.
            case "weapon_PikeWall":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                break;
            // gauntlet-boon-playstyle-overhaul R10: RainMark is a catalogued Bow WeaponBoon, applied through
            // the same WeaponModifiers.Add gate. Its per-cast behavior (which pulses mark, how much they
            // amplify/slow) is snapshot-driven via WeaponRunModifiers.Plan's MarkOnPulse/MarkAmplify/MarkSlow
            // flags; on top of that it needs a run-scoped registry that holds the live marks, applies the
            // slow, and answers the amplifier. Create it once and reconfigure it (with the new rank's
            // amplify/slow) on every pick so higher ranks amplify more; Configure only updates the amounts.
            case "weapon_RainMark":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                if (!_rainMark) _rainMark = gameObject.AddComponent<RainMarkRegistry>();
                int rainRank = WeaponModifiers.Rank(WeaponBoon.RainMark);
                _rainMark.Configure(0.2f * rainRank, rainRank > 0 ? 0.4f : 0f);
                break;
            // gauntlet-boon-playstyle-overhaul R13: EdgeStrike is a catalogued Spear WeaponBoon, applied
            // through the same WeaponModifiers.Add gate. On top of the catalog rank it needs a run-scoped
            // registry that watches the resolved-hit channel: an edge-of-reach hit requests extra stance
            // (via ApplyHitReactionTo) and opens a vulnerability window whose AmplifierFor amplifies the
            // enemy's subsequent direct hits (consulted by DealResolvedAttackDamage through the
            // EdgeVulnerabilities accessor). Create it once and reconfigure it (with the new rank) on every
            // pick so higher ranks break harder; Configure re-subscribes idempotently.
            case "weapon_EdgeStrike":
                foreach (var definition in WeaponRunModifiers.Catalog)
                    if (definition.Id == offer.Id && !WeaponModifiers.Add(definition)) return false;
                if (!_edgeVuln) _edgeVuln = gameObject.AddComponent<EdgeVulnerabilityRegistry>();
                _edgeVuln.Configure(_player, _holder, WeaponModifiers.Rank(WeaponBoon.EdgeStrike));
                break;
            case "conductor": _player.OnHitEffects.Synergies.Add(RunSynergy.Conductor); break;
            case "detonation": _player.OnHitEffects.Synergies.Add(RunSynergy.Detonation); break;
            case "reactor": _player.OnHitEffects.Synergies.Add(RunSynergy.Reactor); break;
            case "resonance": _player.OnHitEffects.Synergies.Add(RunSynergy.Resonance); break;
            // impactful-weapon-boons R8: each pick raises the overflow rank on the element registry (repeatable).
            case "overflow": _player.OnHitEffects.EnableElementalOverflow(); break;
            case "power": AddStat(PlayerStatType.IncreasedDamagePercent, 25); break;
            case "haste": AddStat(PlayerStatType.AttackSpeedMultiplier, .25f); break;
            case "recharge": AddStat(PlayerStatType.CooldownReductionPercent, 15); break;
            case "vitality":
                AddStat(PlayerStatType.MaxHealthBonus, 100);
                _player.RefreshResourceStats(); _player.RestoreHealthToMax(); break;
            case "crit":
                AddStat(PlayerStatType.CriticalChance, 15);
                AddStat(PlayerStatType.CriticalDamageMultiplier, 0.4f);
                break;
            case "brutal": AddStat(PlayerStatType.FlatDamageBonus, 8); break;
            case "bulwark": AddStat(PlayerStatType.Armor, 40); break;
            case "swift": AddStat(PlayerStatType.MovementSpeedMultiplier, 20f, PlayerStatModifierMode.IncreasedPercent); break;
            case "ignite":
                // Each pick makes the burn hit harder; duration stays at 4s.
                _player.OnHitEffects.EnableBurn(6f, 4f);
                break;
            case "frost":
                // Heavy slow with a chance to fully freeze; picking again raises the chance.
                _player.OnHitEffects.EnableChill(0.9f, 2.5f, 0.35f);
                break;
            case "focus":
                Ability q = _weapon.abilities[0];
                q.cooldownTime *= .65f;
                q.ConfigureRunPresentation(q.DisplayName, q.ManaCost * .75f, q.Glyph);
                break;
            case "transform":
                var skill = ScriptableObject.CreateInstance<ArsenalAbility>();
                skill.ConfigureRunTransformation(_weapon.FiresArrows);
                if (_acquired.Exists(owned => owned.Id == "focus"))
                {
                    skill.cooldownTime *= .65f;
                    skill.ConfigureRunPresentation(skill.DisplayName, skill.ManaCost * .75f, skill.Glyph);
                }
                _owned.Add(skill); _weapon.abilities[0] = skill;
                break;
        }
        _acquired.Add(offer);
        _holder.RefreshLoadout();
        IsChoosing = false;
        if (_agent && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        if (TryGetComponent(out CharControlScript controls)) controls.RequireAttackRelease();
        _choices.Clear();
        RewardChosen?.Invoke();
        return true;
    }

    // impactful-weapon-boons R4.3: add the cosmetic Perfect Spacing streak feedback once and bind it to
    // the run-scoped player/holder (no scene lookup). Repeated picks (higher ranks) reuse the same
    // component. The feedback is purely presentational and never affects the damage term.
    private void EnsurePerfectSpacingFeedback()
    {
        if (!_player) return;
        if (!_player.TryGetComponent(out PerfectSpacingFeedback feedback))
            feedback = _player.gameObject.AddComponent<PerfectSpacingFeedback>();
        feedback.Configure(_player, _holder);
    }

    private void AddStat(PlayerStatType stat, float value, PlayerStatModifierMode mode = PlayerStatModifierMode.Flat) =>
        _player.Stats.AddModifier(new PlayerStatModifier(stat, mode, value), this);

    // Skill transforms and one-off utility boons are single-use; stat and elemental boons repeat and stack.
    private static bool IsSingleUse(string id) => id == "transform" || id == "focus" || id == "recharge" || id == "vitality";

    // A Rewrite replaces the slot-1 (Q) ability via a runtime copy, so it needs a live ability to rewrite (R9.5).
    private bool HasRewriteTarget() => _weapon && _weapon.abilities != null && _weapon.abilities.Length > 0 && _weapon.abilities[0];

    private void OnDestroy()
    {
        // Lifecycle unbinding first: remove stat modifiers and detach from the holder / escalation
        // driver so nothing can re-populate run-scoped state while (or after) it is being cleared.
        if (_player) _player.Stats.RemoveModifiersFrom(this);
        if (_holder) _holder.BindRun(null);
        RewardChosen -= EvaluateSystemBreak;
        // R11.4: clear every piece of run-scoped state together, as one atomic operation.
        ClearRunState();
        // Runtime-copy cleanup last: the atomic clear no longer depends on these assets.
        foreach (ScriptableObject asset in _owned) if (asset) Destroy(asset);
    }

    // R11.4: atomically clear all run-scoped state as a single operation, so the element registry
    // (PlayerOnHitEffects), the cascade ranks (RunSynergyEffects, cleared via the element registry's
    // Clear), the combat-event bus (HookBus), and the escalation feedback (SystemBreakState) are all
    // reset together rather than independently or at different times. A previous run's state can never
    // leak into a later run.
    private void ClearRunState()
    {
        // PlayerOnHitEffects.Clear() cascades to RunSynergyEffects.Clear() (it calls _synergies.Clear()),
        // so this single call empties both the element registry and the cascade ranks.
        if (_player && _player.TryGetComponent(out PlayerOnHitEffects effects)) effects.Clear();
        // Drop every subscriber so a previous run's boons never fire in a later run (R8.9).
        Hooks?.Clear();
        // Reset the escalation tier (R10.5).
        _systemBreak?.Reset();
    }
}
