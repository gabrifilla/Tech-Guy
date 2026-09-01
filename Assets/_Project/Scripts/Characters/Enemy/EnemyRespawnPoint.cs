using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyRespawnPoint : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Actor initialEnemy;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private int spawnCount = 1;
    [SerializeField] private float spawnRadius = 0f;
    [SerializeField] private float respawnDelay = 5f;
    [SerializeField] private Transform spawnedParent;
    [SerializeField] private bool alignToNavMesh = true;
    [SerializeField] private float navMeshSearchRadius = 2f;

    [Header("Mob Defaults")]
    [SerializeField] private bool configureRankOnSpawn = true;
    [SerializeField] private bool addCombatReactionIfMissing = true;
    [SerializeField] private EnemyRank enemyRank = EnemyRank.Normal;
    [SerializeField] private bool allowBossAirJuggle;
    [SerializeField] private bool allowBossRagdoll;

    private readonly List<Actor> activeEnemies = new List<Actor>();

    private void Start()
    {
        if (initialEnemy)
        {
            RegisterEnemy(initialEnemy);
            ConfigureSpawnedEnemy(initialEnemy.gameObject);
        }

        if (spawnOnStart)
        {
            SpawnMissingEnemies();
        }
    }

    public void SpawnMissingEnemies()
    {
        int enemiesToSpawn = Mathf.Max(1, spawnCount) - activeEnemies.Count;
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    public Actor SpawnEnemy()
    {
        if (!enemyPrefab)
        {
            Debug.LogWarning($"{nameof(EnemyRespawnPoint)} requires an enemy prefab to spawn.", this);
            return null;
        }

        Vector3 position = ResolveSpawnPosition();
        Quaternion rotation = transform.rotation;
        Transform parent = spawnedParent ? spawnedParent : null;
        GameObject enemyInstance = Instantiate(enemyPrefab, position, rotation, parent);

        if (enemyInstance.TryGetComponent(out NavMeshAgent agent) && agent.enabled)
        {
            agent.Warp(position);
        }

        Actor actor = enemyInstance.GetComponent<Actor>();
        if (!actor)
        {
            actor = enemyInstance.GetComponentInChildren<Actor>();
        }

        if (!actor)
        {
            Debug.LogWarning($"{nameof(EnemyRespawnPoint)} spawned an enemy without an Actor component.", enemyInstance);
            return null;
        }

        RegisterEnemy(actor);
        ConfigureSpawnedEnemy(enemyInstance);
        return actor;
    }

    private void RegisterEnemy(Actor enemy)
    {
        if (!enemy || activeEnemies.Contains(enemy)) return;

        activeEnemies.Add(enemy);
        enemy.Died += OnEnemyDied;
    }

    private void ConfigureSpawnedEnemy(GameObject enemyObject)
    {
        if (!configureRankOnSpawn || !enemyObject) return;

        CombatReactionController reactionController = enemyObject.GetComponentInChildren<CombatReactionController>();
        if (!reactionController && addCombatReactionIfMissing)
        {
            reactionController = enemyObject.AddComponent<CombatReactionController>();
        }

        if (reactionController)
        {
            reactionController.ConfigureRank(enemyRank, allowBossAirJuggle, allowBossRagdoll);
        }
    }

    private void OnEnemyDied(Actor enemy)
    {
        enemy.Died -= OnEnemyDied;
        activeEnemies.Remove(enemy);

        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, respawnDelay));
        SpawnEnemy();
    }

    private Vector3 ResolveSpawnPosition()
    {
        Vector3 position = transform.position + Random.insideUnitSphere * Mathf.Max(0f, spawnRadius);
        position.y = transform.position.y;
        if (!alignToNavMesh) return position;

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, Mathf.Max(0.1f, navMeshSearchRadius), NavMesh.AllAreas))
        {
            return hit.position;
        }

        return position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }

    private void OnValidate()
    {
        spawnCount = Mathf.Max(1, spawnCount);
        spawnRadius = Mathf.Max(0f, spawnRadius);
        respawnDelay = Mathf.Max(0f, respawnDelay);
        navMeshSearchRadius = Mathf.Max(0.1f, navMeshSearchRadius);
    }
}
