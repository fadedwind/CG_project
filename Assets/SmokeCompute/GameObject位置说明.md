# 场景中SmokeComputeManager GameObject的详细位置

## 在Smoke.unity场景中

### 1. **"Interactive" GameObject** ⭐ 推荐使用

**位置：**
- **Hierarchy路径**：直接在场景根目录（顶层）
- **GameObject名称**：`Interactive`
- **状态**：✅ 已激活（Active）
- **分辨率**：640x360
- **组件**：包含 `SmokeComputeManager` 组件

**如何找到：**
1. 打开场景：`Assets/Scenes/Smoke.unity`
2. 在Hierarchy窗口中，直接查看顶层（根目录）
3. 找到名为 **"Interactive"** 的GameObject
4. 选中它，在Inspector中可以看到 `SmokeComputeManager` 组件

**Transform信息：**
- Position: (0, 0, 0)
- Rotation: (0, 0, 0)
- Scale: (1, 1, 1)
- 父对象：无（根对象）

---

### 2. **"High Resolution" GameObject**

**位置：**
- **Hierarchy路径**：直接在场景根目录（顶层）
- **GameObject名称**：`High Resolution`
- **状态**：❌ 未激活（Inactive，需要先激活才能使用）
- **分辨率**：1280x720
- **组件**：包含 `SmokeComputeManager` 组件

**如何找到：**
1. 在Hierarchy窗口中，直接查看顶层（根目录）
2. 找到名为 **"High Resolution"** 的GameObject
3. 注意：它可能是灰色的（未激活状态）
4. 如果要使用它，需要先勾选激活（点击GameObject名称旁边的复选框）

**Transform信息：**
- Position: (0, 0, 0)
- Rotation: (0, 0, 0)
- Scale: (1, 1, 1)
- 父对象：无（根对象）

---

## 操作步骤

### 推荐：使用 "Interactive" GameObject

1. **打开场景**
   - 在Project窗口中，导航到：`Assets/Scenes/Smoke.unity`
   - 双击打开场景

2. **找到GameObject**
   - 在Hierarchy窗口（通常在左侧）
   - 查看顶层（没有缩进的对象）
   - 找到 **"Interactive"** 

3. **选中GameObject**
   - 点击 **"Interactive"** 选中它
   - 在Inspector窗口（通常在右侧）可以看到它的组件

4. **添加VideoFrameToSmoke组件**
   - 在Inspector中，点击 **"Add Component"** 按钮
   - 搜索：`VideoFrameToSmoke` 或 `FluidSim.VideoFrameToSmoke`
   - 或者使用菜单：`Tools` → `Add VideoFrameToSmoke Component`

5. **配置组件**
   - **Smoke Manager**: 拖入同一个GameObject上的 `SmokeComputeManager` 组件
   - 其他参数按之前的说明设置

---

## 如果找不到怎么办？

### 方法1：使用搜索功能
1. 在Hierarchy窗口的搜索框中输入：`Interactive`
2. 或者搜索：`SmokeComputeManager`

### 方法2：展开所有对象
1. 在Hierarchy窗口中，右键点击任意对象
2. 选择 "Expand All"（展开所有）
3. 然后查找 "Interactive"

### 方法3：使用Scene视图
1. 在Scene视图中，按 `F` 键聚焦到选中的对象
2. 在Hierarchy中逐个点击对象，看哪个在Scene中显示烟雾模拟

### 方法4：检查场景是否正确
- 确保打开的是 `Assets/Scenes/Smoke.unity` 场景
- 不是 `SmokeCPU.unity` 或其他场景

---

## 验证找到的对象

选中GameObject后，在Inspector中应该能看到：
- ✅ `Transform` 组件
- ✅ `SmokeComputeManager` 组件（这是关键！）
- 可能还有其他组件

如果看到 `SmokeComputeManager` 组件，说明找对了！







