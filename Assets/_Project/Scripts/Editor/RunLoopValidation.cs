using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class RunLoopValidation
{
    private const string Pending = "TechGuy.RunLoopValidation";
    private static int _phase, _weaponIndex, _room, _checks, _errors;
    private static double _next;
    private static PlayerActor _player;
    private static CharControlScript _controls;
    private static AbilityHolder _holder;
    private static Actor _dummy;
    private static FirstSectorDirector _director;
    private static SectorBoss _boss;
    private static bool _bossTested;
    private static bool _damagePopupConfirmed;
    private static float _healthBeforeBoss;
    private static double _bossDeadline;
    private static readonly Vector3[] RoomPoints = { new Vector3(0,.1f,-4), new Vector3(6,.1f,23), new Vector3(-4,.1f,48) };

    static RunLoopValidation() { if (SessionState.GetBool(Pending, false)) Subscribe(); }
    public static void Run()
    {
        _next = EditorApplication.timeSinceStartup + 20;
        EditorApplication.update += Wait;
    }
    private static void Wait()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < _next) return;
        EditorApplication.update -= Wait;
        SessionState.SetBool(Pending + ".had", PlayerPrefs.HasKey(WeaponLoadout.PreferenceKey));
        SessionState.SetInt(Pending + ".value", PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey));
        PlayerPrefs.DeleteKey(WeaponLoadout.PreferenceKey);
        EditorSceneManager.OpenScene(LobbySceneBuilder.ScenePath);
        SessionState.SetBool(Pending, true); Subscribe(); EditorApplication.EnterPlaymode();
    }
    private static void Subscribe()
    {
        EditorApplication.playModeStateChanged -= State;
        EditorApplication.playModeStateChanged += State;
    }
    private static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        _phase = _weaponIndex = _room = _checks = _errors = 0;
        _bossTested = false;
        _next = EditorApplication.timeSinceStartup + 3;
        Application.logMessageReceived += Log;
        EditorApplication.update += Tick;
    }
    private static void Log(string text, string stack, LogType type)
    { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) _errors++; }
    private static T[] Components<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
        .SelectMany(root => root.GetComponentsInChildren<T>()).ToArray();
    private static void Resolve()
    {
        _player = Components<PlayerActor>().Single(); _controls = _player.GetComponent<CharControlScript>();
        _holder = _player.GetComponent<AbilityHolder>();
    }
    private static void Next(int phase, double delay = .25) { _phase = phase; _next = EditorApplication.timeSinceStartup + delay; }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); _checks++; }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        try
        {
            switch (_phase)
            {
                case 0:
                    Resolve();
                    Require(WeaponLoadout.Select(_player, _weaponIndex), "Equip basic test weapon");
                    _player.Stats.criticalChance = 0;
                    var config = new SerializedObject(_player);
                    config.FindProperty("_manaRegenerationPercentPerSecond").floatValue = 0;
                    config.ApplyModifiedPropertiesWithoutUndo();
                    var target = new GameObject("Basic attack target"); target.SetActive(false);
                    _dummy = target.AddComponent<Actor>(); _dummy.health = 1000;
                    target.AddComponent<BoxCollider>();
                    target.transform.position = _player.transform.position + Vector3.forward * 1.4f + Vector3.up;
                    target.SetActive(true); Physics.SyncTransforms();
                    _damagePopupConfirmed = false;
                    _dummy.DamageReceived += (victim, amount) =>
                        _damagePopupConfirmed = Components<DamagePopup>().Any(popup => Mathf.Approximately(popup.Amount,amount));
                    Next(1); break;
                case 1:
                    _player.mana = 0;
                    Require(_controls.TryBasicAttack(_dummy.transform.position), "Basic attack accepted with zero mana");
                    Require(!_controls.TryBasicAttack(_dummy.transform.position), "Basic cadence enforced");
                    Next(2, .85); break;
                case 2:
                    Require(_dummy.health < 1000, "Basic attack damaged target for weapon " + _weaponIndex);
                    Require(_dummy.GetComponent<EnemyCombatFeedback>() && _dummy.GetComponent<EnemyCombatFeedback>().HealthRatio < 1,
                        "Enemy health feedback reflects damage");
                    Require(_damagePopupConfirmed, "Basic attack creates confirmed damage number");
                    Require(_player.mana == 0, "Basic attack costs no mana");
                    Require(_controls.TryBasicAttack(_dummy.transform.position), "Basic attack repeats");
                    _player.RestoreMana(_player.maxMana);
                    Require(_holder.TryUseAbility(0), "Skill accepted");
                    Require(!_controls.TryBasicAttack(_dummy.transform.position), "Basic blocked during skill cast");
                    Next(3, 2); break;
                case 3:
                    Require(_holder.GetRemainingCooldown(0) > 0, "Skill remains on cooldown");
                    Require(_controls.TryBasicAttack(_player.transform.position + Vector3.forward), "Basic works while skill cools down");
                    Object.Destroy(_dummy.gameObject);
                    _weaponIndex++;
                    if (_weaponIndex < 3) Next(0, 1);
                    else
                    {
                        Require(WeaponLoadout.Select(_player, 0), "Select gauntlet for run");
                        SceneManager.LoadScene("FirstSector"); Next(4, 3);
                    }
                    break;
                case 4:
                    Resolve(); _director = Components<FirstSectorDirector>().Single();
                    Require(_director.Boons.Acquired.Count == 0, "New run starts without boons");
                    Require(_player.CurrentWeapon != Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), "Run owns weapon copy");
                    Require(_player.GetComponent<NavMeshAgent>().Warp(RoomPoints[_room]), "Enter room");
                    Next(5, 1); break;
                case 5:
                    if (_room == 2 && !_bossTested) { Next(50,.1); break; }
                    if (_room == 1 && _dummy)
                    {
                        Require(_dummy.health < 1000, "Transformed Q damages behind the player");
                        Object.Destroy(_dummy.gameObject);
                    }
                    var enemies = Components<EnemyAI>();
                    Require(enemies.Length == (_room == 1 ? 4 : 3), "Only current room enemies active");
                    foreach (var enemy in enemies)
                    {
                        var variant = enemy.GetComponent<EnemyVariant>();
                        bool boss = enemy.GetComponent<SectorBoss>();
                        EnemyRarity rarity = variant && variant.Profile ? variant.Profile.Rarity : EnemyRarity.Normal;
                        Require(enemy.GetComponent<EnemyCombatFeedback>().NameColor == EnemyVisualStyle.NameColor(rarity,boss), "Enemy name uses rarity color");
                    }
                    Require(_director.IsExitLocked(_room), "Room exit sealed during fight");
                    var path = new NavMeshPath();
                    Vector3 beyond = new[] { new Vector3(2,0,13), new Vector3(1,0,39), new Vector3(-4,0,65) }[_room];
                    NavMesh.CalculatePath(_player.transform.position, beyond, NavMesh.AllAreas, path);
                    Require(path.status != NavMeshPathStatus.PathComplete, "Sealed room blocks navigation");
                    foreach (var enemy in enemies) enemy.GetComponent<Actor>().TakeDamage(100000);
                    Require(Components<DamagePopup>().Any(popup => popup.IsLethal), "Lethal hits create elimination feedback");
                    Require(_director.Boons.IsChoosing && _director.IsExitLocked(_room), "Clear waits for reward");
                    Require(_director.Boons.Choices.Count == 3 && _director.Boons.Choices.Select(o => o.Id).Distinct().Count() == 3, "Three distinct rewards");
                    Require(!_controls.TryBasicAttack(Vector3.forward) && !_holder.TryUseAbility(0), "Reward modal blocks combat");
                    Require(_director.Boons.Choose(0), "Reward selection accepted");
                    Require(!_director.Boons.Choose(0), "Reward cannot be claimed twice");
                    Require(!_director.IsExitLocked(_room), "Chosen reward opens exit");
                    Require(_director.Boons.Acquired.Count == _room + 1, "Rewards accumulate");
                    if (_room == 0)
                    {
                        Require(_holder.ActiveAbilities[0] is ArsenalAbility, "Q transformed into nova");
                        Require(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]).abilities[0] is BreakerGauntletAbility, "Shared gauntlet unchanged");
                        var novaTarget = new GameObject("Nova target"); novaTarget.SetActive(false);
                        _dummy = novaTarget.AddComponent<Actor>(); _dummy.health = 1000;
                        novaTarget.AddComponent<BoxCollider>();
                        novaTarget.transform.position = _player.transform.position + Vector3.back * 2 + Vector3.up;
                        novaTarget.SetActive(true); Physics.SyncTransforms();
                        _player.RestoreMana(_player.maxMana);
                        Require(_holder.TryUseAbility(0), "Transformed Q is usable");
                    }
                    if (_room == 1) Require(Mathf.Approximately(_holder.ActiveAbilities[0].ManaCost,60), "Focus upgrades transformed Q");
                    _room++;
                    if (_room < 3)
                    {
                        Require(_player.GetComponent<NavMeshAgent>().Warp(RoomPoints[_room]), "Enter next room");
                        Next(5, 1);
                    }
                    else
                    {
                        Require(_director.IsComplete, "Run complete only after final reward");
                        _player.GetComponent<NavMeshAgent>().Warp(new Vector3(-4,.1f,59));
                        Next(6, 3);
                    }
                    break;
                case 6:
                    Require(SceneManager.GetActiveScene().name == "NexusLobby", "Extraction returns to lobby");
                    Resolve();
                    Require(!_player.GetComponent<RunBoons>() && _holder.ActiveAbilities[0] is BreakerGauntletAbility, "Run upgrades reset in lobby");
                    SceneManager.LoadScene("FirstSector"); Next(7, 3); break;
                case 7:
                    Resolve();
                    Require(_player.GetComponent<RunBoons>().Acquired.Count == 0, "Replay starts clean");
                    _player.TakeDamage(100000); Next(8, 4); break;
                case 8:
                    Require(SceneManager.GetActiveScene().name == "NexusLobby", "Death returns to lobby");
                    Require(_errors == 0, "No runtime errors: " + _errors);
                    Debug.Log("RUN_LOOP_VALIDATION_SUCCESS: " + _checks + " checks passed."); Finish(0); break;
                case 50:
                    _boss = Components<SectorBoss>().Single();
                    Require(!_boss.GetComponent<EnemyAI>().enabled, "Boss replaces generic attacks");
                    Require(_boss.GetComponent<Actor>().maxHealth == _boss.MaximumHealth, "Boss has dedicated health");
                    Require(_boss.GetComponent<CombatReactionController>().Rank == EnemyRank.Boss, "Boss reaction rank");
                    Require(_boss.GetComponent<EnemyVariant>().VisualScaleMultiplier == 2.1f, "Boss scale is distinct");
                    Vector3 bossScale = _boss.transform.localScale;
                    var bossVariant = _boss.GetComponent<EnemyVariant>();
                    bossVariant.Configure(bossVariant.Profile);
                    Vector3 sourceScale = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyVariants/Rare_Haste_Guard.prefab").transform.localScale;
                    Require(_boss.transform.localScale == bossScale && Vector3.Distance(bossScale,sourceScale*2.1f)<.001f,
                        "Size is relative to original prefab without compounding");
                    Require(EnemyVisualStyle.NameColor(EnemyRarity.Normal)==Color.white && EnemyVisualStyle.NameColor(EnemyRarity.Magic).b > .9f &&
                        EnemyVisualStyle.NameColor(EnemyRarity.Rare).r == 1f, "Normal white, magic blue, rare yellow");
                    Require(EnemyVisualStyle.SizeMultiplier(EnemyRarity.Rare)==1.4f, "Rare size differs from normal");
                    foreach (EnemyAI enemy in Components<EnemyAI>())
                        if (!enemy.GetComponent<SectorBoss>()) { enemy.enabled=false; enemy.agent.ResetPath(); enemy.agent.isStopped=true; }
                    _player.GetComponent<NavMeshAgent>().Warp(_boss.transform.position + Vector3.back*3);
                    _player.RestoreHealthToMax();
                    _healthBeforeBoss = _player.health;
                    _bossDeadline = EditorApplication.timeSinceStartup+8;
                    Next(51,.05); break;
                case 51:
                    if (EditorApplication.timeSinceStartup >= _bossDeadline) throw new Exception("Boss did not resolve telegraphed slam");
                    if (_boss.ResolvedAttacks == 0) { Next(51,.05); break; }
                    Require(_player.health < _healthBeforeBoss, "Boss slam damages player inside circle");
                    Require(!_player.IsDead, "Boss does not one-shot a full-health player");
                    Next(52,.05); break;
                case 52:
                    if (EditorApplication.timeSinceStartup >= _bossDeadline+8) throw new Exception("Boss did not begin aimed attack");
                    if (!_boss.IsTelegraphing) { Next(52,.05); break; }
                    Require(_boss.AttackRadius < 4, "Boss alternates slam and targeted blast");
                    _healthBeforeBoss = _player.health;
                    _player.GetComponent<NavMeshAgent>().Warp(_boss.AttackCenter + Vector3.right*4);
                    Next(53,1.3); break;
                case 53:
                    Require(Mathf.Approximately(_player.health,_healthBeforeBoss), "Leaving telegraph avoids damage");
                    _boss.GetComponent<Actor>().TakeDamage(_boss.MaximumHealth*.55f);
                    Require(_boss.IsEnraged, "Boss enters fury below half health");
                    _bossTested = true;
                    Next(5,.1); break;
            }
        }
        catch (Exception exception) { Debug.LogException(exception); Finish(1); }
    }
    private static void Finish(int code)
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= Log;
        SessionState.SetBool(Pending, false);
        if (SessionState.GetBool(Pending + ".had", false)) PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, SessionState.GetInt(Pending + ".value", 0));
        else PlayerPrefs.DeleteKey(WeaponLoadout.PreferenceKey);
        PlayerPrefs.Save(); EditorApplication.Exit(code);
    }
}
