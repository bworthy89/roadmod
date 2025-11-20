using Colossal;
using Game.Simulation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

public class WindDebugSystem : BaseDebugSystem
{
	private struct WindGizmoJob : IJob
	{
		[ReadOnly]
		public NativeArray<WindSimulationSystem.WindCell> m_WindMap;

		public GizmoBatcher m_GizmoBatcher;

		public float2 m_TerrainRange;

		public void Execute()
		{
			for (int i = 0; i < WindSimulationSystem.kResolution.x; i++)
			{
				for (int j = 0; j < WindSimulationSystem.kResolution.y; j++)
				{
					for (int k = 0; k < WindSimulationSystem.kResolution.z; k++)
					{
						if (i == 0 || j == 0 || k == 0 || i == WindSimulationSystem.kResolution.x - 1 || j == WindSimulationSystem.kResolution.y - 1)
						{
							int index = i + j * WindSimulationSystem.kResolution.x + k * WindSimulationSystem.kResolution.x * WindSimulationSystem.kResolution.y;
							WindSimulationSystem.WindCell windCell = m_WindMap[index];
							float3 cellCenter = WindSimulationSystem.GetCellCenter(index);
							cellCenter.y = math.lerp(m_TerrainRange.x, m_TerrainRange.y, ((float)k + 0.5f) / (float)WindSimulationSystem.kResolution.z);
							Color white = Color.white;
							if (math.abs(windCell.m_Velocities.x) > 0.001f)
							{
								float3 @float = cellCenter + new float3(0.5f * (float)CellMapSystem<Wind>.kMapSize / (float)WindSimulationSystem.kResolution.x, 0f, 0f);
								m_GizmoBatcher.DrawArrow(@float, @float + 50f * new float3(windCell.m_Velocities.x, 0f, 0f), white, 1f);
							}
							if (math.abs(windCell.m_Velocities.y) > 0.001f)
							{
								float3 @float = cellCenter + new float3(0f, 0f, 0.5f * (float)CellMapSystem<Wind>.kMapSize / (float)WindSimulationSystem.kResolution.y);
								m_GizmoBatcher.DrawArrow(@float, @float + 50f * new float3(0f, 0f, windCell.m_Velocities.y), white, 1f);
							}
							if (math.abs(windCell.m_Velocities.z) > 0.001f)
							{
								float3 @float = cellCenter + new float3(0f, 0.5f * (float)CellMapSystem<Wind>.kMapSize / (float)WindSimulationSystem.kResolution.x, 0f);
								m_GizmoBatcher.DrawArrow(@float, @float + 50f * new float3(0f, windCell.m_Velocities.z, 0f), white, 1f);
							}
							white = ((!(windCell.m_Pressure < 0f)) ? new Color(0f, math.lerp(0f, 1f, math.saturate(10f * windCell.m_Pressure)), 0f) : new Color(math.lerp(0f, 1f, math.saturate(-10f * windCell.m_Pressure)), 0f, 0f));
							m_GizmoBatcher.DrawWireCube(cellCenter, new float3(10f, 10f * windCell.m_Pressure, 10f), white);
						}
					}
				}
			}
		}
	}

	private WindSimulationSystem m_WindSimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private GizmosSystem m_GizmosSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_WindSimulationSystem = base.World.GetOrCreateSystemManaged<WindSimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		TerrainHeightData data = m_TerrainSystem.GetHeightData();
		float2 terrainRange = new float2(TerrainUtils.ToWorldSpace(ref data, 0f), TerrainUtils.ToWorldSpace(ref data, 65535f));
		JobHandle deps;
		JobHandle dependencies;
		JobHandle jobHandle = new WindGizmoJob
		{
			m_WindMap = m_WindSimulationSystem.GetCells(out deps),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies),
			m_TerrainRange = terrainRange
		}.Schedule(JobHandle.CombineDependencies(inputDeps, dependencies, deps));
		m_WindSimulationSystem.AddReader(jobHandle);
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
	}

	[Preserve]
	public WindDebugSystem()
	{
	}
}
