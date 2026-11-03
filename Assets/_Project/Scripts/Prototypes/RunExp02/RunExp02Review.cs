using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>Local encounter ownership; no ProgressionDirector, reward ticket, currency or save writes.</summary>
public sealed class RunExp02Review : MonoBehaviour
{
    [SerializeField] private PlayerActor _player;
    [SerializeField] private GameObject _fragmento, _emissor;
    [SerializeField] private bool _coverArena;
    private readonly List<Actor> _enemies = new List<Actor>();
    private RunExp02RewardReview _rewards;
    private RunStatistics _statistics;
    public PlayerActor Player => _player;
    public IReadOnlyList<Actor> Enemies => _enemies;
    public int Remaining { get; private set; }
    public int Deaths { get; private set; }
    public bool Completed => Remaining == 0 && Deaths > 0;
    public bool CoverArena => _coverArena;
    public bool ShowToolbar { get; set; } = true;
    private IEnumerator Start()
    {
        // Runtime equip only; deliberately never calls WeaponLoadout.Save.
        _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"));
        yield return null;
        _rewards = gameObject.AddComponent<RunExp02RewardReview>(); _rewards.Bind(this);
        Spawn(3);
    }
    public void Spawn(int count, int only = -1)
    {
        ClearEncounter(); Deaths=0;
        _statistics=new RunStatistics(_player,System.Guid.NewGuid(),402,Time.realtimeSinceStartupAsDouble,Time.timeAsDouble);
        for(int i=0;i<count;i++)
        {
            int kind=only<0?(i%3==2?1:0):only;
            Vector3 position=count==1?new Vector3(0,0,2):count==3?new Vector3(i==0?-4:i==1?4:0,0,i<2?4:7):
                new Vector3(-9+(i%8)*2.5f,0,-1+(i/8)*1.1f);
            if(NavMesh.SamplePosition(position,out var hit,3,NavMesh.AllAreas)) position=hit.position;
            var go=Instantiate(kind==0?_fragmento:_emissor,position,Quaternion.Euler(0,180,0),transform);
            // Actor.Awake attaches this even to isolated candidates. Remove before the first combat frame.
            if(go.TryGetComponent<CoinDrop>(out var coins))Destroy(coins);
            var ai=go.GetComponent<EnemyAI>();ai.player=_player.transform;ai.ConfigureEncounterBounds(Vector3.zero,new Vector2(24,20));
            var actor=go.GetComponent<Actor>(); actor.Died+=OnEnemyDied;_enemies.Add(actor);Remaining++;
            _statistics.Watch(actor);
        }
    }
    private void OnEnemyDied(Actor actor) { actor.Died-=OnEnemyDied; Remaining=Mathf.Max(0,Remaining-1);Deaths++; }
    public void ClearEncounter()
    {
        _statistics?.Dispose();_statistics=null; // Dispose observes teardown; Finish/save is never called.
        foreach(var enemy in _enemies) if(enemy) {enemy.Died-=OnEnemyDied;enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);}
        _enemies.Clear();Remaining=0;
    }
    private void OnDestroy() { ClearEncounter(); }
    public void HoldCombat(bool hold)
    {
        foreach(var enemy in _enemies) if(enemy && !enemy.IsDead)
        {
            if(enemy.TryGetComponent<CombatReadabilityUI>(out var readability))readability.enabled=!hold;
            if(enemy.TryGetComponent<EnemyCombatFeedback>(out var feedback))feedback.enabled=!hold;
            var ai=enemy.GetComponent<EnemyAI>(); if(hold) ai.InterruptAttack();ai.enabled=!hold;
            var agent=enemy.GetComponent<NavMeshAgent>();if(agent && agent.isOnNavMesh)agent.isStopped=hold;
        }
    }
    public void Reload(bool cover)
    {
#if UNITY_EDITOR
        Time.timeScale=1;
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/_Project/Prototypes/RunExp02/"+(cover?"ArenaB":"ArenaA")+".unity",new LoadSceneParameters(LoadSceneMode.Single));
#endif
    }
    private void OnGUI()
    {
        if(!ShowToolbar || (_rewards && _rewards.Visible))return;
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        GUILayout.BeginArea(new Rect(12,12,1256,75),GUI.skin.box);
        GUILayout.BeginHorizontal();GUILayout.Label("RUN-EXP-02 · "+(_coverArena?"Coberturas deslocadas":"Arena aberta")+" · "+Remaining+" ameaças");
        if(GUILayout.Button("Arena A / reiniciar",GUILayout.Width(165)))Reload(false);
        if(GUILayout.Button("Arena B / reiniciar",GUILayout.Width(165)))Reload(true);
        if(_player && !_player.IsDead)
        {
            if(GUILayout.Button("Só Fragmento",GUILayout.Width(125)))Spawn(1,0);
            if(GUILayout.Button("Só Emissor",GUILayout.Width(125)))Spawn(1,1);
            if(GUILayout.Button("Recompensas",GUILayout.Width(135)))_rewards.Open();
        }
        GUILayout.EndHorizontal();GUILayout.Label(Completed?"Encontro concluído — revisão sem progresso persistente.":!_player?"Jogador abatido — reinicie a arena.":"Controles atuais: WASD · mira independente · ataque básico · especial e habilidades conforme HUD · dash configurável. Saia de Play para voltar ao projeto.");
        GUILayout.EndArea();GUI.matrix=old;
    }
}
