using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    public void SetItem(InventoryItemSO item)
    {
        if (item == null || item.icon == null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
            return;
        }
        iconImage.enabled = true;
        iconImage.sprite = item.icon;
    }
}
