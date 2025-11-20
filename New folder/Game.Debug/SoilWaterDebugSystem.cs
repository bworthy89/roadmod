using Colossal;
using Game.Simulation;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

public class SoilWaterDebugSystem : GameSystemBase
{
	private struct SoilWaterGizmoJob : IJob
	{
		[ReadOnly]
		public NativeArray<SoilWater> m_SoilWaterMap;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute()
		{
			for (int i = 64; i < 128; i++)
			{
				for (int j = 64; j < 128; j++)
				{
					int index = i + j * 128;
					SoilWater soilWater = m_SoilWaterMap[index];
					if (soilWater.m_Max > 0)
					{
						float3 cellCenter = SoilWaterSystem.GetCellCenter(index);
						float3 center = cellCenter;
						cellCenter.y += (float)soilWater.m_Max / 400f;
						center.y += (float)soilWater.m_Amount / 400f;
						Color blue = Color.blue;
						m_GizmoBatcher.DrawWireCube(cellCenter, new float3(10f, (float)soilWater.m_Max / 200f, 10f), Color.grey);
						m_GizmoBatcher.DrawWireCube(center, new float3(10f, (float)soilWater.m_Amount / 200f, 10f), blue);
					}
				}
			}
		}
	}

	private SoilWaterSystem m_SoilWaterSystem;

	private GizmosSystem m_GizmosSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_SoilWaterSystem = base.World.GetOrCreateSystemManaged<SoilWaterSystem>();
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		SoilWaterGizmoJob jobData = new SoilWaterGizmoJob
		{
			m_SoilWaterMap = m_SoilWaterSystem.GetMap(readOnly: true, out dependencies),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies2)
		};
		base.Dependency = jobData.Schedule(JobHandle.CombineDependencies(base.Dependency, dependencies2, dependencies));
		m_SoilWaterSystem.AddReader(base.Dependency);
		m_GizmosSystem.AddGizmosBatcherWriter(base.Dependency);
	}

	[Preserve]
	public SoilWaterDebugSystem()
	{
	}
}
