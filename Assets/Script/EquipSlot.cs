using UnityEngine;
using UnityEngine.EventSystems;

public class EquipSlot : ItemSlot
{
    [Header("Equipment Settings")]
    public EquipmentType allowedEquipmentType = EquipmentType.Weapon;

    public override void OnDrop(PointerEventData eventData)
    {
        ItemSlot draggedSlot = eventData.pointerDrag?.GetComponent<ItemSlot>();

        if (draggedSlot != null && draggedSlot != this)
        {
            Item draggedItem = draggedSlot.currentItem;

            // ALWAYS mark as valid target, even if item can't be equipped
            draggedSlot.itemWasDroppedOnValidTarget = true;

            // Check if item can be equipped in this slot
            if (CanEquipItem(draggedItem))
            {
                // Swap items - allowed
                Item tempItem = currentItem;
                SetItem(draggedItem);
                draggedSlot.SetItem(tempItem);

                Debug.Log($"Equipped {allowedEquipmentType}: {draggedItem.itemName}");
            }
            else if (draggedItem != null)
            {
                // Item cannot be equipped here - item stays in original slot
                Debug.Log($"'{draggedItem.itemName}' cannot be equipped in {allowedEquipmentType} slot! Item returned to original position.");

                // Don't do anything - the item will stay in the original slot
                // because we marked it as dropped on valid target
            }
        }
    }

    private bool CanEquipItem(Item item)
    {
        if (item == null) return true; // Allow empty items (for swapping)

        return item.isEquippable && item.equipmentType == allowedEquipmentType;
    }

    // Visual feedback when wrong item is dragged over
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            ItemSlot draggedSlot = eventData.pointerDrag.GetComponent<ItemSlot>();
            if (draggedSlot != null && draggedSlot.currentItem != null)
            {
                bool canEquip = CanEquipItem(draggedSlot.currentItem);

                // Change visual feedback based on whether item can be equipped
                var image = GetComponent<UnityEngine.UI.Image>();
                if (image != null)
                {
                    image.color = canEquip ? Color.green : Color.red;
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Reset visual feedback
        var image = GetComponent<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.color = Color.white;
        }
    }
}