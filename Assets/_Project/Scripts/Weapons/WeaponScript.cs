using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon")]
public class WeaponScript : ScriptableObject
{
    public string weaponName;
    public GameObject weaponPrefab;
    public GameObject pickupPrefab;
    [SerializeField] private bool _firesArrows;
    public bool FiresArrows => _firesArrows;

    [Header("Attack")]
    [SerializeField] public float attackDamage;
    [SerializeField] public float attackSpeed;
    [SerializeField] public float attackDistance;
    [SerializeField] public float attackDelay;
    [SerializeField] public float attackRadius;
    [SerializeField] public Vector3 attackBoxSize;

    [Header("Hit Effects")]
    [SerializeField] public string hitEffectName;
    [SerializeField] public GameObject hitbox;

    [Header("Skills")]
    [SerializeField] public Ability[] abilities;

    [Header("Combat")]
    [SerializeField] private CombatActionProfile _basicProfile;

    public GameObject PickupPrefab => pickupPrefab ? pickupPrefab : weaponPrefab;

    public string HitEffectResourcePath => string.IsNullOrEmpty(hitEffectName)
        ? null
        : $"Weapons/Melee/Gauntlet/{hitEffectName}";

    /// <summary>
    /// The authored <see cref="CombatActionProfile"/> for this weapon's Ataque_Básico (R2.3). May be
    /// <c>null</c> on older assets authored before the profile existed; callers must null-guard.
    /// </summary>
    public CombatActionProfile BasicProfile => _basicProfile;
    [Header("Phase 2 basic reaction")]
    [SerializeField] private int _basicReactionVersion;
    [SerializeField] private bool _overrideBasicReaction;
    [SerializeField] private HitReactionType _basicReaction = HitReactionType.Stagger;
    [SerializeField] private float _basicInterruptContribution = 0.35f;
    [SerializeField] private float[] _basicStageContributions;
    public bool OverridesBasicReaction => _basicReactionVersion >= 1 && _overrideBasicReaction;
    public HitReactionType BasicReaction => _basicReaction;
    public float BasicInterruptContribution(int index) => ReactionNumeric.Safe(
        _basicStageContributions != null && index >= 0 && index < _basicStageContributions.Length
            ? _basicStageContributions[index] : _basicInterruptContribution, 0f, float.MaxValue, 0f);

    /// <summary>
    /// Validates the authored <see cref="BasicProfile"/> in the Inspector without ever mutating it
    /// (R2.3, R3.10, R4.8, R8.7). Invalid phase boundaries or cancel windows are flagged via
    /// <see cref="Debug.LogWarning(object)"/> and the last valid authored values are kept (no silent
    /// mutation). An absent commitment category resolves to <c>Committed</c> with a warning through
    /// the shared <see cref="CombatActionProfile.ResolveCommitment"/> path. A <c>null</c> profile on
    /// older assets is skipped gracefully.
    /// </summary>
    private void OnValidate()
    {
        CombatProfileValidation.Validate(_basicProfile, name);
    }
}
