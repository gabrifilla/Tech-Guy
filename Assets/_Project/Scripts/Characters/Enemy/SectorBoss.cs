using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Final-room boss. Every hit uses the exact circle shown during its windup.</summary>
[RequireComponent(typeof(Actor), typeof(EnemyAI), typeof(CombatReactionController))]
[DisallowMultipleComponent]
public sealed class SectorBoss : MonoBehaviour
{
    [SerializeField] private PlayerActor _player;
    [SerializeField, Min(1)] private float _maximumHealth = 1800;
    [SerializeField, Min(.2f)] private float _slamWindup = 1.2f;
    [SerializeField, Min(.2f)] private float _bombWindup = 1.1f;
    [SerializeField, Min(1f)] private float _slamDamage = 30;
    [SerializeField, Min(1f)] private float _bombDamage = 22;
    private Actor _actor;
    private NavMeshAgent _agent;
    private Animator _animator;
    private CombatGroundRing _boundary, _progress;
    private Coroutine _routine;
    public string DisplayName => "GUARDIÃO DO NÚCLEO";
    public float MaximumHealth => _maximumHealth;
    public bool IsEnraged => _actor && _actor.health <= _actor.maxHealth * .5f;
    public bool IsTelegraphing { get; private set; }
    public Vector3 AttackCenter { get; private set; }
    public float AttackRadius { get; private set; }
    public int ResolvedAttacks { get; private set; }
    public void Configure(PlayerActor player) => _player = player;

    private void Awake()
    {
        _actor = GetComponent<Actor>(); _agent = GetComponent<NavMeshAgent>(); _animator = GetComponent<Animator>();
        GetComponent<EnemyAI>().enabled = false;
        _actor.SetMaxHealth(_maximumHealth);
        _actor.Died += OnDeath;
        if (TryGetComponent(out CombatReactionController reaction)) reaction.ConfigureRank(EnemyRank.Boss);
    }
    private void Start()
    {
        if (!_player) { Debug.LogError("SectorBoss requires its encounter player.", this); enabled = false; return; }
        if (_agent && _agent.enabled && _agent.isOnNavMesh) { _agent.ResetPath(); _agent.isStopped = true; }
        _routine = StartCoroutine(Fight());
    }
    private IEnumerator Fight()
    {
        yield return new WaitForSeconds(1f);
        while (_player && !_player.IsDead && !_actor.IsDead)
        {
            yield return Telegraph(transform.position, 4.5f, _slamWindup, _slamDamage);
            yield return new WaitForSeconds(IsEnraged ? .7f : 1.1f);
            int shots = IsEnraged ? 2 : 1;
            for (int i = 0; i < shots && _player && !_player.IsDead; i++)
            {
                yield return Telegraph(_player.transform.position, 2.6f, _bombWindup, _bombDamage);
                yield return new WaitForSeconds(.45f);
            }
            yield return new WaitForSeconds(IsEnraged ? .7f : 1.6f);
        }
    }
    private IEnumerator Telegraph(Vector3 center, float radius, float windup, float damage)
    {
        if (!_player || _player.IsDead || _actor.IsDead) yield break;
        AttackCenter = center; AttackRadius = radius; IsTelegraphing = true;
        Vector3 direction = _player.transform.position - transform.position; direction.y = 0;
        if (direction.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(direction);
        _boundary = CombatGroundRing.Create(null,"Boss danger boundary",new Color(1,.16f,.08f));
        _progress = CombatGroundRing.Create(null,"Boss impact countdown",new Color(1,.75f,.15f));
        _boundary.Draw(center,radius,.15f);
        float duration = windup * (IsEnraged ? .8f : 1f);
        for (float elapsed=0; elapsed<duration; elapsed+=Time.deltaTime)
        {
            if (!_player || _player.IsDead || _actor.IsDead) { ClearTelegraph(); yield break; }
            _progress.Draw(center,radius*Mathf.Clamp01(elapsed/duration),.18f);
            yield return null;
        }
        ClearTelegraph();
        if (_animator && _animator.HasState(0,Animator.StringToHash("Attack"))) _animator.Play("Attack",0,0);
        var impact = CombatGroundRing.Create(null,"Boss impact",new Color(1,.6f,.15f));
        impact.Draw(center,radius,.3f); Destroy(impact.gameObject,.25f);
        ResolvedAttacks++;
        if (_player && !_player.IsDead && Contains(_player.transform.position,center,radius)) _player.TakeDamage(damage);
    }
    public static bool Contains(Vector3 point, Vector3 center, float radius)
    {
        Vector3 delta = point-center;
        return Mathf.Abs(delta.y)<3f && new Vector2(delta.x,delta.z).sqrMagnitude <= radius*radius;
    }
    private void ClearTelegraph()
    {
        IsTelegraphing = false;
        if (_boundary) { _boundary.gameObject.SetActive(false); Destroy(_boundary.gameObject); }
        if (_progress) { _progress.gameObject.SetActive(false); Destroy(_progress.gameObject); }
    }
    private void OnDeath(Actor actor) => StopFight();
    private void OnDisable() => StopFight();
    private void StopFight()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null; ClearTelegraph();
    }
    private void OnDestroy() { if (_actor) _actor.Died -= OnDeath; }
}
