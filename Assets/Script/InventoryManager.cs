using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform inventoryContent;
    public Transform equipmentContent;

    [Header("Slot Arrays")]
    public ItemSlot[] inventorySlots;
    public ItemSlot[] equipmentSlots;

    [Header("Test Items")]
    public Item[] testItems;

    [Header("Settings")]
    public int maxInventorySlots = 18;
    public int maxEquipmentSlots = 4;
    public bool isInventoryOpen = false;

    [Header("Game Flow Control")]
    [Tooltip("Should the game pause (time scale = 0) when inventory is open")]
    public bool pauseGameWhenInventoryOpen = true;
    [Tooltip("Should enemies stop moving when inventory is open")]
    public bool stopEnemiesWhenInventoryOpen = true;
    [Tooltip("Layer containing enemies to disable when inventory is open")]
    public LayerMask enemyLayer;

    [Header("Debug Settings")]
    [Tooltip("Enable to show loading results in the game view")]
    public bool showLoadingResults = true;

    // Previous time scale (to restore when closing inventory)
    private float previousTimeScale = 1f;

    // Singleton instance
    public static InventoryManager Instance { get; set; }

    void Awake()
    {
        Debug.Log("[INVENTORY] Awake called. Current instance: " + Instance);
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[INVENTORY] Duplicate detected, destroying this one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[INVENTORY] Instance set in Awake: " + Instance);
        InitializeSlots();
    }

    void Start()
    {
        Debug.Log("[INVENTORY] Start called");

        ValidateReferences();
        CloseInventory();
    }

    private void InitializeSlots()
    {
        Debug.Log($"[InventoryManager] InitializeSlots ");
        //AutoDetectInventorySlots();
        //AutoDetectEquipmentSlots();
        if (inventorySlots == null || inventorySlots.Length == 0)
        AutoDetectInventorySlots();

        if (equipmentSlots == null || equipmentSlots.Length == 0)
        AutoDetectEquipmentSlots();
    }

    private void AutoDetectInventorySlots()
    {
        if (inventoryContent != null)
        {
            ItemSlot[] foundSlots = inventoryContent.GetComponentsInChildren<ItemSlot>(true);
            Debug.Log($"[InventoryManager] foundSlots: " + foundSlots);
            if (foundSlots.Length > 0)
            {
                inventorySlots = foundSlots;
                for (int i = 0; i < inventorySlots.Length; i++)
                {
                    Debug.Log($"[InventoryManager] inventorySlots[{i}] is GameObject: {inventorySlots[i]?.gameObject.name}");
                }
            }
        }
    }

    private void AutoDetectEquipmentSlots()
    {
        if (equipmentContent != null)
        {
            ItemSlot[] foundSlots = equipmentContent.GetComponentsInChildren<ItemSlot>(true);
            if (foundSlots.Length > 0)
                equipmentSlots = foundSlots;
        }
    }

    private void ValidateReferences()
    {
        // Ensure critical paths exist
        if (!System.IO.Directory.Exists(Application.persistentDataPath + "/ItemIcons"))
        {
            System.IO.Directory.CreateDirectory(Application.persistentDataPath + "/ItemIcons");
        }
    }

    public InventoryData GetInventoryData()
    {
        if (inventorySlots == null || equipmentSlots == null)
        {
            InitializeSlots();
        }

        // Create the centralized data structure
        InventoryData data = new InventoryData();

        // Set player data (from GameManager or other sources)
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            data.playerPosition = player.transform.position;

            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                data.playerHealth = playerMovement.currentHealth;
            }
        }

        // Current date and player name
        data.saveDateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        data.playerName = "BunlongCHEA"; // Could be from a player profile

        // Collect inventory items
        if (inventorySlots != null)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] != null && inventorySlots[i].HasItem())
                {
                    Item item = inventorySlots[i].currentItem;
                    string itemId = item.itemName.ToLower().Replace(" ", "_");

                    data.inventoryItems.Add(new ItemData(
                        itemId,
                        item.itemName,
                        inventorySlots[i].currentQuantity,
                        i,
                        item.isStackable,
                        item.isEquippable,
                        item.equipmentType.ToString()
                    ));
                }
            }
        }

        // Collect equipment items
        if (equipmentSlots != null)
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                if (equipmentSlots[i] != null && equipmentSlots[i].HasItem())
                {
                    Item item = equipmentSlots[i].currentItem;
                    string itemId = item.itemName.ToLower().Replace(" ", "_");

                    data.equipmentItems.Add(new ItemData(
                        itemId,
                        item.itemName,
                        equipmentSlots[i].currentQuantity,
                        i,
                        item.isStackable,
                        item.isEquippable,
                        item.equipmentType.ToString()
                    ));
                }
            }
        }

        data.isInventoryOpen = isInventoryOpen;

        return data;
    }

    public void LoadInventoryData(InventoryData data)
    {
        Debug.Log("[INVENTORY] LoadInventoryData ENTER");

        if (data == null)
        {
            Debug.LogWarning("InventoryManager: Tried to load null inventory data");
            return;
        }

        if (inventorySlots == null || inventorySlots.Length == 0)
        {
            InitializeSlots();
        }

        try
        {
            // Clear existing inventory
            ClearInventory();
            ClearEquipment();

            Debug.Log($"[INVENTORY] Loading inventory data with {data.inventoryItems?.Count ?? 0} inventory items");
            Debug.Log($"[INVENTORY] inventorySlots null? {inventorySlots == null}");
            Debug.Log($"[INVENTORY] inventorySlots.Length: {(inventorySlots != null ? inventorySlots.Length.ToString() : "n/a")}");
            Debug.Log($"[INVENTORY] data.inventoryItems null? {data.inventoryItems == null}");
            Debug.Log($"[INVENTORY] data.equipmentItems null? {data.equipmentItems == null}");

            // Apply inventory items
            if (data.inventoryItems != null)
            {
                foreach (var itemData in data.inventoryItems)
                {
                    Item item = itemData.GetItemReference();
                    Debug.Log($"[INVENTORY] Placing item {item?.itemName} x{itemData.quantity} at slot {itemData.slotIndex}");

                    if (item != null &&
                        itemData.slotIndex >= 0 &&
                        itemData.slotIndex < inventorySlots.Length &&
                        inventorySlots[itemData.slotIndex] != null)
                    {
                        inventorySlots[itemData.slotIndex].SetItem(item, itemData.quantity);
                    }
                }
            }

            // Apply equipment items
            if (data.equipmentItems != null)
            {
                foreach (var itemData in data.equipmentItems)
                {
                    Item item = itemData.GetItemReference();

                    if (item != null &&
                        itemData.slotIndex >= 0 &&
                        itemData.slotIndex < equipmentSlots.Length &&
                        equipmentSlots[itemData.slotIndex] != null)
                    {
                        equipmentSlots[itemData.slotIndex].SetItem(item, itemData.quantity);
                    }
                }
            }

            // Apply inventory state
            isInventoryOpen = data.isInventoryOpen;

            // Update the UI
            ForceUIRefresh();
            ForceSetSlotSprites();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error loading inventory data: " + e.Message);
        }
    }

    public int AddItem(Item item, int quantity)
    {
        if (item == null || quantity <= 0)
            return quantity;

        if (inventorySlots == null)
        {
            InitializeSlots();
            if (inventorySlots == null)
                return quantity;
        }

        int remainingQuantity = quantity;

        if (item.isStackable)
        {
            foreach (ItemSlot slot in inventorySlots)
            {
                if (slot != null && slot.HasItem() && slot.currentItem.itemName == item.itemName)
                {
                    int spaceInSlot = slot.GetMaxStackSize() - slot.currentQuantity;
                    if (spaceInSlot > 0)
                    {
                        int amountToAdd = Mathf.Min(remainingQuantity, spaceInSlot);
                        slot.AddToStack(amountToAdd);
                        remainingQuantity -= amountToAdd;

                        if (remainingQuantity <= 0) break;
                    }
                }
            }
        }

        if (remainingQuantity > 0)
        {
            foreach (ItemSlot slot in inventorySlots)
            {
                if (slot != null && !slot.HasItem())
                {
                    int amountToAdd = item.isStackable ? Mathf.Min(remainingQuantity, slot.GetMaxStackSize()) : 1;
                    slot.SetItem(item, amountToAdd);
                    remainingQuantity -= amountToAdd;

                    if (remainingQuantity <= 0) break;
                }
            }
        }

        return remainingQuantity;
    }

    // Clear and Player method
    public void OnPlayerDeath()
    {
        if (isInventoryOpen)
            CloseInventory();
    }

    public ItemSlot[] GetInventorySlots()
    {
        if (inventorySlots == null)
            InitializeSlots();

        return inventorySlots ?? new ItemSlot[0];
    }

    public ItemSlot[] GetEquipmentSlots()
    {
        if (equipmentSlots == null)
            InitializeSlots();

        return equipmentSlots ?? new ItemSlot[0];
    }

    public Item[] GetTestItems()
    {
        if (testItems == null || testItems.Length == 0)
            LoadTestItemsFromResources();

        return testItems ?? new Item[0];
    }

    private void LoadTestItemsFromResources()
    {
        Item[] resourceItems = Resources.LoadAll<Item>("Item");

        if (resourceItems == null || resourceItems.Length == 0)
        {
            resourceItems = Resources.LoadAll<Item>("");
        }

        if (resourceItems != null && resourceItems.Length > 0)
            testItems = resourceItems;
    }

    private void SetEnemiesActive(bool active)
    {
        if (!stopEnemiesWhenInventoryOpen)
            return;

        if (enemyLayer == 0)
            return;

        GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allGameObjects)
        {
            if (obj == null || ((1 << obj.layer) & enemyLayer.value) == 0)
                continue;

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = !active;
            }

            MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
            if (scripts != null)
            {
                foreach (MonoBehaviour script in scripts)
                {
                    if (script == null)
                        continue;

                    string scriptName = script.GetType().Name.ToLower();
                    if (scriptName.Contains("enemy") || scriptName.Contains("ai") ||
                        scriptName.Contains("controller") || scriptName.Contains("movement"))
                    {
                        script.enabled = active;
                    }
                }
            }
        }
    }

    public void ToggleInventory()
    {
        if (isInventoryOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        if (isInventoryOpen)
            return;

        previousTimeScale = Time.timeScale;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(true);

        isInventoryOpen = true;

        try
        {
            if (pauseGameWhenInventoryOpen)
            {
                Time.timeScale = 0.01f;
            }

            SetEnemiesActive(false);
        }
        catch (System.Exception e)
        {
            Debug.Log("Error OpenInventory: " + e);
        }
    }

    public void CloseInventory()
    {
        if (!isInventoryOpen)
            return;

        try
        {
            if (pauseGameWhenInventoryOpen)
                Time.timeScale = previousTimeScale > 0 ? previousTimeScale : 1f;

            SetEnemiesActive(true);
        }
        catch (System.Exception)
        {
            Time.timeScale = 1f;
        }

        isInventoryOpen = false;

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    public void ClearInventory()
    {
        if (inventorySlots != null)
        {
            foreach (ItemSlot slot in inventorySlots)
            {
                if (slot != null)
                    slot.ClearSlot();
            }
        }
    }

    public void ClearEquipment()
    {
        if (equipmentSlots != null)
        {
            foreach (ItemSlot slot in equipmentSlots)
            {
                if (slot != null)
                    slot.ClearSlot();
            }
        }
    }

    public void ClearAll()
    {
        ClearInventory();
        ClearEquipment();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Refresh Inventory UI
    public void ForceUIRefresh()
    {
        if (inventorySlots != null)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] != null)
                {
                    if (inventorySlots[i].HasItem())
                    {
                        Item tempItem = inventorySlots[i].currentItem;
                        int tempQuantity = inventorySlots[i].currentQuantity;
                        inventorySlots[i].SetItem(tempItem, tempQuantity);
                    }
                    else
                    {
                        inventorySlots[i].ForceUpdateDisplay();
                    }
                }
            }
        }

        if (equipmentSlots != null)
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                if (equipmentSlots[i] != null)
                {
                    if (equipmentSlots[i].HasItem())
                    {
                        Item tempItem = equipmentSlots[i].currentItem;
                        int tempQuantity = equipmentSlots[i].currentQuantity;
                        equipmentSlots[i].SetItem(tempItem, tempQuantity);
                    }
                    else
                    {
                        equipmentSlots[i].ForceUpdateDisplay();
                    }
                }
            }
        }

        Canvas.ForceUpdateCanvases();
    }

    public void ForceSetSlotSprites()
    {
        if (inventorySlots == null || inventorySlots.Length == 0)
            return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            ItemSlot slot = inventorySlots[i];
            if (slot == null) continue;

            if (slot.HasItem())
            {
                if (slot.itemIcon == null)
                {
                    slot.itemIcon = slot.GetComponentInChildren<Image>(true);
                    if (slot.itemIcon == null)
                    {
                        GameObject iconObj = new GameObject("ItemIcon");
                        iconObj.transform.SetParent(slot.transform, false);
                        slot.itemIcon = iconObj.AddComponent<Image>();
                    }
                }

                if (slot.currentItem.itemIcon != null)
                {
                    slot.itemIcon.gameObject.SetActive(true);
                    slot.itemIcon.enabled = true;
                    slot.itemIcon.sprite = slot.currentItem.itemIcon;
                    slot.itemIcon.color = Color.white;
                }
            }
        }

        Canvas.ForceUpdateCanvases();
    }
}