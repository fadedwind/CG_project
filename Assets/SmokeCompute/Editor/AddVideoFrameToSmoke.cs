#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using FluidSim;

namespace FluidSim.Editor
{
	/// <summary>
	/// 编辑器工具：快速添加VideoFrameToSmoke组件
	/// </summary>
	public class AddVideoFrameToSmoke : EditorWindow
	{
		[MenuItem("Tools/Add VideoFrameToSmoke Component")]
		static void AddComponent()
		{
			// 查找选中的GameObject
			GameObject selected = Selection.activeGameObject;
			
			if (selected == null)
			{
				EditorUtility.DisplayDialog("错误", "请先选择一个GameObject！", "确定");
				return;
			}
			
			// 检查是否已经有这个组件
			VideoFrameToSmoke existing = selected.GetComponent<VideoFrameToSmoke>();
			if (existing != null)
			{
				EditorUtility.DisplayDialog("提示", "该GameObject已经有VideoFrameToSmoke组件了！", "确定");
				return;
			}
			
			// 添加组件
			VideoFrameToSmoke component = selected.AddComponent<VideoFrameToSmoke>();
			
			// 尝试自动找到SmokeComputeManager
			SmokeComputeManager manager = FindObjectOfType<SmokeComputeManager>();
			if (manager != null)
			{
				component.smokeManager = manager;
				EditorUtility.DisplayDialog("成功", 
					$"已添加VideoFrameToSmoke组件到 {selected.name}，并自动设置了SmokeComputeManager引用！", 
					"确定");
			}
			else
			{
				EditorUtility.DisplayDialog("成功", 
					$"已添加VideoFrameToSmoke组件到 {selected.name}，但未找到SmokeComputeManager，请在Inspector中手动设置。", 
					"确定");
			}
			
			// 选中新添加的组件
			Selection.activeGameObject = selected;
		}
		
		[MenuItem("Tools/Add VideoFrameToSmoke Component", true)]
		static bool ValidateAddComponent()
		{
			return Selection.activeGameObject != null;
		}
	}
}
#endif



