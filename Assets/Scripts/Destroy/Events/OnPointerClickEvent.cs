public class OnPointerClickEvent : UnityEngine.MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    public UnityEngine.Events.UnityEvent onPointerClick = new UnityEngine.Events.UnityEvent();

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        onPointerClick.Invoke();
    }
}