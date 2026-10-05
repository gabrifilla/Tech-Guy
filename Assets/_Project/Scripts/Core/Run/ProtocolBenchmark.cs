#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>Explicit diagnostics fixture; absent from release players. Smoke is never D8 acceptance.</summary>
public sealed class ProtocolBenchmark : MonoBehaviour
{
    public const int Seed = 501703;
    public const string Revision = "20261005-reentry-scope-profiling";
    public string State { get; private set; } = "idle";
    public bool Finished { get; private set; }
    public string OutputPath { get; private set; }
    private bool _started, _smoke, _restored;
    private string _caseBuild;
    private int _caseDensity;
    private bool _caseVfx;
    private int _oldSelection, _oldFrameRate, _oldVsync, _oldCoins;
    private bool _hadSelection, _hadCoins;
    private PlayerActor _player;
    private RunBoons _run;
    private ProgressionDirector _director;
    private ProtocolRuntime _runtime;
    private Actor[] _targets;
    private GameObject _targetRoot;
    private readonly List<CaseResult> _results = new List<CaseResult>(54);
    private readonly CultureInfo _culture = CultureInfo.InvariantCulture;
    [Serializable] private sealed class CaseResult
    {
        public string build, rawFile;
        public int density, repetition, frames, width, height;
        public bool vfx, smoke, cpuRecorderValid, memoryRecorderValid, gcRecorderValid;
        public string gcMeasurement = "whole-frame Unity GC Allocated In Frame; conservative upper bound, not isolated Protocol allocations";
        public double cpuP95Ms, cpuP99Ms, gcP95Bytes, frameP95Ms, memoryFirst, memoryLast;
        public int dropsImpact, dropsTick, dropsQuery, dropsVfx;
    }
    private struct Sample { public int Frame; public double Cpu, Gc, Duration, Memory; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var arguments = Environment.GetCommandLineArgs();
        if (!arguments.Contains("-protocol-benchmark")) return;
        var host = new GameObject("Protocol benchmark"); DontDestroyOnLoad(host);
        string output = Path.Combine(Application.persistentDataPath, "ProtocolBenchmarks", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        int index = Array.IndexOf(arguments, "-protocol-output");
        if (index >= 0 && index + 1 < arguments.Length) output = arguments[index + 1];
        int caseIndex = Array.IndexOf(arguments, "-protocol-case");
        string caseFilter = caseIndex >= 0 && caseIndex + 1 < arguments.Length ? arguments[caseIndex + 1] : null;
        host.AddComponent<ProtocolBenchmark>().Begin(output, arguments.Contains("-protocol-smoke"), caseFilter);
    }
    public void Begin(string outputPath, bool smoke = false, string caseFilter = null)
    {
        if (_started) throw new InvalidOperationException("Benchmark already started.");
        if (!string.IsNullOrEmpty(caseFilter))
        {
            var parts = caseFilter.Split(':');
            if (parts.Length != 3 || !new[] { "baseline", "reachable-rank1", "stress-all-rank3" }.Contains(parts[0]) ||
                !int.TryParse(parts[1], out _caseDensity) || !new[] { 8, 32, 64 }.Contains(_caseDensity) ||
                (parts[2] != "on" && parts[2] != "off")) throw new ArgumentException("Case must be build:density:on|off.");
            _caseBuild = parts[0]; _caseVfx = parts[2] == "on";
        }
        _started = true; _smoke = smoke; OutputPath = Path.GetFullPath(outputPath); Directory.CreateDirectory(OutputPath);
        _oldSelection = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0);
        _hadSelection = PlayerPrefs.HasKey(WeaponLoadout.PreferenceKey);
        _hadCoins = PlayerPrefs.HasKey(CurrencyWallet.PreferenceKey); _oldCoins = PlayerPrefs.GetInt(CurrencyWallet.PreferenceKey);
        _oldFrameRate = Application.targetFrameRate; _oldVsync = QualitySettings.vSyncCount;
        PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0); QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
        if (!Application.isEditor) Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
        State = "acquiring rank 1 through published tickets";
        File.WriteAllText(Path.Combine(OutputPath, "environment.txt"),
            "revision=" + Revision + "\nbuildGUID=" + Application.buildGUID + "\nseed=" + Seed + "\nUnity=" + Application.unityVersion + "\nEditor=" + Application.isEditor +
            "\nDevelopment=" + Debug.isDebugBuild + "\nbackend=" + Backend + "\nCPU=" + SystemInfo.processorType +
            "\nthreads=" + SystemInfo.processorCount + "\nRAM_MB=" + SystemInfo.systemMemorySize +
            "\nGPU=" + SystemInfo.graphicsDeviceName + "\nAPI=" + SystemInfo.graphicsDeviceType +
            "\nquality=" + QualitySettings.names[QualitySettings.GetQualityLevel()] +
            "\nmode=" + (smoke ? "SMOKE: no performance acceptance" : "D8: warmup 10s, samples 60s, 3 repetitions") +
            "\ncase=" + (caseFilter ?? "full 54-case matrix") +
            "\nfixture=published Fragile visuals; AI disabled; confirmed 10 HP at 10Hz across density targets in one root; feedback duration 0; generation reset every 10s\n");
        StartCoroutine(GuardedMatrix());
    }
    private IEnumerator GuardedMatrix()
    {
        var stack = new Stack<IEnumerator>(); stack.Push(Matrix());
        while (stack.Count > 0)
        {
            object current = null; bool moved = false; Exception failure = null;
            try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
            catch (Exception error) { failure = error; }
            if (failure != null)
            {
                while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
                State = "failed"; Finished = true; WriteProgress(); RestoreSettings();
                File.WriteAllText(Path.Combine(OutputPath, "failure.txt"), failure.ToString());
                Debug.LogException(failure, this);
                if (!Application.isEditor) Application.Quit(1);
                yield break;
            }
            if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
            if (current is IEnumerator child) stack.Push(child); else yield return current;
        }
    }
    private static string Backend
    {
        get {
#if ENABLE_IL2CPP
            return "IL2CPP";
#else
            return "Mono";
#endif
        }
    }
    private IEnumerator AcquirePublishedBuild()
    {
        yield return SceneManager.LoadSceneAsync("FirstSector"); yield return null; yield return null; yield return null;
        _director = FindAnyObjectByType<ProgressionDirector>(); _player = FindAnyObjectByType<PlayerActor>();
        _run = _player.GetComponent<RunBoons>(); _runtime = _player.GetComponent<ProtocolRuntime>();
        _player.AddDamageImmunity(this); _player.Stats.criticalChance = 0;
        _player.GetComponent<CharControlScript>().enabled = false;
        var rewardUi = _player.GetComponent<RunRewardUI>(); if (rewardUi) rewardUi.enabled = false;
        var mapUi = _director.GetComponent<ManualRunUI>(); if (mapUi) mapUi.enabled = false;
        string[] desired = { "ignite", "conductor", "frost", "resonance" }; int opportunity = 0;
        foreach (int node in new[] { 1, 2, 4, 5, 6, 9, 10 })
        {
            var session = _director.ManualSession;
            if (!_director.TryCommitDestination(session.Id, node) || !_player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(node).Center) ||
                !_director.TryEnterNode(session.Id, node)) throw new InvalidOperationException("Benchmark cannot enter published node " + node);
            yield return null;
            if (node == 10) break;
            foreach (var enemy in _director.GetComponentsInChildren<Actor>()) enemy.TakeDamage(1000000);
            float deadline = Time.realtimeSinceStartup + 10;
            while (session.Phase == RunPhase.Encounter) { CheckDeadline(deadline); yield return null; }
            while (session.Phase == RunPhase.RewardPending)
            {
                deadline = Time.realtimeSinceStartup + 10;
                while (!_run.IsChoosing) { CheckDeadline(deadline); yield return null; }
                yield return null; yield return null;
                bool protocolTicket = _run.ManualTicket.AnnouncedKind == RunRewardKind.Protocol;
                string desiredId = protocolTicket ? desired[opportunity] : null;
                int index = protocolTicket ? _run.Choices.ToList().FindIndex(offer => offer.Id == desiredId) : 0;
                if (index < 0) throw new InvalidOperationException("Published benchmark choice is unavailable.");
                deadline = Time.realtimeSinceStartup + 10;
                while (!_run.Choose(index)) { CheckDeadline(deadline); yield return null; }
                if (protocolTicket) opportunity++;
            }
            yield return null;
        }
        foreach (var enemy in _director.GetComponentsInChildren<Actor>()) enemy.gameObject.SetActive(false);
        _director.enabled = false; // Fixed open encounter: benchmark actors own controlled generations.
        _player.GetComponent<CharControlScript>().enabled = false;
        _player.GetComponent<AbilityHolder>().enabled = false;
        var map = _director.GetComponent<ManualRunUI>(); if (map) map.enabled = false;
        var rewards = _player.GetComponent<RunRewardUI>(); if (rewards) rewards.enabled = false;
    }
    private static void CheckDeadline(float deadline)
    { if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("Published benchmark setup timed out."); }
    private IEnumerator Matrix()
    {
        yield return AcquirePublishedBuild();
        int[] densities = _smoke ? new[] { 8 } : new[] { 8, 32, 64 };
        bool[] visualModes = _smoke ? new[] { true } : new[] { true, false };
        foreach (string build in new[] { "baseline", "reachable-rank1", "stress-all-rank3" })
        {
            if (build == "stress-all-rank3")
                for (int kind = 0; kind < 7; kind++)
                    while (_run.Protocols.Rank((ProtocolKind)kind) < 3)
                        if (!_run.Protocols.TryAcquire(_run.Protocols.SessionId, (ProtocolKind)kind, _run.Protocols.Revision, _ => true))
                            throw new InvalidOperationException("Stress injection failed.");
            foreach (int density in densities)
                foreach (bool vfx in visualModes)
                {
                    if (_caseBuild != null && (build != _caseBuild || density != _caseDensity || vfx != _caseVfx)) continue;
                    for (int repeat = 0; repeat < (_smoke ? 1 : 3); repeat++)
                        yield return Case(build, density, vfx, repeat);
                }
        }
        Finished = true; State = "complete"; WriteProgress();
        RestoreSettings();
        if (!Application.isEditor) Application.Quit();
    }
    private void BuildTargets(int density)
    {
        _targetRoot = new GameObject("Benchmark target generations"); _targetRoot.SetActive(false);
        _targets = new Actor[density]; var random = new System.Random(Seed);
        var prefab = _director.ManualSession.Route.Node(1).Spawns[0].Prefab;
        Vector3 origin = _player.transform.position + Vector3.forward * 3;
        for (int i = 0; i < density; i++)
        {
            var obj = Instantiate(prefab, origin + new Vector3((i % 8 - 3.5f) * .45f, 0, (i / 8) * .45f + (float)random.NextDouble() * .01f), Quaternion.identity, _targetRoot.transform);
            var ai = obj.GetComponent<EnemyAI>(); if (ai) ai.enabled = false;
            var actions = obj.GetComponent<EnemyCombatActions>(); if (actions) actions.enabled = false;
            var membership = obj.GetComponent<ProtocolEncounterMembership>() ?? obj.AddComponent<ProtocolEncounterMembership>();
            membership.Configure(_run.Protocols.SessionId, _runtime.EncounterId);
            _targets[i] = obj.GetComponent<Actor>(); _targets[i].health = 1000000;
        }
        _targetRoot.SetActive(true);
        foreach (var target in _targets) { target.SetMaxHealth(1000000); target.RestoreHealthToMax(); }
        Physics.SyncTransforms();
    }
    private IEnumerator Case(string build, int density, bool vfx, int repeat)
    {
        State = build + "/" + density + "/vfx=" + vfx + "/repeat=" + repeat; WriteProgress();
        _runtime.Configure(_player, _run.Protocols, _run); _runtime.Feedback.Enabled = vfx;
        _runtime.SetChillRollForTests(() => 0); // Fixed seed/element outcome; never used as gameplay proof.
        if (build == "baseline") _runtime.EndOwnedState();
        BuildTargets(density); yield return null;
        var options = ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.WrapAroundWhenCapacityReached | ProfilerRecorderOptions.SumAllSamplesInFrame;
        using (var cpu = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "TechGuy.Protocol.CPU", 1, options))
        using (var memory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory", 1, options))
        using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1, options))
        {
            var samples = new Sample[5000]; int count = 0;
            float warmup = _smoke ? .5f : 10, duration = _smoke ? 1 : 60;
            float started = Time.realtimeSinceStartup, nextAttack = started, nextGeneration = started + 10;
            while (Time.realtimeSinceStartup - started < warmup + duration)
            {
                if (Time.realtimeSinceStartup >= nextAttack)
                {
                    nextAttack = Time.realtimeSinceStartup + .1f;
                    using (_player.BeginImpactFeedback(0))
                    using (var execution = _runtime.BeginExecution())
                    using (_runtime.OpenExecutionScope(execution))
                        foreach (var target in _targets) _player.DealResolvedAttackDamage(target, 10);
                }
                if (Time.realtimeSinceStartup >= nextGeneration)
                {
                    nextGeneration += 10;
                    foreach (var target in _targets) { target.gameObject.SetActive(false); target.RestoreHealthToMax(); target.gameObject.SetActive(true); }
                    Physics.SyncTransforms();
                }
                if (Time.realtimeSinceStartup - started >= warmup)
                {
                    if (count >= samples.Length) throw new InvalidOperationException("Sample capacity reached; run invalid.");
                    samples[count++] = new Sample { Frame = Time.frameCount - 1, Cpu = cpu.LastValue / 1000000d,
                        Gc = gc.LastValue, Duration = Time.unscaledDeltaTime * 1000d, Memory = memory.LastValue };
                }
                yield return null;
            }
            var result = new CaseResult { build = build, density = density, vfx = vfx, repetition = repeat, frames = count, smoke = _smoke,
                width = Screen.width, height = Screen.height,
                cpuRecorderValid = cpu.Valid, memoryRecorderValid = memory.Valid, gcRecorderValid = gc.Valid,
                cpuP95Ms = Percentile(samples, count, s => s.Cpu, .95), cpuP99Ms = Percentile(samples, count, s => s.Cpu, .99),
                gcP95Bytes = Percentile(samples, count, s => s.Gc, .95), frameP95Ms = Percentile(samples, count, s => s.Duration, .95),
                memoryFirst = count > 0 ? samples[0].Memory : 0, memoryLast = count > 0 ? samples[count - 1].Memory : 0,
                dropsImpact = _runtime.Budget.DroppedImpacts, dropsTick = _runtime.Budget.DroppedTicks,
                dropsQuery = _runtime.Budget.DroppedQueries, dropsVfx = _runtime.Feedback.Dropped };
            result.rawFile = build + "-" + density + "-vfx" + vfx + "-r" + repeat + ".csv";
            using (var writer = new StreamWriter(Path.Combine(OutputPath, result.rawFile)))
            {
                writer.WriteLine("frame,protocol_cpu_ms,whole_frame_gc_bytes,frame_ms,total_used_memory");
                for (int i = 0; i < count; i++) writer.WriteLine(samples[i].Frame + "," + samples[i].Cpu.ToString("R", _culture) + "," +
                    samples[i].Gc.ToString("R", _culture) + "," + samples[i].Duration.ToString("R", _culture) + "," + samples[i].Memory.ToString("R", _culture));
            }
            _results.Add(result); File.WriteAllText(Path.Combine(OutputPath, result.rawFile + ".json"), JsonUtility.ToJson(result, true));
        }
        _runtime.CloseEncounter(); Destroy(_targetRoot); _targetRoot = null; _targets = null; yield return null;
    }
    private static double Percentile(Sample[] source, int count, Func<Sample, double> selector, double p)
    {
        if (count == 0) return double.NaN;
        var values = new double[count]; for (int i = 0; i < count; i++) values[i] = selector(source[i]);
        Array.Sort(values); return values[Math.Max(0, (int)Math.Ceiling(count * p) - 1)];
    }
    private void WriteProgress() => File.WriteAllText(Path.Combine(OutputPath, "progress.txt"), State + "\ncompleted=" + _results.Count + "\nsmoke=" + _smoke);
    private void RestoreSettings()
    {
        if (_restored) return;
        _restored = true;
        if (_hadSelection) PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, _oldSelection); else PlayerPrefs.DeleteKey(WeaponLoadout.PreferenceKey);
        if (_hadCoins) PlayerPrefs.SetInt(CurrencyWallet.PreferenceKey, _oldCoins); else PlayerPrefs.DeleteKey(CurrencyWallet.PreferenceKey);
        PlayerPrefs.Save();
        Application.targetFrameRate = _oldFrameRate; QualitySettings.vSyncCount = _oldVsync;
    }
    private void OnDestroy() { if (_started) RestoreSettings(); }
}
#endif
