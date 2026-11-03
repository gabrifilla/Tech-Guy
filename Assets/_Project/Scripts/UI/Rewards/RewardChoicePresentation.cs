using System;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>Read-only editorial layer. Ownership comes only from the acquired runtime inventory.</summary>
public sealed class RewardChoicePresentation
{
    public string Id,Name,Family,Scope,Effect,Context,Restriction,Details,Level;
    public int Glyph;
    private static string ControlLabel(string slot)
    {
        if(slot=="R"&&GamePreferences.UsesDirectControls)return GamePreferences.BindingLabel(GameControl.Move);
        return GamePreferences.BindingLabel((GameControl)((int)GameControl.Skill1+"QWER".IndexOf(slot,StringComparison.Ordinal)));
    }
    public static RewardChoicePresentation For(RunBoons run,RunBoons.Offer offer)
    {
        var view=RunModifierPresentation.For(offer);int rank=RunModifierPresentation.Count(run,offer.Id);
        var model=new RewardChoicePresentation{Id=offer.Id,Name=offer.Title,Scope=view.Scope,Details=view.Description,Context="",Restriction="",Level=rank>0?"NÍVEL "+rank+" → "+(rank+1):"NOVA REESCRITA"};
        if(model.Scope=="Basic")model.Scope="ATAQUE BÁSICO";
        string category=view.Category;
        model.Family=category=="MUTATION"?"MUTAÇÃO":category=="AUGMENT"?"APRIMORAMENTO":category=="PROTOCOL"?"PROTOCOLO":category=="TECHNIQUE"?"TÉCNICA":category=="RESONANCE / DUO"?"RESSONÂNCIA / DUO":category;
        model.Glyph=category=="MUTATION"?0:category=="PROTOCOL"?1:2;
        if(category=="AUGMENT")model.Glyph=3;
        if(category=="RESONANCE / DUO")model.Glyph=4;
        if(offer.Id==RunBoonOfferCatalog.ExhaustedId){model.Glyph=5;model.Family="CONTINUAR";model.Level="SEM AQUISIÇÃO";}
        string text=Regex.Replace(view.Description??"","<[^>]*>","");
        text=Regex.Replace(text,@"^(?:[QWER]: |TRANSFORMA Q: )","");
        int end=text.IndexOf(". ",StringComparison.Ordinal);if(end>=0)text=text.Substring(0,end+1);
        if(text.Length>100){int cut=text.LastIndexOf(' ',Math.Min(94,text.Length-1));text=text.Substring(0,Math.Max(1,cut))+"…";}
        model.Effect=text;
        if(Regex.IsMatch(view.Scope,@"\b[QWER]\b"))
            model.Scope=(view.Scope=="R"&&GamePreferences.UsesDirectControls?"ESPECIAL · ":"HABILIDADE · ")+Regex.Replace(view.Scope,@"\b[QWER]\b",match=>ControlLabel(match.Value));
        model.Details=Regex.Replace(model.Details??"",@"\b[QWER]\b",match=>ControlLabel(match.Value));
        bool titan=run.WeaponModifiers?.Expansion.Has(TitanicFistsDefinition.Id)==true;
        bool fire=RunModifierPresentation.Count(run,"ignite")>0,frost=RunModifierPresentation.Count(run,"frost")>0;
        if(ProtocolInventory.TryKind(offer.Id,out _))model.Name=offer.Title.Split('•').Last().Trim();
        switch(offer.Id)
        {
            case "mutation_gauntlet_titan_fists":model.Effect="Seus golpes finais lançam tornados de vento.";break;
            case "conductor":
                model.Effect=rank>0?"Encadeie mais alvos e amplie o alcance das descargas.":"Seus acertos encadeiam descargas entre inimigos.";
                model.Restriction="Descargas respeitam paredes e alvos válidos.";
                if(titan)model.Context=rank>0?"Novos tornados passam a encadear mais alvos.":"Novos tornados passam a encadear raios.";
                else if(fire||frost)model.Context="O elemento que você já adquiriu viaja nas descargas.";
                break;
            case "detonation":model.Effect=rank>0?"Amplie a força e o raio das explosões.":"Abates por impacto detonam os inimigos próximos.";if(titan)model.Context="Abates dos seus tornados podem detonar a horda.";break;
            case "ignite":model.Effect="Seus acertos deixam queimadura ao longo do tempo.";break;
            case "frost":model.Effect="Seus acertos podem congelar inimigos pequenos.";model.Restriction="Efeito por chance. Alvos resistentes são desacelerados.";break;
            case "reactor":model.Effect="Faça a queimadura crescer com a força do acerto.";model.Restriction=fire?"Atua na queimadura aplicada ao alvo.":"Precisa de fogo aplicado. Não concede fogo.";break;
            case "overflow":model.Effect="Golpeie alvos com fogo ou gelo para gerar uma descarga de dano.";model.Restriction="Precisa do elemento no alvo; não o concede.";break;
            case "resonance":model.Effect="Elementos no alvo fortalecem descargas e explosões.";model.Restriction="Precisa de fogo/gelo e um efeito compatível.";break;
            case "weapon_Piercing":model.Effect="Suas flechas atravessam mais inimigos na mesma linha.";break;
            case "weapon_Ricochet":model.Effect="Suas flechas saltam entre alvos próximos.";break;
            case "weapon_TwinShot":model.Effect="Acrescente flechas ao seu disparo básico.";break;
            case "weapon_EchoThrust":model.Effect="Repita suas estocadas para manter a pressão à distância.";break;
            case "weapon_FlurryEcho":model.Effect="O último golpe da sequência continua numa rajada de socos.";break;
            case "weapon_AsuraEcho":model.Effect="O golpe final da Asura deixa uma réplica de impacto.";break;
        }
        if(GauntletContentAuthoring.TryMutation(offer.Id,out var mutation))
            switch(mutation)
            {
                case GauntletMutation.DemolishingAdvance:model.Effect="Atravesse a horda e encerre o avanço com um uppercut.";model.Restriction="Sólidos bloqueiam. Arremessa para cima somente na quebra.";break;
                case GauntletMutation.PressureField:model.Effect="O último golpe deixa uma zona de impactos pulsantes.";model.Restriction="Os pulsos não geram Asura.";break;
                case GauntletMutation.Epicenter:model.Effect="Faça seus impactos se espalharem ao redor do corpo.";break;
                case GauntletMutation.CadencedAsura:model.Effect="Conduza a sequência de Asura com três pressionamentos.";model.Restriction="Até 0,8s para continuar. Segurar não continua; espera sem proteção.";break;
                case GauntletMutation.EclipseUppercut:model.Effect="Lance inimigos na quebra e prepare a continuação no ar.";model.Restriction="Alvos resistentes recebem a alternativa compatível.";break;
            }
        if(offer.Id==GauntletExpansionCatalog.EchoingFistId){model.Effect="O finalizador confirmado deixa um soco ecoando no lugar.";model.Restriction="Exige contato do finalizador; não repete a sequência inteira.";}
        if(offer.Id==GauntletExpansionCatalog.TemporalEchoId){model.Effect="A habilidade deixa um eco atrasado no local do impacto.";}
        if(offer.Id==GauntletExpansionCatalog.PerfectRhythmId){model.Effect="Continue no ritmo certo para transformar o último soco em onda.";model.Restriction="Depende da Asura Cadenciada e da janela de ritmo.";}
        if(offer.Id==GauntletExpansionCatalog.TitanFallId){model.Effect="Prepare um cruzado descendente antes da onda da queda.";model.Restriction="Exige Uppercut e Queda Violenta adquiridos.";}
        switch(offer.Id)
        {
            case SpearBoonCatalog.HorizonId:model.Effect="Troque o avanço por uma estocada que deixa uma linha detonável.";model.Restriction="A habilidade de hastes nativa detona a linha. Não avança.";break;
            case SpearBoonCatalog.SiegeId:model.Effect="Plante hastes que pulsam por contato ou pela trajetória da lança.";break;
            case SpearBoonCatalog.OrbitId:model.Effect="Faça a lança orbitar uma posição e voltar pelo campo de batalha.";model.Restriction="Antecipar o retorno perde passagens; paredes interrompem a rota.";break;
            case SpearBoonCatalog.ReturnMarkId:model.Effect="Acertar o mesmo alvo na ida e na volta detona uma marca.";model.Restriction="Exige Órbita do Dragão adquirida.";break;
            case SpearBoonCatalog.MoonEchoId:model.Effect="O retorno da órbita deixa um último corte ecoando.";model.Restriction="Exige Órbita do Dragão adquirida.";break;
            case SpearBoonCatalog.ReturnLineId:model.Effect="Consuma a marca de ida e volta para fortalecer sua detonação.";model.Restriction="Exige Órbita e Marca de Retorno adquiridas.";break;
            case BowBoonCatalog.HuntingBondId:model.Effect="Sele o alvo para orientar suas próximas flechas dentro do cone de mira.";model.Restriction="Perde a dispersão. Exige cone, alcance e visão.";break;
            case BowBoonCatalog.JudgmentArrowId:model.Effect="Carregue uma flecha para perfurar a linha inimiga.";model.Restriction="Parado ao carregar; dash ou dano interrompem sem reembolso.";break;
            case BowBoonCatalog.MineGardenId:model.Effect="Prepare minas que detonam quando inimigos se aproximam.";model.Restriction="Substitui o leque e o recuo; replantar remove as minas anteriores.";break;
            case BowBoonCatalog.PredatorSealId:model.Effect="Marque com flechas para acionar suas minas de mais longe.";model.Restriction="Exige Jardim de Minas adquirido.";break;
            case BowBoonCatalog.CuttingReturnId:model.Effect="Sua flecha plenamente carregada retorna pelo caminho percorrido.";model.Restriction="Exige Flecha do Juízo adquirida; sólidos encerram o tiro.";break;
            case BowBoonCatalog.EclipseArcherId:model.Effect="Concentre o disparo carregado num corredor contra o alvo selado.";model.Restriction="Exige Vínculo e Flecha do Juízo compatíveis.";break;
            case BowBoonCatalog.HuntingDetonatorId:model.Effect="Uma flecha no alvo marcado pode detonar uma mina armada.";model.Restriction="Exige minas e selo adquiridos; respeita visão.";break;
        }
        // The detailed source remains available. Never infer an active partner from co-offers.
        if(view.IsRewrite)model.Restriction="Substitui a forma atual de "+model.Scope+". "+model.Restriction;
        return model;
    }
}
