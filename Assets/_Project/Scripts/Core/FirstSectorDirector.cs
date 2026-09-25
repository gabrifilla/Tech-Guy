using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-owned finite encounters. Walking into the extraction ring completes the run.</summary>
public sealed class FirstSectorDirector : MonoBehaviour
{
    [Serializable]
    public sealed class Encounter
    {
        public string title;
        public Transform center;
        public Actor[] enemies;
        [NonSerialized] public bool started;
        [NonSerialized] public bool cleared;
    }

    [SerializeField] private PlayerActor _player;
    [SerializeField] private Encounter[] _encounters;
    [SerializeField] private Transform _exit;
    [SerializeField] private GameObject _exitGlow;
    [SerializeField] private TMP_Text _objective;
    [SerializeField] private string _returnScene = "NexusLobby";
    private int _current;
    private bool _leaving;
    private RunBoons _boons;
    private EncounterGates[] _gates;
    [SerializeField] private Vector2[] _roomSizes = { new Vector2(22,18), new Vector2(24,20), new Vector2(26,22) };
    public int ClearedEncounters => _current;
    public bool IsComplete => _encounters != null && _current == _encounters.Length && (!_boons || !_boons.IsChoosing);
    public RunBoons Boons => _boons;
    public bool IsExitLocked(int room) => _gates != null && room >= 0 && room < _gates.Length && _gates[room].ExitLocked;

    public void Configure(PlayerActor player, Encounter[] encounters, Transform exit, GameObject glow, TMP_Text objective)
    {
        _player = player; _encounters = encounters; _exit = exit; _exitGlow = glow; _objective = objective;
    }

    private void Awake()
    {
        if (!_player || !_exit || !_exitGlow || !_objective || _encounters == null || _encounters.Length == 0)
        {
            Debug.LogError("First sector requires player, encounters, exit and objective references.", this);
            enabled = false;
            return;
        }
        foreach (Encounter encounter in _encounters)
        {
            if (!encounter.center || encounter.enemies == null || encounter.enemies.Length == 0)
            {
                Debug.LogError("First sector has an invalid encounter.", this);
                enabled = false; return;
            }
            foreach (Actor enemy in encounter.enemies)
                if (enemy) { enemy.Died += OnEnemyDied; enemy.gameObject.SetActive(false); }
        }
        _player.Died += OnPlayerDied;
        Actor guardian = _encounters[_encounters.Length - 1].enemies[0];
        if (guardian)
        {
            var boss = guardian.GetComponent<SectorBoss>() ?? guardian.gameObject.AddComponent<SectorBoss>();
            boss.Configure(_player);
            _encounters[_encounters.Length - 1].title = "Derrote o Guardião do Núcleo";
        }
        _boons = _player.GetComponent<RunBoons>() ?? _player.gameObject.AddComponent<RunBoons>();
        _boons.RewardChosen += OnRewardChosen;
        _gates = new EncounterGates[_encounters.Length];
        for (int i = 0; i < _encounters.Length; i++)
        {
            var room = new GameObject("Room " + (i + 1) + " gates");
            room.transform.SetParent(transform, false);
            _gates[i] = room.AddComponent<EncounterGates>();
            _gates[i].Configure(_encounters[i].center.position,
                i < _roomSizes.Length ? _roomSizes[i] : new Vector2(26,22));
        }
        _gates[0].OpenEntrance();
        _exitGlow.SetActive(false);
        RefreshObjective();
        StartCoroutine(CheckProximity());
    }

    private IEnumerator CheckProximity()
    {
        var interval = new WaitForSeconds(.15f);
        while (!_leaving && _player && !_player.IsDead)
        {
            if (_boons.IsChoosing) { yield return interval; continue; }
            if (IsComplete)
            {
                if (HorizontalDistance(_player.transform.position, _exit.position) < 1.5f) ReturnToLobby();
            }
            else
            {
                Encounter encounter = _encounters[_current];
                if (!encounter.started && HorizontalDistance(_player.transform.position, encounter.center.position) < 7)
                {
                    encounter.started = true;
                    _gates[_current].Seal();
                    foreach (Actor enemy in encounter.enemies) if (enemy) enemy.gameObject.SetActive(true);
                    RefreshObjective();
                }
            }
            yield return interval;
        }
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a,b); }

    private void OnEnemyDied(Actor actor)
    {
        if (_current >= _encounters.Length || _leaving || !_player || _player.IsDead || _boons.IsChoosing) return;
        Encounter encounter = _encounters[_current];
        if (!encounter.started || Array.IndexOf(encounter.enemies, actor) < 0) return;
        foreach (Actor enemy in encounter.enemies) if (enemy && !enemy.IsDead) { RefreshObjective(); return; }
        encounter.cleared = true;
        _current++;
        // Each room grants one choice; the next room remains sealed until it is claimed.
        if (_player && !_player.IsDead)
        {
            _player.RestoreMana(_player.maxMana * .2f);
        }
        _boons.OfferReward(_current);
        RefreshObjective();
    }

    private void OnRewardChosen()
    {
        _gates[_current - 1].OpenExit();
        if (_current < _gates.Length) _gates[_current].OpenEntrance();
        _exitGlow.SetActive(IsComplete);
        RefreshObjective();
    }

    private void RefreshObjective()
    {
        if (_boons && _boons.IsChoosing) { _objective.text = "SALA CONCLUÍDA\nEscolha uma bênção para liberar a passagem"; return; }
        if (IsComplete) { _objective.text = "SETOR PURIFICADO\nEntre no portal dourado para voltar ao Nexus"; return; }
        Encounter encounter = _encounters[_current];
        int remaining = 0;
        foreach (Actor enemy in encounter.enemies) if (enemy && !enemy.IsDead) remaining++;
        _objective.text = $"SETOR 01  /  MEMORIA CORROMPIDA\nSALA {_current + 1}/{_encounters.Length}  |  {encounter.title}  |  " +
            (encounter.started ? $"{remaining} inimigos" : "Siga as luzes azuis");
    }

    private void OnPlayerDied(Actor actor)
    {
        _objective.text = "CONEXAO PERDIDA\nRetornando ao Nexus...";
        StartCoroutine(ReturnAfterDeath());
    }
    private IEnumerator ReturnAfterDeath() { yield return new WaitForSeconds(2.5f); ReturnToLobby(); }
    private void ReturnToLobby()
    {
        if (_leaving) return;
        if (!Application.CanStreamedLevelBeLoaded(_returnScene))
        { Debug.LogError("First sector return scene is missing from Build Settings.", this); enabled = false; return; }
        _leaving = true;
        SceneManager.LoadSceneAsync(_returnScene);
    }
    private void OnDestroy()
    {
        if (_boons) _boons.RewardChosen -= OnRewardChosen;
        if (_player) _player.Died -= OnPlayerDied;
        if (_encounters != null) foreach (Encounter encounter in _encounters)
            if (encounter.enemies != null) foreach (Actor enemy in encounter.enemies)
                if (enemy) enemy.Died -= OnEnemyDied;
    }
}
