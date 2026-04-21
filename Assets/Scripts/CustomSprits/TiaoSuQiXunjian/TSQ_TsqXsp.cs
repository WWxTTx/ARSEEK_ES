using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityFramework.Runtime;

public class TSQ_TsqXsp : MonoBase, IBaseBehaviour
{
    bool IBaseBehaviour.UseCallback(int step) => false;
    public Type GetStatusEnumType() => typeof(AvailableStatus);

    // 在 Awake 中提前注册消息，避免 Start 时序晚于消息到达
    protected virtual void Awake()
    {
        AddMsg(new ushort[] {
            (ushort)SmallFlowModuleEvent.SynchronizationTsq
        });

        // 初始化 TextDic
        if (Texts != null)
        {
            foreach (var text in Texts)
            {
                if (text != null && !string.IsNullOrEmpty(text.name))
                {
                    TextDic[text.name] = text;
                }
            }
        }

        // 初始化 PanelDic
        if (UIPanel != null)
        {
            foreach (var panel in UIPanel)
            {
                if (panel != null && !string.IsNullOrEmpty(panel.name))
                {
                    PanelDic[panel.name] = panel;
                }
            }
        }
    }

    SmallFlowCtrl smallFlowCtrl;
    SmallFlowCtrl _SmallFlowCtrl { get { return smallFlowCtrl != null? smallFlowCtrl :
                 FindObjectOfType<SmallFlowCtrl>(); } }

    public override void ProcessEvent(MsgBase msg)
    {
        base.ProcessEvent(msg);
        if (msg.msgId == (ushort)SmallFlowModuleEvent.SynchronizationTsq)
        {
            MsgBrodcastOperate brodcastMsg = msg as MsgBrodcastOperate;
            MsgSyncCustomUI msgUI = brodcastMsg.GetData<MsgSyncCustomUI>();
            if (brodcastMsg.senderId != GlobalInfo.account.id)
            {
                WaitStepInit(msgUI).Forget();
            }
        }
    }

    async UniTaskVoid WaitStepInit(MsgSyncCustomUI msgUI)
    {
        //将表现内容和步骤分开 同步时只执行表现内容 不设置步骤 等待操作者使用结束消息触发下一步
        if (steps.Count == 0)
        {
               DealEvent((AvailableStatus)msgUI.status);
        SetImageRaycast(true);
        }
        else if(steps.Count <= msgUI.stepIndex)
        {
            callback?.Invoke();
            currentStepIndex = msgUI.stepIndex;
            SetTip();
            return;
        }

        await UniTask.WaitUntil(() => steps.Count > 0, cancellationToken: this.GetCancellationTokenOnDestroy());

        for (int i = currentStepIndex; i < msgUI.stepIndex && i < steps.Count; i++)
        {
            ExecuteButtonEvent(steps[i]);
        }

        await UniTask.Yield();
        currentStepIndex = msgUI.stepIndex;
        SetTip();
        SetImageRaycast(true);
    }

    [SerializeField]
    public enum AvailableStatus
    {
        巡检 = 0,
        查看故障信息,
        传感器定位试验,
        xx,

        旋钮开度给定13,
        开机时间,
        关机时间,
        紧急停机时间,
        齿盘测频A,
        齿盘测频B,
        零点标定,
        测量增益,
        打开报警界面,
        跟踪频给,
        跟踪网频,
        静特性试验,
        开机不计时,
        残压测频A,
        残压测频B,
        设置水头,
        空载扰动试验,
        摆动试验,
        频率PID整定,
        空载PID整定,
        开度调节,
        频率调节,
        报告预览,
        甩负荷试验25,
        甩负荷试验50,
        甩负荷试验75,
        甩负荷试验100,
    }

    UnityAction callback;
    UnityAction Othercallback;
    UISmallSceneModule smallSceneModule;
    AvailableStatus status;
    void IBaseBehaviour.Execute(int step, UnityAction callback)
    {
        if (smallSceneModule == null)
        {
            smallSceneModule = Transform.FindObjectOfType<UISmallSceneModule>().GetComponent<UISmallSceneModule>();
        }
        Othercallback = callback;
        Othercallback += () =>
        {
            SetImageRaycast(true);
            ClousAllTip();
        };

        status = (AvailableStatus)step;
        DealEvent(status);
    }

    public List<Text> Texts;
    public List<Button> UIButtons;
    public List<GameObject> UIPanel;
    public List<Sprite> Reports;
    public Dictionary<string, Text> TextDic = new Dictionary<string, Text>();
    public Dictionary<string, GameObject> PanelDic = new Dictionary<string, GameObject>();

    /// <summary>
    /// 处理流程中涉及的表现
    /// </summary>
    public void DealEvent(AvailableStatus status)
    {
        switch (status)
        {
            case AvailableStatus.巡检:

                string[] flow0 = { "通信状态", "频率t", "桨叶开度t", "导叶开度t", "水头t", "B机t" };
                steps = flow0.ToList();
                StartFlow();
                callback = () =>
                {
                    ButenEvent("切到B");
                    DOVirtual.DelayedCall(3, () =>
                    {
                        Othercallback?.Invoke();
                        
                    });
                };
                break;
            case AvailableStatus.查看故障信息:
                string[] flow1 = { "事件报告" };
                steps = flow1.ToList();
                StartFlow();
                break;
            case AvailableStatus.传感器定位试验:
                Monitor(true);
                kd = float.Parse(TextDic["导叶开度"].text);
                RotationControl((int)kd, 0);

                if (!PanelDic["操作界面"].gameObject.activeSelf)
                {
                    string[] flow = { "主操作画面", "开度给定增加", "开度给定确认", "开度给定减少", "开度给定确认" };
                    steps = flow.ToList();
                }
                else
                {
                    string[] flow = { "开度给定增加", "开度给定确认", "开度给定减少", "开度给定确认" };
                    steps = flow.ToList();
                }
                StartFlow();

                callback = () =>
                {
                    smallSceneModule.ShowHint("传感器反馈的导叶开度与导叶开度指示标尺一致", -1);
                    DOVirtual.DelayedCall(3, () =>
                    {
                        Monitor(false);
                        Othercallback?.Invoke();
                        
                    });
                };
                break;
            case AvailableStatus.旋钮开度给定13:
                SetUIPanel("操作界面");
                int mbkd = 13;
                RotationControl(1, 0);
                DOVirtual.DelayedCall(2f, () => {
                    kdgd.DOLocalRotate(new Vector3(-90, 35, 0), 1);
                });
                DOVirtual.DelayedCall(3, () =>
                {
                    ChangeFouces(true);
                    Monitor(true);
                    RotationControl(mbkd, 6);
                    DOTween.To(() => 0f, x =>
                    {
                        TextDic["机组频率"].text = x.ToString("F2");
                        TextDic["齿盘测频"].text = x.ToString("F2");
                        TextDic["机组转速"].text = (x * 2 - 0.1f).ToString("F2");
                    }, 50, 6);
                    DOTween.To(() => 0f, x =>
                    {
                        TextDic["导叶目标"].text = x.ToString("F2");
                        TextDic["导叶目标值"].text = x.ToString("F2");
                        TextDic["导叶开度"].text = x.ToString("F2");
                        CalculateBladeOpening(x);
                    }, mbkd, 6).OnComplete(() =>
                    {
                        ChangeFouces(false);
                        DOVirtual.DelayedCall(1, () =>
                        {
                            kdgd.DOLocalRotate(new Vector3(270, 0, 0), 1);
                        });
                        DOVirtual.DelayedCall(3, () =>
                        {
                            Monitor(false);
                            Othercallback?.Invoke();
                            
                        });
                    });
                });
                break;
            case AvailableStatus.开机时间:
            case AvailableStatus.开机不计时:
                bool isusetime = status == AvailableStatus.开机时间;

                RotationControl(0, 0);
                float usetime = 10f;
                DOVirtual.DelayedCall(1, () =>
                {
                    kdgd.DOLocalRotate(new Vector3(-90, 35, 0), 1);
                });

                DOVirtual.DelayedCall(2, () =>
                {
                    jsq.DOLocalMove(new Vector3(-0.28f, 0.22f, -0.22f), 0);
                    jsq.gameObject.SetActive(true);

                    DOTween.To(() => 0f, (x) =>
                    {
                        jsqNum.text = ConvertTimeToSpriteString(x * 30.4f);
                    }, 1f, usetime).SetEase(Ease.Linear);
                    DOVirtual.DelayedCall(12, () =>
                    {
                        jsq.DOLocalMove(new Vector3(-0.074f, 0.128f, -0.22f), 0);
                        jsq.gameObject.SetActive(false);
                    });
                });

                DOVirtual.DelayedCall(3, () =>
                {
                    ChangeFouces(true);
                    Monitor(true);
                    RotationControl(99, usetime);
                    if (isusetime)
                    {
                        smallSceneModule.ShowHint($"T1阶段0.65秒，导叶开始动作", 1);
                        DOVirtual.DelayedCall(6, () =>
                        {
                            smallSceneModule.ShowHint($"T2阶段23.2秒，导叶继续开启", 2);
                        });
                    }

                    DOTween.To(() => 0f, x =>
                    {
                        TextDic["桨叶反馈工程值"].text = ((int)x).ToString("F2");
                        TextDic["实测值"].text = Mathf.Clamp(x / 100, 15470, 29119).ToString("F2");
                        TextDic["导叶开度"].text = x.ToString("F2");
                        TextDic["导叶目标"].text = x.ToString("F2");
                        TextDic["导叶目标值"].text = x.ToString("F2");
                        CalculateBladeOpening(x);
                    }, 99.00f, usetime).
                    OnComplete(() =>
                    {
                        ChangeFouces(false);
                        DOVirtual.DelayedCall(1, () =>
                        {
                            kdgd.DOLocalRotate(new Vector3(270, 0, 0), 1);
                        });
                        DOVirtual.DelayedCall(3, () =>
                        {
                            Monitor(false);
                            Othercallback?.Invoke();
                            
                        });
                    });
                });
                break;
            case AvailableStatus.关机时间:
            case AvailableStatus.紧急停机时间:
                RotationControl(99, 0);
                DOVirtual.DelayedCall(1, () =>
                {
                    kdgd.DOLocalRotate(new Vector3(-90, -35, 0), 1);
                });

                DOVirtual.DelayedCall(2, () =>
                {
                    jsq.DOLocalMove(new Vector3(-0.28f, 0.22f, -0.22f), 0);
                    jsq.gameObject.SetActive(true);

                    ChangeFouces(true);
                    Monitor(true);

                    float clousetime = 14;
                    if (status != AvailableStatus.紧急停机时间)
                    {
                        clousetime = 13.3f;
                    }

                    DOTween.To(() => 0f, (x) =>
                    {
                        jsqNum.text = ConvertTimeToSpriteString(x * clousetime);
                    }, 1f, clousetime).SetEase(Ease.Linear);

                    DOVirtual.DelayedCall(clousetime, () =>
                    {
                        jsq.DOLocalMove(new Vector3(-0.074f, 0.128f, -0.22f), 0);
                        jsq.gameObject.SetActive(false);
                    });

                    RotationControl(32, 3.2f);
                    DOTween.To(() => 99f, x =>
                    {
                        TextDic["桨叶反馈工程值"].text = ((int)x).ToString("F2");
                        TextDic["实测值"].text = Mathf.Clamp(x / 100, 15470, 29119).ToString("F2");
                        TextDic["导叶开度"].text = x.ToString("F2");
                        TextDic["导叶目标"].text = x.ToString("F2");
                        TextDic["导叶目标值"].text = x.ToString("F2");
                        CalculateBladeOpening(x);
                    }, 32f, 3.3f);

                    smallSceneModule.ShowHint($"T1阶段3.2秒，导叶快速关闭", 1);
                    DOVirtual.DelayedCall(3.2f, () =>
                    {
                        DOTween.To(() => 32f, x =>
                        {
                            TextDic["桨叶反馈工程值"].text = ((int)x).ToString("F2");
                            TextDic["实测值"].text = Mathf.Clamp(x / 100, 15470, 29119).ToString("F2");
                            TextDic["导叶开度"].text = x.ToString("F2");
                            TextDic["导叶目标"].text = x.ToString("F2");
                            TextDic["导叶目标值"].text = x.ToString("F2");
                            CalculateBladeOpening(x);
                        }, 0f, 10f);

                        RotationControl(0, 9.8f);
                        DOVirtual.DelayedCall(9.8f, () =>
                        {
                            smallSceneModule.ShowHint($"T2阶段 9.8秒，导叶开度缓慢关闭", 2);
                        });
                    });


                    DOVirtual.DelayedCall(clousetime, () =>
                    {
                        ChangeFouces(false);
                        DOVirtual.DelayedCall(1, () =>
                        {
                            kdgd.DOLocalRotate(new Vector3(270, 0, 0), 1);
                        });
                        DOVirtual.DelayedCall(3, () =>
                        {
                            Monitor(false);
                            Othercallback?.Invoke();
                            
                        });
                    });
                });
                break;
            case AvailableStatus.齿盘测频A:
                ButenEvent("切到A");
                ChipanCepin();
                break;
            case AvailableStatus.齿盘测频B:
                ButenEvent("切到B");
                ChipanCepin();
                break;
            case AvailableStatus.残压测频A:
                ButenEvent("切到A");
                CnayaCepin();
                break;
            case AvailableStatus.残压测频B:
                ButenEvent("切到B");
                CnayaCepin();
                break;
            case AvailableStatus.零点标定:
                if (!PanelDic["导叶及主配反馈整定"].gameObject.activeSelf)
                {
                    string[] flow = { "参数设定", "导叶及主配反馈整定", "测量零点" };
                    steps = flow.ToList();
                }
                else
                {
                    string[] flow = { "测量零点" };
                    steps = flow.ToList();
                }
                StartFlow();
                break;
            case AvailableStatus.测量增益:
                if (!PanelDic["导叶及主配反馈整定"].gameObject.activeSelf)
                {
                    string[] flow = { "参数设定", "导叶及主配反馈整定", "测量增益" };
                    steps = flow.ToList();
                }
                else
                {
                    string[] flow = { "测量增益" };
                    steps = flow.ToList();
                }
                StartFlow();
                break;
            case AvailableStatus.打开报警界面:
                if (!PanelDic["事件报警"].gameObject.activeSelf)
                {

                    string[] flow2 = { "事件报告" };
                    steps = flow2.ToList();
                    StartFlow();
                }
                else
                {
                    DOVirtual.DelayedCall(2, () =>
                    {
                        Othercallback?.Invoke();
                        
                    });
                }
                break;
            case AvailableStatus.跟踪频给:
                if (!PanelDic["操作界面"].gameObject.activeSelf)
                {
                    string[] flow = { "主操作画面", "跟踪频给" };
                    steps = flow.ToList();
                }
                else
                {
                    string[] flow = { "测量增益" };
                    steps = flow.ToList();
                }
                StartFlow();
                break;
            case AvailableStatus.跟踪网频:
                if (!PanelDic["操作界面"].gameObject.activeSelf)
                {
                    string[] flow = { "主操作画面", "跟踪网频" };
                    steps = flow.ToList();
                }
                else
                {
                    string[] flow = { "跟踪网频" };
                    steps = flow.ToList();
                }
                StartFlow();
                break;
            case AvailableStatus.静特性试验:
                if (!PanelDic["静态试验"].gameObject.activeSelf)
                {
                    string[] flow5 = { "跟踪频给", "试验", "静特性试验", "设置PID", "开始静态试验" };
                    steps = flow5.ToList();
                }
                else
                {
                    string[] flow5 = {"开始静态试验" };
                    steps = flow5.ToList();
                }
                StartFlow();

                callback = () =>
                {
                    LineGraph[] graphs = PanelDic["静态试验"].GetComponentsInChildren<LineGraph>();
                    LineGraph kfx = graphs[0];
                    LineGraph gfx = graphs[1];
                    foreach (var item in graphs)
                    {
                        item.Clear();
                    }

                    for (int i = 0; i < 11; i++)
                    {
                        kfx.points.Add(i * 10 + UnityEngine.Random.Range(-1f, 1f));
                        gfx.points.Add(i * 10 + UnityEngine.Random.Range(-1f, 1f));
                    }

                    //绘制过程参数控制
                    smallSceneModule.ShowHint("正在绘制开方向，对应频率从50赫兹到52赫兹，桨叶开度", 3);
                    DOTween.To(() => 0f, x =>
                    {
                        TextDic["频率给定"].text = (50 + x * 2).ToString("F2");
                        kfx.progress = x;
                        kfx.SetVerticesDirty();
                    }, 1f, 8).SetEase(Ease.Linear);

                    DOVirtual.DelayedCall(9, () =>
                    {
                        smallSceneModule.ShowHint("正在绘制关方向，对应频率从50赫兹到52赫兹，桨叶开度", 4);
                        DOTween.To(() => 0f, x =>
                        {
                            TextDic["频率给定"].text = (50 + x * 2).ToString("F2");
                            gfx.progress = x;
                            gfx.SetVerticesDirty();
                        }, 1f, 8).SetEase(Ease.Linear);
                    });

                    DOVirtual.DelayedCall(18, () =>
                    {
                        PanelDic["试验结果"].SetActive(true);
                        if (TextDic["主用"].text == "A机")
                        {
                            smallSceneModule.ShowHint("试验最大转速死区小于0.012%，试验结论：合格", 5);
                            PanelDic["试验结果"].transform.Find("A机结果").gameObject.SetActive(true);
                            PanelDic["试验结果"].transform.Find("B机结果").gameObject.SetActive(false);
                        }
                        else
                        {
                            smallSceneModule.ShowHint("试验最大转速死区小于0.011%，试验结论：合格", 5);
                            PanelDic["试验结果"].transform.Find("B机结果").gameObject.SetActive(true);
                            PanelDic["试验结果"].transform.Find("A机结果").gameObject.SetActive(false);
                        }
                    });
                    DOVirtual.DelayedCall(24, () =>
                    {
                        Othercallback.Invoke();
                        
                    });
                };
                break;
            case AvailableStatus.设置水头:
                string[] flow7 = { "参数设定", "功率及水头反馈整定", "水位确认给定" };
                steps = flow7.ToList();
                StartFlow();
                callback = () =>
                {
                    DOTween.To(() => 0, x =>
                    {
                        TextDic["水头"].text = x.ToString("F1");
                    }, 12, 2);
                    DOVirtual.DelayedCall(4, () =>
                    {
                        Othercallback.Invoke();
                        
                    });
                };
                break;
            case AvailableStatus.摆动试验:
                if (!PanelDic["摆动试验"].activeSelf)
                {
                    string[] flow8 = { "试验", "空载频率摆动试验", "b电气开限", "b跟踪频给", "摆动试验开始" };
                    steps = flow8.ToList();
                }
                else
                {
                    string[] flow8 = { "摆动试验开始" };
                    steps = flow8.ToList();
                }
                StartFlow();

                callback = () =>
                {
                    LineGraph daoyeKd = PanelDic["摆动试验"].GetComponentInChildren<LineGraph>();

                    //设置绘制的曲线
                    daoyeKd.points.Clear();
                    float[] Abd = { 49.965f, 49.902f, 49.94f, 49.975f, 49.989f, 49.87f, 50.02f, 50.01f, 50, 49.98f, 49.945f, 49.99f, 50.042f, 49.96f, 50.02f };

                    if (TextDic["主用"].text == "A机")
                        daoyeKd.points = Abd.ToList();
                    else
                        daoyeKd.points = Abd.Reverse().ToList();

                    //绘制过程参数控制
                    daoyeKd.progress = 0;
                    TextDic["导叶目标"].text = "14";
                    TextDic["导叶目标值"].text = "14";
                    DOTween.To(() => 0f, x =>
                    {
                        daoyeKd.progress = x;
                        daoyeKd.SetVerticesDirty();
                    }, 1f, 10).SetEase(Ease.Linear);

                    DOVirtual.DelayedCall(7f, () =>
                    {
                        smallSceneModule.ShowHint(string.Format("最高频率{0}HZ，最低频率{1}HZ,频率偏差{2}%", 50.05f, 49.88f, 0.24), 1);
                    });

                    DOVirtual.DelayedCall(12f, () =>
                    {
                        TextDic["b频率偏差"].text = "0.24";
                        TextDic["b最低频率"].text = "49.88";
                        TextDic["b最高频率"].text = "50.05";
                        daoyeKd.progress = 1;
                        daoyeKd.SetVerticesDirty();
                    });


                    DOVirtual.DelayedCall(14, () =>
                    {
                        Othercallback.Invoke();
                        
                    });
                };
                break;
            case AvailableStatus.频率PID整定:
                string[] flow11 = { "参数设定", "PID参数整定", "频率调节PID" };
                steps = flow11.ToList();
                StartFlow();
                break;
            case AvailableStatus.空载PID整定:
                string[] flow12 = { "参数设定", "PID参数整定", "空载调节PID" };
                steps = flow12.ToList();
                StartFlow();
                break;
            case AvailableStatus.开度调节:
                if (!PanelDic["操作界面"].activeSelf)
                {
                    string[] flow26 = { "主操作画面", "开度调节" };
                    steps = flow26.ToList();

                }
                else
                {
                    string[] flow26 = { "开度调节" };
                    steps = flow26.ToList();
                }
                StartFlow();
                break;
            case AvailableStatus.频率调节:
                if (!PanelDic["操作界面"].activeSelf)
                {
                    string[] flow27 = { "主操作画面", "频率调节" };
                    steps = flow27.ToList();
                }
                else
                {
                    string[] flow27 = { "频率调节" };
                    steps = flow27.ToList();
                }
                StartFlow();
                break;
            case AvailableStatus.空载扰动试验:
                TextDic["r扰动频率"].text = "50.00";
                if (TextDic["主用"].text == "A机")
                {
                    string[] flow6 = { "试验", "空载频率扰动试验", "r频率增", "r跟踪频给", "r试验开始" };
                    steps = flow6.ToList();
                }
                else
                {
                    string[] flow6 = { "r频率减", "r跟踪频给", "r试验开始" };
                    steps = flow6.ToList();
                }
                StartFlow();

                callback = () =>
                {
                    LineGraph[] graphs = PanelDic["扰动试验"].GetComponentsInChildren<LineGraph>();

                    LineGraph jizuPl = graphs[0];
                    LineGraph kdMb = graphs[1];
                    LineGraph kdDy = graphs[2];
                    kdMb.Xmin = 13.5f;
                    kdMb.Xmax = 25;
                    kdDy.Xmin = 13.5f;
                    kdDy.Xmax = 25;
                    if (TextDic["主用"].text == "A机")
                    {
                        float[] red1 = new float[]
                        {
50.00f,49.99f,49.98f,49.97f,49.96f,49.95f,49.96f,50.00f,50.10f,50.30f,
50.55f,50.80f,51.05f,51.30f,51.50f,51.65f,51.80f,51.90f,51.98f,52.02f
                        };

                        float[] blue1 = new float[]
                        {
13.8f,13.8f,13.9f,14.0f,14.1f,14.2f,15.0f,18.5f,22.5f,23.5f,
23.5f,22.8f,21.5f,20.2f,19.0f,18.0f,17.2f,16.5f,16.0f,15.6f
                        };

                        float[] green1 = new float[]
                        {
13.8f,13.9f,14f,14.1f,14.2f,15f,23.5f,24.2f,24.4f,23.8f,
22.8f,21.6f,20.4f,19.2f,18.2f,17.5f,16.8f,16.3f,15.6f,15.2f
                        };

                        jizuPl.points = red1.ToList();
                        kdMb.points = blue1.ToList();
                        kdDy.points = green1.ToList();
                    }
                    else
                    {
                        kdMb.Xmin = 6;
                        kdMb.Xmax = 14.5f;
                        kdDy.Xmin = 6;
                        kdDy.Xmax = 14.5f;
                        float[] red2 = new float[]
                      {
51.90f,51.91f,51.92f,51.93f,51.94f,51.95f,51.94f,51.90f,51.80f,51.60f,
51.35f,51.05f,50.80f,50.55f,50.35f,50.20f,50.10f,50.02f,49.98f,49.95f
                      };

                        float[] blue2 = new float[]
                        {
14.5f,14.5f,14.5f,14.5f,14.5f,14.5f,14.4f,12.5f,10.5f,8.8f,
7.8f,7.5f,7.6f,8.5f,9.5f,10.5f,11.6f,12.0f,12.5f,12.8f
                        };

                        float[] green2 = new float[]
                        {
14.5f,14.5f,14.5f,14.5f,14.5f,14.0f,6.5f,6.2f,6.0f,6.1f,
6.8f,7.8f,8.8f,9.8f,10.8f,11.5f,12.0f,12.5f,12.8f,13.0f
                        };
                        jizuPl.points = red2.ToList();
                        kdMb.points = blue2.ToList();
                        kdDy.points = green2.ToList();
                    }



                    //绘制过程参数控制
                    foreach (var item in graphs)
                    {
                        item.progress = 0;
                    }

                    DOTween.To(() => 0f, x =>
                    {
                        foreach (var item in graphs)
                        {
                            item.progress = x;
                            item.SetVerticesDirty();
                        }
                    }, 1f, 10).SetEase(Ease.Linear);

                    DOVirtual.DelayedCall(5f, () =>
                    {
                        if (TextDic["主用"].text == "A机")
                        {
                            smallSceneModule.ShowHint(string.Format("最低频率49.91，最高频率52.19，超调量：9.36%，调节时间：13.99秒"), 1);
                        }
                        else
                        {
                            smallSceneModule.ShowHint(string.Format("最低频率49.92，最高频率51.95，超调量：0.08%，调节时间：9.56秒"), 1);
                        }
                        if (TextDic["主用"].text == "A机")
                        {
                            TextDic["r最低频率"].text = "49.91";
                            TextDic["r最高频率"].text = "52.19";
                            TextDic["r超调量"].text = "9.36";
                            TextDic["r调节时间"].text = "13.99";
                        }
                        else
                        {
                            TextDic["r最低频率"].text = "49.92";
                            TextDic["r最高频率"].text = "51.95";
                            TextDic["r超调量"].text = "0.08";
                            TextDic["r调节时间"].text = "9.56";
                        }
                    });

                    DOVirtual.DelayedCall(12f, () =>
                    {
                        foreach (var item in graphs)
                        {
                            item.progress = 1;
                            item.SetVerticesDirty();
                        }

                        DOVirtual.DelayedCall(2, () =>
                        {
                            Othercallback.Invoke();
                            
                        });
                    });
                };
                break;
            case AvailableStatus.甩负荷试验25:
            case AvailableStatus.甩负荷试验50:
            case AvailableStatus.甩负荷试验75:
            case AvailableStatus.甩负荷试验100:
                LineGraph[] graphs = PanelDic["甩负荷试验"].GetComponentsInChildren<LineGraph>();

                LineGraph jizuPl = graphs[0];
                LineGraph kdMb = graphs[1];
                LineGraph kdDy = graphs[2];

                float max = 0;
                float min = 0;
                float move = 0f;
                float nomone = 0f;
                float[] green = {
0f, 0f, 0f, 0f, 4.7f,
19.4f, 30.0f, 30.0f, 29.0f, 26.0f,
23.0f, 20.0f, 17.0f, 14.0f, 12.0f,
11.3f, 11.0f, 10.7f, 10.3f, 11.0f
};
                switch (status)
                {
                    case AvailableStatus.甩负荷试验25:
                        name = "甩25%负荷试验";
                        float[] red25 = {
    31.0f, 47.0f, 63.0f, 73.4f, 65.3f,  // 到达峰值并开始下落
    46.0f, 42.0f, 39.0f, 37.0f, 35.0f,
    34.0f, 33.0f, 32.5f, 32.0f, 31.8f,
    31.6f, 31.4f, 31.2f, 31.1f, 31.0f   // 平滑收敛到稳态
};

                        float[] blue25 = {
    82.0f, 62.3f, 46.1f, 33.0f, 21.8f,  // 从最高点快速跌落
    16.0f, 15.5f, 15.0f, 14.7f, 14.4f,
    14.1f, 13.9f, 13.7f, 13.5f, 13.4f,
    13.3f, 13.2f, 13.1f, 13.0f, 13.0f   // 平滑靠近最终开度
};
                        max = 55.40f;
                        min = 49.20f;
                        nomone = 0.05f;
                        move = 18.60f;
                        jizuPl.points = red25.ToList();
                        kdMb.points = blue25.ToList();
                        kdDy.points = green.ToList();
                        break;
                    case AvailableStatus.甩负荷试验50:
                        name = "甩50%负荷试验";
                        // 红：频率（超调约 +3.5%）
                        float[] red50 = {
    31.0f, 49.7f, 68.4f, 80.7f, 71.2f,  // 峰值后开始下落
    52.0f, 45.0f, 40.0f, 37.0f, 35.0f,
    34.0f, 33.0f, 32.5f, 32.0f, 31.8f,
    31.6f, 31.4f, 31.2f, 31.1f, 31.0f   // 直接向稳态收敛
};
                        float[] blue50 = {
    82.0f, 64.8f, 49.9f, 36.5f, 24.5f,  // 从最高点跌落
    17.0f, 16.0f, 15.5f, 15.0f, 14.7f,
    14.4f, 14.1f, 13.9f, 13.7f, 13.5f,
    13.4f, 13.3f, 13.1f, 13.0f, 13.0f   // 单调收敛到稳态
};
                        max = 59.85f;
                        min = 48.60f;
                        nomone = 0.06f;
                        move = 32.40f;
                        jizuPl.points = red50.ToList();
                        kdMb.points = blue50.ToList();
                        kdDy.points = green.ToList();
                        break;
                    case AvailableStatus.甩负荷试验75:
                        name = "甩75%负荷试验";
                        float[] red75 = {
    31.0f, 52.1f, 73.2f, 87.0f, 76.2f,
    51.1f, 26.9f, 18.6f, 17.5f, 20.5f,
    23.6f, 26.7f, 29.8f, 33.3f, 35.2f,
    35.9f, 35.4f, 34.1f, 32.5f, 31.0f
};
                        float[] blue75 = {
    82.0f, 67.0f, 52.9f, 39.7f, 27.1f,
    15.4f, 10.6f, 17.9f, 24.7f, 26.0f,
    23.3f, 21.2f, 19.1f, 17.1f, 15.3f,
    14.0f, 13.7f, 13.4f, 13.2f, 13.0f
};
                        max = 61.10f;
                        min = 47.30f;
                        nomone = 0.07f;
                        move = 44.80f;
                        jizuPl.points = red75.ToList();
                        kdMb.points = blue75.ToList();
                        kdDy.points = green.ToList();
                        break;
                    case AvailableStatus.甩负荷试验100:
                        name = "甩100%负荷试验";
                        float[] red = {
31f, 55.1f, 79.2f, 95.0f, 82.7f,
54.0f, 25.3f, 13.7f, 12.1f, 16.4f,
20.7f, 25.0f, 29.3f, 33.6f, 35.8f,
36.6f, 36.0f, 34.5f, 32.7f, 31.0f
};
                        float[] blue = {
82f, 69.4f, 56.8f, 44.2f, 31.6f,
19.0f, 9.0f, 16.3f, 23.6f, 26.1f,
24.0f, 21.9f, 19.8f, 17.7f, 15.6f,
13.5f, 12.8f, 12.3f, 12.0f, 13.0f
};
                        max = 62.68f;
                        min = 46.23f;
                        nomone = 0.07f;
                        move = 54.36f;
                        jizuPl.points = red.ToList();
                        kdMb.points = blue.ToList();
                        kdDy.points = green.ToList();
                        break;
                }

                callback = () =>
                {
                    foreach (var g in graphs) g.progress = 0;

                    DOTween.To(() => 0f, x =>
                    {
                        foreach (var g in graphs)
                        {
                            g.progress = x;
                            g.SetVerticesDirty();
                        }
                    }, 1f, 10).SetEase(Ease.Linear);

                    // ---------- 结束提示 ----------
                    DOVirtual.DelayedCall(6f, () =>
                    {
                        smallSceneModule.ShowHint(string.Format("最高频率：{0:F2} Hz," + "最低频率：{1:F2} %,不动时间:{2:F2} s,调节时间:{3:F2}s", max, min, nomone, move), 1);
                        TextDic["s最低频率"].text = min.ToString("F2");
                        TextDic["s最高频率"].text = max.ToString("F2");
                        TextDic["s不动时间"].text = nomone.ToString("F2");
                        TextDic["s调节时间"].text = move.ToString("F2");
                    });

                    DOVirtual.DelayedCall(14, () =>
                    {
                        Othercallback.Invoke();
                        
                    });
                };

                if (!PanelDic["甩负荷试验"].activeSelf)
                {
                    steps = new List<string> { "试验", "甩负荷试验", "负荷目标" };
                }
                else
                {
                    steps = new List<string> { "负荷目标" };
                }
                StartFlow();

                break;
        }
    }


    void ChipanCepin()
    {
        SetUIPanel("操作界面");
        Settip(TextDic["齿盘测频"], true);
        Settip(TextDic["机组转速"], true);
        DOTween.To(() => 0f, x =>
        {
            TextDic["机组频率"].text = x.ToString("F2");
            TextDic["齿盘测频"].text = x.ToString("F2");
            TextDic["机组转速"].text = (x * 2).ToString("F2");
        }, 50f, 5).OnComplete(() =>
        {
            DOVirtual.DelayedCall(3, () =>
            {
                Settip(TextDic["齿盘测频"], false);
                Settip(TextDic["机组转速"], false);
                
                Othercallback.Invoke();
            });
        });
    }

    void CnayaCepin()
    {
        SetUIPanel("操作界面");
        Settip(TextDic["残压测频"], true);
        Settip(TextDic["机组转速"], true);
        DOTween.To(() => 48f, x =>
        {
            TextDic["机组频率"].text = x.ToString("F2");
            TextDic["残压测频"].text = x.ToString("F2");
            TextDic["机组转速"].text = (x * 2).ToString("F2");
        }, 52f, 5).OnComplete(() =>
        {
            DOVirtual.DelayedCall(3, () =>
            {
                Settip(TextDic["残压测频"], false);
                Settip(TextDic["机组转速"], false);
                Othercallback?.Invoke();
                
            });
        });
    }

    public Transform jsq;
    public TextMeshPro jsqNum;


    /// <summary>
    /// 转换为格式为"分.秒.毫秒"（例如"00.00.00"）的Sprite标签字符串
    /// </summary>
    /// <param name="totalSeconds"></param>
    /// <returns></returns>
    string ConvertTimeToSpriteString(float totalSeconds)
    {
        // 处理负数值（按0处理）
        if (totalSeconds < 0) totalSeconds = 0;

        // 计算时间分量
        int minutes = (int)(totalSeconds / 60);
        float remainingSeconds = totalSeconds % 60;
        int seconds = (int)remainingSeconds;
        int milliseconds = (int)((remainingSeconds - seconds) * 100);

        // 限制分钟数在0-99范围（超过99分钟显示99）
        minutes = Math.Min(minutes, 99);

        // 格式化时间字符串（00.00.00格式）
        string formatted = $"{minutes:D2}.{seconds:D2}.{milliseconds:D2}";

        StringBuilder result = new StringBuilder();
        foreach (char c in formatted)
        {
            // 为每个字符创建sprite标签（包括数字和点号）
            result.Append($"<sprite name=\"{c}\">");
        }

        return result.ToString();
    }

    /// <summary>
    /// 变更聚焦目标
    /// </summary>
    Vector3 oldPos;
    Vector3 oldRot;
    void ChangeFouces(bool newPos)
    {
        if (newPos)
        {
            Transform ctrlGO = transform.Find("point");
            var camera = Camera.main.transform;
            {
                oldPos = camera.position;
                oldRot = camera.eulerAngles;
                camera.DOMove(ctrlGO.position, GlobalInfo.playTimeRatio);
                camera.DORotate(ctrlGO.eulerAngles, GlobalInfo.playTimeRatio);
            }
        }
        else
        {
            var camera = Camera.main.transform;
            {
                camera.DOMove(oldPos, GlobalInfo.playTimeRatio);
                camera.DORotate(oldRot, GlobalInfo.playTimeRatio);
            }
        }
    }


    void Settip(Text t, bool b)
    {
        t.transform.Find("tip").gameObject.SetActive(b);
    }


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


    //开度给旋钮
    public Transform kdgd;
    //水轮机主轴
    public Transform xz;
    //接力器推拉杆
    public Transform tg;
    //观察点位
    public Transform jkwz;
    /// <summary>
    /// 控制水轮机旋转 推拉杆移动到 对应开度位置
    /// </summary>
    /// <param name="rota"></param>
    void RotationControl(int opening, float during)
    {
        //if(opening == 100)
        //{
        //    xz.DOLocalRotate(new Vector3(-90, -15.5f, -111), during);
        //    tg.DOMove(new Vector3(3.9544f, -6.65f, -2.3903f), during);
        //}
        //else if (opening == 0)
        //{
        //    xz.DOLocalRotate(new Vector3(-90, 5.5f, -111), during);
        //    tg.DOMove(new Vector3(3.192f, -6.65f, -3.03f), during);
        //}
        //改为支持全开度 插值
        float normalizedT = opening / 100f;
        Vector3 targetRotation = new Vector3(-90, Mathf.Lerp(5.5f, -15.5f, normalizedT), -111);
        Vector3 targetPosition = new Vector3(
            Mathf.Lerp(3.192f, 3.9544f, normalizedT),
            -6.65f,
            Mathf.Lerp(-3.03f, -2.3903f, normalizedT)
        );
        xz.DOLocalRotate(targetRotation, during);
        if (monitorCamera != null)
            tg.DOMove(targetPosition, during).OnUpdate(() => {
                // 在每一帧，更新相机位置
                monitorCamera.transform.position = jkwz.position;
                monitorCamera.transform.eulerAngles = jkwz.eulerAngles;
            });
    }

    /// <summary>
    /// 处理自由 操作界面上的UI事件
    /// </summary>
    /// <param name="btn"></param>
    float kd;
    /// <summary>
    /// 按钮事件入口 - 本地点击时调用
    /// </summary>
    public void ButenEvent(string eventname)
    {
        if (TryToNext(eventname))
        {
            // 发送广播消息给其他用户（包含操作对象ID）
            ToolManager.SendBroadcastMsg(new MsgSyncCustomUI((ushort)SmallFlowModuleEvent.SynchronizationTsq, (int)status, currentStepIndex), true);
        }
        ExecuteButtonEvent(eventname);
    }

    /// <summary>
    /// 执行按钮事件的实际逻辑
    /// </summary>
    private void ExecuteButtonEvent(string eventname)
    {
        switch (eventname)
        {
            case "通信状态":
                smallSceneModule.ShowHint("“通信状态”方框为绿色，其余方框为白色，无红色故障指示", -1);
                break;
            case "频率t":
                smallSceneModule.ShowHint("机组频率和系统频率保持一致", -1);
                break;
            case "桨叶开度t":
                smallSceneModule.ShowHint("桨叶开度值与目标值相近", -1);
                break;
            case "导叶开度t":
                smallSceneModule.ShowHint("导叶开度值与目标值相近", -1);
                break;
            case "水头t":
                smallSceneModule.ShowHint("水头与实际相符", -1);
                UIButtons[0].gameObject.SetActive(true);
                break;
            case "B机t":
                smallSceneModule.ShowHint("A、B套除“导叶给定”、“导叶反馈”、“桨叶给定”、“桨叶反馈”略有差异外，其余参数保持一致", -1);
                UIButtons[0].gameObject.SetActive(false);
                break;
            case "开度给定增加":
                kd = float.Parse(TextDic["导叶开度"].text);
                DOTween.To(() => kd, x =>
                {
                    TextDic["导叶目标"].text = x.ToString("F2");
                    TextDic["导叶目标值"].text = x.ToString("F2");
                }, kd + 5, 1).OnComplete(() =>
                {
                    kd += 5;
                });
                break;
            case "开度给定减少":
                kd = float.Parse(TextDic["导叶开度"].text);
                DOTween.To(() => kd, x =>
                {
                    TextDic["导叶目标"].text = x.ToString("F2");
                    TextDic["导叶目标值"].text = x.ToString("F2");
                }, kd - 5, 1).OnComplete(() =>
                {
                    kd -= 5;
                });
                break;
            case "开度给定确认":
                float dkd = float.Parse(TextDic["导叶开度"].text);
                RotationControl((int)dkd, 0);
                RotationControl((int)kd, 1);
                DOTween.To(() => dkd, x =>
                {
                    TextDic["导叶开度"].text = x.ToString("F2");
                }, kd, 1);
                break;
            case "参数设定":
                TextDic["主标题"].text = "参数索引";
                SetUIPanel("参数设定");
                break;
            case "试验":
                TextDic["主标题"].text = "试验索引";
                SetUIPanel("试验目录");
                break;
            case "静特性试验":
                TextDic["主标题"].text = "静特性试验";
                SetUIPanel("静态试验");
                break;
            case "跟踪频给":
                if (status == AvailableStatus.静特性试验)
                    smallSceneModule.ShowHint("已切换到跟踪频给状态", 1);
                else
                {
                    Settip(TextDic["跟踪模式"], true);
                    DOVirtual.DelayedCall(3, () =>
                    {
                        Settip(TextDic["跟踪模式"], false);
                    });
                }
                break;
            case "跟踪网频":
                Settip(TextDic["跟踪模式"], true);
                DOVirtual.DelayedCall(3, () =>
                {
                    Settip(TextDic["跟踪模式"], false);
                });
                break;
            case "设置PID":
                // 设置PID参数：bp=6%, Kp=9.99, Ki=9.99, Kd=0, 频率给定=50Hz
                // 电气开限开至全开，导叶接力器全关
                TextDic["Bp"].text = "6.00";
                TextDic["Kp"].text = "9.99";
                TextDic["Ki"].text = "9.99";
                TextDic["Kd"].text = "0.00";
                TextDic["频率给定"].text = "50.00";
                TextDic["电气开限"].text = "99.99";
                // 导叶接力器全关
                DOTween.To(() => float.Parse(TextDic["导叶开度"].text), x =>
                {
                    TextDic["导叶目标"].text = x.ToString("F2");
                    TextDic["导叶目标值"].text = x.ToString("F2");
                    TextDic["导叶开度"].text = x.ToString("F2");
                }, 0f, 2f);
                smallSceneModule.ShowHint("PID参数已设置：bp=6%, Kp=9.99%, Ki=9.99%, Kd=0s，频率给定=50Hz，电气开限=99.99%，导叶全关", 2);
                break;
            case "开始静态试验":
                // 原来的"开始"逻辑，这里不需要额外处理，因为callback会在流程完成时执行
                break;
            case "导叶及主配反馈整定":
                TextDic["主标题"].text = "导叶＆主配反馈整定";
                SetUIPanel("导叶及主配反馈整定");
                break;
            case "测量零点":
                DOTween.To(() => 15435, x =>
                {
                    TextDic["测量零点"].text = x.ToString("F2");
                }, 15470, 1).OnComplete(() =>
                {
                    TextDic["导叶开度"].text = "0.00";
                    TextDic["桨叶反馈工程值"].text = "0.00";
                });
                break;
            case "测量增益":
                DOTween.To(() => 28828, x =>
                {
                    TextDic["测量增益"].text = x.ToString("F2");
                }, 29119, 1).OnComplete(() =>
                {
                    TextDic["导叶开度"].text = "100.00";
                    TextDic["桨叶反馈工程值"].text = "100.00";
                });
                break;
            case "事件报告":
                TextDic["主标题"].text = "事件报警记录";
                SetUIPanel("事件报警");
                if (status == AvailableStatus.打开报警界面)
                    smallSceneModule.ShowHint("发现报警：“调速器紧急停机动作”，“导叶紧急关闭”\r\n", 1);
                break;
            case "主操作画面":
                TextDic["主标题"].text = "操作画面";
                SetUIPanel("操作界面");
                break;
            case "主显示画面":
                TextDic["主标题"].text = "操作画面";
                SetUIPanel("主显示");
                break;
            case "切到A":
                TextDic["主用"].text = "A机";
                UIButtons[1].gameObject.SetActive(false);
                UIButtons[2].gameObject.SetActive(true);
                break;
            case "切到B":
                TextDic["主用"].text = "B机";
                UIButtons[2].gameObject.SetActive(false);
                UIButtons[1].gameObject.SetActive(true);
                break;
            case "功率及水头反馈整定":
                TextDic["主标题"].text = "功率及水头反馈整定";
                SetUIPanel("设置水头");
                break;
            case "空载频率摆动试验":
                TextDic["主标题"].text = "空载频率摆动试验";
                SetUIPanel("摆动试验");
                break;
            case "一次调频监视":
                TextDic["主标题"].text = "一次调频监视";
                SetUIPanel("扰动试验");
                break;
            case "PID参数整定":
                TextDic["主标题"].text = "PID参数整定";
                SetUIPanel("PID参数设置");
                break;
            case "b电气开限":
                DOTween.To(() => 0, x =>
                {
                    TextDic["b电气开限"].text = x.ToString("F2");
                }, 99.99, 1);
                break;
            case "空载调节PID":
                DOTween.To(() => 0f, x =>
                {
                    TextDic["kzKp"].text = Mathf.Lerp(0.45f, 2.75f, x).ToString("F2");
                    TextDic["kzKd"].text = Mathf.Lerp(0f, 0.5f, x).ToString("F2");
                }, 1f, 1);
                break;
            case "r频率增":
                DOTween.To(() => 0f, x =>
                {
                    TextDic["r扰动频率"].text = Mathf.Lerp(50.00f, 52.00f, x).ToString("F2");
                }, 1f, 1);
                break;
            case "r频率减":
                DOTween.To(() => 0f, x =>
                {
                    TextDic["r扰动频率"].text = Mathf.Lerp(50.00f, 48.00f, x).ToString("F2");
                }, 1f, 1);
                break;
            case "频率调节PID":
                DOTween.To(() => 0f, x =>
                {
                    TextDic["PlKp"].text = Mathf.Lerp(0.45f, 2.75f, x).ToString("F2");
                    TextDic["PlKd"].text = Mathf.Lerp(0f, 0.5f, x).ToString("F2");
                }, 1f, 1);
                break;
            case "空载频率扰动试验":
                TextDic["主标题"].text = "空载频率扰动试验";
                SetUIPanel("扰动试验");
                break;
            case "甩负荷试验":
                TextDic["主标题"].text = "甩负荷试验";
                SetUIPanel("甩负荷试验");
                break;
        }


    }

    public void SetUIPanel(string name)
    {
        foreach (var item in UIPanel)
        {
            item.SetActive(false);
        }
        if (PanelDic.ContainsKey(name))
        {
            PanelDic[name].SetActive(true);
        }
    }


    public void SetFinalState()
    {
        SetImageRaycast(true);
        ClousAllTip();
    }

    void SetImageRaycast(bool show)
    {
        if (smallSceneModule && smallSceneModule.RoteInput)
            smallSceneModule.RoteInput.enabled = show;
    }

    // 步骤列表
    List<string> steps = new List<string>();
    // 当前步骤
    int currentStepIndex = 0;

    public void StartFlow()
    {
        SmallFlowCtrl.Wait140 = true;
        SetImageRaycast(false);
        currentStepIndex = 0;
        SetTip();
    }

    void SetTip()
    {
        foreach (var item in UIButtons)
        {
            if (item.transform.Find("tip") != null)
            {
                if (steps.Count > currentStepIndex && item.gameObject.name == steps[currentStepIndex])
                {
                    item.transform.Find("tip").gameObject.SetActive(true);
                }
                else
                {
                    item.transform.Find("tip").gameObject.SetActive(false);
                }
            }
        }
    }

    public Transform TestResoult;
    /// <summary>
    /// 检测当前步骤
    /// </summary>
    /// <param name="stepname"></param>
    public bool TryToNext(string stepname)
    {
        if (steps.Count > currentStepIndex && stepname == steps[currentStepIndex])
        {
            currentStepIndex++;
            SetTip();
            if (currentStepIndex >= steps.Count)
            {
                SmallFlowCtrl.Wait140 = false;
                if (callback != null)
                {
                    callback.Invoke();
                    callback = null;
                }
                else
                {
                    DOVirtual.DelayedCall(2, () =>
                    {
                        Othercallback.Invoke();
                    });
                }
            }
            return true;
        }
        else
            return false;

    }

    // 协联曲线参数（针对12m水头优化）
    private readonly double _shapeCoefficient = 0.9f;    // 曲线形状系数 c
    private readonly double _headCoefficient = 1f;     // 水头影响系数 k（固定水头12m）
    /// <summary>
    /// 根据导叶开度计算桨叶开度 默认桨叶自动 桨叶非自动状况暂不处理
    /// </summary>
    /// <param name="guideOpeningPercent">导叶开度百分比 0-100</param>
    /// <returns>桨叶开度百分比 0-100</returns>
    public void CalculateBladeOpening(double guideOpeningPercent)
    {
        // 输入验证
        if (guideOpeningPercent < 0) guideOpeningPercent = 0;
        if (guideOpeningPercent > 100) guideOpeningPercent = 100;

        // 转换为比例（0-1）
        double alpha = guideOpeningPercent / 100.0;

        // 计算原始桨叶开度比例
        // β = k * [c * α³ + (1-c) * α]
        double betaRaw = _headCoefficient *
                       (_shapeCoefficient * Math.Pow(alpha, 3) +
                        (1 - _shapeCoefficient) * alpha);

        // 箝位到 [0, 1] 范围
        double betaRatio = Math.Max(0.0, Math.Min(1.0, betaRaw));

        // 转换为百分比
        TextDic["桨叶目标值"].text = TextDic["桨叶开度"].text = (betaRatio * 100).ToString("F2");
    }


    // 外部调用------------------------------------          仅用于设置最终状态               ------------------------------------------

    /// <summary>
    ///生成图纸打开图纸显示
    /// </summary>
    /// <param name="i"></param>
    public void SetReports(int i)
    {
        _SmallFlowCtrl.AddSchematic(Reports[i]);
    }

    /// <summary>
    /// 正常运行时屏幕参数
    /// </summary>
    /// <param name="i"></param>
    public void SetScreenParameters(int i)
    {
        TextDic["导叶开度"].enabled = true;
        //发电
        if (i == 2)
        {
            TextDic["电气开限"].text = "91.67";
            TextDic["导叶开度"].text = "79.07";
            TextDic["导叶目标"].text = "79.09";
            TextDic["导叶目标值"].text = "79.09";
            TextDic["机组频率"].text = "50.00";
            TextDic["桨叶开度"].text = "76.91";
            TextDic["桨叶目标值"].text = "76.38";

            TextDic["电网频率"].text = "49.93";
            TextDic["残压测频"].text = "49.93";
            TextDic["齿盘测频"].text = "50.27";
            TextDic["机组转速"].text = "99.8";

            TextDic["机组有功"].text = "43.30";
            TextDic["功率目标"].text = "43.30";

            TextDic["工作状态"].text = "负载运行";
        }
        //停机
        else if (i == 0)
        {
            TextDic["电气开限"].text = "0.00";
            TextDic["导叶开度"].text = "0.22";
            TextDic["导叶目标"].text = "0.00";
            TextDic["导叶目标值"].text = "0.00";
            TextDic["机组频率"].text = "0.00";
            TextDic["桨叶开度"].text = "0.00";
            TextDic["桨叶目标值"].text = "0.00";

            TextDic["电网频率"].text = "49.93";
            TextDic["残压测频"].text = "49.93";
            TextDic["齿盘测频"].text = "0.00";
            TextDic["机组转速"].text = "0.00";
            TextDic["功率目标"].text = "0.00";
            TextDic["机组有功"].text = "0.00";

            TextDic["工作状态"].text = "停机备用";
        }
        //空载
        else if (i == 1)
        {
            TextDic["电气开限"].text = "20.00";
            TextDic["导叶开度"].text = "13.91";
            TextDic["导叶目标"].text = "14.00";
            TextDic["导叶目标值"].text = "14.00";
            TextDic["机组频率"].text = "50.00";
            TextDic["桨叶开度"].text = "1.49";
            TextDic["桨叶目标值"].text = "1.5";

            TextDic["电网频率"].text = "49.93";
            TextDic["残压测频"].text = "49.93";
            TextDic["齿盘测频"].text = "0.00";
            TextDic["机组转速"].text = "99.8";
            TextDic["功率目标"].text = "0.00";
            TextDic["机组有功"].text = "0.00";

            TextDic["工作状态"].text = "空载运行";
        }
    }

    public void SetScreenJzpl(float num)
    {
        TextDic["机组频率"].text = num.ToString("F2");
    }


    /// <summary>
    /// 提供给外部直接立即修改水轮机开度
    /// </summary>
    /// <param name="opening"></param>
    public void SetRotationControlNow(int opening)
    {
        RotationControl(opening, 0f);
        TextDic["导叶目标值"].text = TextDic["导叶目标"].text = TextDic["导叶开度"].text = opening.ToString("F2");
        CalculateBladeOpening(opening);
    }

    /// <summary>
    /// 清除报警信息
    /// </summary>
    public void RemoveEvent()
    {
        foreach (Transform item in bjTextParent)
        {
            Destroy(item.gameObject);
        }
        foreach (Transform item in zlTextParent)
        {
            Destroy(item.gameObject);
        }
        tempConmand.Clear();
        tempErro.Clear();
    }


    /// <summary>
    /// 报警信息
    /// </summary>
    public Transform zlTextParent;
    public Transform bjTextParent;
    public GameObject tempText;
    Dictionary<string, GameObject> tempConmand = new Dictionary<string, GameObject>();
    Dictionary<string, GameObject> tempErro = new Dictionary<string, GameObject>();
    public void AddConmandEvent(string t)
    {
        if(tempConmand.ContainsKey(t))
        {
            return;
        }

        Text temp = Instantiate(tempText, zlTextParent).GetComponent<Text>();
        temp.text = t;
        tempConmand.Add(t, temp.gameObject);
    }
    public void AddErroEvent(string t)
    {
        if (tempErro.ContainsKey(t))
        {
            return;
        }
      
        Text temp = Instantiate(tempText, zlTextParent).GetComponent<Text>();
        temp.text = t;
        tempErro.Add(t, temp.gameObject);
    }

    /// <summary>
    /// 清除提示
    /// </summary>
    public void ClousAllTip()
    {
        currentStepIndex = 0;
        foreach (var item in UIButtons)
        {
            item.transform.Find("tip").gameObject.SetActive(false);
        }
    }

    //导叶开度消失
    public void Daoyexs()
    {
        TextDic["导叶开度"].enabled = false;
        TextDic["导叶目标"].text = "91.67";
        TextDic["导叶目标值"].text = "91.67";
    }
}
