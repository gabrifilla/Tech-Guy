using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Local lobby guide and proximity interaction; stations are assigned by the scene.</summary>
public sealed class LobbyInteraction : MonoBehaviour
{
    [Serializable]
    public sealed class Station
    {
        public Transform anchor;
        public string title;
        [TextArea] public string description;
        [Tooltip("Empty for an informational station.")]
        public string destinationScene;
        public bool weaponSelection;
    }

    [SerializeField] private Transform _player;
    [SerializeField] private Station[] _stations;
    [SerializeField, Min(1f)] private float _interactionRange = 3.5f;
    private Station _nearest;
    private bool _showDetails;
    private bool _loading;
    private GUIStyle _heading, _body, _hint;
    private Texture2D _panel;
    private WeaponScript[] _weapons;
    private int _previewWeapon;
    public bool IsPanelOpen => _showDetails || _loading;

    public void Configure(Transform player, Station[] stations)
    {
        _player = player;
        _stations = stations;
    }

    public bool BlocksAbilityInput(KeyCode key)
    {
        if (_loading || _showDetails) return true;
        if (key != KeyCode.E || !_player || _stations == null) return false;
        foreach (Station station in _stations)
            if (station?.anchor && (_player.position - station.anchor.position).sqrMagnitude < _interactionRange * _interactionRange)
                return true;
        return false;
    }

    private void Awake()
    {
        if (!_player)
        {
            Debug.LogError("LobbyInteraction requires a player reference.", this);
            enabled = false;
        }
        _weapons = Array.ConvertAll(WeaponLoadout.ResourcePaths, path => Resources.Load<WeaponScript>(path));
    }

    private void Update()
    {
        if (!_player || _loading) return;
        Station nearest = null;
        float bestDistance = _interactionRange * _interactionRange;
        if (_stations != null)
            foreach (Station station in _stations)
            {
                if (station == null || !station.anchor) continue;
                float distance = (_player.position - station.anchor.position).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                nearest = station;
            }
        if (_nearest != nearest) _showDetails = false;
        _nearest = nearest;
        if (Keyboard.current == null) return;
        if (Keyboard.current.escapeKey.wasPressedThisFrame) _showDetails = false;
        if (_nearest == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
        if (string.IsNullOrEmpty(_nearest.destinationScene)) _showDetails = !_showDetails;
        else if (Application.CanStreamedLevelBeLoaded(_nearest.destinationScene))
        {
            _loading = true;
            SceneManager.LoadSceneAsync(_nearest.destinationScene);
        }
        else
        {
            Debug.LogWarning("Lobby destination is not enabled in Build Settings: " + _nearest.destinationScene, this);
        }
        if (_showDetails && _player.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent) && agent.isOnNavMesh)
            agent.ResetPath();
        if (_showDetails && _player.TryGetComponent(out CharControlScript control)) control.CancelCombo();
        if (_showDetails && _nearest.weaponSelection && _player.TryGetComponent(out PlayerActor actor))
        {
            int equipped = Array.IndexOf(_weapons, actor.CurrentWeapon);
            _previewWeapon = equipped >= 0 ? equipped : 0;
        }
    }

    private void OnGUI()
    {
        if (!_player) return;
        if (_heading == null)
        {
            _heading = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            _heading.normal.textColor = new Color(0.78f, 0.85f, 0.87f);
            _body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            _body.normal.textColor = new Color(0.86f, 0.9f, 0.97f);
            _hint = new GUIStyle(_body) { fontSize = 12 };
            _panel = new Texture2D(1, 1);
            _panel.SetPixel(0, 0, new Color(0.025f, 0.035f, 0.08f, 0.93f));
            _panel.Apply();
        }
        Matrix4x4 oldMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        float width = Screen.width / scale;
        float height = Screen.height / scale;
        GUI.Label(new Rect(30, 28, 220, 30), "Nexus · Ponto Zero", _heading);
        GUI.Label(new Rect(30, 57, 220, 24), "Área segura", _hint);
        GUI.Label(new Rect(30, 82, 650, 28),
            "Esquerdo: atacar     ·     Direito: mover     ·     E: interagir     ·     Esc: fechar", _hint);
        if (_nearest != null)
        {
            if (_showDetails && _nearest.weaponSelection)
            {
                DrawArsenal(width, height);
                GUI.matrix = oldMatrix;
                return;
            }
            float panelHeight = _showDetails ? 180 : 84;
            Rect box = new Rect((width - 540) / 2, height - panelHeight - 190, 540, panelHeight);
            GUI.DrawTexture(box, _panel);
            GUI.Label(new Rect(box.x + 18, box.y + 10, 505, 30), _nearest.title, _heading);
            GUI.Label(new Rect(box.x + 18, box.y + 46, 505, panelHeight - 50),
                _loading ? "Conectando ao mundo..." : _showDetails ? _nearest.description :
                string.IsNullOrEmpty(_nearest.destinationScene) ? "[E] Acessar terminal" : "[E] Iniciar incursão", _body);
        }
        GUI.matrix = oldMatrix;
    }

    private void DrawArsenal(float width, float height)
    {
        Rect box = new Rect((width - 860) / 2, (height - 420) / 2 - 30, 860, 420);
        GUI.DrawTexture(box, _panel);
        GUI.Label(new Rect(box.x + 24, box.y + 18, 500, 30), "ARSENAL / PREPARAR INCURSÃO", _heading);
        GUI.Label(new Rect(box.xMax - 320, box.y + 20, 240, 26), $"MOEDAS: {CurrencyWallet.Balance}", _heading);
        if (GUI.Button(new Rect(box.xMax - 75, box.y + 18, 50, 28), "Esc")) _showDetails = false;
        string[] titles = { "MANOPLA", "ARCO E FLECHA", "LANÇA" };
        string[] styles = { "Principal · Combos e energia Asura", "Precisão · Projéteis e controle de área", "Alcance · Estocadas e varreduras" };
        var actor = _player.GetComponent<PlayerActor>();
        for (int i = 0; i < titles.Length; i++)
        {
            bool equipped = actor && actor.CurrentWeapon == _weapons[i];
            bool unlocked = WeaponLoadout.IsUnlocked(i);
            GUI.backgroundColor = _previewWeapon == i ? new Color(.25f, .65f, .85f) :
                unlocked ? Color.white : new Color(.45f, .45f, .5f);
            string caption = titles[i] + (equipped ? "  ·  EQUIPADA" : unlocked ? "" : $"  ·  {WeaponLoadout.GetCost(i)} moedas");
            if (GUI.Button(new Rect(box.x + 24 + i * 274, box.y + 62, 262, 60), caption)) _previewWeapon = i;
        }
        GUI.backgroundColor = Color.white;
        GUI.Label(new Rect(box.x + 24, box.y + 137, 810, 28), styles[_previewWeapon], _body);
        WeaponScript selected = _weapons[_previewWeapon];
        if (selected && selected.abilities != null)
            for (int i = 0; i < Mathf.Min(4, selected.abilities.Length); i++)
            {
                Ability skill = selected.abilities[i];
                if (!skill) continue;
                string detail = skill is ArsenalAbility arsenal ? arsenal.Description :
                    new[] { "Avanço e dois socos · Impulso", "Sequência de socos e finalizador · Impulso",
                        "Dois impactos com dano de postura · Choque", "Consome 100 de energia Asura para liberar a rajada" }[i];
                GUI.Label(new Rect(box.x + 24, box.y + 179 + i * 42, 810, 22),
                    $"[{new[] { "Q", "W", "E", "R" }[i]}]  {skill.DisplayName}   ·   {skill.ManaCost:0} mana   ·   {skill.cooldownTime:0.#}s", _body);
                GUI.Label(new Rect(box.x + 60, box.y + 201 + i * 42, 775, 20), detail, _hint);
            }
        bool alreadyEquipped = actor && selected && actor.CurrentWeapon == selected;
        bool previewUnlocked = WeaponLoadout.IsUnlocked(_previewWeapon);
        var actionRect = new Rect(box.xMax - 270, box.yMax - 54, 245, 34);
        if (!previewUnlocked)
        {
            int cost = WeaponLoadout.GetCost(_previewWeapon);
            bool canAfford = CurrencyWallet.CanAfford(cost);
            GUI.enabled = selected && canAfford;
            if (GUI.Button(actionRect, $"LIBERAR · {cost} moedas") && WeaponLoadout.TryUnlock(_previewWeapon))
                WeaponLoadout.Select(actor, _previewWeapon);
            GUI.enabled = true;
            GUI.Label(new Rect(box.x + 24, box.yMax - 48, 640, 28),
                canAfford ? "Junte moedas nas incursões para liberar novas armas.  ·  Esc para fechar"
                          : $"Moedas insuficientes ({CurrencyWallet.Balance}/{cost}).  Derrote inimigos para juntar mais.", _hint);
        }
        else
        {
            GUI.enabled = selected && !alreadyEquipped;
            if (GUI.Button(actionRect, alreadyEquipped ? "EQUIPADA" : "EQUIPAR " + titles[_previewWeapon]))
                WeaponLoadout.Select(actor, _previewWeapon);
            GUI.enabled = true;
            GUI.Label(new Rect(box.x + 24, box.yMax - 48, 520, 28), "Seleção salva para as próximas incursões.  ·  Esc para fechar", _hint);
        }
    }

    private void OnDestroy()
    {
        if (_panel) Destroy(_panel);
    }
}
