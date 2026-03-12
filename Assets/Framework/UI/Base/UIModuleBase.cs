using UnityEngine;
using UnityEngine.Events;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// UI模块类（不常驻，类似弹窗等UI）
    /// </summary>
    public class UIModuleBase : UIBase
    {
        /// <summary>
        /// 父级面板，可在Open时动态修改,在Show函数及之后调用
        /// </summary>
        [HideInInspector]
        public UIPanelBase ParentPanel;

        public override void Hide(UIData uiData = null, UnityAction callback = null)
        {
            closeDelegate?.Invoke();
            base.Hide(uiData, callback);
        }

        public override void Close(UIData uiData = null, UnityAction callback = null)
        {
            closeDelegate?.Invoke();
            base.Close(uiData, callback);
        }

        #region 委托
        public delegate void ModuleDelegate();
        /// <summary>
        /// 关闭委托
        /// </summary>
        public ModuleDelegate closeDelegate;
        #endregion
    }
}
