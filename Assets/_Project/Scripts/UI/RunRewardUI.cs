using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Modal room reward; input blocking is owned by RunBoons, not GUI event timing.</summary>
public sealed class RunRewardUI : MonoBehaviour
{
    private RunBoons _run;
    private GUIStyle _title, _body, _card;
    public void Bind(RunBoons run) => _run = run;

    private void Update()
    {
        if (!_run || !_run.IsChoosing || Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame) _run.Choose(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) _run.Choose(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) _run.Choose(2);
    }

    private void OnGUI()
    {
        if (!_run) return;
        if (_title == null)
        {
            _title = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _body = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            _card = new GUIStyle(GUI.skin.button) { fontSize = 18, wordWrap = true, padding = new RectOffset(22,22,18,18) };
        }
        Matrix4x4 previous = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        float width = Screen.width / scale, height = Screen.height / scale;
        if (!_run.IsChoosing)
        {
            GUI.Label(new Rect(20, 90, 400, 30), "Esquerdo: atacar   ·   Direito: mover   ·   Q/W/E/R: skills", _body);
            string acquired = "";
            foreach (RunBoons.Offer boon in _run.Acquired) acquired += boon.Title + "   ·   ";
            GUI.Label(new Rect(20,120,400,70), acquired, _body);
        }
        else
        {
            Color old = GUI.color;
            GUI.color = new Color(.025f,.04f,.08f,.97f);
            GUI.DrawTexture(new Rect(0,0,width,height), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(0,height/2-210,width,45), $"SALA {_run.RewardRoom} CONCLUÍDA", _title);
            GUI.Label(new Rect(0,height/2-158,width,50), "Escolha uma bênção para esta incursão.\nOs bônus acumulam até você morrer ou voltar ao Nexus.", _body);
            for (int i=0; i<_run.Choices.Count; i++)
            {
                var offer = _run.Choices[i];
                if (GUI.Button(new Rect(width/2-470+i*320,height/2-80,300,215),
                    $"[{i+1}]  {offer.Title}\n\n{offer.Description}", _card))
                { _run.Choose(i); break; }
            }
            GUI.Label(new Rect(0,height/2+156,width,35), "Clique em uma opção ou pressione 1, 2 ou 3 para continuar.", _body);
        }
        GUI.matrix = previous;
    }
}
