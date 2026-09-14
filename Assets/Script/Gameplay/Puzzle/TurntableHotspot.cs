using System;
using UnityEngine;

public class TurntableHotspot : InteractableHotspot
{
    [SerializeField] private InventoryItemSO recordItem;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip motifClip;
    [TextArea][SerializeField] private string needRecordLine = "틀 수 있는 음반이 없다.";
    [TextArea][SerializeField] private string playingLine = "낯익은 네 개의 음이, 방 안에 조용히 번진다.";

    public event Action OnSolved;
    public bool IsSolved { get; private set; }

    protected override void OnInteract()
    {
        var inv = InventoryManager.Instance;
        if (inv == null || !inv.Has(recordItem))
        {
            SubtitleUI.Instance?.ShowLine(needRecordLine);
            return;
        }

        if (musicSource != null && motifClip != null)
        {
            musicSource.Stop();
            musicSource.clip = motifClip;
            musicSource.Play();
        }
        SubtitleUI.Instance?.ShowLine(playingLine);

        if (!IsSolved)
        {
            IsSolved = true;
            OnSolved?.Invoke();
        }
    }
}
