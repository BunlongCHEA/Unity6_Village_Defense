using UnityEngine;

public enum EquipmentType
{
    None,
    Weapon,
    Armor,
    Torso,
    Helmet
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite itemIcon;

    [Header("Stacking")]
    public bool isStackable = true;
    public int maxStackSize = 50;

    [TextArea(3, 5)]
    public string description;

    [Header("Equipment Settings")]
    public bool isEquippable;
    public EquipmentType equipmentType = EquipmentType.None;

    [Header("Drop Settings")]
    [Range(0f, 1f)]

    //dropChance in Item.cs vs baseDropChance in EnemyController.cs
    //Checked SECOND, only if baseDropChance passed
    //Ex: Boss always drops something (baseDropChance 1.0),
    //but legendary weapon is still rare (dropChance 0.5). 
    public float dropChance = 0.5f; // Chance for this item to drop (0 = never, 1 = always)

    [Header("Drop Quantity (for stackable items)")]
    public int minDropQuantity = 1; // Minimum quantity to drop
    public int maxDropQuantity = 5; // Maximum quantity to drop

    [Header("Drop Spread")]
    public float dropSpreadRadius = 1.0f; // How far the item can spread when dropped
    public bool useRandomSpread = true; // Whether to use random positioning within spread radius
}