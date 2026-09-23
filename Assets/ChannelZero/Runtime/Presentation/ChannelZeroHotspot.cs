using System;
using ChannelZero.Runtime.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class ChannelZeroHotspot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private string logicalId;
        [SerializeField] private string roomId;
        [SerializeField] private ChannelEra[] allowedEras = Array.Empty<ChannelEra>();
        [SerializeField] private bool interactable = true;

        private Image hitImage;

        public string LogicalId => logicalId;
        public string RoomId => roomId;
        public event Action<string> Clicked;

        private void Awake()
        {
            hitImage = GetComponent<Image>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (interactable)
                Clicked?.Invoke(logicalId);
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
