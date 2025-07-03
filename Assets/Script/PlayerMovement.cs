using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    // Singleton instance
    public static PlayerMovement Instance { get; private set; }

    // Movement
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    public Animator animator;

    // Player Stats
    public int maxHealth = 100;
    public int currentHealth;
    public int attackDamage = 20;
    public Slider healthBarSlider;

    // Attack
    public float attackDuration = 0.3f;
    public float attackCooldown = 0.2f;
    private bool canAttacking = true;
    private bool isAttacking = false;
    public float attackRange = 0.5f;
    public Transform attackPoint;
    public LayerMask enemyLayers;

    // Combat Effects
    public GameObject hitEffectPrefab;
    public float hitEffectDuration = 0.3f;

    // Knockback & Hurt State
    public float knockbackForce = 3f;
    public float knockbackDuration = 0.2f;
    private bool isKnockedBack = false;
    private bool isHurt = false;
    public float hurtAnimationDuration = 0.5f;
    public float invincibilityDuration = 0.8f;
    private bool isInvincible = false;

    // Player State Management
    public static bool IsPlayerDead { get; private set; } = false;
    private bool isDying = false;

    // Inventory Integration
    private bool isInventoryOpen = false;

    // Menu Integration
    private MenuManager menuManager;

    [Header("Input Control")]
    public bool enableInputDebug = false;
    private bool hasHandledESCThisFrame = false;
    private bool hasHandledTabThisFrame = false;

    Vector2 movement;

    private void Awake()
    {
        Debug.Log("[PLAYER] Awake called. Current instance: " + Instance);
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PLAYER] Duplicate detected, destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[PLAYER] Instance set in Awake: " + Instance);
    }

    private void Start()
    {
        currentHealth = maxHealth;
        IsPlayerDead = false;
        isDying = false;

        // Store initial position as respawn position
        //respawnPosition = transform.position;

        // Find MenuManager
        FindMenuManager();

        // If healthBarSlider is set in the inspector, initialize it
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"));

        Debug.Log($"PlayerMovement initialized by at {GetCurrentDateTime()}");
    }

    private void FindMenuManager()
    {
        menuManager = FindObjectsByType<MenuManager>(FindObjectsSortMode.None).FirstOrDefault();
        if (menuManager != null)
        {
            Debug.Log($"✓ MenuManager found: {menuManager.gameObject.name} by at {GetCurrentDateTime()}");
        }
        else
        {
            Debug.LogWarning($"⚠️ MenuManager not found by at {GetCurrentDateTime()}");
        }
    }

    void Update()
    {
        // Reset frame flags
        hasHandledESCThisFrame = false;
        hasHandledTabThisFrame = false;

        if (enableInputDebug)
        {
            DebugInputSystem();
        }

        // Don't process input if dead or dying
        if (IsPlayerDead || isDying)
        {
            return;
        }

        // PRIORITY 1: ESC for Pause Menu
        if (Input.GetKeyDown(KeyCode.Escape) && !hasHandledESCThisFrame)
        {
            hasHandledESCThisFrame = true;

            // If inventory is ACTUALLY open (not just game paused), close inventory first
            if (isInventoryOpen && InventoryManager.Instance != null && InventoryManager.Instance.IsInventoryOpen())
            {
                Debug.Log($"🎒 ESC pressed while inventory open - closing inventory first by at {GetCurrentDateTime()}");
                InventoryManager.Instance.CloseInventory();
                return; // Don't process pause menu
            }

            // Otherwise handle pause menu
            Debug.Log($"🔑 ESC KEY for pause menu by at {GetCurrentDateTime()}");
            HandleEscapeKey();
            return;
        }

        // PRIORITY 2: Tab for Inventory 
        if (Input.GetKeyDown(KeyCode.Tab) && !hasHandledTabThisFrame)
        {
            hasHandledTabThisFrame = true;

            // Allow Tab to work even if game is paused by inventory itself
            // Only block if game is paused by something OTHER than inventory
            bool isGamePausedByOtherThanInventory = (Time.timeScale == 0f) && !isInventoryOpen;
            
            if (isGamePausedByOtherThanInventory)
            {
                Debug.Log($"⏸️ TAB ignored - game is paused by pause menu by at {GetCurrentDateTime()}");
                return;
            }

            Debug.Log($"🔑 TAB KEY for inventory by at {GetCurrentDateTime()}");
            HandleTabKey();
            return;
        }

        // Don't process other input if game is paused
        if (Time.timeScale == 0f)
        {
            return;
        }

        // Don't process movement/attack input if inventory is open
        if (isInventoryOpen)
        {
            return;
        }

        // Handle attack input
        if (Input.GetMouseButtonDown(0) && canAttacking && !isHurt && !isKnockedBack)
        {
            StartAttack();
        }

        // Movement input
        if (!isKnockedBack)
        {
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.y = Input.GetAxisRaw("Vertical");

            if (movement != Vector2.zero)
            {
                animator.SetFloat("X", movement.x);
                animator.SetFloat("Y", movement.y);
                animator.SetBool("IsWalking", true);
            }
            else
            {
                animator.SetBool("IsWalking", false);
            }
        }
    }

    private void DebugInputSystem()
    {
        if (Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    Debug.Log($"🔑 KEY PRESSED: {keyCode} by at {GetCurrentDateTime()}");
                    break;
                }
            }
        }
    }

    private void HandleEscapeKey()
    {
        Debug.Log($"🔧 Processing ESC key for pause menu by at {GetCurrentDateTime()}");

        if (menuManager == null)
        {
            FindMenuManager();
        }

        if (menuManager != null)
        {
            if (!menuManager.IsInMainMenu())
            {
                Debug.Log($"✅ Toggling pause menu by at {GetCurrentDateTime()}");
                menuManager.TogglePauseMenuFromPlayer();
            }
            else
            {
                Debug.Log($"ℹ️ In MainMenu - ESC ignored by at {GetCurrentDateTime()}");
            }
        }
        else
        {
            Debug.LogError($"❌ MenuManager not found! by at {GetCurrentDateTime()}");
        }
    }

    private void HandleTabKey()
    {
        Debug.Log($"🔧 Processing TAB key for inventory by at {GetCurrentDateTime()}");

        if (InventoryManager.Instance != null)
        {
            Debug.Log($"✅ Toggling inventory by at {GetCurrentDateTime()}");
            InventoryManager.Instance.ToggleInventory();
        }
        else
        {
            Debug.LogError($"❌ InventoryManager.Instance not found! by at {GetCurrentDateTime()}");
        }
    }

    private void FixedUpdate()
    {
        if (IsPlayerDead || isDying || isInventoryOpen || Time.timeScale == 0f)
            return;

        if (!isKnockedBack)
        {
            rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
        }
    }

    // IMPORTANT: Method called by InventoryManager to notify state
    public void SetInventoryOpen(bool open)
    {
        isInventoryOpen = open;

        if (open)
        {
            movement = Vector2.zero;
            animator.SetBool("IsWalking", false);
            Debug.Log($"🎒 Player movement stopped - inventory opened by at {GetCurrentDateTime()}");
        }
        else
        {
            Debug.Log($"🎒 Player movement resumed - inventory closed by at {GetCurrentDateTime()}");
        }
    }

    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    // Rest of the methods remain the same...
    private void StartAttack()
    {
        isAttacking = true;
        canAttacking = false;
        animator.SetTrigger("CanAttacking");
        StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        yield return new WaitForSeconds(attackDuration * 0.3f);

        if (IsPlayerDead || isDying)
        {
            isAttacking = false;
            canAttacking = true;
            yield break;
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();

            if (enemyController != null)
            {
                Vector2 hitDirection = (enemy.transform.position - transform.position).normalized;

                if (hitEffectPrefab != null)
                {
                    Vector3 hitPosition = (transform.position + enemy.transform.position) / 2f;
                    GameObject effect = Instantiate(hitEffectPrefab, hitPosition, Quaternion.identity);
                    Destroy(effect, hitEffectDuration);
                }

                enemyController.TakeDamage(attackDamage, hitDirection);
                Vector2 playerKnockback = -hitDirection * (knockbackForce * 0.15f);
                rb.AddForce(playerKnockback, ForceMode2D.Impulse);
            }
        }

        yield return new WaitForSeconds(attackDuration * 0.7f);
        isAttacking = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttacking = true;
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (IsPlayerDead || isDying || isInvincible)
            return;

        int newHealth = currentHealth - damage;

        if (newHealth <= 0)
        {
            isDying = true;
            IsPlayerDead = true;
            NotifyEnemiesPlayerDead();

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnPlayerDeath();
            }

            currentHealth = 0;
            UpdateHealthBar();

            StartCoroutine(DeathSequence(hitDirection));
            return;
        }

        currentHealth = newHealth;
        UpdateHealthBar();
        StartCoroutine(CombatReactionSequence(hitDirection));
    }

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            healthBarSlider.value = currentHealth;
        }
    }

    private IEnumerator CombatReactionSequence(Vector2 hitDirection)
    {
        isHurt = true;
        isKnockedBack = true;
        isInvincible = true;
        movement = Vector2.zero;
        animator.SetTrigger("IsHurt");
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(hitDirection * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockbackDuration);
        isKnockedBack = false;
        yield return new WaitForSeconds(hurtAnimationDuration - knockbackDuration);
        isHurt = false;
        yield return new WaitForSeconds(invincibilityDuration - hurtAnimationDuration);

        if (!IsPlayerDead && !isDying)
        {
            isInvincible = false;
        }
    }

    private IEnumerator DeathSequence(Vector2 hitDirection)
    {
        animator.SetTrigger("IsDead");
        movement = Vector2.zero;
        isHurt = false;
        isKnockedBack = false;
        isAttacking = false;
        canAttacking = false;
        isInvincible = true;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(hitDirection * knockbackForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(1.0f);

        GetComponent<Collider2D>().enabled = false;
        //this.enabled = false;
    }

    private void NotifyEnemiesPlayerDead()
    {
        EnemyController[] allEnemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        foreach (EnemyController enemy in allEnemies)
        {
            enemy.OnPlayerDeath();
        }
    }

    // Call this at the END of your death animation (Animation Event)
    public void OnDeathAnimationEnd()
    {
        Debug.Log("[PLAYER] Death animation ended, showing restart UI");

        if (menuManager == null)
        {
            menuManager = FindObjectOfType<MenuManager>();
        }

        if (menuManager != null)
        {
            menuManager.ShowRestartUI();
        }
        else
        {
            Debug.LogError("[PLAYER] MenuManager not found in OnDeathAnimationEnd!");
        }
    }

    // Called by MenuManager on restart (keep inventory, reset health and animation)
    public void ResetAfterRestart()
    {
        Debug.Log("[PLAYER] Resetting player after restart");

        // Reset death flags
        isDying = false;
        IsPlayerDead = false;

        // Reset health
        currentHealth = maxHealth;
        if (healthBarSlider != null)
        {
            healthBarSlider.maxValue = maxHealth;
            healthBarSlider.value = currentHealth;
        }

        // Re-enable the collider
        Collider2D playerCollider = GetComponent<Collider2D>();
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        // Reset animator
        animator.Rebind();
        animator.Update(0f);

        // Clear all animation triggers
        animator.ResetTrigger("IsDead");
        animator.ResetTrigger("IsHurt");
        animator.ResetTrigger("CanAttacking");

        // Reset movement variables
        isHurt = false;
        isKnockedBack = false;
        isAttacking = false;
        canAttacking = true;
        isInvincible = false;

        // Reset position if needed
        // transform.position = respawnPosition;

        Debug.Log("[PLAYER] Player successfully reset after death");
    }

    public static void ResetPlayerState()
    {
        IsPlayerDead = false;
    }

    public bool CanTakeDamage()
    {
        return !IsPlayerDead && !isDying && !isInvincible;
    }

    private string GetCurrentDateTime()
    {
        return System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}