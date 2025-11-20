#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class ParkAISystem : GameSystemBase
{
	[BurstCompile]
	private struct ParkTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		public ComponentTypeHandle<Game.Buildings.Park> m_ParkType;

		[ReadOnly]
		public ComponentTypeHandle<MaintenanceConsumer> m_MaintenanceConsumerType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<ModifiedServiceCoverage> m_ModifiedServiceCoverageType;

		[ReadOnly]
		public BufferTypeHandle<Renter> m_RenterType;

		[ReadOnly]
		public ComponentLookup<CoverageData> m_CoverageDatas;

		[ReadOnly]
		public ComponentLookup<ParkData> m_ParkDatas;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> m_MaintenanceRequestData;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public EntityArchetype m_MaintenanceRequestArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.Park> nativeArray2 = chunk.GetNativeArray(ref m_ParkType);
			NativeArray<MaintenanceConsumer> nativeArray3 = chunk.GetNativeArray(ref m_MaintenanceConsumerType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<ModifiedServiceCoverage> nativeArray5 = chunk.GetNativeArray(ref m_ModifiedServiceCoverageType);
			BufferAccessor<Renter> bufferAccessor = chunk.GetBufferAccessor(ref m_RenterType);
			bool flag = chunk.Has(ref m_CurrentDistrictType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity prefab = nativeArray4[i].m_Prefab;
				if (m_ParkDatas.HasComponent(prefab))
				{
					ParkData prefabParkData = m_ParkDatas[prefab];
					Game.Buildings.Park park = nativeArray2[i];
					park.m_Maintenance = (short)math.max(0, park.m_Maintenance - (400 + 50 * bufferAccessor[i].Length) / kUpdatesPerDay);
					nativeArray2[i] = park;
					if (nativeArray3.Length != 0)
					{
						MaintenanceConsumer maintenanceConsumer = nativeArray3[i];
						RequestMaintenanceIfNeeded(unfilteredChunkIndex, entity, park, maintenanceConsumer, prefabParkData);
					}
					if (m_CoverageDatas.HasComponent(prefab))
					{
						CoverageData prefabCoverageData = m_CoverageDatas[prefab];
						nativeArray5[i] = GetModifiedServiceCoverage(park, prefabParkData, prefabCoverageData, cityModifiers);
					}
					if (!flag)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(CurrentDistrict));
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Updated));
					}
				}
			}
		}

		private void RequestMaintenanceIfNeeded(int jobIndex, Entity entity, Game.Buildings.Park park, MaintenanceConsumer maintenanceConsumer, ParkData prefabParkData)
		{
			int maintenancePriority = GetMaintenancePriority(park, prefabParkData);
			if (maintenancePriority > 0 && !m_MaintenanceRequestData.HasComponent(maintenanceConsumer.m_Request))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_MaintenanceRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new MaintenanceRequest(entity, maintenancePriority));
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Buildings.Park> __Game_Buildings_Park_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MaintenanceConsumer> __Game_Simulation_MaintenanceConsumer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		public ComponentTypeHandle<ModifiedServiceCoverage> __Game_Buildings_ModifiedServiceCoverage_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Renter> __Game_Buildings_Renter_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<CoverageData> __Game_Prefabs_CoverageData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkData> __Game_Prefabs_ParkData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> __Game_Simulation_MaintenanceRequest_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Park_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Park>();
			__Game_Simulation_MaintenanceConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MaintenanceConsumer>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Buildings_ModifiedServiceCoverage_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ModifiedServiceCoverage>();
			__Game_Buildings_Renter_RO_BufferTypeHandle = state.GetBufferTypeHandle<Renter>(isReadOnly: true);
			__Game_Prefabs_CoverageData_RO_ComponentLookup = state.GetComponentLookup<CoverageData>(isReadOnly: true);
			__Game_Prefabs_ParkData_RO_ComponentLookup = state.GetComponentLookup<ParkData>(isReadOnly: true);
			__Game_Simulation_MaintenanceRequest_RO_ComponentLookup = state.GetComponentLookup<MaintenanceRequest>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 256;

	private CitySystem m_CitySystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_ParkQuery;

	private EntityArchetype m_MaintenanceRequestArchetype;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / kUpdatesPerDay;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ParkQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.Park>(), ComponentType.ReadWrite<ModifiedServiceCoverage>(), ComponentType.ReadOnly<Renter>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>());
		m_MaintenanceRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<MaintenanceRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_ParkQuery);
		Assert.IsTrue((long)(262144 / kUpdatesPerDay) >= 512L);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new ParkTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Park_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MaintenanceConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Simulation_MaintenanceConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ModifiedServiceCoverageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ModifiedServiceCoverage_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RenterType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_CoverageDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CoverageData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MaintenanceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_City = m_CitySystem.City,
			m_MaintenanceRequestArchetype = m_MaintenanceRequestArchetype,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_ParkQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
	}

	public static int GetMaintenancePriority(Game.Buildings.Park park, ParkData prefabParkData)
	{
		return prefabParkData.m_MaintenancePool - park.m_Maintenance - prefabParkData.m_MaintenancePool / 10;
	}

	public static ModifiedServiceCoverage GetModifiedServiceCoverage(Game.Buildings.Park park, ParkData prefabParkData, CoverageData prefabCoverageData, DynamicBuffer<CityModifier> cityModifiers)
	{
		float num = (float)park.m_Maintenance / (float)math.max(1, prefabParkData.m_MaintenancePool);
		ModifiedServiceCoverage result = new ModifiedServiceCoverage(prefabCoverageData);
		int num2 = Mathf.FloorToInt(num / 0.3f);
		result.m_Magnitude *= 0.95f + 0.05f * (float)math.min(1, num2) + 0.1f * (float)math.max(0, num2 - 1);
		result.m_Range *= 0.95f + 0.05f * (float)num2;
		if (cityModifiers.IsCreated)
		{
			CityUtils.ApplyModifier(ref result.m_Magnitude, cityModifiers, CityModifierType.ParkEntertainment);
		}
		return result;
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
	public ParkAISystem()
	{
	}
}
