using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DashScript : MonoBehaviour
{
    public float dashSpeed;
    public float dashTime;
    public float rotationSpeed = 10f; // Velocidade de rotação

    Vector3 dashDirection;

    // Adicione uma referencia ao NavMeshAgent
    private NavMeshAgent agent;

    Animator animator;

    private void Start()
    {
        // Obtenha a referencia ao NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Posição do mouse no mundo
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Calcula a direção para dar o Dash
                dashDirection = (hit.point - transform.position);
                dashDirection.y = 0; // Zera o y
                dashDirection = dashDirection.normalized;

                // Faz o personagem olhar na direção do Dash
                Quaternion dashRotation = Quaternion.LookRotation(dashDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, dashRotation, Time.deltaTime * rotationSpeed);

                //animator.Play("Dash");
                StartCoroutine(Dash());
            }
        }
    }

    IEnumerator Dash()
    {
        float startTime = Time.time;

        while (Time.time < startTime + dashTime)
        {
            // Move o personagem na direção do dash
            transform.position += dashDirection * dashSpeed * Time.deltaTime;
            yield return null;
        }

        // Faz com que o NavMesh não mova (Personagem para quando chega no ponto final)
        agent.SetDestination(transform.position);
    }
}
