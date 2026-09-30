using UnityEngine;

[CreateAssetMenu(fileName = "Item_", menuName = "Escape/Inventory Item")]
public class InventoryItemSO : ScriptableObject
{
    public string itemId;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
}
