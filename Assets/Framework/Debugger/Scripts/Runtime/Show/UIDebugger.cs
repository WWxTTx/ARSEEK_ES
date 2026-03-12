using UnityEngine;
using UnityEngine.UI;

namespace UnityFramework.Runtime
{
    public class UIDebugger : UIPanelBase
    {
        /// <summary>
        /// 框架内调试信息UI预制体路径
        /// </summary>
        public const string SystemUIDebuggerPath = "Prefabs/";

        /// <summary>
        /// 日志模块控制开关
        /// </summary>
        public Toggle Console;
        /// <summary>
        /// 性能模块控制开关
        /// </summary>
        public Toggle Profiler;
        /// <summary>
        /// 设备信息模块控制开关
        /// </summary>
        public Toggle Information;

        public Toggle Upload;

        /// <summary>
        /// 关闭界面按钮
        /// </summary>
        public Button close;
        /// <summary>
        /// 模块显示容器
        /// </summary>
        public Transform content;

        public override void Open(UIData uiData = null)
        {
            base.Open(uiData);
            Console.onValueChanged.AddListener(OnValueChanged_Logs);
            Profiler.onValueChanged.AddListener(OnValueChanged_Profiler);
            Information.onValueChanged.AddListener(OnValueChanged_Information);

            Upload.onValueChanged.AddListener(OnValueChanged_Upload);
            Upload.gameObject.SetActive(ApiData.state == 0);
            Upload.SetIsOnWithoutNotify(DebuggerSave.UploadEnabled);

            close.onClick.AddListener(() => UIManager.Instance.CloseUI<UIDebugger>());

            OnValueChanged_Logs(true);
        }

        /// <summary>
        /// 调试面板开关
        /// </summary>
        void OnValueChanged_Logs(bool isOn)
        {
            if (isOn)
                UIManager.Instance.OpenModuleUI<UILogModule>(this, content,null,SystemUIDebuggerPath);
            else
                UIManager.Instance.HideModuleUI<UILogModule>(this);
        }

        /// <summary>
        /// 性能面板开关
        /// </summary>
        void OnValueChanged_Profiler(bool isOn)
        {
            if (isOn)
                UIManager.Instance.OpenModuleUI<UIProfilerModule>(this, content, null, SystemUIDebuggerPath);
             else
                UIManager.Instance.HideModuleUI<UIProfilerModule>(this);
        }

        /// <summary>
        /// 设备信息面板开关
        /// </summary>
        void OnValueChanged_Information(bool isOn)
        {
            if (isOn)
                UIManager.Instance.OpenModuleUI<UIInformationModule>(this, content, null, SystemUIDebuggerPath);
            else
                UIManager.Instance.HideModuleUI<UIInformationModule>(this);
        }

        /// <summary>
        /// 日志上传功能开关
        /// </summary>
        void OnValueChanged_Upload(bool isOn)
        {
            DebuggerSave.UploadEnabled = isOn;
        }
    }
}
