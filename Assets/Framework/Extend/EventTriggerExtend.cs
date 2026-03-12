using UnityEngine.Events;
using UnityEngine.EventSystems;

public static class EventTriggerExtend
{
    /// <summary>
    /// 添加事件
    /// </summary>
    /// <param name="self">组件</param>
    /// <param name="eventType">事件类型</param>
    /// <param name="callBack">回调</param>
    public static void AddEvent(this EventTrigger self, EventTriggerType eventType,UnityAction<BaseEventData> callBack)
    {
        if (self.triggers.Count != 0)
        {
            for (int i = 0; i < self.triggers.Count; i++)
            {
                if (self.triggers[i].eventID == eventType)
                {
                    self.triggers[i].callback.AddListener(callBack);
                    return;
                }
            }
        }
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener(callBack);
        self.triggers.Add(entry);
    }
}
