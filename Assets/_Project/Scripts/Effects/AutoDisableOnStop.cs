using UnityEngine;

public class AutoDisableOnStop : MonoBehaviour
{
    void OnParticleSystemStopped()
    {
        gameObject.SetActive(false);
    }
}
