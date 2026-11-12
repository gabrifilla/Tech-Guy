using UnityEngine;

/// <summary>
/// Development aid (11/11/2026): every enemy in the encounter shows its archetype name above it, coloured by role, so
/// the developer can tell who is who while all archetypes still share the same placeholder model. Editor-only, on by
/// default (menu: Tech Guy ▸ Protótipo ▸ Nomes dos inimigos); builds never draw it.
/// </summary>
public sealed partial class ProgressionDirector
{
    public const string EnemyLabelsPrefKey = "TechGuy.Debug.EnemyNames";
    private static readonly Color MeleeLabel = new Color(.95f, .95f, .95f), RangedLabel = new Color(1f, .62f, .25f),
        ControlLabel = new Color(.78f, .55f, 1f), SupportLabel = new Color(.45f, .95f, .55f);
    private GUIStyle _enemyLabelStyle, _enemyLabelShadow;

    private static bool EnemyLabelsEnabled
    {
        get
        {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(EnemyLabelsPrefKey, true);
#else
            return false;
#endif
        }
    }

    private void OnGUI()
    {
        if (!EnemyLabelsEnabled || _manualActors == null || _manualActors.Count == 0 || Event.current.type != EventType.Repaint) return;
        var camera = Camera.main;
        if (!camera) return;
        _enemyLabelStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        _enemyLabelShadow ??= new GUIStyle(_enemyLabelStyle);
        int size = Mathf.Max(11, Mathf.RoundToInt(Screen.height / 62f));
        _enemyLabelStyle.fontSize = _enemyLabelShadow.fontSize = size;
        _enemyLabelShadow.normal.textColor = new Color(0, 0, 0, .85f);
        foreach (var actor in _manualActors)
        {
            if (!actor || actor.IsDead || !actor.gameObject.activeInHierarchy) continue;
            var variant = actor.GetComponent<EnemyVariant>();
            var reaction = actor.GetComponent<CombatReactionController>();
            string name = variant && variant.ArchetypeId is ArchetypeId id ? id.ToString() : actor.name.Replace("(Clone)", "");
            if (reaction && reaction.Rank != EnemyRank.Normal) name += " · " + reaction.Rank;
            float height = 1.2f;
            var collider = actor.GetComponent<Collider>();
            if (collider) height = collider.bounds.max.y - actor.transform.position.y + .35f;
            Vector3 screen = camera.WorldToScreenPoint(actor.transform.position + Vector3.up * height);
            if (screen.z <= 0) continue;
            var rect = new Rect(screen.x - 90, Screen.height - screen.y - size, 180, size * 1.6f);
            _enemyLabelStyle.normal.textColor = LabelColor(variant ? variant.ArchetypeId : null);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), name, _enemyLabelShadow);
            GUI.Label(rect, name, _enemyLabelStyle);
        }
    }

    private static Color LabelColor(ArchetypeId? archetype)
    {
        switch (archetype)
        {
            case ArchetypeId.Shooter: case ArchetypeId.SpreadShooter: case ArchetypeId.Sniper: case ArchetypeId.Bomber: return RangedLabel;
            case ArchetypeId.HazardCaster: case ArchetypeId.Hooker: case ArchetypeId.Spawner: return ControlLabel;
            case ArchetypeId.Healer: case ArchetypeId.ShieldSupport: return SupportLabel;
            default: return MeleeLabel;
        }
    }
}
