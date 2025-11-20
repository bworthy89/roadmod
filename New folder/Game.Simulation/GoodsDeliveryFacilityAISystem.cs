using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GoodsDeliveryFacilityAISystem : GameSystemBase
{
	[BurstCompile]
	private struct GoodsDeliveryFacilityTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		public BufferTypeHandle<Resources> m_ResourcesType;

		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryRequest> m_GoodsDeliveryRequests;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformations;

		[ReadOnly]
		public ComponentLookup<Transform> m_Transforms;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefs;

		[ReadOnly]
		public ComponentLookup<DeliveryTruckData> m_DeliveryTruckDatas;

		[ReadOnly]
		public ComponentLookup<ObjectData> m_ObjectDatas;

		[ReadOnly]
		public ComponentLookup<TransportCompanyData> m_TransportCompanyDatas;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElementBufs;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElementBufs;

		[ReadOnly]
		public BufferLookup<Efficiency> m_EfficiencyBufs;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgradeBufs;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		[ReadOnly]
		public PostConfigurationData m_PostConfigurationData;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		[ReadOnly]
		public EntityArchetype m_PostVanRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_MailTransferRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingCarAddTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<OwnedVehicle> bufferAccessor = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			BufferAccessor<Resources> bufferAccessor3 = chunk.GetBufferAccessor(ref m_ResourcesType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Entity buildingEntity = entity;
				if (chunk.Has<CompanyData>())
				{
					if (!m_PropertyRenters.HasComponent(entity) || !(m_PropertyRenters[entity].m_Property != Entity.Null))
					{
						continue;
					}
					buildingEntity = m_PropertyRenters[entity].m_Property;
				}
				DynamicBuffer<ServiceDispatch> serviceDispatches = bufferAccessor2[i];
				if (serviceDispatches.Length > 0)
				{
					DynamicBuffer<OwnedVehicle> ownedVehicles = bufferAccessor[i];
					DynamicBuffer<Resources> resources = bufferAccessor3[i];
					Tick(unfilteredChunkIndex, entity, buildingEntity, ref random, ownedVehicles, serviceDispatches, resources);
				}
			}
		}

		private void Tick(int jobIndex, Entity companyEntity, Entity buildingEntity, ref Random random, DynamicBuffer<OwnedVehicle> ownedVehicles, DynamicBuffer<ServiceDispatch> serviceDispatches, DynamicBuffer<Resources> resources)
		{
			int transportCompanyAvailableVehicles = VehicleUtils.GetTransportCompanyAvailableVehicles(companyEntity, ref m_EfficiencyBufs, ref m_PrefabRefs, ref m_TransportCompanyDatas, ref m_InstalledUpgradeBufs);
			transportCompanyAvailableVehicles -= ownedVehicles.Length;
			for (int i = 0; i < serviceDispatches.Length; i++)
			{
				Entity request = serviceDispatches[i].m_Request;
				if (m_GoodsDeliveryRequests.HasComponent(request))
				{
					int resources2 = EconomyUtils.GetResources(m_GoodsDeliveryRequests[request].m_Resource, resources);
					if (resources2 <= m_GoodsDeliveryRequests[request].m_Amount)
					{
						break;
					}
					TrySpawnDeliveryTruck(jobIndex, ref random, companyEntity, buildingEntity, request, resources, resources2, ref transportCompanyAvailableVehicles);
					serviceDispatches.RemoveAt(i--);
				}
			}
		}

		private bool TrySpawnDeliveryTruck(int jobIndex, ref Random random, Entity companyEntity, Entity buildingEntity, Entity requestEntity, DynamicBuffer<Resources> resources, int availableResourceAmount, ref int availableDeliveryTrucks)
		{
			if (availableDeliveryTrucks <= 0)
			{
				return false;
			}
			GoodsDeliveryRequest goodsDeliveryRequest = m_GoodsDeliveryRequests[requestEntity];
			PathInformation component = m_PathInformations[requestEntity];
			if (!m_PrefabRefs.HasComponent(component.m_Destination))
			{
				return false;
			}
			DeliveryTruckFlags state = DeliveryTruckFlags.Loaded | DeliveryTruckFlags.UpkeepDelivery;
			Resource resource = goodsDeliveryRequest.m_Resource;
			m_DeliveryTruckSelectData.GetCapacityRange(resource, out var _, out var max);
			int amount = math.min(availableResourceAmount, max);
			int returnAmount = 0;
			Entity entity = m_DeliveryTruckSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, ref m_DeliveryTruckDatas, ref m_ObjectDatas, resource, Resource.NoResource, ref amount, ref returnAmount, m_Transforms[buildingEntity], companyEntity, state);
			if (entity != Entity.Null)
			{
				if (amount > 0)
				{
					EconomyUtils.AddResources(resource, -amount, resources);
				}
				availableDeliveryTrucks--;
				m_CommandBuffer.SetComponent(jobIndex, entity, new Target(component.m_Destination));
				m_CommandBuffer.AddComponent(jobIndex, entity, new Owner(companyEntity));
				m_CommandBuffer.AddComponent(jobIndex, entity, default(GoodsDeliveryVehicle));
				m_CommandBuffer.SetBuffer<ServiceDispatch>(jobIndex, entity).Add(new ServiceDispatch(requestEntity));
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(requestEntity, entity, completed: false));
				if (m_PathElementBufs.HasBuffer(requestEntity))
				{
					DynamicBuffer<PathElement> sourceElements = m_PathElementBufs[requestEntity];
					if (sourceElements.Length != 0)
					{
						DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity);
						PathUtils.CopyPath(sourceElements, default(PathOwner), 0, targetElements);
						m_CommandBuffer.SetComponent(jobIndex, entity, new PathOwner(PathFlags.Updated));
						m_CommandBuffer.SetComponent(jobIndex, entity, component);
					}
				}
				return true;
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<GoodsDeliveryRequest> __Game_Simulation_GoodsDeliveryRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DeliveryTruckData> __Game_Prefabs_DeliveryTruckData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectData> __Game_Prefabs_ObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportCompanyData> __Game_Companies_TransportCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
			__Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Simulation_GoodsDeliveryRequest_RO_ComponentLookup = state.GetComponentLookup<GoodsDeliveryRequest>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup = state.GetComponentLookup<DeliveryTruckData>(isReadOnly: true);
			__Game_Prefabs_ObjectData_RO_ComponentLookup = state.GetComponentLookup<ObjectData>(isReadOnly: true);
			__Game_Companies_TransportCompanyData_RO_ComponentLookup = state.GetComponentLookup<TransportCompanyData>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferLookup = state.GetBufferLookup<Efficiency>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 1024;

	private EntityQuery m_FacilityQuery;

	private EntityArchetype m_HandleRequestArchetype;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

	private SimulationSystem m_SimulationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private DeliveryTruckSelectData m_DeliveryTruckSelectData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 112;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_VehicleCapacitySystem = base.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_FacilityQuery = GetEntityQuery(ComponentType.ReadOnly<GoodsDeliveryFacility>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Event>());
		RequireForUpdate(m_FacilityQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new GoodsDeliveryFacilityTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourcesType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_GoodsDeliveryRequests = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_GoodsDeliveryRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenters = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Transforms = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DeliveryTruckDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DeliveryTruckData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_TransportCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathElementBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElementBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_EfficiencyBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgradeBufs = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_FacilityQuery, base.Dependency);
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
	public GoodsDeliveryFacilityAISystem()
	{
	}
}
