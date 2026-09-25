using UnityEngine;

[RequireComponent(typeof(Actor))]
[DisallowMultipleComponent]
public sealed class CombatReadabilityUI : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    private Actor _actor;
    private PlayerActor _player;
    private EnemyVariant _variant;
    private CombatGroundRing _slowRing;
    private float _slowMultiplier = 1;
    private GUIStyle _style;

    private void Awake()
    {
        _actor = GetComponent<Actor>();
        _player = _actor as PlayerActor;
        _variant = GetComponent<EnemyVariant>();
        if (!_camera) _camera = Camera.main;
    }

    private void LateUpdate()
    {
        if (!_player) return;
        _slowMultiplier = 1;
        foreach (var modifier in _player.Stats.RuntimeModifiers)
            if (modifier.source is EnemyFrostAura && modifier.statType == PlayerStatType.MovementSpeedMultiplier)
                _slowMultiplier *= modifier.value;
        bool slowed = _slowMultiplier < 0.999f && !_actor.IsDead;
        if (slowed && !_slowRing) _slowRing = CombatGroundRing.Create(transform, "Chilled player", Color.cyan);
        if (_slowRing)
        {
            _slowRing.gameObject.SetActive(slowed);
            if (slowed) _slowRing.Draw(transform.position, 0.65f, 0.1f);
        }
    }

    private void OnGUI()
    {
        if (!_actor || _actor.IsDead || !_camera) return;
        // EnemyCombatFeedback now combines health, rank and affixes above each enemy.
        if (!_player && _actor.GetComponent<EnemyCombatFeedback>()) return;
        string text = _player ? (_slowMultiplier < 0.999f ? $"GELO  −{Mathf.RoundToInt((1 - _slowMultiplier) * 100)}% movimento" : "") :
            _variant && _variant.Profile ? _variant.Profile.Rarity + "\n" + _variant.AffixSummary : "";
        if (string.IsNullOrEmpty(text)) return;
        Vector3 position = _camera.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
        if (position.z < 0) return;
        if (_style == null) _style = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
        _style.normal.textColor = _player ? Color.cyan : _variant.Profile.Rarity == EnemyRarity.Rare ? new Color(1, 0.8f, 0.35f) : Color.white;
        GUI.Box(new Rect(position.x - 130, Screen.height - position.y - 40, 260, 42), text, _style);
    }

    private void OnDisable() { if (_slowRing) _slowRing.gameObject.SetActive(false); }
}
