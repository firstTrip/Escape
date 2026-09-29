using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    [Serializable]
    public sealed class HotspotLayoutDefinition
    {
        public string interactionId;
        public int era;
        public float x;
        public float y;
        public float width;
        public float height;
        public bool enabled = true;

        public Rect NormalizedTopLeftRect => new(x, y, width, height);
    }

    [Serializable]
    internal sealed class HotspotLayoutDefinitionFile
    {
        public int schemaVersion;
        public List<HotspotLayoutDefinition> layouts = new();
    }

    public sealed class ChannelZeroHotspotLayoutCatalog
    {
        private readonly Dictionary<string, HotspotLayoutDefinition> exact = new(StringComparer.Ordinal);
        private readonly Dictionary<string, HotspotLayoutDefinition> fallback = new(StringComparer.Ordinal);
        private readonly List<HotspotLayoutDefinition> all = new();
        public IReadOnlyList<HotspotLayoutDefinition> Layouts => all;

        public static ChannelZeroHotspotLayoutCatalog LoadDefault()
        {
            TextAsset asset = Resources.Load<TextAsset>("ChannelZero/Data/hotspot_layouts.v1");
            if (asset == null)
                throw new InvalidOperationException("Hotspot layout resource is missing.");
            return FromJson(asset.text);
        }

        public static ChannelZeroHotspotLayoutCatalog FromJson(string json)
        {
            HotspotLayoutDefinitionFile file = JsonUtility.FromJson<HotspotLayoutDefinitionFile>(json);
            if (file?.layouts == null)
                throw new FormatException("Invalid hotspot layout JSON.");

            ChannelZeroHotspotLayoutCatalog catalog = new();
            foreach (HotspotLayoutDefinition layout in file.layouts)
            {
                if (string.IsNullOrWhiteSpace(layout.interactionId))
                    throw new FormatException("Hotspot layout requires an interaction ID.");
                if (layout.width <= 0f || layout.height <= 0f || layout.x < 0f || layout.y < 0f ||
                    layout.x + layout.width > 1f || layout.y + layout.height > 1f)
                    throw new FormatException($"Hotspot layout is outside normalized bounds: {layout.interactionId}/{layout.era}");

                Dictionary<string, HotspotLayoutDefinition> destination = layout.era == 0
                    ? catalog.fallback
                    : catalog.exact;
                string key = layout.era == 0 ? layout.interactionId : Key(layout.interactionId, layout.era);
                if (!destination.TryAdd(key, layout))
                    throw new FormatException($"Duplicate hotspot layout: {layout.interactionId}/{layout.era}");
                catalog.all.Add(layout);
            }
            return catalog;
        }

        public bool TryResolve(string interactionId, ChannelEra era, out HotspotLayoutDefinition layout) =>
            exact.TryGetValue(Key(interactionId, (int)era), out layout) ||
            fallback.TryGetValue(interactionId, out layout);

        private static string Key(string interactionId, int era) => $"{interactionId}:{era}";
    }
}
