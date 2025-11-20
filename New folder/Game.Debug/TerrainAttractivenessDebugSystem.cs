using System.Runtime.CompilerServices;
using Colossal;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class TerrainAttractivenessDebugSystem : BaseDebugSystem
{
	private struct TerrainAttractivenessGizmoJob : IJob
	{
		[ReadOnly]
		public CellMapData<TerrainAttractiveness> m_Map;

		[ReadOnly]
		public TerrainHeightData m_HeightData;

		public AttractivenessParameterData m_Parameters;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute()
		{
			for (int i = 0; i < m_Map.m_Buffer.Length; i++)
			{
				float3 cellCenter = TerrainAttractivenessSystem.GetCellCenter(i);
				cellCenter.y = TerrainUtils.SampleHeight(ref m_HeightData, cellCenter);
				float num = TerrainAttractivenessSystem.EvaluateAttractiveness(cellCenter, m_Map, m_HeightData, m_Parameters, default(NativeArray<int>));
				if (num > 0f)
				{
					m_GizmoBatcher.DrawWireCube(cellCenter, new float3(10f, num, 10f), Color.white);
				}
			}
		}
	}

	private TerrainAttractivenessSystem m_TerrainAttractivenessSystem;

	private TerrainSystem m_TerrainSystem;

	private EntityQuery m_ParameterQuery;

	private GizmosSystem m_GizmosSystem;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_TerrainAttractivenessSystem = base.World.GetOrCreateSystemManaged<TerrainAttractivenessSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_ParameterQuery = GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle jobHandle = IJobExtensions.Schedule(new TerrainAttractivenessGizmoJob
		{
			m_Map = m_TerrainAttractivenessSystem.GetData(readOnly: true, out dependencies),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies2),
			m_HeightData = m_TerrainSystem.GetHeightData(),
			m_Parameters = m_ParameterQuery.GetSingleton<AttractivenessParameterData>()
		}, JobHandle.CombineDependencies(inputDeps, dependencies2, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		m_TerrainAttractivenessSystem.AddReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		return jobHandle;
	}

	[Preserve]
	public TerrainAttractivenessDebugSystem()
	{
	}
}
