using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TechGuy.Cameras
{

  public class TG_TopDown_Camera : MonoBehaviour
  {
    #region Variables
      public Transform m_Target;

      [SerializeField]
      private float m_Height = 9f;
      [SerializeField]
      private float m_Distance = 10f;
      [SerializeField]
      private float m_Angle = 0f;
      [SerializeField]
      private float m_SmoothSpeed = 0.5f;

      private Vector3 refVelocity;
    #endregion

    #region Main Methods
      // Start is called before the first frame update
      void Start()
      {
        HandleCamera();  
      }

      // Update is called once per frame
      void Update()
      {
        HandleCamera(); 
      }
    #endregion

    #region Helper Methods
      protected virtual void HandleCamera()
      {
        if(!m_Target){
          return;
        }

        // Build the World position Vector
        Vector3 worldPosition = (Vector3.forward * -m_Distance) + (Vector3.up * m_Height);
        // Debug.DrawLine(m_Target.position, worldPosition, Color.red);

        // Build Rotated vector
        Vector3 rotatedVector = Quaternion.AngleAxis(m_Angle, Vector3.up) * worldPosition;
        // Debug.DrawLine(m_Target.position, rotatedVector, Color.green);


        // Move our position
        Vector3 flatTargetPosition = m_Target.position;
        flatTargetPosition.y = 0f;
        Vector3 finalPos = flatTargetPosition + rotatedVector;
        // Debug.DrawLine(m_Target.position, finalPos, Color.blue);

        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref refVelocity, m_SmoothSpeed);
        transform.LookAt(flatTargetPosition);

      }
    #endregion
  }
}
