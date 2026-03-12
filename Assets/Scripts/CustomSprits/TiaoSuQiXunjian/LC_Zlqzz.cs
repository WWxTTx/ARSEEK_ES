using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 励磁柜整流桥装置
/// </summary>
public class LC_Zlqzz : MonoBehaviour, IBaseBehaviour
{
    bool IBaseBehaviour.UseCallback(int step) => false;
    public Type GetStatusEnumType() => typeof(AvailableStatus);
    [SerializeField]
    public enum AvailableStatus
    {
        启动双风机 = 0,
        启动风机1 = 1,
        退柜 = 2,
        投入 = 3,
        故障复位 = 4,
        风机轮换 = 5,
    }

    public AvailableStatus availableStatus;
    public GameObject itemP;

    class MenuNode
    {
        public string menuText; // 菜单显示文字
        public List<MenuNode> children = new List<MenuNode>(); // 子菜单
    }

    MenuNode root;
    private void Awake()
    {
        // 创建菜单结构
        root = new MenuNode()
        {
            menuText = "主菜单",
            children = {
                new MenuNode() {
                    menuText = "模拟量",
                    children = {
                        new MenuNode() { menuText = "模拟量测量" },
                        new MenuNode() { menuText = "频率量测量" },
                        new MenuNode() { menuText = "参考及差值测量" },
                        new MenuNode() { menuText = "其它测量" },
                    }
                },new MenuNode() {
                    menuText = "状态量",
                    children = {
                        new MenuNode() { menuText = "硬压板" ,
                            children = {
                                new MenuNode() { menuText = "开入硬压板" },
                                new MenuNode() { menuText = "开出硬压板" },
                                new MenuNode() { menuText = "主回路硬压板" },
                                new MenuNode() { menuText = "风机硬压板" },
                                new MenuNode() { menuText = "其他硬压板" },
                                new MenuNode() { menuText = "备用硬压板" },
                            }},
                        new MenuNode() { menuText = "动作元件" ,
                            children = {
                                new MenuNode() { menuText = "故障动作元件" },
                                new MenuNode() { menuText = "报警动作元件" },
                           }},
                    }
                },new MenuNode() {
                    menuText = "报告显示",
                    children = {
                        new MenuNode() { menuText = "动作报告" },
                        new MenuNode() { menuText = "自检报告" },
                        new MenuNode() { menuText = "变位报告" },
                        new MenuNode() { menuText = "装置日志" },
                        new MenuNode() { menuText = "清除报告" },
                    }
                }, new MenuNode() {
                    menuText = "定值设置",
                    children = {
                        new MenuNode() { menuText = "设备参数定值" },
                        new MenuNode() { menuText = "采样系数定值" },
                        new MenuNode() { menuText = "额定参数定值" },
                        new MenuNode() { menuText = "控制定值" },
                        new MenuNode() { menuText = "辅助控制定值",
                            children = {
                                new MenuNode() { menuText = "模拟主套运行" },
                                new MenuNode() { menuText = "模拟并网" },
                                new MenuNode() { menuText = "模拟灭磁开关分位" },
                                new MenuNode() { menuText = "模拟灭磁开关合位" },
                                new MenuNode() { menuText = "强制启动风机" },
                                new MenuNode() { menuText = "风机启动模式" },
                           }},
                        new MenuNode() { menuText = "检测及自检定值" ,
                            children = {
                                new MenuNode() { menuText = "故障检测定值" },
                                new MenuNode() { menuText = "信号检测定值" },
                                new MenuNode() { menuText = "全部" },
                           }},
                        new MenuNode() { menuText = "功能软压板",
                            children = {
                                new MenuNode() { menuText = "远方修改定值" },
                                new MenuNode() { menuText = "智能均流投入" },
                                new MenuNode() { menuText = "整流桥自测使能" },
                                new MenuNode() { menuText = "风机定期切换" },
                                new MenuNode() { menuText = "脉冲检测使能" },
                                new MenuNode() { menuText = "异常切脉冲" },
                                new MenuNode() { menuText = "定值切脉冲" },
                                new MenuNode() { menuText = "刀闸容错报警" },
                                new MenuNode() { menuText = "出口传动使能" },
                           }},
                        new MenuNode() { menuText = "DA定值" },
                        new MenuNode() { menuText = "装置参数" },
                        new MenuNode() { menuText = "全部定值" },
                        new MenuNode() { menuText = "最近修改定值" },
                    }
                }, new MenuNode() {
                    menuText = "打印",
                    children = {
                        new MenuNode() { menuText = "装置描述" },
                        new MenuNode() { menuText = "定值设置" },
                        new MenuNode() { menuText = "动作报告" },
                        new MenuNode() { menuText = "自检报告" },
                        new MenuNode() { menuText = "变位报告" },
                        new MenuNode() { menuText = "装置状态" },
                        new MenuNode() { menuText = "打印取消" },
                    }
                },new MenuNode() {
                    menuText = "本地命令",
                    children = {
                        new MenuNode() { menuText = "下载允许" },
                        new MenuNode() { menuText = "手动录波" },
                        new MenuNode() { menuText = "故障复位" },
                        new MenuNode() { menuText = "清除统计" },
                    }
                },new MenuNode() {
                    menuText = "装置信息",
                    children = {
                        new MenuNode() { menuText = "版本信息" },
                        new MenuNode() { menuText = "板卡信息" },
                        new MenuNode() { menuText = "故障复位" },
                        new MenuNode() { menuText = "清除统计" },
                    }
                },new MenuNode() {
                    menuText = " 调试",
                    children = {
                        new MenuNode() { menuText = "出口传动" },
                        new MenuNode() { menuText = "条目动作报告" },
                        new MenuNode() { menuText = "时钟设置" },
                        new MenuNode() { menuText = "语言" },
                    }
                },
            }
        };

        //新建菜单1
        foreach (MenuNode item in root.children)
        {
            GameObject temp = Instantiate(itemP, Parents[0]);
            temp.GetComponent<TextMeshProUGUI>().text = item.menuText; 
            table1.Add(temp);
        }
        DOVirtual.DelayedCall(0.1f, () =>{
            OnExext();
        });
        
    }

    public List<MeshCollider> Butens;
    UnityAction callback;
    UISmallSceneModule smallSceneModule;

    void IBaseBehaviour.Execute(int step, UnityAction callback)
    {
        if(smallSceneModule == null)
        {
            smallSceneModule = FindObjectOfType<UISmallSceneModule>().GetComponent<UISmallSceneModule>();
        }
        this.callback = callback;

        availableStatus = (AvailableStatus)step;
        GuideTip();
    }

    void IBaseBehaviour.SetFinalState()
    {

    }

    /// <summary>
    /// 点击检测
    /// </summary>
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {  
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            foreach (var item in Butens)
            {
                bool isHovering = item.Raycast(ray, out hit, 2);
                if(isHovering)
                {
                    ButenEvent(hit.transform.name);
                }
            }
        }
    }


    int[] selectIndex = { 0,0,0};
    List<MenuNode> selectNode = new List<MenuNode>();
    List<GameObject> table1 = new List<GameObject>();
    List<GameObject> table2 = new List<GameObject>();
    List<GameObject> table3 = new List<GameObject>();
    /// <summary>
    /// 传入子菜单选中序号
    /// 如果被选中存在下级子菜单 添加
    /// -1 移除当前子菜单
    /// </summary>
    /// <param name="table"></param>
    void ChangeTable(bool ad)
    {
        int last = selectNode.Count - 1;
        if (ad)
        {
            List<MenuNode> next = selectNode[last].children;
            if (next[selectIndex[last]].children.Count > 0)
            {
                selectNode.Add(next[selectIndex[last]]);
                //新建时指向第一行
                if(last < 2)
                    selectIndex[last + 1] = 0;

                //新建菜单3
                if (selectNode.Count == 3)
                {
                    foreach (Transform t in Parents[2])
                    {
                        Destroy(t.gameObject);
                    }
                    table3.Clear();

                    Parents[2].gameObject.SetActive(true);
                    foreach (MenuNode item in selectNode[2].children)
                    {
                        GameObject temp = Instantiate(itemP, Parents[2]);
                        table3.Add(temp);
                        temp.name = item.menuText;
                        temp.GetComponent<TextMeshProUGUI>().text = item.menuText;
                    }
                    DOVirtual.DelayedCall(0.1f, () =>
                    {
                        selects[2].gameObject.SetActive(true);
                        selects[2].position = table3[0].transform.position;
                        table3[0].GetComponent<TextMeshProUGUI>().color = w;
                    });
                }
               
                //新建菜单2
                if (selectNode.Count == 2)
                {
                    foreach (Transform t in Parents[1])
                    {
                        Destroy(t.gameObject);
                    }
                    table2.Clear();

                    Parents[1].gameObject.SetActive(true);
                    foreach (MenuNode item in selectNode[1].children)
                    {
                        GameObject temp = Instantiate(itemP, Parents[1]);
                        table2.Add(temp);
                        temp.name = item.menuText;
                        temp.GetComponent<TextMeshProUGUI>().text = item.menuText;
                    }
                    DOVirtual.DelayedCall(0.1f, () =>
                    {
                        selects[1].gameObject.SetActive(true);
                        selects[1].position = table2[0].transform.position;
                        table2[0].GetComponent<TextMeshProUGUI>().color = w;
                    });
                }
            }
        }
        else
        {
            if (selectNode.Count > 1)
            {
                selectNode.Remove(selectNode[selectNode.Count - 1]);
            }
        }


        if (selectNode.Count < 3)
        {
            Parents[2].gameObject.SetActive(false);
            selects[2].gameObject.SetActive(false);
        }
        if (selectNode.Count < 2)
        {
            Parents[1].gameObject.SetActive(false);
            selects[1].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 使用设备网格名称作为按钮触发器
    /// </summary>
    /// <param name="eventname"></param>
    void ButenEvent(string eventname)
    {
        switch (eventname)
        { 
            case "up":
                ChangeSlect(-1);
                break; 
            case "down":
                ChangeSlect(1);
                break;
            case "left":
            case "cancel":
                ChangeTable(false);
                break;
            case "right":
                ChangeTable(true);
                break;
            case "true":
                ChangeTable(true);
                TryFindEvent();
                break;
        }
        DOVirtual.DelayedCall(0.1f, () =>
        {
            GuideTip();
        });
    }

    public List<Transform> selects;
    public List<Transform> Parents;

    public Color w;
    public Color b;
    /// <summary>
    /// 上下选择菜单
    /// </summary>
    void ChangeSlect(int add)
    {
        int last = selectNode.Count - 1;
        List<GameObject> lastlist = GetCerrenList(last);

        MenuNode curren = selectNode[last];
        //限制选中范围
        selectIndex[last] += add;
        if(selectIndex[last] < 0)
        {
            selectIndex[last] = 0;
        }
        if(selectIndex[last] > curren.children.Count - 1)
        {
            selectIndex[last] = curren.children.Count - 1;
        }

        //执行表现
        for (int i = 0; i < lastlist.Count; i++)
        {
            lastlist[i].GetComponent<TextMeshProUGUI>().color = b;
            if (i == selectIndex[last])
            {
                selects[last].position = lastlist[i].transform.position;
                lastlist[i].GetComponent<TextMeshProUGUI>().color = w;
            }
        }
    }

    List<GameObject> GetCerrenList(int last)
    {
        switch (last)
        {
            case 1:
                return table2;
            case 2:
                return table3;
        }
        return table1;
    }


    public Transform[] zhizhen;
    RenderTexture renderTexture;
    Camera monitorCamera;

    /// <summary>
    /// 开关监控相机
    /// </summary>
    /// <param name="show"></param>
    void Monitor(bool show)
    {
        // 控制显示
        smallSceneModule.CameraView.gameObject.SetActive(show);

        if (show)
        {
            // 检查 RenderTexture 是否需要重新创建
            if (renderTexture == null || !renderTexture.IsCreated())
            {
                // 销毁旧纹理（如果存在）
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Destroy(renderTexture);
                }

                // 创建新纹理
                renderTexture = new RenderTexture(456, 404, 0, RenderTextureFormat.ARGB32);
                renderTexture.wrapMode = TextureWrapMode.Clamp;
                renderTexture.filterMode = FilterMode.Bilinear;
                renderTexture.Create(); // 显式创建纹理
            }

            // 相机初始化
            if (monitorCamera == null)
            {
                monitorCamera = new GameObject("MonitorCamera").AutoComponent<Camera>();
                monitorCamera.targetTexture = renderTexture;
                monitorCamera.cullingMask = ~(1 << 2 | 1 << 5 | 1 << 7);
                monitorCamera.backgroundColor = "#505C73".HexToColor();
                monitorCamera.clearFlags = CameraClearFlags.SolidColor;
                monitorCamera.nearClipPlane = 0.01f;
            }

            monitorCamera.targetTexture = renderTexture;
            smallSceneModule.CameraView.GetComponentInChildren<RawImage>().texture = renderTexture;
            monitorCamera.transform.position = new Vector3(3.043f, 1.964f, 7.788f);
            monitorCamera.transform.eulerAngles = new Vector3(0, 0, 0);
        }
        else
        {
            if (monitorCamera != null)
                monitorCamera.targetTexture = null;
            if (renderTexture != null)
            {
                renderTexture.Release();
                renderTexture = null;
                DestroyImmediate(renderTexture);
            }
        }
        monitorCamera.gameObject.SetActive(show);  
    }


    /// <summary>
    /// 检查是否选中正确位置
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    bool EventTrager(int[] node)
    {
        bool allright = true;
        for (int i = 0; i < selectIndex.Length; i++)
        {
            if (selectIndex[i] != node[i])
                allright = false;
        }
        return allright;
    }


    /// <summary>
    /// 确定尝试找到可触发事件
    /// </summary>
    void TryFindEvent()
    {
        switch (availableStatus)
        {
            case AvailableStatus.启动双风机:
                if (EventTrager(new int[] { 3, 4, 5 }))
                {
                    smallSceneModule.ShowHint("已将“风机启动模式”置为0，风机同时启动", -1);
                    EventOut();
                }
                break;
            case AvailableStatus.风机轮换:
                if (EventTrager(new int[] { 3, 4, 5 }))
                {
                    smallSceneModule.ShowHint("已将“风机启动模式”置为1，风机轮换启动，轮换间隔720小时", -1);
                    EventOut();
                }
                break;
            case AvailableStatus.启动风机1:
                if (EventTrager(new int[] { 3, 4, 4 }))
                {
                    smallSceneModule.ShowHint("已将“强制启动风机”置为1，风机 A强制启动", -1);
                    EventOut();
                }
                break;
            case AvailableStatus.退柜:
                if (EventTrager(new int[] { 3, 6, 6 }))
                {
                    smallSceneModule.ShowHint("已将“定值切脉冲”置为1，功能软压板投入", -1);
                    zhizhen[0].DOLocalRotate(new Vector3(0, 15, 0), 0f);
                    zhizhen[1].DOLocalRotate(new Vector3(0, 15, 0), 0f);
                    Monitor(true);
                    zhizhen[0].DOLocalRotate(new Vector3(0, -9, 0), 2f);
                    zhizhen[1].DOLocalRotate(new Vector3(0, -9, 0), 2f);
                    DOVirtual.DelayedCall(4, () =>
                    {
                        Monitor(false);
                        smallSceneModule.ShowHint("整流柜直流输出已降至0", -1);
                        EventOut();
                    });
                }
                break;
            case AvailableStatus.投入:
                if (EventTrager(new int[] { 3, 6, 1 }))
                {
                    smallSceneModule.ShowHint("已将“智能均流投入”置为0，功能软压板退出", -1);
                    zhizhen[0].DOLocalRotate(new Vector3(0, -9, 0), 0f);
                    zhizhen[1].DOLocalRotate(new Vector3(0, -9, 0), 0f);
                    Monitor(true);
                    zhizhen[0].DOLocalRotate(new Vector3(0, 37, 0), 2f);
                    zhizhen[1].DOLocalRotate(new Vector3(0, 37, 0), 2f);
                    DOVirtual.DelayedCall(3, () =>
                    {
                        Monitor(false);
                        smallSceneModule.ShowHint("整流柜直流输出200A", -1);
                        EventOut();
                    });
                }
                break;
            case AvailableStatus.故障复位:
                if (EventTrager(new int[] { 5, 2, 0}))
                {
                    smallSceneModule.ShowHint("已复归故障信号", -1);
                    EventOut();
                }
                break;
        }
    }

    /// <summary>
    /// 设置UI指引位置
    /// </summary>
    void GuideTip()
    {
        switch (availableStatus)
        {
            case AvailableStatus.启动双风机:
            case AvailableStatus.风机轮换:
                SetGuideTip(new int[] { 3, 4, 5 });
                break;
            case AvailableStatus.启动风机1:
                SetGuideTip(new int[] { 3, 4, 4 });
                break;
            case AvailableStatus.退柜:
                SetGuideTip(new int[] { 3, 6, 6 });
        break;
            case AvailableStatus.投入:
                SetGuideTip(new int[] { 3, 6, 1 });
                break;
            case AvailableStatus.故障复位:
                SetGuideTip(new int[] { 5, 2, 0 });
                break;
        }
    }

    public Transform _tip;
    void SetGuideTip(int[] node)
    {
        _tip.gameObject.SetActive(true);
        int last = selectNode.Count - 1;
        List<GameObject> lastlist = GetCerrenList(last);
        for (int i = 0; i < lastlist.Count; i++)
        {
            if (i == node[last])
            {
                //手机端 UI重叠部分出现细线 微调深度值
                Vector3 temp = lastlist[i].transform.position;
                temp.z += -0.0008f;
                _tip.position = temp;
            }
        }
    }

    void EventOut()
    {
        DOVirtual.DelayedCall(2, () => {
            OnExext();
            smallSceneModule.ModelState = ModelState.Operated;
            callback?.Invoke();
        });
    }

    /// <summary>
    /// 退出时重置选中列表
    /// </summary>
    void OnExext()
    {
        for (int i = 0; i < selectIndex.Length; i++)
        {
            selectIndex[i] = 0;
        }

        selectNode.Clear();
        selectNode.Add(root);

        _tip.gameObject.SetActive(false);
        selects[1].gameObject.SetActive(false);
        selects[2].gameObject.SetActive(false);
        Parents[1].gameObject.SetActive(false);
        Parents[2].gameObject.SetActive(false);

        selects[0].position = table1[0].transform.position;
        foreach (GameObject item in table1)
        {
            item.GetComponent<TextMeshProUGUI>().color = b;
        }
        table1[0].GetComponent<TextMeshProUGUI>().color = w;
    }
}
