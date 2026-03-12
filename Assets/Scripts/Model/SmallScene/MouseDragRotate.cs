using System;
using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;

/// <summary>
/// 模型上进行拖拽操作
/// </summary>
public class MouseDragRotate : MonoBase
{
    public enum DragDirection
    {
        Horizontal,
        Vertical
    }

    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    /// <summary>
    /// 是否忽略同一状态的拖拽
    /// </summary>
    public bool IgnoreSameState = true;

    public bool Reverse = false;

    public DragDirection Direction;
    public RotationAxis Axis;

    //松开鼠标再计算旋转角度
    public bool Step;
    //实时计算旋转角度 
    public bool Discrete;

    /// <summary>
    /// 均匀旋转角度
    /// </summary>
    public float StepRotationAngle;
    /// <summary>
    /// 是否使用均匀角度
    /// </summary>
    public bool UseStepAngle = true;

    /// <summary>
    /// 触发旋转的最小拖动距离
    /// </summary>
    public float StepMinDistance;

    public float minAngle = -45f;
    public float maxAngle = 45f;

    public float RotationSpeed = 200f;

    private Vector3 lastMousePosition;
    private bool isMouseOver = false;

    private float currentAngle = 0f;

    private float initialAngle; // 记录鼠标按下时的初始角度

    private Vector3 axis;

    [HideInInspector]
    public Action<string> OnDragFinish;

    [HideInInspector]
    public Action OnModelClicked;

    //todo
    [HideInInspector]
    public Action onFail;

    public List<OpData> OpDatas = new List<OpData>();

    private string modelInfoID;
    private Transform targetTrans;

    private string currentState;

    private float xRotationRatio;
    private float yRotationRatio;

    protected override void InitComponents()
    {
        base.InitComponents();
        AddMsg(
            (ushort)SmallFlowModuleEvent.StartExecute,
            (ushort)SmallFlowModuleEvent.CompleteExecute,
            (ushort)SmallFlowModuleEvent.FocusChanged
        );
        xRotationRatio = Reverse ? -1 : 1;
        yRotationRatio = Reverse ? 1 : -1;
    }

    public void Setup(string id, string currentState, Transform target)
    {
        modelInfoID = id;
        this.currentState = currentState;
        targetTrans = target;
    }

    private bool isSelect = false;

    private float rotationAmount;
    private float newAngle;
    private float actualRotation;
    private bool actualRotate;
    private float accumulateDelta;

    void Update()
    {
        if (!Interactable || targetTrans == null)
            return;

        if (Input.GetMouseButtonDown(0) && isMouseOver && !GUITool.IsOverGUI(Input.mousePosition))
        {
            isSelect = true;

            switch (Axis)
            {
                case RotationAxis.X:
                    axis = Vector3.right;
                    currentAngle = targetTrans.localEulerAngles.x;
                    break;
                case RotationAxis.Y:
                    axis = Vector3.up;
                    currentAngle = targetTrans.localEulerAngles.y;
                    break;
                case RotationAxis.Z:
                    axis = Vector3.forward;
                    currentAngle = targetTrans.localEulerAngles.z;
                    break;
            }
            initialAngle = currentAngle;
            lastMousePosition = Input.mousePosition;
        }

        if (Step)
        {
            #region rotate when release mouse
            //if (Input.GetMouseButtonUp(0) && isSelect)
            //{
            //    isSelect = false;
            //    actualRotation = 0f;

            //    Vector3 delta = Input.mousePosition - lastMousePosition;

            //    switch (Direction)
            //    {
            //        case DragDirection.Horizontal:
            //            if (Mathf.Abs(delta.x) > StepMinDistance)
            //            {
            //                if (UseStepAngle)
            //                {
            //                    rotationAmount = (delta.x > 0 ? xRotationRatio : -xRotationRatio) * StepRotationAngle;
            //                }
            //                else
            //                {
            //                    int angleIndex = OpDatas.FindIndex(o => currentAngle >= o.angleRange.x && currentAngle <= o.angleRange.y);
            //                    if (angleIndex >= 0)
            //                    {
            //                        int destAngleIndex = Mathf.Clamp(angleIndex + (int)(delta.x > 0 ? xRotationRatio : -xRotationRatio), 0, OpDatas.Count - 1);
            //                        Vector2 angleRange = OpDatas[destAngleIndex].angleRange;
            //                        rotationAmount = (angleRange.x + angleRange.y) / 2 - currentAngle;
            //                    }
            //                    else
            //                    {
            //                        rotationAmount = 0f;
            //                    }
            //                }

            //                // Calculate new angle and clamp it
            //                newAngle = Mathf.Clamp(currentAngle + rotationAmount, minAngle, maxAngle);
            //                // Apply rotation difference
            //                actualRotation = newAngle - currentAngle;

            //                targetTrans.Rotate(axis, actualRotation, Space.Self);
            //                lastMousePosition = Input.mousePosition;
            //                currentAngle = newAngle;
            //                DragEnd(actualRotation);
            //            }
            //            else
            //            {
            //                OnModelClicked?.Invoke();
            //            }
            //            break;
            //        case DragDirection.Vertical:
            //            if (Mathf.Abs(delta.y) > StepMinDistance)
            //            {
            //                if (UseStepAngle)
            //                {
            //                    rotationAmount = (delta.y > 0 ? yRotationRatio : -yRotationRatio) * StepRotationAngle;
            //                }
            //                else
            //                {
            //                    int angleIndex = OpDatas.FindIndex(o => currentAngle >= o.angleRange.x && currentAngle <= o.angleRange.y);
            //                    if (angleIndex >= 0)
            //                    {
            //                        int destAngleIndex = Mathf.Clamp(angleIndex + (int)(delta.y > 0 ? yRotationRatio : -yRotationRatio), 0, OpDatas.Count - 1);
            //                        Vector2 angleRange = OpDatas[destAngleIndex].angleRange;
            //                        rotationAmount = (angleRange.x + angleRange.y) / 2 - currentAngle;
            //                    }
            //                    else
            //                    {
            //                        rotationAmount = 0f;
            //                    }
            //                }

            //                newAngle = Mathf.Clamp(currentAngle + rotationAmount, minAngle, maxAngle);
            //                actualRotation = newAngle - currentAngle;

            //                targetTrans.Rotate(axis, actualRotation, Space.Self);
            //                lastMousePosition = Input.mousePosition;
            //                currentAngle = newAngle;
            //                DragEnd(actualRotation);
            //            }
            //            else
            //            {
            //                OnModelClicked?.Invoke();
            //            }
            //            break;
            //    }
            //}
            #endregion

            if (Input.GetMouseButton(0) && isSelect)
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;

                //产生拖拽
                if (!actualRotate)
                    actualRotate = delta.x != 0 || delta.y != 0;

                switch (Direction)
                {
                    case DragDirection.Horizontal:
                        if (Mathf.Abs(delta.x) > StepMinDistance && Mathf.Abs(currentAngle - initialAngle) < 1f)//只能拖动一次
                        {
                            if (UseStepAngle)
                            {
                                rotationAmount = (delta.x > 0 ? xRotationRatio : -xRotationRatio) * StepRotationAngle;
                            }
                            else
                            {
                                int angleIndex = OpDatas.FindIndex(o => currentAngle >= o.angleRange.x && currentAngle <= o.angleRange.y);
                                if (angleIndex >= 0)
                                {
                                    int destAngleIndex = Mathf.Clamp(angleIndex + (int)(delta.x > 0 ? xRotationRatio : -xRotationRatio), 0, OpDatas.Count - 1);
                                    Vector2 angleRange = OpDatas[destAngleIndex].angleRange;
                                    rotationAmount = (angleRange.x + angleRange.y) / 2 - currentAngle;
                                }
                                else
                                {
                                    rotationAmount = 0f;
                                }
                            }

                            newAngle = Mathf.Clamp(currentAngle + rotationAmount, minAngle, maxAngle);
                            actualRotation = newAngle - currentAngle;
                            targetTrans.Rotate(axis, actualRotation, Space.Self);
                            currentAngle = newAngle;
                            lastMousePosition = Input.mousePosition;
                        }
                        break;
                    case DragDirection.Vertical:
                        if (Mathf.Abs(delta.y) > StepMinDistance && Mathf.Abs(currentAngle - initialAngle) < 1f)
                        {
                            if (UseStepAngle)
                            {
                                rotationAmount = (delta.y > 0 ? yRotationRatio : -yRotationRatio) * StepRotationAngle;
                            }
                            else
                            {
                                int angleIndex = OpDatas.FindIndex(o => currentAngle >= o.angleRange.x && currentAngle <= o.angleRange.y);
                                if (angleIndex >= 0)
                                {
                                    int destAngleIndex = Mathf.Clamp(angleIndex + (int)(delta.y > 0 ? yRotationRatio : -yRotationRatio), 0, OpDatas.Count - 1);
                                    Vector2 angleRange = OpDatas[destAngleIndex].angleRange;
                                    rotationAmount = (angleRange.x + angleRange.y) / 2 - currentAngle;
                                }
                                else
                                {
                                    rotationAmount = 0f;
                                }
                            }
                            newAngle = Mathf.Clamp(currentAngle + rotationAmount, minAngle, maxAngle);
                            actualRotation = newAngle - currentAngle;
                            targetTrans.Rotate(axis, actualRotation, Space.Self);
                            currentAngle = newAngle;
                            lastMousePosition = Input.mousePosition;
                        }
                        break;
                }
            }

            if (Input.GetMouseButtonUp(0) && isSelect)
            {
                isSelect = false;
                if (actualRotate)
                {
                    //if (!Discrete)
                    //{
                    //    ModelOperationEventManager.Publish(new DragEvent(this.modelInfoID, targetTrans.gameObject, currentAngle, currentAngle / (maxAngle - minAngle)));
                    //}
                    DragEnd();
                }
                else
                {
                    OnModelClicked?.Invoke();
                }
                actualRotate = false;
            }
        }
        else
        {
            if (Input.GetMouseButton(0) && isSelect)
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;

                //产生拖拽
                if (!actualRotate)
                    actualRotate = delta.x != 0 || delta.y != 0;

                switch (Direction)
                {
                    case DragDirection.Horizontal:
                        if (Discrete)
                        {
                            int prevStep = CalculateStep();
                            accumulateDelta += delta.x * xRotationRatio;
                            int newStep = CalculateStep();

                            if (UseStepAngle)
                            {
                                rotationAmount = (newStep - prevStep) * StepRotationAngle;
                            }
                            else
                            {
                                int angleIndex = OpDatas.FindIndex(o => currentAngle >= o.angleRange.x && currentAngle <= o.angleRange.y);
                                if (angleIndex >= 0 && newStep != prevStep)
                                {
                                    int destAngleIndex = Mathf.Clamp(angleIndex + newStep - prevStep, 0, OpDatas.Count - 1);
                                    Vector2 angleRange = OpDatas[destAngleIndex].angleRange;
                                    rotationAmount = (angleRange.x + angleRange.y) / 2 - currentAngle;
                                }
                                else
                                {
                                    rotationAmount = 0f;
                                }
                            }
                        }
                        else
                        {
                            rotationAmount = delta.x * RotationSpeed * Time.deltaTime;
                        }

                        newAngle = Mathf.Clamp(currentAngle + rotationAmount, minAngle, maxAngle);
                        actualRotation = newAngle - currentAngle;

                        targetTrans.Rotate(axis, actualRotation, Space.Self);

                        lastMousePosition = Input.mousePosition;
                        currentAngle = newAngle;
                        break;
                    case DragDirection.Vertical:
                        if (Discrete)
                        {
                            int prevStep = CalculateStep();
                            accumulateDelta += delta.y * -yRotationRatio;
                            int newStep = CalculateStep();


                            if (UseStepAngle)
                            {
                                rotationAmount = (prevStep - newStep) * StepRotationAngle;
                            }
                            else
                            {
                                int angleIndex = OpDatas.FindIndex(o => currentAngle >= o.angleRange.x && currentAngle <= o.angleRange.y);
                                if (angleIndex >= 0 && newStep != prevStep)
                                {
                                    int destAngleIndex = Mathf.Clamp(angleIndex + newStep - prevStep, 0, OpDatas.Count - 1);
                                    Vector2 angleRange = OpDatas[destAngleIndex].angleRange;
                                    rotationAmount = (angleRange.x + angleRange.y) / 2 - currentAngle;
                                }
                                else
                                {
                                    rotationAmount = 0f;
                                }
                            }
                        }
                        else
                            rotationAmount = -delta.y * RotationSpeed * Time.deltaTime;

                        newAngle = Mathf.Clamp(currentAngle + rotationAmount, minAngle, maxAngle);
                        actualRotation = newAngle - currentAngle;

                        targetTrans.Rotate(axis, actualRotation, Space.Self);

                        lastMousePosition = Input.mousePosition;
                        currentAngle = newAngle;
                        break;
                }
            }

            if (Input.GetMouseButtonUp(0) && isSelect)
            {
                isSelect = false;
                accumulateDelta = 0f;
                if (actualRotate)
                {
                    if (!Discrete)
                    {
                        ModelOperationEventManager.Publish(new DragEvent(this.modelInfoID, targetTrans.gameObject, currentAngle, currentAngle / (maxAngle - minAngle)));
                    }
                    DragEnd();
                }
                else
                {
                    OnModelClicked?.Invoke();
                }
                actualRotate = false;
            }
        }
    }

    private int CalculateStep()
    {
        if (accumulateDelta < 0)
            return Mathf.CeilToInt(accumulateDelta / StepMinDistance);
        else
            return Mathf.FloorToInt(accumulateDelta / StepMinDistance);
    }

    /// <summary>
    /// 拖拽结束 尝试触发操作
    /// </summary>
    private void DragEnd()
    {
        string newState = string.Empty;
        foreach (OpData opData in OpDatas)
        {
            if (currentAngle >= opData.angleRange.x && currentAngle <= opData.angleRange.y)
            {
                newState = opData.opName;
                break;
            }
        }
        if (!string.IsNullOrEmpty(newState))
        {
            if (!newState.Equals(currentState) || !IgnoreSameState)
            {
                currentState = newState;
                OnDragFinish?.Invoke(currentState);
            }
        }
    }

    private void OnMouseEnter()
    {
        if (Interactable)
        {
            if (GlobalInfo.hasRole)
            {
                //todo
            }
            else
            {
                Camera.main.AutoComponent<CameraRotate>().SetEnable(false);
                Camera.main.AutoComponent<CameraMove>().enabled = false;
                Camera.main.AutoComponent<CameraZoom>().enabled = false;
            }
        }
        isMouseOver = true;
    }


    private void OnMouseOver()
    {
        if (Interactable)
        {
            if (GlobalInfo.hasRole)
            {

            }
            else
            {
                Camera.main.AutoComponent<CameraRotate>().SetEnable(false);
                Camera.main.AutoComponent<CameraMove>().enabled = false;
                Camera.main.AutoComponent<CameraZoom>().enabled = false;
            }
        }
        isMouseOver = true;
    }

    private void OnMouseExit()
    {
        isMouseOver = false;
        //执行操作过程中 不修改CameraControl
        if (Interactable)
        {
            if (GlobalInfo.hasRole)
            {

            }
            else
            {
                Camera.main.AutoComponent<CameraRotate>().SetEnable(true);
                Camera.main.AutoComponent<CameraMove>().enabled = true;
                Camera.main.AutoComponent<CameraZoom>().enabled = true;
            }
        }
    }

    private bool interactable;
    public bool Interactable
    {
        get { return interactable; }
        set
        {
            interactable = value;

            if (!interactable)
            {
                isMouseOver = false;
                isSelect = false;
                actualRotate = false;
            }
            //if(boxCollider)
            //    boxCollider.enabled = interactable;
        }
    }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);

        switch (msg.msgId)
        {
            case (ushort)SmallFlowModuleEvent.StartExecute:
                var msgStringBool = msg as MsgStringBool;
                //协同/考核非本人操作
                if (msgStringBool.arg2)
                    return;
                if (!string.IsNullOrEmpty(modelInfoID) && modelInfoID.Equals(msgStringBool.arg1))
                    Interactable = false;
                break;
            case (ushort)SmallFlowModuleEvent.CompleteExecute:
                if (!string.IsNullOrEmpty(modelInfoID) && modelInfoID.Equals((msg as MsgString).arg))
                    Interactable = true;
                break;
            case (ushort)SmallFlowModuleEvent.FocusChanged:
                if(GlobalInfo.ShouldProcess((msg as MsgBrodcastOperate).senderId))
                {
                    Interactable = false;
                }
                break;
        }
    }

    [Serializable]
    public class OpData
    {
        [Tooltip("操作名称")]
        public string opName;

        [Tooltip("操作触发角度范围")]
        public Vector2 angleRange;
    }
}