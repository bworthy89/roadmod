using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
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
public class BuildableAreaDebugSystem : BaseDebugSystem
{
	private struct BuildableAreaGizmoJob : IJobParallelFor
	{
		[ReadOnly]
		public CellMapData<NaturalResourceCell> m_NaturalResourceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public GizmoBatcher m_GizmoBatcher;

		public NativeAccumulator<AverageFloat>.ParallelWriter m_Average;

		public Bounds1 m_BuildableLandMaxSlope;

		public void Execute(int index)
		{
			float3 cellCenter = CellMapSystem<NaturalResourceCell>.GetCellCenter(index, NaturalResourceSystem.kTextureSize);
			float num = AreaResourceSystem.CalculateBuildable(cellCenter, m_NaturalResourceData.m_CellSize, m_WaterSurfaceData, m_TerrainHeightData, m_BuildableLandMaxSlope);
			m_Average.Accumulate(new AverageFloat
			{
				m_Total = num,
				m_Count = 1
			});
			if (num > 0f)
			{
				Color color = Color.Lerp(Color.red, Color.green, num);
				float2 @float = 0.5f * math.sqrt(num) * m_NaturalResourceData.m_CellSize;
				DrawLine(cellCenter + new float3(0f - @float.x, 0f, 0f - @float.y), cellCenter + new float3(@float.x, 0f, 0f - @float.y), color);
				DrawLine(cellCenter + new float3(@float.x, 0f, 0f - @float.y), cellCenter + new float3(@float.x, 0f, @float.y), color);
				DrawLine(cellCenter + new float3(@float.x, 0f, @float.y), cellCenter + new float3(0f - @float.x, 0f, @float.y), color);
				DrawLine(cellCenter + new float3(0f - @float.x, 0f, @float.y), cellCenter + new float3(0f - @float.x, 0f, 0f - @float.y), color);
			}
		}

		private void DrawLine(float3 a, float3 b, Color color)
		{
			a.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, a);
			b.y = TerrainUtils.SampleHeight(ref m_TerrainHeightData, b);
			m_GizmoBatcher.DrawLine(a, b, color);
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct TypeHandle
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
		}
	}

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private NaturalResourceSystem m_NaturalResourceSystem;

	private GizmosSystem m_GizmosSystem;

	private Option m_StrictOption;

	private NativeAccumulator<AverageFloat> m_BuildableArea;

	private float m_LastBuildableArea;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1438325908_0;

	public float buildableArea => m_LastBuildableArea;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
		RequireForUpdate<AreasConfigurationData>();
		m_BuildableArea = new NativeAccumulator<AverageFloat>(Allocator.Persistent);
		m_StrictOption = AddOption("Strict", defaultEnabled: false);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_BuildableArea.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		m_LastBuildableArea = m_BuildableArea.GetResult().average;
		m_BuildableArea.Clear();
		JobHandle dependencies;
		JobHandle dependencies2;
		JobHandle deps;
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(new BuildableAreaGizmoJob
		{
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies),
			m_NaturalResourceData = m_NaturalResourceSystem.GetData(readOnly: true, out dependencies2),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_BuildableLandMaxSlope = (m_StrictOption.enabled ? new Bounds1(0f, 0.3f) : __query_1438325908_0.GetSingleton<AreasConfigurationData>().m_BuildableLandMaxSlope),
			m_Average = m_BuildableArea.AsParallelWriter()
		}, NaturalResourceSystem.kTextureSize * NaturalResourceSystem.kTextureSize, NaturalResourceSystem.kTextureSize, JobUtils.CombineDependencies(inputDeps, dependencies, dependencies2, deps));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		m_NaturalResourceSystem.AddReader(jobHandle);
		return jobHandle;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<AreasConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1438325908_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public BuildableAreaDebugSystem()
	{
	}
}
