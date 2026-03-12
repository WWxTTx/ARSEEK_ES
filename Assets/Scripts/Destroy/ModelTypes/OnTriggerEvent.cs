using UnityEngine;
using UnityEngine.Events;

public class OnTriggerEvent : MonoBehaviour
{
    public class ColliderEvent :UnityEvent<Collider> { }
    public ColliderEvent onCollisionEnter { get; set; } = new ColliderEvent();
    public ColliderEvent onCollisionStay { get; set; } = new ColliderEvent();
    public ColliderEvent onCollisionExit { get; set; } = new ColliderEvent();
    private void OnTriggerEnter(Collider other)
    {
        onCollisionEnter.Invoke(other);
    }
    private void OnTriggerStay(Collider other)
    {
        onCollisionStay.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        onCollisionExit.Invoke(other);
    }
}