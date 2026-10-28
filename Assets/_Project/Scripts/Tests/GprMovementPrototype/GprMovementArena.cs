using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace TechGuy.Tests.GprMovement
{
    // Transient geometry/data only. Nothing is saved into production scenes or NavMesh assets.
    public sealed class GprMovementArena : MonoBehaviour
    {
        public readonly Vector3 Origin = new Vector3(1000, 0, 1000);
        public GprMovementMotor Motor { get; private set; }
        public Camera View { get; private set; }
        public GameObject UnbakedWall { get; private set; }
        private NavMeshData _data;
        private NavMeshDataInstance _instance;
        private readonly List<Material> _materials = new List<Material>();
        private readonly List<NavMeshBuildSource> _sources = new List<NavMeshBuildSource>();
        private RuntimeAnimatorController _animationController;

        public void Build(bool physicalInput = false)
        {
            Box("Piso / borda NavMesh", new Vector3(0, -.1f, 0), new Vector3(30, .2f, 30), Quaternion.identity, new Color(.1f, .17f, .23f));
            Box("Parede", new Vector3(4, 1.5f, 0), new Vector3(.25f, 3, 9), Quaternion.identity, new Color(.7f, .3f, .12f));
            Box("Canto", new Vector3(1, 1.5f, 4.5f), new Vector3(6, 3, .25f), Quaternion.identity, new Color(.7f, .3f, .12f));
            Box("Obstaculo", new Vector3(-3, 1, -4), new Vector3(2, 2, 2), Quaternion.identity, new Color(.65f, .15f, .24f));
            Box("Rampa 20 graus", new Vector3(-7, 1, 8), new Vector3(5, .2f, 6), Quaternion.Euler(-20, 0, 0), new Color(.15f, .45f, .35f));
            var settings = NavMesh.GetSettingsByIndex(0);
            settings.agentRadius = .5f;
            settings.agentHeight = 2;
            settings.agentSlope = 45;
            settings.overrideVoxelSize = true;
            settings.voxelSize = .08f;
            _data = NavMeshBuilder.BuildNavMeshData(settings, _sources, new Bounds(Origin, new Vector3(40, 12, 40)), Vector3.zero, Quaternion.identity);
            _instance = NavMesh.AddNavMeshData(_data);
            UnbakedWall = Box("Parede fina nao incluida no bake", new Vector3(-8, 1.5f, -7), new Vector3(.08f, 3, 4), Quaternion.identity, Color.yellow, false);
            Physics.SyncTransforms(); // Only after fixture construction, not every movement frame.

            var body = new GameObject("C01.2a SOMENTE TESTE — capsule");
            body.transform.SetParent(transform);
            body.transform.position = Origin;
            var agent = body.AddComponent<NavMeshAgent>();
            agent.radius = .5f;
            agent.height = 2;
            agent.speed = 5;
            agent.acceleration = 99999;
            agent.angularSpeed = 0;
            agent.stoppingDistance = 0;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(body.transform, false);
            visual.transform.localPosition = Vector3.up;
            DestroyImmediate(visual.GetComponent<Collider>());
            Paint(visual, new Color(.1f, .8f, 1));
            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.transform.SetParent(body.transform, false);
            nose.transform.localPosition = new Vector3(0, 1, .65f);
            nose.transform.localScale = new Vector3(.18f, .18f, .75f);
            DestroyImmediate(nose.GetComponent<Collider>());
            Paint(nose, Color.white);
            Motor = body.AddComponent<GprMovementMotor>();
            Motor.Initialize(agent);
            Motor.ReadPhysicalInput = physicalInput;
            if (physicalInput) Motor.InputLabel = "dispositivo local (sem aceite registrado)";
#if UNITY_EDITOR
            // Transient parameter sink only: no claim of validating the authored character rig.
            var controller = new UnityEditor.Animations.AnimatorController();
            controller.AddParameter("ActualPlanarSpeed", AnimatorControllerParameterType.Float);
            controller.AddLayer("Prototype");
            controller.layers[0].stateMachine.AddState("IdleWalkParameterProbe");
            _animationController = controller;
            Motor.Presentation = visual.AddComponent<Animator>();
            Motor.Presentation.runtimeAnimatorController = controller;
            Motor.Presentation.applyRootMotion = false;
#endif

            View = new GameObject("C01.2a Camera").AddComponent<Camera>();
            View.transform.SetParent(transform);
            View.transform.position = Origin + new Vector3(16, 23, -23);
            View.transform.LookAt(Origin + Vector3.forward * 2);
            View.orthographic = true;
            View.orthographicSize = 18;
            View.clearFlags = CameraClearFlags.SolidColor;
            View.backgroundColor = new Color(.025f, .04f, .07f);
            View.nearClipPlane = .1f;
            View.farClipPlane = 100;
            View.depth = 100;
            Motor.View = View;
            var light = new GameObject("C01.2a Light").AddComponent<Light>();
            light.transform.SetParent(transform);
            light.type = LightType.Directional;
            light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        private GameObject Box(string label, Vector3 position, Vector3 size, Quaternion rotation, Color color, bool bake = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            go.transform.SetParent(transform);
            go.transform.SetPositionAndRotation(Origin + position, rotation);
            go.transform.localScale = size;
            Paint(go, color);
            if (bake) _sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(Origin + position, rotation, Vector3.one), size = size, area = 0
            });
            return go;
        }

        private void Paint(GameObject go, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            _materials.Add(material);
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        public bool Place(Vector3 local)
        {
            // Setup only, never used by motor to bypass a collision during movement.
            return NavMesh.SamplePosition(Origin + local, out NavMeshHit hit, 2, NavMesh.AllAreas) && Motor.Agent.Warp(hit.position);
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 600, 102), "GPR-C01.2a — ARENA ISOLADA / SEM COMBATE");
            GUI.Label(new Rect(25, 38, 580, 25), "WASD + mira mouse | Espaco: dash cursor | Tab: camera/mundo (so teste)");
            if (Motor != null) GUI.Label(new Rect(25, 64, 580, 40),
                $"{(Motor.CameraRelative ? "Camera" : "Mundo")} | deslocamento {Motor.ActualPlanarSpeed:F3} m/s | {Motor.LastOwner}\nEntrada: {Motor.InputLabel}");
        }

        private void OnDestroy()
        {
            if (_instance.valid) _instance.Remove();
            if (_data != null) DestroyImmediate(_data);
            if (_animationController != null) DestroyImmediate(_animationController);
            foreach (var material in _materials) if (material != null) DestroyImmediate(material);
        }
    }
}
