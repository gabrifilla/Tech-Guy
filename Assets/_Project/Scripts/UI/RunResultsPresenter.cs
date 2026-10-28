using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Nexus modal ownership, one read per opening and input-release barrier.</summary>
[DefaultExecutionOrder(-1200)]
[RequireComponent(typeof(RunResultsView))]
public sealed class RunResultsPresenter : MonoBehaviour
{
    [SerializeField] private PlayerActor _player;
    [SerializeField] private LobbyInteraction _lobby;
    [SerializeField] private TMP_FontAsset _font;
    [SerializeField] private EventSystem _eventSystem;
    private RunResultsView _view;
    private PauseMenuUI _pause;
    private CharControlScript _controls;
    private AbilityHolder _holder;
    private NavMeshAgent _agent;
    private bool _stopped, _ownsAgent, _cursorVisible;
    private CursorLockMode _cursorLock;
    private readonly RunSelectionReleaseGate _release = new RunSelectionReleaseGate();
    private GameObject _lastSelection;
    private int _openedFrame;
    public bool IsOpen { get; private set; }
    public bool BlocksInput => IsOpen || !_release.CanConfirm;
    public RunResultsModel Model { get; private set; }
    public RunResultsView View => _view;
    public void Configure(PlayerActor player,LobbyInteraction lobby,TMP_FontAsset font,EventSystem events)
    { _player=player;_lobby=lobby;_font=font;_eventSystem=events; }
    private void Start()
    {
        if(!_player||!_lobby||!_font||!_eventSystem)
        {Debug.LogError("RunResultsPresenter requires player, lobby, font and EventSystem.",this);enabled=false;return;}
        _view=GetComponent<RunResultsView>();_pause=_player.GetComponent<PauseMenuUI>();_controls=_player.GetComponent<CharControlScript>();_holder=_player.GetComponent<AbilityHolder>();_agent=_player.GetComponent<NavMeshAgent>();
        _lobby.BindResults(this);if(_pause)_pause.BindResults(this);
        _view.Build(_font,_eventSystem,OpenManual,CloseFromInput);
        string expected=RunResultsHandoff.Consume();if(!string.IsNullOrEmpty(expected))OpenExpected(expected);
    }
    public void OpenManual() => Open(null);
    public void OpenExpected(string expectedSession) => Open(expectedSession);
    private void Open(string expected)
    {
        if(IsOpen||!_view||(_pause&&_pause.BlocksInput)||_lobby.IsNavigating)return;
        // Reuse the existing safe stop: no retained cast/dash buffer may replay after this Nexus modal.
        if(_holder&&!_holder.TryStopEncounterOffense())return;
        RunSummary snapshot;string diagnostic;RunSummaryStore.TryRead(out snapshot,out diagnostic);
        Model=new RunResultsModel(snapshot,expected);IsOpen=true;_openedFrame=Time.frameCount;
        _cursorVisible=Cursor.visible;_cursorLock=Cursor.lockState;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        _ownsAgent=_agent&&_agent.isOnNavMesh&&_agent.enabled;
        if(_ownsAgent){_stopped=_agent.isStopped;_agent.isStopped=true;}
        if(_controls){_controls.CancelCombo();_controls.RequireAttackRelease();}
        _lobby.CloseDetailsForResults();_view.Show(Model);
    }
    private void CloseFromInput() { if(Time.frameCount>_openedFrame)Close(); }
    public void Close()
    {
        if(!IsOpen)return;
        if(_view.DetailOpen){_view.CloseDetail();return;}
        IsOpen=false;_release.RequireRelease(Time.frameCount);_view.Hide();
        if(_controls)_controls.RequireAttackRelease();
        // Keep ownership until a later released frame. No held key/click can reach the world.
    }
    private static bool Held()
    {
        var k=Keyboard.current;var m=Mouse.current;var g=Gamepad.current;
        return GamePreferences.AnyGameplayHeld || k!=null&&k.anyKey.isPressed || m!=null&&(m.leftButton.isPressed||m.rightButton.isPressed||m.middleButton.isPressed) ||
            g!=null&&(g.buttonSouth.isPressed||g.buttonEast.isPressed||g.startButton.isPressed);
    }
    private void Update()
    {
        if(!_view)return;
        _view.SetManualAvailable(!BlocksInput&&!(_pause&&_pause.BlocksInput)&&!_lobby.IsPanelOpen);
        if(!IsOpen)
        {
            var mouse=Mouse.current;
            if(_release.CanConfirm && mouse!=null && mouse.leftButton.wasPressedThisFrame && _view.ManualButton.IsInteractable() &&
                RectTransformUtility.RectangleContainsScreenPoint((RectTransform)_view.ManualButton.transform,mouse.position.ReadValue(),null))
            { OpenManual(); return; }
            if(_release.CanConfirm && Keyboard.current!=null && Keyboard.current.enterKey.wasPressedThisFrame &&
                _eventSystem.currentSelectedGameObject==_view.ManualButton.gameObject && _view.ManualButton.IsInteractable())
            { OpenManual(); return; }
            bool previouslyBlocked=!_release.CanConfirm;_release.Observe(Time.frameCount,Held());
            if(previouslyBlocked&&_release.CanConfirm)ReleaseOwnership();return;
        }
        var keyboard=Keyboard.current;
        if(Time.frameCount>_openedFrame&&keyboard!=null)
        {
            if(keyboard.escapeKey.wasPressedThisFrame){Close();return;}
            if(keyboard.tabKey.wasPressedThisFrame)_view.FocusNext(keyboard.shiftKey.isPressed);
            if(!_view.DetailOpen&&keyboard.pageDownKey.wasPressedThisFrame)_view.Scroll(-1);
            if(!_view.DetailOpen&&keyboard.pageUpKey.wasPressedThisFrame)_view.Scroll(1);
        }
        var pad=Gamepad.current;if(Time.frameCount>_openedFrame&&pad!=null&&pad.buttonEast.wasPressedThisFrame){Close();return;}
        if(_eventSystem.currentSelectedGameObject!=_lastSelection){_lastSelection=_eventSystem.currentSelectedGameObject;_view.EnsureSelectionVisible();}
    }
    private void ReleaseOwnership()
    {
        if(_ownsAgent&&_agent&&_agent.enabled&&_agent.isOnNavMesh)_agent.isStopped=_stopped;
        _ownsAgent=false;Cursor.visible=_cursorVisible;Cursor.lockState=_cursorLock;
    }
    private void OnDisable()
    {
        if(IsOpen||!_release.CanConfirm)ReleaseOwnership();
        IsOpen=false;_release.Observe(Time.frameCount+1,false);
        if(_view && _view.Sheet)_view.Hide();
    }
    private void OnDestroy()
    {
        if(IsOpen||!_release.CanConfirm)ReleaseOwnership();
        if(_lobby)_lobby.BindResults(null);if(_pause)_pause.BindResults(null);
    }
}
