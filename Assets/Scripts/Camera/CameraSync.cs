using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 同步相机相对模型的位置朝向
/// </summary>
public class CameraSync : MonoBase
{
    private Transform target;

    private Transform syncTransform;

    private float time;
    private float interval = 0.03f;

    private Vector3 prevPosition;
    private Quaternion prevRotation;

    private Vector3 relativePos;
    private Quaternion relativeRot;

    private float positionThreshold = 0.5f;
    private float angleThreshold = 5f;

    private bool sync = false;

    private bool inARMode = false;


    protected override void InitComponents()
    {
        base.InitComponents();

        target = ModelManager.Instance.modelRoot;

        //根据配置设置有无漫游模式
        if (GlobalInfo.hasRole)
            syncTransform = Camera.main.transform;
        else
            syncTransform = transform;

        prevPosition = Vector3.negativeInfinity;

        AddMsg(new ushort[] 
        {
            (ushort)GazeEvent.SyncCamera,
            (ushort)GazeEvent.UserPose,
            (ushort)RoomChannelEvent.UpdateControl,
#if UNITY_ANDROID || UNITY_IOS
            (ushort)ARModuleEvent.Open,
            (ushort)ARModuleEvent.Close
#endif
        });
    }

    //private void LateUpdate()
    //{
    //    if (target == null || ModelManager.Instance.modelGo == null)
    //        return;

    //    if (ModelManager.Instance.CameraDotween)
    //        return;

    //    if (!inARMode && GlobalInfo.IsOperator())
    //    {
    //        time += Time.deltaTime;
    //        if (time >= interval)
    //        {
    //            if (sync || Diff())
    //            {
    //                sync = false;
    //                prevPosition = transform.position;
    //                prevRotation = transform.rotation;

    //                relativePos = target.InverseTransformPoint(transform.position);
    //                relativeRot = Quaternion.Inverse(target.transform.rotation) * transform.rotation;

    //                MsgIntVector3Vector4 msg = new MsgIntVector3Vector4((ushort)GazeEvent.UserPose, GlobalInfo.account.id,
    //                    relativePos, new Vector4(relativeRot.x, relativeRot.y, relativeRot.z, relativeRot.w));

    //                NetworkManager.Instance.SendFrameMsg(msg);
    //            }
    //            time = 0;
    //        }
    //    }  
    //}

    int count = 0;
    private void FixedUpdate()
    {
        if (target == null || ModelManager.Instance.modelGo == null)
            return;

        if (ModelManager.Instance.CameraDotween)
            return;

        if (!inARMode && GlobalInfo.IsUserOperator())
        {
            if(count % 3 == 0/* && (sync || Diff())*/)
            {
                sync = false;
                prevPosition = transform.position;
                prevRotation = syncTransform.rotation;

                relativePos = target.InverseTransformPoint(transform.position);
                relativeRot = Quaternion.Inverse(target.transform.rotation) * syncTransform.rotation;

                MsgIntVector3Vector4 msg = new MsgIntVector3Vector4((ushort)GazeEvent.UserPose, GlobalInfo.account.id,
                    relativePos, new Vector4(relativeRot.x, relativeRot.y, relativeRot.z, relativeRot.w));

                NetworkManager.Instance.SendFrameMsg(msg);
            }
            count++;
        }
    }

    private bool Diff()
    {
        return Vector3.Distance(transform.position, prevPosition) > positionThreshold || Quaternion.Angle(syncTransform.rotation, prevRotation) > angleThreshold;
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        switch (msg.msgId)
        {
            case (ushort)GazeEvent.SyncCamera:
                sync = true;
                break;
#if UNITY_ANDROID || UNITY_IOS
            case (ushort)ARModuleEvent.Open:
                inARMode = true;
                break;
            case (ushort)ARModuleEvent.Close:
                inARMode = false;
                break;
#endif
        }
    }
}