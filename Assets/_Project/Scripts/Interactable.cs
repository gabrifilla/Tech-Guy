using UnityEngine;

public enum InteractableType { Enemy, Item }

public class Interactable : MonoBehaviour
{
    public Actor myActor { get; private set; }

    public InteractableType interactionType;

    private void Awake()
    {
        if (interactionType == InteractableType.Enemy)
        {
            myActor = GetComponent<Actor>();
            if (myActor == null)
            {
                myActor = GetComponentInParent<Actor>();
            }
        }
    }

    public void Interact(GameObject player)
    {
        switch (interactionType)
        {
            case InteractableType.Enemy:
                InteractWithEnemy();
                break;
            case InteractableType.Item:
                InteractWithItem(player);
                break;
            default:
                Debug.LogWarning("Interaction type is not configured.", this);
                break;
        }
    }

    private void InteractWithEnemy()
    {
        if (myActor == null)
        {
            myActor = GetComponentInParent<Actor>();
        }

        if (myActor == null)
        {
            Debug.LogWarning("Enemy actor was not found.", this);
            return;
        }

        if (myActor.healthBar != null)
        {
            myActor.healthBar.gameObject.SetActive(true);
        }
    }

    private void InteractWithItem(GameObject player)
    {
        WeaponPickup weaponPickup = GetComponent<WeaponPickup>();
        if (weaponPickup == null)
        {
            weaponPickup = GetComponentInParent<WeaponPickup>();
        }

        if (weaponPickup != null)
        {
            weaponPickup.Pickup(player);
            return;
        }

        Destroy(gameObject);
    }
}
