using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashScript : MonoBehaviour
{
    public float dashSpeed;
    public float dashTime;

    Vector3 dashDirection;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Get the position of the mouse in the world
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Calculate the direction to dash
                dashDirection = (hit.point - transform.position).normalized;
                StartCoroutine(Dash());
            }
        }
    }

    IEnumerator Dash()
    {
        float startTime = Time.time;

        while (Time.time < startTime + dashTime)
        {
            // Move the character in the dash direction
            transform.position += dashDirection * dashSpeed * Time.deltaTime;
            yield return null;
        }

        // Stop the NavMeshAgent from moving
        //agent.SetDestination(transform.position);
    }
}
