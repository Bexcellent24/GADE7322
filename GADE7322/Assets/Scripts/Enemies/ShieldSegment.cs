using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ShieldSegment : MonoBehaviour
{
    FrontalShield _controller;

    void Reset()
    {
        var bc = GetComponent<BoxCollider>();
        bc.isTrigger = true;
    }

    void Awake()
    {
        _controller = GetComponentInParent<FrontalShield>();
        var bc = GetComponent<BoxCollider>();
        bc.isTrigger = true;

        var rb = _controller ? _controller.GetComponent<Rigidbody>() : null;
        if (!_controller || !rb)
            Debug.LogWarning("[ShieldSegment] Parent ShieldController with kinematic Rigidbody is recommended.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (_controller) _controller.HandleHit(other);
    }
}