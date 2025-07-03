using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ItemSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image itemIcon;
    public TextMeshProUGUI quantityText;

    [Header("Item Data")]
    public Item currentItem;
    public int currentQuantity = 0;

    private Canvas canvas;
    private GameObject draggedObject;
    public bool itemWasDroppedOnValidTarget = false;

    void Awake()
    {
        if (itemIcon == null)
        {
            SetupItemIcon();
        }

        if (quantityText == null)
        {
            SetupQuantityText();
        }
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        ClearSlot();
    }

    void SetupItemIcon()
    {
        Transform iconChild = transform.Find("ItemIcon");
        if (iconChild != null)
        {
            itemIcon = iconChild.GetComponent<Image>();
        }

        if (itemIcon == null)
        {
            itemIcon = GetComponentInChildren<Image>();
        }
    }

    void SetupQuantityText()
    {
        Transform quantityChild = transform.Find("QuantityText");
        if (quantityChild != null)
        {
            quantityText = quantityChild.GetComponent<TextMeshProUGUI>();
        }

        if (quantityText == null)
        {
            quantityText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (quantityText == null)
        {
            GameObject quantityObj = new GameObject("QuantityText");
            quantityObj.transform.SetParent(transform);
            quantityObj.transform.localScale = Vector3.one;

            quantityText = quantityObj.AddComponent<TextMeshProUGUI>();
            quantityText.text = "";
            quantityText.fontSize = 4;
            quantityText.color = Color.white;
            quantityText.alignment = TextAlignmentOptions.BottomLeft;

            RectTransform rectTransform = quantityText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(2, 2);
            rectTransform.sizeDelta = new Vector2(25, 15);
        }
        else
        {
            quantityText.fontSize = 4;
            quantityText.alignment = TextAlignmentOptions.BottomLeft;

            RectTransform rectTransform = quantityText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(0, 0);
                rectTransform.pivot = new Vector2(0, 0);
                rectTransform.anchoredPosition = new Vector2(2, 2);
                rectTransform.sizeDelta = new Vector2(25, 15);
            }
        }
    }

    public void SetItem(Item newItem, int quantity = 1)
    {
        if (newItem == null)
        {
            ClearSlot();
            return;
        }

        Debug.Log($"[ItemSlot.SetItem] Slot: {gameObject.name}, Setting item: {newItem?.itemName}, quantity: {quantity}");

        //if (currentItem == newItem && newItem.isStackable)
        //{
        //AddToStack(quantity);
        //}
        //else
        //{
        //currentItem = newItem;
        //currentQuantity = Mathf.Min(quantity, GetMaxStackSize());
        //}

        // Always replace the slot contents when SetItem is called
        currentItem = newItem;
        currentQuantity = Mathf.Min(quantity, GetMaxStackSize());
        UpdateItemDisplay();
    }

    public int GetMaxStackSize()
    {
        if (currentItem != null && currentItem.isStackable)
        {
            return currentItem.maxStackSize;
        }
        return 1;
    }

    public bool AddToStack(int amount)
    {
        if (currentItem == null || !currentItem.isStackable)
        {
            return false;
        }

        int newQuantity = currentQuantity + amount;
        int maxStack = GetMaxStackSize();

        if (newQuantity <= maxStack)
        {
            currentQuantity = newQuantity;
            UpdateItemDisplay();
            return true;
        }
        else
        {
            int canAdd = maxStack - currentQuantity;
            if (canAdd > 0)
            {
                currentQuantity = maxStack;
                UpdateItemDisplay();
                return false;
            }
            return false;
        }
    }

    public void AddToStack(int amount, bool logErrors = true)
    {
        if (currentItem != null && currentItem.isStackable)
        {
            int oldQuantity = currentQuantity;
            currentQuantity += amount;
            currentQuantity = Mathf.Min(currentQuantity, GetMaxStackSize());
            UpdateItemDisplay();
        }
    }

    public int RemoveFromStack(int amount)
    {
        if (currentItem == null)
            return 0;

        int actualRemoved = Mathf.Min(amount, currentQuantity);
        currentQuantity -= actualRemoved;

        if (currentQuantity <= 0)
        {
            ClearSlot();
        }
        else
        {
            UpdateItemDisplay();
        }

        return actualRemoved;
    }

    public bool CanStackWith(Item item)
    {
        if (currentItem == null)
            return true;

        return currentItem == item &&
               item != null &&
               item.isStackable &&
               currentQuantity < GetMaxStackSize();
    }

    public int GetAvailableStackSpace()
    {
        if (currentItem == null)
            return 0;

        if (!currentItem.isStackable)
            return 0;

        return GetMaxStackSize() - currentQuantity;
    }

    private void UpdateItemDisplay()
    {
        Debug.Log($"[ItemSlot] UpdateItemDisplay called for slot: {gameObject.name}, item: {currentItem?.itemName}, qty: {currentQuantity}");

        if (itemIcon == null)
        {
            itemIcon = GetComponentInChildren<Image>(true);

            if (itemIcon == null)
            {
                GameObject iconObj = transform.Find("ItemIcon")?.gameObject;
                if (iconObj == null)
                {
                    iconObj = new GameObject("ItemIcon");
                    iconObj.transform.SetParent(transform, false);

                    RectTransform rt = iconObj.AddComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                }

                itemIcon = iconObj.GetComponent<Image>();
                if (itemIcon == null)
                {
                    itemIcon = iconObj.AddComponent<Image>();
                }
            }
        }

        if (itemIcon != null)
        {
            if (currentItem != null && currentQuantity > 0)
            {
                itemIcon.gameObject.SetActive(true);
                itemIcon.enabled = true;

                if (currentItem.itemIcon != null)
                {
                    itemIcon.sprite = currentItem.itemIcon;
                }

                itemIcon.color = Color.white;
            }
            else
            {
                itemIcon.sprite = null;
                itemIcon.gameObject.SetActive(false);
            }
        }

        if (quantityText != null)
        {
            if (currentItem != null && currentQuantity > 1)
            {
                quantityText.text = currentQuantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }

        // Add this log to confirm display
        if (currentItem != null)
        {
            Debug.Log($"[UI] Displaying item '{currentItem.itemName}' (qty: {currentQuantity}) in slot: {gameObject.name}. Icon set: {(itemIcon != null && itemIcon.sprite != null)}");
        }
        else
        {
            Debug.Log($"[UI] Slot: {gameObject.name} cleared/no item. Icon set: {(itemIcon != null && itemIcon.sprite != null)}");
        }

        Canvas.ForceUpdateCanvases();
    }

    public void ClearSlot()
    {
        currentItem = null;
        currentQuantity = 0;
        UpdateItemDisplay();
    }

    public bool HasItem()
    {
        return currentItem != null && currentQuantity > 0;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.SelectSlot(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || currentQuantity <= 0) return;

        itemWasDroppedOnValidTarget = false;

        if (SelectionManager.Instance != null)
        {
            SelectionManager.Instance.SelectSlot(this);
        }

        draggedObject = new GameObject("DraggedItem");
        draggedObject.transform.SetParent(canvas.transform);

        Image dragImage = draggedObject.AddComponent<Image>();
        dragImage.sprite = currentItem.itemIcon;
        dragImage.raycastTarget = false;
        dragImage.color = new Color(1f, 1f, 1f, 0.8f);

        if (currentQuantity > 1)
        {
            GameObject dragQuantityObj = new GameObject("DragQuantityText");
            dragQuantityObj.transform.SetParent(draggedObject.transform);

            TextMeshProUGUI dragQuantityText = dragQuantityObj.AddComponent<TextMeshProUGUI>();
            dragQuantityText.text = currentQuantity.ToString();
            dragQuantityText.fontSize = 4;
            dragQuantityText.color = Color.white;
            dragQuantityText.alignment = TextAlignmentOptions.BottomLeft;

            RectTransform dragQuantityRect = dragQuantityText.GetComponent<RectTransform>();
            dragQuantityRect.anchorMin = new Vector2(0, 0);
            dragQuantityRect.anchorMax = new Vector2(0, 0);
            dragQuantityRect.pivot = new Vector2(0, 0);
            dragQuantityRect.anchoredPosition = new Vector2(2, 2);
            dragQuantityRect.sizeDelta = new Vector2(25, 15);
        }

        draggedObject.transform.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedObject != null)
        {
            draggedObject.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedObject != null)
        {
            if (!itemWasDroppedOnValidTarget && IsDroppedOutsideInventorySystem(eventData.position))
            {
                ClearSlot();
            }

            Destroy(draggedObject);
        }

        itemWasDroppedOnValidTarget = false;
    }

    public virtual void OnDrop(PointerEventData eventData)
    {
        ItemSlot draggedSlot = eventData.pointerDrag?.GetComponent<ItemSlot>();

        if (draggedSlot != null && draggedSlot != this)
        {
            draggedSlot.itemWasDroppedOnValidTarget = true;

            // Handle stacking if possible
            if (currentItem == draggedSlot.currentItem &&
                currentItem != null &&
                currentItem.isStackable)
            {
                int spaceAvailable = GetAvailableStackSpace();
                int toMove = Mathf.Min(draggedSlot.currentQuantity, spaceAvailable);

                if (toMove > 0)
                {
                    AddToStack(toMove);
                    draggedSlot.RemoveFromStack(toMove);
                    return;
                }
            }

            // If can't stack, swap items
            Item tempItem = currentItem;
            int tempQuantity = currentQuantity;

            SetItem(draggedSlot.currentItem, draggedSlot.currentQuantity);
            draggedSlot.SetItem(tempItem, tempQuantity);
        }
    }

    private bool IsDroppedOutsideInventorySystem(Vector2 screenPosition)
    {
        GameObject hitObject = GetUIObjectUnderPosition(screenPosition);

        if (hitObject != null)
        {
            ItemSlot hitSlot = hitObject.GetComponent<ItemSlot>();
            if (hitSlot != null)
            {
                return false;
            }

            if (IsPartOfInventoryUI(hitObject))
            {
                return false;
            }
        }

        return true;
    }

    private GameObject GetUIObjectUnderPosition(Vector2 screenPosition)
    {
        if (canvas == null) return null;

        GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null) return null;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        return results.Count > 0 ? results[0].gameObject : null;
    }

    private bool IsPartOfInventoryUI(GameObject obj)
    {
        if (obj == null) return false;

        Transform current = obj.transform;

        while (current != null)
        {
            if (current.name.Contains("Inventory") ||
                current.name.Contains("Slot") ||
                current.name.Contains("Equipment") ||
                current.name.Contains("Background"))
            {
                return true;
            }

            if (current.GetComponent<ItemSlot>() != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    public void ForceUpdateDisplay()
    {
        UpdateItemDisplay();
    }
}