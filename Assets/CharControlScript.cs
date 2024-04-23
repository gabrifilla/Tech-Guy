using UnityEngine;
using UnityEngine.AI;

public class CharControlScript : MonoBehaviour
{
    [Tooltip("Camera")]
    public Camera cam;
    [Tooltip("Player")]
    public NavMeshAgent player;
    [Tooltip("Player Animator")]
    public Animator playerAnimator;
    [Tooltip("Target Destination")]
    public GameObject targetDest;

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(1)){
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitPoint;

            if(Physics.Raycast(ray, out hitPoint)){
                targetDest.transform.position = hitPoint.point;
                player.SetDestination(hitPoint.point);
            }
        }

        if(player.velocity != Vector3.zero){
            playerAnimator.SetBool("isWalking", true);
        }
        else if(player.velocity == Vector3.zero){
            playerAnimator.SetBool("isWalking", false);
        }
    }
}
