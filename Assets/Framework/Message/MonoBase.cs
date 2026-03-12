using UnityEngine;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 项目UI和3d对象脚本的基类,封装框架内的消息函数
    /// </summary>
    public class MonoBase : MonoBehaviour
    {
        /// <summary>
        /// 已绑定的消息集合
        /// </summary>
        [HideInInspector]
        public ushort[] msgIds;

        protected virtual void Start()
        {
            InitComponents();
        }

        /// <summary>
        /// 组件相关初始化,Start中调用
        /// </summary>
        protected virtual void InitComponents()
        {

        }

        /// <summary>
        /// 添加消息绑定
        /// </summary>
        /// <param name="msgs">添加的消息队列</param>
        protected void AddMsg(params ushort[] msgs)
        {
            if (msgs == null || msgs.Length <= 0)
                return;

            if (msgIds == null || msgIds.Length <= 0)
            {
                msgIds = msgs;
            }
            else
            {
                ushort[] newMsg = new ushort[msgIds.Length + msgs.Length];
                msgIds.CopyTo(newMsg, 0);
                msgs.CopyTo(newMsg, msgIds.Length);
                msgIds = newMsg;
            }

            if (FormMsgManager.Instance != null)
                FormMsgManager.Instance.RegistMsg(this, msgIds);
            else
                Log.Fatal("FormMsgManager Instance is null!");
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="msg"></param>
        protected void SendMsg(MsgBase msg)
        {
            if (FormMsgManager.Instance != null)
                FormMsgManager.Instance.SendMsg(msg);
            else
                Log.Fatal("FormMsgManager Instance is null!");
        }

        /// <summary>
        /// 接收到消息的处理
        /// </summary>
        /// <param name="msg"></param>
        public virtual void ProcessEvent(MsgBase msg) { }
       
        /// <summary>
        /// 销毁时注销已绑定消息
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (msgIds != null && msgIds.Length > 0)
            {
                if (FormMsgManager.Instance != null)
                    FormMsgManager.Instance.UnRegistMsg(this, msgIds);
            }
        }
    }
}