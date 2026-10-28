using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Extends the existing preview, using its real Game View / PlayMode approach. No scene builder,
// runtime component, alternate damage path, or additional test runner is installed.
public static partial class CombatPolishPreview
{
    private const string ObservationRecoveryKey = "TechGuy.CombatPolishPreview.Observation";
    private const string ObservationScene = "Assets/_Project/Scenes/FirstSector.unity";
    private const string ObservationProfiles = "Tools/GameplayObservation/scenarios.json";
    private const BindingFlags ObservationFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static ObservationRecovery _observation;
    private static ObservationManifest _observationManifest;
    private static PlayerActor _observationPlayer;
    private static ProgressionDirector _observationDirector;
    private static Coroutine _observationCoroutine;
    private static EditorWindow _observationView;
    private static Mouse _observationMouse;
    private static readonly List<InputDevice> ObservationDisabledDevices = new List<InputDevice>();
    private static bool _observationFinishing;
    private static bool _observationRestoreReady;
    private static double _observationDeadline;
    private static double _observationProgressTime;
    private static double _observationStartUnscaled;
    private static string _observationLastStatus = "{\"status\":\"idle\"}";

    [Serializable] public sealed class ObservationInput
    {
        public int frame;
        public string action = "basic";
        public int slot;
        public float x;
        public float z;
    }

    [Serializable] public sealed class ObservationRequest
    {
        public string scenario_id = "GAUNTLET_SMALL_GROUP";
        public string phase = "baseline";
        public string mode = "seeded_combat";
        public string seed = "1701";
        public int weapon;
        public int node_id = 1;
        public int enemy_count = 5;
        public int width = 1280;
        public int height = 720;
        public int simulation_fps = 60;
        public int capture_every_frames = 3;
        public int duration_frames = 360;
        public float observe_seconds; // Optional real-time limit for passive current-mode human clips.
        public bool control_enemy_rng = true;
        public ObservationInput[] input_schedule;
    }

    [Serializable] private sealed class ObservationProfileList { public ObservationProfile[] scenarios; }
    [Serializable] private sealed class ObservationProfile
    {
        public string scenario_id;
        public int scenario_version;
        public int weapon;
        public string automated_setup;
        public string limitation;
    }
    [Serializable] private sealed class ObservationPreference { public string key; public bool existed; public int value; }
    [Serializable] private sealed class ObservationSceneState { public string path; public bool loaded; public bool active; }
    [Serializable] private sealed class ObservationRandom { public UnityEngine.Random.State state; }
    [Serializable] private sealed class ObservationRecovery
    {
        public string stage;
        public string output_directory;
        public ObservationRequest request;
        public ObservationSceneState[] scenes;
        public string[] selection;
        public ObservationPreference[] preferences;
        public bool started_playing;
        public float time_scale;
        public float capture_delta;
        public int quality;
        public bool shader_async;
        public string random_json;
        public ulong view_id;
        public bool view_existed;
        public Rect view_position;
        public int view_size_index;
        public int view_group;
        public int temporary_size_index = -1;
        public string temporary_size_name;
        public bool background_input;
        public bool editor_input;
        public int background_input_value;
        public int editor_input_value;
        public string camera_json;
        public ulong camera_id;
        public string requested_seed;
    }
    [Serializable] private sealed class ObservationConditions
    {
        public int scenario_version = 1;
        public string scene_guid;
        public string seed;
        public int weapon;
        public int node_id;
        public int enemy_count;
        public int width;
        public int height;
        public int simulation_fps;
        public int capture_every_frames;
        public string input_schedule;
        public string initial_actor_fingerprint;
        public string camera_json;
        public string render_json;
        public string random_control;
    }
    [Serializable] private sealed class ObservationFrame
    {
        public int index;
        public int relative_frame;
        public float time;
        public string path;
        public string sha256;
        public int width;
        public int height;
    }
    [Serializable] private sealed class ObservationProvenance
    {
        public string utc;
        public string revision;
        public string working_tree;
        public string scene;
        public string requested_seed;
        public string screenshot_method = "Unity ScreenCapture.CaptureScreenshotAsTexture after WaitForEndOfFrame; includes Game View UI";
        public string setup;
        public string limitations;
        public string error;
    }
    [Serializable] private sealed class ObservationRestoration
    {
        public bool restored;
        public bool success;
        public bool preferences;
        public bool scene_setup;
        public bool editor_settings;
        public bool game_view;
        public string error;
    }
    [Serializable] private sealed class ObservationManifest
    {
        public int schema_version = 1;
        public string run_id;
        public string status = "running";
        public string scenario_id;
        public string phase;
        public string unity_version;
        public ObservationProvenance provenance = new ObservationProvenance();
        public ObservationConditions conditions = new ObservationConditions();
        public List<ObservationFrame> frames = new List<ObservationFrame>();
        public string events_path = "events.jsonl";
        public string events_sha256;
        public ObservationRestoration restoration = new ObservationRestoration();
    }
    [Serializable] private sealed class ObservationSchedule { public ObservationInput[] commands; }
    [Serializable] private sealed class ObservationActors { public ObservationActor[] actors; }
    [Serializable] private sealed class ObservationActor
    {
        public string name;
        public string affiliation;
        public Vector3 position;
        public Quaternion rotation;
        public float health;
        public float max_health;
        public string rank;
    }
    [Serializable] private sealed class ObservationCamera
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public bool orthographic;
        public float orthographic_size;
        public float field_of_view;
        public float near;
        public float far;
        public Rect rect;
        public bool hdr;
        public bool msaa;
        public int clear_flags;
        public int culling_mask;
        public Color background;
        public bool follow_enabled;
        public float follow_height;
        public float follow_distance;
        public float follow_angle;
    }
    [Serializable] private sealed class ObservationRender
    {
        public string pipeline;
        public string pipeline_guid;
        public string pipeline_sha256;
        public string graphics_settings_sha256;
        public string quality_settings_sha256;
        public string color_space;
        public int quality;
        public string quality_name;
        public string gpu;
        public string graphics_api;
        public bool post_processing;
        public int anti_aliasing;
        public float render_scale;
        public string[] volume_profiles;
    }

    /// <summary>Starts a finite real-engine observation. It returns immediately; poll ObservationStatus.</summary>
    public static string BeginObservation(string requestJson)
    {
        if (_observation != null || !string.IsNullOrEmpty(SessionState.GetString(ObservationRecoveryKey, "")))
            return ObservationResponse("rejected", "An observation is already active.");
        if (!TryValidateObservationRequest(requestJson, out string validation)) return ObservationResponse("rejected", validation);
        var request = ParseObservationRequest(requestJson);
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            return ObservationResponse("rejected", "Wait for Editor import/compilation or Play Mode transition to finish.");
        if (request.mode == "seeded_combat" && EditorApplication.isPlaying)
            return ObservationResponse("rejected", "Seeded setup starts from Edit Mode so the seed is applied before Awake.");
        if (request.mode == "current" && !EditorApplication.isPlaying)
            return ObservationResponse("rejected", "Current mode requires an already running real scenario/UI; no substitute screen is generated.");
        if (request.mode == "seeded_combat" && Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)))
            return ObservationResponse("rejected", "Save or close dirty/untitled scenes before seeded observation. The tool does not save scenes.");
        try
        {
            _observationRestoreReady = false;
            _observation = SaveObservationState(request);
            string runId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _observation.output_directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/GameplayObservation", runId));
            Directory.CreateDirectory(_observation.output_directory);
            Directory.CreateDirectory(Path.Combine(_observation.output_directory, "frames"));
            _observationManifest = new ObservationManifest
            {
                run_id = runId, scenario_id = request.scenario_id, phase = request.phase, unity_version = Application.unityVersion,
                provenance = new ObservationProvenance
                {
                    utc = DateTime.UtcNow.ToString("O"), revision = ObservationGit("rev-parse HEAD"), working_tree = ObservationGit("status --porcelain=v1"),
                    requested_seed = request.seed, setup = request.mode,
                    limitations = "Fixed simulation steps do not prove bit-identical physics/NavMesh/animation or human game feel. Commands use existing public gameplay APIs, not keyboard binding coverage. No combat balance, immunity, mana, HP, AI enable state or assets are changed."
                }
            };
            PersistObservation();
            ConfigureObservationGameView();
            if (request.mode == "seeded_combat")
            {
                EditorSceneManager.OpenScene(ObservationScene, OpenSceneMode.Single);
                PrepareObservationSeed(request);
                PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, request.weapon);
                if (request.weapon > 0) PlayerPrefs.SetInt("TechGuy.Loadout.Unlocked." + request.weapon, 1);
                _observation.stage = "entering";
                PersistObservation();
                EditorApplication.EnterPlaymode();
            }
            else StartObservationWhenReady();
            return ObservationStatus();
        }
        catch (Exception exception) { FailObservation(exception); return ObservationStatus(); }
    }

    public static string ObservationStatus()
    {
        if (_observation == null) return _observationLastStatus;
        return JsonUtility.ToJson(new ObservationApiStatus
        {
            status = _observation.stage == "restoring" ? "restoring" : _observationManifest?.status ?? _observation.stage, stage = _observation.stage,
            run_id = _observationManifest?.run_id, output_directory = _observation.output_directory,
            frames = _observationManifest?.frames.Count ?? 0, message = _observationManifest?.restoration.error is string restorationError && !string.IsNullOrEmpty(restorationError) ? restorationError : _observationManifest?.provenance.error
        });
    }

    public static string CancelObservation()
    {
        if (_observation != null) FinishObservation("cancelled", "Cancelled by caller.");
        return ObservationStatus();
    }

    public static string ObservationScenarios() => File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "../", ObservationProfiles)));

    public static bool TryValidateObservationRequest(string json, out string error)
    {
        error = null;
        try
        {
            if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{", StringComparison.Ordinal)) throw new ArgumentException("Request must be a JSON object.");
            var request = ParseObservationRequest(json);
            var profile = ReadObservationProfile(request.scenario_id);
            if (request.mode != "seeded_combat" && request.mode != "current") throw new ArgumentException("mode must be seeded_combat or current.");
            if (request.mode == "seeded_combat" && profile.automated_setup != "first-sector-node-1")
                throw new ArgumentException(profile.limitation + " Use mode=current after preparing the real scenario; the tool will only observe it.");
            if (request.phase != "baseline" && request.phase != "before" && request.phase != "after" && request.phase != "candidate") throw new ArgumentException("phase must be baseline, before, after or candidate.");
            if (!ulong.TryParse(request.seed, NumberStyles.None, CultureInfo.InvariantCulture, out _)) throw new ArgumentException("seed must be an unsigned 64-bit integer string.");
            if (request.width < 320 || request.width > 3840 || request.height < 180 || request.height > 2160) throw new ArgumentException("Resolution must be between 320x180 and 3840x2160.");
            if (request.simulation_fps < 15 || request.simulation_fps > 120 || request.capture_every_frames < 1 || request.capture_every_frames > request.simulation_fps)
                throw new ArgumentException("simulation_fps must be 15..120; capture_every_frames must be 1..simulation_fps.");
            if (request.duration_frames < 1 || request.duration_frames > request.simulation_fps * 30) throw new ArgumentException("Duration must be positive and at most 30 simulated seconds.");
            if (float.IsNaN(request.observe_seconds) || float.IsInfinity(request.observe_seconds) || request.observe_seconds < 0 || request.observe_seconds > 30
                || request.observe_seconds > 0 && request.mode != "current")
                throw new ArgumentException("observe_seconds must be 0..30 and is only available in passive current mode.");
            if (request.weapon < 0 || request.weapon > 2 || request.mode == "seeded_combat" && (request.node_id != 1 || request.enemy_count != 5))
                throw new ArgumentException("Seeded setup currently supports node 1 with five production enemies and weapon index 0..2.");
            int previous = -1;
            foreach (var command in request.input_schedule)
            {
                if (command == null || command.frame < previous || command.frame <= 0 || command.frame >= request.duration_frames) throw new ArgumentException("Commands must be ordered, positive and inside duration_frames.");
                if (command.action != "basic" && command.action != "skill" && command.action != "finish" && command.action != "move") throw new ArgumentException("Supported existing gameplay actions: basic, skill, finish, move.");
                if (command.action == "skill" && (command.slot < 0 || command.slot > 3)) throw new ArgumentException("Skill slot must be 0..3.");
                if (float.IsNaN(command.x) || float.IsInfinity(command.x) || float.IsNaN(command.z) || float.IsInfinity(command.z)) throw new ArgumentException("Aim offsets must be finite.");
                previous = command.frame;
            }
            if (request.mode == "current" && request.input_schedule.Length != 0) throw new ArgumentException("Current mode observes without driving input; provide an empty input_schedule.");
            return true;
        }
        catch (Exception exception) { error = exception.Message; return false; }
    }

    [Serializable] private sealed class ObservationApiStatus
    {
        public string status; public string stage; public string run_id; public string output_directory; public int frames; public string message;
    }
    private static string ObservationResponse(string status, string message) => JsonUtility.ToJson(new ObservationApiStatus { status = status, message = message });
    private static ObservationRequest ParseObservationRequest(string json)
    {
        var request = new ObservationRequest();
        JsonUtility.FromJsonOverwrite(json, request);
        var profile = ReadObservationProfile(request.scenario_id);
        // Profiles choose the weapon; callers cannot accidentally label a bow scenario as gauntlet.
        request.weapon = profile.weapon;
        if (request.input_schedule == null) request.input_schedule = request.mode == "current" ? Array.Empty<ObservationInput>() :
            new[] { 30, 75, 120, 165, 210, 255 }.Where(frame => frame < request.duration_frames).Select(frame => new ObservationInput { frame = frame }).ToArray();
        return request;
    }
    private static ObservationProfile ReadObservationProfile(string id)
    {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../", ObservationProfiles));
        var profiles = JsonUtility.FromJson<ObservationProfileList>(File.ReadAllText(path));
        return profiles.scenarios.SingleOrDefault(profile => profile.scenario_id == id) ?? throw new ArgumentException("Unknown scenario_id: " + id);
    }

    [InitializeOnLoadMethod]
    private static void InitializeObservationRecovery()
    {
        EditorApplication.playModeStateChanged -= ObservationPlayModeChanged;
        EditorApplication.playModeStateChanged += ObservationPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= ObservationBeforeReload;
        AssemblyReloadEvents.beforeAssemblyReload += ObservationBeforeReload;
        EditorApplication.quitting -= ObservationEditorQuitting;
        EditorApplication.quitting += ObservationEditorQuitting;
        EditorApplication.update -= ObservationWatchdog;
        EditorApplication.update += ObservationWatchdog;
        string recovery = SessionState.GetString(ObservationRecoveryKey, "");
        if (string.IsNullOrEmpty(recovery)) return;
        try
        {
            _observation = JsonUtility.FromJson<ObservationRecovery>(recovery);
            string manifest = Path.Combine(_observation.output_directory, "manifest.json");
            if (File.Exists(manifest)) _observationManifest = JsonUtility.FromJson<ObservationManifest>(File.ReadAllText(manifest));
            EditorApplication.delayCall += ResumeObservationRecovery;
        }
        catch (Exception exception) { Debug.LogError("Observation recovery requires attention: " + exception.Message); }
    }
    private static void ResumeObservationRecovery()
    {
        if (_observation == null) return;
        if (_observation.stage == "entering" && EditorApplication.isPlaying) StartObservationWhenReady();
        else if (_observation.stage == "restoring" && !EditorApplication.isPlaying)
        { _observationRestoreReady = true; EditorApplication.delayCall += RestoreObservationState; }
        else if (_observation.stage == "running" || _observation.stage == "starting") FinishObservation("failed", "Assembly reload interrupted the recording; partial evidence is retained.");
        else if (!EditorApplication.isPlayingOrWillChangePlaymode) FinishObservation("failed", "Editor returned to Edit Mode before observation started.");
    }
    private static void ObservationBeforeReload()
    {
        if (_observation == null) return;
        if (_observation.stage == "running")
        {
            EndObservationTelemetry();
            CleanupObservationInput();
            _observationManifest.status = "failed";
            _observationManifest.provenance.error = "Assembly reload interrupted the recording.";
        }
        PersistObservation();
    }
    private static void ObservationEditorQuitting()
    {
        if (_observation == null) return;
        EndObservationTelemetry(); CleanupObservationInput();
        _observationManifest.status = "failed";
        _observationManifest.provenance.error = "Editor quit during observation.";
        // Restore persistent preferences even if Unity cannot complete scene restoration while quitting.
        RestoreObservationPreferences(_observation.preferences);
        PersistObservation();
    }
    private static void ObservationWatchdog()
    {
        if (_observation != null && _observation.stage == "restoring" && _observationRestoreReady && !EditorApplication.isPlayingOrWillChangePlaymode)
        { RestoreObservationState(); return; }
        if (_observation == null || _observation.stage != "running") return;
        if (!_observationPlayer || !EditorApplication.isPlaying)
            FinishObservation("failed", "The recorded player/Play Mode ended before the finite observation completed.");
        else if (EditorApplication.timeSinceStartup - _observationProgressTime > 90)
            FinishObservation("failed", "No rendered frame for 90 seconds. Keep the Game View rendering/focused; partial evidence is retained.");
    }
    private static void ObservationPlayModeChanged(PlayModeStateChange state)
    {
        if (_observation == null) return;
        if (state == PlayModeStateChange.EnteredPlayMode && _observation.stage == "entering") StartObservationWhenReady();
        if (state == PlayModeStateChange.EnteredPlayMode && _observation.stage == "restoring" && !_observation.started_playing)
        { EditorApplication.ExitPlaymode(); return; }
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            EndObservationTelemetry(); CleanupObservationInput();
            if (!_observationFinishing && _observation.stage != "restoring")
            {
                _observationManifest.status = "cancelled";
                _observationManifest.provenance.error = "Play Mode exited before recording completed.";
            }
            _observation.stage = "restoring";
            PersistObservation();
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        { _observationRestoreReady = true; EditorApplication.delayCall += RestoreObservationState; }
    }

    private static ObservationRecovery SaveObservationState(ObservationRequest request)
    {
        // Current observations stay in Play Mode and never restore the Edit Mode scene setup.
        // Unity rejects GetSceneManagerSetup while playing, before any capture can begin.
        var setup = EditorApplication.isPlaying ? Array.Empty<SceneSetup>() : EditorSceneManager.GetSceneManagerSetup();
        return new ObservationRecovery
        {
            stage = "preparing", request = request, requested_seed = request.seed, started_playing = EditorApplication.isPlaying,
            scenes = setup.Select(scene => new ObservationSceneState { path = scene.path, loaded = scene.isLoaded, active = scene.isActive }).ToArray(),
            selection = Selection.objects.Select(value => GlobalObjectId.GetGlobalObjectIdSlow(value).ToString()).ToArray(),
            preferences = EditorApplication.isPlaying ? Array.Empty<ObservationPreference>() : CaptureObservationPreferences(), time_scale = Time.timeScale, capture_delta = Time.captureDeltaTime,
            quality = QualitySettings.GetQualityLevel(), shader_async = ShaderUtil.allowAsyncCompilation,
            random_json = JsonUtility.ToJson(new ObservationRandom { state = UnityEngine.Random.state })
        };
    }
    private static ObservationPreference[] CaptureObservationPreferences() => new[]
    {
        WeaponLoadout.PreferenceKey, "TechGuy.Loadout.Unlocked.1", "TechGuy.Loadout.Unlocked.2", CurrencyWallet.PreferenceKey
    }.Select(key => new ObservationPreference { key = key, existed = PlayerPrefs.HasKey(key), value = PlayerPrefs.GetInt(key) }).ToArray();
    private static void RestoreObservationPreferences(ObservationPreference[] preferences)
    {
        foreach (var item in preferences ?? Array.Empty<ObservationPreference>())
            if (item.existed) PlayerPrefs.SetInt(item.key, item.value); else PlayerPrefs.DeleteKey(item.key);
        PlayerPrefs.Save();
    }
    private static void PrepareObservationSeed(ObservationRequest request)
    {
        var director = Object.FindFirstObjectByType<ProgressionDirector>(FindObjectsInactive.Include);
        var source = Object.FindFirstObjectByType<RunSeedSource>(FindObjectsInactive.Include);
        if (!director || !source) throw new InvalidOperationException("FirstSector lacks its production director/RunSeedSource.");
        var data = new SerializedObject(director);
        var definition = data.FindProperty("_manualDefinition").objectReferenceValue as ManualRunDefinition;
        var candidates = data.FindProperty("_archetypePrefabs");
        var prefabs = new List<GameObject>();
        for (int i = 0; i < candidates.arraySize; i++)
            if (candidates.GetArrayElementAtIndex(i).objectReferenceValue is Component component) prefabs.Add(component.gameObject);
        if (!ManualRouteValidator.TrySnapshot(definition, Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[request.weapon]), out var route, out string error))
            throw new InvalidOperationException(error);
        ulong initial = ulong.Parse(request.seed, CultureInfo.InvariantCulture);
        bool found = false;
        for (ulong offset = 0; offset < 256; offset++)
        {
            ulong seed = unchecked(initial + offset);
            if (ManualRunRandomizer.Generate(route, seed, prefabs).Node(request.node_id).Spawns.Count != request.enemy_count) continue;
            request.seed = seed.ToString(CultureInfo.InvariantCulture); found = true; break;
        }
        if (!found) throw new InvalidOperationException("No five-enemy node-1 seed found in the next 256 production seeds.");
        var seedData = new SerializedObject(source);
        seedData.FindProperty("_randomizeEachRun").boolValue = false;
        seedData.FindProperty("_seed").stringValue = request.seed;
        seedData.ApplyModifiedPropertiesWithoutUndo(); // In-memory only. Never SaveScene / SaveAssets.
        _observationManifest.provenance.setup = "FirstSector production route; in-memory fixed RunSeedSource before Awake; public route-selection and player Warp to node 1; all production enemies/AI/resources unchanged.";
        PersistObservation();
    }

    private static void StartObservationWhenReady()
    {
        if (_observation == null || _observation.stage == "restoring" || _observation.stage == "starting" || _observation.stage == "running") return;
        _observation.stage = "starting";
        _observationDeadline = EditorApplication.timeSinceStartup + 45;
        PersistObservation();
        EditorApplication.update -= ObservationStartupTick;
        EditorApplication.update += ObservationStartupTick;
    }
    private static void ObservationStartupTick()
    {
        if (_observation == null || _observation.stage == "restoring") { EditorApplication.update -= ObservationStartupTick; return; }
        try
        {
            if (EditorApplication.timeSinceStartup > _observationDeadline) throw new TimeoutException("Timed out waiting for the actual FirstSector player/director/shaders.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || ShaderUtil.anythingCompiling) return;
            _observationPlayer = Object.FindFirstObjectByType<PlayerActor>();
            _observationDirector = Object.FindFirstObjectByType<ProgressionDirector>();
            if (!_observationPlayer || _observation.request.mode == "seeded_combat" && (!_observationDirector || !_observationDirector.IsStageBuilt || _observationDirector.ManualSession == null)) return;
            if (Screen.width != _observation.request.width || Screen.height != _observation.request.height) return;
            EditorApplication.update -= ObservationStartupTick;
            ShaderUtil.allowAsyncCompilation = false;
            if (_observation.request.mode == "seeded_combat")
            {
                Time.timeScale = 1f;
                Time.captureDeltaTime = 1f / _observation.request.simulation_fps;
                UnityEngine.Random.InitState(unchecked((int)ulong.Parse(_observation.request.seed, CultureInfo.InvariantCulture)));
                ConfigureObservationInput();
            }
            _observationCoroutine = _observationPlayer.StartCoroutine(ObservationFrames());
        }
        catch (Exception exception) { FailObservation(exception); }
    }

    private static IEnumerator ObservationFrames()
    {
        var request = _observation.request;
        if (request.mode == "seeded_combat")
        {
            var ui = Object.FindFirstObjectByType<ManualRunUI>();
            bool selected = false;
            for (int wait = 0; wait < 120 && !selected; wait++)
            {
                selected = ui && ui.TryChooseDestination(0);
                if (!selected) yield return null;
            }
            if (_observation == null || _observation.stage == "restoring" || !EditorApplication.isPlaying) yield break;
            if (!selected) { FinishObservation("failed", "Production route UI did not release/accept node 1."); yield break; }
            var session = _observationDirector.ManualSession;
            var agent = _observationPlayer.GetComponent<NavMeshAgent>();
            if (!agent || !agent.Warp(session.Route.Node(request.node_id).Center) || !_observationDirector.TryEnterNode(session.Id, request.node_id))
            { FinishObservation("failed", "Production node entry/Warp failed."); yield break; }
            // Awake has completed but no enemy Update has run for this newly spawned encounter yet.
            if (request.control_enemy_rng)
            {
                var field = typeof(EnemyAI).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) { FinishObservation("failed", "The explicit per-enemy RNG fixture is incompatible with EnemyAI; refusing false determinism."); yield break; }
                int index = 0;
                foreach (var enemy in _observationDirector.GetComponentsInChildren<EnemyAI>())
                    field.SetValue(enemy, new System.Random(unchecked((int)ulong.Parse(request.seed, CultureInfo.InvariantCulture) + ++index * 7919)));
            }
            yield return null; // UI release gate and original follow/occlusion get their normal player-loop update.
        }
        yield return new WaitForEndOfFrame();
        if (_observation == null || _observation.stage == "restoring" || !EditorApplication.isPlaying) yield break;
        try
        {
            _observationStartUnscaled = Time.unscaledTimeAsDouble;
            PrepareObservationManifest();
            _observation.stage = "running";
            _observationProgressTime = EditorApplication.timeSinceStartup;
            int startFrame = Time.frameCount;
            BeginObservationTelemetry(_observationPlayer, _observationDirector, _observation.output_directory, startFrame);
            PersistObservation();
        }
        catch (Exception exception) { FailObservation(exception); yield break; }
        int firstFrame = Time.frameCount;
        int commandIndex = 0;
        bool wallTimed = request.mode == "current" && request.observe_seconds > 0;
        double observeUntil = Time.realtimeSinceStartupAsDouble + request.observe_seconds;
        for (int relative = 0; _observation != null &&
            (wallTimed ? Time.realtimeSinceStartupAsDouble < observeUntil : relative < request.duration_frames); relative++)
        {
            if (relative > 0) yield return new WaitForEndOfFrame();
            if (_observation == null || _observation.stage == "restoring" || !EditorApplication.isPlaying) yield break;
            try
            {
                _observationProgressTime = EditorApplication.timeSinceStartup;
                SampleObservationTelemetry(Time.frameCount - firstFrame);
                if (relative % request.capture_every_frames == 0) CaptureObservationFrame(Time.frameCount - firstFrame);
            }
            catch (Exception exception) { FailObservation(exception); yield break; }
            if (!wallTimed && relative + 1 == request.duration_frames) break;
            yield return null;
            if (_observation == null || _observation.stage == "restoring" || !EditorApplication.isPlaying) yield break;
            try
            {
                while (commandIndex < request.input_schedule.Length && request.input_schedule[commandIndex].frame <= relative + 1)
                    DriveObservationCommand(request.input_schedule[commandIndex++]);
            }
            catch (Exception exception) { FailObservation(exception); yield break; }
        }
        FinishObservation("complete", null);
    }

    private static void ConfigureObservationInput()
    {
        var settings = InputSystem.settings;
        _observation.background_input = _observation.editor_input = true;
        _observation.background_input_value = (int)settings.backgroundBehavior;
        _observation.editor_input_value = (int)settings.editorInputBehaviorInPlayMode;
        foreach (var device in InputSystem.devices.ToArray())
            if (device.enabled && (device is Mouse || device is Keyboard || device is Gamepad))
            { ObservationDisabledDevices.Add(device); InputSystem.DisableDevice(device); }
        _observationMouse = InputSystem.AddDevice<Mouse>("TechGuyObservationMouse");
        settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        PersistObservation();
    }
    private static void CleanupObservationInput()
    {
        if (_observationMouse != null && _observationMouse.added) InputSystem.RemoveDevice(_observationMouse);
        _observationMouse = null;
        foreach (var device in ObservationDisabledDevices) if (device != null && device.added) InputSystem.EnableDevice(device);
        ObservationDisabledDevices.Clear();
        if (_observation == null) return;
        if (_observation.background_input) InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)_observation.background_input_value;
        if (_observation.editor_input) InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)_observation.editor_input_value;
    }
    private static void DriveObservationCommand(ObservationInput command)
    {
        if (!_observationPlayer) throw new InvalidOperationException("Player no longer exists.");
        var enemies = _observationDirector.GetComponentsInChildren<Actor>().Where(actor => actor.Affiliation == CombatAffiliation.Enemy && !actor.IsDead);
        var target = enemies.OrderBy(actor => (actor.transform.position - _observationPlayer.transform.position).sqrMagnitude).ThenBy(actor => actor.name, StringComparer.Ordinal).FirstOrDefault();
        Vector3 aim = target ? target.transform.position : _observationPlayer.transform.position + _observationPlayer.transform.forward * 3;
        aim = command.action == "move" ? _observationPlayer.transform.position + new Vector3(command.x, 0, command.z) : aim + new Vector3(command.x, 0, command.z);
        if (_observationMouse != null && Camera.main)
            InputState.Change(_observationMouse.position, (Vector2)Camera.main.WorldToScreenPoint(aim));
        bool accepted;
        var holder = _observationPlayer.GetComponent<AbilityHolder>();
        if (command.action == "basic") accepted = _observationPlayer.GetComponent<CharControlScript>().TryDirectionalBasicAttack(aim);
        else if (command.action == "skill") accepted = holder.TryUseAbility(command.slot);
        else if (command.action == "move") accepted = (bool)typeof(CharControlScript).GetMethod("TryRequestGroundMovement", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_observationPlayer.GetComponent<CharControlScript>(), new object[]{aim});
        else accepted = holder.TryFinishSequence();
        RecordObservationEvent("command", _observationPlayer, JsonUtility.ToJson(new ObservationCommandResult { command = command, accepted = accepted, aim = aim }));
    }
    [Serializable] private sealed class ObservationCommandResult { public ObservationInput command; public bool accepted; public Vector3 aim; }

    private static void PrepareObservationManifest()
    {
        var request = _observation.request;
        var camera = Camera.main;
        if (!camera) throw new InvalidOperationException("No real tagged gameplay camera is active.");
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("No graphics device; real rendered evidence is unavailable.");
        var enemies = Object.FindObjectsByType<Actor>(FindObjectsSortMode.None).Where(actor => actor.Affiliation == CombatAffiliation.Enemy && !actor.IsDead).ToArray();
        if (request.mode == "seeded_combat" && (enemies.Length != 5 || _observationDirector.ActiveRunSeed.ToString(CultureInfo.InvariantCulture) != request.seed))
            throw new InvalidOperationException("Effective seed/enemy count differ from the prepared production scenario.");
        var actors = Object.FindObjectsByType<Actor>(FindObjectsSortMode.None).OrderBy(actor => actor.Affiliation).ThenBy(actor => actor.name, StringComparer.Ordinal)
            .ThenBy(actor => actor.transform.position.x).ThenBy(actor => actor.transform.position.z).Select(actor => new ObservationActor
            {
                name = actor.name, affiliation = actor.Affiliation.ToString(), position = actor.transform.position, rotation = actor.transform.rotation,
                health = actor.health, max_health = actor.maxHealth, rank = actor.TryGetComponent(out CombatReactionController reaction) ? reaction.Rank.ToString() : ""
            }).ToArray();
        var scene = SceneManager.GetActiveScene();
        _observationManifest.provenance.scene = scene.path;
        var conditions = _observationManifest.conditions;
        conditions.scenario_version = ReadObservationProfile(request.scenario_id).scenario_version;
        conditions.scene_guid = AssetDatabase.AssetPathToGUID(scene.path);
        conditions.seed = request.mode == "current" ? _observationDirector ? _observationDirector.ActiveRunSeed.ToString(CultureInfo.InvariantCulture) : "uncontrolled" : request.seed;
        conditions.weapon = request.mode == "current" ? (int)WeaponRunModifiers.Identify(_observationPlayer.CurrentWeapon) : request.weapon;
        conditions.node_id = _observationDirector && _observationDirector.ManualSession != null ? _observationDirector.ManualSession.CurrentNode : -1;
        conditions.enemy_count = enemies.Length;
        conditions.width = Screen.width; conditions.height = Screen.height;
        conditions.simulation_fps = request.mode == "current" ? 0 : request.simulation_fps;
        conditions.capture_every_frames = request.capture_every_frames;
        conditions.input_schedule = JsonUtility.ToJson(new ObservationSchedule { commands = request.input_schedule });
        conditions.initial_actor_fingerprint = HashObservationBytes(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new ObservationActors { actors = actors })));
        conditions.camera_json = ObservationCameraJson(camera);
        conditions.render_json = ObservationRenderJson(camera);
        conditions.random_control = request.mode == "current" ? "uncontrolled current gameplay; observation only" : request.control_enemy_rng ?
            "UnityEngine.Random.InitState(seed) before encounter; EnemyAI._rng explicit main-thread fixture: int(seed)+spawnIndex*7919 before first enemy Update; physics/NavMesh not bitwise guaranteed" :
            "UnityEngine.Random.InitState(seed); per-entity EnemyAI System.Random uncontrolled (instance IDs vary)";
        _observation.camera_id = EntityId.ToULong(camera.GetEntityId());
        _observation.camera_json = EditorJsonUtility.ToJson(camera);
        PersistObservation();
    }
    private static string ObservationCameraJson(Camera camera)
    {
        var value = new ObservationCamera
        {
            name = camera.name, position = camera.transform.position, rotation = camera.transform.rotation, orthographic = camera.orthographic,
            orthographic_size = camera.orthographicSize, field_of_view = camera.fieldOfView, near = camera.nearClipPlane, far = camera.farClipPlane,
            rect = camera.rect, hdr = camera.allowHDR, msaa = camera.allowMSAA, clear_flags = (int)camera.clearFlags, culling_mask = camera.cullingMask, background = camera.backgroundColor
        };
        var follow = camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();
        if (follow)
        {
            var data = new SerializedObject(follow);
            value.follow_enabled = follow.enabled;
            value.follow_height = data.FindProperty("m_Height").floatValue;
            value.follow_distance = data.FindProperty("m_Distance").floatValue;
            value.follow_angle = data.FindProperty("m_Angle").floatValue;
        }
        return JsonUtility.ToJson(value);
    }
    private static string ObservationRenderJson(Camera camera)
    {
        var pipeline = GraphicsSettings.currentRenderPipeline;
        string path = pipeline ? AssetDatabase.GetAssetPath(pipeline) : "";
        var urp = pipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        var additional = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        return JsonUtility.ToJson(new ObservationRender
        {
            pipeline = path, pipeline_guid = AssetDatabase.AssetPathToGUID(path), pipeline_sha256 = File.Exists(path) ? HashObservationFile(path) : "",
            graphics_settings_sha256 = HashObservationFile("ProjectSettings/GraphicsSettings.asset"), quality_settings_sha256 = HashObservationFile("ProjectSettings/QualitySettings.asset"),
            color_space = QualitySettings.activeColorSpace.ToString(), quality = QualitySettings.GetQualityLevel(), quality_name = QualitySettings.names[QualitySettings.GetQualityLevel()],
            gpu = SystemInfo.graphicsDeviceName, graphics_api = SystemInfo.graphicsDeviceType.ToString(), post_processing = additional && additional.renderPostProcessing,
            anti_aliasing = urp ? urp.msaaSampleCount : QualitySettings.antiAliasing, render_scale = urp ? urp.renderScale : 1f,
            volume_profiles = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Where(volume => volume.sharedProfile)
                .Select(volume => AssetDatabase.GetAssetPath(volume.sharedProfile)).OrderBy(profile => profile, StringComparer.Ordinal)
                .Select(profile => profile + ":" + (File.Exists(profile) ? HashObservationFile(profile) : "runtime")).ToArray()
        });
    }
    private static void CaptureObservationFrame(int relativeFrame)
    {
        Texture2D image = null;
        try
        {
            image = ScreenCapture.CaptureScreenshotAsTexture();
            if (!image || image.width != _observation.request.width || image.height != _observation.request.height)
                throw new InvalidOperationException("Rendered screenshot resolution does not match the requested fixed Game View size.");
            byte[] bytes = image.EncodeToPNG();
            string relative = "frames/frame-" + relativeFrame.ToString("D6", CultureInfo.InvariantCulture) + ".png";
            File.WriteAllBytes(Path.Combine(_observation.output_directory, relative), bytes);
            _observationManifest.frames.Add(new ObservationFrame
            {
                index = _observationManifest.frames.Count, relative_frame = relativeFrame,
                time = _observation.request.mode == "current" ? (float)(Time.unscaledTimeAsDouble - _observationStartUnscaled) : relativeFrame / (float)_observation.request.simulation_fps,
                path = relative, sha256 = HashObservationBytes(bytes), width = image.width, height = image.height
            });
            PersistObservation();
        }
        finally { if (image) Object.DestroyImmediate(image); }
    }

    private static void FinishObservation(string status, string error)
    {
        if (_observation == null || _observationFinishing || _observation.stage == "restoring") return;
        _observationFinishing = true;
        try
        {
            EditorApplication.update -= ObservationStartupTick;
            if (_observationCoroutine != null && _observationPlayer) _observationPlayer.StopCoroutine(_observationCoroutine);
            _observationCoroutine = null;
            EndObservationTelemetry(); CleanupObservationInput();
            if (!string.IsNullOrEmpty(ObservationTelemetryFailure))
            { status = "failed"; error = "Telemetry failed: " + ObservationTelemetryFailure; }
            _observationManifest.status = status; _observationManifest.provenance.error = error;
            string events = Path.Combine(_observation.output_directory, "events.jsonl");
            if (File.Exists(events)) _observationManifest.events_sha256 = HashObservationFile(events);
            _observation.stage = "restoring";
            PersistObservation();
            if (!_observation.started_playing && EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            else
            {
                if (_observation.started_playing || !EditorApplication.isPlayingOrWillChangePlaymode) _observationRestoreReady = true;
                RestoreObservationState();
            }
        }
        finally { _observationFinishing = false; }
    }
    private static void FailObservation(Exception exception)
    {
        Debug.LogError("Gameplay observation failed: " + exception);
        if (_observation != null && _observationManifest != null) FinishObservation("failed", exception.Message);
        else _observationLastStatus = ObservationResponse("failed", exception.Message);
    }
    private static void RestoreObservationState()
    {
        if (_observation == null) return;
        if (!_observation.started_playing && (!_observationRestoreReady || EditorApplication.isPlayingOrWillChangePlaymode)) return;
        var restoration = _observationManifest.restoration;
        var failures = new List<string>();
        try { RestoreObservationPreferences(_observation.preferences); restoration.preferences = true; } catch (Exception exception) { failures.Add("preferences: " + exception.Message); }
        try
        {
            if (!_observation.started_playing)
            {
                Time.timeScale = _observation.time_scale; Time.captureDeltaTime = _observation.capture_delta;
                if (QualitySettings.GetQualityLevel() != _observation.quality) QualitySettings.SetQualityLevel(_observation.quality, false);
                UnityEngine.Random.state = JsonUtility.FromJson<ObservationRandom>(_observation.random_json).state;
            }
            ShaderUtil.allowAsyncCompilation = _observation.shader_async;
            CleanupObservationInput();
            restoration.editor_settings = true;
        }
        catch (Exception exception) { failures.Add("settings: " + exception.Message); }
        try
        {
            if (!_observation.started_playing)
            {
                // Reload the on-disk scene to discard the seed override before restoring all previous scenes.
                if (SceneManager.GetActiveScene().path == ObservationScene) EditorSceneManager.OpenScene(ObservationScene, OpenSceneMode.Single);
                EditorSceneManager.RestoreSceneManagerSetup(_observation.scenes.Select(scene => new SceneSetup { path = scene.path, isLoaded = scene.loaded, isActive = scene.active }).ToArray());
                Selection.objects = _observation.selection.Select(id => GlobalObjectId.TryParse(id, out var global) ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(global) : null).Where(value => value).ToArray();
            }
            restoration.scene_setup = true;
        }
        catch (Exception exception) { failures.Add("scene setup: " + exception.Message); }
        try { RestoreObservationGameView(); restoration.game_view = true; } catch (Exception exception) { failures.Add("Game View: " + exception.Message); }
        restoration.success = failures.Count == 0;
        restoration.restored = restoration.success;
        restoration.error = string.Join("; ", failures);
        if (!restoration.success) _observationManifest.status = "failed";
        string events = Path.Combine(_observation.output_directory, "events.jsonl");
        if (File.Exists(events)) _observationManifest.events_sha256 = HashObservationFile(events);
        PersistObservation();
        _observation.stage = "finished";
        _observationLastStatus = ObservationStatus();
        SessionState.EraseString(ObservationRecoveryKey);
        _observation = null; _observationManifest = null; _observationPlayer = null; _observationDirector = null;
    }

    private static void ConfigureObservationGameView()
    {
        Type viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView") ?? throw new NotSupportedException("Unity GameView API unavailable.");
        var views = Resources.FindObjectsOfTypeAll(viewType).OfType<EditorWindow>().ToArray();
        _observation.view_existed = views.Length > 0;
        _observationView = views.FirstOrDefault() ?? EditorWindow.GetWindow(viewType);
        _observation.view_id = EntityId.ToULong(_observationView.GetEntityId());
        _observation.view_position = _observationView.position;
        _observation.view_size_index = (int)viewType.GetProperty("selectedSizeIndex", ObservationFlags).GetValue(_observationView);
        object groupType = viewType.GetProperty("currentSizeGroupType", ObservationFlags).GetValue(_observationView);
        _observation.view_group = (int)groupType;
        object group = ObservationGameViewGroup(groupType);
        int count = (int)group.GetType().GetMethod("GetTotalCount", ObservationFlags).Invoke(group, null);
        int selected = -1;
        for (int i = 0; i < count; i++)
        {
            object size = group.GetType().GetMethod("GetGameViewSize", ObservationFlags).Invoke(group, new object[] { i });
            var type = size.GetType();
            if ((int)type.GetProperty("width", ObservationFlags).GetValue(size) == _observation.request.width &&
                (int)type.GetProperty("height", ObservationFlags).GetValue(size) == _observation.request.height &&
                type.GetProperty("sizeType", ObservationFlags).GetValue(size).ToString() == "FixedResolution") { selected = i; break; }
        }
        if (selected < 0)
        {
            var assembly = typeof(EditorWindow).Assembly;
            var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            var kindType = assembly.GetType("UnityEditor.GameViewSizeType");
            _observation.temporary_size_name = "TG-" + _observationManifest.run_id;
            object size = Activator.CreateInstance(sizeType, ObservationFlags, null, new[] { Enum.Parse(kindType, "FixedResolution"), (object)_observation.request.width, _observation.request.height, _observation.temporary_size_name }, CultureInfo.InvariantCulture);
            group.GetType().GetMethod("AddCustomSize", ObservationFlags).Invoke(group, new[] { size });
            selected = count; _observation.temporary_size_index = selected;
        }
        PersistObservation();
        viewType.GetProperty("selectedSizeIndex", ObservationFlags).SetValue(_observationView, selected);
        _observationView.Show(); _observationView.Focus(); _observationView.Repaint();
    }
    private static object ObservationGameViewGroup(object groupType)
    {
        Type sizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes");
        object singleton = sizesType.BaseType.GetProperty("instance", ObservationFlags | BindingFlags.FlattenHierarchy).GetValue(null);
        return sizesType.GetMethod("GetGroup", ObservationFlags).Invoke(singleton, new[] { groupType });
    }
    private static void RestoreObservationGameView()
    {
        Type viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        var view = EditorUtility.EntityIdToObject(EntityId.FromULong(_observation.view_id)) as EditorWindow;
        if (!view) view = Resources.FindObjectsOfTypeAll(viewType).OfType<EditorWindow>().FirstOrDefault();
        if (!view) return;
        if (_observation.temporary_size_index >= 0)
        {
            Type groupType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeGroupType");
            object group = ObservationGameViewGroup(Enum.ToObject(groupType, _observation.view_group));
            int count = (int)group.GetType().GetMethod("GetTotalCount", ObservationFlags).Invoke(group, null);
            if (_observation.temporary_size_index < count)
            {
                object size = group.GetType().GetMethod("GetGameViewSize", ObservationFlags).Invoke(group, new object[] { _observation.temporary_size_index });
                if ((string)size.GetType().GetProperty("baseText", ObservationFlags).GetValue(size) != _observation.temporary_size_name)
                    throw new InvalidOperationException("Temporary Game View size changed externally; it was not removed.");
                group.GetType().GetMethod("RemoveCustomSize", ObservationFlags).Invoke(group, new object[] { _observation.temporary_size_index });
            }
        }
        viewType.GetProperty("selectedSizeIndex", ObservationFlags).SetValue(view, _observation.view_size_index);
        if (_observation.view_existed) view.position = _observation.view_position; else view.Close();
    }
    private static void PersistObservation()
    {
        if (_observation == null) return;
        SessionState.SetString(ObservationRecoveryKey, JsonUtility.ToJson(_observation));
        if (_observationManifest != null && !string.IsNullOrEmpty(_observation.output_directory))
            File.WriteAllText(Path.Combine(_observation.output_directory, "manifest.json"), JsonUtility.ToJson(_observationManifest, true));
    }
    private static string HashObservationBytes(byte[] bytes)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }
    private static string HashObservationFile(string path) => HashObservationBytes(File.ReadAllBytes(path));
    private static string ObservationGit(string arguments)
    {
        try
        {
            using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", arguments)
            { WorkingDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..")), UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true }))
            {
                string result = process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit(5000)) { process.Kill(); return "unavailable: git timeout"; }
                return process.ExitCode == 0 ? result.Trim() : "unavailable";
            }
        }
        catch { return "unavailable"; }
    }
}
