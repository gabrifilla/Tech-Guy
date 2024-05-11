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

    public bool isDashing = false;

    private void Start()
    {
        // Obtenha a referencia ao NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isDashing)
        {
            // Posição do mouse na tela
            Vector3 mousePosition = Input.mousePosition;

            // Posição do personagem na tela
            Vector3 characterScreenPosition = Camera.main.WorldToScreenPoint(transform.position);

            // Calcula a direção para dar o Dash
            dashDirection = (mousePosition - characterScreenPosition).normalized;
            dashDirection.z = dashDirection.y;
            dashDirection.y = 0;

            // Faz o personagem olhar na direção do Dash
            Quaternion dashRotation = Quaternion.LookRotation(dashDirection);
            transform.rotation = dashRotation;

            StartCoroutine(Dash());
        }
    }

    IEnumerator Dash()
    {
        // Defina isDashing como true no início do dash
        isDashing = true;

        animator.Play("Dash");
        float startTime = Time.time;

        while (Time.time < startTime + dashTime)
        {
            // Move o personagem na direção do dash
            transform.position += dashDirection * dashSpeed * Time.deltaTime;
            yield return null;
        }

        // Faz com que o NavMesh não mova (Personagem para quando chega no ponto final)
        agent.SetDestination(transform.position);

        // Defina isDashing como false no final do dash
        isDashing = false;
    }
}
