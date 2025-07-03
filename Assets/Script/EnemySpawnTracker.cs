using UnityEngine;

public class EnemyRespawnTracker : MonoBehaviour
{
    private EnemySpawner parentSpawner;
    private Vector2 originalSpawnPosition;
    private int spawnPointIndex = -1;
    private bool isFromSpawnPoint = false;
    private string spawnPointName = "";
    private SpawnPoint associatedSpawnPoint = null; // Reference to the actual SpawnPoint

    public void Initialize(EnemySpawner spawner, Vector2 spawnPos, int spawnIndex = -1, string pointName = "", SpawnPoint spawnPoint = null)
    {
        parentSpawner = spawner;
        originalSpawnPosition = spawnPos;
        spawnPointIndex = spawnIndex;
        spawnPointName = pointName;
        isFromSpawnPoint = spawnIndex >= 0;
        associatedSpawnPoint = spawnPoint; // Store reference to SpawnPoint for its parameters

        Debug.Log($"EnemyRespawnTracker initialized for {gameObject.name} at spawn point: {pointName}");
    }

    private void OnDestroy()
    {
        // Get respawn timing from the SpawnPoint if available
        float respawnTime = 5f; // Default fallback

        if (associatedSpawnPoint != null && associatedSpawnPoint.IsRespawningEnabled)
        {
            respawnTime = associatedSpawnPoint.GetRespawnTime();
        }
        else if (parentSpawner != null)
        {
            // Use spawner's default if no SpawnPoint available
            respawnTime = parentSpawner.GetDefaultRespawnTime();
        }

        // Notify the spawner that this enemy was destroyed and needs respawning
        if (parentSpawner != null && isFromSpawnPoint)
        {
            parentSpawner.RequestRespawn(originalSpawnPosition, spawnPointIndex, spawnPointName, respawnTime);
        }
    }

    // Public getters for the spawner to use
    public Vector2 GetOriginalSpawnPosition() => originalSpawnPosition;
    public int GetSpawnPointIndex() => spawnPointIndex;
    public string GetSpawnPointName() => spawnPointName;
    public bool IsFromSpawnPoint() => isFromSpawnPoint;
    public SpawnPoint GetAssociatedSpawnPoint() => associatedSpawnPoint;
}