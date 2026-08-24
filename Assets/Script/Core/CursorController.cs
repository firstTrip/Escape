using UnityEngine;

public class CursorController : MonoBehaviour
{
    public static CursorController Instance { get; private set; }

    [SerializeField] private Sprite defaultCursor;
    [SerializeField] private Sprite hoverCursor;
    [SerializeField] private Vector2 hotspot = Vector2.zero;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetHover(false);
    }

    public void SetHover(bool hovering)
    {
        Sprite sprite = hovering ? hoverCursor : defaultCursor;
        if (sprite == null || sprite.texture == null) return;
        Cursor.SetCursor(sprite.texture, hotspot, CursorMode.Auto);
    }
}
