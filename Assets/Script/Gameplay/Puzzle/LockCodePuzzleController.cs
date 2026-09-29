using System;
using UnityEngine;
using UnityEngine.UI;

public class LockCodePuzzleController : NumericCodePuzzleBase
{
    [Header("Reward")]
    [SerializeField] private InventoryItemSO keyItem;
    [SerializeField] private InventoryItemSO lighterItem;
    [SerializeField] private Image lockedVisual;
    [SerializeField] private Image openVisual;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip wrongClip;
    [TextArea][SerializeField] private string solvedLine = "상자가 열렸다. 여벌 열쇠와 오래된 라이터가 들어 있다.";
    [TextArea][SerializeField] private string wrongLine = "숫자가 맞지 않는 것 같다.";

    public event Action OnSolved;
    protected override int[] DefaultCode => new[] { 3, 1, 3 };
    protected override void HandleIncorrectAnswer()
    {
        if (sfxSource != null && wrongClip != null)
            sfxSource.PlayOneShot(wrongClip);
        SubtitleUI.Instance?.ShowFeedback(wrongLine);
    }

    protected override void HandleSolved()
    {
        if (sfxSource != null && openClip != null) sfxSource.PlayOneShot(openClip);
        InventoryManager.Instance?.Add(keyItem);
        InventoryManager.Instance?.Add(lighterItem);
        if (lockedVisual != null) lockedVisual.enabled = false;
        if (openVisual != null) openVisual.enabled = true;
        SubtitleUI.Instance?.ShowFeedback(solvedLine);
        OnSolved?.Invoke();
    }
}
