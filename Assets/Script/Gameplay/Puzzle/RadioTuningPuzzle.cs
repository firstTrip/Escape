using System;
using UnityEngine;

public class RadioTuningPuzzle : NumericCodePuzzleBase
{
    [Header("Outcome")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip staticClip;
    [SerializeField] private AudioClip motifClip;
    [SerializeField] private Sprite transitionSprite;
    [TextArea][SerializeField] private string wrongLine = "지지직거리는 잡음뿐이다.";
    [TextArea][SerializeField] private string transitionText =
        "잡음 사이로, 낯익은 네 개의 음 중 한 소절이 섞여 든다.\n\n2번 공간 · 사무실로 이어집니다.\n(다음 업데이트에서 계속됩니다)";

    public event Action OnSolved;
    protected override string DigitLabelPrefix => "RDial";
    protected override int[] DefaultCode => new[] { 0, 9, 8 };

    protected override void HandleIncorrectAnswer()
    {
        if (sfxSource != null && staticClip != null)
            sfxSource.PlayOneShot(staticClip);
        SubtitleUI.Instance?.ShowFeedback(wrongLine);
    }

    protected override void HandleSolved()
    {
        if (sfxSource != null && motifClip != null) sfxSource.PlayOneShot(motifClip);
        ExaminePanelUI.Instance?.ShowDocument(transitionSprite, transitionText);
        OnSolved?.Invoke();
    }
}
