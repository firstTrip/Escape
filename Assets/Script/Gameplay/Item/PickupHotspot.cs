using UnityEngine;
using UnityEngine.UI;

public class PickupHotspot : InteractableHotspot
{
    [SerializeField] private InventoryItemSO item;
    [TextArea][SerializeField] private string pickupLine;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip pickupClip;
    [SerializeField] private Image visual;

    private bool collected;

    protected override void OnInteract()
    {
        if (collected) return;
        collected = true;

        InventoryManager.Instance?.Add(item);
        if (sfxSource != null && pickupClip != null)
            sfxSource.PlayOneShot(pickupClip);
        if (!string.IsNullOrEmpty(pickupLine))
            SubtitleUI.Instance?.ShowLine(pickupLine);
        if (visual != null)
            visual.enabled = false;

        Interactable = false;
    }
}
