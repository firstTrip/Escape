using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExaminePanelUI : MonoBehaviour, IPuzzleDocumentView
{
    public static ExaminePanelUI Instance { get; private set; }

    [SerializeField] private GameObject root;
    [SerializeField] private Image artwork;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;

    public bool IsOpen => root != null && root.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (bodyText == null)
            bodyText = transform.Find("BodyText")?.GetComponent<TMP_Text>();
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (root != null)
            root.SetActive(false);
    }

    public void Show(Sprite sprite, string text)
    {
        if (root == null) return;
        root.SetActive(true);
        if (artwork != null)
        {
            artwork.enabled = sprite != null;
            artwork.sprite = sprite;
        }
        if (bodyText != null)
            bodyText.text = text ?? "";
    }

    public void ShowDocument(Sprite artworkSprite, string body) => Show(artworkSprite, body);

    public void Close()
    {
        if (root != null)
            root.SetActive(false);
    }
}
