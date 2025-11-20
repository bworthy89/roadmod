using System.Runtime.CompilerServices;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class PersonalCarOwnerSystem : GameSystemBase
{
	[BurstCompile]
	private struct PersonalCarOwnerJob : IJobChunk
	{
		[ReadOnly]
		public SharedComponentTypeHandle<UpdateFrame> m_UpdateFrameType;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Bicycle> m_BicycleType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.PersonalCar> m_PersonalCarType;

		[ReadOnly]
		public ComponentLookup<BicycleOwner> m_BicycleOwners;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public uint m_UpdateFrameIndex;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.GetSharedComponent(m_UpdateFrameType).m_Index != m_UpdateFrameIndex)
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutElementType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Game.Vehicles.PersonalCar> nativeArray3 = chunk.GetNativeArray(ref m_PersonalCarType);
			bool flag = chunk.Has(ref m_BicycleType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				DynamicBuffer<LayoutElement> layout = default(DynamicBuffer<LayoutElement>);
				if (bufferAccessor.Length != 0)
				{
					layout = bufferAccessor[i];
				}
				if (flag)
				{
					Game.Vehicles.PersonalCar personalCar = nativeArray3[i];
					if (!m_BicycleOwners.TryGetComponent(personalCar.m_Keeper, out var componentData) || componentData.m_Bicycle != entity)
					{
						VehicleUtils.DeleteVehicle(m_CommandBuffer, unfilteredChunkIndex, entity, layout);
					}
				}
				else if (nativeArray2.IsCreated)
				{
					bool flag2 = false;
					Entity owner = nativeArray2[i].m_Owner;
					if (m_OwnedVehicles.HasBuffer(owner))
					{
						DynamicBuffer<OwnedVehicle> dynamicBuffer = m_OwnedVehicles[owner];
						for (int j = 0; j < dynamicBuffer.Length; j++)
						{
							if (dynamicBuffer[j].m_Vehicle == entity)
							{
								flag2 = true;
							}
						}
					}
					if (!flag2)
					{
						VehicleUtils.DeleteVehicle(m_CommandBuffer, unfilteredChunkIndex, entity, layout);
					}
				}
				else
				{
					VehicleUtils.DeleteVehicle(m_CommandBuffer, unfilteredChunkIndex, entity, layout);
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
		public SharedComponentTypeHandle<UpdateFrame> __Game_Simulation_UpdateFrame_SharedComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Bicycle> __Game_Vehicles_Bicycle_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.PersonalCar> __Game_Vehicles_PersonalCar_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<BicycleOwner> __Game_Citizens_BicycleOwner_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Simulation_UpdateFrame_SharedComponentTypeHandle = state.GetSharedComponentTypeHandle<UpdateFrame>();
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Vehicles_Bicycle_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Bicycle>(isReadOnly: true);
			__Game_Vehicles_PersonalCar_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.PersonalCar>(isReadOnly: true);
			__Game_Citizens_BicycleOwner_RO_ComponentLookup = state.GetComponentLookup<BicycleOwner>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferLookup = state.GetBufferLookup<OwnedVehicle>(isReadOnly: true);
		}
	}

	public static readonly int kUpdateInterval = 1024;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_VehicleGroup;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return kUpdateInterval;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_VehicleGroup = GetEntityQuery(ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.ReadWrite<Game.Vehicles.PersonalCar>(), ComponentType.Exclude<CarTrailer>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<OutOfControl>(), ComponentType.Exclude<Destroyed>());
		RequireForUpdate(m_VehicleGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrameIndex = m_SimulationSystem.frameIndex / (uint)kUpdateInterval % 16;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new PersonalCarOwnerJob
		{
			m_UpdateFrameType = InternalCompilerInterface.GetSharedComponentTypeHandle(ref __TypeHandle.__Game_Simulation_UpdateFrame_SharedComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BicycleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Bicycle_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PersonalCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PersonalCar_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BicycleOwners = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_BicycleOwner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnedVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_UpdateFrameIndex = updateFrameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_VehicleGroup, base.Dependency);
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
	public PersonalCarOwnerSystem()
	{
	}
}
