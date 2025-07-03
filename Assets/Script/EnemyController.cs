using Assets.Script;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Enemy Stats - Now set by EnemySpawner
    private int maxHealth;
    private int currentHealth;
    private int attackDamage;

    // Movement parameters - Now set by EnemySpawner
    private float moveSpeed;
    private float attackCooldown;
    private float attackDuration;
    private float attackRange;
    private float detectionRange;
    private float stopDistance;
    private float knockbackForce;
    private float patrolRadius;

    // Combat behavior
    [Header("Combat Behavior")]
    [SerializeField] private float optimalCombatDistance = 1.8f; // Preferred fighting distance
    [SerializeField] private float minCombatDistance = 1.0f; // Minimum distance before retreat

    // Circle Movement Parameters
    [Header("Circle Movement")]
    [SerializeField] private float circleDirectionChangeChance = 0.05f; // 5% chance per frame to change direction
    [SerializeField] private float minCircleTime = 0.5f; // Minimum time before allowing direction change
    [SerializeField] private bool enableRandomCircling = true; // Enable/disable random direction changes
    [SerializeField] private bool startClockwise = true; // Initial circle direction

    // Retreat Behavior Parameters
    [Header("Retreat Behavior")]
    [SerializeField] private float retreatSpeedMultiplier = 1.5f; // Speed boost when retreating
    [SerializeField] private float retreatDistance = 3.5f; // How far to retreat from player
    [SerializeField] private float retreatCooldown = 4f; // Time between retreat attempts
    [SerializeField] private float retreatChance = 0.3f; // 30% chance to retreat instead of circle
    [SerializeField] private bool enableRetreat = true; // Enable/disable retreat behavior

    // Item Drop Settings
    [Header("Item Drops")]
    [SerializeField] private ItemDrop[] possibleDrops;
    [SerializeField] private float baseDropChance = 0.5f;

    // Animation parameters
    private bool isHurt = false;

    // Visual Effects
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float hitEffectDuration = 0.3f;

    // References
    public Rigidbody2D rb;
    public Animator animator;

    // Movement
    private Vector2 movement;
    private bool isMoving = false;

    // Circle state tracking
    private bool isCirclingClockwise = true;
    private float lastDirectionChangeTime = 0f;

    // Retreat state tracking
    private bool isRetreating = false;
    private float lastRetreatTime = 0f;
    private Vector2 retreatStartPosition;

    // Attack
    private bool isAttacking = false;
    private float attackTimer = 0f;

    // State
    private bool isKnockedBack = false;
    public bool isDead = false;
    private Transform target;

    // Patrol-specific variables
    private bool isPatrolling = false;
    private Vector2[] patrolPoints;
    private float patrolWaitTime;
    private Coroutine patrolCoroutine;

    // Player death state management
    private bool playerIsDead = false;

    // State management including Retreat
    private enum EnemyState
    {
        Idle,
        Patrol,
        Approach,    // Moving to attack range
        Attack,      // Attacking player
        Circle,      // Circling player at optimal distance
        Retreat      // Retreating from player then returning
    }
    private EnemyState currentState = EnemyState.Idle;

    // Item Drop System
    [System.Serializable]
    public class ItemDrop
    {
        public Item item;
        [Range(0f, 1f)]
        public float dropChance;
        public int minQuantity = 1;
        public int maxQuantity = 1;
    }

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        target = FindFirstObjectByType<PlayerMovement>()?.transform;
        playerIsDead = PlayerMovement.IsPlayerDead;

        // Initialize circle direction
        isCirclingClockwise = startClockwise;

        // Ignore collisions between enemies (just like Player-Enemy)
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Enemy"), LayerMask.NameToLayer("Enemy"), true);
    }

    public void Initialize(float moveSpeed, float attackCooldown, float attackDuration,
                          float attackRange, float detectionRange, float stopDistance,
                          float knockbackForce, float patrolRadius)
    {
        this.moveSpeed = moveSpeed;
        this.attackCooldown = attackCooldown;
        this.attackDuration = attackDuration;
        this.attackRange = attackRange;
        this.detectionRange = detectionRange;
        this.stopDistance = stopDistance;
        this.knockbackForce = knockbackForce;
        this.patrolRadius = patrolRadius;
    }

    public void SetStats(int health, int damage)
    {
        this.maxHealth = health;
        this.currentHealth = health;
        this.attackDamage = damage;
    }

    public void SetItemDrops(ItemDrop[] drops, float dropChance)
    {
        this.possibleDrops = drops;
        this.baseDropChance = dropChance;
    }

    public void SetCircleBehavior(float changeChance, float minTime, bool enableRandom, bool startClockwise)
    {
        this.circleDirectionChangeChance = changeChance;
        this.minCircleTime = minTime;
        this.enableRandomCircling = enableRandom;
        this.isCirclingClockwise = startClockwise;
    }

    public void SetRetreatBehavior(float speedMultiplier, float distance, float cooldown, float chance, bool enabled)
    {
        this.retreatSpeedMultiplier = speedMultiplier;
        this.retreatDistance = distance;
        this.retreatCooldown = cooldown;
        this.retreatChance = chance;
        this.enableRetreat = enabled;
    }

    void Update()
    {
        if (isDead)
            return;

        if (!playerIsDead && PlayerMovement.IsPlayerDead)
        {
            OnPlayerDeath();
        }

        // Handle attack timer
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        // AI decision making
        if (!isKnockedBack && !isAttacking && !isHurt)
        {
            UpdateState();
            ExecuteState();
        }

        UpdateAnimator();
    }

    private void UpdateState()
    {
        if (playerIsDead)
        {
            currentState = isPatrolling ? EnemyState.Patrol : EnemyState.Idle;
            isRetreating = false; // Reset retreat state
            return;
        }

        if (target == null)
            return;

        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        // Simple behavior with retreat option
        if (distanceToTarget <= detectionRange)
        {
            if (distanceToTarget > attackRange)
            {
                // Too far - approach player (unless retreating)
                if (isRetreating)
                {
                    currentState = EnemyState.Retreat;
                }
                else
                {
                    currentState = EnemyState.Approach;
                }
            }
            else if (distanceToTarget < minCombatDistance)
            {
                // Too close - circle around player or retreat
                if (ShouldRetreat())
                {
                    currentState = EnemyState.Retreat;
                }
                else
                {
                    currentState = EnemyState.Circle;
                }
            }
            else if (attackTimer <= 0)
            {
                // In range and ready to attack
                currentState = EnemyState.Attack;
            }
            else
            {
                // In range but on cooldown - circle around or retreat
                if (ShouldRetreat())
                {
                    currentState = EnemyState.Retreat;
                }
                else
                {
                    currentState = EnemyState.Circle;
                }
            }
        }
        else if (isPatrolling)
        {
            currentState = EnemyState.Patrol;
            isRetreating = false; // Reset retreat when out of detection range
        }
        else
        {
            currentState = EnemyState.Idle;
            isRetreating = false; // Reset retreat when idle
        }
    }

    private bool ShouldRetreat()
    {
        if (!enableRetreat || isRetreating)
            return false;

        // Check cooldown
        if (Time.time - lastRetreatTime < retreatCooldown)
            return false;

        // Random chance to retreat
        if (Random.value < retreatChance)
        {
            StartRetreat();
            return true;
        }

        return false;
    }

    private void StartRetreat()
    {
        isRetreating = true;
        lastRetreatTime = Time.time;
        retreatStartPosition = transform.position;
        Debug.Log($"{gameObject.name} starting retreat maneuver!");
    }

    private void ExecuteState()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                SetMovementDirection(Vector2.zero);
                break;

            case EnemyState.Patrol:
                // Handled by coroutine
                break;

            case EnemyState.Approach:
                if (target != null && !playerIsDead)
                {
                    Vector2 direction = (target.position - transform.position).normalized;
                    SetMovementDirection(direction);
                }
                break;

            case EnemyState.Circle:
                if (target != null && !playerIsDead)
                {
                    Vector2 toPlayer = (target.position - transform.position).normalized;
                    Vector2 perpendicular = new Vector2(-toPlayer.y, toPlayer.x);

                    // Apply clockwise/counterclockwise direction
                    if (!isCirclingClockwise)
                        perpendicular = -perpendicular;

                    // Random direction change with configurable parameters
                    if (enableRandomCircling &&
                        Time.time - lastDirectionChangeTime > minCircleTime &&
                        Random.value < circleDirectionChangeChance)
                    {
                        isCirclingClockwise = !isCirclingClockwise;
                        lastDirectionChangeTime = Time.time;
                    }

                    SetMovementDirection(perpendicular);
                }
                break;

            case EnemyState.Retreat:
                if (target != null && !playerIsDead)
                {
                    float distanceToPlayer = Vector2.Distance(transform.position, target.position);

                    // Check if we've retreated far enough
                    if (distanceToPlayer >= retreatDistance)
                    {
                        // Finished retreating - return to combat
                        isRetreating = false;
                        Debug.Log($"{gameObject.name} finished retreat, returning to combat!");
                    }
                    else
                    {
                        // Continue retreating away from player
                        Vector2 retreatDirection = (transform.position - target.position).normalized;

                        // Add slight randomness to avoid predictable straight-line retreat
                        float randomAngle = Random.Range(-15f, 15f) * Mathf.Deg2Rad;
                        Vector2 randomizedDirection = new Vector2(
                            retreatDirection.x * Mathf.Cos(randomAngle) - retreatDirection.y * Mathf.Sin(randomAngle),
                            retreatDirection.x * Mathf.Sin(randomAngle) + retreatDirection.y * Mathf.Cos(randomAngle)
                        );

                        SetMovementDirection(randomizedDirection);
                    }
                }
                break;

            case EnemyState.Attack:
                if (!isAttacking && attackTimer <= 0 && !playerIsDead)
                {
                    StartCoroutine(PerformAttack());
                }
                break;
        }
    }

    void FixedUpdate()
    {
        if (isDead)
            return;

        if (isKnockedBack)
            return;

        if (isHurt)
        {
            rb.linearVelocity *= 0.95f;
        }
        else if (isAttacking)
        {
            rb.linearVelocity *= 0.7f; // Slow down during attack
        }
        else if (isMoving && movement != Vector2.zero)
        {
            float currentSpeed = moveSpeed;

            // Apply speed multiplier for retreat
            if (currentState == EnemyState.Retreat)
            {
                currentSpeed *= retreatSpeedMultiplier;
            }

            rb.MovePosition(rb.position + movement * currentSpeed * Time.fixedDeltaTime);
        }
    }

    private void UpdateAnimator()
    {
        if (movement != Vector2.zero)
        {
            animator.SetFloat("X", movement.x);
            animator.SetFloat("Y", movement.y);
        }

        animator.SetBool("IsWalk", isMoving);
        animator.SetBool("IsAttack", isAttacking);
    }

    public void SetMovementDirection(Vector2 direction)
    {
        movement = direction.normalized;
        isMoving = (movement.sqrMagnitude > 0.01f);
    }

    private IEnumerator PerformAttack()
    {
        if (playerIsDead)
        {
            isAttacking = false;
            yield break;
        }

        isMoving = false;
        isAttacking = true;
        animator.SetBool("IsAttack", true);

        // Wait for wind-up
        yield return new WaitForSeconds(attackDuration * 0.6f);

        if (playerIsDead)
        {
            isAttacking = false;
            animator.SetBool("IsAttack", false);
            yield break;
        }

        // Perform attack
        if (target != null && !playerIsDead)
        {
            PlayerMovement player = target.GetComponent<PlayerMovement>();
            if (player != null && Vector2.Distance(transform.position, target.position) < attackRange + 0.3f)
            {
                Vector2 hitDirection = (player.transform.position - transform.position).normalized;

                // Show hit effect
                if (hitEffectPrefab != null)
                {
                    Vector3 hitPosition = (transform.position + player.transform.position) / 2f;
                    GameObject effect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
                    Destroy(effect, hitEffectDuration);
                }

                if (player.CanTakeDamage())
                {
                    AttackPlayer(player);
                }
            }
        }

        // Wait for follow-through
        yield return new WaitForSeconds(attackDuration * 0.4f);

        // End attack
        isAttacking = false;
        animator.SetBool("IsAttack", false);

        // Set normal cooldown
        attackTimer = attackCooldown;
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isDead || isHurt)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die(hitDirection);
            return;
        }

        StartHurt(hitDirection);
    }

    private void StartHurt(Vector2 hitDirection)
    {
        isHurt = true;
        isKnockedBack = true;
        isRetreating = false; // Cancel retreat when hurt

        animator.SetTrigger("IsHurt");
        SetMovementDirection(Vector2.zero);
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(hitDirection * knockbackForce, ForceMode2D.Impulse);
        StartCoroutine(HurtRecoveryRoutine());
    }

    private IEnumerator HurtRecoveryRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        isKnockedBack = false;
        yield return new WaitForSeconds(0.3f);
        isHurt = false;
    }

    void Die(Vector2 hitDirection)
    {
        Debug.Log("Enemy died!");

        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }

        isDead = true;
        isRetreating = false;
        animator.SetBool("IsAttack", false);
        animator.SetBool("IsWalk", false);
        animator.SetTrigger("IsDead");

        isMoving = false;
        isAttacking = false;
        isHurt = false;
        isKnockedBack = false;
        movement = Vector2.zero;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(hitDirection * knockbackForce, ForceMode2D.Impulse);

        if (deathEffectPrefab != null)
        {
            GameObject effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        DropItems();
        StartCoroutine(DeathCleanup());
    }

    private void DropItems()
    {
        if (possibleDrops == null || possibleDrops.Length == 0)
            return;

        if (Random.value > baseDropChance)
            return;

        foreach (ItemDrop drop in possibleDrops)
        {
            if (drop.item == null) continue;

            // Use the item's individual drop chance
            if (Random.value <= drop.item.dropChance)
            {
                // For stackable items, use the item's quantity range, otherwise use the drop settings
                int quantity;
                if (drop.item.isStackable)
                {
                    // Use item's drop quantity range
                    quantity = Random.Range(drop.item.minDropQuantity, drop.item.maxDropQuantity + 1);
                }
                else
                {
                    // For non-stackable items, use original drop quantity settings
                    quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                }

                for (int i = 0; i < quantity; i++)
                {
                    CreateDroppedItem(drop.item);
                }
            }
        }
    }

    private void CreateDroppedItem(Item item)
    {
        GameObject droppedItem = new GameObject($"DroppedItem_{item.itemName}");

        // Calculate drop position using item's spread parameters
        Vector3 dropPosition;
        if (item.useRandomSpread && item.dropSpreadRadius > 0)
        {
            Vector2 randomOffset = Random.insideUnitCircle * item.dropSpreadRadius;
            dropPosition = transform.position + (Vector3)randomOffset;
        }
        else
        {
            // Use default small spread
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            dropPosition = transform.position + (Vector3)randomOffset;
        }

        droppedItem.transform.position = dropPosition;

        SpriteRenderer spriteRenderer = droppedItem.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = item.itemIcon;
        spriteRenderer.sortingLayerName = "Items";
        spriteRenderer.sortingOrder = 0;

        CircleCollider2D collider = droppedItem.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        Rigidbody2D rb = droppedItem.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0.5f;
        rb.linearDamping = 2f;

        // Add the DroppedItem component
        Assets.Script.DroppedItem droppedComponent = droppedItem.AddComponent<Assets.Script.DroppedItem>();

        // For individual drops, quantity is always 1 since we're creating separate items
        // If you want to create stacks, use CreateDroppedItemStack instead
        droppedComponent.Initialize(item, 1);

        Debug.Log($"Dropped 1x {item.itemName} at position {dropPosition}");
    }

    // Alternative method for creating stacked drops
    private void CreateDroppedItemStack(Item item, int quantity)
    {
        GameObject droppedItem = new GameObject($"DroppedItem_{item.itemName}_x{quantity}");

        // Calculate drop position using item's spread parameters
        Vector3 dropPosition;
        if (item.useRandomSpread && item.dropSpreadRadius > 0)
        {
            Vector2 randomOffset = Random.insideUnitCircle * item.dropSpreadRadius;
            dropPosition = transform.position + (Vector3)randomOffset;
        }
        else
        {
            // Use default small spread
            Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
            dropPosition = transform.position + (Vector3)randomOffset;
        }

        droppedItem.transform.position = dropPosition;

        SpriteRenderer spriteRenderer = droppedItem.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = item.itemIcon;
        spriteRenderer.sortingLayerName = "Items";
        spriteRenderer.sortingOrder = 0;

        CircleCollider2D collider = droppedItem.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        Rigidbody2D rb = droppedItem.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0.5f;
        rb.linearDamping = 2f;

        // Add the DroppedItem component with full quantity
        Assets.Script.DroppedItem droppedComponent = droppedItem.AddComponent<Assets.Script.DroppedItem>();
        droppedComponent.Initialize(item, quantity);

        Debug.Log($"Dropped {quantity}x {item.itemName} as a stack at position {dropPosition}");
    }

    // Updated drop method that creates stacks for stackable items
    private void DropItemsOptimized()
    {
        if (possibleDrops == null || possibleDrops.Length == 0)
            return;

        if (Random.value > baseDropChance)
            return;

        foreach (ItemDrop drop in possibleDrops)
        {
            if (drop.item == null) continue;

            // Use the item's individual drop chance
            if (Random.value <= drop.item.dropChance)
            {
                int quantity;
                if (drop.item.isStackable)
                {
                    // Use item's drop quantity range and create as a single stack
                    quantity = Random.Range(drop.item.minDropQuantity, drop.item.maxDropQuantity + 1);
                    CreateDroppedItemStack(drop.item, quantity);
                }
                else
                {
                    // For non-stackable items, create individual items
                    quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                    for (int i = 0; i < quantity; i++)
                    {
                        CreateDroppedItem(drop.item);
                    }
                }
            }
        }
    }

    private IEnumerator DeathCleanup()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;

        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

    public void AttackPlayer(PlayerMovement player)
    {
        if (player != null && !isDead && !playerIsDead && player.CanTakeDamage())
        {
            Vector2 knockbackDirection = (player.transform.position - transform.position).normalized;
            player.TakeDamage(attackDamage, knockbackDirection);
            rb.AddForce(-knockbackDirection * (knockbackForce * 0.2f), ForceMode2D.Impulse);
        }
    }

    public void OnPlayerDeath()
    {
        playerIsDead = true;
        isRetreating = false;

        if (isAttacking)
        {
            StopAllCoroutines();
            isAttacking = false;
            animator.SetBool("IsAttack", false);

            if (isPatrolling && patrolCoroutine == null)
            {
                patrolCoroutine = StartCoroutine(PatrolCoroutine());
            }
            StartCoroutine(HurtRecoveryRoutine());
        }

        attackTimer = attackCooldown;
        currentState = isPatrolling ? EnemyState.Patrol : EnemyState.Idle;

        Debug.Log($"Enemy {gameObject.name} notified of player death - switching to patrol/idle mode");
    }

    public void StartPatrolling(Vector2[] points, float waitTime)
    {
        if (points.Length < 2)
            return;

        this.patrolPoints = points;
        this.patrolWaitTime = waitTime;
        this.isPatrolling = true;

        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
        }

        patrolCoroutine = StartCoroutine(PatrolCoroutine());
    }

    public void StopPatrolling()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
        isPatrolling = false;
    }

    private IEnumerator PatrolCoroutine()
    {
        int currentPoint = 0;

        while (!isDead && isPatrolling)
        {
            if (currentState != EnemyState.Patrol && currentState != EnemyState.Idle)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            Vector2 targetPoint = patrolPoints[currentPoint];

            while (Vector2.Distance(transform.position, targetPoint) > 0.1f)
            {
                if (isDead || isKnockedBack || isAttacking || isHurt ||
                    currentState != EnemyState.Patrol)
                {
                    yield return null;
                    continue;
                }

                Vector2 direction = (targetPoint - (Vector2)transform.position).normalized;
                SetMovementDirection(direction);
                yield return null;
            }

            SetMovementDirection(Vector2.zero);
            yield return new WaitForSeconds(patrolWaitTime);
            currentPoint = (currentPoint + 1) % patrolPoints.Length;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        // Detection range (green)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Attack range (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Optimal combat distance (yellow)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, optimalCombatDistance);

        // Minimum combat distance (orange)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, minCombatDistance);

        // Retreat distance (purple/magenta)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);

        // Show retreat state
        if (Application.isPlaying && isRetreating)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, retreatStartPosition);
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                Vector2 start = patrolPoints[i];
                Vector2 end = patrolPoints[(i + 1) % patrolPoints.Length];
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(start, 0.2f);
            }
        }

        // Show current state
        if (Application.isPlaying)
        {
            Vector3 textPos = transform.position + Vector3.up * 2.5f;
            string stateText = $"State: {currentState}\nHP: {currentHealth}/{maxHealth}\nRetreating: {isRetreating}";

#if UNITY_EDITOR
            UnityEditor.Handles.Label(textPos, stateText);
#endif
        }
    }

    // Debug methods
    [ContextMenu("Force Retreat")]
    void ForceRetreat()
    {
        if (Application.isPlaying && enableRetreat)
        {
            StartRetreat();
        }
    }

    [ContextMenu("Toggle Circle Direction")]
    void ToggleCircleDirection()
    {
        if (Application.isPlaying)
        {
            isCirclingClockwise = !isCirclingClockwise;
            Debug.Log($"Circle direction changed to: {(isCirclingClockwise ? "Clockwise" : "Counterclockwise")}");
        }
    }
}