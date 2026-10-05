using System.Globalization;

/// <summary>Cards are computed when the ticket opens, then frozen with its offers.</summary>
public static class ProtocolOfferPresentation
{
    private static string Number(float value) => value.ToString("0.##", CultureInfo.GetCultureInfo("pt-BR"));
    public static RunBoons.Offer Create(ProtocolInventory inventory, ProtocolKind kind, bool owned = false)
    {
        var data = inventory.Catalog.Get(kind);
        int current = inventory.Rank(kind), next = owned ? current : current + 1;
        string title, detail;
        switch (kind)
        {
            case ProtocolKind.Ignite:
                title = "Ignite • Ignição";
                detail = "Acertos aplicam Burn: " + Number(data.DamagePerRank * next) + " de dano/s por " + Number(data.Duration) + "s. Primeiro tick após " + Number(data.TickInterval) + "s. Renovar conserva o maior dano e o relógio."; break;
            case ProtocolKind.Frost:
                title = "Frost • Geada";
                detail = Number(System.Math.Min(1f, data.ChancePerRank * next) * 100) + "% de chance de Chill por " + Number(data.Duration) + "s. Congela inimigos pequenos; Heavy/Elite/Boss apenas desaceleram, até 50%/35%/15%."; break;
            case ProtocolKind.Conductor:
                title = "Conductor • Condutor";
                detail = "Encadeia até " + next + " vizinho(s) com " + Number(data.DamageFactorBase * 100) + "% do dano confirmado. Alcance " + Number(data.RadiusBase + data.RadiusPerRank * next) + "m; paredes bloqueiam a cadeia."; break;
            case ProtocolKind.Detonation:
                title = "Detonation • Detonação";
                detail = "Uma explosão por morte: " + Number((data.DamageFactorBase + data.DamageFactorPerAdditionalRank * (next - 1)) * 100) + "% do dano letal confirmado em " + Number(data.RadiusBase + data.RadiusPerRank * next) + "m. Burn amplia o raio; Chill prevalece e estilhaça até seis vítimas."; break;
            case ProtocolKind.Reactor:
                title = "Reactor • Reator";
                detail = "Exige Ignite. Acrescenta " + Number(data.DamagePerRank * next * 100) + "% do dano confirmado ao dano/s do Burn; renovação mantém a maior intensidade."; break;
            case ProtocolKind.Resonance:
                title = "Resonance • Ressonância";
                detail = "Exige Ignite ou Frost, e Conductor ou Detonation. Dano secundário recebe +" + Number(data.DamagePerRank * next * 100) + "% por status prévio (Burn/Chill), uma vez. Não amplifica ataques diretos ou ticks de Burn."; break;
            default:
                title = "Overflow • Transbordamento";
                detail = "Exige Ignite ou Frost. Acerto direto em alvo com Burn/Chill gera um burst de " + Number(data.DamagePerRank * next * 100) + "% do dano confirmado. Não consome nem renova status; secundários não o reativam."; break;
        }
        return new RunBoons.Offer(data.Id, title,
            "Protocol • Rank " + (owned ? current.ToString() : current + " → " + next) + "/" + data.MaxRank + "\n" + detail + "\nEfeitos de Protocol não geram energia Asura.");
    }
}
