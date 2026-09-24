using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyFrostAura : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float _radius = 5f;
    [SerializeField, Range(0f, 0.95f)] private float _slowFraction = 0.3f;
    [SerializeField, Min(0.05f)] private float _scanInterval = 0.15f;
    [SerializeField] private LayerMask _targetLayers = ~0;
    private Actor _owner;
    private Coroutine _routine;
    private readonly HashSet<PlayerActor> _affected = new HashSet<PlayerActor>();
    private readonly HashSet<PlayerActor> _inRange = new HashSet<PlayerActor>();

    private void Awake()
    {
        _owner = GetComponentInParent<Actor>();
        if (!_owner)
        {
            Debug.LogError("EnemyFrostAura must be a child of an Actor.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (!_owner || _owner.IsDead) return;
        _owner.Died += OnOwnerDied;
        _routine = StartCoroutine(Scan());
    }

    private IEnumerator Scan()
    {
        var delay = new WaitForSeconds(Mathf.Max(0.05f, _scanInterval));
        while (!_owner.IsDead)
        {
            _inRange.Clear();
            foreach (Collider hit in Physics.OverlapSphere(transform.position, Mathf.Max(0.1f, _radius),
                _targetLayers, QueryTriggerInteraction.Collide))
            {
                PlayerActor player = hit.GetComponentInParent<PlayerActor>();
                if (player && !player.IsDead && player.isActiveAndEnabled) _inRange.Add(player);
            }
            foreach (PlayerActor player in _affected)
                if (player && !_inRange.Contains(player)) player.Stats.RemoveModifiersFrom(this);
            foreach (PlayerActor player in _inRange)
                if (!_affected.Contains(player))
                    player.Stats.AddModifier(new PlayerStatModifier(PlayerStatType.MovementSpeedMultiplier,
                        PlayerStatModifierMode.MoreMultiplier, 1f - Mathf.Clamp(_slowFraction, 0f, 0.95f)), this);
            _affected.Clear();
            _affected.UnionWith(_inRange);
            yield return delay;
        }
        ClearSlow();
    }

    private void OnOwnerDied(Actor owner) => enabled = false;

    private void OnDisable()
    {
        if (_owner) _owner.Died -= OnOwnerDied;
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        ClearSlow();
    }

    private void ClearSlow()
    {
        foreach (PlayerActor player in _affected)
            if (player) player.Stats.RemoveModifiersFrom(this);
        _affected.Clear();
        _inRange.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, _radius));
    }
}
