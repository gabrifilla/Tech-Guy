using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class WorldClickEnemyAssistTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private Camera _camera;
        private Transform _player;
        private Collider _floor;
        private RenderTexture _target;

        [SetUp]
        public void SetUp()
        {
            _player = Create("Assist player", Vector3.right * 5f).transform;
            var cameraObject = Create("Assist camera", Vector3.up * 10f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _target = new RenderTexture(1920, 1080, 0);
            _camera.targetTexture = _target;
            _camera.aspect = 16f / 9f;
            _floor = AddBox("Assist floor", Vector3.down * .5f, new Vector3(50f, .2f, 50f));
            Assert.AreEqual(1080, _camera.pixelHeight, "Pixel tolerance must be tested at a known projection size.");
        }

        [TearDown]
        public void TearDown()
        {
            if (_camera) _camera.targetTexture = null;
            foreach (var go in _objects) if (go) Object.DestroyImmediate(go);
            _objects.Clear();
            if (_target) { _target.Release(); Object.DestroyImmediate(_target); }
        }

        [Test]
        public void NearCursorMissSelectsTheVisibleEnemyInsteadOfTheGround()
        {
            var enemy = AddEnemy(Vector3.zero);
            Vector2 pointer = Pointer(Vector3.right * .11f);
            Physics.SyncTransforms();
            Assert.IsTrue(WorldClickResolver.TryResolve(_camera.ScreenPointToRay(pointer), _player,
                ~0, out var exact, out _));
            Assert.AreEqual(_floor, exact.collider, "The fixture must miss the actual collider before assistance.");
            Assert.IsTrue(Resolve(pointer, out var assisted, out _));
            Assert.AreEqual(enemy, assisted.collider.GetComponentInParent<Interactable>());
        }

        [Test]
        public void DirectEnemyHitWinsOverAnotherEnemyInsideTheCursorMargin()
        {
            var exact = AddEnemy(Vector3.zero);
            AddEnemy(Vector3.right * .13f);
            Physics.SyncTransforms();
            Assert.IsTrue(Resolve(Pointer(Vector3.zero), out var hit, out _));
            Assert.AreEqual(exact, hit.collider.GetComponentInParent<Interactable>());
        }

        [Test]
        public void CursorFarFromTheEnemyKeepsTheGroundClick()
        {
            AddEnemy(Vector3.zero);
            Physics.SyncTransforms();
            Assert.IsTrue(Resolve(Pointer(Vector3.right * .5f), out var hit, out _));
            Assert.AreEqual(_floor, hit.collider, "Assistance must remain local to the cursor.");
        }

        [Test]
        public void SolidWallBlocksAcquisitionInsideTheCursorMargin()
        {
            AddEnemy(Vector3.zero);
            var wall = AddBox("Assist blocking wall", Vector3.up * 3f, new Vector3(1f, .2f, 1f));
            wall.gameObject.layer = 8;
            Physics.SyncTransforms();
            Assert.IsFalse(WorldClickResolver.TryResolveWithEnemyAssist(_camera, Pointer(Vector3.right * .11f),
                _player, 1 << 0, out _, out _), "Nearby sample rays must not acquire an enemy through solid scenery.");
        }

        [Test]
        public void DirectInteractionIsPreservedBesideAnEnemy()
        {
            var item = AddBox("Assist interaction", Vector3.up * .5f, new Vector3(.1f, 1f, .1f));
            var interactable = item.gameObject.AddComponent<Interactable>();
            interactable.interactionType = InteractableType.Item;
            AddEnemy(Vector3.right * .13f);
            Physics.SyncTransforms();
            Assert.IsTrue(Resolve(Pointer(Vector3.zero), out var hit, out _));
            Assert.AreEqual(item, hit.collider, "Cursor assistance must not replace an explicit interaction.");
        }

        [Test]
        public void ClickingThePlayerKeepsTheAvatarProtectionInsteadOfSelectingANearbyEnemy()
        {
            _player.position = Vector3.zero;
            var body = _player.gameObject.AddComponent<BoxCollider>();
            body.center = Vector3.up * .5f;
            body.size = new Vector3(.1f, 1f, .1f);
            AddEnemy(Vector3.right * .13f);
            Physics.SyncTransforms();
            Assert.IsTrue(Resolve(Pointer(Vector3.zero), out var hit, out bool crossed));
            Assert.IsTrue(crossed);
            Assert.AreEqual(_floor, hit.collider);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void IneligibleNearbyEnemyCannotReplaceTheGroundClick(int state)
        {
            var enemy = AddEnemy(Vector3.zero, state != 0);
            var actor = enemy.GetComponent<Actor>();
            if (state == 1) actor.enabled = false;
            if (state == 2)
            {
                // Death is checked in the same frame, while its collider still exists.
                Object.DestroyImmediate(actor.GetComponent<CoinDrop>());
                actor.TakeDamage(actor.health);
                Assert.IsTrue(actor.IsDead);
            }
            Physics.SyncTransforms();
            Assert.IsTrue(Resolve(Pointer(Vector3.right * .11f), out var hit, out _));
            Assert.AreEqual(_floor, hit.collider, "Missing, disabled and dead actors are not eligible assisted targets.");
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(0f)]
        public void InvalidOrDisabledMarginPreservesTheExactGroundResult(float radius)
        {
            AddEnemy(Vector3.zero);
            Physics.SyncTransforms();
            Assert.IsTrue(WorldClickResolver.TryResolveWithEnemyAssist(_camera, Pointer(Vector3.right * .11f),
                _player, ~0, out var hit, out _, radius));
            Assert.AreEqual(_floor, hit.collider);
        }

        private bool Resolve(Vector2 pointer, out RaycastHit hit, out bool crossed) =>
            WorldClickResolver.TryResolveWithEnemyAssist(_camera, pointer, _player, ~0, out hit, out crossed);

        private Vector2 Pointer(Vector3 world) => _camera.WorldToScreenPoint(world);

        private GameObject Create(string name, Vector3 position)
        {
            var go = new GameObject(name);
            _objects.Add(go);
            go.transform.position = position;
            return go;
        }

        private Collider AddBox(string name, Vector3 position, Vector3 size)
        {
            var go = Create(name, position);
            var collider = go.AddComponent<BoxCollider>();
            collider.size = size;
            return collider;
        }

        private Interactable AddEnemy(Vector3 position, bool withActor = true)
        {
            var go = Create("Assist enemy", position);
            go.SetActive(false);
            var collider = go.AddComponent<BoxCollider>();
            collider.center = Vector3.up * .5f;
            collider.size = new Vector3(.1f, 1f, .1f);
            if (withActor) go.AddComponent<Actor>().health = 100f;
            var interactable = go.AddComponent<Interactable>();
            interactable.interactionType = InteractableType.Enemy;
            go.SetActive(true);
            return interactable;
        }
    }
}
