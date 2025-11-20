# Battle 和 Core 文件夹结构分析

## 📊 当前结构分析

### Battle 文件夹结构

#### 根目录文件（7个）
1. **BattleController.cs** (1134行)
   - **职责**: 主控制器，协调所有战斗系统
   - **功能**: 
     - 初始化所有组件（Managers, Services, UI）
     - 管理游戏流程（开始、重置、继续）
     - 处理场景转换和状态管理
     - 管理金钱、遗物、骰子系统
   - **问题**: 职责过多，代码过长

2. **HandFlowController.cs** (677行)
   - **职责**: 管理手牌流程（开始→选择→滚动→提交→完成）
   - **功能**:
     - 手牌生命周期管理
     - 骰子选择和自动滚动
     - 提交组合和分数计算
     - 目标分数评估
   - **问题**: 与 BattleController 职责有重叠

3. **BattleUI.cs** (154行)
   - **职责**: 统一管理战斗场景的 UI 更新
   - **功能**: 更新各种 UI 文本（等级、目标分数、组合预览等）

4. **CooldownSystem.cs** (455行)
   - **职责**: 管理8骰子池的冷却系统
   - **功能**: 
     - 骰子池管理（8个骰子）
     - 冷却机制（1回合冷却）
     - 手牌计数器（3手限制）
     - 可用骰子列表更新

5. **SettingsPanel.cs** (162行)
   - **职责**: 设置面板的显示和按钮逻辑
   - **功能**: 打开/关闭设置面板，处理重置和退出

6. **ComboPreferencePanel.cs** (114行)
   - **职责**: 组合偏好面板的显示和按钮逻辑
   - **功能**: 打开/关闭组合偏好面板

7. **SceneTransitionManager.cs** (84行)
   - **职责**: 管理战斗场景的场景转换逻辑
   - **功能**: 转换到奖励场景和游戏结束场景

#### 子文件夹

**Scoring/** (2个文件)
- `ScoreCalculator.cs` - 集中式分数计算系统
- `DiceMultiplierCalculator.cs` - 骰子倍数计算器

**Services/** (7个文件)
- `HandManager.cs` - 手牌生命周期管理（滚动次数、锁定状态）
- `ProgressionManager.cs` - 等级进度和目标分数管理
- `MoneyManager.cs` - 金钱管理
- `DiceEffectHandler.cs` - 骰子效果处理器
- `HandCompositionService.cs` - 手牌组合服务
- `BattleUIPresenter.cs` - UI 展示器（格式化文本）
- 其他服务类

**Factories/** (1个文件)
- `DiceViewFactory.cs` - 骰子视图工厂（创建和销毁视图）

**Components/** (3个文件)
- `DiceView.cs` - 骰子视图组件（UI显示）
- `ScoreAnimator.cs` - 分数动画器（动画显示分数）
- `DiceTooltipManager.cs` - 骰子提示管理器

### Core 文件夹结构

1. **GameState.cs** (36行)
   - **职责**: 游戏运行时状态（场景间传递，不持久化）
   - **功能**: 存储场景转换状态、教程模式、游戏结束状态

2. **GameStateManager.cs** (85行)
   - **职责**: 游戏状态管理器（单例，DontDestroyOnLoad）
   - **功能**: 
     - 管理运行时状态（GameState）
     - 管理持久化数据（SaveData）
     - 加载和保存数据

3. **SaveData.cs** (22行)
   - **职责**: 需要持久化的玩家数据
   - **功能**: 存储金钱、遗物名称、骰子类型ID、教程完成状态

---

## 🔍 问题分析

### 1. 文件分类不清晰
- **根目录文件过多**: 7个文件混在一起，职责不明确
- **UI 文件分散**: `BattleUI`, `SettingsPanel`, `ComboPreferencePanel` 都在根目录
- **管理器类分散**: `CooldownSystem`, `SceneTransitionManager` 在根目录，但其他管理器在 Services 文件夹

### 2. 职责划分不明确
- **BattleController 职责过重**: 
  - 初始化所有组件
  - 管理游戏流程
  - 处理场景转换
  - 管理金钱、遗物、骰子
  - 更新 UI
- **HandFlowController 与 BattleController 重叠**: 
  - 两者都管理游戏流程
  - 职责边界不清晰

### 3. 命名不一致
- 有些是 `Manager` (CooldownSystem, HandManager)
- 有些是 `Controller` (BattleController, HandFlowController)
- 有些是 `System` (CooldownSystem)
- 有些是 `Panel` (SettingsPanel, ComboPreferencePanel)

### 4. 文件夹结构混乱
- `Services/` 文件夹包含 Manager 类（HandManager, ProgressionManager）
- `Components/` 文件夹包含 Manager 类（DiceTooltipManager）
- `Scoring/` 文件夹命名合理，但 Calculator 类也可以放在 Services

---

## 💡 文件分类建议

### 建议的新结构

```
Battle/
├── Controllers/          # 控制器层（协调各个系统）
│   ├── BattleController.cs          # 主控制器（简化后）
│   └── HandFlowController.cs       # 手牌流程控制器
│
├── Managers/             # 管理器层（管理特定系统）
│   ├── CooldownSystem.cs           # 冷却系统管理器
│   ├── HandManager.cs              # 手牌管理器（从 Services 移入）
│   ├── ProgressionManager.cs       # 进度管理器（从 Services 移入）
│   └── MoneyManager.cs              # 金钱管理器（从 Services 移入）
│
├── Services/            # 服务层（提供业务逻辑服务）
│   ├── DiceEffectHandler.cs        # 骰子效果处理器
│   ├── HandCompositionService.cs   # 手牌组合服务
│   ├── BattleUIPresenter.cs       # UI 展示器
│   └── SceneTransitionManager.cs   # 场景转换服务（从根目录移入）
│
├── Scoring/             # 计分系统
│   ├── ScoreCalculator.cs          # 分数计算器
│   └── DiceMultiplierCalculator.cs # 骰子倍数计算器
│
├── UI/                  # UI 层（所有 UI 相关组件）
│   ├── BattleUI.cs                 # 战斗 UI 管理器
│   ├── SettingsPanel.cs            # 设置面板
│   ├── ComboPreferencePanel.cs     # 组合偏好面板
│   ├── DiceView.cs                 # 骰子视图组件（从 Components 移入）
│   ├── ScoreAnimator.cs            # 分数动画器（从 Components 移入）
│   └── DiceTooltipManager.cs       # 骰子提示管理器（从 Components 移入）
│
└── Factories/           # 工厂层（创建对象）
    └── DiceViewFactory.cs           # 骰子视图工厂
```

### Core 文件夹结构（保持不变，已经很清晰）

```
Core/
├── GameState.cs         # 运行时状态
├── GameStateManager.cs  # 状态管理器
└── SaveData.cs          # 持久化数据
```

---

## 📋 重构建议

### 阶段 1: 文件移动（低风险）
1. 创建新文件夹结构
2. 移动文件到对应文件夹
3. 更新命名空间（如果需要）
4. 更新 Unity meta 文件引用

### 阶段 2: 职责分离（中风险）
1. **简化 BattleController**:
   - 只负责初始化和协调
   - 将具体业务逻辑委托给对应的 Manager/Service
   
2. **明确 HandFlowController 职责**:
   - 专注于手牌流程（开始→选择→滚动→提交）
   - 不处理游戏状态管理（交给 BattleController）

3. **统一命名规范**:
   - Manager: 管理特定系统（CooldownSystem → CooldownManager）
   - Controller: 协调多个系统（BattleController, HandFlowController）
   - Service: 提供业务逻辑服务（HandCompositionService）
   - Panel: UI 面板组件（SettingsPanel）

### 阶段 3: 代码优化（高风险，可选）
1. 拆分 BattleController 的初始化逻辑
2. 提取公共接口
3. 使用依赖注入模式

---

## 🎯 分类原则

### 按职责分类
- **Controllers/**: 协调多个系统，控制流程
- **Managers/**: 管理特定系统（状态、资源）
- **Services/**: 提供业务逻辑服务（计算、处理）
- **UI/**: 所有 UI 相关组件
- **Factories/**: 创建对象的工厂类
- **Scoring/**: 计分相关（可以合并到 Services，但保持独立更清晰）

### 命名规范
- **Manager**: 管理特定系统（CooldownManager, HandManager）
- **Controller**: 协调多个系统（BattleController, HandFlowController）
- **Service**: 提供业务逻辑（HandCompositionService, DiceEffectHandler）
- **Panel**: UI 面板（SettingsPanel, ComboPreferencePanel）
- **View**: UI 视图组件（DiceView）
- **Animator**: 动画组件（ScoreAnimator）
- **Factory**: 工厂类（DiceViewFactory）
- **Calculator**: 计算器（ScoreCalculator, DiceMultiplierCalculator）

---

## ✅ 优势

1. **清晰的职责划分**: 每个文件夹有明确的职责
2. **易于维护**: 相关文件集中在一起
3. **易于扩展**: 新功能可以轻松找到合适的位置
4. **统一的命名规范**: 文件名和文件夹名一致
5. **降低耦合**: UI、业务逻辑、数据管理分离

---

## ⚠️ 注意事项

1. **Unity 引用**: 移动文件后需要更新 Unity Inspector 中的引用
2. **命名空间**: 如果使用命名空间，可能需要更新
3. **Meta 文件**: Unity 会自动更新 .meta 文件，但需要确保引用正确
4. **测试**: 重构后需要全面测试，确保功能正常

