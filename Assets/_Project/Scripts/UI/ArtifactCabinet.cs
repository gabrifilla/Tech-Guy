using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The artifact cabinet of the Nexus (Etapa 4b, 12/11/2026): where the artifacts are kept and one is equipped for
/// the next run. Each shelf niche holds one artifact — dark while unknown, lit once owned, gold when equipped.
/// Standing next to it shows a mini menu (no full-screen panel): keys 1..N equip or put back the artifact of
/// that niche; the interaction key puts the equipped one back. Visuals are primitives (no new asset).
/// </summary>
public sealed class ArtifactCabinet : MonoBehaviour
{
    public const float InteractionRadius = 1.7f;   // small: the terminal (2.2) and the pedestals (1.35) share the Oficina
    private static readonly Color Body = new Color(.10f, .13f, .17f), Dark = new Color(.05f, .06f, .08f),
        Owned = new Color(.30f, .92f, 1f), Equipped = new Color(1f, .82f, .32f);

    [SerializeField] private Transform _player;

    private MetaProgressStore _store;
    private MetaProgressProfile _profile;
    private readonly ProximityMenu _menu = new ProximityMenu();
    private readonly List<ProximityMenu.Option> _options = new List<ProximityMenu.Option>(5);
    private readonly List<Material> _nicheMaterials = new List<Material>();
    private readonly List<Material> _owned = new List<Material>();
    private Light _glow;

    public MetaProgressProfile Profile => _profile;

    /// <summary>Scene builders and tests bind the player here (no scene lookup at runtime).</summary>
    public void Configure(Transform player) => _player = player;

    private void Start()
    {
        _store = MetaProgressStore.CreateDefault();
        BuildWorld();
        Reload();
    }

    private bool PlayerInRange => _player &&
        Vector3.ProjectOnPlane(_player.position - transform.position, Vector3.up).sqrMagnitude <= InteractionRadius * InteractionRadius;

    private bool Blocked => GameplayModal.IsOpen || DialoguePanel.IsOpen || Time.timeScale <= 0f;

    /// <summary>Re-reads the profile and relights the niches (a gift handed elsewhere may have added an artifact).</summary>
    public void Reload()
    {
        _store ??= MetaProgressStore.CreateDefault();
        _profile = _store.Load();
        for (int i = 0; i < _nicheMaterials.Count && i < Artifacts.All.Count; i++)
        {
            var artifact = Artifacts.All[i];
            bool owned = _profile.HasArtifact(artifact.Id), equipped = _profile.HasEquipped(artifact.Id);
            Paint(_nicheMaterials[i], equipped ? Equipped : owned ? Owned : Dark, equipped ? 2.2f : owned ? 1.1f : 0f);
        }
        if (_glow) _glow.color = _profile.equippedArtifact.Length > 0 ? Equipped : Owned;
    }

    /// <summary>Equips the artifact of niche <paramref name="index"/>, or puts it back when it is the equipped one.</summary>
    public bool TryToggle(int index)
    {
        if (index < 0 || index >= Artifacts.All.Count) return false;
        var artifact = Artifacts.All[index];
        _profile = _store.Load();
        if (!_profile.HasArtifact(artifact.Id))
        { RunToast.Show("Este nicho ainda está vazio. Origem: " + artifact.Source + ".", new Color(.6f, .66f, .7f), 3f); return false; }
        bool wasEquipped = _profile.HasEquipped(artifact.Id);
        bool ok = _store.TryEquipArtifact(wasEquipped ? "" : artifact.Id, out _);
        Reload();
        if (ok) RunToast.Show(wasEquipped ? artifact.Name + " guardado no armário." : "Equipado: " + artifact.Name + ". Vale a partir da próxima run.", Equipped, 3.5f);
        return ok;
    }

    /// <summary>Puts the equipped artifact back. False when nothing is equipped.</summary>
    public bool TryStore()
    {
        _profile = _store.Load();
        if (_profile.equippedArtifact.Length == 0) return false;
        bool ok = _store.TryEquipArtifact("", out _);
        Reload();
        if (ok) RunToast.Show("Artefato guardado. Você parte sem nenhum.", Owned, 3f);
        return ok;
    }

    private bool _wasInRange;

    private void Update()
    {
        bool inRange = PlayerInRange;
        if (inRange && !_wasInRange) Reload(); // pick up artifacts earned since the last visit
        _wasInRange = inRange;
        if (Blocked || !inRange) return;
        for (int i = 0; i < Artifacts.All.Count; i++)
            if (_menu.WasChosen(i + 1)) { TryToggle(i); return; }
        if (_menu.WasChosen(0)) TryStore();
    }

    private void OnGUI()
    {
        if (Blocked || !PlayerInRange || _profile == null) return;
        _options.Clear();
        bool anyEquipped = _profile.equippedArtifact.Length > 0;
        _options.Add(new ProximityMenu.Option("Guardar o artefato equipado", "", anyEquipped, anyEquipped ? "" : "NENHUM"));
        int ownedCount = 0;
        foreach (var artifact in Artifacts.All)
        {
            bool owned = _profile.HasArtifact(artifact.Id), equipped = _profile.HasEquipped(artifact.Id);
            if (owned) ownedCount++;
            _options.Add(owned
                ? new ProximityMenu.Option(artifact.Name, artifact.Description, true, equipped ? "EQUIPADO" : "EQUIPAR")
                : new ProximityMenu.Option("Nicho vazio", "Origem: " + artifact.Source + ".", false, "BLOQUEADO"));
        }
        _menu.Draw("ARMÁRIO DE ARTEFATOS", ownedCount + "/" + Artifacts.All.Count + " encontrados  ·  1 equipado por run", _options, anyEquipped ? 4 : 2);
    }

    private void BuildWorld()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material Mat(Color color)
        {
            var material = new Material(shader);
            Paint(material, color, 0f);
            _owned.Add(material);
            return material;
        }
        var body = Mat(Body);
        Part(PrimitiveType.Cube, body, new Vector3(0, 1.05f, 0), new Vector3(2.6f, 2.1f, .5f), "Armário");
        Part(PrimitiveType.Cube, Mat(new Color(.16f, .20f, .25f)), new Vector3(0, 2.16f, 0), new Vector3(2.8f, .12f, .62f), "Tampo");
        Part(PrimitiveType.Cube, Mat(new Color(.16f, .20f, .25f)), new Vector3(0, .05f, 0), new Vector3(2.8f, .10f, .62f), "Base");
        int count = Artifacts.All.Count;
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) * .5f) * .6f;
            Part(PrimitiveType.Cube, Mat(Dark), new Vector3(x, 1.25f, -.22f), new Vector3(.46f, .62f, .08f), "Nicho " + (i + 1) + " — fundo");
            var niche = Mat(Dark); _nicheMaterials.Add(niche);
            var item = Part(PrimitiveType.Cube, niche, new Vector3(x, 1.25f, -.30f), new Vector3(.20f, .20f, .20f), "Nicho " + (i + 1) + " — artefato");
            item.transform.localRotation = Quaternion.Euler(35f, 45f, 0f);
        }
        _glow = new GameObject("Luz do armário").AddComponent<Light>();
        _glow.transform.SetParent(transform, false); _glow.transform.localPosition = new Vector3(0, 1.6f, -1.1f);
        _glow.type = LightType.Point; _glow.color = Owned; _glow.intensity = 2f; _glow.range = 5f;
    }

    private GameObject Part(PrimitiveType type, Material material, Vector3 position, Vector3 scale, string name)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name; part.transform.SetParent(transform, false); part.transform.localPosition = position; part.transform.localScale = scale;
        if (part.TryGetComponent(out Collider collider)) Destroy(collider);
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    private static void Paint(Material material, Color color, float emission)
    {
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (!material.HasProperty("_EmissionColor")) return;
        if (emission > 0f) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * emission); }
        else material.SetColor("_EmissionColor", Color.black);
    }

    private void OnDestroy() { foreach (var material in _owned) if (material) Destroy(material); }
}
