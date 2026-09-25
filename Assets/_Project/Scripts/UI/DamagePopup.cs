using UnityEngine;

/// <summary>Detached from the victim so lethal damage remains visible after death.</summary>
public sealed class DamagePopup : MonoBehaviour
{
    private float _born;
    private bool _lethal;
    private Camera _camera;
    private GUIStyle _style;
    private Vector3 _impact;
    public float Amount { get; private set; }
    public bool IsLethal => _lethal;

    public static void Show(Vector3 point, float amount, bool lethal)
    {
        var popup = new GameObject("Damage " + Mathf.CeilToInt(amount)).AddComponent<DamagePopup>();
        popup.transform.position = popup._impact = point;
        popup.Amount = amount; popup._lethal = lethal; popup._born = Time.time;
        Destroy(popup.gameObject, 1f);
    }
    private void OnGUI()
    {
        if (!_camera) _camera = Camera.main;
        if (!_camera) return;
        float age = Time.time - _born;
        Vector3 screen = _camera.WorldToScreenPoint(_impact + Vector3.up * (.25f + age * 1.2f));
        if (screen.z <= 0) return;
        float scale = Mathf.Clamp(Screen.height/900f,.8f,1.5f);
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label) { fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter };
            _style.normal.textColor = Color.white;
        }
        _style.fontSize = Mathf.RoundToInt((_lethal ? 27 : 23)*scale);
        Color old = GUI.color;
        float alpha = Mathf.Clamp01((1-age)/.4f);
        var rect = new Rect(screen.x-100,Screen.height-screen.y-20,200,70);
        string text = Mathf.CeilToInt(Amount).ToString() + (_lethal ? "\nELIMINADO" : "");
        GUI.color = new Color(0,0,0,alpha);
        GUI.Label(new Rect(rect.x+2,rect.y+2,rect.width,rect.height),text,_style);
        GUI.color = _lethal ? new Color(1,.8f,.25f,alpha) : new Color(1,1,1,alpha);
        GUI.Label(rect,text,_style);
        // A brief impact marker appears only after damage is confirmed, never on misses.
        if (age < .14f)
        {
            Vector3 impact = _camera.WorldToScreenPoint(_impact);
            Vector2 center = new Vector2(impact.x,Screen.height-impact.y);
            GUI.color = new Color(1,.88f,.6f,1-age/.14f);
            Matrix4x4 original = GUI.matrix;
            GUIUtility.RotateAroundPivot(45,center);
            GUI.DrawTexture(new Rect(center.x-13*scale,center.y-2*scale,26*scale,4*scale),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(center.x-2*scale,center.y-13*scale,4*scale,26*scale),Texture2D.whiteTexture);
            GUI.matrix = original;
        }
        GUI.color = old;
    }
}
