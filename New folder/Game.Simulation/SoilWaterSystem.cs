using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Common;
using Game.Events;
using Game.Prefabs;
using Game.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class SoilWaterSystem : CellMapSystem<SoilWater>, IJobSerializable, IPostDeserialize
{
	[BurstCompile]
	private struct SoilWaterTickJob : IJob
	{
		public NativeArray<SoilWater> m_SoilWaterMap;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public NativeArray<float> m_SoilWaterTextureData;

		public SoilWaterParameterData m_SoilWaterParameters;

		public ComponentLookup<WaterLevelChange> m_Changes;

		public ComponentLookup<FloodCounterData> m_FloodCounterDatas;

		[ReadOnly]
		public ComponentLookup<EventData> m_Events;

		[ReadOnly]
		public NativeList<Entity> m_FloodEntities;

		[ReadOnly]
		public NativeList<Entity> m_FloodPrefabEntities;

		public EntityCommandBuffer m_CommandBuffer;

		public Entity m_FloodCounterEntity;

		public float m_Weather;

		public int m_ShaderUpdatesPerSoilUpdate;

		public int m_LoadDistributionIndex;

		private void HandleInterface(int index, int otherIndex, NativeArray<int> tmp, ref SoilWaterParameterData soilWaterParameters)
		{
			SoilWater soilWater = m_SoilWaterMap[index];
			SoilWater soilWater2 = m_SoilWaterMap[otherIndex];
			int num = tmp[index];
			int num2 = tmp[otherIndex];
			float num3 = soilWater2.m_Surface - soilWater.m_Surface;
			float num4 = (float)soilWater2.m_Amount / (float)soilWater2.m_Max - (float)soilWater.m_Amount / (float)soilWater.m_Max;
			float num5 = soilWaterParameters.m_HeightEffect * num3 / (float)(CellMapSystem<SoilWater>.kMapSize / kTextureSize) + 0.25f * num4;
			num5 = ((!(num5 >= 0f)) ? math.max(0f - soilWaterParameters.m_MaxDiffusion, num5) : math.min(soilWaterParameters.m_MaxDiffusion, num5));
			int num6 = Mathf.RoundToInt(num5 * (float)((num5 > 0f) ? soilWater2.m_Amount : soilWater.m_Amount));
			num += num6;
			num2 -= num6;
			tmp[index] = num;
			tmp[otherIndex] = num2;
		}

		private void StartFlood()
		{
			if (m_FloodPrefabEntities.Length > 0)
			{
				EntityArchetype archetype = m_Events[m_FloodPrefabEntities[0]].m_Archetype;
				Entity e = m_CommandBuffer.CreateEntity(archetype);
				m_CommandBuffer.SetComponent(e, new PrefabRef
				{
					m_Prefab = m_FloodPrefabEntities[0]
				});
				m_CommandBuffer.SetComponent(e, new WaterLevelChange
				{
					m_DangerHeight = 0f,
					m_Direction = new float2(0f, 0f),
					m_Intensity = 0f,
					m_MaxIntensity = 0f
				});
			}
		}

		private void StopFlood()
		{
			m_CommandBuffer.AddComponent<Deleted>(m_FloodEntities[0]);
		}

		public void Execute()
		{
			NativeArray<int> tmp = new NativeArray<int>(m_SoilWaterMap.Length, Allocator.Temp);
			for (int i = 0; i < m_SoilWaterMap.Length; i++)
			{
				int num = i % kTextureSize;
				int num2 = i / kTextureSize;
				if (num < kTextureSize - 1)
				{
					HandleInterface(i, i + 1, tmp, ref m_SoilWaterParameters);
				}
				if (num2 < kTextureSize - 1)
				{
					HandleInterface(i, i + kTextureSize, tmp, ref m_SoilWaterParameters);
				}
			}
			float num3 = math.max(0f, math.pow(2f * math.max(0f, m_Weather - 0.5f), 2f));
			float num4 = 1f / (2f * m_SoilWaterParameters.m_MaximumWaterDepth);
			int2 @int = m_WaterSurfaceData.resolution.xz / kTextureSize;
			FloodCounterData value = m_FloodCounterDatas[m_FloodCounterEntity];
			value.m_FloodCounter = math.max(0f, 0.98f * value.m_FloodCounter + 2f * num3 - 0.1f);
			if (value.m_FloodCounter > 20f && m_FloodEntities.Length == 0)
			{
				StartFlood();
			}
			else if (m_FloodEntities.Length > 0)
			{
				if (value.m_FloodCounter == 0f)
				{
					StopFlood();
				}
				else
				{
					WaterLevelChange value2 = m_Changes[m_FloodEntities[0]];
					value2.m_Intensity = math.max(0f, (value.m_FloodCounter - 20f) / 80f);
					m_Changes[m_FloodEntities[0]] = value2;
				}
			}
			m_FloodCounterDatas[m_FloodCounterEntity] = value;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			int num8 = m_LoadDistributionIndex * kTextureSize / kLoadDistribution;
			int num9 = num8 + kTextureSize / kLoadDistribution;
			for (int j = num8 * kTextureSize; j < num9 * kTextureSize; j++)
			{
				SoilWater value3 = m_SoilWaterMap[j];
				value3.m_Amount = (short)math.max(0, value3.m_Amount + tmp[j] + Mathf.RoundToInt(m_SoilWaterParameters.m_RainMultiplier * num3));
				float surface = TerrainUtils.SampleHeight(ref m_TerrainHeightData, GetCellCenter(j));
				value3.m_Surface = surface;
				short num10 = (short)Mathf.RoundToInt(math.max(0f, 0.1f * (0.5f * (float)value3.m_Max - (float)value3.m_Amount)));
				float x = (float)num10 * m_SoilWaterParameters.m_WaterPerUnit / (float)value3.m_Max;
				int num11 = 0;
				int num12 = 0;
				float num13 = 0f;
				float num14 = 0f;
				int num15 = j % kTextureSize * @int.x + j / kTextureSize * m_WaterSurfaceData.resolution.x * @int.y;
				for (int k = 0; k < @int.x; k += 4)
				{
					for (int l = 0; l < @int.y; l += 4)
					{
						float depth = m_WaterSurfaceData.depths[num15 + k + l * m_WaterSurfaceData.resolution.z].m_Depth;
						if (depth > 0.01f)
						{
							num11++;
							num13 += math.min(m_SoilWaterParameters.m_MaximumWaterDepth, depth);
							num14 += math.min(x, depth);
						}
						num12++;
					}
				}
				num10 = (short)Math.Min(num10, Mathf.RoundToInt((float)value3.m_Max * 10f * num14));
				x = (float)num10 * m_SoilWaterParameters.m_WaterPerUnit / (float)value3.m_Max;
				float num16 = (1f - num4 * num13 / (float)num12) * (float)value3.m_Max;
				short num17 = (short)Mathf.RoundToInt(math.max(0f, m_SoilWaterParameters.m_OverflowRate * ((float)value3.m_Amount - num16)));
				float num18 = 0f;
				if ((float)num17 > 0f)
				{
					num18 = (float)value3.m_Amount / (float)value3.m_Max;
					x = 0f;
				}
				if (num11 == 0)
				{
					x = 0f;
				}
				value3.m_Amount += num10;
				value3.m_Amount -= num17;
				short num19 = (short)Mathf.RoundToInt(math.sign(value3.m_Max / 8 - value3.m_Amount));
				value3.m_Amount += num19;
				num6 += num10 + Math.Max((short)0, num19);
				num5 += num17 + Math.Max(0, -num19);
				num7 += value3.m_Amount;
				m_SoilWaterTextureData[j] = (0f - x) / (float)m_ShaderUpdatesPerSoilUpdate + num18;
				m_SoilWaterMap[j] = value3;
			}
			tmp.Dispose();
		}
	}

	private struct TypeHandle
	{
		public ComponentLookup<WaterLevelChange> __Game_Events_WaterLevelChange_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EventData> __Game_Prefabs_EventData_RO_ComponentLookup;

		public ComponentLookup<FloodCounterData> __Game_Simulation_FloodCounterData_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Events_WaterLevelChange_RW_ComponentLookup = state.GetComponentLookup<WaterLevelChange>();
			__Game_Prefabs_EventData_RO_ComponentLookup = state.GetComponentLookup<EventData>(isReadOnly: true);
			__Game_Simulation_FloodCounterData_RW_ComponentLookup = state.GetComponentLookup<FloodCounterData>();
		}
	}

	public static readonly int kTextureSize = 128;

	public static readonly int kUpdatesPerDay = 1024;

	public static readonly int kLoadDistribution = 8;

	private SimulationSystem m_SimulationSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private ClimateSystem m_ClimateSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private Texture2D m_SoilWaterTexture;

	private EntityQuery m_SoilWaterParameterQuery;

	private EntityQuery m_FloodQuery;

	private EntityQuery m_FloodPrefabQuery;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_336595330_0;

	public int2 TextureSize => new int2(kTextureSize, kTextureSize);

	public Texture soilTexture => m_SoilWaterTexture;

	public static float3 GetCellCenter(int index)
	{
		return CellMapSystem<SoilWater>.GetCellCenter(index, kTextureSize);
	}

	public static SoilWater GetSoilWater(float3 position, NativeArray<SoilWater> soilWaterMap)
	{
		SoilWater result = default(SoilWater);
		int2 cell = CellMapSystem<SoilWater>.GetCell(position, CellMapSystem<SoilWater>.kMapSize, kTextureSize);
		float2 cellCoords = CellMapSystem<SoilWater>.GetCellCoords(position, CellMapSystem<SoilWater>.kMapSize, kTextureSize);
		if (cell.x < 0 || cell.x >= kTextureSize || cell.y < 0 || cell.y >= kTextureSize)
		{
			return result;
		}
		float start = soilWaterMap[cell.x + kTextureSize * cell.y].m_Amount;
		float end = ((cell.x < kTextureSize - 1) ? soilWaterMap[cell.x + 1 + kTextureSize * cell.y].m_Amount : 0);
		float start2 = ((cell.y < kTextureSize - 1) ? soilWaterMap[cell.x + kTextureSize * (cell.y + 1)].m_Amount : 0);
		float end2 = ((cell.x < kTextureSize - 1 && cell.y < kTextureSize - 1) ? soilWaterMap[cell.x + 1 + kTextureSize * (cell.y + 1)].m_Amount : 0);
		result.m_Amount = (short)Mathf.RoundToInt(math.lerp(math.lerp(start, end, cellCoords.x - (float)cell.x), math.lerp(start2, end2, cellCoords.x - (float)cell.x), cellCoords.y - (float)cell.y));
		return result;
	}

	private void CreateFloodCounter()
	{
		base.EntityManager.CreateEntity(base.EntityManager.CreateArchetype(ComponentType.ReadWrite<FloodCounterData>()));
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SoilWaterParameterQuery = GetEntityQuery(ComponentType.ReadOnly<SoilWaterParameterData>());
		m_FloodQuery = GetEntityQuery(ComponentType.ReadOnly<Flood>());
		m_FloodPrefabQuery = GetEntityQuery(ComponentType.ReadOnly<FloodData>());
		CreateFloodCounter();
		CreateTextures(kTextureSize);
		m_SoilWaterTexture = new Texture2D(kTextureSize, kTextureSize, TextureFormat.RFloat, mipChain: false, linear: true)
		{
			name = "SoilWaterTexture",
			hideFlags = HideFlags.HideAndDontSave
		};
		NativeArray<float> rawTextureData = m_SoilWaterTexture.GetRawTextureData<float>();
		for (int i = 0; i < m_Map.Length; i++)
		{
			_ = (float)(i % kTextureSize) / (float)kTextureSize;
			_ = (float)(i / kTextureSize) / (float)kTextureSize;
			SoilWater value = new SoilWater
			{
				m_Amount = 1024,
				m_Max = 8192
			};
			m_Map[i] = value;
			rawTextureData[i] = 0f;
		}
		m_SoilWaterTexture.Apply();
		RequireForUpdate(m_SoilWaterParameterQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TerrainHeightData heightData = m_TerrainSystem.GetHeightData();
		if (heightData.isCreated)
		{
			m_SoilWaterTexture.Apply();
			float value = m_ClimateSystem.precipitation.value;
			int shaderUpdatesPerSoilUpdate = 262144 / (kUpdatesPerDay / kLoadDistribution) / m_WaterSystem.SimulationCycleSteps;
			int loadDistributionIndex = (int)(m_SimulationSystem.frameIndex / (262144 / kUpdatesPerDay) % kLoadDistribution);
			JobHandle deps;
			JobHandle outJobHandle;
			JobHandle outJobHandle2;
			SoilWaterTickJob jobData = new SoilWaterTickJob
			{
				m_SoilWaterMap = m_Map,
				m_TerrainHeightData = heightData,
				m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
				m_SoilWaterTextureData = m_SoilWaterTexture.GetRawTextureData<float>(),
				m_SoilWaterParameters = m_SoilWaterParameterQuery.GetSingleton<SoilWaterParameterData>(),
				m_FloodEntities = m_FloodQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle),
				m_FloodPrefabEntities = m_FloodPrefabQuery.ToEntityListAsync(base.World.UpdateAllocator.ToAllocator, out outJobHandle2),
				m_Changes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_WaterLevelChange_RW_ComponentLookup, ref base.CheckedStateRef),
				m_Events = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EventData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_FloodCounterDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_FloodCounterData_RW_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer(),
				m_FloodCounterEntity = __query_336595330_0.GetSingletonEntity(),
				m_Weather = value,
				m_ShaderUpdatesPerSoilUpdate = shaderUpdatesPerSoilUpdate,
				m_LoadDistributionIndex = loadDistributionIndex
			};
			base.Dependency = IJobExtensions.Schedule(jobData, JobUtils.CombineDependencies(m_WriteDependencies, m_ReadDependencies, outJobHandle, outJobHandle2, deps, base.Dependency));
			AddWriter(base.Dependency);
			m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
			m_TerrainSystem.AddCPUHeightReader(base.Dependency);
			m_WaterSystem.AddSurfaceReader(base.Dependency);
			base.Dependency = JobHandle.CombineDependencies(m_ReadDependencies, m_WriteDependencies, base.Dependency);
		}
	}

	public void PostDeserialize(Context context)
	{
		EntityQuery entityQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<FloodCounterData>());
		try
		{
			if (entityQuery.CalculateEntityCount() == 0)
			{
				CreateFloodCounter();
			}
		}
		finally
		{
			entityQuery.Dispose();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<FloodCounterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_336595330_0 = entityQueryBuilder2.Build(ref state);
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
	public SoilWaterSystem()
	{
	}
}
