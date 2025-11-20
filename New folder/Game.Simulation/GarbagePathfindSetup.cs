using Game.Areas;
using Game.Buildings;
using Game.City;
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
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Simulation;

public struct GarbagePathfindSetup
{
	[BurstCompile]
	private struct SetupGarbageCollectorsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.GarbageFacility> m_GarbageFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Vehicles.GarbageTruck> m_GarbageTruckType;

		[ReadOnly]
		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public BufferTypeHandle<PathElement> m_PathElementType;

		[ReadOnly]
		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<GarbageCollectionRequest> m_GarbageCollectionRequestData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<Game.City.City> m_CityData;

		[ReadOnly]
		public Entity m_City;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			if (chunk.Has<Game.Objects.OutsideConnection>() && !CityUtils.CheckOption(m_CityData[m_City], CityOption.ImportOutsideServices))
			{
				return;
			}
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Game.Buildings.GarbageFacility> nativeArray2 = chunk.GetNativeArray(ref m_GarbageFacilityType);
			if (nativeArray2.Length != 0)
			{
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Game.Buildings.GarbageFacility garbageFacility = nativeArray2[i];
					if ((garbageFacility.m_Flags & (GarbageFacilityFlags.HasAvailableGarbageTrucks | GarbageFacilityFlags.HasAvailableSpace)) != (GarbageFacilityFlags.HasAvailableGarbageTrucks | GarbageFacilityFlags.HasAvailableSpace))
					{
						continue;
					}
					for (int j = 0; j < m_SetupData.Length; j++)
					{
						m_SetupData.GetItem(j, out var entity, out var owner, out var targetSeeker);
						GarbageCollectionRequest garbageCollectionRequest = default(GarbageCollectionRequest);
						if (m_GarbageCollectionRequestData.HasComponent(owner))
						{
							garbageCollectionRequest = m_GarbageCollectionRequestData[owner];
						}
						float num = targetSeeker.m_PathfindParameters.m_Weights.time * 10f;
						if ((garbageFacility.m_Flags & GarbageFacilityFlags.IndustrialWasteOnly) != 0)
						{
							if ((garbageCollectionRequest.m_Flags & GarbageCollectionRequestFlags.IndustrialWaste) == 0)
							{
								continue;
							}
						}
						else if ((garbageCollectionRequest.m_Flags & GarbageCollectionRequestFlags.IndustrialWaste) != 0)
						{
							num += 30f;
						}
						Entity entity2 = nativeArray[i];
						if (AreaUtils.CheckServiceDistrict(entity, entity2, m_ServiceDistricts))
						{
							targetSeeker.FindTargets(entity2, num);
						}
					}
				}
				return;
			}
			NativeArray<Game.Vehicles.GarbageTruck> nativeArray3 = chunk.GetNativeArray(ref m_GarbageTruckType);
			if (nativeArray3.Length == 0)
			{
				return;
			}
			NativeArray<PathOwner> nativeArray4 = chunk.GetNativeArray(ref m_PathOwnerType);
			NativeArray<Owner> nativeArray5 = chunk.GetNativeArray(ref m_OwnerType);
			BufferAccessor<PathElement> bufferAccessor = chunk.GetBufferAccessor(ref m_PathElementType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			for (int k = 0; k < nativeArray3.Length; k++)
			{
				Entity entity3 = nativeArray[k];
				Game.Vehicles.GarbageTruck garbageTruck = nativeArray3[k];
				if ((garbageTruck.m_State & (GarbageTruckFlags.Disabled | GarbageTruckFlags.EstimatedFull)) != 0)
				{
					continue;
				}
				for (int l = 0; l < m_SetupData.Length; l++)
				{
					m_SetupData.GetItem(l, out var entity4, out var owner2, out var targetSeeker2);
					float num2 = 0f;
					if (nativeArray5.Length != 0)
					{
						if (!AreaUtils.CheckServiceDistrict(entity4, nativeArray5[k].m_Owner, m_ServiceDistricts))
						{
							continue;
						}
						if (m_OutsideConnections.HasComponent(nativeArray5[k].m_Owner))
						{
							num2 += 30f;
						}
					}
					m_GarbageCollectionRequestData.TryGetComponent(owner2, out var componentData);
					if ((garbageTruck.m_State & GarbageTruckFlags.IndustrialWasteOnly) != 0)
					{
						if ((componentData.m_Flags & GarbageCollectionRequestFlags.IndustrialWaste) == 0)
						{
							continue;
						}
					}
					else if ((componentData.m_Flags & GarbageCollectionRequestFlags.IndustrialWaste) != 0)
					{
						num2 += 30f;
					}
					if ((garbageTruck.m_State & GarbageTruckFlags.Returning) != 0 || nativeArray4.Length == 0)
					{
						targetSeeker2.FindTargets(entity3, num2);
						continue;
					}
					PathOwner pathOwner = nativeArray4[k];
					DynamicBuffer<ServiceDispatch> dynamicBuffer = bufferAccessor2[k];
					int num3 = math.min(garbageTruck.m_RequestCount, dynamicBuffer.Length);
					PathElement pathElement = default(PathElement);
					bool flag = false;
					if (num3 >= 1)
					{
						DynamicBuffer<PathElement> dynamicBuffer2 = bufferAccessor[k];
						if (pathOwner.m_ElementIndex < dynamicBuffer2.Length)
						{
							num2 += (float)(dynamicBuffer2.Length - pathOwner.m_ElementIndex) * garbageTruck.m_PathElementTime * targetSeeker2.m_PathfindParameters.m_Weights.time;
							pathElement = dynamicBuffer2[dynamicBuffer2.Length - 1];
							flag = true;
						}
					}
					for (int m = 1; m < num3; m++)
					{
						Entity request = dynamicBuffer[m].m_Request;
						if (m_PathInformationData.TryGetComponent(request, out var componentData2))
						{
							num2 += componentData2.m_Duration * targetSeeker2.m_PathfindParameters.m_Weights.time;
						}
						if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
						{
							pathElement = bufferData[bufferData.Length - 1];
							flag = true;
						}
					}
					if (flag)
					{
						targetSeeker2.m_Buffer.Enqueue(new PathTarget(entity3, pathElement.m_Target, pathElement.m_TargetDelta.y, num2));
					}
					else
					{
						targetSeeker2.FindTargets(entity3, entity3, num2, EdgeFlags.DefaultMask, allowAccessRestriction: true, num3 >= 1);
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
	private struct SetupGarbageTransferJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.GarbageFacility> m_GarbageFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public BufferTypeHandle<Resources> m_ResourcesType;

		[ReadOnly]
		public BufferTypeHandle<TradeCost> m_TradeCostType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_StorageLimitData;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			if (chunk.Has(ref m_OutsideConnectionType))
			{
				return;
			}
			NativeArray<Game.Buildings.GarbageFacility> nativeArray2 = chunk.GetNativeArray(ref m_GarbageFacilityType);
			if (nativeArray2.Length == 0)
			{
				return;
			}
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Game.Buildings.GarbageFacility garbageFacility = nativeArray2[i];
				for (int j = 0; j < m_SetupData.Length; j++)
				{
					m_SetupData.GetItem(j, out var _, out var targetSeeker);
					float value = targetSeeker.m_SetupQueueTarget.m_Value2;
					if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.RequireTransport) != SetupTargetFlags.None && (garbageFacility.m_Flags & GarbageFacilityFlags.HasAvailableDeliveryTrucks) == 0)
					{
						continue;
					}
					float num = math.max(0f, 1f - value);
					if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Import) != SetupTargetFlags.None)
					{
						if (garbageFacility.m_AcceptGarbagePriority > num)
						{
							Entity entity2 = nativeArray[i];
							targetSeeker.FindTargets(entity2, 120f * math.saturate(1f - (garbageFacility.m_AcceptGarbagePriority - num)));
						}
					}
					else if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Export) != SetupTargetFlags.None && garbageFacility.m_DeliverGarbagePriority > num)
					{
						Entity entity3 = nativeArray[i];
						targetSeeker.FindTargets(entity3, 120f * math.saturate(1f - (garbageFacility.m_DeliverGarbagePriority - num)));
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
	private struct GarbageCollectorRequestsJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

		[ReadOnly]
		public ComponentTypeHandle<GarbageCollectionRequest> m_GarbageCollectionRequestType;

		[ReadOnly]
		public ComponentLookup<GarbageCollectionRequest> m_GarbageCollectionRequestData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.GarbageTruck> m_GarbageTruckData;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<ServiceRequest> nativeArray2 = chunk.GetNativeArray(ref m_ServiceRequestType);
			NativeArray<GarbageCollectionRequest> nativeArray3 = chunk.GetNativeArray(ref m_GarbageCollectionRequestType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var owner, out var targetSeeker);
				if (!m_GarbageCollectionRequestData.TryGetComponent(owner, out var componentData))
				{
					continue;
				}
				Random random = targetSeeker.m_RandomSeed.GetRandom(unfilteredChunkIndex);
				Entity service = Entity.Null;
				if (m_GarbageTruckData.HasComponent(componentData.m_Target))
				{
					if (targetSeeker.m_Owner.TryGetComponent(componentData.m_Target, out var componentData2))
					{
						service = componentData2.m_Owner;
					}
				}
				else
				{
					if (!targetSeeker.m_PrefabRef.HasComponent(componentData.m_Target))
					{
						continue;
					}
					service = componentData.m_Target;
				}
				for (int j = 0; j < nativeArray3.Length; j++)
				{
					if ((nativeArray2[j].m_Flags & ServiceRequestFlags.Reversed) != 0)
					{
						continue;
					}
					GarbageCollectionRequest garbageCollectionRequest = nativeArray3[j];
					Entity district = Entity.Null;
					if (m_CurrentDistrictData.HasComponent(garbageCollectionRequest.m_Target))
					{
						district = m_CurrentDistrictData[garbageCollectionRequest.m_Target].m_District;
					}
					float num = random.NextFloat(30f);
					if ((componentData.m_Flags & GarbageCollectionRequestFlags.IndustrialWaste) != 0)
					{
						if ((garbageCollectionRequest.m_Flags & GarbageCollectionRequestFlags.IndustrialWaste) == 0)
						{
							continue;
						}
					}
					else if ((garbageCollectionRequest.m_Flags & GarbageCollectionRequestFlags.IndustrialWaste) != 0)
					{
						num += 30f;
					}
					if (AreaUtils.CheckServiceDistrict(district, service, m_ServiceDistricts))
					{
						targetSeeker.FindTargets(nativeArray[j], garbageCollectionRequest.m_Target, num, EdgeFlags.DefaultMask, allowAccessRestriction: true, navigationEnd: false);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private EntityQuery m_GarbageCollectorQuery;

	private EntityQuery m_GarbageTransferQuery;

	private EntityQuery m_GarbageCollectionRequestQuery;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<PathOwner> m_PathOwnerType;

	private ComponentTypeHandle<Owner> m_OwnerType;

	private ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

	private ComponentTypeHandle<ServiceRequest> m_ServiceRequestType;

	private ComponentTypeHandle<GarbageCollectionRequest> m_GarbageCollectionRequestType;

	private ComponentTypeHandle<Game.Buildings.GarbageFacility> m_GarbageFacilityType;

	private ComponentTypeHandle<Game.Vehicles.GarbageTruck> m_GarbageTruckType;

	private ComponentTypeHandle<PrefabRef> m_PrefabRefType;

	private BufferTypeHandle<PathElement> m_PathElementType;

	private BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

	private BufferTypeHandle<Resources> m_ResourcesType;

	private BufferTypeHandle<TradeCost> m_TradeCostType;

	private BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

	private ComponentLookup<PathInformation> m_PathInformationData;

	private ComponentLookup<GarbageCollectionRequest> m_GarbageCollectionRequestData;

	private ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

	private ComponentLookup<Game.City.City> m_CityData;

	private ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

	private ComponentLookup<Game.Vehicles.GarbageTruck> m_GarbageTruckData;

	private ComponentLookup<StorageLimitData> m_StorageLimitData;

	private ComponentLookup<StorageCompanyData> m_StorageCompanyData;

	private BufferLookup<PathElement> m_PathElements;

	private BufferLookup<ServiceDistrict> m_ServiceDistricts;

	private CitySystem m_CitySystem;

	public GarbagePathfindSetup(PathfindSetupSystem system)
	{
		m_GarbageCollectorQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Buildings.GarbageFacility>(),
				ComponentType.ReadOnly<Game.Vehicles.GarbageTruck>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_GarbageTransferQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.GarbageFacility>(),
				ComponentType.ReadOnly<ServiceDispatch>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<Resources>(),
				ComponentType.ReadOnly<TradeCost>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_GarbageCollectionRequestQuery = system.GetSetupQuery(ComponentType.ReadOnly<GarbageCollectionRequest>(), ComponentType.Exclude<Dispatched>(), ComponentType.Exclude<PathInformation>());
		m_EntityType = system.GetEntityTypeHandle();
		m_PathOwnerType = system.GetComponentTypeHandle<PathOwner>(isReadOnly: true);
		m_OwnerType = system.GetComponentTypeHandle<Owner>(isReadOnly: true);
		m_OutsideConnectionType = system.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_ServiceRequestType = system.GetComponentTypeHandle<ServiceRequest>(isReadOnly: true);
		m_GarbageCollectionRequestType = system.GetComponentTypeHandle<GarbageCollectionRequest>(isReadOnly: true);
		m_GarbageFacilityType = system.GetComponentTypeHandle<Game.Buildings.GarbageFacility>(isReadOnly: true);
		m_GarbageTruckType = system.GetComponentTypeHandle<Game.Vehicles.GarbageTruck>(isReadOnly: true);
		m_PrefabRefType = system.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		m_PathElementType = system.GetBufferTypeHandle<PathElement>(isReadOnly: true);
		m_ServiceDispatchType = system.GetBufferTypeHandle<ServiceDispatch>(isReadOnly: true);
		m_ResourcesType = system.GetBufferTypeHandle<Resources>(isReadOnly: true);
		m_TradeCostType = system.GetBufferTypeHandle<TradeCost>(isReadOnly: true);
		m_InstalledUpgradeType = system.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
		m_PathInformationData = system.GetComponentLookup<PathInformation>(isReadOnly: true);
		m_GarbageCollectionRequestData = system.GetComponentLookup<GarbageCollectionRequest>(isReadOnly: true);
		m_OutsideConnections = system.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_CurrentDistrictData = system.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
		m_GarbageTruckData = system.GetComponentLookup<Game.Vehicles.GarbageTruck>(isReadOnly: true);
		m_StorageLimitData = system.GetComponentLookup<StorageLimitData>(isReadOnly: true);
		m_StorageCompanyData = system.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
		m_CityData = system.GetComponentLookup<Game.City.City>(isReadOnly: true);
		m_PathElements = system.GetBufferLookup<PathElement>(isReadOnly: true);
		m_ServiceDistricts = system.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
		m_CitySystem = system.World.GetOrCreateSystemManaged<CitySystem>();
	}

	public JobHandle SetupGarbageCollector(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_GarbageFacilityType.Update(system);
		m_GarbageTruckType.Update(system);
		m_PathOwnerType.Update(system);
		m_OwnerType.Update(system);
		m_PathElementType.Update(system);
		m_ServiceDispatchType.Update(system);
		m_PathInformationData.Update(system);
		m_GarbageCollectionRequestData.Update(system);
		m_PathElements.Update(system);
		m_ServiceDistricts.Update(system);
		m_OutsideConnections.Update(system);
		m_CityData.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupGarbageCollectorsJob
		{
			m_EntityType = m_EntityType,
			m_GarbageFacilityType = m_GarbageFacilityType,
			m_GarbageTruckType = m_GarbageTruckType,
			m_PathOwnerType = m_PathOwnerType,
			m_OwnerType = m_OwnerType,
			m_PathElementType = m_PathElementType,
			m_ServiceDispatchType = m_ServiceDispatchType,
			m_PathInformationData = m_PathInformationData,
			m_GarbageCollectionRequestData = m_GarbageCollectionRequestData,
			m_PathElements = m_PathElements,
			m_ServiceDistricts = m_ServiceDistricts,
			m_OutsideConnections = m_OutsideConnections,
			m_CityData = m_CityData,
			m_City = m_CitySystem.City,
			m_SetupData = setupData
		}, m_GarbageCollectorQuery, inputDeps);
	}

	public JobHandle SetupGarbageTransfer(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_GarbageFacilityType.Update(system);
		m_PrefabRefType.Update(system);
		m_OutsideConnectionType.Update(system);
		m_ResourcesType.Update(system);
		m_TradeCostType.Update(system);
		m_InstalledUpgradeType.Update(system);
		m_StorageCompanyData.Update(system);
		m_StorageLimitData.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupGarbageTransferJob
		{
			m_EntityType = m_EntityType,
			m_GarbageFacilityType = m_GarbageFacilityType,
			m_PrefabRefType = m_PrefabRefType,
			m_OutsideConnectionType = m_OutsideConnectionType,
			m_ResourcesType = m_ResourcesType,
			m_TradeCostType = m_TradeCostType,
			m_InstalledUpgradeType = m_InstalledUpgradeType,
			m_StorageCompanyData = m_StorageCompanyData,
			m_StorageLimitData = m_StorageLimitData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_GarbageTransferQuery, inputDeps);
	}

	public JobHandle SetupGarbageCollectorRequest(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_ServiceRequestType.Update(system);
		m_GarbageCollectionRequestType.Update(system);
		m_GarbageCollectionRequestData.Update(system);
		m_CurrentDistrictData.Update(system);
		m_GarbageTruckData.Update(system);
		m_ServiceDistricts.Update(system);
		return JobChunkExtensions.ScheduleParallel(new GarbageCollectorRequestsJob
		{
			m_EntityType = m_EntityType,
			m_ServiceRequestType = m_ServiceRequestType,
			m_GarbageCollectionRequestType = m_GarbageCollectionRequestType,
			m_GarbageCollectionRequestData = m_GarbageCollectionRequestData,
			m_CurrentDistrictData = m_CurrentDistrictData,
			m_GarbageTruckData = m_GarbageTruckData,
			m_ServiceDistricts = m_ServiceDistricts,
			m_SetupData = setupData
		}, m_GarbageCollectionRequestQuery, inputDeps);
	}
}
