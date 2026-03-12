using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityFramework.Runtime.RequestData;

namespace UnityFramework.Runtime
{
    public class MsgTuple<T> : MsgBase
    {
        public System.Tuple<T> arg;
    }

    public class MsgTuple<T1, T2> : MsgBase
    {
        public System.Tuple<T1, T2> arg;
    }

    public class MsgTuple<T1, T2, T3> : MsgBase
    {
        public System.Tuple<T1, T2, T3> arg;
    }

    public class MsgTuple<T1, T2, T3, T4> : MsgBase
    {
        public System.Tuple<T1, T2, T3, T4> arg;
    }
    public class MsgStringTuple<T1, T2, T3> : MsgBase
    {
        public string arg1;
        public System.Tuple<T1, T2, T3> arg2;
    }
    public class MsgIntTransform : MsgBase
    {
        public int arg1;
        public Transform arg2;

        public MsgIntTransform() { }
        public MsgIntTransform(ushort msgId, int arg1, Transform arg2) : base(msgId)
        {
            this.arg1 = arg1;
            this.arg2 = arg2;
        }
    }

    public class MsgIntVector3Vector4 : MsgBase
    {
        public int arg;
        public MyVector3 v3;
        public MyVector4 v4;

        public MsgIntVector3Vector4() { }
        public MsgIntVector3Vector4(ushort msgId, int arg, Vector3 ve3, Vector4 ve4) : base(msgId)
        {
            this.arg = arg;
            this.v3 = ve3;
            this.v4 = ve4;
        }
    }

    public class MsgHyperlink : MsgBase
    {
        public int id;
        public string title;
        public string url;
        public string docType;
        public bool showClose;

        public MsgHyperlink() { }
        public MsgHyperlink(ushort msgId, int id, string title, string url = null, string docType = null, bool showClose = true) : base(msgId)
        {
            this.id = id;
            this.title = title;
            this.url = url;
            this.docType = docType;
            this.showClose = showClose;
        }

        public MsgHyperlink(ushort msgId, Courseware courseware) : base(msgId)
        {
            this.id = courseware.id;
            this.title = courseware.fileName;
            this.url = courseware.filePath;
            this.docType = courseware.docType;
            this.showClose = true;
        }
    }

    public class MsgHyperlinkClose : MsgBase
    {
        public int id;
        public string title;
        public string docType;
        public bool minimable;

        public MsgHyperlinkClose() { }
        public MsgHyperlinkClose(ushort msgId, int id, string title, string docType, bool minimable) : base(msgId)
        {
            this.id = id;
            this.title = title;
            this.docType = docType;
            this.minimable = minimable;
        }
        public MsgHyperlinkClose(ushort msgId, MsgHyperlink linkData) : base(msgId)
        {
            this.id = linkData.id;
            this.title = linkData.title;
            this.docType = linkData.docType;
            //this.minimable = linkData.minimable;
        }
        public MsgHyperlinkClose(ushort msgId, ShowLinkModuleData moduleData) : base(msgId)
        {
            this.id = moduleData.id;
            this.title = moduleData.title;
            this.docType = moduleData.docType;
        }
    }

    public class MsgHyperlinkMin : MsgBase
    {
        public int id;
        public string title;
        public string url;
        public string type;

        public MsgHyperlinkMin() { }

        public MsgHyperlinkMin(ushort msgId, int id, string title, string url, string type) : base(msgId)
        {
            this.id = id;         
            this.title = title;
            this.url = url;
            this.type = type;
        }

        public MsgHyperlinkMin(ushort msgId, ShowLinkModuleData linkModuleData) : base(msgId)
        {
            this.id = linkModuleData.id;
            this.title = linkModuleData.title;
            this.url = linkModuleData.url;
            this.type = linkModuleData.docType;
        }
    }

    public class MsgBrodcastOperate : MsgBase
    {
        public int senderId;

        public string data;
        public MsgBrodcastOperate() { }
        public MsgBrodcastOperate(ushort msgId, string data) : base(msgId)
        {
            this.senderId = GlobalInfo.account.id;
            this.data = data;
        }

        public T GetData<T>() where T : MsgBase
        {
            return JsonTool.DeSerializable<T>(data);
        }
    }

    public class MsgSyncPaintType : MsgBase
    {
        public int sender;

        public OPLPaintModule.PaintType pt;
        public MsgSyncPaintType() { }
        public MsgSyncPaintType(ushort msgId, int sender, OPLPaintModule.PaintType pt) : base(msgId)
        {
            this.sender = sender;
            this.pt = pt;
        }
    }
    public class MsgSyncPaintWidth : MsgBase
    {
        public int sender;

        public OPLPaintModule.PaintWidth pw;

        public float value;

        public MsgSyncPaintWidth() { }

        public MsgSyncPaintWidth(ushort msgId, int sender, OPLPaintModule.PaintWidth pw, float value) : base(msgId)
        {
            this.sender = sender;
            this.pw = pw;
            this.value = value;
        }
    }
    public class MsgSyncPaintColor : MsgBase
    {
        public int sender;
        public string rgba;
        public MsgSyncPaintColor() { }
        public MsgSyncPaintColor(ushort msgId, int sender, string c) : base(msgId)
        {
            //this.rgba = c.ColorToHex();
            this.sender = sender;
            this.rgba = c;
        }
    }
    public class MsgSyncPaint : MsgBase
    {
        public OPLPaintModule.PaintWidth pw;
        public OPLPaintModule.PaintType pt;
        public string rgba;
        public MyVector2[] points;
        public int screenWidth;
        public int screenHeight;
        public MsgSyncPaint() { }
        public MsgSyncPaint(ushort msgId, OPLPaintModule.PaintWidth pw, OPLPaintModule.PaintType pt, Color paintColor, Vector2[] points, int screenWidth, int screenHeight) : base(msgId)
        {
            this.pw = pw;
            this.pt = pt;
            this.rgba = paintColor.ColorToHex();
            this.points = ToMyVector2(points);
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
        }

        private MyVector2[] ToMyVector2(Vector2[] points)
        {
            if (points == null) return new MyVector2[0];
            MyVector2[] result = new MyVector2[points.Length];
            int len = points.Length;
            for (int i = 0; i < len; i++)
            {
                result[i] = points[i];
            }
            return result;
        }

        public Vector2[] GetPoints()
        {
            if (points == null) return new Vector2[0];
            Vector2[] result = new Vector2[points.Length];
            int len = points.Length;
            for (int i = 0; i < len; i++)
            {
                result[i] = points[i];
            }
            return result;
        }
    }
    public class MsgSyncSubsectionAnime : MsgBase
    {
        public bool playState;
        public int playIndex;
        public float playValue;
        public int startIndex;
        public int endIndex;
        public bool isNull;
        //public List<bool> toggleState;
        public MsgSyncSubsectionAnime() { }
        /*
        public MsgSyncSubsectionAnime(ushort msgId, bool ps, int pi, float pv, List<bool> ts) : base(msgId)
        {
            this.playState = ps;
            this.playIndex = pi;
            this.playValue = pv;
            this.toggleState = ts;
        }
        */
        public MsgSyncSubsectionAnime(ushort msgId, bool playState, int playIndex, float playValue, int startIndex, int endIndex, bool isNull) : base(msgId)
        {
            this.playState = playState;
            this.playIndex = playIndex;
            this.playValue = playValue;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
            this.isNull = isNull;
        }
    }
    public class MsgSyncSubsectionAnimeBState : MsgBase
    {
        public float contentPositionX;
        public bool isPlay;
        public MsgSyncSubsectionAnimeBState() { }
        public MsgSyncSubsectionAnimeBState(ushort msgId, float ContentPositionX, bool IsPlay) : base(msgId)
        {
            this.contentPositionX = ContentPositionX;
            this.isPlay = IsPlay;
        }
    }

    public class MsgDic<T1, T2> : MsgBase
    {
        public Dictionary<T1, T2> arg;
        public MsgDic() { }
        public MsgDic(ushort msgId, Dictionary<T1, T2> arg) : base(msgId)
        {
            this.arg = arg;
        }
    }

    public class MsgExamRecord : MsgBase
    {
        public int examId;
        /// <summary>
        /// key:baikeId string:百科状态
        /// </summary>
        public Dictionary<int, string> arg;
        public MsgExamRecord() { }
        public MsgExamRecord(ushort msgId, int examId, Dictionary<int, string> arg) : base(msgId)
        {
            this.examId = examId;
            this.arg = arg;
        }
    }
    public class MsgList<T> : MsgBase
    {
        public List<T> arg;
        public MsgList() { }
        public MsgList(ushort msgId, List<T> arg) : base(msgId)
        {
            this.arg = arg;
        }
    }

    public class MsgPracticeAction : MsgBase
    {
        /// <summary>
        /// 大项按钮名称
        /// </summary>
        public string ButtonName = "";
        /// <summary>
        /// 击中物品名称
        /// </summary>
        public string TargetName = "";
        /// <summary>
        /// 其他事件处理
        /// </summary>
        public PractionEvent ButtonEvent = 0;
        public enum PractionEvent
        {
            None,
            TipButtonDown,
            TipButtonUp,
            MenuShow,
            MenuHide,
            Reset
        }
        public MsgPracticeAction() { }
        //public MsgPracticeAction(ushort msgId, float ContentPositionX, bool IsPlay) : base(msgId)
        //{
        //    this.contentPositionX = ContentPositionX;
        //    this.isPlay = IsPlay;
        //}
    }
    /// <summary>
    /// 知识点信息 
    /// </summary>
    public class MsgKnowledge : MsgBase
    {
        /// <summary>
        ///  知识点id
        /// </summary>
        public int id;
        /// <summary>
        /// 知识点标题
        /// </summary>
        public string title;
        /// <summary>
        /// 知识点内容
        /// </summary>
        public string content;

        public MsgKnowledge() { }

        public MsgKnowledge(ushort msgId, int id, string title, string content) : base(msgId)
        {
            this.id = id;
            this.title = title;
            this.content = content;
        }
    }


    public class MsgSharedTexture : MsgBase
    {
        public int userId;
        public byte[] data;

        public MsgSharedTexture() { }
        public MsgSharedTexture(ushort msgId, int userId, byte[] data) : base(msgId)
        {
            this.userId = userId;
            this.data = data;
        }
    }

    public class MsgPCControl : MsgBase
    {
        public UIPanelBase uiPanelBase;

        public MsgPCControl() { }
        public MsgPCControl(ushort msgId, UIPanelBase uiPanelBase) : base(msgId)
        {
            this.uiPanelBase = uiPanelBase;
        }
    }

    /// <summary>
    /// 模型层级选中消息
    /// </summary>
    public class MsgHierarchy : MsgBase
    {
        public int userId;
        public string uuid;
        public TreeViewItem item;

        public MsgHierarchy() { }
        public MsgHierarchy(ushort msgId, int userId, string uuid, TreeViewItem item) : base(msgId)
        {
            this.userId = userId;
            this.uuid = uuid;
            this.item = item;
        }
    }


    /// <summary>
    /// 执行操作消息
    /// </summary>
    public class MsgOperation : MsgBase
    {
        public string userNo;
        public string userName;
        public string modelOperation;
        public string operationName;
        public string propId;
        public bool correctOp;

        public MsgOperation() { }

        public MsgOperation(ushort msgId, string modelOperation, string operationName, string propId, bool correctOp = true) : base(msgId)
        {
            this.userNo = GlobalInfo.account.userNo;
            this.userName = GlobalInfo.account.nickname;
            this.modelOperation = modelOperation;
            this.operationName = operationName;
            this.propId = propId;
            this.correctOp = correctOp;
        }
    }

    /// <summary>
    /// 执行操作消息
    /// </summary>
    public class MsgOperation2D : MsgBase
    {
        public string modelOperation;
        public string operationName;

        public MsgOperation2D() { }
        public MsgOperation2D(ushort msgId, string modelOperation, string operationName) : base(msgId)
        {
            this.modelOperation = modelOperation;
            this.operationName = operationName;
        }
    }

    public class MsgStepEnd: MsgBase
    {
        //public string user;
        public string modelInfoId;
        public string operationName;
        public bool hasFocusMode;
        //public string propId;
        //public bool correctOp;

        public MsgStepEnd() { }

        public MsgStepEnd(ushort msgId, string modelInfoId, string operationName, bool hasFocusMode) : base(msgId)
        {
            //this.user = user;
            this.modelInfoId = modelInfoId;
            this.operationName = operationName;
            //this.propId = propId;
            //this.correctOp = correctOp;
            this.hasFocusMode = hasFocusMode;
        }
    }

    public class MsgModelRotate : MsgBase
    {
        public string id;
        public float angleZ;
        public float sumAngle;
        public float normalizedAngleDelta;

        public MsgModelRotate() { }

        public MsgModelRotate(ushort msgId, string id, float angleZ, float sumAngle, float normalizedAngleDelta) : base(msgId)
        {
            this.id = id;
            this.angleZ = angleZ;
            this.sumAngle = sumAngle;
            this.normalizedAngleDelta = normalizedAngleDelta;
        }
    }

    /// <summary>
    /// 直播答题消息
    /// </summary>
    public class MsgJudgeOnline : MsgBase
    {
        public int pediaId;
        public int choiceCount;
        public bool multipleChoice;

        public MsgJudgeOnline() { }
        public MsgJudgeOnline(ushort msgId, int pediaId, int choiceCount, bool multipleChoice) : base(msgId)
        {
            this.pediaId = pediaId;
            this.choiceCount = choiceCount;
            this.multipleChoice = multipleChoice;
        }
    }

    /// <summary>
    /// 开始考核消息
    /// </summary>
    public class MsgExamStart : MsgBase
    {
        public int examId;
        public DateTime startTime;
        public DateTime endTime;
        //public List<int> examinees;
        public Dictionary<int, int> examineeRecords;

        public MsgExamStart(ushort msgId, int examId, DateTime startTime, DateTime endTime, Dictionary<int, int> examineeRecords) : base(msgId)
        {
            this.examId = examId;
            this.startTime = startTime;
            this.endTime = endTime;
            this.examineeRecords = examineeRecords;
        }
    }

    public class MsgElement : MsgBase
    {
        public string ID;
        public string name;
        public string parent;
        public MsgElement() { }
        public MsgElement(ushort msgId, string id, string name, string parent) : base(msgId)
        {
            this.ID = id;
            this.name = name;
            this.parent = parent;
        }
    }
}