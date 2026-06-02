# SmallFlowCtrl 说明文档

> 小场景（SmallScene）培训 / 考核流程的核心控制器。
> 路径：`Assets/Scripts/UI/Course/SmallScene/SmallFlowCtrl.cs`，继承 `MonoBase`。

## 1. 职责定位

`SmallFlowCtrl` 负责把"后台配置好的任务流程"翻译成场景里的实际表现，单一入口统管：

- 任务 / 步骤的推进与跳转（`SelectStep` / `Next` / `Over`）
- 每步开头的初始视角（`initState`）自动演示
- 玩家操作的合法性判定与执行（`TryExecuteOperation` / `TryExecuteFreeOperation`）
- 操作行为与联动的递归执行（`ExecuteOperation` / `RunAction` / `ExecuteFlowLinkOperation`）
- 模型状态的设置、恢复与跳步重建（`SetFinalState` / `SetFinalStateWithLinkages` / `ResetAllToInitState`）
- 高亮提示、操作记录、语音、考核数据恢复的协调

它本身不直接操作模型 Transform，而是通过每个操作对象（`ModelOperation`）上配置的"行为列表（`BehaveBase`）"来驱动。

## 2. 核心数据结构

配置数据是一棵树，定义在 `SmallFlow1.cs`：

```
flows : SmallFlow1[]                 // 任务集合（场景里的子物体，Init 时 GetComponentsInChildren 取得）
 └─ steps : List<SmallStep1>         // 步骤集合
     ├─ initState : List<SmallStepState>   // 初始视角：进入该步自动执行的一组 <操作对象, 操作名>
     ├─ conditions: List<SmallStepState>   // 道具状态限制
     ├─ ops      : List<SmallOp1>          // 并列操作集合（本步要完成的操作；可多个并列）
     │   ├─ operation : ModelOperation     // 可操作对象（MonoBehaviour，挂在物体 GameObject 上）
     │   ├─ optionName: string             // 操作选项名（如 "打开" "点击" "聚焦" …）
     │   └─ prop      : ModelInfo          // 需选择的道具（可空）
     └─ actions  : List<SmallStepSequenceState>  // 步骤级联动：本步全部并列 ops 完成后执行一次
```

> **联动模型说明**：联动分两层，互不混淆：
> - **操作对象自身联动** = `OperationBase.actions`（配置在 `ModelOperation.operations[*]` 上），每次执行该操作时随 `RunAction` 触发，**保持不变**。
> - **步骤级联动** = `SmallStep1.actions`，与 `ops` 并列，在本步**全部并列操作完成后执行一次**（`MarkOpCompleted` 返回 true 的分支，`Next` 之前）。
> - 旧的 `SmallOp1.actions`（每个并列操作各自的联动）已废弃删除，数据迁移至 `SmallStep1.actions`。


- `ModelOperation`（`Assets/Scripts/Model/Config/ModelOperation.cs`）挂在可操作对象 GameObject 上，含 `operations`(List\<OperationBase\>)、`currentState`、`initState`。
  → **可操作对象的世界坐标 = `op.operation.transform.position`**。
- `OperationBase` 持有 `behaveBases : List<BehaveBase>`（具体表现）与 `actions`（联动）。
- `BehaveBase` 子类与 `BehaveType` 枚举定义在 `Assets/Scripts/Model/Config/Other/ModelOperationData.cs`。

运行期索引字典（`Init` 中按 `ModelInfo.PropType` 分类建立）：

| 字段 | 内容 | 来源 PropType |
|---|---|---|
| `operationIDs` | 所有可操作 / 安全工具道具 | Operate / Free / SafetyTool |
| `toolIDs` | 背包 / 上位机 / 图纸道具 | BackPack* / MasterComputer / Schematics |
| `autoProps` | 自动触发道具（不随步骤切换变化） | Auto |
| `naviPoints` | 导航锚点 | Anchor |
| `globalPerspective` | 全局默认视角 | GlobalPerspective |

游标：`index_NowFlow`（当前任务）、`index_NowStep`（当前步骤，属性 setter 含副作用，见下）；便捷属性 `nowFlowSteps` / `nowFlowStep`。

## 3. 执行主线

### 3.1 初始化

`Init(flowsTex)`：取场景里的 `SmallFlow1`，用后台数据回填 ID / 标题，再扫描所有 `ModelInfo` 按 `PropType` 分类登记，并把每个道具设到 `initState`。

### 3.2 进入一个步骤（关键副作用）

`index_NowStep` 的 setter 是流程引擎的"心跳"：

```
set index_NowStep =>
    _index_NowStep = value
    ClearCompletedOps()                      // 清空本步已完成记录
    若有 initState：
        ExecuteInitStateSequentially(...)    // 依次执行初始视角（支持弹窗等待）
            └─ onComplete: AimCameraAtFirstOp() + 播放步骤名语音
    否则：
        AimCameraAtFirstOp() + 播放步骤名语音
```

`ExecuteInitStateSequentially` 递归执行 `initState`，遇到配置了 `BehavePopup` 的操作（且非考核）会弹窗、等用户确认后再 `SetFinalState` 并继续下一个。

### 3.3 执行一次操作

公开入口（两者都先过"导航网关"，见第 5 节）：

- `TryExecuteOperation`：标准操作（计入操作记录、考核判分、培训等待语音）
- `TryExecuteFreeOperation`：自由操作（不计入操作记录列表）

核心流程（`...Core` 方法体）：

```
设置 ignoreMove / CameraDotween 标志 → 发 StartExecute 消息
ExecuteOperation(operation, optionName, prop, callback)   // 执行该操作的 behaveBases
  └─ callback:
       RunAction(op.actions …)                            // 执行操作对象自身的联动（OperationBase.actions）
       SendOperatingRecordMsg(...)                        // 上报操作记录 / 分数（非 dummy）
       MarkOpCompleted(data) ?                            // 把本 op 计入完成，并判断本步是否全部完成
         本步全部完成 →
           BuildLinkageOperations(step) → ExecuteFlowLinkOperation(...)  // 步骤级联动 SmallStep1.actions（可含导航/弹窗），执行一次
           Next()                                          // 进入下一步
         未全部完成 → Over()                                // 停在本步等其余并列操作
```

> 注意：步骤级联动 `SmallStep1.actions` 只在**本步所有并列 ops 完成后执行一次**，不再随单个操作触发（旧 `SmallOp1.actions` 已删除）。单个操作自身的联动仍由 `RunAction(op.actions)` 在每次操作时执行。


- `ExecuteOperation`：按 `optionName` 找到 `OperationBase`，校验 `conditions`，用 `cache` 防重入，依次 `Execute` 其 `behaveBases`，完成后按规则更新 `operation.currentState`（观察 / 聚焦 / 输入 / 点击 / 工具箱 / 收回 等不改状态）。
- `Execute(behaveBases …)`：逐个行为执行；`useCallBack` / 最后一个 / 自定义脚本回调 的行为会等待完成再执行下一个。
- `RunAction` / `ExecuteFlowLinkOperation`：处理联动；后者额外处理弹窗（`BehavePopup`）与角色寻路（`BehavePlayerNavigation`，培训模式需等走到位）。
- `MarkOpCompleted` 把 op 计入 `completedOpIds`，`IsStepComplete` 判断本步 `ops` 是否全部完成。

### 3.4 步骤推进

- `Next(dummy)`：步内则 `index_NowStep+1`，跨任务则 `index_NowFlow+1` 且 step 归 0；广播 `CompleteStep`，再 `Over`。
- `Over(dummy)`：触发 `OnStepAdvanced`，复位 `CameraDotween`，广播 `CompleteExecute`。

## 4. 状态恢复与跳步

`SelectStep(flowIndex, stepIndex, ResetByFlow, answerOp)` 是任意跳转的总入口：

1. 清高亮、重置工具数量
2. `ResetAllToInitState()` 把所有道具复位到预制体默认初始态
3. 以 Flow0/Step0 的 `initState` 作为全局基准
4. 重放所有前置 Flow / 前置 Step 的操作（`SetFinalStateWithLinkages`，`ignoreCondition/ignoreMove=true`，含递归联动）
5. 训练模式重建操作记录；考核模式用联机数据 `SetExamModelStateData` 恢复
6. `ApplyPlayerPositionForStepJump(stepIndex)`：从当前步 `initState` 与上一步 `actions` 向前搜索名为 "Navigation" 的操作，执行其 `Pose / PlayerNavigation` 行为定位角色；找不到则用 `globalPerspective`
7. 设 `index_NowStep`（触发 3.2 的初始视角演示）

关键方法：
- `SetFinalState`：把单个操作"瞬间"设到最终态（不走动画时长），可选递归联动。
- `SetFinalStateWithLinkages`：跳步重建用，递归处理嵌套联动；`Auto` 道具跳过。
- `ExecutePositionBehaviors`：只执行 `Pose / PlayerNavigation`，绕过 `ignoreMove`，专供跳步定位角色。

## 5. dummy / ignoreMove 同步语义

- `dummy=true`：表示"非本人 / 远程同步"的操作，B 端不重放 A 端的相机和移动表现。
- `ignoreMove`：在 `SetFinalState` 中跳过 `Pose / PlayerNavigation` 行为。
- 跳过集合：`DummySkipBehaveTypes`（相机跟随 / 围绕观察 / 聚焦 / 观察 / 寻路 / 测温）与 `DummySkipBehaveTypes_link`（寻路 / 姿态），分别用于 `Execute` 和联动 `RunAction / ExecuteFlowLinkOperation`。

修改这两类逻辑时务必同时考虑 A 端（本人，全表现）与 B 端（dummy，跳过移动 / 相机）两条路径。

## 6. 自动辅助扩展点（本次新增）

集中在 `SmallFlowCtrl.cs` 的 `#region 角色对准与导航（自动辅助）`：

- `playerController`（懒加载缓存）：统一从 `ModelManager.Instance.modelRoot.GetComponentInChildren<PlayerController>()` 取角色。
- `AimCameraAtFirstOp()`：**任务1**。非考核模式下，每步开头把相机对准 `nowFlowStep.ops` 中第一个 `operation != null` 的可操作对象；在 3.2 的两个分支（有 / 无 initState）均于播放步骤名语音前调用。内部调用 `PlayerController.AimAtTarget`，并取消点击锁定视角（`hasTapTarget=false`）。
- `EnsureNearOperationThen(data, dummy, proceed)` + `NavigateNearTargetAsync(...)`：**任务2**。在 `TryExecuteOperation` / `TryExecuteFreeOperation` 真正执行（`...Core`）之前，若角色离可操作对象 > 1 米，先寻路到 1 米内再执行。两种模式都生效；`dummy` 远程同步操作跳过。使用 `PlayerController.StartNavigation(target, snapToTarget:false)`，避免到达后把角色"贴"到物体上。

配套的 `PlayerController`（`PlayerController.cs`）新增：
- `AimAtTarget(Vector3 targetPos, float duration=-1)`：一次性转向（机身 Y + `verticalPoint` X），相机靠 `CameraFollow` 跟随；复用 `RotateTowardsTarget` 的角度公式。
- `StartNavigation(Transform, bool snapToTarget)` 重载 + `navSnapToTarget` 字段；`LateUpdate` 到达后按 `navSnapToTarget` 决定 `EndNavigation(targetPoint)`（吸附姿态）或 `EndNavigation(null)`（仅停下、不吸附）。原 `StartNavigation(Transform)` 默认 `snapToTarget=true`，既有调用方行为不变。

## 7. 复用提示

- 取角色：`ModelManager.Instance.modelRoot.GetComponentInChildren<PlayerController>()`（全工程统一）。
- 取"本步第一个可操作对象"：`nowFlowStep.ops.FirstOrDefault(o => o.operation != null)`。
- 让相机看向某物：`PlayerController.AimAtTarget(worldPos)`。
- 走到某物附近不吸附：`PlayerController.StartNavigation(target, false)` + 等待 `NavEnd`。
- 让角色走到并贴合姿态点：沿用 `BehavePlayerNavigation`（`ModelOperationData.cs`）。
