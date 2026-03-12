using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using static UISmallSceneOperationHistory;

namespace UnityFramework.Runtime
{
    /// <summary>
    /// 基础消息
    /// </summary>
    public class MsgBase
    {
        /// <summary>
        /// 消息id
        /// </summary>
        public ushort msgId;

        public MsgBase() { }

        public MsgBase(ushort tmpId)
        {
            msgId = tmpId;
        }
    }

    #region 单个数据消息
    public class MsgInt : MsgBase
    {
        public int arg;
        public MsgInt() { }
        public MsgInt(ushort msgId, int arg) : base(msgId)
        {
            this.arg = arg;
        }
    }

    public class MsgFloat : MsgBase
    {
        public float arg;
        public MsgFloat() { }
        public MsgFloat(ushort msgId, float arg) : base(msgId)
        {
            this.arg = arg;
        }
    }

    public class MsgString : MsgBase
    {
        public string arg;
        public MsgString() { }
        public MsgString(ushort msgId, string arg) : base(msgId)
        {
            this.arg = arg;
        }
    }

    public class MsgBool : MsgBase
    {
        public bool arg1;
        public MsgBool() { }
        public MsgBool(ushort msgId, bool arg1) : base(msgId)
        {
            this.arg1 = arg1;
        }
    }

    public class MsgGameObject : MsgBase
    {
        public GameObject arg;
        public MsgGameObject() { }
        public MsgGameObject(ushort msgId, GameObject arg) : base(msgId)
        {
            this.arg = arg;
        }
    }

    public class MsgTransform : MsgBase
    {
        public Transform arg;

        public MsgTransform(ushort msgId, Transform arg) : base(msgId)
        {
            this.arg = arg;
        }
        public MsgTransform() { }
    }

    public class MsgRectTransform : MsgBase
    {
        public RectTransform arg;
        public MsgRectTransform(ushort msgId, RectTransform arg) : base(msgId)
        {
            this.arg = arg;
        }
        public MsgRectTransform() { }
    }

    public class MsgUnityAction : MsgBase
    {
        public UnityAction arg;
        public MsgUnityAction(ushort msgId, UnityAction arg) : base(msgId)
        {
            this.arg = arg;
        }
        public MsgUnityAction() { }
    }

    public class MyVector2
    {
        public float x;
        public float y;

        public MyVector2() { }

        public MyVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static implicit operator Vector2(MyVector2 myV2)
        {
            return new Vector2(myV2.x, myV2.y);
        }

        public static implicit operator MyVector2(Vector2 v2)
        {
            return new MyVector2(v2.x, v2.y);
        }
    }

    public class MsgVector2 : MsgBase
    {
        public MyVector2 vector2;

        public MsgVector2() { }
        public MsgVector2(ushort msgId, Vector2 ve2) : base(msgId)
        {
            this.vector2 = ve2;
        }
    }

    public class MyVector3
    {
        public float x;
        public float y;
        public float z;

        public MyVector3() { }

        public MyVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(MyVector3 myV3)
        {
            return new Vector3(myV3.x, myV3.y, myV3.z);
        }

        public static implicit operator MyVector3(Vector3 v3)
        {
            return new MyVector3(v3.x, v3.y, v3.z);
        }
    }
    public class MsgVector3 : MsgBase
    {
        public MyVector3 vector3;

        public MsgVector3() { }
        public MsgVector3(ushort msgId, Vector3 ve3) : base(msgId)
        {
            this.vector3 = ve3;
        }
    }

    public class MyVector4
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public MyVector4() { }

        public MyVector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static implicit operator Vector4(MyVector4 myV4)
        {
            return new Vector4(myV4.x, myV4.y, myV4.z, myV4.w);
        }

        public static implicit operator MyVector4(Vector4 v4)
        {
            return new MyVector4(v4.x, v4.y, v4.z, v4.w);
        }
    }
    public class MsgVector4 : MsgBase
    {
        public MyVector4 vector4;

        public MsgVector4() { }
        public MsgVector4(ushort msgId, Vector4 ve4) : base(msgId)
        {
            this.vector4 = ve4;
        }
    }
    #endregion

    #region 两个数据消息
    public class MsgStringInt : MsgBase
    {
        public string arg1;
        public int arg2;
        public MsgStringInt() { }
        public MsgStringInt(ushort msgId, string arg1, int arg2) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }

    public class MsgStringFloat : MsgBase
    {
        public string arg;
        public float arg1;

        public MsgStringFloat() { }
        public MsgStringFloat(ushort msgId, string arg, float arg1) : base(msgId)
        {
            this.arg = arg;
            this.arg1 = arg1;
        }
    }

    public class MsgStringString : MsgBase
    {
        public string arg1;
        public string arg2;
        public MsgStringString() { }
        public MsgStringString(ushort msgId, string arg1, string arg2) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }

    public class MsgStringBool : MsgBase
    {
        public string arg1;
        public bool arg2;
        public MsgStringBool() { }
        public MsgStringBool(ushort msgId, string arg1, bool arg2) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }

    public class MsgStringVector2 : MsgBase
    {
        public string arg;
        public Vector2 vec;

        public MsgStringVector2() { }
        public MsgStringVector2(ushort msgId, string arg, Vector2 vec) : base(msgId)
        {
            this.arg = arg;
            this.vec = vec;
        }
    }

    public class MsgStringVector3 : MsgBase
    {
        public string arg;
        public MyVector3 vector3;

        public MsgStringVector3() { }
        public MsgStringVector3(ushort msgId, string arg, Vector3 ve3) : base(msgId)
        {
            this.arg = arg;
            this.vector3 = ve3;
        }
    }

    public class MsgIntInt : MsgBase
    {
        public int arg1;
        public int arg2;
        public MsgIntInt() { }
        public MsgIntInt(ushort msgId, int arg1, int arg2) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }

    public class MsgIntFloat : MsgBase
    {
        public int arg1;
        public float arg2;
        public MsgIntFloat() { }
        public MsgIntFloat(ushort msgId, int arg1, float arg2) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }

    public class MsgIntBool : MsgBase
    {
        public int arg1;
        public bool arg2;
        public MsgIntBool() { }
        public MsgIntBool(ushort msgId, int arg1, bool arg2) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }

    public class MsgIntString : MsgBase
    {
        public int arg1;
        public string arg2;
        public MsgIntString() { }
        public MsgIntString(ushort msgId, int arg1, string arg2) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }

    public class MsgBoolBool : MsgBase
    {
        public bool arg1;
        public bool arg2;
        public MsgBoolBool() { }
        public MsgBoolBool(ushort msgId, bool arg1, bool arg2) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }

    public class MsgFloatBool : MsgBase
    {
        public float arg1;
        public bool arg2;
        public MsgFloatBool() { }
        public MsgFloatBool(ushort msgId, float arg1, bool arg2) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }
    #endregion

    #region 三个数据消息
    public class MsgStringIntInt : MsgBase
    {
        public string arg1;
        public int arg2;
        public int arg3;
        public MsgStringIntInt() { }
        public MsgStringIntInt(ushort msgId, string arg1, int arg2, int arg3) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
            this.arg3 = arg3;
        }
    }
    #endregion

    public class MsgOperatingRecord : MsgBase
    {
        /// <summary>
        /// step.hint_success
        /// </summary>
        public string stepHint;
        /// <summary>
        /// op.hint_success
        /// </summary>
        public string opHint;
        /// <summary>
        /// flowIndex
        /// </summary>
        public int flowIndex;
        /// <summary>
        /// stepIndex
        /// </summary>
        public int stepIndex;
        /// <summary>
        /// opIndex
        /// </summary>
        public int opIndex;
        /// <summary>
        /// 是否创建操作记录
        /// </summary>
        public bool createHistoryItem;
        /// <summary>
        /// 操作人工号
        /// </summary>
        public string userNo;
        /// <summary>
        /// 操作人姓名
        /// </summary>
        public string userName;
        /// <summary>
        /// 操作时间
        /// </summary>
        public string createTime;
        /// <summary>
        /// 操作类型
        /// </summary>
        public int opType;

        public MsgOperatingRecord() { }

        public MsgOperatingRecord(ushort msgId, string stepHint, string opHint, int flowIndex, int stepIndex, int opIndex, 
            string userNo, string userName, string createTime, OpType opType, bool createHistoryItem = true) : base(msgId)
        {
            this.stepHint = stepHint;
            this.opHint = opHint;
            this.flowIndex = flowIndex;
            this.stepIndex = stepIndex;
            this.opIndex = opIndex;
            this.createHistoryItem = createHistoryItem;
            this.userNo = userNo;
            this.userName = userName;
            this.createTime = createTime;
            this.opType = (int)opType;
        }

        public MsgOperatingRecord(ushort msgId, string opHint, string createTime) : base(msgId)
        {
            this.userNo = string.Empty;
            this.userName = string.Empty;
            this.opHint = opHint;
            this.createTime = createTime;
            this.opType = (int)OpType.System;
        }
    }

    public class MsgDbug : MsgBase
    {
        public LogData arg;
        public MsgDbug() { }
        public MsgDbug(ushort msgId, LogData arg) : base(msgId)
        {
            this.arg = arg;
        }
    }

    public class MsgBehaveEvent : MsgBase
    {
        public BehaveBase arg;

        public Transform behaveTrans;

        public MsgBehaveEvent() { }
    }

    public class Msg2DOperate : MsgBase
    {
        public ModelOperation operation;
        public string optionName;
        public Msg2DOperate() { }
        public Msg2DOperate(ushort msgId, ModelOperation operation, string optionName) : base(msgId)
        {
            this.operation = operation;
            this.optionName = optionName;
        }
    }
}