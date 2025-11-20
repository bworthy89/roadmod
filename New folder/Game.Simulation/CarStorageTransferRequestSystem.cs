using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CarStorageTransferRequestSystem : GameSystemBase
{
	[BurstCompile]
	private struct TransferJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferLookup<GuestVehicle> m_GuestVehicles;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		public BufferTypeHandle<StorageTransferRequest> m_RequestType;

		public BufferTypeHandle<Resources> m_ResourceType;

		public BufferTypeHandle<TripNeeded> m_TripType;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_Properties;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			BufferAccessor<StorageTransferRequest> bufferAccessor = chunk.GetBufferAccessor(ref m_RequestType);
			BufferAccessor<Resources> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ResourceType);
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<TripNeeded> bufferAccessor3 = chunk.GetBufferAccessor(ref m_TripType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity destination = nativeArray[i];
				DynamicBuffer<StorageTransferRequest> dynamicBuffer = bufferAccessor[i];
				DynamicBuffer<Resources> resources = bufferAccessor2[i];
				DynamicBuffer<TripNeeded> dynamicBuffer2 = bufferAccessor3[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					StorageTransferRequest value = dynamicBuffer[j];
					int resources2 = EconomyUtils.GetResources(value.m_Resource, resources);
					int allBuyingResourcesTrucks = VehicleUtils.GetAllBuyingResourcesTrucks(destination, value.m_Resource, ref m_DeliveryTrucks, ref m_GuestVehicles, ref m_LayoutElements);
					resources2 -= allBuyingResourcesTrucks;
					if ((value.m_Flags & StorageTransferFlags.Incoming) != 0 || (value.m_Flags & StorageTransferFlags.Car) == 0 || resources2 < value.m_Amount)
					{
						continue;
					}
					if (m_Properties.HasComponent(value.m_Target) || m_OutsideConnections.HasComponent(value.m_Target))
					{
						if (value.m_Amount > 0 && resources2 > 0)
						{
							m_DeliveryTruckSelectData.GetCapacityRange(value.m_Resource, out var _, out var max);
							TripNeeded elem = new TripNeeded
							{
								m_TargetAgent = value.m_Target,
								m_Purpose = Purpose.StorageTransfer,
								m_Resource = value.m_Resource,
								m_Data = math.min(math.min(max, value.m_Amount), resources2)
							};
							dynamicBuffer2.Add(elem);
							EconomyUtils.AddResources(value.m_Resource, -elem.m_Data, resources);
							value.m_Amount -= elem.m_Data;
						}
						if (value.m_Amount <= 0)
						{
							dynamicBuffer.RemoveAt(j);
						}
						else
						{
							dynamicBuffer[j] = value;
						}
					}
					else
					{
						dynamicBuffer.RemoveAt(j);
					}
					break;
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
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<GuestVehicle> __Game_Vehicles_GuestVehicle_RO_BufferLookup;

		public BufferTypeHandle<StorageTransferRequest> __Game_Companies_StorageTransferRequest_RW_BufferTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		public BufferTypeHandle<TripNeeded> __Game_Citizens_TripNeeded_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_GuestVehicle_RO_BufferLookup = state.GetBufferLookup<GuestVehicle>(isReadOnly: true);
			__Game_Companies_StorageTransferRequest_RW_BufferTypeHandle = state.GetBufferTypeHandle<StorageTransferRequest>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
			__Game_Citizens_TripNeeded_RW_BufferTypeHandle = state.GetBufferTypeHandle<TripNeeded>();
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
		}
	}

	private EntityQuery m_TransferGroup;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

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
		m_VehicleCapacitySystem = base.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_TransferGroup = GetEntityQuery(ComponentType.ReadOnly<StorageTransferRequest>(), ComponentType.ReadOnly<Resources>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_TransferGroup);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		TransferJob jobData = new TransferJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_GuestVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_GuestVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_RequestType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Companies_StorageTransferRequest_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_TripType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Citizens_TripNeeded_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_Properties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_TransferGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(base.Dependency);
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
	public CarStorageTransferRequestSystem()
	{
	}
}
