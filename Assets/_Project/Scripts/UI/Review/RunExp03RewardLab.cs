using System.Collections;
using UnityEngine;

/// <summary>Isolated UI review only. Real RunBoons choices, runtime copies, no persistence/director.</summary>
public sealed class RunExp03RewardLab : MonoBehaviour
{
    [SerializeField] private PlayerActor _player;
    private RunBoons _run;private RewardSelectionView _view;private bool _loading;private int _room=1;
    public RunBoons Run=>_run;
    public RewardSelectionView View=>_view;
    public bool ShowControls {get;set;}=true;
    public bool AssistedShowcase {get;set;}
    private IEnumerator Start(){yield return Prepare(0);}
    public IEnumerator Prepare(int weapon)
    {
        _loading=true;AssistedShowcase=false;
        if(_view)Destroy(_view);if(_run)Destroy(_run);yield return null;
        _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[weapon]));
        _run=_player.gameObject.AddComponent<RunBoons>();yield return null;yield return null;
        _room=1;_run.OfferReward(_room);_view=_player.GetComponent<RewardSelectionView>();if(!_view)_view=_player.gameObject.AddComponent<RewardSelectionView>();_view.ShowReviewControls=true;_view.Bind(_run,RewardSelectionView.Layout.Artifacts,true);_loading=false;
    }
    private void OnGUI()
    {
        if(_loading||!_run||!ShowControls)return;var old=GUI.matrix;GUI.depth=-110;
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        GUILayout.BeginArea(new Rect(360,10,420,35));GUILayout.BeginHorizontal();
        if(GUILayout.Button("Manopla"))StartCoroutine(Prepare(0));if(GUILayout.Button("Lança"))StartCoroutine(Prepare(2));if(GUILayout.Button("Arco"))StartCoroutine(Prepare(1));
        bool previousEnabled=GUI.enabled;GUI.enabled=previousEnabled&&!AssistedShowcase;
        if(GUILayout.Button(_view&&_view.enabled?"Original":"Conceitos"))
        {if(_view){_view.enabled=!_view.enabled;_player.GetComponent<RunRewardUI>().enabled=!_view.enabled;}}
        GUI.enabled=previousEnabled;
        GUILayout.EndHorizontal();GUILayout.EndArea();
        if(AssistedShowcase)GUI.Label(new Rect(40,36,1120,24),"MOSTRUÁRIO ASSISTIDO · ofertas de arte para comparação, sem aquisição; não representa sorteio de run.");
        if(!_run.IsChoosing)
        {
            GUILayout.BeginArea(new Rect(350,290,580,130),GUI.skin.box);
            GUILayout.Label("Escolha aplicada apenas nesta revisão. Sem run persistente.");
            if(GUILayout.Button("Consultar outra recompensa de teste")){_run.OfferReward(++_room);_view.Refresh();}
            GUILayout.EndArea();
        }
        GUI.matrix=old;
    }
}
