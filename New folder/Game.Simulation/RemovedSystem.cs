using System.Runtime.CompilerServices;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Notifications;
using Game.Prefabs;
using Game.Tools;
using Game.Triggers;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class RemovedSystem : GameSystemBase
{
	[BurstCompile]
	private struct RemovedPropertyJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholds;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<LodgingProvider> m_LodgingProviders;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterBufs;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				DynamicBuffer<Renter> dynamicBuffer = m_RenterBufs[nativeArray[i]];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (m_PropertyRenters.HasComponent(dynamicBuffer[j].m_Renter))
					{
						m_CommandBuffer.RemoveComponent<PropertyRenter>(unfilteredChunkIndex, dynamicBuffer[j].m_Renter);
					}
					if (!m_LodgingProviders.HasComponent(dynamicBuffer[j].m_Renter) || !m_RenterBufs.HasBuffer(dynamicBuffer[j].m_Renter))
					{
						continue;
					}
					DynamicBuffer<Renter> dynamicBuffer2 = m_RenterBufs[dynamicBuffer[j].m_Renter];
					for (int num = dynamicBuffer2.Length - 1; num >= 0; num--)
					{
						if (m_TouristHouseholds.HasComponent(dynamicBuffer2[num].m_Renter))
						{
							TouristHousehold component = m_TouristHouseholds[dynamicBuffer2[num].m_Renter];
							component.m_Hotel = Entity.Null;
							m_CommandBuffer.SetComponent(unfilteredChunkIndex, dynamicBuffer2[num].m_Renter, component);
						}
					}
					m_CommandBuffer.AddComponent<Deleted>(unfilteredChunkIndex, dynamicBuffer[j].m_Renter);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct RemovedWorkplaceJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Employee> m_EmployeeType;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_Purposes;

		[ReadOnly]
		public ComponentLookup<Worker> m_Workers;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<TriggerAction>.ParallelWriter m_TriggerBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				m_CommandBuffer.RemoveComponent<FreeWorkplaces>(unfilteredChunkIndex, nativeArray[i]);
			}
			BufferAccessor<Employee> bufferAccessor = chunk.GetBufferAccessor(ref m_EmployeeType);
			for (int j = 0; j < bufferAccessor.Length; j++)
			{
				DynamicBuffer<Employee> dynamicBuffer = bufferAccessor[j];
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					Entity worker = dynamicBuffer[k].m_Worker;
					if (m_Purposes.HasComponent(worker) && (m_Purposes[worker].m_Purpose == Purpose.GoingToWork || m_Purposes[worker].m_Purpose == Purpose.Working))
					{
						m_CommandBuffer.RemoveComponent<TravelPurpose>(unfilteredChunkIndex, worker);
					}
					if (m_Workers.HasComponent(worker))
					{
						m_CommandBuffer.RemoveComponent<Worker>(unfilteredChunkIndex, worker);
						m_TriggerBuffer.Enqueue(new TriggerAction(TriggerType.CitizenBecameUnemployed, Entity.Null, worker, nativeArray[j]));
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct RemovedCompanyJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<CompanyNotifications> m_NotificationsType;

		public IconCommandBuffer m_IconCommandBuffer;

		public CompanyNotificationParameterData m_CompanyNotificationParameters;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CompanyNotifications> nativeArray = chunk.GetNativeArray(ref m_NotificationsType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CompanyNotifications companyNotifications = nativeArray[i];
				if (companyNotifications.m_NoCustomersEntity != default(Entity))
				{
					m_IconCommandBuffer.Remove(companyNotifications.m_NoCustomersEntity, m_CompanyNotificationParameters.m_NoCustomersNotificationPrefab);
				}
				if (companyNotifications.m_NoInputEntity != default(Entity))
				{
					m_IconCommandBuffer.Remove(companyNotifications.m_NoInputEntity, m_CompanyNotificationParameters.m_NoInputsNotificationPrefab);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct RentersUpdateJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<RentersUpdated> m_RentersUpdatedType;

		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_Parks;

		[ReadOnly]
		public ComponentLookup<Abandoned> m_Abandoneds;

		[ReadOnly]
		public ComponentLookup<MovingAway> m_MovingAways;

		[ReadOnly]
		public ComponentLookup<Deleted> m_Deleteds;

		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public BuildingConfigurationData m_BuildingConfigurationData;

		public IconCommandBuffer m_IconCommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<RentersUpdated> nativeArray = chunk.GetNativeArray(ref m_RentersUpdatedType);
			for (int i = 0; i < chunk.Count; i++)
			{
				RentersUpdated rentersUpdated = nativeArray[i];
				if (!m_Renters.TryGetBuffer(rentersUpdated.m_Property, out var bufferData))
				{
					continue;
				}
				for (int num = bufferData.Length - 1; num >= 0; num--)
				{
					if (m_MovingAways.HasComponent(bufferData[num].m_Renter) || m_Deleteds.HasComponent(bufferData[num].m_Renter))
					{
						bufferData.RemoveAt(num);
					}
				}
				if (!BuildingUtils.IsHomelessShelterBuilding(rentersUpdated.m_Property, ref m_Parks, ref m_Abandoneds))
				{
					for (int num2 = bufferData.Length - 1; num2 >= 0; num2--)
					{
						if (!m_PropertyRenters.HasComponent(bufferData[num2].m_Renter) || m_PropertyRenters[bufferData[num2].m_Renter].m_Property != rentersUpdated.m_Property)
						{
							bufferData.RemoveAt(num2);
						}
					}
				}
				if (m_Buildings.TryGetComponent(rentersUpdated.m_Property, out var componentData) && (componentData.m_Flags & Game.Buildings.BuildingFlags.HighRentWarning) != Game.Buildings.BuildingFlags.None && bufferData.Length == 0)
				{
					m_IconCommandBuffer.Remove(rentersUpdated.m_Property, m_BuildingConfigurationData.m_HighRentNotification);
					componentData.m_Flags &= ~Game.Buildings.BuildingFlags.HighRentWarning;
					m_Buildings[rentersUpdated.m_Property] = componentData;
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

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LodgingProvider> __Game_Companies_LodgingProvider_RO_ComponentLookup;

		[ReadOnly]
		public BufferTypeHandle<Employee> __Game_Companies_Employee_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<CompanyNotifications> __Game_Companies_CompanyNotifications_RO_ComponentTypeHandle;

		public ComponentTypeHandle<RentersUpdated> __Game_Buildings_RentersUpdated_RW_ComponentTypeHandle;

		public BufferLookup<Renter> __Game_Buildings_Renter_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Abandoned> __Game_Buildings_Abandoned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MovingAway> __Game_Agents_MovingAway_RO_ComponentLookup;

		public ComponentLookup<Building> __Game_Buildings_Building_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Companies_LodgingProvider_RO_ComponentLookup = state.GetComponentLookup<LodgingProvider>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferTypeHandle = state.GetBufferTypeHandle<Employee>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Companies_CompanyNotifications_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CompanyNotifications>(isReadOnly: true);
			__Game_Buildings_RentersUpdated_RW_ComponentTypeHandle = state.GetComponentTypeHandle<RentersUpdated>();
			__Game_Buildings_Renter_RW_BufferLookup = state.GetBufferLookup<Renter>();
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Buildings_Abandoned_RO_ComponentLookup = state.GetComponentLookup<Abandoned>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true);
			__Game_Agents_MovingAway_RO_ComponentLookup = state.GetComponentLookup<MovingAway>(isReadOnly: true);
			__Game_Buildings_Building_RW_ComponentLookup = state.GetComponentLookup<Building>();
		}
	}

	private EntityQuery m_DeletedBuildings;

	private EntityQuery m_DeletedWorkplaces;

	private EntityQuery m_DeletedCompanies;

	private EntityQuery m_NeedUpdateRenterQuery;

	private EntityQuery m_BuildingParameterQuery;

	private EntityQuery m_CompanyNotificationParameterQuery;

	private IconCommandSystem m_IconCommandSystem;

	private TriggerSystem m_TriggerSystem;

	private ModificationBarrier5 m_ModificationBarrier;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_TriggerSystem = base.World.GetOrCreateSystemManaged<TriggerSystem>();
		m_DeletedBuildings = GetEntityQuery(ComponentType.ReadOnly<Renter>(), ComponentType.ReadOnly<Deleted>(), ComponentType.Exclude<Temp>());
		m_DeletedWorkplaces = GetEntityQuery(ComponentType.ReadOnly<Employee>(), ComponentType.ReadOnly<Deleted>(), ComponentType.Exclude<Temp>());
		m_DeletedCompanies = GetEntityQuery(ComponentType.ReadOnly<CompanyNotifications>(), ComponentType.ReadOnly<Deleted>(), ComponentType.Exclude<Temp>());
		m_NeedUpdateRenterQuery = GetEntityQuery(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<RentersUpdated>());
		m_BuildingParameterQuery = GetEntityQuery(ComponentType.ReadOnly<BuildingConfigurationData>());
		m_CompanyNotificationParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CompanyNotificationParameterData>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = default(JobHandle);
		if (!m_DeletedBuildings.IsEmptyIgnoreFilter)
		{
			jobHandle = JobChunkExtensions.ScheduleParallel(new RemovedPropertyJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_RenterBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_TouristHouseholds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_LodgingProviders = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_LodgingProvider_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
			}, m_DeletedBuildings, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		}
		JobHandle jobHandle2 = default(JobHandle);
		if (!m_DeletedWorkplaces.IsEmptyIgnoreFilter)
		{
			NativeQueue<TriggerAction> nativeQueue = (m_TriggerSystem.Enabled ? m_TriggerSystem.CreateActionBuffer() : new NativeQueue<TriggerAction>(Allocator.TempJob));
			jobHandle2 = JobChunkExtensions.ScheduleParallel(new RemovedWorkplaceJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_EmployeeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_Employee_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_Purposes = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Workers = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_TriggerBuffer = nativeQueue.AsParallelWriter()
			}, m_DeletedWorkplaces, base.Dependency);
			m_ModificationBarrier.AddJobHandleForProducer(jobHandle2);
			if (m_TriggerSystem.Enabled)
			{
				m_TriggerSystem.AddActionBufferWriter(jobHandle2);
			}
			else
			{
				nativeQueue.Dispose(jobHandle2);
			}
		}
		JobHandle jobHandle3 = default(JobHandle);
		if (!m_DeletedCompanies.IsEmptyIgnoreFilter && !m_CompanyNotificationParameterQuery.IsEmptyIgnoreFilter)
		{
			jobHandle3 = JobChunkExtensions.ScheduleParallel(new RemovedCompanyJob
			{
				m_NotificationsType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_CompanyNotifications_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CompanyNotificationParameters = m_CompanyNotificationParameterQuery.GetSingleton<CompanyNotificationParameterData>(),
				m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
			}, m_DeletedCompanies, base.Dependency);
			m_IconCommandSystem.AddCommandBufferWriter(jobHandle3);
		}
		JobHandle jobHandle4 = default(JobHandle);
		if (!m_NeedUpdateRenterQuery.IsEmptyIgnoreFilter)
		{
			jobHandle4 = JobChunkExtensions.Schedule(new RentersUpdateJob
			{
				m_RentersUpdatedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_RentersUpdated_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RW_BufferLookup, ref base.CheckedStateRef),
				m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Deleteds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Abandoneds = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Abandoned_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Parks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
				m_MovingAways = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Agents_MovingAway_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RW_ComponentLookup, ref base.CheckedStateRef),
				m_BuildingConfigurationData = m_BuildingParameterQuery.GetSingleton<BuildingConfigurationData>(),
				m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer()
			}, m_NeedUpdateRenterQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle));
			m_IconCommandSystem.AddCommandBufferWriter(jobHandle4);
		}
		base.Dependency = JobHandle.CombineDependencies(jobHandle, jobHandle2, JobHandle.CombineDependencies(jobHandle3, jobHandle4));
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
	public RemovedSystem()
	{
	}
}
