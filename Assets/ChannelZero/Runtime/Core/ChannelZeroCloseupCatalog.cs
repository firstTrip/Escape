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
            [Tooltip("0이면 모든 연도에서 사용하는 기본 이미지입니다.")]
            public int eraYear;
            public Sprite artwork;
        }

        [SerializeField] private List<ArtworkEntry> artworks = new();
        public IReadOnlyList<ArtworkEntry> Artworks => artworks;

        public bool TryGetArtwork(string closeupId, string stateId, out Sprite artwork)
        {
            return TryGetArtwork(closeupId, stateId, null, out artwork);
        }

        public bool TryGetArtwork(string closeupId, string stateId, ChannelEra era, out Sprite artwork)
        {
            return TryGetArtwork(closeupId, stateId, (int)era, out artwork);
        }

        private bool TryGetArtwork(string closeupId, string stateId, int? eraYear, out Sprite artwork)
        {
            string requestedState = string.IsNullOrWhiteSpace(stateId)
                ? ChannelZeroIds.DefaultVisualState
                : stateId;

            ArtworkEntry exact = eraYear.HasValue
                ? artworks.Find(entry => entry.closeupId == closeupId
                    && entry.stateId == requestedState
                    && entry.eraYear == eraYear.Value)
                : null;
            ArtworkEntry fallback = artworks.Find(entry => entry.closeupId == closeupId
                && entry.stateId == requestedState
                && entry.eraYear == 0);
            artwork = (exact ?? fallback)?.artwork;
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
