using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public float dashSpeed;
    public float dashTime;
    public float rotationSpeed = 10f;  //Velocidade de rota��o

    Vector3 dashDirection;

    // Adicione uma referencia ao NavMeshAgent
    private UnityEngine.AI.NavMeshAgent agent;

    Animator animator;

    public bool isDashing = false;

    private void Start()
    {
        // Obtenha a referencia ao NavMeshAgent
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

     //Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isDashing)
        {
             //Posi��o do mouse na tela
            Vector3 mousePosition = Input.mousePosition;

             //Posi��o do personagem na tela
            Vector3 characterScreenPosition = Camera.main.WorldToScreenPoint(transform.position);

             //Calcula a dire��o para dar o Dash
            dashDirection = (mousePosition - characterScreenPosition).normalized;
            dashDirection.z = dashDirection.y;
            dashDirection.y = 0;

             //Faz o personagem olhar na dire��o do Dash
            Quaternion dashRotation = Quaternion.LookRotation(dashDirection);
            transform.rotation = dashRotation;

            StartCoroutine(Dash());
        }
    }

    IEnumerator Dash()
    {
         //Defina isDashing como true no in�cio do dash
        isDashing = true;

        animator.Play("Dash");
        float startTime = Time.time;

        while (Time.time < startTime + dashTime)
        {
            // Move o personagem na dire��o do dash
            transform.position += dashDirection * dashSpeed * Time.deltaTime;
            yield return null;
        }

        // Faz com que o NavMesh n�o mova (Personagem para quando chega no ponto final)
        agent.SetDestination(transform.position);

        // Defina isDashing como false no final do dash
        isDashing = false;
    }
}
