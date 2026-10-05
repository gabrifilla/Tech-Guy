using UnityEngine;

/// <summary>Local break-only ring and sound. Presentation never publishes combat hooks or applies hit-stop.</summary>
public sealed class StanceBreakPresentation : MonoBehaviour
{
    [SerializeField] private AudioClip _breakSound;
    [SerializeField, Range(0f, 1f)] private float _volume = 0.25f;
    private AudioSource _source;
    private AudioClip _generatedSound;
    private CombatGroundRing _ring;
    private float _born;
    private Vector3 _point;
    public void Show(Vector3 point)
    {
        if (!isActiveAndEnabled) return;
        CleanupRing(); _point = point; _point.y = transform.position.y; _born = Time.realtimeSinceStartup;
        _ring = CombatGroundRing.Create(null, "Stance break ring", new Color(1f, 0.85f, 0.35f));
        _ring.Draw(_point, 0.4f, 0.15f);
        if (!_source)
        {
            var audio = new GameObject("Stance break audio"); audio.transform.SetParent(transform, false);
            _source = audio.AddComponent<AudioSource>(); _source.playOnAwake = false; _source.spatialBlend = 1f;
        }
        if (!_breakSound && !_generatedSound)
        {
            // The project's current inventory has no AudioClips. Authored sound overrides this fallback.
            const int rate = 22050, count = 4410;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float time = (float)i / rate;
                float envelope = Mathf.Exp(-time * 28f) * Mathf.Min(1f, time * 600f);
                samples[i] = envelope * (Mathf.Sin(time * 1450f * Mathf.PI) * 0.6f + Mathf.Sin(time * 2370f * Mathf.PI) * 0.3f);
            }
            _generatedSound = AudioClip.Create("Procedural stance break", count, 1, rate, false);
            _generatedSound.SetData(samples, 0);
        }
        _source.PlayOneShot(_breakSound ? _breakSound : _generatedSound, _volume);
    }
    private void Update()
    {
        if (!_ring) return;
        float progress = (Time.realtimeSinceStartup - _born) / 0.25f;
        if (progress >= 1f) { CleanupRing(); return; }
        _ring.Draw(_point, Mathf.Lerp(0.4f, 1.6f, progress), Mathf.Lerp(0.15f, 0.01f, progress));
    }
    private void CleanupRing() { if (_ring) { _ring.gameObject.SetActive(false); Destroy(_ring.gameObject); } _ring = null; }
    private void OnDisable() { CleanupRing(); if (_source) _source.Stop(); }
    private void OnDestroy() { if (_generatedSound) Destroy(_generatedSound); }
}
