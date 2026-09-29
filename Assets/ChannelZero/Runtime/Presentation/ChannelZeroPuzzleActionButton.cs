using System;
using ChannelZero.Runtime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [RequireComponent(typeof(RectTransform), typeof(Image), typeof(Button))]
    public sealed class ChannelZeroPuzzleActionButton : MonoBehaviour, IPuzzleActionView
    {
        public string ActionId { get; private set; }

        public void Bind(PuzzleActionView action, TMP_FontAsset font, Action<string> selected)
        {
            ActionId = action.actionId;
            gameObject.name = "PuzzleAction_" + ActionId;

            Image image = GetComponent<Image>();
            image.color = action.interactable
                ? new Color(0.055f, 0.065f, 0.065f, 0.98f)
                : new Color(0.035f, 0.035f, 0.035f, 0.84f);

            Outline outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
            outline.effectColor = action.interactable
                ? new Color(0.72f, 0.5f, 0.18f, 0.88f)
                : new Color(0.24f, 0.22f, 0.18f, 0.55f);
            outline.effectDistance = new Vector2(1f, -1f);

            Button button = GetComponent<Button>();
            button.interactable = action.interactable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selected?.Invoke(ActionId));

            TMP_Text label = GetComponentInChildren<TMP_Text>(true);
            if (label == null)
                label = CreateLabel(font);
            else if (font != null)
                label.font = font;
            label.fontSize = 21f;
            label.alignment = TextAlignmentOptions.Center;
            label.text = action.label;
            label.raycastTarget = false;
        }

        private TMP_Text CreateLabel(TMP_FontAsset font)
        {
            GameObject textObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TMP_Text label = textObject.GetComponent<TMP_Text>();
            if (font != null)
                label.font = font;
            label.color = Color.white;
            return label;
        }
    }
}
