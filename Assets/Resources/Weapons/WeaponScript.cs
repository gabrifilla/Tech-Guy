using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

[CreateAssetMenu]
public class WeaponScript : ScriptableObject
{
    public string weaponName;
    public GameObject weaponPrefab; // Prefab da arma

    [Header("Attack")]
    [SerializeField] public float attackDamage;
    [SerializeField] public float attackSpeed;
    [SerializeField] public float attackDistance;
    [SerializeField] public float attackDelay;
    [SerializeField] public float attackRadius;
    [SerializeField] public float attackArea;
    [SerializeField] public int maxComboCount = 3;

    [Header("Hit Effects")]
    [SerializeField] public string hitEffectName; // Nome do prefab do efeito de ataque

    [Header("Skills")]
    [SerializeField] public Ability[] abilities; // Lista de habilidades desta arma


    public void Attack(Transform handTransform, float attackDamage)
    {
        // Carregue o prefab do efeito de ataque
        ParticleSystem hitEffectPrefab = Resources.Load<ParticleSystem>("Weapons/Melee/Gauntlet/" + hitEffectName);

        // Use um SphereCast na direção que o personagem está olhando para detectar inimigos
        RaycastHit[] hits = Physics.SphereCastAll(handTransform.position, 1f, handTransform.forward, 1f);

        // Itere sobre todos os objetos atingidos
        foreach (RaycastHit hit in hits)
        {
            // Verifique se o objeto atingido é um inimigo
            if (hit.transform.CompareTag("Interactable"))
            {
                // Obtenha o componente Actor
                Actor actor = hit.transform.GetComponent<Actor>();

                // Verifique se o componente Actor existe antes de chamar TakeDamage
                if (actor != null)
                {
                    Instantiate(hitEffectPrefab, actor.transform.position + new Vector3(0, 1, 0), Quaternion.identity);
                    actor.TakeDamage(attackDamage);
                }
            }
        }
    }
}
