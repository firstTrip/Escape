using System;
using System.Collections;
using ChannelZero.Runtime.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class ChannelZeroHotspot : MonoBehaviour, IPointerClickHandler,
        IChannelZeroInteractionSource, IChannelZeroEraLayout
    {
        [SerializeField] private string logicalId;
        [SerializeField] private string roomId;
        [SerializeField] private ChannelEra[] allowedEras = Array.Empty<ChannelEra>();
        [SerializeField] private bool interactable = true;

        private Image hitImage;
        private Outline highlightOutline;
        private Coroutine highlightRoutine;

        public string LogicalId => logicalId;
        public string InteractionId => logicalId;
        public string RoomId => roomId;
        public event Action<string> Activated;

        private void Awake()
        {
            hitImage = GetComponent<Image>();
        }

        private void OnDisable()
        {
            if (highlightOutline != null)
            {
                Color color = highlightOutline.effectColor;
                color.a = 0f;
                highlightOutline.effectColor = color;
            }
            highlightRoutine = null;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (interactable)
                Activated?.Invoke(logicalId);
        }

        public bool IsAvailable(string currentRoomId, ChannelEra currentEra)
        {
            if (roomId != currentRoomId)
                return false;
            if (allowedEras == null || allowedEras.Length == 0)
                return true;
            return Array.IndexOf(allowedEras, currentEra) >= 0;
        }

        public void SetAvailable(bool available)
        {
            interactable = available;
            hitImage ??= GetComponent<Image>();
            hitImage.raycastTarget = available;
            gameObject.SetActive(available);
        }

        public void PulseHighlight(float duration = 0.9f, int pulses = 1)
        {
            if (!gameObject.activeInHierarchy)
                return;
            highlightOutline ??= gameObject.GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            highlightOutline.effectDistance = new Vector2(3f, -3f);
            highlightOutline.useGraphicAlpha = false;
            if (highlightRoutine != null)
                StopCoroutine(highlightRoutine);
            highlightRoutine = StartCoroutine(PulseHighlightRoutine(duration, Mathf.Max(1, pulses)));
        }

        private IEnumerator PulseHighlightRoutine(float duration, int pulses)
        {
            float elapsed = 0f;
            Color brass = new(0.82f, 0.55f, 0.18f, 0f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                brass.a = Mathf.Max(0f, Mathf.Sin(normalized * Mathf.PI * pulses)) * 0.58f;
                highlightOutline.effectColor = brass;
                yield return null;
            }
            brass.a = 0f;
            highlightOutline.effectColor = brass;
            highlightRoutine = null;
        }

        public bool ApplyEraLayout(ChannelEra era, ChannelZeroHotspotLayoutCatalog catalog)
        {
            if (catalog == null || !catalog.TryResolve(logicalId, era, out HotspotLayoutDefinition layout))
                return true;

            RectTransform rect = (RectTransform)transform;
            Rect normalized = layout.NormalizedTopLeftRect;
            rect.anchorMin = new Vector2(normalized.x, 1f - normalized.y - normalized.height);
            rect.anchorMax = new Vector2(normalized.x + normalized.width, 1f - normalized.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return layout.enabled;
        }

#if UNITY_EDITOR
        public void EditorConfigure(string nextLogicalId, string nextRoomId, params ChannelEra[] eras)
        {
            logicalId = nextLogicalId;
            roomId = nextRoomId;
            allowedEras = eras ?? Array.Empty<ChannelEra>();
        }
#endif
    }
}
