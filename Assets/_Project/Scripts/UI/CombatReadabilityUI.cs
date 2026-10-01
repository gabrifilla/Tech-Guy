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

    // Dirty-tracking (R2.4/R2.6): OnGUI used to rebuild the label string every call and recompute
    // WorldToScreenPoint on every OnGUI pass (OnGUI runs at least twice per frame — Layout + Repaint).
    // Reuse the shared pure dirty-tracker (UiValueCache, task 7.1) so the label is rebuilt only when
    // its source value changes, and compute the screen point at most once per frame. On-screen
    // positioning stays frame-accurate (the point still tracks the transform/camera every frame) and
    // the drawn text is byte-for-byte identical.
    private string _label = "";
    // Player branch source: the rounded slow percentage (int.MinValue encodes "not slowed" / no label).
    private readonly UiValueCache<int> _slowPercent = new UiValueCache<int>();
    // Enemy branch source: the rarity + affix summary pair; rebuild when either differs.
    private readonly UiValueCache<(EnemyRarity rarity, string summary)> _enemyLabel = new UiValueCache<(EnemyRarity, string)>();
    private int _screenFrame = -1;
    private Vector3 _screenPoint;

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
        string text = BuildLabel();
        if (string.IsNullOrEmpty(text)) return;
        Vector3 position = ScreenPoint();
        if (position.z < 0) return;
        if (_style == null) _style = new GUIStyle(GUI.skin.box) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
        _style.normal.textColor = _player ? Color.cyan : _variant.Profile.Rarity == EnemyRarity.Rare ? new Color(1, 0.8f, 0.35f) : Color.white;
        GUI.Box(new Rect(position.x - 130, Screen.height - position.y - 40, 260, 42), text, _style);
    }

    // Rebuilds the label string only when its source value changes (R2.4): the player's rounded slow
    // percentage, or the enemy's rarity + affix summary. Returns the exact same text as the inline
    // interpolation did for any given state (R2.6).
    private string BuildLabel()
    {
        if (_player)
        {
            int percent = _slowMultiplier < 0.999f ? Mathf.RoundToInt((1 - _slowMultiplier) * 100) : int.MinValue;
            if (_slowPercent.HasChanged(percent))
                _label = percent == int.MinValue ? "" : $"GELO  −{percent}% movimento";
            return _label;
        }
        if (_variant && _variant.Profile)
        {
            var source = (_variant.Profile.Rarity, _variant.AffixSummary);
            if (_enemyLabel.HasChanged(source))
                _label = source.Item1 + "\n" + source.Item2;
            return _label;
        }
        return "";
    }

    // Computes WorldToScreenPoint at most once per frame (R2.4): OnGUI fires multiple times per frame
    // (Layout + Repaint), and this collapses those to a single projection. The point still tracks the
    // transform and camera every frame, so on-screen positioning is unchanged (R2.6).
    private Vector3 ScreenPoint()
    {
        if (_screenFrame != Time.frameCount)
        {
            _screenFrame = Time.frameCount;
            _screenPoint = _camera.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
        }
        return _screenPoint;
    }

    private void OnDisable() { if (_slowRing) _slowRing.gameObject.SetActive(false); }
}
