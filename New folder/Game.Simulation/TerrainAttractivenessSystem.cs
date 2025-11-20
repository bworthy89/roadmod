using System.Runtime.CompilerServices;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class TerrainAttractivenessSystem : CellMapSystem<TerrainAttractiveness>, IJobSerializable
{
	[BurstCompile]
	private struct TerrainAttractivenessPrepareJob : IJobParallelForBatch
	{
		[ReadOnly]
		public TerrainHeightData m_TerrainData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterData;

		[ReadOnly]
		public CellMapData<ZoneAmbienceCell> m_ZoneAmbienceData;

		public NativeArray<float3> m_AttractFactorData;

		public void Execute(int startIndex, int count)
		{
			for (int i = startIndex; i < startIndex + count; i++)
			{
				float3 cellCenter = GetCellCenter(i);
				m_AttractFactorData[i] = new float3(WaterUtils.SampleDepth(ref m_WaterData, cellCenter), TerrainUtils.SampleHeight(ref m_TerrainData, cellCenter), ZoneAmbienceSystem.GetZoneAmbience(GroupAmbienceType.Forest, cellCenter, m_ZoneAmbienceData.m_Buffer, 1f));
			}
		}
	}

	[BurstCompile]
	private struct TerrainAttractivenessJob : IJobParallelForBatch
	{
		[ReadOnly]
		public NativeArray<float3> m_AttractFactorData;

		[ReadOnly]
		public float m_Scale;

		public NativeArray<TerrainAttractiveness> m_AttractivenessMap;

		public AttractivenessParameterData m_AttractivenessParameters;

		public void Execute(int startIndex, int count)
		{
			for (int i = startIndex; i < startIndex + count; i++)
			{
				float3 cellCenter = GetCellCenter(i);
				float2 @float = 0;
				int num = Mathf.CeilToInt(math.max(m_AttractivenessParameters.m_ForestDistance, m_AttractivenessParameters.m_ShoreDistance) / m_Scale);
				for (int j = -num; j <= num; j++)
				{
					for (int k = -num; k <= num; k++)
					{
						int num2 = math.min(kTextureSize - 1, math.max(0, i % kTextureSize + j));
						int num3 = math.min(kTextureSize - 1, math.max(0, i / kTextureSize + k));
						int index = num2 + num3 * kTextureSize;
						float3 float2 = m_AttractFactorData[index];
						float num4 = math.distance(GetCellCenter(index), cellCenter);
						@float.x = math.max(@float.x, math.saturate(1f - num4 / m_AttractivenessParameters.m_ForestDistance) * float2.z);
						@float.y = math.max(@float.y, math.saturate(1f - num4 / m_AttractivenessParameters.m_ShoreDistance) * ((float2.x > 2f) ? 1f : 0f));
					}
				}
				m_AttractivenessMap[i] = new TerrainAttractiveness
				{
					m_ForestBonus = @float.x,
					m_ShoreBonus = @float.y
				};
			}
		}
	}

	public static readonly int kTextureSize = 128;

	public static readonly int kUpdatesPerDay = 16;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private ZoneAmbienceSystem m_ZoneAmbienceSystem;

	private EntityQuery m_AttractivenessParameterGroup;

	private NativeArray<float3> m_AttractFactorData;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<TerrainAttractiveness>.GetCellCenter(index, kTextureSize);
	}

	public static float EvaluateAttractiveness(float terrainHeight, TerrainAttractiveness attractiveness, AttractivenessParameterData parameters)
	{
		float num = parameters.m_ForestEffect * attractiveness.m_ForestBonus;
		float num2 = parameters.m_ShoreEffect * attractiveness.m_ShoreBonus;
		float num3 = math.min(parameters.m_HeightBonus.z, math.max(0f, terrainHeight - parameters.m_HeightBonus.x) * parameters.m_HeightBonus.y);
		return num + num2 + num3;
	}

	public static float EvaluateAttractiveness(float3 position, CellMapData<TerrainAttractiveness> data, TerrainHeightData heightData, AttractivenessParameterData parameters, NativeArray<int> factors)
	{
		float num = TerrainUtils.SampleHeight(ref heightData, position);
		TerrainAttractiveness attractiveness = GetAttractiveness(position, data.m_Buffer);
		float num2 = parameters.m_ForestEffect * attractiveness.m_ForestBonus;
		AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Forest, num2);
		float num3 = parameters.m_ShoreEffect * attractiveness.m_ShoreBonus;
		AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Beach, num3);
		float num4 = math.min(parameters.m_HeightBonus.z, math.max(0f, num - parameters.m_HeightBonus.x) * parameters.m_HeightBonus.y);
		AttractionSystem.SetFactor(factors, AttractionSystem.AttractivenessFactor.Height, num4);
		return num2 + num3 + num4;
	}

	public static TerrainAttractiveness GetAttractiveness(float3 position, NativeArray<TerrainAttractiveness> attractivenessMap)
	{
		TerrainAttractiveness result = default(TerrainAttractiveness);
		int2 cell = CellMapSystem<TerrainAttractiveness>.GetCell(position, CellMapSystem<TerrainAttractiveness>.kMapSize, kTextureSize);
		float2 cellCoords = CellMapSystem<TerrainAttractiveness>.GetCellCoords(position, CellMapSystem<TerrainAttractiveness>.kMapSize, kTextureSize);
		if (cell.x < 0 || cell.x >= kTextureSize || cell.y < 0 || cell.y >= kTextureSize)
		{
			return result;
		}
		TerrainAttractiveness terrainAttractiveness = attractivenessMap[cell.x + kTextureSize * cell.y];
		TerrainAttractiveness terrainAttractiveness2 = ((cell.x < kTextureSize - 1) ? attractivenessMap[cell.x + 1 + kTextureSize * cell.y] : default(TerrainAttractiveness));
		TerrainAttractiveness terrainAttractiveness3 = ((cell.y < kTextureSize - 1) ? attractivenessMap[cell.x + kTextureSize * (cell.y + 1)] : default(TerrainAttractiveness));
		TerrainAttractiveness terrainAttractiveness4 = ((cell.x < kTextureSize - 1 && cell.y < kTextureSize - 1) ? attractivenessMap[cell.x + 1 + kTextureSize * (cell.y + 1)] : default(TerrainAttractiveness));
		result.m_ForestBonus = (short)Mathf.RoundToInt(math.lerp(math.lerp(terrainAttractiveness.m_ForestBonus, terrainAttractiveness2.m_ForestBonus, cellCoords.x - (float)cell.x), math.lerp(terrainAttractiveness3.m_ForestBonus, terrainAttractiveness4.m_ForestBonus, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
		result.m_ShoreBonus = (short)Mathf.RoundToInt(math.lerp(math.lerp(terrainAttractiveness.m_ShoreBonus, terrainAttractiveness2.m_ShoreBonus, cellCoords.x - (float)cell.x), math.lerp(terrainAttractiveness3.m_ShoreBonus, terrainAttractiveness4.m_ShoreBonus, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
		return result;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		CreateTextures(kTextureSize);
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_ZoneAmbienceSystem = base.World.GetOrCreateSystemManaged<ZoneAmbienceSystem>();
		m_AttractivenessParameterGroup = GetEntityQuery(ComponentType.ReadOnly<AttractivenessParameterData>());
		m_AttractFactorData = new NativeArray<float3>(m_Map.Length, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_AttractFactorData.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TerrainHeightData heightData = m_TerrainSystem.GetHeightData();
		JobHandle deps;
		JobHandle dependencies;
		TerrainAttractivenessPrepareJob jobData = new TerrainAttractivenessPrepareJob
		{
			m_AttractFactorData = m_AttractFactorData,
			m_TerrainData = heightData,
			m_WaterData = m_WaterSystem.GetSurfaceData(out deps),
			m_ZoneAmbienceData = m_ZoneAmbienceSystem.GetData(readOnly: true, out dependencies)
		};
		TerrainAttractivenessJob jobData2 = new TerrainAttractivenessJob
		{
			m_Scale = heightData.scale.x * (float)kTextureSize,
			m_AttractFactorData = m_AttractFactorData,
			m_AttractivenessMap = m_Map,
			m_AttractivenessParameters = m_AttractivenessParameterGroup.GetSingleton<AttractivenessParameterData>()
		};
		JobHandle jobHandle = jobData.ScheduleBatch(m_Map.Length, 4, JobHandle.CombineDependencies(deps, dependencies, base.Dependency));
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_ZoneAmbienceSystem.AddReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		base.Dependency = jobData2.ScheduleBatch(m_Map.Length, 4, JobHandle.CombineDependencies(m_WriteDependencies, m_ReadDependencies, jobHandle));
		AddWriter(base.Dependency);
		base.Dependency = JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies, base.Dependency);
	}

	[Preserve]
	public TerrainAttractivenessSystem()
	{
	}
}
