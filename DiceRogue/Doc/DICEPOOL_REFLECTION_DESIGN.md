# 🎲 DicePool 反射式自动发现设计

## 📋 当前问题分析

### 当前 DicePool 的实现

```csharp
public static class DicePool
{
    public static List<BaseDice> GetAll()
    {
        return new List<BaseDice>
        {
            new BigOne(), new BigSix(), new CounterDice(), ... // 硬编码
        };
    }
}
```

**问题：**
1. ❌ **硬编码**：每次添加新骰子都需要修改 `DicePool.cs`
2. ❌ **维护成本高**：容易遗漏或重复
3. ❌ **不够灵活**：无法动态发现新骰子
4. ❌ **违反开闭原则**：添加新功能需要修改现有代码

---

## 🎯 设计目标

### 理想的数据流

```
Dices 文件夹中的骰子类
    ↓ (自动发现)
反射扫描所有 BaseDice 子类
    ↓
DicePool.GetAll() 动态返回
    ↓
DiceManager.InitializeGlobalDicePool()
```

**优势：**
- ✅ **零维护**：添加新骰子类自动被发现
- ✅ **类型安全**：编译时检查
- ✅ **灵活**：可以添加过滤、排序等逻辑
- ✅ **优雅**：符合开闭原则

---

## 💡 设计方案

### 方案1：反射式自动发现（推荐）

#### 核心思路

使用 C# 反射扫描当前程序集中所有 `BaseDice` 的子类，自动实例化并返回。

#### 实现要点

```csharp
public static class DicePool
{
    private static List<BaseDice> _cachedDiceTypes = null;
    
    /// <summary>
    /// 自动发现所有 BaseDice 的子类并返回实例列表
    /// 使用缓存避免重复反射
    /// </summary>
    public static List<BaseDice> GetAll()
    {
        if (_cachedDiceTypes != null)
        {
            return new List<BaseDice>(_cachedDiceTypes);
        }
        
        _cachedDiceTypes = DiscoverDiceTypes();
        return new List<BaseDice>(_cachedDiceTypes);
    }
    
    /// <summary>
    /// 使用反射发现所有骰子类型
    /// </summary>
    private static List<BaseDice> DiscoverDiceTypes()
    {
        var diceTypes = new List<BaseDice>();
        
        // 获取当前程序集中所有 BaseDice 的子类
        var assembly = typeof(BaseDice).Assembly;
        var baseDiceType = typeof(BaseDice);
        
        var diceClasses = assembly.GetTypes()
            .Where(t => 
                baseDiceType.IsAssignableFrom(t) &&  // 是 BaseDice 的子类
                !t.IsAbstract &&                      // 不是抽象类
                t != baseDiceType                     // 不是 BaseDice 本身
            )
            .ToList();
        
        foreach (var diceType in diceClasses)
        {
            try
            {
                // 创建实例
                var dice = System.Activator.CreateInstance(diceType) as BaseDice;
                if (dice != null)
                {
                    diceTypes.Add(dice);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DicePool] Failed to create instance of {diceType.Name}: {ex.Message}");
            }
        }
        
        Debug.Log($"[DicePool] Discovered {diceTypes.Count} dice types via reflection");
        return diceTypes;
    }
    
    /// <summary>
    /// 清除缓存（用于测试或重新加载）
    /// </summary>
    public static void ClearCache()
    {
        _cachedDiceTypes = null;
    }
    
    // ... 其他方法保持不变
}
```

**优点：**
- ✅ 完全自动，零维护
- ✅ 类型安全
- ✅ 性能优化（缓存）

**缺点：**
- ⚠️ 反射有轻微性能开销（但可以缓存）
- ⚠️ 需要确保所有骰子类都有无参构造函数

---

### 方案2：特性标记 + 反射（更灵活）

#### 核心思路

添加特性标记哪些骰子应该被包含，支持排除某些骰子。

```csharp
/// <summary>
/// 标记骰子是否应该出现在全局池中
/// </summary>
[System.AttributeUsage(System.AttributeTargets.Class)]
public class DicePoolAttribute : System.Attribute
{
    public bool IncludeInPool { get; set; } = true;
    
    public DicePoolAttribute(bool includeInPool = true)
    {
        IncludeInPool = includeInPool;
    }
}

// 使用示例
[DicePool(IncludeInPool = true)]
public class BigOne : BaseDice { ... }

[DicePool(IncludeInPool = false)]  // 临时禁用
public class D8 : BaseDice { ... }
```

**优点：**
- ✅ 更灵活，可以控制哪些骰子被包含
- ✅ 可以临时禁用某些骰子

**缺点：**
- ⚠️ 需要修改所有骰子类添加特性
- ⚠️ 增加复杂度

---

### 方案3：混合方案（推荐用于过渡）

#### 核心思路

保留硬编码列表作为"白名单"，但使用反射作为后备。

```csharp
public static List<BaseDice> GetAll()
{
    // 优先使用硬编码列表（明确控制）
    var hardcodedList = GetHardcodedDice();
    
    // 使用反射发现所有类型
    var discoveredList = DiscoverDiceTypes();
    
    // 合并并去重（按类型名）
    var merged = new List<BaseDice>();
    var addedTypes = new HashSet<string>();
    
    // 先添加硬编码的
    foreach (var dice in hardcodedList)
    {
        var typeName = dice.GetType().Name;
        if (!addedTypes.Contains(typeName))
        {
            merged.Add(dice);
            addedTypes.Add(typeName);
        }
    }
    
    // 再添加反射发现的（如果不在硬编码列表中）
    foreach (var dice in discoveredList)
    {
        var typeName = dice.GetType().Name;
        if (!addedTypes.Contains(typeName))
        {
            merged.Add(dice);
            addedTypes.Add(typeName);
            Debug.Log($"[DicePool] Auto-discovered new dice type: {typeName}");
        }
    }
    
    return merged;
}
```

**优点：**
- ✅ 向后兼容
- ✅ 可以明确控制哪些骰子被包含
- ✅ 自动发现新骰子

**缺点：**
- ⚠️ 仍然需要维护硬编码列表（但可以逐步移除）

---

## 🎯 推荐方案：方案1（纯反射）

### 理由

1. **最优雅**：完全自动，符合开闭原则
2. **零维护**：添加新骰子类自动被发现
3. **性能可接受**：使用缓存，只在首次调用时反射
4. **简单**：不需要修改现有骰子类

### 实现细节

#### 1. 过滤规则

```csharp
var diceClasses = assembly.GetTypes()
    .Where(t => 
        baseDiceType.IsAssignableFrom(t) &&  // 是 BaseDice 的子类
        !t.IsAbstract &&                      // 不是抽象类
        t != baseDiceType &&                  // 不是 BaseDice 本身
        !t.IsGenericType                      // 不是泛型类型
    )
    .ToList();
```

#### 2. 排序规则（可选）

```csharp
// 按稀有度排序：Common -> Rare -> Legendary
diceTypes = diceTypes
    .OrderBy(d => d.tier)
    .ThenBy(d => d.diceName)
    .ToList();
```

#### 3. 排除规则

```csharp
// 排除 NormalDice（它是 Filler，不应该在全局池中）
var excludedTypes = new HashSet<string> { "NormalDice" };

if (!excludedTypes.Contains(diceType.Name))
{
    var dice = System.Activator.CreateInstance(diceType) as BaseDice;
    // ...
}
```

---

## 📊 数据流对比

### 当前数据流

```
DicePool.GetAll() [硬编码]
    ↓
手动维护列表
    ↓
DiceManager.InitializeGlobalDicePool()
```

### 优化后数据流

```
Dices 文件夹中的骰子类
    ↓
反射自动发现
    ↓
DicePool.GetAll() [动态]
    ↓
缓存（首次调用后）
    ↓
DiceManager.InitializeGlobalDicePool()
```

---

## 🔧 实现步骤

### 步骤1：修改 DicePool.cs

1. 添加缓存机制
2. 实现反射发现逻辑
3. 保留原有方法签名（向后兼容）

### 步骤2：处理特殊情况

1. **NormalDice**：应该被排除（它是 Filler）
2. **D8**：如果被临时禁用，可以通过排除列表处理
3. **抽象类**：自动排除

### 步骤3：测试

1. 验证所有现有骰子都被发现
2. 验证新添加的骰子自动被发现
3. 验证性能（缓存是否生效）

---

## ⚠️ 注意事项

### 1. 性能考虑

**反射性能：**
- 首次调用：~1-5ms（取决于骰子数量）
- 后续调用：~0.001ms（使用缓存）

**优化：**
- 使用缓存避免重复反射
- 只在首次调用时执行发现逻辑

### 2. 构造函数要求

**要求：**
- 所有骰子类必须有**无参构造函数**
- 构造函数中初始化属性（如 `diceName`, `tier` 等）

**当前状态：** ✅ 所有骰子类都满足这个要求

### 3. 命名空间

**注意：**
- 反射会扫描整个程序集
- 确保骰子类都在正确的命名空间中
- 避免命名冲突

### 4. 排除列表

**建议排除：**
- `NormalDice` - Filler 骰子，不应该在全局池中
- `BaseDice` - 抽象基类
- 任何测试用的骰子类

---

## 🎯 最终推荐实现

### 核心代码结构

```csharp
public static class DicePool
{
    private static List<BaseDice> _cachedDiceTypes = null;
    private static readonly HashSet<string> _excludedTypes = new HashSet<string>
    {
        "NormalDice",  // Filler 骰子
        "BaseDice"     // 抽象基类
    };
    
    public static List<BaseDice> GetAll()
    {
        if (_cachedDiceTypes != null)
        {
            return new List<BaseDice>(_cachedDiceTypes);
        }
        
        _cachedDiceTypes = DiscoverDiceTypes();
        return new List<BaseDice>(_cachedDiceTypes);
    }
    
    private static List<BaseDice> DiscoverDiceTypes()
    {
        var diceTypes = new List<BaseDice>();
        var assembly = typeof(BaseDice).Assembly;
        var baseDiceType = typeof(BaseDice);
        
        var diceClasses = assembly.GetTypes()
            .Where(t => 
                baseDiceType.IsAssignableFrom(t) &&
                !t.IsAbstract &&
                t != baseDiceType &&
                !_excludedTypes.Contains(t.Name)
            )
            .OrderBy(t => t.Name)  // 排序以便调试
            .ToList();
        
        foreach (var diceType in diceClasses)
        {
            try
            {
                var dice = System.Activator.CreateInstance(diceType) as BaseDice;
                if (dice != null)
                {
                    diceTypes.Add(dice);
                    Debug.Log($"[DicePool] Discovered: {dice.diceName} ({dice.tier})");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DicePool] Failed to create {diceType.Name}: {ex.Message}");
            }
        }
        
        Debug.Log($"[DicePool] Auto-discovered {diceTypes.Count} dice types");
        return diceTypes;
    }
    
    public static void ClearCache()
    {
        _cachedDiceTypes = null;
    }
    
    // 保持原有方法不变
    public static List<BaseDice> GetNonFiller() =>
        GetAll().Where(d => d != null && d.tier != DiceTier.Filler).ToList();
    
    public static List<BaseDice> GetByTier(DiceTier tier) =>
        GetAll().Where(d => d != null && d.tier != DiceTier.Filler && d.tier == tier).ToList();
}
```

---

## 📈 收益分析

### 代码维护

**修改前：**
- 添加新骰子：修改 `DicePool.cs`（1个文件）
- 风险：容易遗漏或拼写错误

**修改后：**
- 添加新骰子：创建新类文件（0个文件需要修改）
- 风险：几乎为零

### 代码行数

**修改前：**
```csharp
return new List<BaseDice>
{
    new BigOne(), new BigSix(), ... // 20+ 行
};
```

**修改后：**
```csharp
return DiscoverDiceTypes(); // 1 行（逻辑在方法中）
```

### 可扩展性

**修改前：**
- 需要修改现有代码
- 违反开闭原则

**修改后：**
- 完全符合开闭原则
- 易于扩展

---

## 🧪 测试建议

### 测试场景

1. **基本功能**
   - [ ] 所有现有骰子都被发现
   - [ ] NormalDice 被正确排除
   - [ ] 缓存正常工作

2. **新骰子测试**
   - [ ] 添加新骰子类
   - [ ] 验证自动被发现
   - [ ] 验证不需要修改 DicePool.cs

3. **性能测试**
   - [ ] 首次调用时间（应该 < 10ms）
   - [ ] 后续调用时间（应该 < 1ms）
   - [ ] 内存占用（缓存大小）

4. **边界情况**
   - [ ] 抽象类被排除
   - [ ] 泛型类被排除
   - [ ] 异常处理（构造函数失败）

---

## 📝 实施建议

### 阶段1：实现反射发现（保持向后兼容）

1. 添加 `DiscoverDiceTypes()` 方法
2. 修改 `GetAll()` 使用反射
3. 保留硬编码列表作为后备（如果反射失败）

### 阶段2：完全切换到反射

1. 移除硬编码列表
2. 完全依赖反射
3. 添加排除列表配置

### 阶段3：优化和测试

1. 性能优化（缓存）
2. 添加日志
3. 完整测试

---

**文档创建时间：** 2024  
**状态：** 设计方案，待实施  
**推荐方案：** 方案1（纯反射 + 缓存）

