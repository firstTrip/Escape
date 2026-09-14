using UnityEngine;
using UnityEngine.UI;

public class RecordShelfHotspot : InteractableHotspot
{
    [SerializeField] private InventoryItemSO recordItem;
    [SerializeField] private Sprite jacketClosedSprite;
    [SerializeField] private Sprite jacketOpenSprite;
    [TextArea][SerializeField] private string closedText =
        "낡은 음반 재킷. 표지에는 아무것도 적혀 있지 않지만, 뒷면 곡 순서표 구석에 작은 숫자가 손으로 적혀 있다 — \'98\'.";
    [TextArea][SerializeField] private string noteText =
        "이 노래를 들으면, 곁에 없어도 결국은 닿는다는 말이 위로가 됐어.\n어릴 때 일찍 떠난 아빠 생각이 날 때마다, 이 노래를 틀었어.";
    [SerializeField] private Image visual;

    private bool collected;

    protected override void OnInteract()
    {
        if (collected)
        {
            ExaminePanelUI.Instance?.Show(jacketOpenSprite, noteText);
            return;
        }

        collected = true;
        InventoryManager.Instance?.Add(recordItem);
        if (visual != null) visual.enabled = false;
        ExaminePanelUI.Instance?.Show(jacketClosedSprite, closedText);
    }
}
