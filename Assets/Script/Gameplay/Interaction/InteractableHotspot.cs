using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public abstract class InteractableHotspot : MonoBehaviour, IInteractable, IHoverable, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private bool interactable = true;

    public bool Interactable
    {
        get => interactable;
        set => interactable = value;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable) return;
        Interact();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;
        OnHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit();
    }

    public void Interact()
    {
        if (!interactable) return;
        OnInteract();
    }

    public virtual void OnHoverEnter()
    {
        CursorController.Instance?.SetHover(true);
    }

    public virtual void OnHoverExit()
    {
        CursorController.Instance?.SetHover(false);
    }

    protected abstract void OnInteract();
}
