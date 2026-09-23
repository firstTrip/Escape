using UnityEngine;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ChannelZeroHudController : MonoBehaviour
    {
        [SerializeField] private Image[] inventorySelectionFrames;
        [SerializeField, Range(0, 6)] private int selectedSlot;

        private void Start()
        {
            RefreshSelection();
        }

        public void SelectSlot0() => SelectSlot(0);
        public void SelectSlot1() => SelectSlot(1);
        public void SelectSlot2() => SelectSlot(2);
        public void SelectSlot3() => SelectSlot(3);
        public void SelectSlot4() => SelectSlot(4);
        public void SelectSlot5() => SelectSlot(5);
        public void SelectSlot6() => SelectSlot(6);

        private void SelectSlot(int index)
        {
            selectedSlot = Mathf.Clamp(index, 0, 6);
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (inventorySelectionFrames == null)
                return;

            for (int i = 0; i < inventorySelectionFrames.Length; i++)
            {
                if (inventorySelectionFrames[i] != null)
                    inventorySelectionFrames[i].gameObject.SetActive(i == selectedSlot);
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(Image[] selectionFrames, int initialSlot)
        {
            inventorySelectionFrames = selectionFrames;
            selectedSlot = Mathf.Clamp(initialSlot, 0, 6);
            RefreshSelection();
        }
#endif
    }
}
