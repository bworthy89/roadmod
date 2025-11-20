using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class BuildingPollutionAddSystem : GameSystemBase
{
	private struct PollutionItem
	{
		public int amount;

		public float2 position;
	}

	[BurstCompile]
	private struct ApplyBuildingPollutionJob<T> : IJob where T : struct, IPollution
	{
		public NativeArray<T> m_PollutionMap;

		public NativeQueue<PollutionItem> m_PollutionQueue;

		public int m_MapSize;

		public int m_TextureSize;

		public float m_MaxRadiusSq;

		public float m_Radius;

		public float m_Multiplier;

		[ReadOnly]
		public NativeArray<float> m_DistanceWeightCache;

		public PollutionParameterData m_PollutionParameters;

		private float GetWeight(int2 cell, float2 position, float radiusSq, float offset, int cellSize)
		{
			float2 @float = new float2(0f - offset + ((float)cell.x + 0.5f) * (float)cellSize, 0f - offset + ((float)cell.y + 0.5f) * (float)cellSize);
			float num = math.lengthsq(position - @float);
			if (num < radiusSq)
			{
				float num2 = 255f * num / m_MaxRadiusSq;
				int num3 = Mathf.FloorToInt(num2);
				return math.lerp(m_DistanceWeightCache[num3], m_DistanceWeightCache[num3 + 1], math.frac(num2));
			}
			return 0f;
		}

		private void AddSingle(int pollution, int mapSize, int textureSize, float2 position, float radius, NativeArray<T> map, ref StackList<float> weightCache)
		{
			int num = mapSize / textureSize;
			float num2 = (float)mapSize / 2f;
			float radiusSq = radius * radius;
			int2 @int = new int2(math.max(0, Mathf.FloorToInt((position.x + num2 - radius) / (float)num)), math.max(0, Mathf.FloorToInt((position.y + num2 - radius) / (float)num)));
			int2 int2 = new int2(math.min(textureSize - 1, Mathf.CeilToInt((position.x + num2 + radius) / (float)num)), math.min(textureSize - 1, Mathf.CeilToInt((position.y + num2 + radius) / (float)num)));
			float num3 = 0f;
			int num4 = 0;
			int2 cell = default(int2);
			cell.x = @int.x;
			while (cell.x <= int2.x)
			{
				cell.y = @int.y;
				while (cell.y <= int2.y)
				{
					float weight = GetWeight(cell, position, radiusSq, 0.5f * (float)mapSize, num);
					num3 += weight;
					weightCache[num4] = weight;
					num4++;
					cell.y++;
				}
				cell.x++;
			}
			num4 = 0;
			float num5 = 1f / (num3 * (float)kUpdatesPerDay);
			cell.x = @int.x;
			while (cell.x <= int2.x)
			{
				int num6 = cell.x + textureSize * @int.y;
				cell.y = @int.y;
				while (cell.y <= int2.y)
				{
					float num7 = (float)pollution * num5 * weightCache[num4];
					num4++;
					if (num7 > 0.2f)
					{
						int num8 = Mathf.CeilToInt(num7);
						T value = map[num6];
						value.Add((short)num8);
						map[num6] = value;
					}
					num6 += textureSize;
					cell.y++;
				}
				cell.x++;
			}
		}

		public void Execute()
		{
			int num = 3 + Mathf.CeilToInt(2f * m_Radius * (float)m_TextureSize / (float)m_MapSize);
			StackList<float> weightCache = stackalloc float[num * num];
			weightCache.Length = num * num;
			PollutionItem item;
			while (m_PollutionQueue.TryDequeue(out item))
			{
				AddSingle((int)(m_Multiplier * (float)item.amount), m_MapSize, m_TextureSize, item.position, m_Radius, m_PollutionMap, ref weightCache);
			}
		}
	}

	[BurstCompile]
	private struct BuildingPolluteJob : IJobChunk
	{
		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> m_DestroyedType;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> m_AbandonedType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Park> m_ParkType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_BuildingEfficiencyType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

		[ReadOnly]
		public ComponentLookup<PollutionData> m_PollutionDatas;

		[ReadOnly]
		public ComponentLookup<PollutionModifierData> m_PollutionModifierDatas;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneDatas;

		[ReadOnly]
		public BufferLookup<Employee> m_Employees;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public ComponentLookup<PollutionEmitModifier> m_PollutionEmitModifier;

		[ReadOnly]
		public PollutionParameterData m_PollutionParameters;

		public NativeQueue<PollutionItem>.ParallelWriter m_GroundPollutionQueue;

		public NativeQueue<PollutionItem>.ParallelWriter m_AirPollutionQueue;

		public NativeQueue<PollutionItem>.ParallelWriter m_NoisePollutionQueue;

		public Entity m_City;

		public uint m_UpdateFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			bool destroyed = chunk.Has(ref m_DestroyedType);
			bool abandoned = chunk.Has(ref m_AbandonedType);
			bool isPark = chunk.Has(ref m_ParkType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_BuildingEfficiencyType);
			BufferAccessor<Renter> bufferAccessor2 = chunk.GetBufferAccessor(ref m_RenterType);
			BufferAccessor<InstalledUpgrade> bufferAccessor3 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray2[i].m_Prefab;
				float3 position = nativeArray3[i].m_Position;
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				DynamicBuffer<Renter> renters = ((bufferAccessor2.Length != 0) ? bufferAccessor2[i] : default(DynamicBuffer<Renter>));
				DynamicBuffer<InstalledUpgrade> installedUpgrades = ((bufferAccessor3.Length != 0) ? bufferAccessor3[i] : default(DynamicBuffer<InstalledUpgrade>));
				PollutionData pollutionData = GetBuildingPollution(prefab, destroyed, abandoned, isPark, efficiency, renters, installedUpgrades, m_PollutionParameters, cityModifiers, ref m_Prefabs, ref m_BuildingDatas, ref m_SpawnableDatas, ref m_PollutionDatas, ref m_PollutionModifierDatas, ref m_ZoneDatas, ref m_Employees, ref m_HouseholdCitizens, ref m_Citizens, ref m_PollutionEmitModifier);
				if (m_PollutionEmitModifier.TryGetComponent(entity, out var componentData))
				{
					componentData.UpdatePollutionData(ref pollutionData);
				}
				if (pollutionData.m_GroundPollution > 0f)
				{
					m_GroundPollutionQueue.Enqueue(new PollutionItem
					{
						amount = (int)pollutionData.m_GroundPollution,
						position = position.xz
					});
				}
				if (pollutionData.m_AirPollution > 0f)
				{
					m_AirPollutionQueue.Enqueue(new PollutionItem
					{
						amount = (int)pollutionData.m_AirPollution,
						position = position.xz
					});
				}
				if (pollutionData.m_NoisePollution > 0f)
				{
					m_NoisePollutionQueue.Enqueue(new PollutionItem
					{
						amount = (int)pollutionData.m_NoisePollution,
						position = position.xz
					});
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Destroyed> __Game_Common_Destroyed_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Abandoned> __Game_Buildings_Abandoned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PollutionData> __Game_Prefabs_PollutionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PollutionModifierData> __Game_Prefabs_PollutionModifierData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<PollutionEmitModifier> __Game_Buildings_PollutionEmitModifier_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Destroyed>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Abandoned>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Park>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_PollutionData_RO_ComponentLookup = state.GetComponentLookup<PollutionData>(isReadOnly: true);
			__Game_Prefabs_PollutionModifierData_RO_ComponentLookup = state.GetComponentLookup<PollutionModifierData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Buildings_PollutionEmitModifier_RO_ComponentLookup = state.GetComponentLookup<PollutionEmitModifier>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 128;

	private SimulationSystem m_SimulationSystem;

	private GroundPollutionSystem m_GroundPollutionSystem;

	private AirPollutionSystem m_AirPollutionSystem;

	private NoisePollutionSystem m_NoisePollutionSystem;

	private CitySystem m_CitySystem;

	private EntityQuery m_PolluterQuery;

	private NativeArray<float> m_DistanceWeightCache;

	private float m_CachedDistanceExponent;

	private NativeQueue<PollutionItem> m_GroundPollutionQueue;

	private NativeQueue<PollutionItem> m_AirPollutionQueue;

	private NativeQueue<PollutionItem> m_NoisePollutionQueue;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_985639356_0;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (16 * kUpdatesPerDay);
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_GroundPollutionSystem = base.World.GetOrCreateSystemManaged<GroundPollutionSystem>();
		m_AirPollutionSystem = base.World.GetOrCreateSystemManaged<AirPollutionSystem>();
		m_NoisePollutionSystem = base.World.GetOrCreateSystemManaged<NoisePollutionSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_GroundPollutionQueue = new NativeQueue<PollutionItem>(Allocator.Persistent);
		m_AirPollutionQueue = new NativeQueue<PollutionItem>(Allocator.Persistent);
		m_NoisePollutionQueue = new NativeQueue<PollutionItem>(Allocator.Persistent);
		m_PolluterQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Placeholder>());
		m_CachedDistanceExponent = float.MinValue;
	}

	[Preserve]
	protected override void OnDestroy()
	{
		if (m_DistanceWeightCache.IsCreated)
		{
			m_DistanceWeightCache.Dispose();
		}
		m_GroundPollutionQueue.Dispose();
		m_AirPollutionQueue.Dispose();
		m_NoisePollutionQueue.Dispose();
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PollutionParameterData singleton = __query_985639356_0.GetSingleton<PollutionParameterData>();
		float num = math.max(math.max(singleton.m_GroundRadius, singleton.m_AirRadius), singleton.m_NoiseRadius);
		num *= num;
		if (m_CachedDistanceExponent != singleton.m_DistanceExponent)
		{
			if (!m_DistanceWeightCache.IsCreated)
			{
				m_DistanceWeightCache = new NativeArray<float>(256, Allocator.Persistent);
			}
			m_CachedDistanceExponent = singleton.m_DistanceExponent;
			for (int i = 0; i < 256; i++)
			{
				m_DistanceWeightCache[i] = GetWeight(math.sqrt(num * (float)i / 256f), singleton.m_DistanceExponent);
			}
		}
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new BuildingPolluteJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DestroyedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AbandonedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingEfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PollutionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PollutionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PollutionModifierDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PollutionModifierData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ZoneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_PollutionEmitModifier = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PollutionEmitModifier_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PollutionParameters = singleton,
			m_GroundPollutionQueue = m_GroundPollutionQueue.AsParallelWriter(),
			m_AirPollutionQueue = m_AirPollutionQueue.AsParallelWriter(),
			m_NoisePollutionQueue = m_NoisePollutionQueue.AsParallelWriter(),
			m_City = m_CitySystem.City,
			m_UpdateFrameIndex = updateFrameWithInterval
		}, m_PolluterQuery, base.Dependency);
		JobHandle dependencies;
		JobHandle jobHandle2 = IJobExtensions.Schedule(new ApplyBuildingPollutionJob<GroundPollution>
		{
			m_PollutionMap = m_GroundPollutionSystem.GetMap(readOnly: false, out dependencies),
			m_MapSize = CellMapSystem<GroundPollution>.kMapSize,
			m_TextureSize = GroundPollutionSystem.kTextureSize,
			m_PollutionParameters = singleton,
			m_MaxRadiusSq = num,
			m_Radius = singleton.m_GroundRadius,
			m_PollutionQueue = m_GroundPollutionQueue,
			m_DistanceWeightCache = m_DistanceWeightCache,
			m_Multiplier = singleton.m_GroundMultiplier
		}, JobHandle.CombineDependencies(jobHandle, dependencies));
		m_GroundPollutionSystem.AddWriter(jobHandle2);
		JobHandle dependencies2;
		JobHandle jobHandle3 = IJobExtensions.Schedule(new ApplyBuildingPollutionJob<AirPollution>
		{
			m_PollutionMap = m_AirPollutionSystem.GetMap(readOnly: false, out dependencies2),
			m_MapSize = CellMapSystem<AirPollution>.kMapSize,
			m_TextureSize = AirPollutionSystem.kTextureSize,
			m_PollutionParameters = singleton,
			m_MaxRadiusSq = num,
			m_Radius = singleton.m_AirRadius,
			m_PollutionQueue = m_AirPollutionQueue,
			m_DistanceWeightCache = m_DistanceWeightCache,
			m_Multiplier = singleton.m_AirMultiplier
		}, JobHandle.CombineDependencies(dependencies2, jobHandle));
		m_AirPollutionSystem.AddWriter(jobHandle3);
		JobHandle dependencies3;
		JobHandle jobHandle4 = IJobExtensions.Schedule(new ApplyBuildingPollutionJob<NoisePollution>
		{
			m_PollutionMap = m_NoisePollutionSystem.GetMap(readOnly: false, out dependencies3),
			m_MapSize = CellMapSystem<NoisePollution>.kMapSize,
			m_TextureSize = NoisePollutionSystem.kTextureSize,
			m_PollutionParameters = singleton,
			m_MaxRadiusSq = num,
			m_Radius = singleton.m_NoiseRadius,
			m_PollutionQueue = m_NoisePollutionQueue,
			m_DistanceWeightCache = m_DistanceWeightCache,
			m_Multiplier = singleton.m_NoiseMultiplier
		}, JobHandle.CombineDependencies(dependencies3, jobHandle));
		m_NoisePollutionSystem.AddWriter(jobHandle4);
		base.Dependency = JobHandle.CombineDependencies(jobHandle2, jobHandle3, jobHandle4);
	}

	private static float GetWeight(float distance, float exponent)
	{
		return 1f / math.max(20f, math.pow(distance, exponent));
	}

	public static PollutionData GetBuildingPollution(Entity prefab, bool destroyed, bool abandoned, bool isPark, float efficiency, DynamicBuffer<Renter> renters, DynamicBuffer<InstalledUpgrade> installedUpgrades, PollutionParameterData pollutionParameters, DynamicBuffer<CityModifier> cityModifiers, ref ComponentLookup<PrefabRef> prefabRefs, ref ComponentLookup<BuildingData> buildingDatas, ref ComponentLookup<SpawnableBuildingData> spawnableDatas, ref ComponentLookup<PollutionData> pollutionDatas, ref ComponentLookup<PollutionModifierData> pollutionModifierDatas, ref ComponentLookup<ZoneData> zoneDatas, ref BufferLookup<Employee> employees, ref BufferLookup<HouseholdCitizen> householdCitizens, ref ComponentLookup<Citizen> citizens, ref ComponentLookup<PollutionEmitModifier> pollutionEmitModifiers)
	{
		PollutionData componentData;
		if (!(destroyed || abandoned))
		{
			if (efficiency > 0f && pollutionDatas.TryGetComponent(prefab, out componentData))
			{
				if (installedUpgrades.IsCreated)
				{
					UpgradeUtils.CombinePollutionStats(ref componentData, installedUpgrades, ref prefabRefs, ref pollutionDatas, ref pollutionEmitModifiers);
				}
				SpawnableBuildingData componentData2;
				if (componentData.m_ScaleWithRenters && !isPark && renters.IsCreated)
				{
					CountRenters(out var count, out var education, renters, ref employees, ref householdCitizens, ref citizens, ignoreEmployees: false);
					float num = (spawnableDatas.TryGetComponent(prefab, out componentData2) ? ((float)(int)componentData2.m_Level) : 5f);
					float num2 = ((count > 0) ? (5f * (float)count / (num + 0.5f * (float)(education / count))) : 0f);
					componentData.m_GroundPollution *= num2;
					componentData.m_AirPollution *= num2;
					componentData.m_NoisePollution *= num2;
				}
				if (cityModifiers.IsCreated && spawnableDatas.TryGetComponent(prefab, out componentData2))
				{
					ZoneData zoneData = zoneDatas[componentData2.m_ZonePrefab];
					if (zoneData.m_AreaType == AreaType.Industrial && (zoneData.m_ZoneFlags & ZoneFlags.Office) == 0)
					{
						CityUtils.ApplyModifier(ref componentData.m_GroundPollution, cityModifiers, CityModifierType.IndustrialGroundPollution);
						CityUtils.ApplyModifier(ref componentData.m_AirPollution, cityModifiers, CityModifierType.IndustrialAirPollution);
					}
				}
				if (installedUpgrades.IsCreated)
				{
					PollutionModifierData data = default(PollutionModifierData);
					UpgradeUtils.CombineStats(ref data, installedUpgrades, ref prefabRefs, ref pollutionModifierDatas);
					componentData.m_GroundPollution *= math.max(0f, 1f + data.m_GroundPollutionMultiplier);
					componentData.m_AirPollution *= math.max(0f, 1f + data.m_AirPollutionMultiplier);
					componentData.m_NoisePollution *= math.max(0f, 1f + data.m_NoisePollutionMultiplier);
				}
			}
			else
			{
				componentData = default(PollutionData);
			}
		}
		else
		{
			BuildingData buildingData = buildingDatas[prefab];
			componentData = new PollutionData
			{
				m_GroundPollution = 0f,
				m_AirPollution = 0f,
				m_NoisePollution = (destroyed ? 0f : (5f * (float)(buildingData.m_LotSize.x * buildingData.m_LotSize.y) * pollutionParameters.m_AbandonedNoisePollutionMultiplier))
			};
		}
		if ((abandoned || isPark) && renters.IsCreated)
		{
			CountRenters(out var count2, out var _, renters, ref employees, ref householdCitizens, ref citizens, ignoreEmployees: true);
			componentData.m_NoisePollution += count2 * pollutionParameters.m_HomelessNoisePollution;
		}
		return componentData;
	}

	private static void CountRenters(out int count, out int education, DynamicBuffer<Renter> renters, ref BufferLookup<Employee> employees, ref BufferLookup<HouseholdCitizen> householdCitizens, ref ComponentLookup<Citizen> citizens, bool ignoreEmployees)
	{
		count = 0;
		education = 0;
		foreach (Renter item in renters)
		{
			if (householdCitizens.TryGetBuffer(item, out var bufferData))
			{
				foreach (HouseholdCitizen item2 in bufferData)
				{
					if (citizens.TryGetComponent(item2, out var componentData))
					{
						education += componentData.GetEducationLevel();
						count++;
					}
				}
			}
			else
			{
				if (ignoreEmployees || !employees.TryGetBuffer(item, out var bufferData2))
				{
					continue;
				}
				foreach (Employee item3 in bufferData2)
				{
					if (citizens.TryGetComponent(item3.m_Worker, out var componentData2))
					{
						education += componentData2.GetEducationLevel();
						count++;
					}
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<PollutionParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_985639356_0 = entityQueryBuilder2.Build(ref state);
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
	public BuildingPollutionAddSystem()
	{
	}
}
