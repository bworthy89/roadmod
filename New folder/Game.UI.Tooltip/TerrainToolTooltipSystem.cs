using System.Runtime.CompilerServices;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class TerrainToolTooltipSystem : TooltipSystemBase
{
	private ToolSystem m_ToolSystem;

	private TerrainToolSystem m_TerrainTool;

	private ToolRaycastSystem m_ToolRaycastSystem;

	private GroundWaterSystem m_GroundWaterSystem;

	private EntityQuery m_ParameterQuery;

	private IntTooltip m_GroundwaterVolume;

	private NativeReference<TempWaterPumpingTooltipSystem.GroundWaterReservoirResult> m_ReservoirResult;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_TerrainTool = base.World.GetOrCreateSystemManaged<TerrainToolSystem>();
		m_ToolRaycastSystem = base.World.GetOrCreateSystemManaged<ToolRaycastSystem>();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_ParameterQuery = GetEntityQuery(ComponentType.ReadOnly<WaterPipeParameterData>());
		RequireForUpdate(m_ParameterQuery);
		m_GroundwaterVolume = new IntTooltip
		{
			path = "groundWaterCapacity",
			icon = "Media/Game/Icons/Water.svg",
			label = LocalizedString.Id("Tools.GROUNDWATER_VOLUME"),
			unit = "volume"
		};
		m_ReservoirResult = new NativeReference<TempWaterPumpingTooltipSystem.GroundWaterReservoirResult>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_ReservoirResult.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_ToolSystem.activeTool == m_TerrainTool && m_TerrainTool.prefab != null && m_TerrainTool.prefab.m_Target == TerraformingTarget.GroundWater && m_ToolRaycastSystem.GetRaycastResult(out var result))
		{
			ProcessResults();
			m_ReservoirResult.Value = default(TempWaterPumpingTooltipSystem.GroundWaterReservoirResult);
			if (GroundWaterSystem.TryGetCell(result.m_Hit.m_HitPosition, out var cell))
			{
				JobHandle dependencies;
				NativeArray<GroundWater> map = m_GroundWaterSystem.GetMap(readOnly: true, out dependencies);
				NativeList<int2> tempGroundWaterPumpCells = new NativeList<int2>(1, Allocator.TempJob) { in cell };
				TempWaterPumpingTooltipSystem.GroundWaterReservoirJob jobData = new TempWaterPumpingTooltipSystem.GroundWaterReservoirJob
				{
					m_GroundWaterMap = map,
					m_PumpCapacityMap = new NativeParallelHashMap<int2, int>(0, Allocator.TempJob),
					m_TempGroundWaterPumpCells = tempGroundWaterPumpCells,
					m_Queue = new NativeQueue<int2>(Allocator.TempJob),
					m_Result = m_ReservoirResult
				};
				base.Dependency = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, dependencies));
				jobData.m_Queue.Dispose(base.Dependency);
				jobData.m_PumpCapacityMap.Dispose(base.Dependency);
				tempGroundWaterPumpCells.Dispose(base.Dependency);
				m_GroundWaterSystem.AddReader(base.Dependency);
			}
		}
		else
		{
			m_ReservoirResult.Value = default(TempWaterPumpingTooltipSystem.GroundWaterReservoirResult);
		}
	}

	private void ProcessResults()
	{
		TempWaterPumpingTooltipSystem.GroundWaterReservoirResult value = m_ReservoirResult.Value;
		if (value.m_Volume > 0)
		{
			WaterPipeParameterData singleton = m_ParameterQuery.GetSingleton<WaterPipeParameterData>();
			float f = singleton.m_GroundwaterReplenish / singleton.m_GroundwaterUsageMultiplier * (float)value.m_Volume;
			m_GroundwaterVolume.value = Mathf.RoundToInt(f);
			if (m_GroundwaterVolume.value > 0)
			{
				AddMouseTooltip(m_GroundwaterVolume);
			}
		}
	}

	[Preserve]
	public TerrainToolTooltipSystem()
	{
	}
}
