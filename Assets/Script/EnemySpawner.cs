using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private bool spawnAllAtStart = true; // Main toggle
    [SerializeField] private int totalEnemiesToSpawn = 10; // How many enemies to spawn at start
    [SerializeField] private LayerMask obstacleLayerMask;

    [Header("Default Respawn Settings (Fallback)")]
    [SerializeField] private bool enableGlobalRespawning = true; // Global respawn toggle
    [SerializeField] private float defaultRespawnDelay = 5f; // Default time before respawning (if SpawnPoint doesn't specify)
    [SerializeField] private float defaultRespawnVariation = 2f; // Default random variation (if SpawnPoint doesn't specify)
    [SerializeField] private bool respectGlobalLimit = true; // Respect total enemies limit when respawning

    [Header("Spawn Method")]
    [SerializeField] private bool useSpawnPointComponents = true; // Use SpawnPoint components in scene
    [SerializeField] private bool useManualSpawnPoints = false; // Use manual transform array

    [Header("Manual Spawn Positions (if useManualSpawnPoints = true)")]
    [SerializeField] private Transform[] manualSpawnPoints; // Drag spawn point GameObjects here
    [SerializeField] private int enemiesPerManualSpawnPoint = 2; // How many enemies per manual spawn point

    [Header("Fallback Random Spawn")]
    [SerializeField] private float spawnRadius = 5f; // If no spawn points, spawn randomly around spawner
    [SerializeField] private float minDistanceFromPlayer = 3f;

    [Header("Enemy Stats")]
    [SerializeField] private bool randomizeStats = true;
    [SerializeField] private int minHealth = 30;
    [SerializeField] private int maxHealth = 70;
    [SerializeField] private int minAttackDamage = 5;
    [SerializeField] private int maxAttackDamage = 15;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackDuration = 0.5f;

    [Header("Enemy Behavior")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float stopDistance = 1.0f;
    [SerializeField] private float knockbackForce = 2f;
    [SerializeField] private float patrolRadius = 3f;

    [Header("Circle Behavior")]
    [SerializeField] private float circleChangeChance = 0.05f;
    [SerializeField] private float minCircleTime = 0.5f;
    [SerializeField] private bool enableRandomCircling = true;
    [SerializeField] private bool startClockwise = true;

    [Header("Retreat Behavior")]
    [SerializeField] private float retreatSpeedMultiplier = 1.5f;
    [SerializeField] private float retreatDistance = 3.5f;
    [SerializeField] private float retreatCooldown = 4f;
    [SerializeField] private float retreatChance = 0.3f;
    [SerializeField] private bool enableRetreat = true;

    [Header("Item Drops")]
    [SerializeField] private EnemyController.ItemDrop[] possibleDrops;
    [SerializeField] private float baseDropChance = 0.5f;

    private Transform playerTransform;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private List<SpawnPoint> registeredSpawnPoints = new List<SpawnPoint>();
    private List<Transform> registeredManualPoints = new List<Transform>();
    private Dictionary<int, int> spawnPointEnemyCounts = new Dictionary<int, int>(); // Track enemy count per spawn point
    private Queue<RespawnRequest> respawnQueue = new Queue<RespawnRequest>();

    // Simple struct for respawn requests
    [System.Serializable]
    private struct RespawnRequest
    {
        public Vector2 spawnPosition;
        public int spawnPointIndex;
        public string spawnPointName;
        public float respawnTime;

        public RespawnRequest(Vector2 pos, int index, string name, float time)
        {
            spawnPosition = pos;
            spawnPointIndex = index;
            spawnPointName = name;
            respawnTime = time;
        }
    }

    private void Start()
    {
        playerTransform = FindFirstObjectByType<PlayerMovement>()?.transform;

        if (obstacleLayerMask == 0)
        {
            obstacleLayerMask = LayerMask.GetMask("PhysicObj");
        }

        // Initialize spawn points
        InitializeSpawnPoints();

        // Simple spawn all at start
        if (spawnAllAtStart)
        {
            SpawnAllEnemies();
        }

        // Start respawn processing
        if (enableGlobalRespawning)
        {
            StartCoroutine(ProcessRespawnQueue());
        }
    }

    private void InitializeSpawnPoints()
    {
        registeredSpawnPoints.Clear();
        registeredManualPoints.Clear();
        spawnPointEnemyCounts.Clear();

        // Method 1: Use SpawnPoint components
        if (useSpawnPointComponents)
        {
            SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                SpawnPoint sp = spawnPoints[i];
                if (sp != null && sp.IsEnabled)
                {
                    registeredSpawnPoints.Add(sp);
                    spawnPointEnemyCounts[i] = 0;
                }
            }

            Debug.Log($"Initialized {registeredSpawnPoints.Count} SpawnPoint components");
        }

        // Method 2: Use manual spawn points
        if (!useSpawnPointComponents && useManualSpawnPoints && manualSpawnPoints != null)
        {
            for (int i = 0; i < manualSpawnPoints.Length; i++)
            {
                Transform manualPoint = manualSpawnPoints[i];
                if (manualPoint != null)
                {
                    registeredManualPoints.Add(manualPoint);
                    spawnPointEnemyCounts[i] = 0;
                }
            }

            Debug.Log($"Initialized {registeredManualPoints.Count} manual spawn points");
        }
    }

    private void Update()
    {
        // Clean up destroyed enemies from the list
        spawnedEnemies.RemoveAll(enemy => enemy == null);
    }

    private void SpawnAllEnemies()
    {
        Debug.Log("=== Spawning All Enemies at Start ===");

        int totalSpawned = 0;

        // Spawn from SpawnPoint components
        if (useSpawnPointComponents && registeredSpawnPoints.Count > 0)
        {
            for (int i = 0; i < registeredSpawnPoints.Count; i++)
            {
                SpawnPoint sp = registeredSpawnPoints[i];
                if (sp == null || !sp.IsEnabled) continue;

                int enemiesToSpawn = sp.MaxEnemiesAtThisPoint;
                Debug.Log($"Spawning {enemiesToSpawn} enemies at {sp.gameObject.name} (Respawn: {sp.RespawnDelay}±{sp.RespawnVariation}s)");

                for (int j = 0; j < enemiesToSpawn; j++)
                {
                    if (totalSpawned >= totalEnemiesToSpawn)
                    {
                        Debug.Log($"Reached total enemy limit: {totalEnemiesToSpawn}");
                        break;
                    }

                    Vector2 spawnPos = sp.GetSpawnPosition();
                    if (SpawnEnemyAtPosition(spawnPos, i, sp.gameObject.name, sp))
                    {
                        totalSpawned++;
                        spawnPointEnemyCounts[i]++;
                    }
                }

                if (totalSpawned >= totalEnemiesToSpawn) break;
            }
        }
        // Spawn from manual points
        else if (useManualSpawnPoints && registeredManualPoints.Count > 0)
        {
            for (int i = 0; i < registeredManualPoints.Count; i++)
            {
                Transform manualPoint = registeredManualPoints[i];
                if (manualPoint == null) continue;

                Debug.Log($"Spawning {enemiesPerManualSpawnPoint} enemies at {manualPoint.name} (Using default respawn: {defaultRespawnDelay}±{defaultRespawnVariation}s)");

                for (int j = 0; j < enemiesPerManualSpawnPoint; j++)
                {
                    if (totalSpawned >= totalEnemiesToSpawn)
                    {
                        Debug.Log($"Reached total enemy limit: {totalEnemiesToSpawn}");
                        break;
                    }

                    Vector2 spawnPos = GetSpawnPosition(manualPoint.position);
                    if (SpawnEnemyAtPosition(spawnPos, i, manualPoint.name, null))
                    {
                        totalSpawned++;
                        spawnPointEnemyCounts[i]++;
                    }
                }

                if (totalSpawned >= totalEnemiesToSpawn) break;
            }
        }
        // Fallback to random spawning
        else
        {
            Debug.Log($"No spawn points found, spawning {totalEnemiesToSpawn} enemies randomly");

            for (int i = 0; i < totalEnemiesToSpawn; i++)
            {
                Vector2 spawnPos = GetRandomSpawnPosition();
                SpawnEnemyAtPosition(spawnPos, -1, "Random", null);
                totalSpawned++;
            }
        }

        Debug.Log($"=== Initial Spawn Complete: {totalSpawned} enemies spawned ===");
    }

    private bool SpawnEnemyAtPosition(Vector2 position, int spawnPointIndex, string spawnPointName, SpawnPoint spawnPoint)
    {
        if (!IsPositionValid(position))
        {
            // Try a few alternative positions
            for (int attempts = 0; attempts < 5; attempts++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * 1f;
                Vector2 newPosition = position + randomOffset;

                if (IsPositionValid(newPosition))
                {
                    position = newPosition;
                    break;
                }
            }

            if (!IsPositionValid(position))
            {
                Debug.LogWarning($"Could not find valid spawn position at {spawnPointName}");
                return false;
            }
        }

        // Create enemy
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        spawnedEnemies.Add(enemy);

        // Add respawn tracker if from a spawn point
        if (spawnPointIndex >= 0)
        {
            EnemyRespawnTracker respawnTracker = enemy.AddComponent<EnemyRespawnTracker>();
            respawnTracker.Initialize(this, position, spawnPointIndex, spawnPointName, spawnPoint);
        }

        // Configure enemy
        ConfigureEnemy(enemy, position);

        Debug.Log($"✓ Spawned enemy at {spawnPointName} (Index: {spawnPointIndex})");
        return true;
    }

    // Public method called by EnemyRespawnTracker when enemy dies
    public void RequestRespawn(Vector2 spawnPosition, int spawnPointIndex, string spawnPointName, float customRespawnTime)
    {
        if (!enableGlobalRespawning) return;

        // Check if this specific spawn point allows respawning
        if (useSpawnPointComponents && spawnPointIndex >= 0 && spawnPointIndex < registeredSpawnPoints.Count)
        {
            SpawnPoint sp = registeredSpawnPoints[spawnPointIndex];
            if (sp != null && !sp.IsRespawningEnabled)
            {
                Debug.Log($"Respawn disabled for spawn point: {spawnPointName}");
                return;
            }
        }

        // Decrease the count for this spawn point
        if (spawnPointEnemyCounts.ContainsKey(spawnPointIndex))
        {
            spawnPointEnemyCounts[spawnPointIndex]--;
            spawnPointEnemyCounts[spawnPointIndex] = Mathf.Max(0, spawnPointEnemyCounts[spawnPointIndex]);
        }

        // Use the custom respawn time from SpawnPoint
        float respawnTime = Time.time + customRespawnTime;

        // Add to respawn queue
        RespawnRequest request = new RespawnRequest(spawnPosition, spawnPointIndex, spawnPointName, respawnTime);
        respawnQueue.Enqueue(request);

        Debug.Log($"Respawn requested for {spawnPointName} (Index: {spawnPointIndex}) in {customRespawnTime:F1} seconds");
    }

    // Public method for EnemyRespawnTracker to get default respawn time
    public float GetDefaultRespawnTime()
    {
        float baseTime = defaultRespawnDelay;
        float variation = Random.Range(-defaultRespawnVariation, defaultRespawnVariation);
        return Mathf.Max(0.1f, baseTime + variation);
    }

    private IEnumerator ProcessRespawnQueue()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Check twice per second

            if (!enableGlobalRespawning || respawnQueue.Count == 0)
                continue;

            // Process all ready respawns
            int queueCount = respawnQueue.Count;
            for (int i = 0; i < queueCount; i++)
            {
                RespawnRequest request = respawnQueue.Dequeue();

                // Check if it's time to respawn
                if (Time.time >= request.respawnTime)
                {
                    // Check if we can respawn (space available and under global limit)
                    if (CanRespawnAtPoint(request.spawnPointIndex) &&
                        (!respectGlobalLimit || spawnedEnemies.Count < totalEnemiesToSpawn))
                    {
                        // Get the SpawnPoint reference for respawning
                        SpawnPoint spawnPoint = null;
                        if (useSpawnPointComponents && request.spawnPointIndex >= 0 && request.spawnPointIndex < registeredSpawnPoints.Count)
                        {
                            spawnPoint = registeredSpawnPoints[request.spawnPointIndex];
                        }

                        if (SpawnEnemyAtPosition(request.spawnPosition, request.spawnPointIndex, request.spawnPointName, spawnPoint))
                        {
                            spawnPointEnemyCounts[request.spawnPointIndex]++;
                            Debug.Log($"✓ Respawned enemy at {request.spawnPointName}");
                        }
                    }
                    else
                    {
                        // Can't respawn yet, put back in queue for later
                        RespawnRequest delayedRequest = new RespawnRequest(
                            request.spawnPosition,
                            request.spawnPointIndex,
                            request.spawnPointName,
                            Time.time + 1f // Try again in 1 second
                        );
                        respawnQueue.Enqueue(delayedRequest);
                    }
                }
                else
                {
                    // Not ready yet, put back in queue
                    respawnQueue.Enqueue(request);
                }
            }
        }
    }

    private bool CanRespawnAtPoint(int spawnPointIndex)
    {
        if (spawnPointIndex < 0) return true; // Random spawn

        // Check SpawnPoint components
        if (useSpawnPointComponents && spawnPointIndex < registeredSpawnPoints.Count)
        {
            SpawnPoint sp = registeredSpawnPoints[spawnPointIndex];
            if (sp == null || !sp.IsEnabled || !sp.IsRespawningEnabled) return false;

            int currentCount = spawnPointEnemyCounts.ContainsKey(spawnPointIndex) ? spawnPointEnemyCounts[spawnPointIndex] : 0;
            return currentCount < sp.MaxEnemiesAtThisPoint;
        }

        // Check manual spawn points
        if (useManualSpawnPoints && spawnPointIndex < registeredManualPoints.Count)
        {
            Transform manualPoint = registeredManualPoints[spawnPointIndex];
            if (manualPoint == null) return false;

            int currentCount = spawnPointEnemyCounts.ContainsKey(spawnPointIndex) ? spawnPointEnemyCounts[spawnPointIndex] : 0;
            return currentCount < enemiesPerManualSpawnPoint;
        }

        return false;
    }

    private void ConfigureEnemy(GameObject enemy, Vector2 spawnPosition)
    {
        EnemyController enemyController = enemy.GetComponent<EnemyController>();
        if (enemyController == null)
            return;

        // Initialize with movement and combat parameters
        enemyController.Initialize(
            moveSpeed, attackCooldown, attackDuration, attackRange,
            detectionRange, stopDistance, knockbackForce, patrolRadius
        );

        // Set health and damage stats
        if (randomizeStats)
        {
            enemyController.SetStats(
                Random.Range(minHealth, maxHealth + 1),
                Random.Range(minAttackDamage, maxAttackDamage + 1)
            );
        }
        else
        {
            enemyController.SetStats(maxHealth, maxAttackDamage);
        }

        // Set item drops
        enemyController.SetItemDrops(possibleDrops, baseDropChance);

        // Configure behaviors
        enemyController.SetCircleBehavior(circleChangeChance, minCircleTime, enableRandomCircling, startClockwise);
        enemyController.SetRetreatBehavior(retreatSpeedMultiplier, retreatDistance, retreatCooldown, retreatChance, enableRetreat);

        // Set up patrol around spawn point
        Vector2[] patrolPoints = GeneratePatrolPoints(spawnPosition);
        enemyController.StartPatrolling(patrolPoints, Random.Range(1f, 3f));
    }

    private Vector2 GetSpawnPosition(Vector3 basePosition)
    {
        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        Vector2 finalPosition = (Vector2)basePosition + randomOffset;

        if (IsPositionValid(finalPosition))
        {
            return finalPosition;
        }

        for (int attempts = 0; attempts < 5; attempts++)
        {
            randomOffset = Random.insideUnitCircle * 1f;
            finalPosition = (Vector2)basePosition + randomOffset;

            if (IsPositionValid(finalPosition))
            {
                return finalPosition;
            }
        }

        return basePosition;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        Vector2 position;
        int attempts = 0;

        do
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float distance = Random.Range(2f, spawnRadius);
            position = (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            attempts++;
        }
        while (!IsPositionValid(position) && attempts < 30);

        return position;
    }

    private bool IsPositionValid(Vector2 position)
    {
        if (playerTransform != null)
        {
            float distanceToPlayer = Vector2.Distance(position, playerTransform.position);
            if (distanceToPlayer < minDistanceFromPlayer)
            {
                return false;
            }
        }

        if (Physics2D.OverlapCircle(position, 0.5f, obstacleLayerMask))
        {
            return false;
        }

        return true;
    }

    private Vector2[] GeneratePatrolPoints(Vector2 center)
    {
        int pointCount = Random.Range(3, 6);
        Vector2[] points = new Vector2[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            float angle = (i / (float)pointCount) * 360f * Mathf.Deg2Rad;
            float distance = Random.Range(1f, patrolRadius);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
            points[i] = center + offset;
        }

        return points;
    }

    // Public methods for external control
    public int GetActiveEnemyCount()
    {
        spawnedEnemies.RemoveAll(enemy => enemy == null);
        return spawnedEnemies.Count;
    }

    public void SetGlobalRespawningEnabled(bool enabled)
    {
        enableGlobalRespawning = enabled;
        Debug.Log($"Global enemy respawning {(enabled ? "enabled" : "disabled")}");
    }

    public void SetDefaultRespawnDelay(float delay)
    {
        defaultRespawnDelay = Mathf.Max(0.1f, delay);
        Debug.Log($"Default respawn delay set to {defaultRespawnDelay} seconds");
    }

    public int GetPendingRespawnCount()
    {
        return respawnQueue.Count;
    }

    // Manual spawn button for testing
    [ContextMenu("Spawn All Enemies Now")]
    public void SpawnAllEnemiesNow()
    {
        SpawnAllEnemies();
    }

    [ContextMenu("Clear All Enemies")]
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                DestroyImmediate(enemy);
            }
        }
        spawnedEnemies.Clear();

        // Reset spawn point counts
        var keys = new List<int>(spawnPointEnemyCounts.Keys);
        foreach (int key in keys)
        {
            spawnPointEnemyCounts[key] = 0;
        }

        // Clear respawn queue
        respawnQueue.Clear();

        Debug.Log("All enemies cleared");
    }

    [ContextMenu("Toggle Global Respawning")]
    public void ToggleGlobalRespawning()
    {
        SetGlobalRespawningEnabled(!enableGlobalRespawning);
    }

    [ContextMenu("Debug Spawn Info")]
    public void DebugSpawnInfo()
    {
        Debug.Log("=== SPAWN INFO ===");
        Debug.Log($"Active Enemies: {GetActiveEnemyCount()}");
        Debug.Log($"Pending Respawns: {GetPendingRespawnCount()}");
        Debug.Log($"Global Respawning: {enableGlobalRespawning}");
        Debug.Log($"Spawn Point Details:");

        for (int i = 0; i < registeredSpawnPoints.Count; i++)
        {
            SpawnPoint sp = registeredSpawnPoints[i];
            int currentCount = spawnPointEnemyCounts.ContainsKey(i) ? spawnPointEnemyCounts[i] : 0;
            Debug.Log($"  {sp.gameObject.name}: {currentCount}/{sp.MaxEnemiesAtThisPoint} enemies, Respawn: {sp.RespawnDelay}±{sp.RespawnVariation}s, Enabled: {sp.IsRespawningEnabled}");
        }

        Debug.Log("=== END INFO ===");
    }

    private void OnDrawGizmosSelected()
    {
        // Draw SpawnPoint components
        if (useSpawnPointComponents)
        {
            SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();
            if (spawnPoints.Length > 0)
            {
                Gizmos.color = Color.green;
                foreach (SpawnPoint sp in spawnPoints)
                {
                    if (sp != null)
                    {
                        Gizmos.DrawLine(transform.position, sp.transform.position);
                    }
                }
            }
        }

        // Draw manual spawn points
        if (useManualSpawnPoints && manualSpawnPoints != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < manualSpawnPoints.Length; i++)
            {
                if (manualSpawnPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(manualSpawnPoints[i].position, 1f);
                    Gizmos.DrawSphere(manualSpawnPoints[i].position, 0.3f);
                }
            }
        }

        // Draw random spawn area
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);

        // Draw player distance
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, minDistanceFromPlayer);
        }
    }
}