# 教程整合到 BattleScene 指南

## 概述

教程系统已整合到 BattleScene，作为 Level 0。现在教程和正常游戏都在同一个场景中，无需场景切换。

## 代码改动总结

### 1. ProgressionManager.cs
- ✅ 添加了 `InitializeTutorialMode()` 方法（设置 Level 0）
- ✅ 添加了 `StartNormalGame()` 方法（从教程转换到 Level 1）
- ✅ 添加了 `IsTutorialMode` 属性（检查是否为教程模式）
- ✅ 更新了 `EvaluateTargetScore()` 跳过 Level 0 的评估

### 2. BattleController.cs
- ✅ 添加了静态标志 `IsTutorialMode`
- ✅ 在 `Start()` 中检测教程模式并初始化
- ✅ 添加了 `CompleteTutorialAndStartLevel1()` 公共方法
- ✅ 更新了 `UpdateLevelInfo()` 显示 "Tutorial" 而不是 "Level 0"
- ✅ 在 `EvaluateTargetScore()` 中跳过教程模式

### 3. TutorialController.cs
- ✅ 修改了 `CompleteTutorial()` 不再切换场景，直接调用 `BattleController.CompleteTutorialAndStartLevel1()`

### 4. RunLoader.cs
- ✅ `StartTutorial()` 现在加载 BattleScene（不是 TutorialScene）
- ✅ 设置 `BattleController.IsTutorialMode = true` 标志
- ✅ `StartRun()` 确保 `IsTutorialMode = false`

## Unity UI 修改步骤

### 步骤 1: 合并场景资源（可选但推荐）

如果你希望完全移除 TutorialScene：

1. **打开 BattleScene**
   - 在 Unity Editor 中打开 `Assets/Scenes/BattleScene.unity`

2. **确保 TutorialController 存在**
   - 检查场景中是否有 `TutorialController` GameObject
   - 如果没有，从 TutorialScene 复制过来：
     - 打开 `Assets/Scenes/TutorialScene.unity`
     - 找到 `TutorialController` GameObject
     - 复制它（Ctrl+D 或右键 Duplicate）
     - 切换回 BattleScene
     - 粘贴到 BattleScene 中

3. **确保 Tutorial UI 元素存在**
   - 检查是否有 `TutorialPromptPanel` GameObject
   - 如果没有，从 TutorialScene 复制：
     - 在 TutorialScene 中找到所有教程相关的 UI 元素
     - 复制到 BattleScene

### 步骤 2: 配置 TutorialController

1. **选择 TutorialController GameObject**
   - 在 BattleScene 的 Hierarchy 中找到 `TutorialController`

2. **配置 Inspector 中的引用**
   - `Battle Controller`: 拖拽 BattleScene 中的 `BattleController` GameObject
   - `Backpack Manager`: 拖拽 `BackpackManager` GameObject
   - `Dice Selection UI`: 拖拽背包相关的 UI
   - `Dice Row Parent`: 拖拽骰子行的父对象
   - `Score Animator`: 拖拽 `ScoreAnimator` GameObject
   - `Roll Button`: 拖拽 Roll 按钮
   - `Submit Combo Button`: 拖拽 Cast/Submit 按钮
   - `Tutorial Prompt Panel`: 拖拽教程提示面板
   - `Tutorial Text`: 拖拽文本组件
   - `Tutorial Continue Button`: 拖拽 Next 按钮

### 步骤 3: 设置 TutorialController 的初始状态

1. **确保 TutorialController 默认禁用**
   - 在 Hierarchy 中选择 `TutorialController` GameObject
   - 在 Inspector 中取消勾选顶部的复选框（禁用 GameObject）
   - 这样只有在教程模式下才会激活

2. **或者添加自动激活逻辑**（推荐）
   - 在 `BattleController.Start()` 中添加：
   ```csharp
   // 在 Start() 方法的最后添加
   if (IsTutorialMode)
   {
       TutorialController tutorial = FindObjectOfType<TutorialController>();
       if (tutorial != null)
       {
           tutorial.gameObject.SetActive(true);
       }
   }
   ```

### 步骤 4: 更新 Build Settings（可选）

如果你想移除 TutorialScene：

1. **打开 Build Settings**
   - File → Build Settings

2. **移除 TutorialScene**
   - 在 Scenes In Build 列表中找到 `TutorialScene`
   - 点击 Remove（或取消勾选）

3. **确保 BattleScene 在列表中**
   - 确保 `BattleScene` 在 Scenes In Build 中
   - 确保它在正确的位置（通常是索引 1 或 2）

### 步骤 5: 测试

1. **测试教程流程**
   - 从 MainScene 点击 "Tutorial" 按钮
   - 应该加载 BattleScene 并显示教程
   - 完成教程后应该无缝进入 Level 1

2. **测试正常游戏流程**
   - 从 MainScene 点击 "Start Game" 按钮
   - 应该直接进入 Level 1，不显示教程

3. **检查 UI 显示**
   - 教程模式下：Level 显示应该为 "Tutorial"
   - Level 1+：Level 显示应该为 "Level 1", "Level 2" 等

## 注意事项

1. **TutorialController 的激活时机**
   - 确保 `TutorialController` 只在教程模式下激活
   - 可以通过代码控制，或手动在 Unity Editor 中设置

2. **场景引用**
   - 如果两个场景共享相同的 GameObject 名称，确保引用正确
   - 检查所有拖拽的引用是否指向正确的对象

3. **性能考虑**
   - TutorialController 在非教程模式下应该被禁用
   - 这样可以避免不必要的更新和事件监听

## 回退方案

如果遇到问题，可以快速回退：

1. **恢复 RunLoader.cs**
   ```csharp
   public void StartTutorial()
   {
       StartCoroutine(LoadSceneWithWipe(tutorialSceneName)); // 改回 TutorialScene
   }
   ```

2. **恢复 TutorialController.cs**
   - 恢复 `CompleteTutorial()` 中的场景切换逻辑

## 验证清单

- [ ] TutorialController 在 BattleScene 中存在
- [ ] 所有引用都已正确配置
- [ ] 教程模式下 Level 显示为 "Tutorial"
- [ ] 完成教程后无缝进入 Level 1
- [ ] 正常游戏流程不受影响
- [ ] 没有控制台错误或警告

