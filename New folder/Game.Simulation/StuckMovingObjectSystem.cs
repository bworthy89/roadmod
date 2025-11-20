using System.Runtime.CompilerServices;
using Game.Common;
using Game.Creatures;
using Game.Pathfind;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class StuckMovingObjectSystem : GameSystemBase
{
	[BurstCompile]
	private struct StuckCheckJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Blocker> m_BlockerType;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> m_GroupMemberType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> m_CurrentVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<RideNeeder> m_RideNeederType;

		[ReadOnly]
		public ComponentTypeHandle<Target> m_TargetType;

		[ReadOnly]
		public ComponentTypeHandle<Car> m_CarType;

		[ReadOnly]
		public ComponentLookup<Blocker> m_BlockerData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> m_ParkedTrainData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<Dispatched> m_DispatchedData;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public ComponentTypeHandle<AnimalCurrentLane> m_AnimalCurrentLaneType;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<CarCurrentLane> m_CarCurrentLaneData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Blocker> nativeArray2 = chunk.GetNativeArray(ref m_BlockerType);
			NativeArray<GroupMember> nativeArray3 = chunk.GetNativeArray(ref m_GroupMemberType);
			NativeArray<CurrentVehicle> nativeArray4 = chunk.GetNativeArray(ref m_CurrentVehicleType);
			NativeArray<RideNeeder> nativeArray5 = chunk.GetNativeArray(ref m_RideNeederType);
			NativeArray<Target> nativeArray6 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray7 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<AnimalCurrentLane> nativeArray8 = chunk.GetNativeArray(ref m_AnimalCurrentLaneType);
			bool flag = chunk.Has(ref m_CarType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Blocker blocker = nativeArray2[i];
				if (blocker.m_Blocker == Entity.Null || blocker.m_Type == BlockerType.Temporary)
				{
					continue;
				}
				if (flag && blocker.m_Type == BlockerType.Crossing)
				{
					Entity entity = blocker.m_Blocker;
					if (m_ControllerData.TryGetComponent(entity, out var componentData))
					{
						entity = componentData.m_Controller;
					}
					if (m_CarCurrentLaneData.TryGetComponent(entity, out var componentData2))
					{
						componentData2.m_LaneFlags |= CarLaneFlags.RequestSpace;
						m_CarCurrentLaneData[entity] = componentData2;
					}
				}
				if (blocker.m_MaxSpeed >= 6)
				{
					continue;
				}
				Entity entity2 = nativeArray[i];
				Entity entity3 = Entity.Null;
				bool flag2;
				if (m_ParkedTrainData.HasComponent(blocker.m_Blocker) || (!flag && m_ParkedCarData.HasComponent(blocker.m_Blocker)))
				{
					flag2 = true;
				}
				else
				{
					if (nativeArray4.Length != 0)
					{
						entity3 = nativeArray4[i].m_Vehicle;
					}
					else if (nativeArray5.Length != 0)
					{
						RideNeeder rideNeeder = nativeArray5[i];
						if (m_DispatchedData.TryGetComponent(rideNeeder.m_RideRequest, out var componentData3))
						{
							entity3 = componentData3.m_Handler;
						}
					}
					else if (nativeArray3.Length != 0)
					{
						GroupMember groupMember = nativeArray3[i];
						if (m_CurrentVehicleData.TryGetComponent(groupMember.m_Leader, out var componentData4))
						{
							entity3 = componentData4.m_Vehicle;
						}
					}
					if (nativeArray6.Length != 0 && entity3 == Entity.Null)
					{
						entity3 = nativeArray6[i].m_Target;
					}
					if (entity3 != Entity.Null)
					{
						if (m_ControllerData.TryGetComponent(entity3, out var componentData5))
						{
							entity3 = componentData5.m_Controller;
						}
						flag2 = IsBlocked(entity2, entity3, blocker);
					}
					else
					{
						flag2 = IsBlocked(entity2, blocker);
					}
				}
				if (!flag2)
				{
					continue;
				}
				if (nativeArray7.Length != 0)
				{
					PathOwner value = nativeArray7[i];
					if ((value.m_State & PathFlags.Pending) == 0)
					{
						value.m_State |= PathFlags.Stuck;
						nativeArray7[i] = value;
					}
				}
				else if (nativeArray8.Length != 0)
				{
					AnimalCurrentLane value2 = nativeArray8[i];
					value2.m_Flags |= CreatureLaneFlags.Stuck;
					nativeArray8[i] = value2;
				}
			}
		}

		private bool IsBlocked(Entity entity, Blocker blocker)
		{
			int num = 0;
			if (m_ControllerData.TryGetComponent(blocker.m_Blocker, out var componentData))
			{
				blocker.m_Blocker = componentData.m_Controller;
			}
			Blocker componentData2;
			while (m_BlockerData.TryGetComponent(blocker.m_Blocker, out componentData2))
			{
				if ((long)(++num) == 100 || blocker.m_Blocker == entity)
				{
					return true;
				}
				blocker = componentData2;
				if (blocker.m_Blocker == Entity.Null)
				{
					return false;
				}
				if (blocker.m_Type == BlockerType.Temporary)
				{
					return false;
				}
				if (blocker.m_MaxSpeed >= 6)
				{
					return false;
				}
				if (m_ControllerData.TryGetComponent(blocker.m_Blocker, out componentData))
				{
					blocker.m_Blocker = componentData.m_Controller;
				}
			}
			return false;
		}

		private bool IsBlocked(Entity entity1, Entity entity2, Blocker blocker)
		{
			int num = 0;
			if (m_ControllerData.TryGetComponent(blocker.m_Blocker, out var componentData))
			{
				blocker.m_Blocker = componentData.m_Controller;
			}
			Blocker componentData2;
			while (m_BlockerData.TryGetComponent(blocker.m_Blocker, out componentData2))
			{
				if ((long)(++num) == 100 || blocker.m_Blocker == entity1 || blocker.m_Blocker == entity2)
				{
					return true;
				}
				blocker = componentData2;
				if (blocker.m_Blocker == Entity.Null)
				{
					return false;
				}
				if (blocker.m_Type == BlockerType.Temporary)
				{
					return false;
				}
				if (blocker.m_MaxSpeed >= 6)
				{
					return false;
				}
				if (m_ControllerData.TryGetComponent(blocker.m_Blocker, out componentData))
				{
					blocker.m_Blocker = componentData.m_Controller;
				}
			}
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
		public ComponentTypeHandle<Blocker> __Game_Vehicles_Blocker_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GroupMember> __Game_Creatures_GroupMember_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RideNeeder> __Game_Creatures_RideNeeder_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Target> __Game_Common_Target_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Car> __Game_Vehicles_Car_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Blocker> __Game_Vehicles_Blocker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Dispatched> __Game_Simulation_Dispatched_RO_ComponentLookup;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle;

		public ComponentLookup<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_Blocker_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Blocker>(isReadOnly: true);
			__Game_Creatures_GroupMember_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroupMember>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentVehicle>(isReadOnly: true);
			__Game_Creatures_RideNeeder_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RideNeeder>(isReadOnly: true);
			__Game_Common_Target_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Target>(isReadOnly: true);
			__Game_Vehicles_Car_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Car>(isReadOnly: true);
			__Game_Vehicles_Blocker_RO_ComponentLookup = state.GetComponentLookup<Blocker>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentLookup = state.GetComponentLookup<ParkedTrain>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Simulation_Dispatched_RO_ComponentLookup = state.GetComponentLookup<Dispatched>(isReadOnly: true);
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalCurrentLane>();
			__Game_Vehicles_CarCurrentLane_RW_ComponentLookup = state.GetComponentLookup<CarCurrentLane>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_ObjectQuery;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 4;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_ObjectQuery = GetEntityQuery(ComponentType.ReadOnly<Blocker>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_ObjectQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint index = (m_SimulationSystem.frameIndex >> 2) % 16;
		m_ObjectQuery.ResetFilter();
		m_ObjectQuery.SetSharedComponentFilter(new UpdateFrame(index));
		StuckCheckJob jobData = new StuckCheckJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_BlockerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Blocker_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GroupMemberType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_GroupMember_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RideNeederType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_RideNeeder_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Car_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BlockerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Blocker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedTrainData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DispatchedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_Dispatched_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarCurrentLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentLookup, ref base.CheckedStateRef)
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_ObjectQuery, base.Dependency);
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
	public StuckMovingObjectSystem()
	{
	}
}
