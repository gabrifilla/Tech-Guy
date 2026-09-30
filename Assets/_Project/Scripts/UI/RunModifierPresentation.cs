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
    { int count = 0; foreach (var offer in run.Acquired) if (offer.Id == id) count++; return count; }
    /// <summary>True when <paramref name="id"/> appears in the current offer set (the choices being presented this reward).</summary>
    public static bool Offered(RunBoons run, string id)
    { foreach (var offer in run.Choices) if (offer.Id == id) return true; return false; }
    /// <summary>True when the partner modifier is already owned, or when this modifier and the partner co-appear in the current offer set.</summary>
    private static bool Combined(RunBoons run, string selfId, string partnerId)
    { return Count(run, partnerId) > 0 || (Offered(run, selfId) && Offered(run, partnerId)); }
    public static RunModifierPresentation For(RunBoons.Offer offer)
    {
        var view = new RunModifierPresentation { Description = offer.Description };
        foreach (var definition in WeaponRunModifiers.Catalog)
        {
            if (definition.Id != offer.Id) continue;
            view.Category = definition.Family == RunWeaponFamily.Bow ? "ARCO" : definition.Family == RunWeaponFamily.Spear ? "LANÇA" : "MANOPLAS";
            view.Accent = definition.Family == RunWeaponFamily.Bow ? new Color(.34f,.8f,1f) : definition.Family == RunWeaponFamily.Spear ? new Color(1f,.53f,.4f) : new Color(1f,.73f,.32f);
            view.Description = definition.Description; view.MaxRank = definition.MaxRank;
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
        bool fire = Count(run,"ignite") > 0, frost = Count(run,"frost") > 0;
        if (id == "reactor") return fire ? "COMBINAÇÃO ATIVA · queimadura escala com o golpe." : "PRECISA DE FOGO · obtenha Lâmina incandescente.";
        if (id == "resonance") return (fire || frost) && (Count(run,"conductor") > 0 || Count(run,"detonation") > 0) ? "COMBINAÇÃO ATIVA · prepare os alvos com elementos." : "COMBINE · fogo/gelo + Bobina ou Reator de sucata.";
        if (id == "conductor") return fire || frost ? "COMBINAÇÃO ATIVA · elementos viajam nos saltos." : "COMBINE · fogo e gelo se espalham nas descargas.";
        if (id == "weapon_Affliction") return fire || frost ? "COMBINAÇÃO ATIVA · elementos amplificam os golpes." : "COMBINE · aplique fogo ou gelo antes de atacar.";
        if (id == "weapon_Orbit") return Combined(run,"weapon_Orbit","weapon_TripleMoon") ? "COMBINAÇÃO ATIVA · luas avançam a cada pulso." : "COMBINE · Órbita das luas acrescenta pulsos ao W.";
        if (id == "haste") return Combined(run,"haste","weapon_ComboNova") ? "COMBINAÇÃO ATIVA · Ímpeto acelera a Nova de combo." : "COMBINE · Ímpeto acelera a cadência da Nova de combo.";
        if (id == "weapon_ComboNova") return Combined(run,"weapon_ComboNova","haste") ? "COMBINAÇÃO ATIVA · Ímpeto acelera a Nova de combo." : "COMBINE · Ímpeto acelera a cadência da Nova de combo.";
        if (id == "weapon_RocketAdvance" && Count(run,"transform") > 0) return "ATENÇÃO · este avanço não se aplica ao Q transformado.";
        // impactful-weapon-boons R9.3: combination hints for the new boons.
        if (id == "weapon_ChargedShot") return Combined(run,"weapon_ChargedShot","weapon_Piercing") || Combined(run,"weapon_ChargedShot","weapon_Sniper") ? "COMBINAÇÃO ATIVA · tiro carregado atravessa a fila." : "COMBINE · perfuração faz o tiro carregado varar inimigos.";
        if (id == "weapon_SplitArrow") return Combined(run,"weapon_SplitArrow","weapon_TwinShot") ? "COMBINAÇÃO ATIVA · mais flechas, mais fragmentos." : "COMBINE · Tiro duplo multiplica os fragmentos ao abater.";
        if (id == "weapon_MomentumStrike") return Combined(run,"weapon_MomentumStrike","bulwark") || Combined(run,"weapon_MomentumStrike","vitality") ? "COMBINAÇÃO ATIVA · sobreviver mantém as pilhas." : "COMBINE · defesa evita perder as pilhas ao levar dano.";
        if (id == "overflow") return fire || frost ? "COMBINAÇÃO ATIVA · consome fogo/gelo para um burst." : "COMBINE · aplique fogo ou gelo antes de acertar.";
        if (id == "weapon_TwinShot" || id == "weapon_WideVolley") return "COMBINE · perfuração e ricochete ampliam a cobertura.";
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
            default: return "BÁSICOS + SKILLS";
        }
    }
}
