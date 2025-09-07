using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    [SerializeField] private LayerMask selectableLayer;
    private RangeIndicator currentIndicator;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // left click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, selectableLayer))
            {
                var actor = hit.collider.GetComponentInParent<Actor>();
                if (actor != null)
                {
                    Select(actor);
                }
            }
            else
            {
                Deselect();
            }
        }
    }

    private void Select(Actor actor)
    {
        if (currentIndicator != null)
            currentIndicator.Hide();

        currentIndicator = actor.GetComponentInChildren<RangeIndicator>();
        if (currentIndicator != null)
            currentIndicator.Show();
    }

    private void Deselect()
    {
        if (currentIndicator != null)
        {
            currentIndicator.Hide();
            currentIndicator = null;
        }
    }
}

