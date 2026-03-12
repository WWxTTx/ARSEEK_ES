using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// ЖЏЛ­екеж
/// </summary>
public class AnimMaskPanel : UIPanelBase
{
    private GameObject AnimMask;

    public override void Open(UIData uiData = null)
    {
        base.Open(uiData);

        AddMsg(new ushort[]
        {
            (ushort)UIAnimEvent.ShowAnimMask,
            (ushort)UIAnimEvent.HideAnimMask
        });

        AnimMask = transform.GetChild(0).gameObject;
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)UIAnimEvent.ShowAnimMask:
                AnimMask.SetActive(true);
                break;
            case (ushort)UIAnimEvent.HideAnimMask:
                AnimMask.SetActive(false);
                break;
        }
    }
}
