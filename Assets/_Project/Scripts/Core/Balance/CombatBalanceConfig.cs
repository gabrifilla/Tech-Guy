using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Central, data-only tuning asset for the combat-balance-tuning feature (Requisito 5). It gathers the
/// weapon-feel knobs — projectile sweep radius, the unified gauntlet basic-hitbox volume, the global
/// skill mana-cost multiplier, the player's starting stats, and the protection-indicator parameters —
/// into a single ScriptableObject a designer can tune iteratively (R5.1, R5.2).
///
/// Every getter reads through <see cref="CombatBalanceBounds"/> so a degenerate authored value can
/// never reach the combat systems (R5.4). The asset lives at
/// <c>Assets/_Project/Resources/CombatBalanceConfig.asset</c> under a fixed name so
/// <see cref="CombatBalance"/> can resolve it with <see cref="Resources.Load"/>; when the asset is
/// absent each consumer falls back to its own local default (R5.3), and the config acts as an
/// override/multiplier rather than rewriting the per-weapon/per-skill assets (R5.5).
/// </summary>
/// <remarks>Feature: combat-balance-tuning, task 1.2. Requirements: 5.1, 5.2, 5.3.</remarks>
[CreateAssetMenu(menuName = "Tech Guy/Combat/Combat Balance Config")]
public sealed class CombatBalanceConfig : ScriptableObject
{
    [Header("Arco — projétil (R1)")]
    [Tooltip("Raio do SphereCastAll do ArsenalProjectile, em metros. Lido com clamp [0.05; 1.0].")]
    [SerializeField] private float projectileSweepRadius = 0.35f;

    [Header("Manopla — golpe básico (R2)")]
    [Tooltip("Caixa frontal unificada do básico da manopla. Cada componente é lido com clamp > 0.")]
    [SerializeField] private Vector3 gauntletBasicBoxSize = new Vector3(1.6f, 1.9f, 1.9f);

    [Tooltip("Alcance efetivo do básico da manopla, alinhado a attackDistance (R2.4). Lido com clamp > 0.")]
    [SerializeField] private float gauntletBasicReach = 2.2f;

    [Header("Manopla — skills (R3)")]
    [Tooltip("Multiplica o custo de mana das skills da manopla. Lido com clamp [0.1; 2.0].")]
    [SerializeField] private float skillManaCostMultiplier = 0.7f;

    [Header("Stats iniciais do player (R4)")]
    [Tooltip("Override de PlayerArpgStats.baseDamage. 0 (ou omitido) mantém o valor autorado.")]
    [SerializeField] private float startingBaseDamage = 5f;

    [Tooltip("Override de armor. Lido com clamp >= 0.")]
    [SerializeField] private float startingArmor = 0f;

    [Tooltip("Override da vida base. 0 mantém o valor autorado no prefab (R4.3); qualquer valor > 0 é lido com clamp > 0.")]
    [SerializeField] private float startingHealth = 0f;

    [Header("Indicador de proteção (R6)")]
    [Tooltip("Liga o ícone de escudo acima da cabeça do inimigo protegido (R6.1).")]
    [SerializeField] private bool protectionIconEnabled = true;

    [Tooltip("Cor da aura/ícone de proteção (R6.7).")]
    [SerializeField] private Color protectionAuraColor = new Color(0.4f, 0.9f, 1f, 1f);

    /// <summary>
    /// Projectile sweep radius clamped to [0.05; 1.0] m (R1). Feeds the <c>SphereCastAll</c> radius of
    /// the arrow so it can never collapse to a point nor punch through solid scenery.
    /// </summary>
    public float ProjectileSweepRadius => CombatBalanceBounds.ClampProjectileSweepRadius(projectileSweepRadius);

    /// <summary>
    /// Unified gauntlet basic-attack box size with every component clamped strictly positive (R2), so
    /// the volume always has a real extent on all three axes.
    /// </summary>
    public Vector3 GauntletBasicBoxSize => CombatBalanceBounds.ClampHitboxSize(gauntletBasicBoxSize);

    /// <summary>Effective reach of the gauntlet basic attack, clamped strictly positive (R2.4).</summary>
    public float GauntletBasicReach => CombatBalanceBounds.ClampHitboxDimension(gauntletBasicReach);

    /// <summary>
    /// Global skill mana-cost multiplier clamped to [0.1; 2.0] (R3.1), keeping the override from
    /// zeroing skill costs or making them punitive.
    /// </summary>
    public float SkillManaCostMultiplier => CombatBalanceBounds.ClampSkillManaCostMultiplier(skillManaCostMultiplier);

    /// <summary>
    /// Starting base-damage override. Returns the authored value untouched; a value of 0 means
    /// "use the player's own default" (R4.3) and is not clamped away.
    /// </summary>
    public float StartingBaseDamage => startingBaseDamage;

    /// <summary>Starting armor override clamped non-negative (R4), keeping the mitigation formula well defined.</summary>
    public float StartingArmor => CombatBalanceBounds.ClampArmor(startingArmor);

    /// <summary>
    /// Starting base-health override. 0 keeps the prefab-authored health (R4.3); any positive value is
    /// clamped strictly &gt; 0 so the player never spawns with a degenerate health pool (R4.4).
    /// </summary>
    public float StartingHealth => startingHealth <= 0f ? 0f : CombatBalanceBounds.ClampBaseHealth(startingHealth);

    /// <summary>Whether the shield icon above protected enemies is enabled (R6.1).</summary>
    public bool ProtectionIconEnabled => protectionIconEnabled;

    /// <summary>Color of the protection aura/icon (R6.7).</summary>
    public Color ProtectionAuraColor => protectionAuraColor;
}

/// <summary>
/// Static accessor to the single <see cref="CombatBalanceConfig"/> asset. Resolves it once via
/// <c>Resources.Load&lt;CombatBalanceConfig&gt;("CombatBalanceConfig")</c> and caches the result; when
/// the asset is missing it returns <c>null</c> so each consumer falls back to its own local default
/// without breaking the combat (R5.3).
///
/// The cache is reset at the start of every play through
/// <see cref="RuntimeInitializeOnLoadMethodAttribute"/> (<c>BeforeSceneLoad</c>), and in the Editor on
/// domain reload, so a stale reference never leaks between play sessions.
/// </summary>
/// <remarks>Feature: combat-balance-tuning, task 1.2. Requirements: 5.1, 5.2, 5.3.</remarks>
public static class CombatBalance
{
    /// <summary>Fixed resource name (no extension) the asset must use so <see cref="Resources.Load"/> can find it.</summary>
    public const string ResourceName = "CombatBalanceConfig";

    private static CombatBalanceConfig _cached;
    private static bool _resolved;

    /// <summary>
    /// The loaded <see cref="CombatBalanceConfig"/>, or <c>null</c> when no asset exists. The lookup runs
    /// once and is cached; consumers must treat <c>null</c> as "use local defaults" (R5.3).
    /// </summary>
    public static CombatBalanceConfig Current
    {
        get
        {
            if (!_resolved)
            {
                _cached = Resources.Load<CombatBalanceConfig>(ResourceName);
                _resolved = true;
            }

            return _cached;
        }
    }

    /// <summary>Clears the cached config so the next <see cref="Current"/> read re-resolves the asset.</summary>
    public static void ResetCache()
    {
        _cached = null;
        _resolved = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetCacheOnPlay() => ResetCache();

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void ResetCacheOnEditorReload() => ResetCache();
#endif
}
