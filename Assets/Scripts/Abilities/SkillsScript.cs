using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillsScript : MonoBehaviour
{
    // Adicione uma referência para o script do personagem
    private CharControlScript charControlScript;
    public Transform player;

    // Referência para o PointerEventData
    private PointerEventData pointerEventData;

    // Referência para o objeto de visualização
    public GameObject abilityRangeIndicator;

    // Cooldown
    public float abilityQCooldown = 5f;
    public float abilityWCooldown = 5f;
    public float abilityECooldown = 5f;
    public float abilityRCooldown = 5f;

    // Rastrear quando a habilidade foi usada pela última vez
    private float lastAbilityQTime;
    private float lastAbilityWTime;
    private float lastAbilityETime;
    private float lastAbilityRTime;

    // Referência para o botão
    public Button Q;
    public Button W;
    public Button E;
    public Button R;

    // Start is called before the first frame update
    void Start()
    {
        // Obtenha a referência ao script do personagem
        charControlScript = GetComponent<CharControlScript>();

        // Crie um novo PointerEventData
        pointerEventData = new PointerEventData(EventSystem.current);

    }

    // Update is called once per frame
    void Update()
    {
        if(player.GetComponent<PlayerActor>().mana > 0)
        {
            // Verifique se a tecla foi pressionada
            if (Input.GetKeyDown(KeyCode.Q))
            {
                // Verifique se o cooldown acabou
                if (Time.time >= lastAbilityQTime + abilityQCooldown)
                {
                    // Atualize o tempo da última vez que a habilidade foi usada
                    lastAbilityQTime = Time.time;

                    // Chama a função para a habilidade Q aqui
                    Q.onClick.Invoke();

                    // Simula o pressionamento do botão
                    ExecuteEvents.Execute(Q.gameObject, pointerEventData, ExecuteEvents.pointerDownHandler);

                    charControlScript.animator.Play("QSkill");

                    // Chama a skill
                    PerformQ();
                    // Gasta mana
                    player.GetComponent<PlayerActor>().UseMana(100);

                    // Mostre o objeto de visualização da skill
                    abilityRangeIndicator.SetActive(true);
                }
            
            }

            // Verifica se a tecla foi liberada
            if (Input.GetKeyUp(KeyCode.Q))
            {
                // Simula a liberação do botão
                ExecuteEvents.Execute(Q.gameObject, pointerEventData, ExecuteEvents.pointerUpHandler);

                // Oculta o objeto de visualização da skill
                abilityRangeIndicator.SetActive(false);
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                // Verifica se o cooldown acabou
                if (Time.time >= lastAbilityWTime + abilityWCooldown)
                {
                    // Chama a função para a habilidade W aqui
                    W.onClick.Invoke();
                }
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                // Verifica se o cooldown acabou
                if (Time.time >= lastAbilityETime + abilityECooldown)
                {
                    // Chama a função para a habilidade E aqui
                    E.onClick.Invoke();
                }
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                // Verifica se o cooldown acabou
                if (Time.time >= lastAbilityRTime + abilityRCooldown)
                {
                    // Chama a função para a habilidade R aqui
                    R.onClick.Invoke();
                }
            }

        }
    }
    void PerformQ()
    {
        for (int i = 0; i < 5; i++)
        {
            // Defina a distância do ataque e o raio da área de ataque
            float attackDistance = 1f;
            float attackRadius = 1f;

            // Use um SphereCast na direção que o personagem está olhando para detectar inimigos
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, attackRadius, transform.forward, attackDistance);

            // Itere sobre todos os objetos atingidos
            foreach (RaycastHit hit in hits)
            {
                // Verifique se o objeto atingido é interagivel
                if (hit.transform.CompareTag("Interactable"))
                {
                    // Obtenha o componente Actor
                    Actor actor = hit.transform.GetComponent<Actor>();

                    // Verifique se o componente Actor existe antes de chamar TakeDamage
                    if (actor != null)
                    {
                        Instantiate(charControlScript.hitEffect, actor.transform.position + new Vector3(0, 1, 0), Quaternion.identity);
                        actor.TakeDamage(15);
                    }
                    else
                    {
                        Debug.Log("O objeto atingido não tem um componente Actor!");
                    }
                }
            }
        }
    }
}
