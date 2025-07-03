using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraCharacter : MonoBehaviour
{
    [Header("Camera Follow Settings")]
    public Transform target; // Assign your player character here or leave null to auto-find
    [Tooltip("Higher = snappier camera. 8-12 is typical for smooth follow.")]
    public float smoothing = 8f;
    public Vector3 offset = new Vector3(0, 0, -10); // Default camera offset (important for 2D games)

    [Header("Camera Boundaries (Optional)")]
    public bool useBoundaries = false;
    public float minX, maxX, minY, maxY;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private void Start()
    {
        // Try to find player on start if target is not assigned
        if (target == null)
        {
            FindPlayer();
        }

        // Set initial camera position immediately (no smoothing on load)
        if (target != null)
        {
            SetCameraPositionImmediate();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            if (showDebugInfo)
                Debug.LogWarning($"[CAMERA] No target assigned at {GetCurrentDateTime()}");
            return;
        }

        FollowTargetSmooth();
    }

    private void FollowTargetSmooth()
    {
        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;

        // Frame-rate independent smoothing
        float lerpFactor = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, lerpFactor);

        // Clamp to boundaries if enabled
        if (useBoundaries)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }

        // Keep z as offset (important for 2D)
        smoothedPosition.z = offset.z;
        transform.position = smoothedPosition;

        if (showDebugInfo)
        {
            Debug.Log($"[CAMERA] Target: {target.position}, Desired: {desiredPosition}, Smoothed: {smoothedPosition}");
        }
    }

    private void SetCameraPositionImmediate()
    {
        if (target == null) return;

        Vector3 immediatePosition = target.position + offset;

        if (useBoundaries)
        {
            immediatePosition.x = Mathf.Clamp(immediatePosition.x, minX, maxX);
            immediatePosition.y = Mathf.Clamp(immediatePosition.y, minY, maxY);
        }

        immediatePosition.z = offset.z;
        transform.position = immediatePosition;

        if (showDebugInfo)
            Debug.Log($"[CAMERA] Camera positioned immediately at {immediatePosition} for target {target.name}");
    }

    private void FindPlayer()
    {
        // Try to find the Player GameObject (tag must be set to "Player")
        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
            target = playerObj.transform;
            if (showDebugInfo)
                Debug.Log($"[CAMERA] Player found: {playerObj.name} at {GetCurrentDateTime()}");
        }
        else
        {
            // Fallback: try to find by name
            playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
                if (showDebugInfo)
                    Debug.Log($"[CAMERA] Player found by name: {playerObj.name} at {GetCurrentDateTime()}");
            }
            else
            {
                Debug.LogWarning($"[CAMERA] Player not found! Make sure Player GameObject has 'Player' tag at {GetCurrentDateTime()}");
            }
        }
    }

    private void OnEnable()
    {
        // Subscribe to sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe from sceneLoaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // This will be called automatically after every scene load
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (showDebugInfo)
            Debug.Log($"[CAMERA] Scene loaded: {scene.name} at {GetCurrentDateTime()}");

        // Find the player in the new scene
        FindPlayer();

        // Position camera immediately on the player (no smoothing for scene transitions)
        if (target != null)
        {
            SetCameraPositionImmediate();
        }

        // OPTIONAL: Move camera to the newly loaded scene for scene organization
        if (scene.IsValid() && gameObject.scene != scene)
        {
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            if (showDebugInfo)
                Debug.Log($"[CAMERA] Camera moved to scene: {scene.name}");
        }
    }

    // Utility method for consistent datetime formatting
    private string GetCurrentDateTime()
    {
        return System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // Public method to manually center camera on target immediately
    public void CenterOnTarget()
    {
        if (target != null)
        {
            SetCameraPositionImmediate();
            if (showDebugInfo)
                Debug.Log($"[CAMERA] Manually centered on target at {GetCurrentDateTime()}");
        }
    }

    // Public method to set new target (for dynamic player assignment)
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            SetCameraPositionImmediate();
            if (showDebugInfo)
                Debug.Log($"[CAMERA] New target set: {target.name} at {GetCurrentDateTime()}");
        }
    }

    // Context menu for testing in the Unity Editor
    [ContextMenu("Center Camera on Player")]
    private void TestCenterCamera()
    {
        FindPlayer();
        CenterOnTarget();
    }

    [ContextMenu("Find Player")]
    private void TestFindPlayer()
    {
        FindPlayer();
    }

    // Gizmos for debugging camera boundaries
    private void OnDrawGizmosSelected()
    {
        if (!useBoundaries) return;

        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((minX + maxX) / 2, (minY + maxY) / 2, transform.position.z);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0);
        Gizmos.DrawWireCube(center, size);
    }
}