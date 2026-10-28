using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Display metadata, independent of reward rules.</summary>
public sealed class RunModifierPresentation
{
    public string Category = "ATRIBUTO", Scope = "PERSONAGEM", Symbol = "+", Description;
    public Color Accent = new Color(.55f,.78f,.69f);
    public int MaxRank;
    /// <summary>True when the offer belongs to the Rewrite category (ability-replacing modifiers such as <c>transform</c>).</summary>
    public bool IsRewrite;
    /// <summary>Classifies ability-replacing ids as Rewrite. Currently only <c>transform</c> qualifies.</summary>
    public static bool IsRewriteId(string id) => id == "transform";
    private static readonly Regex Numbers = new Regex(@"([+−-]?\d+(?:[.,]\d+)?(?:%|m|s)?)");
    public static string Emphasize(string text) => Numbers.Replace(text, "<b><color=#F2DCA6>$1</color></b>");
    public static int Count(RunBoons run, string id)
    {
        if (id == BoonReworkCatalog.ViolentFallId) return run.WeaponModifiers?.HasViolentFall == true ? 1 : 0;
        if (BoonExpansionCatalog.TryDefinition(id, out _)) return run.WeaponModifiers?.Expansion.Has(id) == true ? 1 : 0;
        if (run.Protocols != null && ProtocolInventory.TryKind(id, out var protocol)) return run.Protocols.Rank(protocol);
        if (run.WeaponModifiers != null && run.WeaponModifiers.Family == RunWeaponFamily.Gauntlet)
        {
            if (GauntletContentAuthoring.TryMutation(id, out var mutation))
                return run.WeaponModifiers.GauntletMutations.TryGet(GauntletMutationSelection.SlotOf(mutation), out var selected)
                    && selected == mutation ? 1 : 0;
            if (WeaponRunModifiers.TryDefinition(id, out var definition) && definition.Family == run.WeaponModifiers.Family)
                return definition.Kind == WeaponBoon.ShockRing
                    ? (run.WeaponModifiers.Rank(definition.Kind) > 0 ? 1 : 0) : run.WeaponModifiers.Rank(definition.Kind);
        }
        int count = 0; foreach (var offer in run.Acquired) if (offer.Id == id) count++; return count;
    }
    /// <summary>True when <paramref name="id"/> appears in the current offer set (the choices being presented this reward).</summary>
    public static bool Offered(RunBoons run, string id)
    { foreach (var offer in run.Choices) if (offer.Id == id) return true; return false; }
    /// <summary>True when the partner modifier is already owned, or when this modifier and the partner co-appear in the current offer set.</summary>
    private static bool Combined(RunBoons run, string selfId, string partnerId)
    { return Count(run, partnerId) > 0 || (Offered(run, selfId) && Offered(run, partnerId)); }
    public static RunModifierPresentation For(RunBoons.Offer offer)
    {
        var view = new RunModifierPresentation { Description = offer.Description };
        if (BoonExpansionCatalog.TryDefinition(offer.Id, out var extension))
        {
            view.Category = extension.Family == BoonFamily.Resonance ? "RESONANCE / DUO" : extension.Family.ToString().ToUpperInvariant();
            view.Scope = extension.Slots.ToString(); view.Symbol = extension.Family == BoonFamily.Resonance ? "<>" : view.Scope;
            view.MaxRank = extension.MaxRank;
            view.Accent = extension.Family == BoonFamily.Resonance ? new Color(1f, .77f, .3f) : new Color(.65f, .45f, 1f);
            return view;
        }
        if (offer.Id == BoonReworkCatalog.ViolentFallId)
        {
            view.Category = "AUGMENT"; view.Scope = view.Symbol = "Q"; view.MaxRank = 1;
            view.Accent = new Color(.65f,.4f,1f); view.Description = EclipseBoonCatalog.FallDescription;
            return view;
        }
        if (offer.Id == RunBoonOfferCatalog.ExhaustedId)
        {
            view.Category = "ALTERNATIVA"; view.Scope = "PROGRESSÃO"; view.Symbol = ">";
            return view;
        }
        if (ProtocolInventory.TryKind(offer.Id, out var protocol))
        {
            view.Category = "PROTOCOL"; view.Scope = "COMBATE"; view.MaxRank = 3;
            view.Symbol = protocol == ProtocolKind.Ignite ? "F" : protocol == ProtocolKind.Frost ? "G" :
                protocol == ProtocolKind.Conductor ? ">>" : protocol == ProtocolKind.Detonation ? "*" :
                protocol == ProtocolKind.Reactor ? "F+" : protocol == ProtocolKind.Resonance ? "FG" : "FG+";
            view.Accent = protocol == ProtocolKind.Ignite ? new Color(1f,.48f,.25f) :
                protocol == ProtocolKind.Frost ? new Color(.35f,.82f,1f) : new Color(.77f,.58f,1f);
            return view;
        }
        if (GauntletContentAuthoring.TryMutation(offer.Id, out var mutation))
        {
            view.Category = "MUTATION";
            view.Scope = view.Symbol = GauntletMutationSelection.SlotOf(mutation).ToString();
            view.MaxRank = 1;
            view.Accent = new Color(.77f, .58f, 1f);
            return view;
        }
        foreach (var definition in WeaponRunModifiers.Catalog)
        {
            if (definition.Id != offer.Id) continue;
            view.Category = definition.Family == RunWeaponFamily.Bow ? "ARCO" : definition.Family == RunWeaponFamily.Spear ? "LANÇA" : "MANOPLAS";
            if (definition.Family == RunWeaponFamily.Gauntlet && GauntletContentAuthoring.IsSelectedTechnique(definition.Kind))
                view.Category = "TECHNIQUE";
            if (RunBoonOfferCatalog.IsTechnique(definition.Kind)) view.Category = "TECHNIQUE";
            if (RunBoonOfferCatalog.IsMutation(definition.Kind)) view.Category = "MUTATION";
            view.Accent = definition.Family == RunWeaponFamily.Bow ? new Color(.34f,.8f,1f) : definition.Family == RunWeaponFamily.Spear ? new Color(1f,.53f,.4f) : new Color(1f,.73f,.32f);
            view.Description = definition.Description; view.MaxRank = definition.MaxRank;
            if (definition.Kind == WeaponBoon.ShockRing)
            {
                view.Category = "MUTATION"; view.MaxRank = 1;
                view.Description = "Epicentro legado (ShockRing): mantém o rank e o raio originais. Não acumula com outra seleção de Epicentro.";
            }
            view.Scope = ScopeFor(definition.Kind);
            view.Symbol = view.Scope.Length == 1 ? view.Scope : definition.Family == RunWeaponFamily.Bow ? ">>" : definition.Family == RunWeaponFamily.Spear ? "/" : "III";
            return view;
        }
        switch (offer.Id)
        {
            case "transform": view.Category = "TRANSFORMAÇÃO"; view.Scope = view.Symbol = "Q"; view.MaxRank = 1; view.Accent = new Color(.77f,.58f,1f); view.IsRewrite = true; break;
            case "focus": view.Category = "HABILIDADE"; view.Scope = view.Symbol = "Q"; view.MaxRank = 1; break;
            case "ignite": view.Category = "ELEMENTO"; view.Scope = "ACERTOS"; view.Symbol = "F"; view.Accent = new Color(1f,.48f,.25f); view.Description = "Acertos aplicam queimadura por 4s. Cada cópia acrescenta 6 de dano por segundo; novos acertos renovam a duração."; break;
            case "frost": view.Category = "ELEMENTO"; view.Scope = "ACERTOS"; view.Symbol = "G"; view.Accent = new Color(.35f,.82f,1f); view.Description = "Acertos têm +35 pontos percentuais de chance de congelar por cópia, até 100%. Duração: 2,5s."; break;
            case "conductor": case "detonation": case "reactor": case "resonance": view.Category = "SINERGIA"; view.Scope = "ACERTOS"; view.Symbol = offer.Id == "conductor" ? ">>" : offer.Id == "detonation" ? "*" : offer.Id == "reactor" ? "F+" : "FG"; view.Accent = new Color(.77f,.58f,1f); break;
            // impactful-weapon-boons R8: cross-family Elemental Overflow card (category ELEMENTO, scope ACERTOS).
            case "overflow": view.Category = "ELEMENTO"; view.Scope = "ACERTOS"; view.Symbol = "FG+"; view.Accent = new Color(1f,.62f,.4f); break;
            case "vitality": view.Scope = "VIDA"; view.MaxRank = 1; break;
            case "recharge": view.Scope = "RECARGAS"; view.MaxRank = 1; break;
            case "haste": view.Scope = "ATAQUE BÁSICO"; view.Symbol = ">>"; break;
            case "swift": view.Scope = "MOVIMENTO"; view.Symbol = ">>"; break;
            case "crit": view.Scope = "CRÍTICOS"; view.Symbol = "!"; break;
            case "bulwark": view.Scope = "ARMADURA"; view.Symbol = "III"; break;
            case "power": case "brutal": view.Scope = "DANO"; break;
        }
        view.Description = view.Description.Replace("TRANSFORMA Q: ", "");
        return view;
    }
    public static string Hint(RunBoons run, string id)
    {
        if (id == BoonReworkCatalog.ViolentFallId) return EclipseBoonCatalog.FallDescription;
        if (BoonExpansionCatalog.TryDefinition(id, out var expansion)) return expansion.Family == BoonFamily.Resonance
            ? "DUO · pré-requisitos presentes. Máximo de dois Duos por run."
            : "Forma compatível · até dois Augments por habilidade. Consulte o alvo e as condições no cartão.";
        if (id == RunBoonOfferCatalog.ExhaustedId) return "SEM AQUISIÇÃO · libera a próxima etapa da rota.";
        if (id == WeaponRunModifiers.CatalogId(WeaponBoon.ShockRing) && run.WeaponModifiers != null)
            return "EPICENTRO LEGADO · rank " + run.WeaponModifiers.Rank(WeaponBoon.ShockRing)
                + ": +" + (20 * run.WeaponModifiers.Rank(WeaponBoon.ShockRing)) + "% de raio preservado; seleção única de E.";
        if (GauntletContentAuthoring.TryMutation(id, out var mutation))
            return GauntletContentAuthoring.MutationDescription(mutation);
        bool fire = Count(run,"ignite") > 0, frost = Count(run,"frost") > 0;
        if (id == "reactor") return fire ? "COMBINAÇÃO ATIVA · queimadura escala com o golpe." : "PRECISA DE FOGO · obtenha Lâmina incandescente.";
        if (id == "resonance") return (fire || frost) && (Count(run,"conductor") > 0 || Count(run,"detonation") > 0) ? "COMBINAÇÃO ATIVA · prepare os alvos com elementos." : "COMBINE · fogo/gelo + Bobina ou Reator de sucata.";
        if (id == "conductor") return fire || frost ? "COMBINAÇÃO ATIVA · elementos viajam nos saltos." : "COMBINE · fogo e gelo se espalham nas descargas.";
        if (id == "weapon_Affliction") return fire || frost ? "COMBINAÇÃO ATIVA · elementos amplificam os golpes." : "COMBINE · aplique fogo ou gelo antes de atacar.";
        if (id == "weapon_Orbit") return Combined(run,"weapon_Orbit","weapon_TripleMoon") ? "COMBINAÇÃO ATIVA · luas avançam a cada pulso." : "COMBINE · Órbita das luas acrescenta pulsos ao W.";
        if (id == "haste") return Combined(run,"haste","weapon_ComboNova") ? "COMBINAÇÃO ATIVA · Ímpeto acelera a Nova de combo." : "COMBINE · Ímpeto acelera a cadência da Nova de combo.";
        if (id == "weapon_ComboNova") return Combined(run,"weapon_ComboNova","haste") ? "COMBINAÇÃO ATIVA · Ímpeto acelera a Nova de combo." : "COMBINE · Ímpeto acelera a cadência da Nova de combo.";
        if (id == "weapon_RocketAdvance" && Count(run,"transform") > 0
            && (run.WeaponModifiers == null || !run.WeaponModifiers.GauntletMutations.TryGet(GauntletActionSlot.Q, out _)))
            return "ATENÇÃO · este avanço não se aplica ao Q transformado.";
        // impactful-weapon-boons R9.3: combination hints for the new boons.
        if (id == "weapon_ChargedShot") return Combined(run,"weapon_ChargedShot","weapon_Piercing") || Combined(run,"weapon_ChargedShot","weapon_Sniper") ? "COMBINAÇÃO ATIVA · tiro carregado atravessa a fila." : "COMBINE · perfuração faz o tiro carregado varar inimigos.";
        if (id == "weapon_SplitArrow") return Combined(run,"weapon_SplitArrow","weapon_TwinShot") ? "COMBINAÇÃO ATIVA · mais flechas, mais fragmentos." : "COMBINE · Tiro duplo multiplica os fragmentos ao abater.";
        if (id == "weapon_MomentumStrike") return Combined(run,"weapon_MomentumStrike","bulwark") || Combined(run,"weapon_MomentumStrike","vitality") ? "COMBINAÇÃO ATIVA · sobreviver mantém as pilhas." : "COMBINE · defesa evita perder as pilhas ao levar dano.";
        if (id == "overflow") return fire || frost ? "COMBINAÇÃO ATIVA · burst sem consumir Burn/Chill." : "PRECISA DE ELEMENTO · Ignite ou Frost.";
        if (id == "weapon_TwinShot" || id == "weapon_WideVolley") return "COMBINE · perfuração e ricochete ampliam a cobertura.";
        // gauntlet-boon-playstyle-overhaul R15.3: combination hints for the ten new boons. Each surfaces the
        // partner that turns the boon into a loop — Asura basics feed the Asura finishers, hungry basics bring
        // the skills back, the marking rain pairs with the cursor-guided rain, and the pike wall holds the
        // space the moon orbit controls.
        if (id == "weapon_AsuraFist") return Combined(run,"weapon_AsuraFist","weapon_AsuraEcho") || Combined(run,"weapon_AsuraFist","weapon_AsuraReserve") ? "COMBINAÇÃO ATIVA · básicos enchem o Asura mais rápido." : "COMBINE · básicos carregam as skills de Asura.";
        if (id == "weapon_HungryCombo") return Combined(run,"weapon_HungryCombo","weapon_FlurryEcho") ? "COMBINAÇÃO ATIVA · básicos trazem as skills de volta." : "COMBINE · básicos reduzem a recarga das skills.";
        if (id == "weapon_RainMark") return Combined(run,"weapon_RainMark","weapon_GuidedRain") ? "COMBINAÇÃO ATIVA · a nuvem persegue e marca." : "COMBINE · guie a chuva para manter os alvos marcados.";
        if (id == "weapon_PikeWall") return Combined(run,"weapon_PikeWall","weapon_Orbit") ? "COMBINAÇÃO ATIVA · a zona segura o espaço à frente." : "COMBINE · Órbita das luas controla a zona de hastes.";
        if (id == "weapon_GuardBreaker") return Combined(run,"weapon_GuardBreaker","weapon_SeismicFist") ? "COMBINAÇÃO ATIVA · a quebra arremessa com mais força." : "COMBINE · Punho sísmico arremessa ao quebrar a guarda.";
        if (id == "weapon_SeismicFist") return Combined(run,"weapon_SeismicFist","weapon_GuardBreaker") ? "COMBINAÇÃO ATIVA · a quebra arremessa com mais força." : "COMBINE · Guarda partida rompe a postura para arremessar.";
        if (id == "weapon_AdaptiveCadence") return Combined(run,"weapon_AdaptiveCadence","weapon_KitingStep") ? "COMBINAÇÃO ATIVA · recuar mantém a cadência acelerada." : "COMBINE · Disparo em recuo mantém a distância da cadência.";
        if (id == "weapon_KitingStep") return Combined(run,"weapon_KitingStep","weapon_AdaptiveCadence") ? "COMBINAÇÃO ATIVA · recuar mantém a cadência acelerada." : "COMBINE · Cadência adaptativa recompensa manter a distância.";
        if (id == "weapon_EdgeStrike") return Combined(run,"weapon_EdgeStrike","weapon_SpacingRecoil") ? "COMBINAÇÃO ATIVA · o recuo mantém você na borda do alcance." : "COMBINE · Recuo controlado assenta você no ponto cego.";
        if (id == "weapon_SpacingRecoil") return Combined(run,"weapon_SpacingRecoil","weapon_EdgeStrike") ? "COMBINAÇÃO ATIVA · o recuo mantém você na borda do alcance." : "COMBINE · Ponto cego premia acertar na ponta do alcance.";
        return Count(run,id) > 0 ? "EVOLUÇÃO · reforça um modificador da sua build." : "NOVA AQUISIÇÃO · permanece até o fim desta run.";
    }
    private static string ScopeFor(WeaponBoon kind)
    {
        switch (kind)
        {
            case WeaponBoon.RapidBurst: case WeaponBoon.RocketAdvance: return "Q";
            case WeaponBoon.HeavyBolt: case WeaponBoon.TripleMoon: case WeaponBoon.Orbit: case WeaponBoon.FlurryEcho: case WeaponBoon.MoonShard: return "W";
            case WeaponBoon.WideVolley: case WeaponBoon.Trident: case WeaponBoon.ShockRing: return "E";
            case WeaponBoon.LongRain: case WeaponBoon.GuidedRain: case WeaponBoon.DragonWave: case WeaponBoon.AsuraEcho: case WeaponBoon.AsuraReserve: case WeaponBoon.ReturnWave: return "R";
            case WeaponBoon.Momentum: return "Q / W / E";
            case WeaponBoon.ComboNova: case WeaponBoon.MomentumStrike: return "ATAQUE BÁSICO";
            case WeaponBoon.EchoThrust: case WeaponBoon.PhantomSpear: case WeaponBoon.ChainThrust:
            case WeaponBoon.PerfectSpacing: case WeaponBoon.ImpalingLine: return "ESTOCADAS";
            case WeaponBoon.StanceCrusher: case WeaponBoon.Shockwave: return "HABILIDADES";
            case WeaponBoon.TwinShot: case WeaponBoon.Piercing: case WeaponBoon.Ricochet: case WeaponBoon.Homing:
            case WeaponBoon.SplitArrow: case WeaponBoon.ChargedShot: return "FLECHAS";
            // gauntlet-boon-playstyle-overhaul R15.2/R15.4: scope the ten new boons by the playstyle they
            // change. Gauntlet basic-driven boons read on the ATAQUE BÁSICO; SeismicFist touches both the
            // basic and the skills; the Bow repositioning/cadence boons read as KITING; RainMark lives on
            // the R; the Spear boons read on their estocadas (thrust) or on the W sweep.
            case WeaponBoon.AsuraFist: case WeaponBoon.GuardBreaker: case WeaponBoon.HungryCombo: return "ATAQUE BÁSICO";
            case WeaponBoon.SeismicFist: return "BÁSICOS + SKILLS";
            case WeaponBoon.KitingStep: case WeaponBoon.AdaptiveCadence: return "KITING";
            case WeaponBoon.RainMark: return "R";
            case WeaponBoon.SpacingRecoil: case WeaponBoon.EdgeStrike: return "ESTOCADAS";
            case WeaponBoon.PikeWall: return "W";
            default: return "BÁSICOS + SKILLS";
        }
    }
}
