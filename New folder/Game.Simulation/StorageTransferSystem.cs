using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Economy;
using Game.Net;
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
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class StorageTransferSystem : GameSystemBase
{
	private struct StorageTransferEvent
	{
		public Entity m_Source;

		public Entity m_Destination;

		public float m_Distance;

		public Resource m_Resource;

		public int m_Amount;
	}

	[BurstCompile]
	private struct TransferJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<StorageTransfer> m_TransferType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<Game.Economy.Resources> m_ResourceType;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> m_Limits;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformation;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_Properties;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> m_StorageCompanyDatas;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> m_Resources;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BufferLookup<StorageTransferRequest> m_StorageTransferRequests;

		[ReadOnly]
		public BufferLookup<GuestVehicle> m_GuestVehicles;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> m_DeliveryTrucks;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_BuildingDatas;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.CargoTransportStation> m_CargoTransportStations;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<StorageTransferEvent>.ParallelWriter m_TransferQueue;

		[ReadOnly]
		public DeliveryTruckSelectData m_DeliveryTruckSelectData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public RandomSeed m_RandomSeed;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<StorageTransfer> nativeArray = chunk.GetNativeArray(ref m_TransferType);
			NativeArray<Entity> nativeArray2 = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabType);
			BufferAccessor<Game.Economy.Resources> bufferAccessor = chunk.GetBufferAccessor(ref m_ResourceType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray2[i];
				StorageTransfer storageTransfer = nativeArray[i];
				Entity prefab = nativeArray3[i].m_Prefab;
				if (!m_Limits.HasComponent(prefab) || !m_StorageCompanyDatas.HasComponent(prefab))
				{
					continue;
				}
				StorageCompanyData data = m_StorageCompanyDatas[prefab];
				if (m_InstalledUpgrades.HasBuffer(entity))
				{
					UpgradeUtils.CombineStats(ref data, m_InstalledUpgrades[entity], ref m_Prefabs, ref m_StorageCompanyDatas);
				}
				int num = EconomyUtils.CountResources(data.m_StoredResources);
				if (num == 0)
				{
					continue;
				}
				int num2 = GetStorageLimit(entity, prefab) / num;
				DynamicBuffer<Game.Economy.Resources> resources = bufferAccessor[i];
				int resources2 = EconomyUtils.GetResources(storageTransfer.m_Resource, resources);
				if (m_PathInformation.HasComponent(entity))
				{
					PathInformation pathInformation = m_PathInformation[entity];
					if ((pathInformation.m_State & PathFlags.Pending) != 0)
					{
						continue;
					}
					Entity entity2 = ((storageTransfer.m_Amount < 0) ? pathInformation.m_Origin : pathInformation.m_Destination);
					bool flag = m_OutsideConnections.HasComponent(entity2);
					bool flag2 = m_CargoTransportStations.HasComponent(entity2);
					if ((m_Properties.HasComponent(entity2) || flag) && entity != entity2)
					{
						prefab = m_Prefabs[entity2].m_Prefab;
						UpgradeUtils.TryGetCombinedComponent(entity2, out data, ref m_Prefabs, ref m_StorageCompanyDatas, ref m_InstalledUpgrades);
						num = EconomyUtils.CountResources(data.m_StoredResources);
						if (num == 0)
						{
							m_CommandBuffer.RemoveComponent<StorageTransfer>(unfilteredChunkIndex, entity);
							m_CommandBuffer.RemoveComponent<PathInformation>(unfilteredChunkIndex, entity);
							m_CommandBuffer.RemoveComponent<PathElement>(unfilteredChunkIndex, entity);
							continue;
						}
						int targetCapacity = GetStorageLimit(entity2, prefab) / num;
						resources = m_Resources[entity2];
						long num3 = EconomyUtils.GetResources(storageTransfer.m_Resource, resources);
						int allBuyingResourcesTrucks = VehicleUtils.GetAllBuyingResourcesTrucks(entity2, storageTransfer.m_Resource, ref m_DeliveryTrucks, ref m_GuestVehicles, ref m_LayoutElements);
						num3 -= allBuyingResourcesTrucks;
						if (m_StorageTransferRequests.HasBuffer(entity2))
						{
							long num4 = 0L;
							DynamicBuffer<StorageTransferRequest> dynamicBuffer = m_StorageTransferRequests[entity2];
							for (int j = 0; j < dynamicBuffer.Length; j++)
							{
								StorageTransferRequest storageTransferRequest = dynamicBuffer[j];
								if (storageTransferRequest.m_Resource == storageTransfer.m_Resource)
								{
									num4 += (((storageTransferRequest.m_Flags & StorageTransferFlags.Incoming) != 0) ? storageTransferRequest.m_Amount : (-storageTransferRequest.m_Amount));
								}
							}
							num3 += num4;
						}
						if (flag2 || flag)
						{
							if (storageTransfer.m_Amount < 0)
							{
								if (num3 > 0)
								{
									storageTransfer.m_Amount = -math.min((int)num3, math.abs(storageTransfer.m_Amount));
								}
								else
								{
									storageTransfer.m_Amount = 0;
								}
							}
						}
						else
						{
							storageTransfer.m_Amount = CalculateTransferableAmount(storageTransfer.m_Amount, resources2, num2, (int)math.max(0L, num3), targetCapacity);
						}
						m_DeliveryTruckSelectData.TrySelectItem(ref random, storageTransfer.m_Resource, math.abs(storageTransfer.m_Amount), out var item);
						if (storageTransfer.m_Amount != 0 && (float)item.m_Cost / (float)math.min(math.abs(storageTransfer.m_Amount), item.m_Capacity) <= kMaxTransportUnitCost)
						{
							int num5 = math.abs(storageTransfer.m_Amount) / item.m_Capacity * item.m_Capacity;
							if (num5 != 0)
							{
								m_DeliveryTruckSelectData.TrySelectItem(ref random, storageTransfer.m_Resource, math.abs(storageTransfer.m_Amount) - num5, out item);
								if (math.abs(storageTransfer.m_Amount) - num5 > 0 && (float)(item.m_Cost / (math.abs(storageTransfer.m_Amount) - num5)) > kMaxTransportUnitCost)
								{
									storageTransfer.m_Amount = ((storageTransfer.m_Amount > 0) ? num5 : (-num5));
								}
							}
							if (storageTransfer.m_Amount != 0)
							{
								m_TransferQueue.Enqueue(new StorageTransferEvent
								{
									m_Amount = storageTransfer.m_Amount,
									m_Destination = entity2,
									m_Source = entity,
									m_Distance = pathInformation.m_Distance,
									m_Resource = storageTransfer.m_Resource
								});
							}
						}
						m_CommandBuffer.RemoveComponent<StorageTransfer>(unfilteredChunkIndex, entity);
						m_CommandBuffer.RemoveComponent<PathInformation>(unfilteredChunkIndex, entity);
						m_CommandBuffer.RemoveComponent<PathElement>(unfilteredChunkIndex, entity);
					}
					else
					{
						m_CommandBuffer.RemoveComponent<StorageTransfer>(unfilteredChunkIndex, entity);
						m_CommandBuffer.RemoveComponent<PathInformation>(unfilteredChunkIndex, entity);
						m_CommandBuffer.RemoveComponent<PathElement>(unfilteredChunkIndex, entity);
					}
				}
				else
				{
					float fillProportion = (float)resources2 / (float)num2;
					FindTarget(unfilteredChunkIndex, entity, storageTransfer.m_Resource, storageTransfer.m_Amount, fillProportion, num2);
				}
			}
		}

		private int GetStorageLimit(Entity entity, Entity prefab)
		{
			StorageLimitData data = m_Limits[prefab];
			if (m_InstalledUpgrades.HasBuffer(entity))
			{
				UpgradeUtils.CombineStats(ref data, m_InstalledUpgrades[entity], ref m_Prefabs, ref m_Limits);
			}
			if (m_Properties.HasComponent(entity) && m_Prefabs.HasComponent(m_Properties[entity].m_Property))
			{
				Entity prefab2 = m_Prefabs[m_Properties[entity].m_Property].m_Prefab;
				return data.GetAdjustedLimitForWarehouse(m_SpawnableDatas.HasComponent(prefab2) ? m_SpawnableDatas[prefab2] : new SpawnableBuildingData
				{
					m_Level = 1
				}, m_SpawnableDatas.HasComponent(prefab2) ? m_BuildingDatas[prefab2] : new BuildingData
				{
					m_LotSize = new int2(1, 1)
				});
			}
			if (m_OutsideConnections.HasComponent(entity))
			{
				return data.m_Limit;
			}
			return 0;
		}

		private void FindTarget(int chunkIndex, Entity storage, Resource resource, int amount, float fillProportion, int capacity)
		{
			m_CommandBuffer.AddComponent(chunkIndex, storage, new PathInformation
			{
				m_State = PathFlags.Pending
			});
			m_CommandBuffer.AddBuffer<PathElement>(chunkIndex, storage);
			float transportCost = EconomyUtils.GetTransportCost(1f, math.abs(amount), m_ResourceDatas[m_ResourcePrefabs[resource]].m_Weight, StorageTransferFlags.Car);
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = 111.111115f,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(0.01f, 0.01f, transportCost, 0.01f),
				m_Methods = (PathMethod.Road | PathMethod.CargoTransport | PathMethod.CargoLoading),
				m_IgnoredRules = (RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget a = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_RoadTypes = RoadTypes.Car
			};
			SetupQueueTarget b = new SetupQueueTarget
			{
				m_Type = SetupTargetType.StorageTransfer,
				m_Methods = (PathMethod.Road | PathMethod.CargoLoading),
				m_RoadTypes = RoadTypes.Car,
				m_Entity = storage,
				m_Resource = resource,
				m_Value = amount,
				m_Value2 = fillProportion,
				m_Value3 = capacity
			};
			if (amount < 0)
			{
				CommonUtils.Swap(ref a, ref b);
			}
			SetupQueueItem value = new SetupQueueItem(storage, parameters, a, b);
			m_PathfindQueue.Enqueue(value);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct HandleTransfersJob : IJob
	{
		public NativeQueue<StorageTransferEvent> m_TransferQueue;

		public BufferLookup<TradeCost> m_TradeCosts;

		public ComponentLookup<Game.Companies.StorageCompany> m_StorageCompanies;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> m_TrackLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public ComponentLookup<Connected> m_ConnectedData;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Segment> m_SegmentData;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> m_RouteWaypoints;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInfos;

		[ReadOnly]
		public BufferLookup<PathElement> m_Paths;

		[ReadOnly]
		public ComponentLookup<Building> m_Buildings;

		[ReadOnly]
		public ComponentLookup<Curve> m_Curves;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnections;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> m_CityServiceUpkeeps;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceDatas;

		public ComponentLookup<CompanyStatisticData> m_CompanyStatistics;

		public BufferLookup<StorageTransferRequest> m_Requests;

		public BufferLookup<Game.Economy.Resources> m_Resources;

		public Entity m_City;

		private Entity GetStorageCompanyFromLane(Entity entity)
		{
			while (m_OwnerData.HasComponent(entity))
			{
				entity = m_OwnerData[entity].m_Owner;
				if (m_StorageCompanies.HasComponent(entity))
				{
					return entity;
				}
				if (!m_Renters.HasBuffer(entity))
				{
					continue;
				}
				DynamicBuffer<Renter> dynamicBuffer = m_Renters[entity];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity renter = dynamicBuffer[i].m_Renter;
					if (m_StorageCompanies.HasComponent(renter))
					{
						return renter;
					}
				}
			}
			return Entity.Null;
		}

		private Entity GetStorageCompanyFromWaypoint(Entity entity)
		{
			if (m_ConnectedData.HasComponent(entity))
			{
				entity = m_ConnectedData[entity].m_Connected;
				while (true)
				{
					if (m_StorageCompanies.HasComponent(entity))
					{
						return entity;
					}
					if (m_Renters.HasBuffer(entity))
					{
						DynamicBuffer<Renter> dynamicBuffer = m_Renters[entity];
						for (int i = 0; i < dynamicBuffer.Length; i++)
						{
							Entity renter = dynamicBuffer[i].m_Renter;
							if (m_StorageCompanies.HasComponent(renter))
							{
								return renter;
							}
						}
					}
					if (!m_OwnerData.HasComponent(entity))
					{
						break;
					}
					entity = m_OwnerData[entity].m_Owner;
				}
			}
			return Entity.Null;
		}

		private void GetStorageCompaniesFromSegment(Entity entity, out Entity startCompany, out Entity endCompany)
		{
			Entity owner = m_OwnerData[entity].m_Owner;
			Game.Routes.Segment segment = m_SegmentData[entity];
			DynamicBuffer<RouteWaypoint> dynamicBuffer = m_RouteWaypoints[owner];
			int num = segment.m_Index + 1;
			if (num == dynamicBuffer.Length)
			{
				num = 0;
			}
			startCompany = GetStorageCompanyFromWaypoint(dynamicBuffer[segment.m_Index].m_Waypoint);
			endCompany = GetStorageCompanyFromWaypoint(dynamicBuffer[num].m_Waypoint);
		}

		private float HandleCargoPath(PathInformation pathInformation, DynamicBuffer<PathElement> path, Resource resource, int amount, float weight)
		{
			float num = 0f;
			float num2 = 0f;
			Entity entity = pathInformation.m_Origin;
			StorageTransferFlags storageTransferFlags = (StorageTransferFlags)0;
			int num3 = path.Length;
			int num4 = 0;
			for (int i = 0; i < path.Length; i++)
			{
				Entity target = path[i].m_Target;
				if (m_Curves.HasComponent(target))
				{
					num2 += m_Curves[target].m_Length * math.abs(path[i].m_TargetDelta.y - path[i].m_TargetDelta.x);
				}
				if (m_CarLaneData.HasComponent(target))
				{
					storageTransferFlags |= StorageTransferFlags.Car;
					num3 = math.min(num3, i);
					num4 = math.max(num4, i + 1);
				}
				else if (m_TrackLaneData.HasComponent(target))
				{
					storageTransferFlags |= StorageTransferFlags.Track;
					num3 = math.min(num3, i);
					num4 = math.max(num4, i + 1);
				}
				else if (m_PedestrianLaneData.HasComponent(target))
				{
					Entity storageCompanyFromLane = GetStorageCompanyFromLane(target);
					if (storageCompanyFromLane != Entity.Null && storageCompanyFromLane != entity)
					{
						num += AddCargoPathSection(entity, storageCompanyFromLane, path, num3, num4 - num3, storageTransferFlags, resource, amount, weight, num2);
						entity = storageCompanyFromLane;
						storageTransferFlags = (StorageTransferFlags)0;
						num3 = path.Length;
						num4 = 0;
						num2 = 0f;
					}
				}
				else if (m_ConnectionLaneData.HasComponent(target))
				{
					Game.Net.ConnectionLane connectionLane = m_ConnectionLaneData[target];
					if ((connectionLane.m_Flags & ConnectionLaneFlags.Road) != 0)
					{
						storageTransferFlags |= StorageTransferFlags.Car;
						num3 = math.min(num3, i);
						num4 = math.max(num4, i + 1);
					}
					else if ((connectionLane.m_Flags & ConnectionLaneFlags.Track) != 0)
					{
						storageTransferFlags |= StorageTransferFlags.Track;
						num3 = math.min(num3, i);
						num4 = math.max(num4, i + 1);
					}
				}
				else if (m_SegmentData.HasComponent(target))
				{
					GetStorageCompaniesFromSegment(target, out var startCompany, out var endCompany);
					if (startCompany != Entity.Null && startCompany != entity)
					{
						num += AddCargoPathSection(entity, startCompany, path, num3, num4 - num3, storageTransferFlags, resource, amount, weight, num2);
						entity = startCompany;
						storageTransferFlags = (StorageTransferFlags)0;
						num3 = path.Length;
						num4 = 0;
						num2 = 0f;
					}
					storageTransferFlags |= StorageTransferFlags.Transport;
					if (endCompany != Entity.Null && endCompany != entity)
					{
						num += AddCargoPathSection(entity, endCompany, path, num3, num4 - num3, storageTransferFlags, resource, amount, weight, num2);
						entity = endCompany;
						storageTransferFlags = (StorageTransferFlags)0;
						num3 = path.Length;
						num4 = 0;
						num2 = 0f;
					}
				}
			}
			if (pathInformation.m_Destination != entity)
			{
				num += AddCargoPathSection(entity, pathInformation.m_Destination, path, num3, num4 - num3, storageTransferFlags, resource, amount, weight, num2);
			}
			return num;
		}

		private void AddRequest(DynamicBuffer<StorageTransferRequest> requests, Entity destination, StorageTransferFlags flags, Resource resource, int amount)
		{
			if (m_Buildings.HasComponent(destination) && BuildingUtils.CheckOption(m_Buildings[destination], BuildingOption.Inactive))
			{
				return;
			}
			bool flag = false;
			for (int i = 0; i < requests.Length; i++)
			{
				StorageTransferRequest storageTransferRequest = requests[i];
				if (storageTransferRequest.m_Target == destination && storageTransferRequest.m_Resource == resource && storageTransferRequest.m_Flags == flags)
				{
					storageTransferRequest.m_Amount += amount;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				requests.Add(new StorageTransferRequest
				{
					m_Amount = math.abs(amount),
					m_Resource = resource,
					m_Target = destination,
					m_Flags = flags
				});
			}
		}

		private float AddCargoPathSection(Entity origin, Entity destination, DynamicBuffer<PathElement> path, int startIndex, int length, StorageTransferFlags flags, Resource resource, int amount, float weight, float distance)
		{
			if (m_Requests.HasBuffer(origin) && m_Requests.HasBuffer(destination))
			{
				DynamicBuffer<StorageTransferRequest> requests = m_Requests[origin];
				AddRequest(requests, destination, flags, resource, amount);
				requests = m_Requests[destination];
				AddRequest(requests, origin, flags | StorageTransferFlags.Incoming, resource, math.abs(amount));
				EconomyUtils.GetTransportCost(distance, math.abs(amount), weight, flags);
				return EconomyUtils.GetTransportCost(distance, math.abs(amount), weight, flags);
			}
			return 0f;
		}

		public void Execute()
		{
			DynamicBuffer<TradeCost> costs = m_TradeCosts[m_City];
			StorageTransferEvent item;
			while (m_TransferQueue.TryDequeue(out item))
			{
				if (!m_PathInfos.HasComponent(item.m_Source) || !m_Paths.HasBuffer(item.m_Source))
				{
					continue;
				}
				float weight = EconomyUtils.GetWeight(item.m_Resource, m_ResourcePrefabs, ref m_ResourceDatas);
				float num = HandleCargoPath(m_PathInfos[item.m_Source], m_Paths[item.m_Source], item.m_Resource, item.m_Amount, weight);
				DynamicBuffer<TradeCost> costs2 = m_TradeCosts[item.m_Source];
				DynamicBuffer<TradeCost> costs3 = m_TradeCosts[item.m_Destination];
				TradeCost tradeCost = EconomyUtils.GetTradeCost(item.m_Resource, costs2);
				TradeCost tradeCost2 = EconomyUtils.GetTradeCost(item.m_Resource, costs3);
				float num2 = num / (1f + (float)math.abs(item.m_Amount));
				if (item.m_Amount > 0)
				{
					tradeCost.m_SellCost = math.lerp(tradeCost.m_SellCost, num2 + tradeCost2.m_SellCost, 0.5f);
					tradeCost2.m_BuyCost = math.lerp(tradeCost2.m_BuyCost, num2 + tradeCost.m_BuyCost, 0.5f);
				}
				else
				{
					tradeCost.m_BuyCost = math.lerp(tradeCost.m_BuyCost, num2 + tradeCost2.m_BuyCost, 0.5f);
					tradeCost2.m_SellCost = math.lerp(tradeCost2.m_SellCost, num2 + tradeCost.m_SellCost, 0.5f);
				}
				int num3 = Mathf.RoundToInt(kStorageProfit * num);
				EconomyUtils.GetTradeCost(item.m_Resource, costs);
				if (!m_OutsideConnections.HasComponent(item.m_Source))
				{
					EconomyUtils.SetTradeCost(item.m_Resource, tradeCost, costs2, keepLastTime: true);
					if (m_Resources.HasBuffer(item.m_Source) && !m_CityServiceUpkeeps.HasComponent(item.m_Source))
					{
						EconomyUtils.AddResources(Resource.Money, num3, m_Resources[item.m_Source]);
					}
				}
				else if (item.m_Amount > 0)
				{
					EconomyUtils.SetTradeCost(item.m_Resource, tradeCost2, costs, keepLastTime: false, 0.1f, 0f);
					EconomyUtils.GetTradeCost(item.m_Resource, costs);
				}
				else
				{
					EconomyUtils.SetTradeCost(item.m_Resource, tradeCost2, costs, keepLastTime: false, 0f, 0.1f);
					EconomyUtils.GetTradeCost(item.m_Resource, costs);
				}
				if (!m_OutsideConnections.HasComponent(item.m_Destination))
				{
					EconomyUtils.SetTradeCost(item.m_Resource, tradeCost2, costs3, keepLastTime: true);
					if (m_Resources.HasBuffer(item.m_Destination) && !m_CityServiceUpkeeps.HasComponent(item.m_Destination))
					{
						EconomyUtils.AddResources(Resource.Money, num3, m_Resources[item.m_Destination]);
					}
				}
				else if (item.m_Amount > 0)
				{
					EconomyUtils.SetTradeCost(item.m_Resource, tradeCost, costs, keepLastTime: false, 0f, 0.1f);
					EconomyUtils.GetTradeCost(item.m_Resource, costs);
				}
				else
				{
					EconomyUtils.SetTradeCost(item.m_Resource, tradeCost, costs, keepLastTime: false, 0.1f, 0f);
					EconomyUtils.GetTradeCost(item.m_Resource, costs);
				}
				Game.Companies.StorageCompany value = m_StorageCompanies[item.m_Source];
				value.m_LastTradePartner = item.m_Destination;
				m_StorageCompanies[item.m_Source] = value;
				Game.Companies.StorageCompany value2 = m_StorageCompanies[item.m_Destination];
				value2.m_LastTradePartner = item.m_Source;
				m_StorageCompanies[item.m_Destination] = value2;
				if (m_CompanyStatistics.HasComponent(item.m_Source))
				{
					CompanyStatisticData value3 = m_CompanyStatistics[item.m_Source];
					value3.m_CurrentNumberOfCustomers++;
					m_CompanyStatistics[item.m_Source] = value3;
				}
				if (m_CompanyStatistics.HasComponent(item.m_Destination))
				{
					CompanyStatisticData value4 = m_CompanyStatistics[item.m_Destination];
					value4.m_CurrentCostOfBuyingResources += math.abs(num3);
					m_CompanyStatistics[item.m_Destination] = value4;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<StorageTransfer> __Game_Companies_StorageTransfer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageLimitData> __Game_Companies_StorageLimitData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageCompanyData> __Game_Prefabs_StorageCompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<StorageTransferRequest> __Game_Companies_StorageTransferRequest_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.CargoTransportStation> __Game_Buildings_CargoTransportStation_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<GuestVehicle> __Game_Vehicles_GuestVehicle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.DeliveryTruck> __Game_Vehicles_DeliveryTruck_RO_ComponentLookup;

		public BufferLookup<TradeCost> __Game_Companies_TradeCost_RW_BufferLookup;

		public ComponentLookup<Game.Companies.StorageCompany> __Game_Companies_StorageCompany_RW_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Routes.Segment> __Game_Routes_Segment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Connected> __Game_Routes_Connected_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.TrackLane> __Game_Net_TrackLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<RouteWaypoint> __Game_Routes_RouteWaypoint_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		public BufferLookup<StorageTransferRequest> __Game_Companies_StorageTransferRequest_RW_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CityServiceUpkeep> __Game_City_CityServiceUpkeep_RO_ComponentLookup;

		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

		public ComponentLookup<CompanyStatisticData> __Game_Companies_CompanyStatisticData_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Companies_StorageTransfer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<StorageTransfer>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Economy.Resources>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Companies_StorageLimitData_RO_ComponentLookup = state.GetComponentLookup<StorageLimitData>(isReadOnly: true);
			__Game_Prefabs_StorageCompanyData_RO_ComponentLookup = state.GetComponentLookup<StorageCompanyData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Economy_Resources_RO_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Companies_StorageTransferRequest_RO_BufferLookup = state.GetBufferLookup<StorageTransferRequest>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Buildings_CargoTransportStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.CargoTransportStation>(isReadOnly: true);
			__Game_Vehicles_GuestVehicle_RO_BufferLookup = state.GetBufferLookup<GuestVehicle>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_DeliveryTruck_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.DeliveryTruck>(isReadOnly: true);
			__Game_Companies_TradeCost_RW_BufferLookup = state.GetBufferLookup<TradeCost>();
			__Game_Companies_StorageCompany_RW_ComponentLookup = state.GetComponentLookup<Game.Companies.StorageCompany>();
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Routes_Segment_RO_ComponentLookup = state.GetComponentLookup<Game.Routes.Segment>(isReadOnly: true);
			__Game_Routes_Connected_RO_ComponentLookup = state.GetComponentLookup<Connected>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_TrackLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.TrackLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Routes_RouteWaypoint_RO_BufferLookup = state.GetBufferLookup<RouteWaypoint>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Companies_StorageTransferRequest_RW_BufferLookup = state.GetBufferLookup<StorageTransferRequest>();
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_City_CityServiceUpkeep_RO_ComponentLookup = state.GetComponentLookup<CityServiceUpkeep>(isReadOnly: true);
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
			__Game_Companies_CompanyStatisticData_RW_ComponentLookup = state.GetComponentLookup<CompanyStatisticData>();
		}
	}

	public static readonly float kStorageProfit = 0.01f;

	public static readonly float kMaxTransportUnitCost = 0.01f;

	private EntityQuery m_TransferGroup;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private ResourceSystem m_ResourceSystem;

	private VehicleCapacitySystem m_VehicleCapacitySystem;

	private CitySystem m_CitySystem;

	private NativeQueue<StorageTransferEvent> m_TransferQueue;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_VehicleCapacitySystem = base.World.GetOrCreateSystemManaged<VehicleCapacitySystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TransferQueue = new NativeQueue<StorageTransferEvent>(Allocator.Persistent);
		m_TransferGroup = GetEntityQuery(ComponentType.ReadOnly<StorageTransfer>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Game.Economy.Resources>(), ComponentType.ReadWrite<TripNeeded>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		RequireForUpdate(m_TransferGroup);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_TransferQueue.Dispose();
		base.OnDestroy();
	}

	public static int CalculateTransferableAmount(int original, int sourceAmount, int sourceCapacity, int targetAmount, int targetCapacity)
	{
		if (targetCapacity == 0 && sourceCapacity == 0)
		{
			return 0;
		}
		if (original > 0)
		{
			return math.min(targetCapacity - targetAmount, original);
		}
		return -math.min(sourceCapacity - sourceAmount, -original);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new TransferJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransferType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Companies_StorageTransfer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourceType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PathInformation = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Properties = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Limits = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageLimitData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StorageCompanyDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageCompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_StorageTransferRequests = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_StorageTransferRequest_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnableDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CargoTransportStations = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CargoTransportStation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GuestVehicles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_GuestVehicle_RO_BufferLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_DeliveryTrucks = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_DeliveryTruck_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_TransferQueue = m_TransferQueue.AsParallelWriter(),
			m_DeliveryTruckSelectData = m_VehicleCapacitySystem.GetDeliveryTruckSelectData(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_RandomSeed = RandomSeed.Next()
		}, m_TransferGroup, base.Dependency);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		JobHandle jobHandle2 = IJobExtensions.Schedule(new HandleTransfersJob
		{
			m_TradeCosts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_TradeCost_RW_BufferLookup, ref base.CheckedStateRef),
			m_StorageCompanies = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_StorageCompany_RW_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SegmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Segment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Routes_Connected_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TrackLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_TrackLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Buildings = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_RouteWaypoints = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Routes_RouteWaypoint_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathInfos = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Paths = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_Requests = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_StorageTransferRequest_RW_BufferLookup, ref base.CheckedStateRef),
			m_Curves = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnections = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityServiceUpkeeps = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_City_CityServiceUpkeep_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Resources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_ResourceDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompanyStatistics = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyStatisticData_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TransferQueue = m_TransferQueue,
			m_City = m_CitySystem.City
		}, jobHandle);
		m_ResourceSystem.AddPrefabsReader(jobHandle2);
		base.Dependency = jobHandle2;
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
	public StorageTransferSystem()
	{
	}
}
