using NUnit.Framework;
namespace TechGuy.Tests.EditMode
{
    public sealed class RunSelectionReleaseGateTests
    {
        [Test] public void SameFrameAndHeldSelectionCannotConfirmSecondOrdinal()
        {
            var gate=new RunSelectionReleaseGate(); Assert.IsTrue(gate.CanConfirm);
            gate.RequireRelease(10); gate.Observe(10,false); Assert.IsFalse(gate.CanConfirm);
            gate.Observe(11,true); gate.Observe(12,true); Assert.IsFalse(gate.CanConfirm);
            gate.Observe(13,false); Assert.IsTrue(gate.CanConfirm);
            gate.RequireRelease(13); Assert.IsFalse(gate.CanConfirm);
        }
    }
}
