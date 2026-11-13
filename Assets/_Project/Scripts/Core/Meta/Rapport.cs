using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Fase 1c (decided 12/11/2026): rapport is built by handing rare gifts, found during runs, to the Nexus NPCs
/// (the Hades nectar model) — not by usage. Levels are derived from the gifts given, so nothing here is stored.
/// IDs are persisted in MetaProgress — never rename or reuse.
/// </summary>
public static class Rapport
{
    public const string Ico = "ico";
    public const string IcoDisplayName = "Ico";
    /// <summary>The gift item. Provisional name (one constant; the developer owns the final name).</summary>
    public const string GiftName = "Easter Egg";
    public const string GiftNamePlural = "Easter Eggs";

    public const int IcoMaxLevel = 5;
    /// <summary>Gifts each Ico level costs, from level 2 on: 1, 1, 2, 2 (6 in total). Level 1 is free.</summary>
    private static readonly int[] IcoCosts = { 1, 1, 2, 2 };
    /// <summary>The Desconhecido gains one "layer" per gift; three exist for now.</summary>
    public const int UnknownMaxLayers = 3;

    public static int MaxGifts(string npcId) =>
        npcId == Ico ? IcoCosts.Sum() : npcId == MetaAllies.Unknown ? UnknownMaxLayers : 0;

    public static bool CanReceive(string npcId, int giftsGiven) => giftsGiven >= 0 && giftsGiven < MaxGifts(npcId);

    /// <summary>Ico's rapport level (1..5) for the gifts she has received.</summary>
    public static int IcoLevel(int giftsGiven)
    {
        int level = 1, spent = 0;
        foreach (int cost in IcoCosts)
        {
            spent += cost;
            if (giftsGiven < spent) break;
            level++;
        }
        return level;
    }

    public static int IcoLevel(MetaProgressProfile profile) => profile == null ? 1 : IcoLevel(profile.GiftsGivenTo(Ico));

    /// <summary>Gifts still missing for Ico's next level (0 at the maximum).</summary>
    public static int IcoGiftsToNextLevel(int giftsGiven)
    {
        int spent = 0;
        foreach (int cost in IcoCosts)
        {
            spent += cost;
            if (giftsGiven < spent) return spent - giftsGiven;
        }
        return 0;
    }

    /// <summary>Progress (0..1) inside Ico's current level, for the rapport bar.</summary>
    public static float IcoLevelProgress(int giftsGiven)
    {
        int spent = 0;
        foreach (int cost in IcoCosts)
        {
            if (giftsGiven < spent + cost) return (giftsGiven - spent) / (float)cost;
            spent += cost;
        }
        return 1f;
    }

    public static int UnknownLayers(int giftsGiven) => Math.Max(0, Math.Min(UnknownMaxLayers, giftsGiven));

    // Gauntlet boons that only enter the offer pool at an Ico level (12/11/2026). Only the two that already need
    // other boons first are locked: locking Eco Temporal and Punho Ecoante too left the first Technique offer with
    // 2 cards (its whole pool is 4), starving Reroll and Quarta opção. The initial boons will be redone later and the
    // new Fase 2 boons join these levels. The protected Titanic Fists → Conductor → Detonation combo is never locked.
    private static readonly Dictionary<string, int> BoonLevels = new Dictionary<string, int>
    {
        { GauntletExpansionCatalog.TitanFallId, 2 },
        { GauntletExpansionCatalog.PerfectRhythmId, 3 },
    };

    /// <summary>Ico level a boon needs before it can be offered (1 = always available).</summary>
    public static int RequiredIcoLevel(string boonId) =>
        !string.IsNullOrEmpty(boonId) && BoonLevels.TryGetValue(boonId, out int level) ? level : 1;

    public static bool IsBoonUnlocked(string boonId, int icoLevel) => icoLevel >= RequiredIcoLevel(boonId);

    public static IEnumerable<string> BoonsUnlockedAt(int icoLevel) =>
        BoonLevels.Where(pair => pair.Value == icoLevel).Select(pair => pair.Key).OrderBy(id => id, StringComparer.Ordinal);

    /// <summary>What Ico's <paramref name="level"/> gives, in one line for the weapon card.</summary>
    public static string IcoRewardSummary(int level)
    {
        switch (level)
        {
            case 2: return "Artefato: " + Artifacts.SparringWrap.Name + " · boon Titan Fall nas ofertas";
            case 3: return "Boon Ritmo Perfeito nas ofertas";
            case 4: return "Artefato: " + Artifacts.Mouthguard.Name;
            case 5: return "Artefato assinatura: " + Artifacts.SecondRound.Name;
            default: return "";
        }
    }
}

/// <summary>A keepsake-style artifact: one is equipped at the weapon card and shapes the next run.</summary>
public sealed class Artifact
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Source { get; }
    internal string NpcId { get; }
    internal int GiftsRequired { get; }
    public Artifact(string id, string name, string description, string source, string npcId, int giftsRequired)
    { Id = id; Name = name; Description = description; Source = source; NpcId = npcId; GiftsRequired = giftsRequired; }
}

/// <summary>Fase 1c artifact catalog (1 slot, no ranks). IDs are persisted — never rename or reuse.</summary>
public static class Artifacts
{
    public const int SparringWrapRerolls = 1;
    public const int MouthguardExtraChoices = 1;
    public const float SecondRoundHealFraction = .4f;
    public const float SecondRoundImmunitySeconds = 1.5f;
    public const float LuckyHookGiftChanceMultiplier = 2f;

    // PROVISÓRIO (12/11/2026): the developer liked the set but does not want the level-2 artifact tied to Protocols
    // and will revisit it. Until then it grants a reroll, which uses an existing, validated hook.
    public static readonly Artifact SparringWrap = new Artifact("faixa_sparring", "Faixa de Sparring",
        "+1 nova rolagem de oferta de boon por run. (Efeito provisório.)", "Ico — nível 2", Rapport.Ico, 1);
    public static readonly Artifact Mouthguard = new Artifact("protetor_bucal", "Protetor Bucal",
        "As ofertas das salas elite mostram 1 opção a mais.", "Ico — nível 4", Rapport.Ico, 4);
    public static readonly Artifact SecondRound = new Artifact("segundo_round", "Segundo Round",
        "Uma vez por run, você levanta do nocaute com 40% da vida.", "Ico — nível 5 (assinatura)", Rapport.Ico, 6);
    public static readonly Artifact LuckyHook = new Artifact("anzol_sorte", "Anzol da Sorte",
        "Dobra a chance de fisgar um " + Rapport.GiftName + " na pesca.", "Desconhecido — 1º presente", MetaAllies.Unknown, 1);

    public static readonly IReadOnlyList<Artifact> All = new[] { SparringWrap, Mouthguard, SecondRound, LuckyHook };

    public static bool TryGet(string id, out Artifact artifact)
    { artifact = All.FirstOrDefault(a => a.Id == id); return artifact != null; }

    /// <summary>Every artifact <paramref name="npcId"/> has handed over once it received <paramref name="giftsGiven"/> gifts.</summary>
    public static IEnumerable<Artifact> UnlockedBy(string npcId, int giftsGiven) =>
        All.Where(a => a.NpcId == npcId && giftsGiven >= a.GiftsRequired);
}

/// <summary>Where gifts come from and how rare they are (≈1 every 3 complete runs; decided 12/11/2026).</summary>
public static class GiftEconomy
{
    public const double FishingChance = .08;   // per catch, up to CastsPerVisit catches per serene room
    public const double EliteChance = .04;
    public const double BossChance = .10;
    public const int CastsPerVisit = 3;
    /// <summary>Chance that a run has the serene fishing room ("rare").</summary>
    public const double SereneRoomChance = .35;
    /// <summary>Fragmentos for an ordinary catch (inclusive range).</summary>
    public const int FishFragmentsMin = 2, FishFragmentsMax = 4;

    /// <summary>
    /// A catch brings a gift when the roll is under the chance; the very first gift of a profile is guaranteed on
    /// its first catch so the system is taught once. The Anzol da Sorte doubles the chance.
    /// </summary>
    public static bool FishingGivesGift(MetaProgressProfile profile, double roll)
    {
        if (profile != null && profile.giftsFoundTotal == 0) return true;
        double chance = FishingChance * (profile != null && profile.HasEquipped(Artifacts.LuckyHook.Id) ? Artifacts.LuckyHookGiftChanceMultiplier : 1f);
        return roll < chance;
    }
}
