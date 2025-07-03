using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Point Settings")]
    [SerializeField] private int maxEnemiesAtThisPoint = 3; // How many enemies to spawn here
    [SerializeField] private bool enableSpawning = true; // Can disable specific spawn points

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 5f; // Time before respawning enemy at this point
    [SerializeField] private float respawnVariation = 2f; // Random variation (±seconds) for this point
    [SerializeField] private bool enableRespawning = true; // Enable respawning for this specific point

    [Header("Visual Settings")]
    [SerializeField] private Color gizmoColor = Color.cyan;

    // Public properties for the spawner to read
    public int MaxEnemiesAtThisPoint => maxEnemiesAtThisPoint;
    public bool IsEnabled => enableSpawning;
    public float RespawnDelay => respawnDelay;
    public float RespawnVariation => respawnVariation;
    public bool IsRespawningEnabled => enableRespawning;

    // Simple method to get spawn position with slight randomization
    public Vector2 GetSpawnPosition()
    {
        Vector2 basePosition = transform.position;
        Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
        return basePosition + randomOffset;
    }

    // Get the calculated respawn time for this spawn point
    public float GetRespawnTime()
    {
        float baseTime = respawnDelay;
        float variation = Random.Range(-respawnVariation, respawnVariation);
        return Mathf.Max(0.1f, baseTime + variation); // Minimum 0.1 seconds
    }

    // Methods to change settings at runtime
    public void SetMaxEnemies(int newMax)
    {
        maxEnemiesAtThisPoint = Mathf.Max(0, newMax);
    }

    public void SetEnabled(bool enabled)
    {
        enableSpawning = enabled;
    }

    public void SetRespawnDelay(float delay)
    {
        respawnDelay = Mathf.Max(0.1f, delay);
    }

    public void SetRespawnVariation(float variation)
    {
        respawnVariation = Mathf.Max(0f, variation);
    }

    public void SetRespawningEnabled(bool enabled)
    {
        enableRespawning = enabled;
    }

    private void OnDrawGizmos()
    {
        // Always draw the spawn point
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 1f);

        if (enableSpawning)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }
        else
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }

        // Show respawn indicator
        if (enableSpawning && enableRespawning)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 1.2f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 1f);

        // Show respawn area
        if (enableRespawning)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1.5f);
        }
    }
}