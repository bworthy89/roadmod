#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CrimeAccumulationSystem : GameSystemBase
{
	[BurstCompile]
	private struct CrimeAccumulationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<CrimeProducer> m_CrimeProducerType;

		[ReadOnly]
		public ComponentLookup<PolicePatrolRequest> m_PolicePatrolRequestData;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<CrimeAccumulationData> m_CrimeAccumulationData;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> m_ServiceObjectData;

		[ReadOnly]
		public BufferLookup<DistrictModifier> m_DistrictModifiers;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public EntityArchetype m_PatrolRequestArchetype;

		[ReadOnly]
		public PoliceConfigurationData m_PoliceConfigurationData;

		[ReadOnly]
		public LocalEffectSystem.ReadData m_LocalEffectData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Building> nativeArray2 = chunk.GetNativeArray(ref m_BuildingType);
			NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<CurrentDistrict> nativeArray4 = chunk.GetNativeArray(ref m_CurrentDistrictType);
			NativeArray<PrefabRef> nativeArray5 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<CrimeProducer> nativeArray6 = chunk.GetNativeArray(ref m_CrimeProducerType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				Transform transform = nativeArray3[i];
				float value = GetBuildingCrimeIncreasePerDay(nativeArray5[i].m_Prefab);
				if (value == 0f)
				{
					continue;
				}
				if (nativeArray2.Length != 0)
				{
					Building building = nativeArray2[i];
					if (m_ServiceCoverages.HasBuffer(building.m_RoadEdge))
					{
						float serviceCoverage = NetUtils.GetServiceCoverage(m_ServiceCoverages[building.m_RoadEdge], CoverageService.Police, building.m_CurvePosition);
						value *= m_PoliceConfigurationData.m_CrimePoliceCoverageFactor * math.max(0f, 5f / (5f + serviceCoverage));
					}
					else if (building.m_RoadEdge == Entity.Null)
					{
						continue;
					}
				}
				m_LocalEffectData.ApplyModifier(ref value, transform.m_Position, LocalModifierType.CrimeAccumulation);
				if (nativeArray4.Length != 0)
				{
					CurrentDistrict currentDistrict = nativeArray4[i];
					if (m_DistrictModifiers.HasBuffer(currentDistrict.m_District))
					{
						DynamicBuffer<DistrictModifier> modifiers2 = m_DistrictModifiers[currentDistrict.m_District];
						AreaUtils.ApplyModifier(ref value, modifiers2, DistrictModifierType.CrimeAccumulation);
					}
				}
				CityUtils.ApplyModifier(ref value, modifiers, CityModifierType.CrimeAccumulation);
				float num = math.max(0f, value / (float)kUpdatesPerDay);
				CrimeProducer producer = nativeArray6[i];
				producer.m_Crime = math.min(m_PoliceConfigurationData.m_MaxCrimeAccumulation, producer.m_Crime + num);
				RequestPatrolIfNeeded(unfilteredChunkIndex, entity, ref producer, ref random);
				nativeArray6[i] = producer;
			}
		}

		private float GetBuildingCrimeIncreasePerDay(Entity prefab)
		{
			if (m_SpawnableBuildingData.HasComponent(prefab))
			{
				SpawnableBuildingData spawnableBuildingData = m_SpawnableBuildingData[prefab];
				if (m_CrimeAccumulationData.HasComponent(spawnableBuildingData.m_ZonePrefab))
				{
					return m_CrimeAccumulationData[spawnableBuildingData.m_ZonePrefab].m_CrimeRate;
				}
			}
			else if (m_ServiceObjectData.HasComponent(prefab))
			{
				ServiceObjectData serviceObjectData = m_ServiceObjectData[prefab];
				if (m_CrimeAccumulationData.HasComponent(serviceObjectData.m_Service))
				{
					return m_CrimeAccumulationData[serviceObjectData.m_Service].m_CrimeRate;
				}
			}
			return 0f;
		}

		private void RequestPatrolIfNeeded(int jobIndex, Entity entity, ref CrimeProducer producer, ref Random random)
		{
			if (!(producer.m_Crime < m_PoliceConfigurationData.m_CrimeAccumulationTolerance) && (!m_PolicePatrolRequestData.TryGetComponent(producer.m_PatrolRequest, out var componentData) || (!(componentData.m_Target == entity) && componentData.m_DispatchIndex != producer.m_DispatchIndex)))
			{
				producer.m_PatrolRequest = Entity.Null;
				producer.m_DispatchIndex = 0;
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PatrolRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new PolicePatrolRequest(entity, producer.m_Crime / m_PoliceConfigurationData.m_MaxCrimeAccumulation));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
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

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<CrimeProducer> __Game_Buildings_CrimeProducer_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PolicePatrolRequest> __Game_Simulation_PolicePatrolRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CrimeAccumulationData> __Game_Prefabs_CrimeAccumulationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> __Game_Prefabs_ServiceObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CrimeProducer>();
			__Game_Simulation_PolicePatrolRequest_RO_ComponentLookup = state.GetComponentLookup<PolicePatrolRequest>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_CrimeAccumulationData_RO_ComponentLookup = state.GetComponentLookup<CrimeAccumulationData>(isReadOnly: true);
			__Game_Prefabs_ServiceObjectData_RO_ComponentLookup = state.GetComponentLookup<ServiceObjectData>(isReadOnly: true);
			__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 256;

	public static readonly int kUpdateInterval = 262144 / (kUpdatesPerDay * 16);

	private CitySystem m_CitySystem;

	private LocalEffectSystem m_LocalEffectSystem;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_CrimeProducerQuery;

	private EntityQuery m_PoliceConfigurationQuery;

	private EntityArchetype m_PatrolRequestArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return kUpdateInterval;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_LocalEffectSystem = base.World.GetOrCreateSystemManaged<LocalEffectSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CrimeProducerQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<CrimeProducer>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_PoliceConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
		m_PatrolRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PolicePatrolRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_CrimeProducerQuery);
		Assert.IsTrue((long)(kUpdateInterval * 16) >= 512L);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		PoliceConfigurationData singleton = m_PoliceConfigurationQuery.GetSingleton<PoliceConfigurationData>();
		if (!base.EntityManager.HasEnabledComponent<Locked>(singleton.m_PoliceServicePrefab))
		{
			uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
			m_CrimeProducerQuery.ResetFilter();
			m_CrimeProducerQuery.SetSharedComponentFilter(new UpdateFrame(updateFrameWithInterval));
			JobHandle dependencies;
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new CrimeAccumulationJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurrentDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CrimeProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CrimeProducer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_PolicePatrolRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PolicePatrolRequest_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CrimeAccumulationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CrimeAccumulationData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ServiceObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ServiceObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DistrictModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_ServiceCoverages = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref base.CheckedStateRef),
				m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_City = m_CitySystem.City,
				m_PatrolRequestArchetype = m_PatrolRequestArchetype,
				m_PoliceConfigurationData = singleton,
				m_LocalEffectData = m_LocalEffectSystem.GetReadData(out dependencies),
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_RandomSeed = RandomSeed.Next()
			}, m_CrimeProducerQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
			m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
			m_LocalEffectSystem.AddLocalEffectReader(jobHandle);
			base.Dependency = jobHandle;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public CrimeAccumulationSystem()
	{
	}
}
