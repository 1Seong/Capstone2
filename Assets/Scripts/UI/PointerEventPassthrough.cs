using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointerEventPassthrough : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, 
    IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerEnter(PointerEventData eventData) => PassEvent(eventData, ExecuteEvents.pointerEnterHandler);
    public void OnPointerExit(PointerEventData eventData)  => PassEvent(eventData, ExecuteEvents.pointerExitHandler);
    public void OnPointerDown(PointerEventData eventData)  => PassEvent(eventData, ExecuteEvents.pointerDownHandler);
    public void OnPointerUp(PointerEventData eventData)    => PassEvent(eventData, ExecuteEvents.pointerUpHandler);

    void PassEvent<T>(PointerEventData eventData, ExecuteEvents.EventFunction<T> function)
        where T : IEventSystemHandler
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var result in results)
        {
            if (result.gameObject == gameObject) continue; // 자기 자신 제외
            ExecuteEvents.Execute(result.gameObject, eventData, function);
        }
    }
}
