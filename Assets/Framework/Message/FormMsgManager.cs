using System.Collections.Generic;
using System.Windows.Interop;

namespace UnityFramework.Runtime
{
    public class EventNode
    {
        /// <summary>
        /// 注册消息的脚本
        /// </summary>
        public MonoBase data;
        /// <summary>
		/// 下一个注册此消息的脚本
		/// </summary>
		public EventNode next;

        public EventNode(MonoBase mono)
        {
            data = mono;
            next = null;
        }
    }

    /// <summary>
    /// 框架内消息控制中心
    /// </summary>
    public class FormMsgManager : Singleton<FormMsgManager>
    {
        /// <summary>
        /// 注册的消息字典，key：消息id，value：注册消息的脚本
        /// </summary>
        protected Dictionary<ushort, EventNode> dicEventMsg = new Dictionary<ushort, EventNode>();

        /// <summary>
        /// 给脚本注册多个消息
        /// </summary>
        /// <param name="mono">注册消息的脚本</param>
        /// <param name="msgs">注册的消息数组</param>
        public void RegistMsg(MonoBase mono, params ushort[] msgs)
        {
            if (msgs == null || msgs.Length <= 0) return;
            for (int i = 0; i < msgs.Length; i++)
            {
                ushort id = msgs[i];
                EventNode node = new EventNode(mono);
                RegistMsg(id, node);
            }
        }


        /// <summary>
        /// 给一个消息添加一个node（mono）
        /// </summary>
        /// <param name="id">注册的消息id</param>
        /// <param name="node">注册消息的脚本</param>
        public void RegistMsg(ushort id, EventNode node)
        {
            if (dicEventMsg.ContainsKey(id))
            {
                EventNode tmpNode = dicEventMsg[id];

                while (tmpNode.next != null && tmpNode.data != node.data)//找到最后一个node,并且这个node没有注册这个消息
                {
                    tmpNode = tmpNode.next;
                }

                if (tmpNode.data != node.data)
                {
                    tmpNode.next = node;
                }
            }
            else
            {
                dicEventMsg.Add(id, node);
            }
        }

        /// <summary>
        /// 注销一个脚本的若干个消息
        /// </summary>
        /// <param name="mono"></param>
        /// <param name="msgs"></param>
        public void UnRegistMsg(MonoBase mono, params ushort[] msgs)
        {
            if (msgs == null) return;
            for (int i = 0; i < msgs.Length; i++)
            {
                UnRegistMsg(msgs[i], mono);
            }
        }

        /// <summary>
        /// 注销一个脚本的一个消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mono"></param>
        public void UnRegistMsg(ushort id, MonoBase mono)
        {
            if (!dicEventMsg.ContainsKey(id))
            {
                //Log.Warning("Not contait id:" + id);
                return;
            }

            EventNode node = dicEventMsg[id];

            if (node.data == mono)
            {
                if (node.next == null)
                {
                    dicEventMsg.Remove(id);
                }
                else
                {
                    node.data = node.next.data;
                    node.next = node.next.next;
                }
            }
            else
            {
                while (node.next != null && node.next.data != mono)
                {
                    node = node.next;
                }

                if (node.next == null)
                {
                    //Log.Debug(string.Format("The mono=={0} not regist the msgId=={1},or UnRegiste", mono.name, id));
                }
                else
                {
                    if (node.next.next != null)
                    {
                        node.next = node.next.next;
                    }
                    else
                    {
                        node.next = null;
                    }
                }
            }
        }



        public bool IsDicEventMsgCont(ushort id)
        {
            return dicEventMsg.ContainsKey(id);
        }

        /// <summary>
        /// 本地广播消息 AddMsg(msgIds) / RegistMsg()
        /// </summary>
        /// <param name="msg">广播的消息</param>
        public void SendMsg(MsgBase msg)
        {
            if (!dicEventMsg.ContainsKey(msg.msgId))
            {
                return;
            }
            else
            {
                EventNode node = dicEventMsg[msg.msgId];

                do
                {
                    node.data.ProcessEvent(msg);
                    node = node.next;
                } while (node != null);
            }
        }
    }

}