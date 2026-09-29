using UnityEngine;
using UnityEngine.UI;

public class RecordShelfHotspot : InteractableHotspot, IPuzzle
{
    [SerializeField] private InventoryItemSO recordItem;
    [SerializeField] private Sprite jacketClosedSprite;
    [SerializeField] private Sprite jacketOpenSprite;
    [TextArea][SerializeField] private string closedText =
        "낡은 음반 재킷. 뒷면 곡 순서표 구석에 라디오 주파수처럼 세 자리 숫자가 적혀 있다 — \'098\'.";
    [TextArea][SerializeField] private string noteText =
        "이 노래를 들으면, 곁에 없어도 결국은 닿는다는 말이 위로가 됐어.\n어릴 때 일찍 떠난 아빠 생각이 날 때마다, 이 노래를 틀었어.";
    [SerializeField] private Image visual;

    private bool collected;
    public bool IsSolved => collected;

    protected override void OnInteract()
    {
        if (collected)
        {
            ExaminePanelUI.Instance?.ShowDocument(jacketOpenSprite, noteText);
            return;
        }

        collected = true;
        InventoryManager.Instance?.Add(recordItem);
        if (visual != null) visual.enabled = false;
        ExaminePanelUI.Instance?.ShowDocument(jacketClosedSprite, closedText);
    }
}
