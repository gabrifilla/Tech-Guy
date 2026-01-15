using System.Reflection;
using UnityEngine;

// Ensures player's current hitbox is configured to deal damage
public class PlayerWeaponBootstrap : MonoBehaviour
{
    void Start()
    {
        var actor = GetComponent<PlayerActor>();
        if (actor == null) return;

        var hitboxField = typeof(PlayerActor).GetField("hitbox", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var weaponField = typeof(PlayerActor).GetField("weapon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var hb = (GameObject)hitboxField?.GetValue(actor);
        var weapon = (WeaponScript)weaponField?.GetValue(actor);
        if (hb == null || weapon == null) return;

        var hbDamage = hb.GetComponent<HitboxDamage>();
        if (hbDamage == null) hbDamage = hb.AddComponent<HitboxDamage>();
        hbDamage.owner = actor;
        hbDamage.damage = weapon.attackDamage;
        hbDamage.hitEffectResourcePath = string.IsNullOrEmpty(weapon.hitEffectName) ? null : $"Weapons/Melee/Gauntlet/{weapon.hitEffectName}";
    }
}

