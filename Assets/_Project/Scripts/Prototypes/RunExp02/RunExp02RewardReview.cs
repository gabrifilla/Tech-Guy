using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Read-only presentation of production-generated offers. Never invokes Choose or grants a ticket.</summary>
public sealed class RunExp02RewardReview : MonoBehaviour
{
    private RunExp02Review _review;
    private RunBoons _run;
    private RunRewardUI _native;
    private bool _ready;
    private string _weapon="Manopla";
    private float _previousScale=1;
    private GUIStyle _title,_body,_small;
    public bool Visible { get; private set; }
    public RunBoons Run => _run;
    public void Bind(RunExp02Review review) { _review=review; }
    private void OnDestroy() { if(Visible && Time.timeScale==0)Time.timeScale=_previousScale; }
    public void Open() { if(!Visible)StartCoroutine(Prepare("Weapons/Melee/Gauntlet/Gauntlet","Manopla")); }
    public IEnumerator Prepare(string path,string name)
    {
        if(!Visible)_previousScale=Time.timeScale;
        Visible=true;_ready=false;_review.HoldCombat(true);Time.timeScale=0;
        var player=_review.Player;
        if(_run) {Destroy(_run);yield return null;}
        player.EquipWeapon(Resources.Load<WeaponScript>(path));
        _weapon=name;_run=player.gameObject.AddComponent<RunBoons>();
        _native=player.GetComponent<RunRewardUI>();_native.enabled=false;
        yield return null;yield return null;
        _run.OfferReward(1);_ready=true;
    }
    public void Close()
    {
        _ready=false;Visible=false;
        StartCoroutine(CloseRoutine());
    }
    private IEnumerator CloseRoutine()
    {
        if(_run)Destroy(_run);yield return null;
        if(_native)_native.enabled=false;
        if(_review.Player)_review.Player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"));
        if(_review.Player && _review.Player.TryGetComponent<NavMeshAgent>(out var agent) && agent.isOnNavMesh)agent.isStopped=false;
        Time.timeScale=_previousScale;
        _review.HoldCombat(false);
    }
    // Deliberately uses OWNED inventory only, never another unchosen card as an active synergy.
    public static string Interaction(RunBoons run, string id)
    {
        bool fire=RunModifierPresentation.Count(run,"ignite")>0, frost=RunModifierPresentation.Count(run,"frost")>0;
        if(id=="reactor")return fire?"AO ESCOLHER · sua queimadura já adquirida passa a escalar com o acerto.":"POSSIBILIDADE FUTURA · precisa adquirir fogo. Nenhuma oferta futura é garantida.";
        if(id=="conductor")return fire||frost?"AO ESCOLHER · o elemento adquirido passa a viajar nas descargas.":"VALOR IMEDIATO · descargas. POSSIBILIDADE FUTURA · transportar fogo/gelo, se adquirido.";
        if(id=="overflow"||id=="resonance")return fire||frost?"CONDIÇÃO PRESENTE · fogo/gelo adquirido; consulte as condições do efeito abaixo.":"POSSIBILIDADE FUTURA · depende de fogo/gelo no alvo. Nenhuma oferta futura é garantida.";
        return "AO ESCOLHER · aplica o efeito descrito. Outras combinações dependem de aquisições e condições compatíveis.";
    }
    private void OnGUI()
    {
        if(!Visible||!_ready||!_run)return;
        if(_title==null)
        {
            _title=new GUIStyle(GUI.skin.label){fontSize=25,fontStyle=FontStyle.Bold,wordWrap=true};
            _body=new GUIStyle(GUI.skin.label){fontSize=18,wordWrap=true,richText=true};
            _small=new GUIStyle(_body){fontSize=14};
        }
        var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        Color oldColor=GUI.color;GUI.color=new Color(.035f,.045f,.065f,.99f);GUI.DrawTexture(new Rect(0,0,Screen.width/scale,Screen.height/scale),Texture2D.whiteTexture);GUI.color=oldColor;
        GUI.Label(new Rect(40,24,1200,45),"Inspecionar escolhas · "+_weapon,_title);
        string owned=_run.Acquired.Count==0?"nenhum":string.Join(", ",_run.Acquired.Select(o=>o.Title));
        GUI.Label(new Rect(40,72,1200,50),"Ofertas reais do catálogo atual · inspeção sem aquisição. Ativos agora: "+owned+". Cada consulta é independente; combate pausado.",_small);
        string[] paths={"Weapons/Melee/Gauntlet/Gauntlet","Weapons/Ranged/Bow_arrow/Bow","Weapons/Melee/Spear/Spear"};string[] names={"Manopla","Arco","Lança"};
        for(int i=0;i<3;i++)if(GUI.Button(new Rect(40+i*150,126,140,35),names[i]))StartCoroutine(Prepare(paths[i],names[i]));
        if(GUI.Button(new Rect(1050,126,180,35),"Voltar ao combate"))Close();
        int count=_run.Choices.Count;float w=1180f/Mathf.Max(1,count);
        for(int i=0;i<count;i++)
        {
            var offer=_run.Choices[i];var view=RunModifierPresentation.For(offer);float x=40+i*w;
            GUI.Box(new Rect(x,186,w-18,475),GUIContent.none);
            GUI.Label(new Rect(x+18,202,w-54,38),view.Category+" · "+view.Scope,_small);
            GUI.Label(new Rect(x+18,249,w-54,85),offer.Title,_title);
            GUI.Label(new Rect(x+18,343,w-54,190),view.Description,_body);
            GUI.Label(new Rect(x+18,548,w-54,104),Interaction(_run,offer.Id),_small);
        }
        GUI.Label(new Rect(40,676,1180,35),"Impacto próximo, distância ou utilidade imediata: compare o efeito com seu estilo. A inspeção não recomenda uma sequência fixa.",_small);
        GUI.matrix=old;
    }
}
