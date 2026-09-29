using System;
using ChannelZero.Runtime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ChannelZeroHudController : MonoBehaviour
    {
        [SerializeField] private Image[] inventorySelectionFrames;
        [SerializeField] private Button[] inventorySlots;
        [SerializeField] private TMP_Text[] inventoryLabels;
        [SerializeField] private TMP_FontAsset koreanFont;
        [SerializeField, Range(0, 6)] private int selectedSlot;

        private ChannelZeroSessionState session;
        private Func<string, string> itemNameResolver;
        private Action<string> itemSelected;

        private void Start()
        {
            ResolveInventoryUi();
            RefreshInventory();
            RefreshSelection();
        }

        public void BindInventory(ChannelZeroSessionState state, Func<string, string> nameResolver,
            Action<string> onItemSelected)
        {
            session = state;
            itemNameResolver = nameResolver;
            itemSelected = onItemSelected;
            ResolveInventoryUi();
            RefreshInventory();
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
            if (session != null && selectedSlot < session.inventoryItemIds.Count)
                itemSelected?.Invoke(session.inventoryItemIds[selectedSlot]);
            RefreshInventory();
            RefreshSelection();
        }

        public void RefreshInventory()
        {
            ResolveInventoryUi();
            int itemCount = session?.inventoryItemIds?.Count ?? 0;
            for (int i = 0; i < 7; i++)
            {
                bool occupied = i < itemCount;
                if (inventorySlots != null && i < inventorySlots.Length && inventorySlots[i] != null)
                    inventorySlots[i].interactable = occupied;
                if (inventoryLabels == null || i >= inventoryLabels.Length || inventoryLabels[i] == null)
                    continue;

                TMP_Text label = inventoryLabels[i];
                label.gameObject.SetActive(occupied);
                label.text = occupied
                    ? itemNameResolver?.Invoke(session.inventoryItemIds[i]) ?? session.inventoryItemIds[i]
                    : string.Empty;
            }
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (inventorySelectionFrames == null)
                return;

            for (int i = 0; i < inventorySelectionFrames.Length; i++)
            {
                if (inventorySelectionFrames[i] != null)
                {
                    bool selected = session != null
                        && i < session.inventoryItemIds.Count
                        && session.inventoryItemIds[i] == session.selectedInventoryItemId;
                    inventorySelectionFrames[i].gameObject.SetActive(selected);
                }
            }
        }

        private void ResolveInventoryUi()
        {
            if (inventorySlots == null || inventorySlots.Length != 7)
                inventorySlots = new Button[7];
            if (inventoryLabels == null || inventoryLabels.Length != 7)
                inventoryLabels = new TMP_Text[7];

            for (int i = 0; i < 7; i++)
            {
                Transform slotTransform = transform.Find($"InventorySlot_{i}");
                if (slotTransform == null)
                    continue;
                inventorySlots[i] ??= slotTransform.GetComponent<Button>();
                inventoryLabels[i] ??= slotTransform.Find("ItemLabel")?.GetComponent<TMP_Text>();
                if (inventoryLabels[i] == null)
                    inventoryLabels[i] = CreateItemLabel(slotTransform);
            }
        }

        private TMP_Text CreateItemLabel(Transform parent)
        {
            GameObject labelObject = new("ItemLabel", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.font = koreanFont != null ? koreanFont : TMP_Settings.defaultFontAsset;
            label.fontSize = 17f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 11f;
            label.fontSizeMax = 17f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.94f, 0.82f, 0.58f, 1f);
            label.raycastTarget = false;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(7f, 7f);
            rect.offsetMax = new Vector2(-7f, -7f);
            return label;
        }

#if UNITY_EDITOR
        public void EditorConfigure(Image[] selectionFrames, Button[] slots, TMP_Text[] labels,
            TMP_FontAsset font, int initialSlot)
        {
            inventorySelectionFrames = selectionFrames;
            inventorySlots = slots;
            inventoryLabels = labels;
            koreanFont = font;
            selectedSlot = Mathf.Clamp(initialSlot, 0, 6);
            RefreshSelection();
        }
#endif
    }
}
