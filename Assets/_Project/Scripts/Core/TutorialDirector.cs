using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum TutorialStage { Movement, Dodge, BasicAttack, Skill, Encounter, Reward, Departure }

/// <summary>Scene-owned onboarding; editable beats and hooks can later drive the prologue.</summary>
public sealed class TutorialDirector : MonoBehaviour
{
    [Serializable]
    public sealed class Beat
    {
        public string title;
        public string control;
        [TextArea] public string instruction;
        [TextArea] public string transmission;
        public UnityEvent onEnter = new UnityEvent();
        public Beat(string title, string control, string instruction, string transmission)
        { this.title = title; this.control = control; this.instruction = instruction; this.transmission = transmission; }
    }
    [SerializeField] private PlayerActor _player;
    [SerializeField] private WeaponScript _trainingWeapon;
    [SerializeField] private TutorialTrainingTarget _target;
    [SerializeField] private Actor[] _enemies;
    [SerializeField] private Transform _movementGoal;
    [SerializeField] private Transform _exit;
    [SerializeField] private GameObject _marker;
    [SerializeField] private GameObject _exitLight;
    [SerializeField] private string _lobbyScene = "NexusLobby";
    [SerializeField] private Beat[] _beats =
    {
        new Beat("Reconecte os movimentos", "BOTÃO DIREITO", "Clique no chão e caminhe até o marcador azul.", "Sistemas em calibração. Siga as luzes para acessar o Nexus."),
        new Beat("Saia da linha de perigo", "ESPAÇO", "Aponte para uma área livre e execute uma esquiva.", "Esquivar reposiciona você. Observe a recarga na barra inferior."),
        new Beat("Teste suas manoplas", "BOTÃO ESQUERDO", "Aproxime-se do alvo e acerte 3 ataques básicos. Mire perto dele: o cursor ajuda a direcionar o golpe.", "Ataques básicos não gastam mana. Segure o botão para continuar atacando."),
        new Beat("Libere uma habilidade", "Q", "Aponte para o alvo e acerte com Q. Observe o gasto de mana e a recarga.", "Q, W e E são suas habilidades. Alterne golpes para carregar Asura, usada com R."),
        new Beat("Complete a simulação", "ATAQUE + ESQUIVA", "Derrote os dois drones. Saia das áreas marcadas antes dos impactos.", "Combine ataques e habilidades. Os drones desta calibração causam pouco dano."),
        new Beat("Escolha sua evolução", "1 / 2 / 3", "Escolha um dos três modificadores. Leia o efeito e a habilidade afetada.", "Nas incursões, as escolhas se acumulam e criam novas combinações. Elas duram somente aquela run."),
        new Beat("Entre no Nexus", "BOTÃO DIREITO", "Caminhe até o portal dourado. Você também pode testar seu novo modificador no alvo.", "Calibração concluída. No Nexus, prepare seu equipamento e use o portal para iniciar uma incursão.")
    };
    [SerializeField] private UnityEvent _onCompleted = new UnityEvent();
    private AbilityHolder _abilities;
    private RunBoons _boons;
    private bool _skillUsed, _leaving;
    private int _basicHits, _defeated;
    public TutorialStage Stage { get; private set; }
    public Beat CurrentBeat => _beats[(int)Stage];
    public int BasicHits => _basicHits;
    public int Defeated => _defeated;
    public int EnemyCount => _enemies.Length;
    public bool RewardVisible => _boons && _boons.IsChoosing;
    public bool IsLeaving => _leaving;
    public event Action<TutorialStage> StageChanged;

    public void Configure(PlayerActor player, WeaponScript weapon, TutorialTrainingTarget target, Actor[] enemies,
        Transform movementGoal, Transform exit, GameObject marker, GameObject exitLight)
    {
        _player = player; _trainingWeapon = weapon; _target = target; _enemies = enemies;
        _movementGoal = movementGoal; _exit = exit; _marker = marker; _exitLight = exitLight;
    }
    private void Awake()
    {
        if (!_player || !_trainingWeapon || !_target || !_movementGoal || !_exit || !_marker || !_exitLight ||
            _enemies == null || _enemies.Length == 0 || Array.Exists(_enemies, enemy => !enemy) ||
            _beats == null || _beats.Length != 7 || Array.Exists(_beats, beat => beat == null) ||
            !_player.TryGetComponent(out _abilities))
        { Debug.LogError("Tutorial requires player, weapon, target, enemies, landmarks and seven beats.", this); enabled = false; return; }
        foreach (Actor enemy in _enemies) { enemy.Died += OnEnemyDied; enemy.gameObject.SetActive(false); }
        _target.DamageReceived += OnTargetDamaged;
        _target.gameObject.SetActive(false);
        _abilities.AbilityUsed += OnAbilityUsed;
        _player.Died += OnPlayerDied;
        _exitLight.SetActive(false);
    }
    private void Start()
    {
        _player.GetComponent<PauseMenuUI>().BindTutorial(this);
        // Teach one predictable loadout without changing the player's saved arsenal selection.
        _player.EquipWeapon(_trainingWeapon); _abilities.RefreshLoadout();
        _player.RestoreHealthToMax(); _player.RestoreMana(_player.maxMana);
        Enter(TutorialStage.Movement);
        StartCoroutine(CheckProgress());
    }
    private IEnumerator CheckProgress()
    {
        var interval = new WaitForSeconds(.1f);
        while (!_leaving && _player && !_player.IsDead)
        {
            if (Stage == TutorialStage.Movement && Near(_movementGoal.position)) Enter(TutorialStage.Dodge);
            else if (Stage == TutorialStage.Departure && Near(_exit.position)) Leave(true);
            yield return interval;
        }
    }
    private bool Near(Vector3 point)
    { Vector3 offset = _player.transform.position - point; offset.y = 0; return offset.sqrMagnitude < 2.25f; }
    private void Enter(TutorialStage stage)
    {
        Stage = stage;
        _marker.SetActive(stage == TutorialStage.Movement || stage == TutorialStage.BasicAttack || stage == TutorialStage.Skill || stage == TutorialStage.Departure);
        _marker.transform.position = (stage == TutorialStage.Departure ? _exit.position : stage == TutorialStage.Movement ? _movementGoal.position : _target.transform.position) + Vector3.up * .04f;
        Vector3 markerPosition = _marker.transform.position; markerPosition.y = .04f; _marker.transform.position = markerPosition;
        if (stage == TutorialStage.BasicAttack) _target.gameObject.SetActive(true);
        if (stage == TutorialStage.Encounter)
        {
            _target.gameObject.SetActive(false);
            _player.RestoreHealthToMax(); _player.RestoreMana(_player.maxMana);
            foreach (Actor enemy in _enemies) enemy.gameObject.SetActive(true);
        }
        if (stage == TutorialStage.Reward) StartCoroutine(OfferReward());
        if (stage == TutorialStage.Departure) { _exitLight.SetActive(true); _target.gameObject.SetActive(true); }
        CurrentBeat.onEnter?.Invoke();
        StageChanged?.Invoke(stage);
    }
    private void OnAbilityUsed(int index)
    {
        if (_leaving) return;
        if (Stage == TutorialStage.Dodge && index == 4) Enter(TutorialStage.BasicAttack);
        else if (Stage == TutorialStage.Skill) _skillUsed = index == 0;
    }
    private void OnTargetDamaged(Actor actor, float damage)
    {
        if (damage <= 0 || _leaving) return;
        if (Stage == TutorialStage.BasicAttack && !_abilities.IsCasting)
        {
            if (++_basicHits >= 3) Enter(TutorialStage.Skill);
        }
        else if (Stage == TutorialStage.Skill && _skillUsed && _abilities.IsCasting)
            Enter(TutorialStage.Encounter);
    }
    private void OnEnemyDied(Actor actor)
    {
        if (Stage != TutorialStage.Encounter || _leaving) return;
        _defeated++;
        if (_defeated == _enemies.Length) Enter(TutorialStage.Reward);
    }
    private IEnumerator OfferReward()
    {
        // The final hit may still belong to a casting skill; don't replace its weapon mid-cast.
        while (_abilities && _abilities.IsCasting && !_leaving) yield return null;
        if (_leaving || !_player || _player.IsDead) yield break;
        _boons = _player.GetComponent<RunBoons>() ?? _player.gameObject.AddComponent<RunBoons>();
        _boons.GetComponent<RunRewardUI>().BuildTop = 300f;
        _boons.RewardChosen += OnRewardChosen;
        yield return null;
        if (!_leaving && _player && !_player.IsDead) _boons.OfferReward(1);
    }
    private void OnRewardChosen() { if (!_leaving) Enter(TutorialStage.Departure); }
    private void OnPlayerDied(Actor actor)
    {
        if (_leaving) return;
        _leaving = true;
        SceneManager.LoadSceneAsync(gameObject.scene.name);
    }
    public void Skip() => Leave(false);
    private void Leave(bool completed)
    {
        if (_leaving) return;
        if (!Application.CanStreamedLevelBeLoaded(_lobbyScene))
        { Debug.LogError("Tutorial destination is missing from Build Settings: " + _lobbyScene, this); return; }
        _leaving = true;
        GamePreferences.MarkTutorialSeen();
        if (completed) _onCompleted?.Invoke();
        SceneManager.LoadSceneAsync(_lobbyScene);
    }
    private void OnDestroy()
    {
        if (_abilities) _abilities.AbilityUsed -= OnAbilityUsed;
        if (_player) _player.Died -= OnPlayerDied;
        if (_target) _target.DamageReceived -= OnTargetDamaged;
        if (_boons) _boons.RewardChosen -= OnRewardChosen;
        if (_enemies != null) foreach (Actor enemy in _enemies) if (enemy) enemy.Died -= OnEnemyDied;
    }
}
