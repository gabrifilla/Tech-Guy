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
    private Station _nearest;
    private bool _showDetails;
    private bool _loading;
    private GUIStyle _heading, _body, _hint;
    private Texture2D _panel;

    public void Configure(Transform player, Station[] stations)
    {
        _player = player;
        _stations = stations;
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
        GUI.Label(new Rect(26, height - 35, 650, 28),
            "Clique para mover     ·     E  Interagir     ·     Esc  Fechar", _hint);
        if (_nearest != null)
        {
            float panelHeight = _showDetails ? 180 : 84;
            Rect box = new Rect((width - 540) / 2, height - panelHeight - 55, 540, panelHeight);
            GUI.DrawTexture(box, _panel);
            GUI.Label(new Rect(box.x + 18, box.y + 10, 505, 30), _nearest.title, _heading);
            GUI.Label(new Rect(box.x + 18, box.y + 46, 505, panelHeight - 50),
                _loading ? "Conectando ao mundo..." : _showDetails ? _nearest.description :
                string.IsNullOrEmpty(_nearest.destinationScene) ? "[E] Acessar terminal" : "[E] Iniciar incursão", _body);
        }
        GUI.matrix = oldMatrix;
    }

    private void OnDestroy()
    {
        if (_panel) Destroy(_panel);
    }
}
