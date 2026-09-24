using UnityEngine;

public sealed class LobbyCoreMotion : MonoBehaviour
{
    [SerializeField] private float _degreesPerSecond = 18f;
    [SerializeField] private float _floatAmplitude = 0.15f;
    private Vector3 _origin;

    private void Awake() => _origin = transform.localPosition;

    private void Update()
    {
        transform.Rotate(Vector3.up, _degreesPerSecond * Time.deltaTime, Space.World);
        transform.localPosition = _origin + Vector3.up * (Mathf.Sin(Time.time * 1.4f) * _floatAmplitude);
    }
}
