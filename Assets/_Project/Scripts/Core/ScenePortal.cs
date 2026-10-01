using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A proximity gate that loads <see cref="Destination"/> when the player walks into its radius.
/// Two creation paths are supported:
/// <list type="bullet">
/// <item>Authored portals (e.g. the Nexus lobby) add the component in the editor and already carry
/// their own visual geometry; <see cref="Configure"/> wires the player + destination.</item>
/// <item>Runtime portals (e.g. the procedural run's Extraction_Portal) are created with
/// <see cref="Spawn"/>, which builds a self-contained glowing visual from primitives so the player
/// can actually SEE where to go — no serialized prefab required (mirrors how
/// <see cref="RewardTrophy.Spawn"/> assembles its visual in code).</item>
/// </list>
/// </summary>
public sealed class ScenePortal : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private string _destination;
    [SerializeField, Min(.2f)] private float _radius = 1.25f;

    public string Destination => _destination;

    public void Configure(Transform player, string destination) { _player = player; _destination = destination; }

    /// <summary>
    /// Builds a visible, self-contained extraction portal at <paramref name="position"/>, wires it to
    /// the player and destination scene, and returns the created component. The visual is a glowing
    /// golden ring on a small dais with a soft point light, assembled from primitives so no prefab
    /// wiring is needed. <see cref="Configure"/> is invoked before this returns so the destination is
    /// set in the same frame the component is added, before <see cref="Start"/>'s polling begins.
    /// </summary>
    public static ScenePortal Spawn(Vector3 position, Transform player, string destination, float interactionRadius = 2.2f)
    {
        var root = new GameObject("Extraction_Portal");
        root.transform.position = position;

        // Emissive golden material shared by the portal's parts so it reads as a bright, inviting gate.
        var glowMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        var gold = new Color(1f, 0.84f, 0.25f, 1f);
        glowMat.color = gold;
        if (glowMat.HasProperty("_BaseColor")) glowMat.SetColor("_BaseColor", gold);
        if (glowMat.HasProperty("_EmissionColor"))
        {
            glowMat.EnableKeyword("_EMISSION");
            glowMat.SetColor("_EmissionColor", gold * 2.5f);
        }

        // Dais so the portal reads as a deliberate object on the floor.
        BuildPart(root.transform, PrimitiveType.Cylinder, glowMat,
            new Vector3(0f, 0.05f, 0f), new Vector3(2.6f, 0.1f, 2.6f), Quaternion.identity);

        // A vertical ring of segments forming the gate the player steps into.
        const int segments = 16;
        const float ringRadius = 1.7f;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            var localPos = new Vector3(Mathf.Sin(angle) * ringRadius, ringRadius + 0.6f + Mathf.Cos(angle) * ringRadius, 0f);
            var rot = Quaternion.Euler(0f, 0f, -i * (360f / segments));
            BuildPart(root.transform, PrimitiveType.Cube, glowMat, localPos, new Vector3(0.55f, 0.3f, 0.3f), rot);
        }

        // Soft light so the portal glows and is visible from across the arena.
        var lightObject = new GameObject("Portal glow");
        lightObject.transform.SetParent(root.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, ringRadius + 0.6f, 0f);
        var glow = lightObject.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = gold;
        glow.intensity = 4f;
        glow.range = 10f;

        var portal = root.AddComponent<ScenePortal>();
        portal._radius = Mathf.Max(.2f, interactionRadius);
        portal._material = glowMat;
        portal.Configure(player, destination);
        return portal;
    }

    /// <summary>
    /// Builds one decorative, collider-free primitive part under <paramref name="parent"/> with the
    /// shared portal material. Colliders are stripped because the portal is entered via proximity, not
    /// physics, so it never blocks the player's NavMesh movement into it.
    /// </summary>
    private static void BuildPart(Transform parent, PrimitiveType type, Material material,
        Vector3 localPosition, Vector3 localScale, Quaternion localRotation)
    {
        var part = GameObject.CreatePrimitive(type);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.transform.localRotation = localRotation;
        // Strip the primitive's auto-added collider so it never blocks the player's NavMesh move into the
        // portal. DestroyObject picks DestroyImmediate outside play mode so this is safe when the portal is
        // built from an EditMode test / editor tool, and Destroy at runtime.
        if (part.TryGetComponent(out Collider collider)) DestroyObject(collider);
        if (part.TryGetComponent(out Renderer renderer)) renderer.sharedMaterial = material;
    }

    /// <summary>
    /// Destroys <paramref name="target"/> with the API appropriate to the current context: the editor-only
    /// <c>DestroyImmediate</c> outside play mode (so EditMode tests and editor tools do not hit
    /// "Destroy may not be called from edit mode") and the normal deferred <c>Destroy</c> at runtime.
    /// </summary>
    private static void DestroyObject(Object target)
    {
        if (!target) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }

    private Material _material;

    private IEnumerator Start()
    {
        if (!_player || string.IsNullOrEmpty(_destination))
        { Debug.LogError("Portal requires a player and destination.", this); yield break; }
        var interval = new WaitForSeconds(.15f);
        while (_player)
        {
            Vector3 offset = _player.position - transform.position;
            offset.y = 0;
            if (offset.sqrMagnitude < _radius * _radius)
            {
                if (!Application.CanStreamedLevelBeLoaded(_destination))
                { Debug.LogError("Portal destination is missing from Build Settings: " + _destination, this); yield break; }
                SceneManager.LoadSceneAsync(_destination);
                yield break;
            }
            yield return interval;
        }
    }

    private void OnDestroy()
    {
        if (_material) DestroyObject(_material);
    }
}
