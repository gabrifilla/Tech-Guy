using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponScript weapon;
    [SerializeField] private bool autoPickupOnTrigger = false;

    public WeaponScript Weapon => weapon;

    public void Pickup(GameObject player)
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        PlayerActor playerActor = player.GetComponent<PlayerActor>();
        if (playerActor == null)
        {
            Destroy(gameObject);
            return;
        }

        if (weapon != null)
        {
            playerActor.EquipWeapon(weapon);
        }

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
}
