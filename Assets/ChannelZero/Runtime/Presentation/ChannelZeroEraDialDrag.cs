using UnityEngine;
using UnityEngine.EventSystems;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ChannelZeroEraDialDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private ChannelZeroVerticalSliceController controller;
        [SerializeField, Min(24f)] private float stepThreshold = 72f;

        private float accumulatedHorizontalDelta;

        public void OnBeginDrag(PointerEventData eventData)
        {
            accumulatedHorizontalDelta = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (controller == null)
                return;

            accumulatedHorizontalDelta += eventData.delta.x;
            while (Mathf.Abs(accumulatedHorizontalDelta) >= stepThreshold)
            {
                if (accumulatedHorizontalDelta < 0f)
                {
                    controller.TunePast();
                    accumulatedHorizontalDelta += stepThreshold;
                }
                else
                {
                    controller.TuneFuture();
                    accumulatedHorizontalDelta -= stepThreshold;
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            accumulatedHorizontalDelta = 0f;
        }

#if UNITY_EDITOR
        public void EditorConfigure(ChannelZeroVerticalSliceController targetController, float threshold)
        {
            controller = targetController;
            stepThreshold = Mathf.Max(24f, threshold);
        }
#endif
    }
}
