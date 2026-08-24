using System;
using UnityEngine;
using UnityEngine.UI;

public class PhotoCombinePuzzleController : InteractableHotspot
{
    [SerializeField] private InventoryItemSO fragment1;
    [SerializeField] private InventoryItemSO fragment2;
    [SerializeField] private InventoryItemSO fragment3;
    [SerializeField] private InventoryItemSO combinedPhoto;

    [SerializeField] private Sprite combinedSprite;
    [TextArea][SerializeField] private string combinedText = "이사 온 첫날, 텅 빈 집에서 찍은 사진이다.\n뒷면에는 이렇게 적혀 있다 — “우리 집, 3월 13일.”";
    [TextArea][SerializeField] private string needMoreLine = "조각이 더 필요한 것 같다.";

    public event Action OnSolved;
    public bool IsSolved { get; private set; }

    protected override void OnInteract()
    {
        if (IsSolved)
        {
            ExaminePanelUI.Instance?.Show(combinedSprite, combinedText);
            return;
        }

        var inv = InventoryManager.Instance;
        if (inv == null || !inv.Has(fragment1) || !inv.Has(fragment2) || !inv.Has(fragment3))
        {
            SubtitleUI.Instance?.ShowLine(needMoreLine);
            return;
        }

        inv.Remove(fragment1);
        inv.Remove(fragment2);
        inv.Remove(fragment3);
        inv.Add(combinedPhoto);

        IsSolved = true;
        ExaminePanelUI.Instance?.Show(combinedSprite, combinedText);
        OnSolved?.Invoke();
    }
}
