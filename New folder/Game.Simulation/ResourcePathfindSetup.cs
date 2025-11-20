using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Simulation;

public struct ResourcePathfindSetup
{
	[BurstCompile]
	private struct SetupResourceSellerJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

		[ReadOnly]
		public ComponentLookup<ServiceAvailable> m_ServiceAvailables;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenters;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_Resources;

		[ReadOnly]
		public BufferLookup<TradeCost> m_TradeCosts;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.CargoTransportStation> m_CargoTransportStations;

		[ReadOnly]
		public BufferTypeHandle<StorageTransferRequest> m_StorageTransferRequestType;

		[ReadOnly]
		public ComponentLookup<TransportCompanyData> m_TransportCompanyDatas;

		[ReadOnly]
		public BufferTypeHandle<TripNeeded> m_TripNeededType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public BufferLookup<GuestVehicle> m_GuestVehicleBufs;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElementBufs;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			BufferAccessor<StorageTransferRequest> bufferAccessor = chunk.GetBufferAccessor(ref m_StorageTransferRequestType);
			BufferAccessor<TripNeeded> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TripNeededType);
			BufferAccessor<OwnedVehicle> bufferAccessor3 = chunk.GetBufferAccessor(ref m_OwnedVehicles);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var entity, out var targetSeeker);
				Resource resource = targetSeeker.m_SetupQueueTarget.m_Resource;
				int value = targetSeeker.m_SetupQueueTarget.m_Value;
				if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.RequireTransport) != SetupTargetFlags.None && bufferAccessor3.Length == 0)
				{
					continue;
				}
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					Entity prefab = m_Prefabs[entity2].m_Prefab;
					int num = ((bufferAccessor.Length > 0) ? bufferAccessor[j].Length : 0);
					if (entity2.Equals(entity))
					{
						continue;
					}
					bool flag = m_OutsideConnections.HasComponent(entity2);
					bool flag2 = m_CargoTransportStations.HasComponent(entity2);
					bool flag3 = m_StorageCompanyDatas.HasComponent(prefab) && !flag2 && !flag;
					bool flag4 = m_ServiceAvailables.HasComponent(entity2);
					bool flag5 = m_IndustrialProcessDatas.HasComponent(prefab) && !flag4 && !flag3;
					bool flag6 = EconomyUtils.IsOfficeResource(resource);
					bool flag7 = (targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.BuildingUpkeep) != 0;
					if ((m_Buildings.HasComponent(entity2) && BuildingUtils.CheckOption(m_Buildings[entity2], BuildingOption.Inactive)) || ((flag4 || flag5) && (!m_PropertyRenters.HasComponent(entity2) || m_PropertyRenters[entity2].m_Property == Entity.Null)))
					{
						continue;
					}
					bool flag8 = false;
					if (flag6 && flag5 && (m_IndustrialProcessDatas[prefab].m_Output.m_Resource & resource) != Resource.NoResource)
					{
						flag8 = true;
					}
					else if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Commercial) != 0 && flag4 && (m_IndustrialProcessDatas[prefab].m_Output.m_Resource & resource) != Resource.NoResource)
					{
						flag8 = true;
					}
					else if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Industrial) != 0 && flag5 && (m_IndustrialProcessDatas[prefab].m_Output.m_Resource & resource) != Resource.NoResource)
					{
						flag8 = true;
					}
					else if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.Import) != SetupTargetFlags.None && (flag || flag2 || flag3) && (m_StorageCompanyDatas[prefab].m_StoredResources & resource) != Resource.NoResource)
					{
						flag8 = true;
					}
					if (!flag8 || (!flag && flag7 && bufferAccessor2.Length > 0 && bufferAccessor2[j].Length > 0))
					{
						continue;
					}
					int allBuyingResourcesTrucks = VehicleUtils.GetAllBuyingResourcesTrucks(entity2, resource, ref m_DeliveryTrucks, ref m_GuestVehicleBufs, ref m_LayoutElementBufs);
					int num2 = EconomyUtils.GetResources(resource, m_Resources[entity2]) - allBuyingResourcesTrucks;
					if (num2 <= 0)
					{
						continue;
					}
					float num3 = 0f;
					if (m_ServiceAvailables.HasComponent(entity2))
					{
						num3 -= (float)(math.min(num2, m_ServiceAvailables[entity2].m_ServiceAvailable) * 100);
					}
					else
					{
						if (!flag && num2 < value / 2)
						{
							continue;
						}
						float num4 = math.min(1f, (float)num2 * 1f / (float)value);
						num3 += 100f * (1f - num4);
						if (flag2)
						{
							if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.RequireTransport) != SetupTargetFlags.None)
							{
								if (!m_TransportCompanyDatas.HasComponent(prefab))
								{
									continue;
								}
								TransportCompanyData transportCompanyData = m_TransportCompanyDatas[prefab];
								if (bufferAccessor3[j].Length >= transportCompanyData.m_MaxTransports)
								{
									continue;
								}
							}
							if (bufferAccessor2.Length > 0 && bufferAccessor2[j].Length >= kCargoStationMaxTripNeededQueue)
							{
								continue;
							}
							num3 += kCargoStationAmountBasedPenalty * (float)value;
							num3 += kCargoStationPerRequestPenalty * (float)num;
						}
						if (flag)
						{
							num3 += kOutsideConnectionAmountBasedPenalty * (float)value;
							if (flag7)
							{
								num3 += (float)random.NextInt(300);
							}
						}
					}
					if (m_TradeCosts.HasBuffer(entity2))
					{
						DynamicBuffer<TradeCost> costs = m_TradeCosts[entity2];
						num3 += EconomyUtils.GetTradeCost(resource, costs).m_BuyCost * (float)value * 0.01f;
					}
					targetSeeker.FindTargets(entity2, num3 * 100f);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct SetupResourceExportJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferTypeHandle<Game.Economy.Resources> m_ResourceType;

		[ReadOnly]
		public BufferTypeHandle<TradeCost> m_TradeCosts;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicles;

		[ReadOnly]
		public BufferTypeHandle<TripNeeded> m_TripNeededType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_Limits;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

		[ReadOnly]
		public ComponentLookup<TransportCompanyData> m_TransportCompanyData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_Properties;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.CargoTransportStation> m_CargoTransportStations;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportStation> m_TransportStations;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Game.Economy.Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceType);
			BufferAccessor<TradeCost> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TradeCosts);
			BufferAccessor<OwnedVehicle> bufferAccessor3 = chunk.GetBufferAccessor(ref m_OwnedVehicles);
			BufferAccessor<TripNeeded> bufferAccessor4 = chunk.GetBufferAccessor(ref m_TripNeededType);
			BufferAccessor<InstalledUpgrade> bufferAccessor5 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var _, out var targetSeeker);
				Resource resource = targetSeeker.m_SetupQueueTarget.m_Resource;
				int value = targetSeeker.m_SetupQueueTarget.m_Value;
				if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.RequireTransport) != SetupTargetFlags.None && bufferAccessor3.Length == 0)
				{
					continue;
				}
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					Entity prefab = nativeArray2[j].m_Prefab;
					if (m_Buildings.HasComponent(entity2) && BuildingUtils.CheckOption(m_Buildings[entity2], BuildingOption.Inactive))
					{
						continue;
					}
					bool flag = m_CargoTransportStations.HasComponent(entity2);
					if (flag && m_TransportStations.TryGetComponent(entity2, out var componentData) && (componentData.m_Flags & TransportStationFlags.TransportStopsActive) == 0)
					{
						continue;
					}
					int num = value;
					float num2 = 0.01f;
					if (m_Limits.HasComponent(prefab))
					{
						StorageLimitData data = m_Limits[prefab];
						if (bufferAccessor5.Length != 0)
						{
							UpgradeUtils.CombineStats(ref data, bufferAccessor5[j], ref targetSeeker.m_PrefabRef, ref m_Limits);
						}
						if (m_Properties.HasComponent(entity2))
						{
							Entity property = m_Properties[entity2].m_Property;
							if (m_Prefabs.HasComponent(property))
							{
								Entity prefab2 = m_Prefabs[property].m_Prefab;
								int adjustedLimitForWarehouse = data.GetAdjustedLimitForWarehouse(m_SpawnableBuildingData.HasComponent(prefab2) ? m_SpawnableBuildingData[prefab2] : new SpawnableBuildingData
								{
									m_Level = 1
								}, m_SpawnableBuildingData.HasComponent(prefab2) ? m_BuildingDatas[prefab2] : new BuildingData
								{
									m_LotSize = new int2(1, 1)
								});
								num = adjustedLimitForWarehouse - EconomyUtils.GetResources(resource, bufferAccessor[j]);
								num2 = (float)num / math.max(1f, adjustedLimitForWarehouse);
							}
						}
					}
					StorageCompanyData data2 = m_StorageCompanyDatas[prefab];
					if (bufferAccessor5.Length != 0)
					{
						UpgradeUtils.CombineStats(ref data2, bufferAccessor5[j], ref targetSeeker.m_PrefabRef, ref m_StorageCompanyDatas);
					}
					if ((resource & data2.m_StoredResources) == Resource.NoResource || num < value)
					{
						continue;
					}
					float num3 = 0f;
					if ((targetSeeker.m_SetupQueueTarget.m_Flags & SetupTargetFlags.RequireTransport) != SetupTargetFlags.None)
					{
						if (!m_TransportCompanyData.HasComponent(prefab))
						{
							continue;
						}
						TransportCompanyData transportCompanyData = m_TransportCompanyData[prefab];
						if (bufferAccessor3[j].Length >= transportCompanyData.m_MaxTransports)
						{
							continue;
						}
					}
					if (!flag || bufferAccessor4.Length <= 0 || bufferAccessor4[j].Length < kCargoStationMaxTripNeededQueue)
					{
						float num4 = EconomyUtils.GetTradeCost(resource, bufferAccessor2[j]).m_SellCost * (float)value * 0.01f;
						num4 += num3 * (float)kCargoStationVehiclePenalty;
						targetSeeker.FindTargets(entity2, math.max(0f, -2000f * num2 + num4));
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
	private struct SetupStorageTransferJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<Game.Economy.Resources> m_ResourceType;

		[ReadOnly]
		public BufferTypeHandle<TradeCost> m_TradeCostType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<StorageTransferRequest> m_StorageTransferRequestType;

		[ReadOnly]
		public BufferTypeHandle<TripNeeded> m_TripNeededType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.CargoTransportStation> m_CargoTransportStations;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_StorageLimits;

		[ReadOnly]
		public ComponentLookup<TransportCompanyData> m_TransportCompanyDatas;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.TransportStation> m_TransportStations;

		public PathfindSetupSystem.SetupData m_SetupData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Game.Economy.Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceType);
			BufferAccessor<TradeCost> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TradeCostType);
			BufferAccessor<InstalledUpgrade> bufferAccessor3 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<StorageTransferRequest> bufferAccessor4 = chunk.GetBufferAccessor(ref m_StorageTransferRequestType);
			BufferAccessor<OwnedVehicle> bufferAccessor5 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<TripNeeded> bufferAccessor6 = chunk.GetBufferAccessor(ref m_TripNeededType);
			for (int i = 0; i < m_SetupData.Length; i++)
			{
				m_SetupData.GetItem(i, out var entity, out var targetSeeker);
				Resource resource = targetSeeker.m_SetupQueueTarget.m_Resource;
				int value = targetSeeker.m_SetupQueueTarget.m_Value;
				float value2 = targetSeeker.m_SetupQueueTarget.m_Value2;
				long num = targetSeeker.m_SetupQueueTarget.m_Value3;
				long num2 = Mathf.RoundToInt(value2 * (float)num);
				bool flag = chunk.Has(ref m_OutsideConnectionType);
				switch (resource)
				{
				case Resource.LocalMail:
					if (value > 0 && flag)
					{
						continue;
					}
					break;
				case Resource.UnsortedMail:
				case Resource.OutgoingMail:
					if (value < 0 && flag)
					{
						continue;
					}
					break;
				}
				for (int j = 0; j < nativeArray.Length; j++)
				{
					Entity entity2 = nativeArray[j];
					if (entity2.Equals(entity))
					{
						continue;
					}
					bool flag2 = m_CargoTransportStations.HasComponent(entity2);
					bool flag3 = m_CargoTransportStations.HasComponent(entity);
					if ((flag2 && m_TransportStations.TryGetComponent(entity2, out var componentData) && (componentData.m_Flags & TransportStationFlags.TransportStopsActive) == 0) || (!flag3 && (chunk.Has<TrainStop>() || chunk.Has<ShipStop>() || chunk.Has<AirplaneStop>())) || (m_Buildings.HasComponent(entity2) && BuildingUtils.CheckOption(m_Buildings[entity2], BuildingOption.Inactive)))
					{
						continue;
					}
					Entity prefab = nativeArray2[j].m_Prefab;
					if (!m_StorageCompanyDatas.HasComponent(prefab))
					{
						continue;
					}
					StorageCompanyData data = m_StorageCompanyDatas[prefab];
					StorageLimitData data2 = m_StorageLimits[prefab];
					int num3 = (m_TransportCompanyDatas.HasComponent(prefab) ? m_TransportCompanyDatas[prefab].m_MaxTransports : 0);
					DynamicBuffer<Game.Economy.Resources> resources = bufferAccessor[j];
					DynamicBuffer<StorageTransferRequest> dynamicBuffer = bufferAccessor4[j];
					if (bufferAccessor3.Length != 0)
					{
						UpgradeUtils.CombineStats(ref data2, bufferAccessor3[j], ref targetSeeker.m_PrefabRef, ref m_StorageLimits);
						UpgradeUtils.CombineStats(ref data, bufferAccessor3[j], ref targetSeeker.m_PrefabRef, ref m_StorageCompanyDatas);
					}
					long num4 = EconomyUtils.GetResources(resource, resources);
					long num5 = 0L;
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						StorageTransferRequest storageTransferRequest = dynamicBuffer[k];
						if (storageTransferRequest.m_Resource == resource)
						{
							num5 += (((storageTransferRequest.m_Flags & StorageTransferFlags.Incoming) != 0) ? storageTransferRequest.m_Amount : (-storageTransferRequest.m_Amount));
						}
					}
					num4 += num5;
					int num6 = math.max(1, EconomyUtils.CountResources(data.m_StoredResources));
					long num7 = data2.m_Limit / num6;
					long num8 = value;
					if (flag2)
					{
						num8 = ((value <= 0) ? (-math.min(-value, num4)) : math.min(value, num7 - num4));
					}
					else if (!flag)
					{
						if (num7 + num > 0)
						{
							num8 = (num7 * num2 - num * num4) / (num7 + num);
							if ((value > 0 && num8 < 0) || (value < 0 && num8 > 0))
							{
								num8 = 0L;
							}
						}
						else
						{
							num8 = 0L;
						}
					}
					if (math.abs(num8) < 4000)
					{
						continue;
					}
					float num9 = ((value != 0) ? (1000f * (float)math.abs(num8 / value)) : 0f);
					DynamicBuffer<TradeCost> costs = bufferAccessor2[j];
					TradeCost tradeCost = EconomyUtils.GetTradeCost(resource, costs);
					float num10 = 0.01f * (float)value * math.max(0.1f, (value > 0) ? tradeCost.m_SellCost : (0f - tradeCost.m_BuyCost));
					if (flag2)
					{
						num10 += (float)dynamicBuffer.Length * kCargoStationPerRequestPenalty;
						if (bufferAccessor6.Length > 0 && bufferAccessor6[j].Length > kCargoStationMaxTripNeededQueue)
						{
							continue;
						}
						if (bufferAccessor5.Length > 0 && bufferAccessor5[j].Length >= num3)
						{
							num10 += 1f * (float)bufferAccessor5[j].Length / (float)num3 * (float)kCargoStationVehiclePenalty;
						}
					}
					if ((data.m_StoredResources & resource) != Resource.NoResource && num9 > 0f)
					{
						targetSeeker.FindTargets(entity2, num10 - num9);
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public static readonly float kOutsideConnectionAmountBasedPenalty = 0.03f;

	public static readonly float kCargoStationAmountBasedPenalty = 0.0001f;

	public static readonly float kCargoStationPerRequestPenalty = 0.0001f;

	public static readonly int kCargoStationVehiclePenalty = 5000;

	public static readonly int kCargoStationMaxRequestAmount = 5;

	public static readonly int kCargoStationMaxTripNeededQueue = 10;

	private EntityQuery m_ResourceSellerQuery;

	private EntityQuery m_ExportTargetQuery;

	private EntityQuery m_StorageQuery;

	private ResourceSystem m_ResourceSystem;

	private EntityTypeHandle m_EntityType;

	private ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

	private ComponentTypeHandle<PrefabRef> m_PrefabType;

	private BufferTypeHandle<TradeCost> m_TradeCostType;

	private BufferTypeHandle<StorageTransferRequest> m_StorageTransferRequestType;

	private BufferTypeHandle<Game.Economy.Resources> m_ResourceType;

	private BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

	private BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

	private BufferTypeHandle<TripNeeded> m_TripNeededType;

	private ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

	private ComponentLookup<ServiceCompanyData> m_ServiceCompanies;

	private ComponentLookup<ServiceAvailable> m_ServiceAvailables;

	private ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanys;

	private ComponentLookup<StorageLimitData> m_StorageLimits;

	private ComponentLookup<TransportCompanyData> m_TransportCompanyData;

	private ComponentLookup<Game.Buildings.CargoTransportStation> m_CargoTransportStations;

	private ComponentLookup<Game.Buildings.TransportStation> m_TransportStations;

	private ComponentLookup<PropertyRenter> m_PropertyRenters;

	private ComponentLookup<PrefabRef> m_Prefabs;

	private ComponentLookup<IndustrialProcessData> m_IndustrialProcessDatas;

	private ComponentLookup<ResourceData> m_ResourceDatas;

	private ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

	private ComponentLookup<SpawnableBuildingData> m_SpawnableBuildingDatas;

	private ComponentLookup<BuildingData> m_BuildingDatas;

	private ComponentLookup<Building> m_Buildings;

	private ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

	private BufferLookup<Game.Economy.Resources> m_Resources;

	private BufferLookup<TradeCost> m_TradeCosts;

	private BufferLookup<GuestVehicle> m_GuestVehicleBufs;

	private BufferLookup<LayoutElement> m_LayoutElementBufs;

	public ResourcePathfindSetup(PathfindSetupSystem system)
	{
		m_ResourceSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<ResourceSystem>();
		m_ResourceSellerQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<Game.Economy.Resources>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
				ComponentType.ReadOnly<Game.Buildings.CargoTransportStation>(),
				ComponentType.ReadOnly<ResourceSeller>()
			},
			None = new ComponentType[6]
			{
				ComponentType.ReadOnly<ShipStop>(),
				ComponentType.ReadOnly<AirplaneStop>(),
				ComponentType.ReadOnly<TrainStop>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_ExportTargetQuery = system.GetSetupQuery(ComponentType.ReadOnly<Game.Companies.StorageCompany>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Game.Economy.Resources>(), ComponentType.ReadOnly<TradeCost>(), ComponentType.Exclude<ShipStop>(), ComponentType.Exclude<AirplaneStop>(), ComponentType.Exclude<TrainStop>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Temp>());
		m_StorageQuery = system.GetSetupQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Game.Companies.StorageCompany>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Companies.ProcessingCompany>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_EntityType = system.GetEntityTypeHandle();
		m_OutsideConnectionType = system.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_PrefabType = system.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
		m_TradeCostType = system.GetBufferTypeHandle<TradeCost>(isReadOnly: true);
		m_StorageTransferRequestType = system.GetBufferTypeHandle<StorageTransferRequest>(isReadOnly: true);
		m_TripNeededType = system.GetBufferTypeHandle<TripNeeded>(isReadOnly: true);
		m_OwnedVehicleType = system.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
		m_ResourceType = system.GetBufferTypeHandle<Game.Economy.Resources>(isReadOnly: true);
		m_InstalledUpgradeType = system.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
		m_OutsideConnections = system.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
		m_ServiceCompanies = system.GetComponentLookup<ServiceCompanyData>(isReadOnly: true);
		m_ServiceAvailables = system.GetComponentLookup<ServiceAvailable>(isReadOnly: true);
		m_StorageCompanys = system.GetComponentLookup<Game.Companies.StorageCompany>(isReadOnly: true);
		m_StorageLimits = system.GetComponentLookup<StorageLimitData>(isReadOnly: true);
		m_TransportCompanyData = system.GetComponentLookup<TransportCompanyData>(isReadOnly: true);
		m_PropertyRenters = system.GetComponentLookup<PropertyRenter>(isReadOnly: true);
		m_Prefabs = system.GetComponentLookup<PrefabRef>(isReadOnly: true);
		m_IndustrialProcessDatas = system.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
		m_ResourceDatas = system.GetComponentLookup<ResourceData>(isReadOnly: true);
		m_StorageCompanyDatas = system.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
		m_SpawnableBuildingDatas = system.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
		m_BuildingDatas = system.GetComponentLookup<BuildingData>(isReadOnly: true);
		m_Buildings = system.GetComponentLookup<Building>(isReadOnly: true);
		m_DeliveryTrucks = system.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
		m_Resources = system.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
		m_TradeCosts = system.GetBufferLookup<TradeCost>(isReadOnly: true);
		m_GuestVehicleBufs = system.GetBufferLookup<GuestVehicle>(isReadOnly: true);
		m_LayoutElementBufs = system.GetBufferLookup<LayoutElement>(isReadOnly: true);
		m_CargoTransportStations = system.GetComponentLookup<Game.Buildings.CargoTransportStation>(isReadOnly: true);
		m_TransportStations = system.GetComponentLookup<Game.Buildings.TransportStation>(isReadOnly: true);
	}

	public JobHandle SetupResourceSeller(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_Resources.Update(system);
		m_IndustrialProcessDatas.Update(system);
		m_CargoTransportStations.Update(system);
		m_TransportStations.Update(system);
		m_StorageCompanyDatas.Update(system);
		m_PropertyRenters.Update(system);
		m_TradeCosts.Update(system);
		m_ServiceAvailables.Update(system);
		m_OutsideConnections.Update(system);
		m_StorageTransferRequestType.Update(system);
		m_TripNeededType.Update(system);
		m_Prefabs.Update(system);
		m_Buildings.Update(system);
		m_DeliveryTrucks.Update(system);
		m_GuestVehicleBufs.Update(system);
		m_LayoutElementBufs.Update(system);
		m_OwnedVehicleType.Update(system);
		m_TransportCompanyData.Update(system);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new SetupResourceSellerJob
		{
			m_EntityType = m_EntityType,
			m_OwnedVehicles = m_OwnedVehicleType,
			m_StorageTransferRequestType = m_StorageTransferRequestType,
			m_TripNeededType = m_TripNeededType,
			m_Resources = m_Resources,
			m_IndustrialProcessDatas = m_IndustrialProcessDatas,
			m_CargoTransportStations = m_CargoTransportStations,
			m_StorageCompanyDatas = m_StorageCompanyDatas,
			m_TransportCompanyDatas = m_TransportCompanyData,
			m_PropertyRenters = m_PropertyRenters,
			m_TradeCosts = m_TradeCosts,
			m_ServiceAvailables = m_ServiceAvailables,
			m_OutsideConnections = m_OutsideConnections,
			m_Prefabs = m_Prefabs,
			m_Buildings = m_Buildings,
			m_DeliveryTrucks = m_DeliveryTrucks,
			m_GuestVehicleBufs = m_GuestVehicleBufs,
			m_LayoutElementBufs = m_LayoutElementBufs,
			m_RandomSeed = RandomSeed.Next(),
			m_SetupData = setupData
		}, m_ResourceSellerQuery, inputDeps);
		m_ResourceSystem.AddPrefabsReader(jobHandle);
		return jobHandle;
	}

	public JobHandle SetupResourceExport(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_StorageLimits.Update(system);
		m_PrefabType.Update(system);
		m_ResourceType.Update(system);
		m_OwnedVehicleType.Update(system);
		m_TripNeededType.Update(system);
		m_TradeCostType.Update(system);
		m_InstalledUpgradeType.Update(system);
		m_StorageCompanyDatas.Update(system);
		m_TransportCompanyData.Update(system);
		m_BuildingDatas.Update(system);
		m_SpawnableBuildingDatas.Update(system);
		m_Prefabs.Update(system);
		m_PropertyRenters.Update(system);
		m_CargoTransportStations.Update(system);
		m_TransportStations.Update(system);
		m_Buildings.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupResourceExportJob
		{
			m_EntityType = m_EntityType,
			m_Limits = m_StorageLimits,
			m_PrefabType = m_PrefabType,
			m_ResourceType = m_ResourceType,
			m_OwnedVehicles = m_OwnedVehicleType,
			m_TripNeededType = m_TripNeededType,
			m_TradeCosts = m_TradeCostType,
			m_InstalledUpgradeType = m_InstalledUpgradeType,
			m_StorageCompanyDatas = m_StorageCompanyDatas,
			m_TransportCompanyData = m_TransportCompanyData,
			m_BuildingDatas = m_BuildingDatas,
			m_SpawnableBuildingData = m_SpawnableBuildingDatas,
			m_Prefabs = m_Prefabs,
			m_Properties = m_PropertyRenters,
			m_CargoTransportStations = m_CargoTransportStations,
			m_TransportStations = m_TransportStations,
			m_Buildings = m_Buildings,
			m_SetupData = setupData
		}, m_ExportTargetQuery, inputDeps);
	}

	public JobHandle SetupStorageTransfer(PathfindSetupSystem system, PathfindSetupSystem.SetupData setupData, JobHandle inputDeps)
	{
		m_EntityType.Update(system);
		m_OutsideConnectionType.Update(system);
		m_CargoTransportStations.Update(system);
		m_PrefabType.Update(system);
		m_ResourceType.Update(system);
		m_TradeCostType.Update(system);
		m_InstalledUpgradeType.Update(system);
		m_StorageTransferRequestType.Update(system);
		m_TripNeededType.Update(system);
		m_OwnedVehicleType.Update(system);
		m_Buildings.Update(system);
		m_StorageLimits.Update(system);
		m_StorageCompanyDatas.Update(system);
		m_TransportCompanyData.Update(system);
		m_TransportStations.Update(system);
		return JobChunkExtensions.ScheduleParallel(new SetupStorageTransferJob
		{
			m_EntityType = m_EntityType,
			m_OutsideConnectionType = m_OutsideConnectionType,
			m_CargoTransportStations = m_CargoTransportStations,
			m_PrefabType = m_PrefabType,
			m_ResourceType = m_ResourceType,
			m_TradeCostType = m_TradeCostType,
			m_InstalledUpgradeType = m_InstalledUpgradeType,
			m_StorageTransferRequestType = m_StorageTransferRequestType,
			m_TripNeededType = m_TripNeededType,
			m_OwnedVehicleType = m_OwnedVehicleType,
			m_Buildings = m_Buildings,
			m_StorageLimits = m_StorageLimits,
			m_StorageCompanyDatas = m_StorageCompanyDatas,
			m_TransportCompanyDatas = m_TransportCompanyData,
			m_TransportStations = m_TransportStations,
			m_SetupData = setupData
		}, m_StorageQuery, inputDeps);
	}
}
