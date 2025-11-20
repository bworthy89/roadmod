using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class HouseholdPetBehaviorSystem : GameSystemBase
{
	[BurstCompile]
	private struct HouseholdPetTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<HouseholdPet> m_HouseholdPetType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentTransport> m_CurrentTransportType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> m_CurrentBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> m_DeletedData;

		[ReadOnly]
		public ComponentLookup<Household> m_HouseholdData;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholdData;

		[ReadOnly]
		public ComponentLookup<GroupMember> m_GroupMemberData;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterData;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<HouseholdPet> nativeArray2 = chunk.GetNativeArray(ref m_HouseholdPetType);
			NativeArray<CurrentTransport> nativeArray3 = chunk.GetNativeArray(ref m_CurrentTransportType);
			NativeArray<CurrentBuilding> nativeArray4 = chunk.GetNativeArray(ref m_CurrentBuildingType);
			bool flag = !chunk.Has(ref m_TargetType) && (!nativeArray3.IsCreated || nativeArray4.IsCreated);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				HouseholdPet householdPet = nativeArray2[i];
				if (!m_HouseholdData.HasComponent(householdPet.m_Household))
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], default(Deleted));
				}
				else
				{
					if (!flag || (CollectionUtils.TryGet(nativeArray3, i, out var value) && m_GroupMemberData.HasComponent(value.m_CurrentTransport)) || (CollectionUtils.TryGet(nativeArray4, i, out var value2) && ValidateLocation(householdPet.m_Household, value2.m_CurrentBuilding)))
					{
						continue;
					}
					if (value.m_CurrentTransport != Entity.Null && !m_DeletedData.HasComponent(value.m_CurrentTransport))
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, value.m_CurrentTransport, default(Deleted));
					}
					if (TryGetHome(householdPet.m_Household, out var home))
					{
						if (home != value2.m_CurrentBuilding)
						{
							value2.m_CurrentBuilding = home;
							m_CommandBuffer.AddComponent(unfilteredChunkIndex, nativeArray[i], value2);
						}
					}
					else if (nativeArray4.Length != 0)
					{
						m_CommandBuffer.RemoveComponent<CurrentBuilding>(unfilteredChunkIndex, nativeArray[i]);
					}
				}
			}
		}

		private bool ValidateLocation(Entity household, Entity building)
		{
			if (m_HouseholdCitizens.TryGetBuffer(household, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (m_CurrentBuildingData.TryGetComponent(bufferData[i].m_Citizen, out var componentData) && componentData.m_CurrentBuilding == building)
					{
						return true;
					}
				}
			}
			if (TryGetHome(household, out var home) && home == building)
			{
				return true;
			}
			return false;
		}

		private bool TryGetHome(Entity household, out Entity home)
		{
			if (m_PropertyRenterData.TryGetComponent(household, out var componentData) && m_EntityLookup.Exists(componentData.m_Property) && !m_DeletedData.HasComponent(componentData.m_Property))
			{
				home = componentData.m_Property;
				return true;
			}
			if (m_TouristHouseholdData.TryGetComponent(household, out var componentData2) && m_PropertyRenterData.TryGetComponent(componentData2.m_Hotel, out componentData) && m_EntityLookup.Exists(componentData.m_Property) && !m_DeletedData.HasComponent(componentData.m_Property))
			{
				home = componentData.m_Property;
				return true;
			}
			home = Entity.Null;
			return false;
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
		public ComponentTypeHandle<HouseholdPet> __Game_Citizens_HouseholdPet_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<Deleted> __Game_Common_Deleted_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Household> __Game_Citizens_Household_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GroupMember> __Game_Creatures_GroupMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Citizens_HouseholdPet_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HouseholdPet>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentTransport>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentBuilding>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Common_Deleted_RO_ComponentLookup = state.GetComponentLookup<Deleted>(isReadOnly: true);
			__Game_Citizens_Household_RO_ComponentLookup = state.GetComponentLookup<Household>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentLookup = state.GetComponentLookup<GroupMember>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
		}
	}

	private EntityQuery m_HouseholdPetQuery;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_HouseholdPetQuery = GetEntityQuery(ComponentType.ReadOnly<HouseholdPet>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_HouseholdPetQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameWithInterval = SimulationUtils.GetUpdateFrameWithInterval(m_SimulationSystem.frameIndex, (uint)GetUpdateInterval(SystemUpdatePhase.GameSimulation), 16);
		m_HouseholdPetQuery.SetSharedComponentFilter(new UpdateFrame(updateFrameWithInterval));
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new HouseholdPetTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_HouseholdPetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_HouseholdPet_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentTransportType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_DeletedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Deleted_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Household_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TouristHouseholdData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GroupMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_HouseholdPetQuery, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public HouseholdPetBehaviorSystem()
	{
	}
}
