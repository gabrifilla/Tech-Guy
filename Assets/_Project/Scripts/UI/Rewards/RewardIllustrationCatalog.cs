/// <summary>Presentation-only art mapping. Missing illustrations retain the existing family glyph.</summary>
public static class RewardIllustrationCatalog
{
    public static readonly string[] Keys={"Titanic","TemporalEcho","Conductor","Frost","Ignite","SpearOrbit","Judgment","Piercing"};
    public static string KeyFor(string id)
    {
        switch(id)
        {
            case TitanicFistsDefinition.Id:return "Titanic";
            case GauntletExpansionCatalog.TemporalEchoId:case GauntletExpansionCatalog.EchoingFistId:return "TemporalEcho";
            case "conductor":return "Conductor";case "frost":return "Frost";case "ignite":return "Ignite";
            case SpearBoonCatalog.OrbitId:return "SpearOrbit";case BowBoonCatalog.JudgmentArrowId:return "Judgment";
            case "weapon_Piercing":return "Piercing";default:return null;
        }
    }
}
