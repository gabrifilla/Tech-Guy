using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillsScript : MonoBehaviour
{
    // Adicione uma refer�ncia para o script do personagem
    private CharControlScript charControlScript;

    // Refer�ncia para o PointerEventData
    private PointerEventData pointerEventData;

    // Refer�ncia para o bot�o
    public Button Q;
    public Button W;
    public Button E;
    public Button R;

    // Start is called before the first frame update
    void Start()
    {
        // Obtenha a refer�ncia ao script do personagem
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
            // Chame a fun��o para a habilidade Q aqui
            Q.onClick.Invoke();

            // Simule o pressionamento do bot�o
            ExecuteEvents.Execute(Q.gameObject, pointerEventData, ExecuteEvents.pointerDownHandler);

            Debug.Log("Oi");

            ComboAttack();
        }

        // Verifique se a tecla foi liberada
        if (Input.GetKeyUp(KeyCode.Q))
        {
            // Simule a libera��o do bot�o
            ExecuteEvents.Execute(Q.gameObject, pointerEventData, ExecuteEvents.pointerUpHandler);
        }

        else if (Input.GetKeyDown(KeyCode.W))
        {
            // Chame a fun��o para a habilidade W aqui
            W.onClick.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            // Chame a fun��o para a habilidade E aqui
            E.onClick.Invoke();
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            // Chame a fun��o para a habilidade R aqui
            R.onClick.Invoke();
        }
    }

    void ComboAttack()
    {
        for (int i = 0; i < 5; i++)
        {
            // Espa�o cada ataque por 0.2 segundos
            Invoke("PerformAttack", i * 0.2f);
        }
    }

    void PerformAttack()
    {
        // Defina a dist�ncia do ataque e o raio da �rea de ataque
        float attackDistance = 5f;
        float attackRadius = 2f;

        // Use um SphereCast na dire��o que o personagem est� olhando para detectar inimigos
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, attackRadius, transform.forward, attackDistance);

        // Itere sobre todos os objetos atingidos
        foreach (RaycastHit hit in hits)
        {
            // Verifique se o objeto atingido � interagivel
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
                    Debug.Log("O objeto atingido n�o tem um componente Actor!");
                }
            }
        }
    }
}
