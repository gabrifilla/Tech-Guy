using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>Read-only presentation of a private DTO copy. Damage buckets are the single partition.</summary>
public sealed class RunResultsModel
{
    public sealed class AbilityRow
    {
        public string Key { get; internal set; }
        public string Label { get; internal set; }
        public string Detail { get; internal set; }
        public double Native { get; internal set; }
        public double Secondary { get; internal set; }
        public double Total => Native + Secondary;
    }
    private readonly RunSummary _snapshot;
    private readonly List<AbilityRow> _abilities = new List<AbilityRow>();
    public bool Available => _snapshot != null;
    public string SessionId => _snapshot?.SessionId;
    public string Outcome => _snapshot?.Outcome;
    public string Title => !Available ? "Resultado indisponível" : Outcome == "Victory" ? "Extração concluída" : Outcome == "Death" ? "Morte na incursão" : "Incursão abandonada";
    public string Subtitle => !Available ? "Nenhum resumo válido para esta consulta. Não é possível inferir o desfecho." : Outcome == "Victory" ? "Run concluída por extração." : Outcome == "Death" ? "Seu personagem foi derrotado." : "Você encerrou o percurso antes da extração.";
    public string Identity { get; }
    public string Warning { get; }
    public double Total => _snapshot?.EffectiveDamage ?? 0;
    public string Damage => Available ? Number(Total) : "—";
    public string Received => Available ? Number(_snapshot.DamageReceived) : "—";
    public string Kills => Available ? _snapshot.EnemiesDefeated.ToString() : "—";
    public string Duration => Available ? Time(_snapshot.WallSeconds) : "—";
    public IReadOnlyList<AbilityRow> Abilities => _abilities.AsReadOnly();
    public IReadOnlyList<string> Sources { get; }
    public IReadOnlyList<string> Build { get; }
    public IReadOnlyList<string> Rewards { get; }
    public string Facts { get; }
    public RunResultsModel(RunSummary value, string expectedSession = null)
    {
        Sources = Array.Empty<string>(); Build = Array.Empty<string>(); Rewards = Array.Empty<string>();
        Identity = "SEM REGISTRO DISPONÍVEL"; Warning = ""; Facts = "";
        if (value == null || value.SchemaVersion != 1 || !value.Finalized ||
            !new[] { "Victory", "Death", "Aborted" }.Contains(value.Outcome) ||
            (!string.IsNullOrEmpty(expectedSession) && value.SessionId != expectedSession))
        { if (!string.IsNullOrEmpty(expectedSession)) Warning = "O resultado da run esperada não está disponível. Consulte separadamente o último registro salvo."; return; }
        _snapshot = JsonUtility.FromJson<RunSummary>(JsonUtility.ToJson(value));
        string localStart = DateTime.TryParse(_snapshot.StartedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var start) ? start.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "início indisponível";
        Identity = Weapon(_snapshot.InitialWeapon) + " / ARMA INICIAL  ·  " + Short(_snapshot.SessionId) + "  ·  " + localStart;
        var rows = (_snapshot.Damage ?? new List<RunDamageBucket>()).Where(x => x != null && x.PlayerAttributed && x.Effective > 0).ToArray();
        foreach (var g in rows.GroupBy(x => x.Weapon + ":" + x.Ability))
        {
            var first = g.First(); var ability = _snapshot.Abilities?.FirstOrDefault(x => x != null && x.Weapon == first.Weapon && x.Ability == first.Ability);
            double native = g.Where(x => !x.Secondary).Sum(x => x.Effective), secondary = g.Where(x => x.Secondary).Sum(x => x.Effective);
            string label = Slot(first.Ability) + (first.Weapon == "Unknown" ? "" : " · " + Weapon(first.Weapon));
            string mutations = string.Join(", ", g.Select(x => x.MutationId).Where(x => !string.IsNullOrEmpty(x)).Distinct().Select(Boon));
            _abilities.Add(new AbilityRow { Key = g.Key, Label = label, Native = native, Secondary = secondary,
                Detail = label + "\n\nDano efetivo: " + Number(native + secondary) + " HP\nNativo: " + Number(native) + " HP\nSecundário incluído: " + Number(secondary) +
                " HP\nUsos: " + (ability == null ? "—" : ability.Uses.ToString()) + "\nAcertos nativos / secundários: " + (ability == null ? "—" : ability.NativeHpTransactions + " / " + ability.SecondaryHpTransactions) +
                (mutations.Length > 0 ? "\nTransformação registrada: " + mutations : "") + "\n\nAcertos contam alvos que perderam HP, não precisão. Efeitos já pertencem ao total. Maior dano não mede importância tática." });
        }
        _abilities.Sort((a,b) => { int n=b.Total.CompareTo(a.Total); return n!=0?n:string.CompareOrdinal(a.Key,b.Key); });
        var warnings = new List<string>();
        if (_snapshot.ObserverErrors > 0) warnings.Add("Registro parcial: a coleta pode não cobrir toda a run.");
        double uncertain = rows.Where(x => x.Ability == "Unattributed").Sum(x => x.Effective);
        if (uncertain > 0) warnings.Add(Number(uncertain) + " HP de habilidade incerta, incluídos no total.");
        if (_snapshot.UnattributedEnemyHpLoss > 0) warnings.Add(Number(_snapshot.UnattributedEnemyHpLoss) + " HP sem autoria confirmada, fora do seu total.");
        Warning = string.Join(" ", warnings);
        Sources = rows.OrderByDescending(x => x.Effective).Select(x =>
            (x.Secondary ? "SECUNDÁRIO" : "NATIVO") + "  ·  " + ProducerLabel(x) +
            "  ·  " + Slot(x.Ability) + " / " + Weapon(x.Weapon) + "  ·  " + Number(x.Effective) + " HP" + (string.IsNullOrEmpty(x.MutationId) ? "" : "\nTransformação: " + Boon(x.MutationId))).ToArray();
        Build = _snapshot.Build == null ? new[] { "Build indisponível" } : _snapshot.Build.Where(x => x != null).OrderBy(x => x.Family).Select(x =>
            Family(x.Family) + "  ·  " + Boon(x.Id) + "  ·  " + x.Acquisitions + (x.Acquisitions == 1 ? " aquisição" : " aquisições")).ToArray();
        Rewards = _snapshot.Rewards == null ? new[] { "Histórico indisponível" } : _snapshot.Rewards.Where(x => x != null).Select(x =>
            Boon(x.Id) + "  ·  nó " + x.Node + "  ·  " + Time(x.ScaledSeconds) + " de tempo simulado" + (x.Alternative ? " · alternativa" : "")).ToArray();
        Facts = "Maior impacto efetivo: " + Number(_snapshot.LargestImpact) + " HP\nOverkill: " + (_snapshot.OverkillComplete ? Number(_snapshot.Overkill) + " HP" : "dado parcial") +
            "\nCrédito letal confirmado: " + _snapshot.PlayerCreditedKills + "\nTempo simulado: " + Time(_snapshot.ScaledSeconds) + "\nID: " + _snapshot.SessionId + "\nSeed: " + _snapshot.Seed +
            "\nFases registradas: " + (_snapshot.Progress?.Count.ToString() ?? "indisponível") + "\n" + string.Join(" > ", (_snapshot.Progress ?? new List<RunProgressSummary>()).Where(x => x != null).Select(x => "nó " + x.Node + ": " + x.Phase));
    }
    private static string ProducerLabel(RunDamageBucket value)
    {
        if(value.Producer=="Direct")return "Impacto nativo";
        if(value.Producer=="BurnTick")return "Ignite · queimadura";
        if(value.Producer=="ThermalShock")return "Choque térmico";
        if(value.Producer=="Technique")return "Efeito de técnica";
        return !string.IsNullOrEmpty(value.ProducerId)?Boon(value.ProducerId):value.Producer;
    }
    public static string Number(double n) => n > 0 && n < .1 ? "<0,1" : n.ToString("N1", CultureInfo.GetCultureInfo("pt-BR")).Replace(",0", "");
    public static string Time(double n) => ((int)n / 60).ToString("00") + ":" + ((int)n % 60).ToString("00");
    public static string Short(string id) => string.IsNullOrEmpty(id) ? "—" : id.Substring(0, Math.Min(8, id.Length)).ToUpperInvariant();
    public static string Weapon(string value) => value == "Gauntlet" ? "Manopla" : value == "Bow" ? "Arco" : value == "Spear" ? "Lança" : value ?? "—";
    public static string Slot(string value) => value == "Basic" ? "Ataque básico" : value == "Unattributed" ? "Origem de habilidade incerta" : "Ação " + value;
    public static string Family(string value) => value == "TechniqueOrLegacy" || value == "Technique" ? "Outros efeitos" : value ?? "Categoria indisponível";
    public static string Boon(string id)
    {
        if (string.IsNullOrEmpty(id)) return "Identidade indisponível";
        if (WeaponRunModifiers.TryDefinition(id, out var definition)) return definition.Title;
        if (BoonExpansionCatalog.TryDefinition(id, out var expansion)) return expansion.Title;
        if (ProtocolInventory.TryKind(id, out var protocol)) return protocol.ToString();
        if (id == "mutation_gauntlet_eclipse_uppercut") return "Eclipse Uppercut";
        if (id == "mutation_gauntlet_cadenced_asura") return "Asura Cadenciada";
        if (id == BoonReworkCatalog.ViolentFallId) return "Queda Violenta";
        return id;
    }
}
