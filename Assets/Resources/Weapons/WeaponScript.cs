using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class WeaponScript : ScriptableObject
{
    public string weaponName;
    public GameObject weaponPrefab; // Prefab da arma

    [Header("Attack")]
    [SerializeField] public float attackDamage;
    [SerializeField] public float attackSpeed; // Quanto menor o valor, mais rápido o ataque
    [SerializeField] public float attackDistance;
    [SerializeField] public float attackDelay;
    [SerializeField] public float attackRadius; // Radius para SphereCast
    [SerializeField] public Vector3 attackBoxSize; // Tamanho da hitbox para BoxCast

    [Header("Hit Effects")]
    [SerializeField] public string hitEffectName; // Nome do prefab do efeito de ataque
    [SerializeField] public GameObject hitbox; // Nome do prefab do efeito de ataque

    [Header("Skills")]
    [SerializeField] public Ability[] abilities; // Lista de habilidades desta arma

    public void Attack(Transform handTransform, float attackDamage)
    {
        // Carregue o prefab do efeito de ataque
        ParticleSystem hitEffectPrefab = Resources.Load<ParticleSystem>("Weapons/Melee/Gauntlet/" + hitEffectName);

        // Defina a direção do ataque baseado na orientação do personagem
        Vector3 attackDirection = handTransform.forward;

        // Use BoxCast para detectar inimigos na área do ataque
        RaycastHit[] hits = Physics.BoxCastAll(handTransform.position, attackBoxSize / 2, attackDirection, handTransform.rotation, attackDistance);

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
