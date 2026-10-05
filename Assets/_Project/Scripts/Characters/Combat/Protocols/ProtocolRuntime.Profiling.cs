using System;
using Unity.Profiling;

public sealed partial class ProtocolRuntime
{
    private static readonly ProfilerMarker CpuMarker = new ProfilerMarker("TechGuy.Protocol.CPU");
    private int _profileDepth;
    internal ProtocolHitSnapshot CaptureSnapshot(Actor target)
    { using (Profile()) return new ProtocolHitSnapshot(target); }
    internal ProfileScope Profile() => new ProfileScope(this);
    internal readonly struct ProfileScope : IDisposable
    {
        private readonly ProtocolRuntime _owner;
        internal ProfileScope(ProtocolRuntime owner)
        {
            _owner = owner;
            if (++owner._profileDepth != 1) return;
            CpuMarker.Begin();
        }
        public void Dispose()
        {
            if (--_owner._profileDepth != 0) return;
            CpuMarker.End();
        }
    }
}
