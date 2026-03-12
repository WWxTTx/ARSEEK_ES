using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 自定义脚本，事件在可以面板中绑定，此处仅触发
/// </summary>
public class SampleEvent : MonoBehaviour, IBaseBehaviour
{
    bool IBaseBehaviour.UseCallback(int step) => false;

    public UnityEvent FinalEvents;
    public UnityEvent ExecuteEvents;
    public Type GetStatusEnumType() => null;
    void IBaseBehaviour.Execute(int step, UnityAction callback)
    {
        ExecuteEvents.Invoke();
    }

    public void SetFinalState()
    {
        FinalEvents.Invoke();
    }
}
