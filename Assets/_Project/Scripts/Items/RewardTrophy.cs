using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A claimable trophy dropped when a room is cleared. It waits in a reachable spot so the
/// player can gather dropped coins first; pressing E nearby invokes the claim callback
/// (wired by the run director to open the boon selection) and removes the trophy.
/// </summary>
[DisallowMultipleComponent]
public sealed class RewardTrophy : MonoBehaviour
{
    [SerializeField, Min(0.5f)] private float _interactionRange = 2.4f;
    [SerializeField, Min(0f)] private float _spinSpeed = 45f;
    [SerializeField, Min(0f)] private float _bobHeight = 0.12f;
    [SerializeField, Min(0f)] private float _bobSpeed = 2f;

    private Transform _player;
    private Action _onClaimed;
    private bool _claimed;
    private Vector3 _basePosition;
    private float _phase;
    private GUIStyle _heading, _hint;
    private Texture2D _panel;
    private Material _material;

    /// <summary>Wires the trophy to the player it tracks and the action fired on claim.</summary>
    public void Configure(Transform player, Action onClaimed)
    {
        _player = player;
        _onClaimed = onClaimed;
    }

    /// <summary>
    /// Builds a golden trophy at <paramref name="position"/>, wires it to the player and claim
    /// callback, and returns the created component. The visual is assembled from primitives so
    /// no prefab wiring is required (mirrors how EncounterGates builds its doors in code).
    /// </summary>
    public static RewardTrophy Spawn(Vector3 position, Transform player, Action onClaimed)
    {
        var root = new GameObject("Reward Trophy");
        root.transform.position = position;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var gold = new Material(shader) { color = new Color(1f, 0.78f, 0.22f) };
        gold.EnableKeyword("_EMISSION");
        gold.SetColor("_EmissionColor", new Color(1.1f, 0.72f, 0.2f));
        if (gold.HasProperty("_Metallic")) gold.SetFloat("_Metallic", 0.9f);
        if (gold.HasProperty("_Smoothness")) gold.SetFloat("_Smoothness", 0.85f);

        // Base, stem, cup, and two handles give the silhouette of a trophy.
        BuildPart(root.transform, PrimitiveType.Cube, gold, new Vector3(0f, 0.08f, 0f), new Vector3(0.5f, 0.16f, 0.5f), Quaternion.identity);
        BuildPart(root.transform, PrimitiveType.Cylinder, gold, new Vector3(0f, 0.32f, 0f), new Vector3(0.09f, 0.16f, 0.09f), Quaternion.identity);
        BuildPart(root.transform, PrimitiveType.Cylinder, gold, new Vector3(0f, 0.62f, 0f), new Vector3(0.34f, 0.22f, 0.34f), Quaternion.identity);
        BuildPart(root.transform, PrimitiveType.Sphere, gold, new Vector3(0.26f, 0.62f, 0f), new Vector3(0.16f, 0.28f, 0.08f), Quaternion.identity);
        BuildPart(root.transform, PrimitiveType.Sphere, gold, new Vector3(-0.26f, 0.62f, 0f), new Vector3(0.16f, 0.28f, 0.08f), Quaternion.identity);

        var glowObject = new GameObject("Glow");
        glowObject.transform.SetParent(root.transform, false);
        glowObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        Light glow = glowObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(1f, 0.82f, 0.32f);
        glow.intensity = 2.2f;
        glow.range = 4.5f;

        var trophy = root.AddComponent<RewardTrophy>();
        trophy._material = gold;
        trophy.Configure(player, onClaimed);
        return trophy;
    }

    private static void BuildPart(Transform parent, PrimitiveType type, Material material,
        Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        // Purely decorative: the trophy is claimed via proximity + E, not physics.
        if (part.TryGetComponent(out Collider collider)) Destroy(collider);
        if (part.TryGetComponent(out Renderer renderer)) renderer.sharedMaterial = material;
    }

    private void Start()
    {
        _basePosition = transform.position;
        _phase = UnityEngine.Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        if (_claimed) return;

        transform.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.World);
        if (_bobHeight > 0f)
        {
            Vector3 position = _basePosition;
            position.y += Mathf.Sin(Time.time * _bobSpeed + _phase) * _bobHeight;
            transform.position = position;
        }

        if (!PlayerInRange() || Keyboard.current == null) return;
        if (Keyboard.current.eKey.wasPressedThisFrame) Claim();
    }

    private bool PlayerInRange()
    {
        if (!_player) return false;
        Vector3 delta = _player.position - transform.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= _interactionRange * _interactionRange;
    }

    private void Claim()
    {
        if (_claimed) return;
        _claimed = true;
        Action callback = _onClaimed;
        _onClaimed = null;
        callback?.Invoke();
        Destroy(gameObject);
    }

    private void OnGUI()
    {
        if (_claimed || !PlayerInRange()) return;
        if (_heading == null)
        {
            _heading = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _heading.normal.textColor = new Color(1f, 0.85f, 0.4f);
            _hint = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            _hint.normal.textColor = new Color(0.86f, 0.9f, 0.97f);
            _panel = new Texture2D(1, 1);
            _panel.SetPixel(0, 0, new Color(0.025f, 0.035f, 0.08f, 0.9f));
            _panel.Apply();
        }
        Matrix4x4 previous = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        float width = Screen.width / scale;
        float height = Screen.height / scale;
        var box = new Rect(width / 2 - 250, height - 250, 500, 74);
        GUI.DrawTexture(box, _panel);
        GUI.Label(new Rect(box.x, box.y + 10, box.width, 26), "TROFÉU DA SALA", _heading);
        GUI.Label(new Rect(box.x, box.y + 40, box.width, 24), "[E] Reivindicar bênção  ·  colete suas moedas antes", _hint);
        GUI.matrix = previous;
    }

    private void OnDestroy()
    {
        if (_panel) Destroy(_panel);
        if (_material) Destroy(_material);
    }
}
