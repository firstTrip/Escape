using UnityEngine;

public class Door : InteractableHotspot
{
    [SerializeField] private InventoryItemSO keyItem;
    [TextArea][SerializeField] private string lockedLine = "문은 잠겨 있다.";
    [TextArea][SerializeField] private string haveKeyLine = "이제 이 문을 열 수 있는 열쇠가 있다. 하지만 아직은, 여기서 할 일이 남아 있는 것 같다.";

    protected override void OnInteract()
    {
        bool hasKey = keyItem != null && InventoryManager.Instance != null && InventoryManager.Instance.Has(keyItem);
        SubtitleUI.Instance?.ShowLine(hasKey ? haveKeyLine : lockedLine);
    }
}
