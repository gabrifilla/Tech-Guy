using UnityEngine;

/// <summary>
/// Run-only variation layered on an archetype (Etapa 3, 11/11/2026). Appended values only; persisted inside
/// <see cref="ManualEnemySpawn"/>, so never reorder.
/// </summary>
public enum EnemyModifier
{
    None = 0,
    /// <summary>Shooter that fires a 5-shot fan (replaces the SpreadShooter as its own archetype).</summary>
    Leque = 1,
    /// <summary>Shooter "Rajada": laser bursts (placeholder body until the tail model exists).</summary>
    Rajada = 2,
}

/// <summary>Pure rules for which modifier changes what. Kept apart from the MonoBehaviour for tests.</summary>
public static class EnemyModifierRules
{
    /// <summary>Chance that a drawn Shooter becomes "Leque" (1 in 3, developer decision).</summary>
    public const double LequeChance = 1d / 3d;
    /// <summary>Chance that a drawn, non-Leque Shooter becomes the "Rajada" placeholder.</summary>
    public const double RajadaChance = .25d;
    /// <summary>Chance that a Sniper uses the Emissor look, and a Rush the Fragmento look.</summary>
    public const double SkinChance = .5d;

    public static EnemyAttackKind? AttackFor(ArchetypeId? archetype, EnemyModifier modifier)
    {
        if (archetype != ArchetypeId.Shooter) return null;
        switch (modifier)
        {
            case EnemyModifier.Leque: return EnemyAttackKind.SpreadShot;
            case EnemyModifier.Rajada: return EnemyAttackKind.BurstShot;
            default: return null;
        }
    }

    /// <summary>Name shown above the enemy (the old internal ids stay for saves/tests).</summary>
    public static string DisplayName(ArchetypeId? archetype, EnemyModifier modifier, string fallback)
    {
        string name = archetype switch
        {
            ArchetypeId.Fragile => "Volátil",
            ArchetypeId.Mirror => "Guarda",
            ArchetypeId.SpreadShooter => "Shooter · Leque",
            null => fallback,
            _ => archetype.Value.ToString()
        };
        if (modifier == EnemyModifier.Leque && archetype == ArchetypeId.Shooter) name += " · Leque";
        if (modifier == EnemyModifier.Rajada && archetype == ArchetypeId.Shooter) name += " · Rajada";
        return name;
    }
}

/// <summary>Carries the spawn's modifier on the live enemy and draws a small colored ring at its feet.</summary>
[DisallowMultipleComponent]
public sealed class EnemyModifierTag : MonoBehaviour
{
    private static readonly Color LequeColor = new Color(1f, .82f, .2f), RajadaColor = new Color(1f, .2f, .45f);
    private CombatGroundRing _ring;
    public EnemyModifier Modifier { get; private set; }

    public void Configure(EnemyModifier modifier)
    {
        Modifier = modifier;
        if (modifier == EnemyModifier.None) { if (_ring) Destroy(_ring.gameObject); _ring = null; return; }
        if (!_ring) _ring = CombatGroundRing.Create(transform, "Modificador " + modifier, modifier == EnemyModifier.Leque ? LequeColor : RajadaColor);
        _ring.SetColor(modifier == EnemyModifier.Leque ? LequeColor : RajadaColor);
    }

    private void LateUpdate()
    {
        if (_ring) _ring.Draw(transform.position + Vector3.up * .04f, .75f, .09f);
    }
}
