using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Scene-local RUN-EXP-08 comparison. Never owns collision, gates, rewards or navigation.</summary>
[DisallowMultipleComponent]
public sealed class PainterlyRoomPrototype : MonoBehaviour
{
    [SerializeField] private ProgressionDirector _director;
    [SerializeField] private Material _paint, _floor, _edge, _closed, _coverTop;
    [SerializeField] private int _nodeId = 1;
    [SerializeField] private bool _showPainterly = true;
    private readonly List<MeshRenderer> _changed = new List<MeshRenderer>();
    private readonly List<Material[]> _originalMaterials = new List<Material[]>();
    private readonly List<MeshRenderer> _hidden = new List<MeshRenderer>();
    private readonly List<GameObject> _visuals = new List<GameObject>();
    private readonly List<Mesh> _meshes = new List<Mesh>();
    private readonly List<MeshFilter> _coverFilters = new List<MeshFilter>();
    private readonly List<Mesh> _originalCoverMeshes = new List<Mesh>();
    private Mesh _paintedCover;
    private Transform _arena;
    public bool Ready { get; private set; }
    public bool PainterlyVisible => _showPainterly;
    public int DecoratedNode => _nodeId;
    public int VisualObjectCount => _visuals.Count;

    private IEnumerator Start()
    {
        // Serialized scene reference, no production discovery or persistent state.
        if (!_director || !_paint || !_floor || !_edge || !_closed || !_coverTop) yield break;
        while (_director && (_director.ManualSession == null || !_director.ManualEnvironment.HasGeneratedLayout))
            yield return null;
        if (!_director) yield break;
        var node = _director.ManualSession.Route.Node(_nodeId);
        _arena = _director.ManualEnvironment.Node(_nodeId)?.Arena;
        if (_arena == null || node == null) yield break;
        Build(node);
        Ready = true;
        SetPainterlyVisible(_showPainterly);
    }

    public void SetPainterlyVisible(bool visible)
    {
        _showPainterly = visible;
        for (int i = 0; i < _changed.Count; i++)
            if (_changed[i]) _changed[i].sharedMaterials = visible
                ? _changed[i].name.StartsWith("Cobertura deslocada") ? new[] { _paint, _coverTop }
                : new[] { _changed[i].name == "Walkable arena" || _changed[i].name == "Corridor floor" ? _floor : _paint }
                : _originalMaterials[i];
        foreach (var renderer in _hidden) if (renderer) renderer.enabled = !visible;
        foreach (var visual in _visuals) if (visual) visual.SetActive(visible);
        for (int i = 0; i < _coverFilters.Count; i++)
            if (_coverFilters[i]) _coverFilters[i].sharedMesh = visible ? _paintedCover : _originalCoverMeshes[i];
    }

    private void Paint(MeshRenderer renderer, Material material)
    {
        if (!renderer || _changed.Contains(renderer)) return;
        _changed.Add(renderer);
        _originalMaterials.Add(renderer.sharedMaterials);
        renderer.sharedMaterial = material;
    }

    private void Build(ManualNodeDefinition node)
    {
        foreach (var renderer in _arena.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.name == "Walkable arena") Paint(renderer, _floor);
            else if (renderer.name == "Arena wall" || renderer.name.StartsWith("Cobertura deslocada")) Paint(renderer, _paint);
            else if (renderer.name.StartsWith("Archive service racks") || renderer.name.StartsWith("Trilhas visuais")) _hidden.Add(renderer);
        }
        _paintedCover = PaintedCover(); _meshes.Add(_paintedCover);
        foreach (var filter in _arena.GetComponentsInChildren<MeshFilter>())
            if (filter.name.StartsWith("Cobertura deslocada"))
            { _coverFilters.Add(filter); _originalCoverMeshes.Add(filter.sharedMesh); }
        // Crown planes remain fixed to physical walls and are resolved by their existing collider's fader.
        int index = 0;
        foreach (var collider in _arena.GetComponentsInChildren<BoxCollider>())
        {
            if (collider.name != "Arena wall") continue;
            bool horizontal = collider.transform.localScale.x > collider.transform.localScale.z;
            float length = horizontal ? collider.transform.localScale.x : collider.transform.localScale.z;
            int segments = Mathf.Max(1, Mathf.CeilToInt(length / 3.5f));
            for (int i = 0; i < segments; i++)
            {
                float segment = length / segments;
                float along = -length * .5f + (i + .5f) * segment;
                float lift = .35f + ((index + i * 3) % 5) * .22f;
                // World-space authored dimensions, independently of the scaled native primitive.
                var pos = collider.bounds.center + (horizontal ? Vector3.right : Vector3.forward) * along + Vector3.up * (2 + lift * .5f);
                Box("Pigment crown / decorative", collider.transform, pos,
                    horizontal ? new Vector3(segment - .08f, lift, .43f) : new Vector3(.43f, lift, segment - .08f), _paint);
                if ((i + index) % 3 == 0)
                    Box("Mineral seam", collider.transform, collider.bounds.center + (horizontal ? Vector3.right : Vector3.forward) * along,
                        horizontal ? new Vector3(.08f, 3.7f, .515f) : new Vector3(.515f, 3.7f, .08f), _edge);
            }
            index++;
        }
        // One genuinely illustrated peripheral silhouette; opaque cut mesh, no transparent margins.
        foreach (var collider in _arena.GetComponentsInChildren<BoxCollider>())
        {
            if (collider.name != "Arena wall" || collider.bounds.size.x < 4) continue;
            BuildFacade(collider);
            break;
        }
        var plan = ProceduralRunGeometryPlan.Create(_director.ManualSession.Route);
        foreach (var edge in _director.ManualSession.Route.Connections)
        {
            if (edge.From != _nodeId && edge.To != _nodeId) continue;
            var binding = _director.ManualEnvironment.Connections;
            ManualConnectionBinding connection = null;
            foreach (var candidate in binding) if (candidate.ConnectionId == edge.Id) connection = candidate;
            if (connection == null) continue;
            var points = plan[edge.Id];
            bool outgoing = edge.From == _nodeId;
            var port = outgoing ? points[0] : points[points.Length - 1];
            var direction = outgoing ? (points[1] - points[0]).normalized : (points[points.Length - 2] - points[points.Length - 1]).normalized;
            BuildThreshold(connection, port, direction, outgoing);
        }
    }

    private void BuildFacade(BoxCollider physical)
    {
        var mesh = new Mesh { name = "Fixed pigment strata facade / RUNEXP08" };
        float width = Mathf.Min(physical.bounds.size.x - .1f, 7.5f);
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        // Cut silhouette across its upper edge; baseline wall core remains visibly solid below.
        for (int i = 0; i <= 8; i++)
        {
            float t = i / 8f;
            vertices.Add(new Vector3((t - .5f) * width, 0, 0)); uv.Add(new Vector2(t, 0));
            vertices.Add(new Vector3((t - .5f) * width, 3.4f + (i % 3) * .2f, 0)); uv.Add(new Vector2(t, 1));
            if (i < 8) { int v = i * 2; triangles.AddRange(new[] { v, v + 1, v + 3, v, v + 3, v + 2 }); }
        }
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); _meshes.Add(mesh);
        var go = new GameObject("Illustrated fixed facade / opaque cut mesh");
        go.transform.SetParent(physical.transform, false);
        go.transform.position = new Vector3(physical.bounds.center.x, .04f, physical.bounds.center.z - .258f);
        go.transform.rotation = Quaternion.identity; go.transform.localScale = new Vector3(1 / physical.transform.lossyScale.x, 1 / physical.transform.lossyScale.y, 1 / physical.transform.lossyScale.z);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = _paint; renderer.shadowCastingMode = ShadowCastingMode.Off;
        _visuals.Add(go);
    }

    private void BuildThreshold(ManualConnectionBinding connection, Vector3 port, Vector3 forward, bool outgoing)
    {
        Vector3 side = Vector3.Cross(Vector3.up, forward);
        Transform root = connection.FromAnchor.parent;
        // Columns are centered INSIDE the native corridor walls, outside its four-metre clear passage.
        for (int sign = -1; sign <= 1; sign += 2)
        {
            Vector3 p = port + side * sign * 2.25f + Vector3.up * 2;
            Transform physical = root; float nearest = float.PositiveInfinity;
            foreach (var collider in root.GetComponentsInChildren<BoxCollider>(true))
            {
                if (collider.name != "Corridor wall") continue;
                float distance = (collider.bounds.ClosestPoint(p) - p).sqrMagnitude;
                if (distance < nearest) { nearest = distance; physical = collider.transform; }
            }
            Box(outgoing ? "Forward threshold pier" : "Arrival threshold pier", physical, p,
                Mathf.Abs(forward.z) > .5f ? new Vector3(.48f, 4, 1.2f) : new Vector3(1.2f, 4, .48f), _paint);
            Box("Threshold edge / quiet navigation accent", physical, p - forward * .4f,
                Mathf.Abs(forward.z) > .5f ? new Vector3(.12f, 3.8f, .12f) : new Vector3(.12f, 3.8f, .12f), outgoing ? _edge : _closed);
            Box(outgoing ? "Forward arch shoulder" : "Arrival arch shoulder", physical, port + side * sign * 2.05f + Vector3.up * 4.25f,
                Mathf.Abs(forward.z) > .5f ? new Vector3(.9f, .5f, .5f) : new Vector3(.5f, .5f, .9f), _paint);
        }
        // Reframe five metres of EXISTING traversal. No extra length or waiting.
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = port + forward * (i * 1.1f + .45f);
            Box("Inlaid transition tread", root, p + Vector3.up * .012f,
                Mathf.Abs(forward.z) > .5f ? new Vector3(3.7f, .012f, .12f) : new Vector3(.12f, .012f, 3.7f), outgoing ? _edge : _closed);
        }
        Paint(connection.Barrier.GetComponent<MeshRenderer>(), _paint);
        var gate = connection.Barrier;
        // Child shutter follows the native gate's active state; never a second authority or collider.
        Box("Closed pigment shutter / native gate state", gate.transform, gate.transform.position,
            gate.transform.lossyScale * 1.002f, _closed);
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            if (renderer.name == "Corridor floor") Paint(renderer, _floor);
    }

    private GameObject Box(string label, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var go = new GameObject(label); go.transform.SetParent(parent, false);
        go.transform.position = position; go.transform.rotation = Quaternion.identity;
        var scale = parent.lossyScale;
        go.transform.localScale = new Vector3(size.x / scale.x, size.y / scale.y, size.z / scale.z);
        // Built-in cube mesh from a shared resource; no primitive collider is ever created.
        var filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        // Existing wall/cover volumes already cast the contact shadows. Decorative duplicates do not.
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        _visuals.Add(go); return go;
    }

    private static Mesh PaintedCover()
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>(); var topTriangles = new List<int>();
        // Each side has its own full painting domain instead of the old character trim-atlas UVs.
        var ring = new[] { new Vector2(-.47f,-.5f), new Vector2(-.5f,-.47f), new Vector2(-.5f,.47f), new Vector2(-.47f,.5f), new Vector2(.47f,.5f), new Vector2(.5f,.47f), new Vector2(.5f,-.47f), new Vector2(.47f,-.5f) };
        for (int i = 0; i < ring.Length; i++)
        {
            var a = ring[i]; var b = ring[(i + 1) % ring.Length]; int start = v.Count;
            v.Add(new Vector3(a.x,-.5f,a.y)); v.Add(new Vector3(a.x,.5f,a.y));
            v.Add(new Vector3(b.x,.5f,b.y)); v.Add(new Vector3(b.x,-.5f,b.y));
            uv.AddRange(new[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) });
            triangles.AddRange(new[] { start,start+2,start+1,start,start+3,start+2 });
        }
        int top = v.Count; v.Add(new Vector3(0,.5f,0)); uv.Add(new Vector2(.5f,.5f));
        foreach (var p in ring) { v.Add(new Vector3(p.x,.5f,p.y)); uv.Add(new Vector2(p.x+.5f,p.y+.5f)); }
        for (int i=0;i<8;i++) topTriangles.AddRange(new[] { top,top+1+i,top+1+(i+1)%8 });
        var mesh = new Mesh { name = "Painted cover / native physical envelope / full face UV" };
        mesh.SetVertices(v); mesh.SetUVs(0,uv); mesh.subMeshCount=2; mesh.SetTriangles(triangles,0); mesh.SetTriangles(topTriangles,1); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }

    private void OnDestroy()
    {
        SetPainterlyVisible(false);
        foreach (var go in _visuals) if (go) Destroy(go);
        foreach (var mesh in _meshes) if (mesh) Destroy(mesh);
    }
}
