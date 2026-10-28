using UnityEngine;

/// <summary>Detached from the victim so lethal damage remains visible after death.</summary>
public sealed class DamagePopup : MonoBehaviour
{
    private readonly DamageNumberBurst _burst = new DamageNumberBurst();
    private float _impactAt;
    private Camera _camera;
    private GUIStyle _style;
    private Vector3 _impact;
    private string _text;
    public float Amount => _burst.Amount;
    public bool IsLethal => _burst.IsLethal;

    // Preserve the existing standalone API; aggregation is explicitly owned by the victim's feedback.
    public static void Show(Vector3 point, float amount, bool lethal) => Create(point, amount, lethal);

    public static DamagePopup Create(Vector3 point, float amount, bool lethal)
    {
        if (!DamageNumberBurst.IsValidAmount(amount) || !IsFinite(point)) return null;
        var popup = new GameObject("Damage confirmation").AddComponent<DamagePopup>();
        popup.transform.position = popup._impact = point;
        popup._burst.TryBegin(amount, lethal, Time.time);
        popup._impactAt = Time.time;
        popup.RefreshText();
        // Fixed lifetime from the first hit: sustained multi-hit attacks cannot keep a popup alive forever.
        Destroy(popup.gameObject, 1f);
        return popup;
    }

    public bool TryAccumulate(Vector3 point, float amount, bool lethal)
    {
        if (!isActiveAndEnabled || !IsFinite(point) || !_burst.TryAccumulate(amount, lethal, Time.time)) return false;
        _impact = point;
        _impactAt = Time.time; // Every confirmed contact retains its short impact marker.
        RefreshText();
        return true;
    }

    private void RefreshText()
    {
        float amount = Amount;
        _text = amount >= 1000000f ? (amount / 1000000f).ToString("0.#") + "m" :
            amount >= 1000f ? (amount / 1000f).ToString("0.#") + "k" : Mathf.CeilToInt(amount).ToString();
    }

    private static bool IsFinite(Vector3 point) =>
        !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
        !float.IsNaN(point.y) && !float.IsInfinity(point.y) &&
        !float.IsNaN(point.z) && !float.IsInfinity(point.z);

    private void OnGUI()
    {
        // This is a positioned overlay with no layout or input controls.
        if (Event.current.type != EventType.Repaint) return;
        if (!_camera) _camera = Camera.main;
        if (!_camera) return;
        float age = Time.time - _burst.BornAt;
        Vector3 screen = _camera.WorldToScreenPoint(_impact + Vector3.up * (.25f + age * 1.2f));
        if (screen.z <= 0) return;
        float scale = Mathf.Clamp(Screen.height/900f,.8f,1.5f);
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label) { fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter };
            _style.normal.textColor = Color.white;
        }
        _style.fontSize = Mathf.RoundToInt((IsLethal ? 27 : 23)*scale);
        Color old = GUI.color;
        float alpha = Mathf.Clamp01((1-age)/.4f);
        var rect = new Rect(screen.x-100,Screen.height-screen.y-20,200,70);
        GUI.color = new Color(0,0,0,alpha);
        GUI.Label(new Rect(rect.x+2,rect.y+2,rect.width,rect.height),_text,_style);
        GUI.color = IsLethal ? new Color(1,.8f,.25f,alpha) : new Color(1,1,1,alpha);
        GUI.Label(rect,_text,_style);
        // A brief impact marker appears only after damage is confirmed, never on misses.
        float contactAge = Time.time - _impactAt;
        if (contactAge < .14f)
        {
            Vector3 impact = _camera.WorldToScreenPoint(_impact);
            Vector2 center = new Vector2(impact.x,Screen.height-impact.y);
            GUI.color = new Color(1,.88f,.6f,1-contactAge/.14f);
            Matrix4x4 original = GUI.matrix;
            GUIUtility.RotateAroundPivot(45,center);
            GUI.DrawTexture(new Rect(center.x-13*scale,center.y-2*scale,26*scale,4*scale),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(center.x-2*scale,center.y-13*scale,4*scale,26*scale),Texture2D.whiteTexture);
            GUI.matrix = original;
        }
        GUI.color = old;
    }
}
