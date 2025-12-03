using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace FluidSim
{
	/// <summary>
	/// 将视频帧序列转换为烟雾模拟
	/// 支持从文件夹加载图片帧，或使用Unity VideoPlayer
	/// </summary>
	public class VideoFrameToSmoke : MonoBehaviour
	{
		[Header("帧序列设置")]
		[Tooltip("帧图片所在的文件夹路径（相对于Assets或StreamingAssets）")]
		public string framesFolderPath = "StreamingAssets/Frames";
		
		[Tooltip("帧文件名格式（例如：frame_0000.png, frame_0001.png）")]
		public string frameNameFormat = "frame_{0:D4}.png";
		
		[Tooltip("起始帧编号")]
		public int startFrameIndex = 0;
		
		[Tooltip("总帧数（0表示自动检测）")]
		public int totalFrames = 0;
		
		[Header("播放设置")]
		[Tooltip("是否自动播放")]
		public bool autoPlay = true;
		
		[Tooltip("播放帧率（FPS）")]
		public float playbackFrameRate = 30f;
		
		[Tooltip("是否循环播放")]
		public bool loop = false;
		
		[Header("烟雾设置")]
		[Tooltip("亮度阈值：低于此值的像素将被视为障碍物，高于此值的为烟雾源")]
		[Range(0f, 1f)]
		public float brightnessThreshold = 0.5f;
		
		[Tooltip("烟雾强度倍数")]
		[Range(0f, 10f)]
		public float smokeIntensityMultiplier = 1f;
		
		[Tooltip("烟雾颜色（RGB值，例如：蓝色=(0,0,1)，白色=(1,1,1)）")]
		public Color smokeColor = new Color(0f, 0f, 1f, 1f); // 默认蓝色
		
		[Header("引用")]
		[Tooltip("烟雾模拟管理器")]
		public SmokeComputeManager smokeManager;
		
		[Header("场景设置")]
		[Tooltip("是否隐藏场景中原本的SceneObject（障碍物、发射器等）")]
		public bool hideOriginalSceneObjects = true;
		
		[Header("音频设置")]
		[Tooltip("背景音乐音频剪辑（例如：ba.mp3）")]
		public AudioClip backgroundMusic;
		
		[Tooltip("是否自动播放背景音乐")]
		public bool autoPlayMusic = true;
		
		[Tooltip("音频音量（0-1）")]
		[Range(0f, 1f)]
		public float musicVolume = 1f;
		
		// 内部状态
		private List<Texture2D> frameTextures = new List<Texture2D>();
		private int currentFrameIndex = 0;
		private float frameTimer = 0f;
		private bool isPlaying = false;
		private float startTime = 0f; // 开始播放的时间
		private RenderTexture inputFrameTexture; // 用于传递给Compute Shader的纹理
		
		// Compute Shader相关
		private ComputeShader computeShader;
		private int loadFrameKernel;
		
		// 音频相关
		private AudioSource audioSource;
		
		void Start()
		{
			// 获取Compute Shader引用
			if (smokeManager != null && smokeManager.compute != null)
			{
				computeShader = smokeManager.compute;
				loadFrameKernel = computeShader.FindKernel("LoadFrameFromTexture");
			}
			else
			{
				Debug.LogError("VideoFrameToSmoke: 需要设置SmokeComputeManager引用！");
				return;
			}
			
			// 加载帧序列
			LoadFrameSequence();
			
			// 创建输入纹理
			if (smokeManager != null)
			{
				CreateInputTexture();
			}
			
			// 隐藏原有的SceneObject
			if (hideOriginalSceneObjects)
			{
				HideOriginalSceneObjects();
			}
			
			// 设置音频
			SetupAudio();
			
			// 自动播放
			if (autoPlay && frameTextures.Count > 0)
			{
				isPlaying = true;
				startTime = Time.time;
				// 立即应用第一帧
				currentFrameIndex = 0;
				ApplyFrame(currentFrameIndex);
				
				// 同步启动音频（如果音频已加载）
				if (audioSource != null && backgroundMusic != null && autoPlayMusic)
				{
					audioSource.Play();
					startTime = Time.time; // 重新记录开始时间，确保音画同步
				}
			}
		}
		
		/// <summary>
		/// 设置音频播放
		/// </summary>
		void SetupAudio()
		{
			// 如果没有指定音频剪辑，尝试从Resources加载
			if (backgroundMusic == null)
			{
				backgroundMusic = Resources.Load<AudioClip>("ba");
				if (backgroundMusic == null)
				{
					// 尝试从StreamingAssets加载
					#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
					string audioPath = System.IO.Path.Combine(Application.streamingAssetsPath, "ba.mp3");
					if (System.IO.File.Exists(audioPath))
					{
						StartCoroutine(LoadAudioFromFile(audioPath));
						return;
					}
					#endif
					Debug.LogWarning("VideoFrameToSmoke: 未找到背景音乐文件 ba.mp3");
					return;
				}
			}
			
			// 创建AudioSource组件
			audioSource = gameObject.GetComponent<AudioSource>();
			if (audioSource == null)
			{
				audioSource = gameObject.AddComponent<AudioSource>();
			}
			
			// 配置AudioSource
			audioSource.clip = backgroundMusic;
			audioSource.volume = musicVolume;
			audioSource.loop = loop; // 与视频循环同步
			audioSource.playOnAwake = false;
			
			// 自动播放
			if (autoPlayMusic && backgroundMusic != null)
			{
				audioSource.Play();
			}
		}
		
		#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
		/// <summary>
		/// 从文件加载音频（仅支持编辑器或独立平台）
		/// </summary>
		System.Collections.IEnumerator LoadAudioFromFile(string filePath)
		{
			string url = "file://" + filePath;
			using (UnityEngine.WWW www = new UnityEngine.WWW(url))
			{
				yield return www;
				
				if (www.error != null)
				{
					Debug.LogError($"加载音频失败: {www.error}");
					yield break;
				}
				
				backgroundMusic = www.GetAudioClip(false, false);
				
				// 创建AudioSource组件
				audioSource = gameObject.GetComponent<AudioSource>();
				if (audioSource == null)
				{
					audioSource = gameObject.AddComponent<AudioSource>();
				}
				
				// 配置AudioSource
				audioSource.clip = backgroundMusic;
				audioSource.volume = musicVolume;
				audioSource.loop = loop;
				audioSource.playOnAwake = false;
				
				// 自动播放
				if (autoPlayMusic && backgroundMusic != null)
				{
					audioSource.Play();
				}
			}
		}
		#endif
		
		/// <summary>
		/// 隐藏场景中原本的SceneObject
		/// </summary>
		void HideOriginalSceneObjects()
		{
			SceneObject[] sceneObjects = FindObjectsByType<SceneObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			foreach (SceneObject obj in sceneObjects)
			{
				// 禁用GameObject（这样既不会显示，也不会参与模拟）
				obj.gameObject.SetActive(false);
			}
			Debug.Log($"隐藏了 {sceneObjects.Length} 个原有的SceneObject");
		}
		
		void Update()
		{
			if (!isPlaying || frameTextures.Count == 0) return;
			
			// 使用更精确的时间计算，避免累积误差
			// 基于音频时间同步（如果音频正在播放）
			float targetTime = 0f;
			if (audioSource != null && audioSource.isPlaying && backgroundMusic != null)
			{
				// 使用音频时间作为主时钟
				targetTime = audioSource.time;
			}
			else
			{
				// 如果没有音频，使用游戏时间
				targetTime = Time.time - startTime;
			}
			
			// 计算当前应该显示的帧索引
			int targetFrameIndex = Mathf.FloorToInt(targetTime * playbackFrameRate);
			
			// 如果帧索引发生变化，更新帧
			if (targetFrameIndex != currentFrameIndex)
			{
				// 处理循环
				if (targetFrameIndex >= frameTextures.Count)
				{
					if (loop)
					{
						targetFrameIndex = targetFrameIndex % frameTextures.Count;
					}
					else
					{
						targetFrameIndex = frameTextures.Count - 1;
						isPlaying = false;
						return;
					}
				}
				
				currentFrameIndex = targetFrameIndex;
				ApplyFrame(currentFrameIndex);
			}
		}
		
		/// <summary>
		/// 加载帧序列
		/// </summary>
		void LoadFrameSequence()
		{
			frameTextures.Clear();
			
			// 尝试从StreamingAssets加载
			string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, framesFolderPath);
			if (Directory.Exists(streamingAssetsPath))
			{
				LoadFramesFromFolder(streamingAssetsPath);
				return;
			}
			
			// 尝试从Resources加载
			string resourcesPath = framesFolderPath.Replace("Resources/", "").Replace("Resources\\", "");
			if (resourcesPath != framesFolderPath)
			{
				// 如果路径包含Resources，使用Resources.LoadAll
				Object[] textures = Resources.LoadAll<Texture2D>(resourcesPath);
				foreach (Texture2D tex in textures)
				{
					frameTextures.Add(tex);
				}
				if (frameTextures.Count > 0)
				{
					Debug.Log($"从Resources加载了 {frameTextures.Count} 帧");
					return;
				}
			}
			
			// 尝试从Assets路径加载（仅编辑器模式）
			#if UNITY_EDITOR
			string assetsPath = Path.Combine(Application.dataPath, framesFolderPath);
			if (Directory.Exists(assetsPath))
			{
				LoadFramesFromFolder(assetsPath);
				return;
			}
			#endif
			
			Debug.LogWarning($"VideoFrameToSmoke: 无法找到帧文件夹: {framesFolderPath}");
		}
		
		/// <summary>
		/// 从文件夹加载帧
		/// </summary>
		void LoadFramesFromFolder(string folderPath)
		{
			// 如果totalFrames为0，尝试自动检测
			if (totalFrames == 0)
			{
				// 查找所有图片文件
				string[] imageExtensions = { "*.png", "*.jpg", "*.jpeg" };
				List<string> imageFiles = new List<string>();
				
				foreach (string ext in imageExtensions)
				{
					imageFiles.AddRange(Directory.GetFiles(folderPath, ext));
				}
				
				totalFrames = imageFiles.Count;
				Debug.Log($"自动检测到 {totalFrames} 帧");
			}
			
			// 加载帧
			for (int i = startFrameIndex; i < startFrameIndex + totalFrames; i++)
			{
				string frameName = string.Format(frameNameFormat, i);
				string framePath = Path.Combine(folderPath, frameName);
				
				if (File.Exists(framePath))
				{
					// 加载图片
					byte[] fileData = File.ReadAllBytes(framePath);
					Texture2D tex = new Texture2D(2, 2);
					
					if (tex.LoadImage(fileData))
					{
						// 确保纹理可读
						tex.filterMode = FilterMode.Bilinear;
						tex.wrapMode = TextureWrapMode.Clamp;
						frameTextures.Add(tex);
					}
					else
					{
						Debug.LogWarning($"无法加载帧: {framePath}");
					}
				}
			}
			
			Debug.Log($"成功加载 {frameTextures.Count} 帧");
		}
		
		/// <summary>
		/// 创建输入纹理（用于传递给Compute Shader）
		/// </summary>
		void CreateInputTexture()
		{
			if (smokeManager == null) return;
			
			Vector2Int res = smokeManager.resolution;
			inputFrameTexture = new RenderTexture(res.x, res.y, 0, GraphicsFormat.R8G8B8A8_UNorm);
			inputFrameTexture.enableRandomWrite = false;
			inputFrameTexture.filterMode = FilterMode.Bilinear;
			inputFrameTexture.wrapMode = TextureWrapMode.Clamp;
			inputFrameTexture.Create();
		}
		
		/// <summary>
		/// 更新到下一帧
		/// </summary>
		void UpdateToNextFrame()
		{
			if (frameTextures.Count == 0) return;
			
			// 更新当前帧索引
			currentFrameIndex++;
			if (currentFrameIndex >= frameTextures.Count)
			{
				if (loop)
				{
					currentFrameIndex = 0;
				}
				else
				{
					currentFrameIndex = frameTextures.Count - 1;
					isPlaying = false;
					return;
				}
			}
			
			// 应用当前帧
			ApplyFrame(currentFrameIndex);
		}
		
		/// <summary>
		/// 应用指定帧到烟雾模拟
		/// </summary>
		void ApplyFrame(int frameIndex)
		{
			if (frameIndex < 0 || frameIndex >= frameTextures.Count) return;
			if (smokeManager == null || computeShader == null) return;
			
			// 确保SmokeComputeManager已经初始化并绑定了所有纹理
			// 通过触发一次RunCompute来确保Bind()被调用
			// 但我们需要在调用kernel之前手动绑定纹理到LoadFrameFromTexture kernel
			
			Texture2D sourceFrame = frameTextures[frameIndex];
			
			// 将Texture2D复制到RenderTexture（如果需要缩放）
			Graphics.Blit(sourceFrame, inputFrameTexture);
			
			// 确保分辨率参数已设置（从SmokeComputeManager获取）
			Vector2Int res = smokeManager.resolution;
			
			// 获取所有必要的纹理（通过反射或公共方法）
			// 由于纹理是私有的，我们需要先确保SmokeComputeManager已经初始化
			// 最简单的方法：确保SmokeComputeManager已经运行过一次Bind()
			// 然后我们手动绑定纹理到LoadFrameFromTexture kernel
			
			// 手动绑定所有必要的纹理到LoadFrameFromTexture kernel
			// 注意：这些纹理应该在SmokeComputeManager的Bind()中已经创建
			// 我们需要通过反射或者让SmokeComputeManager提供公共方法来获取这些纹理
			// 但更简单的方法是：使用ComputeHelper.SetTexture，它会自动绑定到指定kernel
			
			// 由于无法直接访问私有纹理，我们需要一个变通方法：
			// 在SmokeComputeManager中添加一个公共方法来绑定纹理到指定kernel
			// 或者，我们可以使用反射（不推荐）
			// 或者，最简单：确保在调用LoadFrameFromTexture之前，SmokeComputeManager已经运行过
			// 然后使用ComputeHelper来绑定纹理
			
			// 临时解决方案：使用反射获取纹理（仅用于调试）
			// 更好的方案：修改SmokeComputeManager添加公共方法
			
			// 设置Compute Shader参数
			computeShader.SetTexture(loadFrameKernel, "InputFrameTexture", inputFrameTexture);
			computeShader.SetFloat("brightnessThreshold", brightnessThreshold);
			computeShader.SetFloat("smokeIntensityMultiplier", smokeIntensityMultiplier);
			computeShader.SetVector("smokeColor", new Vector4(smokeColor.r, smokeColor.g, smokeColor.b, 1f));
			computeShader.SetInts("resolution", res.x, res.y);
			computeShader.SetFloat("ambientTemperature", smokeManager.ambientTemperature);
			
			// 使用反射获取SmokeComputeManager的私有纹理并绑定
			var smokeManagerType = typeof(SmokeComputeManager);
			var obstacleMapField = smokeManagerType.GetField("obstacleMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var smokeMapField = smokeManagerType.GetField("smokeMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			
			if (obstacleMapField != null && smokeMapField != null)
			{
				var obstacleMap = obstacleMapField.GetValue(smokeManager) as RenderTexture;
				var smokeMap = smokeMapField.GetValue(smokeManager) as RenderTexture;
				
				if (obstacleMap != null)
				{
					computeShader.SetTexture(loadFrameKernel, "ObstacleMap", obstacleMap);
				}
				if (smokeMap != null)
				{
					computeShader.SetTexture(loadFrameKernel, "SmokeMap", smokeMap);
				}
			}
			
			// 执行kernel
			computeShader.Dispatch(loadFrameKernel, 
				Mathf.CeilToInt(res.x / 8f), 
				Mathf.CeilToInt(res.y / 8f), 
				1);
			
			// 执行后需要更新障碍物边缘（因为障碍物改变了）
			// 直接调用UpdateObstacleEdges kernel
			int updateObstacleEdgesKernel = computeShader.FindKernel("UpdateObstacleEdges");
			computeShader.Dispatch(updateObstacleEdgesKernel, 
				Mathf.CeilToInt(res.x / 8f), 
				Mathf.CeilToInt(res.y / 8f), 
				1);
		}
		
		/// <summary>
		/// 播放/暂停
		/// </summary>
		public void SetPlaying(bool playing)
		{
			isPlaying = playing;
		}
		
		/// <summary>
		/// 跳转到指定帧
		/// </summary>
		public void GoToFrame(int frameIndex)
		{
			currentFrameIndex = Mathf.Clamp(frameIndex, 0, frameTextures.Count - 1);
			ApplyFrame(currentFrameIndex);
		}
		
		/// <summary>
		/// 重置到第一帧
		/// </summary>
		public void ResetToFirstFrame()
		{
			currentFrameIndex = 0;
			ApplyFrame(currentFrameIndex);
		}
		
		void OnDestroy()
		{
			// 清理纹理
			foreach (Texture2D tex in frameTextures)
			{
				if (tex != null)
				{
					Destroy(tex);
				}
			}
			frameTextures.Clear();
			
			if (inputFrameTexture != null)
			{
				inputFrameTexture.Release();
				Destroy(inputFrameTexture);
			}
		}
		
		// 编辑器辅助方法
		#if UNITY_EDITOR
		[ContextMenu("重新加载帧序列")]
		void ReloadFrames()
		{
			LoadFrameSequence();
		}
		
		[ContextMenu("应用当前帧")]
		void ApplyCurrentFrame()
		{
			if (frameTextures.Count > 0)
			{
				ApplyFrame(currentFrameIndex);
			}
		}
		#endif
	}
}

