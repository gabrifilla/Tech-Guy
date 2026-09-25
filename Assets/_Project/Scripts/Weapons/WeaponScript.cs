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

    public GameObject PickupPrefab => pickupPrefab ? pickupPrefab : weaponPrefab;

    public string HitEffectResourcePath => string.IsNullOrEmpty(hitEffectName)
        ? null
        : $"Weapons/Melee/Gauntlet/{hitEffectName}";
}
