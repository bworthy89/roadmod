using Colossal;
using Game.Simulation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Scripting;

namespace Game.Debug;

public class WaterCullingDebugSystem : BaseDebugSystem
{
	private GizmosSystem m_GizmosSystem;

	private WaterSystem m_WaterSystem;

	private TerrainSystem m_TerrainSystem;

	private Option m_ActiveWaterCellBackdropOption;

	private Option m_ActiveWaterCellOption;

	private Option m_FixedHeight;

	private Option m_UpdateCullingPosition;

	private Option m_OnlyShowCenterTile;

	private float3[] _patchesPos;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_UpdateCullingPosition = AddOption("Update culling position", defaultEnabled: true);
		m_OnlyShowCenterTile = AddOption("Only show center tile", defaultEnabled: false);
		m_ActiveWaterCellOption = AddOption("Show active water cell", defaultEnabled: false);
		m_ActiveWaterCellBackdropOption = AddOption("Show active water cell for back drop", defaultEnabled: false);
		m_FixedHeight = AddOption("Fixed Height", defaultEnabled: false);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		int num = 25;
		int num2 = 49;
		if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline hDRenderPipeline))
		{
			return;
		}
		JobHandle dependencies;
		GizmoBatcher gizmosBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies);
		dependencies.Complete();
		if (m_ActiveWaterCellOption.enabled)
		{
			float num3 = (float)m_WaterSystem.GridSize * m_WaterSystem.CellSize;
			float3 @float = m_TerrainSystem.positionOffset + new float3(num3 * 0.5f, 0f, num3 * 0.5f);
			NativeArray<int> active = m_WaterSystem.GetActive();
			int2 @int = m_WaterSystem.ActiveGridSize();
			float2 float2 = new float2(num3, num3);
			for (int i = 0; i < @int.x; i++)
			{
				for (int j = 0; j < @int.y; j++)
				{
					float2 float3 = new float2((float)i * num3, (float)j * num3);
					float3 center = @float + new float3(float3.x, 400f, float3.y);
					Color color = ((active[i + j * @int.x] > 0) ? Color.green : Color.red);
					gizmosBatcher.DrawWireRect(center, float2 * 0.49f, color);
				}
			}
			return;
		}
		if (m_ActiveWaterCellBackdropOption.enabled)
		{
			float num4 = (float)m_WaterSystem.BackdropGridCellSize * m_WaterSystem.BackdropCellSize;
			float3 float4 = m_TerrainSystem.positionOffset * 4f + new float3(num4 * 0.5f, 0f, num4 * 0.5f);
			int2 int2 = m_WaterSystem.ActiveBackdropGridSize();
			NativeArray<int> activeBackdrop = m_WaterSystem.GetActiveBackdrop();
			float2 float5 = new float2(num4, num4);
			for (int k = 0; k < int2.x; k++)
			{
				for (int l = 0; l < int2.y; l++)
				{
					float2 float6 = new float2((float)k * num4, (float)l * num4);
					float3 center2 = float4 + new float3(float6.x, 400f, float6.y);
					Color color2 = ((activeBackdrop[k + l * int2.x] > 0) ? Color.green : Color.red);
					gizmosBatcher.DrawWireRect(center2, float5 * 0.49f, color2);
				}
			}
			return;
		}
		if (m_UpdateCullingPosition.enabled)
		{
			_patchesPos = hDRenderPipeline.GetWaterPatchesPositions();
		}
		for (int m = 0; m < num; m++)
		{
			float3 center3 = _patchesPos[m];
			float3 float7 = _patchesPos[m + num2];
			if (!m_OnlyShowCenterTile.enabled || m == 12)
			{
				float2 size = new float2(float7.x, float7.y);
				Color color3 = ((float7.z > 0f) ? Color.blue : Color.red);
				if (m_FixedHeight.enabled)
				{
					center3.y = 400f;
				}
				gizmosBatcher.DrawWireNode(center3, 1f, color3);
				gizmosBatcher.DrawWireRect(center3, size, color3);
				gizmosBatcher.DrawWireCube(Matrix4x4.identity, center3, new float3(float7.x, float7.z, float7.y) * 2f, color3);
				if (float7.z > 0f)
				{
					gizmosBatcher.DrawWireCapsule(center3, 64f, float7.z, Color.blue);
				}
			}
		}
	}

	[Preserve]
	public WaterCullingDebugSystem()
	{
	}
}
