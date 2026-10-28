using UnityEngine;

/// <summary>Short contact cue only. No collider, gameplay timer, or combat reservation.</summary>
public static class EclipseMarkVfx
{
    public static void Show(ProtocolRuntime runtime, Vector3 point, bool armed = false)
        => runtime.Feedback?.Emit(ProtocolFeedbackKind.EclipseMark, point, point, armed ? .6f : .35f);
}
