using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>A Nexus terminal upgrade: bought once with Fragmentos, active in every later run.</summary>
public sealed class MetaUpgrade
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int Cost { get; }
    public MetaUpgrade(string id, string name, string description, int cost) { Id = id; Name = name; Description = description; Cost = cost; }
}

/// <summary>
/// Fase 1b terminal catalog. Economy approved 09/11/2026 and revised after the 10/11/2026 playtest (Fragmentos came
/// too fast; a free extra boon was vetoed, an extra choice is fine). IDs are persisted — never rename or reuse.
/// </summary>
public static class MetaUpgrades
{
    public const string Vitality = "vida_reforcada";
    public const string CleanRoom = "sala_limpa";
    public const string Reroll = "reroll";
    public const string Repair = "reparo_reforcado";
    public const string FourthOption = "quarta_opcao";
    public const string ExtraReroll = "rolagem_extra";
    /// <summary>Retired 10/11/2026 (granted a free boon). Kept only so old saves are refunded; never offered.</summary>
    public const string RetiredInitialOffer = "oferta_inicial";

    public const float VitalityMaxHealthBonus = 200f;   // +200 over the 500 base (threat pass 3, 11/11/2026); the only numeric upgrade
    public const int CleanRoomFragments = 2;
    public const int RerollsPerUpgrade = 1;             // "Reroll" and "Rolagem extra" each add one per run
    public const float RepairFractionBonus = .25f;
    public const int OfferCountWithFourthOption = 4;

    public static readonly IReadOnlyList<MetaUpgrade> All = new[]
    {
        new MetaUpgrade(Vitality, "Vida reforçada", "+200 de vida máxima em todas as runs.", 20),
        new MetaUpgrade(Reroll, "Reroll", "1 nova rolagem de oferta de boon por run.", 40),
        new MetaUpgrade(CleanRoom, "Sala limpa", "+2 Fragmentos por sala vencida sem levar dano.", 50),
        new MetaUpgrade(Repair, "Reparo reforçado", "A sala de reparo cura +25% da vida máxima.", 60),
        new MetaUpgrade(ExtraReroll, "Rolagem extra", "+1 nova rolagem de oferta de boon por run.", 75),
        new MetaUpgrade(FourthOption, "Quarta opção", "Todas as ofertas de boon mostram 4 opções.", 90),
    };

    /// <summary>Retired upgrades and the Fragmentos refunded when an old save still owns them.</summary>
    public static readonly IReadOnlyDictionary<string, int> RetiredRefunds = new Dictionary<string, int> { { RetiredInitialOffer, 65 } };

    /// <summary>Rerolls per run granted by the owned upgrades.</summary>
    public static int RerollsFor(MetaProgressProfile profile) =>
        profile == null ? 0 : (profile.HasUpgrade(Reroll) ? RerollsPerUpgrade : 0) + (profile.HasUpgrade(ExtraReroll) ? RerollsPerUpgrade : 0);

    public static bool TryGet(string id, out MetaUpgrade upgrade)
    { upgrade = All.FirstOrDefault(u => u.Id == id); return upgrade != null; }

    /// <summary>The cheapest upgrade not owned yet, or null when everything is bought.</summary>
    public static MetaUpgrade NextGoal(MetaProgressProfile profile) =>
        All.Where(u => profile == null || !profile.HasUpgrade(u.Id)).OrderBy(u => u.Cost).FirstOrDefault();
}

/// <summary>Fragmentos de Dados gains besides the physical drops (CoinDrop rates unchanged).</summary>
public static class FragmentEconomy
{
    // Revised 10/11/2026: one victory banked ~51 and bought two upgrades. Target: 1st purchase in 1–2 runs, the whole
    // catalog (335) in ~10–15 runs.
    public const int CombatRoomCleared = 1;  // every Combat/Elite room won
    public const int EliteKilled = 2;        // guaranteed per Elite-rank enemy
    public const int BossVictory = 10;       // at the universe boss death
}

/// <summary>Fase 1b allies. Identity, lore and final lines belong to the developer (placeholders marked TODO-TEXTO).</summary>
public static class MetaAllies
{
    public const string Unknown = "desconhecido";
    public const string UnknownDisplayName = "Desconhecido";
    public static readonly string[] UnknownRescueLines =
    {
        "TODO-TEXTO: …Você consegue me ver? Achei que ninguém mais passava por aqui.",
        "TODO-TEXTO: Toma, segura isso. É o pouco que eu consigo consertar daqui.",
        "TODO-TEXTO: Obrigado. Vou te esperar no Nexus.",
    };
    /// <summary>Fraction of max health the ally restores on rescue (immediate payoff inside the run).</summary>
    public const float RescueHealFraction = .30f;
    /// <summary>Key in <see cref="MetaProgressProfile.seenDialogues"/>: once seen, the terminal opens without the dialogue.</summary>
    public const string UnknownNexusDialogueKey = "desconhecido.nexus.1";

    /// <summary>Short lines the trapped ally shouts during the fight (placeholder text; the developer owns the voice).</summary>
    public static readonly RescueBarks UnknownBarks = new RescueBarks
    {
        Arrival = new[] { "Ei! Aqui! Não vai embora!", "Você… é de verdade? Me tira daqui!" },
        Fighting = new[] { "Eles estão me prendendo aqui!", "Cuidado, tem mais vindo!", "Isso! Continua!", "Não deixa eles chegarem perto!", "Só mais um pouco…" },
        PlayerHurt = new[] { "Aguenta firme!", "Não cai agora, por favor!" },
        Unlocked = new[] { "A trava caiu! Rápido, me solta!" },
        Waiting = new[] { "Ei… não me deixa aqui.", "Por favor, me solta!" },
    };

    public static readonly string[] UnknownNexusLines =
    {
        "TODO-TEXTO: Consegui religar este terminal. Ele converte Fragmentos em melhorias.",
        "TODO-TEXTO: Traga mais Fragmentos e eu faço o resto.",
    };
}

/// <summary>What a trapped ally says during the rescue room, by moment.</summary>
public sealed class RescueBarks
{
    public string[] Arrival, Fighting, PlayerHurt, Unlocked, Waiting;
}

/// <summary>Text for the run-results progress section (pure, testable).</summary>
public static class MetaProgressSummary
{
    public static string Describe(MetaProgressProfile profile, string sessionId)
    {
        if (profile == null) return null;
        var record = profile.FindRun(sessionId);
        var lines = new List<string> { "PROGRESSO" };
        lines.Add("Fragmentos ganhos nesta run: " + (record != null ? record.fragmentsEarned.ToString(CultureInfo.InvariantCulture) : "—") +
            "   ·   Saldo: " + profile.fragments.ToString(CultureInfo.InvariantCulture));
        var next = MetaUpgrades.NextGoal(profile);
        if (next == null) lines.Add("Todas as melhorias do terminal foram compradas.");
        else if (profile.fragments >= next.Cost) lines.Add("Próxima melhoria: " + next.Name + " (" + next.Cost + ") — já dá para comprar no terminal.");
        else lines.Add("Próxima melhoria: " + next.Name + " — faltam " + (next.Cost - profile.fragments).ToString(CultureInfo.InvariantCulture) + " Fragmentos.");
        if (record != null && !string.IsNullOrEmpty(record.allyRescued)) lines.Add("Novo aliado resgatado.");
        return string.Join("\n", lines);
    }
}
