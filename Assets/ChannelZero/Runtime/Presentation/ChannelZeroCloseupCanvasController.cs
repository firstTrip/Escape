using System;
using ChannelZero.Runtime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ChannelZeroCloseupCanvasController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image artwork;
        [SerializeField] private RectTransform interactionLayer;
        [SerializeField] private RectTransform feedbackLayer;
        [SerializeField] private RectTransform exactTextLayer;
        [SerializeField] private RectTransform inventoryStrip;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text exactTextLabel;
        [SerializeField] private ChannelZeroCloseupCatalog catalog;
        [SerializeField] private Color missingArtworkColor = new(0.16f, 0.14f, 0.12f, 1f);

        private ChannelZeroSessionState session;

        public bool IsOpen => canvasGroup != null && canvasGroup.blocksRaycasts;
        public event Action Closed;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void Open(ChannelZeroSessionState state, string closeupId,
            string artworkStateId = ChannelZeroIds.DefaultVisualState, string exactText = "")
        {
            session = state;
            session?.EnterCloseup(closeupId, artworkStateId);
            Present(closeupId, artworkStateId, exactText);
        }

        public void Restore(ChannelZeroSessionState state, string exactText = "")
        {
            if (state == null || string.IsNullOrWhiteSpace(state.activeCloseupId))
                return;
            session = state;
            Present(state.activeCloseupId, state.activeCloseupStateId, exactText);
        }

        public void SetInputLocked(bool locked)
        {
            if (session != null)
                session.closeupInputLocked = locked;
            if (closeButton != null)
                closeButton.interactable = !locked;
        }

        public void Close()
        {
            if (session != null && !session.TryExitCloseup())
                return;
            SetVisible(false);
            Closed?.Invoke();
        }

        private void Present(string closeupId, string artworkStateId, string exactText)
        {
            Sprite sprite = null;
            bool hasArtwork = catalog != null &&
                catalog.TryGetArtwork(closeupId, artworkStateId, out sprite);
            if (artwork != null)
            {
                artwork.sprite = hasArtwork ? sprite : null;
                artwork.color = hasArtwork ? Color.white : missingArtworkColor;
            }
            if (exactTextLabel != null)
                exactTextLabel.text = exactText;
            if (closeButton != null)
                closeButton.interactable = session == null || !session.closeupInputLocked;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

#if UNITY_EDITOR
        public void EditorConfigure(CanvasGroup group, Image artworkImage, RectTransform interactions,
            RectTransform feedback, RectTransform exactText, RectTransform inventory, Button close,
            TMP_Text label, ChannelZeroCloseupCatalog closeupCatalog)
        {
            canvasGroup = group;
            artwork = artworkImage;
            interactionLayer = interactions;
            feedbackLayer = feedback;
            exactTextLayer = exactText;
            inventoryStrip = inventory;
            closeButton = close;
            exactTextLabel = label;
            catalog = closeupCatalog;
        }
#endif
    }
}
