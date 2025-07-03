using System.Collections;
using UnityEngine;

namespace Assets.Script
{
    public class DroppedItem : MonoBehaviour
    {
        [Header("Item Data")]
        public Item itemData;
        public int quantity = 1;

        [Header("Collection Settings")]
        public float collectionRange = 1.5f;
        public bool autoCollect = true;
        public LayerMask playerLayer = 1 << 7; // Player layer

        [Header("Visual Effects")]
        public float floatHeight = 0.3f;
        public float floatSpeed = 2f;
        public float rotateSpeed = 50f;

        [Header("Collection Feedback")]
        public GameObject collectionEffectPrefab;
        public AudioClip collectionSound;

        private Vector3 startPosition;
        private SpriteRenderer spriteRenderer;
        private bool canBeCollected = true;
        private AudioSource audioSource;

        void Start()
        {
            startPosition = transform.position;
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();

            // Add some random offset to make items spread out
            Vector2 randomOffset = Random.insideUnitCircle * 0.3f;
            transform.position += (Vector3)randomOffset;
            startPosition = transform.position;
        }

        public void Initialize(Item item, int itemQuantity = 1)
        {
            itemData = item;
            quantity = itemQuantity;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null && item != null)
            {
                spriteRenderer.sprite = item.itemIcon;
            }

            gameObject.name = $"DroppedItem_{item.itemName}_x{quantity}";
        }

        void Update()
        {
            if (!canBeCollected) return;

            // Floating animation
            float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Rotation animation
            transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

            // Check for player in range
            if (autoCollect)
            {
                CheckForPlayerCollection();
            }
        }

        private void CheckForPlayerCollection()
        {
            // Don't collect if player is dead
            if (PlayerMovement.IsPlayerDead) return;

            // Use LayerMask to find player
            Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, collectionRange, playerLayer);

            if (playerCollider != null)
            {
                PlayerMovement player = playerCollider.GetComponent<PlayerMovement>();
                if (player != null && !PlayerMovement.IsPlayerDead)
                {
                    CollectItem(player);
                }
            }
        }

        public void CollectItem(PlayerMovement player)
        {
            if (!canBeCollected || itemData == null || quantity <= 0) return;

            // Don't collect if inventory is open (prevents spam collection)
            if (player.IsInventoryOpen()) return;

            if (InventoryManager.Instance != null)
            {
                int remainingQuantity = InventoryManager.Instance.AddItem(itemData, quantity);

                if (remainingQuantity == 0)
                {
                    // All items were collected
                    canBeCollected = false;
                    Debug.Log($"Player collected {quantity}x {itemData.itemName}");

                    // Play collection effects
                    PlayCollectionEffects();

                    StartCoroutine(CollectionEffect());
                }
                else if (remainingQuantity < quantity)
                {
                    // Some items were collected, update remaining
                    int collected = quantity - remainingQuantity;
                    quantity = remainingQuantity;
                    gameObject.name = $"DroppedItem_{itemData.itemName}_x{quantity}";
                    Debug.Log($"Player collected {collected}x {itemData.itemName}, {remainingQuantity} remaining on ground");

                    // Play partial collection effects
                    PlayCollectionEffects();
                }
                else
                {
                    // Inventory full, couldn't collect any
                    Debug.Log($"Inventory is full! Cannot collect {itemData.itemName}");

                    // Show inventory full feedback
                    ShowInventoryFullMessage();
                }
            }
            else
            {
                Debug.LogWarning("No InventoryManager found!");
            }
        }

        private void PlayCollectionEffects()
        {
            // Play collection sound
            if (collectionSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectionSound);
            }

            // Spawn collection effect
            if (collectionEffectPrefab != null)
            {
                GameObject effect = Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }

        private void ShowInventoryFullMessage()
        {
            // Flash red to indicate can't collect
            if (spriteRenderer != null)
            {
                StartCoroutine(FlashRed());
            }
        }

        private IEnumerator FlashRed()
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            spriteRenderer.color = originalColor;
        }

        private IEnumerator CollectionEffect()
        {
            // Simple collection animation - scale down
            Vector3 originalScale = transform.localScale;
            float animationTime = 0.3f;
            float elapsed = 0f;

            while (elapsed < animationTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationTime;
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
                yield return null;
            }

            Destroy(gameObject);
        }

        // Manual collection trigger
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!autoCollect || !canBeCollected) return;

            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null && !PlayerMovement.IsPlayerDead)
            {
                CollectItem(player);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, collectionRange);
        }

        // Public method to force collection (for manual pickup)
        public void ForceCollect()
        {
            PlayerMovement player = PlayerMovement.Instance;
            if (player != null)
            {
                CollectItem(player);
            }
        }
    }
}