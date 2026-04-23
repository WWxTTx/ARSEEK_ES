using System;
using System.Collections.Generic;
using UnityEngine;
using UnityFramework.Runtime;

public class MouseDragMove : MonoBase
{
    public enum DragDirection
    {
        Horizontal,
        Vertical
    }

    public enum MovementAxis
    {
        X,
        Y,
        Z
    }

    public bool IgnoreSameState = true;
    public bool Reverse = false;

    public DragDirection Direction;
    public MovementAxis Axis;

    public bool Step;
    public bool Discrete;

    public float StepMoveDistance;
    public float StepMinDistance;

    public float minPosition = -1f;
    public float maxPosition = 1f;

    public float MoveSpeed = 1f;

    private Vector3 lastMousePosition;
    private bool isMouseOver = false;
    private float currentPosition = 0f;
    private float initialPosition;
    private Vector3 axis;

    [HideInInspector]
    public Action<string> OnDragFinish;

    [HideInInspector]
    public Action OnModelClicked;

    [HideInInspector]
    public Action onFail;

    public List<OpData> OpDatas = new List<OpData>();

    private string modelInfoID;
    private Transform targetTrans;
    private string currentState;

    private float xMoveRatio;
    private float yMoveRatio;

    protected override void InitComponents()
    {
        base.InitComponents();
        AddMsg(
            (ushort)SmallFlowModuleEvent.StartExecute,
            (ushort)SmallFlowModuleEvent.CompleteExecute,
            (ushort)SmallFlowModuleEvent.FocusChanged
        );

        xMoveRatio = Reverse ? -1 : 1;
        yMoveRatio = Reverse ? 1 : -1;
    }

    public void Setup(string id, string currentState, Transform target)
    {
        modelInfoID = id;
        this.currentState = currentState;
        targetTrans = target;
    }

    private bool isSelect = false;
    private float moveAmount;
    private float newPosition;
    private float actualMovement;
    private bool actualMove;
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
                case MovementAxis.X:
                    axis = Vector3.right;
                    currentPosition = targetTrans.localPosition.x;
                    break;
                case MovementAxis.Y:
                    axis = Vector3.up;
                    currentPosition = targetTrans.localPosition.y;
                    break;
                case MovementAxis.Z:
                    axis = Vector3.forward;
                    currentPosition = targetTrans.localPosition.z;
                    break;
            }
            initialPosition = currentPosition;
            lastMousePosition = Input.mousePosition;
        }

        if (Step)
        {
            #region move when release mouse
            //if (Input.GetMouseButtonUp(0) && isSelect)
            //{
            //    isSelect = false;
            //    actualMovement = 0f;

            //    Vector3 delta = Input.mousePosition - lastMousePosition;

            //    switch (Direction)
            //    {
            //        case DragDirection.Horizontal:
            //            if (Mathf.Abs(delta.x) > StepMinDistance)
            //            {
            //                moveAmount = (delta.x > 0 ? xMoveRatio : -xMoveRatio) * StepMoveDistance;
            //                newPosition = Mathf.Clamp(currentPosition + moveAmount, minPosition, maxPosition);
            //                actualMovement = newPosition - currentPosition;

            //                targetTrans.Translate(axis * actualMovement, Space.Self);

            //                lastMousePosition = Input.mousePosition;
            //                currentPosition = newPosition;

            //                DragEnd(actualMovement);
            //            }
            //            else
            //            {
            //                OnModelClicked?.Invoke();
            //            }
            //            break;
            //        case DragDirection.Vertical:
            //            if (Mathf.Abs(delta.y) > StepMinDistance)
            //            {
            //                moveAmount = (delta.y > 0 ? yMoveRatio : -yMoveRatio) * StepMoveDistance;
            //                newPosition = Mathf.Clamp(currentPosition + moveAmount, minPosition, maxPosition);
            //                actualMovement = newPosition - currentPosition;

            //                targetTrans.Translate(axis * actualMovement, Space.Self);

            //                lastMousePosition = Input.mousePosition;
            //                currentPosition = newPosition;

            //                DragEnd(actualMovement);
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

                if (!actualMove)
                    actualMove = delta.x != 0 || delta.y != 0;

                switch (Direction)
                {
                    case DragDirection.Horizontal:
                        if (Mathf.Abs(delta.x) > StepMinDistance && Mathf.Abs(currentPosition - initialPosition) < 0.001f)
                        {
                            moveAmount = (delta.x > 0 ? xMoveRatio : -xMoveRatio) * StepMoveDistance;
                            newPosition = Mathf.Clamp(currentPosition + moveAmount, minPosition, maxPosition);
                            actualMovement = newPosition - currentPosition;

                            targetTrans.Translate(axis * actualMovement, Space.Self);
                            lastMousePosition = Input.mousePosition;
                            currentPosition = newPosition;
                        }
                        break;
                    case DragDirection.Vertical:
                        if (Mathf.Abs(delta.y) > StepMinDistance && Mathf.Abs(currentPosition - initialPosition) < 0.001f)
                        {
                            moveAmount = (delta.y > 0 ? yMoveRatio : -yMoveRatio) * StepMoveDistance;
                            newPosition = Mathf.Clamp(currentPosition + moveAmount, minPosition, maxPosition);
                            actualMovement = newPosition - currentPosition;

                            targetTrans.Translate(axis * actualMovement, Space.Self);
                            lastMousePosition = Input.mousePosition;
                            currentPosition = newPosition;
                        }
                        break;
                }
            }

            if (Input.GetMouseButtonUp(0) && isSelect)
            {
                isSelect = false;
                if (actualMove)
                {
                    //if (!Discrete)
                    //{
                    //    ModelOperationEventManager.Publish(new DragEvent(this.modelInfoID, targetTrans.gameObject, currentPosition, currentPosition / (maxPosition - minPosition)));
                    //}
                    DragEnd(actualMovement);
                }
                else
                {
                    OnModelClicked?.Invoke();
                }
                actualMove = false;
            }
        }
        else
        {
            if (Input.GetMouseButton(0) && isSelect)
            {
                Vector3 delta = Input.mousePosition - lastMousePosition;

                if(!actualMove)
                    actualMove = delta.x != 0 || delta.y != 0;

                switch (Direction)
                {
                    case DragDirection.Horizontal:
                        if (Discrete)
                        {
                            int prevStep = CalculateStep();
                            accumulateDelta += delta.x * xMoveRatio;
                            int newStep = CalculateStep();

                            moveAmount = (newStep - prevStep) * StepMoveDistance;
                        }
                        else
                            moveAmount = delta.x * MoveSpeed * Time.deltaTime;

                        newPosition = Mathf.Clamp(currentPosition + moveAmount, minPosition, maxPosition);
                        actualMovement = newPosition - currentPosition;

                        targetTrans.Translate(axis * actualMovement, Space.Self);

                        lastMousePosition = Input.mousePosition;
                        currentPosition = newPosition;
                        break;
                    case DragDirection.Vertical:
                        if (Discrete)
                        {
                            int prevStep = CalculateStep();
                            accumulateDelta += delta.y * -yMoveRatio;
                            int newStep = CalculateStep();

                            moveAmount = (prevStep - newStep) * StepMoveDistance;
                        }
                        else
                            moveAmount = -delta.y * MoveSpeed * Time.deltaTime;

                        newPosition = Mathf.Clamp(currentPosition + moveAmount, minPosition, maxPosition);
                        actualMovement = newPosition - currentPosition;

                        targetTrans.Translate(axis * actualMovement, Space.Self);

                        lastMousePosition = Input.mousePosition;
                        currentPosition = newPosition;
                        break;
                }
            }

            if (Input.GetMouseButtonUp(0) && isSelect)
            {
                isSelect = false;
                accumulateDelta = 0f;
                if (actualMove)
                {
                    if (!Discrete)
                    {
                        ModelOperationEventManager.Publish(new DragEvent(this.modelInfoID, targetTrans.gameObject, currentPosition, currentPosition / (maxPosition - minPosition)));
                    }

                    DragEnd(actualMovement);
                }
                else
                {
                    OnModelClicked?.Invoke();
                }
                actualMove = false;
            }
        }
    }

    private int CalculateStep()
    {
        if(accumulateDelta < 0)
            return Mathf.CeilToInt(accumulateDelta / StepMinDistance);
        else
            return Mathf.FloorToInt(accumulateDelta / StepMinDistance);
    }

    private void DragEnd(float delta)
    {
        string newState = string.Empty;
        foreach (OpData opData in OpDatas)
        {
            if(currentPosition >= opData.positionRange.x && currentPosition <= opData.positionRange.y)
            {
                newState = opData.opName;
                break;
            }
        }
        if(!string.IsNullOrEmpty(newState))
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
        //if (Interactable)
        //{
        //    ModelManager.Instance.CameraControl = false;
        //}
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
        //if (Interactable && ModelManager.Instance.CameraControl)
        //{
        //    ModelManager.Instance.CameraControl = false;
        //}
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
        //if (Interactable)
        //    ModelManager.Instance.CameraControl = true;
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
                actualMove = false;
            }
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
                Interactable = true;
                break;
            case (ushort)SmallFlowModuleEvent.FocusChanged:
                if (GlobalInfo.ShouldProcess((msg as MsgBrodcastOperate).senderId))
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

        [Tooltip("操作触发位置范围")]
        public Vector2 positionRange;
    }
}
