using UnityEngine;

public class Door : MonoBehaviour, IInteractable, IHoverable
{
    public void Interact()
    {
        Debug.Log("Interact");
    }

    public void OnHoverEnter()
    {
        Debug.Log("HoverEnter");
    }

    public void OnHoverExit()
    {
        Debug.Log("HoverExit");
    }
}
