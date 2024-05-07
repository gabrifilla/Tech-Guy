using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillsScript : MonoBehaviour
{
    // Adicione uma referência para o script do personagem
    private CharControlScript charControlScript;

    // Referência para o PointerEventData
    private PointerEventData pointerEventData;

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
        // Verifique se a tecla foi pressionada
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Chame a função para a habilidade Q aqui
            Q.onClick.Invoke();

            // Simule o pressionamento do botão
            ExecuteEvents.Execute(Q.gameObject, pointerEventData, ExecuteEvents.pointerDownHandler);

            Debug.Log("Oi");

            ComboAttack();
        }

        // Verifique se a tecla foi liberada
        if (Input.GetKeyUp(KeyCode.Q))
        {
            // Simule a liberação do botão
            ExecuteEvents.Execute(Q.gameObject, pointerEventData, ExecuteEvents.pointerUpHandler);
        }

        else if (Input.GetKeyDown(KeyCode.W))
        {
            // Chame a função para a habilidade W aqui
            W.onClick.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            // Chame a função para a habilidade E aqui
            E.onClick.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            // Chame a função para a habilidade R aqui
            R.onClick.Invoke();
        }
    }

    void ComboAttack()
    {
        for (int i = 0; i < 5; i++)
        {
            // Espaço cada ataque por 0.2 segundos
            Invoke("PerformAttack", i * 0.2f);
        }
    }

    void PerformAttack()
    {
        // Defina a distância do ataque e o raio da área de ataque
        float attackDistance = 5f;
        float attackRadius = 2f;

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
