using Unity.VisualScripting;
using UnityEngine;

public class CursorController : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera mainCamera;

    [Header("Raycast")]
    [SerializeField] private LayerMask rayerMask;
    [SerializeField] private float rayDistance = 100f;

    private IInteractable curInteractable;
    private IHoverable curHoverable;
    private Collider currentCollider;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        DetectInteractable();

        if (Input.GetMouseButtonDown(0))
            TryInterative();
    }

    private void DetectInteractable()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit , rayDistance, rayerMask))
        {
            if (currentCollider == hit.collider)
                return;

            ClearCurrentTarget();

            currentCollider = hit.collider;

            hit.collider.TryGetComponent(out curInteractable);
            hit.collider.TryGetComponent(out curHoverable);

            curHoverable?.OnHoverEnter();
            return;
        }

        ClearCurrentTarget();
    }

    private void ClearCurrentTarget()
    {
        curHoverable?.OnHoverExit();

        currentCollider = null;
        curInteractable = null;
        curHoverable = null;
    }

    private void TryInterative()
    {
        curInteractable?.Interact();
    }
}
