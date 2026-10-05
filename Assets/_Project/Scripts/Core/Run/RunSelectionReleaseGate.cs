/// <summary>A new reward ordinal requires a later frame with released selection input.</summary>
public sealed class RunSelectionReleaseGate
{
    private int _claimedFrame;
    public bool CanConfirm { get; private set; } = true;
    public void RequireRelease(int frame) { _claimedFrame=frame; CanConfirm=false; }
    public void Observe(int frame,bool held)
    { if(!CanConfirm&&frame>_claimedFrame&&!held) CanConfirm=true; }
}
