using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TechGuy.Cameras;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    [Category("CameraOcclusion")]
    public sealed class CameraOcclusionFadeTests
    {
        private static readonly Vector3 Origin = new Vector3(5000f, 0f, 5000f);
        private const float FadedOpacity = .12f;
        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<Material> _materials = new List<Material>();
        private CameraOcclusionFader _fader;
        private Transform _target;
        private Material _source;
        private float _previousTimeScale;

        private Vector3 CameraPosition => _target.position + new Vector3(0f, 9f, -10f);

        [SetUp]
        public void SetUp()
        {
            _previousTimeScale = Time.timeScale;
            _target = NewObject("Occlusion test target", Origin).transform;
            _source = NewMaterial();
            _fader = new CameraOcclusionFader(pathRevealDistance: 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _previousTimeScale;
            _fader?.Dispose();
            for (int i = _objects.Count - 1; i >= 0; i--)
                if (_objects[i]) Object.DestroyImmediate(_objects[i]);
            for (int i = _materials.Count - 1; i >= 0; i--)
                if (_materials[i]) Object.DestroyImmediate(_materials[i]);
            _objects.Clear();
            _materials.Clear();
        }

        [Test]
        public void EverySolidObstacleBetweenCameraAndPlayerFadesWithoutRemovingCollision()
        {
            var first = Wall("Near camera wall", -5f);
            var second = Wall("Near player wall", -3f);
            Physics.SyncTransforms();

            TickToCompletion();

            Assert.That(_fader.IsFaded(first.GetComponent<Collider>()), Is.True);
            Assert.That(_fader.IsFaded(second.GetComponent<Collider>()), Is.True);
            Assert.That(Opacity(first.GetComponent<MeshRenderer>().sharedMaterial), Is.EqualTo(FadedOpacity).Within(.001f));
            Assert.That(Opacity(second.GetComponent<MeshRenderer>().sharedMaterial), Is.EqualTo(FadedOpacity).Within(.001f));
            var ray = new Ray(CameraPosition, (_target.position + Vector3.up * 1.7f - CameraPosition).normalized);
            foreach (var wall in new[] { first, second })
            {
                var collider = wall.GetComponent<Collider>();
                Assert.That(collider.enabled, Is.True);
                Assert.That(collider.isTrigger, Is.False);
                Assert.That(collider.Raycast(ray, out _, 20f), Is.True, "Visual fading must preserve the physical wall.");
            }
        }

        [Test]
        public void FadingUsesPrivateMaterialsAndRestoresTheExactOriginalReferences()
        {
            var wall = Wall("Obstruction", -5f);
            var untouched = Wall("Another user of the same material", -5f);
            untouched.transform.position += Vector3.right * 12f;
            var renderer = wall.GetComponent<MeshRenderer>();
            Color authoredColor = MaterialColor(_source);
            int authoredQueue = _source.renderQueue;
            Physics.SyncTransforms();

            TickToCompletion();

            var runtimeMaterial = renderer.sharedMaterial;
            Assert.That(runtimeMaterial, Is.Not.SameAs(_source));
            Assert.That(Opacity(runtimeMaterial), Is.EqualTo(FadedOpacity).Within(.001f));
            Assert.That(runtimeMaterial.GetTag("RenderType", false), Is.EqualTo("Transparent"));
            Assert.That(runtimeMaterial.GetFloat("_ZWrite"), Is.EqualTo(0f));
            Assert.That(MaterialColor(_source), Is.EqualTo(authoredColor));
            Assert.That(_source.renderQueue, Is.EqualTo(authoredQueue));
            Assert.That(untouched.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_source));

            wall.transform.position += Vector3.right * 12f;
            Physics.SyncTransforms();
            TickToCompletion();

            Assert.That(renderer.sharedMaterial, Is.SameAs(_source));
            Assert.That(_fader.IsFaded(wall.GetComponent<Collider>()), Is.False);
            Assert.That(MaterialColor(_source), Is.EqualTo(authoredColor));
        }

        [Test]
        public void FadeIsGradualAndBriefLossOfOcclusionDoesNotCauseFlicker()
        {
            var wall = Wall("Obstruction", -5f);
            var renderer = wall.GetComponent<MeshRenderer>();
            Physics.SyncTransforms();

            _fader.Tick(CameraPosition, _target, .03f);
            float partialOpacity = Opacity(renderer.sharedMaterial);
            Assert.That(partialOpacity, Is.LessThan(1f).And.GreaterThan(FadedOpacity));
            TickToCompletion();
            var runtimeMaterial = renderer.sharedMaterial;

            wall.transform.position += Vector3.right * 12f;
            Physics.SyncTransforms();
            _fader.Tick(CameraPosition, _target, .06f);
            Assert.That(renderer.sharedMaterial, Is.SameAs(runtimeMaterial));
            Assert.That(Opacity(runtimeMaterial), Is.EqualTo(FadedOpacity).Within(.001f), "The restore delay should absorb short probe gaps.");

            _fader.Tick(CameraPosition, _target, .07f);
            Assert.That(Opacity(renderer.sharedMaterial), Is.LessThan(1f).And.GreaterThan(FadedOpacity), "Reappearance should also interpolate.");
            TickToCompletion();
            Assert.That(renderer.sharedMaterial, Is.SameAs(_source));
        }

        [TestCase("Actor")]
        [TestCase("Interactable")]
        [TestCase("Trigger")]
        [TestCase("Ground")]
        [TestCase("Player")]
        public void GameplayActorsInteractionsTriggersGroundAndPlayerNeverFade(string excludedKind)
        {
            var excluded = Wall("Excluded " + excludedKind, -5f);
            if (excludedKind == "Actor")
            {
                var actorRoot = NewObject("Actor owner", excluded.transform.position);
                actorRoot.AddComponent<Actor>();
                excluded.transform.SetParent(actorRoot.transform, true);
            }
            else if (excludedKind == "Interactable")
            {
                var interactionRoot = NewObject("Interactable owner", excluded.transform.position);
                interactionRoot.AddComponent<Interactable>();
                excluded.transform.SetParent(interactionRoot.transform, true);
            }
            else if (excludedKind == "Trigger") excluded.GetComponent<Collider>().isTrigger = true;
            else if (excludedKind == "Ground")
            {
                excluded.transform.position = Origin + Vector3.down * .1f;
                excluded.transform.localScale = new Vector3(20f, .2f, 20f);
            }
            else excluded.transform.SetParent(_target, true);
            var positiveControl = Wall("Eligible wall", -3f);
            Physics.SyncTransforms();

            TickToCompletion();

            Assert.That(_fader.IsFaded(excluded.GetComponent<Collider>()), Is.False);
            Assert.That(excluded.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_source));
            Assert.That(_fader.IsFaded(positiveControl.GetComponent<Collider>()), Is.True, "The fixture must still detect a real obstruction.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ChildCollidersResolveParentMeshesAndRigidbodySiblingMeshes(bool siblingMesh)
        {
            var root = NewObject("Occluder prefab root", Origin + new Vector3(0f, 3f, -5f));
            GameObject mesh;
            if (siblingMesh)
            {
                var body = root.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                mesh = Cube("Mesh sibling", root.transform.position, new Vector3(3f, 6f, .35f));
                Object.DestroyImmediate(mesh.GetComponent<Collider>());
                mesh.transform.SetParent(root.transform, true);
            }
            else
            {
                mesh = Cube("Parent mesh", root.transform.position, new Vector3(3f, 6f, .35f));
                Object.DestroyImmediate(mesh.GetComponent<Collider>());
                mesh.transform.SetParent(root.transform, true);
                root = mesh;
            }
            var colliderObject = NewObject("Collider child", mesh.transform.position);
            colliderObject.transform.SetParent(root.transform, true);
            var collider = colliderObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(3f, 6f, .35f);
            Physics.SyncTransforms();

            TickToCompletion();

            Assert.That(_fader.IsFaded(collider), Is.True);
            Assert.That(mesh.GetComponent<MeshRenderer>().sharedMaterial, Is.Not.SameAs(_source));
            Assert.That(Opacity(mesh.GetComponent<MeshRenderer>().sharedMaterial), Is.EqualTo(FadedOpacity).Within(.001f));
            _fader.Dispose();
            Assert.That(mesh.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_source));
        }

        [Test]
        public void ForwardPathProbeRevealsAnObstructionBeyondThePlayersFeet()
        {
            var ahead = Cube("Wall in the path ahead", Origin + new Vector3(0f, 1f, 1.2f), new Vector3(2f, 2f, .4f));
            Physics.SyncTransforms();
            TickToCompletion();
            Assert.That(_fader.IsFaded(ahead.GetComponent<Collider>()), Is.False, "The wall is outside the body probes.");

            _fader.Dispose();
            _fader = new CameraOcclusionFader(pathRevealDistance: 2.5f);
            TickToCompletion();

            Assert.That(_fader.IsFaded(ahead.GetComponent<Collider>()), Is.True);
            Assert.That(Opacity(ahead.GetComponent<MeshRenderer>().sharedMaterial), Is.EqualTo(FadedOpacity).Within(.001f));
        }

        [Test]
        public void ObstacleContainingTheCameraIsAlsoRevealed()
        {
            var enclosing = Cube("Camera inside wall", CameraPosition, new Vector3(3f, 3f, 3f));
            Physics.SyncTransforms();

            TickToCompletion();

            Assert.That(_fader.IsFaded(enclosing.GetComponent<Collider>()), Is.True);
            Assert.That(Opacity(enclosing.GetComponent<MeshRenderer>().sharedMaterial), Is.EqualTo(FadedOpacity).Within(.001f));
        }

        [Test]
        public void ChangingTargetImmediatelyRestoresThePreviousTargetsObstructions()
        {
            var wall = Wall("Old target obstruction", -5f);
            Physics.SyncTransforms();
            TickToCompletion();
            var nextTarget = NewObject("Next target", Origin + Vector3.right * 20f).transform;

            _fader.Tick(nextTarget.position + new Vector3(0f, 9f, -10f), nextTarget, 0f);

            Assert.That(wall.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_source));
            Assert.That(_fader.IsFaded(wall.GetComponent<Collider>()), Is.False);
        }

        [Test]
        public void LosingTheTargetImmediatelyRestoresItsObstructions()
        {
            var wall = Wall("Lost target obstruction", -5f);
            Physics.SyncTransforms();
            TickToCompletion();

            _fader.Tick(CameraPosition, null, 0f);

            Assert.That(wall.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_source));
            Assert.That(_fader.IsFaded(wall.GetComponent<Collider>()), Is.False);
        }

        [Test]
        public void UnscaledFadeAndRestoreContinueDuringPause()
        {
            var wall = Wall("Paused obstruction", -5f);
            Physics.SyncTransforms();
            Time.timeScale = 0f;

            TickToCompletion();
            Assert.That(_fader.IsFaded(wall.GetComponent<Collider>()), Is.True);
            Assert.That(Opacity(wall.GetComponent<MeshRenderer>().sharedMaterial), Is.EqualTo(FadedOpacity).Within(.001f));
            wall.transform.position += Vector3.right * 12f;
            Physics.SyncTransforms();
            TickToCompletion();

            Assert.That(wall.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_source));
            Assert.That(_fader.IsFaded(wall.GetComponent<Collider>()), Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
        }

        [Test]
        public void DisposeImmediatelyRestoresMaterialsAndCanBeCalledTwice()
        {
            var wall = Wall("Disposed obstruction", -5f);
            Physics.SyncTransforms();
            TickToCompletion();

            _fader.Dispose();
            _fader.Dispose();

            Assert.That(wall.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_source));
            Assert.That(_fader.IsFaded(wall.GetComponent<Collider>()), Is.False);
            Assert.That(wall.GetComponent<Collider>().enabled, Is.True);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void NonFiniteAuthoringValuesFallBackToAWorkingFiniteFade(float invalid)
        {
            _fader.Dispose();
            _fader = new CameraOcclusionFader(invalid, invalid, invalid, invalid, invalid, invalid);
            var wall = Wall("Invalid configuration obstruction", -5f);
            Physics.SyncTransforms();

            TickToCompletion();

            float opacity = Opacity(wall.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.That(float.IsNaN(opacity) || float.IsInfinity(opacity), Is.False);
            Assert.That(opacity, Is.EqualTo(FadedOpacity).Within(.001f));
            Assert.That(_fader.IsFaded(wall.GetComponent<Collider>()), Is.True);
            wall.transform.position += Vector3.right * 12f;
            Physics.SyncTransforms();
            TickToCompletion();
            Assert.That(wall.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_source));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1f)]
        public void InvalidDeltaDoesNotCorruptOpacityOrAdvanceTheRestoreClock(float invalidDelta)
        {
            var wall = Wall("Invalid delta obstruction", -5f);
            Physics.SyncTransforms();
            TickToCompletion();
            var fadedMaterial = wall.GetComponent<MeshRenderer>().sharedMaterial;
            wall.transform.position += Vector3.right * 12f;
            Physics.SyncTransforms();

            _fader.Tick(CameraPosition, _target, invalidDelta);

            Assert.That(wall.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(fadedMaterial));
            Assert.That(Opacity(fadedMaterial), Is.EqualTo(FadedOpacity).Within(.001f));
            TickToCompletion();
            Assert.That(wall.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_source));
        }

        [Test]
        public void IllustratedMaterialUsesItsDitherOpacityWithoutChangingSourceOrRenderMode()
        {
            var shader = Shader.Find("Tech Guy/Illustrated Toon");
            Assert.That(shader, Is.Not.Null, "The project's illustrated shader must be imported.");
            var illustrated = NewMaterial(shader);
            Assert.That(illustrated.HasProperty("_OcclusionOpacity"), Is.True);
            illustrated.SetFloat("_OcclusionOpacity", .8f);
            Color authoredColor = MaterialColor(illustrated);
            int authoredQueue = illustrated.renderQueue;
            var wall = Wall("Illustrated obstruction", -5f);
            var renderer = wall.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = illustrated;
            Physics.SyncTransforms();

            TickToCompletion();

            var runtime = renderer.sharedMaterial;
            Assert.That(runtime, Is.Not.SameAs(illustrated));
            Assert.That(runtime.GetFloat("_OcclusionOpacity"), Is.EqualTo(.8f * FadedOpacity).Within(.001f));
            Assert.That(MaterialColor(runtime), Is.EqualTo(authoredColor));
            Assert.That(runtime.renderQueue, Is.EqualTo(authoredQueue));
            Assert.That(illustrated.GetFloat("_OcclusionOpacity"), Is.EqualTo(.8f));
            _fader.Dispose();
            Assert.That(renderer.sharedMaterial, Is.SameAs(illustrated));
        }

        [Test]
        public void RestorePreservesAMaterialReplacedByAnotherOwnerDuringTheFade()
        {
            var wall = Wall("Material replaced externally", -5f);
            var renderer = wall.GetComponent<MeshRenderer>();
            Physics.SyncTransforms();
            TickToCompletion();
            var replacement = NewMaterial();
            renderer.sharedMaterial = replacement;

            _fader.Dispose();

            Assert.That(renderer.sharedMaterial, Is.SameAs(replacement));
        }

        [UnityTest]
        public IEnumerator ActiveCameraFadeLetsGroundClicksCrossTheWallAndDisableRestoresBlocking()
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            Assert.That(groundLayer, Is.GreaterThanOrEqualTo(0), "The project movement layer must exist.");
            var wall = Cube("Picking obstruction", Origin + new Vector3(0f, 2f, -4f), new Vector3(8f, 4f, .5f));
            var floor = Cube("Picking ground", Origin + Vector3.down * .25f, new Vector3(20f, .5f, 20f));
            floor.layer = groundLayer;
            var cameraObject = NewObject("Camera with real follow behaviour", CameraPosition);
            var follow = cameraObject.AddComponent<TG_TopDown_Camera>();
            follow.m_Target = _target;
            var ray = new Ray(CameraPosition, (_target.position - CameraPosition).normalized);
            Physics.SyncTransforms();

            Assert.That(WorldClickResolver.TryResolve(ray, _target, 1 << groundLayer, out _, out _, follow), Is.False,
                "Before the first camera update the opaque wall must block the ground.");
            yield return new WaitForSecondsRealtime(.3f);
            Physics.SyncTransforms();

            Assert.That(follow.IsOcclusionFaded(wall.GetComponent<Collider>()), Is.True);
            Assert.That(WorldClickResolver.TryResolve(ray, _target, 1 << groundLayer, out var hit, out _, follow), Is.True);
            Assert.That(hit.collider, Is.SameAs(floor.GetComponent<Collider>()));
            Assert.That(wall.GetComponent<Collider>().enabled, Is.True);

            follow.enabled = false;
            Assert.That(wall.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(_source));
            Assert.That(follow.IsOcclusionFaded(wall.GetComponent<Collider>()), Is.False);
            Assert.That(WorldClickResolver.TryResolve(ray, _target, 1 << groundLayer, out _, out _, follow), Is.False,
                "Disabling the camera restores both the material and the normal picking barrier.");
        }

        private GameObject NewObject(string name, Vector3 position)
        {
            var result = new GameObject(name);
            result.transform.position = position;
            _objects.Add(result);
            return result;
        }

        private GameObject Wall(string name, float z)
            => Cube(name, Origin + new Vector3(0f, 3f, z), new Vector3(3f, 6f, .35f));

        private GameObject Cube(string name, Vector3 position, Vector3 scale)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
            result.transform.position = position;
            result.transform.localScale = scale;
            result.GetComponent<MeshRenderer>().sharedMaterial = _source;
            _objects.Add(result);
            return result;
        }

        private Material NewMaterial(Shader shader = null)
        {
            shader = shader ? shader : Shader.Find("Universal Render Pipeline/Unlit");
            Assert.That(shader, Is.Not.Null, "URP Unlit must be available in this URP project.");
            var result = new Material(shader) { name = "Camera occlusion test source" };
            int colorProperty = Shader.PropertyToID(result.HasProperty("_BaseColor") ? "_BaseColor" : "_Color");
            result.SetColor(colorProperty, new Color(.2f, .4f, .6f, 1f));
            _materials.Add(result);
            return result;
        }

        private void TickToCompletion()
        {
            for (int i = 0; i < 40; i++) _fader.Tick(CameraPosition, _target, .05f);
        }

        private static Color MaterialColor(Material material)
            => material.GetColor(material.HasProperty("_BaseColor") ? "_BaseColor" : "_Color");

        private static float Opacity(Material material)
            => material.HasProperty("_OcclusionOpacity") ? material.GetFloat("_OcclusionOpacity") : MaterialColor(material).a;
    }
}
