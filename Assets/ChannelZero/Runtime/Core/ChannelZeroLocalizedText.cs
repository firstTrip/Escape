using System;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    [Serializable]
    public struct ChannelZeroLocalizedText
    {
        [SerializeField] private string key;
        [SerializeField, TextArea] private string fallback;

        public string Key => key ?? string.Empty;
        public string Fallback => fallback ?? string.Empty;

        public ChannelZeroLocalizedText(string key, string fallback)
        {
            this.key = key ?? string.Empty;
            this.fallback = fallback ?? string.Empty;
        }

        public static ChannelZeroLocalizedText Literal(string value) => new(string.Empty, value);
    }

    public interface IChannelZeroTextLocalizer
    {
        string Locale { get; }
        string Resolve(string key, string fallback = "");
    }

    public sealed class NarrativeTextLocalizer : IChannelZeroTextLocalizer
    {
        private readonly NarrativeTextCatalog catalog;

        public string Locale => catalog.Locale;

        public NarrativeTextLocalizer(NarrativeTextCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public string Resolve(string key, string fallback = "")
        {
            if (!string.IsNullOrWhiteSpace(key) &&
                catalog.TryGetById(key, out NarrativeTextEntry entry) &&
                !string.IsNullOrWhiteSpace(entry.text))
                return entry.text;
            return fallback ?? string.Empty;
        }
    }
}
