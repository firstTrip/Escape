using UnityEngine;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ChannelZeroZoomPanelController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image artwork;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (group == null)
                group = GetComponent<CanvasGroup>();
            Close();
        }

        public void Open(Sprite sprite)
        {
            if (artwork != null)
                artwork.sprite = sprite;
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        private void SetOpen(bool open)
        {
            IsOpen = open;
            if (group == null)
                return;
            group.alpha = open ? 1f : 0f;
            group.interactable = open;
            group.blocksRaycasts = open;
        }
    }
}
