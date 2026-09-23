using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    [CreateAssetMenu(menuName = "Channel Zero/Closeup Catalog", fileName = "ChannelZeroCloseupCatalog")]
    public sealed class ChannelZeroCloseupCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class ArtworkEntry
        {
            public string closeupId;
            public string stateId = ChannelZeroIds.DefaultVisualState;
            public Sprite artwork;
        }

        [SerializeField] private List<ArtworkEntry> artworks = new();

        public bool TryGetArtwork(string closeupId, string stateId, out Sprite artwork)
        {
            string requestedState = string.IsNullOrWhiteSpace(stateId)
                ? ChannelZeroIds.DefaultVisualState
                : stateId;
            ArtworkEntry exact = artworks.Find(entry =>
                entry.closeupId == closeupId && entry.stateId == requestedState);
            artwork = exact?.artwork;
            return artwork != null;
        }

#if UNITY_EDITOR
        public void EditorSetArtworks(IEnumerable<ArtworkEntry> entries)
        {
            artworks = new List<ArtworkEntry>(entries);
        }
#endif
    }
}
