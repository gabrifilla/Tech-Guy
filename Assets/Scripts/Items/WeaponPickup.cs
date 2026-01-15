using System.Reflection;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public WeaponScript weapon;
    public bool autoPickupOnTrigger = false;
    private bool pickedUp = false;

    public void Pickup(GameObject player)
    {
        if (weapon == null || player == null) { Destroy(gameObject); return; }

        var playerActor = player.GetComponent<PlayerActor>();
        if (playerActor == null) { Destroy(gameObject); return; }

        // Call PlayerActor.EquipWeapon(WeaponScript) even if it's non-public
        var method = typeof(PlayerActor).GetMethod("EquipWeapon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        method?.Invoke(playerActor, new object[] { weapon });

        // Ensure PlayerActor.weapon field reflects the new weapon and fix hitbox configuration
        var weaponField = typeof(PlayerActor).GetField("weapon", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        weaponField?.SetValue(playerActor, weapon);

        var handField = typeof(PlayerActor).GetField("handTransform", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var handTransform = (Transform)handField?.GetValue(playerActor);

        var hitboxField = typeof(PlayerActor).GetField("hitbox", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var currentHitbox = (GameObject)hitboxField?.GetValue(playerActor);
        if (currentHitbox != null)
        {
            Object.Destroy(currentHitbox);
        }

        if (weapon.hitbox != null && handTransform != null)
        {
            var newHitbox = Object.Instantiate(weapon.hitbox, handTransform.position, handTransform.rotation, handTransform);
            newHitbox.SetActive(false);

            var hbDamage = newHitbox.GetComponent<HitboxDamage>();
            if (hbDamage == null) hbDamage = newHitbox.AddComponent<HitboxDamage>();
            hbDamage.owner = playerActor;
            hbDamage.damage = weapon.attackDamage;
            hbDamage.hitEffectResourcePath = string.IsNullOrEmpty(weapon.hitEffectName) ? null : $"Weapons/Melee/Gauntlet/{weapon.hitEffectName}";

            hitboxField?.SetValue(playerActor, newHitbox);
        }

        pickedUp = true;
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoPickupOnTrigger) return;
        if (other.CompareTag("Player"))
        {
            Pickup(other.gameObject);
        }
    }

    private void OnDestroy()
    {
        // Fallback: if destroyed via Interactable.Interact, still equip to the player
        if (pickedUp) return;
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            try { Pickup(player); } catch { /* ignore */ }
        }
    }
}
