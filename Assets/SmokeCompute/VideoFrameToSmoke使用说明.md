# VideoFrameToSmoke 使用说明

这个脚本可以将视频帧序列转换为烟雾模拟动画。

## 快速开始

### 1. 准备帧序列

将你的视频帧图片放在以下位置之一：

- **推荐**：`Assets/StreamingAssets/Frames/` 文件夹
- 或者：`Assets/Resources/Frames/` 文件夹（需要修改脚本中的路径）

帧文件命名格式：
- `frame_0000.png`, `frame_0001.png`, `frame_0002.png` ... （默认格式）
- 或者自定义格式（在Inspector中修改 `frameNameFormat`）

### 2. 在Unity中设置

1. 在场景中找到或创建 `SmokeComputeManager` 对象
2. 创建一个新的GameObject，添加 `VideoFrameToSmoke` 组件
3. 在Inspector中设置：
   - **Smoke Manager**: 拖入场景中的 `SmokeComputeManager` 对象
   - **Frames Folder Path**: 帧文件夹路径（例如：`Frames` 或 `StreamingAssets/Frames`）
   - **Frame Name Format**: 帧文件名格式（例如：`frame_{0:D4}.png`）
   - **Start Frame Index**: 起始帧编号（通常是0）
   - **Total Frames**: 总帧数（0表示自动检测）

### 3. 调整参数

#### 播放设置
- **Auto Play**: 是否自动播放
- **Playback Frame Rate**: 播放帧率（FPS），建议与原始视频帧率一致
- **Loop**: 是否循环播放

#### 烟雾设置
- **Brightness Threshold**: 亮度阈值（0-1）
  - 低于此值的像素 → 障碍物（黑色区域）
  - 高于此值的像素 → 烟雾源（白色/亮色区域）
  - 对于Bad Apple这样的黑白视频，建议设置为 `0.5`
  
- **Smoke Intensity Multiplier**: 烟雾强度倍数
  - 值越大，烟雾越浓
  - 建议从 `1.0` 开始调整

### 4. 运行

点击Play按钮，脚本会自动：
1. 加载帧序列
2. 将每一帧转换为障碍物和烟雾源
3. 按照设定的帧率播放

## 工作原理

1. **加载帧序列**：从指定文件夹加载所有帧图片
2. **转换为纹理**：将每一帧转换为RenderTexture
3. **Compute Shader处理**：使用GPU计算，根据像素亮度：
   - 暗色像素 → 设置为障碍物（阻挡烟雾）
   - 亮色像素 → 设置为烟雾源（产生烟雾）
4. **物理模拟**：烟雾会根据流体力学进行扩散、上升等效果

## 注意事项

1. **分辨率匹配**：
   - 确保帧图片的分辨率与 `SmokeComputeManager` 的 `Resolution` 匹配
   - 如果不匹配，会自动缩放，但可能影响效果

2. **帧格式**：
   - 支持 PNG、JPG 格式
   - 建议使用PNG以获得更好的质量

3. **性能**：
   - 帧序列会占用内存，如果帧数很多，建议分批处理
   - 可以在运行时通过代码控制播放/暂停

## 代码示例

```csharp
// 获取VideoFrameToSmoke组件
VideoFrameToSmoke videoToSmoke = GetComponent<VideoFrameToSmoke>();

// 暂停/播放
videoToSmoke.SetPlaying(false); // 暂停
videoToSmoke.SetPlaying(true);  // 播放

// 跳转到指定帧
videoToSmoke.GoToFrame(100); // 跳转到第100帧

// 重置到第一帧
videoToSmoke.ResetToFirstFrame();
```

## 常见问题

**Q: 帧加载失败？**
A: 检查文件夹路径是否正确，确保帧文件存在且命名格式正确。

**Q: 烟雾效果不明显？**
A: 尝试增加 `Smoke Intensity Multiplier` 值，或调整 `Brightness Threshold`。

**Q: 播放速度不对？**
A: 调整 `Playback Frame Rate` 参数，使其与原始视频帧率一致。

**Q: 如何导出渲染结果？**
A: 可以使用Unity的Recorder包或手动截图功能。



