using UnityEngine;

public class ExamineHotspot : InteractableHotspot
{
    [SerializeField] private Sprite examineSprite;
    [TextArea][SerializeField] private string examineText;

    protected override void OnInteract()
    {
        ExaminePanelUI.Instance?.Show(examineSprite, examineText);
    }
}
