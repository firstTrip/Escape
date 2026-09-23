using System;
using ChannelZero.Runtime.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ChannelZeroRoomPresenter : MonoBehaviour
    {
        [SerializeField] private Image roomBackground;
        [SerializeField] private RectTransform hotspotLayer;
        [SerializeField] private ChannelZeroRoomCatalog catalog;

        private ChannelZeroHotspot[] hotspots = Array.Empty<ChannelZeroHotspot>();

        public event Action<string> HotspotClicked;

        private void Awake()
        {
            CacheHotspots();
        }

        private void OnEnable()
        {
            CacheHotspots();
            foreach (ChannelZeroHotspot hotspot in hotspots)
                hotspot.Clicked += HandleHotspotClicked;
        }

        private void OnDisable()
        {
            foreach (ChannelZeroHotspot hotspot in hotspots)
                hotspot.Clicked -= HandleHotspotClicked;
        }

        public bool Present(ChannelZeroSessionState state)
        {
            if (state == null || roomBackground == null || catalog == null)
                return false;

            bool found = catalog.TryGetBackground(
                state.roomId, state.era, state.roomVisualStateId, out Sprite sprite);
            if (found)
                roomBackground.sprite = sprite;

            foreach (ChannelZeroHotspot hotspot in hotspots)
                hotspot.SetAvailable(hotspot.IsAvailable(state.roomId, state.era));

            return found;
        }

        private void CacheHotspots()
        {
            if (hotspotLayer != null)
                hotspots = hotspotLayer.GetComponentsInChildren<ChannelZeroHotspot>(true);
        }

        private void HandleHotspotClicked(string logicalId)
        {
            HotspotClicked?.Invoke(logicalId);
        }

#if UNITY_EDITOR
        public void EditorConfigure(Image background, RectTransform layer, ChannelZeroRoomCatalog roomCatalog)
        {
            roomBackground = background;
            hotspotLayer = layer;
            catalog = roomCatalog;
        }
#endif
    }
}
