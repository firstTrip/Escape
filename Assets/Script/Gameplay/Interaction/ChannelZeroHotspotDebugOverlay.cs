using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class ChannelZeroHotspotDebugOverlay : MonoBehaviour
{
    [SerializeField] private CanvasGroup targetGroup;
    [SerializeField] private bool visibleOnStart = true;

    public bool IsVisible { get; private set; }

    public void Configure(CanvasGroup group, bool visible)
    {
        targetGroup = group;
        visibleOnStart = visible;
        SetVisible(visible);
    }

    private void Awake()
    {
        if (targetGroup == null)
            targetGroup = GetComponent<CanvasGroup>();
        SetVisible(visibleOnStart);
    }

    private void Update()
    {
        if (Keyboard.current?.f8Key.wasPressedThisFrame == true)
            SetVisible(!IsVisible);
    }

    [ContextMenu("Toggle Hotspot Overlay")]
    public void Toggle()
    {
        SetVisible(!IsVisible);
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
        if (targetGroup != null)
            targetGroup.alpha = visible ? 1f : 0f;
    }
}
