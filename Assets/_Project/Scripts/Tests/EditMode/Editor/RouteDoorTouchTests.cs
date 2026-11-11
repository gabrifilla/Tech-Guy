using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class RouteDoorTouchTests
    {
        // Gate 4 m wide, 6 m tall, 0.6 m thick, like the FirstSector "Encounter gate".
        private static readonly Bounds North = new Bounds(new Vector3(0, 3, 11), new Vector3(4, 6, .6f));
        private static readonly Bounds East = new Bounds(new Vector3(16, 3, 0), new Vector3(.6f, 6, 4));

        [Test] public void DistanceIgnoresHeightAndMeasuresToTheGateFace()
        {
            Assert.AreEqual(0f, RouteDoorTouch.Distance(North, new Vector3(1, 0, 11)), 1e-4f);
            Assert.AreEqual(1.7f, RouteDoorTouch.Distance(North, new Vector3(0, 0, 9)), 1e-4f);
        }

        [Test] public void PlayerStoppedByTheClosedGateCountsAsTouchingButRoomCentreDoesNot()
        {
            // A capsule of radius ~0.5 pressed against the gate stops about 0.5 m from its face.
            Assert.Less(RouteDoorTouch.Distance(North, new Vector3(0, 0, 10.2f)), RouteDoorTouch.TouchDistance);
            Assert.Greater(RouteDoorTouch.Distance(North, Vector3.zero), RouteDoorTouch.ArmDistance);
        }

        [Test] public void NearestPicksTheDoorThePlayerIsAgainst()
        {
            var doors = new[] { new RouteDoor(3, East), new RouteDoor(4, North) };
            Assert.AreEqual(1, RouteDoorTouch.Nearest(doors, new Vector3(0, 0, 10.3f), out float distance));
            Assert.Less(distance, RouteDoorTouch.TouchDistance);
            Assert.AreEqual(0, RouteDoorTouch.Nearest(doors, new Vector3(15.3f, 0, 0), out _));
            Assert.AreEqual(-1, RouteDoorTouch.Nearest(new RouteDoor[0], Vector3.zero, out _));
        }
    }
}
