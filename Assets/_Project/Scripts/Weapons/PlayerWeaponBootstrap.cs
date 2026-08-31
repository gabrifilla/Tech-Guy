using UnityEngine;

public class PlayerWeaponBootstrap : MonoBehaviour
{
    private void Start()
    {
        PlayerActor actor = GetComponent<PlayerActor>();
        if (actor == null || actor.CurrentWeapon == null)
        {
            return;
        }

        actor.EquipWeapon(actor.CurrentWeapon);
    }
}
