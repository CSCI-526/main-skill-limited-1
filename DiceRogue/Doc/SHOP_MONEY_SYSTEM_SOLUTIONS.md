# ShopScene 金钱系统跨场景访问解决方案分析

## 问题描述

ShopScene 中无法找到 BattleController，导致无法访问金钱系统，显示 $0。

**错误信息：**
```
[Shop] BattleController not found. Displaying $0
```

---

## 方案对比分析

### 方案 1：不销毁 BattleController（使用 DontDestroyOnLoad）

#### 实现方式
在 BattleController 的 `Awake()` 或 `Start()` 中添加：
```csharp
void Awake()
{
    DontDestroyOnLoad(gameObject);
}
```

#### 优点 ✅
1. **实现简单**：只需添加一行代码
2. **保持现有架构**：不需要重构代码
3. **数据一致性**：MoneyManager 实例保持不变，数据同步简单
4. **快速修复**：可以立即解决问题

#### 缺点 ❌
1. **内存浪费**：BattleController 包含大量场景特定的 UI 引用：
   - `diceRowParent`, `diceViewPrefab`
   - `rollButton`, `submitComboButton`, `continueButton`
   - `settingsPanel`, `comboPreferencePanel`
   - `battleUI`, `scoreAnimator`, `backpackManager`
   - `relicDisplay`, `cooldownSystem`
   - 等等...
   
2. **Null 引用风险**：这些 UI 引用在 ShopScene 中都是 null，可能导致：
   - 运行时错误（如果代码访问这些引用）
   - 混乱的代码逻辑（需要大量 null 检查）

3. **架构混乱**：BattleController 是 BattleScene 的控制器，不应该跨场景存在

4. **维护困难**：未来添加新功能时，需要不断检查是否会影响 ShopScene

5. **性能问题**：保留不必要的组件和引用占用内存

#### 风险评估
- **技术风险**：中低（需要大量 null 检查）
- **维护风险**：高（架构混乱，难以维护）
- **性能风险**：中（内存占用）

---

### 方案 2：将需要跨场景的方法单独分离一个类（推荐 ⭐）

#### 实现方式
创建一个新的 `PlayerResourceManager` 单例类，专门管理跨场景的资源：

```csharp
namespace DiceGame
{
    /// <summary>
    /// 玩家资源管理器（单例，DontDestroyOnLoad）
    /// 管理跨场景的玩家资源：金钱、骰子背包、遗物等
    /// </summary>
    public class PlayerResourceManager : MonoBehaviour
    {
        private static PlayerResourceManager _instance;
        public static PlayerResourceManager Instance { get; private set; }
        
        private MoneyManager _moneyManager;
        private DiceManager _diceManager;
        private RelicManager _relicManager;
        private GameStateManager _stateManager;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        private void Initialize()
        {
            _stateManager = GameStateManager.Instance;
            
            // 初始化 MoneyManager（从 SaveData 加载）
            _moneyManager = new MoneyManager(_stateManager.SaveData.money);
            
            // 初始化 DiceManager
            _diceManager = new DiceManager();
            _diceManager.InitializeGlobalDicePool();
            _diceManager.LoadFromSaveData(_stateManager.SaveData);
            
            // 初始化 RelicManager
            _relicManager = new RelicManager();
            _relicManager.InitializeGlobalRelicPool();
            // 加载遗物...
        }
        
        // 金钱相关方法
        public int GetMoney() => _moneyManager.Money;
        public bool SpendMoney(int amount) { /* ... */ }
        public void AddMoney(int amount) { /* ... */ }
        
        // 骰子相关方法
        public bool AddDiceToBackpack(BaseDice dice) { /* ... */ }
        public IReadOnlyList<BaseDice> GetPlayerDiceBackpack() => _diceManager.PlayerDiceBackpack;
        
        // 遗物相关方法
        public bool AddRelicToBackpack(RelicBase relic) { /* ... */ }
        public IReadOnlyList<RelicBase> GetPlayerRelics() => _relicManager.PlayerBackpack;
        
        // 同步方法
        public void SyncFromSaveData() { /* ... */ }
        public void SaveToSaveData() { /* ... */ }
    }
}
```

#### 优点 ✅
1. **清晰的架构**：职责分离，只保留跨场景需要的功能
2. **内存高效**：不保留场景特定的 UI 引用
3. **易于维护**：专门的类管理跨场景资源
4. **可扩展性**：未来添加新资源类型很容易
5. **无 Null 引用风险**：不包含场景特定的引用
6. **符合单一职责原则**：每个类只负责自己的职责

#### 缺点 ❌
1. **需要重构**：需要修改 BattleController 和 ShopManager
2. **开发时间**：需要创建新类并迁移代码
3. **需要测试**：确保所有功能正常工作

#### 风险评估
- **技术风险**：低（架构清晰，代码质量高）
- **维护风险**：低（架构清晰，易于维护）
- **性能风险**：低（内存占用最小）

---

## 推荐方案：方案 2（分离跨场景资源管理器）

### 理由

1. **架构清晰**：符合单一职责原则
2. **长期维护**：未来添加新场景时更容易扩展
3. **性能优化**：只保留必要的数据和逻辑
4. **代码质量**：避免 null 引用和混乱的依赖关系

### 实施步骤

#### 阶段 1：创建 PlayerResourceManager（1-2 小时）
1. 创建 `PlayerResourceManager.cs`
2. 实现单例模式
3. 添加 MoneyManager、DiceManager、RelicManager
4. 实现跨场景需要的公共方法

#### 阶段 2：修改 BattleController（1 小时）
1. 移除 MoneyManager、DiceManager、RelicManager 的直接管理
2. 改为从 `PlayerResourceManager.Instance` 获取
3. 保留场景特定的 UI 管理功能

#### 阶段 3：修改 ShopManager（30 分钟）
1. 移除 BattleController 依赖
2. 改为使用 `PlayerResourceManager.Instance`
3. 简化代码逻辑

#### 阶段 4：测试和验证（1 小时）
1. 测试金钱系统
2. 测试骰子购买
3. 测试数据持久化
4. 测试场景切换

**总时间估算：3-4 小时**

---

## 临时解决方案（如果急需修复）

如果现在需要快速修复，可以先实现方案 1，但需要：

1. 在 BattleController.Awake() 中添加 DontDestroyOnLoad
2. 添加大量 null 检查，避免访问 ShopScene 中不存在的 UI
3. 标记为临时方案，计划后续重构为方案 2

---

## 最终建议

**推荐使用方案 2**，因为：
- 架构更清晰
- 长期维护成本更低
- 性能更好
- 代码质量更高

如果需要快速修复，可以先实现方案 1，但应该计划在后续重构为方案 2。

