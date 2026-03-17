# GenSubtitle UI 重新设计规格文档

**日期:** 2025-03-17
**状态:** 设计阶段（审查中 - v1.1 已修复问题）
**版本:** 1.1

## 概述

重新设计 GenSubtitle 的用户界面，采用渐进式界面方案，根据任务状态自动切换界面布局，并添加15个增强功能以提升用户体验。

## 设计目标

1. **简化信息层级** - 隐藏技术细节，突出核心操作
2. **适应不同场景** - 自动切换界面以匹配当前任务
3. **提升用户体验** - 清晰的操作引导和直观的进度反馈

## 核心设计：渐进式界面

### 界面状态流转

```
空闲状态（引导页） → 处理中状态（进度页） → 已完成状态（编辑页）
      ↑                                                      ↓
      └──────────────── 所有任务清空 ←────────────────────────┘
```

### 状态切换逻辑

#### 状态优先级规则

当多个任务处于不同状态时，按以下优先级决定界面状态：

1. **Processing 状态优先** - 如果有任何任务处于 Transcribing 或 Translating 状态，显示处理中界面
2. **Editing 状态条件** - 只有当用户**显式选中**已完成任务时，才切换到编辑界面
3. **Idle 状态条件** - 任务列表为空时，自动返回空闲状态

#### 详细状态转换表

| 当前状态 | 触发条件 | 目标状态 | 允许 | 说明 |
|---------|---------|---------|------|------|
| Idle | 导入第一个视频文件 | Processing | ✓ | 自动切换到进度界面 |
| Idle | 用户手动操作 | Processing | ✗ | 禁止直接跳转，必须先导入文件 |
| Idle | 任意操作 | Editing | ✗ | 禁止跳转，必须先经过 Processing |
| Processing | 所有任务清空 | Idle | ✓ | 返回引导页面 |
| Processing | 有任务正在处理中 | Idle | ✗ | 禁止跳转，必须等待任务完成 |
| Processing | 用户选中已完成任务 | Editing | ✓ | 切换到编辑模式 |
| Processing | 用户点击"返回引导页" | Idle | ✗ | 禁止跳转，必须先清空任务 |
| Editing | 所有任务清空 | Idle | ✓ | 返回引导页面 |
| Editing | 用户点击"返回进度页"或选中处理中任务 | Processing | ✓ | 返回进度概览 |
| Editing | 有新任务开始处理 | Processing | ✓ | 自动切换回处理中界面 |

#### 状态转换矩阵

```
         Idle  Processing  Editing
Idle      -        ✓         ✗
Processing ✓       -         ✓
Editing   ✓        ✓         -
```

（✓ = 允许转换，✗ = 禁止转换）

#### 边界情况处理

**情况1：多任务混合状态**
- 场景：3个任务处理中，2个任务已完成
- 行为：默认显示 Processing 界面
- 用户可手动选择已完成任务切换到 Editing 界面

**情况2：所有任务完成但未清空**
- 场景：5个任务全部完成，用户未操作
- 行为：保持在 Processing 界面，显示"全部完成"状态
- 用户可手动选择任务进入 Editing 界面

**情况3：编辑时有新任务开始**
- 场景：用户在 Editing 界面编辑，导入新文件
- 行为：自动切换回 Processing 界面
- 保留用户当前编辑内容（自动保存）

## 三种状态界面设计

### 1. 空闲状态（引导页）

#### 布局结构

```
┌─────────────────────────────────────────┐
│  标题栏（精简：Logo + Import + Settings） │
├─────────────────────────────────────────┤
│                                         │
│            🎬                           │
│     欢迎使用 GenSubtitle                │
│      智能双语字幕生成工具                │
│                                         │
│        [📁 导入视频文件]                │
│                                         │
│    支持拖拽文件到窗口                    │
│    或点击上方按钮选择文件                │
│                                         │
│  ────────────  ────────────             │
│  最近处理任务    使用教程               │
│                                         │
└─────────────────────────────────────────┘
```

#### 功能要点

- 大号导入按钮，突出主要操作
- 支持拖拽文件导入
- 显示最近处理的任务（如果有）
- 提供使用教程链接
- 简洁友好的引导体验

### 2. 处理中状态（进度页）

#### 布局结构

```
┌─────────────────────────────────────────┐
│  标题栏 + [返回引导页] 按钮               │
├─────────────────────────────────────────┤
│  📊 处理概览                             │
│  运行中: 2/5  队列: 3  完成: 12  失败: 0 │
├──────────────────┬──────────────────────┤
│  任务列表        │  批量操作            │
│  ┌────────────┐  │  [全部导出]          │
│  │ video1.mp4 │  │  [暂停全部]          │
│  │ ████████░░ │  │  [清空完成]          │
│  │ 转录 65%   │  │                      │
│  └────────────┘  │                      │
│  ┌────────────┐  │                      │
│  │ video2.mp4 │  │                      │
│  │ ██████░░░░ │  │                      │
│  │ 翻译 30%   │  │                      │
│  └────────────┘  │                      │
│                  │                      │
└──────────────────┴──────────────────────┘
```

#### 功能要点

- **统计卡片**：运行中/队列中/已完成/失败数量
- **任务列表**：显示所有任务及实时进度
- **批量操作面板**：全部导出、暂停全部、清空已完成
- 每个任务显示进度条和当前阶段
- **控制台日志**：可折叠面板，默认折叠，仅在出错时自动展开

**控制台日志放置规范：**

- **Idle 状态**：完全隐藏（无日志输出需求）
- **Processing 状态**：底部可折叠面板
  - 默认折叠
  - 出错时自动展开并高亮错误信息
  - 可通过菜单"View → Console Log"手动展开
- **Editing 状态**：完全隐藏
  - 通过菜单"View → Console Log"打开独立日志窗口
  - 日志窗口可关闭或最小化到系统托盘

### 3. 已完成状态（编辑页）

#### 布局结构

```
┌─────────────────────────────────────────┐
│  标题栏 + [返回进度页] 按钮               │
├──────────┬──────────────────────────────┤
│ 任务列表 │  🎬 视频预览                  │
│ ┌──────┐│  ┌─────────────────────────┐ │
│ │video1││  │                         │ │
│ │ ✓完成 ││  │      播放器区域         │ │
│ └──────┘│  │                         │ │
│ ┌──────┐│  └─────────────────────────┘ │
│ │video2││  ▌▌▌▌▌▌▌▌▌▌▌▌▌▌▌▌ 00:00/05:23│
│ │ 处理中││  [播放][暂停][对齐开始][对齐结束]│
│ └──────┘│├──────────────────────────────┤
│          │  📝 字幕编辑                  │
│          │  ┌─────────────────────────┐ │
│          │  │ #  开始  结束  原文 中文 │ │
│          │  │ 1 00:00 00:05 Hello 你好│ │
│          │  │ 2 00:05 00:10 World 世界│ │
│          │  └─────────────────────────┘ │
│          ├──────────────────────────────┤
│          │  [导出] [批量调整] [样式编辑] │
└──────────┴──────────────────────────────┘
```

#### 功能要点

- **左侧任务栏**：显示所有任务，当前选中高亮
- **视频预览**：完整播放器，支持时间轴对齐
- **字幕编辑表格**：可编辑时间、文本，支持快捷键
- **底部操作栏**：导出、批量调整、样式编辑

## 15个增强功能

### 高优先级功能（核心体验）

#### 1. 批量操作
- **全选任务**：Ctrl+A 全选所有任务
- **批量导出**：选中多个任务一键导出
- **批量删除**：选中多个任务批量删除
- **批量时间调整**：统一调整字幕时间偏移

**任务选择交互模型：**

- **复选框列**：任务列表第一列添加复选框
- **Ctrl+Click**：切换单个任务的选中状态
- **Shift+Click**：选择连续范围内的任务
- **全选/取消全选按钮**：批量操作面板顶部
- **选中计数显示**：显示"已选 X 个任务"

#### 2. 搜索过滤
- **文件名搜索**：实时搜索任务文件名
- **状态筛选**：按已完成/失败/处理中筛选
- **日期筛选**：按今天/本周/本月筛选
- **组合筛选**：支持多条件组合

**搜索性能要求：**

- **防抖搜索**：用户输入停止 300ms 后才执行搜索
- **ICollectionView**：使用 WPF 内置过滤机制
- **虚拟化**：任务数 > 50 时启用列表虚拟化
- **性能指标**：搜索响应时间 < 100ms（100个任务）

#### 3. 任务统计
- **统计面板**：总任务数、成功/失败率
- **处理时间**：平均处理时间、预计剩余时间
- **资源使用**：CPU/内存使用情况（可选）

#### 4. 快捷键支持
- `Space` - 播放/暂停
- `←/→` - 微调时间轴（±0.1秒）
- `Ctrl+O` - 导入文件
- `Ctrl+S` - 保存编辑
- `Ctrl+Z` - 撤销
- `Ctrl+Y` 或 `Ctrl+Shift+Z` - 重做
- `Del` - 删除选中任务
- `Ctrl+A` - 全选任务
- `Esc` - 取消选择

**撤销/重做系统：**

- **实现阶段**：第一阶段 B（编辑功能）
- **命令模式**：所有编辑操作实现为 ICommand 对象
- **撤销栈**：保存最近 50 个操作
- **持久化**：撤销/重做历史随自动保存一起保存
- **UI 反馈**：菜单项显示"撤销 [操作名称]"

#### 5. 自动保存
- **防抖保存**：用户停止编辑 2 秒后自动保存（而非固定 30 秒）
- **冲突解决**：采用最后写入策略（Last-Write-Wins），基于时间戳比较
- **用户通知**：自动保存覆盖未保存更改时显示通知
- **恢复选项**：提供"从自动保存恢复"功能
- **保存位置**：`{TaskOutputDir}/.autosave.json`
- **保存内容**：字幕段落的编辑状态、选中项、滚动位置

**冲突解决详细策略：**

1. **自动保存触发条件**
   - 用户修改字幕内容后 2 秒无操作
   - 用户切换任务时
   - 用户切换视图时（离开 Editing 状态）

2. **冲突检测**
   - 比较自动保存时间戳和用户上次手动保存时间戳
   - 如果自动保存更新，显示提示："已自动保存您的编辑"

3. **用户覆盖场景**
   - 如果用户手动保存的版本较旧
   - 自动保存会覆盖手动保存
   - 显示通知："自动保存已覆盖手动保存 [撤销]"

4. **自动保存恢复**
   - 应用启动时检查自动保存文件
   - 如果自动保存比缓存文件新，提示用户恢复
   - 用户可选择：恢复自动保存 / 使用缓存 / 忽略

### 中优先级功能（增强功能）

#### 6. 批量时间调整
- **全局偏移**：所有字幕统一前移/后移
- **按比例缩放**：加速/减速字幕
- **合并重复**：自动合并时间重叠的字幕
- **拆分长句**：将长字幕拆分为多段

#### 7. 重新翻译
- **选中翻译**：选中部分字幕重新翻译
- **切换模型**：使用不同翻译模型重翻
- **翻译记忆**：记住用户修改的翻译

#### 8. 字幕样式编辑
- **可视化编辑器**：字体、颜色、大小、位置
- **预设样式**：提供常用样式模板
- **实时预览**：编辑时实时显示效果

#### 9. 更多导出格式
- **VTT 格式**：Web VTT 格式
- **SSA/ASS 格式**：高级字幕格式
- **TXT 纯文本**：不含时间轴的纯文本
- **双语对照**：原文和译文对照表

#### 10. 模板管理
- **翻译提示词模板**：保存常用的翻译指令
- **字幕样式预设**：保存常用样式配置
- **导出配置模板**：保存导出参数
- **快速应用**：一键应用模板

### 低优先级功能（锦上添花）

#### 11. 完成通知
- **系统通知**：所有任务完成后弹出系统通知
- **声音提示**：播放提示音
- **闪烁窗口**：窗口标题栏闪烁提醒

#### 12. 文件关联
- **右键菜单**：在视频文件上右键选择"用GenSubtitle处理"
- **拖放支持**：拖放文件到应用图标
- **自动关联**：安装时询问是否关联视频格式

#### 13. 多语言识别
- **语言切换检测**：自动检测视频中语言切换
- **分段识别**：不同语言段使用对应模型
- **混合语言字幕**：在同一字幕中标记不同语言

#### 14. 任务分组
- **按日期分组**：今天、昨天、更早
- **按文件夹分组**：相同源文件夹的任务
- **自定义标签**：用户可添加标签分组
- **拖放分组**：拖放任务到不同分组

#### 15. 使用向导
- **首次启动引导**：介绍主要功能
- **交互式教程**：引导用户完成第一个任务
- **功能说明**：各功能的详细说明
- **可跳过**：用户可选择跳过

## 技术实现

### 架构变更

#### ViewState 持久化策略

**策略：不持久化界面状态，始终从 Idle 状态启动**

**理由：**
- 避免用户重启应用时进入混乱状态
- 确保应用启动时界面清晰可预测
- 减少状态恢复相关的 bug

**实现细节：**

1. **应用启动**
   - 始终从 Idle 状态开始
   - 加载缓存的已完成任务（如果有的话）
   - 显示"最近处理任务"列表

2. **状态保存**
   - 保存最后选中的任务 ID（用于恢复编辑位置）
   - 保存各面板的展开/折叠状态
   - 保存窗口大小和位置

3. **状态恢复**
   - 如果有缓存的已完成任务，显示在 Idle 页面的"最近处理任务"区域
   - 用户点击最近任务时，直接跳转到 Editing 界面

**持久化数据结构：**

```csharp
public class ViewStatePersistence
{
    public Guid? LastSelectedTaskId { get; set; }
    public bool ProcessingPanelExpanded { get; set; } = true;
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 820;
}
```

#### 任务历史管理策略

**策略：保留最近 20 个已完成任务，自动删除 30 天前的任务**

**实现细节：**

1. **任务保留规则**
   - 默认保留最近 20 个已完成任务
   - 30 天后自动删除（即使未达到 20 个）
   - 用户可标记任务为"永久保留"

2. **存储位置**
   - 任务缓存：`C:\Users\Admin\Documents\GenSubtitle\.taskcache`
   - 任务元数据：`C:\Users\Admin\Documents\GenSubtitle\.metadata.json`

3. **元数据结构**

```csharp
public class TaskMetadata
{
    public Guid TaskId { get; set; }
    public string FileName { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public bool IsPinned { get; set; }  // 用户标记为永久保留
}
```

4. **清理逻辑**
   - 应用启动时检查并清理过期任务
   - 删除任务缓存文件夹（如果元数据也过期）
   - 保留被标记为永久保留的任务

5. **"最近处理任务"显示**
   - Idle 页面显示最多 5 个最近任务
   - 按完成时间倒序排列
   - 显示文件名、完成时间、处理时长

#### 新增视图状态枚举

```csharp
public enum ViewState
{
    Idle,           // 空闲状态
    Processing,     // 处理中状态
    Editing         // 编辑状态
}
```

#### 新增 ViewStateManager

**完整实现：**

```csharp
public class ViewStateManager : ObservableObject
{
    private readonly ILogger _logger;
    private readonly TaskQueueViewModel _taskQueue;
    private ViewState _currentState = ViewState.Idle;

    public ViewState CurrentState
    {
        get => _currentState;
        private set => SetProperty(ref _currentState, value);
    }

    public ViewStateManager(TaskQueueViewModel taskQueue, ILogger logger)
    {
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 尝试转换到新状态
    /// </summary>
    /// <param name="newState">目标状态</param>
    /// <param name="context">转换上下文（可选）</param>
    /// <returns>转换是否成功</returns>
    public bool TransitionTo(ViewState newState, object context = null)
    {
        if (!CanTransitionTo(newState))
        {
            _logger.LogWarning($"Invalid state transition: {CurrentState} → {newState}");
            return false;
        }

        try
        {
            var oldState = CurrentState;
            OnExitingState(oldState, newState);
            CurrentState = newState;
            OnEnteredState(newState, oldState);
            _logger.LogInformation($"State transition: {oldState} → {newState}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"State transition failed: {CurrentState} → {newState}");
            // 转换失败时，回退到 Idle 状态以确保安全
            if (CurrentState != ViewState.Idle)
            {
                CurrentState = ViewState.Idle;
            }
            return false;
        }
    }

    /// <summary>
    /// 检查是否可以转换到目标状态
    /// </summary>
    public bool CanTransitionTo(ViewState newState)
    {
        // 状态转换矩阵
        return (CurrentState, newState) switch
        {
            (ViewState.Idle, ViewState.Processing) => true,
            (ViewState.Idle, ViewState.Editing) => false,

            (ViewState.Processing, ViewState.Idle) => _taskQueue.Tasks.Count == 0,
            (ViewState.Processing, ViewState.Editing) => true,

            (ViewState.Editing, ViewState.Idle) => _taskQueue.Tasks.Count == 0,
            (ViewState.Editing, ViewState.Processing) => true,

            _ => false
        };
    }

    private void OnExitingState(ViewState oldState, ViewState newState)
    {
        // 离开状态时的清理工作
        switch (oldState)
        {
            case ViewState.Editing:
                // 保存编辑状态
                _logger.LogInformation("Exiting Editing state, saving edit position");
                break;
        }
    }

    private void OnEnteredState(ViewState newState, ViewState oldState)
    {
        // 进入状态时的初始化工作
        switch (newState)
        {
            case ViewState.Idle:
                _logger.LogInformation("Entered Idle state");
                break;
            case ViewState.Processing:
                _logger.LogInformation($"Entered Processing state with {_taskQueue.Tasks.Count} tasks");
                break;
            case ViewState.Editing:
                _logger.LogInformation("Entered Editing state");
                break;
        }
    }
}
```

**ViewModel 所有权模型：**

```
MainViewModel (所有者)
  ├── ViewStateManager
  ├── TaskQueueViewModel (共享，通过构造函数传递)
  ├── IdleViewModel
  ├── ProcessingViewModel (持有 TaskQueueViewModel 引用)
  └── EditingViewModel (持有 TaskQueueViewModel 引用)
```

**通信接口：**

```csharp
public interface ITaskQueueService
{
    ObservableCollection<TaskItemViewModel> Tasks { get; }
    TaskItemViewModel? SelectedTask { get; set; }
    void EnqueueFiles(string[] files);
    Task ExportTaskAsync(TaskItemViewModel task, ExportOptions options, ...);
    // ... 其他必要的方法
}
```

**实现说明：**
- MainViewModel 拥有 TaskQueueViewModel
- 子 ViewModels 通过 ITaskQueueService 接口访问任务队列
- 降低耦合，便于测试和维护

### UI 组件变更

#### MainWindow.xaml 结构

```xml
<Grid>
    <!-- 标题栏（保持不变） -->
    <TitleBar />

    <!-- 动态内容区 -->
    <ContentControl Content="{Binding CurrentView}" />

    <!-- 状态栏（精简） -->
    <StatusBar />
</Grid>
```

#### 新增视图

1. **IdleView.xaml** - 空闲状态引导页
2. **ProcessingView.xaml** - 处理中状态进度页
3. **EditingView.xaml** - 已完成状态编辑页

### ViewModel 变更

#### MainViewModel 变更

```csharp
public class MainViewModel : ObservableObject
{
    private ViewStateManager _viewStateManager;
    private ObservableObject _currentView;

    public ObservableObject CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public void HandleFileImport(string[] files);
    public void HandleTaskSelection(TaskItemViewModel task);
    public void HandleQueueNavigation();
}
```

#### 新增 ViewModels

1. **IdleViewModel** - 引导页逻辑
2. **ProcessingViewModel** - 进度页逻辑
3. **EditingViewModel** - 编辑页逻辑

#### 媒体播放器生命周期管理

**跨状态播放器行为：**

1. **离开 Editing 状态时**
   - 自动暂停播放
   - 保存当前播放位置（时间戳）
   - 释放媒体资源（可选）

2. **进入 Editing 状态时**
   - 恢复上次播放位置
   - 加载选中任务的媒体文件

3. **切换到不同任务时**
   - 保存当前任务的播放位置
   - 加载新任务的播放位置
   - 重置播放状态到暂停

4. **在 Idle 状态时**
   - 完全释放 MediaElement 资源
   - 减少内存占用

**播放位置存储：**

```csharp
public class PlaybackPosition
{
    public Guid TaskId { get; set; }
    public TimeSpan Position { get; set; }
    public DateTime LastUpdated { get; set; }
}

// 存储在任务元数据中
public class TaskMetadata
{
    // ... 其他属性
    public TimeSpan? LastPlaybackPosition { get; set; }
}
```

## 实现阶段

### 第一阶段：核心界面重构（3-4周）

**目标：** 实现渐进式界面框架

#### 第一阶段 A：基础框架（1-2周）

**任务清单：**

- [ ] 创建 ViewStateManager 和状态枚举
- [ ] 实现状态转换逻辑和验证
- [ ] 创建 ITaskQueueService 接口
- [ ] 实现 IdleView 和 IdleViewModel
- [ ] 更新 MainWindow 以支持动态视图切换
- [ ] 单元测试：ViewStateManager 状态转换

**验收标准：**

- 应用启动显示 Idle 界面
- 导入文件后自动切换到 Processing 界面
- 状态转换逻辑正确（通过单元测试）

#### 第一阶段 B：完整视图实现（1-2周）

**任务清单：**

- [ ] 实现 ProcessingView 和 ProcessingViewModel（不含批量操作）
- [ ] 实现 EditingView 和 EditingViewModel
- [ ] 实现任务列表显示
- [ ] 实现视频播放器集成
- [ ] 实现字幕编辑表格
- [ ] 实现状态自动切换逻辑
- [ ] 集成测试：完整的用户流程

**验收标准：**

- 处理中状态显示任务列表和进度
- 完成状态自动展开编辑界面
- 视频预览和字幕编辑功能正常
- 状态切换流畅，无明显卡顿

### 第二阶段：高优先级功能（1-2周）

**目标：** 实现核心增强功能

**任务清单：**

- [ ] **批量操作**
  - [ ] 实现全选功能（Ctrl+A）
  - [ ] 实现批量导出
  - [ ] 实现批量删除
  - [ ] 实现批量时间偏移

- [ ] **搜索过滤**
  - [ ] 实现文件名实时搜索
  - [ ] 实现状态筛选
  - [ ] 实现日期筛选
  - [ ] UI：搜索框和筛选下拉框

- [ ] **任务统计**
  - [ ] 统计服务实现
  - [ ] 统计面板 UI
  - [ ] 处理时间计算
  - [ ] 成功率统计

- [ ] **快捷键支持**
  - [ ] 快捷键注册系统
  - [ ] 播放/暂停快捷键
  - [ ] 时间调整快捷键
  - [ ] 文件操作快捷键
  - [ ] 快捷键帮助对话框

- [ ] **自动保存**
  - [ ] 自动保存服务
  - [ ] 编辑状态序列化
  - [ ] 应用恢复逻辑
  - [ ] 保存提示 UI

**验收标准：**

- 所有批量操作功能正常
- 搜索和筛选快速响应
- 统计数据准确
- 快捷键冲突处理正确
- 自动保存不影响性能

### 第三阶段：中优先级功能（1-2周）

**目标：** 实现增强编辑功能

**任务清单：**

- [ ] **批量时间调整**
  - [ ] 全局偏移对话框
  - [ ] 按比例缩放对话框
  - [ ] 合并重复字幕算法
  - [ ] 拆分长字幕功能

- [ ] **重新翻译**
  - [ ] 选中重新翻译功能
  - [ ] 切换翻译模型
  - [ ] 翻译缓存管理

- [ ] **字幕样式编辑**
  - [ ] 样式编辑器窗口
  - [ ] 样式预设管理
  - [ ] 实时预览功能

- [ ] **更多导出格式**
  - [ ] VTT 格式支持
  - [ ] SSA/ASS 格式支持
  - [ ] TXT 纯文本导出
  - [ ] 双语对照导出

- [ ] **模板管理**
  - [ ] 模板存储服务
  - [ ] 模板管理对话框
  - [ ] 快速应用模板功能

**验收标准：**

- 时间调整功能准确无误
- 重新翻译不影响已有翻译
- 样式编辑器实时预览正确
- 所有导出格式可用
- 模板保存和加载正常

### 第四阶段：低优先级功能（1周）

**目标：** 实现锦上添花的功能

**任务清单：**

- [ ] **完成通知**
  - [ ] 系统通知集成
  - [ ] 声音提示
  - [ ] 窗口闪烁

- [ ] **文件关联**
  - [ ] 注册表关联
  - [ ] 右键菜单集成
  - [ ] 卸载清理

- [ ] **多语言识别**
  - [ ] 语言切换检测算法
  - [ ] 分段识别逻辑
  - [ ] 混合语言字幕标记

- [ ] **任务分组**
  - [ ] 分组逻辑实现
  - [ ] 分组 UI
  - [ ] 拖放分组

- [ ] **使用向导**
  - [ ] 向导窗口
  - [ ] 交互式教程
  - [ ] 功能说明页面
  - [ ] 跳过选项

**验收标准：**

- 通知功能不影响性能
- 文件关联正确注册和卸载
- 多语言识别准确率可接受
- 分组功能易用
- 向导对新用户有帮助

## 用户体验设计

### 视觉设计原则

1. **清晰的信息层级** - 重要操作突出显示
2. **一致的视觉语言** - 统一的颜色、间距、字体
3. **及时的反馈** - 操作后立即给出反馈
4. **容错性** - 支持撤销，避免误操作

### 交互设计原则

1. **渐进式披露** - 高级功能默认隐藏，需要时才显示
2. **智能默认值** - 根据用户行为推荐设置
3. **键盘友好** - 所有操作都可通过键盘完成
4. **性能优先** - 大量任务时保持响应流畅

### 可访问性

1. **键盘导航** - Tab 键顺序合理
2. **屏幕阅读器** - 重要元素有标签
3. **对比度** - 文字和背景对比度足够
4. **字体大小** - 支持系统字体缩放

**键盘导航Tab顺序：**

**Idle 视图：**
1. 导入按钮
2. 最近处理任务列表（第一项）
3. 使用教程链接
4. 设置按钮（标题栏）

**Processing 视图：**
1. 搜索框（如果有）
2. 状态筛选下拉框
3. 任务列表（第一项）
4. 任务列表内导航（↑↓方向键）
5. 批量操作按钮（全部导出、暂停全部、清空完成）
6. 返回引导页按钮

**Editing 视图：**
1. 任务列表（第一项）
2. 任务列表内导航（↑↓方向键）
3. 视频播放器（播放/暂停按钮）
4. 时间轴滑块
5. 对齐开始按钮
6. 对齐结束按钮
7. 字幕表格（第一行）
8. 字幕表格内导航（↑↓方向键）
9. 导出按钮
10. 返回进度页按钮

## 向后兼容性

### 保留现有功能

- 所有现有的转录、翻译、导出功能保持不变
- 现有设置项保持兼容
- 已有任务缓存继续有效

### 数据迁移

- 自动迁移旧版本的任务缓存
- 保留用户的设置和配置
- 首次启动时检测并迁移数据

**UI设置迁移策略：**

1. **当前UI设置（需迁移）**
   - 窗口大小和位置
   - 各面板宽度（QueueColumn, PreviewColumn）
   - 面板可见性状态

2. **新UI设置**
   - ViewState 偏好（默认Idle）
   - 各面板展开/折叠状态
   - ProcessingView: 任务列宽度、批量操作面板宽度
   - EditingView: 预览区域大小、编辑表格列宽

3. **迁移映射**
   - 旧 QueueColumn.Width → 新 ProcessingView.TaskListWidth
   - 旧 PreviewColumn.Width → 新 EditingView.PreviewWidth
   - 窗口大小和位置直接迁移

4. **默认值策略**
   - 首次启动使用保守值（展开所有面板）
   - 旧用户的列宽度设置迁移到对应新视图
   - 无法映射的设置使用默认值

### API 变更

- 公共接口保持稳定
- 新增接口标记为实验性
- 废弃接口保留至少一个版本

## 测试策略

### 单元测试

- ViewStateManager 状态转换逻辑
- 各 ViewModel 的业务逻辑
- 批量操作功能
- 搜索过滤算法
- 时间调整算法

### 集成测试

- 完整的用户流程：导入 → 处理 → 编辑 → 导出
- 状态切换流程
- 批量操作流程
- 多任务并发处理

### UI 测试

- 各状态界面显示正确
- 交互响应及时
- 快捷键功能正常
- 窗口大小调整适应

### 性能测试

- 100+任务的性能表现
- 搜索响应时间 < 100ms
- 状态切换时间 < 200ms
- 内存占用无明显增长

### 兼容性测试

- Windows 10/11 各版本
- 不同屏幕分辨率
- 高 DPI 缩放
- 暗色/亮色主题

## 风险和缓解措施

| 风险 | 影响 | 概率 | 缓解措施 |
|-----|------|------|---------|
| 状态切换逻辑复杂导致 bug | 高 | 中 | 充分的单元测试，分阶段实现 |
| 大量任务时性能问题 | 中 | 中 | 虚拟化列表，分页加载 |
| 用户不适应新界面 | 中 | 低 | 提供使用向导，保留传统模式选项 |
| 开发周期超期 | 低 | 中 | 按优先级分阶段，低优先级可延后 |
| 数据迁移失败 | 高 | 低 | 备份旧数据，迁移失败时回滚 |

## 未解决的问题

以下问题需要进一步讨论：

1. **传统模式保留** - 是否需要保留一个"传统界面"选项供老用户切换？
2. **任务历史管理** - 是否需要永久保存任务历史，还是只保留最近的任务？
3. **云同步** - 是否考虑将来支持云端同步设置和模板？

## 附录

### 相关文件

- 当前 MainWindow.xaml: `src/GenSubtitle.App/Views/MainWindow.xaml`
- MainViewModel.cs: `src/GenSubtitle.App/ViewModels/MainViewModel.cs`
- TaskQueueViewModel.cs: `src/GenSubtitle.App/ViewModels/TaskQueueViewModel.cs`

### 参考资料

- Material Design in XAML Toolkit 文档
- WPF MVVM 模式最佳实践
- Windows 11 UI 设计规范

### 变更历史

| 日期 | 版本 | 变更说明 |
|-----|------|---------|
| 2025-03-17 | 1.0 | 初始设计文档 |
