using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    [SerializeField] private LayerMask selectableLayer;
    private ISelectable currentlySelected;
    private Actor currentActor;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // left click
        {
            // Don't process clicks if clicking on UI!
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("[SelectionManager] Click is over UI, ignoring");
                return;
            }
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayer))
            {
                var actor = hit.collider.GetComponentInParent<Actor>();
                if (actor != null)
                {
                    // Check if this actor implements ISelectable and is actually selectable
                    if (actor is ISelectable selectable && selectable.IsSelectable)
                    {
                        Select(selectable, actor);
                    }
                    else
                    {
                        Deselect();
                    }
                }
                else
                {
                    Deselect();
                }
            }
            else
            {
                // Clicked on nothing
                Deselect();
            }
        }
    }

    private void Select(ISelectable selectable, Actor actor)
    {
        // Deselect previous if it exists
        if (currentlySelected != null)
        {
            currentlySelected.OnDeselected();
        }

        // Select new
        currentlySelected = selectable;
        currentActor = actor;
        currentlySelected.OnSelected();
        
        Debug.Log($"[SelectionManager] Selected: {actor.gameObject.name}");
    }

    private void Deselect()
    {
        if (currentlySelected != null)
        {
            currentlySelected.OnDeselected();
            currentlySelected = null;
            currentActor = null;
            
            Debug.Log("[SelectionManager] Deselected");
        }
    }

    public Actor GetCurrentlySelectedActor()
    {
        return currentActor;
    }
}