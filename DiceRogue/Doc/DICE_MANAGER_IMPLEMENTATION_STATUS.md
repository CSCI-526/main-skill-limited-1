# 🎲 DiceManager 实现状态

## ✅ 已完成：阶段1, 阶段2, 阶段3, 阶段4, 阶段5 & 阶段6

### 阶段1：创建 DiceManager 类 ✅

**文件：** `DiceRogue/Assets/Scripts/Dices/DiceManager.cs`

**实现内容：**
- ✅ 全局骰子池 (`_globalDicePool`)
- ✅ 玩家骰子背包 (`_playerDiceBackpack`)
- ✅ 公共属性 (`GlobalDicePool`, `PlayerDiceBackpack`)
- ✅ `InitializeGlobalDicePool()` - 从 `DicePool.GetAll()` 初始化
- ✅ `AddDiceToBackpack()` - 添加骰子到背包
- ✅ `AddDiceToBackpackByName()` - 通过类型名添加
- ✅ `RemoveDiceFromBackpack()` - 移除骰子
- ✅ `RemoveDiceFromBackpackByName()` - 通过类型名移除
- ✅ `ClearBackpack()` - 清空背包
- ✅ `LoadFromSaveData()` - 从保存数据加载
- ✅ `SaveToSaveData()` - 保存到保存数据
- ✅ `CreateDiceFromTypeName()` - 根据类型名创建骰子实例

---

### 阶段2：修改 BattleController ✅

**文件：** `DiceRogue/Assets/Scripts/Battle/BattleController.cs`

**修改内容：**
1. ✅ 添加 `_diceManager` 字段
2. ✅ 在 `InitializeCoreComponents()` 中初始化 DiceManager
3. ✅ 添加 `InitializeDiceSystem()` 方法
4. ✅ 添加 `UpdateCooldownSystemFromBackpack()` 方法
5. ✅ 重构 `IntegrateRewardDice()` 使用 DiceManager
6. ✅ 修改重置逻辑（`ResetForNewHand()` 和 `OnSettingsResetClicked()`）
7. ✅ 标记 `CreateDiceFromTypeId()` 为废弃（保留向后兼容）

---

### 阶段3：修改 CooldownSystem ✅

**文件：** `DiceRogue/Assets/Scripts/Battle/CooldownSystem.cs`

**修改内容：**
1. ✅ `Awake()` 不再自己生成骰子
2. ✅ 改为调用 `InitializeEmptyPool()` 创建空池
3. ✅ 等待外部调用 `SetPlayerBackpackDice()` 来填充
4. ✅ `GetPlayerBackpackDice()` 标记为 `[Obsolete]`
5. ✅ 改进 `SetPlayerBackpackDice()` 方法

---

### 阶段4：修改游戏重置逻辑 ✅

**文件：** `DiceRogue/Assets/Scripts/Battle/BattleController.cs`

**修改内容：**
1. ✅ `ResetForNewHand()` - 添加骰子背包重置逻辑
2. ✅ `OnSettingsResetClicked()` - 添加骰子背包重置逻辑

---

### 阶段5：修改 BackpackManager ✅

**文件：** `DiceRogue/Assets/Scripts/UI/BackpackManager.cs`

**修改内容：**

#### 1. 添加 DiceManager 支持 ✅
- ✅ 添加 `_diceManager` 字段
- ✅ 添加重载的 `Initialize()` 方法接受 DiceManager
- ✅ 保留原有 `Initialize()` 方法以保持向后兼容

#### 2. 智能显示逻辑 ✅
- ✅ **Selection 模式**：显示当前可用的骰子（从 `CooldownSystem.GetAvailableDice()`）
- ✅ **ViewOnly 模式**：显示玩家背包中的所有骰子（从 `DiceManager.PlayerDiceBackpack`）
- ✅ 添加后备逻辑：如果 DiceManager 不可用，回退到 CooldownSystem

**关键改进：**
```csharp
// Selection 模式：显示可用骰子（用于手牌选择）
if (isSelectionMode) {
    diceToDisplay = _cooldownSystem.GetAvailableDice();
}

// ViewOnly 模式：显示玩家背包（用于查看）
else {
    diceToDisplay = _diceManager.PlayerDiceBackpack.ToList();
}
```

#### 3. BattleController 集成 ✅
- ✅ `BattleController.InitializeManagers()` 现在传递 DiceManager 给 BackpackManager

---

### 阶段6：修改奖励系统 ✅

**文件：** `DiceRogue/Assets/Scripts/Reward/RewardSceneManager.cs`

**修改内容：**

#### 1. 添加 DiceManager 支持 ✅
- ✅ 添加 `_diceManager` 字段
- ✅ 在 `Start()` 中初始化 DiceManager

#### 2. 使用 DiceManager.GlobalDicePool ✅
- ✅ 优先使用 `DiceManager.GlobalDicePool` 获取骰子类型
- ✅ 如果 DiceManager 不可用，回退到 `DicePool.GetAll()`
- ✅ 添加详细的日志记录

**关键改进：**
```csharp
// 优先使用 DiceManager
if (_diceManager != null && _diceManager.GlobalDicePool.Count > 0) {
    allDicePool = _diceManager.GlobalDicePool.ToList();
}
// 后备方案
else {
    allDicePool = DicePool.GetAll();
}
```

---

## 📊 完整数据流

### 游戏启动流程

```
游戏启动
    ↓
BattleController.Start()
    ↓
InitializeCoreComponents()
    ├── DiceManager.InitializeGlobalDicePool()
    │   └── 从 DicePool.GetAll() 初始化全局池
    │
InitializeManagers()
    ├── BackpackManager.Initialize(cooldownSystem, diceManager, ...)
    │   └── 传递 DiceManager 引用
    └── InitializeDiceSystem()
        ├── DiceManager.LoadFromSaveData()
        │   └── 从 SaveData.diceTypeIds 加载玩家背包
        └── UpdateCooldownSystemFromBackpack()
            └── CooldownSystem.SetPlayerBackpackDice()
                └── 构建8个骰子池（补全 NormalDice）
```

### 背包显示流程

```
玩家打开背包（ViewOnly 模式）
    ↓
BackpackManager.ShowBackpack(ViewOnly)
    ↓
RefreshDiceList()
    ├── 检测模式：ViewOnly
    └── 显示 DiceManager.PlayerDiceBackpack
        └── 显示玩家拥有的所有骰子

开始新手（Selection 模式）
    ↓
BackpackManager.ShowBackpack(Selection)
    ↓
RefreshDiceList()
    ├── 检测模式：Selection
    └── 显示 CooldownSystem.GetAvailableDice()
        └── 显示当前可用的骰子（考虑冷却）
```

### 奖励系统流程

```
奖励场景启动
    ↓
RewardSceneManager.Start()
    ├── 创建 DiceManager
    ├── DiceManager.InitializeGlobalDicePool()
    └── 使用 DiceManager.GlobalDicePool
        └── 从全局池选择奖励骰子
```

---

## 🔍 关键改进总结

### 1. 统一的骰子管理
- ✅ 所有骰子操作通过 `DiceManager`
- ✅ 自动处理保存/加载
- ✅ 与 `RelicManager` 结构一致

### 2. 清晰的职责分离
- ✅ `DiceManager` - 管理全局池和玩家背包
- ✅ `CooldownSystem` - 管理当前游戏的8个骰子池
- ✅ `BackpackManager` - UI显示，根据模式选择数据源
- ✅ `BattleController` - 协调各个系统

### 3. 智能的背包显示
- ✅ **Selection 模式**：显示可用骰子（用于游戏）
- ✅ **ViewOnly 模式**：显示玩家背包（用于查看）
- ✅ 自动模式检测和后备逻辑

### 4. 统一的奖励系统
- ✅ 使用 `DiceManager.GlobalDicePool` 获取骰子类型
- ✅ 保持向后兼容（回退到 `DicePool.GetAll()`）

### 5. 向后兼容
- ✅ `BackpackManager` 保留原有 `Initialize()` 方法
- ✅ `RewardSceneManager` 有后备逻辑
- ✅ 所有废弃方法保留但标记为 `[Obsolete]`

---

## ⚠️ 注意事项

### BackpackManager 模式检测

**当前实现：** 通过检查 `submitButton` 的激活状态来检测模式

**优点：**
- 简单直接
- 不需要额外的状态变量

**潜在问题：**
- 如果按钮状态被外部修改，可能导致模式检测错误

**建议：** 如果需要更可靠，可以添加显式的模式参数或状态变量

### RewardSceneManager DiceManager 生命周期

**当前实现：** 每次 `Start()` 都创建新的 DiceManager

**优点：**
- 简单，不需要跨场景管理
- 每次都是最新的全局池

**注意：** DiceManager 实例不会在场景间共享，这是预期的行为

---

## 🧪 测试建议

### BackpackManager 测试

1. **Selection 模式**
   - [ ] 显示当前可用的骰子（不在冷却中）
   - [ ] 不显示冷却中的骰子
   - [ ] 可以正常选择骰子

2. **ViewOnly 模式**
   - [ ] 显示玩家背包中的所有骰子
   - [ ] 包括已获得的奖励骰子
   - [ ] 如果背包为空，显示空列表或提示

3. **后备逻辑**
   - [ ] 如果 DiceManager 不可用，回退到 CooldownSystem
   - [ ] 不影响基本功能

### RewardSceneManager 测试

1. **正常流程**
   - [ ] 使用 DiceManager.GlobalDicePool 获取骰子
   - [ ] 正确生成3个奖励选项
   - [ ] 骰子类型正确

2. **后备逻辑**
   - [ ] 如果 DiceManager 失败，回退到 DicePool.GetAll()
   - [ ] 不影响奖励生成

---

## 📝 实现完成

### 所有阶段完成 ✅

- ✅ **阶段1**：创建 DiceManager 类
- ✅ **阶段2**：修改 BattleController
- ✅ **阶段3**：修改 CooldownSystem
- ✅ **阶段4**：修改游戏重置逻辑
- ✅ **阶段5**：修改 BackpackManager
- ✅ **阶段6**：修改奖励系统

### 系统特点

1. **完整的骰子管理系统**
   - 全局池 + 玩家背包
   - 统一的保存/加载
   - 与 Relic 系统一致的结构

2. **智能的UI显示**
   - 根据模式显示不同的骰子
   - 自动后备逻辑
   - 向后兼容

3. **清晰的代码结构**
   - 职责分离明确
   - 易于维护和扩展
   - 完整的错误处理

---

## 📚 相关文件

### 创建的文件
- `DiceRogue/Assets/Scripts/Dices/DiceManager.cs`

### 修改的文件
- `DiceRogue/Assets/Scripts/Battle/BattleController.cs`
- `DiceRogue/Assets/Scripts/Battle/CooldownSystem.cs`
- `DiceRogue/Assets/Scripts/UI/BackpackManager.cs`
- `DiceRogue/Assets/Scripts/Reward/RewardSceneManager.cs`

### 参考文件
- `DiceRogue/Assets/Scripts/Relics/RelicManager.cs` - 参考实现
- `DiceRogue/Assets/Scripts/Dices/DicePool.cs` - 全局骰子类型定义
- `DiceRogue/Assets/Scripts/Core/SaveData.cs` - 保存数据结构

---

**实现时间：** 2024  
**状态：** ✅ 所有阶段完成  
**下一步：** 测试和优化
