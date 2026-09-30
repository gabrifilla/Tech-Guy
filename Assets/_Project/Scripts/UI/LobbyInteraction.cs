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
    [SerializeField] private Station[] _stations;
    [SerializeField, Min(1f)] private float _interactionRange = 3.5f;
    [SerializeField] private string _mainMenuScene = "MainMenu";
    private Station _nearest;
    private bool _showDetails;
    private bool _loading;
    private GUIStyle _heading, _body, _hint;
    private Texture2D _panel;
    public bool IsPanelOpen => _showDetails || _loading;

    public void ReturnToMainMenu()
    {
        if (_loading) return;
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
        if (_loading || _showDetails) return true;
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
        if (Time.timeScale <= 0f || !_player || _loading) return;
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
        if (Time.timeScale <= 0f || !_player) return;
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
        if (GUI.Button(new Rect(width - 210, 28, 180, 36), "Menu inicial")) ReturnToMainMenu();
        GUI.Label(new Rect(30, 28, 220, 30), "Nexus · Ponto Zero", _heading);
        GUI.Label(new Rect(30, 57, 220, 24), "Área segura", _hint);
        GUI.Label(new Rect(30, 82, 650, 28),
            GamePreferences.BindingLabel(GameControl.Primary) + ": selecionar / mover · " + GamePreferences.BindingLabel(GameControl.Skill3) + ": interagir · Esc: pausar", _hint);
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

    private void OnDestroy()
    {
        if (_panel) Destroy(_panel);
    }
}
