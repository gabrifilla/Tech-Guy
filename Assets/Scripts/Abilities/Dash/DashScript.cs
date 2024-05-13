using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CreateAssetMenu]
public class DashScript : Ability
{
    public float dashVelocity;
    public float dashTime;
    public string dashAnimation = "Dash";

    public bool isDashing = false;

    Vector3 dashDirection;

    public override void Activate(GameObject parent)
    {
        Vector3 mousePosition = Input.mousePosition;

        //Posição do personagem na tela
        Vector3 characterScreenPosition = Camera.main.WorldToScreenPoint(parent.transform.position);

        //Calcula a direção para dar o Dash
        dashDirection = (mousePosition - characterScreenPosition).normalized;
        dashDirection.z = dashDirection.y;
        dashDirection.y = 0;

        //Faz o personagem olhar na direção do Dash
        Quaternion dashRotation = Quaternion.LookRotation(dashDirection);
        parent.transform.rotation = dashRotation;

        parent.GetComponent<AbilityHolder>().StartCoroutine(Dash(parent, dashDirection));
    }

    private IEnumerator Dash(GameObject parent, Vector3 dashDirection)
    {
        //Defina isDashing como true no in�cio do dash
        isDashing = true;

        Animator animator = parent.GetComponent<Animator>();
        if (animator != null)
        {
            animator.Play(dashAnimation);
        }

        // Pega o Nav Mesh do player
        NavMeshAgent agent = parent.GetComponent<NavMeshAgent>();
      
        float startTime = Time.time;
        while (Time.time < startTime + dashTime)
        {
            parent.transform.position += dashDirection * dashVelocity * Time.deltaTime;
            yield return null;
        }

        // Faz com que o NavMesh não mova (Personagem para quando chega no ponto final)
        agent.SetDestination(parent.transform.position);

        // Defina isDashing como false no final do dash
        isDashing = false;
    }
}
