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
    }

    [SerializeField] private Transform _player;
    [SerializeField] private LobbyArsenal _arsenal;
    [SerializeField] private Camera _camera;
    [SerializeField] private bool _blockoutGuide;
    [SerializeField] private Station[] _stations;
    [SerializeField, Min(1f)] private float _interactionRange = 3.5f;
    [SerializeField] private string _mainMenuScene = "MainMenu";
    private Station _nearest;
    private bool _showDetails;
    private bool _loading;
    private GUIStyle _heading, _body;
    private Texture2D _panel;
    private RunResultsPresenter _results;
    public void BindResults(RunResultsPresenter results) => _results = results;
    public void CloseDetailsForResults() => _showDetails = false;
    public bool IsNavigating => _loading;
    public bool BlocksWorldPointer => _arsenal && Mouse.current != null &&
        _arsenal.BlocksPointer(Mouse.current.position.ReadValue());
    public bool IsPanelOpen => _showDetails || _loading || (_results && _results.BlocksInput);

    public void ReturnToMainMenu()
    {
        if (_loading || (_results && _results.BlocksInput)) return;
        if (!Application.CanStreamedLevelBeLoaded(_mainMenuScene))
        { Debug.LogWarning("Main menu is not enabled in Build Settings: " + _mainMenuScene, this); return; }
        _loading = true;
        SceneManager.LoadSceneAsync(_mainMenuScene);
    }

    public void Configure(Transform player, Station[] stations)
    {
        _player = player;
        _stations = stations;
    }

    public bool BlocksAbilityInput(KeyCode key)
    {
        if (IsPanelOpen) return true;
        // The scene's arsenal is a separate object. Reserve its interaction before AbilityHolder polls input.
        if (_arsenal && _arsenal.BlocksAbilityInput(key)) return true;
        if (key != GamePreferences.Binding(GameControl.Skill3) || !_player || _stations == null) return false;
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
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || !_player || _loading || (_results && _results.BlocksInput)) return;
        if (_player.TryGetComponent(out PauseMenuUI pause) && pause.BlocksInput) return;
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
        if (_nearest == null || !GamePreferences.WasPressed(GameControl.Skill3)) return;
        if (_player.TryGetComponent(out CharControlScript inputOwner)) inputOwner.RequireAttackRelease();
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
    }

    private void OnGUI()
    {
        if (Time.timeScale <= 0f || !_player || (_results && _results.BlocksInput)) return;
        if (_player.TryGetComponent(out PauseMenuUI pause) && pause.BlocksInput) return;
        if (_heading == null)
        {
            _heading = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            _heading.normal.textColor = new Color(0.78f, 0.85f, 0.87f);
            _body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            _body.normal.textColor = new Color(0.86f, 0.9f, 0.97f);
            _panel = new Texture2D(1, 1);
            _panel.SetPixel(0, 0, new Color(0.025f, 0.035f, 0.08f, 0.93f));
            _panel.Apply();
        }
        Matrix4x4 oldMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        float width = Screen.width / scale;
        float height = Screen.height / scale;
        Rect guide = new Rect(24, 24, 370, 116);
        GUI.DrawTexture(guide, _panel);
        string equipped = "—";
        if (_player.TryGetComponent(out PlayerActor actor) && actor.CurrentWeapon)
        {
            switch (WeaponRunModifiers.Identify(actor.CurrentWeapon))
            {
                case RunWeaponFamily.Gauntlet: equipped = "Manopla"; break;
                case RunWeaponFamily.Bow: equipped = "Arco"; break;
                case RunWeaponFamily.Spear: equipped = "Lança"; break;
            }
        }
        GUI.Label(new Rect(40, 34, 338, 28), "NEXUS · Equipada: " + equipped, _heading);
        GUI.Label(new Rect(40, 66, 338, 50),
            (_blockoutGuide ? "Travessia via conexão central\n[" : "Arsenal à esquerda · Treino logo adiante\n[") + GamePreferences.MovementLabel +
            "] Mover · [" + GamePreferences.BindingLabel(GameControl.Skill3) + "] Interagir", _body);
        DrawIncursionDirection(width, height, scale);
        if (_nearest != null)
        {
            float panelHeight = _showDetails ? 180 : 84;
            Rect box = new Rect((width - 540) / 2, height - panelHeight - 190, 540, panelHeight);
            GUI.DrawTexture(box, _panel);
            GUI.Label(new Rect(box.x + 18, box.y + 10, 505, 30), _nearest.title, _heading);
            GUI.Label(new Rect(box.x + 18, box.y + 46, 505, panelHeight - 50),
                _loading ? "Conectando ao mundo..." : _showDetails ? _nearest.description :
                string.IsNullOrEmpty(_nearest.destinationScene) ? "[" + GamePreferences.BindingLabel(GameControl.Skill3) + "] Acessar terminal" : "[" + GamePreferences.BindingLabel(GameControl.Skill3) + "] Iniciar incursão", _body);
        }
        GUI.matrix = oldMatrix;
    }

    private void DrawIncursionDirection(float width, float height, float scale)
    {
        if (!_camera || _stations == null) return;
        foreach (var station in _stations)
        {
            if (station?.anchor == null || string.IsNullOrEmpty(station.destinationScene)) continue;
            if (_nearest == station) return; // The existing proximity prompt already owns this information.
            Vector3 point = _camera.WorldToScreenPoint(station.anchor.position);
            Vector2 direction = new Vector2(point.x / scale - width * .5f, height * .5f - point.y / scale);
            if (point.z < 0f) direction = -direction;
            string arrow = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
                ? direction.x > 0 ? "→" : "←" : direction.y > 0 ? "↓" : "↑";
            GUI.Label(new Rect(40, 102, 338, 26), "Incursão " + arrow + " · portal ciano", _body);
            return;
        }
    }

    private void OnDestroy()
    {
        if (_panel) Destroy(_panel);
    }
}
