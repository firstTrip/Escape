using UnityEngine;

public class PhotoRestorePuzzle : InteractableHotspot
{
    [SerializeField] private InventoryItemSO photoCombinedItem;
    [SerializeField] private InventoryItemSO razorBladeItem;
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite aloneSprite;
    [SerializeField] private Sprite absenceSprite;
    [TextArea][SerializeField] private string needItemsLine = "여기서 뭔가 더 할 수 있을 것 같은데, 아직은 아니다.";
    [TextArea][SerializeField] private string backText =
        "면도날로 사진 뒷면을 조심스럽게 긁어낸다.\n테이프 아래, 또 다른 사진이 겹쳐 있다.";
    [TextArea][SerializeField] private string aloneText = "한 사람만 남은 사진이다.";
    [TextArea][SerializeField] private string absenceText = "이제, 아무도 없다.";

    private int stage;
    public bool IsSolved { get; private set; }

    protected override void OnInteract()
    {
        var inv = InventoryManager.Instance;
        bool hasItems = inv != null && inv.Has(photoCombinedItem) && inv.Has(razorBladeItem);
        if (!hasItems)
        {
            SubtitleUI.Instance?.ShowLine(needItemsLine);
            return;
        }

        switch (stage)
        {
            case 0:
                ExaminePanelUI.Instance?.Show(backSprite, backText);
                stage = 1;
                break;
            case 1:
                ExaminePanelUI.Instance?.Show(aloneSprite, aloneText);
                stage = 2;
                break;
            default:
                ExaminePanelUI.Instance?.Show(absenceSprite, absenceText);
                IsSolved = true;
                break;
        }
    }
}
