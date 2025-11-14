# 🎲 骰子管理器实现计划 (Dice Manager Implementation Plan)

## 📋 目标

创建一个类似 `RelicManager` 的 `DiceManager` 系统，实现：
1. **全局骰子池** (Global Dice Pool) - 所有可获得的骰子类型
2. **玩家骰子背包** (Player Dice Backpack) - 玩家已获得的骰子

---

## 🔍 现状分析

### RelicManager 的结构（参考）

```csharp
public class RelicManager
{
    // 全局遗物池 - 所有可获得的遗物
    private readonly List<RelicBase> _globalRelicPool = new();
    
    // 玩家背包 - 玩家已获得的遗物
    private readonly List<RelicBase> _playerBackpack = new();
    
    // 公共属性
    public IReadOnlyList<RelicBase> GlobalRelicPool => _globalRelicPool;
    public IReadOnlyList<RelicBase> PlayerBackpack => _playerBackpack;
    
    // 核心方法
    public void InitializeGlobalRelicPool() { ... }
    public bool AddRelicToBackpack(RelicBase relic) { ... }
    public bool AddRelicToBackpackByName(string relicName) { ... }
    public bool RemoveRelic(RelicBase relic) { ... }
    public void ClearBackpack() { ... }
}
```

### 当前骰子系统的结构

#### 1. DicePool.cs（全局骰子类型定义）
```csharp
public static class DicePool
{
    public static List<BaseDice> GetAll()
    {
        return new List<BaseDice>
        {
            new BigOne(), new BigSix(), new CounterDice(), ...
        };
    }
}
```
**作用：** 定义所有可用的骰子类型（类似全局池）

#### 2. CooldownSystem.cs（当前骰子池管理）
```csharp
public class CooldownSystem : MonoBehaviour
{
    private readonly List<BaseDice> _dicePool = new(); // 8个骰子的池
    
    public void SetPlayerBackpackDice(List<BaseDice> backpackDice) { ... }
    public List<BaseDice> GetAllDice() { ... }
}
```
**问题：** 
- 只管理当前游戏中的8个骰子
- 不是真正的背包系统
- 没有持久化玩家拥有的所有骰子

#### 3. SaveData.cs（保存数据）
```csharp
public class SaveData
{
    public List<string> diceTypeIds = new List<string>(); // 骰子类型ID列表
}
```
**问题：**
- 有保存字段，但没有对应的管理器
- `BattleController.IntegrateRewardDice()` 中手动保存

#### 4. BattleController.cs（奖励骰子集成）
```csharp
private void IntegrateRewardDice()
{
    // 从 PendingDiceTypeIds 创建骰子
    // 添加到 CooldownSystem
    // 保存到 SaveData.diceTypeIds
}
```
**问题：**
- 逻辑分散在 BattleController
- 没有统一的骰子管理

---

## 🎯 设计方案

### 新系统架构

```
DiceManager (类似 RelicManager)
    ├── GlobalDicePool (所有可获得的骰子类型)
    │   └── 从 DicePool.GetAll() 初始化
    │
    └── PlayerDiceBackpack (玩家已获得的骰子)
        ├── 从 SaveData.diceTypeIds 加载
        ├── 可以添加/移除骰子
        └── 保存到 SaveData.diceTypeIds

CooldownSystem (使用 DiceManager)
    └── 从 DiceManager.PlayerDiceBackpack 获取骰子
        └── 构建8个骰子的池（补全到8个）
```

---

## 📝 实现步骤

### 阶段1：创建 DiceManager 类

#### 1.1 创建文件结构
**文件路径：** `DiceRogue/Assets/Scripts/Dices/DiceManager.cs`

**命名空间：** `DiceGame.Dices` 或 `DiceGame`

#### 1.2 类结构设计

```csharp
namespace DiceGame
{
    /// <summary>
    /// Manages dice with two separate pools:
    /// 1. Global dice pool: All dice types that can be obtained this run
    /// 2. Player dice backpack: Dice the player has acquired
    /// </summary>
    public class DiceManager
    {
        // 全局骰子池 - 所有可获得的骰子类型
        private readonly List<BaseDice> _globalDicePool = new();
        
        // 玩家背包 - 玩家已获得的骰子
        private readonly List<BaseDice> _playerDiceBackpack = new();
        
        // 公共属性
        public IReadOnlyList<BaseDice> GlobalDicePool => _globalDicePool;
        public IReadOnlyList<BaseDice> PlayerDiceBackpack => _playerDiceBackpack;
        
        // 初始化方法
        public void InitializeGlobalDicePool() { ... }
        
        // 背包管理方法
        public bool AddDiceToBackpack(BaseDice dice) { ... }
        public bool AddDiceToBackpackByName(string diceTypeName) { ... }
        public bool RemoveDiceFromBackpack(BaseDice dice) { ... }
        public void ClearBackpack() { ... }
        
        // 持久化方法
        public void LoadFromSaveData(SaveData saveData) { ... }
        public void SaveToSaveData(SaveData saveData) { ... }
        
        // 工具方法
        private BaseDice CreateDiceFromTypeName(string typeName) { ... }
    }
}
```

#### 1.3 核心方法实现要点

**InitializeGlobalDicePool()**
```csharp
public void InitializeGlobalDicePool()
{
    _globalDicePool.Clear();
    
    // 从 DicePool.GetAll() 获取所有骰子类型
    var allDiceTypes = DicePool.GetAll();
    
    // 过滤掉 Filler 骰子（如果需要）
    var nonFillerDice = allDiceTypes
        .Where(d => d != null && d.tier != DiceTier.Filler)
        .ToList();
    
    _globalDicePool.AddRange(nonFillerDice);
    
    Debug.Log($"[DiceManager] Initialized global dice pool with {_globalDicePool.Count} dice type(s)");
}
```

**AddDiceToBackpack()**
```csharp
public bool AddDiceToBackpack(BaseDice dice)
{
    if (dice == null)
    {
        Debug.LogWarning("[DiceManager] Cannot add null dice to backpack");
        return false;
    }
    
    // 检查是否已存在（根据类型名称）
    string typeName = dice.GetType().Name;
    if (_playerDiceBackpack.Any(d => d.GetType().Name == typeName))
    {
        Debug.LogWarning($"[DiceManager] Dice already in backpack: {typeName}");
        return false;
    }
    
    // 创建新实例（避免引用问题）
    var newDice = CreateDiceFromTypeName(typeName);
    if (newDice != null)
    {
        _playerDiceBackpack.Add(newDice);
        Debug.Log($"[DiceManager] Added dice to backpack: {newDice.diceName} ({newDice.tier})");
        return true;
    }
    
    return false;
}
```

**LoadFromSaveData() / SaveToSaveData()**
```csharp
public void LoadFromSaveData(SaveData saveData)
{
    _playerDiceBackpack.Clear();
    
    if (saveData == null || saveData.diceTypeIds == null)
    {
        Debug.LogWarning("[DiceManager] SaveData is null or diceTypeIds is null");
        return;
    }
    
    foreach (var typeId in saveData.diceTypeIds)
    {
        var dice = CreateDiceFromTypeName(typeId);
        if (dice != null)
        {
            _playerDiceBackpack.Add(dice);
        }
        else
        {
            Debug.LogWarning($"[DiceManager] Could not create dice from typeId: {typeId}");
        }
    }
    
    Debug.Log($"[DiceManager] Loaded {_playerDiceBackpack.Count} dice from save data");
}

public void SaveToSaveData(SaveData saveData)
{
    if (saveData == null)
    {
        Debug.LogWarning("[DiceManager] SaveData is null");
        return;
    }
    
    saveData.diceTypeIds.Clear();
    saveData.diceTypeIds.AddRange(
        _playerDiceBackpack
            .Where(d => d != null && d.tier != DiceTier.Filler)
            .Select(d => d.GetType().Name)
    );
    
    Debug.Log($"[DiceManager] Saved {saveData.diceTypeIds.Count} dice to save data");
}
```

**CreateDiceFromTypeName()**
```csharp
private BaseDice CreateDiceFromTypeName(string typeName)
{
    // 从全局池中查找原型
    var prototype = _globalDicePool.FirstOrDefault(d => d.GetType().Name == typeName);
    
    if (prototype == null)
    {
        // 如果全局池中没有，尝试从 DicePool.GetAll() 获取
        var allDice = DicePool.GetAll();
        prototype = allDice.FirstOrDefault(d => d.GetType().Name == typeName);
    }
    
    if (prototype != null)
    {
        // 使用反射创建新实例
        var diceType = prototype.GetType();
        var newDice = System.Activator.CreateInstance(diceType) as BaseDice;
        
        if (newDice != null)
        {
            // 复制属性
            newDice.diceName = prototype.diceName;
            newDice.description = prototype.description;
            newDice.tier = prototype.tier;
            newDice.cost = prototype.cost;
            newDice.cooldownAfterUse = prototype.cooldownAfterUse;
            newDice.cooldownRemain = 0;
            newDice.isLocked = false;
            newDice.lastRollValue = 0;
            
            return newDice;
        }
    }
    
    Debug.LogWarning($"[DiceManager] Could not create dice from typeName: {typeName}");
    return null;
}
```

---

### 阶段2：修改 BattleController

#### 2.1 添加 DiceManager 字段

```csharp
public class BattleController : MonoBehaviour
{
    // ... 现有字段 ...
    
    private DiceManager _diceManager;  // 新增
    
    // ... 其他字段 ...
}
```

#### 2.2 初始化 DiceManager

**在 `InitializeCoreComponents()` 中：**
```csharp
private void InitializeCoreComponents()
{
    // ... 现有初始化 ...
    
    // 初始化骰子管理器
    _diceManager = new DiceManager();
    _diceManager.InitializeGlobalDicePool();
    
    // 从保存数据加载玩家背包
    _diceManager.LoadFromSaveData(_stateManager.SaveData);
    
    // ... 其他初始化 ...
}
```

#### 2.3 修改 IntegrateRewardDice()

**替换现有实现：**
```csharp
private void IntegrateRewardDice()
{
    // Check if there are pending reward dice
    if (_stateManager.State.PendingDiceTypeIds.Count == 0)
    {
        Debug.Log("[BattleController] No pending reward dice to integrate");
        return;
    }

    Debug.Log($"[BattleController] Found {_stateManager.State.PendingDiceTypeIds.Count} reward dice to integrate");

    // 使用 DiceManager 添加骰子到背包
    foreach (var typeId in _stateManager.State.PendingDiceTypeIds)
    {
        bool success = _diceManager.AddDiceToBackpackByName(typeId);
        if (success)
        {
            Debug.Log($"[BattleController] Added reward dice to backpack: {typeId}");
        }
        else
        {
            Debug.LogWarning($"[BattleController] Failed to add reward dice: {typeId}");
        }
    }

    // Clear pending list
    _stateManager.State.PendingDiceTypeIds.Clear();

    // 保存到持久化
    _diceManager.SaveToSaveData(_stateManager.SaveData);
    _stateManager.Save();

    // 更新 CooldownSystem（从背包获取骰子）
    UpdateCooldownSystemFromBackpack();

    Debug.Log($"[BattleController] Integrated reward dice. Backpack size: {_diceManager.PlayerDiceBackpack.Count}");
}

private void UpdateCooldownSystemFromBackpack()
{
    // 从 DiceManager 获取玩家背包
    var backpackDice = _diceManager.PlayerDiceBackpack.ToList();
    
    // 更新 CooldownSystem
    cooldownSystem.SetPlayerBackpackDice(backpackDice);
}
```

#### 2.4 修改 InitializeRelicSystem() 的对应逻辑

**在 `InitializeManagers()` 中，添加骰子背包初始化：**
```csharp
private void InitializeManagers()
{
    // ... 现有代码 ...
    
    // 初始化骰子系统：从保存数据加载
    InitializeDiceSystem();
    
    // ... 其他初始化 ...
}

private void InitializeDiceSystem()
{
    // DiceManager 已经在 InitializeCoreComponents() 中初始化
    // 这里只需要确保 CooldownSystem 使用背包数据
    UpdateCooldownSystemFromBackpack();
}
```

---

### 阶段3：修改 CooldownSystem

#### 3.1 修改初始化逻辑

**当前实现：**
```csharp
private void InitializeDicePool()
{
    // TODO: Get dice from player's backpack/inventory system
    var backpackDice = GetPlayerBackpackDice(); // 使用随机生成
    // ...
}
```

**新实现：**
```csharp
// 移除 GetPlayerBackpackDice() 方法
// 依赖外部调用 SetPlayerBackpackDice() 来设置骰子
```

**注意：** `CooldownSystem` 不再自己生成骰子，而是等待 `BattleController` 调用 `SetPlayerBackpackDice()`

#### 3.2 保持 SetPlayerBackpackDice() 方法

**无需修改，继续使用现有实现**

---

### 阶段4：修改游戏重置逻辑

#### 4.1 BattleController.ResetForNewHand()

**在重置时清空背包：**
```csharp
void ResetForNewHand()
{
    // ... 现有重置逻辑 ...
    
    // Reset dice backpack (game over / restart)
    if (_diceManager != null)
    {
        _diceManager.ClearBackpack();
        _diceManager.SaveToSaveData(_stateManager.SaveData);
        _stateManager.Save();
    }
    
    // ... 其他重置逻辑 ...
}
```

#### 4.2 BattleController.OnSettingsResetClicked()

**同样添加清空背包逻辑：**
```csharp
private void OnSettingsResetClicked()
{
    // ... 现有重置逻辑 ...
    
    // Reset dice backpack
    if (_diceManager != null)
    {
        _diceManager.ClearBackpack();
        _diceManager.SaveToSaveData(_stateManager.SaveData);
        _stateManager.ResetSaveData();
    }
    
    // ... 其他重置逻辑 ...
}
```

---

### 阶段5：修改 BackpackManager

#### 5.1 修改数据来源

**当前实现：**
```csharp
private void RefreshDiceList()
{
    var allDice = _cooldownSystem.GetAllDice(); // 从 CooldownSystem 获取
    diceSelectionUI.DisplayDice(allDice);
}
```

**新实现：**
```csharp
// BackpackManager 需要 DiceManager 引用
private DiceManager _diceManager;

public void Initialize(CooldownSystem cooldownSystem, DiceManager diceManager, Action<List<BaseDice>> onDiceSelected)
{
    _cooldownSystem = cooldownSystem;
    _diceManager = diceManager; // 新增
    _onDiceSelected = onDiceSelected;
    // ... 其他初始化 ...
}

private void RefreshDiceList()
{
    // 从 CooldownSystem 获取当前可用的骰子（考虑冷却）
    var availableDice = _cooldownSystem.GetAvailableDice();
    diceSelectionUI.DisplayDice(availableDice);
    
    // 或者：显示玩家背包中的所有骰子（包括冷却中的）
    // var allBackpackDice = _diceManager.PlayerDiceBackpack.ToList();
    // diceSelectionUI.DisplayDice(allBackpackDice);
}
```

**注意：** 需要根据游戏设计决定显示：
- **选项A：** 只显示当前可用的骰子（`GetAvailableDice()`）
- **选项B：** 显示背包中的所有骰子（包括冷却中的）

---

### 阶段6：修改奖励系统

#### 6.1 RewardSceneManager

**当前实现：**
```csharp
allDicePool = DicePool.GetAll(); // 直接使用 DicePool
```

**新实现（可选）：**
```csharp
// 可以继续使用 DicePool.GetAll()
// 或者从 DiceManager.GlobalDicePool 获取
// 两种方式都可以，因为都是全局池
```

**保持不变，因为奖励系统需要从全局池选择，而不是玩家背包**

---

## 🔄 数据流对比

### 修改前

```
DicePool.GetAll()
    ↓
CooldownSystem (随机生成8个骰子)
    ↓
BackpackManager (显示骰子)
    ↓
玩家选择
    ↓
BattleController.IntegrateRewardDice()
    ↓
手动保存到 SaveData.diceTypeIds
```

### 修改后

```
DicePool.GetAll()
    ↓
DiceManager.InitializeGlobalDicePool()
    ↓
DiceManager.LoadFromSaveData() (加载玩家背包)
    ↓
DiceManager.PlayerDiceBackpack
    ↓
CooldownSystem.SetPlayerBackpackDice() (构建8个骰子池)
    ↓
BackpackManager (显示可用骰子)
    ↓
玩家选择
    ↓
奖励骰子 → DiceManager.AddDiceToBackpack()
    ↓
DiceManager.SaveToSaveData() (自动保存)
```

---

## 📋 修改清单

### 需要创建的文件
- [ ] `DiceRogue/Assets/Scripts/Dices/DiceManager.cs`

### 需要修改的文件
- [ ] `DiceRogue/Assets/Scripts/Battle/BattleController.cs`
  - [ ] 添加 `_diceManager` 字段
  - [ ] 在 `InitializeCoreComponents()` 中初始化
  - [ ] 修改 `IntegrateRewardDice()` 方法
  - [ ] 添加 `UpdateCooldownSystemFromBackpack()` 方法
  - [ ] 修改 `ResetForNewHand()` 方法
  - [ ] 修改 `OnSettingsResetClicked()` 方法
  - [ ] 添加 `InitializeDiceSystem()` 方法

- [ ] `DiceRogue/Assets/Scripts/UI/BackpackManager.cs`
  - [ ] 添加 `_diceManager` 字段
  - [ ] 修改 `Initialize()` 方法签名
  - [ ] 修改 `RefreshDiceList()` 方法（可选）

- [ ] `DiceRogue/Assets/Scripts/Battle/CooldownSystem.cs`
  - [ ] 移除 `GetPlayerBackpackDice()` 方法（或标记为废弃）
  - [ ] 修改 `InitializeDicePool()` 注释

### 可选修改的文件
- [ ] `DiceRogue/Assets/Scripts/Reward/RewardSceneManager.cs`
  - [ ] 可以继续使用 `DicePool.GetAll()`，或改为使用 `DiceManager.GlobalDicePool`

---

## ⚠️ 注意事项

### 1. 向后兼容性
- `SaveData.diceTypeIds` 字段保持不变
- 现有的保存/加载逻辑仍然有效

### 2. 初始化顺序
```
1. GameStateManager.Instance
2. DiceManager (初始化全局池)
3. DiceManager.LoadFromSaveData() (加载玩家背包)
4. CooldownSystem.SetPlayerBackpackDice() (设置骰子池)
5. BackpackManager (显示骰子)
```

### 3. 骰子实例化
- 每次添加到背包时创建新实例（避免引用问题）
- 使用 `CreateDiceFromTypeName()` 统一创建

### 4. 测试要点
- [ ] 新游戏：背包为空，使用默认骰子
- [ ] 获得奖励骰子：正确添加到背包并保存
- [ ] 重新加载：从保存数据正确加载背包
- [ ] 游戏重置：清空背包并重新开始
- [ ] 背包显示：正确显示玩家拥有的骰子

---

## 🎯 预期效果

### 修改后系统特点

1. **清晰的职责分离**
   - `DiceManager`：管理全局池和玩家背包
   - `CooldownSystem`：管理当前游戏的8个骰子池
   - `BackpackManager`：UI显示

2. **统一的数据管理**
   - 所有骰子操作通过 `DiceManager`
   - 自动处理保存/加载
   - 与 `RelicManager` 结构一致

3. **易于扩展**
   - 可以轻松添加骰子管理功能
   - 可以添加骰子分类、筛选等功能
   - 可以添加骰子升级、强化等功能

4. **代码可维护性**
   - 逻辑集中，易于调试
   - 与现有 Relic 系统保持一致
   - 减少代码重复

---

## 📚 参考

- `RelicManager.cs` - 参考实现
- `DicePool.cs` - 全局骰子类型定义
- `CooldownSystem.cs` - 当前骰子池管理
- `SaveData.cs` - 保存数据结构

---

**文档创建时间：** 2024  
**状态：** 计划阶段，待实现  
**下一步：** 按照阶段顺序逐步实现

