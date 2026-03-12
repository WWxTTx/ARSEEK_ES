using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 实现此接口的脚本状态过渡会互相影响
/// </summary
public interface ILinkMode
{
    /// <summary>
    /// 设置是否接受控制
    /// </summary>
    bool CanControl { get; }

    /// <summary>
    /// 设置过渡状态
    /// </summary>
    /// <param name="state"></param>
    /// <param name="instant"></param>
    void SetState(int state, bool instant);
}