using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class BaseMasterComputer : MonoBase
{
    private EventTrigger BottomTrigger;
    private Button Background;

    /// <summary>
    /// ¸æ¾¯
    /// </summary>
    public Toggle WarnToggle { get; protected set; }

    protected override void InitComponents()
    {
        base.InitComponents();
        AddMsg((ushort)SmallFlowModuleEvent.MasterComputerOperate);

        BottomTrigger = transform.FindChildByName("BottomTrigger").AutoComponent<EventTrigger>();
        BottomTrigger.AddEvent(EventTriggerType.PointerEnter, (eventData) =>
        {
            SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.ShowTool, true));
        });

        Background = transform.GetComponentByChildName<Button>("Background");
        Background.onClick.AddListener(() =>
        {
            SendMsg(new MsgBool((ushort)SmallFlowModuleEvent.ShowTool, false));
        });
    }

    public void SetParent(Transform parent, int index)
    {
        transform.SetParent(parent);
        transform.SetSiblingIndex(index);

        transform.GetComponent<RectTransform>().offsetMin = Vector3.zero;
        transform.GetComponent<RectTransform>().offsetMax = Vector3.zero;
    }
}
