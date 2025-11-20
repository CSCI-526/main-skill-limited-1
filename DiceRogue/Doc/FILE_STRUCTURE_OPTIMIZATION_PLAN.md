# 📁 文件结构优化计划

## 📋 当前文件结构分析

### Dices 文件夹结构

```
Dices/
├── BaseDice.cs                    ✅ 基类，保留
├── DicePool.cs                    ⚠️ 硬编码，需要改为反射
├── DiceManager.cs                 ✅ 管理器，保留
├── DiceUI.cs                      ✅ UI组件，保留
├── DiceHoverTooltip.cs            ✅ UI组件，保留
├── NormalDice.cs                  ✅ Filler骰子，保留
└── [各种骰子类].cs                 ✅ 具体骰子实现，保留
```

### Battle/Factories 文件夹结构

```
Battle/Factories/
├── DicePoolFactory.cs             ❌ 与 DicePool 重复，可以删除
└── [其他Factory].cs               ✅ 其他工厂类，保留
```

---

## 🔍 文件重复分析

### DicePool vs DicePoolFactory

| 文件 | 职责 | 使用位置 | 状态 |
|------|------|---------|------|
| **DicePool.cs** | 提供所有骰子类型的静态列表 | DiceManager, RewardSceneManager | ✅ 主要数据源 |
| **DicePoolFactory.cs** | 创建随机骰子池（8个骰子） | CooldownSystem（废弃方法） | ❌ 可以删除 |

**结论：**
- `DicePoolFactory` 只在废弃方法中使用
- 功能可以被 `DicePool` + 随机选择替代
- **建议删除** `DicePoolFactory.cs`

---

## 🎯 优化方案

### 阶段1：实现反射式 DicePool

**目标：** 使用反射自动发现所有骰子类型

**修改文件：**
- `DiceRogue/Assets/Scripts/Dices/DicePool.cs`

**关键改进：**
- 移除硬编码列表
- 添加反射发现逻辑
- 添加缓存机制
- 添加排除列表（NormalDice, BaseDice）

---

### 阶段2：优化 DiceManager

**目标：** 使用 `DicePool.GetNonFiller()` 替代手动过滤

**修改文件：**
- `DiceRogue/Assets/Scripts/Dices/DiceManager.cs`

**关键改进：**
- 使用 `DicePool.GetNonFiller()` 替代手动过滤
- 简化代码

---

### 阶段3：删除 DicePoolFactory

**目标：** 移除重复代码

**删除文件：**
- `DiceRogue/Assets/Scripts/Battle/Factories/DicePoolFactory.cs`

**修改文件：**
- `DiceRogue/Assets/Scripts/Battle/CooldownSystem.cs`
  - 移除对 `DicePoolFactory` 的引用
  - 如果废弃方法需要随机池，使用 `DicePool.GetAll()` + 随机选择

---

### 阶段4：优化 RewardSceneManager

**目标：** 使用 `DicePool.GetNonFiller()` 替代 `GetAll()`

**修改文件：**
- `DiceRogue/Assets/Scripts/Reward/RewardSceneManager.cs`

**关键改进：**
- 后备逻辑使用 `GetNonFiller()` 而不是 `GetAll()`

---

## 📊 优化后的文件结构

### Dices 文件夹（优化后）

```
Dices/
├── BaseDice.cs                    ✅ 基类
├── DicePool.cs                    ✅ 反射式自动发现（新实现）
├── DiceManager.cs                 ✅ 管理器（优化后）
├── DiceUI.cs                      ✅ UI组件
├── DiceHoverTooltip.cs            ✅ UI组件
├── NormalDice.cs                  ✅ Filler骰子
└── [各种骰子类].cs                 ✅ 具体骰子实现
```

### Battle/Factories 文件夹（优化后）

```
Battle/Factories/
└── [其他Factory].cs               ✅ DicePoolFactory 已删除
```

---

## 🔄 数据流优化

### 优化前

```
DicePool.GetAll() [硬编码]
    ↓
手动维护列表
    ↓
DiceManager.InitializeGlobalDicePool()
    ↓
手动过滤 Filler
```

### 优化后

```
Dices 文件夹中的骰子类
    ↓
反射自动发现
    ↓
DicePool.GetAll() [动态]
    ↓
缓存
    ↓
DicePool.GetNonFiller() [自动过滤]
    ↓
DiceManager.InitializeGlobalDicePool()
```

---

## 📝 实施步骤

### 步骤1：实现反射式 DicePool ✅

### 步骤2：优化 DiceManager ✅

### 步骤3：删除 DicePoolFactory ✅

### 步骤4：优化 RewardSceneManager ✅

### 步骤5：更新 CooldownSystem ✅

---

**文档创建时间：** 2024  
**状态：** 计划阶段，待实施

