using System;
using ChannelZero.Runtime.Core;
using TMPro;

namespace ChannelZero.Runtime.Presentation
{
    public interface IChannelZeroInteractionSource
    {
        string InteractionId { get; }
        event Action<string> Activated;
        bool IsAvailable(string roomId, ChannelEra era);
        void SetAvailable(bool available);
    }

    public interface IChannelZeroEraLayout
    {
        bool ApplyEraLayout(ChannelEra era, ChannelZeroHotspotLayoutCatalog catalog);
    }

    public interface IChannelZeroPuzzleView
    {
        bool IsOpen { get; }
        TMP_Text NarrativeLabel { get; }
        event Action Closed;
        event Action<string> PuzzleActionRequested;
        void ConfigureInventory(Func<string, string> descriptionResolver);
        void ConfigureLocalization(string locale, Func<string, string, string> resolver);
        void Open(ChannelZeroSessionState state, string closeupId, string artworkStateId, string exactText);
        void OpenLocalized(ChannelZeroSessionState state, string closeupId, string artworkStateId,
            ChannelZeroLocalizedText exactText);
        void Restore(ChannelZeroSessionState state, string exactText);
        void PresentPuzzle(PuzzleView view);
        void SetInputLocked(bool locked);
        void RefreshInventory();
        void ShowFeedback(string message);
        void DismissForSequence();
    }

    public interface IPuzzleActionView
    {
        string ActionId { get; }
        void Bind(PuzzleActionView action, TMP_FontAsset font, Action<string> selected);
    }
}
