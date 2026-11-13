using UnityEngine;

/// <summary>
/// Gray-box body of the Desconhecido (Etapa 4b, 12/11/2026): he starts as an incomplete model and each rapport gift
/// rebuilds part of him. Layer 0: a torso and a head with nothing else, flickering. Layer 1: arms (and the fishing
/// rod he remembered). Layer 2: legs — he stands as the guide he was meant to be. Layer 3: face and colour — as
/// finished as his author left him. Missing parts show as dim wire markers. Primitives only: when the real prefab
/// exists, each layer's pieces are swapped for its parts.
/// </summary>
public sealed class UnknownFigure : MonoBehaviour
{
    private static readonly Color Unfinished = new Color(.42f, .45f, .50f), Finished = new Color(.62f, .74f, .80f),
        Accent = new Color(.30f, .92f, 1f), Ghost = new Color(.30f, .92f, 1f, 1f);

    private int _layers = -1;
    private Material _body, _accent, _ghost;
    private Renderer[] _flicker = System.Array.Empty<Renderer>();
    private float _nextFlicker;

    /// <summary>Layers currently built (0..<see cref="Rapport.UnknownMaxLayers"/>).</summary>
    public int Layers => _layers;
    /// <summary>Solid body pieces currently present (test seam: grows with every layer).</summary>
    public int SolidParts { get; private set; }

    public static UnknownFigure Create(Transform parent, Vector3 localPosition, int layers)
    {
        var go = new GameObject("Desconhecido");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        var figure = go.AddComponent<UnknownFigure>();
        figure.Rebuild(layers);
        return figure;
    }

    /// <summary>Rebuilds the body for <paramref name="layers"/> revealed layers. No-op when unchanged.</summary>
    public void Rebuild(int layers)
    {
        layers = Mathf.Clamp(layers, 0, Rapport.UnknownMaxLayers);
        if (layers == _layers) return;
        _layers = layers;
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        EnsureMaterials();
        float complete = layers / (float)Rapport.UnknownMaxLayers;
        Color bodyColor = Color.Lerp(Unfinished, Finished, complete);
        SetColor(_body, bodyColor, 0f);
        SetColor(_accent, Accent, 1.2f);
        SetColor(_ghost, Ghost * .35f, .4f);
        SolidParts = 0;
        var flicker = new System.Collections.Generic.List<Renderer>();

        // Always there: torso and head (the head is a bare block until the face comes back).
        Solid(PrimitiveType.Cube, new Vector3(0, 1.15f, 0), new Vector3(.52f, .62f, .30f), "Tronco");
        Solid(layers >= 3 ? PrimitiveType.Sphere : PrimitiveType.Cube, new Vector3(0, 1.68f, 0), Vector3.one * (layers >= 3 ? .36f : .30f), "Cabeça");

        // Layer 1 — arms, and the rod of someone who "liked to fish".
        if (layers >= 1)
        {
            Solid(PrimitiveType.Cube, new Vector3(-.36f, 1.12f, 0), new Vector3(.14f, .58f, .16f), "Braço esquerdo");
            Solid(PrimitiveType.Cube, new Vector3(.36f, 1.12f, 0), new Vector3(.14f, .58f, .16f), "Braço direito");
            var rod = Piece(PrimitiveType.Cylinder, new Vector3(.52f, 1.35f, .12f), new Vector3(.03f, .75f, .03f), "Vara de pesca", _accent);
            rod.transform.localRotation = Quaternion.Euler(0, 0, -18f);
        }
        else
        {
            flicker.Add(Marker(new Vector3(-.36f, 1.12f, 0), new Vector3(.14f, .58f, .16f), "Braço esquerdo (faltando)"));
            flicker.Add(Marker(new Vector3(.36f, 1.12f, 0), new Vector3(.14f, .58f, .16f), "Braço direito (faltando)"));
        }

        // Layer 2 — legs: until then the torso hovers over two empty outlines.
        if (layers >= 2)
        {
            Solid(PrimitiveType.Cube, new Vector3(-.14f, .44f, 0), new Vector3(.18f, .82f, .20f), "Perna esquerda");
            Solid(PrimitiveType.Cube, new Vector3(.14f, .44f, 0), new Vector3(.18f, .82f, .20f), "Perna direita");
            Piece(PrimitiveType.Cube, new Vector3(0, 1.32f, -.16f), new Vector3(.16f, .10f, .02f), "Crachá de guia", _accent);
        }
        else
        {
            flicker.Add(Marker(new Vector3(-.14f, .44f, 0), new Vector3(.18f, .82f, .20f), "Perna esquerda (faltando)"));
            flicker.Add(Marker(new Vector3(.14f, .44f, 0), new Vector3(.18f, .82f, .20f), "Perna direita (faltando)"));
        }

        // Layer 3 — a face.
        if (layers >= 3)
        {
            Piece(PrimitiveType.Sphere, new Vector3(-.08f, 1.72f, -.16f), Vector3.one * .06f, "Olho esquerdo", _accent);
            Piece(PrimitiveType.Sphere, new Vector3(.08f, 1.72f, -.16f), Vector3.one * .06f, "Olho direito", _accent);
        }
        else flicker.Add(Marker(new Vector3(0, 1.72f, -.17f), new Vector3(.20f, .05f, .02f), "Rosto (faltando)"));

        _flicker = flicker.ToArray();
    }

    private void Update()
    {
        // Missing parts blink in and out, like geometry that never finished loading.
        if (_flicker.Length == 0 || Time.unscaledTime < _nextFlicker) return;
        _nextFlicker = Time.unscaledTime + Random.Range(.08f, .45f);
        var renderer = _flicker[Random.Range(0, _flicker.Length)];
        if (renderer) renderer.enabled = !renderer.enabled || Random.value < .5f;
    }

    private void Solid(PrimitiveType type, Vector3 position, Vector3 scale, string name)
    { Piece(type, position, scale, name, _body); SolidParts++; }

    private Renderer Marker(Vector3 position, Vector3 scale, string name) =>
        Piece(PrimitiveType.Cube, position, scale * .9f, name, _ghost).GetComponent<Renderer>();

    private GameObject Piece(PrimitiveType type, Vector3 position, Vector3 scale, string name, Material material)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name; part.transform.SetParent(transform, false);
        part.transform.localPosition = position; part.transform.localScale = scale;
        if (part.TryGetComponent(out Collider collider)) Destroy(collider);
        var renderer = part.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = material == _ghost ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        return part;
    }

    private void EnsureMaterials()
    {
        if (_body) return;
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        _body = new Material(shader); _accent = new Material(shader); _ghost = new Material(shader);
    }

    private static void SetColor(Material material, Color color, float emission)
    {
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (emission > 0 && material.HasProperty("_EmissionColor")) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * emission); }
    }

    private void OnDestroy()
    {
        if (_body) Destroy(_body);
        if (_accent) Destroy(_accent);
        if (_ghost) Destroy(_ghost);
    }
}
