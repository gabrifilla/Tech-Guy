using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Display metadata, independent of reward rules.</summary>
public sealed class RunModifierPresentation
{
    public string Category = "ATRIBUTO", Scope = "PERSONAGEM", Symbol = "+", Description;
    public Color Accent = new Color(.55f,.78f,.69f);
    public int MaxRank;
    private static readonly Regex Numbers = new Regex(@"([+−-]?\d+(?:[.,]\d+)?(?:%|m|s)?)");
    public static string Emphasize(string text) => Numbers.Replace(text, "<b><color=#F2DCA6>$1</color></b>");
    public static int Count(RunBoons run, string id)
    { int count = 0; foreach (var offer in run.Acquired) if (offer.Id == id) count++; return count; }
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
            case "transform": view.Category = "TRANSFORMAÇÃO"; view.Scope = view.Symbol = "Q"; view.MaxRank = 1; view.Accent = new Color(.77f,.58f,1f); break;
            case "focus": view.Category = "HABILIDADE"; view.Scope = view.Symbol = "Q"; view.MaxRank = 1; break;
            case "ignite": view.Category = "ELEMENTO"; view.Scope = "ACERTOS"; view.Symbol = "F"; view.Accent = new Color(1f,.48f,.25f); view.Description = "Acertos aplicam queimadura por 4s. Cada cópia acrescenta 6 de dano por segundo; novos acertos renovam a duração."; break;
            case "frost": view.Category = "ELEMENTO"; view.Scope = "ACERTOS"; view.Symbol = "G"; view.Accent = new Color(.35f,.82f,1f); view.Description = "Acertos têm +35 pontos percentuais de chance de congelar por cópia, até 100%. Duração: 2,5s."; break;
            case "conductor": case "detonation": case "reactor": case "resonance": view.Category = "SINERGIA"; view.Scope = "ACERTOS"; view.Symbol = offer.Id == "conductor" ? ">>" : offer.Id == "detonation" ? "*" : offer.Id == "reactor" ? "F+" : "FG"; view.Accent = new Color(.77f,.58f,1f); break;
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
        if (id == "weapon_Orbit") return Count(run,"weapon_TripleMoon") > 0 ? "COMBINAÇÃO ATIVA · luas avançam a cada pulso." : "COMBINE · Órbita das luas acrescenta pulsos ao W.";
        if (id == "weapon_RocketAdvance" && Count(run,"transform") > 0) return "ATENÇÃO · este avanço não se aplica ao Q transformado.";
        if (id == "weapon_TwinShot" || id == "weapon_WideVolley") return "COMBINE · perfuração e ricochete ampliam a cobertura.";
        return Count(run,id) > 0 ? "EVOLUÇÃO · reforça um modificador da sua build." : "NOVA AQUISIÇÃO · permanece até o fim desta run.";
    }
    private static string ScopeFor(WeaponBoon kind)
    {
        switch (kind)
        {
            case WeaponBoon.RapidBurst: case WeaponBoon.RocketAdvance: return "Q";
            case WeaponBoon.HeavyBolt: case WeaponBoon.TripleMoon: case WeaponBoon.Orbit: case WeaponBoon.FlurryEcho: return "W";
            case WeaponBoon.WideVolley: case WeaponBoon.Trident: case WeaponBoon.ShockRing: return "E";
            case WeaponBoon.LongRain: case WeaponBoon.GuidedRain: case WeaponBoon.DragonWave: case WeaponBoon.AsuraEcho: case WeaponBoon.AsuraReserve: return "R";
            case WeaponBoon.Momentum: return "Q / W / E";
            case WeaponBoon.ComboNova: return "ATAQUE BÁSICO";
            case WeaponBoon.EchoThrust: return "ESTOCADAS";
            case WeaponBoon.StanceCrusher: return "HABILIDADES";
            case WeaponBoon.TwinShot: case WeaponBoon.Piercing: case WeaponBoon.Ricochet: case WeaponBoon.Homing: return "FLECHAS";
            default: return "BÁSICOS + SKILLS";
        }
    }
}
