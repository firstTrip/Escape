using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    [CreateAssetMenu(menuName = "Channel Zero/Room Catalog", fileName = "ChannelZeroRoomCatalog")]
    public sealed class ChannelZeroRoomCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class BackgroundEntry
        {
            public string roomId;
            public ChannelEra era;
            public string stateId = ChannelZeroIds.DefaultVisualState;
            public Sprite sprite;
        }

        [SerializeField] private List<BackgroundEntry> backgrounds = new();

        public IReadOnlyList<BackgroundEntry> Backgrounds => backgrounds;

        public bool TryGetBackground(string roomId, ChannelEra era, string stateId, out Sprite sprite)
        {
            string requestedState = string.IsNullOrWhiteSpace(stateId)
                ? ChannelZeroIds.DefaultVisualState
                : stateId;

            BackgroundEntry exact = backgrounds.Find(entry =>
                entry.roomId == roomId && entry.era == era && entry.stateId == requestedState);
            if (exact?.sprite != null)
            {
                sprite = exact.sprite;
                return true;
            }

            BackgroundEntry fallback = backgrounds.Find(entry =>
                entry.roomId == roomId && entry.era == era && entry.stateId == ChannelZeroIds.DefaultVisualState);
            sprite = fallback?.sprite;
            return sprite != null;
        }

#if UNITY_EDITOR
        public void EditorSetBackgrounds(IEnumerable<BackgroundEntry> entries)
        {
            backgrounds = new List<BackgroundEntry>(entries);
        }
#endif
    }
}
