using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class WorldClickResolverTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private GameObject Box(string name, Vector3 position, Transform parent = null)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            go.transform.SetParent(parent);
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            return go;
        }
        [TearDown]
        public void TearDown()
        { foreach (var go in _objects) if (go) Object.DestroyImmediate(go); _objects.Clear(); }

        [Test]
        public void OwnBodyAndChildEquipmentNeverBecomeTheClickTarget()
        {
            var player = Box("Player", new Vector3(0, 2, 0));
            Box("Equipment", new Vector3(0, 3, 0), player.transform);
            var floor = Box("Floor", Vector3.zero);
            Physics.SyncTransforms();
            Assert.That(WorldClickResolver.TryResolve(new Ray(Vector3.up*6, Vector3.down), player.transform,
                ~0, out var hit, out bool crossed), Is.True);
            Assert.That(hit.collider.gameObject, Is.EqualTo(floor));
            Assert.That(crossed, Is.True);
        }

        [Test]
        public void EnemyBehindPlayerRemainsSelectableEvenOutsideMovementMask()
        {
            var player = Box("Player", Vector3.up*3);
            var enemy = Box("Enemy", Vector3.up);
            enemy.AddComponent<Interactable>();
            Physics.SyncTransforms();
            Assert.That(WorldClickResolver.TryResolve(new Ray(Vector3.up*6, Vector3.down), player.transform,
                1 << 8, out var hit, out _), Is.True);
            Assert.That(hit.collider.gameObject, Is.EqualTo(enemy));
        }

        [Test]
        public void NonWalkableWallBlocksEnemyBehindIt()
        {
            var player = Box("Player", Vector3.right*5);
            Box("Wall", Vector3.up*3);
            Box("Enemy", Vector3.up).AddComponent<Interactable>();
            Physics.SyncTransforms();
            Assert.That(WorldClickResolver.TryResolve(new Ray(Vector3.up*6, Vector3.down), player.transform,
                1 << 8, out _, out _), Is.False);
        }

        [Test]
        public void NonInteractiveTriggersDoNotBlockTheGround()
        {
            var player = Box("Player", Vector3.right*5);
            Box("Area trigger", Vector3.up*3).GetComponent<Collider>().isTrigger = true;
            var floor = Box("Floor", Vector3.zero);
            Physics.SyncTransforms();
            Assert.That(WorldClickResolver.TryResolve(new Ray(Vector3.up*6, Vector3.down), player.transform,
                ~0, out var hit, out bool crossed), Is.True);
            Assert.That(hit.collider.gameObject, Is.EqualTo(floor));
            Assert.That(crossed, Is.False);
        }

        [Test]
        public void DeadZoneIgnoresHeightAndAllowsIntentionalNearbyMovement()
        {
            Assert.That(WorldClickResolver.IsNearPlayer(new Vector3(.2f, 1, .2f), Vector3.zero, .55f), Is.True);
            Assert.That(WorldClickResolver.IsNearPlayer(Vector3.right*.8f, Vector3.zero, .55f), Is.False);
        }
    }
}
