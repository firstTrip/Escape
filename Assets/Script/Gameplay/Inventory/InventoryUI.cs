using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private InventorySlotUI[] slots;

    private void OnEnable()
    {
        if (inventoryManager != null)
            inventoryManager.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (inventoryManager != null)
            inventoryManager.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (inventoryManager == null) return;
        var items = inventoryManager.Items;
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].SetItem(i < items.Count ? items[i] : null);
        }
    }
}
