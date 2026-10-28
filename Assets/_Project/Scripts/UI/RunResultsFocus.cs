using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Visible keyboard focus; independent of outcome color and hover tint.</summary>
[RequireComponent(typeof(UnityEngine.UI.Outline))]
public sealed class RunResultsFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    private UnityEngine.UI.Outline _outline;
    private void Awake() { _outline=GetComponent<UnityEngine.UI.Outline>();_outline.enabled=false; }
    public void OnSelect(BaseEventData data) { if(_outline)_outline.enabled=true; }
    public void OnDeselect(BaseEventData data) { if(_outline)_outline.enabled=false; }
    private void OnDisable() { if(_outline)_outline.enabled=false; }
}
