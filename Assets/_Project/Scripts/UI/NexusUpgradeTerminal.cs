using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Fase 1b Nexus terminal. Before the first ally is rescued it stands dark and inert. After the rescue the ally
/// ("Desconhecido", placeholder identity) stands beside it; interacting plays a short dialogue the first time (saved as
/// seen) and then opens the upgrade list — later visits open the list directly (name, one-line description, cost, state, balance). Each upgrade is bought once with Fragmentos and
/// saved at once in MetaProgress. Visuals are primitives (no new asset); the panel owns a <see cref="GameplayModal"/>.
/// Keys: arrows or W/S select · Enter/Space buy · Esc/Backspace/interaction close · mouse click buys.
/// </summary>
public sealed class NexusUpgradeTerminal : MonoBehaviour
{
    public const float InteractionRadius = 2.2f;
    private static readonly Color Gold = new Color(.83f, .71f, .46f, 1), Ink = new Color(.945f, .914f, .843f, 1),
        Muted = new Color(.60f, .66f, .70f, 1), Panel = new Color(.05f, .07f, .09f, .96f), Row = new Color(.09f, .12f, .15f, 1),
        RowSelected = new Color(.16f, .21f, .24f, 1), Cyan = new Color(.30f, .92f, 1f, 1), Owned = new Color(.45f, .80f, .55f, 1),
        Short = new Color(.90f, .63f, .60f, 1);

    [SerializeField] private Transform _player;

    private MetaProgressStore _store;
    private MetaProgressProfile _profile;
    private bool _active, _open;
    private int _selected, _openedFrame;
    private GameObject _panel;
    private TMP_Text _balance, _status;
    private readonly List<(Image background, TMP_Text name, TMP_Text detail, TMP_Text state)> _rows = new List<(Image, TMP_Text, TMP_Text, TMP_Text)>();
    private GUIStyle _prompt;
    private CharControlScript _inputOwner;

    public bool IsActive => _active;
    public bool IsOpen => _open;
    public MetaProgressProfile Profile => _profile;

    private void Start()
    {
        _store = MetaProgressStore.CreateDefault();
        _profile = _store.Load();
        _active = _profile.HasAlly(MetaAllies.Unknown);
        if (_player) _inputOwner = _player.GetComponent<CharControlScript>();
        BuildWorld();
    }

    private bool PlayerInRange => _player &&
        Vector3.ProjectOnPlane(_player.position - transform.position, Vector3.up).sqrMagnitude <= InteractionRadius * InteractionRadius;

    private void Update()
    {
        if (!_active) return;
        if (_open) { UpdatePanel(); return; }
        if (GameplayModal.IsOpen || !PlayerInRange) return;
        if (GamePreferences.WasPressed(GamePreferences.InteractionAction)) Talk();
    }

    /// <summary>True while the ally still has an unseen line; otherwise interacting opens the terminal straight away.</summary>
    public bool HasNewDialogue => _profile == null || !_profile.HasSeenDialogue(MetaAllies.UnknownNexusDialogueKey);

    /// <summary>Unseen dialogue first, then the terminal; once seen, the terminal opens directly (public for tests and tools).</summary>
    public void Talk()
    {
        if (!_active || _open || GameplayModal.IsOpen) return;
        if (!HasNewDialogue) { OpenTerminal(); return; }
        DialoguePanel.Show(MetaAllies.UnknownDisplayName, MetaAllies.UnknownNexusLines, () =>
        {
            _profile = _store.RecordDialogueSeen(MetaAllies.UnknownNexusDialogueKey);
            OpenTerminal();
        }, _inputOwner);
    }

    public void OpenTerminal()
    {
        if (!_active || _open) return;
        _profile = _store.Load();
        _open = true; _openedFrame = Time.frameCount; _selected = 0;
        GameplayModal.Enter();
        BuildPanel();
        Refresh("");
    }

    public void CloseTerminal()
    {
        if (!_open) return;
        _open = false;
        if (_panel) Destroy(_panel);
        _panel = null; _rows.Clear();
        GameplayModal.Exit(_inputOwner);
    }

    /// <summary>Buys the upgrade at <paramref name="index"/> once; false when owned or short of Fragmentos.</summary>
    public bool TryBuy(int index)
    {
        if (!_open || index < 0 || index >= MetaUpgrades.All.Count) return false;
        var upgrade = MetaUpgrades.All[index];
        bool bought = _store.TryPurchase(upgrade.Id, upgrade.Cost, out var profile);
        _profile = profile;
        Refresh(bought ? "Comprada: " + upgrade.Name + ". Ativa a partir da próxima run."
            : _profile.HasUpgrade(upgrade.Id) ? upgrade.Name + " já foi comprada."
            : "Fragmentos insuficientes para " + upgrade.Name + ".");
        return bought;
    }

    private void UpdatePanel()
    {
        if (Time.frameCount <= _openedFrame) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame || GamePreferences.WasPressed(GamePreferences.InteractionAction))
        { CloseTerminal(); return; }
        int count = MetaUpgrades.All.Count;
        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame) { _selected = (_selected + count - 1) % count; Refresh(null); }
        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) { _selected = (_selected + 1) % count; Refresh(null); }
        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) TryBuy(_selected);
    }

    private void Refresh(string status)
    {
        if (!_panel) return;
        _balance.text = "Saldo: " + _profile.fragments + " Fragmentos";
        if (status != null) _status.text = status;
        for (int i = 0; i < _rows.Count; i++)
        {
            var upgrade = MetaUpgrades.All[i]; var row = _rows[i];
            bool owned = _profile.HasUpgrade(upgrade.Id), affordable = _profile.fragments >= upgrade.Cost;
            row.background.color = i == _selected ? RowSelected : Row;
            row.name.text = upgrade.Name + "   ·   " + upgrade.Cost + " Fragmentos";
            row.detail.text = upgrade.Description;
            row.state.text = owned ? "COMPRADA" : affordable ? "DISPONÍVEL" : "SEM SALDO";
            row.state.color = owned ? Owned : affordable ? Cyan : Short;
        }
    }

    private void BuildPanel()
    {
        _panel = new GameObject("Upgrade terminal panel", typeof(RectTransform));
        _panel.transform.SetParent(transform, false);
        var canvas = _panel.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 255;
        var scaler = _panel.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        _panel.AddComponent<GraphicRaycaster>();
        var sheet = Rect("Sheet", _panel.transform, new Vector2(.22f, .10f), new Vector2(.78f, .90f));
        sheet.gameObject.AddComponent<Image>().color = Panel;
        Text(sheet, "TG  /  NEXUS  /  TERMINAL DE MELHORIAS", new Vector2(.04f, .92f), new Vector2(.96f, .97f), 18, Gold, FontStyles.Normal);
        Text(sheet, "Melhorias permanentes", new Vector2(.04f, .84f), new Vector2(.70f, .92f), 38, Ink, FontStyles.Bold);
        _balance = Text(sheet, "", new Vector2(.55f, .84f), new Vector2(.96f, .92f), 26, Cyan, FontStyles.Bold);
        _balance.alignment = TextAlignmentOptions.TopRight;
        for (int i = 0; i < MetaUpgrades.All.Count; i++)
        {
            int index = i; float top = .81f - i * .115f;
            var rowRect = Rect("Upgrade " + MetaUpgrades.All[i].Id, sheet, new Vector2(.04f, top - .105f), new Vector2(.96f, top));
            var background = rowRect.gameObject.AddComponent<Image>();
            var button = rowRect.gameObject.AddComponent<Button>(); button.targetGraphic = background; button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() => { _selected = index; TryBuy(index); });
            var name = Text(rowRect, "", new Vector2(.03f, .48f), new Vector2(.75f, .95f), 26, Ink, FontStyles.Bold);
            var detail = Text(rowRect, "", new Vector2(.03f, .05f), new Vector2(.75f, .50f), 20, Muted, FontStyles.Normal);
            var state = Text(rowRect, "", new Vector2(.75f, .25f), new Vector2(.97f, .75f), 22, Cyan, FontStyles.Bold);
            state.alignment = TextAlignmentOptions.MidlineRight;
            _rows.Add((background, name, detail, state));
        }
        _status = Text(sheet, "", new Vector2(.04f, .055f), new Vector2(.96f, .11f), 20, Gold, FontStyles.Normal);
        Text(sheet, "Setas / W S: escolher   ·   Enter / Espaço / clique: comprar   ·   Esc / Backspace / " +
            GamePreferences.BindingLabel(GamePreferences.InteractionAction) + ": fechar", new Vector2(.04f, .01f), new Vector2(.96f, .05f), 16, Muted, FontStyles.Normal);
    }

    private void BuildWorld()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material Mat(Color color, float emission)
        {
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (emission > 0 && material.HasProperty("_EmissionColor")) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * emission); }
            return material;
        }
        var body = new Color(.12f, .15f, .18f);
        Part(PrimitiveType.Cube, Mat(body, 0), new Vector3(0, .6f, 0), new Vector3(1.1f, 1.2f, .6f), "Terminal body");
        Part(PrimitiveType.Cube, _active ? Mat(Cyan, 1.4f) : Mat(new Color(.05f, .06f, .07f), 0), new Vector3(0, 1.45f, -.05f), new Vector3(1f, .65f, .08f), "Terminal screen");
        if (!_active) return;
        Part(PrimitiveType.Capsule, Mat(new Color(.55f, .58f, .62f), 0), new Vector3(1.3f, 1f, .2f), new Vector3(.55f, .9f, .55f), "Desconhecido");
        var glow = new GameObject("Terminal glow").AddComponent<Light>();
        glow.transform.SetParent(transform, false); glow.transform.localPosition = new Vector3(0, 1.8f, -.6f);
        glow.type = LightType.Point; glow.color = Cyan; glow.intensity = 2.5f; glow.range = 6f;
    }

    private void Part(PrimitiveType type, Material material, Vector3 position, Vector3 scale, string name)
    {
        var part = GameObject.CreatePrimitive(type);
        part.name = name; part.transform.SetParent(transform, false); part.transform.localPosition = position; part.transform.localScale = scale;
        if (part.TryGetComponent(out Collider collider)) Destroy(collider);
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void OnGUI()
    {
        if (!_active || _open || GameplayModal.IsOpen || !PlayerInRange) return;
        _prompt ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height / 36f), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        _prompt.normal.textColor = Cyan;
        GUI.Label(new Rect(0, Screen.height * .70f, Screen.width, Screen.height * .06f),
            "[" + GamePreferences.BindingLabel(GamePreferences.InteractionAction) + "] " +
            (HasNewDialogue ? "Falar com " + MetaAllies.UnknownDisplayName : "Terminal de melhorias"), _prompt);
    }

    private void OnDisable() => CloseTerminal();

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TMP_Text Text(Transform parent, string value, Vector2 min, Vector2 max, float size, Color color, FontStyles style)
    {
        var rect = Rect("Text", parent, min, max);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset) text.font = TMP_Settings.defaultFontAsset;
        text.text = value; text.fontSize = size; text.color = color; text.fontStyle = style; text.raycastTarget = false; text.richText = false;
        text.alignment = TextAlignmentOptions.TopLeft; text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }
}
