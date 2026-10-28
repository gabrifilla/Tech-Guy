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

      [SerializeField, Range(0f, 1f)] private float _occludedOpacity = .12f;
      [SerializeField, Min(.01f)] private float _fadeOutDuration = .12f;
      [SerializeField, Min(.01f)] private float _fadeInDuration = .25f;
      [SerializeField, Min(.05f)] private float _occlusionProbeRadius = .5f;
      [SerializeField, Min(0f)] private float _pathRevealDistance = 2.5f;
      [SerializeField, Min(0f)] private float _occlusionRestoreDelay = .12f;
      private CameraOcclusionFader _occlusionFader;
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
      private void LateUpdate()
      {
        if (_occlusionFader == null)
          _occlusionFader = new CameraOcclusionFader(_occludedOpacity, _fadeOutDuration, _fadeInDuration,
            _occlusionProbeRadius, _pathRevealDistance, _occlusionRestoreDelay);
        _occlusionFader.Tick(transform.position, m_Target, Time.unscaledDeltaTime);
      }

      public bool IsOcclusionFaded(Collider collider)
      {
        return isActiveAndEnabled && _occlusionFader != null && _occlusionFader.IsFaded(collider);
      }

      private void OnDisable()
      {
        _occlusionFader?.Dispose();
        _occlusionFader = null;
      }

      private void OnDestroy() { _occlusionFader?.Dispose(); }

      private void OnValidate()
      {
        _occludedOpacity = CameraOcclusionFader.FiniteClamp(_occludedOpacity, .12f, 0f, 1f);
        _fadeOutDuration = CameraOcclusionFader.FiniteClamp(_fadeOutDuration, .12f, .01f, 5f);
        _fadeInDuration = CameraOcclusionFader.FiniteClamp(_fadeInDuration, .25f, .01f, 5f);
        _occlusionProbeRadius = CameraOcclusionFader.FiniteClamp(_occlusionProbeRadius, .5f, .05f, 2f);
        _pathRevealDistance = CameraOcclusionFader.FiniteClamp(_pathRevealDistance, 2.5f, 0f, 8f);
        _occlusionRestoreDelay = CameraOcclusionFader.FiniteClamp(_occlusionRestoreDelay, .12f, 0f, 2f);
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

            transform.position = finalPos;
            transform.LookAt(flatTargetPosition);

      }
    #endregion
  }
}
