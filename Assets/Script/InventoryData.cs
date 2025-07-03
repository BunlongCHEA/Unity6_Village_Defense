using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryData
{
    // Player Data
    public string saveDateTime = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
    public string playerName = "BunlongCHEA";
    public Vector3 playerPosition = Vector3.zero;
    public int playerHealth = 100;
    public string fileName = "village_defense";

    // Inventory Data
    public List<ItemData> inventoryItems = new List<ItemData>();
    public List<ItemData> equipmentItems = new List<ItemData>();
    public bool isInventoryOpen = false;

    // Utility methods for serialization
    public string ToJsonString()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static InventoryData FromJsonString(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData) || jsonData == "{}")
        {
            return new InventoryData();
        }

        try
        {
            return JsonUtility.FromJson<InventoryData>(jsonData);
        }
        catch
        {
            return new InventoryData();
        }
    }

    // Helper for finding items in Resources
    public static Item FindItemByIdOrName(string itemId, string itemName)
    {
        // Try to find by ID first in Resources
        Item item = Resources.Load<Item>($"Item/{itemId}");
        Debug.Log("item" + item);

        if (item != null)
            return item;

        // Try loading by name from all Resources
        Item[] allItems = Resources.LoadAll<Item>("");
        Debug.Log("allItems" + allItems);

        // Try to match by ID
        foreach (Item resItem in allItems)
        {
            string normalizedId = resItem.itemName.ToLower().Replace(" ", "_");
            Debug.Log("normalizedId" + normalizedId);
            if (normalizedId == itemId)
                return resItem;
        }

        // Try to match by name
        foreach (Item resItem in allItems)
        {
            Debug.Log("resItem.itemName" + resItem.itemName);
            if (resItem.itemName == itemName)
                return resItem;
        }

        // Try to match by case-insensitive name
        foreach (Item resItem in allItems)
        {
            if (string.Equals(resItem.itemName, itemName, System.StringComparison.OrdinalIgnoreCase))
                return resItem;
        }

        return null;
    }
}

[System.Serializable]
public class ItemData
{
    public string itemId;
    public string itemName;
    public int quantity;
    public int slotIndex;
    public bool isStackable;
    public bool isEquippable;
    public string equipmentType;

    // Helper method to convert ItemData to actual Item with quantity
    public Item GetItemReference()
    {
        return InventoryData.FindItemByIdOrName(itemId, itemName);
    }

    // Constructor with both ID and name
    public ItemData(string id, string name, int qty, int slot, bool stackable = false, bool equippable = false, string equipType = "None")
    {
        itemId = id;
        itemName = name;
        quantity = qty;
        slotIndex = slot;
        isStackable = stackable;
        isEquippable = equippable;
        equipmentType = equipType;
    }

    // Constructor from an Item object and slot
    public ItemData(Item item, int qty, int slot)
    {
        if (item != null)
        {
            itemId = item.itemName.ToLower().Replace(" ", "_");
            itemName = item.itemName;
            quantity = qty;
            slotIndex = slot;
            isStackable = item.isStackable;
            isEquippable = item.isEquippable;
            equipmentType = item.equipmentType.ToString();
        }
    }

    // Legacy constructor for backward compatibility
    public ItemData(string name, int qty, int slot, bool stackable = false, bool equippable = false, string equipType = "None")
    {
        itemId = name.ToLower().Replace(" ", "_");
        itemName = name;
        quantity = qty;
        slotIndex = slot;
        isStackable = stackable;
        isEquippable = equippable;
        equipmentType = equipType;
    }
}