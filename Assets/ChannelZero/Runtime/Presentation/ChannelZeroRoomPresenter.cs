using System;
using ChannelZero.Runtime.Core;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ChannelZeroRoomPresenter : MonoBehaviour
    {
        private const string ToolboxOverlayPath =
            "ChannelZero/RoomOverlays/LivingRoom/2001/overlay_livingroom_toolbox_2001_visible_v01";
        private const string TubeCaseOverlayPath =
            "ChannelZero/RoomOverlays/LivingRoom/2001/overlay_livingroom_tube_case_2001_closed_v01";
        private static readonly Rect LivingPropRectTopLeft = new(0.60f, 0.61f, 0.18f, 0.17f);

        [SerializeField] private Image roomBackground;
        [SerializeField] private RectTransform hotspotLayer;
        [SerializeField] private ChannelZeroRoomCatalog catalog;

        private IChannelZeroInteractionSource[] hotspots = Array.Empty<IChannelZeroInteractionSource>();
        private ChannelZeroHotspotLayoutCatalog hotspotLayouts;
        private Image roomStateOverlay;
        private Image completionFlash;
        private Coroutine completionRoutine;

        public event Action<string> HotspotClicked;

        private void Awake()
        {
            CacheHotspots();
            hotspotLayouts = ChannelZeroHotspotLayoutCatalog.LoadDefault();
        }

        private void OnEnable()
        {
            CacheHotspots();
            foreach (IChannelZeroInteractionSource hotspot in hotspots)
                hotspot.Activated += HandleHotspotClicked;
        }

        private void OnDisable()
        {
            foreach (IChannelZeroInteractionSource hotspot in hotspots)
                hotspot.Activated -= HandleHotspotClicked;
        }

        public bool Present(ChannelZeroSessionState state)
        {
            if (state == null || roomBackground == null || catalog == null)
                return false;

            bool found = catalog.TryGetBackground(
                state.roomId, state.era, state.roomVisualStateId, out Sprite sprite);
            if (found)
                roomBackground.sprite = sprite;

            PresentStateOverlay(state);

            foreach (IChannelZeroInteractionSource hotspot in hotspots)
            {
                bool sofaChild = IsSofaChildHotspot(hotspot.InteractionId) &&
                    state.roomId == ChannelZeroIds.LivingRoom && state.era == ChannelEra.Year2001 &&
                    state.GetFlag(Chapter1Flags.SofaChildAppeared) &&
                    !state.GetFlag(Chapter1Flags.ChildSofaIntroSeen);
                bool available = sofaChild || hotspot.IsAvailable(state.roomId, state.era);
                if (available && hotspot is IChannelZeroEraLayout eraLayout)
                    available = eraLayout.ApplyEraLayout(state.era, hotspotLayouts);
                if (available)
                    available = IsProgressiveHotspotVisible(hotspot.InteractionId, state);
                hotspot.SetAvailable(available);
            }

            return found;
        }

        private void PresentStateOverlay(ChannelZeroSessionState state)
        {
            string resourcePath = string.Empty;
            bool visible = state.roomId == ChannelZeroIds.LivingRoom &&
                state.era == ChannelEra.Year2001;
            if (visible)
            {
                resourcePath = state.GetFlag("ToolboxScrewdriverTaken")
                    ? TubeCaseOverlayPath
                    : ToolboxOverlayPath;
            }

            if (!visible)
            {
                if (roomStateOverlay != null)
                    roomStateOverlay.gameObject.SetActive(false);
                return;
            }

            Sprite overlaySprite = Resources.Load<Sprite>(resourcePath);
            if (overlaySprite == null)
            {
                Debug.LogWarning($"Channel Zero room overlay missing: {resourcePath}", this);
                if (roomStateOverlay != null)
                    roomStateOverlay.gameObject.SetActive(false);
                return;
            }

            EnsureStateOverlay();
            roomStateOverlay.sprite = overlaySprite;
            roomStateOverlay.gameObject.SetActive(true);
            ApplyTopLeftRect(roomStateOverlay.rectTransform, LivingPropRectTopLeft);
        }

        private void EnsureStateOverlay()
        {
            if (roomStateOverlay != null || roomBackground == null)
                return;

            GameObject overlayObject = new("RoomStateOverlay", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(roomBackground.transform, false);
            roomStateOverlay = overlayObject.GetComponent<Image>();
            roomStateOverlay.raycastTarget = false;
            roomStateOverlay.preserveAspect = true;
            roomStateOverlay.color = Color.white;
        }

        private static void ApplyTopLeftRect(RectTransform rectTransform, Rect normalized)
        {
            rectTransform.anchorMin = new Vector2(normalized.x,
                1f - normalized.y - normalized.height);
            rectTransform.anchorMax = new Vector2(normalized.x + normalized.width,
                1f - normalized.y);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static bool IsProgressiveHotspotVisible(string interactionId,
            ChannelZeroSessionState state)
        {
            if (IsSofaChildHotspot(interactionId))
                return state.GetFlag(Chapter1Flags.SofaChildAppeared) &&
                    !state.GetFlag(Chapter1Flags.ChildSofaIntroSeen);
            if (interactionId == "Living_Toolbox")
                return state.era == ChannelEra.Year2001;
            if (interactionId == "Living_TubeCase")
                return false;
            return true;
        }

        private static bool IsSofaChildHotspot(string interactionId) =>
            interactionId == "Living_Child" || interactionId == "Living_Mina";

        private void CacheHotspots()
        {
            if (hotspotLayer != null)
                hotspots = hotspotLayer.GetComponentsInChildren<MonoBehaviour>(true)
                    .OfType<IChannelZeroInteractionSource>().ToArray();
        }

        private void HandleHotspotClicked(string logicalId)
        {
            HotspotClicked?.Invoke(logicalId);
        }

        public void PulseHotspot(string logicalId, int pulses = 1)
        {
            ChannelZeroHotspot hotspot = hotspots.OfType<ChannelZeroHotspot>()
                .FirstOrDefault(candidate => candidate.LogicalId == logicalId);
            if (hotspot == null && logicalId == "Living_Child")
                hotspot = hotspots.OfType<ChannelZeroHotspot>()
                    .FirstOrDefault(candidate => candidate.LogicalId == "Living_Mina");
            hotspot?.PulseHighlight(pulses > 1 ? 1.25f : 0.9f, pulses);
        }

        public void FlashCompletion(Color tint, float duration = 0.55f)
        {
            if (roomBackground == null)
                return;
            if (completionFlash == null)
            {
                GameObject flashObject = new("ChapterCompletionFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                flashObject.transform.SetParent(roomBackground.transform.parent, false);
                RectTransform rect = flashObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                completionFlash = flashObject.GetComponent<Image>();
                completionFlash.raycastTarget = false;
                completionFlash.color = new Color(tint.r, tint.g, tint.b, 0f);
                flashObject.transform.SetSiblingIndex(roomBackground.transform.GetSiblingIndex() + 1);
            }
            if (completionRoutine != null)
                StopCoroutine(completionRoutine);
            completionRoutine = StartCoroutine(FlashCompletionRoutine(tint, duration));
        }

        private IEnumerator FlashCompletionRoutine(Color tint, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                tint.a = Mathf.Sin(normalized * Mathf.PI) * 0.18f;
                completionFlash.color = tint;
                yield return null;
            }
            tint.a = 0f;
            completionFlash.color = tint;
            completionRoutine = null;
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
