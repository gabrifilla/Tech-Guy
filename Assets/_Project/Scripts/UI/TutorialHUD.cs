using UnityEngine;

public sealed class TutorialHUD : MonoBehaviour
{
    [SerializeField] private TutorialDirector _tutorial;
    private GUIStyle _title, _body, _caption;
    public void Configure(TutorialDirector tutorial) => _tutorial = tutorial;
    private void OnGUI()
    {
        if (Time.timeScale <= 0f || !_tutorial || !_tutorial.enabled || _tutorial.IsLeaving) return;
        if (_title == null)
        {
            _title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, wordWrap = true };
            _body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            _caption = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
        }
        Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        float width = Screen.width / scale;
        if (!_tutorial.RewardVisible)
        {
            GUI.color = new Color(.025f,.04f,.065f,.94f);
            GUI.DrawTexture(new Rect(24,24,470,250), Texture2D.whiteTexture);
            GUI.color = new Color(.3f,.85f,1);
            GUI.DrawTexture(new Rect(24,24,470,3), Texture2D.whiteTexture);
            GUI.Label(new Rect(42,36,430,22), "CALIBRAÇÃO  /  " + ((int)_tutorial.Stage+1) + " DE 7", _caption);
            GUI.color = Color.white;
            GUI.Label(new Rect(42,63,430,40), _tutorial.CurrentBeat.title, _title);
            GUI.color = new Color(1,.78f,.4f);
            GUI.Label(new Rect(42,103,430,28), _tutorial.CurrentBeat.control, _body);
            GUI.color = Color.white;
            GUI.Label(new Rect(42,137,430,85), _tutorial.CurrentBeat.instruction, _body);
            string progress = _tutorial.Stage == TutorialStage.BasicAttack ? _tutorial.BasicHits + " / 3 acertos" :
                _tutorial.Stage == TutorialStage.Encounter ? _tutorial.Defeated + " / " + _tutorial.EnemyCount + " drones" : "";
            GUI.Label(new Rect(42,226,430,24), progress, _caption);
            GUI.color = new Color(.75f,.85f,.92f);
            GUI.Label(new Rect(width-360,35,326,115), _tutorial.CurrentBeat.transmission, _body);
        }
        GUI.color = Color.white;
        GUI.Label(new Rect(width-250,165,226,40), "ESC  ·  Pausar / pular tutorial", _caption);
        GUI.matrix = matrix; GUI.color = color;
    }
}
