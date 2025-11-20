using Colossal;
using Game.Simulation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

public class GroundWaterDebugSystem : GameSystemBase
{
	private struct GroundWaterGizmoJob : IJob
	{
		[ReadOnly]
		public NativeArray<GroundWater> m_GroundWaterMap;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute()
		{
			for (int i = 0; i < m_GroundWaterMap.Length; i++)
			{
				GroundWater groundWater = m_GroundWaterMap[i];
				if (groundWater.m_Max > 0)
				{
					float3 cellCenter = GroundWaterSystem.GetCellCenter(i);
					cellCenter.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, cellCenter);
					float3 center = cellCenter;
					float3 center2 = cellCenter;
					cellCenter.y += (float)groundWater.m_Max / 400f;
					center.y += (float)groundWater.m_Amount / 400f;
					center2.y += (float)groundWater.m_Polluted / 400f;
					float num = (float)groundWater.m_Polluted / (float)groundWater.m_Amount;
					Color color = ((!(num < 0.1f)) ? Color.Lerp(Color.yellow, Color.red, (num - 0.1f) / 0.9f) : Color.Lerp(Color.green, Color.yellow, 10f * num));
					m_GizmoBatcher.DrawWireCube(cellCenter, new float3(10f, (float)groundWater.m_Max / 200f, 10f), Color.grey);
					m_GizmoBatcher.DrawWireCube(center, new float3(10f, (float)groundWater.m_Amount / 200f, 10f), color);
					if (groundWater.m_Polluted > 0)
					{
						m_GizmoBatcher.DrawWireCube(center2, new float3(10f, (float)groundWater.m_Polluted / 200f, 10f), Color.red);
					}
				}
			}
		}
	}

	private GroundWaterSystem m_GroundWaterSystem;

	private GizmosSystem m_GizmosSystem;

	private TerrainSystem m_TerrainSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		GroundWaterGizmoJob jobData = new GroundWaterGizmoJob
		{
			m_GroundWaterMap = m_GroundWaterSystem.GetMap(readOnly: true, out dependencies),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies2)
		};
		base.Dependency = jobData.Schedule(JobHandle.CombineDependencies(base.Dependency, dependencies2, dependencies));
		m_GroundWaterSystem.AddReader(base.Dependency);
		m_TerrainSystem.AddCPUHeightReader(base.Dependency);
		m_GizmosSystem.AddGizmosBatcherWriter(base.Dependency);
	}

	[Preserve]
	public GroundWaterDebugSystem()
	{
	}
}
