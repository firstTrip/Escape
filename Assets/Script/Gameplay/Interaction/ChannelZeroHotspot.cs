using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class ChannelZeroHotspot : InteractableHotspot
{
    [SerializeField] private string hotspotId;
    [SerializeField] private UnityEvent onInteract;

    public string HotspotId => hotspotId;

    public void Configure(string id)
    {
        hotspotId = id;
    }

    public void SetHotspotEnabled(bool enabled)
    {
        Interactable = enabled;
        GetComponent<Image>().raycastTarget = enabled;
    }

    protected override void OnInteract()
    {
        onInteract?.Invoke();
    }
}
