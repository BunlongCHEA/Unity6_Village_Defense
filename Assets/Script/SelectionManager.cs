using UnityEngine;
using UnityEngine.UI;

public class SelectionManager : MonoBehaviour
{
    [Header("Selection Visual")]
    public GameObject selectionHighlight; // Your selection sprite
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    private ItemSlot currentSelectedSlot;

    public static SelectionManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initially hide selection
        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false);
        }
    }

    public void SelectSlot(ItemSlot slot)
    {
        // Deselect previous slot
        if (currentSelectedSlot != null)
        {
            SetSlotHighlight(currentSelectedSlot, false);
        }

        // Select new slot
        currentSelectedSlot = slot;

        if (currentSelectedSlot != null)
        {
            SetSlotHighlight(currentSelectedSlot, true);

            // Move selection highlight to slot position
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(true);
                selectionHighlight.transform.position = slot.transform.position;
                selectionHighlight.transform.SetParent(slot.transform);
            }

            Debug.Log($"Selected: {slot.currentItem?.itemName ?? "Empty Slot"}");
        }
        else
        {
            // Hide selection highlight
            if (selectionHighlight != null)
            {
                selectionHighlight.SetActive(false);
            }
        }
    }

    public void DeselectSlot()
    {
        if (currentSelectedSlot != null)
        {
            SetSlotHighlight(currentSelectedSlot, false);
            currentSelectedSlot = null;
        }

        if (selectionHighlight != null)
        {
            selectionHighlight.SetActive(false);
        }
    }

    private void SetSlotHighlight(ItemSlot slot, bool isSelected)
    {
        Image slotImage = slot.GetComponent<Image>();
        if (slotImage != null)
        {
            slotImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    public ItemSlot GetSelectedSlot()
    {
        return currentSelectedSlot;
    }

    public bool HasSelection()
    {
        return currentSelectedSlot != null;
    }
}