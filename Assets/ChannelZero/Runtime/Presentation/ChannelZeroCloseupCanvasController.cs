using System;
using System.Collections.Generic;
using ChannelZero.Runtime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ChannelZeroCloseupCanvasController : MonoBehaviour, IChannelZeroPuzzleView
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private CanvasGroup hudChromeGroup;
        [SerializeField] private TMP_Text hudStatusLabel;
        [SerializeField] private Image artwork;
        [SerializeField] private RectTransform interactionLayer;
        [SerializeField] private RectTransform feedbackLayer;
        [SerializeField] private RectTransform exactTextLayer;
        [SerializeField] private RectTransform inventoryStrip;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text exactTextLabel;
        [SerializeField] private TMP_Text closeButtonLabel;
        [SerializeField] private ChannelZeroLocalizedText closeButtonText =
            new("ui.closeup.close", "닫기");
        [Header("Inspector-positioned click text")]
        [SerializeField] private TMP_Text selectedItemDescriptionLabel;
        [SerializeField] private TMP_Text transientFeedbackLabel;
        [SerializeField] private TMP_FontAsset koreanFont;
        [SerializeField] private ChannelZeroCloseupCatalog catalog;
        [SerializeField] private string locale = "ko-KR";
        [SerializeField] private Chapter1TvCloseupLayerStack tvLayerStack;
        [SerializeField] private Color missingArtworkColor = new(0.16f, 0.14f, 0.12f, 1f);

        private ChannelZeroSessionState session;
        private string activeCloseupId;
        private Func<string, string> itemDescriptionResolver;
        private Func<string, string, string> localizedTextResolver;
        private RectTransform stateOverlayRoot;
        private readonly List<Image> stateOverlayImages = new();

        public bool IsOpen => canvasGroup != null && canvasGroup.blocksRaycasts;
        public string Locale => locale;
        public TMP_Text NarrativeLabel => exactTextLabel;
        public TMP_Text CloseButtonLabel => closeButtonLabel;
        public event Action Closed;
        public event Action<string> PuzzleActionRequested;

        public void ConfigureInventory(Func<string, string> descriptionResolver)
        {
            itemDescriptionResolver = descriptionResolver;
        }

        public void ConfigureLocalization(string localeCode, Func<string, string, string> resolver)
        {
            locale = string.IsNullOrWhiteSpace(localeCode) ? "ko-KR" : localeCode;
            localizedTextResolver = resolver;
            RefreshLocalizedChrome();
        }

        private void Awake()
        {
            tvLayerStack ??= GetComponent<Chapter1TvCloseupLayerStack>();
            if (tvLayerStack == null)
                tvLayerStack = gameObject.AddComponent<Chapter1TvCloseupLayerStack>();
            tvLayerStack.Configure(artwork);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            closeButtonLabel ??= closeButton != null
                ? closeButton.GetComponentInChildren<TMP_Text>(true)
                : null;
            RefreshLocalizedChrome();
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
            OpenLocalized(state, closeupId, artworkStateId, ChannelZeroLocalizedText.Literal(exactText));
        }

        public void OpenLocalized(ChannelZeroSessionState state, string closeupId,
            string artworkStateId, ChannelZeroLocalizedText exactText)
        {
            session = state;
            session?.EnterCloseup(closeupId, artworkStateId);
            activeCloseupId = closeupId;
            Present(closeupId, artworkStateId, Resolve(exactText));
            if (closeupId == ChannelZeroIds.LivingCrtFrontCloseup &&
                !(session?.GetFlag(Chapter1Flags.UnpluggedTVObserved) ?? false))
                tvLayerStack?.PlayGlitch(null);
        }

        public void Restore(ChannelZeroSessionState state, string exactText = "")
        {
            if (state == null || string.IsNullOrWhiteSpace(state.activeCloseupId))
                return;
            session = state;
            activeCloseupId = state.activeCloseupId;
            Present(state.activeCloseupId, state.activeCloseupStateId, exactText);
        }

        public void PresentPuzzle(PuzzleView view)
        {
            if (view == null)
                return;

            if (session != null)
                session.activeCloseupStateId = view.artworkStateId;
            PresentArtwork(activeCloseupId, view.artworkStateId);
            if (exactTextLabel != null)
            {
                string title = Resolve(new ChannelZeroLocalizedText(view.titleKey, view.title));
                string body = Resolve(new ChannelZeroLocalizedText(view.bodyKey, view.body));
                exactTextLabel.text = $"<b>{title}</b>\n{body}";
            }
            RebuildPuzzleControls(view);
            RebuildInventory();
        }

        public void SetInputLocked(bool locked)
        {
            if (session != null)
                session.closeupInputLocked = locked;
            if (closeButton != null)
                closeButton.interactable = !locked;
            if (locked && interactionLayer != null)
            {
                foreach (Button button in interactionLayer.GetComponentsInChildren<Button>(true))
                    button.interactable = false;
            }
        }

        public void Close()
        {
            if (session != null && !session.TryExitCloseup())
                return;
            ClearChildren(interactionLayer);
            ClearFeedbackText();
            ClearChildren(inventoryStrip);
            activeCloseupId = string.Empty;
            tvLayerStack?.Hide();
            HideStateOverlays();
            SetVisible(false);
            Closed?.Invoke();
        }

        public void DismissForSequence()
        {
            session?.TryExitCloseup();
            ClearChildren(interactionLayer);
            ClearFeedbackText();
            ClearChildren(inventoryStrip);
            activeCloseupId = string.Empty;
            tvLayerStack?.Hide();
            HideStateOverlays();
            SetVisible(false);
        }

        private void Present(string closeupId, string artworkStateId, string exactText)
        {
            PresentArtwork(closeupId, artworkStateId);
            if (exactTextLabel != null)
                exactTextLabel.text = exactText;
            if (closeButton != null)
                closeButton.interactable = session == null || !session.closeupInputLocked;
            SetVisible(true);
        }

        private void PresentArtwork(string closeupId, string artworkStateId)
        {
            HideStateOverlays();
            if (closeupId == ChannelZeroIds.LivingCrtFrontCloseup && tvLayerStack != null &&
                !(session?.GetFlag(Chapter1Flags.EndingRecognitionActive) ?? false))
            {
                Chapter1TvDisplayState displayState = artworkStateId switch
                {
                    "off" => Chapter1TvDisplayState.Off,
                    "glitch" => Chapter1TvDisplayState.Glitch,
                    _ => session != null && session.GetFlag(Chapter1Flags.ChildSofaIntroSeen)
                        ? Chapter1TvDisplayState.Off
                        : Chapter1TvDisplayState.Noise,
                };
                if (tvLayerStack.Show(displayState)) return;
            }
            tvLayerStack?.Hide();
            Sprite sprite = null;
            ChannelEra era = session?.era ?? ChannelEra.Year2001;
            bool hasArtwork = Chapter1RebuildResources.TryLoadCloseupOverride(session, closeupId, out sprite);
            if (!hasArtwork)
                hasArtwork = catalog != null && catalog.TryGetArtwork(closeupId, artworkStateId, era, out sprite);
            if (!hasArtwork && catalog != null)
                hasArtwork = catalog.TryGetArtwork(closeupId, ChannelZeroIds.DefaultVisualState, era, out sprite);
            if (artwork == null)
                return;
            artwork.sprite = hasArtwork ? sprite : null;
            artwork.color = hasArtwork ? Color.white : missingArtworkColor;
            if (hasArtwork)
                PresentStateOverlays(closeupId);
        }

        private void PresentStateOverlays(string closeupId)
        {
            Sprite[] overlays = Chapter1RebuildResources.LoadCloseupOverlays(session, closeupId);
            if (overlays.Length == 0 || artwork == null)
                return;
            EnsureStateOverlayRoot();
            stateOverlayRoot.gameObject.SetActive(true);
            for (int i = 0; i < overlays.Length; i++)
            {
                Image image;
                if (i < stateOverlayImages.Count)
                    image = stateOverlayImages[i];
                else
                {
                    GameObject layer = new("Chapter1StateOverlay_" + i,
                        typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    layer.transform.SetParent(stateOverlayRoot, false);
                    image = layer.GetComponent<Image>();
                    Stretch(image.rectTransform);
                    image.raycastTarget = false;
                    stateOverlayImages.Add(image);
                }
                image.sprite = overlays[i];
                image.color = Color.white;
                image.preserveAspect = false;
                image.gameObject.SetActive(true);
            }
        }

        private void EnsureStateOverlayRoot()
        {
            if (stateOverlayRoot != null)
                return;
            GameObject root = new("Chapter1StateOverlays", typeof(RectTransform));
            root.transform.SetParent(artwork.transform.parent, false);
            stateOverlayRoot = root.GetComponent<RectTransform>();
            Stretch(stateOverlayRoot);
            stateOverlayRoot.SetSiblingIndex(artwork.transform.GetSiblingIndex() + 1);
            root.SetActive(false);
        }

        private void HideStateOverlays()
        {
            foreach (Image image in stateOverlayImages)
                if (image != null) image.gameObject.SetActive(false);
            if (stateOverlayRoot != null)
                stateOverlayRoot.gameObject.SetActive(false);
        }

        private string Resolve(ChannelZeroLocalizedText localized)
        {
            return localizedTextResolver?.Invoke(localized.Key, localized.Fallback) ?? localized.Fallback;
        }

        private void RefreshLocalizedChrome()
        {
            if (closeButtonLabel != null)
                closeButtonLabel.text = Resolve(closeButtonText);
        }

        private void RebuildPuzzleControls(PuzzleView view)
        {
            ClearChildren(interactionLayer);
            ClearFeedbackText();

            const int columns = 5;
            for (int i = 0; i < view.actions.Count; i++)
            {
                PuzzleActionView action = view.actions[i];
                int row = i / columns;
                int column = i % columns;
                int actionsInRow = Mathf.Min(columns, view.actions.Count - row * columns);
                GameObject buttonObject = new("PuzzleAction_" + action.actionId, typeof(RectTransform), typeof(Image), typeof(Button), typeof(ChannelZeroPuzzleActionButton));
                RectTransform rect = buttonObject.GetComponent<RectTransform>();
                rect.SetParent(interactionLayer, false);
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(200f, 54f);
                rect.anchoredPosition = new Vector2((column - (actionsInRow - 1) * 0.5f) * 214f, 18f + row * 62f);
                IPuzzleActionView actionView = buttonObject.GetComponent<ChannelZeroPuzzleActionButton>();
                string labelKey = string.IsNullOrWhiteSpace(action.labelKey)
                    ? $"puzzle.{view.puzzleId}.action.{action.actionId}"
                    : action.labelKey;
                string localizedLabel = Resolve(new ChannelZeroLocalizedText(labelKey, action.label));
                PuzzleActionView localizedAction = new(action.actionId, localizedLabel,
                    action.interactable, labelKey);
                actionView.Bind(localizedAction, koreanFont, actionId => PuzzleActionRequested?.Invoke(actionId));
            }
        }

        private void RebuildInventory()
        {
            ClearChildren(inventoryStrip);
            SetTextVisible(selectedItemDescriptionLabel, false);
            if (session == null)
                return;

            if (!string.IsNullOrWhiteSpace(session.selectedInventoryItemId))
            {
                string description = itemDescriptionResolver?.Invoke(session.selectedInventoryItemId) ?? string.Empty;
                SetText(selectedItemDescriptionLabel, description);
            }
        }

        public void RefreshInventory() => RebuildInventory();

        public void ShowFeedback(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;
            SetText(transientFeedbackLabel, message);
        }

        private void ClearFeedbackText()
        {
            SetTextVisible(selectedItemDescriptionLabel, false);
            SetTextVisible(transientFeedbackLabel, false);
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label == null)
                return;
            label.text = value;
            SetTextVisible(label, !string.IsNullOrWhiteSpace(value));
        }

        private static void SetTextVisible(TMP_Text label, bool visible)
        {
            if (label == null)
                return;
            if (!visible)
                label.text = string.Empty;
            label.gameObject.SetActive(visible);
        }

        private TMP_Text CreateRuntimeText(string name, Transform parent)
        {
            GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            if (koreanFont != null)
                text.font = koreanFont;
            text.color = Color.white;
            return text;
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null)
                return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            if (hudChromeGroup != null)
            {
                // The CRT frame owns the canonical inventory slots. Keep the chrome raycastable
                // during closeups so those slots remain usable; world/operation input is gated by
                // ChannelZeroVerticalSliceController while the closeup is open.
                hudChromeGroup.interactable = true;
                hudChromeGroup.blocksRaycasts = true;
            }
            if (hudStatusLabel != null)
                hudStatusLabel.enabled = !visible;
        }

#if UNITY_EDITOR
        public void EditorConfigure(CanvasGroup group, CanvasGroup chromeGroup, TMP_Text chromeStatusLabel,
            Image artworkImage, RectTransform interactions,
            RectTransform feedback, RectTransform exactText, RectTransform inventory, Button close,
            TMP_Text label, TMP_Text closeLabel, TMP_Text selectedItemDescription, TMP_Text transientFeedback,
            TMP_FontAsset font, ChannelZeroCloseupCatalog closeupCatalog)
        {
            canvasGroup = group;
            hudChromeGroup = chromeGroup;
            hudStatusLabel = chromeStatusLabel;
            artwork = artworkImage;
            interactionLayer = interactions;
            feedbackLayer = feedback;
            exactTextLayer = exactText;
            inventoryStrip = inventory;
            closeButton = close;
            exactTextLabel = label;
            closeButtonLabel = closeLabel;
            selectedItemDescriptionLabel = selectedItemDescription;
            transientFeedbackLabel = transientFeedback;
            koreanFont = font;
            catalog = closeupCatalog;
        }
#endif
    }

}
