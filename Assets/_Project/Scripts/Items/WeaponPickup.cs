using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponScript weapon;
    [SerializeField] private bool autoPickupOnTrigger = false;
    [SerializeField] private Transform visualContainer;
    [SerializeField] private bool replaceVisualOnSwap = true;
    [SerializeField] private bool hideOriginalVisualOnSwap = true;

    public WeaponScript Weapon => weapon;

    private Renderer[] originalRenderers;
    private GameObject spawnedVisual;

    private void Awake()
    {
        if (!visualContainer)
        {
            visualContainer = transform;
        }

        originalRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public void Pickup(GameObject player)
    {
        if (player == null)
        {
            return;
        }

        PlayerActor playerActor = player.GetComponent<PlayerActor>();
        if (!playerActor)
        {
            playerActor = player.GetComponentInParent<PlayerActor>();
        }

        if (!playerActor)
        {
            playerActor = player.GetComponentInChildren<PlayerActor>();
        }

        if (playerActor == null)
        {
            return;
        }

        if (weapon == null)
        {
            Destroy(gameObject);
            return;
        }

        WeaponScript droppedWeapon = playerActor.EquipWeapon(weapon);
        if (droppedWeapon == null || droppedWeapon == weapon)
        {
            Destroy(gameObject);
            return;
        }

        SetWeapon(droppedWeapon);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoPickupOnTrigger) return;
        if (other.CompareTag("Player"))
        {
            Pickup(other.gameObject);
        }
    }

    private void SetWeapon(WeaponScript newWeapon)
    {
        weapon = newWeapon;

        if (!replaceVisualOnSwap)
        {
            return;
        }

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (spawnedVisual)
        {
            Destroy(spawnedVisual);
        }

        GameObject pickupPrefab = weapon ? weapon.PickupPrefab : null;
        SetOriginalVisualVisible(!hideOriginalVisualOnSwap || pickupPrefab == null);

        if (!pickupPrefab || !visualContainer)
        {
            return;
        }

        spawnedVisual = Instantiate(pickupPrefab, visualContainer);
        spawnedVisual.transform.localPosition = Vector3.zero;
        spawnedVisual.transform.localRotation = Quaternion.identity;
        spawnedVisual.transform.localScale = Vector3.one;
    }

    private void SetOriginalVisualVisible(bool isVisible)
    {
        if (originalRenderers == null) return;

        foreach (Renderer renderer in originalRenderers)
        {
            if (renderer)
            {
                renderer.enabled = isVisible;
            }
        }
    }
}
