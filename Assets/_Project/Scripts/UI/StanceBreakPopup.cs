using UnityEngine;

/// <summary>
/// A short-lived world-anchored "POSTURA QUEBRADA!" callout shown when a tougher enemy's stance
/// breaks. Detached from the victim (mirrors DamagePopup) so it survives even if the enemy dies.
/// </summary>
public sealed class StanceBreakPopup : MonoBehaviour
{
    private float _born;
    private Vector3 _anchor;
    private Camera _camera;
    private GUIStyle _style;

    public static void Show(Vector3 worldPoint)
    {
        var popup = new GameObject("Stance Break Popup").AddComponent<StanceBreakPopup>();
        popup.transform.position = popup._anchor = worldPoint;
        popup._born = Time.time;
        Destroy(popup.gameObject, 1.1f);
    }

    private void OnGUI()
    {
        if (!_camera) _camera = Camera.main;
        if (!_camera) return;

        float age = Time.time - _born;
        Vector3 screen = _camera.WorldToScreenPoint(_anchor + Vector3.up * (0.3f + age * 1.4f));
        if (screen.z <= 0f) return;

        float scale = Mathf.Clamp(Screen.height / 900f, 0.8f, 1.5f);
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        }
        // Pop in fast then fade out.
        float pop = age < 0.12f ? Mathf.Lerp(0.6f, 1.15f, age / 0.12f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((age - 0.12f) / 0.2f));
        _style.fontSize = Mathf.RoundToInt(24 * scale * pop);
        float alpha = Mathf.Clamp01((1.1f - age) / 0.4f);

        var rect = new Rect(screen.x - 150, Screen.height - screen.y - 18, 300, 40);
        const string text = "POSTURA QUEBRADA!";
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, _style);
        GUI.color = new Color(1f, 0.85f, 0.3f, alpha);
        GUI.Label(rect, text, _style);
        GUI.color = old;
    }
}
