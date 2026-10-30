using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEditor;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        [UnityTest, Category("RUN00")]
        public IEnumerator RUN00OverlappingHooksCannotRestartTheControlClock()
        {
            BuildDirectRig(); yield return null;
            var first = BasicVictim(Vector3.forward * 6);
            var second = BasicVictim(Vector3.right * 6);
            float start = Time.time;
            _player.BeginExternalPull(first.transform, 1.5f);
            for (int i = 0; i < 4; i++)
            {
                yield return new WaitForSeconds(.3f);
                _player.BeginExternalPull(second.transform, 1.5f);
            }
            while (Time.time - start < 1.65f) yield return null;
            Assert.IsFalse(_player.IsBeingPulled, "New hooks must not replace an active pull and prolong loss of control.");
            Assert.IsTrue(_controls.enabled);
        }

        [UnityTest, Category("RUN00")]
        public IEnumerator RUN00ZeroDurationPullCannotLeaveAStaleCoroutineLock()
        {
            BuildDirectRig(); yield return null;
            var source = BasicVictim(Vector3.forward * 6);
            _player.BeginExternalPull(source.transform, 0);
            yield return null;
            Assert.IsFalse(_player.IsBeingPulled);
            Assert.IsTrue(_controls.enabled);
        }

        [UnityTest, Category("RUN00")]
        public IEnumerator RUN00ProductionHooksAllowMovementAndDashBetweenConnections()
        {
            using (var input = new DirectInputScope())
            {
                Camera camera = BuildDirectRig(); input.Keys(); yield return null; yield return null;
                var first = BasicVictim(Vector3.forward * 6).gameObject.AddComponent<EnemyCombatActions>();
                var second = BasicVictim(Vector3.right * 6).gameObject.AddComponent<EnemyCombatActions>();
                first.StartCoroutine(RUN00HookLoop(first));
                yield return new WaitForSeconds(.7f);
                Assert.IsTrue(_player.IsBeingPulled, "Production HookLine must actually connect before testing recovery.");
                second.StartCoroutine(RUN00HookLoop(second));
                float deadline = Time.time + 2;
                while (_player.IsBeingPulled && Time.time < deadline) yield return null;
                Assert.IsFalse(_player.IsBeingPulled, "Two production attack sequences must leave a recovery opportunity.");
                input.Keys(); yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true);
                Vector3 start = _player.transform.position;
                input.Keys(Key.A);
                yield return new WaitForSeconds(.15f);
                Assert.IsFalse(_player.IsBeingPulled, "Recovery is an actionable window, not one free frame.");
                Assert.Greater(Vector3.Distance(start, _player.transform.position), .15f);
                input.Keys(); input.Aim(camera, _player.transform.position + Vector3.back * 8);
                yield return null; yield return null;
                var dash = Object.Instantiate(AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset"));
                try
                {
                    Assert.IsTrue(_player.GetComponent<AbilityHolder>().TryUseDash(dash));
                    yield return new WaitForSeconds(.08f);
                    Assert.IsTrue(dash.IsDashing(_player.gameObject));
                }
                finally { Object.DestroyImmediate(dash); first.Cancel(); second.Cancel(); }
            }
        }

        private IEnumerator RUN00HookLoop(EnemyCombatActions actions)
        {
            for (int i = 0; i < 6 && actions && _player && !_player.IsDead; i++)
                yield return actions.Perform(EnemyAttackKind.HookLine, _player, 0, () => true);
        }

        [UnityTest, Category("RUN00")]
        public IEnumerator RUN00PauseAndHitstopPreservePullAndRecoveryClocks()
        {
            BuildDirectRig(); yield return null;
            var source = BasicVictim(Vector3.forward * 6);
            _player.BeginExternalPull(source.transform, .25f);
            Time.timeScale = 0;
            Vector3 before = _player.transform.position;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(_player.IsBeingPulled);
            Assert.Less(Vector3.Distance(before, _player.transform.position), .01f);
            Time.timeScale = 1;
            using (var feedback = _player.BeginImpactFeedback(.08f)) feedback.Scope.ConfirmDamage(true);
            yield return null;
            Assert.IsTrue(_player.IsBeingPulled);
            yield return new WaitForSeconds(.4f);
            Assert.IsFalse(_player.IsBeingPulled);
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.7f);
            _player.BeginExternalPull(source.transform, .25f);
            Assert.IsFalse(_player.IsBeingPulled, "Paused wall time cannot consume the HookLine-only escape window.");
            Time.timeScale = 1;
            yield return new WaitForSeconds(.65f);
            _player.BeginExternalPull(source.transform, .25f);
            Assert.IsTrue(_player.IsBeingPulled, "Recovery must expire; there is no unlimited pull immunity.");
        }

        [UnityTest, Category("RUN00")]
        public IEnumerator RUN00SourceDisableAndPlayerDisableReleaseOnlyOwnedMovement()
        {
            BuildDirectRig(); yield return null;
            var source = BasicVictim(Vector3.forward * 6);
            _player.BeginExternalPull(source.transform, 1.5f);
            source.gameObject.SetActive(false); yield return null; yield return null;
            Assert.IsFalse(_player.IsBeingPulled); Assert.IsTrue(_controls.enabled);
            source.gameObject.SetActive(true); yield return new WaitForSeconds(.65f);
            _player.BeginExternalPull(source.transform, 1.5f);
            _player.gameObject.SetActive(false); yield return null;
            Assert.IsFalse(_player.IsBeingPulled);
            _player.gameObject.SetActive(true); yield return null;
            Assert.IsTrue(_controls.enabled);
            _controls.enabled = false;
            _player.BeginExternalPull(source.transform, .05f);
            yield return new WaitForSeconds(.1f);
            Assert.IsFalse(_controls.enabled, "Pull must not enable a controller disabled by another owner.");
        }

        [UnityTest, Category("RUN00")]
        public IEnumerator RUN00FlameGeometryAnimatesAndPoolReuseCannotLeakIt()
        {
            yield return PrepareTitanic();
            var visual = _player.GetComponent<TitanicFistsVisual>();
            var view = visual.AcquireTornado(1, ProtocolBasicEffects.Both);
            visual.UpdateTornado(view, view.Generation, Vector3.zero, Vector3.forward, .2f);
            var meshes = view.Root.GetComponentsInChildren<MeshFilter>();
            Assert.AreEqual(2, meshes.Length, "Flame silhouette and hot core are actual geometry, separate from the thin identity ribbons.");
            Vector3 before = meshes[0].sharedMesh.vertices[0];
            visual.UpdateTornado(view, view.Generation, Vector3.zero, Vector3.forward, .35f);
            Assert.Greater(Vector3.Distance(before, meshes[0].sharedMesh.vertices[0]), .01f);
            foreach (var mesh in meshes) Assert.IsTrue(mesh.GetComponent<MeshRenderer>().enabled);
            visual.ReleaseTornado(view, view.Generation);
            view = visual.AcquireTornado(1, ProtocolBasicEffects.Conductor);
            foreach (var mesh in meshes) Assert.IsFalse(mesh.GetComponent<MeshRenderer>().enabled);
            Assert.IsTrue(view.Electricity[0].enabled);
            visual.ReleaseTornado(view, view.Generation);
        }

        [UnityTest, Category("RUN00")]
        public IEnumerator RUN00SingleAttackerRepeatsWithARealControlWindow()
        {
            BuildDirectRig(); yield return null;
            var attacker = BasicVictim(Vector3.forward * 6).gameObject.AddComponent<EnemyCombatActions>();
            attacker.StartCoroutine(RUN00HookLoop(attacker));
            float deadline = Time.time + 7, freeSince = -1;
            int pulls = 0; bool previous = false; float longestWindow = 0;
            while (Time.time < deadline)
            {
                bool pulled = _player.IsBeingPulled;
                if (pulled && !previous) { pulls++; freeSince = -1; }
                if (!pulled && previous) freeSince = Time.time;
                if (freeSince >= 0 && !pulled)
                {
                    Assert.IsTrue(_controls.enabled);
                    longestWindow = Mathf.Max(longestWindow, Time.time - freeSince);
                }
                previous = pulled; yield return null;
            }
            Assert.GreaterOrEqual(pulls, 2, "The hook is still dangerous and repeatable.");
            Assert.GreaterOrEqual(longestWindow, .5f);
            attacker.Cancel();
        }

        [UnityTest, Category("RUN00")]
        public IEnumerator RUN00SourceDeathAndSceneUnloadCannotRetainPullOwnership()
        {
            BuildDirectRig(); yield return null;
            var source = BasicVictim(Vector3.forward * 6);
            _player.BeginExternalPull(source.transform, 1.5f);
            source.TakeDamage(1000000); yield return null; yield return null;
            Assert.IsFalse(_player.IsBeingPulled); Assert.IsTrue(_controls.enabled);
            yield return new WaitForSeconds(.65f);
            var scene = UnityEngine.SceneManagement.SceneManager.CreateScene("RUN00 pull source cleanup");
            var other = BasicVictim(Vector3.right * 6);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(other.gameObject, scene);
            _player.BeginExternalPull(other.transform, 1.5f);
            yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            yield return null;
            Assert.IsFalse(_player.IsBeingPulled); Assert.IsTrue(_controls.enabled);
            _player.BeginExternalPull(null, 1.5f); Assert.IsFalse(_player.IsBeingPulled);
        }

        [UnityTest, Category("RUN00")]
        public IEnumerator RUN00PlayerDeathCannotLeavePullOrRestoreDeadInput()
        {
            BuildDirectRig(); yield return null;
            var source = BasicVictim(Vector3.forward * 6);
            var player = _player;
            _player.BeginExternalPull(source.transform, 1.5f);
            _player.TakeDamage(1000000);
            Assert.IsTrue(player.IsDead);
            Assert.IsFalse(_controls.enabled);
            yield return null; yield return null;
            Assert.IsFalse(player);
        }
    }
}
