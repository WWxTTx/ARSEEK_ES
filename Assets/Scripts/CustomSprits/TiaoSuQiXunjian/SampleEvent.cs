using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 自定义脚本，事件在可以面板中绑定，此处仅触发
/// </summary>
public class SampleEvent : MonoBehaviour, IBaseBehaviour
{
    bool IBaseBehaviour.UseCallback(int step) => false;

    public UnityEvent Events;
    public Type GetStatusEnumType() => null;
    void IBaseBehaviour.Execute(int step, UnityAction callback)
    {
        Events?.Invoke();
    }

    public void SetFinalState()
    {
        Events?.Invoke();
    }
}
