using UnityEngine;

public class ChapterTransitionStub : InteractableHotspot
{
    [SerializeField] private Act0HouseController house;
    [SerializeField] private Sprite matchboxSprite;
    [TextArea][SerializeField] private string notReadyLine = "아직은 여기 있어야 할 것 같다.";
    [TextArea][SerializeField] private string transitionText =
        "성냥갑을 손에 쥐자, 다이너의 불빛이 떠오른다.\n\n1번 공간 · 다이너로 이어집니다.\n(다음 업데이트에서 계속됩니다)";

    protected override void OnInteract()
    {
        if (house != null && !house.AllSolved)
        {
            SubtitleUI.Instance?.ShowLine(notReadyLine);
            return;
        }

        ExaminePanelUI.Instance?.Show(matchboxSprite, transitionText);
    }
}
